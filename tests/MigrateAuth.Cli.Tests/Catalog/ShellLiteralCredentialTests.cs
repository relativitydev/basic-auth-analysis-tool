using System.Collections.Immutable;

using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE LITERAL-CREDENTIAL SIGNAL OF THE `Shell` ANALYZER, THROUGH THE ENGINE. A literal credential is one of four forms and, for the four classes that read an
/// encoded pair, a Basic token held in a variable and a configuration line the script writes, a fifth: a literal pair or token. Each form has a true fixture
/// and a false one. Every false fixture expands in some part (a variable, a substitution, a backtick, a backslash, a brace, a glob or a leading tilde), has an
/// empty password, or is a value that a <c>read</c> filled. A false fixture is still recognized where the catalog says so: the finding stays and only the signal
/// is false, because the signal is the zone of a literal and never the value.
/// </summary>
public sealed class ShellLiteralCredentialTests
{
	// ------------------------------------------------------------------------------------------
	// Form 1: the password half of a user option's pair
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("curl -u admin:example \"$URL\"\n")]
	[InlineData("curl -sSu admin:example \"$URL\"\n")]
	[InlineData("curl -u 'admin:example' \"$URL\"\n")]
	[InlineData("curl -u \"admin:example\" \"$URL\"\n")]
	[InlineData("curl -u admin:'example' \"$URL\"\n")]
	[InlineData("curl --user admin:example \"$URL\"\n")]
	[InlineData("curl --user=admin:example \"$URL\"\n")]
	[InlineData("http -a admin:example \"$URL\"\n")]
	[InlineData("lwp-request -C admin:example \"$URL\"\n")]
	[InlineData("curl --anyauth -u admin:example \"$URL\"\n")]
	public void APasswordHalfThatIsLiteralSignalsALiteralCredential(string source)
	{
		Assert.Contains(new ShellRecognitionHarness().Recognize(source), construct => construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("curl -u \"$USER_NAME:$USER_SECRET\" \"$URL\"\n")]
	[InlineData("curl -u \"admin:${SECRET}\" \"$URL\"\n")]
	[InlineData("curl -u admin:$SECRET \"$URL\"\n")]
	[InlineData("curl -u \"admin:$(cat secret.txt)\" \"$URL\"\n")]
	[InlineData("curl -u \"admin:`cat secret.txt`\" \"$URL\"\n")]
	[InlineData("curl -u admin:ex\\ ample \"$URL\"\n")]
	[InlineData("curl -u admin:{a,b} \"$URL\"\n")]
	[InlineData("curl -u admin:*.txt \"$URL\"\n")]
	[InlineData("curl -u ~admin:example \"$URL\"\n")]
	[InlineData("curl -u admin: \"$URL\"\n")]
	[InlineData("curl -u 'admin:' \"$URL\"\n")]
	[InlineData("read -s PASS\ncurl -u \"admin:$PASS\" \"$URL\"\n")]
	[InlineData("http -a \"$U:$P\" \"$URL\"\n")]
	[InlineData("lwp-request -C \"$U:$P\" \"$URL\"\n")]
	public void AnExpandingEmptyOrReadPasswordHalfDoesNotSignalALiteralCredential(string source)
	{
		Assert.DoesNotContain(new ShellRecognitionHarness().Recognize(source), construct => construct.LiteralCredentialSignal);
	}

	[Fact]
	public void AGatedPositiveIsStillAFindingEvenThoughItsSignalIsFalse()
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize("curl -u \"$USER_NAME:$USER_SECRET\" \"$URL\"\n");

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal("SH-001", construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// Form 2: the password in a URL's userinfo
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("curl https://admin:example@host.test/x\n")]
	[InlineData("curl \"https://admin:example@host.test/x\"\n")]
	[InlineData("wget --auth-no-challenge https://admin:example@host.test/x\n")]
	[InlineData("wget https://admin:example@host.test/x\n")]
	[InlineData("API_URL=\"https://admin:example@host.test/x\"\ncurl -sS \"$API_URL\"\n")]
	[InlineData("http \"https://admin:example@host.test/x\"\n")]
	[InlineData("aria2c \"https://admin:example@host.test/x\"\n")]
	public void APasswordInAUrlThatIsLiteralSignalsALiteralCredential(string source)
	{
		Assert.Contains(new ShellRecognitionHarness().Recognize(source), construct => construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("curl \"https://admin:${SECRET}@host.test/x\"\n")]
	[InlineData("curl \"https://${USER_NAME}:${SECRET}@host.test/x\"\n")]
	[InlineData("curl https://admin:$SECRET@host.test/x\n")]
	[InlineData("curl \"https://admin:$(cat secret.txt)@host.test/x\"\n")]
	[InlineData("curl \"https://admin:`cat secret.txt`@host.test/x\"\n")]
	[InlineData("curl https://admin:ex\\ ample@host.test/x\n")]
	[InlineData("wget --auth-no-challenge \"https://admin:${SECRET}@host.test/x\"\n")]
	[InlineData("API_URL=\"https://${U}:${P}@host.test/x\"\ncurl -sS \"$API_URL\"\n")]
	public void AnExpandingPasswordInAUrlDoesNotSignalALiteralCredential(string source)
	{
		Assert.DoesNotContain(new ShellRecognitionHarness().Recognize(source), construct => construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// Form 3: the token of a literal Authorization: Basic header
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("curl -H \"Authorization: Basic dXNlcjpwYXNz\" \"$URL\"\n")]
	[InlineData("curl --header 'authorization: basic dXNlcjpwYXNz' \"$URL\"\n")]
	[InlineData("wget --header=\"Authorization: Basic dXNlcjpwYXNz\" \"$URL\"\n")]
	[InlineData("curl --expand-header=\"Authorization: Basic dXNlcjpwYXNz\" \"$URL\"\n")]
	[InlineData("aria2c --header=\"Authorization: Basic dXNlcjpwYXNz\" \"$URL\"\n")]
	[InlineData("exec 3<>/dev/tcp/host.test/80\nprintf 'GET /x HTTP/1.1\\r\\nAuthorization: Basic dXNlcjpwYXNz\\r\\n\\r\\n' >&3\n")]
	public void ALiteralBasicTokenSignalsALiteralCredential(string source)
	{
		Assert.Contains(new ShellRecognitionHarness().Recognize(source), construct => construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("curl -H \"Authorization: Basic $ENCODED\" \"$URL\"\n")]
	[InlineData("curl -H \"Authorization: Basic ${ENCODED}\" \"$URL\"\n")]
	[InlineData("curl -H \"Authorization: Basic $(printf '%s:%s' \"$U\" \"$P\" | base64)\" \"$URL\"\n")]
	[InlineData("curl -H \"Authorization: Basic `cat token.txt`\" \"$URL\"\n")]
	[InlineData("exec 3<>/dev/tcp/host.test/80\nprintf 'GET /x HTTP/1.1\\r\\nAuthorization: Basic %s\\r\\n\\r\\n' \"$ENCODED\" >&3\n")]
	public void AnExpandingOrTemplateTokenDoesNotSignalALiteralCredential(string source)
	{
		Assert.DoesNotContain(new ShellRecognitionHarness().Recognize(source), construct => construct.LiteralCredentialSignal);
	}

	/// <summary>Near-misses that fall in none of the false-fixture categories: a token too short to be a credential, and a placeholder of a template engine.</summary>
	[Theory]
	[InlineData("curl -H \"Authorization: Basic ab\" \"$URL\"\n")]
	[InlineData("curl --expand-header 'Authorization: Basic {{creds:b64}}' \"$URL\"\n")]
	public void ATokenTooShortOrATemplatePlaceholderIsANearMissAndDoesNotSignalALiteralCredential(string source)
	{
		Assert.DoesNotContain(new ShellRecognitionHarness().Recognize(source), construct => construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// Form 4: a literal password option
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("wget --auth-no-challenge --user=u --password=example \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge --user u --password example \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge --http-user=u --http-password=example \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge --user=u --http-passwd='example' \"$URL\"\n")]
	[InlineData("wget --user=u --password=example \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge -e http_user=u -e http_password=example \"$URL\"\n")]
	[InlineData("wget -e http_password=example \"$URL\"\n")]
	[InlineData("aria2c --http-user=u --http-passwd=example \"$URL\"\n")]
	public void ALiteralPasswordOptionSignalsALiteralCredential(string source)
	{
		Assert.Contains(new ShellRecognitionHarness().Recognize(source), construct => construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("wget --auth-no-challenge --user=u --password=\"$SECRET\" \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge --user=u --password=${SECRET} \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge --user=u --password=\"$(cat secret.txt)\" \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge --user=u --password=`cat secret.txt` \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge --user=u --password=ex\\ ample \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge --user=u --password=~/secret \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge --user=u --password={a,b} \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge --user=u --password=*.txt \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge --user=u --password= \"$URL\"\n")]
	[InlineData("read -s SECRET\nwget --auth-no-challenge --user=u --password=\"$SECRET\" \"$URL\"\n")]
	[InlineData("wget --auth-no-challenge -e \"http_password=${SECRET}\" \"$URL\"\n")]
	[InlineData("aria2c --http-user=u --http-passwd=\"$SECRET\" \"$URL\"\n")]
	public void AnExpandingEmptyOrReadPasswordOptionDoesNotSignalALiteralCredential(string source)
	{
		Assert.DoesNotContain(new ShellRecognitionHarness().Recognize(source), construct => construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// The fifth form: a literal pair or token that is read by a class which is none of the four
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("SH-007", "ENC=$(echo -n admin:example | base64)\ncurl -H \"Authorization: Basic $ENC\" \"$URL\"\n")]
	[InlineData("SH-007", "ENC=$(printf '%s' 'admin:example' | base64)\ncurl -H \"Authorization: Basic $ENC\" \"$URL\"\n")]
	[InlineData("SH-007", "ENC=$(base64 <<< \"admin:example\")\ncurl -H \"Authorization: Basic $ENC\" \"$URL\"\n")]
	[InlineData("SH-008", "AUTH=\"Basic dXNlcjpwYXNz\"\ncurl -sS -H \"Authorization: ${AUTH}\" \"$URL\"\n")]
	[InlineData("SH-008", "HEADER=\"Authorization: Basic dXNlcjpwYXNz\"\ncurl -sS -H \"${HEADER}\" \"$URL\"\n")]
	[InlineData("SH-019", "cat > \"$cfg\" <<EOF\nuser = \"admin:example\"\nEOF\ncurl -K \"$cfg\" \"$URL\"\n")]
	[InlineData("SH-019", "curl -K - \"$URL\" <<EOF\nuser = \"admin:example\"\nEOF\n")]
	[InlineData("SH-019", "cat > \"$cfg\" <<EOF\nheader = \"Authorization: Basic dXNlcjpwYXNz\"\nEOF\ncurl -K \"$cfg\" \"$URL\"\n")]
	[InlineData("SH-020", "cat > \"$rc\" <<EOF\nhttp_user=u\nhttp_password=example\nEOF\nexport WGETRC=\"$rc\"\n")]
	[InlineData("SH-020", "printf 'http_password = example\\n' > \"$rc\"\nexport WGETRC=\"$rc\"\n")]
	public void ALiteralPairOrTokenInAFifthFormClassSignalsALiteralCredential(string patternClass, string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == patternClass && construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("SH-007", "ENC=$(echo -n \"$U:$P\" | base64)\ncurl -H \"Authorization: Basic $ENC\" \"$URL\"\n")]
	[InlineData("SH-007", "ENC=$(printf '%s:%s' \"$U\" \"$P\" | base64)\ncurl -H \"Authorization: Basic $ENC\" \"$URL\"\n")]
	[InlineData("SH-007", "ENC=$(echo -n admin:$P | base64)\ncurl -H \"Authorization: Basic $ENC\" \"$URL\"\n")]
	[InlineData("SH-007", "ENC=$(base64 <<< \"$U:$P\")\ncurl -H \"Authorization: Basic $ENC\" \"$URL\"\n")]
	[InlineData("SH-008", "AUTH=\"Basic ${ENCODED}\"\ncurl -sS -H \"Authorization: ${AUTH}\" \"$URL\"\n")]
	[InlineData("SH-008", "HEADER=\"Authorization: Basic $(make_token)\"\ncurl -sS -H \"${HEADER}\" \"$URL\"\n")]
	[InlineData("SH-008", "SCHEME=Basic\ncurl -sS -H \"Authorization: $SCHEME $TOKEN\" \"$URL\"\n")]
	[InlineData("SH-019", "cat > \"$cfg\" <<EOF\nuser = \"${U}:${P}\"\nEOF\ncurl -K \"$cfg\" \"$URL\"\n")]
	[InlineData("SH-019", "printf 'user = \"%s:%s\"\\n' \"$U\" \"$P\" > \"$cfg\"\ncurl -K \"$cfg\" \"$URL\"\n")]
	[InlineData("SH-019", "cat > \"$cfg\" <<EOF\nheader = \"Authorization: Basic ${TOKEN}\"\nEOF\ncurl -K \"$cfg\" \"$URL\"\n")]
	[InlineData("SH-020", "printf 'http_password = %s\\n' \"$P\" > \"$rc\"\nexport WGETRC=\"$rc\"\n")]
	[InlineData("SH-020", "echo \"password=$P\" >> \"$rc\"\nexport WGETRC=\"$rc\"\n")]
	[InlineData("SH-020", "cat > \"$rc\" <<EOF\nhttp_user=u\nEOF\nexport WGETRC=\"$rc\"\n")]
	public void AnExpandingOrAbsentValueInAFifthFormClassDoesNotSignalALiteralCredentialButIsStillRecognized(string patternClass, string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == patternClass);
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == patternClass && construct.LiteralCredentialSignal);
	}

	/// <summary>
	/// A pair encoded inside a client's own header substitution is one finding with the header class as the survivor, and the signal of the pair it suppressed
	/// is carried by the survivor, so the warning is not lost to the collapse.
	/// </summary>
	[Fact]
	public void ALiteralPairEncodedInsideAHeaderSubstitutionSurvivesAsTheHeaderFindingWithTheSignal()
	{
		const string SOURCE_TEXT = "curl -H \"Authorization: Basic $(echo -n admin:example | base64)\" \"$URL\"\n";

		RecognizedConstruct construct = Assert.Single(new ShellRecognitionHarness().Recognize(SOURCE_TEXT));

		Assert.Equal("SH-003", construct.PatternClassIdentifier);
		Assert.True(construct.LiteralCredentialSignal);
	}
}
