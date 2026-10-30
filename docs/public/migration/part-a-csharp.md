# Rule Patterns for C# (Part A of the Migration Documentation)

Part A describes the Basic Authentication patterns the tool looks for in C# code, so that you can find them in your own code without the tool, and so that you can understand why the tool reported a finding. Each pattern class has an identifier, such as `CS-005`, which is the same identifier a tool report shows, so a finding and this document can be matched.

Every pattern class below has the same five parts:

- **Identifier and name.** In the heading.
- **What it looks like.** A short description and a placeholder example.
- **How to search.** A pattern you can run with `rg` (ripgrep). If you use `grep`, run `grep -rnE --include="*.cs"` with the same expression, replacing `\s` by `[[:space:]]` and `\w` by `[[:alnum:]_]`.
- **Confidence.** When a match is High, Medium or Low, including the cases that go to manual review.
- **Which flow.** The signals that indicate Client Credentials or Authorization Code, and the default.

Every example uses obvious placeholders such as `"<user>"`, `"<password>"` and `<host>`. No example contains a real credential. To decide what to do about a pattern, go to [Part C](part-c-csharp.md); for where the new secrets go, see Secret Management (Part B of the Migration Documentation), [`part-b-secret-management.md`](part-b-secret-management.md).

The catalog covers `.cs` source files only. It describes the constructs by their shape, so a pattern class can match code from a library it does not name.

## How confidence is assigned

Every finding receives exactly one of three levels. The levels depend on what the tool can establish from the file, and never on how sure it is about the rest of your system.

| Level | What it means |
| --- | --- |
| **High** | The code almost certainly performs Basic Authentication against a live call path. A known credential-bearing type or call is used, or a Basic `Authorization` header is explicitly constructed, in executable, non-test code. |
| **Medium** | The code matches a Basic Authentication shape, but the credential source or the call path cannot be established from the file alone. |
| **Low** | The match is present in the text but unlikely to be live authenticating code: a comment, a documentation string, a test fixture or mock, sample or example code, or a vendored or generated file. |

Five rules apply to every class:

- **A class states a base level, and levels combine by taking the lower.** A class whose construct shows both the scheme and the credential carrier keeps its base level. A class whose construct does not, because the scheme or the credential is in another statement, or because the destination is unknown, lands one level lower. That is why several classes whose base level is High are reported as Medium.
- **Ambiguity resolves downward.** When two levels are possible, the lower one is used, so that uncertainty goes to manual review and not to a recommendation. A scheme written as a named constant that cannot be resolved in the file gives Medium; a scheme that resolves to something other than Basic gives no finding.
- **A file's status only lowers the level.** A match is demoted to **Low** when it sits in a comment or in a string body, or in a file the tool recognizes as test, generated, fake, sample, example or vendored code. Nothing raises a level.
- **A Low match is reported for manual review, with no recommendation and no flow.** It is not evidence that the code does not authenticate. Part C says how to judge each case.
- **An argument may be an indexer, and an indexer argument is never a literal credential.** Wherever a class below reads a user name, a password, a token or an address from the call, the argument may be a quoted literal, a dotted name with an optional call, or that name followed by one or two bracket suffixes, such as `configuration["<section>:<key>"]`, `ConfigurationManager.AppSettings["<key>"]` or `parts[1]`. The bracket holds a quoted literal or a short expression with no bracket, comma, quote, parenthesis or semicolon inside. A bracket inside a bracket, a bracket that holds a comma, and a bracket that opens the argument instead of following an expression are not read. A value read from configuration is code and not an embedded secret, so a match that captures an indexer never raises the in-code secret warning. This applies to every class below except CS-009 and CS-011, which do not read their arguments this way.

What marks a file for demotion in C#:

- **File names:** `.g.cs`, `.Designer.cs` and `.generated.cs`; `.Tests.cs`, `.Test.cs` and `.Spec.cs`; a name that begins or ends with `Fake`, `Mock` or `Stub`.
- **Folders:** a folder named `test`, `tests`, `spec` or `fixtures` in any letter case, or a folder whose name ends in `.Test` or `.Tests`.
- **Sample and example code:** a folder named `sample`, `samples`, `example` or `examples` in any letter case, matched as a whole folder name, so `SamplesCore` and `Sampler` do not match; and a file whose name begins with `Sample` or `Example`. A file name that only contains the word further in, such as `RateSampleReader.cs`, does not match. The file-name rule has a cost: it also reaches a real client class whose name starts with the word, such as `SampleRateConverter.cs`, and that match is reported at Low for manual review instead of being lost.
- **Deployment:** a folder named `Deployment`, in any letter case, matched as a whole folder name.
- **Vendored code:** a folder named `vendor` or `third-party` in any letter case, matched as a whole folder name, so `VendorPortal` does not match and neither does a file called `Vendor.cs`. The tool reads vendored folders, it does not skip them. A vendored file that is not under a folder of those names is not demoted and is judged like any other file.
- **File content:** a generated-code header, the `GeneratedCode` attribute, or a test attribute such as `[Fact]`, `[Test]`, `[TestMethod]`, `[TestFixture]` or `[Theory]`.

**A literal secret is a separate warning.** When a class captures a credential and the password or token is written literally into the source, the tool also raises an in-code secret warning. The warning is independent of the level: a demoted match keeps it. It is read from where the credential sits, not from its value: a literal inside a string, including help or documentation text, raises it, and a literal inside a comment does not. A value read through an indexer is code, not a literal, and does not raise it.

## Pattern classes

Eighteen classes are registered for C#. Sixteen of them can produce a recommendation. `CS-011` and `CS-019` are always Low and always go to manual review. The identifiers run from `CS-001` to `CS-019` with one gap: the identifier `CS-010` was withdrawn and is not reused, which is why the list jumps from `CS-009` to `CS-011`.

### CS-001 · SdkUserNamePasswordCredentials

<a id="cs-001"></a>

A Relativity SDK credential object built from a user name and a password, usually handed to the SDK's service factory settings. Both argument forms count: values that arrive in variables or indexer reads, and values written literally into the source. The type may be written bare, partly qualified (`Security.<Type>`), fully qualified or with `global::`, and the shorthand `new(...)` is read where the declared type stands directly in front of the variable, as in `<Type> variable = new(...)`. A type renamed with a `using` alias is not read; see [Declared unsupported coverage](#declared-unsupported-coverage).

**Example.** Placeholders only:

```csharp
Credentials credentials = new UsernamePasswordCredentials("<user>", "<password>");
ServiceFactorySettings settings = new ServiceFactorySettings(new Uri("<host>/Relativity.Rest/api"), credentials);
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'UsernamePasswordCredentials'
```

**Confidence.** **High.** The type name declares both that the object is a credential and that it carries a user name and a password, so the construct is High wherever it sits in executable, non-test code. It also matches the target-typed form `new(user, password)`. **Low** when the match is in a comment, in a string body, or in a file the tool recognizes as test, generated or fake code. When the password is written literally into the source, the match also raises the in-code secret warning, which is separate from the level and is not removed by a Low level. When the same construct is also a legacy RSAPI client construction, CS-002 is reported instead of this class, so that one construct produces one finding. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. Credentials usually arrive here as variables or indexer reads. A variable filled from a console prompt, a login form or a desktop password box points to Authorization Code; one filled from a configuration key, an environment variable or a secret store points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-001-migration).

### CS-002 · SdkLegacyRsapiCredentials

<a id="cs-002"></a>

The retired RSAPI client constructed together with a Relativity SDK user name and password credential. The client's first argument, its address, may be a variable, an indexer read or a construction such as `new Uri(...)` with at most one further level of nested parentheses; an address nested deeper is not read. The recommendation differs from CS-001 because the RSAPI client itself is retired: the call moves to Object Manager rather than only changing its credential. The client may be written bare, partly qualified, fully qualified or with `global::`, and so may the nested credential type; the client may also be declared in front of its variable with the shorthand, as in `RSAPIClient client = new(address, new UsernamePasswordCredentials(...))`. A nested credential written with the shorthand `new(user, password)` is not read.

**Example.** Placeholders only:

```csharp
var client = new RSAPIClient(new Uri("<host>/Relativity.Services"), new UsernamePasswordCredentials("<user>", "<password>"));
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'RSAPIClient'
```

**Confidence.** **High.** The credential type is nested inside the client construction, so the construct shows both the scheme and the credential carrier; a client built with a credential variable instead, such as `new RSAPIClient(uri, bearerCredentials)`, does not match. **Low** in a comment, in a string body, or in a test, generated or fake file. This class replaces CS-001 on the same construct. It does not match an RSAPI client obtained without a visible credential; that form is listed under [Declared unsupported coverage](#declared-unsupported-coverage). See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The credential comes from wherever the surrounding code obtains the user name and password. A console prompt, a login form or a desktop password box in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-002-migration).

### CS-003 · CredentialCacheBasicScheme

<a id="cs-003"></a>

A credential added to a credential cache under the Basic scheme, so that an HTTP client sends Basic Authentication to that address. The address may be a `new Uri(...)` construction or a variable or indexer read, and the four-argument overload that takes a host and a port before the scheme counts too. The address may be a variable, an indexer read, or a construction such as `new Uri(...)`, `new System.Uri(...)` or the shorthand `new(...)` with at most one further level of nested parentheses, and the constructed credential may be written bare, partly qualified, fully qualified or with `global::`. A credential written as the shorthand `new(user, password)` in the last argument is not read.

**Example.** Placeholders only:

```csharp
cache.Add(new Uri("<host>"), "Basic", new NetworkCredential("<user>", "<password>"));
cache.Add(address, "Basic", new NetworkCredential("<user>", "<password>"));
cache.Add("<host>", port, "Basic", new NetworkCredential("<user>", "<password>"));
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'CredentialCache|\.Add\s*\(.*"Basic"'
```

**Confidence.** **High** when the scheme argument is the text `Basic`, or a named constant in the same file that resolves to it. If the scheme resolves to something else, the tool reports nothing. If it cannot be resolved, because the constant is declared more than once or comes from elsewhere, the level is **Medium**. The scheme argument and the constructed credential are required; an `Add` whose third argument is not a constructed credential is not matched. This class replaces CS-008 on the credential constructed inside the cache entry's own call, so one cache entry is one finding; a credential constructed in another statement and passed to the cache by a variable is still CS-008 at its own place. **Low** in a comment, in a string body, or in a test, generated or fake file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The credential pair is usually read from the surrounding code. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-003-migration).

### CS-004 · ThirdPartyBasicAuthenticatorType

<a id="cs-004"></a>

An authenticator type from an HTTP library whose name declares both the Basic scheme and the authenticator role, constructed with a user name and a password. The class is defined by the shape of the name, not by one library. The type may be written bare, partly qualified (`Security.<Type>`), fully qualified or with `global::`, and the shorthand `new(...)` is read where the declared type stands directly in front of the variable, as in `<Type> variable = new(...)`.

**Example.** Placeholders only:

```csharp
client.Authenticator = new HttpBasicAuthenticator("<user>", "<password>");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'Basic\w*Authenticator'
```

**Confidence.** **High.** A type name that carries both a scheme word and an authenticator word declares the scheme and the credential carrier by itself. A name with only one of the two words, such as an unrelated identifier that happens to contain `Basic`, does not match. **Low** in a comment, in a string body, or in a test, generated or fake file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The user name and password come from the surrounding code. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-004-migration).

### CS-005 · AuthorizationHeaderValueScheme

<a id="cs-005"></a>

An HTTP authentication header value constructed with a scheme argument that is, or resolves to, `Basic`. The type may be written bare, partly qualified (`Security.<Type>`), fully qualified or with `global::`, and the shorthand `new(...)` is read where the declared type stands directly in front of the variable, as in `<Type> variable = new(...)`. The shorthand assignment to the header property, `Authorization = new("Basic", credential)`, is not read.

**Example.** Placeholders only:

```csharp
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "<base64 of user:password>");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'AuthenticationHeaderValue'
```

**Confidence.** **Medium.** The confidence rules name an explicitly constructed Basic `Authorization` header as High, but this construct does not by itself show that the header reaches an outbound request, so it lands at Medium. If the scheme is a named constant that cannot be resolved in the file, the level stays Medium. If the scheme resolves to something other than Basic, the tool reports nothing. **Low** in a comment, in a string body, or in a test, generated or fake file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The encoded credential is usually built a few lines away. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-005-migration).

### CS-006 · AuthorizationHeaderStringLiteral

<a id="cs-006"></a>

The `Authorization` header name written as a string and added or assigned with a value that carries the Basic scheme, either concatenated or interpolated, or as one fully literal string. Reading the header is inbound authentication and does not match.

**Example.** Placeholders only:

```csharp
client.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedCredentials}");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' '"Authorization"'
```

**Confidence.** **Medium.** The header write shows the scheme but not that the request goes out, so it lands at Medium. A scheme that resolves to something other than Basic is not reported. **Low** in a comment, in a string body other than the header name itself, or in a test, generated or fake file. A header value written as a verbatim or raw string literal is not recognized; see [Declared unsupported coverage](#declared-unsupported-coverage). See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The encoded pair is usually assembled nearby. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-006-migration).

### CS-007 · NetworkCredentialOnHttpHandler

<a id="cs-007"></a>

A network credential built from a user name and a password and assigned to an HTTP client or handler. Assigning the default or integrated credentials does not match, because that is integrated Windows authentication. The handler type and the credential may be written bare, partly qualified, fully qualified or with `global::`. The constructor may carry parentheses before the initializer, `new HttpClientHandler() { ... }` or `new SocketsHttpHandler(settings) { ... }`, and the shorthand is read in `HttpClientHandler handler = new() { ... }`, in `HttpClientHandler handler = new(options) { ... }` and in `HttpClientHandler handler = new();` followed by the property write `handler.Credentials = new NetworkCredential(...)`.

**Example.** Placeholders only:

```csharp
var handler = new HttpClientHandler { Credentials = new NetworkCredential("<user>", "<password>") };
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'Credentials\s*=\s*new\s+[\w.:]*NetworkCredential'
```

**Confidence.** **Medium.** The credential type serves several protocols, so it does not declare Basic Authentication by itself, and the level is Medium. Only an HTTP client or handler counts as the destination; a credential built for a mail, file-transfer or directory client is CS-019 and is Low. This class replaces CS-008 when both match the same construct. **Low** in a comment, in a string body, or in a test, generated or fake file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The credential values usually come from the surrounding code. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-007-migration).

### CS-008 · NetworkCredentialConstruction

<a id="cs-008"></a>

A network credential built from a user name and a secret when no other class in the same file accounts for it. What remains is a credential whose consumer the tool does not recognize, such as the legacy web client, web request and SOAP proxy classes, where the framework performs the exchange and no Basic scheme or Base64 text appears in the file. The type may be written bare, partly qualified (`Security.<Type>`), fully qualified or with `global::`, and the shorthand `new(...)` is read where the declared type stands directly in front of the variable, as in `<Type> variable = new(...)`. A credential created by an object initializer that carries both halves is not read; see [Declared unsupported coverage](#declared-unsupported-coverage).

**Example.** Placeholders only:

```csharp
var credential = new NetworkCredential("<user>", "<password>");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'NetworkCredential'
```

**Confidence.** **Medium.** The credential type does not declare a scheme and no recognized HTTP consumer is present, so the level is Medium. This class is replaced by CS-007 where an HTTP client or handler binding is present, by CS-003 where the credential is constructed inside a credential cache entry, and by CS-019, at Low, where a mail, file-transfer, directory or proxy sink is present. A non-HTTP sink that is not one of those still produces a Medium finding here. **Low** in a comment, in a string body, or in a test, generated or fake file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The credential values usually come from the surrounding code. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-008-migration).

### CS-009 · Base64UserPasswordPair

<a id="cs-009"></a>

A user name and a password joined with a colon, built by concatenation, formatting, joining or interpolation, and passed to a Base64 encoder within a few lines, or written inside the encoder's own argument. Both halves of the pair must be present; a pair with an empty half is CS-018. Each half of the pair may be a variable, a member access such as `userBox.Text`, a call such as `Console.ReadLine()` or `Secrets.Read(index)`, or an indexer read such as `parts[0]`; a call may hold one further level of nested parentheses. The pair can be written inside the encoder's call or in a statement before it. A half written as a quoted literal is not read as a half by this class: a pair with an empty literal half is CS-018.

**Example.** Placeholders only:

```csharp
string pair = $"{userName}:{password}";
string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
string inlineEncoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + ":" + password));
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'ToBase64String'
```

**Confidence.** **Medium.** Building and encoding a pair is the shape of Basic Authentication, but the construct does not show where the result goes, so it is Medium. The pair and the encoder must be within five lines of each other; a pair assembled further away from its encoder is not matched. When the pair is written inside the encoder's argument, it must be on the same statement and within 200 characters of the call's opening parenthesis, in one of the same four spellings: interpolated, concatenated, `string.Format("{0}:{1}", a, b)` or `string.Join(":", a, b)`. An inline pair with an empty half is CS-018, and a colon-joined value that is not inside a call to the encoder does not match. A service that decodes an incoming header itself can also produce this finding; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** in a comment, in a string body, or in a test, generated or fake file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The two halves of the pair are usually parameters or variables. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-009-migration).

### CS-011 · AuthenticationHelperDefinition

<a id="cs-011"></a>

The definition of the project's own authentication helper: a method whose name contains `Credential`, `Authorization` or `Authenticator` and that builds the header or the credential. It is the definition, not a call site.

**Example.** Placeholders only:

```csharp
private static string BuildAuthorizationHeader(string userName, string password)
{
	return "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"));
}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' '(public|private|internal|protected|static)\s.*\w*(Credential|Authorization|Authenticator)\w*\s*\('
```

**Confidence.** **Low, always, and always manual review.** The advice for a helper is to migrate the helper once, and that is not a mechanical code change, so the tool reports the definition at Low with no recommendation. Its call sites do not appear as findings, because the helper hides the credential from them. The definition is one finding. A credential the helper builds is a second finding, at its own line, when another class recognizes it: a colon-joined pair that the helper encodes to Base64, whether the pair is written inside the encoder's argument or assigned to a variable a few lines above it, is reported as CS-009; a network credential it constructs is reported as CS-008. A helper that only passes on a value it received, such as one that prefixes an already encoded string with `Basic`, produces the definition alone. Search for the helper's name to find its callers; [Part C](part-c-csharp.md#cs-011-judging) says how to judge it. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** This class produces no flow, because a Low match gets no recommendation. It has no flow of its own to confirm; decide the flow from who supplies the credential to the helper's callers, using the signals in [Flow signals](#flow-signals) in the same file as each caller.

### CS-012 · HttpRequestHeaderEnumAuthorization

<a id="cs-012"></a>

The `Authorization` header set through the framework's header enumeration on a web client or web request, so no `"Authorization"` string appears in the file. Both writes count: the indexer, and the collection's `Add` or `Set` call with the enumeration member as the first argument. This is the form that older integrations use. The enumeration member may be written fully qualified, with `global::` or partly qualified. The value may also be one interpolated string that holds the scheme word and the credential in one hole, `$"Basic {encoded}"`; a scheme other than Basic written there, such as `$"Bearer {token}"`, gives no finding. The scheme may also be held in a named constant, in four spellings: the bare constant, `SCHEME + " " + encoded`; the constant written with a type or namespace qualifier, `AuthConstants.SCHEME + " " + encoded` or `global::App.AuthConstants.SCHEME + " " + encoded`; the constant inside an interpolation hole, `$"{SCHEME} {encoded}"`; and the qualified constant inside an interpolation hole, `$"{AuthConstants.SCHEME} {encoded}"`. The credential may be a variable, a member access or an indexer read. A constant the file declares is resolved: one that resolves to something other than Basic gives no finding. A constant the file does not declare, a qualified one included, cannot be resolved and gives Medium. A qualified constant that has `Bearer`, `Negotiate`, `Digest`, `Ntlm` or `Kerberos` anywhere in its spelling, in any letter case, gives no finding.

**Example.** Placeholders only:

```csharp
client.Headers[HttpRequestHeader.Authorization] = "Basic " + encoded;
client.Headers.Add(HttpRequestHeader.Authorization, "Basic " + encoded);
client.Headers.Set(HttpRequestHeader.Authorization, "Basic " + encoded);
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'HttpRequestHeader\.Authorization'
```

**Confidence.** **Medium.** It shows the scheme in the assigned value but not that the request goes out, so it lands at Medium. A scheme that resolves to something other than Basic is not reported. The value may also be one fully literal string. An `Add` or `Set` with another enumeration member, such as `HttpRequestHeader.ContentType`, does not match, and the string-named write `Headers.Add("Authorization", ...)` is CS-006. A value written as a verbatim or raw string literal is not recognized. **Low** in a comment, in a string body, or in a test, generated or fake file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The encoded credential is usually built nearby. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-012-migration).

### CS-013 · WcfBasicTransportCredential

<a id="cs-013"></a>

A service binding set to Basic transport credentials, optionally followed within five lines by a user name and password assigned on the client's credentials. This is the only form where the scheme is written in a member name. The enumeration member may be written bare, partly qualified, fully qualified or with `global::`, as in `ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Basic`.

**Example.** Placeholders only:

```csharp
binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
client.ClientCredentials.UserName.UserName = "<user>";
client.ClientCredentials.UserName.Password = "<password>";
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'ClientCredentialType\s*=\s*[\w.:]*HttpClientCredentialType\.Basic'
```

**Confidence.** **Medium.** The binding states Basic outright, but the credential carrier is usually in a different statement, so the class lands at Medium. The credential window is optional: a binding whose credentials live in a configuration file is still reported, because the binding is in the code. **Low** in a comment, in a string body, or in a test, generated or fake file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. A binding whose credentials sit in a configuration file is not read, because the tool does not scan configuration files, so the flow falls to the default unless the code shows otherwise. Signals in the code around the binding decide as usual: a prompt, login form or password control points to Authorization Code, and a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-013-migration).

### CS-014 · SchemeNamingExtensionMethod

<a id="cs-014"></a>

A fluent extension method whose name declares the Basic scheme, called with a user name and a password. It is defined by the shape of the call and is common in code that talks to a REST API.

**Example.** Placeholders only:

```csharp
request.WithBasicAuth("<user>", "<password>");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' '\.WithBasicAuth\s*\('
```

**Confidence.** **High.** The method name states the scheme and the two arguments are the credential, so the construct is High. **Low** in a comment, in a string body, or in a test, generated or fake file. The tool recognizes the method name shown above; a different method with the same shape is not covered. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The two arguments usually come from the surrounding code. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-014-migration).

### CS-015 · SdkConstructorWithUserAndPassword

<a id="cs-015"></a>

A Relativity SDK entry point constructed directly from a user name, a password and a web service URL. Only the three-argument form counts, because the same type has other constructors that authenticate by other means, and a static factory, `ImportAPI.CreateByRsaBearerToken(webServiceUrl)`, that takes no token and uses the identity of the hosting process. The type may be written bare, partly qualified (`Security.<Type>`), fully qualified or with `global::`, and the shorthand `new(...)` is read where the declared type stands directly in front of the variable, as in `<Type> variable = new(...)`.

**Example.** Placeholders only:

```csharp
var importApi = new ImportAPI("<user>", "<password>", "<web service URL>");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'ImportAPI'
```

**Confidence.** **High.** The type name and the credential pair declare the scheme and the carrier. A construction that passes only a URL, and a call to the factory, are not reported, since they carry no credential pair. The recommendation in Part C names that factory, says it takes no token and applies to a process hosted by the Relativity service account such as an agent, and records that no documented `ImportAPI` factory accepts a token acquired by a standalone or desktop process (known issue 12 in the [release notes](../user-guide/02-release-notes.md#known-issues)). **Low** in a comment, in a string body, or in a test, generated or fake file. The tool recognizes the entry point shown above. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The credential pair usually comes from the surrounding code. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-015-migration).

### CS-016 · CredentialAssembledAcrossStatements

<a id="cs-016"></a>

A network credential whose user name is set in an object initializer and whose password arrives by a later property assignment, within five lines. The pair never appears in one expression. The credential type may be written bare, partly qualified, fully qualified or with `global::`, and the constructor may carry empty parentheses before the initializer, `new NetworkCredential() { UserName = ... }`. The shorthand `new() { ... }` form is not read; see [Declared unsupported coverage](#declared-unsupported-coverage).

**Example.** Placeholders only:

```csharp
NetworkCredential credential = new NetworkCredential { UserName = userName };
credential.Password = password;
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'NetworkCredential\s*(\(\s*\))?\s*\{'
```

**Confidence.** **Medium.** The credential type declares no scheme and the two halves are in separate statements, so the level is Medium. A credential that is bound to a mail or other non-HTTP client is a near miss and is not a migration target; confirm the destination before acting. **Low** in a comment, in a string body, or in a test, generated or fake file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The two halves usually come from the surrounding code. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-016-migration).

### CS-017 · SdkSettingsCredentialProperties

<a id="cs-017"></a>

The same Relativity SDK credential set through named properties of a settings object instead of a constructor. Both the user name property and the password property must be present.

**Example.** Placeholders only:

```csharp
importJob.Settings.RelativityUsername = userName;
importJob.Settings.RelativityPassword = password;
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'RelativityUsername|RelativityPassword'
```

**Confidence.** **High.** The property names declare both the carrier and the vendor. A user name property on its own is not a credential and does not match. **Low** in a comment, in a string body, or in a test, generated or fake file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The two values usually come from the surrounding code. A prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-017-migration).

### CS-018 · TokenCarriedUnderBasicScheme

<a id="cs-018"></a>

A token or API key carried through a Basic header with one half of the pair empty. The token is not the problem; the scheme is. The migration removes the Base64 step and changes the prefix to `Bearer`.

**Example.** Placeholders only:

```csharp
string pair = "" + ":" + accessToken;
string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'ToBase64String'
```

**Confidence.** **Medium, always reported.** A token wearing the Basic prefix is a migration target whatever the token's origin, and the tool cannot tell where the header goes, so the level is Medium. A pair with both halves non-empty is CS-009 instead. When the token is written literally into the source, the match also raises the in-code secret warning. **Low** in a comment, in a string body, or in a test, generated or fake file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a login page or a credential source in another file is not seen; follow the credential to where it comes from before you decide. This class exchanges no credential for the prefix change itself, so the flow matters only for how the token is obtained. The recommendation still names one flow: a prompt, login form or password control in the same file points to Authorization Code; a configuration, environment variable or secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-csharp.md#cs-018-migration).

### CS-019 · NonHttpCredentialSink

<a id="cs-019"></a>

A network credential bound to a client that is not an HTTP client: mail, file transfer, directory or proxy. It is a near-miss class. It exists so that these credentials stop being reported as migration targets. The sink is decided by its own type, so the credential is reported here wherever it is bound: in the initializer of the sink, `new SmtpClient(host, port) { Credentials = new NetworkCredential(user, password) }`, with or without constructor parentheses and with other members around the credential; in a shorthand declaration, `SmtpClient mailer = new(host, port) { ... }` or `SmtpClient mailer = new(host, port);` followed by the binding; and in a later statement. A credential completed across statements and then bound by an initializer or a later assignment is read the same way. The sink and the credential may be written bare, partly qualified, fully qualified or with `global::`, and a file-transfer request is read when it is obtained from `WebRequest.Create(...)` or `FtpWebRequest.Create(...)` and given its type by a cast in front of the call, `(FtpWebRequest)WebRequest.Create(...)`, or by `as FtpWebRequest` after it, qualified or not. A web request given another type, such as `as HttpWebRequest`, carries an HTTP credential and is not read here.

**Example.** Placeholders only:

```csharp
var client = new SmtpClient("<mail host>", 25);
client.Credentials = new NetworkCredential("<user>", "<password>");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n --glob '*.cs' 'SmtpClient|FtpWebRequest|DirectoryEntry|DirectorySearcher|WebProxy'
```

**Confidence.** **Low, always, and always manual review.** The tool does not believe this is Basic Authentication towards a system being migrated, so it reports the match at Low with no recommendation. The set of non-HTTP sinks it knows is small and fixed: the mail client, the file-transfer request, the directory entry and searcher, and the web proxy. A sink outside that set is still reported at Medium under CS-008. [Part C](part-c-csharp.md#cs-019-judging) says how to judge it. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** This class produces no flow, because a Low match gets no recommendation. If the same credential is also used to call RelativityOne, that use is reported separately and gets its own flow.

## Flow signals

The tool recommends one of two OAuth 2.0 flows for every High or Medium finding. It chooses from signals in the code, and only from the code.

- An **interactive-user signal** is evidence that the credential belongs to a person who supplies it at run time. It decides **Authorization Code**.
- A **fixed-credential signal** is evidence that the credential is fixed for the integration. It decides **Client Credentials**.
- With **no signal**, the default flow applies. The default is **Client Credentials** unless your user profile file sets another, and the recommendation carries a note asking you to confirm the choice.
- If both kinds are present, the interactive-user signal wins.

**The cross-file limit.** Signals are read in the same file as the finding, and only there. A login page, a login handler or a credential prompt in a different file is not seen, and neither is a credential that arrives through dependency injection or configuration binding. For most real integrations the credential reaches the finding's file from another file, so the default is the expected outcome, and the note it carries is the guide. A comment never counts as a signal, and a literal credential written into the source is not evidence of either flow.

Some signals read a key or a name. They match `password`, `secret`, `credential` and `token` anywhere inside the key, in any letter case, and they match `pass`, `pwd` and `apikey` only as whole words, so `Db:Pass`, `DB_PWD` and `ApiKey` match while `Bypass` and `Compass` do not.

### Interactive-user signals

- `ConsolePasswordPromptText`: A console write call whose text asks the person for a password, passphrase, secret or PIN, such as "enter password" or "password:".
- `ConsoleReadLineIntoCredentialVariable`: A console line read whose result is assigned to a variable, field or property named for a password, passphrase, secret or PIN.
- `ConsoleMaskedKeyRead`: A console key read with the echo suppressed, used next to a credential-named identifier, or used in a loop that appends each key to a buffer until Enter is pressed.
- `WpfPasswordBoxRead`: Use of the desktop password box control by its type name, as a declaration, a construction or a read of its `Password` or `SecurePassword` member; or a cast to the control, or an `as` conversion to it, whose `Password` or `SecurePassword` member is read in the same file, either directly on the cast expression or through the variable that stores the result within 400 characters after it. A cast whose member is never read, a cast to another control type and a name that only begins with the control's name do not count. A control declared in markup and read under another name with no cast is not recognized, because its type is written only in the markup file; that is the cross-file limit below.
- `WinFormsPasswordMasking`: Turning a Windows Forms text box into a password field: `UseSystemPasswordChar` set to true, or any assignment to `PasswordChar`.
- `PasswordSignInCall`: A call to `PasswordSignInAsync`, `CheckPasswordSignInAsync` or `CheckPasswordAsync`, the identity methods that validate a password a person submitted.
- `PostedLoginHandlerBinding`: An `[HttpPost]` attribute followed closely by a method whose name contains `Login`, `LogIn`, `SignIn` or `LogOn`.
- `PasswordDataTypeAttribute`: The `DataType(DataType.Password)` attribute on a model member.
- `HtmlPasswordInputMarkup`: The markup `type="password"` inside a string that builds an HTML input.
- `PostedFormCredentialFieldRead`: A read of a posted form collection whose key names a password field. A hidden anti-forgery token field does not count.

### Fixed-credential signals

- `ConfigurationIndexerCredentialRead`: A read through the configuration interface, by indexer, `GetValue` or `GetSection`, whose key names a credential. An indexer counts only on a configuration-shaped receiver, such as a name ending in `Configuration`, `Config` or `Section`, or a `GetSection(...)` call; a header, session, query, cookie or dictionary indexer does not.
- `ConfigurationManagerCredentialRead`: A read from the application settings or connection strings collection of the framework-style configuration file, with a credential-naming key.
- `PlatformInstanceSettingCredentialRead`: A read of a host-platform instance setting by section and name, where the name is credential-named.
- `EnvironmentVariableCredentialRead`: A read of an environment variable whose name is credential-named.
- `SecretClientRead`: A call to `GetSecret` or `GetSecretAsync` on a cloud key-vault secret client.
- `ProtectedDataUnprotectRead`: A call to the platform data-protection routine that decrypts a value previously protected for the user or the machine.
- `UserSecretsConfigurationSource`: A call to `AddUserSecrets`, which adds the development secret store as a configuration source.

Part C explains how to confirm the flow yourself: see [Confirming the flow](part-c-csharp.md#confirming-the-flow).

## Declared unsupported coverage

The catalog states what it does not cover, so that a missing finding is not read as a clean result. The tool prints the same list in its report. The list below is what the C# analyzer declares, in the tool's own words, followed by the reason.

- **Confirming whether a piece of Basic Authentication code is actually calling RelativityOne or a different server.** The tool cannot determine, from the code in a single file, what server a request is actually sent to. Such findings are still reported, with a note that the finding may not apply if the destination is not RelativityOne. Review each one to confirm the destination before acting on it.
- **A service that checks incoming Basic Authentication from its own callers, rather than sending it out to another server.** The tool cannot always tell which direction a credential is flowing in from the code in one file. A service that only accepts Basic Authentication from its callers has nothing to migrate to Relativity's token-based authentication. The tool excludes the most common inbound form, but a service that decodes the header itself may still be reported as if it were sending credentials out, so confirm the direction before acting on a finding like this.
- **A project's own custom authentication helper or wrapper written in a form the tool does not recognize.** The tool matches known code patterns and cannot see inside every custom wrapper. It does detect the common case where a helper method or class visibly builds or holds a credential (CS-011), but not every form a custom wrapper can take. Review any authentication helper code by hand.
- **Login pages, forms or controllers that sit in a different file from the code that uses the credential, so the flow is chosen without them.** The tool reads one file at a time. A login handler, a password field or a credential prompt in a file other than the finding's is not seen, so the finding is assigned the default flow, Client Credentials unless your user profile file says otherwise, and carries the default note. That outcome is expected for most real integrations and is not a failure of the tool. The note tells you to use Authorization Code where the integration acts for a signed-in user.
- **A flow signal that lies beyond the part of a file the tool could read reliably.** Each flow signal is tested where it sits in the file against the tool's reading of code, comments and strings. Where that reading is trustworthy only up to some point, a signal after that point does not count, and the finding is decided as if the signal were absent. This can only move the result toward the default; it never assigns a flow the file does not show.
- **Credentials supplied through dependency injection, configuration binding, or another mechanism that connects code across multiple files.** The tool examines each file on its own and cannot trace how a credential registered in one file reaches the code that consumes it in another file.
- **Credential objects created through reflection or another dynamically computed expression.** The type or method name is never written out as plain text in the source in this case, so the tool has nothing to match against.
- **Credentials stored in configuration files rather than in source code.** Configuration files are not source code in any language the tool scans; they are a different format and are not part of what it reads. Review configuration files separately.
- **A user name and password written directly into a URL, such as "https://user:password@host".** The tool does not detect this form at all. It is a real exposure independent of the OAuth2 migration and should be searched for and removed regardless of which authentication scheme is in use.
- **Confirmation that a matched Relativity SDK credential type actually comes from the Relativity SDK, rather than a different library that happens to use the same type name.** The tool matches on the type name as written in the code, not on which package or library it was installed from. A different library using an identically named type would be matched the same way. Confirm the source of the type where this matters.
- **The retired RSAPI client obtained through a generic factory method that does not show a credential.** The tool only recognizes this retired client when a credential is visibly built together with it, so as not to flag code that is not a problem. A client obtained through a factory with no visible credential in that same code is not detected, even though it is still worth reviewing by hand.
- **A credential, scheme name, or header value written using an unusual quoting style - such as a verbatim or raw string literal - rather than an ordinary quoted string.** The tool's matching only recognizes the ordinary quoted-string style. Text written with a different quoting style is not matched, even though the rest of the file around it is still read correctly.
- **A file whose analysis takes too long and is stopped by a time limit.** When this happens, every finding for that file is discarded, not just the part that was slow. The file is counted in the report's skipped items, so the loss is visible as a number, but which specific issues were lost in that file is not shown. Review such files by hand.
- **Which specific findings were left out when the tool reached its limit on the number of findings it reports, for a single file or for the whole scan.** The tool limits how many findings it reports per file and overall, and the report states how many were left out. Only aggregate counts are reported; the omitted findings themselves are not named or located individually.
- **A single hand-built Authorization header that the tool reports as two or three separate findings at nearby locations.** The tool does not merge findings that come from more than one piece of code working together, such as a credential built on one line and used on another. Each part is reported separately even though they are one migration, and all of the reported locations need to be fixed together.
- **Authentication performed inside a compiled library or binary dependency, rather than in the client's own source code.** The tool reads source code as text; a compiled binary has no source text for it to examine.
- **C# code embedded inside another file type, such as a Razor view, or code written in a related .NET language such as VB.NET.** This analyzer only scans plain C# source files; other file types and other languages are not covered by it.
- **Code inside a preprocessor conditional block (such as "#if"/"#endif"), and certain unusual string literal forms (an interpolated raw string, or a raw string opened with more than one "$").** These are known gaps in how the tool reads C# text into code versus comments and strings, rather than a limitation of any one detection rule. Code inside a block that is switched off with a literal `false` condition is not demoted either; it is reported as ordinary code.
- **A Base64-encoded value made of two parts separated by a colon that is not actually a credential.** The tool cannot always tell such values apart from an encoded user name and password pair, so a non-credential value with this same shape may occasionally be reported and should be dismissed on review.
- **An interpolated string that writes a literal brace as a doubled opening brace.** The tool reads the doubled opening brace as the start of an interpolated expression followed by an inner brace, not as a literal brace. Text between a balanced pair is read as code, so a construct written there can be reported as if it were live code, and an unpaired doubled brace can make the tool read a later stretch of the file as the inside of that expression until it finds the end. The loss is a false finding or a demoted one in the lines near such a literal.
- **A user name and password joined by a colon and converted to Base64 by the project's own helper method, rather than by the standard library's Base64 conversion, or by that conversion called without its type name through a "using static" directive.** The tool recognizes the standard library's conversion only, written with its type name, so a colon-joined pair converted by a differently named helper, or by the conversion reached without its type name, is not matched on its own. A header value that names the scheme, written next to that conversion, is still reported - review any code that builds an Authorization header through its own encoding helper.
- **A credential type, client, authenticator, header type or SDK entry point that the file renames with a "using" alias, or an enumeration member or conversion reached through "using static" without its type.** The type name appears only on the "using" line, and following a rename means resolving what a name refers to, which the tool does not do. A use written under the alias, or a member written without its type, is not recognized, so a file that does nothing else is missed - search for such aliases and review those files by hand.
- **A credential or client created with the shorthand "new(...)" where the type name does not stand in front of the variable (an assignment to an existing variable, a return, an argument, a property initializer, a credential written as the last argument of a credential-cache entry).** Without the type name nothing in the text marks the call as one of the recognized kinds. The form with the declared type directly in front of the variable, "Type x = new(...)", is recognized. The shorthand assignment to the header property, "Authorization = new("Basic", credential)", is left out for the same reason: reading it would match on a property name that other types also carry.
- **A credential cache entry added by a collection initializer, and a credential created by an object initializer that carries both the user name and the password in the one initializer.** The first has no "Add" call for the tool to match on, so only the credential inside it is reported, as a plain credential construction. The second is read by no rule: the rule for a credential completed in a later statement needs the password there, and the construction rule needs constructor arguments. Review such code by hand.
- **A scheme named by a constant written with a type or namespace qualifier, as the scheme argument of a credential-cache entry, of an authentication header value or of a header written by name, or in the interpolated spelling of a header written by name; a header-enumeration value built from a constant that carries the scheme and the space and is concatenated directly, or formatted with the standard library's format call; a qualified constant that names another scheme in any segment of its spelling, in the header-enumeration value.** A qualified constant is read only where the header is written through the header enumeration, concatenated or in an interpolation hole, and the other classes are widened when the shape is shown to them. A constant that carries the scheme and the space is not read when it is concatenated directly, because that shape would otherwise match every two-operand concatenation written into an Authorization header. A qualified constant is never resolved, so one that names another scheme without Bearer, Negotiate, Digest, Ntlm or Kerberos anywhere in its spelling is reported at Medium as if it were Basic, and one that has such a word in its spelling but names Basic is not reported. Review such code by hand.
- **A Windows Presentation Foundation password control reached through a call with a nested call in its argument, a constructed operand or a parenthesized operand in the stored "as" form ("Find(Get("x")) as PasswordBox", "new Factory() as PasswordBox", "(expression) as PasswordBox").** The operand of the cast or "as" is bounded, so the signal that a person typed a password into the control does not fire on these three operands and the finding is decided as if the signal were absent. That can only move the result toward the default flow; it never assigns a flow the file does not show.
- **A C# source file saved in a single-byte text encoding other than Windows-1252 (for example a Cyrillic or Central European code page), rather than in UTF-8, UTF-16 or Windows-1252.** A file that is not UTF-8 or UTF-16 is read as Windows-1252, which covers Latin-1 and the other Western European files. A file in another single-byte code page that holds a byte Windows-1252 leaves undefined cannot be read that way: it is counted among the files that could not be read as text, and nothing inside it is examined for Basic Authentication. One that holds only bytes Windows-1252 defines is read, but its accented or other non-ASCII characters come out as different characters; a credential written in plain ASCII is still found, and one whose value holds such a character is read with the wrong character. Convert those files to UTF-8 and scan again, or review them by hand.
