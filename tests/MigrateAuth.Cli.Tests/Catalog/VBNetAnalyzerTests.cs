using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Release;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The `VBNet` analyzer against its catalog, through the real embedded analyzer and the real recognition engine: never through a hand-built construct,
/// because what is under test is whether the expressions the analyzer declares recognize the forms the catalog states and decline the exclusions it states.
///
/// Every source below is INVENTED from the catalog's own prose, with placeholder names throughout. One fixture per covered cell of the type-by-form table and one per near-miss.
/// </summary>
public sealed class VBNetAnalyzerTests
{
	private const string _LANGUAGE_NAME = "VBNet";

	private const string _VB_001 = "VB-001";
	private const string _VB_002 = "VB-002";
	private const string _VB_003 = "VB-003";
	private const string _VB_004 = "VB-004";
	private const string _VB_005 = "VB-005";
	private const string _VB_006 = "VB-006";
	private const string _VB_007 = "VB-007";
	private const string _VB_008 = "VB-008";
	private const string _VB_009 = "VB-009";
	private const string _VB_010 = "VB-010";
	private const string _VB_011 = "VB-011";
	private const string _VB_012 = "VB-012";
	private const string _VB_013 = "VB-013";
	private const string _VB_014 = "VB-014";
	private const string _VB_015 = "VB-015";
	private const string _VB_016 = "VB-016";
	private const string _VB_017 = "VB-017";
	private const string _VB_018 = "VB-018";
	private const string _VB_019 = "VB-019";

	private static readonly ImmutableArray<string> _everyClass =
	[
		_VB_001, _VB_002, _VB_003, _VB_004, _VB_005, _VB_006, _VB_007, _VB_008, _VB_009, _VB_010,
		_VB_011, _VB_012, _VB_013, _VB_014, _VB_015, _VB_016, _VB_017, _VB_018, _VB_019
	];

	// ------------------------------------------------------------------------------------------
	// The contract
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheVBNetAnalyzerFitsTheContractWithNoViolations()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(ReadAnalyzer()));
	}

	[Fact]
	public void TheAnalyzerIsRegisteredUnderTheVbExtensionAndCaseDoesNotMatter()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		foreach (string extension in new[] { ".vb", ".VB", ".Vb" })
		{
			Assert.True(catalog.TryGetAnalyzerForFileExtension(extension, out ILanguageAnalyzer? analyzer), extension);
			Assert.Same(catalog.AnalyzersByLanguageName[_LANGUAGE_NAME], analyzer);
		}

		foreach (string extension in new[] { ".vbs", ".bas", ".cls", ".frm", ".vbhtml", ".aspx" })
		{
			Assert.False(catalog.TryGetAnalyzerForFileExtension(extension, out _), extension);
		}
	}

	[Fact]
	public void NineteenClassesAreDeclaredSeventeenCanReachHighOrMediumAndTwoAreLow()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.Equal(_everyClass.Order(StringComparer.Ordinal), analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal));
		Assert.Equal(
			[_VB_018, _VB_019],
			analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.BaseLevel is ConfidenceLevel.Low).Select(entry => entry.Key).Order(StringComparer.Ordinal));
		Assert.Equal(
			[_VB_001, _VB_002, _VB_003, _VB_004, _VB_005, _VB_006, _VB_007, _VB_008, _VB_009, _VB_010, _VB_011],
			analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.BaseLevel is ConfidenceLevel.High).Select(entry => entry.Key).Order(StringComparer.Ordinal));
		Assert.Equal(
			[_VB_012, _VB_013, _VB_014, _VB_015, _VB_016, _VB_017],
			analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.BaseLevel is ConfidenceLevel.Medium).Select(entry => entry.Key).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void TheSelfSufficientClassesAndTheEstablishedDestinationsAreTheOnesTheCatalogDeclares()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.Equal(
			[_VB_001, _VB_002, _VB_003, _VB_004, _VB_005, _VB_010, _VB_011],
			analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.SelfSufficiency is SelfSufficiency.DeclaresSchemeAndCredentialCarrier).Select(entry => entry.Key).Order(StringComparer.Ordinal));
		Assert.Equal(
			[_VB_001, _VB_002, _VB_010, _VB_011],
			analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.DestinationCertainty is DestinationCertainty.Established).Select(entry => entry.Key).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void EveryExpressionCarriesTheCaseInsensitiveOptionsTheExplicitCaptureAndThePolicyTimeout()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		const System.Text.RegularExpressions.RegexOptions expected =
			System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.ExplicitCapture;

		Assert.All(VBNetRegisteredExpressions.All, pair =>
		{
			Assert.Equal(expected, pair.Expression.Options & expected);
			Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, pair.Expression.MatchTimeout);
		});
		Assert.Equal(43, VBNetRegisteredExpressions.All.Count);
		Assert.Equal(20, analyzer.CandidateSelectionAndRecommendation.FlowSignals.Length);
	}

	[Fact]
	public void TheAnchorsAreDeclaredCaseInsensitiveAndEveryClassFixtureBringsOne()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		FrozenSet<PreselectionAnchor> anchors = analyzer.CandidateSelectionAndRecommendation.PreselectionAnchors;

		Assert.Equal(
			["Authenticator", "Authorization", "CreateObject", "Credential", "DownloadFile", "ImportAPI", "RelativityPassword", "RelativityUsername", "ToBase64String", "UploadFile", "WithBasicAuth"],
			anchors.Select(anchor => anchor.Text).Order(StringComparer.Ordinal));
		Assert.All(anchors, anchor => Assert.Equal(CaseSensitivity.Insensitive, anchor.Sensitivity));
		Assert.All(
			ClassFixtures(),
			fixture => Assert.True(CandidatePreselection.IsCandidate(analyzer, fixture.Source), fixture.PatternClass));
		Assert.True(CandidatePreselection.IsCandidate(analyzer, "dim c = new networkcredential(a, b)"));
		Assert.False(CandidatePreselection.IsCandidate(analyzer, "Dim isBasic As Boolean = True"));
		Assert.False(CandidatePreselection.IsCandidate(analyzer, "Dim userName As String = Environment.UserName"));
	}

	[Fact]
	public void SixSubsumptionTriplesAreDeclaredAsTheCatalogStatesThem()
	{
		FrozenSet<SubsumptionRule> relation = ReadAnalyzer().SubsumptionRelation;

		Assert.Equal(6, relation.Count);
		Assert.Contains(relation, rule => rule is { SubsumingPatternClassIdentifier: _VB_002, SubsumedPatternClassIdentifier: _VB_001, When: SubsumptionCondition.AnchorPositionsAreIdentical });
		Assert.Contains(relation, rule => rule is { SubsumingPatternClassIdentifier: _VB_003, SubsumedPatternClassIdentifier: _VB_013, When: SubsumptionCondition.MatchSpansOverlap });
		Assert.Contains(relation, rule => rule is { SubsumingPatternClassIdentifier: _VB_012, SubsumedPatternClassIdentifier: _VB_013, When: SubsumptionCondition.MatchSpansOverlap });
		Assert.Contains(relation, rule => rule is { SubsumingPatternClassIdentifier: _VB_016, SubsumedPatternClassIdentifier: _VB_013, When: SubsumptionCondition.MatchSpansOverlap });
		Assert.Contains(relation, rule => rule is { SubsumingPatternClassIdentifier: _VB_018, SubsumedPatternClassIdentifier: _VB_013, When: SubsumptionCondition.MatchSpansOverlap });
		Assert.Contains(relation, rule => rule is { SubsumingPatternClassIdentifier: _VB_015, SubsumedPatternClassIdentifier: _VB_014, When: SubsumptionCondition.AnchorPositionsAreIdentical });
	}

	[Fact]
	public void TheSchemeInterpretationIsDeclaredForFourClassesAndAnIndeterminateSchemeProposesMedium()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		string[] withScheme =
		[
			.. analyzer.DetectionRulesByPatternClass
				.Where(entry => entry.Value.SymbolicVerdictInterpretation.IsSubjectToSymbolicResolution)
				.Select(entry => entry.Key)
				.Order(StringComparer.Ordinal)
		];

		Assert.Equal([_VB_003, _VB_006, _VB_007, _VB_008], withScheme);
		Assert.All(withScheme, patternClass =>
		{
			SymbolicVerdictInterpretation interpretation = analyzer.DetectionRulesByPatternClass[patternClass].SymbolicVerdictInterpretation;

			Assert.Equal("scheme", interpretation.SchemeArgumentCaptureGroupName);
			Assert.Equal("Basic", interpretation.ExpectedSchemeToken);
			Assert.Equal(SymbolicVerdictResponse.ConfirmsPatternClass, interpretation.WhenResolvedMatching);
			Assert.Equal(SymbolicVerdictResponse.SuppressesFinding, interpretation.WhenResolvedNotMatching);
			Assert.Equal(SymbolicVerdictResponse.ProposesMediumLevel, interpretation.WhenIndeterminate);
		});
	}

	[Fact]
	public void TheCredentialPositionIsDeclaredForEveryClassButTheHelperDefinition()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.Equal(
			[_VB_019],
			analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.CredentialPosition.CaptureGroupName is null).Select(entry => entry.Key));
		Assert.All(
			analyzer.DetectionRulesByPatternClass.Where(entry => entry.Key != _VB_019),
			entry => Assert.Equal("credential", entry.Value.CredentialPosition.CaptureGroupName));
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

	// ------------------------------------------------------------------------------------------
	// Demotion: the file, folder and content signals, and the Deployment decision
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheDemotionSignalsAreAllInsensitiveAndTheDeploymentFolderIsNotOne()
	{
		LanguageShapedDemotionSignals signals = ReadAnalyzer().DemotionSignals;

		Assert.All(signals.FileNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
		Assert.All(signals.FolderNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
		Assert.DoesNotContain(signals.FolderNameSignals, signal => signal.FolderName.Equals("Deployment", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(signals.FolderNameSignals, signal => signal.FolderName == "Service References");
		Assert.Contains(signals.FolderNameSignals, signal => signal.FolderName == "Web References");
		Assert.Equal(3, signals.ContentMarkers.Length);
		Assert.Contains(signals.ContentMarkers, marker => marker.Zone is ContentMarkerZone.AnyZone);
		Assert.Equal(2, signals.ContentMarkers.Count(marker => marker.Zone is ContentMarkerZone.CodeOnly));
	}

	private const string _HIGH_SOURCE = "Dim c As New UsernamePasswordCredentials(userName, password)";

	[Theory]
	[InlineData("source/Module.vb", ConfidenceLevel.High)]
	[InlineData("Deployment/Module.vb", ConfidenceLevel.High)]
	[InlineData("deployment/scripts/Module.vb", ConfidenceLevel.High)]
	[InlineData("source/Module.Tests.vb", ConfidenceLevel.Low)]
	[InlineData("source/ModuleTests.vb", ConfidenceLevel.Low)]
	[InlineData("source/Module.Test.vb", ConfidenceLevel.Low)]
	[InlineData("source/Module.Spec.vb", ConfidenceLevel.Low)]
	[InlineData("source/FakeClient.vb", ConfidenceLevel.Low)]
	[InlineData("source/SampleClient.vb", ConfidenceLevel.Low)]
	[InlineData("tests/Module.vb", ConfidenceLevel.Low)]
	[InlineData("Module.Tests/Module.vb", ConfidenceLevel.Low)]
	[InlineData("vendor/Module.vb", ConfidenceLevel.Low)]
	[InlineData("third-party/Module.vb", ConfidenceLevel.Low)]
	[InlineData("examples/Module.vb", ConfidenceLevel.Low)]
	[InlineData("Service References/Reference.vb", ConfidenceLevel.Low)]
	[InlineData("Web References/Reference.vb", ConfidenceLevel.Low)]
	[InlineData("source/Resources.Designer.vb", ConfidenceLevel.Low)]
	[InlineData("My Project/Settings.Designer.vb", ConfidenceLevel.Low)]
	[InlineData("source/Module.g.vb", ConfidenceLevel.Low)]
	[InlineData("source/Module.generated.vb", ConfidenceLevel.Low)]
	[InlineData("My Project/Settings.vb", ConfidenceLevel.High)]
	[InlineData("source/Latest.vb", ConfidenceLevel.High)]
	[InlineData("source/Reference.vb", ConfidenceLevel.High)]
	public void AHighConstructIsDemotedByTheFileAndFolderSignalsAndNotByTheDeploymentFolderOrAHandWrittenSettingsFile(string relativePath, ConfidenceLevel expected)
	{
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(_HIGH_SOURCE, relativePath);

		Assert.Equal(expected, constructs.Single(construct => construct.PatternClassIdentifier == _VB_001).Level);
	}

	[Theory]
	[InlineData("' <auto-generated>\n' This code was generated by a tool.\n' </auto-generated>\nOption Strict On\nModule M\n\tSub S()\n\t\tDim c As New UsernamePasswordCredentials(userName, password)\n\tEnd Sub\nEnd Module", ConfidenceLevel.Low)]
	[InlineData("Option Strict On\nModule M\n\t<System.CodeDom.Compiler.GeneratedCodeAttribute(\"tool\", \"1\")>\n\tSub S()\n\t\tDim c As New UsernamePasswordCredentials(userName, password)\n\tEnd Sub\nEnd Module", ConfidenceLevel.Low)]
	[InlineData("Module M\n\t<TestMethod>\n\tSub S()\n\t\tDim c As New UsernamePasswordCredentials(userName, password)\n\tEnd Sub\nEnd Module", ConfidenceLevel.Low)]
	[InlineData("Module M\n\t<Fact>\n\tSub S()\n\t\tDim c As New UsernamePasswordCredentials(userName, password)\n\tEnd Sub\nEnd Module", ConfidenceLevel.Low)]
	[InlineData("Module M\n\t' a comment mentioning <TestMethod> only\n\tSub S()\n\t\tDim c As New UsernamePasswordCredentials(userName, password)\n\tEnd Sub\nEnd Module", ConfidenceLevel.High)]
	[InlineData("Module M\n\tSub S()\n\t\tDim c As New UsernamePasswordCredentials(userName, password)\n\t\t' <auto-generated> in a comment after the code\n\tEnd Sub\nEnd Module", ConfidenceLevel.High)]
	public void AHighConstructIsDemotedByTheContentMarkersWhereTheyStandInCode(string source, ConfidenceLevel expected)
	{
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		Assert.Equal(expected, constructs.Single(construct => construct.PatternClassIdentifier == _VB_001).Level);
	}

	// ------------------------------------------------------------------------------------------
	// One fixture per class: the level the catalog gives it
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EveryClassFixtureFiresItsClassAtTheLevelTheCatalogGivesIt()
	{
		Assert.All(ClassFixtures(), fixture => AssertFires(fixture.Source, fixture.PatternClass, fixture.Level));
	}

	// ------------------------------------------------------------------------------------------
	// The covered cells of the type-by-form table, and the declared ones
	// ------------------------------------------------------------------------------------------

	public static TheoryData<string, ConfidenceLevel, string> CoveredForms()
	{
		TheoryData<string, ConfidenceLevel, string> data = [];

		foreach ((string patternClass, ConfidenceLevel level, string source) in _coveredForms)
		{
			data.Add(patternClass, level, source);
		}

		return data;
	}

	[Fact]
	public void TheEmptyHalfAssemblyIsReportedOnceAsTheEmptyHalfClassAndTheBothHalvesPairIsNot()
	{
		string source = "Dim pair = String.Format(\"{0}:{1}\", String.Empty, sessionBlob)\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))";
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _VB_015);
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _VB_014);
	}

	[Theory]
	[MemberData(nameof(CoveredForms))]
	public void ACoveredFormIsRecognizedAtTheLevelOfItsClass(string patternClass, ConfidenceLevel level, string source)
	{
		AssertFires(source, patternClass, level);
	}

	public static TheoryData<string, string> DeclinedForms()
	{
		TheoryData<string, string> data = [];

		foreach ((string patternClass, string source) in _declinedForms)
		{
			data.Add(patternClass, source);
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(DeclinedForms))]
	public void ADeclaredFormOrANearMissIsNotRecognizedByTheClassItResembles(string patternClass, string source)
	{
		AssertNeverFires(source, patternClass);
	}

	[Theory]
	[InlineData("Dim handler As New HttpClientHandler()\nhandler.UseDefaultCredentials = True")]
	[InlineData("Dim handler As New HttpClientHandler()\nhandler.Credentials = CredentialCache.DefaultCredentials")]
	[InlineData("Dim handler As New HttpClientHandler()\nhandler.Credentials = CredentialCache.DefaultNetworkCredentials")]
	[InlineData("Dim client As New WebClient()\nclient.Headers.Add(\"Content-Type\", \"application/json\")")]
	[InlineData("Dim value = Request.Headers(\"Authorization\")")]
	[InlineData("client.DefaultRequestHeaders.Add(\"Proxy-Authorization\", \"Basic \" & encoded)")]
	[InlineData("Dim isBasic As Boolean = True ' Visual Basic")]
	[InlineData("Dim hash = Convert.ToBase64String(sha.ComputeHash(bytes))")]
	[InlineData("Dim picture = Convert.ToBase64String(File.ReadAllBytes(path))")]
	public void ANearMissProducesNoHighOrMediumFinding(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.Level is ConfidenceLevel.High or ConfidenceLevel.Medium);
	}

	/// <summary>The two header-write classes cannot overlap: a key written as the enumeration member is the second class's alone, and a key literal or a named constant is the first's alone.</summary>
	[Theory]
	[InlineData("request.Headers.Add(HttpRequestHeader.Authorization, \"Basic \" & encoded)", "VB-008", "VB-007")]
	[InlineData("client.Headers(System.Net.HttpRequestHeader.Authorization) = \"Basic \" & encoded", "VB-008", "VB-007")]
	[InlineData("request.Headers.Add(\"Authorization\", \"Basic \" & encoded)", "VB-007", "VB-008")]
	[InlineData("Private Const AUTHORIZATION_HEADER As String = \"Authorization\"\nrequest.AddHeader(AUTHORIZATION_HEADER, \"Basic \" & encoded)", "VB-007", "VB-008")]
	public void TheTwoHeaderWriteClassesNeverReportTheSameWrite(string source, string expectedClass, string excludedClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == expectedClass);
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == excludedClass);
	}

	[Fact]
	public void ABearerHeaderBesideABasicOneIsSuppressedAndTheBasicOneIsJudgedOnItsOwn()
	{
		string source = "client.DefaultRequestHeaders.Add(\"Authorization\", \"Bearer \" & accessToken)\nclient.DefaultRequestHeaders.Add(\"Authorization\", \"Basic \" & encoded)";

		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);
		RecognizedConstruct[] headerWrites = [.. constructs.Where(construct => construct.PatternClassIdentifier == _VB_007)];

		Assert.Single(headerWrites);
		Assert.Equal(ConfidenceLevel.Medium, headerWrites[0].Level);
		Assert.True(headerWrites[0].AnchorOffset > source.IndexOf('\n', StringComparison.Ordinal));
	}

	[Fact]
	public void ASchemeConstantDeclaredOnceResolvesAndOneDeclaredInTwoBranchesIsIndeterminateAndKeptAtMedium()
	{
		string resolvedBasic = "Private Const SCHEME As String = \"Basic\"\nSub S()\n\tcache.Add(New Uri(root), SCHEME, New NetworkCredential(userName, password))\nEnd Sub";
		string resolvedBearer = "Private Const SCHEME As String = \"Bearer\"\nSub S()\n\tcache.Add(New Uri(root), SCHEME, New NetworkCredential(userName, password))\nEnd Sub";
		string indeterminate = "Sub S(kind As Integer)\n\tDim scheme As String\n\tSelect Case kind\n\t\tCase 1\n\t\t\tscheme = \"Basic\"\n\t\tCase Else\n\t\t\tscheme = \"Bearer\"\n\tEnd Select\n\tclient.Headers.Authorization = New AuthenticationHeaderValue(scheme, encoded)\nEnd Sub";

		AssertFires(resolvedBasic, _VB_003, ConfidenceLevel.High);
		AssertNeverFires(resolvedBearer, _VB_003);
		AssertFires(indeterminate, _VB_006, ConfidenceLevel.Medium);
	}

	[Fact]
	public void AQualifiedSchemeConstantIsNotReadAsTheSchemeArgumentOfTheCacheOrTheHeaderValueAndIsDeclared()
	{
		AssertNeverFires("cache.Add(New Uri(root), Constants.SCHEME, New NetworkCredential(userName, password))", _VB_003);
		AssertNeverFires("request.Headers.Authorization = New AuthenticationHeaderValue(Constants.SCHEME, encoded)", _VB_006);
		AssertNeverFires("Private Const SCHEME As String = \"Basic\"\nSub S()\n\tcache.Add(New Uri(root), Constants.SCHEME, New NetworkCredential(userName, password))\nEnd Sub", _VB_003);
	}

	// ------------------------------------------------------------------------------------------
	// The forms the catalog's text leaves open, pinned as the analyzer reads them
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void AnObjectInitializerOfTheSdkCredentialTypeOrALeadingDotBlockIsNotReadWhileTheNetworkCredentialInitializerTheSettingsPairAndTheHandlerInitializerAre()
	{
		string credentialInitializer = "Dim c As New NetworkCredential With {.UserName = userName, .Password = password}";
		string sdkInitializer = "Dim c As New UsernamePasswordCredentials With {.UserName = userName, .Password = password}";
		string leadingDotBlock = "With credential\n\t.UserName = userName\n\t.Password = password\nEnd With";

		AssertFires(credentialInitializer, _VB_013, ConfidenceLevel.Medium);
		AssertNeverFires(sdkInitializer, _VB_001);
		AssertNeverFires(leadingDotBlock, _VB_013);
		AssertFires("Dim s As New RelativitySettings With {.RelativityUsername = userName, .RelativityPassword = password}", _VB_011, ConfidenceLevel.High);
		AssertFires("With settings\n\t.RelativityUsername = userName\n\t.RelativityPassword = password\nEnd With", _VB_011, ConfidenceLevel.High);
		AssertFires("Dim h As New HttpClientHandler With {.Credentials = New NetworkCredential(userName, password)}", _VB_012, ConfidenceLevel.Medium);
	}

	[Fact]
	public void AnAliasOfTheHttpHandlerTypeIsNotResolvedSoItsCredentialAssignmentFallsToTheResidueClass()
	{
		string aliasedHandler = "Imports H = System.Net.Http.HttpClientHandler\nSub S()\n\tDim h As New H()\n\th.Credentials = New NetworkCredential(userName, password)\nEnd Sub";
		string aliasedDefault = "Imports H = System.Net.Http.HttpClientHandler\nSub S()\n\tDim h As New H()\n\th.Credentials = CredentialCache.DefaultCredentials\nEnd Sub";

		AssertFires(aliasedHandler, _VB_012, ConfidenceLevel.Medium);
		AssertFires("Sub S()\n\tDim h As New H()\n\th.Credentials = New NetworkCredential(userName, password)\nEnd Sub", _VB_013, ConfidenceLevel.Medium);
		AssertNeverFires("Sub S()\n\tDim h As New H()\n\th.Credentials = New NetworkCredential(userName, password)\nEnd Sub", _VB_012);
		AssertNeverFires(aliasedDefault, _VB_013);
	}

	[Fact]
	public void NamedArgumentsAreNotInterpretedByNameSoOutOfOrderNamesAreReadByTheResidueAndTheHeaderValueButNotByTheHeaderWrite()
	{
		AssertFires("Dim credential = New NetworkCredential(password:=secret, userName:=name)", _VB_013, ConfidenceLevel.Medium);
		AssertFires("Dim credential = New NetworkCredential(userName:=name, password:=secret)", _VB_013, ConfidenceLevel.Medium);
		AssertFires("request.Headers.Authorization = New AuthenticationHeaderValue(parameter:=encoded, scheme:=\"Basic\")", _VB_006, ConfidenceLevel.Medium);
		AssertFires("request.Headers.Authorization = New AuthenticationHeaderValue(scheme:=\"Basic\", parameter:=encoded)", _VB_006, ConfidenceLevel.Medium);
		AssertFires("client.DefaultRequestHeaders.Add(name:=\"Authorization\", value:=\"Basic \" & encoded)", _VB_007, ConfidenceLevel.Medium);
		AssertNeverFires("client.DefaultRequestHeaders.Add(value:=\"Basic \" & encoded, name:=\"Authorization\")", _VB_007);
	}

	[Fact]
	public void TheLateBoundOpenIsReadByItsLiteralVerbWhateverTheReceiverIsCreatedWithAndNotThroughAVerbVariable()
	{
		AssertFires("Dim http = CreateObject(\"MSXML2.XMLHTTP\")\nhttp.Open(\"GET\", address, False, userName, password)", _VB_017, ConfidenceLevel.Medium);
		AssertFires("Dim http = Server.CreateObject(\"MSXML2.ServerXMLHTTP\")\nhttp.Open(\"GET\", address, False, userName, password)", _VB_017, ConfidenceLevel.Medium);
		AssertFires("With CreateObject(\"MSXML2.XMLHTTP\")\n\t.Open(\"GET\", address, False, userName, password)\nEnd With", _VB_017, ConfidenceLevel.Medium);
		AssertNeverFires("Dim verb As String = \"GET\"\nDim http = CreateObject(\"MSXML2.XMLHTTP\")\nhttp.Open(verb, address, False, userName, password)", _VB_017);
		AssertFires("Dim http = Activator.CreateInstance(Type.GetTypeFromProgID(\"MSXML2.XMLHTTP\"))\nhttp.Open(\"GET\", address, False, userName, password)", _VB_017, ConfidenceLevel.Medium);
		AssertNeverFires("Dim http = Activator.CreateInstance(Type.GetTypeFromProgID(\"MSXML2.XMLHTTP\"))\nhttp.Open(verb, address, False, userName, password)", _VB_017);
		AssertFires("Dim http = CreateObject(\"MSXML2.XMLHTTP\")\nhttp.Open(method:=\"GET\", url:=address, async:=False, user:=userName, password:=password)", _VB_017, ConfidenceLevel.Medium);
		AssertNeverFires("Dim http = CreateObject(\"MSXML2.XMLHTTP\")\nhttp.Open(\"GET\", address, False)", _VB_017);
		AssertNeverFires("Dim http = Server.CreateObject(\"MSXML2.ServerXMLHTTP\")\nhttp.Open(\"GET\", address, False, userName)", _VB_017);
	}

	[Fact]
	public void AColonFirstPairIsTheMediumClassOfTheTokenAndAPairAccumulatedAcrossStatementsIsReadByNeitherPairClass()
	{
		string wholePairInOneAppend = "Dim pair As String = \"\"\npair &= userName & \":\" & password\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))";
		string colonFirstAppend = "Dim pair As String = \"\"\npair &= \":\" & password\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))";
		string acrossStatements = "Dim pair As String = \"\"\npair &= userName\npair &= \":\"\npair &= password\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))";
		string reassigned = "Dim pair As String = userName\npair = pair & \":\"\npair = pair & password\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))";

		AssertFires(wholePairInOneAppend, _VB_014, ConfidenceLevel.Medium);
		AssertFires(colonFirstAppend, _VB_015, ConfidenceLevel.Medium);
		AssertNeverFires(acrossStatements, _VB_014);
		AssertNeverFires(acrossStatements, _VB_015);
		AssertNeverFires(reassigned, _VB_014);
		AssertNeverFires(reassigned, _VB_015);
	}

	// ------------------------------------------------------------------------------------------
	// The literal credential signal
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("Dim c As New UsernamePasswordCredentials(\"alice\", \"hunter2\")", true)]
	[InlineData("Dim c As New UsernamePasswordCredentials(userName, \"hunter2\")", true)]
	[InlineData("Dim c As New UsernamePasswordCredentials(userName, password)", false)]
	[InlineData("Dim c As New UsernamePasswordCredentials(userName, GetSecret())", false)]
	[InlineData("Dim c As New UsernamePasswordCredentials(userName, settings(\"key\"))", false)]
	[InlineData("Dim c As New NetworkCredential(userName, \"hunter2\")", true)]
	[InlineData("Dim c As New NetworkCredential(userName, secret)", false)]
	[InlineData("client.DefaultRequestHeaders.Add(\"Authorization\", \"Basic dXNlcjpwYXNz\")", true)]
	[InlineData("client.DefaultRequestHeaders.Add(\"Authorization\", \"Basic \" & encoded)", false)]
	public void TheLiteralCredentialSignalComesFromTheZoneOfTheCapturedCredentialAndNeverFromTheValue(string source, bool expected)
	{
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);
		RecognizedConstruct[] reported = [.. constructs.Where(construct => construct.Level is ConfidenceLevel.High or ConfidenceLevel.Medium)];

		Assert.NotEmpty(reported);
		Assert.Contains(reported, construct => construct.LiteralCredentialSignal == expected);
		Assert.DoesNotContain(reported, construct => construct.LiteralCredentialSignal != expected);
	}

	[Fact]
	public void ADemotedFindingKeepsItsLiteralCredentialSignal()
	{
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness()
			.Recognize("Dim c As New UsernamePasswordCredentials(\"alice\", \"hunter2\")", "tests/Module.vb");

		RecognizedConstruct construct = constructs.Single(candidate => candidate.PatternClassIdentifier == _VB_001);

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.True(construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// Subsumption at work
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheLegacyClientSubsumesTheCredentialTypeItNestsAndTheEmptyHalfSubsumesThePair()
	{
		ImmutableArray<RecognizedConstruct> legacy = new VBNetRecognitionHarness()
			.Recognize("Dim c As New RSAPIClient(uri, New UsernamePasswordCredentials(userName, password))");
		ImmutableArray<RecognizedConstruct> empty = new VBNetRecognitionHarness()
			.Recognize("Dim pair = \"\" & \":\" & token\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))");

		Assert.Contains(legacy, construct => construct.PatternClassIdentifier == _VB_002);
		Assert.DoesNotContain(legacy, construct => construct.PatternClassIdentifier == _VB_001);
		Assert.Contains(empty, construct => construct.PatternClassIdentifier == _VB_015);
		Assert.DoesNotContain(empty, construct => construct.PatternClassIdentifier == _VB_014);
	}

	[Fact]
	public void TheInitializerCredentialAndTheCastAndWithNewReceiversAreTheSinksAndTheResidueIsNotReportedBesideThem()
	{
		string[] httpSinks =
		[
			"Dim h As New SocketsHttpHandler With {.Credentials = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret}}",
			"With New WebClient()\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With",
			"With CType(widgetRequest, HttpWebRequest)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With",
			"CType(widgetRequest, HttpWebRequest).Credentials = New NetworkCredential(widgetUser, widgetSecret)",
			"Dim proxy As SoapHttpClientProtocol = New WidgetProxy()\nproxy.Credentials = New NetworkCredential(widgetUser, widgetSecret)"
		];

		foreach (string source in httpSinks)
		{
			ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

			Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _VB_012 && construct.Level is ConfidenceLevel.Medium);
			Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _VB_013);
		}

		ImmutableArray<RecognizedConstruct> mail = new VBNetRecognitionHarness()
			.Recognize("With New SmtpClient(widgetHost, 25)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With");

		Assert.Contains(mail, construct => construct.PatternClassIdentifier == _VB_018 && construct.Level is ConfidenceLevel.Low);
		Assert.DoesNotContain(mail, construct => construct.PatternClassIdentifier == _VB_013);
		Assert.DoesNotContain(mail, construct => construct.PatternClassIdentifier == _VB_012);
	}

	[Fact]
	public void ASinkThatConsumesTheCredentialKeepsItAndTheResidueIsNotReportedBesideIt()
	{
		ImmutableArray<RecognizedConstruct> http = new VBNetRecognitionHarness()
			.Recognize("Dim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(userName, password)}");
		ImmutableArray<RecognizedConstruct> mail = new VBNetRecognitionHarness()
			.Recognize("Dim smtp As New SmtpClient With {.Credentials = New NetworkCredential(userName, password)}");
		ImmutableArray<RecognizedConstruct> cache = new VBNetRecognitionHarness()
			.Recognize("cache.Add(New Uri(root), \"Basic\", New NetworkCredential(userName, password))");

		Assert.DoesNotContain(http, construct => construct.PatternClassIdentifier == _VB_013);
		Assert.Contains(http, construct => construct.PatternClassIdentifier == _VB_012);
		Assert.DoesNotContain(mail, construct => construct.PatternClassIdentifier == _VB_013);
		Assert.Contains(mail, construct => construct.PatternClassIdentifier == _VB_018 && construct.Level is ConfidenceLevel.Low);
		Assert.DoesNotContain(cache, construct => construct.PatternClassIdentifier == _VB_013);
	}

	// ------------------------------------------------------------------------------------------
	// The fixtures
	// ------------------------------------------------------------------------------------------

	/// <summary>One invented fixture per class, with the level the catalog gives it.</summary>
	internal static ImmutableArray<(string PatternClass, ConfidenceLevel Level, string Source)> ClassFixtures()
	{
		return
		[
			(_VB_001, ConfidenceLevel.High, "Dim c As New UsernamePasswordCredentials(userName, password)\nDim s As New ServiceFactorySettings(restUri, c)"),
			(_VB_002, ConfidenceLevel.High, "Dim proxy As New RSAPIClient(serviceUri, New UsernamePasswordCredentials(userName, password))"),
			(_VB_003, ConfidenceLevel.High, "cache.Add(New Uri(instanceUrl), \"Basic\", New NetworkCredential(userName, password))"),
			(_VB_004, ConfidenceLevel.High, "Dim authenticator As New HttpBasicAuthenticator(userName, password)"),
			(_VB_005, ConfidenceLevel.High, "Dim configured = request.WithBasicAuth(userName, password)"),
			(_VB_006, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue(\"Basic\", encodedCredentials)"),
			(_VB_007, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Add(\"Authorization\", \"Basic \" & encoded)"),
			(_VB_008, ConfidenceLevel.Medium, "request.Headers(HttpRequestHeader.Authorization) = \"Basic \" & encoded"),
			(_VB_009, ConfidenceLevel.Medium, "binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic"),
			(_VB_010, ConfidenceLevel.High, "Dim api As New ImportAPI(userName, password, webServiceUrl)"),
			(_VB_011, ConfidenceLevel.High, "With settings\n\t.RelativityUsername = userName\n\t.RelativityPassword = password\nEnd With"),
			(_VB_012, ConfidenceLevel.Medium, "Dim handler As New HttpClientHandler\nhandler.Credentials = New NetworkCredential(userName, password)"),
			(_VB_013, ConfidenceLevel.Medium, "Dim credential = New NetworkCredential(userName, password)"),
			(_VB_014, ConfidenceLevel.Medium, "Dim pair = userName & \":\" & password\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
			(_VB_015, ConfidenceLevel.Medium, "Dim pair = \"\" & \":\" & token\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
			(_VB_016, ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(address, destination, userName, password)"),
			(_VB_017, ConfidenceLevel.Medium, "Dim request As Object = CreateObject(\"MSXML2.ServerXMLHTTP.6.0\")\nrequest.Open(\"GET\", address, False, userName, password)"),
			(_VB_018, ConfidenceLevel.Low, "Dim smtp As New SmtpClient With {.Credentials = New NetworkCredential(userName, password)}"),
			(_VB_019, ConfidenceLevel.Low, "Private Function BuildBasicAuthHeader(userName As String) As String\nEnd Function\nDim credential As String = Nothing")
		];
	}

	private static readonly ImmutableArray<(string PatternClass, ConfidenceLevel Level, string Source)> _coveredForms =
	[
		// VB-001: F1, F2, F3, F8 (qualified, Global, namespace alias), F10 in order
		(_VB_001, ConfidenceLevel.High, "Dim handed = Wrap(New UsernamePasswordCredentials(userName, password))"),
		(_VB_001, ConfidenceLevel.High, "Dim c As Credentials = New UsernamePasswordCredentials(userName, password)"),
		(_VB_001, ConfidenceLevel.High, "Dim c As Credentials\nc = New UsernamePasswordCredentials(userName, password)"),
		(_VB_001, ConfidenceLevel.High, "Dim c As New Vendor.Sdk.UsernamePasswordCredentials(userName, password)"),
		(_VB_001, ConfidenceLevel.High, "Dim c As New Global.Vendor.Sdk.UsernamePasswordCredentials(userName, password)"),
		(_VB_001, ConfidenceLevel.High, "Imports Sdk = Vendor.Sdk\nDim c As New Sdk.UsernamePasswordCredentials(userName, password)"),
		(_VB_001, ConfidenceLevel.High, "Dim c As New UsernamePasswordCredentials(userName:=login, password:=secret)"),
		(_VB_001, ConfidenceLevel.High, "Dim c As New UsernamePasswordCredentials(userName,\n\tpassword)"),
		(_VB_001, ConfidenceLevel.High, "Dim c As New UsernamePasswordCredentials(userName, _\n\tpassword)"),
		(_VB_001, ConfidenceLevel.High, "Dim c As New UsernamePasswordCredentials(userName, Decode(raw, key))"),
		(_VB_001, ConfidenceLevel.High, "dim c as new usernamepasswordcredentials(username, password)"),

		// VB-002
		(_VB_002, ConfidenceLevel.High, "Dim proxy As New Vendor.Sdk.RSAPIClient(New Uri(serviceAddress), New Vendor.Sdk.UsernamePasswordCredentials(userName, password))"),
		(_VB_002, ConfidenceLevel.High, "Dim proxy As RSAPIClient = New RSAPIClient(serviceUri, New UsernamePasswordCredentials(userName, password))"),

		// VB-003: the Add call, a literal, a constant, a call and a variable as the credential
		(_VB_003, ConfidenceLevel.High, "cache.Add(uri, \"Basic\", GetCredential())"),
		(_VB_003, ConfidenceLevel.High, "cache.Add(uri, \"basic\", credential)"),
		(_VB_003, ConfidenceLevel.High, "cache.Add(New Uri(root), \"Basic\", New NetworkCredential(userName:=login, password:=secret))"),
		(_VB_003, ConfidenceLevel.High, "Global.System.Net.CredentialCache.Add(New Uri(root), \"Basic\", New NetworkCredential(userName, password))"),

		// VB-004: F1, F2, F3 and F6 (a property assigned the authenticator)
		(_VB_004, ConfidenceLevel.High, "client.Authenticator = New HttpBasicAuthenticator(userName, password)"),
		(_VB_004, ConfidenceLevel.High, "Dim a As Authenticator = New RestBasicAuthenticator(userName, password)"),
		(_VB_004, ConfidenceLevel.High, "Dim a As New Lib.HttpBasicAuthenticator(userName, password, Encoding.UTF8)"),
		// F1
		(_VB_004, ConfidenceLevel.High, "Dim wrapped = Wrap(New HttpBasicAuthenticator(userName, password))"),
		// F10
		(_VB_004, ConfidenceLevel.High, "Dim a As New HttpBasicAuthenticator(userName:=login, password:=secret)"),

		// VB-005: F5 (leading-dot call in a With) and F6 (the call itself)
		(_VB_005, ConfidenceLevel.High, "With request\n\t.WithBasicAuth(userName, password)\nEnd With"),
		(_VB_005, ConfidenceLevel.High, "Dim configured = builder.WithBasicAuthentication(userName, password)"),
		// F10
		(_VB_005, ConfidenceLevel.High, "Dim configured = request.WithBasicAuth(userName:=login, password:=secret)"),
		// F8
		(_VB_005, ConfidenceLevel.High, "Dim configured = Vendor.Http.Builder.WithBasicAuth(userName, password)"),

		// VB-006: F1, F2, F3, F5, the static factory, a bare-identifier scheme
		(_VB_006, ConfidenceLevel.Medium, "With client.DefaultRequestHeaders\n\t.Authorization = New AuthenticationHeaderValue(\"Basic\", encodedCredentials)\nEnd With"),
		(_VB_006, ConfidenceLevel.Medium, "Dim header As New AuthenticationHeaderValue(\"Basic\", encoded)"),
		(_VB_006, ConfidenceLevel.Medium, "Dim header As AuthenticationHeaderValue = New AuthenticationHeaderValue(\"BASIC\", Encode(user, pass))"),
		(_VB_006, ConfidenceLevel.Medium, "request.Headers.Authorization = New System.Net.Http.Headers.AuthenticationHeaderValue(\"Basic\", parts(1))"),
		(_VB_006, ConfidenceLevel.Medium, "Dim header = AuthenticationHeaderValue.Parse(\"Basic \" & encoded)"),
		(_VB_006, ConfidenceLevel.Medium, "Dim header = AuthenticationHeaderValue.Parse(\"Basic dXNlcjpwYXNz\")"),
		(_VB_006, ConfidenceLevel.Medium, "Private Const BASIC_SCHEME As String = \"Basic\"\nDim header As New AuthenticationHeaderValue(BASIC_SCHEME, encoded)"),
		// F1
		(_VB_006, ConfidenceLevel.Medium, "Send(New AuthenticationHeaderValue(\"Basic\", encoded))"),
		// F10
		(_VB_006, ConfidenceLevel.Medium, "Dim header As New AuthenticationHeaderValue(scheme:=\"Basic\", parameter:=encoded)"),

		// VB-007: W1 with every method, the parenthesized indexer, W2 forms, V1 to V8 and alternative P
		(_VB_007, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Add(\"Authorization\", \"Basic \" & encoded)"),
		(_VB_007, ConfidenceLevel.Medium, "request.Headers.Set(\"Authorization\", \"Basic \" & encoded)"),
		(_VB_007, ConfidenceLevel.Medium, "request.Headers.Append(\"Authorization\", \"Basic \" & encoded)"),
		(_VB_007, ConfidenceLevel.Medium, "request.AddHeader(\"Authorization\", \"Basic \" & encoded)"),
		(_VB_007, ConfidenceLevel.Medium, "http.SetRequestHeader(\"Authorization\", \"Basic \" & encoded)"),
		(_VB_007, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.TryAddWithoutValidation(\"Authorization\", \"Basic \" & encoded)"),
		(_VB_007, ConfidenceLevel.Medium, "request.Headers(\"Authorization\") = \"Basic \" & encoded"),
		(_VB_007, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Add(\"Authorization\", \"Basic dXNlcjpwYXNz\")"),
		(_VB_007, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Add(\"Authorization\", \"Basic \" + encoded)"),
		(_VB_007, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Add(\"Authorization\", $\"Basic {encoded}\")"),
		(_VB_007, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Add(\"Authorization\", String.Format(\"Basic {0}\", encoded))"),
		(_VB_007, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Add(\"Authorization\", String.Concat(\"Basic \", encoded))"),
		(_VB_007, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Add(\"Authorization\", CreateBasicValue(userName, password))"),
		(_VB_007, ConfidenceLevel.Medium, "Dim value As String = String.Format(\"Basic {0}\", encoded)\nclient.DefaultRequestHeaders.Add(\"Authorization\", value)"),
		(_VB_007, ConfidenceLevel.Medium, "Private Const SCHEME As String = \"Basic\"\nclient.DefaultRequestHeaders.Add(\"Authorization\", SCHEME & \" \" & encoded)"),
		(_VB_007, ConfidenceLevel.Medium, "Private Const AUTHORIZATION_HEADER As String = \"Authorization\"\nrequest.AddHeader(AUTHORIZATION_HEADER, \"Basic \" & encoded)"),
		(_VB_007, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Add(\"Authorization\", apiToken)"),
		(_VB_007, ConfidenceLevel.Medium, "With client.DefaultRequestHeaders\n\t.Add(\"Authorization\", \"Basic \" & encoded)\nEnd With"),
		(_VB_007, ConfidenceLevel.Medium, "http.SetRequestHeader(\"Authorization\", \"Basic \" & encoded)\nhttp.Send()"),
		// F10
		(_VB_007, ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Add(name:=\"Authorization\", value:=\"Basic \" & encoded)"),

		// VB-008
		(_VB_008, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, \"Basic \" & encoded)"),
		(_VB_008, ConfidenceLevel.Medium, "request.Headers.Set(System.Net.HttpRequestHeader.Authorization, String.Format(\"Basic {0}\", encoded))"),
		(_VB_008, ConfidenceLevel.Medium, "Dim h = client.Headers\nclient.Headers(System.Net.HttpRequestHeader.Authorization) = $\"Basic {encoded}\""),
		// F5
		(_VB_008, ConfidenceLevel.Medium, "With client\n\t.Headers(HttpRequestHeader.Authorization) = \"Basic \" & encoded\nEnd With"),

		// VB-009: F5 and F6
		(_VB_009, ConfidenceLevel.Medium, "With binding.Security.Transport\n\t.ClientCredentialType = HttpClientCredentialType.Basic\nEnd With"),
		(_VB_009, ConfidenceLevel.Medium, "binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Basic\nfactory.Credentials.UserName.Password = secret"),

		// VB-010: F1, F2, F3, F8 and the subtype
		(_VB_010, ConfidenceLevel.High, "Dim api As ImportAPI = New ImportAPI(userName, password, webServiceUrl)"),
		(_VB_010, ConfidenceLevel.High, "Dim api As New Vendor.Sdk.ImportAPI(userName, password, webServiceUrl)"),
		(_VB_010, ConfidenceLevel.High, "Dim api As New ExtendedImportAPI(userName, password, webServiceUrl)"),
		(_VB_010, ConfidenceLevel.High, "Dim api As New ImportAPI(userName, password, New Uri(root).ToString())"),
		// F10
		(_VB_010, ConfidenceLevel.High, "Dim api As New ImportAPI(userName:=login, password:=secret, webServiceUrl:=root)"),

		// VB-011: both orders, F5 and F6
		(_VB_011, ConfidenceLevel.High, "settings.RelativityUsername = userName\nsettings.RelativityPassword = password"),
		(_VB_011, ConfidenceLevel.High, "settings.RelativityPassword = password\nsettings.RelativityUsername = userName"),
		(_VB_011, ConfidenceLevel.High, "Dim settings As New Vendor.Sdk.Settings With {.RelativityUsername = userName, .RelativityPassword = password}"),
		// F5
		(_VB_011, ConfidenceLevel.High, "With importJob.Settings\n\t.RelativityUsername = userName\n\t.RelativityPassword = password\nEnd With"),

		// VB-012: the initializer, and the assignment after a typed creation, a typed With, the cast
		(_VB_012, ConfidenceLevel.Medium, "Dim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(userName, password)}"),
		(_VB_012, ConfidenceLevel.Medium, "Dim client As New WebClient()\nclient.Credentials = New NetworkCredential(userName, password)"),
		(_VB_012, ConfidenceLevel.Medium, "Dim request As HttpWebRequest = CType(WebRequest.Create(address), HttpWebRequest)\nrequest.Credentials = New NetworkCredential(userName, password)"),
		(_VB_012, ConfidenceLevel.Medium, "Dim request As HttpWebRequest = CType(WebRequest.Create(address), HttpWebRequest)\nWith request\n\t.Credentials = New NetworkCredential(userName, password)\nEnd With"),
		(_VB_012, ConfidenceLevel.Medium, "Dim handler = New System.Net.Http.HttpClientHandler()\nhandler.Credentials = New System.Net.NetworkCredential(userName, password)"),
		(_VB_012, ConfidenceLevel.Medium, "Dim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(userName:=login, password:=secret)}"),

		// VB-013: F1, F2, F3, F4 (both halves in one initializer), qualified, a call as the password
		(_VB_013, ConfidenceLevel.Medium, "Dim credential As New NetworkCredential(userName, password)"),
		(_VB_013, ConfidenceLevel.Medium, "Dim credential As ICredentials = New NetworkCredential(userName, password)"),
		(_VB_013, ConfidenceLevel.Medium, "Dim wrapped = Wrap(New System.Net.NetworkCredential(userName, password))"),
		(_VB_013, ConfidenceLevel.Medium, "Dim credential = New NetworkCredential(userName, Decode(raw, key))"),
		(_VB_013, ConfidenceLevel.Medium, "Dim credential = New NetworkCredential(userName:=login, password:=secret)"),
		(_VB_013, ConfidenceLevel.Medium, "factory.Credentials = New NetworkCredential(My.Settings.User, My.Settings.Secret)"),

		// VB-014: the three spellings of the pair and the encoder inside the call
		(_VB_014, ConfidenceLevel.Medium, "Dim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\" & password))"),
		(_VB_014, ConfidenceLevel.Medium, "Dim encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{userName}:{password}\"))"),
		(_VB_014, ConfidenceLevel.Medium, "Dim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format(\"{0}:{1}\", userName, password)))"),
		(_VB_014, ConfidenceLevel.Medium, "Dim pair = userName & \":\" & password\nDim bytes = Encoding.ASCII.GetBytes(pair)\nDim encoded = System.Convert.ToBase64String(bytes)"),
		(_VB_014, ConfidenceLevel.Medium, "Dim pair = String.Concat(userName, \":\", password)\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),

		// VB-015
		(_VB_015, ConfidenceLevel.Medium, "Dim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Empty & \":\" & token))"),
		(_VB_015, ConfidenceLevel.Medium, "Dim pair = \":\" & apiKey\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),

		// Further forms of VB-007, VB-008 and VB-015 (invented names)
		(_VB_007, ConfidenceLevel.Medium, "widgetClient.Headers.Add(\"Authorization\", String.Join(\" \", \"Basic\", sessionBlob))"),
		(_VB_007, ConfidenceLevel.Medium, "widgetClient.Headers.Add(AUTH_KEY_AUTHORIZATION, \"Basic \" & sessionBlob)"),
		(_VB_008, ConfidenceLevel.Medium, "With widgetClient.Headers\n\t.Add(HttpRequestHeader.Authorization, \"Basic \" & sessionBlob)\nEnd With"),
		(_VB_008, ConfidenceLevel.Medium, "With widgetClient\n\t.Headers.Set(HttpRequestHeader.Authorization, \"Basic \" & sessionBlob)\nEnd With"),
		(_VB_008, ConfidenceLevel.Medium, "widgetClient.Headers.Add(header:=HttpRequestHeader.Authorization, value:=\"Basic \" & sessionBlob)"),
		(_VB_008, ConfidenceLevel.Medium, "Dim hdrValue As String = \"Basic \" & sessionBlob\nwidgetClient.Headers(HttpRequestHeader.Authorization) = hdrValue"),
		(_VB_008, ConfidenceLevel.Medium, "Dim hdrValue As String = String.Format(\"Basic {0}\", sessionBlob)\nwidgetClient.Headers.Add(HttpRequestHeader.Authorization, hdrValue)"),
		(_VB_015, ConfidenceLevel.Medium, "Dim seed As String = \"\"\nDim pair = seed & \":\" & sessionBlob\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_015, ConfidenceLevel.Medium, "Dim pair = String.Format(\"{0}:{1}\", String.Empty, sessionBlob)\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_015, ConfidenceLevel.Medium, "Dim pair = $\"{sessionBlob}:{String.Empty}\"\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_015, ConfidenceLevel.Medium, "Dim pair = String.Concat(\"\", \":\", sessionBlob)\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_015, ConfidenceLevel.Medium, "Dim pair = String.Join(\":\", String.Empty, sessionBlob)\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_015, ConfidenceLevel.Medium, "Dim pair = sessionBlob & \":\" & \"\"\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_015, ConfidenceLevel.Medium, "Dim encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(\":\" & sessionBlob))"),
		(_VB_015, ConfidenceLevel.Medium, "Dim pair = sessionBlob & \":\"\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_015, ConfidenceLevel.Medium, "Dim encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(sessionBlob & \":\"))"),

		// Further forms of VB-012, VB-013, VB-017 and VB-018 (invented names)
		(_VB_012, ConfidenceLevel.Medium, "Dim h As New SocketsHttpHandler With {.Credentials = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret}}"),
		(_VB_012, ConfidenceLevel.Medium, "Dim h As New SocketsHttpHandler With {.Credentials = New NetworkCredential With {.Password = widgetSecret, .UserName = widgetUser}}"),
		(_VB_012, ConfidenceLevel.Medium, "Dim h As New HttpClientHandler With {.Credentials = New NetworkCredential() With {.UserName = widgetUser, .Password = widgetSecret}}"),
		(_VB_012, ConfidenceLevel.Medium, "Dim h As New HttpClientHandler With {.Credentials = New NetworkCredential With {\n\t.UserName = widgetUser,\n\t.Password = widgetSecret\n}}"),
		(_VB_012, ConfidenceLevel.Medium, "Dim w = New WebClient()\nw.Credentials = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret}"),
		(_VB_012, ConfidenceLevel.Medium, "With New WebClient()\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_012, ConfidenceLevel.Medium, "With CType(widgetRequest, HttpWebRequest)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_012, ConfidenceLevel.Medium, "With DirectCast(widgetRequest, System.Net.HttpWebRequest)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_012, ConfidenceLevel.Medium, "With TryCast(widgetRequest, HttpWebRequest)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_012, ConfidenceLevel.Medium, "With CType(widgetFactory.Create(address), HttpWebRequest)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_012, ConfidenceLevel.Medium, "CType(widgetRequest, HttpWebRequest).Credentials = New NetworkCredential(widgetUser, widgetSecret)"),
		(_VB_012, ConfidenceLevel.Medium, "DirectCast(widgetRequest, HttpWebRequest).Credentials = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret}"),
		(_VB_012, ConfidenceLevel.Medium, "Dim proxy As SoapHttpClientProtocol = New WidgetProxy()\nproxy.Credentials = New NetworkCredential(widgetUser, widgetSecret)"),
		(_VB_012, ConfidenceLevel.Medium, "Dim proxy As HttpWebClientProtocol = New WidgetProxy()\nproxy.Credentials = New NetworkCredential(widgetUser, widgetSecret)"),
		(_VB_012, ConfidenceLevel.Medium, "Dim proxy As New SoapHttpClientProtocol()\nproxy.Credentials = New NetworkCredential With {.Password = widgetSecret, .UserName = widgetUser}"),
		(_VB_013, ConfidenceLevel.Medium, "Dim c = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret}"),
		(_VB_013, ConfidenceLevel.Medium, "Dim c = New NetworkCredential With {.Password = widgetSecret, .UserName = widgetUser}"),
		(_VB_013, ConfidenceLevel.Medium, "Dim c As New NetworkCredential() With {.UserName = widgetUser, .Password = widgetSecret}"),
		(_VB_013, ConfidenceLevel.Medium, "Dim c As New NetworkCredential With {\n\t.UserName = widgetUser,\n\t.Password = widgetSecret\n}"),
		(_VB_013, ConfidenceLevel.Medium, "consumer.Connect(address, New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret})"),
		(_VB_017, ConfidenceLevel.Medium, "http.Open(bstrMethod:=\"GET\", bstrUrl:=address, varAsync:=False, bstrUser:=widgetUser, bstrPassword:=widgetPwd)"),
		(_VB_017, ConfidenceLevel.Medium, "http.Open(bstrMethod:=\"PUT\", _\n\tbstrUrl:=address, _\n\tvarAsync:=False, _\n\tbstrUser:=widgetUser, _\n\tbstrPassword:=widgetPwd)"),
		(_VB_017, ConfidenceLevel.Medium, "http.Open(\"POST\", address, varAsync:=True, bstrUser:=widgetUser, bstrPassword:=widgetPwd)"),
		(_VB_017, ConfidenceLevel.Medium, "http.SetCredentials(bstrUserName:=widgetUser, bstrPassword:=widgetPwd, Flags:=0)"),
		(_VB_017, ConfidenceLevel.Medium, "http.SetCredentials(widgetUser, bstrPassword:=widgetPwd, Flags:=HTTPREQUEST_SETCREDENTIALS_FOR_SERVER)"),
		(_VB_018, ConfidenceLevel.Low, "With New SmtpClient(widgetHost, 25)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_018, ConfidenceLevel.Low, "With New WebProxy(address)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_018, ConfidenceLevel.Low, "With New System.Net.Mail.SmtpClient(widgetHost, 25)\n\t.EnableSsl = True\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_018, ConfidenceLevel.Low, "Dim r = DirectCast(FtpWebRequest.Create(address), FtpWebRequest)\nr.Credentials = New NetworkCredential(widgetUser, widgetSecret)"),
		(_VB_018, ConfidenceLevel.Low, "Dim r = TryCast(System.Net.FtpWebRequest.Create(address), FtpWebRequest)\nr.Credentials = New NetworkCredential(widgetUser, widgetSecret)"),
		(_VB_018, ConfidenceLevel.Low, "Dim r = CType(WebRequest.Create(address), FtpWebRequest)\nr.Credentials = New NetworkCredential(widgetUser, widgetSecret)"),
		(_VB_018, ConfidenceLevel.Low, "Dim entry As New DirectoryEntry(widgetPath, widgetUser, widgetSecret)"),
		(_VB_018, ConfidenceLevel.Low, "Dim entry = New System.DirectoryServices.DirectoryEntry(widgetPath, widgetUser, widgetSecret, AuthenticationTypes.Secure)"),
		(_VB_018, ConfidenceLevel.Low, "Dim entry As New DirectoryEntry(path:=widgetPath, username:=widgetUser, password:=widgetSecret)"),
		(_VB_018, ConfidenceLevel.Low, "Dim finder As New DirectorySearcher(New DirectoryEntry(widgetPath, widgetUser, widgetSecret))"),
		(_VB_018, ConfidenceLevel.Low, "Dim psi As New ProcessStartInfo(widgetExe) With {.UserName = widgetUser, .Password = widgetSecret}"),
		(_VB_018, ConfidenceLevel.Low, "Dim psi As New ProcessStartInfo(widgetExe) With {.Password = widgetSecret, .UserName = widgetUser}"),
		(_VB_018, ConfidenceLevel.Low, "Dim psi As New ProcessStartInfo(widgetExe)\npsi.UserName = widgetUser\npsi.Password = widgetSecret"),
		(_VB_018, ConfidenceLevel.Low, "Dim psi As ProcessStartInfo = New ProcessStartInfo(widgetExe)\npsi.Password = widgetSecret\npsi.UserName = widgetUser"),
		(_VB_018, ConfidenceLevel.Low, "With New ProcessStartInfo(widgetExe)\n\t.UserName = widgetUser\n\t.Password = widgetSecret\nEnd With"),
		(_VB_018, ConfidenceLevel.Low, "Process.Start(widgetExe, widgetUser, widgetSecret, widgetDomain)"),

		// Narrowed readings of VB-007, VB-014, VB-001, VB-002, VB-010 and VB-016: the read forms (invented names)
		(_VB_007, ConfidenceLevel.Medium, "widgetClient.Headers.Add(\"Authorization\", authToken)"),
		(_VB_007, ConfidenceLevel.Medium, "widgetClient.Headers.Add(\"Authorization\", token)"),
		(_VB_007, ConfidenceLevel.Medium, "widgetClient.Headers.Add(\"Authorization\", sessionBearishToken)"),
		(_VB_007, ConfidenceLevel.Medium, "widgetClient.Headers.Add(\"Authorization\", unbearableToken)"),
		(_VB_007, ConfidenceLevel.Medium, "widgetClient.Headers.Add(\"Authorization\", accessoryToken)"),
		(_VB_007, ConfidenceLevel.Medium, "widgetClient.Headers.Add(\"Authorization\", accessCredential)"),
		(_VB_007, ConfidenceLevel.Medium, "widgetClient.Headers.Add(\"Authorization\", encodedCredential)"),
		(_VB_007, ConfidenceLevel.Medium, "widgetClient.Headers.Add(\"Authorization\", settings.apiKey)"),
		(_VB_007, ConfidenceLevel.Medium, "widgetClient.Headers(\"Authorization\") = authToken"),
		(_VB_014, ConfidenceLevel.Medium, "Dim pair = widgetHost & \":\" & passport\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, ConfidenceLevel.Medium, "Dim pair = widgetHost & \":\" & portal\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, ConfidenceLevel.Medium, "Dim pair = widgetHost & \":\" & Passport\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, ConfidenceLevel.Medium, "Dim pair = widgetHost & \":\" & portalName\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, ConfidenceLevel.Medium, "Dim pair = widgetHost & \":\" & supportCode\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, ConfidenceLevel.Medium, "Dim pair = widgetHost & \":\" & widgetPassword\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, ConfidenceLevel.Medium, "Dim pair = widgetHost & \":\" & report\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, ConfidenceLevel.Medium, "Dim encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{widgetHost}:{passport}\"))"),
		(_VB_001, ConfidenceLevel.High, "Public Class WidgetHolder\nEnd Class\nDim c = New Vendor.Sdk.UsernamePasswordCredentials(userName, password)"),
		(_VB_001, ConfidenceLevel.High, "Public Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\nDim c = New Vendor.Sdk.UsernamePasswordCredentials(userName, password)"),
		(_VB_001, ConfidenceLevel.High, "Public Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\nDim c = New Global.Vendor.Sdk.UsernamePasswordCredentials(userName, password)"),
		(_VB_001, ConfidenceLevel.High, "Public Class UsernamePasswordCredentialsFactory\nEnd Class\nDim c = New UsernamePasswordCredentials(userName, password)"),
		(_VB_002, ConfidenceLevel.High, "Public Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\nDim c As New RSAPIClient(rsapiUri, New Vendor.Sdk.UsernamePasswordCredentials(userName, password))"),
		(_VB_010, ConfidenceLevel.High, "Dim api As New ImportAPI(userName, password, webServiceUrl)"),
		(_VB_010, ConfidenceLevel.High, "Dim api As New ImportAPI(userName, Me.ImportPassword, webServiceUrl)"),
		(_VB_010, ConfidenceLevel.High, "Dim api As New ImportAPI(userName, tokenPassword, webServiceUrl)"),
		(_VB_010, ConfidenceLevel.High, "Dim api As New ImportAPI(userName, userSecretToken, webServiceUrl)"),
		(_VB_010, ConfidenceLevel.High, "Dim api As New ImportAPI(userName, pwd, webServiceUrl)"),
		(_VB_010, ConfidenceLevel.High, "Dim api As New ImportAPI(userName, \"literal\", webServiceUrl)"),
		(_VB_010, ConfidenceLevel.High, "Dim api As New ImportAPI(userName, password:=secret, webServiceUrl)"),
		(_VB_016, ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(\"https://host.test/file.bin\", destination, userName, password)"),
		(_VB_016, ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(\"http://host.test/file.bin\", destination, userName, password)"),
		(_VB_016, ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(widgetAddress, destination, userName, password)"),
		(_VB_016, ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(New Uri(\"https://host.test/file.bin\"), destination, userName, password)"),
		(_VB_016, ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(address:=\"https://host.test/f\", destination, userName, password)"),
		(_VB_016, ConfidenceLevel.Medium, "Private Const widgetFeed As String = \"https://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)"),
		(_VB_016, ConfidenceLevel.Medium, "Private Const widgetFeed As String = \"ftp://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(otherFeed, destination, userName, password)"),

		(_VB_016, ConfidenceLevel.Medium, "Private Const widgetFeed As String = \"https://host.test/file.bin\"\nPrivate Const otherFeed As String = \"ftp://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)"),
		(_VB_016, ConfidenceLevel.Medium, "Private Const widgetFeed As String = \"https://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)\nPrivate Const widgetFeedBackup As String = \"ftp://host.test/file.bin\""),
		// VB-016: F5 (With My.Computer.Network) and the credential-worded third argument
		(_VB_016, ConfidenceLevel.Medium, "With My.Computer.Network\n\t.DownloadFile(address, destination, userName, password)\nEnd With"),
		(_VB_016, ConfidenceLevel.Medium, "My.Computer.Network.UploadFile(source, address, userName, password)"),
		(_VB_016, ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(address, destination, userName, password, True, 100000, True)"),
		// F10
		(_VB_016, ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(address:=source, destinationFileName:=destination, userName:=login, password:=secret)"),

		// VB-017: Open with five arguments, SetCredentials, and the cast of the late-bound object
		(_VB_017, ConfidenceLevel.Medium, "Dim http As Object = CreateObject(\"MSXML2.ServerXMLHTTP.6.0\")\nhttp.Open(\"POST\", address, False, userName, password)"),
		(_VB_017, ConfidenceLevel.Medium, "Dim http As Object = CreateObject(\"WinHttp.WinHttpRequest.5.1\")\nhttp.SetCredentials(userName, password, 0)"),
		// F7
		(_VB_017, ConfidenceLevel.Medium, "Dim http = CType(CreateObject(\"MSXML2.ServerXMLHTTP.6.0\"), Object)\nhttp.Open(\"GET\", address, False, userName, password)"),

		// VB-018: sinks, the cast FtpWebRequest, the host-and-port cache entry
		(_VB_018, ConfidenceLevel.Low, "Dim proxy As New WebProxy(address) With {.Credentials = New NetworkCredential(userName, password)}"),
		(_VB_018, ConfidenceLevel.Low, "Dim ftp = DirectCast(WebRequest.Create(address), FtpWebRequest)\nftp.Credentials = New NetworkCredential(userName, password)"),
		(_VB_018, ConfidenceLevel.Low, "cache.Add(host, 25, \"Basic\", New NetworkCredential(userName, password))"),
		// F5
		(_VB_018, ConfidenceLevel.Low, "Dim proxy As New WebProxy(address)\nWith proxy\n\t.Credentials = New NetworkCredential(userName, password)\nEnd With"),
		// F6
		(_VB_018, ConfidenceLevel.Low, "Dim proxy As New WebProxy(address)\nproxy.Credentials = New NetworkCredential(userName, password)"),

		// VB-019: modifiers and the three name tails
		(_VB_019, ConfidenceLevel.Low, "Public Shared Function GetBasicAuthorization(userName As String) As String\nEnd Function\nDim credential As String"),
		(_VB_019, ConfidenceLevel.Low, "Private Sub ApplyBasicHeader(request As HttpRequestMessage)\nEnd Sub\nDim credential As String"),
		(_VB_019, ConfidenceLevel.Low, "Friend Async Function MakeBasicCredentials() As Task\nEnd Function\nDim credential As String")
	];

	private static readonly ImmutableArray<(string PatternClass, string Source)> _declinedForms =
	[
		// VB-001: a bearer type; a single argument; the alias of the type (D 12); the initializer (D 10); a cast (NA 1); out-of-order names (D 14)
		(_VB_001, "Dim c As New BearerTokenCredentials(accessToken)"),
		(_VB_001, "Dim c As New UsernamePasswordCredentials(userName)"),
		(_VB_001, "Imports Creds = Vendor.Sdk.UsernamePasswordCredentials\nDim c As New Creds(userName, password)"),
		(_VB_001, "Dim c = CType(existing, UsernamePasswordCredentials)"),
		(_VB_001, "Dim c As New UsernamePasswordCredentials With {.UserName = userName, .Password = password}"),
		(_VB_001, "Dim value = Activator.CreateInstance(GetType(UsernamePasswordCredentials), userName, password)"),

		// VB-002: the retired client with no credential, or a bearer one
		(_VB_002, "Dim proxy As New RSAPIClient(serviceUri, New BearerTokenCredentials(accessToken))"),
		(_VB_002, "Dim proxy = factory.CreateProxy(Of IRSAPIClient)()"),

		// VB-003: a scheme other than Basic, a cache entry without a scheme literal, the host-and-port overload (VB-018's), an initializer (D 16)
		(_VB_003, "cache.Add(New Uri(root), \"Bearer\", New NetworkCredential(userName, password))"),
		(_VB_003, "cache.Add(New Uri(root), \"NTLM\", New NetworkCredential(userName, password))"),
		(_VB_003, "cache.Add(host, 25, \"Basic\", New NetworkCredential(userName, password))"),
		(_VB_003, "Dim cache As New CredentialCache From {{uri, \"Basic\", credential}}"),

		// VB-004: only one of the two tokens, or one argument
		(_VB_004, "Dim a As New BasicWidget(userName, password)"),
		(_VB_004, "Dim a As New HttpAuthenticator(userName, password)"),
		(_VB_004, "Dim a As New HttpBasicAuthenticator(userName)"),

		// VB-005: a single argument, a name carrying only the scheme word
		(_VB_005, "Dim configured = request.WithBasicAuth(userName)"),
		(_VB_005, "Dim configured = request.WithBasic(userName, password)"),

		// VB-006: another scheme; a TryParse or a Parse of a variable; a type alias
		(_VB_006, "client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue(\"Bearer\", accessToken)"),
		(_VB_006, "AuthenticationHeaderValue.TryParse(rawValue, parsed)"),
		(_VB_006, "Dim header = AuthenticationHeaderValue.Parse(rawValue)"),
		(_VB_006, "Imports Header = System.Net.Http.Headers.AuthenticationHeaderValue\nDim h As New Header(\"Basic\", encoded)"),

		// VB-007: a read, the proxy key, another header, another scheme, a neutral-name value (declared)
		(_VB_007, "Dim value = Request.Headers(\"Authorization\")"),
		(_VB_007, "client.DefaultRequestHeaders.Add(\"Proxy-Authorization\", \"Basic \" & encoded)"),
		(_VB_007, "client.DefaultRequestHeaders.Add(\"Accept\", \"Basic \" & encoded)"),
		(_VB_007, "client.DefaultRequestHeaders.Add(\"Authorization\", \"Bearer \" & accessToken)"),
		(_VB_007, "client.DefaultRequestHeaders.Add(\"Authorization\", headerValue)"),
		(_VB_007, "Dim headers As New WebHeaderCollection From {{\"Authorization\", encoded}}"),

		// VB-008: a different enumeration member, another scheme
		(_VB_008, "request.Headers.Add(HttpRequestHeader.ContentType, \"Basic \" & encoded)"),
		(_VB_008, "request.Headers.Add(HttpRequestHeader.Authorization, \"Bearer \" & accessToken)"),

		// VB-009: another credential type
		(_VB_009, "binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Windows"),
		(_VB_009, "binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate"),

		// VB-010: arity two; a different suffix
		(_VB_010, "Dim api As New ImportAPI(userName, password)"),
		(_VB_010, "Dim api As New ImportAPIFactory(userName, password, webServiceUrl)"),

		// VB-011: one of the two properties
		(_VB_011, "settings.RelativityUsername = userName"),
		(_VB_011, "settings.RelativityPassword = password"),

		// VB-012: a default-credentials assignment; a non-HTTP receiver
		(_VB_012, "Dim handler As New HttpClientHandler()\nhandler.Credentials = CredentialCache.DefaultCredentials"),
		(_VB_012, "Dim smtp As New SmtpClient()\nsmtp.Credentials = New NetworkCredential(userName, password)"),
		(_VB_012, "Dim client As New WebClient()\nclient.Timeout = 30"),

		// VB-013: a domain (three arguments), a cache entry's credential (VB-003's), one argument, out-of-order names (D 14)
		(_VB_013, "Dim credential = New NetworkCredential(userName, password, domain)"),
		(_VB_013, "Dim credential = New NetworkCredential()"),
		(_VB_013, "cache.Add(New Uri(root), \"Basic\", New NetworkCredential(userName, password))"),

		// VB-014: no colon pair; a pair that is not followed by an encoder; a hash
		(_VB_014, "Dim encoded = Convert.ToBase64String(File.ReadAllBytes(path))"),
		(_VB_014, "Dim pair = userName & \":\" & password"),
		(_VB_014, "Dim hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)))"),
		(_VB_014, "Dim encoded = Base64Helper.Encode(userName & \":\" & password)"),

		// VB-015: a pair with both halves present
		(_VB_015, "Dim pair = userName & \":\" & password\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),

		// Near-misses beside each further form above, invented names
		(_VB_007, "widgetClient.Headers.Add(HttpRequestHeader.ProxyAuthorization, \"Basic \" & sessionBlob)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", String.Join(\", \", \"Basic\", sessionBlob))"),
		(_VB_008, "Dim hdrValue As String = \"Bearer \" & sessionBlob\nwidgetClient.Headers(HttpRequestHeader.Authorization) = hdrValue"),
		(_VB_008, "Dim hdrValue As String = sessionBlob\nwidgetClient.Headers(HttpRequestHeader.Authorization) = hdrValue"),
		(_VB_008, "With widgetClient.Headers\n\t.Add(HttpRequestHeader.ContentType, \"Basic \" & sessionBlob)\nEnd With"),
		(_VB_015, "seed = \"\"\n\tDim filler0 = 0\n\tDim filler1 = 1\n\tDim filler2 = 2\n\tDim filler3 = 3\n\tDim filler4 = 4\n\tDim filler5 = 5\n\tDim filler6 = 6\n\tDim filler7 = 7\n\tDim filler8 = 8\n\tDim filler9 = 9\n\tDim filler10 = 10\npair = seed & \":\" & sessionBlob\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_015, "If seed = \"\" Then\n\tpair = seed & \":\" & sessionBlob\nEnd If\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_015, "Dim pair = String.Format(\"{0}:{1}\", leftName, sessionBlob)\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_015, "Dim encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(\":\" & sessionBlob & suffix))"),
		(_VB_015, "Dim pair = sessionBlob & \":\" & suffix\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_015, "Dim label = sessionBlob & \":\"\nDim shown = label.Trim()"),

		// Near-misses beside the further forms of VB-012, VB-013, VB-017 and VB-018, invented names
		(_VB_012, "Dim h As New HttpClientHandler With {.Credentials = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret, .Domain = widgetDomain}}"),
		(_VB_012, "Dim h As New HttpClientHandler With {.Credentials = New NetworkCredential With {.UserName = widgetUser}}"),
		(_VB_012, "Dim h As New WidgetHandler With {.Credentials = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret}}"),
		(_VB_012, "With New WidgetClient()\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_012, "With CType(widgetRequest, WidgetRequest)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_012, "CType(widgetRequest, SmtpClient).Credentials = New NetworkCredential(widgetUser, widgetSecret)"),
		(_VB_012, "With widgetClient\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_012, "With New WebClient()\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret, widgetDomain)\nEnd With"),
		(_VB_012, "Dim proxy As SoapHttpClientProtocol = New WidgetProxy()\nproxy.Credentials = CredentialCache.DefaultCredentials"),
		(_VB_013, "Dim c = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret, .Domain = widgetDomain}"),
		(_VB_013, "Dim c = New NetworkCredential With {.UserName = widgetUser}"),
		(_VB_013, "Dim c = New NetworkCredential With {.Password = widgetSecret}"),
		(_VB_013, "cache.Add(New Uri(root), \"Digest\", New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret})"),
		(_VB_017, "http.Open(bstrMethod:=\"LOCK\", bstrUrl:=address, varAsync:=False, bstrUser:=widgetUser, bstrPassword:=widgetPwd)"),
		(_VB_017, "http.Open(bstrMethod:=\"GET\", bstrUrl:=address, varAsync:=False, bstrUser:=widgetUser)"),
		(_VB_017, "http.Open(bstrMethod:=verb, bstrUrl:=address, varAsync:=False, bstrUser:=widgetUser, bstrPassword:=widgetPwd)"),
		(_VB_017, "http.SetCredentials(bstrUserName:=widgetUser)"),
		(_VB_018, "Dim r = DirectCast(FtpWebRequest.Create(address), WidgetRequest)\nr.Credentials = New NetworkCredential(widgetUser, widgetSecret)"),
		(_VB_018, "With New WidgetClient(widgetHost, 25)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With"),
		(_VB_018, "Dim entry As New DirectoryEntry(widgetPath)"),
		(_VB_018, "Dim entry As New DirectoryEntry(widgetPath, widgetUser)"),
		(_VB_018, "Dim finder As New DirectorySearcher(widgetEntry)"),
		(_VB_018, "Dim psi As New ProcessStartInfo(widgetExe)\npsi.UserName = widgetUser"),
		(_VB_018, "Dim psi As New ProcessStartInfo(widgetExe) With {.FileName = widgetExe, .Arguments = widgetArgs}"),
		(_VB_018, "Process.Start(widgetExe, widgetArgs)"),
		(_VB_018, "Dim psi As New WidgetInfo(widgetExe)\npsi.UserName = widgetUser\npsi.Password = widgetSecret"),

		// Near-misses of the narrowed readings above (invented names)
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", bearerToken)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", BearerHeader)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", accessToken)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", AccessToken)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", access_token)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", ACCESS_TOKEN)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", bearer_value)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", userBearer)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", BEARER_TOKEN)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", settings.AccessToken)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", Me.userBearer)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", bearer)"),
		(_VB_007, "widgetClient.Headers.Add(\"Authorization\", Access_Token)"),
		(_VB_007, "widgetClient.Headers(\"Authorization\") = bearerToken"),
		(_VB_014, "Dim pair = widgetHost & \":\" & port\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, "Dim pair = widgetHost & \":\" & portNumber\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, "Dim pair = widgetHost & \":\" & portNo\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, "Dim pair = widgetHost & \":\" & PortNum\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, "Dim pair = widgetHost & \":\" & server_port\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, "Dim pair = widgetHost & \":\" & SERVER_PORT\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, "Dim pair = widgetHost & \":\" & Me.WidgetPort\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, "Dim pair = widgetHost & \":\" & settings.PORT_NUMBER\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_014, "Dim encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(widgetHost & \":\" & serverPort))"),
		(_VB_014, "Dim encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{widgetHost}:{portNumber}\"))"),
		(_VB_014, "Dim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format(\"{0}:{1}\", widgetHost, portNo)))"),
		(_VB_014, "Dim pair = String.Concat(widgetHost, \":\", PortNum)\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		(_VB_001, "Public Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\nDim c = New UsernamePasswordCredentials(userName, password)"),
		(_VB_001, "Public Structure UsernamePasswordCredentials\nEnd Structure\nDim c As New UsernamePasswordCredentials(userName, password)"),
		(_VB_001, "Dim c = New UsernamePasswordCredentials(userName, password)\nPublic Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\n"),
		(_VB_002, "Public Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\nDim c As New RSAPIClient(rsapiUri, New UsernamePasswordCredentials(userName, password))"),
		(_VB_010, "Dim api As New ImportAPI(userName, systemToken, webServiceUrl)"),
		(_VB_010, "Dim api As New ImportAPI(userName, bearerToken, webServiceUrl)"),
		(_VB_010, "Dim api As New ImportAPI(userName, Me.AuthToken, webServiceUrl)"),
		(_VB_010, "Dim api As New ImportAPI(userName, TOKEN, webServiceUrl)"),
		(_VB_010, "Dim api As New ImportAPI(userName, sessionToken, webServiceUrl)"),
		(_VB_010, "Dim api As New ImportAPI(userName, password:=systemToken, webServiceUrl)"),
		(_VB_016, "My.Computer.Network.DownloadFile(\"ftp://host.test/file.bin\", destination, userName, password)"),
		(_VB_016, "My.Computer.Network.DownloadFile(\"ftps://host.test/file.bin\", destination, userName, password)"),
		(_VB_016, "My.Computer.Network.DownloadFile(\"sftp://host.test/file.bin\", destination, userName, password)"),
		(_VB_016, "My.Computer.Network.DownloadFile(\"FTP://host.test/file.bin\", destination, userName, password)"),
		(_VB_016, "My.Computer.Network.DownloadFile(New Uri(\"ftp://host.test/file.bin\"), destination, userName, password)"),
		(_VB_016, "My.Computer.Network.DownloadFile(New System.Uri(\"ftps://host.test/file.bin\"), destination, userName, password)"),
		(_VB_016, "My.Computer.Network.DownloadFile(address:=\"ftp://host.test/file.bin\", destination, userName, password)"),
		(_VB_016, "Private Const widgetFeed As String = \"ftp://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)"),
		(_VB_016, "Dim widgetFeed As String = \"sftp://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)"),
		(_VB_016, "Dim widgetFeed = \"ftps://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)"),
		(_VB_016, "Public Const Widget_FEED As String = \"ftp://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widget_feed, destination, userName, password)"),
		(_VB_016, "My.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)\nPrivate Const widgetFeed As String = \"ftp://host.test/file.bin\""),
		(_VB_016, "Private Const widgetFeed As String = \"ftp://host.test/file.bin\"\nPublic Sub Pull()\n\tDim filler0 = 0\n\tDim filler1 = 1\n\tDim filler2 = 2\n\tMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)\nEnd Sub"),
		(_VB_016, "With My.Computer.Network\n\t.DownloadFile(\"ftp://host.test/file.bin\", destination, userName, password)\nEnd With"),

		// VB-016: a transfer with fewer than four arguments (no credential), a file-system method of the same name outside the network class
		(_VB_016, "My.Computer.Network.DownloadFile(address, destination)"),
		(_VB_016, "client.DownloadFile(address, destination, userName, password)"),

		// VB-017: Open with fewer than five arguments, a non-HTTP verb
		(_VB_017, "request.Open(\"GET\", address, False)"),
		(_VB_017, "connection.Open(\"Provider=X\", address, False, userName, password)"),

		// VB-018: a plain HTTP sink (VB-012's)
		(_VB_018, "Dim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(userName, password)}"),

		// VB-019: a call and not a definition; a name without the scheme word
		(_VB_019, "Dim header = BuildBasicAuthHeader(userName)"),
		(_VB_019, "Private Function BuildHeader(userName As String) As String\nEnd Function")
	];

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static ILanguageAnalyzer ReadAnalyzer()
	{
		return LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_LANGUAGE_NAME];
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

	private static void AssertFires(string source, string patternClass, ConfidenceLevel level)
	{
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		Assert.True(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass && construct.Level == level),
			$"{patternClass} did not fire at {level} on [{source}]; got [{Describe(constructs)}]");
	}

	private static void AssertNeverFires(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		Assert.False(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass),
			$"{patternClass} fired on [{source}]; got [{Describe(constructs)}]");
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));
	}
}
