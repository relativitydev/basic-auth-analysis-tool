# Rule Patterns for TypeScript (Part A of the Migration Documentation)

Part A describes the Basic Authentication patterns the tool looks for in TypeScript code, so that you can find them in your own source without the tool, and so that you can understand why the tool reported a finding. Each pattern class has an identifier, such as `JS-004` or `TS-001`, which is the same identifier a tool report shows, so a finding and this document can be matched.

**Twenty-one classes are registered for TypeScript: nineteen are inherited from JavaScript and two exist for TypeScript alone.** TypeScript is a superset of the syntax of JavaScript, so the tool reads TypeScript text with the JavaScript rules and keeps their identifiers: `JS-001` to `JS-019` are the classes described in [Part A for JavaScript](part-a-javascript.md), read on TypeScript files, and a finding in a `.ts` file shows the same `JS-` identifier with TypeScript as its language. Each inherited class below says what is the same and what a TypeScript form changes. The two classes of TypeScript alone are `TS-001` (a typed credential object) and `TS-003` (an injected NestJS HTTP service). The inherited classes that can produce a recommendation are `JS-001` to `JS-006`, `JS-008`, `JS-010`, `JS-012`, `JS-013`, `JS-015` and `JS-017`; seven inherited classes are always Low and always go to manual review, `JS-007`, `JS-009`, `JS-011`, `JS-014`, `JS-016`, `JS-018` and `JS-019`; `TS-001` is Medium and `TS-003` is High.

Every pattern class below has the same five parts:

- **Identifier and name.** In the heading.
- **What it looks like.** A short description and a placeholder example. A paragraph that begins **In TypeScript.** says what a TypeScript form changes for an inherited class.
- **How to search.** A pattern you can run with `rg` (ripgrep). The tool reads a header name and the word `Basic` in any letter case, so every search ignores letter case (`-i`). If you use `grep`, run `grep -rniE --include="*.ts" --include="*.tsx" --include="*.mts" --include="*.cts"` with the same expression, replacing `\s` by `[[:space:]]`, `\w` by `[[:alnum:]_]` and `\b` by `\<` or `\>` where your `grep` does not read them. In PowerShell the expression works unchanged with `Select-String`: `Get-ChildItem -Recurse -Include *.ts,*.tsx,*.mts,*.cts | Select-String -Pattern '<expression>'`, which also ignores letter case. A search reads one line at a time, so a call or an object written over several lines is found through the line that holds the word the search looks for. A search finds candidate files and never replaces reading them: open each match and check it against the description of its class.
- **Confidence.** When a match is High, Medium or Low, including the cases that go to manual review.
- **Which flow.** The signals that indicate Client Credentials or Authorization Code, and the default.

Every example uses obvious placeholders such as `userName`, `secret`, `url` and `host.example`. No example contains a real credential. To decide what to do about a pattern, go to [Part C](part-c-typescript.md); for where the new secrets go, see Secret Management (Part B of the Migration Documentation), [`part-b-secret-management.md`](part-b-secret-management.md).

The tool reads files with the extensions `.ts`, `.tsx`, `.mts` and `.cts`, and declaration files (`.d.ts`, `.d.mts` and `.d.cts`), which it reads and reports at Low; see [Declaration files](#declaration-files). The searches below use the glob `'*.{ts,tsx,mts,cts}'`, which also finds the declaration files. Files with the extensions `.js`, `.mjs`, `.cjs` and `.jsx` are JavaScript files and are described in [Part A for JavaScript](part-a-javascript.md). **Extensionless runner scripts are not read.** A script with no extension whose first line names `ts-node`, `tsx` or `deno` (`#!/usr/bin/env ts-node`, `#!/usr/bin/env tsx`, `#!/usr/bin/env -S deno run`) has no TypeScript extension, and the tool attributes a language by its extension or by a shebang that names a listed interpreter, which these third-party runners are not. Search such scripts separately; see [Declared unsupported coverage](#declared-unsupported-coverage). TypeScript embedded in other files (single-file components, HTML templates, notebooks and Markdown) is not read either.

**Two environments.** TypeScript runs in a browser and in Node.js, and the same construct can mean different things in each. Every class below says where its construct exists: in the browser, in Node.js or in both. The environment is not a setting of the tool. A file is read as one language with one set of classes, so a finding in a file that runs in a browser and one in a file that runs in Node.js are decided by the same rules; the two environments differ in the advice in Part C and in the flow signals, below.

**TypeScript has no credential type of its own.** A type annotation changes nothing about where a credential travels, and a type alone moves no credential. As in JavaScript, a credential goes through an **option of an HTTP client**, a **header**, the **user information of an address** or an **encoder**, so most classes below are written from the forms of the call. Two classes read a type: `TS-001` reads an object literal that holds an `auth` object with a literal user name and a literal password and whose type is one the tool knows by name, and `TS-003` reads an `auth` object given to an injected NestJS HTTP service. The clients the tool knows by name are `axios`, `request`, `superagent`, `got`, `node-fetch`, `undici`, jQuery and the Node.js `http` and `https` modules, with `fetch` and `XMLHttpRequest` in the browser and in Node.js, and the Angular `HttpHeaders` forms of `JS-004`. An options object held in a variable, a client behind an alias and a wrapper function are not followed; see [Declared unsupported coverage](#declared-unsupported-coverage).

**The tool does not resolve types.** A type name is a token in the text. The tool never resolves a type across files, reads `tsconfig.json`, follows a path alias or an import, or knows what a type alias expands to. It knows four type names of the `axios` package (`AxiosRequestConfig`, `AxiosBasicCredentials`, `CreateAxiosDefaults` and `InternalAxiosRequestConfig`) and no other type name. Names of Angular and NestJS interfaces that the tool does not know are left out rather than guessed.

**One call is read at a time.** A class that anchors on a call reads the arguments of that one call, up to a bounded length, with brackets and braces balanced. A string, a template literal and a regular expression are read as single units. A use of a name, a `new URL` binding or a function body is read within thirty lines of its definition.

<a id="generic-argument-lists"></a>

**A generic argument list before the parenthesis.** TypeScript writes the type arguments of a call between the callee and its parenthesis (`axios.get<Reply>(url, ...)`). The classes anchored on a callee (`JS-001`, `JS-003`, `JS-015` and `JS-016`) read that list when it follows the callee directly, with no blank before the `<`, on one line, up to three levels of angle brackets and 120 characters per level. A list that is deeper, longer, spans a line or follows a blank (`axios.get <T>(`) is not read, and the finding is missed; it is never a false one.

**A tight comparison chain is read as a generic call, in JavaScript text too.** Because a generic list is the same characters as a comparison chain such as `request<a>(`, a chain of that shape followed by the credential shape of one of those four classes is read as a call, in a JavaScript file as well as in a TypeScript file, and the tool can report it, which is a false positive. A regular expression cannot separate the two.

## Find your code

Start here if you know what your code does and not which class describes it. Each row names a common situation and the class that covers it. Open the class, read its description and its search, and check the match against your code. A situation that is not in the table may still be covered: the classes are listed in full below, and what the tool does not read is in [Declared unsupported coverage](#declared-unsupported-coverage).

| Your code does this | Class |
| --- | --- |
| Gives an HTTP client an `auth` option with a user name and a password (`axios`, `request`), with or without a generic argument list on the call | [JS-001](#js-001) |
| Writes an object typed as an `axios` request configuration (`AxiosRequestConfig`, `CreateAxiosDefaults`, `InternalAxiosRequestConfig`) that holds an `auth` object with a `username` and a `password` member of any value form (a literal, a name, a member access, a bare name), or types the `auth` value as `AxiosBasicCredentials` | [TS-001](#ts-001) |
| Passes an `auth` object with a user name and a password to an injected NestJS `HttpService` (`this.httpService.get(url, { auth: ... })`) | [TS-003](#ts-003) |
| Calls `.auth(` with a user name and a password on a request chain (`superagent`, `request`) | [JS-002](#js-002) |
| Calls `.auth(` with one argument, or with a third argument that selects automatic scheme choice | [JS-012](#js-012) |
| Puts a user name and a password in the address of a request (`https://user:password@host/...`) | [JS-003](#js-003) |
| Holds an address with a user name and a password in a variable or an object key | [JS-013](#js-013) |
| Writes an `Authorization` header whose value starts with `Basic` (`headers`, `setRequestHeader`, `setHeader`, `Headers`, `HttpHeaders`, `setHeaders`, `.set`, `.append`, a bracket assignment, or any object with an `Authorization` key) | [JS-004](#js-004) |
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
| Holds an `auth` object with a user name and a password that is not given to an HTTP client the tool knows (a database driver, a mail transport, a message broker) | [JS-011](#js-011) |
| Defines its own function that builds the Basic header or encodes the credential | [JS-007](#js-007) |

## How confidence is assigned

Every finding receives exactly one of three levels. The levels depend on what the tool can establish from the file, and never on how sure it is about the rest of your system.

| Level | What it means |
| --- | --- |
| **High** | The code almost certainly performs Basic Authentication against a live call path. The construct names the scheme and the place the credential travels: an HTTP client is given a user name and a password through its documented option, or a header whose value starts with the literal `Basic` is written, in executable code that is not test code. |
| **Medium** | The code matches a Basic Authentication shape, but the scheme, the carrier or the destination cannot be established from the file alone. A High match in test code is also Medium. |
| **Low** | The match is present in the text but unlikely to be live authenticating code: a comment, a string that is not code, a mock, sample or example file, a vendored or generated file, or a declaration file. Seven classes are also Low by their own definition. |

Six rules apply to every class:

- **A class states a base level, and levels combine by taking the lower.** A class whose construct shows both the scheme and the carrier keeps its base level. A class whose construct does not, because the scheme is held in a variable, the use of an encoder is not in the text, the type of an object is a name the tool cannot resolve, or the destination is unknown, lands one level lower. That is why several classes whose subject is certain are reported as Medium.
- **Ambiguity resolves downward.** When two levels are possible, the lower one is used, so that uncertainty goes to manual review and not to a recommendation.
- **A file's status only lowers the level.** A match is demoted when its anchor sits in a comment or in a string, or in a file the tool recognizes as test, mock, sample, vendored or generated code, or in a declaration file. **Test code is demoted to Medium and no lower:** a match that would be High in a source file is Medium in a test, it is given a recommendation, and the report marks the finding as test code. Every other kind of file is demoted to **Low**. Nothing raises a level.
- **A Low match is reported for manual review, with no recommendation and no flow.** It is not evidence that the code does not authenticate. Part C says how to judge each case.
- **Every class anchors on a code token, never on a string.** A match is read from a call, a property or key, a method name, an encoder or a declared name, and then extends into the strings to read the header name, the scheme word and the credential. A class never anchors on the text `Basic ` or on a quoted header name, so the word `Basic` in a comment, a label or a menu entry is not a finding. A key written in quotes, such as a quoted `headers`, is text to the tool and is reported at Low; see [Declared unsupported coverage](#declared-unsupported-coverage).
- **A construct that is read is a write, never a read.** A class reads a header being set, a credential being given to a client or a pair being encoded. A server, a guard, an interceptor or a test controller that reads the incoming `Authorization` header, decodes it to check a user, or matches it in a test double is inbound authentication and is not reported at High or Medium.

What marks a file for demotion in TypeScript, with the level it gives:

- **Test code, Medium at most.** Folders named `test`, `tests`, `__tests__`, `spec`, `e2e`, `fixture`, `fixtures` or `cypress`. File names that end in `.test.` or `.spec.` followed by `ts`, `tsx`, `mts` or `cts` (`login.spec.ts`). A file that calls `describe(` and, within one hundred lines, `it(` or `test(`, found in code and not in a comment or a string. The report writes `Test code: yes` under the finding, and the JSON data file sets `testCode` to `true`. The tool recognizes test frameworks through those test words and never by the name of a library. **The Angular testing module and its testing controller are not a signal:** a test that expects a request and reads its `Authorization` header is inbound, and the call that reads it is not a carrier. A test whose expected value holds an encoder (`btoa(...)`, or `Buffer.from(...)` with `.toString('base64')`) is read at `JS-005` or `JS-006`, at Medium and marked as test code, because the encoder is built and never sent.
- **Mocks, Low.** Folders named `__mocks__`, `mock`, `mocks`, `stub`, `stubs`, `fake` or `fakes`; file names that end in `.mock.`, `.stub.` or `.fake.` followed by `ts`, `tsx`, `mts` or `cts`. A mock is recognized by its folder or file name and by nothing else, so a mock with a neutral name is reported at the level its class gives it, or at Medium when it is also in a test folder.
- **Samples, Low.** Folders named `sample`, `samples`, `example`, `examples`, `demo` or `demos`. Folders only: a file name that begins with `example` is not demoted, because it would also reach a real source file.
- **Vendored code, Low.** Folders named `vendor`, `third-party`, `third_party` or `bower_components`. The tool reads these folders, it does not skip them.
- **Generated files, Low.** A comment on the first line, or on the second when the first is the `#!` line, that holds `auto-generated`, `autogenerated`, `auto generated`, `@generated` or `generated by`; or a comment within the first ten lines that holds `do not edit`, `do not modify`, `do not hand-edit`, `changes will be lost` or `changes will be overwritten`. A comment on a later line that only says `generated by` is not a marker. **TypeScript has no minified name:** a `.min.ts` is not a convention and no file name marks a bundle as generated; a file with any line over 10,000 characters is skipped whole and counted in the report. A generated API client is recognized only through its banner.
- **Declaration files, Low.** File names that end in `.d.ts`, `.d.mts` or `.d.cts`. See [Declaration files](#declaration-files).
- **Letter case does not matter** for names and folders. Folders are matched as whole names, so `contest` does not match `test`, and the separator is part of the file suffix, so `latest.ts` is not a test file.
- **Test and another kind together are Low.** A file that is test code and also a mock, a sample, vendored, generated or a declaration file, such as `mock_server.test.ts`, is Low, and the report still marks it as test code.
- **Not a signal: a folder named `Deployment`.** The folder holds the code that authenticates, so a file under it is reported at its full level. The cost is that a deployment file that only illustrates a pattern is reported as if it were live.
- **Not read at all: `node_modules`, `dist` and `build`.** The repository walk skips these folders, in lower case, and `obj`, `.git`, `.vs` and `packages`, and a `bin` folder that has a `*.csproj`, `*.vbproj` or `*.fsproj` file beside it, so no demotion is needed for them. A `bin` folder with no project file beside it is read like any other folder. `Dist` and `BUILD` in other letter cases are read, and no name marks their content as generated.
- **Test code the tool does not recognize.** A test with a neutral name, outside every test folder and with no `describe` and `it` calls, is judged as production code and keeps its level, which may be High.

**A literal credential is a separate warning.** When a class captures a credential and the password or token is written literally into the code, the tool also raises an in-code secret warning, without printing the value. The warning is independent of the level: a demoted match keeps it. A credential is literal in four cases only: a string literal in a credential position (a password argument, a `password` or `pass` member, a `.password` assignment); a template literal with no `${ }` hole that holds the pair or the password; the password in the user information of an address; and a literal token after `Basic ` in an `Authorization` header value. A password read from `process.env`, a variable, a call, or a concatenation of a literal with code is not literal, and a template literal with a hole is not literal. **A literal is a credential literal only when it is the whole value:** a literal followed by a TypeScript cast, a `satisfies` clause or a non-null mark (`password: 'x' as string`, `password: 'x' satisfies Secret`, `password: 'x'!`) is not whole, so the finding, its class and its level do not change and the warning is not raised. A placeholder is not literal either, and carries no warning: the empty string; a run of three or more of the mask characters `*`, `x`, `.`, `-`, `_` and `#`; a marker word wrapped in `<>`, `[]`, `{{ }}` or `%%`; and the words `placeholder`, `changeme`, `change-me`, `change_me`, `replace-me`, `replaceme`, `your-password`, `your_password`, `redacted`, `todo`, `fixme`, `dummy` and `not-a-real-secret`, in any letter case. The finding itself is unchanged by a placeholder.

**How the tool reads strings and comments.** It marks where each comment and each string begins and ends so that a match in text is demoted. A comment is `//` to the end of the line or `/* ... */`; a `#!` line is a comment only at the very first character of the file. A string is single-quoted, double-quoted or a template literal; a template literal may hold `${ }` holes, nested up to eight deep, and the inside of a hole is code. A regular expression literal is a unit of its own. The tool decides whether a slash opens a regular expression from the character before it, and in TypeScript a postfix non-null mark (`value!`) directly before a slash is a division; a few unusual layouts are read as the other thing, see [Declared unsupported coverage](#declared-unsupported-coverage). JSX in `.tsx` files is not modelled: a quote in element text damages only its own line, and a backtick or a block-comment opener in element text can make the tool read the rest of the file as text. When a file you expect to produce findings reports only Low ones after some point, or is reported as not fully read, look for an unterminated template literal or block comment, or a stray backtick in JSX text, above that point.

### Declaration files

<a id="declaration-files"></a>

A declaration file holds types and no runtime call. The tool recognizes it by its file name, `.d.ts`, `.d.mts` or `.d.cts`, attributes it to TypeScript by its final extension, reads it and reports every finding in it at **Low**, for manual review, with no recommendation and no flow. A declaration file in test code is still marked as test code.

**The sentence the report writes for such a file is imprecise.** The tool treats a declaration file as a generated file, and the report's sentence for a Low match in a generated file says that the file is generated. That is true of a declaration file a compiler produced, and it is **not true of a hand-written declaration of ambient types**: the level is right and the sentence is wrong, and the tool cannot tell the two apart. Read the sentence as "a declaration file" and judge the match with [Judging a Low-confidence match](part-c-typescript.md#judging-a-low-confidence-match) in Part C.

## Pattern classes

Twenty-one classes are registered for TypeScript. Fourteen of them can produce a recommendation: the twelve inherited classes that can reach High or Medium, `TS-001` and `TS-003`. Seven, `JS-007`, `JS-009`, `JS-011`, `JS-014`, `JS-016`, `JS-018` and `JS-019`, are always Low and always go to manual review.

### TS-001 · TypedBasicCredentialAnnotation

<a id="ts-001"></a>

An object literal that holds an `auth` member with a `username` and a `password` member, whatever follows the colon (a string literal, a name, a member access such as `this.clientSecret` or `dto.password`, a call or a template), and whose type is one of the `axios` request-configuration types the tool knows, or whose `auth` value is typed by the `axios` credentials type. The tool reads the `auth` key, the two member keys and the type token next to the object, and not the value, so `{ username: userName, password: this.clientSecret }` is read as well as a literal pair. A member written as a bare name with no value counts as a member too: `{ username, password }` is read, and either member may be written either way (`{ username: accountName, password }`). A class of TypeScript alone: a type alone moves no credential, and the object may be held and given to a client elsewhere, so the class is Medium. The class exists in the browser and in Node.js, because types are compile-time.

The forms of the type that are read, for the four names the tool knows (`AxiosRequestConfig`, `CreateAxiosDefaults`, `InternalAxiosRequestConfig` and, for the `auth` value, `AxiosBasicCredentials`):

- an annotation on a declaration (`const config: AxiosRequestConfig = { ... }`), on a typed parameter default and on a typed class property;
- an assertion or a `satisfies` clause after the object (`{ ... } as AxiosRequestConfig`, `{ ... } satisfies AxiosRequestConfig`), with at most 300 characters and no brace between the end of the `auth` value and the type;
- an angle-bracket assertion directly before the object (`<AxiosRequestConfig>{ ... }`), which TypeScript allows in `.ts`, `.mts` and `.cts` files and not in `.tsx`;
- a qualified name whose last segment is a known name (`Axios.AxiosRequestConfig`);
- the credentials type on the `auth` value itself (`auth: <AxiosBasicCredentials>{ ... }`, `auth: { ... } as AxiosBasicCredentials`, `auth: { ... } satisfies AxiosBasicCredentials`).

**Example.** Placeholders only:

```typescript
	const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: 'exampleSecret' } };
	const settings = { auth: { username: 'exampleUser', password: 'exampleSecret' } } satisfies AxiosRequestConfig;
	const named: AxiosRequestConfig = { baseURL: host, auth: { username: userName, password: this.clientSecret } };
	const bare: AxiosRequestConfig = { baseURL: host, auth: { username, password } };
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\b(AxiosRequestConfig|AxiosBasicCredentials|CreateAxiosDefaults|InternalAxiosRequestConfig)\b'
rg -n -i -g '*.{ts,tsx,mts,cts}' '\bauth\s*:\s*[{<]'
```

**Confidence.** **Medium.** The type ties the object to the configuration of an HTTP client, which a plain `auth` object (`JS-011`, Low) is not, but the tool does not see where the object is used: it may be held and given to a client elsewhere. The in-code secret warning is raised only when the password member is a whole string literal (or a template literal with no hole), unless a cast, a `satisfies` clause or a non-null mark follows the literal; where the password is a name, a member access, a call or a bare name, no credential is captured and no warning is raised. Where the object sits in the call of an `axios` client the tool knows, the finding is `JS-001`, at High, and this class is dropped. Not read: an uncited type, the same object under a type name the tool does not know; an `auth` object with only one of the two members; a name that is the value of another key (`{ label: username, password }`); a member named `user` or `pass` (the class reads `username` and `password` only); a name the tool does not know; a type alias, an import alias (`AxiosRequestConfig as Config`), a type declared in the same file or in another file, and a path alias; a return-type annotation on the function that returns the object; a known name followed by an explicit generic argument list in the annotation, typed-default and angle-bracket forms (it is read after `as` and `satisfies`); a literal typed by the credentials type that is not the value of an `auth` key; an object that holds a nested member, such as a `headers: { ... }` member, **before** the `auth` key; and an options object held in a variable and given to a client elsewhere. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned): in test code the class is Medium at most, in a declaration file it is Low, and a type member with no value (`auth: { username: string; password: string }` in an interface or a type alias) is not an object literal and is not read.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#ts-001-migration).

### TS-003 · NestHttpServiceAuthObject

<a id="ts-003"></a>

An `auth` object with a user name member (`username`) and a password member (`password`), with any values, in the arguments of a call on an injected NestJS HTTP service: `this.httpService.get<Reply>(url, { auth: { username: userName, password: secret } })`. A member written as a bare name with no value counts as a member (`{ username, password }`, or `{ username: accountName, password }`). The two names are the ones the credentials type of the wrapped client cites: a member named `user` or `pass` is not read by this class, and the same object is reported at Low by `JS-011` for manual review. The pair as the request body with no `auth` key (`this.httpService.post(url, { username, password })`) and an `auth` object with only one of the two members are not read. The call is on a receiver whose name contains `http`, in any letter case, followed by one of the eleven methods of the service (`request`, `get`, `delete`, `head`, `post`, `put`, `patch`, `postForm`, `putForm`, `patchForm` and `query`); a generic argument list on the method is allowed. The service wraps `axios` and sends the object it is given as the request configuration. A class of TypeScript alone. The class exists in Node.js, in a NestJS server.

**The tool reads a name and never a type.** A receiver is read because its name contains `http`, and nothing documents that name as a convention for the injected service, so the rule is a heuristic with two consequences. An injected service whose name does not contain `http` (`this.client.get(url, { auth: ... })`) is not read as this class: the `auth` object is reported at Low by `JS-011` where its shape matches. A receiver whose name contains `http` and that is **not** an `HttpService`, an Angular `HttpClient` held as `this.http` for example, is read as this class at High when its call has a documented method name and an `auth` object with `username` and `password`, which is a false positive and a declared limit; confirm the type of the receiver by hand and review each such finding before acting.

**Example.** Placeholders only:

```typescript
	const reply = await firstValueFrom(this.httpService.get<Reply>(url, { auth: { username: userName, password: secret } }));
	const second = await firstValueFrom(this.httpService.post(url, body, { auth: { username: accountName, password } }));
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\w*http\w*\.(request|get|delete|head|post|put|patch|postForm|putForm|patchForm|query)\s*[<(]'
rg -n -i -g '*.{ts,tsx,mts,cts}' '\bauth\s*:\s*\{'
```

**Confidence.** **High.** The call names the carrier (the service wraps an HTTP client whose request configuration accepts the `auth` pair) and the pair is in the call, so the construct is High in executable code that is not test code. The wrapped-client statement and the methods were checked for one package version, and the signatures of the methods other than `request` and `get` were not read. A literal password also raises the in-code secret warning, unless a cast, a `satisfies` clause or a non-null mark follows the literal. Not read: a call through the `axiosRef` getter of the service (`this.httpService.axiosRef.get(url, { auth: ... })`), which returns the underlying client and is not one of the methods; an `auth` option in the configuration of the HTTP module (`register`, or the factory of `registerAsync`), which is not a call and is reported at Low by `JS-011` where its shape matches; a method that is not on the list; and the fetch-based client that the current NestJS documentation presents as the replacement of the axios-based module, which is not encoded (an `Authorization` header written through it is still read by `JS-004`). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#ts-003-migration).

### JS-001 · ClientAuthCredentialObject

<a id="js-001"></a>

An HTTP client given an `auth` option that holds a user name and a password: `axios.get(url, { auth: { username: userName, password: secret } })`, `axios.create({ ..., auth: { ... } })`, an assignment to `axios.defaults.auth`, and the same option of `request`. The `auth` key must be a direct member of the options object of the call, not nested under another member: an `auth` under a `proxy` member is a proxy credential and is not read. `axios` takes `username` and `password`; `request` takes `user` or `username` with `pass` or `password`. A member written as a bare name, with no value, counts as a member, so `axios.get(url, { auth: { username, password } })` is read, and either member may be written either way (`{ username: userName, password }`). Not read: a name that is the value of another key (`{ label: username, password: secret }`), a name inside a nested group, an `auth` object with only one of the two members, and the same two bare names as the request body with no `auth` key (`axios.post(url, { username, password })`). The class exists in the browser and in Node.js for `axios`, and in Node.js for `request`.

**In TypeScript.** This is the class `JS-001` of JavaScript, read on TypeScript text. A generic argument list between the client and the opening parenthesis (`axios.get<Reply>(url, { auth: ... })`) is read within the bounds of [A generic argument list before the parenthesis](#generic-argument-lists); a type annotation on the result (`const response: Reply = await axios.get(...)`) and an `as` or `satisfies` after the options object change nothing. **A call through an injected client** (`this.http.get<Reply>(url, { auth: ... })` or `this.httpService.get(url, { auth: ... })`) is not read by this class, because the client it reads is `axios` or `request` by name: an injected NestJS `HttpService` is read by [TS-003](#ts-003), and any other injected receiver leaves the `auth` object to [JS-011](#js-011) at Low. An object typed as an `axios` configuration that holds an `auth` object with a user name and a password and is not given to a call is [TS-001](#ts-001). A non-null mark before a slash inside the arguments of the call is a declared limit.

**Example.** Placeholders only:

```typescript
	const response = await axios.get(url, { auth: { username: userName, password: secret } });
	const client = axios.create({ baseURL: host, auth: { username: userName, password: secret } });
	const typed = await axios.get<Reply>(url, { auth: { username: userName, password: secret } });
	const forwarded = await axios.get(url, { auth: { username, password } });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\bauth\s*[:=]'
```

**Confidence.** **High.** The documented `auth` option of the client is Basic Authentication, so the construct is High wherever it sits in executable code that is not test code. The `auth` object must sit in the call of a client the tool knows; the same shape with no such call next to it is JS-011 and is Low. When the password is a string literal, or a template literal with no hole, the match also raises the in-code secret warning. Not read: a `bearer` member of the `auth` of `request`, which is the migrated form; `sendImmediately` set to false, which is JS-016; an options object held in a variable and given to the client elsewhere, and a client behind an alias, which are in [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-001-migration).

### JS-002 · ClientAuthMethodCall

<a id="js-002"></a>

An `auth` method called with a user name and a password on a request chain: `superagent.get(url).auth(userName, secret)`, and `request`'s `auth(username, password, sendImmediately, bearer)`. The method must have two or more arguments and sit on a chain that starts at a request-making call; a `.auth(` on an object that is not a request chain, such as an authentication SDK, takes no arguments of this shape and is not read. The class exists in the browser and in Node.js for `superagent`, and in Node.js for `request`.

**In TypeScript.** The chain has no generic list, so the form reads as it does in JavaScript. A non-null mark or a cast inside the chain (`request!.auth(userName, secret)`) has not been checked on TypeScript text and may not be read.

**Example.** Placeholders only:

```typescript
	const response = await superagent.get(url).auth(userName, secret);
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\.auth\s*\('
```

**Confidence.** **High.** Two credential arguments to a method named for authentication declare both the scheme and the carrier. When the second argument is a string literal, the match also raises the in-code secret warning. Not read: a third argument that is an object with `type` set to `bearer`, and a first argument `null` with a fourth bearer argument of `request`, which are the migrated forms; a third argument `false`, which is JS-016; a one-argument call and a third argument that selects `type: 'auto'`, which are JS-012. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-002-migration).

### JS-003 · CredentialsInRequestUrl

<a id="js-003"></a>

A request-making call whose address is a string literal, or a template literal with no hole, that carries a user name and a password: `http.get("https://user:password@host/path", handler)`, and the same through `https.request`, `axios` and its verbs, `got`, the `superagent` and `request` chains, `fetch`, `new Request` and the `fetch` and `request` of `undici`. The scheme must be `http` or `https`; `postgres://`, `mongodb://`, `redis://`, `amqp://`, `sftp://` and every other scheme are not read. What the address does depends on the consumer: Node.js `http` and `https` send Basic on the first request, and several clients document reading the credentials from the address, but the built-in `fetch`, `new Request` and `undici`'s `fetch` **reject such an address with a `TypeError`** and send nothing (checked by NSerio locally on one Node.js version). What `got`, `node-fetch` and the `request` function of `undici` do with such an address has not been checked. The class exists in Node.js, where the user information is sent, and in the browser, where `fetch` throws.

**In TypeScript.** A generic argument list after the callee is read within the bounds of [A generic argument list before the parenthesis](#generic-argument-lists). A call through an injected receiver (`this.http.get('https://user:password@host.example/')`) is not read by this class; a header written in such a call is still read by [JS-004](#js-004), because a header write is not tied to a callee.

**Example.** Placeholders only:

```typescript
	const request = http.get("https://exampleUser:exampleSecret@host.example/path", handler);
	const response = await fetch("https://exampleUser:exampleSecret@host.example/path");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' 'https?://[^/@ ]+:[^/@ ]*@'
```

**Confidence.** **High.** The credential is in the source whichever consumer reads it, so the construct is High in executable code that is not test code. A literal password in the address also raises the in-code secret warning. Where the consumer is `fetch` the call cannot have authenticated, so the finding is a bug report as much as a migration item. Not read: any scheme but `http` and `https`; an address assembled from parts (a concatenation, a template with holes, a `URL` parsed from a string); an address held in a variable or a key with no call next to it, which is JS-013. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-003-migration).

### JS-004 · AuthorizationHeaderScheme

<a id="js-004"></a>

An `Authorization` header whose value starts with the literal word `Basic`, written through any of six carriers: the `headers` key of an options object; a header-setting call (`setRequestHeader(` of `XMLHttpRequest`, `setHeader(` of Node.js `http`); the `Headers` constructor; `.set(` and `.append(` with the header name as the first argument; a property assignment on a header object (`headers["Authorization"] = ...`, `headers.Authorization = ...`, `headers.common[...] = ...`); and the key `Authorization` of any object literal, which reads a header object that a function builds and hands to a client elsewhere, a template literal tagged by a function included. The header name and the scheme word may be written in any letter case. The value may be a literal token, a concatenation, an encoder call or a template with a hole. It may also be an environment read with a fallback literal that begins with `Basic` (`process.env.API_AUTH || "Basic ..."`, and the `??` form), which is a Basic header written in the code itself, used whenever the variable is absent. `Proxy-Authorization` and `X-Authorization` are not read. The class exists in the browser and in Node.js.

**In TypeScript.** The carriers are the same as in JavaScript, and the Angular forms are read at the carrier when the value starts with a `Basic` literal: `new HttpHeaders({ Authorization: 'Basic ' + encoded })`, a quoted key with a template value, `setHeaders: { Authorization: ... }`, and the `.set(` and `.append(` calls on an Angular headers object. A typed header record (`const headers: Record<string, string> = { Authorization: 'Basic ' + encoded }`) and a cast or `satisfies` after the literal are read too. **An identifier value in an object key is not read** (`new HttpHeaders({ Authorization: header })`, `setHeaders: { Authorization: header }`), by design; the same name given to `.set(` or `.append(` is read by [JS-017](#js-017). **A type member can look like a header object.** A key `Authorization` whose literal value is `'Basic'` and that sits in an interface or a type alias is the same text as a header object. The tool refuses a `Basic` literal followed by a semicolon or by a single vertical bar (`Authorization: 'Basic' | 'Bearer'`), and a template-literal type whose hole is `string` or `number` (`` `Basic ${string}` ``); it cannot refuse a member that ends in a closing brace, a comma or the end of the line (`interface Header { Authorization: 'Basic' }`), or a literal inside a `headers:` member of a type, so those are reported at High in a file that is not a declaration file, which is a false positive, and at Low in a declaration file. A header whose value is an environment read with a TypeScript tail before a fallback operator (`process.env.API_AUTH! || 'Basic ...'`) is not read.

**Example.** Placeholders only:

```typescript
	await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });
	xhr.setRequestHeader("Authorization", "Basic " + encoded);
	headers["Authorization"] = "Basic " + encoded;
	headers.set("Authorization", "Basic " + encoded);
	const angularHeaders = new HttpHeaders({ Authorization: 'Basic ' + encoded });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' 'authorization.{1,6}basic'
rg -n -i -g '*.{ts,tsx,mts,cts}' '(setRequestHeader|setHeader)\s*\('
rg -n -i -g '*.{ts,tsx,mts,cts}' '\bheaders\s*\[|\bheaders\.(common|authorization)'
rg -n -i -g '*.{ts,tsx,mts,cts}' '(\.set|\.append)\s*\(\s*.authorization'
rg -n -i -g '*.{ts,tsx,mts,cts}' '\b(HttpHeaders|setHeaders)\b'
```

**Confidence.** **High.** The scheme word is in the text and the carrier is a header write, sent as written on the first request, so the construct is High in executable code that is not test code. When the token after `Basic ` is a literal, the match also raises the in-code secret warning. The fallback literal of an environment read counts only when it is whole: `process.env.API_AUTH || 'Basic ' + suffix` is a concatenation, not a Basic header written in the code, and is not read by this class. Not read: any scheme but `Basic`; a header built by a function call, a hook or an interceptor; a `Basic` literal held in a variable with no header write in the same statement; a header object whose `Authorization` key holds an identifier, which the key carrier does not read and JS-017 excludes (a literal `Basic` value under an `Authorization` key is read wherever the object sits); and the reading of an incoming header, which is inbound authentication. A header whose value is an identifier is JS-017. A quoted carrier key, `"headers":`, is text to the tool and is reported at Low. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-004-migration).

### JS-005 · Base64UserPasswordPair

<a id="js-005"></a>

A user name and a password joined by a colon and encoded with `btoa`, the value of a Basic header: `btoa(userName + ":" + secret)`, ``btoa(`${userName}:${secret}`)``, a literal pair, or `[a, b].join(":")`. A quoted literal pair that continues into a concatenation inside the encoder call, `btoa('exampleUser:exampleSecret' + suffix)`, is read as well, and so is a pair split across two quoted literals, a literal whose last character is the colon followed by a plus and a second quoted literal, `btoa('exampleUser:' + 'exampleSecret')`, with or without more concatenation after the second literal. Not read: a literal that ends in a colon followed by code (`btoa('exampleUser:' + secret)`), and two literals with no colon between them (`btoa('exampleUser' + 'exampleSecret')`). The rule reads the construct and does not enumerate the other arguments. `btoa` is a global of the browser and of Node.js.

**In TypeScript.** The encoder and the pair are read in a typed context as they are in JavaScript (`const header: string = 'Basic ' + btoa(userName + ':' + secret)`). **A quoted literal pair followed by a concatenation inside the encoder call** (`'Basic ' + btoa('exampleUser:exampleSecret' + suffix)`) is read at Medium, in JavaScript and in TypeScript; the literal there is not the whole argument, so it raises no in-code secret warning. Pair operands that carry a non-null mark or a cast (`userName! + ':' + (secret as string)`) have not been checked and may not be read.

**Example.** Placeholders only:

```typescript
	const token = btoa(userName + ":" + secret);
	const header = "Basic " + btoa(`${userName}:${secret}`);
	const continued = "Basic " + btoa('exampleUser:exampleSecret' + suffix);
	const split = "Basic " + btoa('exampleUser:' + 'exampleSecret');
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\bbtoa\s*\('
```

**Confidence.** **Medium.** Building and encoding a pair is the shape of Basic Authentication, but the construct does not show the scheme or where the result goes, so it is Medium. To keep cache keys and `host:port` pairs out, the match needs the word `Basic` or the word `Authorization` within 300 characters on either side of the encoder, in any part of the text; an encoder with neither is not read at this class. A pair whose use is further away is read at the header, JS-004 or JS-017, if at all. Not read: an encoder whose `Basic` or `Authorization` word is further than that; `btoa` of a value that is not a colon-joined pair (`btoa(host + ":" + port)` is read as a pair and kept out only by the use requirement). A whole literal pair also raises the in-code secret warning. A quoted literal pair that continues into a concatenation inside the encoder call is read at Medium too, and it raises no warning, because the literal is not the whole argument; a pair split across two literals is read at Medium too and raises no warning either, because no one literal holds the pair. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-005-migration).

### JS-006 · NodeBufferBase64Pair

<a id="js-006"></a>

A user name and a password joined by a colon and encoded to Base64 with `Buffer`: ``Buffer.from(`${userName}:${secret}`).toString("base64")``, and the legacy `new Buffer(userName + ":" + secret).toString("base64")`. The pair is followed by `.toString(` with the argument `base64`. `Buffer` is a global of Node.js, and a browser bundle that polyfills it is read the same way.

**In TypeScript.** As [JS-005](#js-005): a typed context changes nothing, and operands that carry a non-null mark or a cast have not been checked.

**Example.** Placeholders only:

```typescript
	const header = "Basic " + Buffer.from(`${userName}:${secret}`).toString("base64");
	const legacy = "Basic " + new Buffer(userName + ":" + secret).toString("base64");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\bBuffer(\.from)?\s*\('
```

**Confidence.** **Medium.** As JS-005, the construct does not show the scheme or where the result goes. The match needs the colon-joined pair, a following `.toString(` whose argument is `base64`, and the word `Basic` or `Authorization` within 300 characters on either side. Not read: `base64url`, which is not Basic; a decode, `Buffer.from(value, "base64").toString()`, which is inbound; a pair that is not colon-joined. A whole literal pair also raises the in-code secret warning. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-006-migration).

### JS-007 · AuthenticationHelperDefinition

<a id="js-007"></a>

The definition of the code's own authentication helper: a function, declared with `function` or bound to a `const` as a function or an arrow, whose body builds a Basic header or encodes a credential from the function's own parameters (a construct of JS-004, JS-005 or JS-006 that uses them). The class reads the declaration, not the calls. The module pattern encourages exactly this centralization, so a helper is common. The class exists in the browser and in Node.js.

**In TypeScript.** A function declaration or a `const` bound to a function or an arrow is read with typed parameters and a return type. **A class method is not read as a helper:** an Angular service's method (`buildHeader(userName: string): HttpHeaders { ... }`) is not a function declaration or a `const`, so its name is not reported by this class, and the construct inside it is still reported at its own location by its own class.

**Example.** Placeholders only:

```typescript
	function buildBasicHeader(userName, secret) {
		return { Authorization: "Basic " + btoa(userName + ":" + secret) };
	}
```

**How to search.** The tool reads the body of the function, not its name, so the search below finds the helpers whose name follows the usual convention. To find the rest, run the searches of JS-004, JS-005 and JS-006 and look above each match for an enclosing function that uses its own parameters.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\w*basic\w*(auth|header|credential)\w*|\w*(auth|header|credential)\w*basic\w*'
```

**Confidence.** **Low**, always, and always for manual review. The advice for a helper is not a mechanical code change: migrate the helper once, and note that its call sites do not appear as findings, because the helper hides them. Calls to a wrapper are not followed, and the construct inside the helper is reported at its own location by its own class, so the helper and its inner construct are two findings at two places. A helper in another module is not read.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-typescript.md#js-007-judging).

### JS-008 · NodeCoreAuthString

<a id="js-008"></a>

The `auth` option of the Node.js core `http` or `https` call, a string of the form `user:password` that Node.js turns into the Basic header: ``http.request({ host: hostName, path: "/path", auth: `${userName}:${secret}` }, handler)``. The call may be `get` or `request`, including the form that takes an address and then the options. The option is read whatever its value, a literal, a template, a concatenation or an identifier, because in these calls the property is Basic by definition. The class exists in Node.js.

**In TypeScript.** The call form is read as it is in JavaScript. An options object typed by a Node.js type and **held** in a variable (`const options: https.RequestOptions = { ..., auth }`) and handed to the call elsewhere is not read: the call and the object are in different statements. This class reads a string `auth`, so [TS-001](#ts-001) does not apply to it.

**Example.** Placeholders only:

```typescript
	const req = http.request({ host: hostName, path: "/path", auth: `${userName}:${secret}` }, handler);
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\b(http|https)\.(get|request)\s*\('
rg -n -i -g '*.{ts,tsx,mts,cts}' '\bauth\s*:'
```

**Confidence.** **High.** Node.js documents the option as a `user:password` string that it turns into an `Authorization` header, and sends it on the first request, so the construct is High in executable code that is not test code. When the value is a literal pair, the part after the colon also raises the in-code secret warning; a template with a hole is not literal. Not read: an options object held in a variable and given to the call elsewhere; an `auth` string in the options of a driver that is not an `http` or `https` call. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-008-migration).

### JS-009 · XhrOpenCredentials

<a id="js-009"></a>

The `open` method of an `XMLHttpRequest` called with a user name and a password as its fourth and fifth arguments, or with an address that carries them as its second: `xhr.open("GET", url, true, userName, secret)`. `window.open(` and `document.open(` and an `open` with fewer than five arguments and no user information are not read. The class exists in the browser.

**In TypeScript.** Nothing differs from JavaScript.

**Example.** Placeholders only:

```typescript
	xhr.open("GET", url, true, userName, secret);
	xhr.open("GET", "https://exampleUser:exampleSecret@host.example/path");
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\.open\s*\((.*,){4}'
rg -n -i -g '*.{ts,tsx,mts,cts}' '\.open\s*\(.*://[^/@ ]+:[^/@ ]*@'
```

**Confidence.** **Low**, always, and always for manual review: no flow and no recommendation. As the web platform's specification for the request is written, the credentials given this way are sent only in answer to a challenge from the server, and RelativityOne does not challenge with Basic (this rests on a single RelativityOne instance observed for the earlier languages, is not documented by Relativity, and was not probed on Relativity Server), so as written the call does not send Basic to it. A server that does challenge with Basic gets it. This reading rests on the text of the specification and has not been run in a browser, so what a given browser sends is unchecked. Confirm where the call points and whether a preemptive option or a hand-built header is also used. A literal password in the fifth argument also raises the in-code secret warning.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-typescript.md#js-009-judging).

### JS-010 · UrlCredentialAssignment

<a id="js-010"></a>

A `username` or a `password` assigned on a `URL` object, which builds an address with user information: `target.username = userName; target.password = secret;` where `target` was created with `new URL(base)` within the previous thirty lines. The `URL` class exists in the browser and in Node.js. The two assignments are two findings.

**In TypeScript.** The two assignments are read when the `URL` is bound without a type annotation. **A typed binding** (`const target: URL = new URL(base)`) is not read, and the setters on it are not found.

**Example.** Placeholders only:

```typescript
	const target = new URL(base);
	target.username = userName;
	target.password = secret;
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\.(username|password)\s*='
rg -n -i -g '*.{ts,tsx,mts,cts}' 'new\s+URL\s*\('
```

**Confidence.** **Medium.** The assignments may be far apart and the address may not be used for a request in the same file, and the consumer is not seen: Node.js `http` sends the user information as Basic, the built-in `fetch` throws on it, and an `XMLHttpRequest` given the same address is read at Low by JS-009. The match needs a name that a `new URL(` expression binds within thirty lines, which keeps `user.password = value` of an ordinary application out. A literal password assignment also raises the in-code secret warning. Not read: a receiver bound further away, or built with `URL.parse` or from parts. A `URL` whose scheme is not visible where the setters are, such as one built from a parameter, is read, and a masked value assigned to a non-HTTP address there is reported at Medium. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-010-migration).

### JS-011 · NonHttpAuthObject

<a id="js-011"></a>

An `auth` object that holds a user-like member (`username`, `user` or `login`) and a password-like member (`password`, `pass` or `pwd`), with no HTTP client the tool knows in the same call: `openMailTransport({ host: mailHost, port: mailPort, auth: { user: userName, pass: secret } })`. A database driver, a mail transport and a message broker use this shape for their own login, so the construct is the shape of an HTTP client's option without being one. The class exists in the browser and in Node.js.

**In TypeScript.** The class also reads an `auth` object that [TS-001](#ts-001) and [TS-003](#ts-003) read, but the higher class wins where both read the same object, so a typed configuration or an injected NestJS `HttpService` is reported by the class that knows more. An injected service whose receiver name does not contain `http`, a configuration typed by a name the tool does not know, and an `auth` option in a NestJS module configuration are reported here, at Low, where their shape matches. So is an `auth` object that names `user` and `pass` in the call of an injected service, because [TS-003](#ts-003) reads `username` and `password` only.

**Example.** Placeholders only:

```typescript
	const transport = openMailTransport({ host: mailHost, port: mailPort, auth: { user: userName, pass: secret } });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\bauth\s*:\s*\{.*\b(user|username|login)\b.*\b(pass|password|pwd)\b'
```

**Confidence.** **Low**, always, and always for manual review: no flow and no recommendation. The credential goes to a sink that is not an HTTP client, such as remoting, mail, file transfer, a directory or a proxy; that is live code, but not Basic Authentication towards RelativityOne. The destination is not visible, and the shape is the one an HTTP client uses, so the tool does not migrate it. A reader of the report should see where such a credential object sits. Where an HTTP client the tool knows is in the same call, the match is JS-001 or JS-016 instead, and this class is dropped. A literal password-like member also raises the in-code secret warning, which a Low match keeps.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-typescript.md#js-011-judging).

### JS-012 · ClientAuthMethodSingleArgument

<a id="js-012"></a>

An `auth` method called with one argument, or with three arguments whose third is an options object with `type` set to `auto`, on a request chain: `superagent.get(url).auth(credentialPair)`. With one argument the client takes the password as empty and, with no options, its type is `basic`; with `auto` it leaves the choice of scheme to the user agent. The same call with an options argument that selects `bearer` is the migrated code. The class exists in the browser and in Node.js.

**In TypeScript.** As [JS-002](#js-002): the chain has no generic list, and a non-null mark or a cast inside it has not been checked.

**Example.** Placeholders only:

```typescript
	const response = await superagent.get(url).auth(credentialPair);
	const automatic = await superagent.get(url).auth(userName, secret, { type: "auto" });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\.auth\s*\(\s*[^,)]+\)'
rg -n -i -g '*.{ts,tsx,mts,cts}' 'type.{1,4}auto'
```

**Confidence.** **Medium.** The single-argument form defaults to Basic at the package version read, and the `auto` form does not fix the scheme in the text, and the same call with a `bearer` option is the migrated code, so the construct alone does not establish the scheme. A call that selects `bearer` is not read. The class has no literal credential position.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-012-migration).

### JS-013 · UrlUserinfoHeldLiteral

<a id="js-013"></a>

An address with a user name and a password held as a string literal, or a template literal with no hole, in a declaration or an object key, with no call in the same text to use it: `const baseUrl = "https://user:password@host/path"`, or `{ baseURL: "https://user:password@host" }`. The declaration is `const`, `let`, `var` or a bare assignment. The scheme must be `http` or `https`. The class exists in the browser and in Node.js.

**In TypeScript.** A declaration with a type annotation is read (`const baseUrl: string = 'https://exampleUser:exampleSecret@host.example/path'`). **The refusal of a proxy-named address does not reach a typed declaration:** in `const proxyUrl: string = '...'` the tool reads the word of the annotation and not the declared name, so the address is reported at Medium, which is a false positive; review a proxy address written that way by hand. An untyped declaration and a key are refused by name as in JavaScript.

**Example.** Placeholders only:

```typescript
	const baseUrl = "https://exampleUser:exampleSecret@host.example/path";
	const options = { baseURL: "https://exampleUser:exampleSecret@host.example" };
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' 'https?://[^/@ ]+:[^/@ ]*@'
```

**Confidence.** **Medium.** The literal may be a `git` remote or an unused constant, and where it is used is not in the text, so the construct is one level below High. Where a known call holds the same literal in its arguments, the match is JS-003 and this class is dropped. A literal password in the address also raises the in-code secret warning. Not read: an address assembled from parts, a literal in a configuration file, and a literal in another module. A name or a key that contains `proxy` or `proxies`, in any letter case (`proxyUrl`, `httpsProxy`, `PROXY_URL`), marks the address as a credential for a proxy and not for the service, and the match is not reported, even when the address points at the target; an address held under a neutral name is still reported, whatever it points at. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-013-migration).

### JS-014 · JqueryAjaxCredentials

<a id="js-014"></a>

A jQuery request given a user name and a password: `$.ajax({ url: apiUrl, username: userName, password: secret })`, and the same through `jQuery.ajax` and `ajaxSetup`. A jQuery request goes through `XMLHttpRequest`, so the form is the one of JS-009, and the library's documentation describes the two settings as credentials for answering an authentication challenge. The class exists in the browser. The version in which each setting was added is not known.

**In TypeScript.** Nothing differs from JavaScript; the tool knows no jQuery type name.

**Example.** Placeholders only:

```typescript
	$.ajax({ url: apiUrl, username: userName, password: secret });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\b(ajax|ajaxSetup)\s*\('
rg -n -i -g '*.{ts,tsx,mts,cts}' '\busername\b.*\bpassword\b'
```

**Confidence.** **Low**, always, and always for manual review: no flow and no recommendation. As written the call sends Basic only to a server that challenges with Basic, and RelativityOne does not challenge with Basic (this rests on a single RelativityOne instance observed for the earlier languages, is not documented by Relativity, and was not probed on Relativity Server), so as written it does not send Basic to it. Confirm where the call points and whether a preemptive option or a hand-built header is also used. A literal password also raises the in-code secret warning.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-typescript.md#js-014-judging).

### JS-015 · GotCredentialOptions

<a id="js-015"></a>

A `got` call given both `username` and `password` options: `got(url, { username: userName, password: secret })`, and the same through the verbs of `got` and `got.extend(`. The two options are documented as the credential for Basic Authentication. The class exists in Node.js.

**In TypeScript.** A generic argument list after `got` is read within the bounds of [A generic argument list before the parenthesis](#generic-argument-lists) (`got<Reply>(url, { username: userName, password: secret })`).

**Example.** Placeholders only:

```typescript
	const response = await got(url, { username: userName, password: secret });
	const typedResponse = await got<Reply>(url, { username: userName, password: secret });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' '\bgot(\.\w+)?\s*(<[^(]*>)?\s*\('
rg -n -i -g '*.{ts,tsx,mts,cts}' '\busername\s*:'
```

**Confidence.** **High.** The two options are documented as the Basic credential, so the construct is High in executable code that is not test code. The documentation was read for one package version, and earlier major versions are unchecked; whether `got` sends Basic on the first request is not stated in the page read. A literal password also raises the in-code secret warning. Not read: a header set through a hook of `got`. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-015-migration).

### JS-016 · RequestChallengeDependentAuth

<a id="js-016"></a>

A `request` call whose `auth` option, or `auth` method, turns off the immediate send: `request.get(url, { auth: { user: userName, pass: secret, sendImmediately: false } })`. With `sendImmediately` false the client sends Basic only after it receives a 401 answer. The `request` client has been deprecated since 2020, and a repository that still uses it is the case this class is for. The class exists in Node.js.

**In TypeScript.** A generic argument list after the call is read within the bounds of [A generic argument list before the parenthesis](#generic-argument-lists).

**Example.** Placeholders only:

```typescript
	request.get(url, { auth: { user: userName, pass: secret, sendImmediately: false } });
	request.get(url).auth(userName, secret, false);
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' 'sendImmediately\s*:\s*false'
rg -n -i -g '*.{ts,tsx,mts,cts}' '\.auth\s*\([^)]*,\s*false\s*[,)]'
```

**Confidence.** **Low**, always, and always for manual review: no flow and no recommendation. With the immediate send off, the call sends Basic only to a server that challenges with Basic, and RelativityOne does not challenge with Basic (this rests on a single RelativityOne instance observed for the earlier languages, is not documented by Relativity, and was not probed on Relativity Server), so as written it does not send Basic to it. Confirm where the call points and whether a preemptive option or a hand-built header is also used. A literal password also raises the in-code secret warning. JS-001 and JS-002 do not read this form, so the classes never report the same call.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-typescript.md#js-016-judging).

### JS-017 · AuthorizationHeaderValueHeldInVariable

<a id="js-017"></a>

An `Authorization` header written through one of the carriers of JS-004 (the `headers` key, a header-setting call, the `Headers` constructor, `.set(` or `.append(`, a bracket or dot assignment) whose value is an identifier, a precomputed token, where that identifier was declared once, before the use, in the same file, with a value that starts with `Basic`: `const encodedHeader = "Basic " + encodedPair;` then `{ headers: { Authorization: encodedHeader } }`. The class exists in the browser and in Node.js.

**In TypeScript.** A typed constant is counted (`const encodedHeader: string = 'Basic ' + encodedPair;`), so the variable resolves where a plain JavaScript reading would collect no name. A name copied from an environment read is counted too, with the three outcomes described below. **The value tails `this.header`, `header!` and `header as string` are not read:** the value must be a bare name, so a member access, a non-null mark or a cast leaves the name unresolved and the class silent. A declaration whose environment read carries a TypeScript tail (`process.env.API_AUTH!`, `process.env.API_AUTH as string`) is not counted either.

**Example.** Placeholders only:

```typescript
	const encodedHeader = "Basic " + encodedPair;
	await fetch(url, { headers: { Authorization: encodedHeader } });
	const typedHeader: string = 'Basic ' + encodedPair;
	await fetch(url, { headers: { Authorization: typedHeader } });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' 'authorization.{0,3}\s*:\s*[A-Za-z_$]'
rg -n -i -g '*.{ts,tsx,mts,cts}' '=\s*.basic\b'
```

**Confidence.** **Medium.** The scheme is in a variable, so the construct does not by itself show that the header is `Basic`. The tool resolves the variable against the declarations of the file: one declaration, before the use, with a literal on the right of the equals sign. A value that resolves to `Basic` confirms the finding, which stays Medium; a value that resolves to `Bearer` is the migrated code and is not reported; **a variable the tool cannot resolve is not reported either**, because an identifier-valued header is the usual form of code that has already migrated. A variable assigned from an environment value in the same file is JS-018, at Low, and one assigned from an environment value with a fallback literal that begins with `Basic` is read here, at Medium. Not read: a token built by a call, a parameter, a destructuring or a declaration in another module; a declaration that follows a brace or a semicolon on the same line; a literal continued over lines; and a template with a hole. The class has no literal credential position. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-typescript.md#js-017-migration).

### JS-018 · AuthorizationHeaderValueFromEnvironment

<a id="js-018"></a>

An `Authorization` header written through one of the carriers of JS-004 whose whole value, scheme included, is read from the process environment, so that the code never shows the word `Basic`. Five forms of the read are covered and only these: `process.env.NAME`, `process.env['NAME']`, `import.meta.env.NAME`, `Deno.env.get('NAME')` and `Bun.env.NAME`, with the bracket spelling for the three that have one. The value may be the read alone; the read with one literal fallback after `||` or `??`; a template whose only hole is the read; or an identifier that was assigned from one of these in the same file, once, before the use. A fallback literal that begins with `Basic` is not this class: it is a Basic header written in the code itself and is read as JS-004, at High. A fallback that begins with `Bearer` is not read. A match that has an explicit `Bearer` value for an `Authorization` header written within thirty lines of it, before or after, of at most 400 characters each, is not read either (the guard is a window and not the whole file), because an environment variable that holds a whole `Authorization` value is, in real code, most often a bearer token. The class exists in Node.js, in Deno, in Bun and in browser code whose bundler substitutes the read at build time.

**In TypeScript.** **The form that strict code usually writes is the one that is silent.** An environment read with a TypeScript tail (`process.env.API_AUTH!`, `process.env.API_AUTH as string`, `<string>process.env.API_AUTH`, `process.env.API_AUTH satisfies string`) is not read, directly or through a copied name, and so is a read outside the five forms (a destructuring, an alias, optional chaining, a read through a configuration service or an imported environment object); only a read without a tail, such as `process.env.API_AUTH ?? ''`, is read. A tail before a fallback operator is not read either.

**Example.** Placeholders only:

```typescript
	await fetch(url, { headers: { Authorization: process.env.API_AUTH } });
	client.defaults.headers.common['Authorization'] = import.meta.env.VITE_API_AUTH;
	const authValue = process.env.API_AUTH ?? '';
	await fetch(url, { headers: { Authorization: authValue } });
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' 'authorization.{0,3}\s*[:=,(]\s*(process\.env|import\.meta\.env|Deno\.env|Bun\.env)'
rg -n -i -g '*.{ts,tsx,mts,cts}' '=\s*(process\.env|import\.meta\.env|Deno\.env|Bun\.env)'
```

**Confidence.** **Low**, always, and always for manual review: no flow and no recommendation. The header takes its whole value from the environment, so the code does not show whether the scheme is Basic or a bearer token; a bearer token is the migrated form and a Basic value is not, so confirm what the deployed value holds and migrate it if it is Basic. The tool does not read the environment. The class has no literal credential position, and no in-code secret warning is raised for it. Not read: an environment read held through another variable, a destructuring, an alias or a call, and a read with optional chaining; a name with two declarations, or one declared after its use, or one declared in another file; a fallback that is not a whole literal and a chain of fallbacks; a template with other text or other holes; a different header (`Proxy-Authorization`, `X-Authorization`); an object literal that only holds the key, such as `const o = { Authorization: process.env.X }`, which the carriers of the class do not read; and a match with an explicit `Bearer` value written within thirty lines of it. The guard reads text and not code, so a comment or a string that spells the header name with a `Bearer` literal silences it too, a `Bearer` fallback of any environment read within the window silences it as well (the operator of a Bearer fallback must stand on the same line as its literal, so a Bearer fallback split over a line feed does not silence it), and a `Bearer` literal held in a variable first and given to the header later is not read; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned) only lowers it further.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-typescript.md#js-018-judging).

### JS-019 · AuthorizationHeaderValueFromSettingsProperty

<a id="js-019"></a>

An `Authorization` header written through one of the carriers of JS-004 whose whole value is a property of a settings object, `<receiver>.<property>`, where the property name ends with `authorization` in any letter case (`settings.serviceAuthorization`, `config.serviceAuthorization`) and the same file assigns that property name from an environment value. The receiver is a chain of one to eight names and none of them is `headers`; an environment read written directly as the value is not a receiver, it is [JS-018](#js-018). The property is joined to its environment value by its name alone, and the join reads one member per line at the start of the line: an object member (`serviceAuthorization: process.env.NAME,`) or an assignment (`settings.serviceAuthorization = process.env.NAME;`, and `this.serviceAuthorization = process.env.NAME;`), where the environment value is one of the five forms of JS-018, alone, with one literal fallback after `||` or `??`, or as the only hole of a template. The join does not depend on the order of the file: the settings object may be built after the function that uses it, which is the difference from JS-017 and JS-018. The environment variable must have a name that contains `auth`, `token`, `secret`, `credential`, `key`, `pass` or `basic`, so a host, an address, an endpoint or a port is not a credential, and a property whose name starts with `require`, `skip`, `enable`, `disable`, `is`, `has` or `use` is a flag and not a header value. The class exists in Node.js, in Deno, in Bun and in browser code whose bundler substitutes the read at build time.

**In TypeScript.** **A type annotation is not an assignment.** The property is joined by name alone, and a member that only declares a type (`serviceAuthorization: string;`, `serviceAuthorization: AuthHeader` or `serviceAuthorization: 'Basic' | 'Bearer'` in an interface or a type, with or without the semicolon, or a class property without an initializer) is not counted, so a settings object whose members are declared by an interface in the same file is read like an untyped one; one residual limit is that the last member of an object literal whose value is a bare name or a dotted path with no comma (`name: other.path`) cannot be told from a type, so it is read as a type and is not counted. A class field with an initializer, annotated or not and with or without modifiers, is counted as an assignment of the name but is never read as the one that fills it, even when it is filled from an environment read: a class field assigned from an environment value is not read. A name with two assignments of any form is not read. A typed tail is not read either: a property followed by a non-null mark or a type assertion at its use (`settings.serviceAuthorization!`, `settings.serviceAuthorization as string`), and an environment read followed by one in the member that fills the property (`process.env.NAME!`, `process.env.NAME as string`).

**Example.** Placeholders only:

```typescript
	const settings = {
		serviceAuthorization: process.env.SERVICE_AUTH_HEADER
	};
	await fetch(url, { headers: { Authorization: settings.serviceAuthorization } });
	client.defaults.headers.common['Authorization'] = config.serviceAuthorization;
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i -g '*.{ts,tsx,mts,cts}' 'authorization.{0,3}\s*[:=,(]\s*[A-Za-z_$][A-Za-z0-9_$.]*\.[A-Za-z0-9_$]*authorization\b'
rg -n -i -g '*.{ts,tsx,mts,cts}' '^\s*(this\.|[A-Za-z_$][A-Za-z0-9_$.]*\.)?[A-Za-z0-9_$]*authorization\s*[:=]\s*(process\.env|import\.meta\.env|Deno\.env|Bun\.env)'
```

**Confidence.** **Low**, always, and always for manual review: no flow and no recommendation. The header takes its whole value from a settings property that is filled from an environment value, so the code does not show whether the scheme is Basic or a bearer token; confirm what the deployed value holds and migrate it if it is Basic. The tool does not read the environment. The class has no literal credential position, and no in-code secret warning is raised for it. The property is joined by name alone, in the same file, and the tool does not know which object a property belongs to: every assignment of that name written at the start of a line counts, whether it comes from an environment value, a literal or a call, or is a class field with an initializer (which counts, but never fills the property); a member that only declares a type, such as one declared by an interface or a type, is no assignment and is not counted, and only one assignment from an environment value is enough to read the use. A name assigned twice is not read, so two objects that carry the same property name hide a true use, and so does a literal assignment of that name elsewhere. Not read: a property whose name does not end with `authorization` (`auth`, `authHeader`); bracket access, destructuring, optional chaining and an object shorthand; a call written as the receiver; several members written on one line; a class field assigned from an environment value; a property filled from a call, from an alias of the environment object or from another file; a flag name and an environment variable named for something else; a property filled from an environment value with a scheme word as the fallback, which no rule reports; a different header (`Proxy-Authorization`, `X-Authorization`); an inbound read such as `req.headers.authorization`; and a match with an explicit `Bearer` value written for an `Authorization` header within thirty lines of it, before or after, of at most 400 characters each (a `Bearer` write 31 or more lines away does not silence it). The guard reads text and not code, so a comment or a string that spells the header name with a `Bearer` literal silences it too; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned) only lowers it further.

**Which flow.** A Low match is given no flow and no recommendation. See [how Part C tells you to judge it](part-c-typescript.md#js-019-judging).

## Flow signals

The tool recommends one of two OAuth 2.0 flows for every High or Medium finding. It chooses from signals in the code, and only from the code.

- An **interactive-user signal** is evidence that the credential belongs to a person who supplies it at run time. It decides **Authorization Code**.
- A **fixed-credential signal** is evidence that the credential is fixed for the integration. It decides **Client Credentials**.
- With **no signal**, the default flow applies. The default is **Client Credentials** unless your user profile file sets another, and the recommendation carries a note asking you to confirm the choice.
- If both kinds are present, the interactive-user signal wins. A file that reads a credential from the environment and falls back to a hidden prompt when it is unset therefore receives Authorization Code, although it may well be an unattended integration. Confirm the flow for such a file by hand.

**The cross-file limit.** Signals are read in the same file as the finding, and only there. A password typed in another module, a credential read in another file, and a credential that a caller passes in are not seen. For most real files the credential reaches the finding's file from another file, so the default is the expected outcome, and the note it carries is the guide. **In an Angular application the usual split is a component that holds the form and a service that makes the call**, so the file with the call has no interactive signal and reads Client Credentials by default although a person typed the password; follow the credential back to the component. A comment never counts as a signal, and a literal credential written into the code is not evidence of either flow.

**Three kinds of application.** Browser code is user-facing by construction. A Node.js server may act for people who sign in to it, and a Node.js command-line tool may ask a person for a password at a terminal; a job or a service acts as itself. The tool does not know which kind of application a file belongs to: it knows only the signals in the file. Part C labels its variants by application type for that reason, and each variant opens with the precondition it is for. **The markers of a framework are not signals:** an Angular component or service decorator, a NestJS controller or module decorator, and the server markers such as `require(` or a `node:` import do not decide a flow, because an Angular service and a NestJS controller each act for a user or for themselves.

**What marks a Node.js file.** A file with the extension `.cts` or `.d.cts`, and a file whose first line is a shebang that names `node` or `nodejs`, whatever its extension (a `.ts` file that keeps `#!/usr/bin/env node`), is read as a Node.js file. In such a file the browser signals do not fire, so a file that merely holds a variable named `document`, or markup in a string, is not read as interactive. **Every other file is not marked:** a plain `.ts`, `.mts` or `.tsx` file is read in the same way, because an ES module runs in both environments and JSX is also server-rendered. A file that starts with a `ts-node` or `tsx` shebang is not marked either. The consequence is that **a NestJS or Express server, which is ordinarily a `.ts` file with no shebang, is read with the browser signals**: a server file that renders a page with a password field into a string, or whose validation message holds the word `password`, can be read as interactive, and a Node.js command-line file whose only evidence is the wording of a prompt is read as the default. Check the flow of such a file by hand.

**Twenty signals are registered for TypeScript, fourteen interactive-user and six fixed-credential.** Sixteen are the signals of JavaScript, twelve interactive-user and four fixed-credential, and four are added for TypeScript. Their names below are the names the tool uses.

Some signals read the name of a variable or a key. The name matches when it contains `password`, `passwd`, `secret`, `credential`, `credentials`, `creds` or `token` anywhere, in any letter case, and when it contains `pass`, `pw`, `pwd` or `apikey` only as a whole word, so `DB_PASS` and `dbPass` match while `bypass` and `compass` do not.

### Interactive-user signals

- `DomPasswordFieldRead`: a lookup with `getElementById`, `querySelector` or `querySelectorAll` whose identifier or selector names a password field, followed within 200 characters by `.value`. Browser code; not read in a Node.js file.
- `FormPasswordPropertyRead`: `.password.value` read from a form's `elements` or from a name that contains `form`, `new FormData(...).get("password")`, and `elements.namedItem("password").value`. A `document.password` with no `.value` is a record's field and is not read. Browser code; not read in a Node.js file.
- `BrowserPromptCall`: `window.prompt(`, or a bare `prompt(` with a credential word in its string argument and no declaration of `prompt` in the thirty lines before the call. A `prompt` the project or a prompt library defines is not the browser's. Browser code; not read in a Node.js file.
- `BrowserStorageRead`: `localStorage.getItem(` or `sessionStorage.getItem(` with a key that holds `password`, `passwd`, `pwd` or `credentials`. A stored token is deliberately not evidence. Browser code; not read in a Node.js file.
- `XhrFiveArgumentOpen`: an `open(` with five arguments after a dot, the form of JS-009. `window.open(` and `document.open(` are not read. Browser code; not read in a Node.js file.
- `HtmlPasswordInputMarkup`: the text `<input` followed within 200 characters by `type` and `password`, in a string, a template body or JSX text. An inline template literal of an Angular component is a string body, so this signal reads it; a separate template file is not read. Not read in a Node.js file.
- `RequestBodyPasswordForwarded`: a password member of a request body in a route handler (`req.body.password`, `request.body.password`, `ctx.request.body.password`, the bracket form, and a destructuring of `req.body`), and a `get("password")` on a `URLSearchParams` built from the request. A body member that is not a credential word is not read. Node.js server code. A NestJS handler is read by `NestBodyPasswordForwarded`.
- `NodeMutedPasswordPrompt`: an assignment to `_writeToOutput`, or a call to `setRawMode(` with a credential word within 600 characters on either side: the idiom of a prompt that hides the typed characters. `_writeToOutput` is not a documented public interface and is named from common use. Node.js command-line code.
- `PromptLibraryPasswordType`: an object key `type` whose value is the string `password`, the option the common terminal prompt libraries use for a hidden answer. No library is named. Node.js command-line code.
- `PromptWording`: the word `password` or `passphrase` in the string argument of a call to `question(` or `prompt(`. A validation message that holds the words is not a prompt. Not read in a Node.js file.
- `ReadlineInterfaceQuestion`: `question(` called on a name that a `createInterface(` call binds in the same file, within thirty lines before the call. The wording of the question is not read. Node.js command-line code.
- `MutedWritableOutput`: a `new Writable(` whose text holds, within 400 characters, a word that names a muted state (`mute` or `muted`; `mutable` does not match). Node.js command-line code.
- `AngularFormPasswordRead`: a reactive-form password read with its value read: `this.form.value.password`, `this.form.get('password')` followed within 200 characters by `.value`, `form.controls.password` followed by `.value`, and a `getRawValue()` read on a form-named receiver whose result is read for the `password` control, directly or through a local name, in a file that defines a `password` control. The receiver is a name that contains `form`, in any letter case. **A form definition with no read is not a signal** (`new FormGroup({ password: new FormControl('') })`, a typed form model, a validator set on the control with no `.value`), and neither is a message that names the field. The names of the Angular forms interfaces are not checked against documentation, so a name the tool does not list loses a flow signal and never a finding. Browser code; not read in a Node.js file.
- `NestBodyPasswordForwarded`: a request-body password read in a NestJS handler: `@Body('password')` as a parameter decorator, or a `@Body()` parameter whose `.password` is read within the handler (`dto.password`). A class that only **declares** the member (`password!: string`), a guard that checks a Basic header, `@Body('username')` and `@Query('token')` are not read. The names of the NestJS decorators are not checked against documentation. Node.js server code.

### Fixed-credential signals

- `ProcessEnvCredentialRead`: `process.env.NAME` or `process.env["NAME"]` where `NAME` carries a credential word. `process.env.NODE_ENV`, `process.env.PORT` and `process.env.HOST` are not read.
- `ConfigLibraryCredentialRead`: a read of a credential-named setting from a receiver named `config`, `conf`, `nconf`, `settings` or `cfg`: `config.get("...")` with a credential word in the first argument, or a property read such as `config.apiPassword`. The receivers are names by convention, not packages. **An injected `configService` is not in the list**; that form is read by `NestConfigServiceCredentialRead`.
- `SecretManagerClientCall`: a call to a method that reads a secret store: `getSecretValue(`, `GetSecretValueCommand`, `getSecret(`, `accessSecretVersion(` and `getPassword(`. The names are taken from common use and have not been checked against each vendor's documentation, so a store whose method has another name is not read.
- `CredentialNamedLiteralAssignment`: a declaration, an assignment or an object key whose name carries a credential word and whose value is a non-empty string literal or a template literal with no hole. A value that is one of the three `fetch` credential modes (`include`, `omit`, `same-origin`) is refused, as is a name that ends in a label word (`Label`, `Placeholder`, `Field`, `Name`, `Pattern`, `Regex`, `Length`, `Hint`, `Message` or `Error`). It does not read a declaration with a type annotation; that form is read by `TypedCredentialNamedLiteralAssignment`. This signal is its own reading and is not the in-code secret warning: a literal credential is a secret to remove, and it is evidence of a fixed credential only through this signal.
- `NestConfigServiceCredentialRead`: a read of a credential-named key from an injected configuration service: `.get(...)` or `.getOrThrow(...)`, either with an optional generic argument list (`.get<string>(...)`), on a receiver whose name contains `config`, in any letter case, with a quoted first argument that holds a credential word (`RELATIVITY_CLIENT_SECRET`, `password`). A key that names no credential (`PORT`, `database.host`) and a receiver with no `config` in its name are not read. The names of the NestJS configuration service are not checked against documentation.
- `TypedCredentialNamedLiteralAssignment`: a typed declaration, class property or typed object member whose name carries a credential word and whose value is a non-empty string literal or a template literal with no hole (`const password: string = 'x'`, `private readonly secret: string = 'x'`). The `fetch` credential modes and the label-word endings are refused as in `CredentialNamedLiteralAssignment`. An interface or type alias member with no value (`password: string;`) is not a signal. As above, a literal credential is evidence of a fixed credential only through this signal and is not evidence of an interactive kind.

**What is deliberately not a signal.** The `credentials` member of `fetch` (`include`, `omit`, `same-origin`) and `withCredentials` are cookie policies and not a credential. A prompt whose wording sits in a validation message is not a prompt. A type declaration, a form definition or a class that declares a password member without reading it is not a signal.

The names of the libraries, frameworks and methods in the signals above are signal vocabulary, not class options: a wrong name loses a flow signal, so the file reads the default, and never loses a finding. They are named from common use and have not been checked against documentation, except where a section above says otherwise.

Part C explains how to confirm the flow yourself: see [Confirming the flow](part-c-typescript.md#confirming-the-flow).

## Declared unsupported coverage

The catalog states what it does not cover, so that a missing finding is not read as a clean result. The tool prints the same list in its report. The list below is what the TypeScript analyzer declares, in the tool's own words, followed by the reason. It holds the entries of the JavaScript list that apply to TypeScript text, the entries of that list whose wording differs for TypeScript, and the entries that exist for TypeScript alone: the extensionless runner scripts, the declaration-file sentence, the postfix non-null mark and a slash, the generic argument lists, the types the tool does not resolve, the injected clients, and the flow misreads that the layout of TypeScript makes likelier.

- **a request written by hand to a socket (net, tls or raw HTTP/2) with a literal Authorization: Basic line in the text**: the transports are an open set, and anchoring on the header text alone would make every file that mentions it a candidate; search such files separately for the literal header
- **an options object, authentication object or header object held in a variable, a module or a default export and handed to a client elsewhere, and an alias of a client or of the http module**: the call and the object are in different statements and the tool reads text, not the program; the object is read only where a header carrier or an authentication object with a user and a password sits in the same text
- **a 'Basic ...' literal held in a variable with no header write in the same statement, and a header value held in an identifier that cannot be resolved**: a literal in a variable has no call to anchor on and its use is not in the statement; a name the file does not declare exactly once before its use is not resolved, and a value that resolves to another scheme is the migrated form. A header value that is an environment read (process.env, import.meta.env, Deno.env.get or Bun.env), alone, with a || or ?? literal fallback, or as the only hole of a template, is sent to manual review, and so is a name assigned from one in the same file. Left unread: an environment read held through another variable, a destructuring, an alias or a call; a name with two declarations, or one declared after its use; a fallback that is not a whole literal, and a chain of fallbacks; a template with other text or other holes; an object literal that only holds the key, such as const o = { Authorization: process.env.X }, which the carriers of the class do not read; and a header that has an explicit Bearer value written for an Authorization header within thirty lines of it, before or after, of at most 400 characters each, where a match is missed on purpose: the guard reads text and not code, so a comment or a string that spells the header name with a Bearer literal silences it too, and a Bearer literal held in a variable first and given to the header later is not read; a Bearer fallback of any environment read within the window silences it too, and the operator of a Bearer fallback must stand on the same line as its literal, so a Bearer fallback split over a line feed does not silence it
- **header-setting methods and hooks of third-party clients whose documentation was not read for them, and a header built by a function call**: an option or method name is read only after its documentation and version range were checked, so an unchecked name is left out rather than guessed
- **a credentialed URL given directly to a jQuery or other XMLHttpRequest wrapper**: such a request sends the credential only after the server challenges for it, which is not how the tool's target behaves; a URL held in a variable is read as a held URL
- **calls to a wrapper function are not followed; the construct inside the wrapper is reported where the wrapper is defined, and a literal given to an aliased callee is not read**: following a call needs the mapping of a named parameter to its argument, which an expression cannot make; the construct is reported once, at the definition, and the call sites are not
- **computed property names, such as headers[name] or an object key written as [key], where the name is not a literal**: the name is not text at the call, so neither the header nor the option can be read
- **variable use across statements: a credential assigned in one statement and sent in another is not joined**: a variable has no marker in this language and the tool does not track it; the only cross-statement reading is a header value held in a name declared once before its use in the same file
- **shell text inside an executed string, such as a curl command given to child_process**: the command is text inside a string and the tool does not read a string as a script; search such calls separately
- **a project's own wrapper beyond what one statement shows**: a wrapper's behaviour is not in the call to it and wrapper calls are not followed
- **credentials assembled across modules**: no analysis joins a definition in one file to its use in another
- **runtimes other than Node.js, such as Deno and Bun**: the runtimes are an open set; their built-in clients and shell helpers are not listed, and of their environment reads only Deno.env.get and Bun.env are read, as the value of an Authorization header, although code written on the standard fetch and XMLHttpRequest is read as any other
- **a quoted carrier key, such as 'headers' or "headers" written in quotes**: the key is read as text inside a string and a construct anchored there is routed to manual review at Low; the header name and scheme inside it are still read
- **an encoder whose Basic or Authorization word is more than 300 characters from it**: the use requirement that keeps cache keys and host and port pairs out of the findings reads 300 characters on each side; the encoder is read at the header if at all
- **headers given to undici as a flat array of alternating names and values**: the array has no key per header, so a name and its value are not read as a pair
- **a header-writing construct built and never sent, such as a dead branch or a function that is never called**: whether a line runs depends on control flow, which the tool does not read; such code may be reported although it never authenticates to RelativityOne
- **the Unicode line separator and paragraph separator, which JavaScript treats as line ends**: the tool's line ends at the line feed; the reading is damaged around such a character, and no construct fails to be read
- **a regular expression written after a trailing line comment or after a block comment**: the tool decides whether a slash opens a regular expression from the text before it, and not from where comments are; the slash is read as a division and the expression as code
- **a regular expression written after a closing parenthesis (if (c) /re/.test(s)) or as the first token after a closing brace**: a slash after those characters is read as a division, which keeps a self-closing tag after an expression hole out of the regular-expression reading; that rare regular expression is read as code
- **a template literal nested more than eight deep, and any unterminated literal or block comment**: the depth is bounded and an unterminated literal cannot be read past; the file is reported as not fully read and a construct after the damage is not found
- **the upper-case folders Dist and BUILD**: the walk skips only the lower-case dist and build, so these are read and no name marks their content as generated; a generated tree under them is reported at its full level
- **a file under a folder the repository walk skips: obj, .git, .vs, node_modules, dist, build and packages, and a bin folder with a compiled project file (*.csproj, *.vbproj or *.fsproj) beside it; a bin folder with no project file beside it is scanned like any other folder, so a node script under it is read, while one under a bin beside a project file is not**: a skipped folder is excluded before any file is read, so no rule can reach the file; this is a limit of the walk and not of a form of the construct
- **where a credential lives: .env files, JSON or YAML configuration and .npmrc, which the tool does not read**: the credential is in another file, not in the text analyzed, and a configuration file is where a credential usually lives; search such files separately for user names and passwords. A header whose whole value is read from the process environment is reported for review: the value is held in the environment or in the file that sets it, and the tool does not read the environment, so it does not say what the value holds
- **a password typed in another module, so that the file that sends it holds no sign-in evidence**: sign-in evidence is read per file; a file whose evidence is elsewhere is assigned the default flow, which is an expected outcome and not a failure - where the code acts for a signed-in user, use the Authorization Code flow
- **sign-in evidence the tool does not list: a custom prompt wrapper, a credential read through a helper, and markers of a server alone**: the set of possible prompts is open and a prompt with no stable shape cannot be listed in advance; only the signals with a stable shape in the code are read, and a file whose evidence is of another kind is assigned the default flow
- **a flow misread: server markup in a plain .js or .mjs file and a locally defined prompt function beyond the reading window, which are assigned the interactive flow, and a node script whose only evidence is prompt wording, which is assigned the default flow**: the reading separates a file that names itself a node script from a browser file, and a window is bounded; server markup and a prompt function beyond the window may be assigned the interactive flow although the code acts as itself. A node script whose only evidence is prompt wording is misread the other way: the wording is not read in a node script, so nothing marks it as interactive and it is assigned the default flow, Client Credentials, with its note; where such a script acts for a signed-in user, choose Authorization Code by hand
- **a credentialed URL assembled from parts: a concatenation, a URL parsed from a string or a template with holes**: the user information is not one literal at the call, and only a literal is read
- **a URL object whose scheme is not visible where its user name and password are assigned, such as one built from a parameter**: the setters are read on a name bound to a URL built from a literal; when the scheme is in a parameter a non-HTTP address cannot be told, so such an assignment is reported at Medium
- **a file whose analysis takes too long, a call longer than the reading window of 1,500 units, a use of a name more than thirty lines after its definition, and a quoted string longer than the window allows**: these are internal limits that keep the cost bounded: a file that trips a time limit is skipped and counted in the report, so the loss is visible as a number, and a window that grew without bound would make the cost grow without bound
- **which findings were left out when the findings cap was reached, per file or in all**: the cap is reported in aggregate only, so the report cannot say which findings were left out
- **confirmation that a call goes to RelativityOne and not to another server**: the tool sees a call and cannot determine the destination host; such findings are still reported, with a note that they may not apply if the destination is not RelativityOne - review each one before acting
- **one construction split across statements: an encoder in one statement and the header that uses it in another, and the two assignments of a URL object's user name and password**: the tool merges only a construct nested in another, so a split construction is reported as two findings; the encoder finding is the expected one of the pair
- **a .ts, .tsx, .mts or .cts file with any line over 10,000 characters, which includes a minified single-line bundle**: this is an internal limit that keeps the cost bounded: such a file is skipped whole and counted in the report, so the loss is visible as a number; a shorter file is read
- **TypeScript embedded in other files: single-file components (script lang="ts"), HTML templates, notebooks and Markdown**: the containing file is not a TypeScript file, so its text is not read as one; search such files separately
- **a test, mock, sample or vendored file with a neutral name**: no name or folder says the file is one and no text shows it, so it is reported at its full level and may be a false positive; review such findings. A TypeScript file has no minified form that is recognised by name
- **proxy credentials: the Proxy-Authorization header, the credential member of a proxy option and user information in a proxy URL**: this is Basic towards a proxy and not towards the target, which is not the migration this tool serves; such text is kept out of the findings when the name is written without a type annotation or is a key. A URL with user information that is held under a name containing proxy is read as a proxy URL and is not reported, even when it points at the target service; review such a name by hand. A declaration with a type annotation, such as const proxyUrl: string = a URL with user information, is read as a service address and reported at Medium, so review a proxy address written that way
- **generated API clients (OpenAPI generators, typed SDKs)**: the call surface is hidden behind generated methods and no list can name every one; a generated-file banner is still read as a sign that the file is generated
- **what .tsx inherits from the JSX limits: a backtick or /* in element text abandons the tail of the file; a template nested nine deep abandons it; an apostrophe is a recovered literal**: element text is not modelled; a backtick, a block-comment opener or a ninth level of template abandons the rest of the file, which is reported as not fully read, and an apostrophe damages only its own line
- **a constant declaration that follows a brace or a semicolon on the same line, a literal continued over lines by a backslash, a template with a hole, and a declared value that carries a TypeScript tail**: the declaration is read one line at a time from the start of the line, which keeps the cost linear, and a name assigned from an environment read is read the same way, so a declaration of it that follows a brace or a semicolon on the same line is not counted either; a value followed by a non-null mark, a cast, a satisfies clause or an angle-bracket assertion, a value that reads another name or a call, and an annotation that holds an equals sign are not counted either; a name with one counted and one uncounted declaration can be resolved to the wrong value, which is a missed or misplaced finding
- **an extensionless runner script whose first line names ts-node, tsx or deno (#!/usr/bin/env ts-node, #!/usr/bin/env tsx, #!/usr/bin/env -S deno run)**: the file has no TypeScript extension and the tool attributes a language by its extension or by a shebang that names a listed interpreter, which these third-party runners are not; such a script is not read, so search for it separately
- **the generated-file sentence for hand-written ambient typing in a declaration file (.d.ts, .d.mts or .d.cts)**: a declaration file is reported at Low for manual review because of its suffix, with the sentence written for generated files; the level is right but the sentence is wrong for a hand-written declaration, and the tool cannot tell the two apart
- **inbound Basic Authentication: a guard, middleware, interceptor or test controller that checks or reads a Basic header**: this is a different construct, because the tool reads what a program sends and not what it receives; such code is kept out of the findings by its shape, but a test whose expected header is written as an object literal cannot be told from a header write and is reported at Medium as test code
- **a Relativity-published TypeScript client or typings, and Relativity's extensibility surfaces (reviewapi, ConvenienceApi.RelativityHttpClient)**: no server-side TypeScript client or type package for Relativity REST was found, so raw HTTP is what the tool reads; these surfaces were documented only in part, run in the browser, and no class reads a call through either
- **a blank before the postfix non-null mark: x !/2, x ! / 2**: the tool decides from the character directly before the mark whether a slash divides, so a blank makes the mark a logical not and the slash opens a regular expression; it is recovered at the line end, and when a later slash on the same line closes it the code between the two slashes is read as a string, so a construct there may be demoted or missed with no anomaly counted
- **a bare member name for the user name or the password in a credential object that no rule reads in that spelling (a Low auth object with no client beside it, got, jQuery; not an axios or request option, and not an object that TypeScript's typed-object and NestJS service rules read), and a Base64 encoder whose first literal ends in a colon and whose password part is code**: in these objects the tool reads a member only when it is written with a key and a colon, whereas the axios and request option rules read a bare name too, and in TypeScript so do the typed-object and NestJS service rules; the tool also reads an encoder pair split between two literals but not a literal followed by code, because the second part is then hard to tell from a host and port or a cache key; review such code by hand
- **a header value built inside the project's own function, as a template that joins a scheme held in a constant with an encoded pair**: the call that uses such a function is a header built by a function call, which is not read, and the function itself is reported at the Low level, so reading its template would not bring back a finding at the call; review such helper functions and the code that calls them by hand
- **a closing brace before the postfix non-null mark: }!/2**: a closing brace is not read as the end of an operand, so the slash opens a regular expression with the same consequence as a blank before the mark
- **two postfix non-null marks in a row: x!!/2**: the second mark follows a mark and not an operand, so the slash opens a regular expression with the same consequence as a blank before the mark; this is rare in code
- **the postfix non-null mark and a slash in .tsx element text: wow!/ directly after a word is read correctly, and the damage is with a blank before the mark (wow !/</p>)**: element text is not modelled; the slash opens a regular expression that the slash of the closing tag closes one character later, so the damage is one character wide and no anomaly is counted
- **the postfix non-null mark read as a regular-expression opener inside the argument window of a call: axios.get(a!/b, { auth: ... }) // .../**: the argument window of a call reads a slash after an exclamation mark as the opening of a regular expression, so a later slash on the same line can close it over the object the class looks for; a finding is missed, never a false one
- **an instantiation expression followed by a division: id<number> / 2**: a slash after a greater-than sign is read as the opening of a regular expression, which is recovered at the line end; one line is damaged and the case is rare
- **generic nesting beyond depth 3, a level longer than 120 units, a list spanning a line feed, and a blank before the opening angle bracket, such as axios.get <T>(**: the list is read at a bounded depth and length and only when it follows the callee directly, so a callee with a longer, deeper, multi-line or spaced list is not read; a finding is missed, never a false one
- **a typed URL binding: const target: URL = new URL(base)**: the expression reads a URL binding without a type annotation, so the user name and password setters of an annotated binding are not read
- **type resolution across files, path aliases, same-file aliases and project-declared types**: a type name is a token in the text: the tool never resolves a type across files, reads tsconfig.json, follows an import or knows what an alias expands to, so an object typed by a name the tool does not know is not read as typed; a known name followed by an explicit generic argument list is read after an as or satisfies clause and not after an annotation, a typed default or an angle-bracket assertion
- **a typed credential object whose type name the tool does not know, such as one typed by a request, jQuery or Node.js typing**: only the names the tool has documentation for are read: AxiosRequestConfig, AxiosBasicCredentials, CreateAxiosDefaults and InternalAxiosRequestConfig; an object typed by another name is reported at Low where its shape matches, or not at all
- **a typed credential object whose auth key follows a nested brace in the same object**: the object before the auth key is read flat, because a bounded, linear reading cannot balance braces, so a headers member placed before auth hides the type; a finding is missed and the object is reported at Low where its shape matches
- **Angular and NestJS API names not read (HttpClient, HttpHeaders, interceptors, HttpTestingController, ConfigService, routing decorators; HttpService at any version other than @nestjs/axios 12.0.1)**: a name is read only after its documentation and version range were checked, so an unchecked name is left out rather than guessed
- **an injected HttpService whose receiver name does not contain http (this.client.get(..., { auth: ... }))**: the tool reads the name of the receiver and never its type; a call on such a receiver is missed at High and the auth object is reported at Low where its shape matches
- **an injected client as the receiver of a callee-anchored class, such as this.http.get with a credentialed URL, or a call through the service's axiosRef**: the callee lists of the callee-anchored classes name no injected receiver, so the finding of those classes is missed; header writes are not tied to a callee and are still read
- **an auth option in a module configuration, a property assignment on a request-configuration object inside an interceptor callback, and a call whose receiver and member are split by a line break**: each is a form the tool has no class for: a configuration is not a call, an assignment is not a key, and the callee lists allow no white space before the dot; a High finding is missed, and the configuration form is read at Low where its shape matches
- **the fetch-based HttpClient that replaces the axios-based module (@nestjs/http-client)**: the package is not encoded because its version and its documentation of Basic Authentication were not checked; an Authorization header written through it is still read, and an http-named client given an auth pair may be read at High although that package does not document the pair
- **an Angular HttpClient or another client injected under a name that contains http, given an auth object with a username and a password, which is read as a call on the NestJS HttpService**: the receiver is recognised by its name, because no page documents a naming convention for the injected service; such a call is reported at the High level although the client may not send Basic Authentication, so review each one before acting
- **a type-position keyed literal that cannot be told from a header value: an interface member ending in a closing brace, a comma or a line end ({ Authorization: 'Basic' }, Authorization: 'Basic', ...)**: nothing in the text of such a member differs from a header object written as a value, so it is reported at High in a file that is not a declaration file and at Low in one, which is a false positive; a member ending in a semicolon or a single vertical bar is refused
- **a type-position literal inside a headers: member (an interface member headers: { Authorization: 'Basic'; })**: the header carrier anchors on the headers key and the tail refusal does not reach it, so the member is reported at High in a file that is not a declaration file, which is a false positive, and at Low in one
- **an identifier value in an Angular Authorization object (new HttpHeaders({ Authorization: header }), setHeaders: { Authorization: header })**: an object key is read only when its value starts with a Basic literal, so an identifier value is not read there by design; the same name given to set or append is read
- **the value tails this.x, x! and x as T of a header value held in a name**: the identifier value reads a bare name and refuses a member access, a non-null mark and a cast, and only a module-level constant or a class field with a literal is counted; the name is then not resolved and nothing is reported
- **an Authorization header whose value is an environment read with a TypeScript tail, such as process.env.X!, process.env.X as string or process.env.X satisfies string, and an environment read outside the five forms**: the direct form and a name copied from the read refuse a tail, so the form strict code usually writes is the one that is silent; a read outside process.env, import.meta.env, Deno.env.get and Bun.env, such as a destructuring, an alias or a read through a configuration service, is not read either, so no finding is raised for them
- **a typed declaration whose name denotes a proxy: const proxyUrl: string = a URL with user information**: the expression reads the word of the type annotation and not the declared name, so the refusal of a name that denotes a proxy does not reach it and the address is reported at Medium, which is a false positive
- **a credential literal followed by a TypeScript tail, such as password: 'x' as string, password: 'x' satisfies Secret or password: 'x'!**: a literal is a credential literal only when it is the whole value, so with a tail the finding and its level are unchanged but the warning that the secret is written in the code is not raised
- **a property of a settings interface, type or class used as the value of an Authorization header whose name does not end with authorization (for example auth or authHeader), although it is filled from an environment value**: the tool joins the property to the environment value by a name that ends with authorization, which is the form it was shown to occur in; a property under another name is not read, so such a header is not reported; review headers whose value comes from a typed settings object by hand
- **a settings property read by bracket access, by destructuring, by optional chaining or as object shorthand, a call written as the receiver, several members written on one line, a class field assigned from an environment value, a property filled from a call, from an alias of the environment object or from another file, a property filled from an environment value with a scheme word as the fallback, and a property filled from an environment variable whose name has none of the words auth, token, secret, credential, key, pass or basic**: the tool reads one member per line, written with a plain key or a dot (a this receiver included), and filled directly from one of the five environment forms by a variable whose name suggests a credential; it does not read a class field, an object shorthand, a call as the receiver, a call, an alias, another file or an environment variable named for something else, and a fallback that names a scheme is not reported by any rule; review such settings by hand
- **a user name and a password joined by a colon and given to the project's own encoder function, typed or not, in a file that never names the Basic scheme**: the tool recognises the standard encoders only; a function of the project's own that is not shown to encode anything, with no scheme word and no header beside it, gives the tool nothing to anchor on, so such a pair is not reported, whether or not the function carries types; review the project's own encoding helpers by hand
- **a credential object, an options object or a header object chosen by a selector (a TypeScript enumeration, a switch or a map) and returned from a function, with no client visible in the same statement**: the tool reads one expression at a time and does not follow a value from where it is chosen to where it is used, so such an object is not reported even when it carries a credential; review functions that return credentials or client options by hand
- **a settings property with a typed tail (a non-null mark or a type assertion) at its use, and the environment read that fills it with such a tail**: the tool ends the value at a non-null mark or a type assertion, so such a header is not reported; review typed settings by hand
- **the flow misreads TypeScript's layout makes likelier: a .ts server in the script dialect read by the browser signals; a password typed in another module (a component holds the form and a service makes the call); .tsx and .mts carry no dialect; a ts-node or tsx shebang resolves to script**: the node dialect separates only a file that declares itself, and signals are read per file, so a server written as a plain .ts file is read as a browser file and may be assigned the interactive flow, and a form held in a component with the call in a service is assigned the default flow
- **a generic call read in JavaScript text: a tight comparison chain, or a Flow explicit type argument, with no blank before <**: the generic argument group is the same characters as a comparison chain, and a regular expression cannot separate them, so such text followed by the credential shape of a callee class may be reported, which is a false positive
- **a header-object finding lost on JavaScript text that carries a refused tail: a Basic literal in a value position followed by a semicolon, a single vertical bar or, in Flow text, the ending of an object type**: the tail refusal reads the characters after the literal and cannot tell a type member from a value with the same characters, so a finding of an Authorization key can be missed, never a false one

### What the declared list does not say

Seven classes are supported at Low only, `JS-007`, `JS-009`, `JS-011`, `JS-014`, `JS-016`, `JS-018` and `JS-019`: the tool recognizes them and routes them to manual review, so they are not in the list above. A call that is challenge-dependent (`JS-009`, `JS-014` and `JS-016`) is reported so that you see it, and it is not a statement that the call authenticates to RelativityOne. **A declaration file is read and reported at Low**, and it is not in the list above for that reason; the one imprecision it carries is the sentence the report writes for it, which the list states and [Declaration files](#declaration-files) explains.

**What was not run.** The statements about what a browser sends rest on the text of the web platform's specifications and were not run in a browser. The behaviour of the third-party clients rests on their documentation at the package version read, and is unchecked for any other version: `axios`, `undici`, `got` and `superagent` were read at their then-current release, and `node-fetch` and `request` at an earlier release than the latest. No third-party client was installed or executed. Node.js was run on one version on Windows. **No TypeScript compiler was used to decide what a form means:** the statements about type names rest on the declaration files of the `axios` package at the versions read, and the statements about Angular and NestJS names that are not listed above are not verified. Read the findings with that in mind, and check a client's option or a type name against the version you use.
