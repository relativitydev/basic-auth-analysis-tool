# User Guide

This guide explains how to set up and run `migrate-auth`, how to read what it writes, and the OAuth 2.0 background you need to act on it. It has eight sections, one file each. For what changed in each release, see the [release notes](02-release-notes.md).

[Documentation index](../README.md#documentation)

**Contents**

| Section | What it covers |
| --- | --- |
| [1. Using the tool](01-using-the-tool.md) | What the tool does and does not do, setting it up and verifying it, the session, the commands, the configuration file, reading the report, choosing and confirming the flow, where outputs go, and exit codes. |
| [2. Release notes](02-release-notes.md) | What changed in this release, what it supports, known issues, and how to report an issue. |
| [3. Limits of the tool](03-limits.md) | Supported languages, what is not covered, the accuracy figures, and what a result does and does not mean. |
| [4. OAuth 2.0 essentials](04-oauth2-essentials.md) | Why Basic Authentication is being replaced, Bearer tokens, and the two flows. |
| [5. Configuring the OAuth2 Client](05-configuring-the-oauth2-client.md) | The OAuth2 Client fields for each flow. |
| [6. Secrets](06-secrets.md) | What the new secrets are and the basic rules. |
| [7. Token expiry](07-token-expiry.md) | The token lifetime and how each flow handles expiry. |
| [8. More information from Relativity](08-more-from-relativity.md) | Links to Relativity's official documentation. |

The patterns the tool looks for, and what to do about each, are in the [Migration Documentation](../migration/README.md): Part A (rule patterns), Part B (Secret Management) and Part C (migration suggestions), with a page for each of the eight languages. It also serves readers who do not run the tool: it can be followed by hand to find and migrate Basic Authentication.
