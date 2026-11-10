using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// TWO RULES OF THE `JavaScript` CLASSES (the shorthand member and the split pair). (1) `JS-001`: a member of the `auth` object written as a bare name (shorthand property) is a member,
/// directly after the opening brace or after a comma at depth one; a name that is the value of another key, or that sits in a nested group, is not. No credential is captured for it. (2) `JS-005`: inside the
/// encoder call, a quoted literal whose last character is the colon, a plus and a second quoted literal is read at Medium with no credential; a literal ending in a colon followed by code is not read (declared).
/// Every script is invented with placeholder names, through the shipped analyzer and the real recognition engine. Each rule has its fixtures, its neighbour controls and both outcomes.
/// </summary>
public sealed class JavaScriptShorthandMemberAndSplitPairTests
{
	private const string _CLIENT_OBJECT_CLASS = "JS-001";
	private const string _PAIR_CLASS = "JS-005";
	private const string _BUFFER_ENCODER_CLASS = "JS-006";
	private const int _UNRELATED_LINES_BEYOND_THE_WINDOW = 12;

	// ---- JS-001: a shorthand member is a member ----

	[Theory]
	[InlineData("const response = await axios.get(url, { auth: { username, password } });\n")]
	[InlineData("const client = axios.create({ baseURL: host, auth: { username: userName, password } });\n")]
	[InlineData("const client = axios.create({ baseURL: host, auth: { username, password: secret } });\n")]
	[InlineData("const response = await axios.get(url, { auth: {\n\tusername,\n\tpassword\n} });\n")]
	[InlineData("const response = await axios.get(url, { auth: { password, username } });\n")]
	[InlineData("axios.defaults.auth = { username, password };\n")]
	[InlineData("request.get(url, { auth: { user: userName, pass } });\n")]
	[InlineData("request(url, { auth: { user, pass } });\n")]
	public void AShorthandMemberIsReadAsAMemberOfTheAuthObjectAtHigh(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs, candidate => candidate.PatternClassIdentifier == _CLIENT_OBJECT_CLASS);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
	}

	[Fact]
	public void AShorthandUserWithALiteralPasswordRaisesTheSecretWarningForTheLiteralOnly()
	{
		const string source = "await axios.get(url, { auth: { username, password: 'exampleSecret' } });\n";

		RecognizedConstruct construct = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == _CLIENT_OBJECT_CLASS);

		Assert.True(construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("await axios.post(url, { username, password });\n")]
	[InlineData("await axios.get(url, { auth: { username } });\n")]
	[InlineData("await axios.get(url, { auth: { password } });\n")]
	[InlineData("await axios.get(url, { auth: { label: username, password: secret } });\n")]
	[InlineData("await axios.get(url, { auth: { label: username, password } });\n")]
	[InlineData("await axios.get(url, { auth: { token, nested: { username, password } } });\n")]
	[InlineData("await axios.get(url, { auth: { username, password: secret, sendImmediately: false } });\n")]
	[InlineData("await axios.get(url, { auth: { username, userNameValue } });\n")]
	[InlineData("await axios.get(url, { auth: { username.value, password } });\n")]
	[InlineData("await client.get(url, { auth: { username, password } });\n")]
	public void ANeighbourOfAShorthandPairIsNotReadAsTheClientAuthObject(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _CLIENT_OBJECT_CLASS);
	}

	[Theory]
	[InlineData("await axios.get(url, { auth: { username: userName, password: secret } });\n")]
	[InlineData("await axios.get(url, { auth: { username: 'exampleUser', password: 'exampleSecret' } });\n")]
	public void TheColonFormIsStillReadAsBefore(string source)
	{
		Assert.Contains(new JavaScriptRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _CLIENT_OBJECT_CLASS && construct.Level == ConfidenceLevel.High);
	}

	// ---- JS-005: a pair split across two literals ----

	[Theory]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:' + 'exampleSecret');\n")]
	[InlineData("const line = 'Basic ' + btoa('exampleUser:' + 'exampleSecret' + extra + this.stamp);\n")]
	[InlineData("headers.Authorization = btoa(\"exampleUser:\" + \"exampleSecret\");\n")]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:'\n\t+ 'exampleSecret');\n")]
	public void APairSplitAcrossTwoLiteralsIsReadAtMediumAndRaisesNoSecretWarning(string source)
	{
		RecognizedConstruct construct = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == _PAIR_CLASS);

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("const key = btoa('cache:' + 'entries');\n")]
	[InlineData("const pair = btoa('exampleUser' + 'exampleSecret');\n")]
	[InlineData("const header = 'Basic ' + btoa('exampleUser' + 'exampleSecret');\n")]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:' + secret);\n")]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:' + '');\n")]
	public void ANeighbourOfASplitPairIsNotReadAsTheEncoderPair(string source)
	{
		Assert.DoesNotContain(new JavaScriptRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _PAIR_CLASS);
	}

	[Fact]
	public void TheSplitPairWithNeitherUseWordWithinTheWindowIsNotRead()
	{
		string unrelatedLines = string.Concat(Enumerable.Repeat("const unrelated = compute();\n", _UNRELATED_LINES_BEYOND_THE_WINDOW));
		string source = $"const key = btoa('cache:' + 'entries');\n{unrelatedLines}const header = 'Basic ' + other;\n";

		Assert.DoesNotContain(new JavaScriptRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _PAIR_CLASS);
	}

	[Theory]
	[InlineData("const header = 'Basic ' + btoa('user:examplePass');\n", true)]
	[InlineData("const header = 'Basic ' + btoa('user:examplePass' + suffix);\n", false)]
	[InlineData("const header = 'Basic ' + btoa(userName + ':' + secret);\n", false)]
	public void TheOtherEncoderFormsAreReadAsBefore(string source, bool expectedSignal)
	{
		RecognizedConstruct construct = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == _PAIR_CLASS);

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.Equal(expectedSignal, construct.LiteralCredentialSignal);
	}

	[Fact]
	public void TheBufferEncoderIsUnchangedByTheSplitPairAndStillNeedsAWholeLiteralPair()
	{
		const string source = "const header = 'Basic ' + Buffer.from('exampleUser:' + 'exampleSecret').toString('base64');\n";

		Assert.DoesNotContain(new JavaScriptRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _BUFFER_ENCODER_CLASS);
	}
}
