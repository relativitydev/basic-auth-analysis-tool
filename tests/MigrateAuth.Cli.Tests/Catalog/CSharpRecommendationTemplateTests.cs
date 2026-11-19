using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The catalog's seventeen recommendation templates, registered against the real
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
	private const int _TEMPLATED_PATTERN_CLASS_COUNT = 17;
	private const string _PARAGRAPH_SEPARATOR = "\n\n";
	private const int _ONE_PER_FLOW_FORM = 1;
	private const int _BOTH_FRAMEWORK_FORMS = 2;
	private const int _THREE_REJECTIONS = 3;
	private const string _CSHARP_FENCE = "```csharp";
	private const string _TOKEN_PROVIDER_CLASS = "public sealed class AccessTokenProvider : IDisposable";
	private const string _CODE_EXCHANGE_METHOD = "public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)";
	private const string _DESKTOP_RECEIVER_METHOD = "public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)";
	private const string _DOTNET_JSON_PARSER = "JsonDocument.Parse(";
	private const string _FRAMEWORK_JSON_PARSER = "new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(";
	private const string _VERIFIER_GUARD = "if (!string.IsNullOrEmpty(request.CodeVerifier))";

	/// <summary>The seventeen classes the catalog gives a template - every High or Medium class.</summary>
	private static readonly ImmutableArray<string> _templatedPatternClasses =
	[
		"CS-001", "CS-002", "CS-003", "CS-004", "CS-005", "CS-006", "CS-007", "CS-008", "CS-009",
		"CS-012", "CS-013", "CS-014", "CS-015", "CS-016", "CS-017", "CS-018", "CS-020"
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
	private const string _CATALOG_CLIENT_CREDENTIALS_FALLBACK_TEXT = "Basic Authentication is found at this location, but the construct is not one that this text describes, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and no migration to RelativityOne applies. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API. The documented destination in C# is either the Relativity SDK's bearer-token credential type, where the code uses the SDK, or a bearer `Authorization` header, where it builds requests itself; either carries a token obtained from the platform's identity endpoint with the client-credentials grant. The client ID and secret that obtain that token belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source or tracked configuration. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The catalog's "Generic fallback content for CSharp, Authorization Code", copied verbatim.</summary>
	private const string _CATALOG_AUTHORIZATION_CODE_FALLBACK_TEXT = "Basic Authentication is found at this location, but the construct is not one that this text describes, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and no migration to RelativityOne applies. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API, and this match is assigned the Authorization Code flow, in which the application sends the user to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are verified by NSerio against a live instance; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source or tracked configuration; Code requires a client secret. After the change the application no longer collects or forwards the user's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

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

	/// <summary>All seventeen classes answer for both flows and CS-011 and CS-019 answer for neither.</summary>
	[Fact]
	public void TheAnalyzerDeclaresTheSeventeenTemplatesForBothFlowsAndNothingForTheTwoUntemplatedClasses()
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
	public void ExactlySeventeenClassesAreTemplated()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		int templatedCount = analyzer.DetectionRulesByPatternClass.Keys
			.Count(patternClassIdentifier =>
				analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, CredentialFlow.ClientCredentials) is not null);

		Assert.Equal(_TEMPLATED_PATTERN_CLASS_COUNT, templatedCount);
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
		Assert.Contains("this text does not resolve that", text, StringComparison.Ordinal);
		Assert.Contains("this guidance gives no code form", text, StringComparison.Ordinal);
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

	/// <summary>
	/// Every class carries the Client Credentials token-acquisition code, in one form for .NET and one for .NET Framework 4.7.2:
	/// the token request with the client ID and secret, the cache, the renewal before expiry and the single retry on rejection.
	/// </summary>
	[Theory]
	[MemberData(nameof(TemplatedClassesUnderBothFlows))]
	public void EveryClassCarriesTheClientCredentialsTokenCodeInBothFrameworkForms(string patternClassIdentifier)
	{
		string text = CSharpRenderedTexts.FullText(patternClassIdentifier, CredentialFlow.ClientCredentials);

		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, _TOKEN_PROVIDER_CLASS));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, _DOTNET_JSON_PARSER));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, _FRAMEWORK_JSON_PARSER));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "{ \"grant_type\", \"client_credentials\" },"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "$\"{_host}{_TOKEN_PATH}\""));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "private const string _BEARER_SCHEME = \"Bearer\";"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "private const string _TOKEN_PATH = \"/Relativity/Identity/connect/token\";"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "if (response.StatusCode == HttpStatusCode.Unauthorized)"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "response = await SendOnceAsync(createRequest, true);"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "public async Task<string> GetAccessTokenAsync(bool forceRenewal)"));
		Assert.DoesNotContain(_CODE_EXCHANGE_METHOD, text, StringComparison.Ordinal);
		Assert.DoesNotContain(_DESKTOP_RECEIVER_METHOD, text, StringComparison.Ordinal);
	}

	/// <summary>The class never carries a literal credential: the client ID, secret and scope are constructor arguments.</summary>
	[Theory]
	[MemberData(nameof(TemplatedClassesUnderBothFlows))]
	public void TheClientCredentialsTokenCodeTakesTheSecretAsAConstructorArgumentAndSendsItOnlyInTheTokenRequestForm(string patternClassIdentifier)
	{
		string text = CSharpRenderedTexts.FullText(patternClassIdentifier, CredentialFlow.ClientCredentials);

		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "_clientSecret = clientSecret;"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "{ \"client_secret\", _clientSecret },"));
		Assert.DoesNotContain("private readonly string _clientSecret =", text, StringComparison.Ordinal);
		Assert.DoesNotContain("curl", text, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Every class carries the Authorization Code exchange in both framework forms, and the loopback receiver of the desktop variant;
	/// the code verifier is added only when one was produced.
	/// </summary>
	[Theory]
	[MemberData(nameof(TemplatedClassesUnderBothFlows))]
	public void EveryClassCarriesTheAuthorizationCodeExchangeInBothFrameworkFormsAndTheDesktopReceiver(string patternClassIdentifier)
	{
		string text = CSharpRenderedTexts.FullText(patternClassIdentifier, CredentialFlow.AuthorizationCode);

		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, _CODE_EXCHANGE_METHOD));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, _DOTNET_JSON_PARSER));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, _FRAMEWORK_JSON_PARSER));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "{ \"grant_type\", \"authorization_code\" },"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "$\"{request.Host}{_TOKEN_PATH}\""));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "private const string _TOKEN_PATH = \"/Relativity/Identity/connect/token\";"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "\"/Relativity/Identity/connect/token\""));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "public sealed class AuthorizationCodeRequest"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "public string CodeVerifier { get; set; }"));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, _VERIFIER_GUARD));
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(text, "fields[\"code_verifier\"] = request.CodeVerifier;"));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, _DESKTOP_RECEIVER_METHOD));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, "listener.Prefixes.Add(redirectUrl);"));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, "The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix"));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, "it ends with a slash, as a listener prefix must."));
		Assert.DoesNotContain("the user signs in again", text, StringComparison.Ordinal);
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, "if (query[_STATE_PARAMETER] != expectedState)"));
		Assert.Equal(_THREE_REJECTIONS, CountOf(text, "throw new InvalidOperationException("));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, "throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);"));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, "throw new InvalidOperationException(_MISSING_CODE_MESSAGE);"));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, "if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))"));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, "{query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}"));
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, "if (string.IsNullOrEmpty(authorizationCode))"));
		Assert.True(text.IndexOf("if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))", StringComparison.Ordinal)
			< text.IndexOf("authorizationCode = query[_CODE_PARAMETER];", StringComparison.Ordinal));
		Assert.True(text.IndexOf("if (query[_STATE_PARAMETER] != expectedState)", StringComparison.Ordinal)
			< text.IndexOf("authorizationCode = query[_CODE_PARAMETER];", StringComparison.Ordinal));
		Assert.Contains("Send the `code_verifier` only when the authorize request carried a `code_challenge`", text, StringComparison.Ordinal);
		Assert.Contains("The exchange example above reads only the `access_token` of the response", text, StringComparison.Ordinal);
		Assert.Equal(_ONE_PER_FLOW_FORM, CountOf(text, "Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();"));
		Assert.DoesNotContain(_TOKEN_PROVIDER_CLASS, text, StringComparison.Ordinal);
	}

	/// <summary>The use-site change stays: the target form still hands the token to the SDK credential type.</summary>
	[Fact]
	public void TheUseSiteChangeOfTheSdkClassesIsKept()
	{
		foreach (CredentialFlow flow in CSharpRenderedTexts.Flows)
		{
			string text = CSharpRenderedTexts.FullText("CS-001", flow);

			Assert.Contains("new BearerTokenCredentials(accessToken)", text, StringComparison.Ordinal);
			Assert.Contains("ServiceFactorySettings settings = new ServiceFactorySettings(", text, StringComparison.Ordinal);
		}
	}

	/// <summary>The code blocks are fenced as C# and the new text is the token-acquisition share, after the class share of the provenance.</summary>
	[Fact]
	public void TheTokenCodeFollowsTheClassShareOfTheProvenance()
	{
		RecommendationTemplate template =
			CSharpRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.TemplateProvider("CS-005", CredentialFlow.ClientCredentials)!;

		Assert.True(template.CredentialProvenance.IndexOf(_TOKEN_PROVIDER_CLASS, StringComparison.Ordinal)
			> ClassShareOf(template.CredentialProvenance).Length);
		Assert.Equal(_BOTH_FRAMEWORK_FORMS, CountOf(template.CredentialProvenance, _CSHARP_FENCE));
	}

	public static TheoryData<string> TemplatedClassesUnderBothFlows()
	{
		TheoryData<string> data = [];

		foreach (string patternClassIdentifier in CSharpRenderedTexts.TemplatedPatternClasses)
		{
			data.Add(patternClassIdentifier);
		}

		return data;
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

	private static int CountOf(string text, string fragment)
	{
		int count = 0;
		int index = text.IndexOf(fragment, StringComparison.Ordinal);

		while (index >= 0)
		{
			count++;
			index = text.IndexOf(fragment, index + fragment.Length, StringComparison.Ordinal);
		}

		return count;
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
