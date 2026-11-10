using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The command windows, the explicit parameter and its splat, the assembled scheme, the literal credential inside a secure-string conversion and the reshaped
/// receiver, session and splat windows of the `PowerShell` analyzer as the design states them.
///
/// Every script is INVENTED from the catalog's own forms, with placeholder names throughout, and carries no fragment, path or third-party name from any
/// repository.
/// </summary>
public sealed class PowerShellCatalog05DetectionTests
{
	private const string _PS_001 = "PS-001";
	private const string _PS_002 = "PS-002";
	private const string _PS_003 = "PS-003";
	private const string _PS_004 = "PS-004";
	private const string _PS_005 = "PS-005";
	private const string _PS_006 = "PS-006";
	private const string _PS_007 = "PS-007";
	private const string _PS_008 = "PS-008";
	private const string _PS_009 = "PS-009";
	private const string _PS_012 = "PS-012";
	private const string _PS_013 = "PS-013";
	private const string _FILLER_LINE = "$total = $first + $second\n";
	private const string _SECURE_LITERAL_CREDENTIAL = "New-Object System.Net.NetworkCredential($u, (ConvertTo-SecureString 'a-secret' -AsPlainText -Force))";
	private const string _SPLAT_USE = "Invoke-RestMethod @p";

	// ------------------------------------------------------------------------------------------
	// The logical line of a command continues inside brackets
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Headers @{\n\tAccept = 'application/json'\n} -Credential $c -Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Body (\n\t$payload\n) -Authentication Basic")]
	[InlineData("Invoke-WebRequest -Uri $uri -Body ($payload | ConvertTo-Json) -Authentication Basic")]
	[InlineData("irm $uri -Body @(\n\t1,\n\t2\n) -Authentication Basic -Credential $c")]
	[InlineData("iwr $uri -Headers @{\n\tAccept = 'a'\n\tOuter = @{\n\t\tInner = (1 | Out-Null)\n\t}\n} -Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Body ($payload |\n\tConvertTo-Json) -Authentication:Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Headers @{ Accept = 'a' } `\n\t-Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri <# note #> -Body (\n$payload\n) <# more #> -Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Headers @{\r\n\tAccept = 'a'\r\n} -Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -ScriptBlock {\n\tGet-Date | Out-Null\n} -Au Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Index [\n\t1\n] -Authentication Basic")]
	public void ACommandContinuesAcrossLineBreaksAndPipesInsideBracketsAndTheExplicitParameterAfterThemIsRead(string source)
	{
		AssertFires(source, _PS_003, ConfidenceLevel.High);
		AssertNeverFires(source, _PS_012);
	}

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri | Out-Null -Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri\n-Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Headers @{ Accept = 'a'\n-Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Headers @{\n\tAccept = 'a'\n}\n-Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Body ($payload | ConvertTo-Json) | Select-Object -Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Body (\n\t$payload\n) -Authentication Bearer")]
	public void ALineBreakOrAPipeOutsideEveryBracketAndABracketThatNeverClosesEndTheCommand(string source)
	{
		AssertNeverFires(source, _PS_003);
	}

	[Fact]
	public void ABracketThatNeverClosesEndsTheWindowAtItsBoundAndTheParameterFarBelowIsNotRead()
	{
		string source = "Invoke-RestMethod -Uri $uri -Headers @{\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, 40)) + "-Authentication Basic\n";

		AssertNeverFires(source, _PS_003);
	}

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Headers @{\n\tAccept = 'a'\n} -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Body ($payload | ConvertTo-Json) -Credential $c")]
	[InlineData("iwr $uri -Body (\n$payload\n) -Cr $c")]
	[InlineData("irm $uri -Headers @{\n\tAccept = 'a'\n} -Credential:$c")]
	public void ACredentialAfterABracketedArgumentThatSpansLinesIsLowForManualReview(string source)
	{
		AssertFires(source, _PS_012, ConfidenceLevel.Low);
	}

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Credential $c -Headers @{\n\tAccept = 'a'\n} -Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication Basic -Headers @{\n\tAccept = 'a'\n} -Credential $c")]
	public void ACommandThatCarriesTheSchemeAcrossABracketedArgumentIsOneFindingAndNotAlsoTheCredentialWithoutAScheme(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Equal([_PS_003], constructs.Select(construct => construct.PatternClassIdentifier));
	}

	[Theory]
	[InlineData("Invoke-Command -ComputerName $s -ScriptBlock {\n\thostname\n} -Credential $c")]
	[InlineData("Invoke-Command -ComputerName $s -ScriptBlock { Get-Date | Out-Null } -Credential $c")]
	[InlineData("Start-Process -FilePath $f -ArgumentList @(\n\t'a'\n) -Credential $c")]
	[InlineData("New-PSSession -ComputerName $s -ConfigurationName (\n\t$name\n) -Cred $c")]
	public void ACredentialOnANonHttpCmdletAfterABracketedArgumentThatSpansLinesIsLowForManualReview(string source)
	{
		AssertFires(source, _PS_013, ConfidenceLevel.Low);
	}

	[Fact]
	public void ASessionVariableAfterABracketedArgumentThatSpansLinesNamesAWebSession()
	{
		AssertFires("Invoke-RestMethod -Uri $u -Headers @{\n\ta = 1\n} -SessionVariable s\n$s.Credentials = $networkCredential", _PS_012, ConfidenceLevel.Low);
	}

	[Fact]
	public void TheCmdletOfANonHttpCredentialIsTheAnchorAndTheWebParameterIsTheAnchorOfTheExplicitAndTheWebClasses()
	{
		const string nonHttp = "Start-Process -FilePath $f -Credential $c";
		const string explicitScheme = "Invoke-RestMethod -Uri $uri -Authentication Basic";
		const string webCredential = "Invoke-RestMethod -Uri $uri -Credential $c";

		Assert.Equal(0, AnchorOf(nonHttp, _PS_013));
		Assert.Equal(explicitScheme.IndexOf("-Authentication", StringComparison.Ordinal), AnchorOf(explicitScheme, _PS_003));
		Assert.Equal(webCredential.IndexOf("-Credential", StringComparison.Ordinal), AnchorOf(webCredential, _PS_012));
	}

	// ------------------------------------------------------------------------------------------
	// The scheme in parentheses
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication ('Basic') -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication (\"Basic\") -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication ( 'basic' ) -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication:('Basic')")]
	[InlineData("Invoke-RestMethod -Uri $uri -Au (\"BASIC\")")]
	[InlineData("$p = @{ Authentication = ('Basic') }\nInvoke-RestMethod @p")]
	public void ASchemeLiteralInParenthesesIsComparedLikeTheLiteralAndBasicIsRead(string source)
	{
		AssertFires(source, _PS_003, ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication ('Bearer') -Token $t")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication (\"OAuth\") -Token $t")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication ('None')")]
	[InlineData("$p = @{ Authentication = ('Bearer') }\nInvoke-RestMethod @p")]
	public void ASchemeLiteralInParenthesesThatIsNotBasicIsSuppressed(string source)
	{
		AssertNeverFires(source, _PS_003);
	}

	// ------------------------------------------------------------------------------------------
	// The splat hashtable built key by key
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("$p = @{ Uri = $u; Credential = $c }\n$p.Authentication = 'Basic'\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u; Credential = $c }\n$p['Authentication'] = 'Basic'\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u; Credential = $c }\n$p[\"Authentication\"] = \"Basic\"\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u; Credential = $c }\n$p.Add('Authentication', 'Basic')\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u; Credential = $c }\n$p.Add(\"Authentication\", \"Basic\")\n" + _SPLAT_USE)]
	[InlineData("$p = @{}\n$p.Uri = $u\n$p.Authentication = 'BASIC'\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u }\n$p.Authentication = [Microsoft.PowerShell.Commands.WebAuthenticationType]::Basic\n" + _SPLAT_USE)]
	[InlineData("$scheme = 'Basic'\n$p = @{ Uri = $u }\n$p['Authentication'] = $scheme\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u }\n$p.Authentication = 'Basic'\nInvoke-WebRequest -Uri $u @p")]
	[InlineData("$p = @{ Uri = $u }\n$p.Authentication = 'Basic'\nirm @p -Method Get")]
	public void AHashtableBuiltAfterItsCreationByADotAnIndexerOrAnAddIsReadWhenAWebCmdletSplatsIt(string source)
	{
		AssertFires(source, _PS_003, ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("$p = @{ Uri = $u }\n$p.Authentication = 'Bearer'\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u }\n$p['Authentication'] = 'OAuth'\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u }\n$p.Add('Authentication', 'None')\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u }\n$p.Add(\"Authentication\", \"Bearer\")\n" + _SPLAT_USE)]
	public void AHashtableBuiltKeyByKeyWithAnotherSchemeIsSuppressed(string source)
	{
		AssertNeverFires(source, _PS_003);
	}

	[Theory]
	[InlineData("$p = @{ To = $a }\n$p.Authentication = 'Basic'\nSend-MailMessage @p")]
	[InlineData("$p = @{ To = $a }\n$p['Authentication'] = 'Basic'\nSend-MailMessage @p")]
	[InlineData("$p = @{ To = $a }\n$p.Add('Authentication', 'Basic')\nSend-MailMessage @p")]
	[InlineData("$p = @{ Uri = $u }\n$p.Add('Uri', 'Basic')\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u }\n$q = @{ Uri = $u }\n$q.Authentication = 'Basic'\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u }\n$p.Authentication = 'Basic'\nGet-ChildItem @p")]
	public void AHashtableBuiltKeyByKeyIsNotTheExplicitParameterWhenAnotherCmdletSplatsItOrAnotherTableIsSplatted(string source)
	{
		AssertNeverFires(source, _PS_003);
	}

	[Fact]
	public void TheKeyWrittenToTheTableIsReadWithinTwentyLinesOfTheSplatAndNotBeyond()
	{
		string within = "$p = @{ Uri = $u }\n$p.Authentication = 'Basic'\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, 19)) + _SPLAT_USE;
		string beyond = "$p = @{ Uri = $u }\n$p.Authentication = 'Basic'\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, 20)) + _SPLAT_USE;

		AssertFires(within, _PS_003, ConfidenceLevel.High);
		AssertNeverFires(beyond, _PS_003);
	}

	[Fact]
	public void TheKeyInTheLiteralIsReadWithinTwentyLinesOfTheSplatAndNotBeyond()
	{
		string within = "$p = @{ Authentication = 'Basic' }\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, 19)) + _SPLAT_USE;
		string beyond = "$p = @{ Authentication = 'Basic' }\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, 20)) + _SPLAT_USE;

		AssertFires(within, _PS_003, ConfidenceLevel.High);
		AssertNeverFires(beyond, _PS_003);
	}

	[Theory]
	[InlineData("$p = @{ Uri = $u; Credential = $c }\n$p.Authentication = 'Basic'\n" + _SPLAT_USE)]
	[InlineData("$p = @{ Uri = $u; Credential = $c; Authentication = 'Basic' }\n" + _SPLAT_USE)]
	public void ASplatThatCarriesTheCredentialAndTheSchemeIsOneFindingOfTheExplicitClass(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Equal([_PS_003], constructs.Select(construct => construct.PatternClassIdentifier));
	}

	[Theory]
	[InlineData("$p = @{ Authentication = 'Basic' }\nInvoke-RestMethod @p -Credential $c")]
	[InlineData("$p = @{ Authentication = 'Basic' }\nInvoke-RestMethod -Credential $c @p")]
	[InlineData("$p = @{ Uri = $u }\n$p.Authentication = 'Basic'\nInvoke-RestMethod -Uri $u @p -Cr $c -Method Get")]
	[InlineData("$p = @{ Credential = $c }\nInvoke-RestMethod @p -Authentication Basic")]
	[InlineData("$p = @{ Credential = $c }\nInvoke-RestMethod -Authentication Basic @p")]
	[InlineData("$p = @{ Credential = $c }\nInvoke-RestMethod -Headers @{\n\ta = 1\n} @p -Authentication Basic")]
	public void ACommandThatCarriesTheSchemeByASplatAndTheCredentialByAParameterOrTheReverseIsOneFindingWhateverTheOrderOnTheLine(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Equal([_PS_003], constructs.Select(construct => construct.PatternClassIdentifier));
	}

	[Theory]
	[InlineData("$p = @{ Credential = $c }\nInvoke-RestMethod @p -Authentication Bearer -Token $t")]
	[InlineData("$p = @{ Authentication = 'Bearer' }\nInvoke-RestMethod @p -Credential $c")]
	[InlineData("$p = @{ Authentication = 'Basic' }\nInvoke-RestMethod @p\nInvoke-RestMethod -Uri $u -Credential $c")]
	public void ACommandWhoseSchemeIsNotBasicOrWhoseCredentialIsOnAnotherCommandKeepsTheLowFinding(string source)
	{
		AssertFires(source, _PS_012, ConfidenceLevel.Low);
	}

	[Fact]
	public void TheKeyOfTheSplatIsTheAnchorWhetherItStandsInTheLiteralOrIsWrittenAfterwards()
	{
		const string literal = "$p = @{ Uri = $u; Authentication = 'Basic' }\nInvoke-RestMethod @p";
		const string dot = "$p = @{ Uri = $u }\n$p.Authentication = 'Basic'\nInvoke-RestMethod @p";
		const string indexer = "$p = @{ Uri = $u }\n$p['Authentication'] = 'Basic'\nInvoke-RestMethod @p";
		const string add = "$p = @{ Uri = $u }\n$p.Add('Authentication', 'Basic')\nInvoke-RestMethod @p";

		Assert.Equal(literal.IndexOf("Authentication", StringComparison.Ordinal), AnchorOf(literal, _PS_003));
		Assert.Equal(dot.IndexOf(".Authentication", StringComparison.Ordinal) + 1, AnchorOf(dot, _PS_003));
		Assert.Equal(indexer.IndexOf("'Authentication'", StringComparison.Ordinal), AnchorOf(indexer, _PS_003));
		Assert.Equal(add.IndexOf("'Authentication'", StringComparison.Ordinal), AnchorOf(add, _PS_003));
	}

	[Fact]
	public void TheCredentialKeyOfTheSplatIsStillTheLowClassAndTheSessionAndSplatWindowsAreTwentyAndEightLines()
	{
		string within = "$p = @{ Credential = $c }\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, 19)) + _SPLAT_USE;
		string beyond = "$p = @{ Credential = $c }\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, 20)) + _SPLAT_USE;
		string sessionAtEight = "$s = New-Object Microsoft.PowerShell.Commands.WebRequestSession\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, 7)) + "$s.Credentials = $networkCredential";
		string sessionAtNine = "$s = New-Object Microsoft.PowerShell.Commands.WebRequestSession\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, 8)) + "$s.Credentials = $networkCredential";

		AssertFires(within, _PS_012, ConfidenceLevel.Low);
		AssertNeverFires(beyond, _PS_012);
		AssertFires(sessionAtEight, _PS_012, ConfidenceLevel.Low);
		AssertNeverFires(sessionAtNine, _PS_012);
	}

	// ------------------------------------------------------------------------------------------
	// The assembled scheme
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("$h = @{ Authorization = ('Ba' + 'sic') + ' ' + $encoded }")]
	[InlineData("$h = @{ Authorization = 'Ba' + 'sic' + ' ' + $encoded }")]
	[InlineData("$h = @{ Authorization = \"Ba\" + \"sic\" + ' ' + $encoded }")]
	[InlineData("$h = @{ Authorization = ('Basic' -replace 'x', 'y') + ' ' + $encoded }")]
	[InlineData("$h = @{ Authorization = 'Basic' -replace 'x', 'y' }")]
	[InlineData("$h = @{ Authorization = ('Ba', 'sic') -join '' }")]
	[InlineData("$h = @{ Authorization = '{0}' -f 'Basic' }")]
	[InlineData("$h = @{ Authorization = '{0} {1}' -f 'Basic', $encoded }")]
	[InlineData("$h = @{ Authorization = 'Basic'.ToUpper() + ' ' + $encoded }")]
	[InlineData("$h = @{ Authorization = ('Bear' + 'er') + ' ' + $token }")]
	[InlineData("$h = @{ 'Authorization' = 'Ba' + 'sic' + ' ' + $encoded }")]
	[InlineData("$value = ('Ba' + 'sic') + ' ' + $encoded\n$h = @{ Authorization = $value }")]
	public void AnAssembledSchemeInAHeaderHashtableIsIndeterminateAndKeptAtMediumAndNeverComparedAsAFragment(string source)
	{
		AssertFires(source, _PS_004, ConfidenceLevel.Medium);
		Assert.All(new PowerShellRecognitionHarness().Recognize(source), construct => Assert.False(construct.LiteralCredentialSignal));
	}

	[Theory]
	[InlineData("$headerTable.Add('Authorization', ('Ba' + 'sic') + ' ' + $encoded)")]
	[InlineData("$headerTable['Authorization'] = 'Ba' + 'sic' + ' ' + $encoded")]
	[InlineData("$headerTable.Authorization = 'Basic' -replace 'x', 'y'")]
	public void AnAssembledSchemeInAHeaderWriteIsIndeterminateAndKeptAtMedium(string source)
	{
		AssertFires(source, _PS_005, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, ('Ba' + 'sic') + ' ' + $encoded)")]
	[InlineData("$request.Headers[[System.Net.HttpRequestHeader]::Authorization] = 'Ba' + 'sic' + ' ' + $encoded")]
	public void AnAssembledSchemeInTheEnumerationWriteIsIndeterminateAndKeptAtMedium(string source)
	{
		AssertFires(source, _PS_006, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("[System.Net.Http.Headers.AuthenticationHeaderValue]::new('Ba' + 'sic', $encoded)")]
	[InlineData("[System.Net.Http.Headers.AuthenticationHeaderValue]::new(('Ba' + 'sic'), $encoded)")]
	[InlineData("[System.Net.Http.Headers.AuthenticationHeaderValue]::new(('Ba', 'sic') -join '', $encoded)")]
	[InlineData("[System.Net.Http.Headers.AuthenticationHeaderValue]::new('Basic'.ToUpper(), $encoded)")]
	[InlineData("New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Ba' + 'sic', $encoded)")]
	[InlineData("New-Object -TypeName System.Net.Http.Headers.AuthenticationHeaderValue -ArgumentList ('Ba' + 'sic'), $encoded")]
	[InlineData("[System.Net.Http.Headers.AuthenticationHeaderValue]::new('{0}' -f 'Basic', $encoded)")]
	public void AnAssembledSchemeOnTheTypedHeaderIsIndeterminateAndKeptAtMedium(string source)
	{
		AssertFires(source, _PS_007, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("$cache.Add([Uri]$uri, 'Ba' + 'sic', (New-Object System.Net.NetworkCredential($u, $p)))")]
	[InlineData("$cache.Add($uri, ('Ba', 'sic') -join '', $cred)")]
	[InlineData("$cache.Add($host, 443, 'Basic'.ToUpper(), $cred)")]
	[InlineData("$cache.Add($uri, '{0}' -f 'Basic', $cred)")]
	public void AnAssembledSchemeOnTheCredentialCacheIsIndeterminateAndKeptAtMediumNotHigh(string source)
	{
		AssertFires(source, _PS_008, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication ('Ba' + 'sic') -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication ('Basic' -replace 'x', 'y') -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication (('Ba', 'sic') -join '') -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication ('Basic'.ToUpper()) -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication ('Bear' + 'er') -Token $t")]
	[InlineData("$p = @{ Authentication = 'Ba' + 'sic' }\nInvoke-RestMethod @p")]
	[InlineData("$p = @{ Uri = $u }\n$p.Authentication = 'Ba' + 'sic'\nInvoke-RestMethod @p")]
	[InlineData("$p = @{ Uri = $u }\n$p.Add('Authentication', ('Ba' + 'sic'))\nInvoke-RestMethod @p")]
	public void AnAssembledSchemeOnTheExplicitParameterIsKeptAtMediumBecauseItsSchemeArgumentTookNoPartInTheMatch(string source)
	{
		AssertFires(source, _PS_003, ConfidenceLevel.Medium);
		Assert.DoesNotContain(
			new PowerShellRecognitionHarness().Recognize(source),
			construct => construct.PatternClassIdentifier == _PS_003 && construct.Level == ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication Basic -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication [Microsoft.PowerShell.Commands.WebAuthenticationType]::Basic -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication 'Basic' -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication \"Basic\" -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication ('Basic') -Credential $c")]
	[InlineData("$scheme = 'Basic'\nInvoke-RestMethod -Uri $uri -Authentication $scheme -Credential $c")]
	[InlineData("$p = @{ Authentication = 'Basic' }\nInvoke-RestMethod @p")]
	[InlineData("$p = @{ Authentication = [Microsoft.PowerShell.Commands.WebAuthenticationType]::Basic }\nInvoke-RestMethod @p")]
	public void ASchemeWrittenWholeOrResolvedToBasicOnTheExplicitParameterStaysAtHigh(string source)
	{
		AssertFires(source, _PS_003, ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("$basic = 'Bearer'\nInvoke-RestMethod -Uri $uri -Authentication Basic -Credential $c")]
	[InlineData("$Basic = 'Bearer'\nInvoke-RestMethod -Uri $uri -Authentication Basic -Credential $c")]
	[InlineData("$basic = 'Bearer'\nInvoke-RestMethod -Uri $uri -Authentication [Microsoft.PowerShell.Commands.WebAuthenticationType]::Basic -Credential $c")]
	[InlineData("$basic = 'Bearer'\n$p = @{ Authentication = [Microsoft.PowerShell.Commands.WebAuthenticationType]::Basic }\nInvoke-RestMethod @p")]
	public void ABareBasicOrTheEnumerationMemberIsNeverResolvedAsTheNameOfAVariableTheScriptDeclares(string source)
	{
		AssertFires(source, _PS_003, ConfidenceLevel.High);
	}

	[Fact]
	public void AResolvedBearerVariableOnTheExplicitParameterIsStillSuppressed()
	{
		AssertNeverFires("$scheme = 'Bearer'\nInvoke-RestMethod -Uri $uri -Authentication $scheme -Token $t", _PS_003);
	}

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication $scheme -Credential $c")]
	[InlineData("function Get-Data($scheme = 'Bearer') {\n\tInvoke-RestMethod -Uri $u -Authentication $scheme\n}")]
	[InlineData("$scheme = 'Bearer'\n$scheme = 'Basic'\nInvoke-RestMethod -Uri $u -Authentication $scheme")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication $options.Scheme -Credential $c")]
	public void ASchemeVariableThatCannotBeResolvedKeepsTheLevelOfTheBareWordNotTheMediumOfAnAssembledScheme(string source)
	{
		AssertFires(source, _PS_003, ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("$h = @{ Authorization = 'Basic ' + $encoded }", true)]
	[InlineData("$h = @{ Authorization = 'Bearer ' + $token }", false)]
	[InlineData("$h = @{ Authorization = ('Basic', $encoded) -join ' ' }", true)]
	[InlineData("$h = @{ Authorization = ('Bearer', $token) -join ' ' }", false)]
	[InlineData("$h = @{ Authorization = 'Basic {0}' -f $encoded }", true)]
	[InlineData("$h = @{ Authorization = 'Bearer {0}' -f $token }", false)]
	[InlineData("$h = @{ Authorization = 'Basic' + ' ' + $encoded }", true)]
	public void ASchemeThatIsWrittenWholeIsStillComparedAndABearerOneIsStillSuppressed(string source, bool read)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Equal(read, constructs.Any(construct => construct.PatternClassIdentifier == _PS_004 && construct.Level == ConfidenceLevel.Medium));
		Assert.False(!read && constructs.Any(construct => construct.PatternClassIdentifier == _PS_004));
	}

	// ------------------------------------------------------------------------------------------
	// A literal inside a secure-string conversion is a literal credential
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("(ConvertTo-SecureString 'a-secret' -AsPlainText -Force)")]
	[InlineData("(ConvertTo-SecureString -String 'a-secret' -AsPlainText -Force)")]
	[InlineData("(ConvertTo-SecureString -AsPlainText -Force 'a-secret')")]
	[InlineData("(ConvertTo-SecureString \"a-secret\" -AsPlainText)")]
	[InlineData("( ConvertTo-SecureString 'a-secret' -As -Force )")]
	[InlineData("(CONVERTTO-SECURESTRING 'a-secret' -ASPLAINTEXT -FORCE)")]
	public void ALiteralGivenToTheSecureStringConversionInsideAnHttpCredentialSetsTheLiteralCredentialSignal(string conversion)
	{
		string source = "$wc = New-Object System.Net.WebClient\n$wc.Credentials = New-Object System.Net.NetworkCredential($u, " + conversion + ")";
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);
		RecognizedConstruct construct = Assert.Single(constructs, candidate => candidate.PatternClassIdentifier == _PS_009);

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.True(construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("(ConvertTo-SecureString $plain -AsPlainText -Force)")]
	[InlineData("(ConvertTo-SecureString $env:SERVICE_PASSWORD -AsPlainText -Force)")]
	[InlineData("(ConvertTo-SecureString -String $plain -AsPlainText -Force)")]
	[InlineData("(ConvertTo-SecureString $settings['Password'] -AsPlainText -Force)")]
	[InlineData("(ConvertTo-SecureString 'a-secret')")]
	[InlineData("(ConvertTo-SecureString $text)")]
	[InlineData("$secure")]
	public void ASecureStringConversionOfAnythingButALiteralSetsNothing(string password)
	{
		string source = "$wc = New-Object System.Net.WebClient\n$wc.Credentials = New-Object System.Net.NetworkCredential($u, " + password + ")";
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);
		RecognizedConstruct construct = Assert.Single(constructs, candidate => candidate.PatternClassIdentifier == _PS_009);

		Assert.False(construct.LiteralCredentialSignal);
	}

	[Fact]
	public void ACredentialObjectBuiltFromAConversionAndPassedByVariableStaysWithoutTheSignal()
	{
		const string source = "$cred = New-Object System.Management.Automation.PSCredential($u, (ConvertTo-SecureString 'a-secret' -AsPlainText -Force))\n"
			+ "$wc = New-Object System.Net.WebClient\n$wc.Credentials = $cred.GetNetworkCredential()";
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.All(constructs, construct => Assert.False(construct.LiteralCredentialSignal));
		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _PS_009);
	}

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $u -Credential $cred")]
	[InlineData("Invoke-RestMethod -Uri $u -Credential (New-Object System.Management.Automation.PSCredential($u, (ConvertTo-SecureString 'a-secret' -AsPlainText -Force)))")]
	public void TheWebCredentialClassKeepsNoCredentialPositionWhateverIsGivenToTheParameter(string source)
	{
		RecognizedConstruct construct = Assert.Single(new PowerShellRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == _PS_012);

		Assert.False(construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("$c = New-Object Sdk.UsernamePasswordCredentials($u, (ConvertTo-SecureString 'a-secret' -AsPlainText -Force))", _PS_001, ConfidenceLevel.High)]
	[InlineData("$c = New-Object -TypeName Sdk.UsernamePasswordCredentials -ArgumentList $u, (ConvertTo-SecureString 'a-secret' -AsPlainText -Force)", _PS_001, ConfidenceLevel.High)]
	[InlineData("$c = [Sdk.UsernamePasswordCredentials]::new($u, (ConvertTo-SecureString 'a-secret' -AsPlainText -Force))", _PS_001, ConfidenceLevel.High)]
	[InlineData("$p = New-Object Sdk.RSAPIClient ((New-Object System.Uri $a), (New-Object Sdk.UsernamePasswordCredentials($u, (ConvertTo-SecureString 'a-secret' -AsPlainText -Force))))", _PS_002, ConfidenceLevel.High)]
	[InlineData("$cache.Add([Uri]$uri, 'Basic', (" + _SECURE_LITERAL_CREDENTIAL + "))", _PS_008, ConfidenceLevel.High)]
	[InlineData("$mailer = New-Object System.Net.Mail.SmtpClient\n$mailer.Credentials = " + _SECURE_LITERAL_CREDENTIAL, _PS_013, ConfidenceLevel.Low)]
	[InlineData("$root = New-Object System.DirectoryServices.DirectoryEntry($path, $u, (ConvertTo-SecureString 'a-secret' -AsPlainText -Force))", _PS_013, ConfidenceLevel.Low)]
	public void TheLiteralGivenToTheConversionSetsTheSignalOnEveryConstructThatTakesTheCredentialAsAnArgument(string source, string patternClass, ConfidenceLevel level)
	{
		RecognizedConstruct construct = Assert.Single(new PowerShellRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == patternClass);

		Assert.Equal(level, construct.Level);
		Assert.True(construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// The receiver windows: the assignment first, the receiver behind it
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData(8, true)]
	[InlineData(9, false)]
	public void TheReceiverOfAnHttpCredentialIsCreatedAtMostEightLinesBeforeTheAssignment(int linesBetween, bool read)
	{
		string source = "$wc = New-Object System.Net.WebClient\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, linesBetween - 1)) + "$wc.Credentials = " + _SECURE_LITERAL_CREDENTIAL;

		Assert.Equal(read, Fires(source, _PS_009));
	}

	[Theory]
	[InlineData(8, true)]
	[InlineData(9, false)]
	public void TheReceiverOfANonHttpCredentialIsCreatedAtMostEightLinesBeforeTheAssignment(int linesBetween, bool read)
	{
		string source = "$m = [System.Net.Mail.SmtpClient]::new()\n" + string.Concat(Enumerable.Repeat(_FILLER_LINE, linesBetween - 1)) + "$m.Credentials = " + _SECURE_LITERAL_CREDENTIAL;

		Assert.Equal(read, Fires(source, _PS_013));
	}

	[Theory]
	[InlineData("$w = [System.Net.WebClient]@{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	[InlineData("$w = New-Object System.Net.WebClient -Property @{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	[InlineData("$w = New-Object -TypeName System.Net.WebClient -Property @{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	[InlineData("$w = New-Object -ArgumentList $x -TypeName System.Net.WebClient -Property @{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	[InlineData("$w = New-Object -TypeName 'System.Net.WebClient' -Property @{\n\t\tCredentials = " + _SECURE_LITERAL_CREDENTIAL + "\n\t}")]
	[InlineData("$w = [System.Net.Http.HttpClientHandler]@{ UseCookies = $true; Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	public void TheInitializerOfAnHttpReceiverIsReadWhereverTheTypeNameStandsInACreationForm(string source)
	{
		Assert.True(Fires(source, _PS_009), source);
	}

	[Theory]
	[InlineData("$w = New-Object -ArgumentList $x System.Net.WebClient -Property @{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	[InlineData("$w = New-Object -Property @{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	[InlineData("$w = @{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	[InlineData("$w = [System.Net.Mail.SmtpClient]@{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	public void ATypeNameInTheInitializerThatNoCreationFormAllowsAndATableOfAnotherTypeAreNotAnHttpReceiver(string source)
	{
		Assert.False(Fires(source, _PS_009), source);
	}

	[Theory]
	[InlineData("$m = New-Object System.Net.Mail.SmtpClient -Property @{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	[InlineData("$m = [System.Net.WebProxy]@{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	[InlineData("$m = New-Object -ArgumentList $x -TypeName System.Net.Mail.SmtpClient -Property @{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	public void TheInitializerOfANonHttpReceiverIsReadAsTheLowClassAndNotAsTheHttpOne(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Equal([_PS_013], constructs.Select(construct => construct.PatternClassIdentifier));
	}

	[Theory]
	[InlineData("$wc = New-Object System.Net.WebClient\n$wc.Credentials = New-Object System.Net.NetworkCredential($u, $p)")]
	[InlineData("$wc = New-Object -TypeName 'System.Net.WebClient'\n$wc.Credentials = $cred.GetNetworkCredential()")]
	[InlineData("$wc = [System.Net.WebClient]::new()\n$wc.Credentials = [System.Net.NetworkCredential]$cred")]
	[InlineData("$r = [System.Net.WebRequest]::Create($url)\n$r.Credentials = (New-Object System.Net.NetworkCredential($u, $p))")]
	public void TheHttpReceiverFormsAreStillReadAtMedium(string source)
	{
		AssertFires(source, _PS_009, ConfidenceLevel.Medium);
	}

	[Fact]
	public void ANonHttpReceiverAndAnHttpOneWithinTheWindowAreOneLowFindingOnTheSharedAssignment()
	{
		const string source = "$wc = New-Object System.Net.WebClient\n$m = New-Object System.Net.Mail.SmtpClient\n$m.Credentials = New-Object System.Net.NetworkCredential($u, $p)";
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Equal([_PS_013], constructs.Select(construct => construct.PatternClassIdentifier));
		Assert.Equal(source.IndexOf("Credentials", StringComparison.Ordinal), constructs[0].AnchorOffset);
	}

	[Theory]
	[InlineData("$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession -Property @{ Credentials = $c }")]
	[InlineData("$session = New-Object -TypeName Microsoft.PowerShell.Commands.WebRequestSession -Property @{ Credentials = $c }")]
	[InlineData("$session = New-Object -ArgumentList $x -TypeName Microsoft.PowerShell.Commands.WebRequestSession -Property @{ Credentials = $c }")]
	[InlineData("$session = [Microsoft.PowerShell.Commands.WebRequestSession]@{ Credentials = $c }")]
	public void TheInitializerOfAWebSessionIsReadAtLow(string source)
	{
		AssertFires(source, _PS_012, ConfidenceLevel.Low);
	}

	// ------------------------------------------------------------------------------------------
	// The generated and the interpreted form agree
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Headers @{\n\tAccept = 'a'\n} -Credential $c -Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Body ($payload | ConvertTo-Json) -Authentication ('Basic')")]
	[InlineData("irm $uri -Authentication ('Ba' + 'sic') -Credential $c")]
	[InlineData("iwr $uri -Cr $c <# note #> -Headers @{\n\tAccept = 'a'\n}")]
	[InlineData("Invoke-RestMethod -Uri $uri `\n\t-Credential $c -Authentication Basic")]
	[InlineData("Invoke-Command -ComputerName $s -ScriptBlock {\n\thostname | Out-Null\n} -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $u -Headers @{\n\ta = 1\n} -SessionVariable s\n$s.Credentials = $networkCredential")]
	[InlineData("$p = @{ Uri = $u; Credential = $c }\n$p.Authentication = 'Basic'\nInvoke-RestMethod @p")]
	[InlineData("$p = @{ Uri = $u }\n$p['Authentication'] = 'Ba' + 'sic'\nInvoke-RestMethod @p")]
	[InlineData("$p = @{ Uri = $u }\n$p.Add(\"Authentication\", 'Basic')\n$total = 1\nirm @p")]
	[InlineData("$p = @{ Uri = $u; Credential = $c }\nInvoke-WebRequest @p\nSend-MailMessage @p")]
	[InlineData("$p = @{ Authentication = $s }; $p = @{ Credential = $c }; $q = @{ Authentication = 'Basic' }\nirm @q")]
	[InlineData("$wc = New-Object System.Net.WebClient\n$wc.Credentials = " + _SECURE_LITERAL_CREDENTIAL)]
	[InlineData("$w = New-Object -ArgumentList $x -TypeName System.Net.WebClient -Property @{ Credentials = " + _SECURE_LITERAL_CREDENTIAL + " }")]
	[InlineData("$m = [System.Net.Mail.SmtpClient]::new()\n$m.Credentials = $cred.GetNetworkCredential()")]
	[InlineData("$session = [Microsoft.PowerShell.Commands.WebRequestSession]@{ Credentials = $c }")]
	[InlineData("$x.Credentials = $y; New-Object System.Net.WebClient; $m.Credentials = $y; New-Object System.Net.Mail.SmtpClient; $x.Credentials = $y")]
	[InlineData("$h = @{ Authorization = ('Ba' + 'sic') + ' ' + $encoded }")]
	[InlineData("$cache.Add($uri, ('Ba', 'sic') -join '', $cred)")]
	[InlineData("[System.Net.Http.Headers.AuthenticationHeaderValue]::new('Ba' + 'sic', $encoded)")]
	[InlineData("$root = New-Object System.DirectoryServices.DirectoryEntry($path, $u, (ConvertTo-SecureString 'a-secret' -AsPlainText -Force))")]
	public void TheGeneratedAndTheInterpretedFormOfTheReshapedClassesReadAScriptAlike(string source)
	{
		PowerShellRecognitionHarness harness = new();
		string normalized = MigrateAuth.Cli.Reading.TextNormalization.Normalize(source);

		foreach (string patternClass in new[] { _PS_001, _PS_002, _PS_003, _PS_004, _PS_005, _PS_006, _PS_007, _PS_008, _PS_009, _PS_012, _PS_013 })
		{
			Regex generated = harness.Analyzer.DetectionRulesByPatternClass[patternClass].Expression;
			Regex interpreted = new(generated.ToString(), generated.Options, Regex.InfiniteMatchTimeout);

			Assert.Equal(Summarize(interpreted, normalized), Summarize(generated, normalized));
		}
	}

	private static string Summarize(Regex expression, string text)
	{
		return string.Join(
			";",
			expression.Matches(text).Select(match => $"{match.Index}+{match.Length}:{GroupExtent(match, "construct")}:{GroupExtent(match, "header")}:{GroupExtent(match, "scheme")}:{GroupExtent(match, "credential")}"));
	}

	private static string GroupExtent(Match match, string groupName)
	{
		Group group = match.Groups[groupName];

		return group.Success ? $"{group.Index}+{group.Length}" : "-";
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static bool Fires(string source, string patternClass)
	{
		return new PowerShellRecognitionHarness().Recognize(source).Any(construct => construct.PatternClassIdentifier == patternClass);
	}

	private static int AnchorOf(string source, string patternClass)
	{
		RecognizedConstruct construct = Assert.Single(new PowerShellRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == patternClass);

		return construct.AnchorOffset;
	}

	private static void AssertFires(string source, string patternClass, ConfidenceLevel level)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.True(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass && construct.Level == level),
			$"{patternClass} did not fire at {level} on [{source}]; got [{Describe(constructs)}]");
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
