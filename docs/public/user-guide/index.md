# User Guide

This guide explains how to install and run `migrate-auth`, how to read what it writes, and the OAuth 2.0 background you need to act on it. It has eight sections, one file each. The tool is an alpha release: see the [release notes](02-release-notes.md).

| Section | What it covers |
| --- | --- |
| [1. Using the tool](01-using-the-tool.md) | What the tool does and does not do, installing and verifying it, the session, the commands, the configuration file, reading the report, where outputs go, and exit codes. |
| [2. Release notes and What's next](02-release-notes.md) | What this release supports, known issues, how to report an issue, and the next milestones. |
| [3. Limits of the tool](03-limits.md) | Languages delivered, what is not covered, the accuracy figures, and what a result does and does not mean. |
| [4. OAuth 2.0 essentials](04-oauth2-essentials.md) | Why Basic Authentication is being replaced, Bearer tokens, and the two flows. |
| [5. Configuring the OAuth2 Client](05-configuring-the-oauth2-client.md) | The OAuth2 Client fields for each flow. |
| [6. Secrets](06-secrets.md) | What the new secrets are and the basic rules. |
| [7. Token expiry](07-token-expiry.md) | The token lifetime and how each flow handles expiry. |
| [8. More information from Relativity](08-more-from-relativity.md) | Links to Relativity's official documentation. |

The rules behind each finding are in the Migration Documentation: [Part A, rule patterns for C#](../migration/part-a-csharp.md), [Part B, Secret Management](../migration/part-b-secret-management.md), and [Part C, migration suggestions for C#](../migration/part-c-csharp.md).
