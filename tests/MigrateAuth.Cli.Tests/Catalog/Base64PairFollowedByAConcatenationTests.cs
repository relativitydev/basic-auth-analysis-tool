using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The encoder class of the shared `JavaScript` and `TypeScript` expressions on an encoder argument that is a literal pair followed by a concatenation, <c>btoa('user:pass' + suffix)</c>. The literal pair is
/// read anywhere in the argument, as the other pair shapes (a template, the colon as its own literal, a join) are. The class is
/// shared by both languages, so every case runs through both recognition harnesses.
/// </summary>
public sealed class Base64PairFollowedByAConcatenationTests
{
	private const string _CLASS = "JS-005";

	[Theory]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:exampleSecret' + suffix);\n")]
	[InlineData("const header = 'Basic ' + btoa(\"exampleUser:exampleSecret\" + suffix);\n")]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:exampleSecret' + suffix + more);\n")]
	[InlineData("const header = 'Basic ' + btoa( 'exampleUser:exampleSecret'+suffix );\n")]
	[InlineData("headers.set('Authorization', 'Basic ' + btoa('exampleUser:exampleSecret' + suffix));\n")]
	public void ALiteralPairFollowedByAConcatenationIsReadAtMediumWithNoLiteralCredentialInJavaScript(string source)
	{
		AssertReadWithNoLiteralCredential(new JavaScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("const header: string = 'Basic ' + btoa('exampleUser:exampleSecret' + suffix);\n")]
	[InlineData("const header: string = 'Basic ' + btoa(\"exampleUser:exampleSecret\" + suffix);\n")]
	[InlineData("const header: string = 'Basic ' + btoa('exampleUser:exampleSecret' + suffix + more);\n")]
	[InlineData("const header: string = 'Basic ' + btoa( 'exampleUser:exampleSecret'+suffix );\n")]
	[InlineData("headers.set('Authorization', 'Basic ' + btoa('exampleUser:exampleSecret' + suffix));\n")]
	public void ALiteralPairFollowedByAConcatenationIsReadAtMediumWithNoLiteralCredentialInTypeScript(string source)
	{
		AssertReadWithNoLiteralCredential(new TypeScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:exampleSecret');\n", true)]
	[InlineData("const header = 'Basic ' + btoa( \"exampleUser:exampleSecret\" );\n", true)]
	[InlineData("const header = 'Basic ' + btoa(`${userName}:${secret}`);\n", false)]
	[InlineData("const header = 'Basic ' + btoa(userName + ':' + secret + suffix);\n", false)]
	[InlineData("const header = 'Basic ' + btoa(`${userName}:${secret}` + suffix);\n", false)]
	public void ThePairShapesThatWereAlreadyReadKeepTheirReadingInJavaScript(string source, bool expectedSignal)
	{
		RecognizedConstruct construct = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == _CLASS);

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.Equal(expectedSignal, construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("const header: string = 'Basic ' + btoa('exampleUser:exampleSecret');\n", true)]
	[InlineData("const header: string = 'Basic ' + btoa( \"exampleUser:exampleSecret\" );\n", true)]
	[InlineData("const header: string = 'Basic ' + btoa(`${userName}:${secret}`);\n", false)]
	[InlineData("const header: string = 'Basic ' + btoa(userName + ':' + secret + suffix);\n", false)]
	[InlineData("const header: string = 'Basic ' + btoa(`${userName}:${secret}` + suffix);\n", false)]
	public void ThePairShapesThatWereAlreadyReadKeepTheirReadingInTypeScript(string source, bool expectedSignal)
	{
		RecognizedConstruct construct = Assert.Single(new TypeScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == _CLASS);

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.Equal(expectedSignal, construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("const key = btoa('exampleHost:8080' + suffix);\n")]
	[InlineData("const header = 'Basic ' + btoa('exampleUser' + suffix);\n")]
	[InlineData("const header = 'Basic ' + btoa(userName + suffix);\n")]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:exampleSecret', suffix);\n")]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:exampleSecret' - suffix);\n")]
	[InlineData("const header = 'Basic ' + btoa(JSON.stringify(payload) + suffix);\n")]
	public void AConcatenationThatIsNotACredentialPairIsStillNotReadInJavaScript(string source)
	{
		Assert.DoesNotContain(new JavaScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == _CLASS);
	}

	[Theory]
	[InlineData("const key: string = btoa('exampleHost:8080' + suffix);\n")]
	[InlineData("const header: string = 'Basic ' + btoa('exampleUser' + suffix);\n")]
	[InlineData("const header: string = 'Basic ' + btoa(userName + suffix);\n")]
	[InlineData("const header: string = 'Basic ' + btoa('exampleUser:exampleSecret', suffix);\n")]
	[InlineData("const header: string = 'Basic ' + btoa('exampleUser:exampleSecret' - suffix);\n")]
	[InlineData("const header: string = 'Basic ' + btoa(JSON.stringify(payload) + suffix);\n")]
	public void AConcatenationThatIsNotACredentialPairIsStillNotReadInTypeScript(string source)
	{
		Assert.DoesNotContain(new TypeScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == _CLASS);
	}

	private static void AssertReadWithNoLiteralCredential(ImmutableArray<RecognizedConstruct> constructs)
	{
		RecognizedConstruct construct = Assert.Single(constructs, candidate => candidate.PatternClassIdentifier == _CLASS);

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
	}
}
