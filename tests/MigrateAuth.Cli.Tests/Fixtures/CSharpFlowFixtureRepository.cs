using System.Collections.Immutable;

namespace MigrateAuth.Cli.Tests.Fixtures;

/// <summary>
/// AN INVENTED C# REPOSITORY WHOSE THREE FILES EACH HOLD ONE HIGH FINDING AND DIFFER ONLY IN THE FLOW
/// EVIDENCE BESIDE IT: one file with an interactive-user signal, one with a fixed-credential signal,
/// and one with neither. Every name is generic; none comes from any real repository.
///
/// The files are ordered by path so a report lists them fixed, interactive, plain, which is what the
/// tests that read findings by position rely on. They are placed into a temporary repository and run
/// through the REAL embedded `CSharp` analyzer - nothing here is a hand-built finding.
/// </summary>
internal static class CSharpFlowFixtureRepository
{
	internal const string FIXED_FILE_NAME = "FixedService.cs";
	internal const string INTERACTIVE_FILE_NAME = "InteractiveSignIn.cs";
	internal const string PLAIN_FILE_NAME = "PlainClient.cs";

	private const string _FIXED_FILE_CONTENT =
		"public sealed class FixedService\n{\n\tprivate readonly string userName;\n\n\tprivate void Build()\n\t{\n\t\tstring servicePassword = Environment.GetEnvironmentVariable(\"SERVICE_PWD\");\n\t\tCredentials credentials = new UsernamePasswordCredentials(userName, servicePassword);\n\t}\n}\n";

	private const string _INTERACTIVE_FILE_CONTENT =
		"public sealed class InteractiveSignIn\n{\n\tprivate readonly string userName;\n\n\tprivate void Build()\n\t{\n\t\tConsole.Write(\"Enter your password: \");\n\t\tstring enteredPassword = Console.ReadLine();\n\t\tCredentials credentials = new UsernamePasswordCredentials(userName, enteredPassword);\n\t}\n}\n";

	private const string _PLAIN_FILE_CONTENT =
		"public sealed class PlainClient\n{\n\tprivate readonly string userName;\n\tprivate readonly string password;\n\n\tprivate void Build()\n\t{\n\t\tCredentials credentials = new UsernamePasswordCredentials(userName, password);\n\t}\n}\n";

	/// <summary>The three files, as (relative path, content), in the order a report lists their findings.</summary>
	internal static ImmutableArray<(string RelativePath, string Content)> AllFiles { get; } =
	[
		(FIXED_FILE_NAME, _FIXED_FILE_CONTENT),
		(INTERACTIVE_FILE_NAME, _INTERACTIVE_FILE_CONTENT),
		(PLAIN_FILE_NAME, _PLAIN_FILE_CONTENT)
	];

	/// <summary>Only the interactive file and the plain one, for the claim that needs exactly those two.</summary>
	internal static ImmutableArray<(string RelativePath, string Content)> InteractiveAndPlainFiles { get; } =
	[
		(INTERACTIVE_FILE_NAME, _INTERACTIVE_FILE_CONTENT),
		(PLAIN_FILE_NAME, _PLAIN_FILE_CONTENT)
	];
}
