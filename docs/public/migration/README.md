# Migration Documentation

The Migration Documentation tells you how to find Basic Authentication in your own source code and how to move it to OAuth 2.0 for RelativityOne. It is written for developers and operators who own an integration that signs in to RelativityOne with a user name and a password. You need no program to use it: you search your code with the expressions it gives, judge each match, and apply the change it describes. Everything is plain text that you read and a search command that you run yourself.

## The three parts and the order to use them

1. **Find with Part A.** Part A lists, for each language, every pattern class: what the construct looks like, an expression to search for it, how sure the match is (High, Medium or Low) and which OAuth 2.0 flow it needs.
2. **Change with Part C.** Part C gives, for each pattern class and each flow, the code to replace, the code that replaces it, how to obtain and cache the token, and the OAuth2 Client to create.
3. **Protect the new secrets with Part B.** Part B says where the new credentials live and how to look after them: how to store, rotate and revoke them, how to keep them out of source code, and how to choose a least-privilege Context User. Part B is shared by all languages.

## The eight languages

| Language | Part A: find it | Part C: change it |
| --- | --- | --- |
| C# | [Rule patterns for C#](part-a-csharp.md) | [Migration suggestions for C#](part-c-csharp.md) |
| VB.NET | [Rule patterns for VB.NET](part-a-vbnet.md) | [Migration suggestions for VB.NET](part-c-vbnet.md) |
| JavaScript | [Rule patterns for JavaScript](part-a-javascript.md) | [Migration suggestions for JavaScript](part-c-javascript.md) |
| TypeScript | [Rule patterns for TypeScript](part-a-typescript.md) | [Migration suggestions for TypeScript](part-c-typescript.md) |
| Python | [Rule patterns for Python](part-a-python.md) | [Migration suggestions for Python](part-c-python.md) |
| Go | [Rule patterns for Go](part-a-go.md) | [Migration suggestions for Go](part-c-go.md) |
| PowerShell | [Rule patterns for PowerShell](part-a-powershell.md) | [Migration suggestions for PowerShell](part-c-powershell.md) |
| Shell | [Rule patterns for Shell](part-a-shell.md) | [Migration suggestions for Shell](part-c-shell.md) |

Part B for all of them: [Secret Management](part-b-secret-management.md).

## How to work through it by hand

1. **Search.** Open Part A for the language of your code. Run the search expression of each pattern class, or start from the "Find your code" table when you know what your code does and not which class describes it, and search the files of that language in your repository. Search hidden folders and files that your version control ignores too, if your code lives there, and leave out the version control folder itself.
2. **Judge each match.** Read the match in its file and compare it with the description of the class. Part A tells you how sure the match is and what would make it something other than live authentication code. A match that is not live code still needs a person to look at it (see [What High, Medium and Low mean](#what-high-medium-and-low-mean)).
3. **Choose the flow.** Decide which OAuth 2.0 flow the code needs (see [How to choose the flow by hand](#how-to-choose-the-flow-by-hand)).
4. **Apply the suggestion.** Open Part C for the same language, find the entry for the class and the flow, replace the Basic Authentication code, and create the OAuth2 Client that the entry names.
5. **Protect the new secrets.** Store the client secret as Part B describes, and remove the old user name and password from the code and from the places where it was stored.

Repeat for every language that your repository uses.

## How to choose the flow by hand

Ask one question of the code that holds the match: **who supplies the credential at run time?**

- If a person types it or signs in, the code acts for that person. Use **Authorization Code**. The application redirects the signed-in person to RelativityOne, and the token it receives carries that person's permissions.
- If it comes from configuration, settings, an environment variable or a secret store, and no person is present, use **Client Credentials**. The application sends its client ID and client secret to the token endpoint, and every call runs as the OAuth2 Client's Context User.

Some cases need care:

- **Look beyond the file.** A prompt, a login page or a login handler may sit in a different file from the match, and a credential may reach the match through dependency injection, configuration binding or a parameter. Follow the credential back to where it comes from. If a person supplies it, use Authorization Code, even if the match itself shows nothing about a person.
- **Both kinds are present.** Code that uses a stored credential when there is one and falls back to a prompt when there is not holds both. Decide by how the code runs. If it is in practice an unattended integration, the stored credential is the one it uses, and Client Credentials is the right flow.
- **A literal credential is not evidence of either flow.** A user name and password written into the code is a secret to remove. It does not tell you who supplies the credential at run time.
- **Use the flow of an existing OAuth2 Client.** If your RelativityOne instance already has an OAuth2 Client for this integration, use the flow of that client. The flow is fixed when an OAuth2 Client is created, so moving an integration from one flow to the other means creating a new OAuth2 Client ([OAuth2 clients](https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm)).

## What High, Medium and Low mean

Each pattern class in Part A carries a confidence level. The level is a property of the pattern and of where the match sits, not a measurement of your code.

- **High.** The construct itself shows Basic Authentication on a live call path, in executable code that is not test code. For example, a type name that declares both that an object is a credential and that it carries a user name and a password. Treat a High match as code to migrate.
- **Medium.** The construct has the shape of Basic Authentication, but the credential source or the call path cannot be established from the file alone. A High match in a file of test code is also Medium: test code is capped at Medium and no lower, and it is migrated like any other code. Check the match against your code before you change it.
- **Low.** The text is present but is unlikely to be live authenticating code. A match is Low when it is inside a comment or a string, or in a file that is a mock, a sample, an example, vendored or generated code, including a file that is test code and also one of those. These are the general causes. The section "How confidence is assigned" in the Part A of each language lists further causes specific to that language, for example deployment code in C#, a help block in PowerShell and a declaration file in TypeScript, and the list is not repeated here, so read that section for your language. A Low match has no flow and no suggestion in Part C. **A Low level is not evidence that the code does not authenticate.** It means a person has to look. Part C explains, for each language, how to judge each cause.

## What the patterns do not cover

Each Part A ends with a list called Declared unsupported coverage. It says what the patterns of that language do not cover, so that a search that finds nothing is not read as a clean result. Read it before you conclude that a repository has no Basic Authentication, and search the uncovered forms by other means.
