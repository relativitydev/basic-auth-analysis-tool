# Rule Patterns for Go (Part A of the Migration Documentation)

Part A describes the Basic Authentication patterns the tool looks for in Go code, so that you can find them in your own source without the tool, and so that you can understand why the tool reported a finding. Each pattern class has an identifier, such as `GO-002`, which is the same identifier a tool report shows, so a finding and this document can be matched.

Every pattern class below has the same five parts:

- **Identifier and name.** In the heading.
- **What it looks like.** A short description and a placeholder example.
- **How to search.** A pattern you can run with `rg` (ripgrep). The tool reads a header name and the word `Basic` in any letter case, so every search ignores letter case (`-i`). If you use `grep`, run `grep -rniE --include="*.go"` with the same expression, replacing `\s` by `[[:space:]]` and `\b` by `\<` or `\>` where your `grep` does not read them. In PowerShell the expression works unchanged with `Select-String`: `Get-ChildItem -Recurse -Include *.go | Select-String -Pattern '<expression>'`, which also ignores letter case. A search reads one line at a time, so a call written over several lines is found through the line that holds the word the search looks for. A search finds candidate files and never replaces reading them: open each match and check it against the description of its class.
- **Confidence.** When a match is High, Medium or Low, including the cases that go to manual review.
- **Which flow.** The signals that indicate Client Credentials or Authorization Code, and the default.

Every example uses obvious placeholders such as `userName`, `secret`, `encoded`, `endpoint` and `host.example`. No example contains a real credential. To decide what to do about a pattern, go to [Part C](part-c-go.md); for where the new secrets go, see Secret Management (Part B of the Migration Documentation), [`part-b-secret-management.md`](part-b-secret-management.md).

The tool reads files with the extension `.go`, and nothing else. A Go source file always has that extension, so there is no rule for a file with no extension: a script with no extension is never read as Go, even when its first line is a Go comment that is also a shell command (a first line such as `//usr/bin/env go run "$0" "$@"; exit $?`). Assembly files (`.s`), module files, HTML and text templates and files read through an embed directive are not Go source and are not read as Go. The searches below use the glob `'*.go'`. The folders the repository walk skips are listed in [How confidence is assigned](#how-confidence-is-assigned).

**Go has no credential type.** Other languages send a credential through a type that names the credential. Go sends it through a **method of the request** (`SetBasicAuth`), an **`Authorization` header** written by a call or by an assignment, the **user information of an address**, or an **encoder**, so the classes below are written from the forms of the call and of the header carrier: which method, which header write, and how the value is written. A header form is read whatever the receiver, because it is anchored on the header write and not on a client name. The standard library is the same on every platform, and every class applies on every platform. A value held in a variable and given to a client elsewhere, a header held in a struct, a helper in another file or package and a wrapper function are not followed; see [Declared unsupported coverage](#declared-unsupported-coverage).

**Four carriers write a header.** Three classes and two further ones start from the same four ways of writing the `Authorization` header, so they are named here once.

- **A header-setting call.** `Set` or `Add` after a dot, with the whole header name `Authorization` (an interpreted or a raw literal, in any letter case) as the first argument: `request.Header.Set("Authorization", ...)`, `headers.Add("authorization", ...)`. The receiver can be any expression.
- **An index assignment.** A name indexed by the literal key `Authorization` and assigned a `[]string{...}` literal or an `append(...)` call: `request.Header["Authorization"] = []string{...}`. The key is a literal; a key held in a constant is not read.
- **An `http.Header` literal.** `Header{` after the package name (`http.Header{`), after an alias (`h.Header{`) or bare (`Header{`, as a dot import is written), followed by the key `Authorization`: `http.Header{"Authorization": {...}}`.
- **An unnamed map literal.** `map[string][]string{` or `map[string]string{` followed by the key `Authorization`. The literal does not say that it is a header object, which is why its class is Medium.

**One call is read at a time.** A class that anchors on a call reads the arguments of that one call, up to a bounded length, with brackets balanced. A string, a raw string, a rune and a comment are read as single units. A use of a name is read within the same file, and a name is followed to its declaration only when the file declares it once, before the use.

## Find your code

Start here if you know what your code does and not which class describes it. Each row names a common situation and the class that covers it. Open the class, read its description and its search, and check the match against your code. A situation that is not in the table may still be covered: the classes are listed in full below, and what the tool does not read is in [Declared unsupported coverage](#declared-unsupported-coverage).

| Your code does this | Class |
| --- | --- |
| Calls `SetBasicAuth(...)` on a request | [GO-001](#go-001) |
| Writes an `Authorization` header with `Set`, `Add` or an index assignment, and the value starts with `Basic` (`"Basic "+encoded`, `fmt.Sprintf("Basic %s", encoded)`) | [GO-002](#go-002) |
| Builds an `http.Header` literal with an `Authorization` key and a `Basic` value | [GO-003](#go-003) |
| Builds a `map[string][]string` or `map[string]string` literal with an `Authorization` key and a `Basic` value | [GO-004](#go-004) |
| Puts a user name and a password in the address given to `Get`, `Head`, `Post`, `PostForm`, `NewRequest` or `NewRequestWithContext` (`https://user:secret@host/...`) | [GO-005](#go-005) |
| Encodes a user name and a password joined by a colon with `base64.StdEncoding` or `base64.RawStdEncoding` | [GO-006](#go-006) |
| Builds a `Userinfo` with `url.UserPassword(...)` | [GO-007](#go-007) |
| Holds an address with a user name and a password in a constant or a variable, or parses one with `url.Parse` or `url.ParseRequestURI` | [GO-008](#go-008) |
| Writes an `Authorization` header whose value is a variable that holds `Basic ...` | [GO-009](#go-009) |
| Defines a `RoundTrip` method that adds Basic Authentication to the request | [GO-010](#go-010) |
| Defines a function that builds a Basic header from its parameters | [GO-011](#go-011) |
| Writes an `Authorization` header whose value is an environment read, or a name that holds one | [GO-012](#go-012) |
| Writes an `Authorization` header whose value is a call to a function of the same file that builds a Basic value | [GO-013](#go-013) |

## How confidence is assigned

Every finding receives exactly one of three levels. The levels depend on what the tool can establish from the file, and never on how sure it is about the rest of your system.

| Level | What it means |
| --- | --- |
| **High** | The code almost certainly performs Basic Authentication against a live call path. The construct names the scheme and the place the credential travels: a call of `SetBasicAuth`, a header whose value starts with the literal `Basic`, or an address with a user name and a password given to a request call, in executable code that is not test code. |
| **Medium** | The code matches a Basic Authentication shape, but the scheme, the carrier or the destination cannot be established from the file alone. A High match in test code is also Medium. |
| **Low** | The match is present in the text but unlikely to be live authenticating code: a comment or a string that is not code, a mock, sample, vendored or generated file, or a definition that is not itself a use. Three classes are also Low by their own definition. |

Six rules apply to every class:

- **A class states a base level, and levels combine by taking the lower.** A class whose construct shows both the scheme and the carrier keeps its base level. A class whose construct does not, because the scheme is held in a variable, the use of an encoder is not in the text, or the receiver of a call is not visible, lands one level lower. That is why several classes whose subject is certain are reported as Medium.
- **Ambiguity resolves downward.** When two levels are possible, the lower one is used, so that uncertainty goes to manual review and not to a recommendation.
- **A file's status only lowers the level.** A match is demoted when its anchor sits in a comment or in a string, or in a file the tool recognizes as test, mock, sample, vendored or generated code. **Test code is demoted to Medium and no lower:** a match that would be High in a source file is Medium in a test, it is given a recommendation, and the report marks the finding as test code. Every other kind of file is demoted to **Low**. Nothing raises a level.
- **A Low match is reported for manual review, with no recommendation and no flow.** It is not evidence that the code does not authenticate. Part C says how to judge each case.
- **Every class anchors on a code token, never on a string.** A match is read from a method name, a header-setting call, an indexed name, a literal's type name, an encoder or a declared name, and then extends into the strings to read the header name, the scheme word and the credential. A class never anchors on the text `Basic ` or on a quoted header name, so the word `Basic` in a comment, a label or a log line is not a finding at its own level.
- **A construct that is read is a write, never a read.** A class reads a header being set, a credential being given to a request or a pair being encoded. A server that reads the incoming `Authorization` header (`request.BasicAuth()`), decodes it to check a user, builds a `WWW-Authenticate` challenge or compares it in a test double is inbound authentication and is not reported at High or Medium. A header whose leading word is `Bearer`, `Digest` or any word other than `Basic` is the migrated form and is not a finding.

**A match in a comment or in a string is Low.** A comment and a string are not code, so a construct that is spelled in a comment, in a raw string or in an interpreted string is reported at Low with the reason that the match is a comment or a string literal. A call written in an executable statement is a finding at its class level. Go has no string interpolation, so no text inside a string is ever read as code.

What marks a file for demotion in Go, with the level it gives. Folder and file names are matched without regard to letter case, and a folder is matched as a whole name, so `contest` does not match `test`:

- **Test code, Medium at most.** File names that end in `_test.go`, and folders named `testdata`, `test`, `tests`, `e2e`, `fixture`, `fixtures`, `testutil` or `testutils`. **A file under `testdata/` is test code and reads at Medium at most**, like every other test file: there is no separate, lower reading for fixture data. The report writes `Test code: yes` under the finding, and the JSON data file sets `testCode` to `true`. A test that uses `httptest`, a mock-server package or a stub transport is test code because of its name or its folder, and not because of the package it imports.
- **Mocks, Low.** Folders named `mock`, `mocks`, `fake`, `fakes`, `stub` or `stubs`; file names that begin with `mock_`, `mocks_`, `fake_` or `stub_` or end in `_mock.go`, `_mocks.go`, `_fake.go` or `_stub.go`, and the files `mock.go`, `mocks.go`, `fake.go`, `fakes.go`, `stub.go` and `stubs.go`. A mock is recognized by its folder or file name and by nothing else, so a mock with a neutral name is reported at the level its class gives it, or at Medium when it is also in a test folder.
- **Samples, Low.** Folders named `example`, `examples`, `_example`, `_examples`, `sample`, `samples`, `demo` or `demos`; file names that end in `example_test.go`; and a function named `Example`, `ExampleName`, `ExampleType_Method` or `Example_suffix`, declared with `func` at the start of a line and an empty parameter list, in any file. Folders only, never a file name prefix: a file that begins with `example` is not demoted, because it would also reach a real source file such as an `exampleClient.go`. The word `Example` in a comment or in a string is not a function.
- **Vendored code, Low.** Folders named `vendor`, `third_party`, `third-party`, `thirdparty` or `Godeps`. The tool reads these folders, it does not skip them.
- **Generated files, Low.** File names that end in `.pb.go`, `.pb.gw.go`, `_generated.go`, `.generated.go`, `_gen.go` or `.gen.go`, or begin with `zz_generated.`; or the header line `// Code generated ... DO NOT EDIT.` written as a line comment on a line of its own and preceded by nothing but blank lines and line comments, at most fifty of them, so the header stands on line 51 at the latest. A header inside a block comment, after the first line of code, or without the closing words is not recognized.
- **Test and another kind together are Low.** A file that is test code and also a mock, a sample, vendored or generated, such as `testdata/mocks/client.go`, `mock_server_test.go` or `example_test.go`, is Low, and the report still marks it as test code. A file under `testdata/` with no other marker is test code only and is Medium.
- **Not a signal: a folder named `Deployment`.** The folder holds the code that authenticates, so a file under it is reported at its full level. The cost is that a deployment file that only illustrates a pattern is reported as if it were live.
- **Not read at all: `node_modules`, `dist` and `build`.** The repository walk skips these folders, in lower case, and `obj`, `.git`, `.vs` and `packages`, and a `bin` folder that has a `*.csproj`, `*.vbproj` or `*.fsproj` file beside it. A Go repository has no such project file, so its `bin` folder is read, and holds built binaries and not text. No demotion is declared for these folders.
- **Test code the tool does not recognize.** A test with a neutral name, outside every test folder and without the `_test.go` ending, is judged as production code and keeps its level, which may be High.
- **A test assertion on an encoded Basic header is Medium, as designed.** A test that builds the expected value `"Basic " + base64.StdEncoding.EncodeToString([]byte("name:token"))` to compare it with the header a local test server received is test code and is reported at Medium under [GO-006](#go-006), with the test-code mark. It is not refused, because the encoder is the construct the class reads wherever it stands.

**A literal secret is a separate warning.** When a class captures a credential and the password or token is written literally into the code, the tool also raises an in-code secret warning, without printing the value. The warning is independent of the level: a demoted match keeps it. A credential is literal in these cases only: a whole string literal, interpreted or raw, in a credential position (the last argument of `SetBasicAuth`, the second argument of `url.UserPassword`); the password in the user information of an address; a literal token after `Basic ` in an `Authorization` header value when the token is the whole literal (at least four letters, digits or `+`, `/`, `=`, `_`, `-`); and a whole `"name:token"` literal that is the operand of an encoder, whether the encoder stands alone or is nested in the value of an `Authorization` header written in the same expression. A call written as `gofmt` writes it over several lines is read the same way: after the literal, blanks, line breaks, at most one comma and comments, within 200 characters, do not remove the warning. A password read from `os.Getenv`, a variable, a named constant, a call, or **a concatenation of a literal with code is not literal**, so `"Basic <token>"+suffix` carries no warning. An interpreted literal that holds an escape sequence in the position of a password is not captured: the finding is reported at its level and the warning is absent. A placeholder is not literal either, and carries no warning: the empty string; a run of three or more of the mask characters `*`, `x`, `.`, `-`, `_` and `#`; a marker word wrapped in `<>`, `[]`, `{{ }}` or `%%`; and the words `placeholder`, `changeme`, `change-me`, `change_me`, `replace-me`, `replaceme`, `your-password`, `your_password`, `redacted`, `todo`, `fixme`, `dummy` and `not-a-real-secret`, in any letter case. The finding itself is unchanged by a placeholder.

**How the tool reads strings and comments.** It marks where each comment and each string begins and ends so that a match in text is demoted. A comment is `//` to the end of the line, or `/*` to `*/`; a block comment does not nest. A string is interpreted (double-quoted, with the backslash as the escape), a rune (single-quoted, with the same escape, so `'"'`, `'\''` and `'/'` are literals and never a string or a comment) or raw (back-quoted, which may run over several lines and has no escape, so a backslash before the closing back quote is text and the literal ends there). A struct tag is a string literal and is read as one, and a directive comment such as `//go:build` is a line comment. The C code of a cgo preamble is written in a comment before the import of the `C` pseudo-package, so it is skipped as a comment and is not analyzed. A literal that Go itself rejects (an interpreted or rune literal across a line, an escaped line break) is recovered at the end of the line; see [Limits of the reading](#limits-of-the-reading).

## Pattern classes

Thirteen classes are registered for Go. Ten of them can produce a recommendation. Three, `GO-010`, `GO-011` and `GO-012`, are always Low and always go to manual review.

### GO-001 · RequestSetBasicAuth

<a id="go-001"></a>

A call of the method `SetBasicAuth` after a dot: `request.SetBasicAuth(userName, secret)`, on a request made by `http.NewRequest` or `http.NewRequestWithContext`, by a `&http.Request{...}` literal, on a receiver of any name, through the method expression `(*http.Request).SetBasicAuth(request, userName, secret)`, inside the body of a `RoundTrip` method, and on any other type that exposes a method of this name (the tool reads the name and not the type). However the package is imported, the method name is the same. The credential is the last argument when it is a whole string literal. Not read: a declaration `func (c *T) SetBasicAuth(`, where a closing parenthesis and a blank, not a dot, stand before the name; and a method value that is not called (`f := request.SetBasicAuth`). A call used to authenticate the client at an OAuth 2.0 token endpoint is Basic on the wire and is read at High all the same.

**Example.** Placeholders only:

```go
	request, err := http.NewRequest(http.MethodGet, endpoint, nil)
	request.SetBasicAuth(userName, secret)
	client.SetBasicAuth(userName, secret)
	request.SetBasicAuth(
		userName,
		secret,
	)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' '\.SetBasicAuth\s*\('
```

**Confidence.** **High.** The name of the method declares the scheme, and its only use is to write the `Authorization` header of a request, so the construct is High wherever it sits in executable code that is not test code. When the password is a whole string literal, the match also raises the in-code secret warning. Not read as a finding: the inbound `request.BasicAuth()` and the declaration of a method of this name. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-go.md#go-001-migration).

### GO-002 · AuthorizationHeaderBasicScheme

<a id="go-002"></a>

A write of an `Authorization` header whose value starts with the literal `Basic`, by a header-setting call or by an index assignment (the first two carriers above). The value starts with an interpreted or a raw literal whose first word is `Basic` in any letter case (`"Basic "`, `` `Basic ` ``; `"BasicAuth"` is another word), or is a call of `Sprintf` whose format literal starts so (`fmt.Sprintf("Basic %s", encoded)`). `Add` is read as well as `Set`, and the call inside the body of a `RoundTrip` method is read. The encoded value can be built in the same expression or in an earlier statement; when the encoder stands in the same expression, the header write and the encoder are one finding of this class. Not read: a leading word other than `Basic` (`Bearer`, `Digest`), which is the migrated form; the names `Proxy-Authorization` and `X-Authorization`; a header name held in a constant; a value that is a name (that is [GO-009](#go-009)) or a call (that is [GO-013](#go-013)); the method expression `http.Header.Set(h, ...)`, which writes the header name second; and a method value that is not called. A read of the incoming header (`request.Header.Get("Authorization")`) is inbound authentication and is not read.

**Example.** Placeholders only:

```go
	encoded := base64.StdEncoding.EncodeToString([]byte(userName + ":" + secret))
	request.Header.Set("Authorization", "Basic "+encoded)
	request.Header["Authorization"] = []string{"Basic " + encoded}
	request.Header.Set("Authorization", fmt.Sprintf("Basic %s", encoded))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' '(Set|Add)\s*\(\s*["`]authorization["`]\s*,|\[\s*["`]authorization["`]\s*\]\s*='
```

**Confidence.** **High.** The scheme word is in the text and the carrier is a header write, so the construct is High in executable code that is not test code. When the token after `Basic ` is the whole literal, or the encoder nested in the value holds a whole `"name:token"` literal, the match also raises the in-code secret warning. Not read as a finding: a leading word other than `Basic`, a proxy header, an inbound read and a challenge. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-go.md#go-002-migration).

### GO-003 · HeaderLiteralBasicScheme

<a id="go-003"></a>

An `http.Header` composite literal with the key `Authorization` and a value that starts with the literal `Basic`: `http.Header{"Authorization": {"Basic " + encoded}}`, with the type written as `http.Header{`, through an alias (`h.Header{`) or bare (`Header{`, which also reads a type of the project's own with that name). The elements are written `{ "Basic " + x }`, with the element type left out, or `[]string{ "Basic " + x }`. The literal is read as an expression, as the value of a struct field (`&http.Request{Header: http.Header{...}}`) or as an argument. The key is read in any letter case. Not read: a leading word other than `Basic`; the key `Proxy-Authorization`; and a header map filled key by key in a loop.

**Example.** Placeholders only:

```go
	headers := http.Header{"Authorization": {"Basic " + encoded}}
	other := h.Header{"authorization": []string{"Basic " + encoded}}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' 'Header\s*\{.*["`]authorization["`]\s*:'
```

**Confidence.** **High.** The literal's type names an HTTP header object, and the key and the scheme word are in the text. When the token after `Basic ` is the whole literal, the match also raises the in-code secret warning. A literal key that is not in canonical form (`"authorization"`) is read all the same, although `Header.Get("Authorization")` does not find it. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-go.md#go-003-migration).

### GO-004 · MapLiteralBasicScheme

<a id="go-004"></a>

An unnamed map literal, `map[string][]string{` or `map[string]string{`, with the key `Authorization` and a value that starts with the literal `Basic`: `map[string]string{"Authorization": "Basic " + encoded}`. The conversion `http.Header(map[string][]string{...})` is read through the literal inside it. A map of this shape handed to a client elsewhere is the typical way a Go program feeds a header set to a helper, and the literal does not say that it is a header object. A map held in a variable and given to a client in another statement is not followed. Not read: a leading word other than `Basic`.

**Example.** Placeholders only:

```go
	flat := map[string]string{"Authorization": "Basic " + encoded}
	headers := map[string][]string{"Authorization": {"Basic " + encoded}}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' 'map\[string\](\[\]string|string)\s*\{.*["`]authorization["`]\s*:'
```

**Confidence.** **Medium.** The scheme word is in the text, but nothing says that the map is an HTTP header object or where it goes, so the class is capped at Medium by design. When the token after `Basic ` is the whole literal, the match also raises the in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-go.md#go-004-migration).

### GO-005 · URLUserinfoInRequestCall

<a id="go-005"></a>

A call of a request-making name whose arguments hold an `http` or `https` address that carries a user name and a password: `http.Get("https://user:secret@host.example/path")`, and the same through the names `Get`, `Head`, `Post`, `PostForm`, `NewRequest` and `NewRequestWithContext`, qualified, aliased, on any receiver (`client.Get(`) or bare. The address is a string literal in any argument of the call, at the first level of nesting: a literal inside a call that is itself an argument (`f("https://...")`) is not entered. The scheme must be `http` or `https`, in any letter case; every other scheme (a database, a cache, a broker, a file transfer address) is not read. The standard library turns the user information of a request address into a Basic `Authorization` header when the request has none. Not read: an address with no password (`user@host`), an address whose user name or password holds a backslash escape, an address assembled from parts, an address held in a variable (which is [GO-008](#go-008)), and a call that is not one of the names above. `NewRequestWithContext` needs go1.13 or later.

**Example.** Placeholders only:

```go
	response, err := http.Get("https://user:secret@host.example/path")
	request, err := http.NewRequest(http.MethodGet, "https://user:secret@host.example/path", nil)
	response, err = client.Post("http://user:secret@host.example/path", contentType, body)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' 'https?://[^/@ ]+:[^/@ ]*@'
```

**Confidence.** **High.** The library's own behaviour turns the user information into a Basic header, and the credential is in the source whichever call sends it. A literal password in the address also raises the in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-go.md#go-005-migration).

### GO-006 · Base64UserPasswordPair

<a id="go-006"></a>

A Base64 encoder, `StdEncoding` or `RawStdEncoding` (qualified, aliased or bare) directly followed by `.EncodeToString(`, `.Encode(` or `.AppendEncode(`, whose source argument is a `[]byte(` conversion of a user name and a password joined by a colon. The pair is written in one of four ways: a concatenation with the literal `":"` between two operands (`userName + ":" + secret`); `Sprintf` whose format literal holds a colon between two verbs (`"%s:%s"`, `"%v:%v"`; an interpreted format literal only); `Join` with the separator `":"`; or a single whole literal that holds a colon (`"user:secret"`). The encoder is read only when the word `Basic` or `Authorization` stands within 300 characters of the call, so a cache key or a `host:port` text is not read; and it is not read when `Proxy-Authorization` stands within the same 300 characters on that side, because the pair is then a proxy credential. When the encoder is nested in the value of a header write, a header literal or a map literal written in the same expression, the header class ([GO-002](#go-002), [GO-003](#go-003) or [GO-004](#go-004)) reads the whole construct and this class is not reported separately. Not read: `URLEncoding` and `RawURLEncoding`, which are not the alphabet of the Basic scheme, a custom alphabet made with `NewEncoding`, a pair that is not colon-joined, a pair built in another statement and encoded in a later one, and every decoder, because the decoder is the inbound direction. `AppendEncode` needs go1.22 or later.

**Example.** Placeholders only:

```go
	pair := base64.StdEncoding.EncodeToString([]byte(userName + ":" + secret))
	value := "Basic " + pair
	other := base64.RawStdEncoding.EncodeToString([]byte(fmt.Sprintf("%s:%s", userName, secret)))
	third := base64.StdEncoding.EncodeToString([]byte(strings.Join(parts, ":")))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' 'StdEncoding\s*\.\s*(EncodeToString|Encode|AppendEncode)\s*\('
```

**Confidence.** **Medium.** The encoding is the Basic construction of the pair, but nothing in the call says it is sent as a header; the use word within the window is what makes the match. When the encoder is the value of an `Authorization` header written in the same expression, the header class reads it at High and the finding is one finding. A whole `"name:token"` literal also raises the in-code secret warning; a concatenation, a `Sprintf` and a `Join` are not literal. A test that compares the expected encoded value with the header a local test server received is reported here at Medium, as test code. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-go.md#go-006-migration).

### GO-007 · UserinfoUserPassword

<a id="go-007"></a>

A call of `UserPassword` (qualified `url.UserPassword`, aliased or bare) with two arguments, a user name and a password: `address.User = url.UserPassword(userName, secret)`, in the `User:` field of a `url.URL{...}` literal, held in a variable or passed as an argument. The second argument and the closing parenthesis must both stand within 300 units of the first comma, and one comma may follow the second argument before the closing parenthesis, on one line or over several. Not read: a declaration `func UserPassword(` of the project's own function; `url.User(name)`, which has no password; a call that begins within 400 characters after the opening parenthesis of a `ProxyURL(`; a call that follows, on the same line, a name that contains `proxy` or `proxies`; and a call that follows, on the same line, a `Scheme:` field whose value is a literal other than `http` or `https` (a database or broker address). The standard library's documentation says that this function should only be used with legacy web sites.

**Example.** Placeholders only:

```go
	address.User = url.UserPassword(userName, secret)
	address := &url.URL{Scheme: "https", User: url.UserPassword(userName, secret), Host: "host.example"}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' '\bUserPassword\s*\('
```

**Confidence.** **Medium.** The `Userinfo` carries a credential, but the scheme, the carrier and the destination are not visible: the value may feed a request address, where the client turns it into a Basic header, or any other consumer. When the second argument is a whole string literal, the match also raises the in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-go.md#go-007-migration).

### GO-008 · URLUserinfoHeldLiteral

<a id="go-008"></a>

An `http` or `https` address with a user name and a password that is held and not given to a request-making call: the first argument of `url.Parse` or `url.ParseRequestURI` (qualified, aliased or bare), or the value of a `const`, `var` or short declaration: `address, err := url.Parse("https://user:secret@host.example/path")`, `const serviceURL = "https://user:secret@host.example/path"`. A declaration is read from the start of its line, with an optional `const` or `var`, the name and an optional type, and the literal may follow on the next line. Not read: a name that contains `proxy` or `proxies`, which is a credential for a proxy; an address held as the value of a struct field key or of a map key; any scheme other than `http` or `https`; an address with no password; an address whose user name or password holds a backslash escape; and an address assembled from parts. When the address is given to a request call, [GO-005](#go-005) reads it and this class does not.

**Example.** Placeholders only:

```go
	address, err := url.Parse("https://user:secret@host.example/path")
	const serviceURL = "https://user:secret@host.example/path"
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' 'https?://[^/@ ]+:[^/@ ]*@'
```

**Confidence.** **Medium.** The consumer of the address is not visible. A literal password in the address also raises the in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-go.md#go-008-migration).

### GO-009 · AuthorizationHeaderValueHeldInVariable

<a id="go-009"></a>

An `Authorization` header written by a header-setting call or by an index assignment whose value is a simple name that the same file declares, before the use, as a value that begins with the scheme word `Basic`. For a call the name is the second argument; for an index assignment it is the element of the `[]string{...}` literal or the value appended. Only those two carriers are read here: a header literal or a map literal whose value is a name is not. The declaration is `name := value`, `name = value` or `var name = value`, on one line at the start of a line, and the value begins with a literal whose first word is `Basic` in any letter case. Not read, so the class stays silent: a name the file cannot resolve (a parameter, an import, a call, a declaration in another file), a name that is declared more than once, and an index assignment whose right-hand side is a bare name that holds a list of values. A value that begins with another word (`Bearer`, `Token`) is the migrated form and is not reported; a name that resolves to an environment read is [GO-012](#go-012), and this class stays silent for it.

**Example.** Placeholders only:

```go
	header := "Basic " + encoded
	request.Header.Set("Authorization", header)
```

```go
	value := "Basic " + encoded
	request.Header["Authorization"] = []string{value}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' '["`]authorization["`]\s*,\s*[A-Za-z_][A-Za-z0-9_]*\s*\)'
rg -n -i -g '*.go' '["`]authorization["`]\s*\]\s*='
rg -n -i -g '*.go' '^\s*(var\s+|const\s+)?[A-Za-z_][A-Za-z0-9_]*\s*(:=|=)\s*["`]basic\b'
```

**Confidence.** **Medium.** The scheme word is not at the header write, so the class is capped at Medium, and a resolved `Basic` confirms the class and never raises it. A declaration that no header uses gives no finding, because the class reads a header write and not a declaration. A held value is two facts at two places: when the declaration also holds an encoder over a colon-joined pair, [GO-006](#go-006) reads that encoder at the declaration and the two findings stand. The class carries no credential, so it raises no in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-go.md#go-009-migration).

### GO-010 · RoundTripperBasicAuthDefinition

<a id="go-010"></a>

The definition of a method named `RoundTrip` (`func (receiver) RoundTrip(`) whose body, within a window of about thirty lines after the head, holds a construct that writes Basic Authentication: a call of `SetBasicAuth`, a header write with a `Basic` value, a header literal or a map literal with a `Basic` value, or an encoder over a colon-joined pair with its use word. The window is not cut at the closing brace of the method, so a construct in the function that follows within the window counts for the head. The fields of the receiver count as credentials (`transport.userName`). The class reads a definition and not a use: the construct inside the body is reported at its own anchor by its own class, and this class names the transport as a second fact at a second anchor. A function type with a `RoundTrip` method defined as a call is read through the method; an anonymous function value has no method name and is not read.

**Example.** Placeholders only:

```go
	func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {
		request.SetBasicAuth(transport.userName, transport.secret)
		return transport.base.RoundTrip(request)
	}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' 'func\s*\([^)]*\)\s*RoundTrip\s*\('
```

**Confidence.** **Low**, always, and always for manual review: no flow and no recommendation. A Medium finding needs a recommendation, and this class is a definition site. Read the finding as the name of the transport that holds a Basic construct; the construct itself is a separate finding in the same file, and its class gives the migration. A body that holds an encoded colon pair, `Basic` and a `Proxy-Authorization` header is reported here too, because the check that keeps proxy headers out applies to an encoder written on its own only. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned) only lowers it further.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-go.md#go-010-judging).

### GO-011 · AuthenticationHelperDefinition

<a id="go-011"></a>

The definition of a function or a method, other than `RoundTrip`, that builds a Basic header from its own parameters: `func basicHeader(userName string, secret string) string { return "Basic " + base64.StdEncoding.EncodeToString([]byte(userName+":"+secret)) }`. The head is read through the first eight parameters, each with an optional simple type. Two readings count the body, both within the same window of about thirty lines. A header write, a header literal, a map literal or an encoder over a colon-joined pair counts when a parameter of the function appears in its value. The encoded helper counts when the body holds an encoder (`StdEncoding` or `RawStdEncoding` with `.EncodeToString(`, `.Encode(` or `.AppendEncode(`) over any operand and the literal `"Basic "` concatenated with `+` to a name or to the encoder call, in either order, with a parameter anywhere in the window. A server's challenge builder (`"Basic realm=..."`) is not a helper, and neither is a function that holds only the `"Basic "` literal with no encoder and no header write. Not read: a function with no parameter, a generic function, a parameter after the eighth, and a parameter that follows one written with a type of other syntax (`map[string]string`, `func(...)`, `chan T`). The construct inside the helper is reported at its own anchor by its own class, and a call to a helper of this kind is reported by [GO-013](#go-013) when it is the value of an `Authorization` header in the same file.

**Example.** Placeholders only:

```go
	func basicHeader(userName string, secret string) string {
		return "Basic " + base64.StdEncoding.EncodeToString([]byte(userName+":"+secret))
	}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' '["`]basic\s*["`]\s*\+|Sprintf\s*\(\s*["`]basic\b'
```

**Confidence.** **Low**, always, and always for manual review: no flow and no recommendation. A Medium finding needs a recommendation, and this class is a definition site whose guidance is advice for a person. A function that holds an encoder over any operand and a `"Basic "` literal for another purpose (a log line, a cache key) is reported here too. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned) only lowers it further.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-go.md#go-011-judging).

### GO-012 · AuthorizationHeaderValueFromEnvironment

<a id="go-012"></a>

An `Authorization` header written by a header-setting call or by an index assignment whose value is an environment read, as the whole value, with the environment name written as a string literal at the call: `request.Header.Set("Authorization", os.Getenv("SERVICE_AUTH_HEADER"))`; or whose value is a name that the same file declares as an environment read (`value, found := os.LookupEnv("SERVICE_AUTH_HEADER")`) with no `Basic` leading word. The reads are `Getenv` and `LookupEnv` (`LookupEnv` needs go1.5 or later), qualified or bare. Not read: an environment name held in a constant or a variable (`os.Getenv(headerName)`), and a value that is more than the read. `"Basic "+os.Getenv("X")` has a literal leading word and is [GO-002](#go-002) at High, not this class.

**Example.** Placeholders only:

```go
	request.Header.Set("Authorization", os.Getenv("SERVICE_AUTH_HEADER"))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' '(Set|Add)\s*\(\s*["`]authorization["`]\s*,\s*([A-Za-z_][A-Za-z0-9_]*\.)?(Getenv|LookupEnv)\s*\('
rg -n -i -g '*.go' '\b(Getenv|LookupEnv)\s*\('
```

**Confidence.** **Low**, always, and always for manual review: no flow and no recommendation. The scheme is not visible: the variable may hold `Basic ...`, `Bearer ...` or a bare token, and the tool cannot tell, so it routes the construct to manual review rather than say nothing or recommend. The second search lists every environment read; keep the ones whose value reaches an `Authorization` header. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned) only lowers it further.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-go.md#go-012-judging).

### GO-013 · AuthorizationHeaderValueFromLocalHelper

<a id="go-013"></a>

An `Authorization` header written by a header-setting call or by an index assignment whose value is a call `name(...)` to a bare name, and the file declares one function of that name that [GO-011](#go-011) reads as a Basic helper, before or after the call: `request.Header.Set("Authorization", basicHeader(userName, secret))`. For an index assignment the call is the element of the `[]string{...}` literal or the value appended. The call is the whole value: nothing but a closing bracket and an optional comma follows its closing parenthesis, so `basicHeader(userName, secret) + suffix` is not read. A helper that builds a `Bearer` value, which holds neither an encoder nor a `"Basic "` literal, is the migrated form and gives nothing. Not read: a method call (`client.basicHeader(...)`) and a call through a package name; a name declared twice, such as a function and a method of the same name; a helper in another file or package, a function value held in a variable and a closure; a helper that the definition class does not read, including one built with `fmt.Sprintf("Basic %s", ...)`; a header literal or a map literal whose value is a call; and a value that is more than the call (a concatenation, a conversion, a parenthesized call). The helper is reported at its definition by [GO-011](#go-011) and by the classes of its inner constructs, and the call at its header write by this class: two facts, or three, at different anchors.

**Example.** Placeholders only:

```go
	func basicHeader(userName string, secret string) string {
		return "Basic " + base64.StdEncoding.EncodeToString([]byte(userName+":"+secret))
	}

	func send() {
		request.Header.Set("Authorization", basicHeader(userName, secret))
	}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.go' '["`]authorization["`]\s*,\s*[A-Za-z_][A-Za-z0-9_]*\s*\('
rg -n -i -g '*.go' '\]\s*=\s*(\[\]string\s*\{\s*|append\s*\([^,]*,\s*)[A-Za-z_][A-Za-z0-9_]*\s*\('
```

**Confidence.** **Medium.** The header write says where the value goes and the helper says it is Basic, but the two are in different places of the file, so the class is capped at Medium, and a resolved helper confirms the class and never raises it. A helper that builds Basic for a destination other than RelativityOne is reported at Medium too, because the tool cannot see the destination. The class carries no credential, so it raises no in-code secret warning; a literal credential, if there is one, is the helper's own finding. A call from a file whose name ends in `_test.go` is reported at Medium as test code. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-go.md#go-013-migration).

## Flow signals

The tool recommends one of two OAuth 2.0 flows for every High or Medium finding. It chooses from signals in the code, and only from the code.

- An **interactive-user signal** is evidence that the credential belongs to a person who supplies it at run time. It decides **Authorization Code**.
- A **fixed-credential signal** is evidence that the credential is fixed for the integration. It decides **Client Credentials**.
- With **no signal**, the default flow applies. The default is **Client Credentials** unless your user profile file sets another, and the recommendation carries a note asking you to confirm the choice.
- If both kinds are present, the interactive-user signal wins. A file that reads a credential from the environment and falls back to a prompt at the terminal when it is unset therefore receives Authorization Code, although it may well be an unattended integration. Confirm the flow for such a file by hand.

**The cross-file limit.** Signals are read in the same file as the finding, and only there. A password typed in another package, a credential read in another file, and a credential that a caller passes in are not seen. For most real files the credential reaches the finding's file from another file, so the default is the expected outcome, and the note it carries is the guide. A comment never counts as a signal, and a literal credential written into the code is not evidence of either flow.

**Two kinds of application.** A Go program may be a web server that acts for people who sign in to it, a command-line tool that asks a person for a password at a terminal, or a job or a service that acts as itself. The tool does not know which kind of application a file belongs to: it knows only the signals in the file, and a server that does no more than listen, route or read an inbound `BasicAuth` is not a signal. Part C labels its variants by application type for that reason, and each variant opens with the precondition it is for.

**Eleven signals are registered for Go, six interactive-user and five fixed-credential.** Their names below are the names the tool uses.

Some signals read the name of a variable or a key. The name matches when it contains `password`, `passwd`, `secret`, `credential`, `cred` or `token` anywhere, in any letter case, and when it contains `pass`, `pw`, `pwd` or `apikey` only as a whole word, so `DB_PASS` and `dbPass` match while `bypass` and `compass` do not. A name such as `creditLimit` contains `cred` and reads as credential-named, which can only move a file from the default to Client Credentials and never loses a finding. The signals that read a prompt or a form key use the same list.

### Interactive-user signals

- `TerminalPasswordRead`: a call of `ReadPassword(`, qualified or bare, with the standard input (`Stdin`) or a descriptor name (`fd`, `descriptor` or `fileDescriptor`, not after a dot) within 200 characters after the opening parenthesis, on the same line. The credential word is not required: the call names the act. Its own declaration `func ReadPassword(` is refused. Command-line code.
- `StdinCredentialPromptRead`: the standard input (`os.Stdin`, or bare `Stdin`) with, each within 300 characters of it, a read call (`NewReader(`, `NewScanner(`, `ReadString(` or a `Fscan` call) and a string literal that is a prompt naming a credential (`password`, `passwd`, `passphrase` or `pass:`). A reader of the standard input alone does not show a person typing: a pipe or a redirected file is standard input too, so a filter that reads lines with no prompt wording is not a signal. Command-line code.
- `FmtScanCredentialVariable`: a call of `Scan`, `Scanln` or `Scanf`, or of `Fscan` or `Fscanln` whose first argument ends in `Stdin`, with an argument `&name` where the bare name carries a credential word (`fmt.Scanln(&password)`). `fmt.Scanln(&count)` is not a signal, a scan of a string (`Sscanf`) is not read, and `&cfg.Password` is not read. Command-line code.
- `LoginFormPasswordRead`: a read of a form field named with a credential word by `FormValue` or `PostFormValue`, by `Form.Get` or `PostForm.Get`, or by the index forms `Form[` and `PostForm[`. A form value read through `FormValue` can also arrive in the query string, and the tool cannot tell the two apart. A read of `username` or of `redirect` is not a signal, and neither is a query string read directly. Web-server code.
- `HtmlPasswordInputMarkup`: `<input` followed, in the same tag, by `type="password"` or `type='password'`, written in a literal. `<input` and the attribute name are read in any letter case, but the value `password` only in lower case, so `type="Password"` is not read. Markup in a template file that is not a `.go` file is not read. Web-server code.
- `LoginRouteRegistration`: a call of `HandleFunc(` or `Handle(`, on any receiver, whose first argument is a literal that holds a path segment equal to `login`, `signin`, `sign-in`, `signon` or `logon`, including a method-prefixed pattern such as `"POST /login"`. `/healthz`, `/logout` and `/api/login-events` are not login routes. It is the weakest of the six: an interface that issues a token to a machine also has a `login` segment. Web-server code.

### Fixed-credential signals

- `EnvironmentCredentialRead`: `os.Getenv("NAME")` or `os.LookupEnv("NAME")`, qualified or bare, where `NAME` is written as a literal and carries a credential word. `os.Getenv("PORT")` and `os.Getenv("HOME")` are not signals.
- `ConfigReceiverCredentialRead`: a read of a credential-named setting from a receiver named `cfg`, `conf`, `config` or `settings`, directly (`cfg.Password`) or after one intermediate member (`cfg.Account.Secret`), or through `Get`, `GetString` or `String` with a credential-named key, or a credential-named variable bound with `:=` from an indexed receiver of such a name. The receivers are names by convention, not packages, and a configuration object with another name is not read.
- `SecretManagerClientCall`: a call of `GetSecretValue(`, `AccessSecretVersion(` or `GetSecret(`, the whole name directly before the parenthesis. A method of that name on an unrelated object is read as a signal too, and an interactive signal outweighs it.
- `CredentialNamedLiteralAssignment`: a declaration, an assignment or a struct field key whose name carries a credential word and whose value is a one-line string literal of at most 400 characters, interpreted or raw. A name that ends in a label word (`Label`, `Placeholder`, `Field`, `Name`, `Pattern`, `Regex`, `Length`, `Hint`, `Message`, `Error`, `Key`, `Header`, `Param`, `Env`, `EnvName`, `EnvVar`, `URL` or `Path`, matched as written) is refused. This signal is its own reading and is not the in-code secret warning: a literal credential is a secret to remove, and it is evidence of a fixed credential only through this signal.
- `SecretFileRead`: a call of `ReadFile(`, qualified or bare, whose first argument is a literal path that names a secret (`secret`, `password`, `credential`, `token` or `/run/secrets/`). `os.ReadFile("config.json")` is not a signal.

**What is deliberately not a signal.** A server marker on its own (`http.ListenAndServe`, a router, `os.Args`), an inbound read of a Basic header, a reader of the standard input with no prompt and no credential name, and a query string read directly. A literal credential is not evidence of either flow.

The names of the functions, receivers and secret-store methods in the signals above are signal vocabulary, not class options: a wrong name loses a flow signal, so the file reads the default, and never loses a finding. They are named from the documentation of the standard library and of the platforms, and have not been checked against real code.

Part C explains how to confirm the flow yourself: see [Confirming the flow](part-c-go.md#confirming-the-flow).

## Limits of the reading

Three limits change what a finding at its level or a missing finding means. Each is stated here in the words the tool's own report uses where it has them.

**A file that is not valid Go can be read wrongly.** Go itself rejects an interpreted or rune literal across a line, an escaped line break and a lone quote character; the tool recovers at the end of the line and reads on. An unterminated block comment is different: the tool gives up on the file from the opening of the comment. The rest of the file is then no longer trusted, and it is read as text, so a construct there is reported at Low and not at its own level, and a literal credential there carries no in-code secret warning. If a file that you expect to produce findings reports only Low ones after some point, look above that point for a `/*` that nothing closes.

**A match in a comment or in a string is Low.** A comment and a string are not code, so a construct spelled in one is reported at Low with no recommendation, as described under [How confidence is assigned](#how-confidence-is-assigned). A comment that quotes a legacy call, a struct tag, a log line or a command inside a string is therefore a manual-review item and never a finding at the level of its class.

**When a run passes the findings cap, the drop order is by path prefix and not by confidence.** When a run finds more than 50,000 findings, the tool keeps the 50,000 whose path sorts first (ascending, ordinal order of the normalized path) and drops the rest, by path and not by confidence, and it keeps at most 2,000 findings from any one file; a folder whose path sorts early (for example `cmd/` or `internal/`) is kept ahead of one that sorts late (for example `testdata/`, `third_party/` or `vendor/`), whatever the confidence of its findings, and a folder that sorts late can lose all its findings to a run that passes the cap. A Low finding that sorts early is therefore kept ahead of a High finding that sorts late. In a repository whose findings do not pass 50,000 the cap drops nothing. Because a vendored folder is read and not skipped, a large one can push a real finding out of the cap; keep such folders out of the scanned path where you can, and the report says in aggregate that the cap was reached and never which findings were left out.

## Declared unsupported coverage

The catalog states what it does not cover, so that a missing finding is not read as a clean result. The tool prints the same list in its report. The list below is what the Go analyzer declares, in the tool's own words, followed by the reason.

- **a request written by hand to a socket or to a raw TLS connection, with a literal Authorization: Basic line in the text**: the set of transports is open, and the header text sits inside a text string, which the tool treats as weak evidence and reports only at Low; flagging every file that mentions the header would flag nearly every network file.
- **generated API clients, hooks and interceptors the tool has not read, and a header written by a function call, except an Authorization header written by Set, Add or an index assignment whose value is a call to a Basic helper declared once in the same file (that call is reported); a helper in another file or package, a method call and a call through a value held in a variable stay unread**: the set is open: the requests are hidden behind generated methods, and the tool recognizes only the HTTP clients it lists; search such clients separately for Basic credentials.
- **ways of sending credentials other than a standard HTTP header: the metadata of a remote procedure call, a message header of another protocol**: the tool recognizes only a header set by a call, an assignment to a header entry, or a header written out as a list of values, so a Basic value written anywhere else is not detected; a connection that takes a standard HTTP header is covered like any other header.
- **a custom transport written as a function type or an anonymous function value, with no method named RoundTrip at the construct**: the tool finds a custom transport through the method name RoundTrip, and a function has none; a Basic construct inside the function is still detected where it is written.
- **the credential options of a third-party client other than a method named SetBasicAuth: a configuration struct with user name and password fields, a builder with another method name**: the set of such clients is open, and the tool recognizes only the HTTP clients it lists.
- **the encoding of a user name and password with a custom alphabet, or with the streaming encoder**: the alphabet is only known when the program runs, and the streaming form has no call to recognize; neither is the standard encoding that the Basic scheme uses.
- **a header call written through its type with the header as the first argument, and a method taken as a value and never called, whether a header method or the Basic method**: the tool recognizes a header call by its first argument being the header name; written through its type the header comes first, and a method taken as a value has no call at that place.
- **a Basic value held in a variable, or in an element of a composite literal such as a table of test cases, with no header call in the same statement; a name whose value the file does not settle; a header or map entry whose value is a name or a function call**: a text held in a variable shows nothing about how it is used, and its use is not in the same statement; the tool follows the value of a name only when the file declares it once, before its use, and otherwise stays silent, because a header set from a name is the usual shape of the migrated Bearer code.
- **a header name that is not written as text at the call, such as a constant that holds Authorization, and a header map filled key by key in a loop or with a computed key**: the name is not visible at the call; the tool follows the value of a name to find the scheme word and not to find a header name; a constant holding the header name is common in Go, so search for such constants.
- **an assignment to a header entry whose right-hand side is a bare name that holds a list of header values**: only a list written out in place or an append call is recognized at the assignment, so a bare name cannot be told from any other list.
- **a credentialed HTTP or HTTPS URL held as the value of a struct field key or of a map key**: the tool recognizes a credentialed URL kept in a variable or constant by a parse call or by the name it is declared under, and a field key or a map key is neither; the Medium finding for such a URL is absent.
- **an interpreted string literal that holds an escape sequence in the position of a literal password**: the construct is still reported at its level and with its flow; only the warning about a secret written in the code is absent, because the tool does not decode escape sequences and does not read a text that contains a backslash as a password.
- **any construction outside the forms the tool lists: a colon pair built with a print or byte-join call, a Basic value built with a print or string-join call, a credentialed URL held in a slice element, a return value or an element of a composite value written without a key, a raw format text given directly to the encoder, an environment variable read whose name is not written as a text or that is not the whole header value, and a URL whose user name or password holds an escape**: each detection recognizes a fixed list of forms and does not follow values between statements; a construction outside the list has nothing to recognize, so its finding is absent.
- **a credentialed URL assembled from parts: a format call, a concatenation, or the string form of a URL built field by field**: the user information is not one text at the call, and the URL detections read a single text only; user information built by the standard function is detected where that function is called.
- **a credential struct or factory of the project's own type, and a header object or a request held in a struct field, a variable or a package-level value and handed to a transport or helper defined elsewhere**: the credential and its use are in different statements and the tool does not parse the program; it detects the construct that writes the credential only where that construct is itself one it recognizes.
- **calls to a wrapper function, except an Authorization header written by Set, Add or an index assignment whose value is a call to a Basic helper declared once in the same file; a call to a helper in another file or package, a method call, a name declared twice, a function value held in a variable, a header literal or map value that is a call, a call to a helper the tool does not read as a Basic helper, and a literal given to the project's own function**: the tool cannot tell which argument of a call becomes which parameter of a function; the same-file call is read only because the header write names the Authorization header and the file names the helper; the helper's definition is reported where it is written, and the Basic construct inside it is reported there too.
- **a pair built in one statement and encoded in another, and a buffer or builder that assembles a user name, a colon and a password**: the tool does not follow a variable from one statement to another; the only exception is a header value held in a name that the file declares once, before its use, whose scheme word the tool reads.
- **a declaration that follows a semicolon or a brace on the same line, several names declared on one line, a raw text continued over several lines, a name declared in another file, as a parameter or as a struct field, and an environment fallback written as an if statement**: the tool reads the declaration of a name only when it starts a line and fits on it, which keeps the work small; reading the value of a name wrongly or not at all can miss or misplace a finding but never stops the scan.
- **a bare name imported without a qualifier that belongs to the project and not to the standard library**: the tool cannot see the import statements, so a bare name is taken to be the standard library's name, and the header name, the value or the credentialed text near it carries the evidence; such a name can be a false positive.
- **shell text in an argument of a command-execution call, such as a transfer tool given a user and a password**: the text is inside a string, which the tool reports only at Low, and reading a command inside a string needs the shell's own way of quoting text, which a Go file does not use.
- **the C code of a cgo preamble, in the comment before the import of the C pseudo-package**: the preamble is a comment as far as the tool can tell and its text is another language; it is skipped as a comment and not analyzed.
- **assembly files ending in .s**: they are not Go source, so they are not attributed to Go and stay in the list of files that were not assessed.
- **SQL text inside a string literal**: a string is not analyzed as another language, and a construct inside a string is reported only at Low.
- **a polyglot run script with no file extension whose first line is a Go comment that is also a shell command**: the file is not recognized as Go and is never read, because a file with no extension is recognized only by its first line and Go has no first-line signature; the report names the files it did not assess.
- **files that are not Go: HTML and text templates, files read through an embed directive, module files, recorded request fixtures and data fixtures under a test-data folder**: the container is not a Go file, so its text is not analyzed as one; a login form in a template file is not taken as evidence of how a user signs in; search such files separately.
- **credentials held in files the tool does not read: environment files, YAML, JSON or TOML configuration, a mounted secret, a pipeline variable**: the credential is in another file, not in the text analyzed, and a configuration file is where a credential usually lives; search such files separately for user names and passwords.
- **struct tags and other literals read as data: a tag such as a JSON field name, a format string**: a struct tag is a text string and is skipped as one; its content is not analyzed.
- **a Basic construct that is built and never sent: a dead branch, a function that is never called, code behind a build constraint that no build selects**: whether a line runs depends on control flow, which the tool cannot know without parsing; such code may be reported although it never authenticates to RelativityOne.
- **a test, mock, sample, vendored or generated file with a neutral name: a test helper in a folder with an ordinary name, a hand-written fake named like a source file, a copy of a dependency outside the vendor folder**: no name or folder says it is one and nothing in its text shows it, so it is reported at its full level and may be a false positive; the opposite case is a source file under a folder named tests, which is reported at Medium instead of High.
- **a generated file whose header the tool does not recognize: the header inside a block comment, after the first line of code, or without the closing words**: the tool recognizes a generated-file header only as a line comment before the first line of code, within the first fifty lines; a file generated another way is reported at its full level.
- **a file that is not valid Go: a string or character literal across a line, an escaped line break, a lone quote character, an unterminated block comment**: Go itself rejects these texts; the tool recovers at the end of the line, and an unterminated block comment makes it give up on the file from its opening, so later constructs are treated as text inside a string and reported at Low.
- **the Basic method used to authenticate the client at an OAuth 2.0 token endpoint**: it is Basic on the wire and is reported at High, and telling a token endpoint from any other request needs the destination, which the tool cannot see; review such findings before acting.
- **a file with any line over 10,000 characters**: the file is skipped whole and counted in the report, so the loss is visible as a number; no finding is reported for it.
- **proxy credentials: a proxy authorization header, user information given to a proxy function, the connect header of a transport, user information in a proxy URL**: this is Basic towards a proxy and not towards the target; it is kept out of the findings, so a URL held under a name that contains proxy is not reported even when it points at the target, and a user and password pair is not reported when the text just before or after it also names a proxy authorization header; the body of a transport or helper that writes both is reported at Low.
- **a user information call that belongs to a proxy or to a non-HTTP URL but lies beyond the stretch of text the tool checks: more than 400 characters into a proxy function's argument, on a later line than a variable whose name contains proxy or than the scheme field of its literal**: the tool checks only a limited stretch of text and cannot read the structure of a call or of a literal; such a call is reported at Medium and may be a false positive.
- **a helper definition that is not recognized: a generic function, a function with no parameter, a parameter after the eighth, and a parameter that follows one written in a different parameter form**: the tool reads the head of a function through at most eight parameters written with simple types; the Low finding at the definition is absent, and the Basic construct inside the helper is still reported where it is written.
- **a transport or helper whose body holds an encoded colon pair, Basic and a proxy authorization header**: the check that keeps proxy headers out applies to the encoder written on its own only; for the body of a transport or helper it does not, so such a definition is reported at Low and may be a false positive.
- **sign-in evidence the tool does not list: an inbound request's credentials that a handler forwards, a credential taken from a command-line flag or argument, an environment read whose name is held in a constant, a custom prompt wrapper, another terminal library, a login form password field whose type is not written in lower case**: the set of possible prompts is open and a prompt with no stable shape cannot be listed in advance; the finding is kept and the file is assigned the default flow, which is an expected outcome and not a failure.
- **the misreadings of the evidence of how a user signs in: a standard-input read with a credential prompt in a program run unattended, a login route that issues a token to a machine, a project's own password-reading function that wraps a different read**: the checks look at the shape of the code and not at intent, so a file may be taken as interactive that is not; review the flow recommended for such a file.
- **a form value read as a posted password that actually arrived in the query string**: the tool reads a form value from the query string as well as from the form body and cannot tell them apart, so a file may be taken as interactive although its password came in a URL.
- **login routes registered by the routers of third-party packages, and a web front end written in Go and compiled to WebAssembly**: only the standard library's own route registration is taken as a login route; a router package's interface was not studied, and a compiled front end has no server at all; the finding is kept and the flow falls to the default.
- **a credential-named assignment whose text is a raw string over several lines or is longer than 400 characters**: the tool reads only a one-line text of at most 400 characters here; a miss loses a piece of sign-in evidence, so the file takes the default flow, and never loses a finding.
- **a file whose analysis takes too long, a call whose arguments run past about 1,500 characters, a use more than thirty line breaks after its definition, a quoted string longer than about a thousand characters, and a line longer than 10,000 characters**: these are limits of the tool that keep the time spent on a file under control: a file that exceeds a time limit is skipped and counted in the report, so the loss is visible as a number, and a limit that grew without end would make the time grow without end.
- **findings left out when the findings cap is reached, listed per file or overall**: the cap drops findings in path order and not by confidence, and it is reported in aggregate only, so the report cannot say which findings were left out; a vendored folder is also scanned and its findings count against the cap.
- **confirmation that a call goes to RelativityOne and not to another server**: the tool sees a call and cannot determine the destination host; such findings are still reported, with a note that they may not apply if the destination is not RelativityOne - review each one before acting.
- **a construction whose encoder and header are written in different statements**: one construction is then two findings, the header and the encoder, because only a construction written in one statement is merged into one; the encoder finding is expected and not an error.

### What the declared list does not say

Three classes are supported at Low only, `GO-010`, `GO-011` and `GO-012`: the tool recognizes them and routes them to manual review, so they are not in the list above. Part A does not describe the Go toolchain that builds your code, and a statement here about what the standard library does rests on its documentation at the version read; check a function's behaviour against the version you use.

**What was not run.** The behaviour of the standard library's HTTP client rests on its documentation at the version read, and on a short execution against a local test server for the user information of an address. No third-party client was installed or executed, and no Go program was run against RelativityOne. Read the findings with that in mind.
