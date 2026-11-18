using System.Collections.Frozen;
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
/// The catalog's ten `Go` recommendation templates and its two generic fallback texts, registered against the real embedded analyzer rather than a
/// hand-built double, because what is under test is whether the analyzer this build ships answers <see cref="RecommendationTemplateProvider"/> the way the
/// catalog and <see cref="AnalyzerContractValidator"/> both require. The comparison of the rendered blocks with the blocks the catalog document writes is in
/// the other part of this class.
/// </summary>
public sealed partial class GoRecommendationTemplateTests
{
	private const string _RELATIVE_PATH_PLACEHOLDER = "{path}";
	private const string _LINE_PLACEHOLDER = "{line}";
	private const string _APPLIES_TO_LEAD_IN = "**Applies to:** every Go version";
	private const string _GO_FENCE = "```go";
	private const string _FENCE = "```";
	private const string _NEWLINE = "\n";
	private const string _SPACE_INDENT = " ";
	private const string _TOKEN_STEP_PATTERN_CLASS = "GO-001";
	private const string _URL_CLASS = "GO-005";
	private const string _HELD_URL_CLASS = "GO-008";
	private const string _CLIENT_FILE_NAME = "client.go";
	private const string _WEB_SERVER_VARIANT_LEAD_IN = "**Variant 1 — a Go web server.**";
	private const string _COMMAND_LINE_VARIANT_LEAD_IN = "**Variant 2 — a command-line tool or a desktop process.**";
	private const string _PROCESS_VARIANT_LEAD_IN = "**Variant: a Go process acting as itself.**";
	private const string _SERVICE_VARIANT_LEAD_IN = "**Variant: a Go web service that does not act for a signed-in user.**";
	private const string _PRECONDITION_LEAD_IN = "**Precondition: this variant is for";
	private const int _TEMPLATED_CLASS_COUNT = 10;
	private const int _UNTEMPLATED_CLASS_COUNT = 3;
	private const int _FLOW_COUNT = 2;
	private const int _LEAD_IN_SEPARATOR_LENGTH = 1;
	private const int _FINDING_SEQUENTIAL_INDEX = 1;
	private const int _FINDING_LINE = 1;

	/// <summary>The packages the design allows a block to use: the standard library only, and no module is added.</summary>
	private static readonly ImmutableArray<string> _forbiddenModuleMarkers =
	[
		"golang.org/x",
		"github.com/",
		"gopkg.in/",
		"go get",
		"go.mod",
		"oauth2.Config",
		"clientcredentials"
	];

	/// <summary>The labels a fact carries when it rests on something other than Relativity's own documentation.</summary>
	private static readonly ImmutableArray<string> _provenanceLabels =
	[
		"documented",
		OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL,
		"inferred",
		"NSerio recommendation"
	];

	/// <summary>The fixture source of the Client Credentials fallback: a High finding of the first class and a fixed signal in one file.</summary>
	private const string _CLIENT_CREDENTIALS_FIXTURE =
		"func fixtureClientCredentials(request *http.Request) {\n"
		+ "\trequest.SetBasicAuth(\"service-user\", os.Getenv(\"SERVICE_PASSWORD\"))\n"
		+ "}\n";

	/// <summary>The fixture source of the Authorization Code fallback: a High finding of the first class and an interactive signal in one file.</summary>
	private const string _AUTHORIZATION_CODE_FIXTURE =
		"func fixtureAuthorizationCode(w http.ResponseWriter, r *http.Request) {\n"
		+ "\tsecret := r.FormValue(\"password\")\n"
		+ "\trequest, err := http.NewRequest(http.MethodGet, \"https://host.example/api\", nil)\n"
		+ "\tif err != nil {\n"
		+ "\t\thttp.Error(w, \"unavailable\", http.StatusInternalServerError)\n"
		+ "\t\treturn\n"
		+ "\t}\n"
		+ "\trequest.SetBasicAuth(r.FormValue(\"username\"), secret)\n"
		+ "}\n";

	/// <summary>Ten classes answer for both flows and the three Low classes answer for neither: they are routed to manual review.</summary>
	[Fact]
	public void TenClassesAnswerForBothFlowsAndThreeLowClassesAnswerForNeither()
	{
		ILanguageAnalyzer analyzer = GoRenderedTexts.Analyzer();

		Assert.Equal(_TEMPLATED_CLASS_COUNT, GoRenderedTexts.TemplatedPatternClasses.Length);
		Assert.Equal(_UNTEMPLATED_CLASS_COUNT, GoRenderedTexts.UntemplatedPatternClasses.Length);

		foreach (CredentialFlow flow in GoRenderedTexts.Flows)
		{
			Assert.All(
				GoRenderedTexts.TemplatedPatternClasses,
				patternClass => Assert.NotNull(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
			Assert.All(
				GoRenderedTexts.UntemplatedPatternClasses,
				patternClass => Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
			Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(GoRenderedTexts.UNDECLARED_PATTERN_CLASS, flow));
		}
	}

	/// <summary>Every declared class is either templated or Low: the two lists are the analyzer's thirteen classes, and each Low class declares Low.</summary>
	[Fact]
	public void TheTwoListsAreTheAnalyzersThirteenClassesAndEveryUntemplatedClassIsLow()
	{
		ILanguageAnalyzer analyzer = GoRenderedTexts.Analyzer();
		ImmutableArray<string> listed = [.. GoRenderedTexts.TemplatedPatternClasses, .. GoRenderedTexts.UntemplatedPatternClasses];

		Assert.Equal(analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal), listed.Order(StringComparer.Ordinal));
		Assert.All(
			GoRenderedTexts.UntemplatedPatternClasses,
			patternClass => Assert.Equal(ConfidenceLevel.Low, analyzer.DetectionRulesByPatternClass[patternClass].BaseLevel));
		Assert.All(
			GoRenderedTexts.TemplatedPatternClasses,
			patternClass => Assert.NotEqual(ConfidenceLevel.Low, analyzer.DetectionRulesByPatternClass[patternClass].BaseLevel));
	}

	[Fact]
	public void EveryTemplateIsCompleteAndTheValidatorFindsNoIncompleteTemplate()
	{
		ILanguageAnalyzer analyzer = GoRenderedTexts.Analyzer();

		foreach (string patternClass in GoRenderedTexts.TemplatedPatternClasses)
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
		foreach (string patternClass in GoRenderedTexts.TemplatedPatternClasses)
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
		foreach (CredentialFlow flow in GoRenderedTexts.Flows)
		{
			foreach (string patternClass in GoRenderedTexts.TemplatedPatternClasses)
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

	/// <summary>Every template's source form opens by stating the Go versions it applies to, asks the destination question first in its target form, and fences its code as `go`.</summary>
	[Fact]
	public void EveryTemplateStatesItsGoVersionsFirstAndAsksTheDestinationFirstAndFencesItsCodeAsGo()
	{
		foreach (string patternClass in GoRenderedTexts.TemplatedPatternClasses)
		{
			RecommendationTemplate template = TemplateOf(patternClass, CredentialFlow.ClientCredentials);

			Assert.StartsWith(_APPLIES_TO_LEAD_IN, template.GenericSourceForm, StringComparison.Ordinal);
			Assert.Contains(_GO_FENCE, template.GenericSourceForm, StringComparison.Ordinal);
			Assert.StartsWith("**First, the destination:**", template.TargetFormUnderTokenAuthentication, StringComparison.Ordinal);
			Assert.Contains(_GO_FENCE, template.TargetFormUnderTokenAuthentication, StringComparison.Ordinal);
		}

		Assert.All(GoRenderedTexts.AllComposedTexts(), AssertEveryFenceIsGoOrItsClosing);
	}

	/// <summary>The indentation rule of the project: a block fenced `go` is indented with tabs only.</summary>
	[Fact]
	public void EveryLineInsideAFenceIsIndentedWithTabsOnly()
	{
		foreach (string text in GoRenderedTexts.AllComposedTexts())
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
		foreach (string patternClass in GoRenderedTexts.TemplatedPatternClasses)
		{
			string clientCredentials = TemplateOf(patternClass, CredentialFlow.ClientCredentials).CredentialProvenance;
			string authorizationCode = TemplateOf(patternClass, CredentialFlow.AuthorizationCode).CredentialProvenance;

			Assert.Contains("grant_type=client_credentials", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("\"client_credentials\"", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("Authorization: Bearer", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("X-CSRF-Header", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("\"authorization_code\"", authorizationCode, StringComparison.Ordinal);
			Assert.Contains("\"S256\"", authorizationCode, StringComparison.Ordinal);
			Assert.DoesNotContain("\"client_credentials\"", authorizationCode, StringComparison.Ordinal);
			Assert.DoesNotContain("\"authorization_code\"", clientCredentials, StringComparison.Ordinal);
		}
	}

	/// <summary>The Client Credentials text carries both variants, each with its precondition, and the Authorization Code text both of its own.</summary>
	[Fact]
	public void EachFlowCarriesItsVariantsAndEachVariantOpensWithItsPrecondition()
	{
		string clientCredentials = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;
		string authorizationCode = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.All(
			new[] { _PROCESS_VARIANT_LEAD_IN, _SERVICE_VARIANT_LEAD_IN },
			lead => Assert.StartsWith(_PRECONDITION_LEAD_IN, clientCredentials[(clientCredentials.IndexOf(lead, StringComparison.Ordinal) + lead.Length + _LEAD_IN_SEPARATOR_LENGTH)..], StringComparison.Ordinal));
		Assert.All(
			new[] { _WEB_SERVER_VARIANT_LEAD_IN, _COMMAND_LINE_VARIANT_LEAD_IN },
			lead => Assert.StartsWith(_PRECONDITION_LEAD_IN, authorizationCode[(authorizationCode.IndexOf(lead, StringComparison.Ordinal) + lead.Length + _LEAD_IN_SEPARATOR_LENGTH)..], StringComparison.Ordinal));
	}

	/// <summary>The standard library only: no block names a module, a module command or the extended OAuth package, and the blocks use the five packages the design names.</summary>
	[Fact]
	public void EveryBlockUsesTheStandardLibraryOnlyAndNamesNoModule()
	{
		foreach (string text in GoRenderedTexts.AllComposedTexts())
		{
			Assert.All(_forbiddenModuleMarkers, marker => Assert.DoesNotContain(marker, text, StringComparison.Ordinal));
		}

		string code = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.Contains("http.NewRequestWithContext", code, StringComparison.Ordinal);
		Assert.Contains("url.Values", code, StringComparison.Ordinal);
		Assert.Contains("json.NewDecoder", code, StringComparison.Ordinal);
		Assert.Contains("rand.Read", code, StringComparison.Ordinal);
		Assert.Contains("sha256.Sum256", code, StringComparison.Ordinal);
	}

	/// <summary>The web-server variant serves the redirect and the callback with `net/http`, and the command-line variant listens on the loopback address only.</summary>
	[Fact]
	public void TheWebServerVariantServesTheRedirectWithNetHttpAndTheCommandLineVariantListensOnLoopbackOnly()
	{
		string code = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;
		string webServer = code[code.IndexOf(_WEB_SERVER_VARIANT_LEAD_IN, StringComparison.Ordinal)..code.IndexOf(_COMMAND_LINE_VARIANT_LEAD_IN, StringComparison.Ordinal)];
		string commandLine = code[code.IndexOf(_COMMAND_LINE_VARIANT_LEAD_IN, StringComparison.Ordinal)..];

		Assert.Contains("http.Redirect(w, r, authorizeAddress(", webServer, StringComparison.Ordinal);
		Assert.Contains("http.SetCookie(", webServer, StringComparison.Ordinal);
		Assert.Contains("func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request)", webServer, StringComparison.Ordinal);
		Assert.Contains("net.Listen(\"tcp\", listenAddress)", commandLine, StringComparison.Ordinal);
		Assert.Contains("host != \"127.0.0.1\" && host != \"::1\"", commandLine, StringComparison.Ordinal);
		Assert.Contains("http.NotFound(w, r)", commandLine, StringComparison.Ordinal);
		Assert.Contains("subtle.ConstantTimeCompare", commandLine, StringComparison.Ordinal);
	}

	/// <summary>Every flow's token step reads the secret from the environment and never writes it, and the Client Credentials block does not send it with Basic or in the URL.</summary>
	[Fact]
	public void ClientCredentialsReadsTheSecretFromTheEnvironmentAndNeverSendsItWithBasicOrInTheUrl()
	{
		string clientCredentials = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;

		Assert.Contains("os.Getenv(clientSecretEnvironmentName)", clientCredentials, StringComparison.Ordinal);
		Assert.Contains("**Do not** send the client secret with `SetBasicAuth`", clientCredentials, StringComparison.Ordinal);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, clientCredentials, StringComparison.Ordinal);
		Assert.DoesNotContain(".SetBasicAuth(", clientCredentials[clientCredentials.IndexOf(_PROCESS_VARIANT_LEAD_IN, StringComparison.Ordinal)..], StringComparison.Ordinal);
	}

	/// <summary>
	/// THE LABEL OF THE AUTHORIZATION CODE AND PKCE FACTS. The authorize and token endpoints and the PKCE and `state` behaviour are labelled as verified by NSerio and not documented by
	/// Relativity, in the token-acquisition part of every class, in the language's own fallback text, and in the OAuth2 Client block the shared engine appends.
	/// </summary>
	[Fact]
	public void TheAuthorizationCodeAndPkceFactsCarryTheNSerioVerifiedLabelInEveryPlaceTheyAreStated()
	{
		foreach (string patternClass in GoRenderedTexts.TemplatedPatternClasses)
		{
			string code = TemplateOf(patternClass, CredentialFlow.AuthorizationCode).CredentialProvenance;
			string composed = GoRenderedTexts.FullText(patternClass, CredentialFlow.AuthorizationCode);

			Assert.Contains(OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL, code, StringComparison.Ordinal);
			Assert.Contains(OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL, composed, StringComparison.Ordinal);
		}

		string pkceParagraph = ParagraphStartingWith(TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance, "**PKCE.**");

		Assert.Contains(OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL, pkceParagraph, StringComparison.Ordinal);
		Assert.Contains(
			OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL,
			GoRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.AuthorizationCode],
			StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL, GoRenderedTexts.GenericRouteText(CredentialFlow.AuthorizationCode), StringComparison.Ordinal);
	}

	/// <summary>The two classes that expose a credential in a URL state the rotation, in the template and not only in the report's security note.</summary>
	[Fact]
	public void TheTwoUrlClassesStateTheRotationAndTheOthersDoNot()
	{
		const string rotation = "Rotate it, and assume the repository history has published it.";

		Assert.Contains(rotation, TemplateOf(_URL_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance, StringComparison.Ordinal);
		Assert.Contains(rotation, TemplateOf(_HELD_URL_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance, StringComparison.Ordinal);
		Assert.DoesNotContain(rotation, TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance, StringComparison.Ordinal);
	}

	/// <summary>
	/// No client-facing text, composed or declared, leaks an internal identifier. A fact is labelled in words (documented, verified by NSerio, inferred, recommended), as the other
	/// languages' templates label theirs, and never by a research identifier or a source citation.
	/// </summary>
	[Fact]
	public void NoComposedTextOrFallbackTextLeaksAnInternalIdentifier()
	{
		foreach (string text in GoRenderedTexts.AllComposedTexts())
		{
			Assert.Empty(InternalIdentifiersIn(text));
		}

		foreach (CredentialFlow flow in GoRenderedTexts.Flows)
		{
			Assert.Empty(InternalIdentifiersIn(GoRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]));
		}
	}

	/// <summary>The leak check still fails when a client-facing text carries an internal identifier: each sample of the public-source guard, injected into a real text, is reported.</summary>
	[Fact]
	public void TheLeakCheckStillReportsEveryInternalIdentifierOfTheGuard()
	{
		string composedText = GoRenderedTexts.AllComposedTexts().First();
		string fallbackText = GoRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![GoRenderedTexts.Flows.First()];

		Assert.Empty(InternalIdentifiersIn(composedText));
		Assert.Empty(InternalIdentifiersIn(fallbackText));
		Assert.All(PublicSourceIdentifierTests.InternalIdentifierSamples(), sample =>
		{
			Assert.NotEmpty(InternalIdentifiersIn($"{composedText}{_NEWLINE}{sample}"));
			Assert.NotEmpty(InternalIdentifiersIn($"{fallbackText}{_NEWLINE}{sample}"));
		});
	}

	/// <summary>A fact about the destination is traced to its source in the internal records and in no client-facing text: no research question and no source number is cited.</summary>
	[Fact]
	public void NoComposedTextCitesAResearchQuestionOrASourceNumber()
	{
		Regex citation = new(@"\bQ-[A-Z]+|\(\s*(?:S|T|CF|VF|DOC)[-\d]|\bCF-|\bVF-|\bDOC-\d|\bO5-\d", RegexOptions.CultureInvariant);

		Assert.All(GoRenderedTexts.AllComposedTexts(), text => Assert.DoesNotMatch(citation, text));
	}

	/// <summary>Every token-acquisition part states where each of its facts comes from, by one of the labels, and the fallback texts do the same for the endpoints.</summary>
	[Fact]
	public void EveryFlowsTokenAcquisitionAndEveryFallbackLabelsTheProvenanceOfItsFacts()
	{
		foreach (CredentialFlow flow in GoRenderedTexts.Flows)
		{
			string tokenAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).CredentialProvenance;
			string fallback = GoRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

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
		Regex quotedCredentialAssignment = new(@"(?:client_secret|clientSecret|password)\s*(?::=|=)\s*[""`][^""`$]+[""`]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		foreach (string text in GoRenderedTexts.AllComposedTexts())
		{
			Assert.DoesNotMatch(quotedCredentialAssignment, text);
		}
	}

	/// <summary>A sentence that handles a secret names the Part B pointer through the shared constant and never a link.</summary>
	[Fact]
	public void EverySecretHandlingSentenceNamesThePartBPointer()
	{
		foreach (CredentialFlow flow in GoRenderedTexts.Flows)
		{
			Assert.Contains(SecretManagementPointer.PART_B_NAME, TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).CredentialProvenance, StringComparison.Ordinal);
			Assert.Contains(SecretManagementPointer.PART_B_NAME, GoRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow], StringComparison.Ordinal);
		}

		Assert.All(GoRenderedTexts.AllComposedTexts(), text => Assert.DoesNotContain("http://secret", text, StringComparison.OrdinalIgnoreCase));
	}

	// ------------------------------------------------------------------------------------------
	// The generic fallback of each flow
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheDeclaredFallbackTextIsNonBlankForBothFlowsAndValidatorClean()
	{
		ILanguageAnalyzer analyzer = GoRenderedTexts.Analyzer();

		Assert.Equal(_FLOW_COUNT, analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Count);
		Assert.All(
			analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Values,
			text => Assert.False(string.IsNullOrWhiteSpace(text)));
		Assert.Empty(AnalyzerContractValidator.Validate(analyzer));
	}

	/// <summary>The generic route composes from the language's own fallback text and then the shared engine's OAuth2 Client block.</summary>
	[Fact]
	public void TheGenericRouteComposesTheLanguageFallbackTextOfEachFlow()
	{
		foreach (CredentialFlow flow in GoRenderedTexts.Flows)
		{
			string fallback = GoRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.Contains(fallback, GoRenderedTexts.GenericRouteText(flow), StringComparison.Ordinal);
		}

		Assert.NotEqual(
			GoRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.ClientCredentials],
			GoRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.AuthorizationCode]);
	}

	/// <summary>
	/// A fixture per flow, at the finding: a High `Go` finding of a class with no pattern-specific template renders the `Go` fallback text of its flow, not the shared
	/// engine's generic text and not another class's template.
	/// </summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void AHighFindingOfAClassWithNoTemplateRendersTheGoFallbackOfItsFlow(CredentialFlow flow)
	{
		Finding finding = ReportHarness.FindingAt(
			_FINDING_SEQUENTIAL_INDEX,
			_CLIENT_FILE_NAME,
			_FINDING_LINE,
			GoRenderedTexts.UNDECLARED_PATTERN_CLASS,
			ConfidenceLevel.High,
			language: AssessedLanguage.Go,
			flow: flow,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(GoRenderedTexts.Catalog(), new GenericRecommendationProvider());

		string composed = RecommendationComposer.ComposeArtifactText(renderer.Render(finding, EmissionDestination.StructuredData))!;
		string fallback = GoRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];
		string sharedGenericText = flow == CredentialFlow.AuthorizationCode
			? GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT
			: GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT;

		Assert.Contains(fallback, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(sharedGenericText, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(_GO_FENCE, composed, StringComparison.Ordinal);
	}

	/// <summary>
	/// THE GENERIC-FALLBACK FIXTURES OF THE DESIGN, CONSTRUCTED. Every High class has a template, so the situation is built: the source of each fixture is recognized as a High finding of the
	/// first class with the flow evidence its flow needs (a fixed signal for Client Credentials, an interactive signal for Authorization Code), and the same finding is rendered by an analyzer
	/// from which the template of that class is withheld. The output is the `Go` fallback text of the flow, composed with the OAuth2 Client block.
	/// </summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials, _CLIENT_CREDENTIALS_FIXTURE, FlowEvidence.Fixed)]
	[InlineData(CredentialFlow.AuthorizationCode, _AUTHORIZATION_CODE_FIXTURE, FlowEvidence.Interactive)]
	public void TheConstructedFixtureRendersTheFallbackWhenItsClassTemplateIsWithheld(CredentialFlow flow, string fixtureSource, FlowEvidence expectedEvidence)
	{
		RecognitionResult result = new GoRecognitionHarness().RecognizeFully(fixtureSource);
		RecognizedConstruct construct = Assert.Single(result.Constructs, candidate => candidate.PatternClassIdentifier == _TOKEN_STEP_PATTERN_CLASS);

		Assert.Equal(expectedEvidence, result.FlowEvidence);
		Assert.Equal(ConfidenceLevel.High, construct.Level);

		LanguageCatalog withheld = new(LanguageCatalog.CurrentVersion, [new TemplateWithheldGoAnalyzer(GoRenderedTexts.Analyzer(), _TOKEN_STEP_PATTERN_CLASS)]);
		Finding finding = ReportHarness.FindingAt(
			_FINDING_SEQUENTIAL_INDEX,
			_CLIENT_FILE_NAME,
			_FINDING_LINE,
			_TOKEN_STEP_PATTERN_CLASS,
			ConfidenceLevel.High,
			language: AssessedLanguage.Go,
			flow: flow,
			flowDecision: expectedEvidence == FlowEvidence.Fixed ? FlowDecision.FixedSignal : FlowDecision.InteractiveSignal);
		RecommendationRenderer renderer = new(withheld, new GenericRecommendationProvider());

		string composed = RecommendationComposer.ComposeArtifactText(renderer.Render(finding, EmissionDestination.StructuredData))!;
		string fallback = GoRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

		Assert.Contains(fallback, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(_GO_FENCE, composed, StringComparison.Ordinal);
	}

	/// <summary>A High finding of a templated class renders its own template for the flow, with the token acquisition of that flow.</summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials, "\"client_credentials\"")]
	[InlineData(CredentialFlow.AuthorizationCode, "\"authorization_code\"")]
	public void AHighFindingOfATemplatedClassRendersItsTemplateAndTheTokenAcquisitionOfItsFlow(CredentialFlow flow, string grant)
	{
		Finding finding = ReportHarness.FindingAt(
			_FINDING_SEQUENTIAL_INDEX,
			_CLIENT_FILE_NAME,
			_FINDING_LINE,
			_TOKEN_STEP_PATTERN_CLASS,
			ConfidenceLevel.High,
			language: AssessedLanguage.Go,
			flow: flow,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(GoRenderedTexts.Catalog(), new GenericRecommendationProvider());

		string composed = RecommendationComposer.ComposeArtifactText(renderer.Render(finding, EmissionDestination.StructuredData))!;

		Assert.Contains(grant, composed, StringComparison.Ordinal);
		Assert.Contains(TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).GenericSourceForm, composed, StringComparison.Ordinal);
	}

	private static void AssertEveryFenceIsGoOrItsClosing(string text)
	{
		bool inside = false;

		foreach (string line in text.Split(_NEWLINE))
		{
			if (line.TrimStart().StartsWith(_FENCE, StringComparison.Ordinal))
			{
				Assert.Equal(inside, line.Trim() == _FENCE);
				Assert.True(inside || line.Trim() == _GO_FENCE, $"A fence that is not `go`: {line}");
				inside = !inside;
			}
		}

		Assert.False(inside);
	}

	private static string ParagraphStartingWith(string text, string leadIn)
	{
		int start = text.IndexOf(leadIn, StringComparison.Ordinal);
		int end = text.IndexOf("\n\n", start, StringComparison.Ordinal);

		Assert.True(start >= 0 && end > start, $"No paragraph starts with {leadIn}");

		return text[start..end];
	}

	/// <summary>Every internal identifier a client-facing text carries, from the public-source guard's lists.</summary>
	private static ImmutableArray<string> InternalIdentifiersIn(string text)
	{
		return [.. PublicSourceIdentifierTests.InternalIdentifierPatterns().AddRange(PublicSourceIdentifierTests.InternalTemplateIdentifierPatterns()).SelectMany(pattern => pattern.Matches(text)).Select(match => match.Value)];
	}

	private static RecommendationTemplate TemplateOf(string patternClass, CredentialFlow flow)
	{
		return GoRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)!;
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

	/// <summary>The shipped `Go` analyzer with the template of one class withheld: the constructed case of the generic-fallback fixtures, which no shipped situation reaches.</summary>
	private sealed class TemplateWithheldGoAnalyzer : ILanguageAnalyzer
	{
		private readonly ILanguageAnalyzer _inner;

		internal TemplateWithheldGoAnalyzer(ILanguageAnalyzer inner, string withheldPatternClass)
		{
			_inner = inner;
			CandidateSelectionAndRecommendation = new CandidateSelectionAndRecommendation
			{
				PreselectionAnchors = inner.CandidateSelectionAndRecommendation.PreselectionAnchors,
				TemplateProvider = (patternClass, flow) => patternClass == withheldPatternClass ? null : inner.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow),
				FlowSignals = inner.CandidateSelectionAndRecommendation.FlowSignals,
				LanguageFallbackTextByFlow = inner.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow,
				FileDialects = inner.CandidateSelectionAndRecommendation.FileDialects,
				WrapperTracking = inner.CandidateSelectionAndRecommendation.WrapperTracking
			};
		}

		public LexicalProfile LexicalProfile => _inner.LexicalProfile;

		public LanguageIdentification LanguageIdentification => _inner.LanguageIdentification;

		public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass => _inner.DetectionRulesByPatternClass;

		public LanguageShapedDemotionSignals DemotionSignals => _inner.DemotionSignals;

		public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; }

		public FrozenSet<SubsumptionRule> SubsumptionRelation => _inner.SubsumptionRelation;

		public ConstructPrecedence? ConstructPrecedence => _inner.ConstructPrecedence;
	}
}
