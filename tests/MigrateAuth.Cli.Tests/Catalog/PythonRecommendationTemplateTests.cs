using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Release;
using MigrateAuth.Cli.Tests.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The catalog's thirteen `Python` recommendation templates and its two generic fallback texts, registered against the real embedded analyzer rather than a hand-built double, because what is under test is whether the
/// analyzer this build ships answers <see cref="RecommendationTemplateProvider"/> the way the catalog and <see cref="AnalyzerContractValidator"/> both require.
/// </summary>
public sealed class PythonRecommendationTemplateTests
{
	private const string _RELATIVE_PATH_PLACEHOLDER = "{path}";
	private const string _LINE_PLACEHOLDER = "{line}";
	private const string _APPLIES_TO_LEAD_IN = "**Applies to:**";
	private const string _PYTHON_FENCE = "```python";
	private const string _FENCE = "```";
	private const string _NEWLINE = "\n";
	private const string _SPACE_INDENT = " ";
	private const string _TOKEN_STEP_PATTERN_CLASS = "PY-001";
	private const string _HEADER_PATTERN_CLASS = "PY-003";
	private const int _TEMPLATED_CLASS_COUNT = 13;
	private const int _VARIANT_COUNT_OF_AUTHORIZATION_CODE = 2;
	private const int _FALLBACK_TEXT_COUNT = 2;
	private const string _PRECONDITION_LEAD_IN = "**Precondition:";
	private const string _PLAIN_FILE_NAME = "plain.py";
	private const string _CLIENT_CREDENTIALS_GRANT_FRAGMENT = "grant_type\": \"client_credentials";

	/// <summary>The label of a fact NSerio verified and Relativity does not document, in the exact words the requirement writes it.</summary>
	private const string _VERIFIED_LABEL = "verified by NSerio; not documented by Relativity";

	private const int _MINIMUM_LABELLED_STATEMENTS = 4;

	/// <summary>The classes that expose a credential in an address or on disk: their text carries the rotation statement.</summary>
	private static readonly ImmutableArray<string> _classesThatCarryTheRotationStatement = ["PY-010", "PY-011", "PY-012"];

	/// <summary>The standard-library modules and the four authorized clients a block of the templates may import.</summary>
	private static readonly ImmutableArray<string> _allowedImportedModules =
	[
		"base64", "hashlib", "http", "os", "requests", "secrets", "time", "urllib", "webbrowser"
	];

	/// <summary>The labels a fact carries when it rests on something other than Relativity's own documentation.</summary>
	private static readonly ImmutableArray<string> _provenanceLabels =
	[
		"documented",
		_VERIFIED_LABEL,
		"inferred",
		"NSerio recommendation"
	];

	/// <summary>Thirteen classes answer for both flows and the one Low class answers for neither: it is routed to manual review.</summary>
	[Fact]
	public void ThirteenClassesAnswerForBothFlowsAndTheLowClassAnswersForNeither()
	{
		ILanguageAnalyzer analyzer = PythonRenderedTexts.Analyzer();

		Assert.Equal(_TEMPLATED_CLASS_COUNT, PythonRenderedTexts.TemplatedPatternClasses.Length);
		Assert.Single(PythonRenderedTexts.UntemplatedPatternClasses);

		foreach (CredentialFlow flow in PythonRenderedTexts.Flows)
		{
			Assert.All(
				PythonRenderedTexts.TemplatedPatternClasses,
				patternClass => Assert.NotNull(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
			Assert.All(
				PythonRenderedTexts.UntemplatedPatternClasses,
				patternClass => Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
			Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(PythonRenderedTexts.UNDECLARED_PATTERN_CLASS, flow));
		}
	}

	/// <summary>Every declared class is either templated or Low: the two lists are the analyzer's fourteen classes, and the Low class declares Low.</summary>
	[Fact]
	public void TheTwoListsAreTheAnalyzersFourteenClassesAndTheUntemplatedClassIsLow()
	{
		ILanguageAnalyzer analyzer = PythonRenderedTexts.Analyzer();
		ImmutableArray<string> listed = [.. PythonRenderedTexts.TemplatedPatternClasses, .. PythonRenderedTexts.UntemplatedPatternClasses];

		Assert.Equal(analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal), listed.Order(StringComparer.Ordinal));
		Assert.All(
			PythonRenderedTexts.UntemplatedPatternClasses,
			patternClass => Assert.Equal(ConfidenceLevel.Low, analyzer.DetectionRulesByPatternClass[patternClass].BaseLevel));
		Assert.All(
			PythonRenderedTexts.TemplatedPatternClasses,
			patternClass => Assert.NotEqual(ConfidenceLevel.Low, analyzer.DetectionRulesByPatternClass[patternClass].BaseLevel));
	}

	[Fact]
	public void EveryTemplateIsCompleteAndTheValidatorFindsNoIncompleteTemplate()
	{
		ILanguageAnalyzer analyzer = PythonRenderedTexts.Analyzer();

		foreach (string patternClass in PythonRenderedTexts.TemplatedPatternClasses)
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
		foreach (string patternClass in PythonRenderedTexts.TemplatedPatternClasses)
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
		foreach (CredentialFlow flow in PythonRenderedTexts.Flows)
		{
			foreach (string patternClass in PythonRenderedTexts.TemplatedPatternClasses)
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
	/// Every template's source form opens by stating what it applies to and carries one `python` block; every target form opens with the destination question and carries one; and every fence of every composed text is
	/// a `python` fence or its closing.
	/// </summary>
	[Fact]
	public void EveryTemplateStatesWhatItAppliesToFirstAndFencesItsCodeAsPython()
	{
		foreach (string patternClass in PythonRenderedTexts.TemplatedPatternClasses)
		{
			RecommendationTemplate template = TemplateOf(patternClass, CredentialFlow.ClientCredentials);

			Assert.StartsWith(_APPLIES_TO_LEAD_IN, template.GenericSourceForm, StringComparison.Ordinal);
			Assert.Contains(_PYTHON_FENCE, template.GenericSourceForm, StringComparison.Ordinal);
			Assert.StartsWith("**First, the destination:**", template.TargetFormUnderTokenAuthentication, StringComparison.Ordinal);
			Assert.Contains(_PYTHON_FENCE, template.TargetFormUnderTokenAuthentication, StringComparison.Ordinal);
		}

		Assert.All(PythonRenderedTexts.AllComposedTexts(), AssertEveryFenceIsPythonOrItsClosing);
	}

	/// <summary>The indentation rule of the project: a block fenced `python` is indented with tabs only.</summary>
	[Fact]
	public void EveryLineInsideAFenceIsIndentedWithTabsOnly()
	{
		foreach (string text in PythonRenderedTexts.AllComposedTexts())
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
		foreach (string patternClass in PythonRenderedTexts.TemplatedPatternClasses)
		{
			string clientCredentials = TemplateOf(patternClass, CredentialFlow.ClientCredentials).CredentialProvenance;
			string authorizationCode = TemplateOf(patternClass, CredentialFlow.AuthorizationCode).CredentialProvenance;

			Assert.Contains(_CLIENT_CREDENTIALS_GRANT_FRAGMENT, clientCredentials, StringComparison.Ordinal);
			Assert.Contains("Authorization: Bearer", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("X-CSRF-Header", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("\"grant_type\": \"authorization_code\"", authorizationCode, StringComparison.Ordinal);
			Assert.Contains("code_challenge_method", authorizationCode, StringComparison.Ordinal);
			Assert.DoesNotContain(_CLIENT_CREDENTIALS_GRANT_FRAGMENT, authorizationCode, StringComparison.Ordinal);
			Assert.DoesNotContain("authorization_code", clientCredentials, StringComparison.Ordinal);
		}
	}

	/// <summary>The Client Credentials text has one variant and the Authorization Code text two, each opening with the precondition it is for.</summary>
	[Fact]
	public void TheClientCredentialsTextHasOneVariantAndTheAuthorizationCodeTextTwoEachWithItsPrecondition()
	{
		string clientCredentials = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;
		string authorizationCode = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.Single(Regex.Matches(clientCredentials, Regex.Escape(_PRECONDITION_LEAD_IN)));
		Assert.Contains("a Python process acting as itself", clientCredentials, StringComparison.Ordinal);
		Assert.Equal(_VARIANT_COUNT_OF_AUTHORIZATION_CODE, Regex.Matches(authorizationCode, Regex.Escape(_PRECONDITION_LEAD_IN)).Count);
		Assert.Contains("a server-side web application", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("a command-line or desktop tool", authorizationCode, StringComparison.Ordinal);
	}

	/// <summary>The web-server variant receives the redirect in a handler of the standard library and the command-line variant on a loopback listener bound to `127.0.0.1` on a fixed port.</summary>
	[Fact]
	public void TheWebServerVariantUsesTheStandardLibraryHandlerAndTheCommandLineVariantBindsLoopbackOnly()
	{
		string authorizationCode = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.Contains("class CallbackHandler(http.server.BaseHTTPRequestHandler)", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("LOOPBACK_HOST = \"127.0.0.1\"", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("REGISTERED_PORT = 8123", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("allow_reuse_address = False", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("secrets.compare_digest", authorizationCode, StringComparison.Ordinal);
		Assert.DoesNotContain("0.0.0.0", authorizationCode, StringComparison.Ordinal);
	}

	/// <summary>The blocks import only the standard library and the authorized clients: no framework is installed and none is imported.</summary>
	[Fact]
	public void TheBlocksImportOnlyTheStandardLibraryAndTheAuthorizedClients()
	{
		Regex import = new(@"^(?:import|from)[ \t]+([A-Za-z_][\w]*)", RegexOptions.Multiline | RegexOptions.CultureInvariant);

		foreach (string text in PythonRenderedTexts.AllComposedTexts())
		{
			Assert.All(import.Matches(text).Select(match => match.Groups[1].Value), module => Assert.Contains(module, _allowedImportedModules));
		}

		Assert.NotEmpty(import.Matches(PythonRenderedTexts.FullText(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode)));
	}

	/// <summary>Every token request keeps the secret in the process environment and never in the code.</summary>
	[Fact]
	public void ClientCredentialsReadsTheSecretFromTheEnvironmentAndNamesThePartBPointer()
	{
		string clientCredentials = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;

		Assert.Contains("os.environ[\"RELATIVITY_CLIENT_SECRET\"]", clientCredentials, StringComparison.Ordinal);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, clientCredentials, StringComparison.Ordinal);
		Assert.Contains("do not refresh", clientCredentials, StringComparison.Ordinal);
	}

	/// <summary>The classes that expose a credential in an address or in a file carry the rotation statement.</summary>
	[Fact]
	public void TheClassesThatExposeACredentialCarryTheRotationStatement()
	{
		foreach (string patternClass in _classesThatCarryTheRotationStatement)
		{
			Assert.Contains("Rotate it", PythonRenderedTexts.FullText(patternClass, CredentialFlow.ClientCredentials), StringComparison.Ordinal);
		}

		Assert.DoesNotContain("Rotate it", PythonRenderedTexts.FullText(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials), StringComparison.Ordinal);
	}

	/// <summary>
	/// No client-facing text, composed or declared, leaks an internal identifier. A fact is labelled in words (documented, verified by NSerio, inferred, recommended), as the other languages' templates label theirs, and never
	/// by a research identifier or a source citation.
	/// </summary>
	[Fact]
	public void NoComposedTextOrFallbackTextLeaksAnInternalIdentifier()
	{
		foreach (string text in PythonRenderedTexts.AllComposedTexts())
		{
			Assert.Empty(InternalIdentifiersIn(text));
		}

		foreach (CredentialFlow flow in PythonRenderedTexts.Flows)
		{
			Assert.Empty(InternalIdentifiersIn(PythonRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]));
		}
	}

	/// <summary>The leak check still fails when a client-facing text carries an internal identifier: each sample of the public-source guard, injected into a real text, is reported.</summary>
	[Fact]
	public void TheLeakCheckStillReportsEveryInternalIdentifierOfTheGuard()
	{
		string composedText = PythonRenderedTexts.AllComposedTexts().First();
		string fallbackText = PythonRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![PythonRenderedTexts.Flows.First()];

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
		Regex citation = new(@"\bQ-[A-Z]+|\(\s*(?:S|T|CF|VF|D)-?\d|\bCF-\d|\bVF-\d|\bO5-|cost:", RegexOptions.CultureInvariant);

		Assert.All(PythonRenderedTexts.AllComposedTexts(), text => Assert.DoesNotMatch(citation, text));
	}

	/// <summary>Every token-acquisition part states where each of its facts comes from, by one of the labels, and the fallback texts do the same for the endpoints.</summary>
	[Fact]
	public void EveryFlowsTokenAcquisitionAndEveryFallbackLabelsTheProvenanceOfItsFacts()
	{
		foreach (CredentialFlow flow in PythonRenderedTexts.Flows)
		{
			string tokenAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).CredentialProvenance;
			string fallback = PythonRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.Contains(_provenanceLabels, label => tokenAcquisition.Contains(label, StringComparison.Ordinal));
			Assert.Contains(_provenanceLabels, label => fallback.Contains(label, StringComparison.Ordinal));
		}

		string codeAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.All(_provenanceLabels, label => Assert.Contains(label, codeAcquisition, StringComparison.Ordinal));
	}

	/// <summary>
	/// THE LABEL OF THE ENDPOINT AND PKCE FACTS. The Authorization Code text carries the exact label on the authorize endpoint, PKCE, the client-secret requirement and the loopback redirect, and the
	/// Authorization Code fallback names the label for the endpoints and for PKCE. Every composed Authorization Code text of every class carries it too, because the token acquisition is part of each.
	/// </summary>
	[Fact]
	public void TheAuthorizationCodeFactsAboutTheEndpointsAndPkceCarryTheVerifiedLabel()
	{
		string codeAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;
		string codeFallback = PythonRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.AuthorizationCode];

		Assert.True(Regex.Count(codeAcquisition, Regex.Escape(_VERIFIED_LABEL)) >= _MINIMUM_LABELLED_STATEMENTS);
		Assert.Contains($"authorize endpoint `<host>/Relativity/Identity/connect/authorize` (***{_VERIFIED_LABEL}***", codeAcquisition, StringComparison.Ordinal);
		Assert.Contains($"works without it, ***{_VERIFIED_LABEL}***", codeAcquisition, StringComparison.Ordinal);
		Assert.Contains($"tested and rejected, ***{_VERIFIED_LABEL}***", codeAcquisition, StringComparison.Ordinal);
		Assert.Contains($"on a **fixed registered port** is accepted, ***{_VERIFIED_LABEL}***", codeAcquisition, StringComparison.Ordinal);
		Assert.Contains($"when PKCE is used (***{_VERIFIED_LABEL}***)", codeAcquisition, StringComparison.Ordinal);
		Assert.Contains($"both are {_VERIFIED_LABEL}", codeFallback, StringComparison.Ordinal);
		Assert.Contains($"which is also {_VERIFIED_LABEL}", codeFallback, StringComparison.Ordinal);

		foreach (string patternClass in PythonRenderedTexts.TemplatedPatternClasses)
		{
			Assert.Contains(_VERIFIED_LABEL, PythonRenderedTexts.FullText(patternClass, CredentialFlow.AuthorizationCode), StringComparison.Ordinal);
		}
	}

	/// <summary>The target forms of PY-003 carry the CSRF header on every line that sets the authorization header, in both flows.</summary>
	[Fact]
	public void ThePy003TargetFormWritesTheCsrfHeaderOnEveryRequestForm()
	{
		foreach (CredentialFlow flow in PythonRenderedTexts.Flows)
		{
			string target = TemplateOf(_HEADER_PATTERN_CLASS, flow).TargetFormUnderTokenAuthentication;

			Assert.Contains("\"X-CSRF-Header\": CSRF_HEADER_VALUE})", target, StringComparison.Ordinal);
			Assert.Contains("session.headers[\"X-CSRF-Header\"] = CSRF_HEADER_VALUE", target, StringComparison.Ordinal);
			Assert.Contains("request.add_header(\"X-CSRF-Header\", CSRF_HEADER_VALUE)", target, StringComparison.Ordinal);
		}
	}

	/// <summary>The `urllib3` token request sends a form-encoded body: the multipart default is refused by a token endpoint that expects a form.</summary>
	[Fact]
	public void TheUrllib3TokenRequestIsFormEncoded()
	{
		string clientCredentials = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;

		Assert.Contains("urllib3.PoolManager().request(\"POST\", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)", clientCredentials, StringComparison.Ordinal);
	}

	/// <summary>A template carries a placeholder where a tenant value or a credential belongs and never a value: the secret comes from the environment or a parameter.</summary>
	[Fact]
	public void NoTemplateWritesACredentialOrATenantValueIntoItsCode()
	{
		Regex quotedCredentialAssignment = new(@"(?:client_secret|clientSecret|password)\s*=\s*['""][^'""$]+['""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		foreach (string text in PythonRenderedTexts.AllComposedTexts())
		{
			Assert.DoesNotMatch(quotedCredentialAssignment, text);
		}
	}

	/// <summary>A sentence that handles a secret names the Part B pointer through the shared constant and never a link.</summary>
	[Fact]
	public void EverySecretHandlingSentenceNamesThePartBPointer()
	{
		foreach (CredentialFlow flow in PythonRenderedTexts.Flows)
		{
			Assert.Contains(SecretManagementPointer.PART_B_NAME, PythonRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow], StringComparison.Ordinal);
		}

		foreach (string patternClass in PythonRenderedTexts.TemplatedPatternClasses)
		{
			Assert.Contains(SecretManagementPointer.PART_B_NAME, TemplateOf(patternClass, CredentialFlow.ClientCredentials).CredentialProvenance, StringComparison.Ordinal);
		}

		Assert.All(PythonRenderedTexts.AllComposedTexts(), text => Assert.DoesNotContain("http://secret", text, StringComparison.OrdinalIgnoreCase));
	}

	// ------------------------------------------------------------------------------------------
	// The generic fallback of each flow
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheDeclaredFallbackTextIsNonBlankForBothFlowsAndValidatorClean()
	{
		ILanguageAnalyzer analyzer = PythonRenderedTexts.Analyzer();

		Assert.Equal(_FALLBACK_TEXT_COUNT, analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Count);
		Assert.All(
			analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Values,
			text => Assert.False(string.IsNullOrWhiteSpace(text)));
		Assert.Empty(AnalyzerContractValidator.Validate(analyzer));
	}

	/// <summary>The generic route composes from the language's own fallback text and then the shared engine's OAuth2 Client block.</summary>
	[Fact]
	public void TheGenericRouteComposesTheLanguageFallbackTextOfEachFlow()
	{
		foreach (CredentialFlow flow in PythonRenderedTexts.Flows)
		{
			string fallback = PythonRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.Contains(fallback, PythonRenderedTexts.GenericRouteText(flow), StringComparison.Ordinal);
		}

		Assert.NotEqual(
			PythonRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.ClientCredentials],
			PythonRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.AuthorizationCode]);
	}

	/// <summary>
	/// A fixture per flow, at the finding: a High `Python` finding of a class with no pattern-specific template renders the `Python` fallback text of its flow, not the shared engine's generic text and not another class's
	/// template.
	/// </summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void AHighFindingOfAClassWithNoTemplateRendersThePythonFallbackOfItsFlow(CredentialFlow flow)
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_PLAIN_FILE_NAME,
			1,
			PythonRenderedTexts.UNDECLARED_PATTERN_CLASS,
			ConfidenceLevel.High,
			language: AssessedLanguage.Python,
			flow: flow,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(PythonRenderedTexts.Catalog(), new GenericRecommendationProvider());

		string composed = RecommendationComposer.ComposeArtifactText(renderer.Render(finding, EmissionDestination.StructuredData))!;
		string fallback = PythonRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];
		string sharedGenericText = flow == CredentialFlow.AuthorizationCode
			? GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT
			: GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT;

		Assert.Contains(fallback, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(sharedGenericText, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(_PYTHON_FENCE, composed, StringComparison.Ordinal);
	}

	/// <summary>A High finding of a templated class renders its own template for the flow, with the token acquisition of that flow.</summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials, "\"grant_type\": \"client_credentials\"")]
	[InlineData(CredentialFlow.AuthorizationCode, "\"grant_type\": \"authorization_code\"")]
	public void AHighFindingOfATemplatedClassRendersItsTemplateAndTheTokenAcquisitionOfItsFlow(CredentialFlow flow, string grant)
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_PLAIN_FILE_NAME,
			1,
			_TOKEN_STEP_PATTERN_CLASS,
			ConfidenceLevel.High,
			language: AssessedLanguage.Python,
			flow: flow,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(PythonRenderedTexts.Catalog(), new GenericRecommendationProvider());

		string composed = RecommendationComposer.ComposeArtifactText(renderer.Render(finding, EmissionDestination.StructuredData))!;

		Assert.Contains(grant, composed, StringComparison.Ordinal);
		Assert.Contains(TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).GenericSourceForm, composed, StringComparison.Ordinal);
	}

	private static void AssertEveryFenceIsPythonOrItsClosing(string text)
	{
		bool inside = false;

		foreach (string line in text.Split(_NEWLINE))
		{
			if (line.TrimStart().StartsWith(_FENCE, StringComparison.Ordinal))
			{
				Assert.Equal(inside, line.Trim() == _FENCE);
				Assert.True(inside || line.Trim() == _PYTHON_FENCE, $"A fence that is not `python`: {line}");
				inside = !inside;
			}
		}

		Assert.False(inside);
	}

	/// <summary>Every internal identifier a client-facing text carries, from the public-source guard's lists.</summary>
	private static ImmutableArray<string> InternalIdentifiersIn(string text)
	{
		return [.. PublicSourceIdentifierTests.InternalIdentifierPatterns().AddRange(PublicSourceIdentifierTests.InternalTemplateIdentifierPatterns()).SelectMany(pattern => pattern.Matches(text)).Select(match => match.Value)];
	}

	private static RecommendationTemplate TemplateOf(string patternClass, CredentialFlow flow)
	{
		return PythonRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)!;
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
