# Rule Patterns for Shell (Part A of the Migration Documentation)

[Documentation index](../README.md#documentation)

**Contents**

- [Find your code](#find-your-code)
- [How confidence is assigned](#how-confidence-is-assigned)
- [Pattern classes](#pattern-classes)
	- [SH-001 · CurlUserPassword](#sh-001--curluserpassword)
	- [SH-002 · CurlUserWithoutPassword](#sh-002--curluserwithoutpassword)
	- [SH-003 · AuthorizationHeaderBasic](#sh-003--authorizationheaderbasic)
	- [SH-004 · WgetPreemptiveCredentials](#sh-004--wgetpreemptivecredentials)
	- [SH-005 · CurlUrlUserinfo](#sh-005--curlurluserinfo)
	- [SH-006 · CurlNetrcOption](#sh-006--curlnetrcoption)
	- [SH-007 · EncodedCredentialPair](#sh-007--encodedcredentialpair)
	- [SH-008 · BasicTokenInVariable](#sh-008--basictokeninvariable)
	- [SH-009 · ArgumentArrayCredentials](#sh-009--argumentarraycredentials)
	- [SH-010 · UrlUserinfoAssignment](#sh-010--urluserinfoassignment)
	- [SH-011 · WgetChallengeDependentCredentials](#sh-011--wgetchallengedependentcredentials)
	- [SH-012 · AuthenticationHelperDefinition](#sh-012--authenticationhelperdefinition)
	- [SH-013 · HttpieAuthentication](#sh-013--httpieauthentication)
	- [SH-014 · Aria2cCredentials](#sh-014--aria2ccredentials)
	- [SH-015 · ChallengeDependentClientCredentials](#sh-015--challengedependentclientcredentials)
	- [SH-016 · WgetExecuteCredentialsPreemptive](#sh-016--wgetexecutecredentialspreemptive)
	- [SH-017 · WgetExecuteCredentialsChallengeDependent](#sh-017--wgetexecutecredentialschallengedependent)
	- [SH-018 · HandBuiltBasicRequest](#sh-018--handbuiltbasicrequest)
	- [SH-019 · CurlConfigurationWrittenByScript](#sh-019--curlconfigurationwrittenbyscript)
	- [SH-020 · WgetConfigurationWrittenByScript](#sh-020--wgetconfigurationwrittenbyscript)
	- [SH-021 · CurlViaSameFileAliasOrVariable](#sh-021--curlviasamefilealiasorvariable)
	- [SH-022 · WgetViaSameFileAliasOrVariable](#sh-022--wgetviasamefilealiasorvariable)
	- [SH-023 · CurlAnyauthWithUser](#sh-023--curlanyauthwithuser)
	- [SH-024 · EncodedPairHeldInVariable](#sh-024--encodedpairheldinvariable)
	- [SH-025 · OptionsHeldInStringVariable](#sh-025--optionsheldinstringvariable)
- [Flow signals](#flow-signals)
	- [Interactive-user signals](#interactive-user-signals)
	- [Fixed-credential signals](#fixed-credential-signals)
	- [Choosing the flow by hand](#choosing-the-flow-by-hand)
- [Declared unsupported coverage](#declared-unsupported-coverage)
	- [Shell run from a string](#shell-run-from-a-string)

Part A describes how to find the Basic Authentication patterns in Shell scripts, so that you can find them in your own code. Each pattern class has an identifier, such as `SH-003`, so a match can be named and looked up in this document.

This is Part A of the Migration Documentation, for developers who need to find Basic Authentication in their own Shell scripts. You use it without running anything: it lists every pattern class, how to search for it by hand, its confidence level and which flow it needs. When you have found a match and its class, go to [Part C for Shell](part-c-shell.md) to change it, and to [Part B](part-b-secret-management.md) for where the new secrets go. New here? Start with the [Migration Documentation overview](README.md).

Every pattern class below has the same five parts:

- **Identifier and name.** In the heading.
- **What it looks like.** A short description and a placeholder example.
- **How to search.** A pattern you can run with `rg` (ripgrep). The name of a client and the words of an `Authorization` header can be written in any letter case, so every search ignores letter case (`-i`). If you use `grep` or `git grep`, run `grep -rniE --include="*.sh"` (or `git grep -niE`) with the same expression, replacing `\s` by `[[:space:]]`, `\w` by `[[:alnum:]_]` and `\W` by `[^[:alnum:]_]`; inside a bracket expression write `[:alnum:]_` for `\w` without the outer brackets. In PowerShell the expression works unchanged with `Select-String`: `Get-ChildItem -Recurse -Include *.sh | Select-String -Pattern '<expression>'`, which also ignores letter case. A search reads one line at a time. A command continued over several lines with a trailing backslash is found through the line that holds the word the search looks for only when the expression is one word; an expression of two words, such as `curl.*-u`, finds nothing when the client name and the option are on different lines. For those classes the search section adds an expression for the continuation line, which looks for the option (for an address, the address) alone at the start of a line. `rg` skips files that your `.gitignore` excludes and hidden files and folders; to search them too, add `--hidden --no-ignore --glob '!.git'`.
- **Confidence.** When a match is High, Medium or Low, including the cases that go to manual review.
- **Which flow.** The signals that indicate Client Credentials or Authorization Code, and how to choose between them when no signal is found.

Some descriptions give the exact boundary of the rule (a distance in characters or lines). Read each candidate against the description, including its distance limit.

Every example uses obvious placeholders such as `${API_USER}`, `${API_PASSWORD}` and `${HOST}`. No example contains a real credential. To decide what to do about a pattern, go to [Part C](part-c-shell.md); for where the new secrets go, see Secret Management (Part B of the Migration Documentation), [`part-b-secret-management.md`](part-b-secret-management.md).

Shell code lives in files with the extensions `.sh`, `.bash`, `.zsh` and `.ksh`, and in files with no extension whose first line names `bash`, `sh`, `zsh` or `ksh` in lower case, directly or after `env` and any options written as one word. The searches below use `--glob '*.sh'`; add `--glob '*.bash'`, `--glob '*.zsh'` and `--glob '*.ksh'`, or leave the glob out and search the whole folder, to include the other extensions and the scripts that have none. Scripts for `fish`, `csh`, `tcsh` and Windows batch files, shell commands written inside other files, and shell start-up files are not covered; see [Declared unsupported coverage](#declared-unsupported-coverage).

**The dialects.** A shell script is written for one of four dialects, which the classes cover: `bash`, `zsh`, `ksh93` and `mksh`. A first line that names `sh`, the POSIX shell with `dash` as the usual interpreter, is read as `bash`, and one that names `ksh` is read as `ksh93`. The patterns are read as written whatever the dialect, so a form that a dialect does not run still matches where it is written, and each class below says which dialects run its form. Arrays, for example, exist in `bash`, `zsh` and `ksh` and not in POSIX `sh`.

**A shell script has no types.** Other languages send a credential through a type or a library call. A shell script sends it by calling an HTTP client with arguments, so the classes below are written from the forms of the call: which client, which option, and how the value is written. The clients covered are `curl` and `wget`, which have the full set of forms, and HTTPie (`http` and `https`), `aria2c` and `lwp-request`, each with a class of its own. A request written by hand to a socket, a client behind a same-file alias or command variable, and a configuration the script writes for a client are covered by classes of their own.

**One command is read at a time.** A client class reads one simple command: the words from the client name up to the next unquoted `;`, `&&`, `||`, `|`, `&` or line end that is not escaped with a backslash. A quoted string is one unit, so an option word inside a string is not an option. The command may be continued over several lines with a trailing backslash. Several classes that match inside one command give one match, the most specific one.

## Find your code

Start here if you know what your code does and not which class describes it. Each row names a common situation and the class that covers it. Open the class, read its description and its search, and check the match against your code. A situation that is not in the table may still be covered: the classes are listed in full below, and what the rules do not cover is in [Declared unsupported coverage](#declared-unsupported-coverage).

| Your code does this | Class |
| --- | --- |
| Calls `curl` with `-u` or `--user` and a user name and a password | [SH-001](#sh-001) |
| Calls `curl` with `-u` and a user name only, so that `curl` asks for the password | [SH-002](#sh-002) |
| Writes the header `Authorization: Basic ...` in a `curl` or `wget` call | [SH-003](#sh-003) |
| Calls `wget` with a user name, a password and `--auth-no-challenge` | [SH-004](#sh-004) |
| Calls `wget` with a user name and a password and without `--auth-no-challenge` | [SH-011](#sh-011) |
| Gives `curl` an address that carries a user name and a password | [SH-005](#sh-005) |
| Calls `curl` with `--netrc`, `--netrc-file` or `-n` | [SH-006](#sh-006) |
| Joins a user name and a password with a colon and encodes the result to Base64 | [SH-007](#sh-007), [SH-024](#sh-024) |
| Holds the word `Basic`, a token or a whole `Authorization` header in a variable that a later call uses | [SH-008](#sh-008) |
| Holds the options of a call, a credential included, in an array | [SH-009](#sh-009) |
| Holds the options of a call, a credential included, in a string variable | [SH-025](#sh-025) |
| Holds an address with a user name and a password in a variable that a later call uses | [SH-010](#sh-010) |
| Defines its own function that builds the Basic header or the credential | [SH-012](#sh-012) |
| Calls HTTPie (`http` or `https`) with `-a` or `--auth` | [SH-013](#sh-013) |
| Calls `aria2c` with a user name and a password | [SH-014](#sh-014) |
| Calls `lwp-request` with `-C`, a literal Basic `-H` header or an address with userinfo, or `aria2c` with its challenge switch | [SH-015](#sh-015) |
| Gives `wget` a user name and a password with `-e` or `--execute` | [SH-016](#sh-016), [SH-017](#sh-017) |
| Writes the request and the `Authorization` header to a socket by hand | [SH-018](#sh-018) |
| Writes a configuration file for `curl` with the credential in it, and calls `curl` with `-K` | [SH-019](#sh-019) |
| Writes a configuration file for `wget` with the credential in it | [SH-020](#sh-020) |
| Names `curl` or `wget` in a variable or an alias and calls it through the name | [SH-021](#sh-021), [SH-022](#sh-022) |
| Calls `curl` with `--anyauth` and a user name and a password | [SH-023](#sh-023) |

## How confidence is assigned

A match has exactly one of three levels. The levels depend on what can be established from the file, and never on what the rest of your system does.

| Level | What it means |
| --- | --- |
| **High** | The code almost certainly performs Basic Authentication against a live call path. A client that sends Basic on its first request is called with a credential, or a literal `Authorization: Basic` header is given to it, in executable code that is not test code. |
| **Medium** | The code matches a Basic Authentication shape, but the credential source or the call path cannot be established from the file alone. A High match in test code is also Medium. |
| **Low** | The match is present in the text but unlikely to be live authenticating code: a comment, a string that is not a command, a mock, sample or example script, or a vendored or generated file. Seven classes are also Low by their own definition. |

Six rules apply to every class:

- **A class states a base level, and levels combine by taking the lower.** A class whose construct shows both the client and the credential keeps its base level. A class whose construct does not, because the client is named in another statement or the credential is held in a variable or a file, lands at Medium at most: the level is capped at Medium, and an unknown destination does not change it. That is why several classes whose subject is certain are Medium.
- **Ambiguity resolves downward.** When two levels are possible, the lower one is used, so that uncertainty goes to manual review.
- **A file's status only lowers the level.** A match is demoted when it sits in a comment or in a string that is not a command, or in a file of test, mock, sample, vendored or generated code. **Test code is demoted to Medium and no lower:** a match that would be High in a script is Medium in a test. Every other kind of file is demoted to **Low**. Nothing raises a level.
- **A Low match is for manual review, and no flow is decided for it.** It is not evidence that the code does not authenticate. Part C says how to judge each case.
- **A value that the shell expands is never a literal credential.** Wherever a class reads a password or a token from the call, the value is read as a literal only when it holds no `$`, no backtick and no backslash, and no brace, glob character or leading `~`: a variable, a command substitution or a glob is code that runs when the script does, and nothing guesses what it holds. Such a match keeps its level, but it is never an in-code secret.
- **A construct that is built and never sent does not match.** A class that builds a credential in a variable, an array or a pair matches it only when a call to a known client uses the variable within the next thirty lines of at most 400 characters; an encoder whose output is printed, compared or discarded is not read. A *wrapper*, a function in the same file that itself calls a client with its own parameters, is matched at the call that passes a literal credential to it; a call that passes variables gives no match, and a call from one wrapper into another is not followed. A *helper*, a function that only returns a value such as a header, is not a wrapper: its callers do not match (see [SH-012](#sh-012)). See [Declared unsupported coverage](#declared-unsupported-coverage) for what stays unread.

What marks a file for demotion in Shell, with the level it gives:

- **Test code, Medium at most.** Folders named `test`, `tests`, `spec`, `fixture` or `fixtures`. File names that end in `_test`, `-test`, `.test` or `_spec` before the extension, or begin with `test_` or `test-`, for the extensions `.sh`, `.bash`, `.zsh` and `.ksh`. A script that sources a shell unit-test library (`. FILE` or `source FILE` where the path names `shunit2`, `bashunit`, `assert.sh`, `bats-assert`, `bats-support`, `shellspec_helper` or `roundup`), and a script with a `Describe` block followed by an `It` block in the style of ShellSpec, found in code and not in a comment or a string.
- **Mocks, Low.** Folders named `mock`, `mocks`, `stub`, `stubs`, `fake` or `fakes`; file names that begin with `mock_`, `fake_` or `stub_` or end in `_mock`, `_fake` or `_stub` before the extension. A mock is recognized by its folder or file name and by nothing else, so a mock with a neutral name keeps the level its class gives it, or is Medium when it is also in a test folder.
- **Samples, Low.** Folders named `sample`, `samples`, `example` or `examples`, and file names that begin with `sample` or `example`. **The cost:** the prefix also reaches a real script whose name starts with the word, such as `sampler.sh`, and that match is Low, for manual review, instead of being lost.
- **Vendored code, Low.** Folders named `vendor`, `third-party` or `third_party`. A vendored folder is read: a match in it is Low.
- **Generated files, Low.** A comment on the first line, or on the second when the first is the `#!` line, that holds `auto-generated`, `auto generated`, `autogenerated` or `generated by`, or a comment within the first ten lines that holds `do not edit`, `do not modify`, `do not change`, `do not hand-edit`, `changes will be lost` or `changes will be overwritten`. A comment on a later line that only says `generated by` is not a marker.
- **Letter case does not matter** for names and folders. Folders are matched as whole names, so `contest` does not match `test`, and the separator is part of the file suffix, so `latest.sh` is not a test file.
- **Test and another kind together are Low.** A file that is test code and also a mock, a sample, vendored or generated, such as `mock_server_test.sh`, is Low.
- **Not a signal: a folder named `Deployment`.** The folder holds deployment scripts, which are what this search is for, so a script under it keeps its full level. The cost is that a deployment script that only illustrates a pattern is judged as if it were live.
- **Test code that no rule identifies.** A test script with a neutral name, outside every test folder and holding no listed test library, is judged as production code and keeps its level, which may be High.

**A literal secret is a separate fact.** When a class captures a credential and the password or token is written literally into the script, the match also holds an in-code secret. That is independent of the level: a demoted match is still one. The literal forms are the password half of a `user:password` value given to a client's user option, the password in the userinfo of an address, the token of a literal `Authorization: Basic` header, a literal password option of `wget` or `aria2c`, a literal pair inside an encoding pipeline, a literal Basic token assigned to a variable, and a literal credential in a configuration the script writes. A value read with `read` is not a literal.

**How strings and comments are told from code.** Where each comment and each string begins and ends decides whether a match is in text, and so demoted. The strings of a shell are single-quoted, double-quoted and `$'...'` strings, and the here-documents introduced by `<<` and `<<-`. A `#` starts a comment only where a word starts, so `${value#prefix}` is not a comment. Inside a double-quoted string a `$( )` substitution is read as code; the commands inside backticks and the word of a `${name op word}` expansion are read as code too, while `$name` and a plain `${name}` are read as part of the string, which is why the classes that need them look for a `$` as text and do not resolve what it holds. A here-document body is a string, except that a body the shell runs as a command is read as code (see [Shell run from a string](#shell-run-from-a-string)). A command substitution written with backticks on one line is read as code (inside a double-quoted string the commands inside backticks are read as code, as above; a string the shell itself runs is read as code in full, see [Shell run from a string](#shell-run-from-a-string)); a here-document opened inside it has no body, as in the shell, and the lines after it are read as code. The same holds for a here-document opened inside a `$( )` that stands in the body of an unquoted here-document: it takes no body from the lines after the outer terminator, and those lines are read as code. A quote that is never closed, or a here-document that the script never closes, makes the rest of the file text. Every match after it is Low. If a file you expect to hold High or Medium matches holds only Low ones after some point, look for an unbalanced quote or an unterminated here-document above that point.

## Pattern classes

Shell has twenty-five pattern classes. Eighteen of them can be High or Medium. Seven, `SH-011`, `SH-012`, `SH-015`, `SH-017`, `SH-020`, `SH-022` and `SH-023`, are always Low and always go to manual review.

### SH-001 · CurlUserPassword

<a id="sh-001"></a>

A `curl` call whose user option, `-u` or `--user`, carries a user name and a password. `curl` sends Basic on its first request by default when it is given a user, so the option alone is enough. The option may stand alone, last in a cluster of flag letters (`-sSu`), attached to its value (`-uuser:password`) or separated from it, and the long option may take its value after `=`. The value is read in every shape that holds a colon or an expansion: a bare word, a quoted string, quoted and bare parts joined, a variable, a pair of variables, a command substitution, a glob. The option may stand anywhere in the command after `curl`, and `curl.exe`, a path (`/usr/bin/curl`), `sudo`, `time`, `env`, `command` and `xargs` before it are read. `--basic` changes nothing. Runs in `bash`, `sh`, `zsh` and `ksh`; the option belongs to `curl`, not to the shell.

**Example.** Placeholders only:

```sh
	curl --silent --show-error -u "${API_USER}:${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
	curl -sSu user:password --basic "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'curl.*(\s-[A-Za-z]*u|\s--user)'
rg -n -i --glob '*.sh' '^\s*(-[A-Za-z]*u|--user)(\s|=|$)'
```

The second expression finds the option on the continuation line of a command that is split after the client name.

**Confidence.** **High.** `curl`'s default for a user is Basic and it is sent on the first request, even to a server that challenges only with a bearer token, so the construct is High wherever it sits in executable code that is not test code. A value that holds an expansion, such as `-u "$CREDS"`, is also High, because `curl` sends Basic whatever the variable holds, but it carries no literal credential and is not an in-code secret. Not matched, for the methods of SH-001: a command that holds `--digest`, `--ntlm`, `--ntlm-wb`, `--negotiate` or `--aws-sigv4`, none of which sends Basic on the first request. Also not matched: a URL with a scheme that is not HTTP (`ftp://`, `sftp://`, `smtp://`, `imap://`, `ldap://` and the other protocols `curl` speaks), written in the command or held in a same-file variable assigned just before; `-U` and `--proxy-user`, which are proxy credentials; and `--user-agent`. `--anyauth` with a user is SH-023. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned). When the password is written literally, the match also holds an in-code secret.

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-001-migration).

### SH-002 · CurlUserWithoutPassword

<a id="sh-002"></a>

A `curl` call whose user option carries a user name and no password: `curl -u user`. `curl` then asks for the password on the terminal and sends Basic with what the person types. The value must be a literal word with no colon, no `$`, no backtick and no backslash; a value written as a variable (`-u "$CREDS"`) shows no colon but is not a prompt, and is SH-001. The class is also an interactive-user signal for the flow (see [Flow signals](#flow-signals)). Runs in all four dialects.

**Example.** Placeholders only:

```sh
	curl -u user "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'curl.*(\s-[A-Za-z]*u|\s--user)'
rg -n -i --glob '*.sh' '^\s*(-[A-Za-z]*u|--user)(\s|=|$)'
```

The last expression finds the option on the continuation line of a command that is split after the client name.

**Confidence.** **High.** `curl` prompts for the password and then sends Basic, so the construct is High in executable code that is not test code. The class sets no literal credential, because the value holds no password. The same exclusions as SH-001 apply: one of the methods listed under SH-001 in the command, a URL that is not HTTP, and a proxy option. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. A `curl -u user` is itself an interactive-user signal, so it is normally assigned Authorization Code. See [the migration entries in Part C](part-c-shell.md#sh-002-migration).

### SH-003 · AuthorizationHeaderBasic

<a id="sh-003"></a>

An `Authorization` header with the scheme `Basic` given to `curl` or `wget` as a header option: `curl -H "Authorization: Basic ..."`, or `wget --header="Authorization: Basic ..."`. The header name and the word `Basic` may be written in any letter case, with a blank or none after the colon. The value may be a literal token, a variable, or a command substitution that builds the token (`$(printf ... | base64)`). The option is `-H`, `--header` or `--expand-header` for `curl`, attached, separate or with `=`, and `--header` for `wget` (`wget -H` is a different option). The header may be one of several. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	curl -sS -H "Authorization: Basic ${ENCODED}" "${HOST}/..."
	wget --header="Authorization: Basic ${ENCODED}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'authorization:\s*basic'
rg -n -i --glob '*.sh' '(curl|wget).*(\s-H|--header|--expand-header)'
rg -n -i --glob '*.sh' '^\s*(-H|--header|--expand-header)(\s|=|$)'
```

The last expression finds the option on the continuation line of a command that is split after the client name.

**Confidence.** **High.** The scheme word is a literal and the header is sent as written on the first request, so the construct is High in executable code that is not test code. When the token is a literal run of Base64 characters, the match also holds an in-code secret; a token held in a variable or built by a command carries no literal. Not read: any scheme other than `Basic`, `Proxy-Authorization`, and a header written to a file and given as `-H @FILE`; see [Declared unsupported coverage](#declared-unsupported-coverage). A header held in a variable is SH-008, and a header given to HTTPie or `aria2c` is SH-013 or SH-014. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-003-migration).

### SH-004 · WgetPreemptiveCredentials

<a id="sh-004"></a>

A `wget` call that gives a user name and a password and tells `wget` to send them without waiting to be asked: `--auth-no-challenge`. Without that switch `wget` sends nothing until the server answers with a Basic challenge; with it, `wget` sends Basic on the first request. The credential is the option pair `--user` and `--password` (or `--http-user` and `--http-password`, or the alias `--http-passwd`), separate or with `=`, or the userinfo of an address given to `wget`, in either order with the switch. The switch may also be written as a `wget` setting given with `-e`, as `-e auth_no_challenge=on`. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	wget --auth-no-challenge --user="${API_USER}" --password="${API_PASSWORD}" "${HOST}/..."
	wget --auth-no-challenge "https://${API_USER}:${API_PASSWORD}@host.example/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'wget.*--auth-no-challenge'
rg -n -i --glob '*.sh' 'wget.*--(http-)?passw(or)?d'
rg -n -i --glob '*.sh' '^\s*--(auth-no-challenge|(http-)?passw(or)?d)(\s|=|$)'
```

The last expression finds the option on the continuation line of a command that is split after the client name.

**Confidence.** **High.** The switch makes `wget` send Basic on the first request, so the construct is High in executable code that is not test code. With a literal password or a literal address userinfo, the match also holds an in-code secret. The same call without the switch is SH-011 and is Low. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-004-migration).

### SH-005 · CurlUrlUserinfo

<a id="sh-005"></a>

A `curl` call whose address carries a user name and a password: `curl https://user:password@host/...`. `curl` sends Basic from the address on the first request, even to a server that challenges only with a bearer token. The address may be written bare or in quotes, and the user and the password may be expansions. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	curl -sS "https://${API_USER}:${API_PASSWORD}@host.example/..."
	curl https://user:password@host.example/...
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'curl.*://[^/@ ]+:[^/@ ]*@'
rg -n -i --glob '*.sh' '^\s*"?[a-z][a-z0-9+.-]*://[^/@ ]+:[^/@ ]*@'
```

The last expression finds the address on the continuation line of a command that is split after the client name.

**Confidence.** **High.** `curl` sends Basic from the address on the first request, so the construct is High in executable code that is not test code. With a literal password in the address, the match also holds an in-code secret; an address written as a bare word with a brace or a glob character in it is read without a literal credential, because the shell expands it. Not matched: a command that holds one of the methods listed under SH-001, and any command that is not a known client, such as `git clone https://user:password@host/repo`: such an address is a real exposure, but it is not a Basic header sent by a client these classes cover. An address held in a variable that a later `curl` uses is SH-010. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-005-migration).

### SH-006 · CurlNetrcOption

<a id="sh-006"></a>

A `curl` call that takes its credential from the `.netrc` file: `-n`, `--netrc`, `--netrc-file` or `--netrc-optional`. The credential is in a file that no class reads, and the option may find no entry, so the match carries no credential of its own. `-n` may stand alone or last in a cluster (`-sn`). The class is also a fixed-credential signal for the flow. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	curl --netrc "${HOST}/..."
	curl -sSn --netrc-file "${NETRC_FILE}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'curl.*(\s-[A-Za-z]*n|--netrc)'
rg -n -i --glob '*.sh' '^\s*(-[A-Za-z]*n|--netrc)(\s|=|$)'
```

**Confidence.** **Medium.** The credential lives in another file and the option may find nothing, so the construct is Medium and not High. The class declares no literal credential. A `.netrc` that `wget` reads by default, with no option, and the contents of any `.netrc`, are not read; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-006-migration).

### SH-007 · EncodedCredentialPair

<a id="sh-007"></a>

A user name and a password joined by a colon and encoded to Base64, the value of a Basic header: `printf '%s:%s' "${API_USER}" "${API_PASSWORD}" | base64`. The producer is `printf` or `echo`, with or without `-n` or `-e`, and the pair may be written with variables, as literals or as a format. The output goes to the encoder directly or through up to two filter stages (`tr`, `cut`, `head`, `sed`, `awk`, `fold`, `tee`); the pair may also be given to the encoder as a here-string (`base64 <<< "${API_USER}:${API_PASSWORD}"`), or built by a `python` or `perl` one-liner that holds an encoder name. The encoders are `base64`, `gbase64`, `basenc --base64`, `busybox base64`, `openssl base64`, `openssl enc -base64` and `-a`, and `uuencode -m`. The here-string form (`<<<`) is supported by `bash`, `zsh`, `ksh93` and `mksh` (and by `sh`, read as `bash`) and not by POSIX `sh`; `base64` options differ by system (`-w0` is GNU, `openssl base64 -A` is portable).

**Example.** Placeholders only:

```sh
	ENCODED=$(printf '%s:%s' "${API_USER}" "${API_PASSWORD}" | base64 -w0)
	curl -sS -H "Authorization: Basic ${ENCODED}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'base64|uuencode|openssl\s+enc\s(.*\s)?-a([^[:alnum:]_-]|$)'
```

**Confidence.** **Medium.** Building and encoding a pair is the shape of Basic Authentication, but the construct does not show where the result goes, so it is Medium. The encoder's output must reach a known client: the pipeline stands inside the client's command as a substitution, or its output is assigned to a variable that a client uses within thirty lines; an encoder whose output is printed, compared or discarded is not read. Not read: a decode flag (`-d`, `-D`, `--decode`), and a pair that is a host and a port (a right half of one to five digits, or a name that holds `port`, or a left half named `host`, `server` or `address`). With a literal pair, the match also holds an in-code secret. A pair that is first held in a variable is SH-024. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-007-migration).

### SH-008 · BasicTokenInVariable

<a id="sh-008"></a>

A variable that holds the word `Basic`, a Basic token or a whole `Authorization` header, used by a later header option. Three shapes are read: a token (`AUTH="Basic ${ENCODED}"` used as `-H "Authorization: ${AUTH}"`), a whole header (`HEADER="Authorization: Basic ${ENCODED}"` used as `-H "${HEADER}"`), and a scheme (the variable holds only `Basic` and the header is `Authorization: ${SCHEME} ${TOKEN}`). The assignment may carry `export`, `readonly`, `local` or `declare`, and may be in quotes or bare. The use is a header option of a known client (`curl -H`, `--header`, `--expand-header`, `wget --header`), within thirty lines of the assignment. A scheme variable that holds `Bearer` is never read. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	AUTH="Basic ${ENCODED}"
	curl -sS -H "Authorization: ${AUTH}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' '=.{0,2}basic'
rg -n -i --glob '*.sh' '=.{0,2}authorization:\s*basic'
```

**Confidence.** **Medium.** The scheme or the header is held in a variable, so the construct does not by itself show that the request goes out. With a literal token inside the assigned value, the match also holds an in-code secret. Not read: a header file given as `-H @FILE`, a value built with no literal prefix (`HEADER=$(make_header)`), a use more than thirty lines after the assignment, and a use in another file; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-008-migration).

### SH-009 · ArgumentArrayCredentials

<a id="sh-009"></a>

The options of a call held in an array, with a credential option among the elements, and the array passed to the client: `options=(-sS -u "${API_USER}:${API_PASSWORD}")` then `curl "${options[@]}"`. The credential option is `-u` or `--user`, `--netrc`, the HTTPie options `-a` and `--auth`, `aria2c`'s `--http-passwd`, `lwp-request`'s `-C`, or a header option followed by an `Authorization: Basic` header text. The array may be assigned, appended to (`options+=(--netrc)`), set element by element, or associative, and the use is `"${name[@]}"`, `${name[*]}` or an element, within thirty lines. The text of the array, from its opening parenthesis to its closing one, is read when it is on one line of up to 10,000 characters or on up to thirty lines of at most 400 characters each, and the credential option may stand anywhere in it. A longer array is not read (see [Declared unsupported coverage](#declared-unsupported-coverage)). Arrays exist in `bash`, `zsh` and `ksh` and not in POSIX `sh` or `dash`; the example below is a `bash` script.

**Example.** Placeholders only (arrays belong to `bash`, `zsh` and `ksh`):

```sh
	options=(-sS -u "${API_USER}:${API_PASSWORD}")
	options+=(--netrc)
	curl "${options[@]}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' '=\(.*(\s-[A-Za-z]*u|--user|--netrc|--auth|--http-passwd|\s-[aC]\s|basic)'
rg -n -i --glob '*.sh' '^\s*"?(-[A-Za-z]*u|--user|--netrc|--auth|--http-passwd|-[aC])("|\s|$)'
```

The second expression finds a credential option on a continuation line, which an array that spans several lines needs.

**Confidence.** **Medium.** The credential option is in an array and the call that uses it is elsewhere, so the construct is Medium and not High. The class declares no literal credential. A header option whose header text does not show `Authorization` and `Basic` reads nothing, and a quoted option inside the array (`("-u" ...)`) is read at Low. Not read: a use more than thirty lines after the array, and an array reached through a name reference or an indirect expansion; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-009-migration).

### SH-010 · UrlUserinfoAssignment

<a id="sh-010"></a>

An address with a user name and a password assigned to a variable that a later call uses: `API_URL="https://${API_USER}:${API_PASSWORD}@host.example/..."` then `curl -sS "${API_URL}/..."`. The assignment may carry `export`, `readonly` or `local`; the use is `$name` or `${name}` inside the command of a known client within thirty lines. An assignment that no known client uses is not read, so an address used only by `git` does not match. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	API_URL="https://${API_USER}:${API_PASSWORD}@host.example/..."
	curl -sS "${API_URL}/Relativity.REST/api/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' '=.{0,2}https?://[^/@ ]+:[^/@ ]*@'
```

**Confidence.** **Medium.** The address reaches a client only through the variable, so the construct is Medium and not High. With a literal password in the address, the match also holds an in-code secret. Not read: an assignment no known client uses, a use more than thirty lines after the assignment, and a use in another file. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-010-migration).

### SH-011 · WgetChallengeDependentCredentials

<a id="sh-011"></a>

A `wget` call that gives a user name and a password and does not give `--auth-no-challenge`: `wget --user=... --password=...`, or an address with userinfo. As written, `wget` sends no `Authorization` header until the server answers with a Basic challenge, and a server that answers with a bearer challenge, as RelativityOne does (RelativityOne does not challenge with Basic; this rests on a single RelativityOne instance, is not documented by Relativity, and was not probed on Relativity Server), gets nothing. The class reads the same credential options as SH-004. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	wget --user="${API_USER}" --password="${API_PASSWORD}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'wget.*--(http-)?(user|passw(or)?d)'
rg -n -i --glob '*.sh' '^\s*--(http-)?(user|passw(or)?d)(\s|=|$)'
```

The last expression finds the option on the continuation line of a command that is split after the client name.

**Confidence.** **Low**, always, and always for manual review: no flow is decided. The call passes a credential and no preemptive switch, so it sends Basic only to a server that challenges with Basic. Confirm where the call points and whether `--auth-no-challenge` or a hand-built header is also used. With a literal password, the match also holds an in-code secret. The same call with the switch is SH-004. This class takes the place of the other Low classes at the same position.

**Which flow.** No flow is decided for a Low match. See [how Part C tells you to judge it](part-c-shell.md#sh-011-judging).

### SH-012 · AuthenticationHelperDefinition

<a id="sh-012"></a>

The definition of the script's own authentication helper: a function whose name carries the word `basic` and one of `auth`, `header` or `credential`, in either order, such as `build_basic_auth_header`. It is written `name() {` or `function name {`. The class reads the declaration, not the calls. The credential lives in the helper's body and in its callers. When the helper only returns the header or the credential, neither is matched by another class, and a literal credential passed to it is not an in-code secret. A function that itself calls a client is a wrapper, not only a helper: see [How confidence is assigned](#how-confidence-is-assigned). Runs in all four dialects.

**Example.** Placeholders only:

```sh
	build_basic_auth_header() {
		printf 'Authorization: Basic %s' "$(printf '%s:%s' "$1" "$2" | base64)"
	}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' '\w*basic\w*(auth|header|credential)\w*\s*\(\)'
rg -n -i --glob '*.sh' '\w*(auth|header|credential)\w*basic\w*\s*\(\)'
rg -n -i --glob '*.sh' 'function\s+\w*basic\w*(auth|header|credential)'
rg -n -i --glob '*.sh' 'function\s+\w*(auth|header|credential)\w*basic'
```

**Confidence.** **Low**, always, and always for manual review. The advice for a helper is not a mechanical code change: migrate the helper once, and note that its call sites do not match, because the helper hides them. A file is searched only if it holds a word that points to an HTTP client or to Basic authentication (a client name, `Authorization`, `base64`, `netrc` or a URL scheme), so a helper in a file with no other such word may not match. A helper whose body contains a construct of another class that reaches a client also matches that class, at another location; those are two facts about two places.

**Which flow.** No flow is decided for a Low match. See [how Part C tells you to judge it](part-c-shell.md#sh-012-judging).

### SH-013 · HttpieAuthentication

<a id="sh-013"></a>

A call of HTTPie, the `http` or `https` command, with a user: `http --auth "${API_USER}:${API_PASSWORD}" GET "${HOST}/..."`. HTTPie's default authentication type is `basic` and it sends the header on the first request. The option is `-a` or `--auth`, attached, separate or with `=`; the value is read in the shapes of SH-001, and a user with no password is the interactive form, where HTTPie asks for the password. An address with userinfo given to the command, and the request item `Authorization:Basic TOKEN`, are also read. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	http --auth "${API_USER}:${API_PASSWORD}" GET "${HOST}/Relativity.REST/api/..."
	http GET "${HOST}/..." "Authorization:Basic ${ENCODED}"
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' '(^|\s)https?\s(.*\s)?(-a|--auth)'
rg -n -i --glob '*.sh' '(^|\s)https?\s.*authorization:\s*basic'
rg -n -i --glob '*.sh' '^\s*(-a|--auth)(\s|=|$)'
```

The last expression finds the option on the continuation line of a command that is split after the client name.

**Confidence.** **High.** HTTPie sends Basic on the first request by default, so the construct is High in executable code that is not test code. With a literal password or token, the match also holds an in-code secret. Not read: a command with `-A` or `--auth-type` set to anything but `basic` (`digest`, `bearer`, a plugin type), a URL with a scheme that is not HTTP, and other HTTPie-like clients; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-013-migration).

### SH-014 · Aria2cCredentials

<a id="sh-014"></a>

A call of `aria2c` with credentials: `aria2c --http-user="${API_USER}" --http-passwd="${API_PASSWORD}" "${HOST}/..."`. `aria2c` sends the credentials without waiting for a challenge unless `--http-auth-challenge` is set, and always when they are in the address. The class reads `--http-passwd` (with `--http-user` anywhere in the command), a `--header` option with a literal `Authorization: Basic` value, and an address with userinfo. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	aria2c --http-user="${API_USER}" --http-passwd="${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'aria2c'
```

**Confidence.** **High.** `aria2c` sends the credentials on the first request by default, so the construct is High in executable code that is not test code. With a literal password, the match also holds an in-code secret. Not read: a command that holds `--http-auth-challenge` (SH-015 reads it at Low), and proxy credentials (`--http-proxy-user`, `--all-proxy-user`). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-014-migration).

### SH-015 · ChallengeDependentClientCredentials

<a id="sh-015"></a>

A call of `lwp-request` or `aria2c` that gives a credential, in four forms: `-C` of `lwp-request` (attached or separate) with a user and password, and `aria2c` as SH-014 with the challenge switch `--http-auth-challenge`, both of which send Basic only after the server challenges with Basic; and a `-H` option of `lwp-request` with a literal `Authorization: Basic` header, and an address with userinfo given to `lwp-request`, both of which are sent on the first request, without waiting for a challenge. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	lwp-request -C "${API_USER}:${API_PASSWORD}" "${HOST}/..."
	aria2c --http-auth-challenge=true --http-user="${API_USER}" --http-passwd="${API_PASSWORD}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'lwp-request|http-auth-challenge'
```

**Confidence.** **Low**, always, and always for manual review: no flow is decided. The `-C` form of `lwp-request` and the challenge switch of `aria2c` send Basic only after a Basic challenge, and RelativityOne does not challenge with Basic (this rests on a single RelativityOne instance, is not documented by Relativity, and was not probed on Relativity Server), so as written those calls do not send Basic to it; a server that asks for Basic gets it. Confirm where the call points. The other two forms of `lwp-request` are different: the `-H` header and the userinfo of an address are sent on the first request, without waiting for a challenge, so migrate them like SH-003 and SH-005, whatever the level of this class. With a literal password, the match also holds an in-code secret. The `GET`, `HEAD` and `POST` aliases of `lwp-request` are not matched.

**Which flow.** No flow is decided for a Low match. See [how Part C tells you to judge it](part-c-shell.md#sh-015-judging).

### SH-016 · WgetExecuteCredentialsPreemptive

<a id="sh-016"></a>

A `wget` call that gives the credential as a setting with `-e` or `--execute`, together with the preemptive switch: `wget --auth-no-challenge -e "http_user=..." -e "http_password=..."`. A setting given with `-e` is the same as a line of `wget`'s start-up file. The option may be separate, attached or with `=`; the setting is `http_user`, `http_password`, `user` or `password`, quoted or bare; and the switch is `--auth-no-challenge` or the setting `-e auth_no_challenge=on`. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	wget --auth-no-challenge -e "http_user=${API_USER}" -e "http_password=${API_PASSWORD}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'wget.*(-e|--execute)\s*[^ ]*(http_)?(user|passw(or)?d)'
rg -n -i --glob '*.sh' '^\s*(-e|--execute)\s*[^ ]*(http_)?(user|passw(or)?d)'
```

The last expression finds the option on the continuation line of a command that is split after the client name.

**Confidence.** **High.** With the preemptive switch `wget` sends Basic on the first request, so the construct is High in executable code that is not test code. With a literal password, the match also holds an in-code secret. The same call without the switch is SH-017 and is Low. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-016-migration).

### SH-017 · WgetExecuteCredentialsChallengeDependent

<a id="sh-017"></a>

The form of SH-016 without a preemptive switch: `wget -e "http_user=..." -e "http_password=..."`. As SH-011, `wget` sends Basic only after a Basic challenge. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	wget -e "http_user=${API_USER}" -e "http_password=${API_PASSWORD}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'wget.*(-e|--execute)\s*[^ ]*(http_)?(user|passw(or)?d)'
rg -n -i --glob '*.sh' '^\s*(-e|--execute)\s*[^ ]*(http_)?(user|passw(or)?d)'
```

The last expression finds the option on the continuation line of a command that is split after the client name.

**Confidence.** **Low**, always, and always for manual review: no flow is decided. The reason is the one given for SH-011: as written the call sends Basic only to a server that challenges with Basic. With a literal password, the match also holds an in-code secret. This class gives way to SH-016 and SH-011 where they match the same command.

**Which flow.** No flow is decided for a Low match. See [how Part C tells you to judge it](part-c-shell.md#sh-017-judging).

### SH-018 · HandBuiltBasicRequest

<a id="sh-018"></a>

A request written to a socket by hand, with a Basic `Authorization` header in it: the script opens `/dev/tcp`, or pipes the request into `nc`, `ncat`, `netcat`, `socat` or `openssl s_client`. The class reads the sink (the word `/dev/tcp`, the operator that opens a quoted `/dev/tcp` path, or the client word) and, within twelve lines on either side, a request line (`GET`, `POST`, `PUT`, `DELETE`, `HEAD`, `PATCH` or `OPTIONS`, a path and `HTTP/1.0` or `HTTP/1.1`) and an `Authorization:` header line with `Basic` and a literal token, an expansion or a `%s` format. The `/dev/tcp` form belongs to `bash` and `ksh93`; `zsh`, `mksh` and `dash` have none. The other forms run in all four dialects.

**Example.** Placeholders only (`/dev/tcp` belongs to `bash` and `ksh93`):

```sh
	exec 3<>/dev/tcp/host.example/80
	printf 'GET /api/ HTTP/1.1\r\nHost: host.example\r\nAuthorization: Basic %s\r\n\r\n' "${ENCODED}" >&3
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' '/dev/tcp|\s(nc|ncat|netcat|socat)\s|s_client'
rg -n -i --glob '*.sh' 'authorization:\s*basic'
```

**Confidence.** **Medium.** Basic is certainly written to the socket, but where the socket goes and whether the code runs are not in the text, and no known client is named. With a literal token, the match also holds an in-code secret. Not read: a listening side (`nc -l`, `socat` with `TCP-LISTEN`), a response (`WWW-Authenticate`, `HTTP/1.1 401`) with no `Authorization:` request header, and an `Authorization:` header that the script reads with `grep`, `case` or `awk`, which is an inbound check. A request built by hand in another language is not read. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-018-migration).

### SH-019 · CurlConfigurationWrittenByScript

<a id="sh-019"></a>

A configuration for `curl` that the script writes, with the credential in it, read by a `curl` call with `-K` or `--config`: a line `user = "user:password"` or `header = "Authorization: Basic ..."`. Three ways are read: `curl -K -` fed by a here-document on the same command; `curl -K "$cfg"` where, within thirty lines, the same path was written by a here-document redirection, `printf` or `echo` with `>` or `>>`, `tee`, or a brace group, subshell or loop whose closing line carries the redirection; and `curl -K <(printf ...)` with a process substitution (`bash`, `zsh`, `ksh`). Runs in all four dialects.

**Example.** Placeholders only:

```sh
	cat > "${CONFIG_FILE}" <<-EOF
		user = "${API_USER}:${API_PASSWORD}"
	EOF
	curl --silent --show-error --config "${CONFIG_FILE}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'curl.*(\s-K|--config)'
rg -n -i --glob '*.sh' '^\s*(--)?user\s*[=:]'
rg -n -i --glob '*.sh' '^\s*(-K|--config)(\s|=|$)'
```

The last expression finds the option on the continuation line of a command that is split after the client name.

**Confidence.** **Medium.** The script writes the credential into the configuration and `curl` reads it, but the two are joined only by the text of the path, so the construct is Medium and not High. With a literal password or a literal Basic token in the configuration line, the match also holds an in-code secret. Not read: a configuration the script only reads, a write made by a function with arguments or to a path reached by indirection, and a write more than thirty lines before the call; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-019-migration).

### SH-020 · WgetConfigurationWrittenByScript

<a id="sh-020"></a>

A configuration for `wget` that the script writes, with the credential in it, read through `WGETRC` or `wget --config`: lines such as `http_user = ...` and `http_password = ...` written with a here-document, `printf`, `echo` or `tee`, or inside a brace group, subshell or loop whose closing line carries the redirection, to the path that `WGETRC` or `--config` names. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	printf 'http_user = %s\nhttp_password = %s\n' "${API_USER}" "${API_PASSWORD}" > "${RC_FILE}"
	export WGETRC="${RC_FILE}"
	wget "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'WGETRC|wget.*--config'
```

**Confidence.** **Low**, always, and always for manual review: no flow is decided. The script writes the credential and `wget` reads it, but `wget` sends Basic only after a Basic challenge unless the file also turns the preemptive setting on, and the weaker fact decides the level. Confirm where the call points. The class carries no literal credential position for a user line; a literal password value is an in-code secret. A configuration the script only reads is not read.

**Which flow.** No flow is decided for a Low match. See [how Part C tells you to judge it](part-c-shell.md#sh-020-judging).

### SH-021 · CurlViaSameFileAliasOrVariable

<a id="sh-021"></a>

A `curl` call made through a name that the same file defines: `CURL=curl` and then `${CURL} -u ...`, or `alias c=curl` and then `c -u ...`. The definition is read within thirty lines before the use: `alias NAME=curl`, `NAME=curl`, `NAME=/path/to/curl`, `NAME=curl.exe`, and `NAME=$(command -v curl)`, `$(which curl)` or `$(type -p curl)`, with `export`, `readonly`, `local` or `declare`. The use is the command word (`NAME` for an alias, `$NAME`, `${NAME}`, `"$NAME"` or `"${NAME}"` for a variable). Behind the name three forms are read: a user option with a value (a user name only is the interactive form), a literal `Authorization: Basic` header option, and an address with userinfo. The variable form runs in all four dialects; an alias is expanded on a later line by `dash`, `zsh`, `ksh` and `mksh`, and by `bash` only with `expand_aliases` on or in POSIX mode.

**Example.** Placeholders only:

```sh
	CURL=curl
	${CURL} --silent --show-error -u "${API_USER}:${API_PASSWORD}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' '\w+=[^ ]*curl|\w+=.?\$\(.*curl'
rg -n -i --glob '*.sh' 'alias\s+\w+=.?[^ ]*curl'
```

**Confidence.** **Medium.** The command word is proved only by a definition in the same file, so the join is by name and ambiguity resolves downward. The class declares no literal credential. Not read: the other forms behind a name (a credentials file, the any-authentication option, a configuration file, an argument array), a name reached by indirection, and a definition more than thirty lines before the use; see [Declared unsupported coverage](#declared-unsupported-coverage). **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-021-migration).

### SH-022 · WgetViaSameFileAliasOrVariable

<a id="sh-022"></a>

A `wget` call made through a name that the same file defines, with a credential: `WGET=wget` and then `${WGET} --user=... --password=...`. The definitions are those of SH-021 for `wget`. Behind the name the class reads the long options `--user`, `--http-user`, `--password` and `--http-password` (and `--passwd`, `--http-passwd`), and an address with userinfo. `wget` has no short option for the user or the password. Runs as SH-021.

**Example.** Placeholders only:

```sh
	WGET=wget
	${WGET} --user="${API_USER}" --password="${API_PASSWORD}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' '\w+=[^ ]*wget|\w+=.?\$\(.*wget'
rg -n -i --glob '*.sh' 'alias\s+\w+=.?[^ ]*wget'
```

**Confidence.** **Low**, always, and always for manual review: no flow is decided. The definition rule is that of SH-021 and the level is that of SH-011: `wget` sends Basic only after a Basic challenge. The preemptive switch behind a name is not promoted, because the join is by name. The class declares no literal credential, so a password written there is not an in-code secret.

**Which flow.** No flow is decided for a Low match. See [how Part C tells you to judge it](part-c-shell.md#sh-022-judging).

### SH-023 · CurlAnyauthWithUser

<a id="sh-023"></a>

A `curl` call that gives a user and `--anyauth`: `curl --anyauth -u "${API_USER}:${API_PASSWORD}" "${HOST}/..."`. `--anyauth` makes `curl` wait for the server's challenge and choose the method, so it sends nothing on the first request to a server that does not challenge. The class reads a command that holds `--anyauth` and a user option with a value in the shapes of SH-001; a command with one of the methods listed under SH-001 is not matched. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	curl --anyauth -u "${API_USER}:${API_PASSWORD}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'curl.*--anyauth'
rg -n -i --glob '*.sh' '^\s*--anyauth(\s|$)'
```

The last expression finds the option on the continuation line of a command that is split after the client name.

**Confidence.** **Low**, always, and always for manual review: no flow is decided. As written the call does not send Basic to a server that does not challenge with Basic, such as RelativityOne (this rests on a single RelativityOne instance, is not documented by Relativity, and was not probed on Relativity Server); a server that offers Basic gets it. Confirm where the call points. With a literal password, the match also holds an in-code secret.

**Which flow.** No flow is decided for a Low match. See [how Part C tells you to judge it](part-c-shell.md#sh-023-judging).

### SH-024 · EncodedPairHeldInVariable

<a id="sh-024"></a>

A user name and a password joined by a colon in a variable, then encoded to Base64 in a later statement: `pair="${API_USER}:${API_PASSWORD}"`, then `printf '%s' "${pair}" | base64`. The pair test and the refusals are those of SH-007, and the encoder must consume `"$name"` within thirty lines, with its output reaching a known client as SH-007 requires. Runs as SH-007.

**Example.** Placeholders only:

```sh
	pair="${API_USER}:${API_PASSWORD}"
	ENCODED=$(printf '%s' "${pair}" | base64 | tr -d '\n')
	curl -sS -H "Authorization: Basic ${ENCODED}" "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' 'base64|uuencode|openssl\s+enc\s(.*\s)?-a([^[:alnum:]_-]|$)'
rg -n -i --glob '*.sh' '^\s*\w+="[^"]*:[^"]*"'
```

**Confidence.** **Medium**, for the reason given for SH-007. The class declares no literal credential. Not read: a pair built by a function or reached through another file, and a pair that is a host and a port. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-024-migration).

### SH-025 · OptionsHeldInStringVariable

<a id="sh-025"></a>

The options of a call held in a string variable, with a credential option among them, and the variable used unquoted so that the shell splits it into words: `CURL_OPTIONS="-sS -u ${API_USER}:${API_PASSWORD}"` then `curl ${CURL_OPTIONS} "${HOST}/..."`. The credential options are those of SH-009. The use is `$name` or `${name}` unquoted in a known client's command within thirty lines. The quoted use `"$name"` is one word and is not read. Runs in all four dialects.

**Example.** Placeholders only:

```sh
	CURL_OPTIONS="-sS -u ${API_USER}:${API_PASSWORD}"
	curl ${CURL_OPTIONS} "${HOST}/..."
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.sh' '\w+=.[^"]*\s(-[A-Za-z]*u|--user|--netrc|--auth|--http-passwd)'
```

**Confidence.** **Medium.** The credential is in a string and the call that uses it is elsewhere, so the construct is Medium and not High. The class declares no literal credential. A string that holds a credential is split into words by the shell, so it also appears in the process list while the command runs. Not read: the quoted use, and a use more than thirty lines after the assignment. **Low** under the file and text rules of [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. When no signal is found, use Client Credentials unless something shows that a person signs in. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. See [the migration entries in Part C](part-c-shell.md#sh-025-migration).

## Flow signals

Each High or Medium match needs one of two OAuth 2.0 flows. Choose it from signals in the code, and only from the code.

- An **interactive-user signal** is evidence that the credential belongs to a person who supplies it at run time. It decides **Authorization Code**.
- A **fixed-credential signal** is evidence that the credential is fixed for the integration. It decides **Client Credentials**.
- With **no signal**, when nothing shows that a person signs in, use **Client Credentials**; the choice needs confirming by hand.
- If both kinds are present, the interactive-user signal wins. A script that reads a credential from the environment and falls back to a silent prompt when it is unset therefore points to Authorization Code, although it may well be an unattended integration. Confirm the flow for such a script by hand.

**The cross-file limit.** Signals are read in the same file as the match, and only there. A prompt in a script that this one `source`s, a credential read in another file, and a credential that a calling script passes in are not seen. For most real scripts the credential reaches the matching file from another file, so a match with no signal is the expected outcome; follow the credential to where it comes from and apply the criterion under [Choosing the flow by hand](#choosing-the-flow-by-hand). A comment never counts as a signal, and a literal credential written into the script is not evidence of either flow.

**A prompt that feeds another tool is not a signal.** A prompt that is directly the input of `sudo -S`, `sshpass`, `ssh` or an `openssl` password argument, on the same line, is not evidence about RelativityOne: the answer goes to a privilege tool or a remote login. The same holds when the variable the `read` fills reaches such a command within forty lines. A prompt whose own wording names `ssh` or `sudo` still counts.

**Dialects.** Two signals depend on the dialect of the file, which comes from the interpreter the first line names and, when the first line names none that is covered, from the extension. `read -s` is accepted and does not hide the typed input in `ksh93` and `mksh`, so a silent `read -s` is not taken as a hidden prompt in a script of either dialect; and `read -p` is a prompt in `bash` alone, because in `zsh`, `ksh93` and `mksh` it is a coprocess option, so the wording of a `read -p` prompt counts in `bash` only, and a first line that names `sh` is read as `bash`. A first line that names `ksh` or `ksh93`, directly or through `env`, is read as `ksh93`, and one that names `zsh` as `zsh`; a first line that names `mksh` is read as `mksh`, and without one no extension selects it; a first line that names an interpreter wins over the extension, so a `.zsh` or `.ksh` file whose first line names `bash` is read as `bash`. Without such a first line a `.ksh` file is read as `ksh93` and a `.zsh` file as `zsh`. A file with no extension is analyzed only when its first line names `bash`, `sh`, `zsh` or `ksh`, so an extensionless script that names `ksh93` is not. A file with neither a first line naming its dialect nor one of those two extensions is read as `bash`. The other signals are read in every dialect.

**Shell has fifteen flow signals, ten interactive-user and five fixed-credential.**

Some signals read the name of a variable. The name matches when it contains `password`, `passwd`, `secret`, `credential`, `credentials`, `creds` or `token` anywhere, in any letter case, and when it contains `pass`, `pw`, `pwd` or `apikey` only as a whole word, so `DB_PASS` and `dbPass` match while `bypass` and `compass` do not.

### Interactive-user signals

- The `read` builtin with the silent option `-s` before any option that takes an argument (`read -s`, `read -rs`, `read -sp "Password: "`), with the input not redirected from a file or a here-string; `</dev/tty` still counts. Not read in `ksh93` and `mksh`, where the option hides nothing.
- `stty -echo` followed by a `read` within the next five lines, the portable way to read a secret in POSIX `sh`.
- `read` into a credential-named variable (the name rule above), with the input not redirected, and the `zsh` and `ksh` form `read 'name?prompt'`. A `while` loop that reads from a file on its `done` line, and a `read` that follows a pipe, are not interactive.
- `systemd-ask-password`; `whiptail` or `dialog` with `--passwordbox`; `zenity` with `--password`, or with `--entry` and `--hide-text`; `kdialog` with `--password`.
- `osascript` with a script that asks for an answer `with hidden answer`, the masked prompt of macOS.
- `curl -u user` or `--user user` with a literal user name and no password, directly or behind a same-file alias or command variable (class SH-002).
- `wget` with `--ask-password`.
- The word `password` or `passphrase` inside the string of the prompt option `read -p "..."`. Read in `bash` only (a script whose first line names `sh` is read as `bash`), because `-p` is a coprocess option in the other dialects.
- The word `password` or `passphrase` in the `printf` or `echo` on the line before a `read`, or in the prompt of the `zsh` and `ksh` form `read "name?Password: "`. Read in every dialect.
- `http` or `https` with `-a user` or `--auth user` with a literal user name and no password.

### Fixed-credential signals

- In the arguments of a known client (`curl`, `wget`, `http`, `https`, `aria2c`, `lwp-request`), an expansion of a variable whose name names a credential (`$PASSWORD`, `${API_TOKEN}`, `$CREDS`) or the required-variable form `${NAME:?message}`. The arguments may continue over a backslash and a line feed. `curl -u "$CREDS"` is this signal and not an interactive one.
- A read from a secret store: `security find-generic-password` and `find-internet-password` (the macOS keychain), `secret-tool lookup`, `pass show`, `gopass show`, `vault kv get`, `aws secretsmanager get-secret-value`, `aws ssm get-parameter`, `az keyvault secret show`, `gcloud secrets versions access`, `op read` and `bw get password`.
- The `curl` options `-n`, `--netrc`, `--netrc-file` and `--netrc-optional` (class SH-006).
- `read` into a credential-named variable with the input redirected from a file (`read -r PASS < FILE`): a value read from a file is stored, not typed.
- A `while` or `until` loop whose `read` fills a credential-named variable and whose `done`, within twenty lines, is redirected from a file or a process substitution, not from `/dev/tty`.

### Choosing the flow by hand

The one question is who the code acts for. When no person signs in and the code acts for the integration itself, use **Client Credentials**. When the code acts for a person who is signed in, use **Authorization Code**. When both kinds of evidence are present, the interactive-user evidence wins and the flow is Authorization Code. When a file holds two unrelated pieces of code, one for a signed-in person and one that runs unattended, judge each piece by who it acts for. A literal credential written into the source is not evidence of either flow. When an OAuth2 Client already exists for this integration, use its flow: the flow is fixed when an OAuth2 Client is created and cannot be changed afterwards.

## Declared unsupported coverage

The rules state what they do not cover, so that a missing finding is not read as a clean result. The list below is what the Shell rules declare as unsupported, each followed by the reason.

Some entries below describe limits that apply only to an automated scan, such as a time limit or a cap on the number of results. They do not apply when you search by hand.

- **other shells and batch files: fish, csh and tcsh scripts, and Windows batch files ending in .bat or .cmd.** These are different languages from the POSIX family of shells these classes cover; a file of that kind is outside Shell.
- **shell commands embedded in other files: CI pipeline definitions, Dockerfiles, Makefiles, crontabs, service unit files and Markdown documents.** The containing file is not a shell script, so its text is not analyzed as one; many command lines that pass a user name and password to an HTTP client live in such files, so search them separately.
- **shell scripts that are not attributed to Shell: files ending in .bats, shell start-up files such as .bashrc, .bash_profile, .profile, .zshrc, .zshenv and .envrc, and a script with no file extension whose first line does not name bash, sh, zsh or ksh, in lower case, as the interpreter, directly or after env and any options written as one word, such as dash, ash, mksh, ksh93 or busybox sh, or that puts a separate argument word or a variable assignment between env and the shell.** Such a file is outside Shell, so nothing in it is searched; a silent omission is the cost. The first line is read only for a file with no extension: a file ending in .sh, .bash, .zsh or .ksh is read whatever its first line names, and a file with any other extension is not attributed to Shell by its first line.
- **a source file saved in a single-byte encoding other than Windows-1252, or one whose only line terminator is a carriage return or the Unicode line separator.** A single-byte file is read as Windows-1252, so a file in another encoding may not be read with confidence; line ends other than carriage return and line feed are not recognized.
- **credentials held in a file the script only reads: the contents of a .netrc file, a .curlrc file or a configuration file passed to curl that the script does not write, a .wgetrc file, .env files and other credential files.** The credential is in another file, not in the text analyzed, and a configuration file is where a credential usually lives; search such files separately for user names and passwords. A configuration the script writes itself is read.
- **a header, token or scheme supplied by another file through source or the dot command.** No per-file analysis joins a definition in one file to its use in another; an option such as a user given as a variable is read whatever the variable holds.
- **evidence of how a user signs in that is supplied from another file through source or the dot command.** Sign-in evidence is read per file; a file whose evidence is elsewhere is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in; that is an expected outcome and not a failure - where the script acts for a signed-in user, use the Authorization Code flow.
- **sign-in evidence that is not listed: a custom prompt function, a prompt program, a credential read by command substitution from a file, a sourced environment file.** The set of possible prompts is open and a prompt with no stable shape cannot be listed in advance; only the signals with a stable shape in the code are read, and a file whose evidence is of another kind is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in.
- **the idiom of an environment variable first and a silent prompt otherwise.** The flow rule assigns the interactive flow when an interactive signal is present, so such a file is assigned Authorization Code even where the script also reads a fixed credential.
- **a header written to a file that is passed to the client as a header file.** The header text is in a file the script names by path and builds with format strings and variables; telling a Basic header from the migrated Bearer one needs that text, which a per-file analysis has only for the part the file writes literally.
- **a name reached by indirection, and, behind a same-file alias or command variable, the forms of a credentials file option, the any-authentication option, a configuration file, an argument array and a header taken from a file.** A name reached by indirection is not text at the call, and the further forms behind an alias are left out to keep that reading inside its cost limits; the same forms written directly on the client's command are read.
- **a client called inside a wrapper function with variables: the call inside the function reads its credential from the function's parameters or from variables set elsewhere.** A call inside a wrapper function is read only where a call to the function passes a literal credential; a call that passes variables yields no match, because the value of a variable is not followed across a function boundary.
- **any other client: other command-line transfer tools, python, perl and node programs that make the call, the short aliases of the lwp-request tool, and a configuration of another client that the script writes.** The set of clients is open and no list can name every one; a call written by hand to a network device or to a socket is read, a program in another language is not.
- **a mock, fake or stub script with a neutral name.** No name or folder says it is one and no text shows it, so it keeps its full level and may be a false positive; review such matches.
- **a construct that is in a dead branch or after an exit, or a function that is never called when it references no parameter.** Whether a line runs depends on the order and the conditions of execution, which cannot be known without parsing the script; such code may match although it never authenticates to RelativityOne. A construct that is built into a variable that nothing sends is a different case: it does not match.
- **confirmation that a call goes to RelativityOne and not to another server.** Only a command line is seen and the destination host cannot be determined; a match may not apply if the destination is not RelativityOne - review each one before acting.
- **ANSI-C quoting in a script run by dash, including as a here-document terminator word.** dash has no ANSI-C quoting and reads such a word as an expansion, a different word from the one bash, zsh, ksh93 and mksh decode, so the script means something else in dash itself; the form is read as those shells do.
- **a here-document terminator continued across a backslash and a line feed on a body line, in a script run by dash.** dash compares the word with the physical line and removes the pair only afterwards, so such a terminator never closes the body in dash; bash joins the lines and closes it, and bash is followed.
- **a here-document terminator word quoted across a raw line feed, in a script run by dash.** dash closes the body on two consecutive lines that hold the two halves of the word, while bash never closes it and reads to the end of the file; bash is followed, so the lines dash runs after the two halves are read as body.
- **a here-document opened inside a command substitution that closes on the opening line, in code or in a double-quoted string, in bash before version 5.2 and in dash.** Those shells end the here-document at the substitution's end with an empty body and run the following lines as commands, where they are read as the body, as bash 5.2 does.
- **a file whose analysis takes too long, a command longer than the reading window, a quoted string longer than 4,096 characters inside a window, a line longer than 10,000 characters, an array of command options whose text, from its opening parenthesis to its closing one, is longer than one line of 10,000 characters or thirty lines of 400 characters, and a use of a variable more than thirty lines after its definition.** These are internal limits that keep the cost bounded: a file that trips a time limit or holds a line over the ceiling is skipped and counted, so the loss is visible as a number, and a window that grew without bound would make the cost grow without bound. The time spent reading the strings and comments of a file is limited to 1000 milliseconds.
- **which matches were left out when the cap on matches was reached, per file or in all.** The cap is given in aggregate only, so it cannot be said which matches were left out.
- **more than 5,000 here-document openings waiting for their bodies in one region of a file.** An opening past that bound is read as code and its body is read as code, silently; no script holds thousands of openings before its first body, and the bound limits memory and time.
- **whether a double parenthesis opens arithmetic or two nested subshells, when a here-document body inside the expression hides its closing, and any run read after the per-file read-ahead budget is spent.** The choice is a bounded read-ahead and not the shell's parser; after the budget is spent a run is read as not arithmetic, silently, so that the walk stays linear.

### Shell run from a string

A string or a script that the shell runs is read as code, so a construct in it keeps its class's own level and is not Low. What is read as code is the body of `eval`, of a shell given `-c` (`bash`, `sh`, `zsh`, `ksh`, `dash`, `mksh`), of `su -c`, and of `ssh host "..."`, the commands inside backticks and inside `${...}` in a double-quoted string, and a here-document that is fed to a shell, written to a file ending in `.sh`, `.bash`, `.zsh`, `.ksh` or `.dash`, or whose first line is a shell `#!` line. A string inside a string is read up to eight levels.

Some forms of a script run from a string are **not read as code**, so a construct in them stays text and is Low: the stage after a pipe on a continuation line; `cat <<EOF > f; bash f`, where the script is written and run by separate commands; `bash < file`; process substitution; here-strings; `tee file <<EOF`; a prefix wrapper such as `env`, `nohup`, `exec` or `command` before the shell; a quoted command word; a `$'...'` string; `bash -c -- 'command'`; a shell name split over two lines with a backslash; backticks and `${...}` inside a here-document body; the earlier stages of a pipeline, since only the last stage is asked; text more than 256 characters before the quote, or more than 32 words into the command; nesting deeper than eight levels; and the inline reading of a very large file, which has a budget. A string that cannot be read is left as text, silently. Review a Low match inside a quoted command by hand: it may be live code.
