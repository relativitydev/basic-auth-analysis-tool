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
/// The catalog's eighteen `Shell` recommendation templates and its two generic fallback texts, registered against the real embedded analyzer rather than a
/// hand-built double, because what is under test is whether the analyzer this build ships answers <see cref="RecommendationTemplateProvider"/> the way the
/// catalog and <see cref="AnalyzerContractValidator"/> both require.
/// </summary>
public sealed class ShellRecommendationTemplateTests
{
	private const string _RELATIVE_PATH_PLACEHOLDER = "{path}";
	private const string _LINE_PLACEHOLDER = "{line}";
	private const string _APPLIES_TO_LEAD_IN = "**Applies to:**";
	private const string _SH_FENCE = "```sh";
	private const string _BASH_FENCE = "```bash";
	private const string _FENCE = "```";
	private const string _NEWLINE = "\n";
	private const string _SPACE_INDENT = " ";
	private const string _TOKEN_STEP_PATTERN_CLASS = "SH-001";
	private const string _LISTENER_PARAGRAPH_LEAD_IN = "**Loopback listener variant";
	private const string _PARAGRAPH_SEPARATOR = "\n\n";
	private const string _SHELL_DIALECT_WORD = "`bash`";
	private const string _SHEBANG_LESS_FILE_NAME = "Plain.sh";

	/// <summary>The one class whose source and target forms need <c>bash</c>: POSIX <c>sh</c> has no arrays.</summary>
	private const string _ARRAY_PATTERN_CLASS = "SH-009";

	/// <summary>The labels a fact carries when it rests on something other than Relativity's own documentation.</summary>
	private static readonly ImmutableArray<string> _provenanceLabels =
	[
		"documented",
		OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL,
		"inferred",
		"NSerio recommendation"
	];

	/// <summary>Eighteen classes answer for both flows and the seven Low classes answer for neither: they are routed to manual review.</summary>
	[Fact]
	public void EighteenClassesAnswerForBothFlowsAndSevenLowClassesAnswerForNeither()
	{
		ILanguageAnalyzer analyzer = ShellRenderedTexts.Analyzer();

		Assert.Equal(18, ShellRenderedTexts.TemplatedPatternClasses.Length);
		Assert.Equal(7, ShellRenderedTexts.UntemplatedPatternClasses.Length);

		foreach (CredentialFlow flow in ShellRenderedTexts.Flows)
		{
			Assert.All(
				ShellRenderedTexts.TemplatedPatternClasses,
				patternClass => Assert.NotNull(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
			Assert.All(
				ShellRenderedTexts.UntemplatedPatternClasses,
				patternClass => Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
			Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(ShellRenderedTexts.UNDECLARED_PATTERN_CLASS, flow));
		}
	}

	/// <summary>Every declared class is either templated or Low: the two lists are the analyzer's twenty-five classes, and each Low class declares Low.</summary>
	[Fact]
	public void TheTwoListsAreTheAnalyzersTwentyFiveClassesAndEveryUntemplatedClassIsLow()
	{
		ILanguageAnalyzer analyzer = ShellRenderedTexts.Analyzer();
		ImmutableArray<string> listed = [.. ShellRenderedTexts.TemplatedPatternClasses, .. ShellRenderedTexts.UntemplatedPatternClasses];

		Assert.Equal(analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal), listed.Order(StringComparer.Ordinal));
		Assert.All(
			ShellRenderedTexts.UntemplatedPatternClasses,
			patternClass => Assert.Equal(ConfidenceLevel.Low, analyzer.DetectionRulesByPatternClass[patternClass].BaseLevel));
		Assert.All(
			ShellRenderedTexts.TemplatedPatternClasses,
			patternClass => Assert.NotEqual(ConfidenceLevel.Low, analyzer.DetectionRulesByPatternClass[patternClass].BaseLevel));
	}

	[Fact]
	public void EveryTemplateIsCompleteAndTheValidatorFindsNoIncompleteTemplate()
	{
		ILanguageAnalyzer analyzer = ShellRenderedTexts.Analyzer();

		foreach (string patternClass in ShellRenderedTexts.TemplatedPatternClasses)
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
		foreach (string patternClass in ShellRenderedTexts.TemplatedPatternClasses)
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
		foreach (CredentialFlow flow in ShellRenderedTexts.Flows)
		{
			foreach (string patternClass in ShellRenderedTexts.TemplatedPatternClasses)
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
	/// Every template's source form opens by stating the dialects it applies to (the shells it names), and every code block is fenced `sh` where it is
	/// POSIX and `bash` where it needs `bash`: the class that reads an array is the one whose forms are `bash`.
	/// </summary>
	[Fact]
	public void EveryTemplateStatesItsDialectsFirstAndFencesItsCodeAsShOrBash()
	{
		foreach (string patternClass in ShellRenderedTexts.TemplatedPatternClasses)
		{
			RecommendationTemplate template = TemplateOf(patternClass, CredentialFlow.ClientCredentials);

			Assert.StartsWith(_APPLIES_TO_LEAD_IN, template.GenericSourceForm, StringComparison.Ordinal);
			Assert.Contains(_SHELL_DIALECT_WORD, template.GenericSourceForm.Split(_NEWLINE)[0], StringComparison.Ordinal);
			Assert.True(
				template.GenericSourceForm.Contains(_SH_FENCE, StringComparison.Ordinal) || template.GenericSourceForm.Contains(_BASH_FENCE, StringComparison.Ordinal),
				patternClass);
		}

		Assert.Contains(_BASH_FENCE, TemplateOf(_ARRAY_PATTERN_CLASS, CredentialFlow.ClientCredentials).GenericSourceForm, StringComparison.Ordinal);
		Assert.DoesNotContain(_SH_FENCE, TemplateOf(_ARRAY_PATTERN_CLASS, CredentialFlow.ClientCredentials).GenericSourceForm, StringComparison.Ordinal);
		Assert.All(ShellRenderedTexts.AllComposedTexts(), AssertEveryFenceIsShBashOrItsClosing);
	}

	/// <summary>The indentation rule of the project: a block fenced `sh` is indented with tabs only (the `bash` fence is exempt from the documentation drift check, but follows the rule as well).</summary>
	[Fact]
	public void EveryLineInsideAFenceIsIndentedWithTabsOnly()
	{
		foreach (string text in ShellRenderedTexts.AllComposedTexts())
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
		foreach (string patternClass in ShellRenderedTexts.TemplatedPatternClasses)
		{
			string clientCredentials = TemplateOf(patternClass, CredentialFlow.ClientCredentials).CredentialProvenance;
			string authorizationCode = TemplateOf(patternClass, CredentialFlow.AuthorizationCode).CredentialProvenance;

			Assert.Contains("grant_type=client_credentials", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("--data-urlencode", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("Authorization: Bearer", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("X-CSRF-Header", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("grant_type=authorization_code", authorizationCode, StringComparison.Ordinal);
			Assert.Contains("code_challenge_method=S256", authorizationCode, StringComparison.Ordinal);
			Assert.DoesNotContain("grant_type=client_credentials", authorizationCode, StringComparison.Ordinal);
		}
	}

	/// <summary>The Authorization Code text carries the manual copy in full, as blocks, and the loopback listener as one paragraph with no block.</summary>
	[Fact]
	public void TheManualCopyVariantIsInFullAndTheListenerIsOneParagraph()
	{
		string code = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.Contains("IFS= read -r returned_url", code, StringComparison.Ordinal);
		Assert.Contains("returned_state", code, StringComparison.Ordinal);
		Assert.Contains("code_verifier", code, StringComparison.Ordinal);
		Assert.Single(Regex.Matches(code, Regex.Escape(_LISTENER_PARAGRAPH_LEAD_IN)));

		int start = code.IndexOf(_LISTENER_PARAGRAPH_LEAD_IN, StringComparison.Ordinal);
		int end = code.IndexOf(_PARAGRAPH_SEPARATOR, start, StringComparison.Ordinal);
		string paragraph = code[start..end];

		Assert.DoesNotContain(_FENCE, paragraph, StringComparison.Ordinal);
		Assert.Contains("`nc`", paragraph, StringComparison.Ordinal);
	}

	/// <summary>Every flow's token step puts the token in a header file and not on the command line, and the secret in a file and not on one.</summary>
	[Fact]
	public void ClientCredentialsKeepsTheSecretAndTheTokenOffTheCommandLine()
	{
		string clientCredentials = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;

		Assert.Contains("client_secret@${CLIENT_SECRET_FILE}", clientCredentials, StringComparison.Ordinal);
		Assert.Contains("-H @\"${HEADER_FILE}\"", clientCredentials, StringComparison.Ordinal);
		Assert.Contains("umask 077", clientCredentials, StringComparison.Ordinal);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, clientCredentials, StringComparison.Ordinal);
	}

	/// <summary>
	/// No client-facing text, composed or declared, leaks an internal identifier. A fact is labelled in words (documented, verified by NSerio, checked
	/// locally, inferred, recommended), as the other languages' templates label theirs, and never by a research identifier or a source citation.
	/// </summary>
	[Fact]
	public void NoComposedTextOrFallbackTextLeaksAnInternalIdentifier()
	{
		foreach (string text in ShellRenderedTexts.AllComposedTexts())
		{
			Assert.Empty(InternalIdentifiersIn(text));
		}

		foreach (CredentialFlow flow in ShellRenderedTexts.Flows)
		{
			Assert.Empty(InternalIdentifiersIn(ShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]));
		}
	}

	/// <summary>The leak check still fails when a client-facing text carries an internal identifier: each sample of the public-source guard, injected into a real text, is reported.</summary>
	[Fact]
	public void TheLeakCheckStillReportsEveryInternalIdentifierOfTheGuard()
	{
		string composedText = ShellRenderedTexts.AllComposedTexts().First();
		string fallbackText = ShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![ShellRenderedTexts.Flows.First()];

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
		Regex citation = new(@"\bQ-[A-Z]+|\(\s*(?:S|T|CF|VF)\d|\bCF-\d|\bVF-\d", RegexOptions.CultureInvariant);

		Assert.All(ShellRenderedTexts.AllComposedTexts(), text => Assert.DoesNotMatch(citation, text));
	}

	/// <summary>Every token-acquisition part states where each of its facts comes from, by one of the labels, and the fallback texts do the same for the endpoints.</summary>
	[Fact]
	public void EveryFlowsTokenAcquisitionAndEveryFallbackLabelsTheProvenanceOfItsFacts()
	{
		foreach (CredentialFlow flow in ShellRenderedTexts.Flows)
		{
			string tokenAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).CredentialProvenance;
			string fallback = ShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.Contains(_provenanceLabels, label => tokenAcquisition.Contains(label, StringComparison.Ordinal));
			Assert.Contains(_provenanceLabels, label => fallback.Contains(label, StringComparison.Ordinal));
		}

		string codeAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.All(_provenanceLabels, label => Assert.Contains(label, codeAcquisition, StringComparison.Ordinal));
	}

	/// <summary>A template carries a placeholder where a tenant value or a credential belongs and never a value: the secret comes from a file, a variable or a header file.</summary>
	[Fact]
	public void NoTemplateWritesACredentialOrATenantValueIntoItsCode()
	{
		Regex quotedCredentialAssignment = new(@"(?:client_secret|clientSecret|password)\s*=\s*['""][^'""$]+['""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		foreach (string text in ShellRenderedTexts.AllComposedTexts())
		{
			Assert.DoesNotMatch(quotedCredentialAssignment, text);
		}
	}

	/// <summary>A sentence that handles a secret names the Part B pointer through the shared constant and never a link.</summary>
	[Fact]
	public void EverySecretHandlingSentenceNamesThePartBPointer()
	{
		foreach (CredentialFlow flow in ShellRenderedTexts.Flows)
		{
			Assert.Contains(SecretManagementPointer.PART_B_NAME, TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).CredentialProvenance, StringComparison.Ordinal);
			Assert.Contains(SecretManagementPointer.PART_B_NAME, ShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow], StringComparison.Ordinal);
		}

		Assert.All(ShellRenderedTexts.AllComposedTexts(), text => Assert.DoesNotContain("http://secret", text, StringComparison.OrdinalIgnoreCase));
	}

	// ------------------------------------------------------------------------------------------
	// The generic fallback of each flow
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheDeclaredFallbackTextIsNonBlankForBothFlowsAndValidatorClean()
	{
		ILanguageAnalyzer analyzer = ShellRenderedTexts.Analyzer();

		Assert.Equal(2, analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Count);
		Assert.All(
			analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Values,
			text => Assert.False(string.IsNullOrWhiteSpace(text)));
		Assert.Empty(AnalyzerContractValidator.Validate(analyzer));
	}

	/// <summary>The generic route composes from the language's own fallback text and then the shared engine's OAuth2 Client block.</summary>
	[Fact]
	public void TheGenericRouteComposesTheLanguageFallbackTextOfEachFlow()
	{
		foreach (CredentialFlow flow in ShellRenderedTexts.Flows)
		{
			string fallback = ShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.Contains(fallback, ShellRenderedTexts.GenericRouteText(flow), StringComparison.Ordinal);
		}

		Assert.NotEqual(
			ShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.ClientCredentials],
			ShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.AuthorizationCode]);
	}

	/// <summary>
	/// A fixture per flow, at the finding: a High `Shell` finding of a class with no pattern-specific template renders the `Shell` fallback text of its flow,
	/// not the shared engine's generic text and not another class's template.
	/// </summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void AHighFindingOfAClassWithNoTemplateRendersTheShellFallbackOfItsFlow(CredentialFlow flow)
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_SHEBANG_LESS_FILE_NAME,
			1,
			ShellRenderedTexts.UNDECLARED_PATTERN_CLASS,
			ConfidenceLevel.High,
			language: AssessedLanguage.Shell,
			flow: flow,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(ShellRenderedTexts.Catalog(), new GenericRecommendationProvider());

		string composed = RecommendationComposer.ComposeArtifactText(renderer.Render(finding, EmissionDestination.StructuredData))!;
		string fallback = ShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];
		string sharedGenericText = flow == CredentialFlow.AuthorizationCode
			? GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT
			: GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT;

		Assert.Contains(fallback, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(sharedGenericText, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(_SH_FENCE, composed, StringComparison.Ordinal);
	}

	/// <summary>A High finding of a templated class renders its own template for the flow, with the token acquisition of that flow.</summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials, "grant_type=client_credentials")]
	[InlineData(CredentialFlow.AuthorizationCode, "grant_type=authorization_code")]
	public void AHighFindingOfATemplatedClassRendersItsTemplateAndTheTokenAcquisitionOfItsFlow(CredentialFlow flow, string grant)
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_SHEBANG_LESS_FILE_NAME,
			1,
			_TOKEN_STEP_PATTERN_CLASS,
			ConfidenceLevel.High,
			language: AssessedLanguage.Shell,
			flow: flow,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(ShellRenderedTexts.Catalog(), new GenericRecommendationProvider());

		string composed = RecommendationComposer.ComposeArtifactText(renderer.Render(finding, EmissionDestination.StructuredData))!;

		Assert.Contains(grant, composed, StringComparison.Ordinal);
		Assert.Contains(TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).GenericSourceForm, composed, StringComparison.Ordinal);
	}

	private static void AssertEveryFenceIsShBashOrItsClosing(string text)
	{
		bool inside = false;

		foreach (string line in text.Split(_NEWLINE))
		{
			if (line.TrimStart().StartsWith(_FENCE, StringComparison.Ordinal))
			{
				Assert.Equal(inside, line.Trim() == _FENCE);
				Assert.True(inside || line.Trim() == _SH_FENCE || line.Trim() == _BASH_FENCE, $"A fence that is neither `sh` nor `bash`: {line}");
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
		return ShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)!;
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
