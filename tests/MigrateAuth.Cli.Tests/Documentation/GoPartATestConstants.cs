namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>The class identifiers, the counts and the helper text that the Go Part A tests share, declared once.</summary>
internal static class GoPartATestConstants
{
	internal const string TRANSPORT_CLASS = "GO-010";
	internal const string HELPER_CLASS = "GO-011";
	internal const string ENVIRONMENT_CLASS = "GO-012";
	internal const string HELPER_CALL_CLASS = "GO-013";
	internal const int REGISTERED_CLASS_COUNT = 13;
	internal const int INTERACTIVE_SIGNAL_COUNT = 6;
	internal const int FIXED_SIGNAL_COUNT = 5;

	/// <summary>A function that builds a Basic header from its parameters, followed by a blank line.</summary>
	internal const string BASIC_HELPER_DECLARATION = "func basicHeader(userName string, secret string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))\n}\n\n";

	/// <summary>The same helper written as a method, so that the name is declared twice in a file that also holds the function.</summary>
	internal const string BASIC_HELPER_METHOD_DECLARATION = "func (client *apiClient) basicHeader(userName string, secret string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))\n}\n\n";
}
