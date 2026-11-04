using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE PROXY REFUSAL AND THE LITERAL RULE OF THE `JavaScript` CLASSES. (1) A user-information address held under a name or a key that contains
/// `proxy` or `proxies`, in any letter case, is a credential for a proxy and not for the service, and `JS-013` refuses it: no finding at any level. A target address held
/// under a neutral name is still read at Medium. (2) A quoted or hole-free template literal is a credential literal only when it is the whole value: when anything but a
/// value terminator follows its closing delimiter, the finding stays at its level and the in-code secret warning is not raised. Every script is invented with placeholder
/// names, through the shipped analyzer and the real recognition engine.
/// </summary>
public sealed class JavaScriptProxyUrlAndLiteralRuleTests
{
	private const string _USERINFO_URL_CLASS = "JS-013";

	// ---- The proxy refusal ----

	[Theory]
	[InlineData("const proxyUrl = 'http://exampleUser:exampleSecret@proxy.example:3128';\n")]
	[InlineData("send({ url: target, proxy: 'http://exampleUser:exampleSecret@proxy.example:3128' });\n")]
	[InlineData("const agentOptions = { httpsProxy: 'http://exampleUser:exampleSecret@proxy.example:3128' };\n")]
	[InlineData("const PROXY_URL = 'http://exampleUser:exampleSecret@proxy.example:3128';\n")]
	[InlineData("const https_proxy = 'http://exampleUser:exampleSecret@proxy.example:3128';\n")]
	[InlineData("const options = { proxies: 'http://exampleUser:exampleSecret@proxy.example:3128' };\n")]
	[InlineData("const HttpProxyEndpoint = `http://exampleUser:exampleSecret@proxy.example:3128`;\n")]
	[InlineData("axios.get(url, { proxy: 'http://exampleUser:exampleSecret@proxy.example:3128' });\n")]
	public void AUserinfoAddressHeldUnderANameThatContainsProxyIsNotReportedAtAnyLevel(string source)
	{
		Assert.Empty(new JavaScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("const baseUrl = 'https://exampleUser:exampleSecret@host.example/path';\n")]
	[InlineData("const options = { baseURL: 'https://exampleUser:exampleSecret@host.example' };\n")]
	[InlineData("const serviceUrl = 'https://exampleUser:exampleSecret@host.example/path';\n")]
	[InlineData("const endpoint = `https://exampleUser:exampleSecret@host.example/path`;\n")]
	[InlineData("const egressUrl = 'http://exampleUser:exampleSecret@proxy.example:3128';\n")]
	[InlineData("const gatewayUrl = 'http://exampleUser:exampleSecret@gateway.example:3128';\n")]
	public void AUserinfoAddressHeldUnderANeutralNameIsStillReadAtMedium(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _USERINFO_URL_CLASS && construct.Level == ConfidenceLevel.Medium);
	}

	[Fact]
	public void ATargetAddressGivenDirectlyToARequestCallIsStillTheCallsClassAndNotTheProxyRefusals()
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize("const response = await fetch('https://exampleUser:exampleSecret@host.example/path');\n");

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == "JS-003" && construct.Level == ConfidenceLevel.High);
	}

	// ---- The literal rule ----

	[Theory]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'exampleSecret' } });\n", true)]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'exampleSecret', } });\n", true)]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'exampleSecret' // note\n} });\n", true)]
	[InlineData("axios.get(url, { auth: { username: userName, password: `exampleSecret` } });\n", true)]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'prefix' + suffix } });\n", false)]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'a' + 'b' } });\n", false)]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'prefix'.concat(suffix) } });\n", false)]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'prefix'[0] } });\n", false)]
	[InlineData("axios.get(url, { auth: { username: userName, password: `prefix` + suffix } });\n", false)]
	public void AnOptionMemberCountsAsALiteralCredentialOnlyWhenTheLiteralIsTheWholeValue(string source, bool expectedSignal)
	{
		RecognizedConstruct construct = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == "JS-001");

		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(expectedSignal, construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("await superagent.get(url).auth(userName, 'exampleSecret');\n", true)]
	[InlineData("await superagent.get(url).auth(userName, 'prefix' + suffix);\n", false)]
	[InlineData("const response = await got(url, { username: userName, password: 'exampleSecret' });\n", true)]
	[InlineData("const response = await got(url, { username: userName, password: 'prefix' + suffix });\n", false)]
	public void AnArgumentOfAnAuthCallAndAnOptionOfAClientCountAsALiteralCredentialOnlyWhenWhole(string source, bool expectedSignal)
	{
		RecognizedConstruct construct = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier is "JS-002" or "JS-015");

		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(expectedSignal, construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("await fetch(url, { headers: { Authorization: 'Basic dXNlcjpwYXNz' } });\n", true)]
	[InlineData("await fetch(url, { headers: { Authorization: 'Basic dXNlcjpwYXNz', Accept: 'x' } });\n", true)]
	[InlineData("await fetch(url, { headers: { Authorization: 'Basic dXNlcjpwYXNz' + suffix } });\n", false)]
	[InlineData("await fetch(url, { headers: { Authorization: `Basic dXNlcjpwYXNz`.trim() } });\n", false)]
	public void ABasicHeaderTokenCountsAsALiteralCredentialOnlyWhenTheLiteralIsTheWholeValue(string source, bool expectedSignal)
	{
		RecognizedConstruct construct = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == "JS-004");

		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(expectedSignal, construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("const baseUrl = 'https://exampleUser:exampleSecret@host.example/path';\n", true)]
	[InlineData("const baseUrl = `https://exampleUser:exampleSecret@host.example/path`;\n", true)]
	[InlineData("const options = { baseURL: 'https://exampleUser:exampleSecret@host.example' };\n", true)]
	[InlineData("const baseUrl = 'https://exampleUser:exampleSecret@host.example/path' + suffix;\n", false)]
	[InlineData("const baseUrl = 'https://exampleUser:exampleSecret@host.example/path'.concat(suffix);\n", false)]
	[InlineData("const baseUrl = `https://exampleUser:exampleSecret@${host}/path`;\n", false)]
	[InlineData("const baseUrl = `https://exampleUser:exampleSecret@host.example/${path}`;\n", false)]
	[InlineData("const baseUrl = `https://exampleUser:exampleSecret@host.example/$path`;\n", true)]
	public void AUserinfoAddressCountsAsALiteralCredentialOnlyWhenTheAddressLiteralIsTheWholeValue(string source, bool expectedSignal)
	{
		RecognizedConstruct construct = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == _USERINFO_URL_CLASS);

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.Equal(expectedSignal, construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("const header = 'Basic ' + btoa('user:examplePass');\n", true)]
	[InlineData("const header = 'Basic ' + btoa('user:examplePass' + [a, b].join(':'));\n", false)]
	public void AnEncoderPairCountsAsALiteralCredentialOnlyWhenTheLiteralIsTheWholeArgument(string source, bool expectedSignal)
	{
		RecognizedConstruct construct = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == "JS-005");

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.Equal(expectedSignal, construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("const target = new URL(base);\ntarget.username = userName;\ntarget.password = 'exampleSecret';\n", true)]
	[InlineData("const target = new URL(base);\ntarget.username = userName;\ntarget.password = 'prefix' + suffix;\n", false)]
	public void ASetterOfAUrlObjectCountsAsALiteralCredentialOnlyWhenTheLiteralIsTheWholeValue(string source, bool expectedSignal)
	{
		RecognizedConstruct construct = new JavaScriptRecognitionHarness().Recognize(source).Where(candidate => candidate.PatternClassIdentifier == "JS-010").OrderBy(candidate => candidate.AnchorOffset).Last();

		Assert.Equal(expectedSignal, construct.LiteralCredentialSignal);
	}
}
