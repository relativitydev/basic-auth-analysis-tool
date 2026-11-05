using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The two Base64 classes of the `PowerShell` analyzer as the design states them: the encoder call's own window, the
/// named spellings, the empty half in every spelling a half can take, the precedence of the token class over the pair class, and the tie of a pair to its
/// encoder that decides what is a pair and what the literal-credential signal reports.
///
/// Every script is INVENTED from the catalog's own forms, with placeholder names throughout, and carries no fragment, path or third-party name from any
/// repository.
/// </summary>
public sealed class PowerShellCatalog05Tests
{
	private const string _PS_010 = "PS-010";
	private const string _PS_011 = "PS-011";
	private const string _ENCODE_OPEN = "[Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(";
	private const string _ENCODE_CLOSE = "))";
	private const string _ENCODE_OVER_VARIABLE = "$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))";
	private const string _ENCODE_OVER_BYTES = "$encoded = [Convert]::ToBase64String($bytes)";
	private const int _FAR_FILLER_LINES = 12;
	private const int _NEAR_FILLER_LINES = 8;
	private const string _FILLER_LINE = "$total = 1\n";

	// ------------------------------------------------------------------------------------------
	// The encoder call's own window
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("$encoded = [Convert]::ToBase64String(\n\t[Text.Encoding]::ASCII.GetBytes(\n\t\t\"$($u):$($p)\"\n\t)\n)")]
	[InlineData("$encoded = [Convert]::ToBase64String(`\n\t[Text.Encoding]::ASCII.GetBytes(\"$($u):$($p)\"))")]
	[InlineData("$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(\n\t$u + ':' + $p\n))")]
	[InlineData("$encoded = [Convert]::ToBase64String((Get-Bytes ($a)), [Text.Encoding]::ASCII.GetBytes(\"$($u):$($p)\"))")]
	public void APairInsideTheEncoderCallIsReadAcrossLineBreaksBackticksAndBalancedGroups(string source)
	{
		AssertFires(source, _PS_010);
	}

	[Theory]
	[InlineData("$encoded = [Convert]::ToBase64String(\n\t[Text.Encoding]::ASCII.GetBytes(\n\t\t$null + ':' + $key\n\t)\n)")]
	[InlineData("$encoded = [Convert]::ToBase64String(`\n\t[Text.Encoding]::ASCII.GetBytes(\"$($null):$($key)\"))")]
	public void ATokenPairInsideTheEncoderCallIsReadAcrossLineBreaksAndBackticksToo(string source)
	{
		AssertFires(source, _PS_011);
		AssertNeverFires(source, _PS_010);
	}

	[Theory]
	[InlineData("$encoded = [Convert]::ToBase64String($bytes) + \"$($u):$($p)\"")]
	[InlineData("$encoded = [Convert]::ToBase64String($bytes) + ($u + ':' + $p)")]
	[InlineData("$encoded = [Convert]::ToBase64String($bytes); $label = $u + ':' + $p")]
	[InlineData("$encoded = [Convert]::ToBase64String($bytes)\n$label = \"$($u):$($p)\"")]
	public void APairAfterTheEncoderCallsClosingParenthesisIsNotReadAsInsideIt(string source)
	{
		AssertNeverFires(source, _PS_010);
	}

	[Fact]
	public void ATokenPairAfterTheEncoderCallsClosingParenthesisIsNotReadAsInsideItEither()
	{
		AssertNeverFires("$encoded = [Convert]::ToBase64String($bytes) + \"$($null):$($key)\"", _PS_011);
	}

	// ------------------------------------------------------------------------------------------
	// The named spellings of the pair class
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("\"someone:$secret\"")]
	[InlineData("\"someone:$($credential.GetNetworkCredential().Password)\"")]
	[InlineData("\"$($user):a-secret\"")]
	[InlineData("\"${user}:a-secret\"")]
	[InlineData("\"someone:${secret}\"")]
	[InlineData("'{0}:{1}' -f ($u, $p)")]
	[InlineData("'{0}:{1}' -f @($u, $p)")]
	[InlineData("'{0}:{1}' -f $u, $p")]
	[InlineData("[string]::Concat($u, ':', $p)")]
	[InlineData("[System.String]::Concat($u, ':', $p)")]
	[InlineData("[string]::Join(':', $u, $p)")]
	[InlineData("[System.String]::Join(':', $u, $p)")]
	[InlineData("$u + \u2018:\u2019 + $p")]
	[InlineData("$u + \u201C:\u201D + $p")]
	[InlineData("\u201C$($u):$($p)\u201D")]
	[InlineData("\u2018{0}:{1}\u2019 -f $u, \u2018a-secret\u2019")]
	public void TheNamedPairSpellingsAreReadInsideTheEncoderCall(string pair)
	{
		AssertFires(_ENCODE_OPEN + pair + _ENCODE_CLOSE, _PS_010);
		AssertNeverFires(_ENCODE_OPEN + pair + _ENCODE_CLOSE, _PS_011);
	}

	[Theory]
	[InlineData("\"someone:$secret\"")]
	[InlineData("'{0}:{1}' -f @($u, $p)")]
	[InlineData("[string]::Concat($u, ':', $p)")]
	[InlineData("[string]::Join(':', $u, $p)")]
	[InlineData("$u + \u2018:\u2019 + $p")]
	public void TheNamedSpellingsAreReadWhenAssignedAndReadBackToo(string pair)
	{
		AssertFires("$pair = " + pair + "\n" + _ENCODE_OVER_VARIABLE, _PS_010);
	}

	// ------------------------------------------------------------------------------------------
	// The empty half
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("\"$($null):$($key)\"")]
	[InlineData("\"$(''):$key\"")]
	[InlineData("\"$(\"\"):$key\"")]
	[InlineData("\"$([string]::Empty):$key\"")]
	[InlineData("\"$([System.String]::Empty):$($key)\"")]
	[InlineData("\"${null}:$key\"")]
	[InlineData("\"$null`:$key\"")]
	[InlineData("\"$($key):$($null)\"")]
	[InlineData("\"$($key):$('')\"")]
	[InlineData("\"$($key):$([string]::Empty)\"")]
	[InlineData("\"$key`:${null}\"")]
	[InlineData("\"$key`:$null\"")]
	[InlineData("\"$(''):a-token\"")]
	[InlineData("\"a-token:$('')\"")]
	[InlineData("[string]::Concat($null, ':', $key)")]
	[InlineData("[string]::Join(':', '', $key)")]
	public void AnEmptyHalfInsideAHoleOrBracedOrBeforeABacktickColonIsTheTokenClassNotThePairClass(string pair)
	{
		string source = _ENCODE_OPEN + pair + _ENCODE_CLOSE;

		AssertFires(source, _PS_011);
		AssertNeverFires(source, _PS_010);
	}

	[Theory]
	[InlineData("$blank = ''", "\"$($blank):$($key)\"")]
	[InlineData("$blank = \"\"", "\"${blank}:$key\"")]
	[InlineData("$blank = $null", "\"$blank`:$key\"")]
	[InlineData("$blank = [string]::Empty", "\"$($blank):$($key)\"")]
	[InlineData("$blank = ''", "$blank + ':' + $key")]
	[InlineData("$blank = $null", "'{0}:{1}' -f $blank, $key")]
	[InlineData("$blank = ''", "'{0}:{1}' -f ($blank, $key)")]
	[InlineData("$blank = ''", "[string]::Format('{0}:{1}', $blank, $key)")]
	[InlineData("$blank = ''", "($blank, $key) -join ':'")]
	[InlineData("$blank = ''", "[string]::Join(':', $blank, $key)")]
	[InlineData("$blank = ''", "$key + ':' + $blank")]
	[InlineData("$blank = ''", "\"$($key):$($blank)\"")]
	[InlineData("$blank = $null", "'{0}:{1}' -f $key, $blank")]
	public void AVariableAssignedAnEmptyHalfAndUsedInAnySpellingIsTheTokenClassInsideTheCall(string assignment, string pair)
	{
		string source = assignment + "\n$x = 1\n" + _ENCODE_OPEN + pair + _ENCODE_CLOSE;

		AssertFires(source, _PS_011);
		AssertNeverFires(source, _PS_010);
	}

	[Theory]
	[InlineData("\"$($blank):$($key)\"")]
	[InlineData("$blank + ':' + $key")]
	[InlineData("'{0}:{1}' -f $blank, $key")]
	public void AVariableAssignedAnEmptyHalfAndUsedInAnySpellingIsTheTokenClassWhenThePairIsAssignedAndReadBack(string pair)
	{
		string source = "$blank = ''\n$other = 2\n$pair = " + pair + "\n" + _ENCODE_OVER_VARIABLE;

		AssertFires(source, _PS_011);
		AssertNeverFires(source, _PS_010);
	}

	[Fact]
	public void AnEmptyHalfAssignedMoreThanTenLinesBeforeTheEncoderIsNotReadAndTheConstructIsThePairClass()
	{
		string source = "$blank = ''\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, _FAR_FILLER_LINES)) + _ENCODE_OPEN + "\"$($blank):$($key)\"" + _ENCODE_CLOSE;

		AssertNeverFires(source, _PS_011);
		AssertFires(source, _PS_010);
	}

	[Fact]
	public void AnEmptyHalfThatIsAParameterDefaultIsNotReadAsAnAssignment()
	{
		string source = "param($blank = '')\n" + _ENCODE_OPEN + "\"$($blank):$($key)\"" + _ENCODE_CLOSE;

		AssertNeverFires(source, _PS_011);
		AssertFires(source, _PS_010);
	}

	[Theory]
	[InlineData("$parts = @('', $key)\n$pair = $parts -join ':'\n")]
	[InlineData("$parts = @($null, $key)\n$pair = $parts -join ':'\n")]
	[InlineData("$parts = '', $key\n$pair = $parts -join ':'\n")]
	[InlineData("$parts = @($key, '')\n$pair = $parts -join ':'\n")]
	[InlineData("$parts = $null, $key\n$x = 1\n$pair = $parts -join ':'\n")]
	[InlineData("$parts = [string]::Empty, $key\n$pair = $parts -join ':'\n")]
	public void AnArrayWithAnEmptyElementThatIsJoinedWithAColonIsTheTokenClass(string source)
	{
		string script = source + _ENCODE_OVER_VARIABLE;

		AssertFires(script, _PS_011);
		AssertNeverFires(script, _PS_010);
	}

	[Theory]
	[InlineData("$parts = @($u, $p)\n$pair = $parts -join ':'\n")]
	[InlineData("$parts = $u, $p\n$x = 1\n$pair = $parts -join ':'\n")]
	[InlineData("$parts = 'someone', 'a-secret'\n$pair = $parts -join ':'\n")]
	public void AnArrayOfTwoNonEmptyElementsThatIsJoinedWithAColonIsThePairClass(string source)
	{
		string script = source + _ENCODE_OVER_VARIABLE;

		AssertFires(script, _PS_010);
		AssertNeverFires(script, _PS_011);
	}

	[Fact]
	public void AnArrayJoinWhoseEncoderIsMoreThanFiveLinesAfterOrWhoseArrayIsMoreThanTenLinesBeforeIsNotRead()
	{
		string fillerFive = string.Concat(Enumerable.Repeat(_FILLER_LINE, 7));
		string fillerTen = string.Concat(Enumerable.Repeat(_FILLER_LINE, _FAR_FILLER_LINES));

		AssertNeverFires("$parts = $u, $p\n$pair = $parts -join ':'\n" + fillerFive + _ENCODE_OVER_VARIABLE, _PS_010);
		AssertNeverFires("$parts = $u, $p\n" + fillerTen + "$pair = $parts -join ':'\n" + _ENCODE_OVER_VARIABLE, _PS_010);
		AssertFires("$parts = $u, $p\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, _NEAR_FILLER_LINES)) + "$pair = $parts -join ':'\n" + _ENCODE_OVER_VARIABLE, _PS_010);
	}

	[Fact]
	public void ThePairClassAndTheTokenClassAreOneFindingWhereBothMatchTheSameEncoderAndTheTokenClassWins()
	{
		string source = "$encoded = " + _ENCODE_OPEN + "\"$($null):$($key)\"" + _ENCODE_CLOSE;

		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Equal([_PS_011], constructs.Select(construct => construct.PatternClassIdentifier));
		Assert.Equal(ConfidenceLevel.Medium, constructs[0].Level);
	}

	[Fact]
	public void ThePairExpressionStillMatchesAnEmptyHoleSoTheSubsumptionAloneDecidesTheClass()
	{
		string source = "$encoded = " + _ENCODE_OPEN + "\"$($null):$($key)\"" + _ENCODE_CLOSE;

		Assert.Matches(new PowerShellRecognitionHarness().Analyzer.DetectionRulesByPatternClass[_PS_010].Expression, source);
		Assert.Contains(new PowerShellRecognitionHarness().Analyzer.SubsumptionRelation, rule => rule is { SubsumingPatternClassIdentifier: _PS_011, SubsumedPatternClassIdentifier: _PS_010, When: SubsumptionCondition.AnchorPositionsAreIdentical });
	}

	// ------------------------------------------------------------------------------------------
	// A pair accumulated with += is the token class, by declaration
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void APairAccumulatedAcrossTwoStatementsWithPlusEqualsIsReportedAsTheTokenClassByDeclaration()
	{
		string source = "$pair = $u\n$pair += ':' + $p\n" + _ENCODE_OVER_VARIABLE;

		AssertFires(source, _PS_011);
		AssertNeverFires(source, _PS_010);
	}

	// ------------------------------------------------------------------------------------------
	// The pair is tied to the encoder
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("$pair = 'someone:a-secret'\n$bytes = [Text.Encoding]::ASCII.GetBytes($pair)\n$encoded = [Convert]::ToBase64String($bytes)")]
	[InlineData("$bytes = [Text.Encoding]::ASCII.GetBytes(\"$($u):$($p)\")\n$encoded = [Convert]::ToBase64String($bytes)")]
	[InlineData("$pair = $u + ':' + $p\n$x = 1\n$encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($pair))")]
	[InlineData("$script:pair = \"$($u):$($p)\"\n$encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($script:pair))")]
	[InlineData("${pair} = \"$($u):$($p)\"\n$encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(${pair}))")]
	public void APairAssignedToAVariableThatIsReadBackBeforeOrInsideTheEncoderIsRead(string source)
	{
		AssertFires(source, _PS_010);
	}

	[Theory]
	[InlineData("$url = 'https://example.invalid'\n$bytes = [IO.File]::ReadAllBytes($path)\n$encoded = [Convert]::ToBase64String($bytes)")]
	[InlineData("$time = '12:30'\n$encoded = [Convert]::ToBase64String([IO.File]::ReadAllBytes($path))")]
	[InlineData("$endpoint = 'host:443'\n$encoded = [Convert]::ToBase64String([Security.Cryptography.SHA256]::HashData($bytes))")]
	[InlineData("$folder = 'C:\\data'\n$encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))")]
	[InlineData("$label = 'name:value'\n$encoded = [Convert]::ToBase64String($fileBytes)")]
	[InlineData("$label = \"$($u):$($p)\"\n$encoded = [Convert]::ToBase64String($fileBytes)")]
	[InlineData("$label = \"$($u):$($p)\"\n$encoded = [Convert]::ToBase64String($fileBytes)\nWrite-Host $label")]
	public void AColonBearingLiteralOrAPairAboveAnEncoderItNeverReachesIsNotAPairAtAll(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier is _PS_010 or _PS_011);
	}

	[Fact]
	public void APairMoreThanTenLinesBeforeTheEncoderIsNotReadAndOneWithinTenLinesIs()
	{
		string far = "$pair = $u + ':' + $p\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, _FAR_FILLER_LINES)) + _ENCODE_OVER_VARIABLE + "\n";
		string farReadEarly = "$pair = $u + ':' + $p\n$copy = $pair\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, _FAR_FILLER_LINES)) + _ENCODE_OVER_BYTES + "\n";
		string near = "$pair = $u + ':' + $p\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, _NEAR_FILLER_LINES)) + _ENCODE_OVER_VARIABLE + "\n";

		AssertNeverFires(far, _PS_010);
		AssertNeverFires(farReadEarly, _PS_010);
		AssertFires(near, _PS_010);
	}

	[Theory]
	[InlineData("1.2.3.4:8080")]
	[InlineData("a:secret")]
	[InlineData("someone:/path")]
	[InlineData("someone:\\path")]
	[InlineData("12:34")]
	public void AOneLiteralTextThatLooksLikeAUrlADriveOrAPortOrATimeIsNotAPair(string literal)
	{
		string source = "$pair = '" + literal + "'\n" + _ENCODE_OVER_VARIABLE;

		AssertNeverFires(source, _PS_010);
	}

	[Theory]
	[InlineData("someone:secret1")]
	[InlineData("some.one:a-secret")]
	[InlineData("ab:cd")]
	public void AOneLiteralPairOfTwoWordsIsAPair(string literal)
	{
		AssertFires("$pair = '" + literal + "'\n" + _ENCODE_OVER_VARIABLE, _PS_010);
		AssertFires("$pair = \"" + literal + "\"\n" + _ENCODE_OVER_VARIABLE, _PS_010);
	}

	// ------------------------------------------------------------------------------------------
	// What counts as a literal credential
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("$url = 'https://example.invalid'\n$pair = \"$($env:SERVICE_USER):$($env:SERVICE_SECRET)\"\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("param($Uri = 'https://example.invalid')\n$pair = \"$($env:SERVICE_USER):$($env:SERVICE_SECRET)\"\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$format = '{0}:{1}'\n$pair = $env:SERVICE_USER + ':' + $env:SERVICE_SECRET\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$separator = ':'\n$pair = $env:SERVICE_USER + ':' + $env:SERVICE_SECRET\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$url = 'https://example.invalid'\n$stored = Import-Clixml -Path $path\n$pair = \"$($stored.UserName):$($stored.GetNetworkCredential().Password)\"\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$url = 'https://example.invalid'\n$c = Get-Credential\n$pair = $c.UserName + ':' + $c.GetNetworkCredential().Password\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$time = '12:30'\n$c = Get-Credential\n$pair = \"$($c.UserName):$($c.GetNetworkCredential().Password)\"\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$url = 'https://example.invalid'\n$secret = Read-Host -Prompt 'Secret' -AsSecureString\n$pair = \"$($u):$($secret)\"\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$label = 'host:443'\n$pair = '{0}:{1}' -f $env:SERVICE_USER, $env:SERVICE_SECRET\n" + _ENCODE_OVER_VARIABLE)]
	public void TheLiteralCredentialSignalIsFalseWhenTheHalvesAreCodeWhateverColonBearingLiteralStandsAbove(string source)
	{
		Assert.False(SignalOf(source, _PS_010));
	}

	[Theory]
	[InlineData("$pair = 'someone:a-secret'\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('someone:a-secret'))")]
	[InlineData("$pair = $u + ':' + 'a-secret'\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = '{0}:{1}' -f $u, 'a-secret'\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = \"$($u):a-secret\"\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = [string]::Concat($u, ':', 'a-secret')\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$url = 'https://example.invalid'\n$pair = 'someone:a-secret'\n" + _ENCODE_OVER_VARIABLE)]
	public void TheLiteralCredentialSignalIsTrueForALiteralPasswordHalfOfThePairThatReachesTheEncoder(string source)
	{
		Assert.True(SignalOf(source, _PS_010));
	}

	[Theory]
	[InlineData("$pair = '' + ':' + 'a-token'\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = ':a-token'\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = \"$('')`:a-token\"\n" + _ENCODE_OVER_VARIABLE)]
	public void TheLiteralCredentialSignalIsTrueForTheNonEmptyLiteralHalfOfATokenPairToo(string source)
	{
		Assert.True(SignalOf(source, _PS_011));
	}

	[Fact]
	public void TheLiteralCredentialSignalIsFalseForATokenHalfThatIsCode()
	{
		Assert.False(SignalOf("$pair = ':' + $env:SERVICE_TOKEN\n" + _ENCODE_OVER_VARIABLE, _PS_011));
	}

	// ------------------------------------------------------------------------------------------
	// The double-quoted spellings and what is not a pair
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("\"C:$directory\"")]
	[InlineData("\"12:$value\"")]
	[InlineData("\"$($hostName):443\"")]
	[InlineData("\"$($hostName):/path\"")]
	[InlineData("\"$($hostName):\\path\"")]
	[InlineData("\"$env:SERVICE_USER\"")]
	[InlineData("\"Result: $value\"")]
	public void ADoubleQuotedTextThatIsNotAPairIsNotReadAsOne(string text)
	{
		AssertNeverFires(_ENCODE_OPEN + text + _ENCODE_CLOSE, _PS_010);
		AssertNeverFires(_ENCODE_OPEN + text + _ENCODE_CLOSE, _PS_011);
	}

	[Theory]
	[InlineData("\"$($user):a-secret\"", _PS_010)]
	[InlineData("\"$($user)`:$($secret)\"", _PS_010)]
	[InlineData("\"user-name:$($secret)\"", _PS_010)]
	[InlineData("\":$($token)\"", _PS_011)]
	[InlineData("\":a-token\"", _PS_011)]
	[InlineData("\"$($user):\"", _PS_011)]
	[InlineData("\"a-user:\"", _PS_011)]
	[InlineData("\"a-user:$('')\"", _PS_011)]
	[InlineData("\"$(''):$($token)\"", _PS_011)]
	[InlineData("':a-token'", _PS_011)]
	[InlineData("'a-user:'", _PS_011)]
	public void TheDoubleQuotedAndSingleQuotedSpellingsAreReadInTheClassOfTheirEmptyHalf(string pair, string expectedClass)
	{
		string other = expectedClass == _PS_010 ? _PS_011 : _PS_010;
		string source = _ENCODE_OPEN + pair + _ENCODE_CLOSE;

		AssertFires(source, expectedClass);
		AssertNeverFires(source, other);
	}

	[Theory]
	[InlineData("$parts = $u, $p\n$pair = $other -join ':'\n")]
	[InlineData("$parts = $u, $p\n$pair = $parts -join '-'\n")]
	[InlineData("$parts = $u\n$pair = $parts -join ':'\n")]
	public void AnArrayJoinThatDoesNotJoinThePairsArrayWithAColonIsNotAPair(string source)
	{
		AssertNeverFires(source + _ENCODE_OVER_VARIABLE, _PS_010);
	}

	// ------------------------------------------------------------------------------------------
	// The generated and the interpreted form agree
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(\"$($u):$($p)\"))")]
	[InlineData("$encoded = [Convert]::ToBase64String(\n\t[Text.Encoding]::ASCII.GetBytes(\n\t\t$u + ':' + $p\n\t)\n)")]
	[InlineData("$encoded = [Convert]::ToBase64String($bytes) + \"$($u):$($p)\"")]
	[InlineData("$pair = 'someone:a-secret'\n$bytes = [Text.Encoding]::ASCII.GetBytes($pair)\n$encoded = [Convert]::ToBase64String($bytes)")]
	[InlineData("$pair = \"user-name:$($secret)\"\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = '{0}:{1}' -f ($u, $p)\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = [string]::Concat($u, ':', $p)\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = [string]::Join(':', $u, $p)\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = $u + ‘:’ + $p\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$parts = $u, $p\n$x = 1\n$pair = $parts -join ':'\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$parts = @('', $key)\n$pair = $parts -join ':'\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$parts = $key, $null\n$pair = $parts -join ':'\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$blank = ''\n$pair = \"$($blank):$($key)\"\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$blank = $null\n$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($blank + ':' + $key))")]
	[InlineData("$pair = \"$($null):$($key)\"\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = \"$key`:${null}\"\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = $u\n$pair += ':' + $p\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$pair = ':a-token'\n" + _ENCODE_OVER_VARIABLE)]
	[InlineData("$url = 'https://example.invalid'\n$time = '12:30'\n$encoded = [Convert]::ToBase64String($fileBytes)")]
	[InlineData("$a = $x, $y; $p = @{ Authentication = $s }; [Convert]::ToBase64String($bytes) + $value, $b = 1")]
	public void TheGeneratedAndTheInterpretedFormOfBothClassesReadAScriptAlike(string source)
	{
		PowerShellRecognitionHarness harness = new();

		foreach (string patternClass in new[] { _PS_010, _PS_011 })
		{
			Regex generated = harness.Analyzer.DetectionRulesByPatternClass[patternClass].Expression;
			Regex interpreted = new(generated.ToString(), generated.Options, Regex.InfiniteMatchTimeout);

			Assert.Equal(Summarize(interpreted, source), Summarize(generated, source));
		}
	}

	private static string Summarize(Regex expression, string text)
	{
		return string.Join(
			";",
			expression.Matches(text).Select(match => $"{match.Index}+{match.Length}:{GroupExtent(match, "encoder")}:{GroupExtent(match, "credential")}"));
	}

	private static string GroupExtent(Match match, string groupName)
	{
		Group group = match.Groups[groupName];

		return group.Success ? $"{group.Index}+{group.Length}" : "-";
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static bool SignalOf(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);
		RecognizedConstruct construct = Assert.Single(constructs, candidate => candidate.PatternClassIdentifier == patternClass);

		return construct.LiteralCredentialSignal;
	}

	private static void AssertFires(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.True(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass && construct.Level == ConfidenceLevel.Medium),
			$"{patternClass} did not fire at Medium on [{source}]; got [{Describe(constructs)}]");
	}

	private static void AssertNeverFires(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.False(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass),
			$"{patternClass} fired on [{source}]; got [{Describe(constructs)}]");
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));
	}
}
