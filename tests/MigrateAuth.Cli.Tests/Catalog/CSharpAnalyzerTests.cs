using System.Collections.Immutable;
using System.Linq;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The `CSharp` analyzer's structural half. CS-001..CS-009 and CS-019 against
/// the catalog, through the real embedded analyzer and the real recognition
/// engine - never through a hand-built <see cref="RecognizedConstruct"/>, because what is under
/// test is whether the regular expressions the analyzer declares actually recognize the forms the catalog
/// states and decline the exclusions it states.
///
/// Every source fixture below is SYNTHETIC, written from the catalog's own prose rather than from
/// any repository, with generic names throughout.
/// </summary>
public sealed class CSharpAnalyzerTests
{
	private const string _LANGUAGE_NAME = "CSharp";

	private const string _CS_001 = "CS-001";
	private const string _CS_002 = "CS-002";
	private const string _CS_003 = "CS-003";
	private const string _CS_004 = "CS-004";
	private const string _CS_005 = "CS-005";
	private const string _CS_006 = "CS-006";
	private const string _CS_007 = "CS-007";
	private const string _CS_008 = "CS-008";
	private const string _CS_009 = "CS-009";
	private const string _CS_011 = "CS-011";
	private const string _CS_012 = "CS-012";
	private const string _CS_013 = "CS-013";
	private const string _CS_014 = "CS-014";
	private const string _CS_015 = "CS-015";
	private const string _CS_016 = "CS-016";
	private const string _CS_017 = "CS-017";
	private const string _CS_018 = "CS-018";
	private const string _CS_019 = "CS-019";

	/// <summary>The fence lines that wrap a template's source form.</summary>
	private const string _CODE_FENCE_OPENING = "```csharp\n";
	private const string _CODE_FENCE_CLOSING = "\n```";

	/// <summary>The termination proofs read the anchor group back by name.</summary>
	private const string _CONSTRUCT_CAPTURE_GROUP_NAME = "construct";

	private static readonly ImmutableArray<string> _internalTerminologyFragments =
	[
		"catalog", "Section ", "subsumption", "BCL", "CS-0", "catch", "rule loop",
		"suspected but not proven", "helper-definition class", "truncation counter",
		"DR-", "R-CS", "inc DR", "lexical", "anchor", "match bound", "per-file", "profile"
	];

	// ------------------------------------------------------------------------------------------
	// Golden criterion: the analyzer fits the contract with no engine change.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheCSharpAnalyzerFitsTheContractWithNoViolations()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(analyzer);

		Assert.Empty(violations);
	}

	[Fact]
	public void TheCSharpAnalyzerIsRegisteredInTheEmbeddedCatalogUnderDotCs()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		Assert.True(catalog.AnalyzersByLanguageName.ContainsKey(_LANGUAGE_NAME));
		Assert.True(catalog.TryGetAnalyzerForFileExtension(".cs", out ILanguageAnalyzer? analyzer));
		Assert.Same(catalog.AnalyzersByLanguageName[_LANGUAGE_NAME], analyzer);
	}

	[Fact]
	public void EighteenPatternClassesAreDeclared()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.Equal(18, analyzer.DetectionRulesByPatternClass.Count);
	}

	[Fact]
	public void FourteenPreselectionAnchorsAreDeclared()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.Equal(14, analyzer.CandidateSelectionAndRecommendation.PreselectionAnchors.Count);
	}

	// ------------------------------------------------------------------------------------------
	// Every rule's match timeout is the policy bound.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EveryDetectionRuleExpressionCarriesThePolicyMatchTimeout()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.All(
			analyzer.DetectionRulesByPatternClass.Values,
			rule => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, rule.Expression.MatchTimeout));
	}

	[Fact]
	public void EveryContentMarkerCarriesThePolicyMatchTimeout()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.NotEmpty(analyzer.DemotionSignals.ContentMarkers);
		Assert.All(
			analyzer.DemotionSignals.ContentMarkers,
			marker => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, marker.Expression.MatchTimeout));
	}

	// ------------------------------------------------------------------------------------------
	// DemotionSignals is contract element 4, and AnalyzerContractValidator.Validate checks
	// only one thing about it - that each content marker's match timeout is policy-bound
	// (ContentMarkerMatchTimeoutNotPolicyBound). The rest of element 4, including the demotion
	// this test exercises, is unchecked structurally and is proved functionally instead.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void ElementFourDemotionSignalsAreNotValidatedStructurallyButFireFunctionally()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credentials = new UsernamePasswordCredentials(userName, "hunter2");
			""";

		ImmutableArray<RecognizedConstruct> underTestFolder =
			harness.Recognize(source, "src/SomeProject.Tests/Module.cs");
		ImmutableArray<RecognizedConstruct> underOrdinaryFolder =
			harness.Recognize(source, "src/SomeProject/Module.cs");

		RecognizedConstruct demoted = Assert.Single(underTestFolder);
		RecognizedConstruct undemoted = Assert.Single(underOrdinaryFolder);

		Assert.Equal(ConfidenceLevel.Medium, demoted.Level);
		Assert.True(demoted.TestCode);
		Assert.Equal(ConfidenceLevel.High, undemoted.Level);
	}

	[Fact]
	public void TheDotTestsFolderSuffixSignalDemotes()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credentials = new UsernamePasswordCredentials(userName, "hunter2");
			""";

		ImmutableArray<RecognizedConstruct> recognized =
			harness.Recognize(source, "src/Sample.Core.Tests/Module.cs");

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void ADotTestsFileNameSuffixDemotes()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credentials = new UsernamePasswordCredentials(userName, "hunter2");
			""";

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(source, "source/Module.Tests.cs");

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void AGeneratedCodeHeaderContentMarkerDemotesEvenThoughItSitsInAComment()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			// <auto-generated>
			var credentials = new UsernamePasswordCredentials(userName, "hunter2");
			""";

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(source);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	[Fact]
	public void ATestAttributeContentMarkerDemotesOnlyWhenItSitsOnCode()
	{
		CSharpRecognitionHarness harness = new();
		const string demotedSource = """
			[Fact]
			var credentials = new UsernamePasswordCredentials(userName, "hunter2");
			""";
		const string undemotedSource = """
			// mentions [Fact] only in prose
			var credentials = new UsernamePasswordCredentials(userName, "hunter2");
			""";

		RecognizedConstruct demoted = Assert.Single(harness.Recognize(demotedSource));
		RecognizedConstruct undemoted = Assert.Single(harness.Recognize(undemotedSource));

		Assert.Equal(ConfidenceLevel.Medium, demoted.Level);
		Assert.Equal(ConfidenceLevel.High, undemoted.Level);
	}

	// ------------------------------------------------------------------------------------------
	// CS-001 SdkUserNamePasswordCredentials
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS001MatchesTheDirectConstructionForm()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			Credentials credentials = new UsernamePasswordCredentials(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_001, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	[Fact]
	public void CS001MatchesTheTargetTypedFormOfCSharpNine()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			UsernamePasswordCredentials credentials = new(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_001, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	[Fact]
	public void CS001MatchesTheEmbeddedLiteralCredentialFormAndSignalsIt()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credentials = new UsernamePasswordCredentials("admin", "hunter2");
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_001, construct.PatternClassIdentifier);
		Assert.True(construct.LiteralCredentialSignal);
	}

	/// <summary>
	/// The bounded argument expression: a member-access chain, the ordinary shape of a credential
	/// read off a settings object. Not a literal, so the literal-credential signal stays false.
	/// </summary>
	[Fact]
	public void CS001MatchesArgumentsWrittenAsMemberAccessChains()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credentials = new UsernamePasswordCredentials(options.UserName, options.Password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_001, construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// CS-002 SdkLegacyRsapiCredentials, and the CS-002/CS-001 subsumption triple.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS002SubsumesCS001AtIdenticalAnchorPositions()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var client = new RSAPIClient(rsapiUri, new UsernamePasswordCredentials(userName, password));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_002, construct.PatternClassIdentifier);
	}

	[Fact]
	public void CS001AloneStillMatchesWhenNotWrappedByRSAPIClient()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credentials = new UsernamePasswordCredentials(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_001, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// The catalog's declared cost: the retired RSAPI client reached WITHOUT a credential is
	/// not covered. CS-002 requires the nested credential type, so a generic factory construction
	/// carrying no credential argument must not match at all.
	/// </summary>
	[Fact]
	public void CS002DoesNotMatchAnRSAPIClientReachedWithoutACredential()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var client = proxyFactory.CreateProxy<RSAPIClient>();
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>
	/// The nested credential pair as embedded literals emits exactly one CS-002 finding, with
	/// CS-001 subsumed rather than co-emitted.
	/// </summary>
	[Fact]
	public void CS002MatchesEmbeddedLiteralCredentialArgumentsAndStillSubsumesCS001()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var client = new RSAPIClient(rsapiUri, new UsernamePasswordCredentials("admin", "hunter2"));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_002, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// The bounded argument expression, applied to the outer client's own first argument and to both
	/// halves of the nested pair alike - a member-access chain reading the URI and the credentials off
	/// a settings object.
	/// </summary>
	[Fact]
	public void CS002MatchesArgumentsWrittenAsMemberAccessChains()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var client = new RSAPIClient(settings.Uri, new UsernamePasswordCredentials(settings.UserName, settings.Credentials.Secret));
			""";

		Assert.Contains(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_002);
	}

	/// <summary>
	/// A wrapper that merely receives the nested credential, with no <c>RSAPIClient</c> in front of it,
	/// is CS-001's construct, never CS-002's.
	/// </summary>
	[Fact]
	public void CS002DoesNotMatchANestedCredentialWithNoRSAPIClientInFrontOfIt()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var client = Wrap(settings, new UsernamePasswordCredentials(userName, password));
			""";

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(source);

		Assert.DoesNotContain(recognized, construct => construct.PatternClassIdentifier == _CS_002);
		Assert.Equal(_CS_001, Assert.Single(recognized).PatternClassIdentifier);
	}

	/// <summary>
	/// A string literal as the outer client's first argument yields exactly one CS-002.
	/// </summary>
	[Fact]
	public void CS002MatchesALiteralFirstArgumentAndSubsumesCS001()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var client = new RSAPIClient("rsapi-endpoint", new UsernamePasswordCredentials(userName, password));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_002, construct.PatternClassIdentifier);
	}

	// ------------------------------------------------------------------------------------------
	// CS-003 CredentialCacheBasicScheme
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// CS-003 matches, and the nested <c>NetworkCredential</c> construction that CS-008 would also read
	/// is subsumed by it: the catalog declares CS-003 as subsuming CS-008 at overlapping spans, so the
	/// cache entry is one finding.
	/// </summary>
	[Fact]
	public void CS003MatchesTheCacheAddInvocationWithALiteralSchemeAndSubsumesCS008()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			cache.Add(new Uri(instanceUrl), "Basic", new NetworkCredential(userName, password));
			""";

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(source);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_CS_003, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	/// <summary>
	/// The scheme verdict suppresses CS-003 itself; the nested CS-008 match is a separate rule with
	/// no scheme argument of its own, so it is unaffected and still reports the bare construction.
	/// </summary>
	[Fact]
	public void CS003SuppressesItselfWhenTheResolvedSchemeIsNotBasicButCS008StillFires()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			const string SCHEME = "Digest";
			cache.Add(new Uri(instanceUrl), SCHEME, new NetworkCredential(userName, password));
			""";

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(source);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_CS_008, construct.PatternClassIdentifier);
	}

	/// <summary>The bounded argument expression: member-access chains for both halves of the pair.</summary>
	[Fact]
	public void CS003MatchesArgumentsWrittenAsMemberAccessChains()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			cache.Add(new Uri(instanceUrl), "Basic", new NetworkCredential(settings.UserName, settings.Credentials.Secret));
			""";

		Assert.Contains(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_003);
	}

	/// <summary>
	/// The <c>new Uri(...)</c> argument accepts one level of nested parentheses, so a call such as
	/// <c>new Uri(x.ToString())</c> is read.
	/// </summary>
	[Fact]
	public void CS003MatchesANestedUriConstructorArgumentWithOneLevelOfNesting()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			cache.Add(new Uri(instanceUri.ToString()), "Basic", new NetworkCredential(userName, password));
			""";

		Assert.Contains(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_003);
	}

	// ------------------------------------------------------------------------------------------
	// CS-004 ThirdPartyBasicAuthenticatorType
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS004MatchesAnAuthenticatorTypeCarryingBothTheSchemeAndTheRoleToken()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var authenticator = new HttpBasicAuthenticator(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_004, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	[Fact]
	public void CS004MatchesRegardlessOfATrailingEncodingArgument()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var authenticator = new HttpBasicAuthenticator(userName, password, encoding);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_004, construct.PatternClassIdentifier);
	}

	[Fact]
	public void CS004DoesNotMatchAUnrelatedBasicToken()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var options = new ExampleOptions { UseBasicParsing = true };
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>
	/// The class is defined by a type name carrying both the scheme token and an authenticator-role
	/// token, with no requirement that either be prefixed. The bare archetype name, with no prefix
	/// before <c>Basic</c> at all, must match.
	/// </summary>
	[Fact]
	public void CS004MatchesTheBareArchetypeNameWithNoPrefix()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var authenticator = new BasicAuthenticator(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_004, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	/// <summary>The bounded argument expression: member-access chains for both halves of the pair.</summary>
	[Fact]
	public void CS004MatchesArgumentsWrittenAsMemberAccessChains()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var authenticator = new HttpBasicAuthenticator(settings.UserName, settings.Credentials.Secret);
			""";

		Assert.Contains(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_004);
	}

	// ------------------------------------------------------------------------------------------
	// CS-005 AuthorizationHeaderValueScheme
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS005MatchesAndComposesToMediumBecauseItIsNotSelfSufficient()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_005, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>The scheme verdict, resolved and not matching, suppresses the finding entirely.</summary>
	[Fact]
	public void CS005SuppressesTheFindingOnAResolvedNonBasicConstant()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			const string SCHEME_TOKEN = "Digest";
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(SCHEME_TOKEN, encodedCredentials);
			""";

		Assert.Empty(harness.Recognize(source));
	}

	[Fact]
	public void CS005LandsAtMediumWhenTheSchemeConstantIsIndeterminate()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(SCHEME_TOKEN, encodedCredentials);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_005, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>The bounded argument expression: a member-access chain reading a credential off a settings object.</summary>
	[Fact]
	public void CS005MatchesAMemberAccessCredentialArgument()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", settings.EncodedCredentials);
			""";

		Assert.Contains(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_005);
	}

	/// <summary>The bounded argument expression: a member-access chain ending in one invocation.</summary>
	[Fact]
	public void CS005MatchesAnInvocationCredentialArgument()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentialBytes));
			""";

		Assert.Contains(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_005);
	}

	/// <summary>
	/// Both the scheme and the credential arguments accept a plain quoted literal (the scheme via its
	/// own literal-or-identifier alternative, the credential via <see cref="_ARGUMENT_EXPRESSION"/>'s
	/// own literal branch), and the finding fires with the literal-credential signal set.
	/// </summary>
	[Fact]
	public void CS005MatchesTheFullyLiteralSchemeAndCredentialFormAndSignalsIt()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var h = new AuthenticationHeaderValue("Basic", "ZGVtbzpodW50ZXIy");
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_005, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.True(construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// CS-006 AuthorizationHeaderStringLiteral - the anchor opens on the literal's opening quote.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS006MatchesTheAddFormWithAnInterpolatedSchemeValue()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedCredentials}");
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
		// High base, but not self-sufficient (RequiresCallPath), so composition lands it at Medium.
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>
	/// The anchor resolves to the header literal's opening quote - a Delimiter zone,
	/// which counts as code - so the class keeps its High base rather than reading as StringBody.
	/// </summary>
	[Fact]
	public void CS006AnchorZoneIsDelimiterNotStringBody()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedCredentials}");
			""";

		FileRecognitionContext context = harness.ContextFor(source);
		DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[_CS_006];
		System.Text.RegularExpressions.Match match = rule.Expression.Match(context.NormalizedText);

		Assert.True(match.Success);
		Assert.True(rule.AnchorPosition.TryResolveOffset(match, out int anchorOffset));
		Assert.Equal(LexicalZone.Delimiter, context.Mask.ZoneAt(anchorOffset));
	}

	/// <summary>The rule requires a write, not a read - the catalog's own stated exclusion.</summary>
	[Fact]
	public void CS006DoesNotMatchAReadOfTheAuthorizationHeader()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var header = Request.Headers["Authorization"];
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>
	/// The bounded argument expression, inside the interpolation hole: a member-access chain ending
	/// in one invocation.
	/// </summary>
	[Fact]
	public void CS006MatchesAnInvocationCredentialArgumentInsideTheHole()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(credentialBytes)}");
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// The named-constant CONCATENATION scheme form matches structurally. The verification for this
	/// one is deliberately mechanical rather than verdict-specific - see the three tests below for
	/// why the concatenation form, not the interpolation-hole form, is where verdict correctness is
	/// actually provable, and the reason recorded there for both.
	/// </summary>
	[Fact]
	public void CS006MatchesTheNamedConstantConcatenationForm()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			const string SCHEME = "Basic";
			client.DefaultRequestHeaders.Add("Authorization", SCHEME + " " + encodedCredentials);
			""";

		Assert.Contains(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_006);
	}

	/// <summary>
	/// The named-constant CONCATENATION scheme form: a constant resolved to "Basic" confirms the
	/// pattern class, so the finding is reported.
	///
	/// THE CONCATENATION FORM, NOT THE INTERPOLATION-HOLE FORM, IS WHAT THESE THREE VERDICT TESTS
	/// USE. `CS-006`'s named-constant HOLE form and CS-009's own interpolated
	/// pair both open on <c>$"</c>, a two-character opening whose closing is the one-character <c>"</c>
	/// - and the shared scanner's <c>MatchAConstantOpening</c> sets
	/// <c>LiteralOccurrence.MinimumClosingLength</c> from the OPENING's matched length rather than
	/// the rule's declared <c>Closing</c> length. Under <c>DelimiterLength.Fixed</c> - every quote
	/// rule here except the raw literal - that closing can never satisfy a minimum longer than
	/// itself, so `$"..."`, `@"..."`, `$@"..."` and `@$"..."` never close through
	/// <c>TryCloseTheLiteral</c> at all: <c>$"{SCHEME}"</c> alone yields
	/// <c>ValidThroughPosition == 0</c>. The concatenation form's scheme and credential arguments
	/// sit in plain <c>"..."</c> literals, whose opening and closing are the same one-character token
	/// - unaffected - which is what makes it the form these three tests can actually prove something
	/// with.
	/// </summary>
	[Fact]
	public void CS006WithAConcatenatedNamedConstantResolvedToBasicIsAFinding()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			const string SCHEME = "Basic";
			client.DefaultRequestHeaders.Add("Authorization", SCHEME + " " + encodedCredentials);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// The named-constant CONCATENATION scheme form: a constant resolved to "Bearer" is
	/// resolved-and-not-matching, which the rule reads as SuppressesFinding - already-migrated code
	/// produces no finding at all. See <see cref="CS006WithAConcatenatedNamedConstantResolvedToBasicIsAFinding"/>
	/// for why this uses the concatenation form.
	/// </summary>
	[Fact]
	public void CS006WithAConcatenatedNamedConstantResolvedToBearerSuppressesTheFinding()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			const string SCHEME = "Bearer";
			client.DefaultRequestHeaders.Add("Authorization", SCHEME + " " + encodedCredentials);
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>
	/// The named-constant CONCATENATION scheme form: an undeclared (ambiguous) constant is
	/// indeterminate, which the rule reads as ProposesMediumLevel - the finding still reports. See
	/// <see cref="CS006WithAConcatenatedNamedConstantResolvedToBasicIsAFinding"/> for why this uses
	/// the concatenation form.
	/// </summary>
	[Fact]
	public void CS006WithAnAmbiguousConcatenatedNamedConstantProposesMediumLevel()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Add("Authorization", SCHEME + " " + encodedCredentials);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	// ------------------------------------------------------------------------------------------
	// CS-006: the fifth value shape - one fully literal string, no concatenation or
	// interpolation at all.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS006MatchesTheFullyLiteralSingleStringFormAndSignalsTheCredential()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			http.DefaultRequestHeaders.Add("Authorization", "Basic ZGVtbzpodW50ZXIy");
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.True(construct.LiteralCredentialSignal);
	}

	/// <summary>The fully literal form reached through a bare <c>.Add</c> call, with no <c>DefaultRequestHeaders</c> segment.</summary>
	[Fact]
	public void CS006MatchesTheFullyLiteralFormOnABareAddCall()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			request.Headers.Add("Authorization", "Basic ZGVtbzpodW50ZXIy");
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
	}

	/// <summary>The fully literal form reached through the indexer-assignment write, not only <c>.Add</c>.</summary>
	[Fact]
	public void CS006MatchesTheFullyLiteralFormOnTheIndexerAssignmentWrite()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			headers["Authorization"] = "Basic ZGVtbzpodW50ZXIy";
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// A non-Basic scheme in the fully literal form resolves and does not match, which suppresses the
	/// finding entirely - the same verdict the concatenated named-constant form already reaches, per
	/// <see cref="CS006WithAConcatenatedNamedConstantResolvedToBearerSuppressesTheFinding"/>.
	/// </summary>
	[Fact]
	public void CS006FullyLiteralFormSuppressesTheFindingOnABearerScheme()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			http.DefaultRequestHeaders.Add("Authorization", "Bearer accessToken123");
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>
	/// Must not overlap the sibling concatenated literal-prefix-scheme alternative: a closing quote
	/// immediately after the scheme word (rather than a token character) routes the match through the
	/// concatenation alternative instead, so the credential still reads the concatenation's own bare
	/// identifier and the literal-credential signal stays false.
	/// </summary>
	[Fact]
	public void CS006FullyLiteralAlternativeDoesNotOverlapTheConcatenatedForm()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedCredentials);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	/// <summary>
	/// Declared unsupported: a verbatim literal opens on <c>@"</c>, a
	/// token this alternative's own opening quote does not itself expect, so the fully literal form
	/// is not reached through it.
	/// </summary>
	[Fact]
	public void CS006DoesNotMatchTheFullyLiteralFormWrittenAsAVerbatimLiteral()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			http.DefaultRequestHeaders.Add("Authorization", @"Basic ZGVtbzpodW50ZXIy");
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>The catalog's own stated exclusion still holds for the fully literal shape: a read is not a write.</summary>
	[Fact]
	public void CS006DoesNotMatchAnIndexerReadComparedAgainstAFullyLiteralValue()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			if (headers["Authorization"] == "Basic abc") { }
			""";

		Assert.Empty(harness.Recognize(source));
	}

	// ------------------------------------------------------------------------------------------
	// CS-006: the partly literal value (a fully literal token followed by
	// concatenation), and the hole-free interpolated literal.
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// A fully literal token immediately followed by concatenation is not the pure fully literal
	/// shape - its own trailing negative lookahead declines this input - so the finding is carried by
	/// the sibling alternative whose credential group spans from the token through the concatenation
	/// into the appended expression. The captured extent therefore crosses into code, and the
	/// literal-credential signal reads false even though the header is still built from a partly
	/// hard-coded value and the finding still fires - keep the
	/// finding, signal false, rather than silently dropping a real positive.
	/// </summary>
	[Fact]
	public void CS006MatchesThePartlyLiteralFormAndTheSignalReadsFalse()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			http.DefaultRequestHeaders.Add("Authorization", "Basic abc" + suffix);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	/// <summary>Hole-free interpolated literal concatenated with an expression.</summary>
	[Fact]
	public void CS006MatchesTheHoleFreeInterpolatedPartlyLiteralFormAndTheSignalReadsFalse()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			http.DefaultRequestHeaders.Add("Authorization", $"Basic ZGVtbzpodW50ZXIy" + suffix);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	/// <summary>
	/// The partly literal shape's own credential group swallows the concatenation and the appended
	/// expression, so it never yields control to the widened literal-prefix concatenation
	/// alternative for the same input - a non-quote character immediately after the scheme's space
	/// (here, the token's own first letter) is what routes this input through the fully literal
	/// family rather than through the concatenation family, whose own scheme capture requires a
	/// closing quote there instead. One finding, not two.
	/// </summary>
	[Fact]
	public void CS006PartlyLiteralFormDoesNotAlsoMatchTheConcatenationAlternative()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			http.DefaultRequestHeaders.Add("Authorization", "Basic abc" + suffix);
			""";

		Assert.Single(harness.Recognize(source));
	}

	/// <summary>
	/// A hole-free interpolated literal reaches the fully literal shape through its own optional
	/// leading <c>$</c>: the credential token excludes <c>{</c> and so cannot cross an
	/// interpolation hole it does not itself have.
	/// </summary>
	[Fact]
	public void CS006MatchesTheHoleFreeInterpolatedFormAndSignalsTheCredential()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			http.DefaultRequestHeaders.Add("Authorization", $"Basic ZGVtbzpodW50ZXIy");
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
		Assert.True(construct.LiteralCredentialSignal);
	}

	/// <summary>
	/// An interpolated value that DOES carry a hole reaches only the interpolated literal-prefix-scheme
	/// alternative: the interpolated-hole alternative is tried first in the alternation and the fully
	/// literal alternative's own token cannot cross the hole's opening brace, so there is exactly one
	/// finding.
	/// </summary>
	[Fact]
	public void CS006StillMatchesTheInterpolatedFormWithAHoleExactlyOnce()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedCredentials}");
			""";

		Assert.Single(harness.Recognize(source));
	}

	/// <summary>
	/// The literal-prefix concatenation credential accepts the same grouped argument expression as
	/// the other credential positions, consistently with CS-012's own
	/// sibling alternative. A literal credential on that side of the concatenation is wholly source
	/// text, so the signal reads true.
	/// </summary>
	[Fact]
	public void CS006ConcatenatedLiteralPrefixSchemeWithALiteralCredentialSignalsTrue()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Add("Authorization", "Basic " + "abc");
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
		Assert.True(construct.LiteralCredentialSignal);
	}

	/// <summary>An identifier credential on the concatenated side stays Code, so the signal reads false.</summary>
	[Fact]
	public void CS006ConcatenatedLiteralPrefixSchemeWithAnIdentifierCredentialSignalsFalse()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Add("Authorization", "Basic " + x);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	/// <summary>A member-access-and-invocation credential is still captured whole, and the signal stays false.</summary>
	[Fact]
	public void CS006ConcatenatedLiteralPrefixSchemeWithAnInvocationCredentialMatchesWithSignalFalse()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(bytes));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_006, construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// CS-007 NetworkCredentialOnHttpHandler - the HTTP allow-list and the default-credentials
	// exclusion, and the CS-007/CS-008 subsumption triple.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS007MatchesTheHandlerInitializerFormAndSubsumesCS008()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var handler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) };
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_007, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>The catalog's own stated exclusion: default credentials are integrated auth, not Basic.</summary>
	[Fact]
	public void CS007DoesNotMatchDefaultCredentials()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var handler = new HttpClientHandler { Credentials = CredentialCache.DefaultCredentials };
			""";

		Assert.Empty(harness.Recognize(source));
	}

	[Fact]
	public void CS007DoesNotMatchUseDefaultCredentials()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var handler = new HttpClientHandler { UseDefaultCredentials = true };
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>The allow-list: a mail client is not an HTTP client or handler, so CS-007 declines it.</summary>
	[Fact]
	public void CS007DoesNotMatchANetworkCredentialBoundToAMailClient()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var client = new SmtpClient(host, port);
			client.Credentials = new NetworkCredential(userName, password);
			""";

		Assert.DoesNotContain(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_007);
	}

	/// <summary>The bounded argument expression: member-access chains for both halves of the pair.</summary>
	[Fact]
	public void CS007MatchesArgumentsWrittenAsMemberAccessChains()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var handler = new HttpClientHandler { Credentials = new NetworkCredential(settings.UserName, settings.Credentials.Secret) };
			""";

		Assert.Contains(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_007);
	}

	/// <summary>
	/// The direct-assignment form: the allow-listed type constructed into a variable, followed within
	/// the bounded window by that SAME variable's <c>Credentials</c> property receiving the credential
	/// construction, must match CS-007 and subsume the otherwise-independent CS-008 match on the same
	/// construction.
	/// </summary>
	[Fact]
	public void CS007MatchesTheDirectAssignmentFormAndSubsumesCS008()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var handler = new HttpClientHandler();
			handler.Credentials = new NetworkCredential(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_007, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>The allow-list still declines a mail client in the direct-assignment form, exactly as it does in the initializer form.</summary>
	[Fact]
	public void CS007DirectAssignmentDoesNotMatchOnANonAllowListedType()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var client = new SmtpClient(host, port);
			client.Credentials = new NetworkCredential(userName, password);
			""";

		Assert.DoesNotContain(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_007);
	}

	/// <summary>
	/// The backreference requires the SAME variable: a construction assigned to one variable followed
	/// by a property write through a different one must not match CS-007's direct-assignment form.
	/// </summary>
	[Fact]
	public void CS007DirectAssignmentDoesNotMatchWhenThePropertyIsSetOnADifferentVariable()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var primary = new HttpClientHandler();
			secondary.Credentials = new NetworkCredential(userName, password);
			""";

		Assert.DoesNotContain(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_007);
	}

	/// <summary>
	/// A lazy line-bounded window placed inside an OPTIONAL group makes the
	/// platform's source-generated regex return a malformed, repeating match. CS-007's window is
	/// REQUIRED, never optional, so exhausting it - the allow-listed construction followed by more than
	/// five unrelated lines and no property assignment at all - must still let enumeration of
	/// <see cref="Regex.Matches(string)"/> terminate, with every match actually returned carrying a
	/// successful <c>construct</c> group.
	/// </summary>
	[Fact]
	public void CS007DirectAssignmentWindowTerminatesWhenExhausted()
	{
		CSharpRecognitionHarness harness = new();
		string source = "var handler = new HttpClientHandler();\n"
			+ string.Concat(Enumerable.Range(1, 11).Select(i => $"var filler{i} = {i};\n"));

		AssertBoundedTermination(source, _CS_007);
	}

	/// <summary>
	/// The same required window, exercised over a few thousand lines with no allow-listed construction
	/// anywhere, to confirm the new alternative carries no catastrophic-backtracking risk at file scale.
	/// </summary>
	[Fact]
	public void CS007DirectAssignmentWindowTerminatesOverThousandsOfLines()
	{
		string source = string.Concat(Enumerable.Range(1, 4000).Select(i => $"var filler{i} = {i};\n"));

		AssertBoundedTermination(source, _CS_007);
	}

	/// <summary>
	/// Enumerates the real, source-generated expression of the given pattern class directly (bypassing
	/// the harness's own zone mask, which this termination proof does not need) with a hard iteration cap
	/// the loop itself enforces regardless of what the regex does, then asserts the regex ended
	/// enumeration before that cap was reached, and checks strict advance: each successive match's
	/// <c>Index</c> must exceed the previous
	/// one's, which is what actually distinguishes bounded termination from a malformed, repeating match.
	/// </summary>
	private static void AssertBoundedTermination(string source, string patternClassIdentifier)
	{
		const int maxIterations = 10_000;
		CSharpRecognitionHarness harness = new();
		DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[patternClassIdentifier];
		string normalizedText = RecognitionHarness.Normalize(source);

		int iterations = 0;
		int previousIndex = -1;
		System.Text.RegularExpressions.Match match = rule.Expression.Match(normalizedText);

		while (match.Success && iterations < maxIterations)
		{
			iterations++;
			Assert.True(match.Groups[_CONSTRUCT_CAPTURE_GROUP_NAME].Success);
			Assert.True(match.Index > previousIndex);
			previousIndex = match.Index;
			match = match.NextMatch();
		}

		Assert.True(iterations < maxIterations, "Enumeration did not terminate within the iteration cap.");
	}

	// ------------------------------------------------------------------------------------------
	// CS-008 NetworkCredentialConstruction - the residue, and its two subsumption triples.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS008MatchesAConstructionNoCatalogedBindingAccountsFor()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credential = new NetworkCredential(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_008, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>The bounded argument expression: member-access chains for both halves of the pair.</summary>
	[Fact]
	public void CS008MatchesArgumentsWrittenAsMemberAccessChains()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credential = new NetworkCredential(settings.UserName, settings.Credentials.Secret);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_008, construct.PatternClassIdentifier);
	}

	[Fact]
	public void CS019SubsumesCS008AtOverlappingSpansForANonHttpSink()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var client = new SmtpClient(host, port);
			client.Credentials = new NetworkCredential(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_019, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	// ------------------------------------------------------------------------------------------
	// CS-009 Base64UserPasswordPair - the colon-pair requirement, and CS-009/CS-018 exclusivity.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS009MatchesTheInterpolatedColonSeparatedPairReachingTheEncoderWithinTheWindow()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = $"{identifier}:{secret}";
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void CS009MatchesTheConcatenatedColonSeparatedPair()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = identifier + ":" + secret;
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// The catalog's own colon-pair requirement: BOTH halves must be non-empty. What the catalog
	/// assigns to CS-018 instead - an empty half - must not fire CS-009. This is the empty-half
	/// interpolated form of CS-018, placed as the file's only construct so the known base
	/// scanner defect (see the CS-018 rule's own doc comment and this file's CS-018 section below) has
	/// nothing after it to corrupt; the engine reads the encoder's anchor zone correctly regardless,
	/// and CS-018 fires at its own Medium base rather than CS-009 firing at all.
	/// </summary>
	[Fact]
	public void CS009DoesNotFireOnTheEmptyHalfFormWhichCS018FiresOnInstead()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = $"{""}:{accessToken}";
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(source);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_CS_018, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>
	/// The class's own template shows a source form; the analyzer must read that very text as the
	/// class it illustrates, at its base level, and not as the neighbouring pair class.
	/// </summary>
	[Fact]
	public void TheCS018TemplateSourceFormIsReadAsCS018AtMediumAndNotAsCS009()
	{
		CSharpRecognitionHarness harness = new();
		RecommendationTemplate template = harness.Analyzer.CandidateSelectionAndRecommendation
			.TemplateProvider(_CS_018, CredentialFlow.ClientCredentials)!;
		string source = template.GenericSourceForm
			.Replace(_CODE_FENCE_OPENING, string.Empty, StringComparison.Ordinal)
			.Replace(_CODE_FENCE_CLOSING, string.Empty, StringComparison.Ordinal);

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_018, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>
	/// The <c>string.Format("{0}:{1}", …)</c> spelling of the pair.
	/// </summary>
	[Fact]
	public void CS009MatchesTheStringFormatSpelling()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = string.Format("{0}:{1}", identifier, secret);
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
	}

	/// <summary>The colon-pair requirement still holds for the <c>string.Format</c> spelling.</summary>
	[Fact]
	public void CS009DoesNotFireOnAnEmptyHalfInTheStringFormatSpelling()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = string.Format("{0}:{1}", "", accessToken);
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>The <c>string.Join(":", …)</c> spelling of the pair.</summary>
	[Fact]
	public void CS009MatchesTheStringJoinSpelling()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = string.Join(":", identifier, secret);
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
	}

	/// <summary>The colon-pair requirement still holds for the <c>string.Join</c> spelling.</summary>
	[Fact]
	public void CS009DoesNotFireOnAnEmptyHalfInTheStringJoinSpelling()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = string.Join(":", "", accessToken);
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		Assert.Empty(harness.Recognize(source));
	}

	// ------------------------------------------------------------------------------------------
	// Recognition runs sequentially, one worker, degree 1 - not independently
	// testable as a claim about parallelism, so this asserts the
	// observable consequence: the same input recognized twice, through two fresh engine
	// instances, produces the identical ordered sequence every time.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void RecognitionIsDeterministicAcrossRepeatedRuns()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credentials = new UsernamePasswordCredentials(userName, password);
			var authenticator = new HttpBasicAuthenticator(userName, password);
			var result = client.WithBasicAuth(userName, password);
			""";

		ImmutableArray<RecognizedConstruct> firstRun = harness.Recognize(source);
		ImmutableArray<RecognizedConstruct> secondRun = harness.Recognize(source);

		Assert.Equal(3, firstRun.Length);
		// ImmutableArray<T>'s own IEquatable compares the underlying array by reference, not by
		// value - .ToArray() forces xUnit's element-wise sequence comparer instead.
		Assert.Equal(firstRun.ToArray(), secondRun.ToArray());
	}

	// ------------------------------------------------------------------------------------------
	// CS-011 AuthenticationHelperDefinition - the method declaration, and its co-emission with
	// CS-001 (the catalog's "three absences": no subsumption between the two).
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS011MatchesAHelperMethodDeclarationAndCoEmitsWithCS001()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			private ServiceFactorySettings BuildServiceFactoryCredentials(string userName, string password)
			{
				Credentials credentials = new UsernamePasswordCredentials(userName, password);
				ServiceFactorySettings settings = new ServiceFactorySettings(restEndpointUri, credentials);
				return settings;
			}
			""";

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(source);

		Assert.Equal(2, recognized.Length);
		Assert.Contains(recognized, construct => construct.PatternClassIdentifier == _CS_011 && construct.Level == ConfidenceLevel.Low);
		Assert.Contains(recognized, construct => construct.PatternClassIdentifier == _CS_001 && construct.Level == ConfidenceLevel.High);
	}

	/// <summary>A call site carries no access modifier and no opening brace, so it is not a DECLARATION.</summary>
	[Fact]
	public void CS011DoesNotMatchACallSite()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credentials = BuildServiceFactoryCredentials(userName, password);
			""";

		Assert.DoesNotContain(harness.Recognize(source), construct => construct.PatternClassIdentifier == _CS_011);
	}

	// ------------------------------------------------------------------------------------------
	// CS-012 HttpRequestHeaderEnumAuthorization - the write form only, and the scheme verdict.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS012MatchesTheWebClientHeaderEnumWriteFormWithALiteralScheme()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.Headers[HttpRequestHeader.Authorization] = "Basic " + encoded;
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_012, construct.PatternClassIdentifier);
		// High base, but not self-sufficient (RequiresCallPath), so composition lands it at Medium.
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>The catalog's own stated exclusion: reading the header is not a write.</summary>
	[Fact]
	public void CS012DoesNotMatchAReadOfTheHeaderEnumIndexer()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var value = client.Headers[HttpRequestHeader.Authorization];
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>The named-constant concatenation scheme form, resolved and matching, confirms the class.</summary>
	[Fact]
	public void CS012MatchesTheNamedConstantConcatenationFormResolvedToBasic()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			const string SCHEME = "Basic";
			request.Headers[HttpRequestHeader.Authorization] = SCHEME + " " + encoded;
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_012, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// The bounded argument expression in the named-constant scheme form: a member-access chain, or
	/// a chain ending in one invocation, is captured whole as the credential - not truncated at its
	/// first identifier - exactly as the literal-scheme form already captures it.
	/// </summary>
	[Theory]
	[InlineData("creds.Encoded")]
	[InlineData("GetPassword()")]
	[InlineData("vault.Secrets.Read(name)")]
	public void CS012NamedConstantFormCapturesAMemberAccessOrInvocationCredentialWhole(string credentialExpression)
	{
		CSharpRecognitionHarness harness = new();
		string source = "const string SCHEME = \"Basic\";\n"
			+ "request.Headers[HttpRequestHeader.Authorization] = SCHEME + \" \" + " + credentialExpression + ";\n";

		FileRecognitionContext context = harness.ContextFor(source);
		DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[_CS_012];
		System.Text.RegularExpressions.Match match = rule.Expression.Match(context.NormalizedText);

		Assert.True(match.Success);
		Assert.Equal(credentialExpression, match.Groups[rule.CredentialPosition.CaptureGroupName!].Value);
		Assert.Equal(_CS_012, Assert.Single(harness.Recognize(source)).PatternClassIdentifier);
	}

	// ------------------------------------------------------------------------------------------
	// CS-012: the third value shape - one fully literal string, no concatenation at all.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS012MatchesTheFullyLiteralSingleStringFormAndSignalsTheCredential()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			wc.Headers[HttpRequestHeader.Authorization] = "Basic ZGVtbzpodW50ZXIy";
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_012, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.True(construct.LiteralCredentialSignal);
	}

	/// <summary>The generic anchor - no restriction on what precedes <c>.Headers[...]</c> - already reaches <c>HttpWebRequest</c> just as it reaches <c>WebClient</c>.</summary>
	[Fact]
	public void CS012MatchesTheFullyLiteralFormOnAnHttpWebRequestHeaderCollection()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			request.Headers[HttpRequestHeader.Authorization] = "Basic ZGVtbzpodW50ZXIy";
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_012, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// A non-Basic scheme in the fully literal form resolves and does not match, which suppresses the
	/// finding entirely - the same verdict the concatenated named-constant form already reaches.
	/// </summary>
	[Fact]
	public void CS012FullyLiteralFormSuppressesTheFindingOnABearerScheme()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			wc.Headers[HttpRequestHeader.Authorization] = "Bearer accessToken123";
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>
	/// Must not overlap the sibling concatenated literal-prefix-scheme alternative: a closing quote
	/// immediately after the scheme word routes the match through the concatenation alternative
	/// instead, so the literal-credential signal stays false.
	/// </summary>
	[Fact]
	public void CS012FullyLiteralAlternativeDoesNotOverlapTheConcatenatedForm()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			client.Headers[HttpRequestHeader.Authorization] = "Basic " + encoded;
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_012, construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	/// <summary>Declared unsupported: a verbatim literal is not reached, the same declared limit as CS-006's sibling shape.</summary>
	[Fact]
	public void CS012DoesNotMatchTheFullyLiteralFormWrittenAsAVerbatimLiteral()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			wc.Headers[HttpRequestHeader.Authorization] = @"Basic ZGVtbzpodW50ZXIy";
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>The catalog's own stated exclusion still holds for the fully literal shape: a read compared against a literal is not a write.</summary>
	[Fact]
	public void CS012DoesNotMatchAReadComparedAgainstAFullyLiteralValue()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			if (headers[HttpRequestHeader.Authorization] == "Basic abc") { }
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>The catalog's own stated exclusion still holds: reading the indexer into a variable is not a write.</summary>
	[Fact]
	public void CS012DoesNotMatchAReadAssignedToAVariable()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string v = wc.Headers[HttpRequestHeader.Authorization];
			""";

		Assert.Empty(harness.Recognize(source));
	}

	// ------------------------------------------------------------------------------------------
	// CS-012: the same partly literal value and hole-free interpolated
	// literal as CS-006, mirrored here since both classes share the fully literal token.
	// ------------------------------------------------------------------------------------------

	/// <summary>Mirrors <see cref="CS006MatchesThePartlyLiteralFormAndTheSignalReadsFalse"/> for the header-enum write form.</summary>
	[Fact]
	public void CS012MatchesThePartlyLiteralFormAndTheSignalReadsFalse()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			wc.Headers[HttpRequestHeader.Authorization] = "Basic abc" + suffix;
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_012, construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	/// <summary>Hole-free interpolated literal concatenated with an expression.</summary>
	[Fact]
	public void CS012MatchesTheHoleFreeInterpolatedPartlyLiteralFormAndTheSignalReadsFalse()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			wc.Headers[HttpRequestHeader.Authorization] = $"Basic ZGVtbzpodW50ZXIy" + suffix;
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_012, construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	/// <summary>Mirrors <see cref="CS006PartlyLiteralFormDoesNotAlsoMatchTheConcatenationAlternative"/> for the header-enum write form.</summary>
	[Fact]
	public void CS012PartlyLiteralFormDoesNotAlsoMatchTheConcatenationAlternative()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			wc.Headers[HttpRequestHeader.Authorization] = "Basic abc" + suffix;
			""";

		Assert.Single(harness.Recognize(source));
	}

	/// <summary>Mirrors <see cref="CS006MatchesTheHoleFreeInterpolatedFormAndSignalsTheCredential"/> for the header-enum write form.</summary>
	[Fact]
	public void CS012MatchesTheHoleFreeInterpolatedFormAndSignalsTheCredential()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			wc.Headers[HttpRequestHeader.Authorization] = $"Basic ZGVtbzpodW50ZXIy";
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_012, construct.PatternClassIdentifier);
		Assert.True(construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// CS-013 WcfBasicTransportCredential - the optional bounded credential capture.
	// ------------------------------------------------------------------------------------------

	/// <summary>Absent capture: the credential half never appears, so the signal is correctly false, and the finding still exists.</summary>
	[Fact]
	public void CS013MatchesTheBindingAloneWithNoCredentialCaptureAndStillReportsAFinding()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_013, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
	}

	/// <summary>The optional capture, within the bounded five-line window, with a literal password.</summary>
	[Fact]
	public void CS013CapturesALiteralPasswordWithinTheBoundedWindowAndSignalsIt()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
			client.ClientCredentials.UserName.UserName = userName;
			client.ClientCredentials.UserName.Password = "hunter2";
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_013, construct.PatternClassIdentifier);
		Assert.True(construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// CS-014 SchemeNamingExtensionMethod - the catalog's own archetype spelling.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS014MatchesTheArchetypeExtensionMethod()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var result = client.WithBasicAuth(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_014, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	/// <summary>A method not named after the scheme must not match, even with the same two-argument shape.</summary>
	[Fact]
	public void CS014DoesNotMatchANonSchemeNamedMethod()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var result = client.WithAuth(userName, password);
			""";

		Assert.Empty(harness.Recognize(source));
	}

	// ------------------------------------------------------------------------------------------
	// CS-015 SdkConstructorWithUserAndPassword - the word boundary, and the arity requirement.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS015MatchesTheThreeArgumentConstructorForm()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var importApi = new ImportAPI(userName, password, webServiceUrl);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_015, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	/// <summary>
	/// The catalog's "one expression detail that decides a real case": a longer identifier
	/// carrying <c>ImportAPI</c> as a prefix must not match, because the trailing word boundary fails.
	/// </summary>
	[Fact]
	public void CS015DoesNotMatchALongerIdentifierCarryingTheTypeNameAsAPrefix()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var importApi = new ImportAPIWrapper(userName, password, webServiceUrl);
			""";

		Assert.Empty(harness.Recognize(source));
	}

	/// <summary>The catalog's own declared cost: the URL-only overload must not match.</summary>
	[Fact]
	public void CS015DoesNotMatchTheUrlOnlyOverload()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var importApi = new ImportAPI(webServiceUrl);
			""";

		Assert.Empty(harness.Recognize(source));
	}

	// ------------------------------------------------------------------------------------------
	// CS-016 CredentialAssembledAcrossStatements - the bounded five-line proximity window.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS016MatchesTheInitializerHalfCompletedByATrailingPropertyAssignment()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			NetworkCredential credential = new NetworkCredential { UserName = userName };
			credential.Password = password;
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_016, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>Beyond the bounded window, the trailing property assignment is out of reach.</summary>
	[Fact]
	public void CS016DoesNotMatchBeyondTheBoundedWindow()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			NetworkCredential credential = new NetworkCredential { UserName = userName };
			var filler1 = 1;
			var filler2 = 2;
			var filler3 = 3;
			var filler4 = 4;
			var filler5 = 5;
			var filler6 = 6;
			var filler7 = 7;
			var filler8 = 8;
			var filler9 = 9;
			var filler10 = 10;
			credential.Password = password;
			""";

		Assert.Empty(harness.Recognize(source));
	}

	// ------------------------------------------------------------------------------------------
	// CS-017 SdkSettingsCredentialProperties - both properties required.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS017MatchesBothSettingsPropertiesTogether()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			importJob.Settings.RelativityUsername = userName;
			importJob.Settings.RelativityPassword = password;
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_017, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	/// <summary>The catalog's own stated requirement: a username property alone is not a credential.</summary>
	[Fact]
	public void CS017DoesNotMatchTheUsernamePropertyAlone()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			importJob.Settings.RelativityUsername = userName;
			""";

		Assert.Empty(harness.Recognize(source));
	}

	// ------------------------------------------------------------------------------------------
	// CS-018 TokenCarriedUnderBasicScheme - the empty-half idiom, and CS-009/CS-018 exclusivity.
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// The empty-string-operand concatenation form. Provably unaffected by the known base scanner
	/// defect (see the CS-018 rule's own doc comment): no verbatim or interpolated literal precedes
	/// this construct anywhere in the fixture.
	/// </summary>
	[Fact]
	public void CS018MatchesTheEmptyStringOperandConcatenationForm()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = "" + ":" + accessToken;
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_018, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>The simpler assignment-anchored concatenation form, with no explicit empty-string operand.</summary>
	[Fact]
	public void CS018MatchesTheAssignmentAnchoredConcatenationForm()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = ":" + accessToken;
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_018, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// Mutual exclusion, from the CS-018 side: CS-009's own both-halves-non-empty construct must not
	/// also fire CS-018, since the assignment-anchored alternative requires the colon literal to open
	/// immediately after <c>=</c> with no identifier between - which <c>identifier + ":" + secret</c>
	/// does not satisfy.
	/// </summary>
	[Fact]
	public void CS018DoesNotMatchCS009sBothHalvesNonEmptyConstruct()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = identifier + ":" + secret;
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// The RIGHT half empty, as an explicit empty-string operand, mirroring the
	/// LEFT explicit-empty-operand form.
	/// </summary>
	[Fact]
	public void CS018MatchesTheRightEmptyExplicitOperandConcatenationForm()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = accessToken + ":" + "";
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_018, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>
	/// The RIGHT half empty with no explicit empty-string operand at all - the colon literal is the
	/// last thing before the statement's own <c>;</c>, mirroring the existing LEFT
	/// assignment-anchored form.
	/// </summary>
	[Fact]
	public void CS018MatchesTheRightEmptyEndOfExpressionConcatenationForm()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = accessToken + ":";
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_018, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// Mirrors <see cref="CS018DoesNotMatchCS009sBothHalvesNonEmptyConstruct"/> from the right-empty
	/// side: something (<c>+ b</c>) still stands between the colon and the statement's <c>;</c>, so
	/// CS-009's own construct is not mistaken for the end-of-expression form.
	/// </summary>
	[Fact]
	public void CS018RightEmptyEndOfExpressionFormDoesNotMatchCS009sBothHalvesNonEmptyConstruct()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = identifier + ":" + secret;
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// The RIGHT-empty INTERPOLATED mirror with no second hole at all, <c>$"{token}:"</c>. Proved
	/// structurally against the real, source-generated expression rather than through the full harness:
	/// a known base scanner defect (see this class's own doc comment and
	/// <see cref="CS018MatchesTheEmptyStringOperandConcatenationForm"/>'s neighbor) means an
	/// interpolated literal's zone past its own opening is not reliable evidence for a CONFIDENCE
	/// verdict, which is exactly why the LEFT interpolated form carries the same declared limitation
	/// and is not exercised end-to-end either.
	/// </summary>
	[Fact]
	public void CS018MatchesTheRightEmptyInterpolatedFormWithNoSecondHoleStructurally()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = $"{accessToken}:";
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[_CS_018];
		string normalizedText = RecognitionHarness.Normalize(source);
		System.Text.RegularExpressions.Match match = rule.Expression.Match(normalizedText);

		Assert.True(match.Success);
		Assert.Equal("accessToken", match.Groups[rule.CredentialPosition.CaptureGroupName!].Value);
	}

	/// <summary>
	/// The RIGHT-empty INTERPOLATED mirror with an explicit empty second hole, <c>$"{token}:{""}"</c>.
	/// Proved structurally for the same reason as
	/// <see cref="CS018MatchesTheRightEmptyInterpolatedFormWithNoSecondHoleStructurally"/>.
	/// </summary>
	[Fact]
	public void CS018MatchesTheRightEmptyInterpolatedFormWithAnExplicitEmptySecondHoleStructurally()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			string pair = $"{accessToken}:{""}";
			string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
			""";

		DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[_CS_018];
		string normalizedText = RecognitionHarness.Normalize(source);
		System.Text.RegularExpressions.Match match = rule.Expression.Match(normalizedText);

		Assert.True(match.Success);
		Assert.Equal("accessToken", match.Groups[rule.CredentialPosition.CaptureGroupName!].Value);
	}

	// ------------------------------------------------------------------------------------------
	// CS-019 NonHttpCredentialSink - the closed BCL set, and its own subsumption of CS-008.
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("SmtpClient")]
	[InlineData("FtpWebRequest")]
	[InlineData("DirectoryEntry")]
	[InlineData("DirectorySearcher")]
	[InlineData("WebProxy")]
	public void CS019MatchesEachMemberOfTheClosedNonHttpSinkSet(string sinkTypeName)
	{
		CSharpRecognitionHarness harness = new();
		string source = $"""
			var sink = new {sinkTypeName}(host, port);
			sink.Credentials = new NetworkCredential(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_019, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	/// <summary>The bounded argument expression: member-access chains for both halves of the pair.</summary>
	[Fact]
	public void CS019MatchesArgumentsWrittenAsMemberAccessChains()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var sink = new SmtpClient(host, port);
			sink.Credentials = new NetworkCredential(settings.UserName, settings.Credentials.Secret);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_019, construct.PatternClassIdentifier);
	}

	// ------------------------------------------------------------------------------------------
	// CS-019 - the two shapes the property-assignment shape cannot reach: FtpWebRequest's
	// cast-from-WebRequest.Create, and DirectoryEntry's credential-first constructor (which also
	// reaches DirectorySearcher through its root entry).
	// ------------------------------------------------------------------------------------------

	/// <summary>The var-declared form of the cast: <c>var x = (FtpWebRequest)WebRequest.Create(...)</c>.</summary>
	[Fact]
	public void CS019MatchesFtpWebRequestCastWithAVarDeclaration()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var request = (FtpWebRequest)WebRequest.Create(address);
			request.Credentials = new NetworkCredential(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_019, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	/// <summary>The explicitly typed declaration form of the same cast.</summary>
	[Fact]
	public void CS019MatchesFtpWebRequestCastWithAnExplicitlyTypedDeclaration()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create(address);
			request.Credentials = new NetworkCredential(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_019, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	/// <summary>The bounded window: a few unrelated lines between the cast and the credential write, within five.</summary>
	[Fact]
	public void CS019MatchesFtpWebRequestCastWithUnrelatedLinesWithinTheWindow()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var request = (FtpWebRequest)WebRequest.Create(address);
			var filler1 = 1;
			var filler2 = 2;
			request.Credentials = new NetworkCredential(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_019, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	/// <summary>The guard: an <c>HttpWebRequest</c> cast is not <c>FtpWebRequest</c>, so this stays at CS-008 only.</summary>
	[Fact]
	public void CS019DoesNotMatchAnHttpWebRequestCast()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var request = (HttpWebRequest)WebRequest.Create(address);
			request.Credentials = new NetworkCredential(userName, password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_008, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>The back-reference guard: an Ftp request on one variable and a credential write on a different (HttpWebRequest) variable must not subsume.</summary>
	[Fact]
	public void CS019DoesNotMatchWhenTheCredentialIsAssignedOnADifferentVariableThanTheFtpRequest()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var ftpRequest = (FtpWebRequest)WebRequest.Create(address);
			var httpRequest = (HttpWebRequest)WebRequest.Create(otherAddress);
			httpRequest.Credentials = new NetworkCredential(userName, password);
			""";

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(source);

		Assert.Contains(recognized, construct => construct.PatternClassIdentifier == _CS_008 && construct.Level == ConfidenceLevel.Medium);
		Assert.DoesNotContain(recognized, construct => construct.PatternClassIdentifier == _CS_019);
	}

	/// <summary>The credential-first shape: the <c>NetworkCredential</c> is built before the <c>DirectoryEntry</c> that consumes it.</summary>
	[Fact]
	public void CS019MatchesDirectoryEntryCredentialFirst()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credential = new NetworkCredential(userName, password);
			var entry = new DirectoryEntry(path, credential.UserName, credential.Password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_019, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	/// <summary><c>DirectorySearcher</c> takes no credential of its own; it is reached through its root entry, built on a separate statement.</summary>
	[Fact]
	public void CS019MatchesDirectorySearcherThroughASeparateRootEntry()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credential = new NetworkCredential(userName, password);
			var entry = new DirectoryEntry(path, credential.UserName, credential.Password);
			var searcher = new DirectorySearcher(entry);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_019, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	/// <summary>The same root entry, built inline as the <c>DirectorySearcher</c> constructor's own argument.</summary>
	[Fact]
	public void CS019MatchesDirectorySearcherWithAnInlineRootEntry()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credential = new NetworkCredential(userName, password);
			var searcher = new DirectorySearcher(new DirectoryEntry(path, credential.UserName, credential.Password));
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_019, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	/// <summary>The guard: a <c>DirectoryEntry</c> nearby that does not reference the credential variable stays at CS-008 only.</summary>
	[Fact]
	public void CS019DoesNotMatchADirectoryEntryThatDoesNotUseTheCredentialVariable()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var credential = new NetworkCredential(userName, password);
			var entry = new DirectoryEntry(path);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_008, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>The word-boundary guard: a variable whose name merely STARTS WITH the credential's name is not the same identifier.</summary>
	[Fact]
	public void CS019DoesNotMatchWhenTheReferencingIdentifierOnlyStartsWithTheCredentialName()
	{
		CSharpRecognitionHarness harness = new();
		const string source = """
			var login = new NetworkCredential(userName, password);
			var entry = new DirectoryEntry(path, loginSettings.UserName, loginSettings.Password);
			""";

		RecognizedConstruct construct = Assert.Single(harness.Recognize(source));

		Assert.Equal(_CS_008, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>
	/// The required-window rule, exercised for the FtpWebRequest alternative: the cast
	/// followed by more than five unrelated lines and no completion must still let enumeration
	/// terminate, with every match actually returned carrying a successful <c>construct</c> group and
	/// a strictly advancing <c>Index</c>.
	/// </summary>
	[Fact]
	public void CS019FtpWebRequestWindowTerminatesWhenExhausted()
	{
		string source = "var request = (FtpWebRequest)WebRequest.Create(address);\n"
			+ string.Concat(Enumerable.Range(1, 11).Select(i => $"var filler{i} = {i};\n"));

		AssertBoundedTermination(source, _CS_019);
	}

	/// <summary>The same required window, exercised over a few thousand lines with no cast anywhere.</summary>
	[Fact]
	public void CS019FtpWebRequestWindowTerminatesOverThousandsOfLines()
	{
		string source = string.Concat(Enumerable.Range(1, 4000).Select(i => $"var filler{i} = {i};\n"));

		AssertBoundedTermination(source, _CS_019);
	}

	/// <summary>The anchor statement repeated many times with no completion, to prove no malformed, repeating match.</summary>
	[Fact]
	public void CS019FtpWebRequestWindowDoesNotProduceAMalformedRepeatingMatch()
	{
		string source = string.Concat(Enumerable.Range(1, 500).Select(i => $"var request{i} = (FtpWebRequest)WebRequest.Create(address);\n"));

		AssertBoundedTermination(source, _CS_019);
	}

	/// <summary>
	/// The required-window rule, exercised for the DirectoryEntry alternative: the
	/// credential construction followed by more than five unrelated lines and no completion must
	/// still let enumeration terminate.
	/// </summary>
	[Fact]
	public void CS019DirectoryEntryWindowTerminatesWhenExhausted()
	{
		string source = "var credential = new NetworkCredential(userName, password);\n"
			+ string.Concat(Enumerable.Range(1, 11).Select(i => $"var filler{i} = {i};\n"));

		AssertBoundedTermination(source, _CS_019);
	}

	/// <summary>The same required window, exercised over a few thousand lines with no credential construction anywhere.</summary>
	[Fact]
	public void CS019DirectoryEntryWindowTerminatesOverThousandsOfLines()
	{
		string source = string.Concat(Enumerable.Range(1, 4000).Select(i => $"var filler{i} = {i};\n"));

		AssertBoundedTermination(source, _CS_019);
	}

	/// <summary>The anchor statement repeated many times with no completion, to prove no malformed, repeating match.</summary>
	[Fact]
	public void CS019DirectoryEntryWindowDoesNotProduceAMalformedRepeatingMatch()
	{
		string source = string.Concat(Enumerable.Range(1, 500).Select(i => $"var credential{i} = new NetworkCredential(userName, password);\n"));

		AssertBoundedTermination(source, _CS_019);
	}

	/// <summary>
	/// The declared-unsupported text is what the client's
	/// report shows for what the tool does not detect; none of it may read as an internal
	/// engineering note. Checked against the real embedded analyzer, not a hand-built fixture,
	/// because what is under test is whether the text this build ships is actually free of the
	/// terms an internal reader - not a client - would recognize.
	/// </summary>
	[Fact]
	public void NoDeclaredUnsupportedConstructOrReasonContainsInternalTerminology()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		foreach (DeclaredUnsupportedCoverage entry in analyzer.LanguageIdentification.DeclaredUnsupportedCoverage)
		{
			foreach (string fragment in _internalTerminologyFragments)
			{
				Assert.DoesNotContain(fragment, entry.Construct, StringComparison.OrdinalIgnoreCase);
				Assert.DoesNotContain(fragment, entry.Reason, StringComparison.OrdinalIgnoreCase);
			}
		}
	}

	private static ILanguageAnalyzer ReadAnalyzer()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		return catalog.AnalyzersByLanguageName[_LANGUAGE_NAME];
	}
}
