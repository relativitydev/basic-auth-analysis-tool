using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The catalog's sixteen recommendation templates, registered against the real
/// embedded `CSharp` analyzer rather than a hand-built double, because what is under test is
/// whether the analyzer this build ships actually answers <see cref="RecommendationTemplateProvider"/>
/// the way the catalog and <see cref="AnalyzerContractValidator"/> both require - not whether some
/// analyzer could.
/// </summary>
public sealed class CSharpRecommendationTemplateTests
{
	private const string _LANGUAGE_NAME = "CSharp";
	private const string _RELATIVE_PATH_PLACEHOLDER = "{path}";
	private const string _LINE_PLACEHOLDER = "{line}";
	private const string _IMPORT_API_PATTERN_CLASS = "CS-015";
	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	/// <summary>The sixteen classes the catalog gives a template - every High or Medium class.</summary>
	private static readonly ImmutableArray<string> _templatedPatternClasses =
	[
		"CS-001", "CS-002", "CS-003", "CS-004", "CS-005", "CS-006", "CS-007", "CS-008", "CS-009",
		"CS-012", "CS-013", "CS-014", "CS-015", "CS-016", "CS-017", "CS-018"
	];

	/// <summary>The two classes the catalog's "Classes without a template" names.</summary>
	private static readonly ImmutableArray<string> _untemplatedPatternClasses = ["CS-011", "CS-019"];

	/// <summary>
	/// The catalog's "Generic fallback content for CSharp", copied from the catalog
	/// for comparison, with the one encoding choice applied: the secret-handling sentence names
	/// <c>Secret Management (Part B of the Migration Documentation)</c>, as the section's own labels rule
	/// requires. <c>CSharpRecommendationTemplates</c> is `internal` to the CLI assembly with no
	/// `InternalsVisibleTo` grant, so this file reads back the one member of the contract that IS
	/// public: <see cref="CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow"/> on the real
	/// embedded analyzer.
	/// </summary>
	private const string _CATALOG_CLIENT_CREDENTIALS_FALLBACK_TEXT = "The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API. The documented destination in C# is either the Relativity SDK's bearer-token credential type, where the code uses the SDK, or a bearer `Authorization` header, where it builds requests itself; either carries a token obtained from the platform's identity endpoint with the client-credentials grant. The client ID and secret that obtain that token belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source or tracked configuration. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The catalog's "Generic fallback content for CSharp, Authorization Code", copied verbatim.</summary>
	private const string _CATALOG_AUTHORIZATION_CODE_FALLBACK_TEXT = "The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API, and this finding is assigned the Authorization Code flow, in which the application sends the user to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are verified by NSerio against a live instance; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source or tracked configuration; Code requires a client secret. After the change the application no longer collects or forwards the user's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The analyzer declares each flow's generic text from the catalog.</summary>
	[Fact]
	public void TheAnalyzerDeclaresCatalogSectionSixsFallbackTextForBothFlows()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.Equal(
			_CATALOG_CLIENT_CREDENTIALS_FALLBACK_TEXT,
			analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.ClientCredentials]);
		Assert.Equal(
			_CATALOG_AUTHORIZATION_CODE_FALLBACK_TEXT,
			analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.AuthorizationCode]);
	}

	/// <summary>All sixteen classes answer for both flows and CS-011 and CS-019 answer for neither.</summary>
	[Fact]
	public void TheAnalyzerDeclaresTheSixteenTemplatesForBothFlowsAndNothingForTheTwoUntemplatedClasses()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		foreach (CredentialFlow flow in Enum.GetValues<CredentialFlow>())
		{
			foreach (string patternClassIdentifier in _templatedPatternClasses)
			{
				Assert.NotNull(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, flow));
			}

			foreach (string patternClassIdentifier in _untemplatedPatternClasses)
			{
				Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, flow));
			}
		}
	}

	/// <summary>Part 1 is flow-neutral: three of a template's fields are the same text under either flow.</summary>
	[Fact]
	public void TheSourceFormTargetFormAndVerificationNoteAreIdenticalAcrossFlows()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		foreach (string patternClassIdentifier in _templatedPatternClasses)
		{
			RecommendationTemplate clientCredentials =
				analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, CredentialFlow.ClientCredentials)!;
			RecommendationTemplate authorizationCode =
				analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, CredentialFlow.AuthorizationCode)!;

			Assert.Equal(clientCredentials.GenericSourceForm, authorizationCode.GenericSourceForm);
			Assert.Equal(clientCredentials.TargetFormUnderTokenAuthentication, authorizationCode.TargetFormUnderTokenAuthentication);
			Assert.Equal(clientCredentials.VerificationNote, authorizationCode.VerificationNote);
			Assert.NotEqual(clientCredentials.CredentialProvenance, authorizationCode.CredentialProvenance);
			Assert.StartsWith(ClassShareOf(clientCredentials.CredentialProvenance), authorizationCode.CredentialProvenance, StringComparison.Ordinal);
		}
	}

	/// <summary>Declared and non-blank - the state <see cref="AnalyzerContractValidator"/> requires.</summary>
	[Fact]
	public void TheDeclaredLanguageFallbackTextIsNonBlankAndValidatorClean()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.Equal(2, analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Count);
		Assert.All(
			analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Values,
			text => Assert.False(string.IsNullOrWhiteSpace(text)));

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(analyzer);

		Assert.DoesNotContain(
			violations,
			violation => violation.Code == AnalyzerContractViolationCode.LanguageFallbackTextBlank);
	}

	[Fact]
	public void EveryHighOrMediumClassResolvesToACompleteTemplate()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		foreach (string patternClassIdentifier in _templatedPatternClasses)
		{
			RecommendationTemplate? template =
				analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, CredentialFlow.ClientCredentials);

			Assert.NotNull(template);
			AssertComplete(template);
		}
	}

	[Fact]
	public void ExactlySixteenClassesAreTemplated()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		int templatedCount = analyzer.DetectionRulesByPatternClass.Keys
			.Count(patternClassIdentifier =>
				analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, CredentialFlow.ClientCredentials) is not null);

		Assert.Equal(16, templatedCount);
	}

	[Theory]
	[MemberData(nameof(UntemplatedPatternClasses))]
	public void CS011AndCS019CarryNoTemplate(string patternClassIdentifier)
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		RecommendationTemplate? template =
			analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, CredentialFlow.ClientCredentials);

		Assert.Null(template);
	}

	/// <summary>
	/// The validator assertion, distinct from
	/// <c>TheCSharpAnalyzerFitsTheContractWithNoViolations</c>: named specifically for a template
	/// missing one of its four required fields, rather than only for "no violations of any kind".
	/// </summary>
	[Fact]
	public void TheValidatorFindsNoIncompleteRecommendationTemplate()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(analyzer);

		Assert.Empty(violations);
		Assert.DoesNotContain(
			violations,
			violation => violation.Code == AnalyzerContractViolationCode.RecommendationTemplateIncomplete);
	}

	[Fact]
	public void AnUnknownPatternClassIdentifierResolvesToNull()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		RecommendationTemplate? template =
			analyzer.CandidateSelectionAndRecommendation.TemplateProvider("CS-404", CredentialFlow.ClientCredentials);

		Assert.Null(template);
	}

	/// <summary>
	/// The encoding rule permits only `{path}` and `{line}` as substitution tokens, and neither
	/// is intended anywhere in the catalog - every template speaks in the generic terms the
	/// catalog requires. A stray occurrence of either token would be silently "substituted" by
	/// <see cref="Reporting.RecommendationRenderer"/> rather than reported, so this is checked here
	/// instead.
	/// </summary>
	[Fact]
	public void NoTemplateFieldContainsAPathOrLinePlaceholder()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		foreach (CredentialFlow flow in Enum.GetValues<CredentialFlow>())
		{
			foreach (string patternClassIdentifier in _templatedPatternClasses)
			{
				RecommendationTemplate template =
					analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, flow)!;

				foreach (string field in FieldsOf(template))
				{
					Assert.DoesNotContain(_RELATIVE_PATH_PLACEHOLDER, field, StringComparison.Ordinal);
					Assert.DoesNotContain(_LINE_PLACEHOLDER, field, StringComparison.Ordinal);
				}
			}
		}
	}

	/// <summary>The normalization rule strips leading and trailing blank content from every field.</summary>
	[Fact]
	public void NoTemplateFieldHasLeadingOrTrailingWhitespace()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		foreach (CredentialFlow flow in Enum.GetValues<CredentialFlow>())
		{
			foreach (string patternClassIdentifier in _templatedPatternClasses)
			{
				RecommendationTemplate template =
					analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, flow)!;

				foreach (string field in FieldsOf(template))
				{
					Assert.Equal(field, field.Trim());
				}
			}
		}
	}

	/// <summary>The ImportAPI template names the one documented factory, says it takes no token, and records the known issue.</summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void TheImportApiTemplateNamesTheFactoryAndRecordsTheKnownIssue(CredentialFlow flow)
	{
		string text = CSharpRenderedTexts.FullText(_IMPORT_API_PATTERN_CLASS, flow);

		Assert.Contains("ImportAPI.CreateByRsaBearerToken(webServiceUrl)", text, StringComparison.Ordinal);
		Assert.Contains("takes the web service URL and no token", text, StringComparison.Ordinal);
		Assert.Contains("Relativity Service Account", text, StringComparison.Ordinal);
		Assert.Contains("a known issue of this release, listed in its release notes", text, StringComparison.Ordinal);
		Assert.Contains("this template gives no code form", text, StringComparison.Ordinal);
		Assert.Contains("RelativityOne documentation does not describe this class", text, StringComparison.Ordinal);
	}

	/// <summary>The ImportAPI template never tells the reader to carry the access token into the factory.</summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void TheImportApiTemplateDoesNotTellTheReaderToCarryTheTokenIntoTheFactory(CredentialFlow flow)
	{
		string text = CSharpRenderedTexts.FullText(_IMPORT_API_PATTERN_CLASS, flow);

		Assert.DoesNotContain("carrying the access token", text, StringComparison.Ordinal);
		Assert.DoesNotContain("a static factory taking a bearer token", text, StringComparison.Ordinal);
		Assert.DoesNotContain("token-taking factory", text, StringComparison.Ordinal);
	}

	public static TheoryData<string> UntemplatedPatternClasses()
	{
		TheoryData<string> data = [];

		foreach (string patternClassIdentifier in _untemplatedPatternClasses)
		{
			data.Add(patternClassIdentifier);
		}

		return data;
	}

	private static string ClassShareOf(string clientCredentialsProvenance)
	{
		int shareEnd = clientCredentialsProvenance.IndexOf(_PARAGRAPH_SEPARATOR, StringComparison.Ordinal);

		return clientCredentialsProvenance[..shareEnd];
	}

	private static void AssertComplete(RecommendationTemplate? template)
	{
		Assert.False(string.IsNullOrWhiteSpace(template!.GenericSourceForm));
		Assert.False(string.IsNullOrWhiteSpace(template.TargetFormUnderTokenAuthentication));
		Assert.False(string.IsNullOrWhiteSpace(template.CredentialProvenance));
		Assert.False(string.IsNullOrWhiteSpace(template.VerificationNote));
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

	private static ILanguageAnalyzer ReadAnalyzer()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		return catalog.AnalyzersByLanguageName[_LANGUAGE_NAME];
	}
}
