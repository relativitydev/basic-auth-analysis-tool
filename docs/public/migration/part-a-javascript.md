# Rule Patterns for JavaScript (Part A of the Migration Documentation)

[Documentation index](../README.md#documentation)

**Contents**

- [Find your code](#find-your-code)
- [How confidence is assigned](#how-confidence-is-assigned)
- [Pattern classes](#pattern-classes)
	- [JS-001 · ClientAuthCredentialObject](#js-001--clientauthcredentialobject)
	- [JS-002 · ClientAuthMethodCall](#js-002--clientauthmethodcall)
	- [JS-003 · CredentialsInRequestUrl](#js-003--credentialsinrequesturl)
	- [JS-004 · AuthorizationHeaderScheme](#js-004--authorizationheaderscheme)
	- [JS-005 · Base64UserPasswordPair](#js-005--base64userpasswordpair)
	- [JS-006 · NodeBufferBase64Pair](#js-006--nodebufferbase64pair)
	- [JS-007 · AuthenticationHelperDefinition](#js-007--authenticationhelperdefinition)
	- [JS-008 · NodeCoreAuthString](#js-008--nodecoreauthstring)
	- [JS-009 · XhrOpenCredentials](#js-009--xhropencredentials)
	- [JS-010 · UrlCredentialAssignment](#js-010--urlcredentialassignment)
	- [JS-011 · NonHttpAuthObject](#js-011--nonhttpauthobject)
	- [JS-012 · ClientAuthMethodSingleArgument](#js-012--clientauthmethodsingleargument)
	- [JS-013 · UrlUserinfoHeldLiteral](#js-013--urluserinfoheldliteral)
	- [JS-014 · JqueryAjaxCredentials](#js-014--jqueryajaxcredentials)
	- [JS-015 · GotCredentialOptions](#js-015--gotcredentialoptions)
	- [JS-016 · RequestChallengeDependentAuth](#js-016--requestchallengedependentauth)
	- [JS-017 · AuthorizationHeaderValueHeldInVariable](#js-017--authorizationheadervalueheldinvariable)
	- [JS-018 · AuthorizationHeaderValueFromEnvironment](#js-018--authorizationheadervaluefromenvironment)
	- [JS-019 · AuthorizationHeaderValueFromSettingsProperty](#js-019--authorizationheadervaluefromsettingsproperty)
- [Flow signals](#flow-signals)
	- [Interactive-user signals](#interactive-user-signals)
	- [Fixed-credential signals](#fixed-credential-signals)
	- [Choosing the flow by hand](#choosing-the-flow-by-hand)
- [Declared unsupported coverage](#declared-unsupported-coverage)
	- [What the declared list does not say](#what-the-declared-list-does-not-say)

Part A describes the Basic Authentication patterns to look for in JavaScript code, so that you can find them in your own source. Each pattern class has an identifier, such as `JS-004`, which a finding also carries, so a finding and this document can be matched.

This is Part A of the Migration Documentation, for developers who need to find Basic Authentication in their own JavaScript code. You use it without running anything: it lists every pattern class, how to search for it by hand, its confidence level and which flow it needs. When you have found a match and its class, go to [Part C for JavaScript](part-c-javascript.md) to change it, and to [Part B](part-b-secret-management.md) for where the new secrets go. New here? Start with the [Migration Documentation overview](README.md).

Every pattern class below has the same five parts:

- **Identifier and name.** In the heading.
- **What it looks like.** A short description and a placeholder example.
- **How to search.** A pattern you can run with `rg` (ripgrep). A header name and the word `Basic` are matched in any letter case, so every search ignores letter case (`-i`). If you use `grep`, run `grep -rniE --include="*.js" --include="*.mjs" --include="*.cjs" --include="*.jsx"` with the same expression, replacing `\s` by `[[:space:]]`, `\w` by `[[:alnum:]_]` and `\b` by `\<` or `\>` where your `grep` does not read them (the same holds for `git grep -E`); inside a bracket expression, write `[:alnum:]_` without the outer brackets. In PowerShell the expression works unchanged with `Select-String`: `Get-ChildItem -Recurse -Include *.js,*.mjs,*.cjs,*.jsx | Select-String -Pattern '<expression>'`, which also ignores letter case. A search reads one line at a time, so a call or an object written over several lines is found through the line that holds the word the search looks for. A search finds candidate files and never replaces reading them: open each match and check it against the description of its class. `rg` skips files that your `.gitignore` excludes and hidden files and folders; to search them too, add `--hidden --no-ignore --glob '!.git'`.
- **Confidence.** When a match is High, Medium or Low, including the cases that go to manual review.
- **Which flow.** The signals that indicate Client Credentials or Authorization Code, and how to choose between them when no signal is found.

Some descriptions give the exact boundary of the rule (a distance in characters or lines). Read each candidate against the description, including its distance limit.

Every example uses obvious placeholders such as `userName`, `secret`, `url` and `host.example`. No example contains a real credential. To decide what to do about a pattern, go to [Part C](part-c-javascript.md); for where the new secrets go, see Secret Management (Part B of the Migration Documentation), [`part-b-secret-management.md`](part-b-secret-management.md).

The rules apply to files with the extensions `.js`, `.mjs`, `.cjs` and `.jsx`, and to files with no extension whose first line is a shebang that names `node` or `nodejs`, directly or after `env` and its options (`#!/usr/bin/env node`, `#!/usr/bin/env -S node --no-warnings`). The searches below use the glob `'*.{js,mjs,cjs,jsx}'`; leave the glob out and search the whole folder to include the scripts that have no extension. `TypeScript` files (`.ts`, `.tsx`, `.mts` and `.cts`) are read as TypeScript and are described in [Part A for TypeScript](part-a-typescript.md), `JavaScript` embedded in other files is not covered, and the folders that are left out are listed in [Declared unsupported coverage](#declared-unsupported-coverage).

**Two environments.** JavaScript runs in a browser and in Node.js, and the same construct can mean different things in each. Every class below says where its construct exists: in the browser, in Node.js or in both. A file is read as one language with one set of classes, so a finding in a file that runs in a browser and one in a file that runs in Node.js are decided by the same rules; the two environments differ in the advice in Part C and in the flow signals, below.

**JavaScript has no credential type.** Other languages send a credential through a type that names the credential. JavaScript sends it through an **option of an HTTP client**, a **header**, the **user information of an address** or an **encoder**, so the classes below are written from the forms of the call: which client or which carrier, which option, and how the value is written. A header form is read whatever the client, because it is anchored on the header write and not on a client name. The rules name these clients: `axios`, `request`, `superagent`, `got`, `node-fetch`, `undici`, jQuery and the Node.js `http` and `https` modules, with `fetch` and `XMLHttpRequest` in the browser and in Node.js. An options object held in a variable, a client behind an alias and a wrapper function are not followed; see [Declared unsupported coverage](#declared-unsupported-coverage).

**One call is read at a time.** A class that anchors on a call reads the arguments of that one call, up to a bounded length, with brackets and braces balanced. A string, a template literal and a regular expression are read as single units. A `new URL` binding and a function body are read within thirty lines of their definition; a header value held in a name is resolved anywhere earlier in the same file (JS-017, JS-018).

## Find your code

Start here if you know what your code does and not which class describes it. Each row names a common situation and the class that covers it. Open the class, read its description and its search, and check the match against your code. A situation that is not in the table may still be covered: the classes are listed in full below, and what the rules do not cover is in [Declared unsupported coverage](#declared-unsupported-coverage).

| Your code does this | Class |
| --- | --- |
| Gives an HTTP client an `auth` option with a user name and a password (`axios`, `request`) | [JS-001](#js-001) |
| Calls `.auth(` with a user name and a password on a request chain (`superagent`, `request`) | [JS-002](#js-002) |
| Calls `.auth(` with one argument, or with a third argument that selects automatic scheme choice | [JS-012](#js-012) |
| Puts a user name and a password in the address of a request (`https://user:password@host/...`) | [JS-003](#js-003) |
| Holds an address with a user name and a password in a variable or an object key | [JS-013](#js-013) |
| Writes an `Authorization` header whose value starts with `Basic` (`headers`, `setRequestHeader`, `setHeader`, `Headers`, `.set`, `.append`, a bracket assignment, or any object with an `Authorization` key) | [JS-004](#js-004) |
| Writes an `Authorization` header whose value is a variable that holds `Basic ...` | [JS-017](#js-017) |
| Writes an `Authorization` header whose whole value is read from the environment (`process.env`, `import.meta.env`, `Deno.env.get`, `Bun.env`), alone, with a fallback, or through a name assigned from it | [JS-018](#js-018) |
| Writes an `Authorization` header whose whole value is a settings property that is filled from an environment value (`settings.serviceAuthorization`) | [JS-019](#js-019) |
| Joins a user name and a password with a colon and encodes the result with `btoa` | [JS-005](#js-005) |
| Joins a user name and a password with a colon and encodes the result with `Buffer` | [JS-006](#js-006) |
| Gives Node.js `http` or `https` an `auth` option holding `user:password` | [JS-008](#js-008) |
| Sets `username` and `password` on a `URL` object | [JS-010](#js-010) |
| Gives `got` the `username` and `password` options | [JS-015](#js-015) |
| Calls `open` on an `XMLHttpRequest` with a user name and a password, or with an address that carries them | [JS-009](#js-009) |
| Calls `$.ajax` with `username` and `password` | [JS-014](#js-014) |
| Gives `request` an `auth` option or method with the immediate send turned off | [JS-016](#js-016) |
| Holds an `auth` object with a user name and a password that is not given to an HTTP client the rules name (a database driver, a mail transport, a message broker) | [JS-011](#js-011) |
| Defines its own function that builds the Basic header or encodes the credential | [JS-007](#js-007) |

## How confidence is assigned

A match has exactly one of three levels. The levels depend on what can be established from the file alone, and never on what the rest of your system does.

| Level | What it means |
| --- | --- |
| **High** | The code almost certainly performs Basic Authentication against a live call path. The construct names the scheme and the place the credential travels: an HTTP client is given a user name and a password through its documented option, or a header whose value starts with the literal `Basic` is written, in executable code that is not test code. |
| **Medium** | The code matches a Basic Authentication shape, but the scheme, the carrier or the destination cannot be established from the file alone. A High match in test code is also Medium. |
| **Low** | The match is present in the text but unlikely to be live authenticating code: a comment, a string that is not code, a mock, sample or example file, or a vendored or generated file. Seven classes are also Low by their own definition. |

Six rules apply to every class:

- **A class states a base level, and levels combine by taking the lower.** A class whose construct shows both the scheme and the carrier keeps its base level. A class whose construct does not, because the scheme is held in a variable or the use of an encoder is not in the text, lands at Medium at most. An unknown destination does not change the level. That is why several classes whose subject is certain are Medium.
- **Ambiguity resolves downward.** When two levels are possible, the lower one is used, so that uncertainty goes to manual review and not to a mechanical change.
- **A file's status only lowers the level.** A match is demoted when its anchor sits in a comment or in a string, or in a file that is test, mock, sample, vendored or generated code. **Test code is demoted to Medium and no lower:** a match that would be High in a source file is Medium in a test. Every other kind of file is demoted to **Low**. Nothing raises a level.
- **A Low match is for manual review, with no flow.** It is not evidence that the code does not authenticate. Part C says how to judge each case.
- **Every class anchors on a code token, never on a string.** A match is read from a call, a property or key, a method name, an encoder or a declared name, and then extends into the strings to read the header name, the scheme word and the credential. A class never anchors on the text `Basic ` or on a quoted header name, so the word `Basic` in a comment, a label or a menu entry is not a finding. A key written in quotes, such as a quoted `headers`, is read as text and matches at Low; see [Declared unsupported coverage](#declared-unsupported-coverage).
- **A construct that is read is a write, never a read.** A class reads a header being set, a credential being given to a client or a pair being encoded. A server that reads the incoming `Authorization` header, decodes it to check a user, or matches it in a test double is inbound authentication and does not match at High or Medium. The exception is a literal `Basic` value under an `Authorization` key of an object literal, which cannot be told from a header write: it matches JS-004 wherever the object sits, at Medium in test code.

What marks a file for demotion in JavaScript, with the level it gives:

- **Test code, Medium at most.** Folders named `test`, `tests`, `__tests__`, `spec`, `e2e`, `fixture`, `fixtures` or `cypress`. File names that end in `.test.` or `.spec.` followed by `js`, `mjs`, `cjs` or `jsx` (`login.spec.jsx`). A file that calls `describe(` and, within one hundred lines, `it(` or `test(`, found in code and not in a comment or a string. A test that uses a mock-server library is test code because of its folder, its name or its `describe` and `it` calls, and not because of the library it imports.
- **Mocks, Low.** Folders named `__mocks__`, `mock`, `mocks`, `stub`, `stubs`, `fake` or `fakes`; file names that end in `.mock.`, `.stub.` or `.fake.` followed by `js`, `mjs`, `cjs` or `jsx`. A mock is recognized by its folder or file name and by nothing else, so a mock with a neutral name matches at the level its class gives it, or at Medium when it is also in a test folder.
- **Samples, Low.** Folders named `sample`, `samples`, `example`, `examples`, `demo` or `demos`. Folders only: a file name that begins with `example` is not demoted, because it would also reach a real source file such as an `exampleClient.js`.
- **Vendored code, Low.** Folders named `vendor`, `third-party`, `third_party` or `bower_components`. These folders are read.
- **Generated files, Low.** File names that end in `.min.js`, `.bundle.js` or `.chunk.js`; a comment on the first line, or on the second when the first is the `#!` line, that holds `auto-generated`, `autogenerated`, `auto generated`, `@generated` or `generated by`; or a comment within the first ten lines that holds `do not edit`, `do not modify`, `do not hand-edit`, `changes will be lost` or `changes will be overwritten`. A comment on a later line that only says `generated by` is not a marker.
- **Letter case does not matter** for names and folders. Folders are matched as whole names, so `contest` does not match `test`, and the separator is part of the file suffix, so `latest.js` is not a test file.
- **Test and another kind together are Low.** A file that is test code and also a mock, a sample, vendored or generated, such as `mock_server.test.js`, is Low, and it is still test code.
- **Not a signal: a folder named `Deployment`.** The folder holds the code that authenticates, so a file under it keeps its full level. The cost is that a deployment file that only illustrates a pattern is judged as if it were live.
- **Not read at all: `node_modules`, `dist` and `build`.** These folders are left out of the search, in lower case, and `obj`, `.git`, `.vs` and `packages`, and a `bin` folder that has a `*.csproj`, `*.vbproj` or `*.fsproj` file beside it, so no demotion is needed for them.
- **Read like any other folder: a `bin` folder with no project file beside it, and `Dist` and `BUILD` in other letter cases.** No name marks their content as generated.
- **Test code that no rule recognizes.** A test with a neutral name, outside every test folder and with no `describe` and `it` calls, is judged as production code and keeps its level, which may be High.
- **A minified bundle.** A file with any line over 10,000 characters is not read, so a minified single-line bundle produces no match.
- **A shorter minified file.** It is read and demoted by its name.

**A literal secret is a separate fact.** When a class captures a credential and the password or token is written literally into the code, the match also holds an in-code secret, a separate fact that never prints the value. The in-code secret is independent of the level: a demoted match keeps it. A credential is literal in four cases only: a string literal in a credential position (a password argument, a `password` or `pass` member, a `.password` assignment); a template literal with no `${ }` hole that holds the pair or the password; the password in the user information of an address; and a literal token after `Basic ` in an `Authorization` header value. A password read from `process.env`, a variable, a call, or a concatenation of a literal with code is not literal, and a template literal with a hole is not literal. A placeholder is not literal either, and holds no in-code secret: the empty string; a run of three or more of the mask characters `*`, `x`, `.`, `-`, `_` and `#`; a marker word wrapped in `<>`, `[]`, `{{ }}` or `%%`; and the words `placeholder`, `changeme`, `change-me`, `change_me`, `replace-me`, `replaceme`, `your-password`, `your_password`, `redacted`, `todo`, `fixme`, `dummy` and `not-a-real-secret`, in any letter case. The finding itself is unchanged by a placeholder.

**How strings and comments are read.** Each comment and each string is delimited so that a match in text is demoted. A comment is `//` to the end of the line or `/* ... */`; a `#!` line is a comment only at the very first character of the file. A string is single-quoted, double-quoted or a template literal; a template literal may hold `${ }` holes, nested up to eight deep, and the inside of a hole is code. A regular expression literal is a unit of its own. Whether a slash opens a regular expression is decided from the character before it, so a few unusual layouts are read as a division; see [Declared unsupported coverage](#declared-unsupported-coverage). JSX is not parsed: a quote in element text damages only its own line, and a backtick or a block-comment opener in element text can make the rest of the file read as text. When the matches in a file are Low after some point where you expect higher ones, or the file is not fully read, look for an unterminated template literal or block comment, or a stray backtick in JSX text, above that point.

## Pattern classes

JavaScript has nineteen pattern classes. Twelve of them can reach High or Medium and point to a flow. Seven, `JS-007`, `JS-009`, `JS-011`, `JS-014`, `JS-016`, `JS-018` and `JS-019`, are always Low and always go to manual review.

### JS-001 · ClientAuthCredentialObject

<a id="js-001"></a>

An HTTP client given an `auth` option that holds a user name and a password: `axios.get(url, { auth: { username: userName, password: secret } })`, `axios.create({ ..., auth: { ... } })`, an assignment to `axios.defaults.auth`, and the same option of `request`. The `auth` key must be a direct member of the options object of the call, not nested under another member: an `auth` under a `proxy` member is a proxy credential and is not read. `axios` takes `username` and `password`; `request` takes `user` or `username` with `pass` or `password`. A member written as a bare name, with no value, counts as a member, so `axios.get(url, { auth: { username, password } })` is read, and either member may be written either way (`{ username: userName, password }`). Not read: a name that is the value of another key (`{ label: username, password: secret }`), a name inside a nested group, an `auth` object with only one of the two members, and the same two bare names as the request body with no `auth` key (`axios.post(url, { username, password })`). The class exists in the browser and in Node.js for `axios`, and in Node.js for `request`.

**Example.** Placeholders only:

```javascript
	const response = await axios.get(url, { auth: { username: userName, password: secret } });
	const client = axios.create({ baseURL: host, auth: { username: userName, password: secret } });
	const forwarded = await axios.get(url, { auth: { username, password } });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\bauth\s*[:=]'
```

**Confidence.** **High.** The documented `auth` option of the client is Basic Authentication, so the construct is High wherever it sits in executable code that is not test code. The `auth` object must sit in the call of a client the rules name; the same shape with no such call next to it is JS-011 and is Low. When the password is a string literal, or a template literal with no hole, the match also holds an in-code secret. Not read: a `bearer` member of the `auth` of `request`, which is the migrated form; `sendImmediately` set to false, which is JS-016; an options object held in a variable and given to the client elsewhere, and a client behind an alias, which are in [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-001-migration).

### JS-002 · ClientAuthMethodCall

<a id="js-002"></a>

An `auth` method called with a user name and a password on a request chain: `superagent.get(url).auth(userName, secret)`, and `request`'s `auth(username, password, sendImmediately, bearer)`. The method must have two or more arguments and sit on a chain that starts at a request-making call; a `.auth(` on an object that is not a request chain, such as an authentication SDK, takes no arguments of this shape and is not read. The class exists in the browser and in Node.js for `superagent`, and in Node.js for `request`.

**Example.** Placeholders only:

```javascript
	const response = await superagent.get(url).auth(userName, secret);
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\.auth\s*\('
```

**Confidence.** **High.** Two credential arguments to a method named for authentication declare both the scheme and the carrier. When the second argument is a string literal, the match also holds an in-code secret. Not read: a second argument that is an object with `type` set to `bearer` (`.auth(token, { type: 'bearer' })`), and a first argument `null` with a fourth bearer argument of `request`, which are the migrated forms; a third argument `false`, which is JS-016; a one-argument call and a third argument that selects `type: 'auto'`, which are JS-012. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-002-migration).

### JS-003 · CredentialsInRequestUrl

<a id="js-003"></a>

A request-making call whose address is a string literal, or a template literal with no hole, that carries a user name and a password: `https.get("https://user:password@host/path", handler)`, and the same through `https.request`, `axios` and its verbs, `got`, the `superagent` and `request` chains, `fetch`, `new Request` and the `fetch` and `request` of `undici`. The scheme must be `http` or `https`; `postgres://`, `mongodb://`, `redis://`, `amqp://`, `sftp://` and every other scheme are not read. What the address does depends on the consumer: Node.js `http` and `https` send Basic on the first request, and several clients document reading the credentials from the address, but the `fetch` built into Node.js and `new Request` **reject such an address with a `TypeError`** and send nothing (checked locally on one Node.js version). What `got`, `node-fetch` and the separately installed `undici` package (its `fetch` and its `request` function) do with such an address has not been checked. The class exists in Node.js, where the user information is sent, and in the browser, where `fetch` throws.

**Example.** Placeholders only:

```javascript
	const request = https.get("https://exampleUser:exampleSecret@host.example/path", handler);
	const response = await fetch("https://exampleUser:exampleSecret@host.example/path");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' 'https?://[^/@ ]+:[^/@ ]*@'
```

**Confidence.** **High.** The credential is in the source whichever consumer reads it, so the construct is High in executable code that is not test code. A literal password in the address also holds an in-code secret. Where the consumer is `fetch` the call cannot have authenticated, so the finding is a bug report as much as a migration item. Not read: any scheme but `http` and `https`; an address assembled from parts (a concatenation, a template with holes, a `URL` parsed from a string); an address held in a variable or a key with no call next to it, which is JS-013. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-003-migration).

### JS-004 · AuthorizationHeaderScheme

<a id="js-004"></a>

An `Authorization` header whose value starts with the literal word `Basic`, written through any of six carriers: the `headers` key of an options object; a header-setting call (`setRequestHeader(` of `XMLHttpRequest`, `setHeader(` of Node.js `http`); the `Headers` constructor; `.set(` and `.append(` with the header name as the first argument; a property assignment on a header object (`headers["Authorization"] = ...`, `headers.Authorization = ...`, `headers.common[...] = ...`); and the key `Authorization` of any object literal, which reads a header object that a function builds and hands to a client elsewhere, a template literal tagged by a function included. The header name and the scheme word may be written in any letter case. The value may be a literal token, a concatenation, an encoder call or a template with a hole. It may also be an environment read with a fallback literal that begins with `Basic` (`process.env.API_AUTH || "Basic ..."`, and the `??` form), which is a Basic header written in the code itself, used whenever the variable is absent. `Proxy-Authorization` and `X-Authorization` are not read. A Basic header whose value is built with an encoder call on the same line is one match, at the header, JS-004; the encoder inside it does not match again. When the encoded pair is a literal inside the encoder (`'Basic ' + btoa('exampleUser:exampleSecret')`), that single header finding holds no in-code secret, the same as an encoder built with the Node.js `Buffer`. An encoder separated from the `Basic` literal by another operand is not merged and matches separately as JS-005. The class exists in the browser and in Node.js.

**Example.** Placeholders only:

```javascript
	await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });
	xhr.setRequestHeader("Authorization", "Basic " + encoded);
	headers["Authorization"] = "Basic " + encoded;
	headers.set("Authorization", "Basic " + encoded);
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' 'authorization.{1,6}basic'
rg -n -i -g '*.{js,mjs,cjs,jsx}' '(setRequestHeader|setHeader)\s*\('
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\bheaders\s*\[|\bheaders\.(common|authorization)'
rg -n -i -g '*.{js,mjs,cjs,jsx}' '(\.set|\.append)\s*\(\s*.authorization'
```

**Confidence.** **High.** The scheme word is in the text and the carrier is a header write, sent as written on the first request, so the construct is High in executable code that is not test code. When the token after `Basic ` is a literal, the match also holds an in-code secret. The fallback literal of an environment read counts only when it is whole: `process.env.API_AUTH || 'Basic ' + suffix` is a concatenation, not a Basic header written in the code, and is not read by this class. A literal `Basic` value under an `Authorization` key is read wherever the object sits. Not read: any scheme but `Basic`; a header built by a function call, a hook or an interceptor; a `Basic` literal held in a variable with no header write in the same statement; a header object whose `Authorization` key holds an identifier, which the key carrier does not read and JS-017 excludes; and the reading of an incoming header, which is inbound authentication. A header whose value is an identifier is JS-017. A quoted carrier key, `"headers":`, is read as text and matches at Low. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-004-migration).

### JS-005 · Base64UserPasswordPair

<a id="js-005"></a>

A user name and a password joined by a colon and encoded with `btoa`, the value of a Basic header: `btoa(userName + ":" + secret)`, ``btoa(`${userName}:${secret}`)``, a literal pair, or `[a, b].join(":")`. A quoted literal pair that continues into a concatenation inside the encoder call, `btoa('exampleUser:exampleSecret' + suffix)`, is read as well, and so is a pair split across two quoted literals, a literal whose last character is the colon followed by a plus and a second quoted literal, `btoa('exampleUser:' + 'exampleSecret')`, with or without more concatenation after the second literal. Not read: a literal that ends in a colon followed by code (`btoa('exampleUser:' + secret)`), and two literals with no colon between them (`btoa('exampleUser' + 'exampleSecret')`). The rule reads the construct and does not enumerate the other arguments. `btoa` is a global of the browser and of Node.js. An encoder call that sits inside the value of a Basic header that JS-004 reads matches at the header and not here; an encoder in another statement, or in a value with no header write, matches here.

**Example.** Placeholders only:

```javascript
	const token = btoa(userName + ":" + secret);
	const header = "Basic " + btoa(`${userName}:${secret}`);
	const continued = "Basic " + btoa('exampleUser:exampleSecret' + suffix);
	const split = "Basic " + btoa('exampleUser:' + 'exampleSecret');
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\bbtoa\s*\('
```

**Confidence.** **Medium.** Building and encoding a pair is the shape of Basic Authentication, but the construct does not show the scheme or where the result goes, so it is Medium. To keep cache keys and `host:port` pairs out, the match needs the word `Basic` or the word `Authorization` within 300 characters on either side of the encoder, in any part of the text; an encoder with neither is not read at this class. A pair whose use is further away is read at the header, JS-004 or JS-017, if at all. `btoa(host + ":" + port)` is read as a pair, and only the use requirement keeps it out. A whole literal pair also holds an in-code secret. A quoted literal pair that continues into a concatenation inside the encoder call is read at Medium too, and it holds no in-code secret, because the literal is not the whole argument; a pair split across two literals is read at Medium too and holds no in-code secret either, because no one literal holds the pair. Not read: an encoder whose `Basic` or `Authorization` word is further than that; `btoa` of a value that is not a colon-joined pair. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-005-migration).

### JS-006 · NodeBufferBase64Pair

<a id="js-006"></a>

A user name and a password joined by a colon and encoded to Base64 with `Buffer`: ``Buffer.from(`${userName}:${secret}`).toString("base64")``, and the legacy `new Buffer(userName + ":" + secret).toString("base64")`. The pair is followed by `.toString(` with the argument `base64`. `Buffer` is a global of Node.js, and a browser bundle that polyfills it is read the same way.

**Example.** Placeholders only:

```javascript
	const header = "Basic " + Buffer.from(`${userName}:${secret}`).toString("base64");
	const legacy = "Basic " + new Buffer(userName + ":" + secret).toString("base64");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\bBuffer(\.from)?\s*\('
```

**Confidence.** **Medium.** As JS-005, the construct does not show the scheme or where the result goes. The match needs the colon-joined pair, a following `.toString(` whose argument is `base64`, and the word `Basic` or `Authorization` within 300 characters on either side. Not read: `base64url`, which is not Basic; a decode, `Buffer.from(value, "base64").toString()`, which is inbound; a pair that is not colon-joined. A whole literal pair also holds an in-code secret. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-006-migration).

### JS-007 · AuthenticationHelperDefinition

<a id="js-007"></a>

The definition of the code's own authentication helper: a function, declared with `function` or bound to a `const` as a function or an arrow, whose body builds a Basic header or encodes a credential from the function's own parameters (a construct of JS-004, JS-005 or JS-006 that uses them). The class reads the declaration, not the calls. The module pattern encourages exactly this centralization, so a helper is common. The class exists in the browser and in Node.js.

**Example.** Placeholders only:

```javascript
	function buildBasicHeader(userName, secret) {
		return { Authorization: "Basic " + btoa(userName + ":" + secret) };
	}
```

**How to search.** The class reads the body of the function, not its name, so the search below finds the helpers whose name follows the usual convention. To find the rest, run the searches of JS-004, JS-005 and JS-006 and look above each match for an enclosing function that uses its own parameters.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\w*basic\w*(auth|header|credential)\w*|\w*(auth|header|credential)\w*basic\w*'
```

**Confidence.** **Low**, always, and always for manual review. A helper hides its call sites, so they do not match, and calls to a wrapper are not followed. The construct inside the helper matches at its own location by its own class, so the helper and its inner construct are two matches at two places. A helper in another module is not read.

**Which flow.** A Low match has no flow and is for manual review. See [how Part C tells you to judge it](part-c-javascript.md#js-007-judging).

### JS-008 · NodeCoreAuthString

<a id="js-008"></a>

The `auth` option of the Node.js core `http` or `https` call, a string of the form `user:password` that Node.js turns into the Basic header: ``http.request({ host: hostName, path: "/path", auth: `${userName}:${secret}` }, handler)``. The call may be `get` or `request`, including the form that takes an address and then the options. The option is read whatever its value, a literal, a template, a concatenation or an identifier, because in these calls the property is Basic by definition. The class exists in Node.js.

**Example.** Placeholders only:

```javascript
	const req = http.request({ host: hostName, path: "/path", auth: `${userName}:${secret}` }, handler);
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\b(http|https)\.(get|request)\s*\('
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\bauth\s*:'
```

**Confidence.** **High.** Node.js documents the option as a `user:password` string that it turns into an `Authorization` header, and sends it on the first request, so the construct is High in executable code that is not test code. When the value is a literal pair, the part after the colon also holds an in-code secret; a template with a hole is not literal. Not read: an options object held in a variable and given to the call elsewhere; an `auth` string in the options of a driver that is not an `http` or `https` call. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-008-migration).

### JS-009 · XhrOpenCredentials

<a id="js-009"></a>

The `open` method of an `XMLHttpRequest` called with a user name and a password as its fourth and fifth arguments, or with an address that carries them as its second: `xhr.open("GET", url, true, userName, secret)`. `window.open(` and `document.open(` and an `open` with fewer than five arguments and no user information are not read. The class exists in the browser.

**Example.** Placeholders only:

```javascript
	xhr.open("GET", url, true, userName, secret);
	xhr.open("GET", "https://exampleUser:exampleSecret@host.example/path");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\.open\s*\((.*,){4}'
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\.open\s*\(.*://[^/@ ]+:[^/@ ]*@'
```

**Confidence.** **Low**, always, and always for manual review: no flow. As the web platform's specification for the request is written, the credentials given this way are sent only in answer to a challenge from the server, and RelativityOne does not challenge with Basic (this rests on a single RelativityOne instance observed for the other languages, is not documented by Relativity, and was not probed on Relativity Server), so as written the call does not send Basic to it. A server that does challenge with Basic gets it. This reading rests on the text of the specification and has not been run in a browser, so what a given browser sends is unchecked. Confirm where the call points and whether a preemptive option or a hand-built header is also used. A literal password in the fifth argument also holds an in-code secret.

**Which flow.** A Low match has no flow and is for manual review. See [how Part C tells you to judge it](part-c-javascript.md#js-009-judging).

### JS-010 · UrlCredentialAssignment

<a id="js-010"></a>

A `username` or a `password` assigned on a `URL` object, which builds an address with user information: `target.username = userName; target.password = secret;` where `target` was created with `new URL(base)` within the previous thirty lines. The `URL` class exists in the browser and in Node.js. The two assignments are two findings.

**Example.** Placeholders only:

```javascript
	const target = new URL(base);
	target.username = userName;
	target.password = secret;
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\.(username|password)\s*='
rg -n -i -g '*.{js,mjs,cjs,jsx}' 'new\s+URL\s*\('
```

**Confidence.** **Medium.** The assignments may be far apart and the address may not be used for a request in the same file, and the consumer is not seen: Node.js `http` sends the user information as Basic, the built-in `fetch` throws on it, and an `XMLHttpRequest` given the same address is read at Low by JS-009. The match needs a name that a `new URL(` expression binds within thirty lines, which keeps `user.password = value` of an ordinary application out. A literal password assignment also holds an in-code secret. A `URL` whose scheme is not visible where the setters are, such as one built from a parameter, is read, and a masked value assigned to a non-HTTP address there matches at Medium. Not read: a receiver bound further away, or built with `URL.parse` or from parts. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-010-migration).

### JS-011 · NonHttpAuthObject

<a id="js-011"></a>

An `auth` object that holds a user-like member (`username`, `user` or `login`) and a password-like member (`password`, `pass` or `pwd`), with no HTTP client the rules name in the same call: `openMailTransport({ host: mailHost, port: mailPort, auth: { user: userName, pass: secret } })`. A database driver, a mail transport and a message broker use this shape for their own login, so the construct is the shape of an HTTP client's option without being one. The class exists in the browser and in Node.js.

**Example.** Placeholders only:

```javascript
	const transport = openMailTransport({ host: mailHost, port: mailPort, auth: { user: userName, pass: secret } });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\bauth\s*:\s*\{.*\b(user|username|login)\b.*\b(pass|password|pwd)\b'
```

**Confidence.** **Low**, always, and always for manual review: no flow. The credential goes to a sink that is not an HTTP client, such as remoting, mail, file transfer, a directory or a proxy; that is live code, but not Basic Authentication towards RelativityOne. The destination is not visible, and the shape is the one an HTTP client uses, so no migration can be assumed for it. It matches so that you can see where such a credential object sits. Where an HTTP client the rules name is in the same call, the match is JS-001 or JS-016 instead, and this class does not match. A literal password-like member also holds an in-code secret, which a Low match keeps.

**Which flow.** A Low match has no flow and is for manual review. See [how Part C tells you to judge it](part-c-javascript.md#js-011-judging).

### JS-012 · ClientAuthMethodSingleArgument

<a id="js-012"></a>

An `auth` method called with one argument, or with three arguments whose third is an options object with `type` set to `auto`, on a request chain: `superagent.get(url).auth(credentialPair)`. With one argument the client takes the password as empty and, with no options, its type is `basic`; with `auto` it leaves the choice of scheme to the user agent. The same call with an options argument that selects `bearer` is the migrated code. The class exists in the browser and in Node.js.

**Example.** Placeholders only:

```javascript
	const response = await superagent.get(url).auth(credentialPair);
	const automatic = await superagent.get(url).auth(userName, secret, { type: "auto" });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\.auth\s*\(\s*[^,)]+\)'
rg -n -i -g '*.{js,mjs,cjs,jsx}' 'type.{1,4}auto'
```

**Confidence.** **Medium.** The single-argument form defaults to Basic in the documented package version, and the `auto` form does not fix the scheme in the text, and the same call with a `bearer` option is the migrated code, so the construct alone does not establish the scheme. A call that selects `bearer` is not read. The class has no literal credential position.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-012-migration).

### JS-013 · UrlUserinfoHeldLiteral

<a id="js-013"></a>

An address with a user name and a password held as a string literal, or a template literal with no hole, in a declaration or an object key, with no call in the same text to use it: `const baseUrl = "https://user:password@host/path"`, or `{ baseURL: "https://user:password@host" }`. The declaration is `const`, `let`, `var` or a bare assignment. The scheme must be `http` or `https`. The class exists in the browser and in Node.js.

**Example.** Placeholders only:

```javascript
	const baseUrl = "https://exampleUser:exampleSecret@host.example/path";
	const options = { baseURL: "https://exampleUser:exampleSecret@host.example" };
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' 'https?://[^/@ ]+:[^/@ ]*@'
```

**Confidence.** **Medium.** The literal may be a `git` remote or an unused constant, and where it is used is not in the text, so the construct is one level below High. Where a known call holds the same literal in its arguments, the match is JS-003 and this class does not match. A literal password in the address also holds an in-code secret. A name or a key that contains `proxy` or `proxies`, in any letter case (`proxyUrl`, `httpsProxy`, `PROXY_URL`), marks the address as a credential for a proxy and not for the service, and there is no match, even when the address points at the target; an address held under a neutral name still matches, whatever it points at. Not read: an address assembled from parts, a literal in a configuration file, and a literal in another module. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-013-migration).

### JS-014 · JqueryAjaxCredentials

<a id="js-014"></a>

A jQuery request given a user name and a password: `$.ajax({ url: apiUrl, username: userName, password: secret })`, and the same through `jQuery.ajax` and `ajaxSetup`. A jQuery request goes through `XMLHttpRequest`, so the form is the one of JS-009, and the library's documentation describes the two settings as credentials for answering an authentication challenge. The class exists in the browser. The version in which each setting was added is not known.

**Example.** Placeholders only:

```javascript
	$.ajax({ url: apiUrl, username: userName, password: secret });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\b(ajax|ajaxSetup)\s*\('
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\busername\b.*\bpassword\b'
```

**Confidence.** **Low**, always, and always for manual review: no flow. As written the call sends Basic only to a server that challenges with Basic, and RelativityOne does not challenge with Basic (this rests on a single RelativityOne instance observed for the other languages, is not documented by Relativity, and was not probed on Relativity Server), so as written it does not send Basic to it. Confirm where the call points and whether a preemptive option or a hand-built header is also used. A literal password also holds an in-code secret.

**Which flow.** A Low match has no flow and is for manual review. See [how Part C tells you to judge it](part-c-javascript.md#js-014-judging).

### JS-015 · GotCredentialOptions

<a id="js-015"></a>

A `got` call given both `username` and `password` options: `got(url, { username: userName, password: secret })`, and the same through the verbs of `got` and `got.extend(`. The two options are documented as the credential for Basic Authentication. The class exists in Node.js.

**Example.** Placeholders only:

```javascript
	const response = await got(url, { username: userName, password: secret });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\bgot(\.\w+)?\s*\('
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\busername\s*:'
```

**Confidence.** **High.** The two options are documented as the Basic credential, so the construct is High in executable code that is not test code. The documentation describes one package version, and earlier major versions may differ; the documentation does not state whether `got` sends Basic on the first request. A literal password also holds an in-code secret. Not read: a header set through a hook of `got`. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-015-migration).

### JS-016 · RequestChallengeDependentAuth

<a id="js-016"></a>

A `request` call whose `auth` option, or `auth` method, turns off the immediate send: `request.get(url, { auth: { user: userName, pass: secret, sendImmediately: false } })`. With `sendImmediately` false the client sends Basic only after it receives a 401 answer. The `request` client has been deprecated since 2020, and a repository that still uses it is the case this class is for. The class exists in Node.js.

**Example.** Placeholders only:

```javascript
	request.get(url, { auth: { user: userName, pass: secret, sendImmediately: false } });
	request.get(url).auth(userName, secret, false);
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' 'sendImmediately\s*:\s*false'
rg -n -i -g '*.{js,mjs,cjs,jsx}' '\.auth\s*\([^)]*,\s*false\s*[,)]'
```

**Confidence.** **Low**, always, and always for manual review: no flow. With the immediate send off, the call sends Basic only to a server that challenges with Basic, and RelativityOne does not challenge with Basic (this rests on a single RelativityOne instance observed for the other languages, is not documented by Relativity, and was not probed on Relativity Server), so as written it does not send Basic to it. Confirm where the call points and whether a preemptive option or a hand-built header is also used. A literal password also holds an in-code secret. JS-001 and JS-002 do not read this form, so no call matches two of these classes.

**Which flow.** A Low match has no flow and is for manual review. See [how Part C tells you to judge it](part-c-javascript.md#js-016-judging).

### JS-017 · AuthorizationHeaderValueHeldInVariable

<a id="js-017"></a>

An `Authorization` header written through one of the carriers of JS-004 (the `headers` key, a header-setting call, the `Headers` constructor, `.set(` or `.append(`, a bracket or dot assignment) whose value is an identifier, a precomputed token, where that identifier was declared once, before the use, in the same file, with a value that starts with `Basic`: `const encodedHeader = "Basic " + encodedPair;` then `{ headers: { Authorization: encodedHeader } }`. The class exists in the browser and in Node.js.

**Example.** Placeholders only:

```javascript
	const encodedHeader = "Basic " + encodedPair;
	await fetch(url, { headers: { Authorization: encodedHeader } });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' 'authorization.{0,3}\s*[:=,(]\s*[A-Za-z_$]'
rg -n -i -g '*.{js,mjs,cjs,jsx}' '=\s*.basic\b'
```

**Confidence.** **Medium.** The scheme is in a variable, so the construct does not by itself show that the header is `Basic`. The variable is resolved against the declarations of the file: one declaration, before the use, with a literal on the right of the equals sign. A value that resolves to `Basic` confirms the finding, which stays Medium; a value that resolves to `Bearer` is the migrated code and does not match; **a variable that cannot be resolved does not match either**, because an identifier-valued header is the usual form of code that has already migrated. A variable assigned from an environment value in the same file is JS-018, at Low, and one assigned from an environment value with a fallback literal that begins with `Basic` is read here, at Medium. Not read: a token built by a call, a parameter, a destructuring or a declaration in another module; a declaration that follows a brace or a semicolon on the same line; a literal continued over lines; and a template with a hole. The class has no literal credential position. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-javascript.md#js-017-migration).

### JS-018 · AuthorizationHeaderValueFromEnvironment

<a id="js-018"></a>

An `Authorization` header written through one of the carriers of JS-004 whose whole value, scheme included, is read from the process environment, so that the code never shows the word `Basic`. Four reads are covered and only these, in seven spellings: `process.env.NAME` and `process.env['NAME']`, `import.meta.env.NAME` and `import.meta.env['NAME']`, `Deno.env.get('NAME')`, and `Bun.env.NAME` and `Bun.env['NAME']`. The value may be the read alone; the read with one literal fallback after `||` or `??`; a template whose only hole is the read; or an identifier that was assigned from one of these in the same file, once, before the use. A fallback literal that begins with `Basic` is not this class: it is a Basic header written in the code itself and is read as JS-004, at High. A fallback that begins with `Bearer` is not read. A match that has an explicit `Bearer` value for an `Authorization` header written within thirty lines of it, before or after, of at most 400 characters each, is not read either (the guard is a window and not the whole file), because an environment variable that holds a whole `Authorization` value is, in real code, most often a bearer token. The class exists in Node.js, in Deno, in Bun and in browser code whose bundler substitutes the read at build time.

**Example.** Placeholders only:

```javascript
	await fetch(url, { headers: { Authorization: process.env.API_AUTH } });
	client.defaults.headers.common['Authorization'] = import.meta.env.VITE_API_AUTH;
	const authValue = process.env.API_AUTH ?? '';
	await fetch(url, { headers: { Authorization: authValue } });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' 'authorization.{0,3}\s*[:=,(]\s*(process\.env|import\.meta\.env|Deno\.env|Bun\.env)'
rg -n -i -g '*.{js,mjs,cjs,jsx}' '=\s*(process\.env|import\.meta\.env|Deno\.env|Bun\.env)'
```

**Confidence.** **Low**, always, and always for manual review: no flow. The header takes its whole value from the environment, so the code does not show whether the scheme is Basic or a bearer token; a bearer token is the migrated form and a Basic value is not, so confirm what the deployed value holds and migrate it if it is Basic. The environment itself is not read. The class has no literal credential position, and no in-code secret is counted for it. Not read: an environment read held through another variable, a destructuring, an alias or a call, and a read with optional chaining; a name with two declarations, or one declared after its use, or one declared in another file; a fallback that is not a whole literal and a chain of fallbacks; a template with other text or other holes; a different header (`Proxy-Authorization`, `X-Authorization`); an object literal that only holds the key, such as `const o = { Authorization: process.env.X }`, which the carriers of the class do not read; and a match with an explicit `Bearer` value written within thirty lines of it. The guard reads text and not code, so a comment or a string that spells the header name with a `Bearer` literal silences it too, a `Bearer` fallback of any environment read within the window silences it as well (the operator of a Bearer fallback must stand on the same line as its literal, so a Bearer fallback split over a line feed does not silence it), and a `Bearer` literal held in a variable first and given to the header later is not read; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned) only lowers it further.

**Which flow.** A Low match has no flow and is for manual review. See [how Part C tells you to judge it](part-c-javascript.md#js-018-judging).

### JS-019 · AuthorizationHeaderValueFromSettingsProperty

<a id="js-019"></a>

An `Authorization` header written through one of the carriers of JS-004 whose whole value is a property of a settings object, `<receiver>.<property>`, where the property name contains `authorization` in any letter case (`settings.authorizationHeader`, `config.serviceAuthorization`) and the same file assigns that property name from an environment value. The receiver is a chain of one to eight names and none of them is `headers`; an environment read written directly as the value is not a receiver, it is [JS-018](#js-018). The property is joined to its environment value by its name alone, and the join reads one member per line at the start of the line: an object member (`serviceAuthorization: process.env.NAME,`) or an assignment (`settings.serviceAuthorization = process.env.NAME;`, and `this.serviceAuthorization = process.env.NAME;`), where the environment value is one of the reads of JS-018, alone, with one literal fallback after `||` or `??`, or as the only hole of a template. The join does not depend on the order of the file: the settings object may be built after the function that uses it, which is the difference from JS-017 and JS-018. The environment variable must have a name that contains `auth`, `token`, `secret`, `credential`, `key`, `pass` or `basic`, so a host, an address, an endpoint or a port is not a credential, and a property whose name starts with `require`, `skip`, `enable`, `disable`, `is`, `has` or `use` is a flag and not a header value. The class exists in Node.js, in Deno, in Bun and in browser code whose bundler substitutes the read at build time.

**Example.** Placeholders only:

```javascript
	const settings = {
		serviceAuthorization: process.env.SERVICE_AUTH_HEADER
	};
	await fetch(url, { headers: { Authorization: settings.serviceAuthorization } });
	client.defaults.headers.common['Authorization'] = config.serviceAuthorization;
	const options = {
		authorizationHeader: process.env.SERVICE_AUTH_HEADER
	};
	await fetch(url, { headers: { Authorization: options.authorizationHeader } });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{js,mjs,cjs,jsx}' 'authorization.{0,3}\s*[:=,(]\s*[A-Za-z_$][A-Za-z0-9_$.]*\.[A-Za-z0-9_$]*authorization[A-Za-z0-9_$]*'
rg -n -i -g '*.{js,mjs,cjs,jsx}' '^\s*(this\.|[A-Za-z_$][A-Za-z0-9_$.]*\.)?[A-Za-z0-9_$]*authorization[A-Za-z0-9_$]*\s*[:=]\s*(process\.env|import\.meta\.env|Deno\.env|Bun\.env)'
```

**Confidence.** **Low**, always, and always for manual review: no flow. The header takes its whole value from a settings property that is filled from an environment value, so the code does not show whether the scheme is Basic or a bearer token; confirm what the deployed value holds and migrate it if it is Basic. The environment itself is not read. The class has no literal credential position, and no in-code secret is counted for it. The property is joined by name alone, in the same file, and the rule does not know which object a property belongs to: every assignment of that name written at the start of a line counts, whether it comes from an environment value, a literal or a call, and the use is read only when the file holds exactly one assignment of the name and it is an environment value; one residual limit is that the last member of an object literal whose value is a bare name or a dotted path with no trailing comma (`name: otherValue` followed by a line that closes the object) cannot be told from a type declaration, so it is read as a type and is not counted. A name assigned twice is not read, so two objects that carry the same property name hide a true use, and so does a literal assignment of that name elsewhere. A flag word after the word (`authorizationEnabled`) is read at Low when its environment variable has a credential word. A flag word at the start of the name (`requireAuthorizationHeader`) is not read. A fallback literal that begins with `Basic`, written directly in an `Authorization` header write, is JS-004 and not this class. Not read: a property whose name does not contain `authorization` (`auth`, `authHeader`); bracket access, destructuring, optional chaining and an object shorthand; a call written as the receiver; several members written on one line; a class field assigned from an environment value; a property filled from a call, from an alias of the environment object or from another file; a flag name and an environment variable named for something else; a property filled from an environment value with a scheme word as the fallback, which no rule matches when the header takes the property by reference; a different header (`Proxy-Authorization`, `X-Authorization`); an inbound read such as `req.headers.authorization`; and a match with an explicit `Bearer` value written for an `Authorization` header within thirty lines of it, before or after, of at most 400 characters each (a `Bearer` write 31 or more lines away does not silence it). The guard reads text and not code, so a comment or a string that spells the header name with a `Bearer` literal silences it too; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned) only lowers it further.

**Which flow.** A Low match has no flow and is for manual review. See [how Part C tells you to judge it](part-c-javascript.md#js-019-judging).

## Flow signals

Each High or Medium match needs one of two OAuth 2.0 flows. Choose it from signals in the code, and only from the code.

- An **interactive-user signal** is evidence that the credential belongs to a person who supplies it at run time. It decides **Authorization Code**.
- A **fixed-credential signal** is evidence that the credential is fixed for the integration. It decides **Client Credentials**.
- With **no signal**, when nothing shows that a person signs in, use **Client Credentials**; confirm the choice by hand.
- If both kinds are present, the interactive-user signal wins. A file that reads a credential from the environment and falls back to a hidden prompt when it is unset therefore points to Authorization Code, although it may well be an unattended integration. Confirm the flow for such a file by hand.

**The cross-file limit.** Signals are read in the same file as the match, and only there. A password typed in another module, a credential read in another file, and a credential that a caller passes in are not seen. For most real files the credential reaches the matching file from another file, so a match with no signal is the expected outcome; follow the credential to where it comes from and apply the criterion under [Choosing the flow by hand](#choosing-the-flow-by-hand). A comment never counts as a signal, and a literal credential written into the code is not evidence of either flow.

**Three kinds of application.** Browser code is user-facing by construction. A Node.js server may act for people who sign in to it, and a Node.js command-line tool may ask a person for a password at a terminal; a job or a service acts as itself. A file does not say which kind of application it belongs to: only the signals in it count. Part C labels its variants by application type for that reason, and each variant opens with the precondition it is for. The server markers alone, such as `require(`, `module.exports`, a `node:` import or `__dirname`, are not signals, because a Node.js login handler acts for a person.

**What marks a Node.js file.** A file with a first line naming `node` or `nodejs`, and a file with the extension `.cjs`, is read as a Node.js file. In such a file the browser signals do not fire, so a file that merely holds a variable named `document`, or markup in a string, is not read as interactive. A plain `.js` or `.mjs` file is not marked, because an ES module runs in both environments; the browser signals read there. The consequence is stated in [Declared unsupported coverage](#declared-unsupported-coverage): a plain `.js` server file that renders a page with a password field into a string is read as interactive.

**JavaScript has sixteen flow signals, twelve interactive-user and four fixed-credential.**

Some signals read the name of a variable or a key. The name matches when it contains `password`, `passwd`, `secret`, `credential`, `credentials`, `creds` or `token` anywhere, in any letter case, and when it contains `pass`, `pw`, `pwd` or `apikey` only as a whole word, so `DB_PASS` and `dbPass` match while `bypass` and `compass` do not.

### Interactive-user signals

- A lookup with `getElementById`, `querySelector` or `querySelectorAll` whose identifier or selector names a password field, followed within 200 characters by `.value`. Browser code; not read in a Node.js file.
- `.password.value` read from a form's `elements` or from a name that contains `form`, `new FormData(...).get("password")`, and `elements.namedItem("password").value`. A `document.password` with no `.value` is a record's field and is not read. Browser code; not read in a Node.js file.
- `window.prompt(`, or a bare `prompt(` with a credential word in its string argument and no declaration of `prompt` in the thirty lines before the call. A `prompt` the project or a prompt library defines is not the browser's. Browser code; not read in a Node.js file.
- `localStorage.getItem(` or `sessionStorage.getItem(` with a key that holds `password`, `passwd`, `pwd` or `credentials`. A stored token is deliberately not evidence. Browser code; not read in a Node.js file.
- An `open(` with five arguments after a dot, the form of JS-009. `window.open(` and `document.open(` are not read. Browser code; not read in a Node.js file.
- The text `<input` followed within 200 characters by `type` and `password`, in a string, a template body or JSX text. Not read in a Node.js file.
- A password member of a request body in a route handler (`req.body.password`, `request.body.password`, `ctx.request.body.password`, the bracket form, and a destructuring of `req.body`), and a `get("password")` on a `URLSearchParams` built from the request. A body member that is not a credential word is not read. Node.js server code.
- An assignment to `_writeToOutput`, or a call to `setRawMode(` with a credential word within 600 characters on either side: the idiom of a prompt that hides the typed characters. `_writeToOutput` is not a documented public interface and is named from common use. Node.js command-line code.
- An object key `type` whose value is the string `password`, the option the common terminal prompt libraries use for a hidden answer. No library is named. Node.js command-line code.
- The word `password` or `passphrase` in the string argument of a call to `question(` or `prompt(`. A validation message that holds the words is not a prompt. Not read in a Node.js file.
- `question(` called on a name that a `createInterface(` call binds in the same file, within thirty lines before the call. The wording of the question is not read. Node.js command-line code.
- A `new Writable(` whose text holds, within 400 characters, a word that names a muted state (`mute` or `muted`; `mutable` does not match). Node.js command-line code.

### Fixed-credential signals

- `process.env.NAME` or `process.env["NAME"]` where `NAME` carries a credential word. `process.env.NODE_ENV`, `process.env.PORT` and `process.env.HOST` are not read.
- A read of a credential-named setting from a receiver named `config`, `conf`, `nconf`, `settings` or `cfg`: `config.get("...")` with a credential word in the first argument, or a property read such as `config.apiPassword`. The receivers are names by convention, not packages.
- A call to a method that reads a secret store: `getSecretValue(`, `GetSecretValueCommand`, `getSecret(`, `accessSecretVersion(` and `getPassword(`. The names are taken from common use and have not been checked against each vendor's documentation, so a store whose method has another name is not read.
- A declaration, an assignment or an object key whose name carries a credential word and whose value is a non-empty string literal or a template literal with no hole. A value that is one of the three `fetch` credential modes (`include`, `omit`, `same-origin`) is refused, as is a name that ends in a label word (`Label`, `Placeholder`, `Field`, `Name`, `Pattern`, `Regex`, `Length`, `Hint`, `Message` or `Error`). This signal is its own reading and is not an in-code secret: a literal credential is a secret to remove, and it is evidence of a fixed credential only through this signal.

**What is deliberately not a signal.** The `credentials` member of `fetch` (`include`, `omit`, `same-origin`) and `withCredentials` are cookie policies and not a credential. A prompt whose wording sits in a validation message is not a prompt.

A library or method name in the signals above is signal vocabulary, not a class option: a wrong name loses a flow signal, so the file takes the flow of its other signals, or the default flow (Client Credentials unless another default is configured) if it has none, and never loses a match. They are named from common use and have not been checked against documentation.

### Choosing the flow by hand

The one question is who the code acts for. When no person signs in and the code acts for the integration itself, use **Client Credentials**. When the code acts for a person who is signed in, use **Authorization Code**. When both kinds of evidence are present, the interactive-user evidence wins and the flow is Authorization Code. When a file holds two unrelated pieces of code, one for a signed-in person and one that runs unattended, judge each piece by who it acts for. A literal credential written into the source is not evidence of either flow. When an OAuth2 Client already exists for this integration, use its flow: the flow is fixed when an OAuth2 Client is created and cannot be changed afterwards.

## Declared unsupported coverage

The rules state what they do not cover, so that a missing finding is not read as a clean result. The list below is what the JavaScript rules declare as unsupported, each followed by the reason.

Some entries below describe limits that apply only to an automated scan, such as a time limit or a cap on the number of results. They do not apply when you search by hand.

- **a request written by hand to a socket (net, tls or raw HTTP/2) with a literal Authorization: Basic line in the text**: the transports are an open set, and anchoring on the header text alone would make every file that mentions it a candidate; search such files separately for the literal header.
- **an options object, authentication object or header object held in a variable, a module or a default export and handed to a client elsewhere, and an alias of a client or of the http module**: the call and the object are in different statements and the reading is of text, not of the program; the object is read only where a header carrier or an authentication object with a user and a password sits in the same text.
- **a 'Basic ...' literal held in a variable with no header write in the same statement, and a header value held in an identifier that cannot be resolved**: a literal in a variable has no call to anchor on and its use is not in the statement; a name the file does not declare exactly once before its use is not resolved, and a value that resolves to another scheme is the migrated form. A header value that is an environment read (process.env, import.meta.env, Deno.env.get or Bun.env), alone, with a || or ?? literal fallback, or as the only hole of a template, is sent to manual review, and so is a name assigned from one in the same file. Left unread: an environment read held through another variable, a destructuring, an alias or a call; a name with two declarations, or one declared after its use; a fallback that is not a whole literal, and a chain of fallbacks; a template with other text or other holes; an object literal that only holds the key, such as const o = { Authorization: process.env.X }, which the carriers of the class do not read; and a header that has an explicit Bearer value written for an Authorization header within thirty lines of it, before or after, of at most 400 characters each, where a match is missed on purpose: the guard reads text and not code, so a comment or a string that spells the header name with a Bearer literal silences it too, and a Bearer literal held in a variable first and given to the header later is not read; a Bearer fallback of any environment read within the window silences it too, and the operator of a Bearer fallback must stand on the same line as its literal, so a Bearer fallback split over a line feed does not silence it.
- **header-setting methods and hooks of third-party clients whose documentation was not read for them, and a header built by a function call**: an option or method name that the rules do not list is not read.
- **a credentialed URL given directly to a jQuery or other XMLHttpRequest wrapper**: such a request sends the credential only after the server challenges for it, which is not how the target service behaves; a URL held in a variable is read as a held URL.
- **calls to a wrapper function are not followed; the construct inside the wrapper is matched where the wrapper is defined, and a literal given to an aliased callee is not read**: following a call needs the mapping of a named parameter to its argument, which an expression cannot make; the construct is matched once, at the definition, and the call sites are not.
- **computed property names, such as headers[name] or an object key written as [key], where the name is not a literal**: the name is not text at the call, so neither the header nor the option can be read.
- **generated API clients**: the call surface is hidden behind generated methods and no list can name every one.
- **variable use across statements: a credential assigned in one statement and sent in another is not joined**: a variable has no marker in this language and is not tracked; the cross-statement readings are a header value held in a name declared once before its use in the same file, a URL binding read within thirty lines, and a settings property joined to an environment value by name.
- **shell text inside an executed string, such as a curl command given to child_process**: the command is text inside a string and a string is not read as a script; search such calls separately.
- **a project's own wrapper beyond what one statement shows**: a wrapper's behaviour is not in the call to it and wrapper calls are not followed.
- **credentials assembled across modules**: no analysis joins a definition in one file to its use in another.
- **runtimes other than Node.js, such as Deno and Bun**: the runtimes are an open set; their built-in clients and shell helpers are not listed, and of their environment reads only Deno.env.get and Bun.env are read, as the value of an Authorization header, although code written on the standard fetch and XMLHttpRequest is read as any other.
- **proxy credentials: the Proxy-Authorization header, the credential member of a proxy option and user information in a proxy URL**: this is Basic towards a proxy and not towards the target, which is not the migration this text serves; such text is kept out of the matches. A URL with user information that is held under a name containing proxy is read as a proxy URL and is not matched, even when it points at the target service; review such a name by hand.
- **a quoted carrier key, such as 'headers' or "headers" written in quotes**: the key is read as text inside a string and a construct anchored there is routed to manual review at Low; the header name and scheme inside it are still read.
- **an encoder whose Basic or Authorization word is more than 300 characters from it**: the use requirement that keeps cache keys and host and port pairs out of the matches reads 300 characters on each side; the encoder is read at the header if at all.
- **headers given to undici as a flat array of alternating names and values**: the array has no key per header, so a name and its value are not read as a pair.
- **a header-writing construct built and never sent, such as a dead branch or a function that is never called**: whether a line runs depends on control flow, which is not read; such code may be matched although it never authenticates to RelativityOne.
- **a test, mock, sample or vendored file with a neutral name, and the .min.mjs, .min.cjs, .bundle.mjs and .chunk.mjs forms of a generated name**: no name or folder says the file is one and no text shows it, so it is given its full level and may be a false positive; review such matches.
- **HTML-like comments (the opener `<!--` and the closer `-->`) in a non-module script**: these are legal code in other positions (a comparison followed by a decrement, a countdown loop), so they are not read as comments; code on such a line is read as code.
- **the Unicode line separator and paragraph separator, which the language treats as line ends**: a line ends at the line feed; the reading is damaged around such a character, and no construct fails to be read.
- **a regular expression written after a trailing line comment or after a block comment**: whether a slash opens a regular expression is decided from the text before it, and not from where comments are; the slash is read as a division and the expression as code.
- **JSX beyond the two rules for closing tags: a backtick or a block-comment opener in element text abandons the rest of the file, and a quote in element text damages only its own line**: element text is not parsed, because handling it means parsing; the file is listed as not fully read and a construct after the damage is not found.
- **a regular expression written after a closing parenthesis (if (c) /re/.test(s)) or as the first token after a closing brace**: a slash after those characters is read as a division, which keeps a self-closing JSX tag after an expression hole out of the regular-expression reading; that rare regular expression is read as code.
- **a template literal nested more than eight deep, and any unterminated literal or block comment**: the depth is bounded and an unterminated literal cannot be read past; the file is listed as not fully read and a construct after the damage is not found.
- **the upper-case folders Dist and BUILD**: the walk skips only the lower-case dist and build, so these are read and no name marks their content as generated; a generated tree under them is given its full level.
- **a .js, .mjs, .cjs or .jsx file with any line over 10,000 characters, which includes a minified single-line bundle**: this is an internal limit that keeps the cost bounded: such a file is skipped whole and counted, so the loss is visible as a number; a shorter minified file is read.
- **JavaScript embedded in other files: HTML script elements, single-file components of frameworks and templating files, and extensions other than .js, .mjs, .cjs and .jsx**: the containing file is not a JavaScript file, so its text is not read as one; search such files separately.
- **a file under a folder the repository walk skips: obj, .git, .vs, node_modules, dist, build and packages, and a bin folder with a compiled project file (*.csproj, *.vbproj or *.fsproj) beside it; a bin folder with no project file beside it is walked like any other folder, so a node script under it is read, while one under a bin beside a project file is not**: a skipped folder is excluded before any file is read, so no rule can reach the file; this is a limit of the walk and not of a form of the construct.
- **where a credential lives: .env files, JSON or YAML configuration and .npmrc, which are not read**: the credential is in another file, not in the text analyzed, and a configuration file is where a credential usually lives; search such files separately for user names and passwords. A header whose whole value is read from the process environment is matched for review: the value is held in the environment or in the file that sets it, and the environment is not read, so the match does not say what the value holds.
- **a password typed in another module, so that the file that sends it holds no sign-in evidence**: sign-in evidence is read per file; a file whose evidence is elsewhere is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in; that is an expected outcome and not a failure - where the code acts for a signed-in user, use the Authorization Code flow.
- **sign-in evidence that is not listed: a custom prompt wrapper, a credential read through a helper, and markers of a server alone**: the set of possible prompts is open and a prompt with no stable shape cannot be listed in advance; only the signals with a stable shape in the code are read, and a file whose evidence is of another kind is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in.
- **a flow misread: server markup in a plain .js or .mjs file and a locally defined prompt function beyond the reading window, which are assigned the interactive flow, and a node script whose only evidence is prompt wording, which is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in**: the reading separates a file that names itself a node script from a browser file, and a window is bounded; server markup and a prompt function beyond the window may be assigned the interactive flow although the code acts as itself. A node script whose only evidence is prompt wording is misread the other way: the wording is not read in a node script, so nothing marks it as interactive and it is given the default flow (Client Credentials unless another default is configured), with its note, because nothing in it shows that a person signs in; where such a script acts for a signed-in user, choose Authorization Code by hand.
- **a credentialed URL assembled from parts: a concatenation, a URL parsed from a string or a template with holes**: the user information is not one literal at the call, and only a literal is read.
- **a constant declaration that follows a brace or a semicolon on the same line, a literal continued over lines by a backslash, and a template with a hole**: the declaration is read one line at a time from the start of the line, which keeps the cost linear, and a name assigned from an environment read is read the same way, so a declaration of it that follows a brace or a semicolon on the same line is not counted either; a name with one counted and one uncounted declaration can be resolved to the wrong value, which is a missed or misplaced match.
- **a URL object whose scheme is not visible where its user name and password are assigned, such as one built from a parameter**: the setters are read on a name bound to a URL built from a literal; when the scheme is in a parameter a non-HTTP address cannot be told, so such an assignment is given Medium confidence.
- **a bare member name for the user name or the password in a credential object that no rule reads in that spelling (a Low auth object with no client beside it, got, jQuery; not an axios or request option, and not an object that TypeScript's typed-object and NestJS service rules read), and a Base64 encoder whose first literal ends in a colon and whose password part is code**: in these objects a member is read only when it is written with a key and a colon, whereas the axios and request option rules read a bare name too, and in TypeScript so do the typed-object and NestJS service rules; an encoder pair split between two literals is read, but a literal followed by code is not, because the second part is then hard to tell from a host and port or a cache key; review such code by hand.
- **a header value built inside the project's own function, as a template that joins a scheme held in a constant with an encoded pair**: the call that uses such a function is a header built by a function call, which is not read, and the function itself is given the Low level, so reading its template would not bring back a match at the call; review such helper functions and the code that calls them by hand.
- **a settings property used as the value of an Authorization header whose name does not contain authorization (for example auth or authHeader), although it is filled from an environment value**: the property is joined to the environment value by a name that contains authorization, which is the form it was shown to occur in; a property under another name is not read, so such a header is not matched; review headers whose value comes from a settings object by hand.
- **a settings property read by bracket access, by destructuring, by optional chaining or as object shorthand, a call written as the receiver, several members written on one line, a class field assigned from an environment value, a property filled from a call, from an alias of the environment object or from another file, a property filled from an environment value with a scheme word as the fallback, and a property filled from an environment variable whose name has none of the words auth, token, secret, credential, key, pass or basic**: one member is read per line, written with a plain key or a dot (a this receiver included), and filled directly from one of the environment reads of JS-018 by a variable whose name suggests a credential; it does not read a class field, an object shorthand, a call as the receiver, a call, an alias, another file or an environment variable named for something else, and a fallback that names a scheme is not matched by any rule; review such settings by hand.
- **a user name and a password joined by a colon and given to the project's own encoder function, in a file that never names the Basic scheme**: only the standard encoders are recognised; a function of the project's own that is not shown to encode anything, with no scheme word and no header beside it, gives the rule nothing to anchor on, so such a pair is not matched; review the project's own encoding helpers by hand.
- **a credential object, an options object or a header object chosen by a selector (an enumeration, a switch or a map) and returned from a function, with no client visible in the same statement**: one expression is read at a time and a value is not followed from where it is chosen to where it is used, so such an object is not matched even when it carries a credential; review functions that return credentials or client options by hand.
- **an encoder call separated from the Basic literal of a header value by another operand on the same line, such as a separator or a second literal between the scheme and the encoder**: the encoder is merged into the header match only where the encoder call directly follows the Basic literal; with another operand between them the header and the encoder match separately for one construct, so one construct is counted twice; read the two as one.
- **a tight comparison chain or a Flow explicit type argument, with no blank before <, followed by the credential shape of a callee class**: the generic argument group is the same characters as a comparison chain, and a regular expression cannot separate them, so such text may be matched, which is a false positive.
- **a header-object match lost on a Basic literal that carries a refused tail: a Basic literal in a value position followed by a semicolon, a single vertical bar or, in Flow text, the ending of an object type**: the tail refusal reads the characters after the literal and cannot tell a type member from a value with the same characters, so a match of an Authorization key can be missed, never a false one.
- **a file whose analysis takes too long, a call longer than the reading window of 1,500 units, a URL binding used more than thirty lines after its definition, and a quoted string longer than the window allows**: these are internal limits that keep the cost bounded: a file that trips a time limit is skipped and counted, so the loss is visible as a number, and a window that grew without bound would make the cost grow without bound.
- **which matches were left out when the cap on matches was reached, per file or in all**: the cap is stated in aggregate only, so it cannot say which matches were left out.
- **confirmation that a call goes to RelativityOne and not to another server**: a call is seen but the destination host cannot be determined; such matches are still kept, with a note that they may not apply if the destination is not RelativityOne - review each one before acting.
- **one construction split across statements: an encoder in one statement and the header that uses it in another, and the two assignments of a URL object's user name and password**: only a construct nested in another is merged, so a split construction matches twice; the encoder match is the expected one of the pair.

### What the declared list does not say

Seven classes match at Low only, `JS-007`, `JS-009`, `JS-011`, `JS-014`, `JS-016`, `JS-018` and `JS-019`: they are for manual review, so they are not in the list above. A call that is challenge-dependent (`JS-009`, `JS-014` and `JS-016`) matches so that you see it, and a match is not a statement that the call authenticates to RelativityOne.

**Limits of these statements.** The statements about what a browser sends rest on the web platform's specifications and have not been tested in a browser. The behaviour of the third-party clients rests on their documentation and may differ in other package versions, and the behaviour of the separately installed `undici` package is unconfirmed. The documentation of `axios`, `undici`, `got` and `superagent` is that of their current release at the time it was read, and that of `node-fetch` and `request` is that of an earlier release than the latest. The `fetch` built into Node.js is confirmed only on one Node.js version, on Windows. Read the findings with that in mind, and check a client's option against the version you use.
