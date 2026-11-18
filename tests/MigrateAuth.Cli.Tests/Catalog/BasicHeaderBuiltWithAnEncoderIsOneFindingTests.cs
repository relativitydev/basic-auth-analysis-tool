using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// A Basic header whose value is built with an encoder call on the same line is one finding, the header class at High, and the encoder inside it is not reported again. The classes are shared by both languages, so
/// every case runs through both recognition harnesses. The header class over the encoder class is declared in the catalog (the header class's match covers the encoder call, so the two spans overlap and the encoder
/// finding is subsumed); the cases that must stay two findings, or one encoder finding, are pinned here as well.
/// </summary>
public sealed class BasicHeaderBuiltWithAnEncoderIsOneFindingTests
{
	private const string _HEADER_CLASS = "JS-004";
	private const string _ENCODER_CLASS = "JS-005";
	private const int _FINDINGS_OF_A_SPLIT_CONSTRUCTION = 2;

	public static TheoryData<string> HeaderWritesWithAnEncoderOnOneLine => new()
	{
		"headers['Authorization'] = 'Basic ' + btoa(userName + ':' + secret);\n",
		"xhr.setRequestHeader('Authorization', 'Basic ' + btoa(`${userName}:${secret}`));\n",
		"headers.set('Authorization', 'Basic ' + btoa(userName + ':' + secret));\n",
		"const requestOptions = { Authorization: 'Basic ' + btoa(userName + ':' + secret) };\n",
		"const templateOptions = { Authorization: `Basic ${btoa(userName + ':' + secret)}` };\n",
		"await fetch(endpointUrl, { headers: { Authorization: 'Basic ' + btoa(`${userName}:${secret}`) } });\n",
		"await fetch(endpointUrl, { headers: { Authorization: 'Basic ' + btoa(userName + ':' + secret) } });\n",
		"await fetch(endpointUrl, { headers: { Authorization: `Basic ${btoa(`${userName}:${secret}`)}` } });\n",
		"fetch(url,{headers:{Authorization:\"Basic \"+btoa(user+\":\"+pass)}})\n",
		"fetch(url,{headers:{Authorization:`Basic ${btoa(`${user}:${pass}`)}`}})\n",
		"await fetch(endpointUrl, { headers: { Authorization: \"Basic \" + btoa([userName, secret].join(':')) } });\n",
		"headers['Authorization'] = 'Basic ' + btoa('exampleUser:exampleSecret');\n",
		"headers['Authorization'] = 'Basic ' + btoa('exampleUser:exampleSecret' + suffix);\n",
		"headers['Authorization'] = 'Basic ' + btoa('exampleUser:' + 'exampleSecret');\n",
		"headers['Authorization'] = 'Basic ' + window.btoa(userName + ':' + secret);\n",
		"headers.append('Authorization', 'Basic ' + btoa(userName + ':' + secret));\n",
		"headers.Authorization = 'Basic ' + btoa(userName + ':' + secret);\n",
		"headers.common['Authorization'] = `Basic ${btoa(userName + ':' + secret)}`;\n",
		"const response = await fetch(endpointUrl, { headers: new Headers([['Authorization', 'Basic ' + btoa(userName + ':' + secret)]]) });\n",
		"const response = await fetch(endpointUrl, { headers: new Headers({ Authorization: 'Basic ' + btoa(userName + ':' + secret) }) });\n",
		"const options = { headers: { 'Authorization': 'Basic ' + btoa(userName + ':' + secret) } };\n",
		"const options = {\n\theaders: {\n\t\tAuthorization: 'Basic ' + btoa(userName + ':' + secret),\n\t\tAccept: 'text/plain'\n\t}\n};\n"
	};

	public static TheoryData<string> TypedHeaderWritesWithAnEncoderOnOneLine => new()
	{
		"const typedHeaders: Record<string, string> = { Authorization: 'Basic ' + btoa(userName + ':' + secret) };\n",
		"const angularHeaders = new HttpHeaders({ Authorization: 'Basic ' + btoa(userName + ':' + secret) });\n",
		"const angularHeaders = headers.set('Authorization', 'Basic ' + btoa(`${userName}:${secret}`));\n",
		"const angularHeaders = headers.append('Authorization', 'Basic ' + btoa(userName + ':' + secret));\n",
		"const options = { setHeaders: { Authorization: `Basic ${btoa(userName + ':' + secret)}` } };\n"
	};

	[Theory]
	[MemberData(nameof(HeaderWritesWithAnEncoderOnOneLine))]
	public void ABasicHeaderBuiltWithAnEncoderOnOneLineIsOneHeaderFindingInJavaScript(string source)
	{
		AssertOneHeaderFinding(new JavaScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[MemberData(nameof(HeaderWritesWithAnEncoderOnOneLine))]
	[MemberData(nameof(TypedHeaderWritesWithAnEncoderOnOneLine))]
	public void ABasicHeaderBuiltWithAnEncoderOnOneLineIsOneHeaderFindingInTypeScript(string source)
	{
		AssertOneHeaderFinding(new TypeScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("const pair = btoa(userName + ':' + secret);\nheaders['Authorization'] = 'Basic ' + pair;\n")]
	[InlineData("const pair = btoa(`${userName}:${secret}`);\nheaders.set('Authorization', `Basic ${pair}`);\n")]
	[InlineData("headers['Authorization'] = 'Basic ' + token;\nconst other = btoa(userName + ':' + secret);\n")]
	public void AnEncoderInAnotherStatementThanTheHeaderIsTwoFindingsInJavaScript(string source)
	{
		AssertEncoderAndHeaderFindings(new JavaScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("const pair = btoa(userName + ':' + secret);\nheaders['Authorization'] = 'Basic ' + pair;\n")]
	[InlineData("const pair = btoa(`${userName}:${secret}`);\nheaders.set('Authorization', `Basic ${pair}`);\n")]
	[InlineData("headers['Authorization'] = 'Basic ' + token;\nconst other = btoa(userName + ':' + secret);\n")]
	public void AnEncoderInAnotherStatementThanTheHeaderIsTwoFindingsInTypeScript(string source)
	{
		AssertEncoderAndHeaderFindings(new TypeScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("const header = 'Basic ' + btoa(userName + ':' + secret);\n")]
	[InlineData("const header = `Basic ${btoa(userName + ':' + secret)}`;\n")]
	[InlineData("headers['Authorization'] = 'Bearer ' + btoa(userName + ':' + secret);\n")]
	public void AnEncoderThatNoBasicHeaderWriteHoldsIsOneEncoderFindingInJavaScript(string source)
	{
		AssertOnlyTheEncoderFinding(new JavaScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("const header = 'Basic ' + btoa(userName + ':' + secret);\n")]
	[InlineData("const header = `Basic ${btoa(userName + ':' + secret)}`;\n")]
	[InlineData("headers['Authorization'] = 'Bearer ' + btoa(userName + ':' + secret);\n")]
	public void AnEncoderThatNoBasicHeaderWriteHoldsIsOneEncoderFindingInTypeScript(string source)
	{
		AssertOnlyTheEncoderFinding(new TypeScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("const key = btoa(host + ':' + port);\n")]
	[InlineData("const key = btoa(hostName + ':' + portNumber);\n")]
	public void AnEncoderOfANonCredentialPairIsNotReadInJavaScript(string source)
	{
		Assert.Empty(new JavaScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("const key = btoa(host + ':' + port);\n")]
	[InlineData("const key = btoa(hostName + ':' + portNumber);\n")]
	public void AnEncoderOfANonCredentialPairIsNotReadInTypeScript(string source)
	{
		Assert.Empty(new TypeScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("const headers = { Authorization: `Basic ${Buffer.from(`${userName}:${secret}`).toString('base64')}` };\n")]
	[InlineData("headers['Authorization'] = 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64');\n")]
	public void ABufferEncoderInABasicHeaderValueKeepsItsOneHeaderFindingInJavaScript(string source)
	{
		AssertOneHeaderFinding(new JavaScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("const headers = { Authorization: `Basic ${Buffer.from(`${userName}:${secret}`).toString('base64')}` };\n")]
	[InlineData("headers['Authorization'] = 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64');\n")]
	public void ABufferEncoderInABasicHeaderValueKeepsItsOneHeaderFindingInTypeScript(string source)
	{
		AssertOneHeaderFinding(new TypeScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("headers['Authorization'] = 'Basic ' + btoa('exampleUser:exampleSecret');\n")]
	[InlineData("headers['Authorization'] = `Basic ${btoa('exampleUser:exampleSecret')}`;\n")]
	public void TheHeaderFindingOfAnEncodedConstantCarriesNoWarningInJavaScript(string source)
	{
		AssertOneHeaderFindingWithNoLiteralCredentialSignal(new JavaScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("headers['Authorization'] = 'Basic ' + btoa('exampleUser:exampleSecret');\n")]
	[InlineData("headers['Authorization'] = `Basic ${btoa('exampleUser:exampleSecret')}`;\n")]
	public void TheHeaderFindingOfAnEncodedConstantCarriesNoWarningInTypeScript(string source)
	{
		AssertOneHeaderFindingWithNoLiteralCredentialSignal(new TypeScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("headers['Authorization'] = 'Basic ' + separator + btoa(userName + ':' + secret);\n")]
	[InlineData("headers.set('Authorization', 'Basic ' + separator + btoa(`${userName}:${secret}`));\n")]
	public void AnEncoderSeparatedFromTheBasicLiteralByAnotherOperandIsTwoFindingsInJavaScript(string source)
	{
		AssertEncoderAndHeaderFindings(new JavaScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("headers['Authorization'] = 'Basic ' + separator + btoa(userName + ':' + secret);\n")]
	[InlineData("headers.set('Authorization', 'Basic ' + separator + btoa(`${userName}:${secret}`));\n")]
	public void AnEncoderSeparatedFromTheBasicLiteralByAnotherOperandIsTwoFindingsInTypeScript(string source)
	{
		AssertEncoderAndHeaderFindings(new TypeScriptRecognitionHarness().Recognize(source));
	}

	private static void AssertOneHeaderFindingWithNoLiteralCredentialSignal(ImmutableArray<RecognizedConstruct> constructs)
	{
		RecognizedConstruct construct = Assert.Single(constructs);

		Assert.Equal(_HEADER_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
	}

	private static void AssertOneHeaderFinding(ImmutableArray<RecognizedConstruct> constructs)
	{
		RecognizedConstruct construct = Assert.Single(constructs);

		Assert.Equal(_HEADER_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	private static void AssertEncoderAndHeaderFindings(ImmutableArray<RecognizedConstruct> constructs)
	{
		Assert.Equal(_FINDINGS_OF_A_SPLIT_CONSTRUCTION, constructs.Length);
		Assert.Equal(ConfidenceLevel.Medium, Assert.Single(constructs, candidate => candidate.PatternClassIdentifier == _ENCODER_CLASS).Level);
		Assert.Equal(ConfidenceLevel.High, Assert.Single(constructs, candidate => candidate.PatternClassIdentifier == _HEADER_CLASS).Level);
	}

	private static void AssertOnlyTheEncoderFinding(ImmutableArray<RecognizedConstruct> constructs)
	{
		RecognizedConstruct construct = Assert.Single(constructs);

		Assert.Equal(_ENCODER_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}
}
