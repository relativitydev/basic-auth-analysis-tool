using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Release;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The catalog's seventeen `VBNet` recommendation templates and its two generic fallback texts, registered against the real embedded analyzer
/// rather than a hand-built double, because what is under test is whether the analyzer this build ships answers
/// <see cref="RecommendationTemplateProvider"/> the way the catalog and <see cref="AnalyzerContractValidator"/> both require.
/// </summary>
public sealed class VBNetRecommendationTemplateTests
{
	private const string _RELATIVE_PATH_PLACEHOLDER = "{path}";
	private const string _LINE_PLACEHOLDER = "{line}";
	private const string _APPLIES_TO_LEAD_IN = "**Applies to:**";
	private const string _VBNET_FENCE = "```vbnet";
	private const string _FENCE = "```";
	private const string _NET_FRAMEWORK_RUNTIME = ".NET Framework 4.7.2";
	private const string _TEXT_FENCE = "```text";
	private const string _TOKEN_STEP_PATTERN_CLASS = "VB-006";
	private const string _NEWLINE = "\n";

	/// <summary>The five classes whose target form is prose because no single code form is correct for all of their callers.</summary>
	private static readonly ImmutableArray<string> _proseTargetFormPatternClasses = ["VB-004", "VB-005", "VB-009", "VB-011", "VB-013"];

	/// <summary>The labels a fact carries when it rests on something other than Relativity's own documentation.</summary>
	private static readonly ImmutableArray<string> _provenanceLabels =
	[
		"documented",
		OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL,
		"inferred",
		"NSerio recommendation"
	];

	[Fact]
	public void SeventeenClassesAnswerForBothFlowsAndTwoLowClassesAnswerForNeither()
	{
		ILanguageAnalyzer analyzer = VBNetRenderedTexts.Analyzer();

		foreach (CredentialFlow flow in VBNetRenderedTexts.Flows)
		{
			Assert.All(
				VBNetRenderedTexts.TemplatedPatternClasses,
				patternClass => Assert.NotNull(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
			Assert.All(
				VBNetRenderedTexts.UntemplatedPatternClasses,
				patternClass => Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
		}
	}

	[Fact]
	public void EveryTemplateIsCompleteAndTheValidatorFindsNoIncompleteTemplate()
	{
		ILanguageAnalyzer analyzer = VBNetRenderedTexts.Analyzer();

		foreach (string patternClass in VBNetRenderedTexts.TemplatedPatternClasses)
		{
			Assert.All(
				FieldsOf(TemplateOf(patternClass, CredentialFlow.ClientCredentials)),
				field => Assert.False(string.IsNullOrWhiteSpace(field)));
		}

		Assert.DoesNotContain(
			AnalyzerContractValidator.Validate(analyzer),
			violation => violation.Code == AnalyzerContractViolationCode.RecommendationTemplateIncomplete);
	}

	/// <summary>Part 1 is flow-neutral: three of a template's fields are the same text under either flow, and the provenance share leads both.</summary>
	[Fact]
	public void TheSourceFormTargetFormAndVerificationNoteAreIdenticalAcrossFlows()
	{
		foreach (string patternClass in VBNetRenderedTexts.TemplatedPatternClasses)
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
		foreach (CredentialFlow flow in VBNetRenderedTexts.Flows)
		{
			foreach (string patternClass in VBNetRenderedTexts.TemplatedPatternClasses)
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

	/// <summary>Every template's source form opens by stating the runtimes it applies to, and every code block in it is fenced as `vbnet` (or `text` for a one-line scheme change).</summary>
	[Fact]
	public void EveryTemplateStatesWhatItAppliesToFirstAndFencesItsCodeAsVbNet()
	{
		foreach (string patternClass in VBNetRenderedTexts.TemplatedPatternClasses)
		{
			RecommendationTemplate template = TemplateOf(patternClass, CredentialFlow.ClientCredentials);

			Assert.StartsWith(_APPLIES_TO_LEAD_IN, template.GenericSourceForm, StringComparison.Ordinal);
			Assert.Contains(_VBNET_FENCE, template.GenericSourceForm, StringComparison.Ordinal);
			Assert.Equal(!_proseTargetFormPatternClasses.Contains(patternClass), template.TargetFormUnderTokenAuthentication.Contains(_VBNET_FENCE, StringComparison.Ordinal));
		}

		Assert.All(VBNetRenderedTexts.AllComposedTexts(), AssertEveryFenceIsVbNetOrTextOrItsClosing);
	}

	/// <summary>Where the two runtimes differ the token acquisition carries labelled sub-blocks and not a second template.</summary>
	[Fact]
	public void WhereTheRuntimesDifferTheTokenAcquisitionCarriesLabelledSubBlocks()
	{
		string tokenAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;

		Assert.Contains("System.Text.Json", tokenAcquisition, StringComparison.Ordinal);
		Assert.Contains($"**{_NET_FRAMEWORK_RUNTIME}**", tokenAcquisition, StringComparison.Ordinal);
		Assert.Contains("System.Web.Script.Serialization", tokenAcquisition, StringComparison.Ordinal);
	}

	/// <summary>Part 2 for each flow: the token acquisition the catalog declares, appended to every class's provenance.</summary>
	[Fact]
	public void EveryProvenanceEndsWithTheTokenAcquisitionOfItsFlow()
	{
		foreach (string patternClass in VBNetRenderedTexts.TemplatedPatternClasses)
		{
			string clientCredentials = TemplateOf(patternClass, CredentialFlow.ClientCredentials).CredentialProvenance;
			string authorizationCode = TemplateOf(patternClass, CredentialFlow.AuthorizationCode).CredentialProvenance;

			Assert.Contains("grant_type=client_credentials", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("X-CSRF-Header", clientCredentials, StringComparison.Ordinal);
			Assert.Contains("grant_type=authorization_code", authorizationCode, StringComparison.Ordinal);
			Assert.Contains("HttpListener", authorizationCode, StringComparison.Ordinal);
			Assert.DoesNotContain("grant_type=client_credentials", authorizationCode, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// No client-facing text, composed or declared, leaks an internal identifier. A fact is labelled in words (documented, verified by
	/// NSerio, checked locally, inferred, recommended), as the first language's templates label theirs, and never by a research identifier.
	/// </summary>
	[Fact]
	public void NoComposedTextOrFallbackTextLeaksAnInternalIdentifier()
	{
		foreach (string text in VBNetRenderedTexts.AllComposedTexts())
		{
			Assert.Empty(InternalIdentifiersIn(text));
		}

		foreach (CredentialFlow flow in VBNetRenderedTexts.Flows)
		{
			Assert.Empty(InternalIdentifiersIn(VBNetRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]));
		}
	}

	/// <summary>
	/// The leak check still fails when a client-facing text carries an internal identifier: each sample of the public-source guard, injected into a
	/// real composed text and into a real fallback text, is reported.
	/// </summary>
	[Fact]
	public void TheLeakCheckStillReportsEveryInternalIdentifierOfTheGuard()
	{
		string composedText = VBNetRenderedTexts.AllComposedTexts().First();
		string fallbackText = VBNetRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![VBNetRenderedTexts.Flows.First()];

		Assert.Empty(InternalIdentifiersIn(composedText));
		Assert.Empty(InternalIdentifiersIn(fallbackText));
		Assert.All(PublicSourceIdentifierTests.InternalIdentifierSamples(), sample =>
		{
			Assert.NotEmpty(InternalIdentifiersIn(composedText + _NEWLINE + sample));
			Assert.NotEmpty(InternalIdentifiersIn(fallbackText + _NEWLINE + sample));
		});
	}

	/// <summary>Every token-acquisition part states where each of its facts comes from, by one of the labels, and the fallback texts do the same for the endpoints.</summary>
	[Fact]
	public void EveryFlowsTokenAcquisitionAndEveryFallbackLabelsTheProvenanceOfItsFacts()
	{
		foreach (CredentialFlow flow in VBNetRenderedTexts.Flows)
		{
			string tokenAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).CredentialProvenance;
			string fallback = VBNetRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.Contains(_provenanceLabels, label => tokenAcquisition.Contains(label, StringComparison.Ordinal));
			Assert.Contains(_provenanceLabels, label => fallback.Contains(label, StringComparison.Ordinal));
		}

		string codeAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.All(_provenanceLabels, label => Assert.Contains(label, codeAcquisition, StringComparison.Ordinal));
	}

	/// <summary>A template carries a placeholder where a tenant value or a credential belongs and never a value: the secret comes from a variable.</summary>
	[Fact]
	public void NoTemplateWritesACredentialOrATenantValueIntoItsCode()
	{
		Regex quotedCredentialAssignment = new(@"(?:client_secret|clientSecret|password)\s*=\s*['""][^'""$]+['""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		foreach (string text in VBNetRenderedTexts.AllComposedTexts())
		{
			Assert.DoesNotMatch(quotedCredentialAssignment, text);
			Assert.DoesNotContain("https://", text, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public void TheDeclaredFallbackTextIsNonBlankForBothFlowsAndValidatorClean()
	{
		ILanguageAnalyzer analyzer = VBNetRenderedTexts.Analyzer();

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
		foreach (CredentialFlow flow in VBNetRenderedTexts.Flows)
		{
			string fallback = VBNetRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.Contains(fallback, VBNetRenderedTexts.GenericRouteText(flow), StringComparison.Ordinal);
		}
	}

	private static void AssertEveryFenceIsVbNetOrTextOrItsClosing(string text)
	{
		bool inside = false;

		foreach (string line in text.Split(_NEWLINE))
		{
			if (line.TrimStart().StartsWith(_FENCE, StringComparison.Ordinal))
			{
				Assert.Equal(inside, line.Trim() == _FENCE);
				Assert.True(inside || line.Trim() == _VBNET_FENCE || line.Trim() == _TEXT_FENCE, $"A fence that is neither `vbnet` nor `text`: {line}");
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
		return VBNetRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)!;
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
