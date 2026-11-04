using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Release;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The `PowerShell` analyzer against its catalog, through the real embedded analyzer and the real recognition engine: never through a hand-built
/// construct, because what is under test is whether the expressions the analyzer declares recognize the forms the catalog states and decline the
/// exclusions it states.
///
/// Every script below is INVENTED from the catalog's own prose, with placeholder names throughout, and carries no fragment, path or third-party name
/// from any repository.
/// </summary>
public sealed class PowerShellAnalyzerTests
{
	private const string _TERM_SEPARATOR = " ";

	private const string _LANGUAGE_NAME = "PowerShell";

	private const string _PS_001 = "PS-001";
	private const string _PS_002 = "PS-002";
	private const string _PS_003 = "PS-003";
	private const string _PS_004 = "PS-004";
	private const string _PS_005 = "PS-005";
	private const string _PS_006 = "PS-006";
	private const string _PS_007 = "PS-007";
	private const string _PS_008 = "PS-008";
	private const string _PS_009 = "PS-009";
	private const string _PS_010 = "PS-010";
	private const string _PS_011 = "PS-011";
	private const string _PS_012 = "PS-012";
	private const string _PS_013 = "PS-013";
	private const string _PS_014 = "PS-014";

	private static readonly ImmutableArray<string> _everyClass =
	[
		_PS_001, _PS_002, _PS_003, _PS_004, _PS_005, _PS_006, _PS_007, _PS_008, _PS_009, _PS_010, _PS_011, _PS_012, _PS_013, _PS_014
	];

	private static readonly ImmutableArray<string> _templatedClasses =
	[
		_PS_001, _PS_002, _PS_003, _PS_004, _PS_005, _PS_006, _PS_007, _PS_008, _PS_009, _PS_010, _PS_011
	];

	// ------------------------------------------------------------------------------------------
	// The contract
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void ThePowerShellAnalyzerFitsTheContractWithNoViolations()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(ReadAnalyzer()));
	}

	[Fact]
	public void TheAnalyzerIsRegisteredUnderItsThreeExtensionsAndCaseDoesNotMatter()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		foreach (string extension in new[] { ".ps1", ".psm1", ".psd1", ".PS1", ".Psm1" })
		{
			Assert.True(catalog.TryGetAnalyzerForFileExtension(extension, out ILanguageAnalyzer? analyzer), extension);
			Assert.Same(catalog.AnalyzersByLanguageName[_LANGUAGE_NAME], analyzer);
		}

		Assert.False(catalog.TryGetAnalyzerForFileExtension(".ps1xml", out _));
		Assert.False(catalog.TryGetAnalyzerForFileExtension(".psc1", out _));
	}

	[Fact]
	public void FourteenClassesAreDeclaredAndElevenCanReachHighOrMedium()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.Equal(_everyClass.Order(StringComparer.Ordinal), analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal));
		Assert.Equal(
			[_PS_012, _PS_013, _PS_014],
			analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.BaseLevel is ConfidenceLevel.Low).Select(entry => entry.Key).Order(StringComparer.Ordinal));
		Assert.Equal(
			new[] { _PS_003, _PS_008, _PS_001, _PS_002, _PS_004, _PS_005, _PS_006, _PS_007 }.Order(StringComparer.Ordinal),
			analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.BaseLevel is ConfidenceLevel.High).Select(entry => entry.Key).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void EveryExpressionCarriesTheCaseInsensitiveOptionsAndThePolicyTimeout()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		const RegexOptions expected = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

		Assert.All(analyzer.DetectionRulesByPatternClass, entry =>
		{
			Assert.Equal(expected, entry.Value.Expression.Options & expected);
			Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, entry.Value.Expression.MatchTimeout);
		});
		Assert.All(analyzer.CandidateSelectionAndRecommendation.FlowSignals, signal =>
		{
			Assert.Equal(expected, signal.Expression.Options & expected);
			Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, signal.Expression.MatchTimeout);
		});
	}

	[Fact]
	public void TheFirstLineExpressionIsCaseSensitiveAndCarriesTheLexicalBudget()
	{
		Regex expression = ReadAnalyzer().LanguageIdentification.FirstLineDetectionExpression!;

		Assert.Equal(LexicalEnginePolicy.LEXICAL_BUDGET, expression.MatchTimeout);
		Assert.Equal(RegexOptions.None, expression.Options & RegexOptions.IgnoreCase);
	}

	[Theory]
	[InlineData("#!/usr/bin/env pwsh", true)]
	[InlineData("#!/usr/bin/pwsh", true)]
	[InlineData("#!/usr/bin/pwsh -NoProfile", true)]
	[InlineData("#!/usr/bin/env -S pwsh -File", true)]
	[InlineData("#!/opt/microsoft/powershell/7/powershell", true)]
	[InlineData("#! /usr/bin/env powershell", true)]
	[InlineData("#!/usr/bin/pwshx", false)]
	[InlineData("#!/usr/bin/env bash", false)]
	[InlineData("#!/bin/sh", false)]
	[InlineData("#!/usr/bin/env PWSH", false)]
	[InlineData("# pwsh is mentioned in a comment", false)]
	[InlineData("pwsh", false)]
	public void AShebangThatNamesThePowerShellInterpreterIsClaimedByTheAnalyzerItself(string firstLine, bool claimed)
	{
		Regex expression = ReadAnalyzer().LanguageIdentification.FirstLineDetectionExpression!;

		Assert.Equal(claimed, expression.IsMatch(firstLine));
	}

	[Fact]
	public void AnExtensionlessScriptWithAShebangIsAttributedToPowerShellThroughTheAnalyzersOwnRule()
	{
		LanguageAttribution attribution = new(LanguageCatalogRegistry.BuildEmbeddedCatalog());
		PowerShellRecognitionHarness harness = new();

		AttributedLanguage claimed = attribution.Attribute(harness.PathOf("tools/deploy"), "#!/usr/bin/env pwsh");
		AttributedLanguage notClaimed = attribution.Attribute(harness.PathOf("tools/other"), "#!/usr/bin/env bash");

		Assert.True(claimed.IsRecognized);
		Assert.Equal(_LANGUAGE_NAME, claimed.LanguageName);
		Assert.False(notClaimed.IsRecognized);
	}

	[Fact]
	public void TheFourAnchorsAreDeclaredCaseInsensitiveAndEveryClassFixtureBringsOne()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		FrozenSet<PreselectionAnchor> anchors = analyzer.CandidateSelectionAndRecommendation.PreselectionAnchors;

		Assert.Equal(["Authentication", "Authorization", "Credential", "ToBase64String"], anchors.Select(anchor => anchor.Text).Order(StringComparer.Ordinal));
		Assert.All(anchors, anchor => Assert.Equal(CaseSensitivity.Insensitive, anchor.Sensitivity));
		Assert.All(
			ClassFixtures(),
			fixture => Assert.True(CandidatePreselection.IsCandidate(analyzer, fixture.Source), fixture.PatternClass));
		Assert.True(CandidatePreselection.IsCandidate(analyzer, "get-CREDENTIAL"));
		Assert.False(CandidatePreselection.IsCandidate(analyzer, "Invoke-WebRequest -Uri $u -UseBasicParsing"));
	}

	[Fact]
	public void TheDemotionSignalsAreAllInsensitiveAndTheDeploymentFolderIsNotOne()
	{
		LanguageShapedDemotionSignals signals = ReadAnalyzer().DemotionSignals;

		Assert.All(signals.FileNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
		Assert.All(signals.FolderNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
		Assert.DoesNotContain(signals.FolderNameSignals, signal => signal.FolderName.Equals("Deployment", StringComparison.OrdinalIgnoreCase));
		Assert.Equal(
			["example", "examples", "fixture", "fixtures", "mock", "mocks", "sample", "samples", "test", "tests", "third-party", "vendor"],
			signals.FolderNameSignals.Select(signal => signal.FolderName).Order(StringComparer.Ordinal));
		Assert.Equal(2, signals.ContentMarkers.Length);
		Assert.Contains(signals.ContentMarkers, marker => marker.Zone is ContentMarkerZone.AnyZone);
		Assert.Contains(signals.ContentMarkers, marker => marker.Zone is ContentMarkerZone.CodeOnly);
	}

	[Fact]
	public void FourSubsumptionTriplesAreDeclaredAsTheCatalogStatesThem()
	{
		FrozenSet<SubsumptionRule> relation = ReadAnalyzer().SubsumptionRelation;

		Assert.Equal(4, relation.Count);
		Assert.Contains(relation, rule => rule is { SubsumingPatternClassIdentifier: _PS_002, SubsumedPatternClassIdentifier: _PS_001, When: SubsumptionCondition.AnchorPositionsAreIdentical });
		Assert.Contains(relation, rule => rule is { SubsumingPatternClassIdentifier: _PS_003, SubsumedPatternClassIdentifier: _PS_012, When: SubsumptionCondition.MatchSpansOverlap });
		Assert.Contains(relation, rule => rule is { SubsumingPatternClassIdentifier: _PS_013, SubsumedPatternClassIdentifier: _PS_009, When: SubsumptionCondition.MatchSpansOverlap });
		Assert.Contains(relation, rule => rule is { SubsumingPatternClassIdentifier: _PS_011, SubsumedPatternClassIdentifier: _PS_010, When: SubsumptionCondition.AnchorPositionsAreIdentical });
	}

	[Fact]
	public void TheSchemeInterpretationIsDeclaredForSixClassesAndTheExplicitParameterLeavesAnIndeterminateVerdictAlone()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		string[] withScheme =
		[
			.. analyzer.DetectionRulesByPatternClass
				.Where(entry => entry.Value.SymbolicVerdictInterpretation.IsSubjectToSymbolicResolution)
				.Select(entry => entry.Key)
				.Order(StringComparer.Ordinal)
		];

		Assert.Equal([_PS_003, _PS_004, _PS_005, _PS_006, _PS_007, _PS_008], withScheme);
		Assert.All(
			withScheme.Where(patternClass => patternClass != _PS_003),
			patternClass => Assert.Equal(
				SymbolicVerdictResponse.ProposesMediumLevel,
				analyzer.DetectionRulesByPatternClass[patternClass].SymbolicVerdictInterpretation.WhenIndeterminate));
		Assert.Equal(
			SymbolicVerdictResponse.NoEffect,
			analyzer.DetectionRulesByPatternClass[_PS_003].SymbolicVerdictInterpretation.WhenIndeterminate);
		Assert.Equal(
			SymbolicVerdictResponse.ProposesMediumLevel,
			analyzer.DetectionRulesByPatternClass[_PS_003].SymbolicVerdictInterpretation.WhenSchemeArgumentDidNotParticipate);
		Assert.All(
			withScheme.Where(patternClass => patternClass != _PS_003),
			patternClass => Assert.Null(
				analyzer.DetectionRulesByPatternClass[patternClass].SymbolicVerdictInterpretation.WhenSchemeArgumentDidNotParticipate));
	}

	[Fact]
	public void TheDeclaredUnsupportedCoverageIsWrittenForTheClientAndCarriesNoInternalTerm()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.NotEmpty(analyzer.LanguageIdentification.DeclaredUnsupportedCoverage);
		Assert.All(analyzer.LanguageIdentification.DeclaredUnsupportedCoverage, entry =>
		{
			Assert.Empty(UnsupportedCoverageInternalTermsIn(entry.Construct));
			Assert.Empty(UnsupportedCoverageInternalTermsIn(entry.Reason));
		});
	}

	/// <summary>
	/// Both internal-term checks still fail when a client-facing text carries an internal term: each identifier of the public-source guard, injected
	/// as the guard's own sample into a real template and a real unsupported-coverage entry, is reported, as is each of the checks' own terms.
	/// </summary>
	[Fact]
	public void TheInternalTermChecksStillReportEveryTermTheyAreBuiltFrom()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		string templateText = analyzer.CandidateSelectionAndRecommendation.TemplateProvider(_templatedClasses[0], CredentialFlow.ClientCredentials)!.CredentialProvenance;
		string coverageText = analyzer.LanguageIdentification.DeclaredUnsupportedCoverage.First().Reason;

		Assert.Empty(InternalTermsIn(templateText));
		Assert.Empty(UnsupportedCoverageInternalTermsIn(coverageText));
		Assert.All(PublicSourceIdentifierTests.InternalIdentifierSamples(), sample =>
		{
			Assert.NotEmpty(InternalTermsIn(templateText + _TERM_SEPARATOR + sample));
			Assert.NotEmpty(UnsupportedCoverageInternalTermsIn(coverageText + _TERM_SEPARATOR + sample));
		});
		Assert.All(PublicSourceIdentifierTests.InternalCoverageTerms(), term => Assert.NotEmpty(UnsupportedCoverageInternalTermsIn(coverageText + _TERM_SEPARATOR + term)));
	}

	// ------------------------------------------------------------------------------------------
	// One fixture per class, at the level the catalog gives it
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EveryClassFiresOnItsOwnInventedFixtureAtItsLevel()
	{
		PowerShellRecognitionHarness harness = new();

		foreach ((string patternClass, ConfidenceLevel level, string source) in ClassFixtures())
		{
			ImmutableArray<RecognizedConstruct> constructs = harness.Recognize(source);

			Assert.True(
				constructs.Any(construct => construct.PatternClassIdentifier == patternClass && construct.Level == level),
				$"{patternClass} did not fire at {level}; got [{Describe(constructs)}]");
		}
	}

	[Theory]
	[InlineData("$c = New-Object Vendor.Sdk.UsernamePasswordCredentials($loginName, $loginSecret)")]
	[InlineData("$c = New-Object UsernamePasswordCredentials ($u, $p)")]
	[InlineData("$c = New-Object Sdk.UsernamePasswordCredentials $u, $p")]
	[InlineData("$c = New-Object -TypeName Sdk.UsernamePasswordCredentials -ArgumentList $u, $p")]
	[InlineData("$c = New-Object -T Sdk.UsernamePasswordCredentials -A $u, $p")]
	[InlineData("$c = New-Object -ArgumentList $u, $p -TypeName Sdk.UsernamePasswordCredentials")]
	[InlineData("$c = New-Object Sdk.UsernamePasswordCredentials -ArgumentList $u, $p")]
	[InlineData("$c = New-Object -TypeName UsernamePasswordCredentials -ArgumentList @($u, $p)")]
	[InlineData("$c = New-Object -TypeName UsernamePasswordCredentials -ArgumentList $pair")]
	[InlineData("$c = New-Object 'Sdk.UsernamePasswordCredentials'($u, $p)")]
	[InlineData("$c = New-Object \"UsernamePasswordCredentials\" -ArgumentList $u, $p")]
	[InlineData("$c = [Sdk.UsernamePasswordCredentials]::new($u, $p)")]
	[InlineData("$c = [UsernamePasswordCredentials]::new($settings['User'], $settings['Password'])")]
	[InlineData("$c = [Sdk.Credentials.UsernamePasswordCredentials]::new($u, [System.Environment]::GetEnvironmentVariable('SECRET'))")]
	[InlineData("$c = NEW-OBJECT usernamepasswordcredentials($u, $p)")]
	public void TheSdkCredentialIsReadInEveryCreationFormAndEveryTypeSpelling(string source)
	{
		AssertFires(source, _PS_001, ConfidenceLevel.High);
	}

	[Fact]
	public void ABearerCredentialTypeOfTheSameSdkIsNotTheUserNameAndPasswordClass()
	{
		AssertSilent("$c = New-Object Sdk.BearerTokenCredentials($accessToken)");
	}

	[Fact]
	public void TheLegacyClientWithTheNestedCredentialIsOneFindingAndTheCredentialClassIsSubsumed()
	{
		const string source = """
			$proxy = New-Object Sdk.RSAPIClient ((New-Object System.Uri -ArgumentList $serviceAddress), (New-Object Sdk.UsernamePasswordCredentials('user', 'secret')))
			""";

		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Equal([_PS_002], constructs.Select(construct => construct.PatternClassIdentifier));
		Assert.Equal(ConfidenceLevel.High, constructs[0].Level);
		Assert.True(constructs[0].LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("$p = [Sdk.RSAPIClient]::new([Uri]$address, [Sdk.UsernamePasswordCredentials]::new($u, $p))")]
	[InlineData("$p = New-Object -TypeName Sdk.RSAPIClient -ArgumentList (New-Object System.Uri $address), (New-Object Sdk.UsernamePasswordCredentials($u, $p))")]
	[InlineData("$p = New-Object -ArgumentList $address, (New-Object Sdk.UsernamePasswordCredentials($u, $p)) -TypeName Sdk.RSAPIClient")]
	public void TheLegacyClientIsReadWhateverTheSpellingOfItsEndpoint(string source)
	{
		AssertFires(source, _PS_002, ConfidenceLevel.High);
		AssertNeverFires(source, _PS_001);
	}

	[Theory]
	[InlineData("$p = New-Object Sdk.RSAPIClient ($uri)")]
	[InlineData("$p = New-Object Sdk.RSAPIClient ($uri, (New-Object Sdk.BearerTokenCredentials($t)))")]
	public void TheLegacyClientWithABearerCredentialOrNoCredentialDeclines(string source)
	{
		AssertNeverFires(source, _PS_002);
	}

	// ------------------------------------------------------------------------------------------
	// PS-003: the explicit scheme parameter
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic")]
	[InlineData("Invoke-WebRequest -Uri $uri -Authentication Basic -Credential $credential")]
	[InlineData("irm $uri -Cred $c -Au Basic")]
	[InlineData("iwr $uri -Cr $c -Auth Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Credential:$c -Authentication:Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication 'Basic' -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication \"Basic\" -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication [Microsoft.PowerShell.Commands.WebAuthenticationType]::Basic")]
	[InlineData("Start-BitsTransfer -Source $s -Destination $d -Authentication Basic -Credential $c")]
	[InlineData("INVOKE-RESTMETHOD -URI $uri -AUTHENTICATION BASIC")]
	[InlineData("Invoke-RestMethod -Uri $uri <# note #> -Authentication <# another #> Basic")]
	[InlineData("curl $uri -Authentication Basic -Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $uri `\n\t-Credential $c `\n\t-Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri –Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri —Authentication Basic")]
	[InlineData("Invoke-RestMethod -Uri $uri ―Authentication Basic")]
	public void TheExplicitSchemeParameterIsReadUnderEveryPrefixDashAndSpelling(string source)
	{
		AssertFires(source, _PS_003, ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $t")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication OAuth -Token $t")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication None")]
	[InlineData("Invoke-RestMethod -Uri $uri -Authentication 'Bearer' -Token $t")]
	[InlineData("Invoke-RestMethod -Uri $uri -AllowUnencryptedAuthentication -Credential $c")]
	public void ABearerOrOtherSchemeAndTheUnencryptedSwitchAreNotTheBasicParameter(string source)
	{
		AssertNeverFires(source, _PS_003);
	}

	[Fact]
	public void ASchemeVariableIsResolvedThroughTheSymbolTableInEverySpellingAndComparedWithoutCase()
	{
		AssertFires("$scheme = 'Basic'\nInvoke-RestMethod -Uri $u -Authentication $scheme -Credential $c", _PS_003, ConfidenceLevel.High);
		AssertFires("${scheme} = 'BASIC'\nInvoke-RestMethod -Uri $u -Authentication $SCHEME", _PS_003, ConfidenceLevel.High);
		AssertFires("$script:scheme = 'basic'\nInvoke-RestMethod -Uri $u -Authentication $scheme", _PS_003, ConfidenceLevel.High);
		AssertNeverFires("$scheme = 'Bearer'\nInvoke-RestMethod -Uri $u -Authentication $scheme -Token $t", _PS_003);
	}

	[Fact]
	public void ATwiceDeclaredSchemeVariableIsAmbiguousAndAParameterDefaultIsNeverResolved()
	{
		AssertFires("$scheme = 'Bearer'\n$scheme = 'Basic'\nInvoke-RestMethod -Uri $u -Authentication $scheme", _PS_003, ConfidenceLevel.High);
		AssertFires("function Get-Data($scheme = 'Bearer') {\n\tInvoke-RestMethod -Uri $u -Authentication $scheme\n}", _PS_003, ConfidenceLevel.High);
	}

	[Fact]
	public void TheExplicitParameterInASplatHashtableIsReadWhenAWebCmdletSplatsItWithinTwentyLines()
	{
		const string source = """
			$params = @{
				Uri            = $uri
				Credential     = $c
				Authentication = 'Basic'
			}
			Invoke-RestMethod @params
			""";

		AssertFires(source, _PS_003, ConfidenceLevel.High);
	}

	[Fact]
	public void AHashtableKeyNamedAsTheParameterThatNoWebCmdletSplatsIsAMailConfigurationAndNotThisClass()
	{
		const string source = """
			$mail = @{
				Authentication = 'Basic'
			}
			Send-Notification @mail
			Invoke-RestMethod -Uri $uri
			""";

		AssertNeverFires(source, _PS_003);
	}

	[Fact]
	public void ASplatFarBeyondTheTwentyLineWindowIsNotRead()
	{
		string source = "$params = @{ Authentication = 'Basic' }\n" + string.Concat(Enumerable.Repeat("$total = $a + $b\n", 25)) + "Invoke-RestMethod @params\n";

		AssertNeverFires(source, _PS_003);
	}

	// ------------------------------------------------------------------------------------------
	// PS-004 to PS-007: the Basic header
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Headers @{ Authorization = \"Basic $encoded\" }")]
	[InlineData("$h = @{ 'Authorization' = 'Basic ' + $encoded }")]
	[InlineData("$h = @{ \"Authorization\" = \"Basic $($encoded)\" }")]
	[InlineData("$h = [ordered]@{ authorization = 'Basic {0}' -f $encoded }")]
	[InlineData("$h = @{ Authorization = [string]::Format('Basic {0}', $encoded) }")]
	[InlineData("$h = @{ Authorization = ('Basic', $encoded) -join ' ' }")]
	[InlineData("$h = @{ Authorization = 'Basic dXNlcjpwdw==' }")]
	[InlineData("$h = @{ Authorization = Get-BasicAuthorizationValue $u $p }")]
	[InlineData("$h = @{\n\tAccept = 'application/json'\n\tAuthorization = \"Basic $encoded\"\n}")]
	[InlineData("$scheme = 'Basic'\n$h = @{ Authorization = $scheme + ' ' + $encoded }")]
	[InlineData("$scheme = 'Basic'\n$h = @{ Authorization = \"$($scheme) $encoded\" }")]
	[InlineData("$value = \"Basic $encoded\"\n$h = @{ Authorization = $value }")]
	[InlineData("$h = @{ Authorization = \"$scheme $encoded\" }")]
	public void TheHashtableLiteralWithASchemeBearingValueIsReadWhereverItIsUsed(string source)
	{
		AssertFires(source, _PS_004, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("$h = @{ Authorization = \"Bearer $accessToken\" }")]
	[InlineData("$h = @{ 'Proxy-Authorization' = \"Basic $encoded\" }")]
	[InlineData("$h = @{ 'X-Authorization-Mode' = \"Basic $encoded\" }")]
	[InlineData("$h = @{ Authorization = $token }")]
	[InlineData("$scheme = 'Bearer'\n$h = @{ Authorization = $scheme + ' ' + $encoded }")]
	[InlineData("$h = $Request.Headers['Authorization']")]
	public void ABearerHeaderAProxyKeyAnOpaqueValueAndAReadAreNotTheBasicHeader(string source)
	{
		AssertNeverFires(source, _PS_004);
		AssertNeverFires(source, _PS_005);
	}

	[Fact]
	public void ABearerHeaderBesideABasicOneIsJudgedOnItsOwnAndOnlyTheBasicValueIsReported()
	{
		const string source = """
			$bearer = @{ Authorization = "Bearer $accessToken" }
			$basic = @{ Authorization = "Basic $encoded" }
			""";

		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Single(constructs);
		Assert.Equal(_PS_004, constructs[0].PatternClassIdentifier);
	}

	[Theory]
	[InlineData("$headerTable.Add('Authorization', 'Basic ' + $encoded)")]
	[InlineData("$headerTable.Set(\"Authorization\", \"Basic $encoded\")")]
	[InlineData("$h.Append('Authorization', \"Basic $encoded\")")]
	[InlineData("$headerTable['Authorization'] = \"Basic $encoded\"")]
	[InlineData("$headerTable.Authorization = \"Basic $encoded\"")]
	[InlineData("$httpClient.DefaultRequestHeaders.Authorization = \"Basic $encoded\"")]
	[InlineData("$httpClient.DefaultRequestHeaders.TryAddWithoutValidation('Authorization', \"Basic $encoded\")")]
	[InlineData("$client.AddHeader('Authorization', 'Basic ' + $encoded)")]
	[InlineData("$request.setRequestHeader(\"Authorization\", \"Basic \" + $enc)")]
	[InlineData("$request.Headers.Add(\"Authorization: Basic $encoded\")")]
	[InlineData("$request.Headers.Add('Authorization:Basic dXNlcjpwdw==')")]
	[InlineData("$value = 'Basic ' + $encoded\n$client.Headers.Add('Authorization', $value)")]
	[InlineData("$value = [string]::Format('Basic {0}', $encoded)\n$h['Authorization'] = $value")]
	public void AWriteOfTheHeaderOnAnyReceiverIsReadInEveryWriteForm(string source)
	{
		AssertFires(source, _PS_005, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("$request.Headers.Add(\"Proxy-Authorization: Basic $encoded\")")]
	[InlineData("$h.Add('Content-Type', 'application/json')")]
	[InlineData("$headerTable.Add('Authorization', \"Bearer $accessToken\")")]
	[InlineData("$headerTable.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Basic', $e)")]
	public void AnotherKeyAProxyKeyABearerValueAndATypedValueAreNotTheStringKeyWrite(string source)
	{
		AssertNeverFires(source, _PS_005);
	}

	[Theory]
	[InlineData("$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, \"Basic $encoded\")")]
	[InlineData("$request.Headers.Set([Net.HttpRequestHeader]::Authorization, 'Basic ' + $encoded)")]
	[InlineData("$request.Headers.Add([HttpRequestHeader]::Authorization, \"Basic $encoded\")")]
	[InlineData("$client.Headers[[System.Net.HttpRequestHeader]::Authorization] = \"Basic $encoded\"")]
	public void TheEnumerationWriteIsReadFullyQualifiedPartlyQualifiedOrByItsShortName(string source)
	{
		AssertFires(source, _PS_006, ConfidenceLevel.Medium);
	}

	[Fact]
	public void AnotherMemberOfTheHeaderEnumerationIsNotTheAuthorizationHeader()
	{
		AssertNeverFires("$request.Headers.Add([System.Net.HttpRequestHeader]::ContentType, \"Basic $encoded\")", _PS_006);
	}

	[Theory]
	[InlineData("$h = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Basic', $encoded)")]
	[InlineData("$h = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Basic', $encoded)")]
	[InlineData("$h = New-Object -TypeName AuthenticationHeaderValue -ArgumentList 'Basic', $encoded")]
	[InlineData("$h = New-Object AuthenticationHeaderValue -ArgumentList 'Basic', $encoded")]
	[InlineData("$h = New-Object -ArgumentList 'Basic', $encoded -TypeName AuthenticationHeaderValue")]
	[InlineData("$scheme = 'Basic'\n$h = New-Object AuthenticationHeaderValue -ArgumentList $scheme, $encoded")]
	[InlineData("$h = New-Object AuthenticationHeaderValue -ArgumentList $pair")]
	[InlineData("$h = [AuthenticationHeaderValue]::Parse(\"Basic $encoded\")")]
	[InlineData("$ok = [AuthenticationHeaderValue]::TryParse('Basic dXNlcjpwdw==', [ref]$parsed)")]
	[InlineData("$h = [System.Net.Http.Headers.AuthenticationHeaderValue]\"Basic $encoded\"")]
	[InlineData("$h = [AuthenticationHeaderValue]'Basic dXNlcjpwdw=='")]
	public void ATypedHeaderValueIsReadInEveryConstructionParseAndCastForm(string source)
	{
		AssertFires(source, _PS_007, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("$h = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $token)")]
	[InlineData("$scheme = 'Bearer'\n$h = New-Object AuthenticationHeaderValue -ArgumentList $scheme, $token")]
	[InlineData("$h = [AuthenticationHeaderValue]::Parse(\"Bearer $token\")")]
	public void ABearerTypedHeaderValueIsSuppressedByTheSchemeVerdict(string source)
	{
		AssertNeverFires(source, _PS_007);
	}

	// ------------------------------------------------------------------------------------------
	// PS-008 to PS-011
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("$cache.Add([Uri]$uri, 'Basic', (New-Object System.Net.NetworkCredential($loginName, $loginSecret)))")]
	[InlineData("$cache.Add([Uri]$uri, 'Basic', $networkCredential)")]
	[InlineData("$cache.Add($uri, 'Basic', $cred.GetNetworkCredential())")]
	[InlineData("$cache.Add($hostName, 443, 'Basic', $cred)")]
	[InlineData("$cache.Add((New-Object Uri $u), \"basic\", [System.Net.NetworkCredential]$cred)")]
	[InlineData("$scheme = 'Basic'\n$cache.Add($uri, $scheme, $cred)")]
	public void TheCredentialCacheEntryIsReadWithTheCredentialAConstructionACallOrAVariable(string source)
	{
		AssertFires(source, _PS_008, ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("$cache.Add([Uri]$uri, 'Negotiate', $cred)")]
	[InlineData("$cache.Add([Uri]$uri, 'NTLM', $cred)")]
	[InlineData("$cache.Add([Uri]$uri, 'Digest', $cred)")]
	[InlineData("$list.Add($a, $b)")]
	public void AnotherSchemeAndACollectionWithTwoArgumentsAreNotTheCredentialCacheClass(string source)
	{
		AssertNeverFires(source, _PS_008);
	}

	[Theory]
	[InlineData("$handler = New-Object System.Net.Http.HttpClientHandler\n$handler.Credentials = New-Object System.Net.NetworkCredential($loginName, $loginSecret)")]
	[InlineData("$wc = New-Object System.Net.WebClient\n$wc.Credentials = $cred.GetNetworkCredential()")]
	[InlineData("$wc = [System.Net.WebClient]::new()\n$wc.Credentials = [System.Net.NetworkCredential]$cred")]
	[InlineData("$h = New-Object System.Net.Http.SocketsHttpHandler\n$h.Credentials = New-Object System.Net.NetworkCredential('a', 'b')")]
	[InlineData("$h = New-Object -TypeName WinHttpHandler\n$h.Credentials = (New-Object NetworkCredential($u, $p))")]
	[InlineData("$request = [System.Net.WebRequest]::Create($url)\n$request.Credentials = New-Object System.Net.NetworkCredential($u, $p)")]
	[InlineData("$request = [System.Net.HttpWebRequest][System.Net.WebRequest]::CreateHttp($url)\n$request.Credentials = New-Object System.Net.NetworkCredential($u, $p)")]
	[InlineData("$h = [System.Net.Http.HttpClientHandler]@{ Credentials = New-Object System.Net.NetworkCredential($u, $p) }")]
	[InlineData("$w = New-Object System.Net.WebClient -Property @{ Credentials = (New-Object System.Net.NetworkCredential($u, $p)) }")]
	public void ACredentialOnAnAllowListedHttpReceiverIsReadWhereverItIsGiven(string source)
	{
		AssertFires(source, _PS_009, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("$handler = New-Object System.Net.Http.HttpClientHandler\n$handler.Credentials = [System.Net.CredentialCache]::DefaultCredentials")]
	[InlineData("$handler = New-Object System.Net.Http.HttpClientHandler\n$handler.UseDefaultCredentials = $true")]
	[InlineData("$handler = New-Object System.Net.Http.HttpClientHandler\n$handler.Credentials = $networkCredential")]
	public void IntegratedAuthenticationAndAVariableAreNotAConstructedCredentialOnAnHttpType(string source)
	{
		AssertNeverFires(source, _PS_009);
	}

	[Fact]
	public void TheReceiverMustBeCreatedWithinTheEightLinesBeforeTheAssignment()
	{
		string source = "$wc = New-Object System.Net.WebClient\n" + string.Concat(Enumerable.Repeat("$total = $a + $b\n", 9)) + "$wc.Credentials = New-Object System.Net.NetworkCredential($u, $p)\n";

		AssertNeverFires(source, _PS_009);
	}

	[Fact]
	public void ACredentialOnANonHttpSinkSubsumesTheHttpTypeClassAndEmitsOnlyTheLowRow()
	{
		const string source = """
			$wc = New-Object System.Net.WebClient
			$mailer = New-Object System.Net.Mail.SmtpClient($server)
			$mailer.Credentials = New-Object System.Net.NetworkCredential($loginName, $loginSecret)
			""";

		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Equal([_PS_013], constructs.Select(construct => construct.PatternClassIdentifier));
		Assert.Equal(ConfidenceLevel.Low, constructs[0].Level);
	}

	[Theory]
	[InlineData("$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(\"$($loginName):$($loginSecret)\"))")]
	[InlineData("$pair = \"${u}:${p}\"\n$encoded = [System.Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($pair))")]
	[InlineData("$pair = \"$u`:$p\"\n$enc = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($pair))")]
	[InlineData("$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($u + ':' + $p))")]
	[InlineData("$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($u+':'+$p))")]
	[InlineData("$pair = ($u, $p) -join ':'\n$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))")]
	[InlineData("$pair = '{0}:{1}' -f $u, $p\n$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))")]
	[InlineData("$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes([string]::Format('{0}:{1}', $u, $p)))")]
	[InlineData("$pair = 'someone:secret'\n$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))")]
	[InlineData("$parts = $u, $p\n$x = 1\n$y = 2\n$pair = $parts -join ':'\n$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))")]
	[InlineData("$enc = [Convert]::ToBase64String([Text.Encoding]::GetEncoding('ISO-8859-1').GetBytes(\"$($u):$($p)\"))")]
	[InlineData("$bytes = (New-Object Text.UTF8Encoding).GetBytes(\"$($u):$($p)\")\n$enc = [Convert]::ToBase64String($bytes)")]
	[InlineData("$pair = $settings['User'] + ':' + $settings['Password']\n$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))")]
	[InlineData("$enc = [CONVERT]::TOBASE64STRING([Text.Encoding]::ASCII.GetBytes($U + ':' + $P))")]
	public void AColonPairOfTwoNonEmptyHalvesReachingTheEncoderIsReadInEverySpelling(string source)
	{
		AssertFires(source, _PS_010, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($u))")]
	[InlineData("$enc = [Convert]::ToBase64String($documentBytes)")]
	public void AnEncoderOverASingleValueIsNotThePairClass(string source)
	{
		AssertNeverFires(source, _PS_010);
	}

	[Fact]
	public void APairMoreThanTenLinesBeforeTheEncoderIsNotReadAndOneWithinTenLinesIs()
	{
		string far = "$pair = $u + ':' + $p\n" + string.Concat(Enumerable.Repeat("$total = 1\n", 12)) + "$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))\n";
		string near = "$pair = $u + ':' + $p\n" + string.Concat(Enumerable.Repeat("$total = 1\n", 8)) + "$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))\n";

		AssertNeverFires(far, _PS_010);
		AssertFires(near, _PS_010, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("$pair = \":\" + $accessToken\n$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))")]
	[InlineData("$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($token + ':' + ''))")]
	[InlineData("$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($null + ':' + $t))")]
	[InlineData("$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('' + ':' + $t))")]
	[InlineData("$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes([string]::Empty + ':' + $t))")]
	[InlineData("$pair = \":$($t)\"\n$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))")]
	[InlineData("$pair = '{0}:{1}' -f $null, $t\n$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))")]
	public void ATokenWithOneEmptyHalfUnderTheBasicSchemeIsTheTokenClassAndNeverThePairClass(string source)
	{
		AssertFires(source, _PS_011, ConfidenceLevel.Medium);
		AssertNeverFires(source, _PS_010);
	}

	// ------------------------------------------------------------------------------------------
	// PS-012 to PS-014: the Low classes
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $uri -Credential $credential")]
	[InlineData("iwr $u -Cr $c")]
	[InlineData("irm $u -Cred $c")]
	[InlineData("curl $u -Credential:$c")]
	[InlineData("wget $u -Crede $c")]
	[InlineData("Invoke-WebRequest -Uri $u –Credential $c")]
	[InlineData("Invoke-RestMethod -Uri $u `\n\t-Credential $c")]
	[InlineData("INVOKE-RESTMETHOD -URI $U -CREDENTIAL $C")]
	[InlineData("New-WebServiceProxy -Uri $u -Credential $c")]
	[InlineData("$p = @{ Uri = $u; Credential = $c }\nInvoke-WebRequest @p")]
	[InlineData("$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession\n$session.Credentials = New-Object System.Net.NetworkCredential($u, $p)")]
	[InlineData("$session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()\n$session.Credentials = $cred.GetNetworkCredential()")]
	[InlineData("Invoke-RestMethod -Uri $u -SessionVariable s\n$s.Credentials = $networkCredential")]
	[InlineData("$s = [Microsoft.PowerShell.Commands.WebRequestSession]@{ Credentials = $c }")]
	public void ACredentialOnAWebCmdletOrAWebSessionWithoutASchemeIsLowAndForManualReview(string source)
	{
		AssertFires(source, _PS_012, ConfidenceLevel.Low);
	}

	[Theory]
	[InlineData("Invoke-RestMethod -Uri $u -UseDefaultCredentials")]
	[InlineData("Invoke-RestMethod -Uri $u -CertificateThumbprint $t")]
	[InlineData("Invoke-RestMethod -Uri $u -Certificate $cert")]
	[InlineData("$c = Get-Credential\nInvoke-RestMethod -Uri $u")]
	[InlineData("Get-Credential -Message 'x'")]
	[InlineData("$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession\n$session.Credentials = [System.Net.CredentialCache]::DefaultCredentials")]
	[InlineData("Invoke-RestMethod -Uri $u -Authentication Bearer -Credential $c")]
	[InlineData("Invoke-Command -ComputerName $s -Credential $c -ScriptBlock { 1 }")]
	public void IntegratedAuthenticationCertificatesAndACommandThatAlsoCarriesTheSchemeAreNotTheWebCredentialClass(string source)
	{
		AssertNeverFires(source, _PS_012);
	}

	[Fact]
	public void ACommandThatSendsBasicExplicitlyIsNotAlsoReportedAsACredentialThatSendsNothing()
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(
			"Invoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic");

		Assert.Equal([_PS_003], constructs.Select(construct => construct.PatternClassIdentifier));
	}

	[Theory]
	[InlineData("Invoke-Command -ComputerName $server -Credential $credential -ScriptBlock { hostname }")]
	[InlineData("Enter-PSSession -ComputerName $s -Credential $c")]
	[InlineData("New-PSSession -ComputerName $s -Cred $c")]
	[InlineData("New-PSDrive -Name Z -PSProvider FileSystem -Root $share -Credential $c")]
	[InlineData("Start-Process notepad -Credential $c")]
	[InlineData("Start-Job -ScriptBlock { 1 } -Credential $c")]
	[InlineData("New-CimSession -ComputerName $s -Credential $c")]
	[InlineData("Get-WmiObject Win32_Bios -ComputerName $s -Credential $c")]
	[InlineData("Send-MailMessage -To $a -Credential $c -SmtpServer $s")]
	[InlineData("$mailer = New-Object System.Net.Mail.SmtpClient\n$mailer.Credentials = New-Object System.Net.NetworkCredential($u, $p)")]
	[InlineData("$proxy = [System.Net.WebProxy]::new($address)\n$proxy.Credentials = New-Object System.Net.NetworkCredential($u, $p)")]
	[InlineData("$request = [System.Net.FtpWebRequest][System.Net.WebRequest]::Create('ftp://host/file')\n$request.Credentials = New-Object System.Net.NetworkCredential($u, $p)")]
	[InlineData("$root = New-Object System.DirectoryServices.DirectoryEntry($path, $u, $p)")]
	[InlineData("$root = [System.DirectoryServices.DirectoryEntry]::new($path, $u, 'literal')")]
	public void ACredentialConsumerOfTheClosedNonHttpSetIsALowRowForManualReview(string source)
	{
		AssertFires(source, _PS_013, ConfidenceLevel.Low);
	}

	[Fact]
	public void ADirectoryEntryTakesItsCredentialAsTextAndALiteralPasswordRaisesTheSecuritySignal()
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(
			"$root = New-Object System.DirectoryServices.DirectoryEntry($path, $u, 'literal')");

		Assert.True(constructs.Single().LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("function New-BasicAuthorizationHeader {\n\tparam($loginName, $loginSecret)\n}")]
	[InlineData("function Get-BasicAuthHeader { }")]
	[InlineData("filter ConvertTo-BasicCredentialValue { $_ }")]
	[InlineData("function global:Set-BasicHeaderValue { }")]
	public void AFunctionNamedForTheSchemeAndARoleWordIsTheHelperDefinitionAtLow(string source)
	{
		AssertFires(source, _PS_014, ConfidenceLevel.Low);
	}

	[Theory]
	[InlineData("function Get-BasicallyAuthorized { }")]
	[InlineData("function Get-BasicInformation { }")]
	[InlineData("function New-AuthorizationHeader { }")]
	public void AFunctionWithTheWordBasicInsideAnotherWordOrWithNoSchemeWordIsNotTheHelperDefinition(string source)
	{
		AssertNeverFires(source, _PS_014);
	}

	// ------------------------------------------------------------------------------------------
	// Zones, near-misses and the demotion signals
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("Invoke-WebRequest -Uri $uri -UseBasicParsing")]
	[InlineData("Invoke-WebRequest -Uri $uri -UseBasicParsing -UseDefaultCredentials")]
	[InlineData("$handler = New-Object System.Net.Http.HttpClientHandler\n$handler.Credentials = [System.Net.CredentialCache]::DefaultCredentials")]
	[InlineData("$name = 'Basic'")]
	public void TheNearMissesTheCatalogNamesStayOutOfEveryClass(string source)
	{
		Assert.Empty(new PowerShellRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("# Invoke-RestMethod -Uri $u -Credential $c -Authentication Basic")]
	[InlineData("<# .EXAMPLE\n Invoke-RestMethod -Uri $u -Credential $c -Authentication Basic #>")]
	[InlineData("$help = \"Invoke-RestMethod -Uri $u -Credential $c -Authentication Basic\"")]
	[InlineData("$help = @'\nInvoke-RestMethod -Uri $u -Credential $c -Authentication Basic\n'@")]
	[InlineData("# $c = New-Object Sdk.UsernamePasswordCredentials($u, $p)")]
	public void AMatchWhoseAnchorLiesInACommentOrAStringBodyComposesToLow(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.NotEmpty(constructs);
		Assert.All(constructs, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
	}

	[Theory]
	[InlineData("tests/Basic.Tests.ps1")]
	[InlineData("src/Module.TESTS.PS1")]
	[InlineData("test/helper.ps1")]
	[InlineData("src/Tests/helper.ps1")]
	[InlineData("mock/helper.ps1")]
	[InlineData("src/mocks/helper.ps1")]
	[InlineData("fixtures/helper.ps1")]
	[InlineData("fixture/helper.ps1")]
	[InlineData("docs/Samples/helper.ps1")]
	[InlineData("examples/helper.ps1")]
	[InlineData("src/SampleUpload.ps1")]
	[InlineData("src/exampleHelper.ps1")]
	[InlineData("src/Module.generated.ps1")]
	[InlineData("src/Module.g.ps1")]
	public void AFileOrFolderTheLanguageNamesAsTestMockFixtureSampleOrGeneratedIsDemotedToLow(string path)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(
			"Invoke-RestMethod -Uri $uri -Credential $c -Authentication Basic",
			path);

		Assert.Single(constructs);
		Assert.Equal(ConfidenceLevel.Low, constructs[0].Level);
		Assert.True(constructs[0].LowReasons.HasFlag(LowConfidenceReasons.DemotedFile), path);
	}

	[Theory]
	[InlineData("vendor/Module.ps1")]
	[InlineData("third-party/lib/Module.ps1")]
	public void AVendoredFolderDemotesToLowWithTheVendoredReason(string path)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(
			"Invoke-RestMethod -Uri $uri -Credential $c -Authentication Basic",
			path);

		Assert.Equal(ConfidenceLevel.Low, constructs.Single().Level);
		Assert.True(constructs.Single().LowReasons.HasFlag(LowConfidenceReasons.DemotedVendoredFile), path);
	}

	[Theory]
	[InlineData("Deployment/Install.ps1")]
	[InlineData("deployment/scripts/Install.ps1")]
	[InlineData("src/Deployment/Tasks/Install.psm1")]
	public void AHighConstructInAScriptUnderADeploymentFolderStaysHigh(string path)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(
			"Invoke-RestMethod -Uri $uri -Credential $c -Authentication Basic",
			path);

		Assert.Equal(ConfidenceLevel.High, constructs.Single().Level);
	}

	[Theory]
	[InlineData("# Auto-generated by a tool. Do not edit.\nInvoke-RestMethod -Uri $u -Credential $c -Authentication Basic")]
	[InlineData("\n#  This file was generated by the build\nInvoke-RestMethod -Uri $u -Credential $c -Authentication Basic")]
	[InlineData("<#\n Autogenerated module, do not edit\n#>\nInvoke-RestMethod -Uri $u -Credential $c -Authentication Basic")]
	public void AGeneratedHeaderInTheFirstTenLinesDemotesTheFile(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Equal(ConfidenceLevel.Low, constructs.Single().Level);
	}

	[Fact]
	public void AGeneratedHeaderBeyondTheFirstTenLinesAndTheWordInCodeDoNotDemote()
	{
		string far = string.Concat(Enumerable.Repeat("$total = 1\n", 12)) + "# generated by hand\nInvoke-RestMethod -Uri $u -Credential $c -Authentication Basic";
		const string inCode = "Write-Host 'generated by'\nInvoke-RestMethod -Uri $u -Credential $c -Authentication Basic";
		PowerShellRecognitionHarness harness = new();

		Assert.Equal(ConfidenceLevel.High, harness.Recognize(far).Single().Level);
		Assert.Equal(ConfidenceLevel.High, harness.Recognize(inCode).Single().Level);
	}

	[Fact]
	public void APesterFileNotNamedAsOneIsDemotedByItsContentAndTheWordsInAStringAreNot()
	{
		const string pester = """
			Describe 'the client' {
				It 'sends the header' {
					Invoke-RestMethod -Uri $u -Credential $c -Authentication Basic | Should -Not -BeNullOrEmpty
				}
			}
			""";
		const string inString = """
			$note = "Describe 'x' { It 'y' { 1 | Should -Be 1 } }"
			Invoke-RestMethod -Uri $u -Credential $c -Authentication Basic
			""";
		PowerShellRecognitionHarness harness = new();

		Assert.Equal(ConfidenceLevel.Low, harness.Recognize(pester).Single().Level);
		Assert.Equal(ConfidenceLevel.High, harness.Recognize(inString).Single().Level);
	}

	[Fact]
	public void AShebangOnlyScriptIsReadLikeAnyOther()
	{
		AssertFires("#!/usr/bin/env pwsh\nInvoke-RestMethod -Uri $u -Credential $c -Authentication Basic", _PS_003, ConfidenceLevel.High);
	}

	// ------------------------------------------------------------------------------------------
	// The literal-credential signal
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("$c = New-Object Sdk.UsernamePasswordCredentials('user', 'secret')", true)]
	[InlineData("$c = New-Object Sdk.UsernamePasswordCredentials($u, 'secret')", true)]
	[InlineData("$c = New-Object Sdk.UsernamePasswordCredentials($u, $p)", false)]
	[InlineData("$c = New-Object Sdk.UsernamePasswordCredentials($u, $settings['Password'])", false)]
	[InlineData("$c = New-Object Sdk.UsernamePasswordCredentials($u, $env:SERVICE_PASSWORD)", false)]
	[InlineData("$c = New-Object Sdk.UsernamePasswordCredentials($u, \"$($cfg.Password)\")", false)]
	[InlineData("$h = @{ Authorization = 'Basic dXNlcjpwdw==' }", true)]
	[InlineData("$h = @{ Authorization = \"Basic $encoded\" }", false)]
	[InlineData("$h = @{ Authorization = 'Basic ' + $encoded }", false)]
	[InlineData("$pair = 'someone:secret'\n$e = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))", true)]
	[InlineData("$e = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(\"$($u):$($p)\"))", false)]
	public void TheLiteralCredentialSignalComesFromTheZoneOfTheCapturedCredentialAndNeverFromTheValue(string source, bool expected)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.LiteralCredentialSignal == expected);
		Assert.DoesNotContain(constructs, construct => construct.LiteralCredentialSignal != expected && construct.Level != ConfidenceLevel.Low && constructs.Length == 1);
	}

	// ------------------------------------------------------------------------------------------
	// Flow signals
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void NineFlowSignalsAreDeclaredFiveInteractiveAndFourFixed()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.Equal(9, signals.Length);
		Assert.Equal(5, signals.Count(signal => signal.Kind is FlowSignalKind.InteractiveUser));
		Assert.Equal(4, signals.Count(signal => signal.Kind is FlowSignalKind.FixedCredential));
		Assert.Equal(signals.Length, signals.Select(signal => signal.Name).Distinct(StringComparer.Ordinal).Count());
		Assert.Equal(
			FlowSignalZone.CodeOrStringBody,
			signals.Single(signal => signal.Name == "ReadHostCredentialPromptText").Zone);
		Assert.All(
			signals.Where(signal => signal.Name != "ReadHostCredentialPromptText"),
			signal => Assert.Equal(FlowSignalZone.CodeOnly, signal.Zone));
	}

	[Theory]
	[InlineData("$c = Get-Credential -Message 'prompt'", FlowEvidence.Interactive)]
	[InlineData("$c = get-credential", FlowEvidence.Interactive)]
	[InlineData("$s = Read-Host -Prompt 'x' -AsSecureString", FlowEvidence.Interactive)]
	[InlineData("$s = Read-Host 'x' -As", FlowEvidence.Interactive)]
	[InlineData("$s = Read-Host -MaskInput", FlowEvidence.Interactive)]
	[InlineData("$c = $Host.UI.PromptForCredential('t', 'm', '', '')", FlowEvidence.Interactive)]
	[InlineData("$u = Read-Host 'Enter your user name'", FlowEvidence.Interactive)]
	[InlineData("$u = Read-Host -Prompt \"Please type the password\"", FlowEvidence.Interactive)]
	[InlineData("$box = New-Object System.Windows.Controls.PasswordBox\n$v = $box.Password", FlowEvidence.Interactive)]
	[InlineData("$text.UseSystemPasswordChar = $true", FlowEvidence.Interactive)]
	[InlineData("$s = $env:SERVICE_PASSWORD", FlowEvidence.Fixed)]
	[InlineData("$s = ${env:Api_Secret}", FlowEvidence.Fixed)]
	[InlineData("$s = [Environment]::GetEnvironmentVariable('MY_API_KEY')", FlowEvidence.Fixed)]
	[InlineData("$s = Get-Item Env:SERVICE_TOKEN", FlowEvidence.Fixed)]
	[InlineData("$c = Import-Clixml $path", FlowEvidence.Fixed)]
	[InlineData("$c = Get-Secret -Name x", FlowEvidence.Fixed)]
	[InlineData("$c = Get-AutomationPSCredential -Name x", FlowEvidence.Fixed)]
	[InlineData("$s = ConvertTo-SecureString $plain -AsPlainText -Force", FlowEvidence.Fixed)]
	[InlineData("$s = ConvertTo-SecureString -String $plain -AsPlainText -Force", FlowEvidence.Fixed)]
	public void EachFlowSignalFiresOnItsOwnInventedFixtureAlongsideAHighConstruct(string signalSource, FlowEvidence expected)
	{
		const string construct = "\nInvoke-RestMethod -Uri $u -Credential $c -Authentication Basic\n";

		RecognitionResult result = new PowerShellRecognitionHarness().RecognizeFully(signalSource + construct);

		Assert.Equal(expected, result.FlowEvidence);
	}

	[Theory]
	[InlineData("$s = $env:Path")]
	[InlineData("$s = $env:Compass")]
	[InlineData("$s = Get-CredentialFromVault -Name x")]
	[InlineData("$s = Get-SecretInfo")]
	[InlineData("# $c = Get-Credential")]
	[InlineData("$note = 'Get-Credential is described here'")]
	[InlineData("$s = ConvertTo-SecureString 'abc' -AsPlainText -Force")]
	[InlineData("$s = ConvertTo-SecureString -String \"abc\" -AsPlainText -Force")]
	[InlineData("$s = ConvertTo-SecureString -AsPlainText -Force 'abc'")]
	[InlineData("$s = ConvertTo-SecureString $encrypted")]
	[InlineData("Invoke-Command -ComputerName $s -Credential (Get-Credential) -ScriptBlock { 1 }")]
	[InlineData("Invoke-Command -ComputerName $s -Credential:(Get-Credential) -ScriptBlock { 1 }")]
	[InlineData("Enter-PSSession -ComputerName $s -Cred (Get-Credential)")]
	[InlineData("$u = Read-Host 'Enter the server name'")]
	public void ANonSignalAQuotedNameACommentALiteralAndTheDirectRemotingPromptCarryNoFlowEvidence(string signalSource)
	{
		const string construct = "\nInvoke-RestMethod -Uri $u -Credential $c -Authentication Basic\n";

		RecognitionResult result = new PowerShellRecognitionHarness().RecognizeFully(signalSource + construct);

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	[Fact]
	public void APromptAssignedToAVariableAndPassedToARemotingCmdletLaterIsStillInteractiveAsTheCatalogDeclares()
	{
		const string source = """
			$credential = Get-Credential
			Invoke-Command -ComputerName $server -Credential $credential -ScriptBlock { hostname }
			Invoke-RestMethod -Uri $u -Credential $c -Authentication Basic
			""";

		Assert.Equal(FlowEvidence.Interactive, new PowerShellRecognitionHarness().RecognizeFully(source).FlowEvidence);
	}

	[Fact]
	public void AFileWithBothKindsOfEvidenceIsInteractiveAsTheBaseRuleStates()
	{
		const string source = """
			$credential = $null
			if ($env:SERVICE_PASSWORD) { $credential = New-Object System.Management.Automation.PSCredential('x', $secure) }
			if (-not $credential) { $credential = Get-Credential }
			Invoke-RestMethod -Uri $u -Credential $c -Authentication Basic
			""";

		Assert.Equal(FlowEvidence.Interactive, new PowerShellRecognitionHarness().RecognizeFully(source).FlowEvidence);
	}

	[Fact]
	public void ACredentialWrittenIntoSourceIsNotFlowEvidence()
	{
		const string source = "$c = New-Object Sdk.UsernamePasswordCredentials('user', 'secret')";

		Assert.Equal(FlowEvidence.None, new PowerShellRecognitionHarness().RecognizeFully(source).FlowEvidence);
	}

	// ------------------------------------------------------------------------------------------
	// Recommendations
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void ElevenClassesCarryATemplateForBothFlowsAndTheThreeLowClassesCarryNone()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		RecommendationTemplateProvider provider = analyzer.CandidateSelectionAndRecommendation.TemplateProvider;

		foreach (CredentialFlow flow in Enum.GetValues<CredentialFlow>())
		{
			Assert.All(_templatedClasses, patternClass => Assert.NotNull(provider(patternClass, flow)));
			Assert.All([_PS_012, _PS_013, _PS_014], patternClass => Assert.Null(provider(patternClass, flow)));
		}
	}

	[Fact]
	public void EveryTemplateStatesItsEditionsAsksTheDestinationQuestionAndNamesTheSecretManagementPointer()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		RecommendationTemplateProvider provider = analyzer.CandidateSelectionAndRecommendation.TemplateProvider;

		foreach (string patternClass in _templatedClasses)
		{
			RecommendationTemplate template = provider(patternClass, CredentialFlow.ClientCredentials)!;
			RecommendationTemplate codeTemplate = provider(patternClass, CredentialFlow.AuthorizationCode)!;

			Assert.StartsWith("**Editions:**", template.GenericSourceForm, StringComparison.Ordinal);
			Assert.Contains("```powershell", template.GenericSourceForm, StringComparison.Ordinal);
			Assert.Equal(template.GenericSourceForm, codeTemplate.GenericSourceForm);
			Assert.Equal(template.TargetFormUnderTokenAuthentication, codeTemplate.TargetFormUnderTokenAuthentication);
			Assert.Equal(template.VerificationNote, codeTemplate.VerificationNote);
			Assert.NotEqual(template.CredentialProvenance, codeTemplate.CredentialProvenance);
			Assert.Contains("Secret Management (Part B of the Migration Documentation)", template.CredentialProvenance, StringComparison.Ordinal);
			Assert.Empty(InternalTermsIn(template.CredentialProvenance + codeTemplate.CredentialProvenance + template.TargetFormUnderTokenAuthentication + template.VerificationNote));
		}

		string[] destinationFirst = [_PS_001, _PS_002, _PS_003, _PS_004, _PS_005, _PS_006, _PS_007, _PS_008, _PS_009, _PS_010];

		Assert.All(
			destinationFirst,
			patternClass => Assert.Contains("**First, the destination:**", provider(patternClass, CredentialFlow.ClientCredentials)!.TargetFormUnderTokenAuthentication, StringComparison.Ordinal));
		Assert.DoesNotContain("**First, the destination:**", provider(_PS_011, CredentialFlow.ClientCredentials)!.TargetFormUnderTokenAuthentication, StringComparison.Ordinal);
	}

	[Fact]
	public void TheTwoTokenAcquisitionPartsCarryTheCatalogsElementsAndTheDesktopListenerFact()
	{
		RecommendationTemplateProvider provider = ReadAnalyzer().CandidateSelectionAndRecommendation.TemplateProvider;
		string clientCredentials = provider(_PS_004, CredentialFlow.ClientCredentials)!.CredentialProvenance;
		string authorizationCode = provider(_PS_004, CredentialFlow.AuthorizationCode)!.CredentialProvenance;

		Assert.Contains("grant_type=client_credentials", clientCredentials, StringComparison.Ordinal);
		Assert.Contains("X-CSRF-Header", clientCredentials, StringComparison.Ordinal);
		Assert.Contains("do not refresh", clientCredentials, StringComparison.Ordinal);
		Assert.All(
			new[] { "Sign in.", "Receive the code.", "Exchange the code.", "Use the token.", "On expiry.", "PKCE.", "The script stops collecting the password.", "Single sign-on." },
			element => Assert.Contains(element, authorizationCode, StringComparison.Ordinal));
		Assert.Contains("HttpListener", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("ends in `/`", authorizationCode, StringComparison.Ordinal);
	}

	[Fact]
	public void TheFallbackTextOfBothFlowsNamesPowerShellAndTheTwoWaysAScriptReachesRelativityOne()
	{
		FrozenDictionary<CredentialFlow, string> fallback = ReadAnalyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!;

		Assert.Contains("in PowerShell", fallback[CredentialFlow.ClientCredentials], StringComparison.Ordinal);
		Assert.Contains("bearer `Authorization` header", fallback[CredentialFlow.ClientCredentials], StringComparison.Ordinal);
		Assert.Contains("Authorization Code flow", fallback[CredentialFlow.AuthorizationCode], StringComparison.Ordinal);
		Assert.Contains("the script sends the user to RelativityOne", fallback[CredentialFlow.AuthorizationCode], StringComparison.Ordinal);
	}

	// ------------------------------------------------------------------------------------------
	// The shared vocabulary
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("-Cr", true)]
	[InlineData("-Cre", true)]
	[InlineData("-Cred", true)]
	[InlineData("-Crede", true)]
	[InlineData("-Creden", true)]
	[InlineData("-Credent", true)]
	[InlineData("-Credenti", true)]
	[InlineData("-Credentia", true)]
	[InlineData("-Credential", true)]
	[InlineData("-CREDENTIAL", true)]
	[InlineData("–Cred", true)]
	[InlineData("—Cred", true)]
	[InlineData("―Cred", true)]
	[InlineData("-C", false)]
	[InlineData("-Ce", false)]
	[InlineData("-CredentialX", false)]
	[InlineData("-Credz", false)]
	[InlineData("-UseDefaultCredentials", false)]
	public void EveryPrefixOfTheCredentialParameterFromCrUpIsReadUnderEveryDashAndNoOtherWordIs(string parameter, bool read)
	{
		string source = $"Invoke-RestMethod -Uri $u {parameter} $c";

		Assert.Equal(read, Recognized(source, _PS_012));
	}

	[Theory]
	[InlineData("-Au", true)]
	[InlineData("-Aut", true)]
	[InlineData("-Auth", true)]
	[InlineData("-Authentication", true)]
	[InlineData("-A", false)]
	[InlineData("-AuthenticationX", false)]
	[InlineData("-AllowUnencryptedAuthentication", false)]
	public void EveryPrefixOfTheAuthenticationParameterFromAuUpIsReadAndAllowUnencryptedIsNot(string parameter, bool read)
	{
		string source = $"Invoke-RestMethod -Uri $u {parameter} Basic";

		Assert.Equal(read, Recognized(source, _PS_003));
	}

	[Theory]
	[InlineData("Get-Credential -Message 'x'")]
	[InlineData("New-Credential -Name x")]
	[InlineData("Get-AuthenticationToken -Name x")]
	public void ACommandNameThatContainsTheParameterWordIsNotAParameter(string commandLine)
	{
		string source = $"Invoke-RestMethod -Uri $u; {commandLine}";

		Assert.False(Recognized(source, _PS_012));
		Assert.False(Recognized(source, _PS_003));
	}

	[Fact]
	public void AllCasesOfTheCmdletNamesAndTheAliasesAreRead()
	{
		foreach (string name in new[] { "Invoke-RestMethod", "invoke-restmethod", "IRM", "iwr", "Invoke-WebRequest", "curl", "wget" })
		{
			Assert.True(Recognized($"{name} $u -Credential $c", _PS_012), name);
		}
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static ILanguageAnalyzer ReadAnalyzer()
	{
		return LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_LANGUAGE_NAME];
	}

	/// <summary>Every internal term a template text carries: this test's own terms and the public-source guard's internal identifiers.</summary>
	private static ImmutableArray<string> InternalTermsIn(string text)
	{
		return [.. PublicSourceIdentifierTests.InternalIdentifierPatterns().AddRange(PublicSourceIdentifierTests.InternalTemplateTermPatterns()).SelectMany(pattern => pattern.Matches(text)).Select(match => match.Value)];
	}

	/// <summary>Every internal term an unsupported-coverage text carries: this test's own plain terms and the public-source guard's internal identifiers.</summary>
	private static ImmutableArray<string> UnsupportedCoverageInternalTermsIn(string text)
	{
		return
		[
			.. PublicSourceIdentifierTests.InternalCoverageTerms().Where(term => text.Contains(term, StringComparison.Ordinal)),
			.. PublicSourceIdentifierTests.InternalIdentifierPatterns().SelectMany(pattern => pattern.Matches(text)).Select(match => match.Value)
		];
	}

	private static bool Recognized(string source, string patternClass)
	{
		return new PowerShellRecognitionHarness().Recognize(source).Any(construct => construct.PatternClassIdentifier == patternClass);
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

	private static void AssertSilent(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{Describe(constructs)}]");
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));
	}

	/// <summary>One invented fixture per class, with the level the catalog gives it. Shared with the agreement tests of the generated and interpreted forms.</summary>
	internal static ImmutableArray<(string PatternClass, ConfidenceLevel Level, string Source)> ClassFixtures()
	{
		return
		[
			(_PS_001, ConfidenceLevel.High, "$credentials = New-Object Vendor.Sdk.UsernamePasswordCredentials($loginName, $loginSecret)\n$settings = New-Object Vendor.Sdk.ServiceFactorySettings($restEndpointUri, $credentials)"),
			(_PS_002, ConfidenceLevel.High, "$proxy = New-Object Vendor.Sdk.RSAPIClient ((New-Object System.Uri -ArgumentList $serviceAddress), (New-Object Vendor.Sdk.UsernamePasswordCredentials($loginName, $loginSecret)))"),
			(_PS_003, ConfidenceLevel.High, "Invoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic"),
			(_PS_004, ConfidenceLevel.Medium, "Invoke-RestMethod -Uri $uri -Headers @{ Authorization = \"Basic $encoded\" }"),
			(_PS_005, ConfidenceLevel.Medium, "$headerTable.Add('Authorization', 'Basic ' + $encoded)"),
			(_PS_006, ConfidenceLevel.Medium, "$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, \"Basic $encoded\")"),
			(_PS_007, ConfidenceLevel.Medium, "$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Basic', $encoded)"),
			(_PS_008, ConfidenceLevel.High, "$cache = New-Object System.Net.CredentialCache\n$cache.Add([Uri]$uri, 'Basic', (New-Object System.Net.NetworkCredential($loginName, $loginSecret)))"),
			(_PS_009, ConfidenceLevel.Medium, "$handler = New-Object System.Net.Http.HttpClientHandler\n$handler.Credentials = New-Object System.Net.NetworkCredential($loginName, $loginSecret)"),
			(_PS_010, ConfidenceLevel.Medium, "$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(\"$($loginName):$($loginSecret)\"))"),
			(_PS_011, ConfidenceLevel.Medium, "$pair = \":\" + $accessToken\n$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))"),
			(_PS_012, ConfidenceLevel.Low, "Invoke-RestMethod -Uri $uri -Credential $credential"),
			(_PS_013, ConfidenceLevel.Low, "Invoke-Command -ComputerName $server -Credential $credential -ScriptBlock { hostname }"),
			(_PS_014, ConfidenceLevel.Low, "function New-BasicAuthorizationHeader {\n\tparam($loginName, $loginSecret)\n}\n$credential = $null")
		];
	}
}
