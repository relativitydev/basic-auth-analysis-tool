# Rule Patterns for Python (Part A of the Migration Documentation)

Part A describes the Basic Authentication patterns the tool looks for in Python code, so that you can find them in your own source without the tool, and so that you can understand why the tool reported a finding. Each pattern class has an identifier, such as `PY-003`, which is the same identifier a tool report shows, so a finding and this document can be matched.

Every pattern class below has the same five parts:

- **Identifier and name.** In the heading.
- **What it looks like.** A short description and a placeholder example.
- **How to search.** A pattern you can run with `rg` (ripgrep). The tool reads a header name and the word `Basic` in any letter case, so every search ignores letter case (`-i`). If you use `grep`, run `grep -rniE --include="*.py" --include="*.pyw"` with the same expression, replacing `\s` by `[[:space:]]`, `\w` by `[[:alnum:]_]` and `\b` by `\<` or `\>` where your `grep` does not read them. In PowerShell the expression works unchanged with `Select-String`: `Get-ChildItem -Recurse -Include *.py,*.pyw | Select-String -Pattern '<expression>'`, which also ignores letter case. A search reads one line at a time, so a call written over several lines is found through the line that holds the word the search looks for. A search finds candidate files and never replaces reading them: open each match and check it against the description of its class.
- **Confidence.** When a match is High, Medium or Low, including the cases that go to manual review.
- **Which flow.** The signals that indicate Client Credentials or Authorization Code, and the default.

Every example uses obvious placeholders such as `user_name`, `secret`, `url` and `example.test`. No example contains a real credential. To decide what to do about a pattern, go to [Part C](part-c-python.md); for where the new secrets go, see Secret Management (Part B of the Migration Documentation), [`part-b-secret-management.md`](part-b-secret-management.md).

The tool reads files with the extensions `.py` and `.pyw`, and files with no extension whose first line is a shebang that names `python`, `python2`, `python3` or `python3.` followed by digits, directly or after `env` and its options (`#!/usr/bin/python3`, `#!/usr/bin/env python3`, `#!/usr/bin/env -S python3`). The searches below use the glob `'*.{py,pyw}'`; leave the glob out and search the whole folder to include the scripts that have no extension. Type stubs (`.pyi`), notebooks (`.ipynb`), Cython sources (`.pyx`), Python code held in another kind of file, and Python 2 syntax are not read as Python; the folders the repository walk skips are listed in [Declared unsupported coverage](#declared-unsupported-coverage).

**Python has no credential type.** Other languages send a credential through a type that names the credential. Python sends it through the **argument of an HTTP client**, a **header**, the **user information of an address** or an **encoder**, so the classes below are written from the forms of the call: which helper or which carrier, which keyword, and how the value is written. A header form is read whatever the client, because it is anchored on the header write and not on a client name. The clients the tool knows by name are `requests`, `httpx`, `aiohttp` and `urllib3`, and the standard library's `urllib.request` and `http.client`. A value held in a variable and given to a client elsewhere, a client behind an alias and a wrapper function are not followed; see [Declared unsupported coverage](#declared-unsupported-coverage).

**One call is read at a time.** A class that anchors on a call reads the arguments of that one call, up to a bounded length, with brackets balanced. A string, an `f`-string and a docstring are read as single units. A use of a name is read within a bounded distance of its definition, in the same file.

## Find your code

Start here if you know what your code does and not which class describes it. Each row names a common situation and the class that covers it. Open the class, read its description and its search, and check the match against your code. A situation that is not in the table may still be covered: the classes are listed in full below, and what the tool does not read is in [Declared unsupported coverage](#declared-unsupported-coverage).

| Your code does this | Class |
| --- | --- |
| Builds a Basic helper object: `HTTPBasicAuth(...)` (`requests`), `BasicAuth(...)` (`httpx`, `aiohttp`) | [PY-001](#py-001) |
| Gives `requests` or `httpx` an `auth` argument that is a pair, `auth=(user_name, secret)` | [PY-002](#py-002) |
| Gives anything else an `auth` argument or attribute that is a pair (a session, an alias, a wrapper, `aiohttp`) | [PY-006](#py-006) |
| Writes an `Authorization` header whose value starts with `Basic` (`headers=`, `session.headers[...]`, `add_header`, `putheader`, `dict(Authorization=...)`) | [PY-003](#py-003) |
| Writes an `Authorization` header whose value is a variable that holds `Basic ...` | [PY-013](#py-013) |
| Joins a user name and a password with a colon and encodes the result with `b64encode`, `standard_b64encode` or `encodebytes` | [PY-007](#py-007) |
| Calls `aiohttp`'s `encode_basic_auth` | [PY-008](#py-008) |
| Builds `HTTPBasicAuthHandler(...)` for `urllib.request` | [PY-004](#py-004) |
| Gives a password manager a user name and a password with `add_password(...)` | [PY-009](#py-009) |
| Gives `urllib3` `make_headers(basic_auth=...)` | [PY-005](#py-005) |
| Gives `httpx` a `.netrc` file with `NetRCAuth()` | [PY-010](#py-010) |
| Puts a user name and a password in the address of a request (`https://user:secret@host/...`) | [PY-011](#py-011) |
| Holds an address with a user name and a password in a variable or a keyword | [PY-012](#py-012) |
| Builds a mapping with an `Authorization` key and a `Basic` value that is not given to a request in the same text | [PY-014](#py-014) |

## How confidence is assigned

Every finding receives exactly one of three levels. The levels depend on what the tool can establish from the file, and never on how sure it is about the rest of your system.

| Level | What it means |
| --- | --- |
| **High** | The code almost certainly performs Basic Authentication against a live call path. The construct names the scheme and the place the credential travels: a Basic helper object, a pair given to the `auth` argument of a client the tool knows by name, or a header whose value starts with the literal `Basic` is written, in executable code that is not test code. |
| **Medium** | The code matches a Basic Authentication shape, but the scheme, the carrier or the destination cannot be established from the file alone. A High match in test code is also Medium. |
| **Low** | The match is present in the text but unlikely to be live authenticating code: a comment, a docstring or another string that is not code, a mock, sample or example file, or a vendored or generated file. One class is also Low by its own definition. |

Six rules apply to every class:

- **A class states a base level, and levels combine by taking the lower.** A class whose construct shows both the scheme and the carrier keeps its base level. A class whose construct does not, because the scheme is held in a variable, the use of an encoder is not in the text, or the receiver of a call is not visible, lands one level lower. That is why several classes whose subject is certain are reported as Medium.
- **Ambiguity resolves downward.** When two levels are possible, the lower one is used, so that uncertainty goes to manual review and not to a recommendation.
- **A file's status only lowers the level.** A match is demoted when its anchor sits in a comment or in a string, or in a file the tool recognizes as test, mock, sample, vendored or generated code. **Test code is demoted to Medium and no lower:** a match that would be High in a source file is Medium in a test, it is given a recommendation, and the report marks the finding as test code. Every other kind of file is demoted to **Low**. Nothing raises a level.
- **A Low match is reported for manual review, with no recommendation and no flow.** It is not evidence that the code does not authenticate. Part C says how to judge each case.
- **Every class anchors on a code token, never on a string.** A match is read from a callee, a keyword, an attribute name, an encoder or a declared name, and then extends into the strings to read the header name, the scheme word and the credential. A class never anchors on the text `Basic ` or on a quoted header name, so the word `Basic` in a comment, a label or a docstring is not a finding at its own level. A quoted key such as `"Authorization"` written in a mapping is text to the tool and is reported at Low; see [PY-014](#py-014).
- **A construct that is read is a write, never a read.** A class reads a header being set, a credential being given to a client or a pair being encoded. A server that reads the incoming `Authorization` header, decodes it to check a user, or compares it in a test double is inbound authentication and is not reported at High or Medium.

**A match in a docstring is Low.** A docstring is a string, and the tool reads the body of every string as text and never as code, so a construct that is spelled in a docstring, a comment or any other string is reported at Low with the reason that the match is a comment or a string literal. The rule is not limited to the first statement of a module, class or function: the same text in any string is Low. A call written in an executable statement is a finding at its class level, and so is a call inside a replacement field of an `f`-string, because the field is code. A docstring that holds a `>>>` doctest line is Low like every string: the tool does not read a doctest as code.

What marks a file for demotion in Python, with the level it gives:

- **Test code, Medium at most.** Folders named `test`, `tests`, `testing`, `fixture`, `fixtures` or `e2e`. File names that begin with `test_` or end in `_test.py` or `_test.pyw`, and the files `conftest.py` and `tests.py`. A file with a class statement whose base is a test-case class (`TestCase`, `unittest.TestCase`, `IsolatedAsyncioTestCase`), found in code and not in a comment or a string, with the base list on the line of the `class` keyword. The report writes `Test code: yes` under the finding, and the JSON data file sets `testCode` to `true`. A test that uses a mock library is test code because of its folder, its name or its test class, and not because of the library it imports.
- **Mocks, Low.** Folders named `mock`, `mocks`, `stub`, `stubs`, `fake` or `fakes`; file names that begin with `mock_`, `stub_` or `fake_` or end in `_mock.py`, `_stub.py` or `_fake.py`, and the files `mock.py`, `mocks.py` and `fakes.py`. A mock is recognized by its folder or file name and by nothing else, so a mock with a neutral name is reported at the level its class gives it, or at Medium when it is also in a test folder.
- **Samples, Low.** Folders named `sample`, `samples`, `example`, `examples`, `demo` or `demos`. Folders only: a file name that begins with `example` is not demoted, because it would also reach a real source file such as an `example_client.py`.
- **Vendored code, Low.** Folders named `vendor`, `site-packages`, `venv`, `.venv`, `dist-packages`, `__pypackages__` or `.tox`. The tool reads these folders, it does not skip them.
- **Generated files, Low.** File names that end in `_pb2.py` or `_pb2_grpc.py`; a comment line within the first three lines (a `#!` line and an encoding line may come first) that holds `auto-generated`, `autogenerated`, `auto generated`, `@generated` or `generated by`; or a comment line within the first ten lines that holds `do not edit`, `do not modify`, `do not hand-edit`, `changes will be lost` or `changes will be overwritten`. The comment must stand on a line of its own, so a trailing comment after code is not a marker.
- **Letter case does not matter** for names and folders. Folders are matched as whole names, so `contest` does not match `test`, and the separator is part of the file prefix and suffix, so `latest.py` is not a test file.
- **Test and another kind together are Low.** A file that is test code and also a mock, a sample, vendored or generated, such as `tests/mocks/client.py` or `mock_server_test.py`, is Low, and the report still marks it as test code. A file named `test_fakes.py` is test code only and is Medium.
- **Not a signal: a folder named `Deployment`.** The folder holds the code that authenticates, so a file under it is reported at its full level. The cost is that a deployment file that only illustrates a pattern is reported as if it were live.
- **Not a signal either: `__pycache__`, `env`, `virtualenv`, `.nox`, `.eggs` and a versioned name such as `venv3`.** The tool reads these folders and does not lower their findings, so a virtual environment kept under one of those names is reported at its full level; see [Declared unsupported coverage](#declared-unsupported-coverage).
- **Not read at all: `node_modules`, `dist` and `build`.** The repository walk skips these folders, in lower case, and `obj`, `.git`, `.vs` and `packages`, and a `bin` folder that has a `*.csproj`, `*.vbproj` or `*.fsproj` file beside it, so no demotion is needed for them.
- **Test code the tool does not recognize.** A test with a neutral name, outside every test folder and with no test class, is judged as production code and keeps its level, which may be High.

**A literal secret is a separate warning.** When a class captures a credential and the password or token is written literally into the code, the tool also raises an in-code secret warning, without printing the value. The warning is independent of the level: a demoted match keeps it. A credential is literal in these cases only: a string literal in a credential position (a password argument, an element of an `auth` pair, a `password=` or `passwd=` keyword value), plain, raw or bytes, single or double quoted; an `f`-string with no replacement field that holds the pair or the password; the password in the user information of an address; a literal token after `Basic ` in an `Authorization` header value; and a literal token after `Basic ` in a declaration `NAME = "Basic <token>"` whose name a header then uses. A password read from `os.environ`, a variable, a call, a `%` or `.format(` construction or a concatenation of a literal with code is not literal, and an `f`-string with a replacement field is not literal. A placeholder is not literal either, and carries no warning: the empty string; a run of three or more of the mask characters `*`, `x`, `.`, `-`, `_` and `#`; a marker word wrapped in `<>`, `[]`, `{{ }}` or `%%`; and the words `placeholder`, `changeme`, `change-me`, `change_me`, `replace-me`, `replaceme`, `your-password`, `your_password`, `redacted`, `todo`, `fixme`, `dummy` and `not-a-real-secret`, in any letter case. The finding itself is unchanged by a placeholder. The warning needs the credential to be one single-line quoted literal that is the whole value; a triple-quoted literal, adjacent literals, a literal with a backslash escape and the value of a `login=` keyword still produce the finding, and the warning is not raised for them (see [Declared unsupported coverage](#declared-unsupported-coverage)).

**How the tool reads strings and comments.** It marks where each comment and each string begins and ends so that a match in text is demoted. A comment is `#` to the end of the line. A string is single-quoted, double-quoted or triple-quoted, with the prefixes `r`, `b`, `u`, `f` and `t` in any letter case and in the usual combinations. The replacement fields of an `f`-string and of a `t`-string are code, nested to any depth the file uses, and a doubled brace is a literal brace. The text after the colon of a field, the format specification, is literal text in Python and the tool reads it as such where it can; see [Limits of the reading](#limits-of-the-reading).

## Pattern classes

Fourteen classes are registered for Python. Thirteen of them can produce a recommendation. One, `PY-014`, is always Low and always goes to manual review.

### PY-001 · BasicAuthHelperConstruction

<a id="py-001"></a>

A Basic helper object built with at least one argument: `HTTPBasicAuth(...)` of `requests`, and `BasicAuth(...)` of `httpx` and `aiohttp`, whatever the qualification before the name (`auth.HTTPBasicAuth(`, `requests.auth.HTTPBasicAuth(`, a module alias). The object is created in an argument, in an assignment (`auth = HTTPBasicAuth(...)`), as a session attribute value or as a default argument. The credential is the second positional argument or the `password=` or `passwd=` keyword; a `username=` or `login=` value is never the credential. Not read: a name followed by `.` and not `(` (`BasicAuth.decode(`, `BasicAuth.from_url(`, which parse an inbound header or an address), a type annotation, `isinstance(x, HTTPBasicAuth)`, a subclass definition (`class X(HTTPBasicAuth):`), a helper renamed on import and called under the new name, and the different words `HTTPProxyAuth(`, `HTTPDigestAuth(` and `HTTPBasic(`. A construction that is the value of the `proxy_auth` keyword is a proxy credential and is not read.

**Example.** Placeholders only:

```python
	response = requests.get(url, auth=HTTPBasicAuth(user_name, secret))
	client = httpx.Client(auth=httpx.BasicAuth(username=user_name, password=secret))
	session = aiohttp.ClientSession(auth=aiohttp.BasicAuth(user_name, secret))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' '\b(http)?basicauth\s*\('
```

**Confidence.** **High.** The name of the helper declares the scheme, and the object's only use is the `auth` argument of a request or a client, so the construct is High wherever it sits in executable code that is not test code. When the password is a string literal, the match also raises the in-code secret warning. Not read as a finding: a proxy credential, Digest authentication and the inbound helpers named above. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-001-migration).

### PY-002 · NamedClientAuthTuple

<a id="py-002"></a>

The `auth` keyword of a call to a named client, given a parenthesized pair: `requests.get(url, auth=(user_name, secret))`, and the same for the other verbs of `requests` and `httpx` (`get`, `post`, `put`, `patch`, `delete`, `head`, `options`, `request`, `stream` where the client has it), `requests.Request`, `httpx.Client` and `httpx.AsyncClient`. The callee is read as the module word `requests.` or `httpx.` followed by the name, not preceded by a dot or a word character, so `pkg.requests.get(` is not read, and a module alias or a name imported with `from` makes the callee unnamed: the same pair is then [PY-006](#py-006). The pair has exactly two elements, a trailing comma allowed; a list, a name, a one-element tuple and a three-element tuple are not read. `aiohttp` is deliberately not in the list: its documented `auth` argument is a `BasicAuth` object and no page documents a tuple for it. Not read: a call whose text holds the word `grant_type` within the call, which is a token request that authenticates the client to the token endpoint and is not a Basic migration, and an `auth` value that is an object (`HTTPDigestAuth(...)`, a bearer helper).

**Example.** Placeholders only:

```python
	response = requests.get(url, auth=(user_name, secret))
	client = httpx.AsyncClient(auth=(user_name, secret))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' '\bauth\s*=\s*\('
```

**Confidence.** **High.** In a call to a named `requests` or `httpx` client the `auth` keyword with a pair is documented as Basic Authentication, so the construct is High in executable code that is not test code. When the second element is a string literal, the match also raises the in-code secret warning. A pair held in a variable (`auth=creds`) is not read; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-002-migration).

### PY-003 · AuthorizationHeaderBasicScheme

<a id="py-003"></a>

A write of an `Authorization` header whose value starts with the literal `Basic`. The header is written in one of four ways: the `headers` keyword or a name `headers` given a mapping (`headers={"Authorization": ...}`, `headers = {...}`), an attribute `headers` updated or assigned (`session.headers["Authorization"] = ...`, `session.headers.update({...})`), the methods `add_header(`, `add_unredirected_header(` and `putheader(` with the header name as the first argument, and the keyword form `dict(Authorization=...)`. The value starts with a plain, raw or bytes literal, an `f`-string, or the left operand of a `+`, of a `%` or of `.format(`, and its first word is `Basic` in any letter case (`"Basic "`, `"Basic{}"`; `"BasicAuth"` is another word), or the value is a call of `encode_basic_auth(` of `aiohttp`. A header whose value is an `f`-string, a `%` construction or a `.format(` over an encoder is one finding of this class, and an encoder that stands in another statement is a second finding of [PY-007](#py-007). Not read: a leading word other than `Basic` (`Bearer`, `Token`, `Digest`), which is the migrated form; the names `Proxy-Authorization` and `X-Authorization`; a `t`-string value; a value that starts with a name, which is [PY-013](#py-013); and a header mapping with no carrier, which is [PY-014](#py-014). A read of the incoming header (`request.headers["Authorization"]`, `request.authorization`) is inbound authentication and is not read.

**Example.** Placeholders only:

```python
	response = requests.get(url, headers={"Authorization": "Basic " + encoded})
	session.headers["Authorization"] = f"Basic {encoded}"
	request.add_header("Authorization", "Basic %s" % encoded)
	headers = dict(Authorization="Basic {}".format(encoded))
	session = aiohttp.ClientSession(headers={"Authorization": encode_basic_auth(user_name, secret)})
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' 'authorization.{0,3}\s*[:=,]'
```

**Confidence.** **High.** The scheme word or the Basic-named encoder is in the text and the carrier is a header write, so the construct is High in executable code that is not test code. When the token after `Basic ` is a literal, the match also raises the in-code secret warning. Not read as a finding: a leading word other than `Basic`, a proxy header and an inbound read. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-003-migration).

### PY-004 · UrllibBasicAuthHandlerConstruction

<a id="py-004"></a>

A Basic handler built for `urllib.request`: `HTTPBasicAuthHandler(` followed by a parenthesis, with or without the `urllib.request.` qualification or a module alias. The handler is the object that answers a challenge with Basic credentials; the credentials are given to a password manager in another statement, which is [PY-009](#py-009), and the two are reported as two findings. Not read: `ProxyBasicAuthHandler(`, which is a proxy credential, `HTTPDigestAuthHandler(` and `ProxyDigestAuthHandler(`, which are Digest, and `AbstractBasicAuthHandler` used as a base class.

**Example.** Placeholders only:

```python
	handler = urllib.request.HTTPBasicAuthHandler(password_manager)
	opener = urllib.request.build_opener(urllib.request.HTTPBasicAuthHandler())
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' '\bHTTPBasicAuthHandler\s*\('
```

**Confidence.** **High.** The name of the handler declares the scheme, so the construct is High in executable code that is not test code. The class reads no credential, because the credential is in the password manager, and raises no in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-004-migration).

### PY-005 · Urllib3MakeHeadersBasicAuth

<a id="py-005"></a>

A call of `make_headers` of `urllib3` (with or without `urllib3.util.`, `urllib3.`, `util.` or a module alias) whose arguments hold the keyword `basic_auth`. The keyword holds a colon-separated `user:password` string, and the call builds the `authorization: basic ...` header. The credential is the password part of a literal `"user:password"`; an `f`-string with a replacement field is not literal. Not read: the keyword `proxy_basic_auth`, which builds a proxy header.

**Example.** Placeholders only:

```python
	headers = urllib3.util.make_headers(basic_auth=f"{user_name}:{secret}")
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' '\bbasic_auth\s*='
```

**Confidence.** **High.** The keyword is documented as the Basic header builder, so the construct is High in executable code that is not test code. When the argument is a literal `"user:password"`, the match also raises the in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-005-migration).

### PY-006 · UnnamedAuthTuple

<a id="py-006"></a>

The word `auth` as a keyword, an attribute after a dot or a simple name, followed by `=` and a parenthesized pair, where no named `requests` or `httpx` call is required: `session.auth = (user_name, secret)`, `session.get(url, auth=(user_name, secret))`, a call through a module alias, an `aiohttp` call with a tuple, and `auth = (user_name, secret)`. The receiver is not examined, so the class also reads a pair given to a client that is not an HTTP client (a database driver, a graph database or a message broker uses the same keyword for its own login). Not read: a pair on a line that holds the word `grant_type` within 300 characters either side, which is a token request; a name that merely ends in `auth` (`proxy_auth`); and a pair of the wrong size.

**Example.** Placeholders only:

```python
	response = session.get(url, auth=(user_name, secret))
	session.auth = (user_name, secret)
	auth = (user_name, secret)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' '\bauth\s*=\s*\('
```

**Confidence.** **Medium**, never High. Nothing in the text says which callee receives the pair or that the scheme is Basic, so the class is capped at Medium by design, and it also reads a pair given to a non-HTTP client; confirm what the pair is given to. When the second element is a string literal, the match also raises the in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-006-migration).

### PY-007 · Base64UserPasswordPair

<a id="py-007"></a>

A Base64 encoder (`b64encode`, `standard_b64encode` or `encodebytes`, with or without the `base64.` qualification or a module alias) whose first argument is a user name and a password joined by a colon: `f"{user_name}:{secret}"`, `user_name + ":" + secret`, `"%s:%s" % (user_name, secret)`, `"{}:{}".format(user_name, secret)`, `":".join((user_name, secret))` and a literal `"user:secret"`, with or without a trailing `.encode(...)` or a `b""` prefix. A pair that is built in one statement and held in a name is also read when the encoder is given that name, or the name with `.encode(...)`. The encoder is read only when the word `Basic` or `Authorization` stands within 300 characters either side of the pair, across lines; for a held name only the word `Basic` counts. Without that word the encoder is a signature, a nonce or a cache key and is not read. Not read: `urlsafe_b64encode`, `b32encode`, `b16encode`, `a85encode`, `b85encode` and every decoder, because the decoder is the inbound direction.

Two forms are read that are easy to miss. An `f`-string whose replacement fields hold quote characters is read when its literal has the prefix `f` or `F`: the colon of the pair is the first colon outside every field, and a field holds a name chain with up to three trailers, each a subscript with one quoted string or a call with quoted strings or names (`f'{cfg["user"]}:{cfg["password"]}'`, `f"{os.getenv('U')}:{os.getenv('P')}"`). A field with an operator, a conditional, a conversion, a format specification or deeper nesting is not read, and a literal without the prefix `f` holds no replacement fields. And a held name is read when the encoder call is written over several lines, with at most forty blanks, tabs and line breaks in each seam; a comment inside a seam is not whitespace and is not read.

**Example.** Placeholders only:

```python
	token = base64.b64encode(f"{user_name}:{secret_value}".encode()).decode()
	headers = {"Accept": "text/plain", "Authorization": "Basic " + token}
```

```python
	token = base64.b64encode(f"{settings['user']}:{settings['password']}".encode()).decode()
	headers = {"Authorization": "Basic " + token}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' '\b(b64encode|standard_b64encode|encodebytes)\s*\('
```

**Confidence.** **Medium.** The encoding is the Basic construction of the user-pass, but nothing in the call says it is sent as a header; the use word within the window is what makes the match. When the encoder is the value of an `Authorization` header written in the same statement, the header class [PY-003](#py-003) reads it at High and the finding is one finding. A pair that is a whole literal also raises the in-code secret warning; an `f`-string with a field, a concatenation and a `%` construction are not literal, and a pair held in a name carries no credential. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-007-migration).

### PY-008 · AiohttpEncodeBasicAuth

<a id="py-008"></a>

A call of `encode_basic_auth` of `aiohttp`, with or without the `aiohttp.` qualification, with at least one argument. The function builds the value of an `Authorization` header, or of a `Proxy-Authorization` header, and the call does not show which. When the call is the value of an `Authorization` header, [PY-003](#py-003) reads it at High and this class is not reported separately.

**Example.** Placeholders only:

```python
	value = aiohttp.encode_basic_auth(user_name, secret)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' '\bencode_basic_auth\s*\('
```

**Confidence.** **Medium.** The function declares the scheme and not the carrier or the destination. A call that builds a `Proxy-Authorization` header is reported here too, because the class cannot see the key from the callee: check the header the value goes to. When the second argument is a string literal, the match also raises the in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-008-migration).

### PY-009 · PasswordManagerAddPassword

<a id="py-009"></a>

A call of the method `add_password` after a dot with four or more arguments: `manager.add_password(None, "https://example.test/", user_name, secret)`, and the fifth argument `is_authenticated` of the prior-authentication manager. A password manager stores a user name and a password for a realm and an address and does not choose the scheme; the scheme is chosen by the handler that receives the manager, in another statement ([PY-004](#py-004) for Basic).

**Example.** Placeholders only:

```python
	manager = urllib.request.HTTPPasswordMgrWithDefaultRealm()
	manager.add_password(None, "https://example.test/", user_name, secret)
	prior_manager = urllib.request.HTTPPasswordMgrWithPriorAuth()
	prior_manager.add_password(None, "https://example.test/", user_name, secret, is_authenticated=True)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' '\.add_password\s*\('
```

**Confidence.** **Medium.** The manager is scheme-agnostic, so the match does not show Basic. A manager that is given to a Digest handler or to a proxy handler is reported here too and is not told from the Basic case: check the handler. When the fourth argument is a string literal, the match also raises the in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-009-migration).

### PY-010 · HttpxNetrcAuth

<a id="py-010"></a>

A call of `NetRCAuth(`, with or without the `httpx.` qualification. The helper makes `httpx` read the user name and the password of the requested host from a `.netrc` file and send them with HTTP Basic authentication. The credential is in a file outside the repository, so the code holds no credential and the class reads none.

**Example.** Placeholders only:

```python
	client = httpx.Client(auth=httpx.NetRCAuth())
	client = httpx.Client(auth=httpx.NetRCAuth(file="/path/to/netrc"))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' '\bNetRCAuth\s*\('
```

**Confidence.** **Medium.** The text names the source of the credential and not the scheme, and the credential is not in the code: the finding tells you that the program sends Basic from a `.netrc` file, and the file is where the credential lives. No in-code secret warning is raised. The implicit use of a `.netrc` file, where no construct appears in the code, is not read; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-010-migration).

### PY-011 · CredentialsInRequestUrl

<a id="py-011"></a>

A call of a request-making name whose arguments hold an address that carries a user name and a password: `requests.get("https://user:secret@example.test/items")`, and the same through the names `get`, `post`, `put`, `patch`, `delete`, `head`, `options`, `request`, `stream`, `urlopen` and `Request`, qualified or not and on any receiver (`session.get(`, `client.stream(`, `urllib.request.urlopen(`). The address is a string literal in any argument of the call, at the first level of nesting: a literal inside a list, a tuple or a mapping given to the call (a `proxies` mapping) is not entered. The scheme must be `http` or `https`; every other scheme (a database, a cache, a broker, a file transfer address) is not read. The user name and the password hold no blank, `/`, `@`, quote, backslash or brace, so an `f`-string with a replacement field in the user information is not read, while one with a field in the host or the path is read. What the call does with such an address depends on the client, and the tool does not claim to know. Not read: an address assembled from parts, an address held in a variable (which is [PY-012](#py-012)), and an address held under a name or keyword that contains `proxy` or `proxies`, which is a credential for a proxy.

**Example.** Placeholders only:

```python
	response = requests.get("https://user:secret@example.test/items")
	async with session.get("https://user:secret@example.test/items") as response:
		...
	response = urllib.request.urlopen("https://user:secret@example.test/items")
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' 'https?://[^/@ ]+:[^/@ ]*@'
```

**Confidence.** **Medium.** Whether a client turns the user information of an address into a Basic header is a fact of the client, and the tool does not know it. The credential is in the source whichever client reads it, so the finding is worth acting on at once. A literal password in the address also raises the in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-011-migration).

### PY-012 · UrlUserinfoHeldLiteral

<a id="py-012"></a>

An address with a user name and a password held in a variable or given as a keyword: `BASE_URL = "https://user:secret@example.test/items"`, `client = Client(base_url="https://user:secret@example.test/")`. The class reads the name or keyword directly before `=` or `:=` whose value is a string literal of that shape, and an annotated declaration at the last name of its annotation when that name directly precedes the `=`. The scheme must be `http` or `https`. Not read: an attribute target (`self.url = "..."`), a name or keyword that contains `proxy` or `proxies`, and an address assembled from parts. When the keyword is the argument of a request call, [PY-011](#py-011) reads the same address and the finding is one finding.

**Example.** Placeholders only:

```python
	BASE_URL = "https://user:secret@example.test/items"
	client = Client(base_url="https://user:secret@example.test/")
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' 'https?://[^/@ ]+:[^/@ ]*@'
```

**Confidence.** **Medium.** The consumer of the address is not visible. A literal password in the address also raises the in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-012-migration).

### PY-013 · AuthorizationHeaderValueHeldInVariable

<a id="py-013"></a>

An `Authorization` header whose value is a simple name (letters, digits and underscore, at most 64 characters) that the same file declares, before the use, as a value that begins with the scheme word `Basic`. The header is written by any carrier of [PY-003](#py-003). The declaration is `NAME = value`, or the annotated `NAME: annotation = value`, on one line at the start of a line, and the value begins with a literal whose first word is `Basic` in any letter case. Five forms of the value are read, each followed by the end of the statement: the literal alone, with or without a token (`"Basic"`, `"Basic dXNl..."`); the literal and `+` and an operand (`"Basic " + encoded`, `"Basic " + b64encode(pair).decode()`); an `f`-string that begins with the word (`f"Basic {encoded}"`); the literal and `%` and an operand; and the literal and `.format(` with its arguments. An operand is a name or an attribute chain of at most 64 characters followed by up to three call groups in all, each group possibly followed by further attribute accesses and calls; each call group is balanced, on one line, of at most 120 units, where a unit is one character or a nested group of at most 120 characters, and nested to one level. Not read, so the class stays silent: a declaration that continues past the operand (a second `+`, a conditional, `or`, a fourth call group), one over a line break or a backslash, a triple-quoted one, an attribute or subscript target, a declaration in another file, a parameter default, and a name that is declared more than once. A value that begins with another word (`Bearer`, `Token`, `Digest`) is the migrated form and is not reported; a name the file cannot resolve (a parameter, an import, a call) is silent too.

**Example.** Placeholders only:

```python
	scheme_value = "Basic"
	response = requests.get(url, headers={"Authorization": scheme_value})
```

```python
	compound_value = "Basic " + encoded
	response = requests.get(url, headers={"Authorization": compound_value})
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' 'authorization.{0,3}\s*[:=,]\s*[A-Za-z_][A-Za-z0-9_]*\s*[,)}]'
rg -n -i -g '*.{py,pyw}' '^\s*[A-Za-z_][A-Za-z0-9_]*\s*(:[^=]*)?=\s*[rRuUfF]{0,2}.Basic\b'
```

**Confidence.** **Medium.** The scheme word is not at the header write, so the class is capped at Medium, and a resolved `Basic` confirms the class and never raises it. A declaration of the form `NAME = "Basic <token>"` holds a literal credential in code: the finding then also raises the in-code secret warning, anchored at the header that uses the name, and the other forms carry no token and no warning. A declaration that no header uses gives no finding, because the class reads a header write and not a declaration. A held value is two facts at two places: when the declaration also holds an encoder over a colon-joined pair with a use word in its window, [PY-007](#py-007) reads that encoder at the declaration and the two findings stand. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-python.md#py-013-migration).

### PY-014 · BasicLiteralInAuthorizationMapping

<a id="py-014"></a>

A quoted key `"Authorization"` followed by a colon and a value that begins with the literal `Basic`, written in a mapping that no header carrier of [PY-003](#py-003) holds: a mapping built in a function and returned, held under another name, or merged later. The key is read in any quote and any letter case. The class reads a quoted key because every dictionary key in Python is a string, and the anchor is therefore text to the tool; the class is Low by its own definition. A mapping under a header carrier is read at High by [PY-003](#py-003), and one whose value holds an encoder is read by [PY-007](#py-007); in both cases this class is not reported separately. A value that is an `encode_basic_auth(` call is not read by this class.

**Example.** Placeholders only:

```python
	def build_headers():
		return {"Authorization": "Basic dXNlcjpzZWNyZXQ="}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{py,pyw}' '.authorization.\s*:\s*[rRbBuUfF]{0,2}.basic'
```

**Confidence.** **Low**, always, and always for manual review: no flow and no recommendation. A literal `Basic` header held in a mapping is the commonest way a fixed credential is written, so the tool reports it rather than staying silent, but it cannot show that the mapping reaches a request. When the token after `Basic ` is a literal, the match also raises the in-code secret warning, and the warning is kept at Low. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned) only lowers it further.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-python.md#py-014-judging).

## Flow signals

The tool recommends one of two OAuth 2.0 flows for every High or Medium finding. It chooses from signals in the code, and only from the code.

- An **interactive-user signal** is evidence that the credential belongs to a person who supplies it at run time. It decides **Authorization Code**.
- A **fixed-credential signal** is evidence that the credential is fixed for the integration. It decides **Client Credentials**.
- With **no signal**, the default flow applies. The default is **Client Credentials** unless your user profile file sets another, and the recommendation carries a note asking you to confirm the choice.
- If both kinds are present, the interactive-user signal wins. A file that reads a credential from the environment and falls back to a hidden prompt when it is unset therefore receives Authorization Code, although it may well be an unattended integration. Confirm the flow for such a file by hand.

**The cross-file limit.** Signals are read in the same file as the finding, and only there. A password typed in another module, a credential read in another file, and a credential that a caller passes in are not seen. For most real files the credential reaches the finding's file from another file, so the default is the expected outcome, and the note it carries is the guide. A comment never counts as a signal, and a literal credential written into the code is not evidence of either flow.

**Two kinds of application.** A Python file does not run in a browser page. A Python web server may act for people who sign in to it, and a command-line tool may ask a person for a password at a terminal; a job or a service acts as itself. The tool does not know which kind of application a file belongs to: it knows only the signals in the file. Part C labels its variants by application type for that reason, and each variant opens with the precondition it is for.

**Twelve signals are registered for Python, eight interactive-user and four fixed-credential.** Their names below are the names the tool uses.

Some signals read the name of a variable or a key. The name matches when it contains `password`, `passwd`, `passphrase`, `secret`, `credential`, `cred` or `token` anywhere, in any letter case, and when it contains `pass`, `pw`, `pwd` or `apikey` only as a whole word, so `DB_PASS` and `dbPass` match while `bypass` and `compass` do not. The signals that read a prompt or a form key use a narrower list: `password`, `passwd`, `pwd` and `credentials`, with no `token`.

### Interactive-user signals

- `GetpassCall`: the word `getpass` directly followed by an opening parenthesis, whether written `getpass.getpass(` or, after `from getpass import getpass`, as a bare `getpass(`. The standard library function prompts for a password without echoing it. Command-line code. A script run by a scheduler with no terminal that calls it is still read as interactive, because the tool reads the call and not the run.
- `InputPasswordPrompt`: a call of `input(` whose first argument is a string literal that holds a credential word of the narrower list within 100 characters of its opening quote. `input(` with a name is not read, a member call (`ui.input(`) is not read, and a function named `input` that the file declares in the thirty lines before the call is not the built-in.
- `WebBrowserOpenCall`: `open`, `open_new` or `open_new_tab` after the module word `webbrowser`, written without alias. A browser is opened for a person who is at a screen.
- `LoopbackRedirectReceiver`: a listener of the standard library (`HTTPServer(`, `ThreadingHTTPServer(` or `TCPServer(`) bound to a loopback host (`127.0.0.1`, `localhost` or `::1`) within 200 characters on the same line, together with a read of the query key `code` within 1,000 characters of the bind. Both parts are required, so a loopback test server that reads no `code` is not a signal, and a server bound to all interfaces is not either.
- `RequestFormCredentialRead`: a read of a form field named with a credential word from `request.form` (Flask) or `request.POST` (Django), by subscript or `.get(`. A key that ends in a label word (`password_label`, `password_field`) is refused, and `request` must not be preceded by a dot, so `self.request.POST[...]` is not read. Web-server code.
- `FastApiCredentialForm`: an annotated parameter named with a credential word and declared with `Form(`, on one line, or the name `OAuth2PasswordRequestForm` not preceded by a dot. Web-server code.
- `DjangoAuthenticateOrLogin`: a call of `authenticate(` with a `password=` keyword, or `login(request`. Web-server code.
- `FrameworkSessionUse`: `request.session[`, or the name `session` in a `from flask import` list written on one line. The word `session` alone, as in `requests.Session()`, is not a signal. Web-server code.

### Fixed-credential signals

- `EnvironmentCredentialRead`: `os.environ["NAME"]`, `os.environ.get("NAME")` or `os.getenv("NAME")` where `NAME` carries a credential word. `os` must be the unaliased module word, so `from os import environ` is not read, and `os.environ["HOME"]` is not a signal.
- `ConfigFileCredentialRead`: a read of a credential-named key from a receiver named `config`, `conf`, `settings`, `cfg` or `parser`, by subscript or `.get(`, or a read of an attribute of such a receiver whose name carries a credential word (`settings.SERVICE_PASSWORD`). The receivers are names by convention, not packages, and a configuration object with another name is not read.
- `NetrcCredentialRead`: a call of the method `authenticators` after a dot, the standard library's lookup of the credential of a host in a `.netrc` file.
- `CredentialNamedLiteralAssignment`: a declaration, an assignment, a keyword argument, a parameter default or an annotated class attribute whose name carries a credential word and whose value is a non-empty string literal or an `f`-string with no field. An attribute target (`self.password = "..."`) is not read, and a name that ends in a label word (`Label`, `Placeholder`, `Field`, `Name`, `Pattern`, `Regex`, `Length`, `Hint`, `Message`, `Error` or `Prompt`) is refused. This signal is its own reading and is not the in-code secret warning: a literal credential is a secret to remove, and it is evidence of a fixed credential only through this signal.

**What is deliberately not a signal.** The word `session` alone, `getpass.getuser(`, which returns the login name and asks for nothing, a loopback server that reads no `code`, and an inbound read of a Basic header. A literal credential is not evidence of either flow.

The names of the libraries, functions and receivers in the signals above are signal vocabulary, not class options: a wrong name loses a flow signal, so the file reads the default, and never loses a finding. They are named from the documentation of the standard library and of the frameworks, and have not been checked against real code.

Part C explains how to confirm the flow yourself: see [Confirming the flow](part-c-python.md#confirming-the-flow).

## Limits of the reading

Four limits change what a finding at its level or a missing finding means. Each is stated here in the words the tool's own report uses where it has them.

**A format specification is read for most forms of `#`, not all.** In an `f`-string or a `t`-string the text after the colon of a replacement field is literal text. The tool reads the field as code and repairs only the common forms of a hash in a format specification: a `#` that directly follows the colon, an alignment or fill character (`<`, `>`, `^`), a sign (`+`, `-`) or `=` is read as part of the specification (`f"{n:#x}"`, `f"{n:>#10x}"`). Most forms are covered, and not every form: a `#` after a space sign (`f"{count: #x}"`), a `#` after the `z` option (`f"{value:z#.2f}"`), a quote character used as the fill character (`f"{count:'>10}"`) and a backslash before a brace (`f"\{name}"`) are not. With the first three of these forms the tool can stop reading the file at that point.

**What an abandoned scan costs.** When the tool stops reading a file at such a form, the rest of the file is no longer trusted, and three things follow, each reported rather than hidden:

1. **The region after the point is read as string text.** A construct there is reported at Low for manual review and not at its own level, so a High finding that lies after the point is demoted to Low and is outside the recommendation.
2. **The credential and flow signals are silent after the point.** A literal credential there carries no in-code secret warning, and no flow signal there is seen: a file whose only interactive signal lies after the point is assigned the default flow, Client Credentials with its note, and not Authorization Code.
3. **The content demotion markers are not applied after the point.** A test class or a generated-file banner that lies after the point does not lower the finding, so a file that only such a marker would demote is not demoted there, and the finding carries no test-code mark. The markers read from the name of the file or its folder are not affected.

A comment written with no space after a colon, an operator character or a dictionary colon (`if ready:# note`) is read with the `#` as a format mark, so its words can be read as code, and an apostrophe or a triple quote in such a comment can open a string that nothing closes and lose the rest of the file. If a file that you expect to produce findings reports only Low ones after some point, or is reported as not fully read, look above that point for one of these forms.

**A match in a docstring is Low.** A docstring is a string, so a construct spelled in it is reported at Low with no recommendation, as described under [How confidence is assigned](#how-confidence-is-assigned). A docstring that quotes a legacy call, or a doctest line, is therefore a manual-review item and never a finding at the level of its class.

**When a run passes the findings cap, the drop order is by path prefix and not by confidence.** When a run finds more than 50,000 findings, the tool keeps the 50,000 whose path sorts first (ascending, ordinal order of the normalized path) and drops the rest, by path and not by confidence, and it keeps at most 2,000 findings from any one file; a folder whose path sorts early (for example `.venv/`) is kept ahead of one that sorts late (for example `tests/`, `third_party/` or `vendor/`), whatever the confidence of its findings, and a folder that sorts late can lose all its findings to a run that passes the cap. A Low finding that sorts early is therefore kept ahead of a High finding that sorts late. In a repository whose findings do not pass 50,000 the cap drops nothing. Because a vendored or environment folder is read and not skipped, a large one can push a real finding out of the cap; keep such folders out of the scanned path where you can, and the report says in aggregate that the cap was reached and never which findings were left out.

## Declared unsupported coverage

The catalog states what it does not cover, so that a missing finding is not read as a clean result. The tool prints the same list in its report. The list below is what the Python analyzer declares, in the tool's own words, followed by the reason.

- **type stub files ending in .pyi, notebooks ending in .ipynb and Cython sources ending in .pyx**: these are not read as Python: a stub holds signatures, a notebook holds its code inside JSON text and a Cython source is a different language; such files are not attributed to Python and stay in the list of files that were not assessed.
- **Python code held in a file of another type: a fenced block in a Markdown document, a template, a configuration file or a JSON string**: the containing file is not a Python script, so its text is not analyzed as one; search such files separately for credentials passed to an HTTP client.
- **a Python script with no file extension whose first line does not name python, python2, python3 or python3 followed by a dot and digits, in lower case, as the interpreter, directly or after env and options written as one word: python2 with a minor version such as python2.7, pypy, an interpreter that follows a variable assignment after env, an interpreter followed by the -S option, and a file with no first line at all**: such a file is not attributed to Python, so it stays in the list of files that were not assessed; a silent omission is the cost, and the report names the files it did not assess. The first line is read only for a file with no extension: a file ending in .py or .pyw is read whatever its first line names.
- **syntax that exists only in Python 2: the print statement, backtick quoting, raw_input and the combined ur prefix on a string**: Python 2 code is read as Python 3 and is not parsed as Python 2; a construct that has no Python 3 form is not recognized, while the library names that survive, such as the Basic authentication handler of the standard library, are.
- **a format specification in an f-string or t-string field that holds a hash after a space sign or after the z option, a quote character used as the fill character, or a backslash before a brace**: the text after the colon of a field is literal text in Python, but the tool reads the field as code and repairs only the common forms of a hash; with these forms it can stop reading the file at that point.
- **what follows a file that the tool stopped reading at such a form: every construct after that point**: the rest of the file is read as string text, so a construct there is reported at Low and not at its own level, a literal credential there carries no in-code secret warning, no sign-in evidence there is seen, so a file whose only evidence lies there is assigned the default flow, and a marker of a test or a generated file that lies there does not apply.
- **a comment written with no space directly after a colon, an operator character or a dictionary colon, such as a comment after a block opening colon, and one that holds an apostrophe or a triple quote**: the hash after such a character is taken for part of a format specification and not for a comment, so the words of the comment can be read as code, and an apostrophe or a triple quote in it can open a string that nothing closes.
- **another language written inside a string: SQL, markup, a template or a regular expression, and a doctest line in a docstring**: the text of a string is not read as another language or as code; a construct written in it is reported at Low for manual review, never at its own level.
- **a tuple, header mapping or URL held in a variable, module or argument and handed over elsewhere (for example an auth value that is a name, headers given as a name, `*args` and `**kwargs`); a URL assembled from parts; a list, a one-element or a three-element tuple in place of a pair; arguments nested deeper than four levels or longer than the reading window of one call; a URL with user information inside a bracketed group of a call such as a proxies mapping; and a token request whose grant type is built in another statement**: the tool reads one call at a time, within a bounded window, and does not join a definition to its use; a pair held in a name that is itself called auth is read, and a helper object held in a name is read where it is created.
- **a header written by merging, copying or a hook: the `|` and `**` operators, setdefault, copy, a custom adapter**: the header carriers the tool reads are a fixed list of the documented forms, and a header assembled by another mechanism has no carrier to anchor on.
- **a Basic header built by a project function and returned, or held with no header write; an encoder and its header in different statements farther apart than the reading window; a call to a wrapper function; a scheme held in a parameter default, in another module or in a multi-line or triple-quoted literal**: calls to a wrapper are not followed and the scheme of a held value is resolved only from one single-line declaration before its use in the same file; the construct inside the wrapper is read where it is written, and a literal credential handed to the wrapper carries no in-code secret warning at the call.
- **a Basic header assembled through a t-string template and rendered by project code, a header joined with join, and an assembly whose first operand is not a literal or whose scheme word is held in a constant or split across literals**: a template is not a string and no HTTP client documents rendering one, and only a leading literal word is read as the scheme.
- **the implicit use of a .netrc file: a client that reads credentials from the home directory or from the netrc environment variable with no construct in the code, and a program that reads the file itself with the standard library**: the code holds no Basic construct and the credential is in a file outside the analyzed text; search such files separately for credentials.
- **the credential options of HTTP clients the tool does not document, such as another library's own basic authentication, HTTP authentication or login option, and undocumented or private helpers and a hand-written authentication class**: the set of clients is open and no list can name every one; the header and the two-element auth forms are read whatever the client, and a Basic header that a hand-written class writes is read where it is written.
- **a request written by hand to a socket, a TLS stream or an asynchronous stream, including a literal Authorization: Basic line**: the set of places a request text can be written to is open.
- **code reached through reflection or text: exec, eval, getattr with a built name and an import with a built name**: executed strings are not parsed, and a name that is built at run time is not text at the call.
- **credentials of the non-HTTP protocols of the standard library (mail, file transfer and similar) and the Digest, NTLM, Kerberos and OAuth 1 helpers**: these are other mechanisms, not Basic authentication, and are deliberately not reported.
- **whether an auth pair, a proxy header built with the Basic encoder of an HTTP client, a password manager given to a Digest or proxy handler, or a Basic construct that is built and never sent (unreachable code, a branch that is never taken) is Basic authentication to RelativityOne**: the tool does not examine the receiver of a call, its destination or whether a line runs, so these constructs are reported at their level and may be false positives; review each one before acting.
- **confirmation that a call goes to RelativityOne and not to another server**: the tool sees a call and cannot determine the destination host; such findings are still reported, with a note that they may not apply if the destination is not RelativityOne - review each one before acting.
- **a test, mock, sample or generated file with a neutral name and no marker in its text**: no name or marker says it is one and no text shows it, so it is reported at its full level and may be a false positive; review such findings.
- **a product file named like a test, a source file under a folder named for tests, a source folder named like a virtual environment (venv, .venv, dist-packages, `__pypackages__`, .tox), and the folders env, virtualenv, .nox, .eggs and a versioned venv name**: the demotion of a file by its name rests on conventions: a product file named like a test or a source folder with such a name is reported at a lower level than it deserves, and a virtual-environment folder with another name is walked and reported at its full level.
- **sign-in evidence the tool does not list: a prompt library outside the standard library, a password given as a command-line argument, a web server whose sign-in is handled outside the code, a program that is already an OAuth callback, the raw_input prompt of Python 2, and a configuration read through a receiver that is not in the list**: the set of possible prompts is open and only the signals with a stable shape in the code are read; a file whose evidence is of another kind is assigned the default flow, which is an expected outcome and not a failure - where the program acts for a signed-in user, use the Authorization Code flow.
- **sign-in evidence written in a form the tool does not read: a request object reached through another object, a framework import list written over several lines, a qualified form class name, a form field with no annotation, a name imported from os or from the browser module, and a module renamed on import**: the signal expressions require the plain form, so such a file reads the default flow; the evidence is lost and never a finding.
- **a program that opens a browser to a help page and also reads its credential from the environment, a command-line tool that listens on all interfaces to receive a redirect, a program run with no terminal that prompts for a password, and a prompt function defined more than thirty lines before its call**: the flow rule assigns the interactive flow when an interactive signal is present, so such a file can be assigned Authorization Code where it is run for a service, and the other forms are read as the default; review the flow of such a file.
- **a file whose analysis takes too long, a file larger than 2 MiB, a line longer than 10,000 characters, a string literal read as part of a call that is longer than 300 characters, and a credential longer than 200 characters**: these are internal limits that keep the cost bounded: a file that trips a time limit or holds a line over the ceiling is skipped and counted in the report, so the loss is visible as a number, and a window that grew without bound would make the cost grow without bound.
- **which findings were left out when the findings cap was reached, per file or in all, including a real finding pushed out by the many findings of a walked vendored folder**: the cap is reported in aggregate only, so the report cannot say which findings were left out. The drop order of the cap is stated under [Limits of the reading](#limits-of-the-reading).
- **a literal credential written as a triple-quoted string, as adjacent literals, with a backslash escape or as the value of a login keyword, and a literal assigned to an attribute such as self.password or self.url**: the construct is still reported where its class reads it, but the in-code secret warning is not raised for these forms, and an attribute target that holds a literal credential or a user-information URL is not read at all.

### What the declared list does not say

One class is supported at Low only, `PY-014`: the tool recognizes it and routes it to manual review, so it is not in the list above. Part A does not describe the interpreter that runs your code, and a statement here about what a client does rests on that client's documentation at the version read; check a client's argument against the version you use.

**What was not run.** The behaviour of the third-party clients (`requests`, `httpx`, `aiohttp` and `urllib3`) rests on their documentation at the version read, and is unchecked for any other version. No third-party client was installed or executed. Read the findings with that in mind.
