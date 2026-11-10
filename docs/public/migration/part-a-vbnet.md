# Rule Patterns for VB.NET (Part A of the Migration Documentation)

Part A describes the Basic Authentication patterns the tool looks for in VB.NET code, so that you can find them in your own source without the tool, and so that you can understand why the tool reported a finding. Each pattern class has an identifier, such as `VB-007`, which is the same identifier a tool report shows, so a finding and this document can be matched.

Every pattern class below has the same five parts:

- **Identifier and name.** In the heading.
- **What it looks like.** A short description and a placeholder example.
- **How to search.** A pattern you can run with `rg` (ripgrep). VB.NET is not case-sensitive, so every search ignores letter case (`-i`). If you use `grep`, run `grep -rniE --include="*.vb"` with the same expression, replacing `\s` by `[[:space:]]`, `\w` by `[[:alnum:]_]` and `\W` by `[^[:alnum:]_]`. In PowerShell the expression works unchanged with `Select-String`: `Get-ChildItem -Recurse -Include *.vb | Select-String -Pattern '<expression>'`, which also ignores letter case. A file saved as UTF-16, which some Windows editors do by default, is not read by `grep`, which sees it as binary and reports no match; `rg` and `Select-String` read it, so use one of them when your files may be UTF-16. A search reads one line at a time, so a statement continued over several lines with ` _` is found through the line that holds the word the search looks for.
- **Confidence.** When a match is High, Medium or Low, including the cases that go to manual review.
- **Which flow.** The signals that indicate Client Credentials or Authorization Code, and the default.

Every example uses obvious placeholders such as `userName`, `password` and `<host>`. No example contains a real credential. To decide what to do about a pattern, go to [Part C](part-c-vbnet.md); for where the new secrets go, see Secret Management (Part B of the Migration Documentation), [`part-b-secret-management.md`](part-b-secret-management.md).

The tool reads files with the extension `.vb`, including the code-behind of a web page (`.aspx.vb`), and nothing else. The searches below use `--glob '*.vb'`. Code written in Visual Basic 6, VBA, VBScript or another Basic dialect is not VB.NET and is not read; neither is VB.NET written inside a web page's markup. See [Declared unsupported coverage](#declared-unsupported-coverage). The catalog describes the constructs by their shape, so a pattern class can match code from a library it does not name.

**The two runtimes.** VB.NET code runs on .NET Framework and on .NET, and the two differ in ways that matter here, and each class says where. The retired Relativity RSAPI client and the classic Windows Communication Foundation transport settings belong to .NET Framework. `My.Computer.Network` is available on .NET only in Windows desktop applications.

**Statements span lines.** A VB.NET statement ends at the end of the line unless it is continued with a trailing ` _` or by a line end that the language allows (after a comma, an opening parenthesis or an operator). The tool reads a statement across its continuation lines, and a window of "five lines" in this document means five statements, with a continued statement counting as one. A colon can also put several statements on one line.

## Find your code

Start here if you know what your code does and not which class describes it. Each row names a common situation and the class that covers it. Open the class, read its description and its search, and check the match against your code. A situation that is not in the table may still be covered: the classes are listed in full below, and what the tool does not read is in [Declared unsupported coverage](#declared-unsupported-coverage).

| Your code does this | Class |
| --- | --- |
| Builds the Relativity SDK user name and password credential | [VB-001](#vb-001) |
| Builds the retired RSAPI client with a user name and password | [VB-002](#vb-002) |
| Adds a credential to a credential cache under the `Basic` scheme | [VB-003](#vb-003) |
| Uses an authenticator type or an extension method named for Basic authentication, from a third-party library | [VB-004](#vb-004), [VB-005](#vb-005) |
| Creates an authentication header value with the scheme `Basic` | [VB-006](#vb-006) |
| Writes the `Authorization` header by name, with `.Add("Authorization", ...)`, `.Set(...)` or `.Headers("Authorization") = ...` | [VB-007](#vb-007) |
| Writes the header through the framework's header enumeration, with no `"Authorization"` string | [VB-008](#vb-008) |
| Sets the Basic transport credential type of a WCF binding | [VB-009](#vb-009) |
| Builds the Import API object, or the settings of an import job, with a user name and a password | [VB-010](#vb-010), [VB-011](#vb-011) |
| Gives a `NetworkCredential` to a web client, a handler or a web request | [VB-012](#vb-012) |
| Builds a `NetworkCredential` that nothing in the file visibly uses | [VB-013](#vb-013) |
| Joins a user name and a password with a colon and encodes the result to Base64 | [VB-014](#vb-014) |
| Sends a token or key under the `Basic` scheme, with one half of the pair empty | [VB-015](#vb-015) |
| Calls `My.Computer.Network.DownloadFile` or `UploadFile` with a user name and a password | [VB-016](#vb-016) |
| Calls a late-bound HTTP object, such as MSXML or WinHTTP, with a user name and a password | [VB-017](#vb-017) |
| Passes a credential to a mail client, a proxy, an FTP request, a directory object or a process | [VB-018](#vb-018) |
| Defines its own function that builds the Basic header or the credential | [VB-019](#vb-019) |

## How confidence is assigned

Every finding receives exactly one of three levels. The levels depend on what the tool can establish from the file, and never on how sure it is about the rest of your system.

| Level | What it means |
| --- | --- |
| **High** | The code almost certainly performs Basic Authentication against a live call path. A known credential-bearing type or call is used, or a credential is added to a credential cache under the Basic scheme, in executable, non-test code. |
| **Medium** | The code matches a Basic Authentication shape, but the credential source or the call path cannot be established from the file alone. A High match in a file of test code is also Medium. |
| **Low** | The match is present in the text but unlikely to be live authenticating code: a comment, a documentation string, a mock or fake, sample or example code, or a vendored or generated file. Two classes are also Low by their own definition. |

Five rules apply to every class:

- **A class states a base level, and levels combine by taking the lower.** A class whose construct shows both the scheme and the credential carrier keeps its base level. A class whose construct does not, because the scheme or the credential is in another statement, or because the destination is unknown, lands one level lower. That is why several classes whose base level is High are reported as Medium.
- **Ambiguity resolves downward.** When two levels are possible, the lower one is used, so that uncertainty goes to manual review and not to a recommendation. A scheme held in a constant that cannot be resolved in the file gives Medium; a scheme that resolves to something other than Basic gives no finding.
- **A file's status only lowers the level.** A match is demoted to **Low** when it sits in a comment or in a string body, or in a file the tool recognizes as mock, generated, sample, example or vendored code. **A file the tool recognizes as test code is demoted to Medium and no lower:** a match that would be High is Medium, it is given a recommendation, and the report marks the finding as test code. Nothing raises a level.
- **A Low match is reported for manual review, with no recommendation and no flow.** It is not evidence that the code does not authenticate. Part C says how to judge each case.
- **An argument may be a call or an index, and such an argument is never a literal credential.** Wherever a class below reads a user name, a password, a token or an address from the call, the argument may be a quoted literal, a variable in any spelling, a member access, or one of those followed by one or two parenthesized groups, such as `settings("<key>")`, `Decode(raw, key)` or `parts(1)`. In VB.NET a call and an index are written the same way, and the tool reads both. A value read from configuration or computed by a call is code, so a match that captures one never raises the in-code secret warning.

**Why a `NetworkCredential` on a handler is Medium and a `NetworkCredential` that nothing uses is also Medium.** `NetworkCredential` declares no scheme: the same type carries integrated Windows authentication, mail, file transfer and proxy credentials as well as Basic. The tool therefore decides by where the credential goes. A credential given to a web client, handler or web request whose type the file shows (VB-012) is reported with a recommendation, and you confirm the destination. A credential whose consumer the tool does not recognize (VB-013) is reported the same way, because a credential built from a user name and a password is the commonest shape of a Basic exchange; the cost is that a credential built for another purpose, such as a share, a mail server or a directory, is reported too, and you dismiss it on review. If the credential goes to a mail client, a proxy, an FTP request, a directory object or a process, it is VB-018 and Low.

What marks a file for demotion in VB.NET:

- **Test code, Medium at most:** a file name that ends in `.Tests.vb`, `.Test.vb`, `.Spec.vb` or `Tests.vb`; a folder named `test`, `tests`, `spec` or `fixtures`, or a folder whose name ends in `.Test` or `.Tests`; and a test attribute such as `<TestMethod>`, `<TestClass>`, `<Fact>`, `<Theory>`, `<Test>` or `<TestFixture>` found in code and not in a comment or a string. A finding in test code is given the recommendation of its class and is marked: the report writes `Test code: yes` under it, and the JSON data file sets `testCode` to `true`. The suffix `Tests.vb` also reaches a file that is not a test, such as `Contests.vb`, which is then reported at Medium at most and marked as test code. A bare `Test.vb` is not a signal, so `Latest.vb` is not demoted.
- **Mocks and fakes, Low:** a file name that ends in `Fake.vb`, `Mock.vb` or `Stub.vb`, or begins with `Fake`, `Mock` or `Stub`. VB.NET has no mock folder, so a mock is recognized by its file name and by nothing else; a mock with a neutral name is reported at Medium when it is in test code, and at the level of its class when it is not.
- **Generated code, Low:** a file name that ends in `.Designer.vb`, `.g.vb` or `.generated.vb`, and a folder named `Service References` or `Web References`.
- **Sample and example code, Low:** a folder named `sample`, `samples`, `example` or `examples`, and a file name that begins with `Sample` or `Example`.
- **Letter case does not matter** for names and folders, and a folder is matched as a whole folder name, so `Contest` does not match.
- **Test and another kind together are Low.** A file that is test code and also a mock, a sample or generated, such as `MockServiceTests.vb`, is Low, and the report still marks it as test code.
- **The cost of the file-name rule for samples:** the prefix rule also reaches a real file whose name starts with the word, such as `SampleRateConverter.vb`, and that match is reported at Low for manual review instead of being lost.
- **Vendored code:** a folder named `vendor` or `third-party` in any letter case, matched as a whole folder name. The tool reads vendored folders, it does not skip them. A vendored file that is not under a folder of those names is not demoted and is judged like any other file.
- **File content:** a generated-file header (`<auto-generated` in a comment) or a `GeneratedCode` attribute marks a file as generated code, and is Low. A test file that has none of the test names, none of the test attributes and is not in a test folder is judged as production code and keeps its level, which may be High. A hand-written `My Project\Settings.vb` is not demoted, and neither is a file called `Reference.vb` that is not under a service-reference folder.
- **Not a signal: a folder named `Deployment`.** The folder holds the deployment code this tool exists to find, so code under it is reported at its full level. The cost is that a deployment script that only illustrates a pattern is reported as if it were live.

**A literal secret is a separate warning.** When a class captures a credential and the password or token is written literally into the code, the tool also raises an in-code secret warning. The warning is independent of the level: a demoted match keeps it. It is read from where the credential sits, not from its value: a literal inside a string, including documentation text, raises it, and a literal inside a comment does not. A value that is a call or an index is code, not a literal, and does not raise it.

**How the tool reads strings and comments.** It marks where each comment, string and interpolated string begins and ends so that a match in text is demoted. A double quote that the language does not close on its line, for example one inside an XML literal, a stray quote in a long string or a quote in a block that a `#If` condition switches off, makes the tool read the rest of the file as text. Every match after it is reported at Low, with no recommendation. If a file you expect to produce findings reports only Low ones after some point, look for an unbalanced double quote above that point. This also happens to a string delimited by a mixed pair of typographic quotes; see [Declared unsupported coverage](#declared-unsupported-coverage).

## Pattern classes

Nineteen classes are registered for VB.NET. Seventeen of them can produce a recommendation. `VB-018` and `VB-019` are always Low and always go to manual review.

### VB-001 · SdkUserNamePasswordCredentials

<a id="vb-001"></a>

A Relativity SDK credential object built from a user name and a password, usually handed to the SDK's service factory settings. Both argument forms count: values that arrive in variables or calls, and values written literally into the code. The type may be written bare, qualified, with `Global.` in front, or through an `Imports` alias of its namespace, and the object may be created standing alone, as an argument, after `As New`, after `= New`, or by an assignment after a bare `Dim`. Arguments may be named in order (`userName:=`, `password:=`) and the call may be continued over several lines.

**Example.** Placeholders only:

```vbnet
	Dim credentials As Credentials = New UsernamePasswordCredentials(userName, password)
	Dim settings As New ServiceFactorySettings(restEndpointUri, credentials)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'UsernamePasswordCredentials'
```

**Confidence.** **High.** The type name declares both that the object is a credential and that it carries a user name and a password, so the construct is High wherever it sits in executable, non-test code, and whatever namespace is written in front of the name. A credential type of the same SDK that carries a bearer token does not match. **Low** when the match is in a comment, in a string body, or in a file the tool recognizes as generated or vendored code (a file of test code is Medium at most). When the password is written literally into the code, the match also raises the in-code secret warning, which is separate from the level and is not removed by a Low level. When the same construct is also a legacy RSAPI client construction, VB-002 is reported instead of this class, so that one construct produces one finding. A type of your own with the same simple name as the SDK type, declared as a class or structure in the same file, is not reported when it is written without a namespace; written with a namespace it is still read. Creating the object with an object initializer, or with `Activator.CreateInstance`, is not read; see [Declared unsupported coverage](#declared-unsupported-coverage). See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-001-migration).

### VB-002 · SdkLegacyRsapiCredentials

<a id="vb-002"></a>

The retired Relativity RSAPI client built with a nested SDK user name and password credential. The client belongs to .NET Framework. The recommendation for this class differs from VB-001's because the client is retired: the guidance is to move the call to the current service, not to swap the credential.

**Example.** Placeholders only:

```vbnet
	Dim client As New RSAPIClient(rsapiUri, New UsernamePasswordCredentials(userName, password))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'RSAPIClient'
```

**Confidence.** **High.** The client type and the nested credential type are both written in one expression. A client built with another kind of credential, or obtained without one, does not match; a client obtained from a generic typed proxy factory with no credential is not read. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). This class takes the place of VB-001 on the same construct.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-002-migration).

### VB-003 · CredentialCacheBasicScheme

<a id="vb-003"></a>

A credential added to a credential cache under the `Basic` scheme: `cache.Add(<address>, "Basic", <credential>)`. The third argument may be a `New NetworkCredential(...)` construction, a call or a variable, because the scheme argument and the cache already declare Basic. The scheme may be a quoted literal in any letter case, or a bare constant that the file declares once as `"Basic"`. A cache entry whose scheme is another one, such as `Digest` or `NTLM`, is not Basic and is not reported. The four-argument form that takes a host and a port is a mail-server entry and is VB-018.

**Example.** Placeholders only:

```vbnet
	cache.Add(New Uri(instanceUrl), "Basic", New NetworkCredential(userName, password))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' '\.Add\(.*,\s*"Basic"\s*,'
rg -n -i --glob '*.vb' 'CredentialCache'
```

**Confidence.** **High.** The scheme argument and the cache entry declare the exchange, so the construct is High in executable, non-test code. A scheme held in a constant that the tool cannot resolve (a constant in another file, one declared in two branches, or a qualified name) gives Medium, or no match for a qualified name; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-003-migration).

### VB-004 · ThirdPartyBasicAuthenticatorType

<a id="vb-004"></a>

An authenticator type of a third-party library whose name declares the scheme, such as `HttpBasicAuthenticator`, built with a user name and a password. The class is defined by shape: a type name that carries both the word `Basic` and an authenticator word, built with a credential pair. Requiring both words is what keeps it safe, because `Basic` alone is too common. The type may be written bare or qualified, and may be assigned to a property such as `client.Authenticator`. Extra arguments, such as an encoding, do not stop the match.

**Example.** Placeholders only:

```vbnet
	Dim authenticator As New HttpBasicAuthenticator(userName, password)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'Basic\w*Authenticator'
```

**Confidence.** **High.** The type name declares both the scheme and the role. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-004-migration).

### VB-005 · SchemeNamingExtensionMethod

<a id="vb-005"></a>

A fluent extension method of a third-party library whose name declares the scheme, called with a user name and a password: `request.WithBasicAuth(userName, password)`. The class is defined by shape: a method name that carries the word `Basic` and an authentication word, with two credential arguments. It is read as an ordinary call and as the leading-dot call inside a `With` block.

**Example.** Placeholders only:

```vbnet
	Dim configured = request.WithBasicAuth(userName, password)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' '\.\w*Basic\w*Auth\w*\('
```

**Confidence.** **High.** The method name declares the scheme and the two arguments are the credential. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-005-migration).

### VB-006 · AuthorizationHeaderValueScheme

<a id="vb-006"></a>

An `AuthenticationHeaderValue` created with the scheme `Basic`, either with the constructor or with `AuthenticationHeaderValue.Parse("Basic " & credential)`. The scheme may be a literal in any letter case or a constant that the file declares once. The credential may be a variable, a call or an index. The value may be assigned to `Authorization` inside a `With` block, to `request.Headers.Authorization`, or passed on. A `TryParse`, and a `Parse` of a variable that the tool cannot see the scheme of, are not read. A file whose only construct is the header value, such as a function that builds and returns it, with no other credential word in the file, is read too.

**Example.** Placeholders only:

```vbnet
	client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Basic", encodedCredentials)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'AuthenticationHeaderValue'
```

**Confidence.** **Medium.** The scheme is visible, but the value is built where the credential is encoded and may be sent anywhere, so the construct is reported one level below High. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). A scheme that resolves to `Bearer` gives no finding.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-006-migration).

### VB-007 · AuthorizationHeaderWrite

<a id="vb-007"></a>

A write of the `Authorization` header by name, with a Basic value. The write may be a call (`.Add`, `.Set`, `.Append`, `.TryAddWithoutValidation`, `.AddHeader`, or `.SetRequestHeader` on a late-bound HTTP object, each optionally through `.DefaultRequestHeaders`), a parenthesized index (`request.Headers("Authorization") = ...`), or the single-string overload (`.Headers.Add("Authorization: Basic " & credential)`). The header key may be a literal or a named constant whose name carries the word `Authorization`. The value may be one literal, a literal scheme joined to an expression with `&` or `+`, an interpolated string, `String.Format`, `String.Concat` or `String.Join`, a constant scheme joined to a credential, a call to a function whose name carries the scheme word, or a variable assigned one of these in the five statements above the write.

**Example.** Placeholders only:

```vbnet
	client.Headers.Add("Authorization", "Basic " & encodedCredentials)
	request.Headers("Authorization") = String.Format("Basic {0}", encodedCredentials)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' '"Authorization'
rg -n -i --glob '*.vb' '\(\s*[A-Za-z_][A-Za-z0-9_.]*Authorization[A-Za-z0-9_]*\s*,'
```

**Confidence.** **Medium.** The header and the scheme are visible, but the credential that is encoded is built elsewhere. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). The rule needs a write: a read of `Request.Headers("Authorization")` is inbound authentication and is not reported, and the whole word must be the key, so `Proxy-Authorization` and `X-Authorization` do not match. A value held in a variable or a parameter, with no scheme-bearing assignment above it, is read only where the name carries a credential word, such as `password`, `secret`, `credential` or `token`, and it is then Medium. The words `pass`, `pwd` and `apikey` count as whole words, so `relayPassHeader` and `forward_pass_header` are read, while `Bypass`, `Compass` and `passport` are not. A name that carries the word `bearer` or `access_token` is read as a bearer value and is not reported. A value held under a neutral name is not read; see [Declared unsupported coverage](#declared-unsupported-coverage).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-007-migration).

### VB-008 · HttpRequestHeaderEnumAuthorization

<a id="vb-008"></a>

The same header write as VB-007, made through the framework's header enumeration, so that the string `"Authorization"` never appears: `request.Headers(HttpRequestHeader.Authorization) = "Basic " & encoded`, or `.Add(HttpRequestHeader.Authorization, ...)`. The enumeration member may be written qualified or partly qualified, and the leading-dot form inside a `With` block is read. The value shapes are those of VB-007, including a scheme written as a literal argument of `String.Format`. Only the `Authorization` member counts: another member of the enumeration, such as `ProxyAuthorization`, is not this header.

**Example.** Placeholders only:

```vbnet
	client.Headers(HttpRequestHeader.Authorization) = "Basic " & encoded
	request.Headers.Add(HttpRequestHeader.Authorization, String.Format("Basic {0}", encoded))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'HttpRequestHeader\.Authorization'
```

**Confidence.** **Medium**, for the reason given for VB-007. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). A member reached through an `Imports` of the enumeration type, without its name, is not read.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-008-migration).

### VB-009 · WcfBasicTransportCredential

<a id="vb-009"></a>

A Windows Communication Foundation binding whose transport credential type is Basic: `binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic`. The classic form belongs to .NET Framework. The member may be written qualified or partly qualified, and the leading-dot form inside `With binding.Security.Transport` is read. When the same file assigns the password of the channel (`...ClientCredentials.UserName.Password = ...` or `...Credentials.UserName.Password = ...`) within five statements, that assignment is the credential the finding points at. A binding whose credential lives in a configuration file is still a migration target and is still reported.

**Example.** Placeholders only:

```vbnet
	binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic
	client.ClientCredentials.UserName.Password = password
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'HttpClientCredentialType\.Basic'
rg -n -i --glob '*.vb' '\.UserName\.Password\s*='
```

**Confidence.** **Medium.** The carrier of the credential is in a different statement from the transport setting. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). A member reached through an `Imports` of the enumeration, without its name, is not read.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-009-migration).

### VB-010 · SdkConstructorWithUserAndPassword

<a id="vb-010"></a>

The Relativity Import API object built directly from a user name and a password: `New ImportAPI(userName, password, webServiceUrl)`. The class reads the three-argument constructor only. The overloads that take only a service address, and the static factory `CreateByRsaBearerToken(webServiceUrl)`, are the destination of the migration and are not reported. The type name may be written bare, qualified, through an `Imports` alias of its namespace, or as a longer name that ends in `ImportAPI`. The password argument must not be a bearer token: a token-named value is not the second half of a pair, and neither is a call whose name carries `Token` and none of `password`, `secret` or `pwd`, such as `FetchPlatformToken()` or `FetchPlatformToken(scope)`; a call that also carries one of those words, such as `GetPasswordToken()`, is read. A type of your own with the same simple name as the SDK type, declared as a class or a structure in the same file close to the use, is not read when it is written without a namespace; written with a namespace it is read.

**Example.** Placeholders only:

```vbnet
	Dim importApi As New ImportAPI(userName, password, webServiceUrl)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'ImportAPI'
```

**Confidence.** **High.** The type name and the credential pair at the right arity declare the exchange. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). A type of another vendor whose name ends in `ImportAPI` and takes three arguments is read as this one. **Two known costs.** A type of your own with the same simple name as the SDK type, declared in another file or farther than about one thousand characters from the use, is still reported here; dismiss such a finding on review. And a `New ImportAPI(...)` whose second argument is a system token returned by a call whose own argument holds a further call, such as `FetchPlatformToken(Resolve(scope))`, is still reported as a user-and-password pair. Check that the second argument is a password before you act.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-010-migration).

### VB-011 · SdkSettingsCredentialProperties

<a id="vb-011"></a>

The settings object of a Relativity SDK import job given a user name and a password through its properties: `.RelativityUsername = userName` and `.RelativityPassword = password`. Both assignments must be within five statements of each other, in either order, written on a receiver (`settings.RelativityUsername = ...`), as the leading-dot assignment inside a `With` block, or as an object initializer.

**Example.** Placeholders only:

```vbnet
	With importJob.Settings
		.RelativityUsername = userName
		.RelativityPassword = password
	End With
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'RelativityUsername|RelativityPassword'
```

**Confidence.** **High.** The member names declare both the carrier and the vendor. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). A file that assigns only one of the two properties is not reported.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-011-migration).

### VB-012 · NetworkCredentialOnHttpType

<a id="vb-012"></a>

A `NetworkCredential` built from a user name and a password and given to an HTTP type: `Dim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(userName, password)}`. The tool decides by the type, so the type is listed: `HttpClientHandler`, `WebRequestHandler`, `SocketsHttpHandler`, `WinHttpHandler`, `WebClient`, `HttpWebRequest`, and the legacy SOAP proxy types `HttpWebClientProtocol` and `SoapHttpClientProtocol`. The type is found in an object initializer, or in the five statements above an assignment (`As [New] T`, `= New T`, a `CType`, `DirectCast` or `TryCast`, or a `With New T` or `With CType(..., T)` block). The credential may also be built with an object initializer that carries both halves. A credential that is `CredentialCache.DefaultCredentials`, or `UseDefaultCredentials = True`, is integrated Windows authentication and is not reported.

**Example.** Placeholders only:

```vbnet
	Dim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(userName, password)}
```

```vbnet
	Dim client As New WebClient()
	client.Credentials = New NetworkCredential(userName, password)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' '\.Credentials\s*=\s*New\s+[A-Za-z_.]*NetworkCredential'
rg -n -i --glob '*.vb' 'HttpClientHandler|WebRequestHandler|SocketsHttpHandler|WinHttpHandler|WebClient|HttpWebRequest|HttpWebClientProtocol|SoapHttpClientProtocol'
```

**Confidence.** **Medium.** `NetworkCredential` declares no scheme, so the tool cannot tell Basic from other schemes from the file alone. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). A receiver whose type is not written in the five statements above is not read as HTTP, and its credential falls to VB-013. A credential held in a variable and assigned later, and a credential built across several statements, are not read; see [Declared unsupported coverage](#declared-unsupported-coverage).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-012-migration).

### VB-013 · NetworkCredentialConstruction

<a id="vb-013"></a>

A `NetworkCredential` built from a user name and a password that no other class in the file accounts for: the residue. Its consumer is a library, a factory or a call the tool does not recognize, or the BCL performs the exchange and no `Basic` and no Base64 appear in the file. The credential may be built with the constructor, with named arguments, or with an object initializer that carries both halves.

**Example.** Placeholders only:

```vbnet
	Dim credential = New NetworkCredential(userName, password)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'New\s+[A-Za-z_.]*NetworkCredential'
```

**Confidence.** **Medium.** The destination is unknown. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). The class does not report a credential that is an argument of a credential-cache `Add` with a scheme (the cache entry is VB-003, or is not Basic), a credential built with a third argument, which is a domain and marks integrated Windows authentication, or a credential that VB-003, VB-012, VB-016 or VB-018 already accounts for. **The cost:** a `NetworkCredential` built for a consumer that is not an HTTP client, such as a share, a mail server or a directory object whose type the tool does not list, is reported at Medium. A credential used only as a local container is reported too. Dismiss such findings on review.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-013-migration).

### VB-014 · Base64UserPasswordPair

<a id="vb-014"></a>

A user name and a password joined by a colon and encoded to Base64, the value of a Basic header: `Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & ":" & password))`. The pair may be assembled by `&` or `+`, by `String.Format("{0}:{1}", ...)`, by `String.Concat`, by `String.Join(":", ...)` or by an interpolated string, in the encoder's argument or in a variable assigned in the five statements above it, with any encoding between the pair and the encoder. A colon written as a character literal (`":"c`) is read like a colon written as a string, inside the encoder call and in a pair assembled before it. Both halves must be non-empty. The second half must carry a credential word in its name, in any segment of a dotted name: `password`, `secret`, `credential` or `token` anywhere in a name, or `pass`, `pwd` or `apikey` as a whole word, so `txtPassword.Text` and `password.ToString()` are read and `siteName` is not. A half is a variable, a member access, a call or an index; a quoted literal is not a half, which is what separates this class from VB-015. A half whose last member is `Port` is not a credential half, and neither is a second half whose last member is `Length`, `Count` or `Size`. The check on the members `Length`, `Count`, `Size` and `.Port` is applied in only some spellings of the pair: the check is not applied to a first half ending in `Length`, `Count` or `Size` joined with `&` or `+`, to a half followed by `.ToString()`, to a second half ending in `Port` written inside the encoder call, or to a second half ending in `Length`, `Count` or `Size` written inside the encoder call with `&`, `+`, an interpolated string or `String.Concat`. In those spellings a name and a size or a port, such as `userName` and `password.Port`, can be reported as a credential pair when the second half carries a credential word; dismiss such a finding on review.

**Example.** Placeholders only:

```vbnet
	Dim pair As String = userName & ":" & password
	Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'ToBase64String'
rg -n -i --glob '*.vb' '":"'
```

**Confidence.** **Medium.** The pair is visible but the scheme and the destination are not. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). The tool recognizes the standard library's `Convert.ToBase64String` by name, so a pair encoded by your own helper is not read; see [Declared unsupported coverage](#declared-unsupported-coverage). A value of the same shape that is not a credential, such as a user name and a token expiry, can be reported because the second half carries a credential word, and should be dismissed on review. A pair whose second half carries no credential word is not read; see [Declared unsupported coverage](#declared-unsupported-coverage). A pair assembled across several statements is not read.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-014-migration).

### VB-015 · TokenCarriedUnderBasicScheme

<a id="vb-015"></a>

A token or an API key carried through a Basic header: a pair with one empty half is encoded to Base64, so that the token travels as the password (or the user name) of a Basic value. The defect is the scheme, not the token, and the migration is the cheapest in the catalog: the encoding step is deleted and one word changes to `Bearer`. The empty half is the literal `""`, `String.Empty`, `vbNullString`, a variable assigned an empty literal in the ten statements above, or no operand at all, with the colon first or last. `vbNullString` is read as the first operand inside the encoder call too. The colon may also be written as a character literal (`":"c`). An interpolated string with the colon first or last and the credential as its only hole, such as `$":{accessToken}"`, is read the same way. A call to `String.Concat` or `String.Join` may be continued over several lines with an underscore. The pair may be assembled by any of the means listed for VB-014, including `String.Format`, `String.Concat`, `String.Join` and interpolation, in a separate statement or inside the encoder call.

**Example.** Placeholders only:

```vbnet
	Dim pair As String = "" & ":" & accessToken
	Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'ToBase64String'
rg -n -i --glob '*.vb' '"[^"]*:[^"]*"'
```

**Confidence.** **Medium.** It is reported whatever the provenance of the token, because the scheme is what has to change. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). This class takes the place of VB-014 on the same encoder, because the empty half is the stronger fact. An encoder called by your own helper is not read. A value of the same shape that is not a credential, such as a host and a port, can be reported and should be dismissed on review. Some spellings of the empty half or of the assembly are not read; see [Declared unsupported coverage](#declared-unsupported-coverage).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-015-migration).

### VB-016 · NetworkTransferWithCredentials

<a id="vb-016"></a>

A transfer made with the `My.Computer.Network` object and a user name and a password: `My.Computer.Network.DownloadFile(address, destinationPath, userName, password)`, or `UploadFile`. The call may be written as the leading-dot call inside a `With My.Computer.Network` block, with named arguments, with further overloads that add a timeout and an overwrite flag, and with an overload that takes a credential object (a `New NetworkCredential(...)`, or a variable whose name carries a credential word). `My.Computer.Network` is available on .NET only in Windows desktop applications.

**Example.** Placeholders only:

```vbnet
	My.Computer.Network.DownloadFile(address, destinationPath, userName, password, False, 100000, True)
	My.Computer.Network.UploadFile(sourcePath, address, userName, password)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'My\.Computer\.Network'
rg -n -i --glob '*.vb' '\.(DownloadFile|UploadFile)\('
```

**Confidence.** **Medium.** The protocol follows from the address, and the address scheme is not visible in the call. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). A call to an `ftp://` address, written as a literal or held in a constant, is not reported. An overload that the documentation does not list, an argument list longer than the reading window, and the network class of the runtime library created directly are not read.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-016-migration).

### VB-017 · LateBoundHttpObjectCredentials

<a id="vb-017"></a>

A late-bound HTTP object, such as the COM objects of MSXML or WinHTTP, opened or configured with a user name and a password: `http.Open("GET", url, False, userName, password)`, or `http.SetCredentials(userName, password, 0)`. The call is read by its shape: an `Open` call with an HTTP verb written as a literal first and five arguments, or a `SetCredentials` call, whatever the object is created with. Arguments may be named. The `setRequestHeader("Authorization", ...)` call on such an object is VB-007.

**Example.** Placeholders only:

```vbnet
	Dim http As Object = CreateObject("MSXML2.ServerXMLHTTP.6.0")
	http.Open("GET", url, False, userName, password)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' '\.Open\(\s*(bstrMethod:=\s*)?"(GET|POST|PUT|DELETE|HEAD|PATCH)"'
rg -n -i --glob '*.vb' '\.SetCredentials\('
rg -n -i --glob '*.vb' 'CreateObject\(\s*"(MSXML2|Microsoft\.XMLHTTP|WinHttp)'
```

**Confidence.** **Medium.** The scheme the object uses is not visible. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). An `Open` whose method is held in a variable, and an `Open` with fewer than five arguments, are not read. The tool does not examine how the object was created: a five-argument `Open` with an HTTP verb is read whatever the object comes from, whether a `CreateObject` call, a call on a server object, a program-identifier lookup or some other kind of object.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-vbnet.md#vb-017-migration).

### VB-018 · NonHttpCredentialSink

<a id="vb-018"></a>

A credential handed to a consumer that is not an HTTP client: a mail client (`SmtpClient`), a web proxy (`WebProxy`), an FTP request (`FtpWebRequest`, also obtained through a cast of `WebRequest.Create`), a directory object (`DirectoryEntry`, `DirectorySearcher`), or a process started with a user name and a password (`ProcessStartInfo`, `Process.Start`). It also covers the credential-cache entry that carries a host and a port. The class exists so that these credentials stop being reported as migration targets. A credential built across several statements and then given to a mail client produces no finding, which is the correct outcome.

**Example.** Placeholders only:

```vbnet
	Dim mailer As New SmtpClient(host, port)
	mailer.Credentials = New NetworkCredential(userName, password)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' 'SmtpClient|WebProxy|FtpWebRequest|DirectoryEntry|DirectorySearcher|ProcessStartInfo|Process\.Start\('
rg -n -i --glob '*.vb' '\.Add\([^,]+,\s*[^,]+,\s*"[A-Za-z]+"\s*,'
```

**Confidence.** **Low**, always, and always for manual review: no flow and no recommendation. The tool does not believe the credential is Basic Authentication towards RelativityOne. A non-HTTP consumer outside this closed list yields a VB-013 finding at Medium, which you dismiss on review. This class takes the place of VB-013 on the same credential.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-vbnet.md#vb-018-judging).

### VB-019 · AuthenticationHelperDefinition

<a id="vb-019"></a>

The definition of the code's own authentication helper: a `Function` or `Sub` whose name carries the word `Basic` and an authentication word, such as `GetBasicAuthorization` or `ApplyBasicHeader`, with any of the modifiers `Private`, `Public`, `Friend`, `Shared` and `Async`. The class reads the declaration, not the calls. The credential lives in the helper's body and in its callers, where VB-006, VB-007 and VB-014 report it.

**Example.** Placeholders only:

```vbnet
	Private Function BuildBasicAuthHeader(userName As String) As String
	End Function
	Dim credential As String = Nothing
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.vb' '(Function|Sub)\s+\w*Basic\w*\('
```

**Confidence.** **Low**, always, and always for manual review. The advice for a helper is not a mechanical code change: migrate the helper once, and note that its call sites do not appear as findings, because the helper hides them. A file is read only if it carries a word the tool preselects on, so a helper in a file with no other authentication word may not be reported. A helper that builds the header also matches the header classes, at another location; those are two facts about two places.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-vbnet.md#vb-019-judging).

## Flow signals

The tool recommends one of two OAuth 2.0 flows for every High or Medium finding. It chooses from signals in the code, and only from the code.

- An **interactive-user signal** is evidence that the credential belongs to a person who supplies it at run time. It decides **Authorization Code**.
- A **fixed-credential signal** is evidence that the credential is fixed for the integration. It decides **Client Credentials**.
- With **no signal**, the default flow applies. The default is **Client Credentials** unless your user profile file sets another, and the recommendation carries a note asking you to confirm the choice.
- If both kinds are present, the interactive-user signal wins. A file that reads a stored credential and falls back to a prompt when none is found therefore receives Authorization Code, although it may well be an unattended integration. The same happens to a file that holds two unrelated pieces of code, one that serves a signed-in user and one that runs unattended. Confirm the flow for such a file by hand.

**The cross-file limit.** Signals are read in the same file as the finding, and only there. A prompt in another file, a partial class or module defined elsewhere, project-level imports, a loaded configuration file, a login control declared in the markup of a web page whose code-behind is the file being read, and a credential passed in as a parameter by another file are not seen. For most real code the credential reaches the finding's file from another file, so the default is the expected outcome, and the note it carries is the guide. A comment never counts as a signal, and a literal credential written into the code is not evidence of either flow.

Some signals read the name of a configuration key, setting or environment variable. The name matches when it contains `password`, `secret`, `credential` or `token` anywhere, in any letter case, and when it contains `pass`, `pwd` or `apikey` (also written `api_key` or `api-key`) only as a whole word, so `Db:Pass` and `ApiKey` match while `Bypass` and `Compass` do not.

### Interactive-user signals

- `ConsolePasswordPromptText`: `Console.Write` or `Console.WriteLine` (also on `Console.Out`) whose first literal argument asks for a password, passphrase, secret or PIN. A prompt that is commented out does not count.
- `ConsoleReadLineIntoCredentialVariable`: `Console.ReadLine()` assigned to a variable, field or property whose name names a password, passphrase, secret or PIN, as `name = Console.ReadLine()` or `Dim name As String = Console.ReadLine()`.
- `ConsoleMaskedKeyRead`: `Console.ReadKey(True)` (or `intercept:=True`) either near a name that names a password, passphrase, secret or PIN, or in a loop that appends each key character to a buffer. Reading a key with the echo suppressed is how a console masks typed input.
- `InputBoxCredentialPrompt`: a call of `InputBox(...)`, `Interaction.InputBox(...)` or `Microsoft.VisualBasic.Interaction.InputBox(...)` whose prompt text asks for a password, passphrase, secret, PIN or user name.
- `WpfPasswordBoxRead`: a `PasswordBox` obtained with `As PasswordBox`, `New PasswordBox`, a `DirectCast`, `CType` or `TryCast`, or `TypeOf ... Is PasswordBox`, whose `Password` or `SecurePassword` is read within a short distance. A control declared in XAML is not recognized, because its type is written only in the markup.
- `WinFormsPasswordMasking`: the Windows Forms assignment `UseSystemPasswordChar = True` or an assignment to `PasswordChar`, including the leading-dot assignment inside a `With` block.
- `PasswordSignInCall`: `Membership.ValidateUser(...)`, `FormsAuthentication.Authenticate(...)`, or the identity manager's `PasswordSignIn` and `PasswordSignInAsync` calls.
- `WebFormsLoginHandlerBinding`: a handler bound to the login control's authenticate event, written `Handles <control>.Authenticate` or named `<control>_Authenticate`, and a `Handles <control>.Click` handler that reads the text of a password control in the same procedure. The control itself is declared in markup, which is not read.
- `PasswordDataTypeAttribute`: the attribute `<DataType(DataType.Password)>`, which marks a model property that is bound to a form field a person fills.
- `HtmlPasswordInputMarkup`: a string in the code that holds an HTML `<input ... type="password"` element, with either quote style.
- `PostedFormCredentialFieldRead`: a read of the posted form collection, such as `Request.Form("password")`, whose key names a credential, on a receiver named like a form. A read of the query string is not included.

### Fixed-credential signals

- `ConfigurationIndexerCredentialRead`: a read through a configuration object, with its parenthesized index (`configuration("Section:Key")`), `GetValue(Of T)(...)` or `GetSection(...)`, whose key names a credential. The receiver must be named like a configuration object (a name that is or ends in `Configuration`, `Config`, `Section`, `Settings` or `AppSettings`), or be a `GetSection(...)` call; `Request.Headers("token")` and `Session("password")` do not count.
- `ConfigurationManagerCredentialRead`: `ConfigurationManager.AppSettings("<key>")`, `.AppSettings.Get("<key>")` or `ConfigurationManager.ConnectionStrings("<key>")` with a credential-naming key.
- `MySettingsCredentialRead`: `My.Settings.<Name>` where the name names a credential.
- `PlatformInstanceSettingCredentialRead`: a read of a host-platform instance setting by section and name, where the name is credential-named. The section may be a literal or a constant, and the name may be a literal or an identifier that names a credential; a name that reaches the call through a helper's parameter is not read.
- `EnvironmentVariableCredentialRead`: `Environment.GetEnvironmentVariable(...)` with a credential-named literal.
- `RelativitySecretStoreRead`: a read from the platform's secret store, `Helper.GetSecretStore().Get(<path>)` or `.Get(<path>)` on a receiver typed or named as the secret store, within a short distance.
- `SecretClientRead`: `GetSecret` or `GetSecretAsync` on a name typed as a `SecretClient`, within a short distance.
- `ProtectedDataUnprotectRead`: `ProtectedData.Unprotect(...)`, a credential recovered from protected storage.
- `UserSecretsConfigurationSource`: a call to `AddUserSecrets`, with or without a type argument.

Part C explains how to confirm the flow yourself: see [Confirming the flow](part-c-vbnet.md#confirming-the-flow).

## Declared unsupported coverage

The catalog states what it does not cover, so that a missing finding is not read as a clean result. The tool prints the same list in its report. The list below is what the VB.NET analyzer declares, in the tool's own words, followed by the reason.

- **Visual Basic 6, VBA, VBScript and other Basic dialects: files ending in .vbs, .bas, .cls, .frm or .vbhtml, and classic ASP.** These are a different language from Visual Basic .NET; such files are not attributed to it and stay in the list of files that were not assessed.
- **Visual Basic .NET written inside markup: the server-side script blocks and inline code of .aspx, .ascx and .master files.** The markup is not scanned; the code-behind file of the same page, which ends in .aspx.vb, is.
- **credentials stored in configuration files such as app.config, web.config, .settings files, appsettings.json or .resx resources.** These files belong to none of the languages this tool reads, and a configuration file is where a credential usually lives; search them separately for user names and passwords.
- **a source file saved in a single-byte encoding other than Windows-1252, or one whose only line terminator is the Unicode line or paragraph separator.** The tool reads a single-byte file as Windows-1252, and the report names the files it could not read with confidence; line ends other than carriage return and line feed are not recognized.
- **credentials, a scheme or evidence of how a user signs in that are supplied from another file: a partial class or module in another file, a code-behind page whose markup holds the login control, project-level imports, a loaded configuration file.** No per-file analysis joins a definition in one file to its use in another; a file whose flow evidence is elsewhere is assigned the default flow, which is an expected outcome and not a failure - where the code acts for a signed-in user, use the Authorization Code flow.
- **a scheme or credential name set in a way the tool does not read as a constant: a non-literal reassignment, several names declared in one statement, a static local, a constant in another file or project, a resource or application setting, or a constant reached through an import alias.** The scheme cannot be resolved, so the finding is kept at a lower level instead of being dropped; a scheme that is in fact not Basic and cannot be resolved is still reported at that level - review such findings.
- **a scheme argument written as a qualified constant, such as a constant held in another module and named with its module.** The scheme is read only when it is a literal or a bare name declared in the same file; with a qualified name the tool does not match the construct as written.
- **a service that checks incoming Basic Authentication from its own callers, rather than sending it out to another server.** The tool cannot determine from a single file which way a call faces; code that writes a header is read, which excludes the commonest inbound form, but a service that decodes the header itself may still be reported.
- **confirmation that a matched Relativity SDK type comes from the Relativity SDK, that a call goes to RelativityOne and not to another server, or that a credential is consumed by an HTTP client.** The tool matches the type name as written and cannot see the assembly or the destination address; such findings are still reported, with a note that they may not apply if the destination is not RelativityOne - review each one before acting.
- **whether a credential, header or value is ever sent: one that is built and only logged, built on a branch that never runs, or sent to an address that is not an HTTP server.** Reachability, transmission and protocol need control-flow and data-flow analysis the tool does not perform; such code may be reported although it never authenticates to RelativityOne.
- **code inside a disabled conditional-compilation branch, such as a block under a false condition.** The tool reads a disabled branch as code: a construct in it is reported as if it were live, and a stray double quote in it can end the analysis of the rest of the file.
- **XML literals written in expression position, and any code inside them.** Modelling them means tracking element context, which is parsing; an apostrophe in their prose is read as a comment and an odd double quote can make the tool read the rest of the file as text, so findings after it are reported at the lowest level.
- **a string delimited by a mixed pair of typographic quotes, a typographic interpolated string, the fullwidth quotation mark, the fullwidth apostrophe, and an authorization header key written between typographic quotes.** The compiler accepts these but the tool's reading of the text can invert from the opening character to the next quote of its kind, so findings after it may be reported at the wrong level.
- **the folding of letter case for non-ASCII letters in names and in the words the tool searches for.** The words searched for are ASCII; a name written with other letters that differs from them only in case is not matched.
- **a Relativity SDK credential created by an object initializer or by assigning its properties after creation.** Whether the type can be created without arguments and its properties set was not established, so the construct is not read.
- **a credential or a client created by reflection or from a computed type: Activator.CreateInstance with a type, a generic Activator.CreateInstance, or a type name read from configuration.** The type name does not appear as a constructor call in the text.
- **a type, client, authenticator, header type or SDK entry point that the file renames with an import alias of the type, or a user type with the same simple name as an SDK type.** Following a rename is a symbol resolution the tool does not do; an alias of a namespace with the type written in full is read, and a user type with the same name as an SDK type is read as the SDK type unless the same file declares it close to the use, which can be a false positive for a type declared in another file or farther away.
- **a system token returned by a call whose own argument holds a further call, handed as the password of the SDK entry point.** The tool recognizes a token returned by a call whose arguments hold no call of their own; with a deeper call it is reported as a user name and password, a false positive to dismiss on review.
- **arguments written out of order with names, such as a password given by name before the user name, and a named argument that ends the call without a closing parenthesis.** The tool reads named arguments in order only; out of order the two halves would be swapped and the credential would be misread.
- **a collection initializer that writes a header or a credential-cache entry, such as a header collection or a credential cache created with a list of entries.** There is no call to anchor the finding on.
- **a credential assigned from a variable whose receiver type is not written in the lines before it, a With block on a variable whose type is declared elsewhere, and a credential held in a variable and assigned later.** The credential type or the receiver type is not in the text; a credential built with its arguments is read where it is built.
- **a credential built across statements: an empty credential created first and its user name and password set afterwards, then bound to a client.** No pattern reads the pair split across statements.
- **a credential built with three arguments, the third a domain.** A domain marks integrated Windows authentication, which Basic Authentication does not carry; a Basic exchange that also sends a domain is missed.
- **file-transfer calls of My.Computer.Network with overloads that are not listed in the documentation, an argument list that spans more than the reading window, and the network class of the runtime library instantiated directly.** The credential arguments are read at their documented positions only.
- **late-bound COM requests that remain unread: a call to Open whose method is held in a variable, and an Open with fewer than five arguments.** The call shape is not in the text.
- **a user name and password joined by a colon and encoded by the project's own Base64 helper, by an encoder called without its type name, or built up across several statements.** The tool recognizes the standard library's conversion by name only; widening it to any call with Base64 in its name would read every conversion as the end of a credential pair. A header value that names the scheme, written next to the conversion, is still reported.
- **a half whose last member is `Length`, `Count` or `Size`, or a second half whose last member is `Port` written inside the encoder call, where the second half is named with a credential word, in the spellings of the pair where the check that declines it is not applied.** The pair is reported as an encoded user name and password, so a name and a size can be reported as a credential pair.
- **a user name and password joined by a separator held in a named constant.** The colon is not in the text of the pair; a colon written as a literal is read.
- **a user name and password written directly into a URL, such as an address of the form user, colon, password, at sign, host, including through a URI builder.** The tool does not detect this form. This is a real exposure independent of the OAuth2 migration and should be searched for and removed whatever authentication scheme is in use.
- **Basic Authentication performed by a native program the code starts, such as a command-line transfer tool given a user and password or an Authorization header argument.** An argument to an external command is text and not a Visual Basic construct.
- **an Authorization header written with a value held in a variable or a parameter whose name carries no credential word, and a value produced by the code's own function whose name carries no scheme word.** Neither the scheme nor the name gives evidence of what the value is, and reading a neutral name would report every write of an opaque value; a value whose name carries a credential word or a scheme word, or that is assigned a scheme-bearing value in the lines before it, is read.
- **a fixed Authorization value kept in a constant, whose declaration is more than a few lines from the write, and written by name.** The write carries only a neutral name; a value written in full in the write itself is read.
- **an enumeration member reached through an import of the enumeration type without its name, and a header-enumeration value built from a constant that carries the scheme and the space and joined directly.** The member name carries no type to anchor on, and reading a bare concatenation would read every two-part value written into an Authorization header.
- **a user name and password joined by a colon and encoded that are not a credential, such as a user name and a token expiry.** The tool cannot always tell such values apart from an encoded user name and password, so a value with this shape may occasionally be reported and should be dismissed on review.
- **a user name and password joined by a colon and encoded, where the second half is held under a name that carries no credential word, such as `pin` or `passphrase`.** The tool reads the second half of an encoded pair only when its name carries a credential word, so that a pair of two ordinary values is not reported; a credential kept under a neutral name is not reported as an encoded pair, and a header value that names the scheme next to it is still read.
- **an Authorization header written with a value held in a variable whose name carries `pass`, `pwd` or `apikey` in lower case followed directly by a digit, such as `pass2`.** The name is not read as carrying a credential word, while the same word with a capital first letter, such as `Pass2`, is read.
- **flow evidence the tool does not list: a login control declared in markup, a password control declared in XAML, a custom credential prompt, a vault command other than the ones it knows, and an instance-setting key that reaches the call through a helper's parameter.** Only the signals with a stable shape in the code are read; a file whose evidence is of another kind is assigned the default flow.
