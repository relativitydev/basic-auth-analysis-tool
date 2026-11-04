namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// The one name every text uses for where a client's OAuth 2.0 credentials belong.
///
/// The pointer is to Part B of the Migration Documentation, which the product delivers with this
/// tool. It carries a name and nothing else: no URL, because the tool is offline by design and a
/// link would be a claim about a location this build cannot check, and no phrase describing how the
/// document is delivered, because that changes with the release and a text compiled into the binary
/// would go stale. A text that needs to send its reader to the Secret Management material takes the
/// name from here, so a rename is one edit and a test can assert that no text spells it differently.
/// </summary>
public static class SecretManagementPointer
{
	/// <summary>The Part B name, exactly as the Migration Documentation titles it.</summary>
	public const string PART_B_NAME = "Secret Management (Part B of the Migration Documentation)";
}
