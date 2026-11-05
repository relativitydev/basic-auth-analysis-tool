# Secrets (User Guide, section 6 of 8)

Moving from Basic Authentication to OAuth 2.0 does not remove secrets. It changes what they are and how long they are valid. This section says what the new secrets are and the basic rules for them. The procedures are in [Secret Management (Part B of the Migration Documentation)](../migration/part-b-secret-management.md).

## What the new secrets are

- **The client secret** of an OAuth2 Client. The application presents it, with the client ID, to obtain a token. Anyone who holds the client ID and the client secret can obtain tokens as that client. RelativityOne shows the secret **once**, when the client is created or when the secret is reset.
- **Refresh tokens**, where you enable them on an Authorization Code client. A refresh token lets the application obtain new access tokens without asking the user to sign in again, so it is as sensitive as a password. A Client Credentials client has no refresh tokens.
- **Access tokens** are secrets too, but they are short-lived. Send them only over HTTPS and never write them to a log.

The client ID and the Redirect URLs are not secrets.

## The basic rules

1. **Never hardcode a secret.** Not in source code, not in a configuration file that is committed, not in a build script, not in a test. The tool flags a credential written literally into the source with a security note; see [secret warnings](01-using-the-tool.md#secret-warnings). Part B, [Hardcoded credentials](../migration/part-b-secret-management.md#hardcoded-credentials), explains how to remove one and why deleting the line is not enough.
2. **Keep secrets in a secret store**, and read them at run time. Part B, [Reading secrets from a vault at runtime](../migration/part-b-secret-management.md#reading-secrets-from-a-vault-at-runtime), shows how.
3. **Rotate the client secret.** Part B, [Rotating client secrets](../migration/part-b-secret-management.md#rotating-client-secrets), gives the procedure. A reset invalidates the previous secret immediately, so plan the cut-over before you reset it.
4. **Revoke what is compromised or no longer needed.** For a client secret that means resetting it, or disabling or deleting the client. For refresh tokens, see [Storing and revoking refresh tokens](../migration/part-b-secret-management.md#storing-and-revoking-refresh-tokens). Whether disabling a client ends access tokens that were already issued is not documented by Relativity; treat those tokens as valid until they expire.
5. **Give each integration its own OAuth2 Client**, with a Context User that has only the permissions the integration needs. Part B, [Choosing a least-privilege Context User](../migration/part-b-secret-management.md#choosing-a-least-privilege-context-user), explains the choice.

Never paste a secret into a ticket, a chat message or a report. The tool's report and JSON file contain no credential values, but they list where credentials are; handle them accordingly.
