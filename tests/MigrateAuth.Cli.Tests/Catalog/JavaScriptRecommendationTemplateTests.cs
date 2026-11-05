using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Release;
using MigrateAuth.Cli.Tests.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The recommendation texts the real embedded `JavaScript` analyzer composes, read through the shared engine's <see cref="RecommendationComposer"/> exactly as the
/// report does. This is <see cref="ShellRenderedTexts"/>'s shape retargeted at the `JavaScript` language, shared by the template tests and by the grant check.
/// </summary>
internal static class JavaScriptRenderedTexts
{
	/// <summary>A class identifier the `JavaScript` analyzer does not declare, so its High or Medium finding takes the generic route.</summary>
	public const string UNDECLARED_PATTERN_CLASS = "JS-404";

	private const string _LANGUAGE_NAME = "JavaScript";

	/// <summary>The twelve classes that give advice; the seven Low classes carry no template.</summary>
	public static readonly ImmutableArray<string> TemplatedPatternClasses =
	[
		"JS-001", "JS-002", "JS-003", "JS-004", "JS-005", "JS-006", "JS-008", "JS-010", "JS-012", "JS-013", "JS-015", "JS-017"
	];

	/// <summary>The seven Low classes, routed to manual review.</summary>
	public static readonly ImmutableArray<string> UntemplatedPatternClasses = ["JS-007", "JS-009", "JS-011", "JS-014", "JS-016", "JS-018", "JS-019"];

	/// <summary>Both flows, in the order the tests report them.</summary>
	public static readonly ImmutableArray<CredentialFlow> Flows =
	[
		CredentialFlow.ClientCredentials,
		CredentialFlow.AuthorizationCode
	];

	/// <summary>The embedded catalog is immutable, so it is built once and shared by every test that composes text.</summary>
	private static readonly Lazy<LanguageCatalog> _catalog = new(LanguageCatalogRegistry.BuildEmbeddedCatalog);

	public static LanguageCatalog Catalog()
	{
		return _catalog.Value;
	}

	public static ILanguageAnalyzer Analyzer()
	{
		return _catalog.Value.AnalyzersByLanguageName[_LANGUAGE_NAME];
	}

	/// <summary>The composed recommendation of one class under one flow.</summary>
	public static RenderedRecommendation Compose(string patternClassIdentifier, CredentialFlow flow)
	{
		return RecommendationComposer.Compose(_catalog.Value, AssessedLanguage.JavaScript, patternClassIdentifier, flow);
	}

	/// <summary>The whole composed text of one templated class under one flow, as the artifact carries it.</summary>
	public static string FullText(string patternClassIdentifier, CredentialFlow flow)
	{
		return RecommendationComposer.ComposeArtifactText(Compose(patternClassIdentifier, flow))!;
	}

	/// <summary>The generic route's composed text for a class the analyzer does not declare.</summary>
	public static string GenericRouteText(CredentialFlow flow)
	{
		return RecommendationComposer.ComposeArtifactText(Compose(UNDECLARED_PATTERN_CLASS, flow))!;
	}

	/// <summary>Every composed text the `JavaScript` language can produce for a High or Medium finding.</summary>
	public static ImmutableArray<string> AllComposedTexts()
	{
		ImmutableArray<string>.Builder texts = ImmutableArray.CreateBuilder<string>();

		foreach (CredentialFlow flow in Flows)
		{
			foreach (string patternClassIdentifier in TemplatedPatternClasses)
			{
				texts.Add(FullText(patternClassIdentifier, flow));
			}

			texts.Add(GenericRouteText(flow));
		}

		return texts.ToImmutable();
	}
}

/// <summary>
/// The catalog's twelve `JavaScript` recommendation templates and its two generic fallback texts, registered against the real embedded analyzer rather than a
/// hand-built double, because what is under test is whether the analyzer this build ships answers <see cref="RecommendationTemplateProvider"/> the way the
/// catalog and <see cref="AnalyzerContractValidator"/> both require.
/// </summary>
public sealed class JavaScriptRecommendationTemplateTests
{
	private const string _RELATIVE_PATH_PLACEHOLDER = "{path}";
	private const string _LINE_PLACEHOLDER = "{line}";
	private const string _APPLIES_TO_LEAD_IN = "**Applies to:**";
	private const string _JAVASCRIPT_FENCE = "```javascript";
	private const string _FENCE = "```";
	private const string _NEWLINE = "\n";
	private const string _SPACE_INDENT = " ";
	private const string _TOKEN_STEP_PATTERN_CLASS = "JS-001";
	private const string _FILE_NAME = "client.js";
	private const string _PRECONDITION_LEAD_IN = "**Precondition:";
	private const string _VARIANT_LEAD_IN = "**Variant";
	private const string _BROWSER_LABEL = "*Browser:*";
	private const string _NODE_LABEL = "*Node.js:*";
	private const string _ROTATION_SENTENCE = "Completing the migration does not close this exposure; rotation does.";
	private const string _BROWSER_ROTATION_LEAD = "If this code runs in a browser, the credential has been public for as long as the code has shipped.";
	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	private const int _TEMPLATED_CLASS_COUNT = 12;
	private const int _LOW_CLASS_COUNT = 7;
	private const int _CLIENT_CREDENTIALS_VARIANT_COUNT = 2;
	private const int _AUTHORIZATION_CODE_VARIANT_COUNT = 3;

	/// <summary>The classes whose credential is exposed whatever the migration does: the rotation statement is unconditional.</summary>
	private static readonly ImmutableArray<string> _exposureClasses = ["JS-003", "JS-010", "JS-013"];

	/// <summary>The classes that can run in a browser: they state the rotation of a credential that browser code carries.</summary>
	private static readonly ImmutableArray<string> _browserClasses = ["JS-001", "JS-002", "JS-004", "JS-005", "JS-006", "JS-012", "JS-017"];

	/// <summary>What no browser block may hold: a client secret, a token, an authorization header, the identity endpoint or the environment of a server.</summary>
	private static readonly ImmutableArray<string> _termsABrowserBlockMustNotHold =
	[
		"client_secret", "clientSecret", "CLIENT_SECRET", "RELATIVITY_CLIENT", "Bearer", "access_token", "accessToken", "Authorization", "Identity/connect", "grant_type", "process.env"
	];

	/// <summary>The labels a fact carries when it rests on something other than Relativity's own documentation.</summary>
	private static readonly ImmutableArray<string> _provenanceLabels =
	[
		"documented",
		OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL,
		"inferred",
		"NSerio recommendation"
	];

	/// <summary>Twelve classes answer for both flows and the seven Low classes answer for neither: they are routed to manual review.</summary>
	[Fact]
	public void TwelveClassesAnswerForBothFlowsAndSevenLowClassesAnswerForNeither()
	{
		ILanguageAnalyzer analyzer = JavaScriptRenderedTexts.Analyzer();

		Assert.Equal(_TEMPLATED_CLASS_COUNT, JavaScriptRenderedTexts.TemplatedPatternClasses.Length);
		Assert.Equal(_LOW_CLASS_COUNT, JavaScriptRenderedTexts.UntemplatedPatternClasses.Length);

		foreach (CredentialFlow flow in JavaScriptRenderedTexts.Flows)
		{
			Assert.All(
				JavaScriptRenderedTexts.TemplatedPatternClasses,
				patternClass => Assert.NotNull(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
			Assert.All(
				JavaScriptRenderedTexts.UntemplatedPatternClasses,
				patternClass => Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
			Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(JavaScriptRenderedTexts.UNDECLARED_PATTERN_CLASS, flow));
		}
	}

	/// <summary>Every declared class is either templated or Low: the two lists are the analyzer's nineteen classes, and each Low class declares Low.</summary>
	[Fact]
	public void TheTwoListsAreTheAnalyzersNineteenClassesAndEveryUntemplatedClassIsLow()
	{
		ILanguageAnalyzer analyzer = JavaScriptRenderedTexts.Analyzer();
		ImmutableArray<string> listed = [.. JavaScriptRenderedTexts.TemplatedPatternClasses, .. JavaScriptRenderedTexts.UntemplatedPatternClasses];

		Assert.Equal(analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal), listed.Order(StringComparer.Ordinal));
		Assert.All(
			JavaScriptRenderedTexts.UntemplatedPatternClasses,
			patternClass => Assert.Equal(ConfidenceLevel.Low, analyzer.DetectionRulesByPatternClass[patternClass].BaseLevel));
		Assert.All(
			JavaScriptRenderedTexts.TemplatedPatternClasses,
			patternClass => Assert.NotEqual(ConfidenceLevel.Low, analyzer.DetectionRulesByPatternClass[patternClass].BaseLevel));
	}

	/// <summary>A Low finding has no use-site part: it renders the manual-review route, names its class and carries no template text and no OAuth2 Client block.</summary>
	[Theory]
	[InlineData("JS-007")]
	[InlineData("JS-009")]
	[InlineData("JS-011")]
	[InlineData("JS-014")]
	[InlineData("JS-016")]
	[InlineData("JS-018")]
	[InlineData("JS-019")]
	public void ALowClassRendersManualReviewAndNoUseSite(string patternClass)
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_FILE_NAME,
			1,
			patternClass,
			ConfidenceLevel.Low,
			language: AssessedLanguage.JavaScript,
			lowReasons: LowConfidenceReasons.PatternClass);
		RecommendationRenderer renderer = new(JavaScriptRenderedTexts.Catalog(), new GenericRecommendationProvider());

		RenderedRecommendation rendered = renderer.Render(finding, EmissionDestination.StructuredData);

		Assert.Equal(RecommendationKind.ManualReview, rendered.Kind);
		Assert.Equal(RecommendationKind.ManualReview, renderer.Route(finding));
		Assert.Null(RecommendationComposer.ComposeArtifactText(rendered));
		Assert.Empty(rendered.GenericSourceForm);
		Assert.Empty(rendered.TargetFormUnderTokenAuthentication);
		Assert.Contains(patternClass, rendered.Narrative, StringComparison.Ordinal);
	}

	[Fact]
	public void EveryTemplateIsCompleteAndTheValidatorFindsNoIncompleteTemplate()
	{
		ILanguageAnalyzer analyzer = JavaScriptRenderedTexts.Analyzer();

		foreach (string patternClass in JavaScriptRenderedTexts.TemplatedPatternClasses)
		{
			Assert.All(
				FieldsOf(TemplateOf(patternClass, CredentialFlow.ClientCredentials)),
				field => Assert.False(string.IsNullOrWhiteSpace(field)));
		}

		Assert.DoesNotContain(
			AnalyzerContractValidator.Validate(analyzer),
			violation => violation.Code == AnalyzerContractViolationCode.RecommendationTemplateIncomplete);
	}

	/// <summary>Part 1 is flow-neutral: three of a template's fields are the same text under either flow, and the provenance differs by part 2.</summary>
	[Fact]
	public void TheSourceFormTargetFormAndVerificationNoteAreIdenticalAcrossFlows()
	{
		foreach (string patternClass in JavaScriptRenderedTexts.TemplatedPatternClasses)
		{
			RecommendationTemplate clientCredentials = TemplateOf(patternClass, CredentialFlow.ClientCredentials);
			RecommendationTemplate authorizationCode = TemplateOf(patternClass, CredentialFlow.AuthorizationCode);

			Assert.Equal(clientCredentials.GenericSourceForm, authorizationCode.GenericSourceForm);
			Assert.Equal(clientCredentials.TargetFormUnderTokenAuthentication, authorizationCode.TargetFormUnderTokenAuthentication);
			Assert.Equal(clientCredentials.VerificationNote, authorizationCode.VerificationNote);
			Assert.NotEqual(clientCredentials.CredentialProvenance, authorizationCode.CredentialProvenance);
		}
	}

	[Fact]
	public void NoTemplateFieldCarriesAPathOrLinePlaceholderOrLeadingOrTrailingWhitespace()
	{
		foreach (CredentialFlow flow in JavaScriptRenderedTexts.Flows)
		{
			foreach (string patternClass in JavaScriptRenderedTexts.TemplatedPatternClasses)
			{
				Assert.All(
					FieldsOf(TemplateOf(patternClass, flow)),
					field =>
					{
						Assert.DoesNotContain(_RELATIVE_PATH_PLACEHOLDER, field, StringComparison.Ordinal);
						Assert.DoesNotContain(_LINE_PLACEHOLDER, field, StringComparison.Ordinal);
						Assert.Equal(field, field.Trim());
					});
			}
		}
	}

	/// <summary>
	/// Every template's source form opens by stating the environments it applies to, every target form asks the destination first, and every code block is fenced
	/// <c>javascript</c>.
	/// </summary>
	[Fact]
	public void EveryTemplateStatesItsEnvironmentsFirstAsksTheDestinationFirstAndFencesItsCodeAsJavaScript()
	{
		foreach (string patternClass in JavaScriptRenderedTexts.TemplatedPatternClasses)
		{
			RecommendationTemplate template = TemplateOf(patternClass, CredentialFlow.ClientCredentials);

			Assert.StartsWith(_APPLIES_TO_LEAD_IN, template.GenericSourceForm, StringComparison.Ordinal);
			Assert.Contains(_JAVASCRIPT_FENCE, template.GenericSourceForm, StringComparison.Ordinal);
			Assert.StartsWith("**First, the destination:**", template.TargetFormUnderTokenAuthentication, StringComparison.Ordinal);
		}

		Assert.All(JavaScriptRenderedTexts.AllComposedTexts(), AssertEveryFenceIsJavaScriptOrItsClosing);
	}

	/// <summary>The environments of the five classes the catalog limits: Node.js only for the two that name a Node.js client or core module, browser first for the rest.</summary>
	[Theory]
	[InlineData("JS-003", "Node.js (the userinfo is sent); browser (`fetch` throws)")]
	[InlineData("JS-008", "Node.js (`http` and `https`)")]
	[InlineData("JS-015", "Node.js; `got`")]
	[InlineData("JS-006", "Node.js; a browser bundle that polyfills `Buffer`")]
	[InlineData("JS-001", "browser and Node.js; `axios` (both), `request` (Node.js)")]
	public void TheEnvironmentsOfAClassAreTheOnesTheCatalogStates(string patternClass, string environments)
	{
		string firstLine = TemplateOf(patternClass, CredentialFlow.ClientCredentials).GenericSourceForm.Split(_NEWLINE)[0];

		Assert.Equal($"{_APPLIES_TO_LEAD_IN} {environments}.", firstLine);
	}

	/// <summary>The indentation rule of the project: a block fenced `javascript` is indented with tabs only.</summary>
	[Fact]
	public void EveryLineInsideAFenceIsIndentedWithTabsOnly()
	{
		foreach (string text in JavaScriptRenderedTexts.AllComposedTexts())
		{
			bool inside = false;

			foreach (string line in text.Split(_NEWLINE))
			{
				if (line.TrimStart().StartsWith(_FENCE, StringComparison.Ordinal))
				{
					inside = !inside;
				}
				else if (inside)
				{
					Assert.False(line.StartsWith(_SPACE_INDENT, StringComparison.Ordinal), $"A space-indented line inside a fence: {line}");
				}
			}
		}
	}

	/// <summary>Part 2 for each flow: the token acquisition the catalog declares, appended to every class's provenance.</summary>
	[Fact]
	public void EveryProvenanceEndsWithTheTokenAcquisitionOfItsFlow()
	{
		foreach (string patternClass in JavaScriptRenderedTexts.TemplatedPatternClasses)
		{
			string clientCredentials = TemplateOf(patternClass, CredentialFlow.ClientCredentials).CredentialProvenance;
			string authorizationCode = TemplateOf(patternClass, CredentialFlow.AuthorizationCode).CredentialProvenance;

			Assert.Equal(PartTwoOf(TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance), PartTwoOf(clientCredentials));
			Assert.Equal(PartTwoOf(TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance), PartTwoOf(authorizationCode));
			Assert.Contains("grant_type=client_credentials", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("client_secret", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("Authorization: Bearer", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("X-CSRF-Header", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("grant_type=authorization_code", authorizationCode, StringComparison.Ordinal);
			Assert.Contains("code_challenge_method=S256", authorizationCode, StringComparison.Ordinal);
			Assert.DoesNotContain("grant_type=client_credentials", authorizationCode, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// THE VARIANTS. Application type is not known to the engine, so a finding receives every variant of its flow, labelled, each with its own precondition as the
	/// paragraph that follows its label: two for Client Credentials, three for Authorization Code.
	/// </summary>
	[Fact]
	public void EveryVariantIsLabelledAndOpensWithItsOwnPrecondition()
	{
		string clientCredentials = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;
		string authorizationCode = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.Equal(_CLIENT_CREDENTIALS_VARIANT_COUNT, VariantLabelsIn(clientCredentials).Length);
		Assert.Equal(_AUTHORIZATION_CODE_VARIANT_COUNT, VariantLabelsIn(authorizationCode).Length);
		Assert.Equal(_CLIENT_CREDENTIALS_VARIANT_COUNT, Regex.Matches(clientCredentials, Regex.Escape(_PRECONDITION_LEAD_IN)).Count);
		Assert.Equal(_AUTHORIZATION_CODE_VARIANT_COUNT, Regex.Matches(authorizationCode, Regex.Escape(_PRECONDITION_LEAD_IN)).Count);
		Assert.All(
			[clientCredentials, authorizationCode],
			text => Assert.All(
				text.Split(_PARAGRAPH_SEPARATOR).Select((paragraph, index) => (paragraph, index)).Where(entry => entry.paragraph.StartsWith(_VARIANT_LEAD_IN, StringComparison.Ordinal)),
				entry => Assert.StartsWith(_PRECONDITION_LEAD_IN, text.Split(_PARAGRAPH_SEPARATOR)[entry.index + 1], StringComparison.Ordinal)));
	}

	[Fact]
	public void TheAuthorizationCodeTextRendersTheBrowserTheNodeServerAndTheNodeCommandLineVariantsTogetherInThatOrder()
	{
		string authorizationCode = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;
		ImmutableArray<string> labels = VariantLabelsIn(authorizationCode);

		Assert.Equal("**Variant 1 — Browser with a server-side component**", labels[0]);
		Assert.Equal("**Variant 2 — Node.js server**", labels[1]);
		Assert.Equal("**Variant 3 — Node.js command line or desktop**", labels[2]);
		Assert.Contains("If your code runs in Node.js, use variant 2 or variant 3.", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("It is not for browser code (variant 1) or a command-line tool (variant 3).", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("It is not for a web server (variant 2) or browser code (variant 1).", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("node:http", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("127.0.0.1", authorizationCode, StringComparison.Ordinal);
	}

	[Fact]
	public void TheClientCredentialsTextRendersTheNodeProcessAndTheBrowserVariantsTogetherEachWithItsPrecondition()
	{
		string clientCredentials = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;
		ImmutableArray<string> labels = VariantLabelsIn(clientCredentials);

		Assert.Equal("**Variant: a Node.js process acting as itself**", labels[0]);
		Assert.Equal("**Variant: browser code that does not act for a signed-in user**", labels[1]);
		Assert.Contains("does not act for a signed-in person", clientCredentials, StringComparison.Ordinal);
		Assert.Contains("an unauthenticated relay into RelativityOne", clientCredentials, StringComparison.Ordinal);
	}

	/// <summary>Cross-origin access to RelativityOne is stated both ways and neither is verified, in the browser variant of the Authorization Code text.</summary>
	[Fact]
	public void CorsIsStatedBothWaysAndNeitherIsVerified()
	{
		string authorizationCode = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.Contains("both not verified", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("*If RelativityOne serves CORS to browser callers*", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("*If it does not*", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("The recommended architecture holds under either answer.", authorizationCode, StringComparison.Ordinal);
		Assert.DoesNotContain("verified by NSerio against a live instance; not documented by Relativity** that", authorizationCode, StringComparison.Ordinal);
	}

	/// <summary>
	/// Two Client Credentials statements that are not verified are not asserted: the secret being displayed once, and the placement of the secret in the request with
	/// the names of the response fields as the standard shape.
	/// </summary>
	[Fact]
	public void TheUnverifiedClientCredentialsStatementsAreNotAsserted()
	{
		string clientCredentials = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;

		Assert.DoesNotContain("displayed once", clientCredentials, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("not verified against RelativityOne", clientCredentials, StringComparison.Ordinal);
		Assert.DoesNotContain("standard OAuth shape", clientCredentials, StringComparison.Ordinal);
		Assert.DoesNotContain("`expires_in`", clientCredentials, StringComparison.Ordinal);
	}

	/// <summary>
	/// THE ROTATION STATEMENT of the classes that expose a credential: unconditional for a URL that carries one and for an assembled one, conditional on browser code for the
	/// classes that can run there, and stated in the browser variant of Client Credentials.
	/// </summary>
	[Fact]
	public void TheRotationStatementIsPresentForTheClassesThatExposeACredential()
	{
		foreach (CredentialFlow flow in JavaScriptRenderedTexts.Flows)
		{
			Assert.All(_exposureClasses, patternClass => Assert.Contains(_ROTATION_SENTENCE, JavaScriptRenderedTexts.FullText(patternClass, flow), StringComparison.Ordinal));
			Assert.All(
				_browserClasses,
				patternClass =>
				{
					string text = JavaScriptRenderedTexts.FullText(patternClass, flow);

					Assert.Contains(_BROWSER_ROTATION_LEAD, text, StringComparison.Ordinal);
					Assert.Contains(_ROTATION_SENTENCE, text, StringComparison.Ordinal);
				});
		}

		Assert.Contains(_ROTATION_SENTENCE, TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance, StringComparison.Ordinal);
	}

	/// <summary>
	/// NO BROWSER VARIANT PLACES A CLIENT SECRET OR A RELATIVITYONE TOKEN IN BROWSER CODE. Read over the text the report renders, for every class and both flows: each
	/// fenced block introduced by a browser label holds none of the terms of a secret, a token, an authorization header or the identity endpoint.
	/// </summary>
	[Fact]
	public void NoBrowserVariantPlacesAClientSecretOrATokenInBrowserCode()
	{
		int browserBlocks = 0;

		foreach (string text in JavaScriptRenderedTexts.AllComposedTexts())
		{
			ImmutableArray<string> blocks = BrowserBlocksIn(text);

			browserBlocks += blocks.Length;
			Assert.All(blocks, block => Assert.Empty(TermsABrowserBlockHolds(block)));
		}

		Assert.True(browserBlocks > 0, "The check read no browser block.");
		Assert.All(
			JavaScriptRenderedTexts.Flows.Select(flow => JavaScriptRenderedTexts.FullText(_TOKEN_STEP_PATTERN_CLASS, flow)),
			text => Assert.NotEmpty(BrowserBlocksIn(text)));
	}

	/// <summary>The check still fails when a browser block holds a secret or a token, and it does not read a Node.js block as a browser one.</summary>
	[Fact]
	public void TheBrowserBlockCheckStillReportsASecretOrATokenInABrowserBlock()
	{
		string leaking = "*Browser:* the call moves.\n\n```javascript\nawait fetch(route, { headers: { Authorization: `Bearer ${accessToken}` } });\n```";
		string leakingSecret = "**Variant 1 — Browser with a server-side component**\n\n```javascript\nconst clientSecret = \"x\";\n```";
		string node = "*Node.js:*\n\n```javascript\nawait fetch(route, { headers: { Authorization: `Bearer ${accessToken}` } });\n```";

		Assert.NotEmpty(BrowserBlocksIn(leaking).SelectMany(block => TermsABrowserBlockHolds(block)));
		Assert.NotEmpty(BrowserBlocksIn(leakingSecret).SelectMany(block => TermsABrowserBlockHolds(block)));
		Assert.Empty(BrowserBlocksIn(node));
	}

	/// <summary>
	/// No client-facing text, composed or declared, leaks an internal identifier. A fact is labelled in words (documented, verified by NSerio, checked
	/// locally, inferred, recommended), as the other languages' templates label theirs, and never by a research identifier or a source citation.
	/// </summary>
	[Fact]
	public void NoComposedTextOrFallbackTextLeaksAnInternalIdentifier()
	{
		foreach (string text in JavaScriptRenderedTexts.AllComposedTexts())
		{
			Assert.Empty(InternalIdentifiersIn(text));
		}

		foreach (CredentialFlow flow in JavaScriptRenderedTexts.Flows)
		{
			Assert.Empty(InternalIdentifiersIn(JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]));
		}
	}

	/// <summary>The leak check still fails when a client-facing text carries an internal identifier: each sample of the public-source guard, injected into a real text, is reported.</summary>
	[Fact]
	public void TheLeakCheckStillReportsEveryInternalIdentifierOfTheGuard()
	{
		string composedText = JavaScriptRenderedTexts.AllComposedTexts().First();
		string fallbackText = JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![JavaScriptRenderedTexts.Flows.First()];

		Assert.Empty(InternalIdentifiersIn(composedText));
		Assert.Empty(InternalIdentifiersIn(fallbackText));
		Assert.All(PublicSourceIdentifierTests.InternalIdentifierSamples(), sample =>
		{
			Assert.NotEmpty(InternalIdentifiersIn($"{composedText}{_NEWLINE}{sample}"));
			Assert.NotEmpty(InternalIdentifiersIn($"{fallbackText}{_NEWLINE}{sample}"));
		});
	}

	/// <summary>No client-facing text cites the source of a fact about the destination: no research question and no source number.</summary>
	[Fact]
	public void NoComposedTextCitesAResearchQuestionOrASourceNumber()
	{
		Regex citation = new(@"\bQ-[A-Z]+|\(\s*(?:S|T|CF|VF)\d|\bCF-\w+|\bVF-\d|\bO5-\d|\bJS-\d{3}\b|\bitem \d+\b", RegexOptions.CultureInvariant);

		Assert.All(JavaScriptRenderedTexts.AllComposedTexts(), text => Assert.DoesNotMatch(citation, text));
	}

	/// <summary>Every token-acquisition part states where each of its facts comes from, by one of the labels, and the fallback texts do the same for the endpoints.</summary>
	[Fact]
	public void EveryFlowsTokenAcquisitionAndEveryFallbackLabelsTheProvenanceOfItsFacts()
	{
		foreach (CredentialFlow flow in JavaScriptRenderedTexts.Flows)
		{
			string tokenAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).CredentialProvenance;
			string fallback = JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.Contains(_provenanceLabels, label => tokenAcquisition.Contains(label, StringComparison.Ordinal));
			Assert.Contains(_provenanceLabels, label => fallback.Contains(label, StringComparison.Ordinal));
		}

		string codeAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.All(_provenanceLabels, label => Assert.Contains(label, codeAcquisition, StringComparison.Ordinal));
	}

	/// <summary>A template carries a placeholder where a tenant value or a credential belongs and never a value: the secret comes from the environment.</summary>
	[Fact]
	public void NoTemplateWritesACredentialOrATenantValueIntoItsCode()
	{
		Regex quotedCredentialAssignment = new(@"(?:client_secret|clientSecret|CLIENT_SECRET|password)\s*[=:]\s*['""][^'""$]+['""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		foreach (string text in JavaScriptRenderedTexts.AllComposedTexts())
		{
			foreach (string block in BlocksIn(text))
			{
				if (block.Contains("client_secret", StringComparison.OrdinalIgnoreCase))
				{
					Assert.Contains("process.env.RELATIVITY_CLIENT_SECRET", block, StringComparison.Ordinal);
				}

				Assert.DoesNotMatch(quotedCredentialAssignment, block);
			}
		}
	}

	/// <summary>A sentence that handles a secret names the Part B pointer through the shared constant and never a link.</summary>
	[Fact]
	public void EverySecretHandlingSentenceNamesThePartBPointer()
	{
		foreach (CredentialFlow flow in JavaScriptRenderedTexts.Flows)
		{
			Assert.Contains(SecretManagementPointer.PART_B_NAME, TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).CredentialProvenance, StringComparison.Ordinal);
			Assert.Contains(SecretManagementPointer.PART_B_NAME, JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow], StringComparison.Ordinal);
		}

		Assert.All(JavaScriptRenderedTexts.AllComposedTexts(), text => Assert.DoesNotContain("http://secret", text, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>No template recommends a grant outside the two the tool recommends, and none puts the destination's token endpoint behind a browser.</summary>
	[Fact]
	public void NoTemplateNamesARefusedGrantInItsCode()
	{
		foreach (string text in JavaScriptRenderedTexts.AllComposedTexts())
		{
			Assert.DoesNotContain("grant_type: \"password\"", text, StringComparison.Ordinal);
			Assert.DoesNotContain("grant_type=password", text, StringComparison.Ordinal);
			Assert.DoesNotContain("device_code", text, StringComparison.OrdinalIgnoreCase);
		}
	}

	// ------------------------------------------------------------------------------------------
	// The generic fallback of each flow
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheDeclaredFallbackTextIsNonBlankForBothFlowsAndValidatorClean()
	{
		ILanguageAnalyzer analyzer = JavaScriptRenderedTexts.Analyzer();

		Assert.Equal(JavaScriptRenderedTexts.Flows.Length, analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Count);
		Assert.All(
			analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Values,
			text => Assert.False(string.IsNullOrWhiteSpace(text)));
		Assert.Empty(AnalyzerContractValidator.Validate(analyzer));
	}

	/// <summary>The generic route composes from the language's own fallback text and then the shared engine's OAuth2 Client block.</summary>
	[Fact]
	public void TheGenericRouteComposesTheLanguageFallbackTextOfEachFlow()
	{
		foreach (CredentialFlow flow in JavaScriptRenderedTexts.Flows)
		{
			string fallback = JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.Contains(fallback, JavaScriptRenderedTexts.GenericRouteText(flow), StringComparison.Ordinal);
		}

		Assert.NotEqual(
			JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.ClientCredentials],
			JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.AuthorizationCode]);
	}

	/// <summary>The fallback is a pointer and not an instruction: no code block, no before-and-after form, and no secret in browser code.</summary>
	[Fact]
	public void TheFallbackTextsOfBothFlowsAreAPointerWithNoCodeAndNoBrowserSecret()
	{
		foreach (CredentialFlow flow in JavaScriptRenderedTexts.Flows)
		{
			string fallback = JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.DoesNotContain(_FENCE, fallback, StringComparison.Ordinal);
			Assert.Contains("did not recognize the construct", fallback, StringComparison.Ordinal);
			Assert.Contains("a pointer and not an instruction", fallback, StringComparison.Ordinal);
			Assert.Contains("a secret shipped to a browser is public", JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.ClientCredentials], StringComparison.Ordinal);
		}

		Assert.Contains("never in browser code", JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.AuthorizationCode], StringComparison.Ordinal);
	}

	/// <summary>
	/// A fixture per flow, at the finding: a High `JavaScript` finding of a class with no pattern-specific template renders the `JavaScript` fallback text of its flow,
	/// not the shared engine's generic text and not another class's template.
	/// </summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void AHighFindingOfAClassWithNoTemplateRendersTheJavaScriptFallbackOfItsFlow(CredentialFlow flow)
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_FILE_NAME,
			1,
			JavaScriptRenderedTexts.UNDECLARED_PATTERN_CLASS,
			ConfidenceLevel.High,
			language: AssessedLanguage.JavaScript,
			flow: flow,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(JavaScriptRenderedTexts.Catalog(), new GenericRecommendationProvider());

		string composed = RecommendationComposer.ComposeArtifactText(renderer.Render(finding, EmissionDestination.StructuredData))!;
		string fallback = JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];
		string sharedGenericText = flow == CredentialFlow.AuthorizationCode
			? GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT
			: GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT;

		Assert.Equal(RecommendationKind.Generic, renderer.Route(finding));
		Assert.Contains(fallback, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(sharedGenericText, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(_JAVASCRIPT_FENCE, composed, StringComparison.Ordinal);
	}

	/// <summary>A High finding of a templated class renders its own template for the flow, with the token acquisition of that flow.</summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials, "grant_type=client_credentials")]
	[InlineData(CredentialFlow.AuthorizationCode, "grant_type=authorization_code")]
	public void AHighFindingOfATemplatedClassRendersItsTemplateAndTheTokenAcquisitionOfItsFlow(CredentialFlow flow, string grant)
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_FILE_NAME,
			1,
			_TOKEN_STEP_PATTERN_CLASS,
			ConfidenceLevel.High,
			language: AssessedLanguage.JavaScript,
			flow: flow,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(JavaScriptRenderedTexts.Catalog(), new GenericRecommendationProvider());

		string composed = RecommendationComposer.ComposeArtifactText(renderer.Render(finding, EmissionDestination.StructuredData))!;

		Assert.Contains(grant, composed, StringComparison.Ordinal);
		Assert.Contains(TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).GenericSourceForm, composed, StringComparison.Ordinal);
	}

	// ------------------------------------------------------------------------------------------
	// The literal credential, true and false
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// THE LITERAL CREDENTIAL, TRUE. A string literal in a credential position, a template literal with no hole, a URL literal with a password in its userinfo and a literal token
	/// after the scheme word all read as literal; and the same text with a hole in single or double quotes reads as literal too, because inside quotes <c>${...}</c> is text.
	/// </summary>
	[Theory]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'exampleSecret' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: \"exampleSecret\" } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: `exampleSecret` } });", "JS-001")]
	[InlineData("http.get(url, { auth: 'exampleUser:exampleSecret' }, handler);", "JS-008")]
	[InlineData("http.get(url, { auth: `exampleUser:exampleSecret` }, handler);", "JS-008")]
	[InlineData("const baseUrl = 'https://exampleUser:exampleSecret@host.example/path';", "JS-013")]
	[InlineData("fetch(url, { headers: { Authorization: 'Basic ZXhhbXBsZVVzZXI6ZXhhbXBsZVNlY3JldA==' } });", "JS-004")]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:exampleSecret');", "JS-005")]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'user:${pass}' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: \"user:${pass}\" } });", "JS-001")]
	[InlineData("http.get(url, { auth: 'user:${pass}' }, handler);", "JS-008")]
	[InlineData("http.get(url, { auth: \"user:${pass}\" }, handler);", "JS-008")]
	public void ALiteralCredentialReadsAsLiteral(string source, string patternClass)
	{
		RecognizedConstruct construct = ConstructOf(source, patternClass);

		Assert.True(construct.LiteralCredentialSignal, source);
	}

	/// <summary>
	/// THE LITERAL CREDENTIAL, FALSE. A template with a hole is code in its hole, so it does not read as literal; a placeholder form of the vocabulary does not either; nor does a
	/// variable, a read of the environment or a concatenation of a literal with code.
	/// </summary>
	[Theory]
	[InlineData("axios.get(url, { auth: { username: userName, password: `user:${pass}` } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: `${secret}` } });", "JS-001")]
	[InlineData("http.get(url, { auth: `user:${pass}` }, handler);", "JS-008")]
	[InlineData("http.get(url, { auth: `${userName}:${secret}` }, handler);", "JS-008")]
	[InlineData("axios.get(url, { auth: { username: userName, password: secret } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: process.env.ENTRY } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'changeme' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: '********' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'xxxxxx' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: '<password>' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: '[PASSWORD]' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: '{{secret}}' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: '%SECRET%' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'REDACTED' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'your-password' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'TODO' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: '' } });", "JS-001")]
	[InlineData("http.get(url, { auth: 'exampleUser:changeme' }, handler);", "JS-008")]
	[InlineData("http.get(url, { auth: 'exampleUser:********' }, handler);", "JS-008")]
	[InlineData("const baseUrl = 'https://exampleUser:changeme@host.example/path';", "JS-013")]
	[InlineData("const header = 'Basic ' + btoa(`exampleUser:${secret}`);", "JS-005")]
	public void ANonLiteralCredentialDoesNotReadAsLiteral(string source, string patternClass)
	{
		RecognizedConstruct construct = ConstructOf(source, patternClass);

		Assert.False(construct.LiteralCredentialSignal, source);
	}

	/// <summary>
	/// A BACKTICK TEMPLATE WITH A HOLE reads the literal-credential signal as false; THE SAME TEXT IN SINGLE OR DOUBLE QUOTES reads it as true, because in quotes the
	/// <c>${...}</c> is literal text and not a hole.
	/// </summary>
	[Fact]
	public void TheSameTextWithAHoleIsNotLiteralInBackticksAndLiteralInSingleAndDoubleQuotes()
	{
		RecognizedConstruct backtick = ConstructOf("axios.get(url, { auth: { username: userName, password: `user:${pass}` } });", "JS-001");
		RecognizedConstruct single = ConstructOf("axios.get(url, { auth: { username: userName, password: 'user:${pass}' } });", "JS-001");
		RecognizedConstruct doubled = ConstructOf("axios.get(url, { auth: { username: userName, password: \"user:${pass}\" } });", "JS-001");
		RecognizedConstruct backtickPair = ConstructOf("http.get(url, { auth: `user:${pass}` }, handler);", "JS-008");
		RecognizedConstruct singlePair = ConstructOf("http.get(url, { auth: 'user:${pass}' }, handler);", "JS-008");
		RecognizedConstruct doublePair = ConstructOf("http.get(url, { auth: \"user:${pass}\" }, handler);", "JS-008");

		Assert.False(backtick.LiteralCredentialSignal);
		Assert.True(single.LiteralCredentialSignal);
		Assert.True(doubled.LiteralCredentialSignal);
		Assert.False(backtickPair.LiteralCredentialSignal);
		Assert.True(singlePair.LiteralCredentialSignal);
		Assert.True(doublePair.LiteralCredentialSignal);
	}

	/// <summary>The finding is unchanged by the literal rule: a placeholder, a hole and a literal each still yield the same class at the same level, only the signal differs.</summary>
	[Fact]
	public void TheLiteralRuleChangesTheSignalAndNeverTheFinding()
	{
		RecognizedConstruct literal = ConstructOf("axios.get(url, { auth: { username: userName, password: 'exampleSecret' } });", "JS-001");
		RecognizedConstruct placeholder = ConstructOf("axios.get(url, { auth: { username: userName, password: 'changeme' } });", "JS-001");
		RecognizedConstruct withAHole = ConstructOf("axios.get(url, { auth: { username: userName, password: `${secret}` } });", "JS-001");

		Assert.Equal(ConfidenceLevel.High, literal.Level);
		Assert.Equal(literal.Level, placeholder.Level);
		Assert.Equal(literal.Level, withAHole.Level);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	/// <summary>Part 2 of a provenance: what follows the class's own one-paragraph share, which is the same text for every class of a flow.</summary>
	private static string PartTwoOf(string provenance)
	{
		return provenance[(provenance.IndexOf(_PARAGRAPH_SEPARATOR, StringComparison.Ordinal) + _PARAGRAPH_SEPARATOR.Length)..];
	}

	private static RecognizedConstruct ConstructOf(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize($"{source}{_NEWLINE}");

		return Assert.Single(constructs, construct => construct.PatternClassIdentifier == patternClass);
	}

	private static void AssertEveryFenceIsJavaScriptOrItsClosing(string text)
	{
		bool inside = false;

		foreach (string line in text.Split(_NEWLINE))
		{
			if (line.TrimStart().StartsWith(_FENCE, StringComparison.Ordinal))
			{
				Assert.Equal(inside, line.Trim() == _FENCE);
				Assert.True(inside || line.Trim() == _JAVASCRIPT_FENCE, $"A fence that is not `javascript`: {line}");
				inside = !inside;
			}
		}

		Assert.False(inside);
	}

	/// <summary>The bold variant labels of a token-acquisition part, in order, each the whole of its paragraph.</summary>
	private static ImmutableArray<string> VariantLabelsIn(string text)
	{
		return [.. text.Split(_PARAGRAPH_SEPARATOR).Where(paragraph => paragraph.StartsWith(_VARIANT_LEAD_IN, StringComparison.Ordinal) && !paragraph.Contains(_NEWLINE, StringComparison.Ordinal))];
	}

	/// <summary>
	/// The fenced blocks the text introduces as browser code: the block that follows the browser label of a use site, or a variant whose label names a browser. A block with
	/// another label, or none, is not a browser block.
	/// </summary>
	private static ImmutableArray<string> BrowserBlocksIn(string text)
	{
		ImmutableArray<string>.Builder blocks = ImmutableArray.CreateBuilder<string>();
		bool browserLabel = false;
		bool inside = false;
		System.Text.StringBuilder current = new();

		foreach (string line in text.Split(_NEWLINE))
		{
			if (line.TrimStart().StartsWith(_FENCE, StringComparison.Ordinal))
			{
				if (inside && browserLabel)
				{
					blocks.Add(current.ToString());
				}

				current.Clear();
				inside = !inside;
			}
			else if (inside)
			{
				current.AppendLine(line);
			}
			else if (line.Contains(_BROWSER_LABEL, StringComparison.Ordinal))
			{
				browserLabel = true;
			}
			else if (line.Contains(_NODE_LABEL, StringComparison.Ordinal))
			{
				browserLabel = false;
			}
			else if (line.StartsWith(_VARIANT_LEAD_IN, StringComparison.Ordinal))
			{
				browserLabel = line.Contains("browser", StringComparison.OrdinalIgnoreCase);
			}
		}

		return blocks.ToImmutable();
	}

	/// <summary>Every fenced block of a text, in order.</summary>
	private static ImmutableArray<string> BlocksIn(string text)
	{
		ImmutableArray<string>.Builder blocks = ImmutableArray.CreateBuilder<string>();
		bool inside = false;
		System.Text.StringBuilder current = new();

		foreach (string line in text.Split(_NEWLINE))
		{
			if (line.TrimStart().StartsWith(_FENCE, StringComparison.Ordinal))
			{
				if (inside)
				{
					blocks.Add(current.ToString());
				}

				current.Clear();
				inside = !inside;
			}
			else if (inside)
			{
				current.AppendLine(line);
			}
		}

		return blocks.ToImmutable();
	}

	private static ImmutableArray<string> TermsABrowserBlockHolds(string block)
	{
		return [.. _termsABrowserBlockMustNotHold.Where(term => block.Contains(term, StringComparison.Ordinal))];
	}

	/// <summary>Every internal identifier a client-facing text carries, from the public-source guard's lists.</summary>
	private static ImmutableArray<string> InternalIdentifiersIn(string text)
	{
		return [.. PublicSourceIdentifierTests.InternalIdentifierPatterns().AddRange(PublicSourceIdentifierTests.InternalTemplateIdentifierPatterns()).SelectMany(pattern => pattern.Matches(text)).Select(match => match.Value)];
	}

	private static RecommendationTemplate TemplateOf(string patternClass, CredentialFlow flow)
	{
		return JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)!;
	}

	private static ImmutableArray<string> FieldsOf(RecommendationTemplate template)
	{
		return
		[
			template.GenericSourceForm,
			template.TargetFormUnderTokenAuthentication,
			template.CredentialProvenance,
			template.VerificationNote
		];
	}
}
