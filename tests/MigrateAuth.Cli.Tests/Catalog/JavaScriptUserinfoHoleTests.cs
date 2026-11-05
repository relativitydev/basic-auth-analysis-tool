using System.Collections.Immutable;

using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// A credentialed URL assembled from parts is declared unsupported, and a template with a hole in the password is one: the password of the userinfo is read only where it is a
/// literal, so neither the held-URL class nor the consumer class reads a URL whose password is a hole. The scripts are invented, with placeholder names.
/// </summary>
public sealed class JavaScriptUserinfoHoleTests
{
	[Theory]
	[InlineData("const target = `https://exampleUser:${secret}@host.example/path`;\n")]
	[InlineData("const response = await fetch(`https://exampleUser:${secret}@host.example/path`);\n")]
	[InlineData("http.get(`https://${userName}:${secret}@host.example/path`, handler);\n")]
	public void AUrlWhosePasswordIsAHoleGivesNeitherTheHeldUrlNorTheConsumerFinding(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier is "JS-013" or "JS-003");
	}

	[Fact]
	public void AUrlWhosePasswordIsALiteralIsStillRead()
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize("const target = `https://exampleUser:exampleSecret@host.example/path`;\n");

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == "JS-013");
	}
}
