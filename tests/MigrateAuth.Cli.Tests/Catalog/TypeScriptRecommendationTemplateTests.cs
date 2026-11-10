using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Documentation;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Release;
using MigrateAuth.Cli.Tests.Reporting;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The recommendation texts the real embedded `TypeScript` analyzer composes, read through the shared engine's <see cref="RecommendationComposer"/> exactly as the report does.
/// This is <see cref="JavaScriptRenderedTexts"/>'s shape retargeted at the `TypeScript` language, shared by the template tests and by the grant check.
/// </summary>
internal static class TypeScriptRenderedTexts
{
	/// <summary>A class identifier the `TypeScript` analyzer does not declare, so its High or Medium finding takes the generic route.</summary>
	public const string UNDECLARED_PATTERN_CLASS = "TS-404";

	private const string _LANGUAGE_NAME = "TypeScript";

	/// <summary>The fourteen classes that give advice: the twelve inherited and the two of `TypeScript`.</summary>
	public static readonly ImmutableArray<string> TemplatedPatternClasses =
	[
		"JS-001", "JS-002", "JS-003", "JS-004", "JS-005", "JS-006", "JS-008", "JS-010", "JS-012", "JS-013", "JS-015", "JS-017", "TS-001", "TS-003"
	];

	/// <summary>The seven inherited Low classes, routed to manual review.</summary>
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
		return RecommendationComposer.Compose(_catalog.Value, AssessedLanguage.TypeScript, patternClassIdentifier, flow);
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

	/// <summary>Every composed text the `TypeScript` language can produce for a High or Medium finding.</summary>
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
/// THE INTERNAL MEMBERS THE `TypeScript` TEMPLATES PUBLISH FOR THEIR OWN TESTS, read from the shipped assembly by name. The assembly grants its tests no access to its internals, so each member is
/// reached by reflection and the lookup fails loudly when a member is renamed or removed: a test never silently passes because a hook went missing. The constants of the `JavaScript` templates are read
/// the same way, never copied.
/// </summary>
internal static class TypeScriptTemplateHooks
{
	private const string _LANGUAGES_NAMESPACE = "MigrateAuth.Cli.Catalog.Languages";
	private const string _TYPESCRIPT_TEMPLATES = "TypeScriptRecommendationTemplates";
	private const string _JAVASCRIPT_TEMPLATES = "JavaScriptRecommendationTemplates";
	private const string _SUBSTITUTION_TYPE = "TemplateSubstitution";
	private const BindingFlags _MEMBERS = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

	internal static string ClientCredentialsTokenAcquisition => (string)ReadProperty(_TYPESCRIPT_TEMPLATES, "ClientCredentialsTokenAcquisition");

	internal static string AuthorizationCodeTokenAcquisition => (string)ReadProperty(_TYPESCRIPT_TEMPLATES, "AuthorizationCodeTokenAcquisition");

	internal static string TypingSentence => TypeScript("TYPING_SENTENCE");

	internal static string DestinationQuestion => TypeScript("DESTINATION_QUESTION");

	internal static string BrowserRotation => TypeScript("BROWSER_ROTATION");

	internal static string RotationStatement => TypeScript("ROTATION_STATEMENT");

	internal static string TokenLifetime => TypeScript("TOKEN_LIFETIME");

	/// <summary>A constant of the `TypeScript` templates.</summary>
	internal static string TypeScript(string name)
	{
		return ReadConstant(_TYPESCRIPT_TEMPLATES, name);
	}

	/// <summary>A constant of the `JavaScript` templates, as it stands.</summary>
	internal static string JavaScript(string name)
	{
		return ReadConstant(_JAVASCRIPT_TEMPLATES, name);
	}

	/// <summary>The edits that make a flow's `JavaScript` token acquisition a `TypeScript` text: name, the text found and the text that replaces it.</summary>
	internal static ImmutableArray<(string Name, string Find, string Replacement)> Substitutions(CredentialFlow flow)
	{
		string property = flow == CredentialFlow.AuthorizationCode ? "AuthorizationCodeSubstitutions" : "ClientCredentialsSubstitutions";
		System.Collections.IEnumerable edits = (System.Collections.IEnumerable)ReadProperty(_TYPESCRIPT_TEMPLATES, property);
		ImmutableArray<(string Name, string Find, string Replacement)>.Builder read = ImmutableArray.CreateBuilder<(string Name, string Find, string Replacement)>();

		foreach (object edit in edits)
		{
			Type type = edit.GetType();

			read.Add(((string)Member(type, "Name", edit), (string)Member(type, "Find", edit), (string)Member(type, "Replacement", edit)));
		}

		return read.ToImmutable();
	}

	/// <summary>Applies one edit with the code that applies them, so that the refusal of an edit that does not find its text exactly once can be read.</summary>
	internal static string Substitute(string text, string name, string find, string replacement)
	{
		Type templates = TemplatesType(_TYPESCRIPT_TEMPLATES);
		Type edit = TemplatesType(_SUBSTITUTION_TYPE);
		object created = Activator.CreateInstance(edit, name, find, replacement)!;
		Array one = Array.CreateInstance(edit, 1);

		one.SetValue(created, 0);

		MethodInfo create = typeof(ImmutableArray).GetMethods()
			.Single(method => method.Name == nameof(ImmutableArray.Create) && method.IsGenericMethodDefinition && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.IsArray);
		object edits = create.MakeGenericMethod(edit).Invoke(null, [one])!;
		MethodInfo substitute = templates.GetMethod("Substitute", _MEMBERS) ?? throw new MissingMemberException(templates.FullName, "Substitute");

		try
		{
			return (string)substitute.Invoke(null, [text, edits])!;
		}
		catch (TargetInvocationException failure) when (failure.InnerException is not null)
		{
			ExceptionDispatchInfo.Capture(failure.InnerException).Throw();

			throw;
		}
	}

	private static Type TemplatesType(string typeName)
	{
		return typeof(EmbeddedLanguageAnalyzers).Assembly.GetType($"{_LANGUAGES_NAMESPACE}.{typeName}", throwOnError: true)!;
	}

	private static object ReadProperty(string typeName, string propertyName)
	{
		Type type = TemplatesType(typeName);
		PropertyInfo property = type.GetProperty(propertyName, _MEMBERS) ?? throw new MissingMemberException(type.FullName, propertyName);

		return property.GetValue(null) ?? throw new InvalidOperationException($"{typeName}.{propertyName} returned nothing.");
	}

	private static string ReadConstant(string typeName, string fieldName)
	{
		Type type = TemplatesType(typeName);
		FieldInfo field = type.GetField(fieldName, _MEMBERS) ?? throw new MissingMemberException(type.FullName, fieldName);

		return (string)(field.GetRawConstantValue() ?? throw new InvalidOperationException($"{typeName}.{fieldName} holds nothing."));
	}

	private static object Member(Type type, string name, object instance)
	{
		PropertyInfo property = type.GetProperty(name) ?? throw new MissingMemberException(type.FullName, name);

		return property.GetValue(instance) ?? throw new InvalidOperationException($"{type.Name}.{name} returned nothing.");
	}
}

/// <summary>
/// The catalog's fourteen `TypeScript` recommendation templates and its two generic fallback texts, registered against the real embedded analyzer rather than a hand-built
/// double, because what is under test is whether the analyzer this build ships answers <see cref="RecommendationTemplateProvider"/> the way the catalog and
/// <see cref="AnalyzerContractValidator"/> both require. The token acquisition of each flow is the `JavaScript` text with the `typescript` fence and the typed blocks substituted
/// in, each substitution occurring exactly once, and its blocks equal those the catalog writes.
/// </summary>
public sealed partial class TypeScriptRecommendationTemplateTests
{
	private const string _RELATIVE_PATH_PLACEHOLDER = "{path}";
	private const string _LINE_PLACEHOLDER = "{line}";
	private const string _APPLIES_TO_LEAD_IN = "**Applies to:**";
	private const string _TYPESCRIPT_FENCE = "```typescript";
	private const string _JAVASCRIPT_FENCE = "```javascript";
	private const string _FENCE = "```";
	private const string _NEWLINE = "\n";
	private const string _SPACE_INDENT = " ";
	private const string _TAB = "\t";
	private const string _TOKEN_STEP_PATTERN_CLASS = "JS-001";
	private const string _FILE_NAME = "client.ts";
	private const string _INTERACTIVE_FILE_NAME = "Interactive.ts";
	private const string _FIXED_FILE_NAME = "Fixed.ts";
	private const string _PLAIN_FILE_NAME = "Plain.ts";
	private const string _HEADER_PATTERN_CLASS = "JS-004";
	private const string _PRECONDITION_LEAD_IN = "**Precondition:";
	private const string _VARIANT_LEAD_IN = "**Variant";
	private const string _BROWSER_LABEL = "*Browser:*";
	private const string _ANGULAR_LABEL = "*Angular `HttpClient`, browser:*";
	private const string _NODE_LABEL = "*Node.js:*";
	private const string _ROTATION_SENTENCE = "Completing the migration does not close this exposure; rotation does.";
	private const string _BROWSER_ROTATION_LEAD = "If this code runs in a browser, the credential has been public for as long as the code has shipped.";
	private const string _PARAGRAPH_SEPARATOR = "\n\n";
	private const string _ANGULAR_SHELL_MARKER = "The Angular shell.";
	private const string _NEST_SHELL_MARKER = "A NestJS controller that calls the two functions is a shell.";

	private const int _TEMPLATED_CLASS_COUNT = 14;
	private const int _LOW_CLASS_COUNT = 7;
	private const int _CLIENT_CREDENTIALS_VARIANT_COUNT = 2;
	private const int _AUTHORIZATION_CODE_VARIANT_COUNT = 3;
	private const int _CLIENT_CREDENTIALS_BLOCK_COUNT = 1;
	private const int _AUTHORIZATION_CODE_BLOCK_COUNT = 5;

	/// <summary>The classes whose credential is exposed whatever the migration does: the rotation statement is unconditional.</summary>
	private static readonly ImmutableArray<string> _exposureClasses = ["JS-003", "JS-010", "JS-013"];

	/// <summary>The classes that can run in a browser: they state the rotation of a credential that browser code carries.</summary>
	private static readonly ImmutableArray<string> _browserClasses = ["JS-001", "JS-002", "JS-004", "JS-005", "JS-006", "JS-012", "JS-017", "TS-001"];

	/// <summary>What no browser block may hold: a client secret, a token, an authorization header, the identity endpoint or the environment of a server.</summary>
	private static readonly ImmutableArray<string> _termsABrowserBlockMustNotHold =
	[
		"client_secret", "clientSecret", "CLIENT_SECRET", "RELATIVITY_CLIENT", "Bearer", "access_token", "accessToken", "Authorization", "Identity/connect", "grant_type", "process.env", "refresh_token"
	];

	/// <summary>The labels a fact carries when it rests on something other than Relativity's own documentation.</summary>
	private static readonly ImmutableArray<string> _provenanceLabels =
	[
		"documented",
		OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL,
		"inferred",
		"NSerio recommendation"
	];

	// ------------------------------------------------------------------------------------------
	// Which classes answer
	// ------------------------------------------------------------------------------------------

	/// <summary>Fourteen classes answer for both flows and the seven Low classes answer for neither: they are routed to manual review.</summary>
	[Fact]
	public void FourteenClassesAnswerForBothFlowsAndSevenLowClassesAnswerForNeither()
	{
		ILanguageAnalyzer analyzer = TypeScriptRenderedTexts.Analyzer();

		Assert.Equal(_TEMPLATED_CLASS_COUNT, TypeScriptRenderedTexts.TemplatedPatternClasses.Length);
		Assert.Equal(_LOW_CLASS_COUNT, TypeScriptRenderedTexts.UntemplatedPatternClasses.Length);

		foreach (CredentialFlow flow in TypeScriptRenderedTexts.Flows)
		{
			Assert.All(
				TypeScriptRenderedTexts.TemplatedPatternClasses,
				patternClass => Assert.NotNull(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
			Assert.All(
				TypeScriptRenderedTexts.UntemplatedPatternClasses,
				patternClass => Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)));
			Assert.Null(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(TypeScriptRenderedTexts.UNDECLARED_PATTERN_CLASS, flow));
		}
	}

	/// <summary>Every declared class is either templated or Low: the two lists are the analyzer's twenty-one classes, each Low class declares Low and the two classes of `TypeScript` do not.</summary>
	[Fact]
	public void TheTwoListsAreTheAnalyzersTwentyOneClassesAndEveryUntemplatedClassIsLow()
	{
		ILanguageAnalyzer analyzer = TypeScriptRenderedTexts.Analyzer();
		ImmutableArray<string> listed = [.. TypeScriptRenderedTexts.TemplatedPatternClasses, .. TypeScriptRenderedTexts.UntemplatedPatternClasses];

		Assert.Equal(analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal), listed.Order(StringComparer.Ordinal));
		Assert.All(
			TypeScriptRenderedTexts.UntemplatedPatternClasses,
			patternClass => Assert.Equal(ConfidenceLevel.Low, analyzer.DetectionRulesByPatternClass[patternClass].BaseLevel));
		Assert.All(
			TypeScriptRenderedTexts.TemplatedPatternClasses,
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
			language: AssessedLanguage.TypeScript,
			lowReasons: LowConfidenceReasons.PatternClass);
		RecommendationRenderer renderer = new(TypeScriptRenderedTexts.Catalog(), new GenericRecommendationProvider());

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
		ILanguageAnalyzer analyzer = TypeScriptRenderedTexts.Analyzer();

		foreach (string patternClass in TypeScriptRenderedTexts.TemplatedPatternClasses)
		{
			Assert.All(
				FieldsOf(TemplateOf(patternClass, CredentialFlow.ClientCredentials)),
				field => Assert.False(string.IsNullOrWhiteSpace(field)));
		}

		Assert.DoesNotContain(
			AnalyzerContractValidator.Validate(analyzer),
			violation => violation.Code == AnalyzerContractViolationCode.RecommendationTemplateIncomplete);
		Assert.Empty(AnalyzerContractValidator.Validate(analyzer));
	}

	/// <summary>Part 1 is flow-neutral: three of a template's fields are the same text under either flow, and the provenance differs by part 2.</summary>
	[Fact]
	public void TheSourceFormTargetFormAndVerificationNoteAreIdenticalAcrossFlows()
	{
		foreach (string patternClass in TypeScriptRenderedTexts.TemplatedPatternClasses)
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
		foreach (CredentialFlow flow in TypeScriptRenderedTexts.Flows)
		{
			foreach (string patternClass in TypeScriptRenderedTexts.TemplatedPatternClasses)
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
	/// Every template's source form opens by stating the environments it applies to, every target form asks the destination first, every inherited class renders the `JavaScript` entry
	/// and every code block is fenced <c>typescript</c>, none <c>javascript</c>.
	/// </summary>
	[Fact]
	public void EveryTemplateStatesItsEnvironmentsFirstAsksTheDestinationFirstAndFencesItsCodeAsTypeScript()
	{
		foreach (string patternClass in TypeScriptRenderedTexts.TemplatedPatternClasses)
		{
			RecommendationTemplate template = TemplateOf(patternClass, CredentialFlow.ClientCredentials);

			Assert.StartsWith(_APPLIES_TO_LEAD_IN, template.GenericSourceForm, StringComparison.Ordinal);
			Assert.Contains(_TYPESCRIPT_FENCE, template.GenericSourceForm, StringComparison.Ordinal);
			Assert.StartsWith("**First, the destination:**", template.TargetFormUnderTokenAuthentication, StringComparison.Ordinal);
		}

		Assert.All(TypeScriptRenderedTexts.AllComposedTexts(), AssertEveryFenceIsTypeScriptOrItsClosing);
	}

	/// <summary>An inherited class renders the `JavaScript` entry with the `typescript` fence and nothing else changed in its use-site fields.</summary>
	[Fact]
	public void AnInheritedClassRendersTheJavaScriptEntryWithTheTypeScriptFence()
	{
		foreach (string patternClass in TypeScriptRenderedTexts.TemplatedPatternClasses.Where(candidate => candidate.StartsWith("JS-", StringComparison.Ordinal) && candidate != _HEADER_PATTERN_CLASS))
		{
			RecommendationTemplate inherited = JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.TemplateProvider(patternClass, CredentialFlow.ClientCredentials)!;
			RecommendationTemplate rendered = TemplateOf(patternClass, CredentialFlow.ClientCredentials);

			Assert.Equal(inherited.GenericSourceForm.Replace(_JAVASCRIPT_FENCE, _TYPESCRIPT_FENCE, StringComparison.Ordinal), rendered.GenericSourceForm);
			Assert.Equal(inherited.TargetFormUnderTokenAuthentication.Replace(_JAVASCRIPT_FENCE, _TYPESCRIPT_FENCE, StringComparison.Ordinal), rendered.TargetFormUnderTokenAuthentication);
			Assert.Equal(inherited.VerificationNote.Replace(_JAVASCRIPT_FENCE, _TYPESCRIPT_FENCE, StringComparison.Ordinal), rendered.VerificationNote);
			Assert.DoesNotContain(_JAVASCRIPT_FENCE, rendered.GenericSourceForm, StringComparison.Ordinal);
		}
	}

	/// <summary>The header class renders the `JavaScript` entry and adds the Angular `HttpClient` form: its source, its target and its note.</summary>
	[Fact]
	public void TheHeaderClassAddsTheAngularHttpClientFormToTheJavaScriptEntry()
	{
		RecommendationTemplate inherited = JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.TemplateProvider(_HEADER_PATTERN_CLASS, CredentialFlow.ClientCredentials)!;
		RecommendationTemplate rendered = TemplateOf(_HEADER_PATTERN_CLASS, CredentialFlow.ClientCredentials);

		Assert.StartsWith(inherited.GenericSourceForm.Replace(_JAVASCRIPT_FENCE, _TYPESCRIPT_FENCE, StringComparison.Ordinal), rendered.GenericSourceForm, StringComparison.Ordinal);
		Assert.StartsWith(inherited.TargetFormUnderTokenAuthentication.Replace(_JAVASCRIPT_FENCE, _TYPESCRIPT_FENCE, StringComparison.Ordinal), rendered.TargetFormUnderTokenAuthentication, StringComparison.Ordinal);
		Assert.Contains("new HttpHeaders({ Authorization: \"Basic \" + encoded })", rendered.GenericSourceForm, StringComparison.Ordinal);
		Assert.Contains("this.http.get<Reply>(API_ROUTE);", rendered.TargetFormUnderTokenAuthentication, StringComparison.Ordinal);
		Assert.Contains("its own origin with no `Authorization` header", rendered.TargetFormUnderTokenAuthentication, StringComparison.Ordinal);
		Assert.Contains("a browser never holds a RelativityOne token", rendered.TargetFormUnderTokenAuthentication, StringComparison.Ordinal);
		Assert.Contains("`HttpHeaders` object", rendered.VerificationNote, StringComparison.Ordinal);
		Assert.Contains("`setHeaders` option", rendered.VerificationNote, StringComparison.Ordinal);
	}

	/// <summary>The environments of the classes the catalog limits, and of the two classes of `TypeScript`.</summary>
	[Theory]
	[InlineData("JS-003", "Node.js (the userinfo is sent); browser (`fetch` throws)")]
	[InlineData("JS-008", "Node.js (`http` and `https`)")]
	[InlineData("JS-015", "Node.js; `got`")]
	[InlineData("JS-006", "Node.js; a browser bundle that polyfills `Buffer`")]
	[InlineData("JS-001", "browser and Node.js; `axios` (both), `request` (Node.js)")]
	[InlineData("TS-001", "browser and Node.js")]
	[InlineData("TS-003", "Node.js, a NestJS server")]
	public void TheEnvironmentsOfAClassAreTheOnesTheCatalogStates(string patternClass, string environments)
	{
		string firstLine = TemplateOf(patternClass, CredentialFlow.ClientCredentials).GenericSourceForm.Split(_NEWLINE)[0];

		Assert.Equal($"{_APPLIES_TO_LEAD_IN} {environments}.", firstLine);
	}

	/// <summary>The indentation rule of the project: a block fenced `typescript` is indented with tabs only.</summary>
	[Fact]
	public void EveryLineInsideAFenceIsIndentedWithTabsOnly()
	{
		foreach (string text in TypeScriptRenderedTexts.AllComposedTexts())
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

	// ------------------------------------------------------------------------------------------
	// Part 2: the token acquisition, the labelled variants and the Code-flow delta
	// ------------------------------------------------------------------------------------------

	/// <summary>Part 2 for each flow: the token acquisition the catalog declares, appended to every class's provenance.</summary>
	[Fact]
	public void EveryProvenanceEndsWithTheTokenAcquisitionOfItsFlow()
	{
		foreach (string patternClass in TypeScriptRenderedTexts.TemplatedPatternClasses)
		{
			string clientCredentials = TemplateOf(patternClass, CredentialFlow.ClientCredentials).CredentialProvenance;
			string authorizationCode = TemplateOf(patternClass, CredentialFlow.AuthorizationCode).CredentialProvenance;

			Assert.EndsWith(TypeScriptTemplateHooks.ClientCredentialsTokenAcquisition, clientCredentials, StringComparison.Ordinal);
			Assert.EndsWith(TypeScriptTemplateHooks.AuthorizationCodeTokenAcquisition, authorizationCode, StringComparison.Ordinal);
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
	/// THE VARIANTS. Application type is not known to the engine, so a finding receives every variant of its flow, labelled, each with its own precondition as the paragraph that follows its
	/// label: two for Client Credentials, three for Authorization Code.
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

	/// <summary>
	/// THE CODE-FLOW DELTA: wording and typing only. The three application types and their preconditions are those of `JavaScript`; the blocks are typed, an interface describes each token
	/// response, the typing sentence says an assertion validates nothing at run time, and the two framework shells are thin callers of the erasable functions above them.
	/// </summary>
	[Fact]
	public void TheCodeFlowDeltaIsTheTypedBlocksTheTypingSentenceAndTheTwoShells()
	{
		string clientCredentials = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;
		string authorizationCode = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode).CredentialProvenance;

		Assert.Contains("interface ClientCredentialsResponse", clientCredentials, StringComparison.Ordinal);
		Assert.Contains("requireEnvironmentValue(\"RELATIVITY_CLIENT_SECRET\")", clientCredentials, StringComparison.Ordinal);
		Assert.Contains(TypeScriptTemplateHooks.TypingSentence, clientCredentials, StringComparison.Ordinal);
		Assert.Contains("interface DocumentSummary", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("interface TokenResponse", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("interface RedirectParameters", authorizationCode, StringComparison.Ordinal);
		Assert.Contains(TypeScriptTemplateHooks.TypingSentence, authorizationCode, StringComparison.Ordinal);
		Assert.Contains(_ANGULAR_SHELL_MARKER, authorizationCode, StringComparison.Ordinal);
		Assert.Contains(_NEST_SHELL_MARKER, authorizationCode, StringComparison.Ordinal);
		Assert.Contains("@Injectable({ providedIn: \"root\" })", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("@Controller(\"auth\")", authorizationCode, StringComparison.Ordinal);
		Assert.Single(Regex.Matches(authorizationCode, Regex.Escape(_ANGULAR_SHELL_MARKER)));
		Assert.Single(Regex.Matches(authorizationCode, Regex.Escape(_NEST_SHELL_MARKER)));
		Assert.True(
			authorizationCode.IndexOf(_ANGULAR_SHELL_MARKER, StringComparison.Ordinal) < authorizationCode.IndexOf("**Variant 2", StringComparison.Ordinal),
			"The Angular shell stands in the browser variant.");
		Assert.True(
			authorizationCode.IndexOf(_NEST_SHELL_MARKER, StringComparison.Ordinal) > authorizationCode.IndexOf("**Variant 2", StringComparison.Ordinal)
				&& authorizationCode.IndexOf(_NEST_SHELL_MARKER, StringComparison.Ordinal) < authorizationCode.IndexOf("**Variant 3", StringComparison.Ordinal),
			"The NestJS shell stands in the Node.js server variant.");
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
		Assert.True(
			authorizationCode.IndexOf("*If RelativityOne serves CORS", StringComparison.Ordinal) > authorizationCode.IndexOf(_ANGULAR_SHELL_MARKER, StringComparison.Ordinal),
			"The Cross-origin paragraph follows the Angular shell as its own paragraph.");
	}

	/// <summary>The two Client Credentials statements that are not verified are not asserted.</summary>
	[Fact]
	public void TheUnverifiedClientCredentialsStatementsAreNotAsserted()
	{
		string clientCredentials = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance;

		Assert.DoesNotContain("displayed once", clientCredentials, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("not verified against RelativityOne", clientCredentials, StringComparison.Ordinal);
		Assert.DoesNotContain("standard OAuth shape", clientCredentials, StringComparison.Ordinal);
	}

	/// <summary>
	/// THE ROTATION STATEMENT of the classes that expose a credential: unconditional for a URL that carries one and for an assembled one, conditional on browser code for the classes that can run
	/// there, and stated in the browser variant of Client Credentials.
	/// </summary>
	[Fact]
	public void TheRotationStatementIsPresentForTheClassesThatExposeACredential()
	{
		foreach (CredentialFlow flow in TypeScriptRenderedTexts.Flows)
		{
			Assert.All(_exposureClasses, patternClass => Assert.Contains(_ROTATION_SENTENCE, TypeScriptRenderedTexts.FullText(patternClass, flow), StringComparison.Ordinal));
			Assert.All(
				_browserClasses,
				patternClass =>
				{
					string text = TypeScriptRenderedTexts.FullText(patternClass, flow);

					Assert.Contains(_BROWSER_ROTATION_LEAD, text, StringComparison.Ordinal);
					Assert.Contains(_ROTATION_SENTENCE, text, StringComparison.Ordinal);
				});
		}

		Assert.Contains(_ROTATION_SENTENCE, TemplateOf(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.ClientCredentials).CredentialProvenance, StringComparison.Ordinal);
	}

	/// <summary>The rotation, destination and lifetime sentences `TypeScript` carries are the `JavaScript` ones, word for word.</summary>
	[Fact]
	public void TheSentencesTheTypeScriptTemplatesRepeatAreTheJavaScriptSentences()
	{
		Assert.StartsWith(TypeScriptTemplateHooks.DestinationQuestion, TypeScriptTemplateHooks.JavaScript("JS_001_TARGET_FORM"), StringComparison.Ordinal);
		Assert.EndsWith(TypeScriptTemplateHooks.BrowserRotation, TypeScriptTemplateHooks.JavaScript("JS_001_VERIFICATION_NOTE"), StringComparison.Ordinal);
		Assert.EndsWith(TypeScriptTemplateHooks.RotationStatement, TypeScriptTemplateHooks.JavaScript("JS_003_VERIFICATION_NOTE"), StringComparison.Ordinal);
		Assert.Contains(TypeScriptTemplateHooks.TokenLifetime, TypeScriptTemplateHooks.JavaScript("JS_001_VERIFICATION_NOTE"), StringComparison.Ordinal);
	}

	/// <summary>
	/// THE DECLARED RELATIVITY-CLIENT STATEMENT. No Relativity-published client or typings are assumed: the templates reach RelativityOne by raw HTTP through the clients the catalog covers, with
	/// the built-in <c>fetch</c> and no package.
	/// </summary>
	[Fact]
	public void NoTemplateAssumesARelativityPublishedClientAndTheBlocksUseTheBuiltInFetch()
	{
		foreach (string text in TypeScriptRenderedTexts.AllComposedTexts())
		{
			Assert.DoesNotContain("@relativity", text, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("relativitydev", text, StringComparison.OrdinalIgnoreCase);
		}

		Assert.Contains("fetch(IDENTITY_TOKEN_URL", TypeScriptTemplateHooks.ClientCredentialsTokenAcquisition, StringComparison.Ordinal);
		Assert.Contains("The block uses the built-in `fetch` and no package.", TypeScriptTemplateHooks.ClientCredentialsTokenAcquisition, StringComparison.Ordinal);
	}

	// ------------------------------------------------------------------------------------------
	// The substitution of the JavaScript constants, each edit exactly once, and the comparison with the catalog
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// THE EXACTLY-ONCE SUBSTITUTION. Part 2 is built by substituting the `typescript` fence and the typed blocks into the `JavaScript` constants, and each substitution finds its text exactly once. The
	/// count is made here independently of the code that applies the edits, over the `JavaScript` constant as it stands, so a change to a `JavaScript` template constant that moves the text an edit
	/// stands on fails this test.
	/// </summary>
	[Fact]
	public void EverySubstitutionOfTheJavaScriptTokenAcquisitionOccursExactlyOnce()
	{
		AssertEachSubstitutionOccursExactlyOnce(TypeScriptTemplateHooks.JavaScript("CLIENT_CREDENTIALS_TOKEN_ACQUISITION"), TypeScriptTemplateHooks.Substitutions(CredentialFlow.ClientCredentials));
		AssertEachSubstitutionOccursExactlyOnce(TypeScriptTemplateHooks.JavaScript("AUTHORIZATION_CODE_TOKEN_ACQUISITION"), TypeScriptTemplateHooks.Substitutions(CredentialFlow.AuthorizationCode));
		Assert.Single(TypeScriptTemplateHooks.Substitutions(CredentialFlow.ClientCredentials));
		Assert.Equal(_AUTHORIZATION_CODE_BLOCK_COUNT, TypeScriptTemplateHooks.Substitutions(CredentialFlow.AuthorizationCode).Length);
	}

	/// <summary>The four `JavaScript` blocks that the typed blocks replace each occur exactly once, with their fence, in the constant of their flow, and in no other.</summary>
	[Fact]
	public void EachJavaScriptBlockThatIsReplacedOccursExactlyOnceInItsOwnFlowAndInNoOther()
	{
		(string Block, string Home, string Other)[] blocks =
		[
			(TypeScriptTemplateHooks.JavaScript("CLIENT_CREDENTIALS_NODE_PROCESS_BLOCK"), TypeScriptTemplateHooks.JavaScript("CLIENT_CREDENTIALS_TOKEN_ACQUISITION"), TypeScriptTemplateHooks.JavaScript("AUTHORIZATION_CODE_TOKEN_ACQUISITION")),
			(TypeScriptTemplateHooks.JavaScript("AUTHORIZATION_CODE_BROWSER_BLOCK"), TypeScriptTemplateHooks.JavaScript("AUTHORIZATION_CODE_TOKEN_ACQUISITION"), TypeScriptTemplateHooks.JavaScript("CLIENT_CREDENTIALS_TOKEN_ACQUISITION")),
			(TypeScriptTemplateHooks.JavaScript("AUTHORIZATION_CODE_NODE_SERVER_BLOCK"), TypeScriptTemplateHooks.JavaScript("AUTHORIZATION_CODE_TOKEN_ACQUISITION"), TypeScriptTemplateHooks.JavaScript("CLIENT_CREDENTIALS_TOKEN_ACQUISITION")),
			(TypeScriptTemplateHooks.JavaScript("AUTHORIZATION_CODE_NODE_CLI_BLOCK"), TypeScriptTemplateHooks.JavaScript("AUTHORIZATION_CODE_TOKEN_ACQUISITION"), TypeScriptTemplateHooks.JavaScript("CLIENT_CREDENTIALS_TOKEN_ACQUISITION"))
		];

		foreach ((string block, string home, string other) in blocks)
		{
			string fenced = $"{_JAVASCRIPT_FENCE}{_NEWLINE}{block}{_NEWLINE}{_FENCE}";

			Assert.Equal(1, CountOf(home, fenced));
			Assert.Equal(0, CountOf(other, fenced));
		}
	}

	/// <summary>After every edit no `javascript` fence is left in a token acquisition, and the number of fenced blocks is the number the catalog writes.</summary>
	[Fact]
	public void NoJavaScriptFenceIsLeftInATokenAcquisitionAndTheTypedBlocksAreCounted()
	{
		string clientCredentials = TypeScriptTemplateHooks.ClientCredentialsTokenAcquisition;
		string authorizationCode = TypeScriptTemplateHooks.AuthorizationCodeTokenAcquisition;

		Assert.DoesNotContain(_JAVASCRIPT_FENCE, clientCredentials, StringComparison.Ordinal);
		Assert.DoesNotContain(_JAVASCRIPT_FENCE, authorizationCode, StringComparison.Ordinal);
		Assert.Equal(_CLIENT_CREDENTIALS_BLOCK_COUNT, CountOf(clientCredentials, _TYPESCRIPT_FENCE));
		Assert.Equal(_AUTHORIZATION_CODE_BLOCK_COUNT, CountOf(authorizationCode, _TYPESCRIPT_FENCE));
	}

	/// <summary>The substitution fails loudly when an edit finds its text zero times or more than once, so a drifted `JavaScript` constant cannot leave a block unreplaced.</summary>
	[Fact]
	public void TheSubstitutionRefusesAnEditThatDoesNotFindItsTextExactlyOnce()
	{
		Assert.Throws<InvalidOperationException>(() => TypeScriptTemplateHooks.Substitute("abc", "missing", "text that is absent", "x"));
		Assert.Throws<InvalidOperationException>(() => TypeScriptTemplateHooks.Substitute("abab", "twice", "ab", "x"));
		Assert.Equal("xc", TypeScriptTemplateHooks.Substitute("abc", "once", "ab", "x"));
	}

	// ------------------------------------------------------------------------------------------
	// No browser variant places a client secret or a RelativityOne token in browser code
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// NO BROWSER VARIANT PLACES A CLIENT SECRET OR A RELATIVITYONE TOKEN IN BROWSER CODE. Read over the text the report renders, for every class and both flows: each fenced block introduced by a
	/// browser label (the browser use site, the Angular form, the browser variant) holds none of the terms of a secret, a token, an authorization header or the identity endpoint.
	/// </summary>
	[Fact]
	public void NoBrowserVariantPlacesAClientSecretOrATokenInBrowserCode()
	{
		int browserBlocks = 0;

		foreach (string text in TypeScriptRenderedTexts.AllComposedTexts())
		{
			ImmutableArray<string> blocks = BrowserBlocksIn(text);

			browserBlocks += blocks.Length;
			Assert.All(blocks, block => Assert.Empty(TermsABrowserBlockHolds(block)));
		}

		Assert.True(browserBlocks > 0, "The check read no browser block.");
		Assert.All(
			TypeScriptRenderedTexts.Flows.Select(flow => TypeScriptRenderedTexts.FullText(_TOKEN_STEP_PATTERN_CLASS, flow)),
			text => Assert.NotEmpty(BrowserBlocksIn(text)));
		Assert.Contains(
			BrowserBlocksIn(TypeScriptRenderedTexts.FullText(_HEADER_PATTERN_CLASS, CredentialFlow.AuthorizationCode)),
			block => block.Contains("this.http.get<Reply>(API_ROUTE);", StringComparison.Ordinal));
		Assert.Contains(
			BrowserBlocksIn(TypeScriptRenderedTexts.FullText(_TOKEN_STEP_PATTERN_CLASS, CredentialFlow.AuthorizationCode)),
			block => block.Contains("@angular/common/http", StringComparison.Ordinal));
	}

	/// <summary>The check still fails when a browser block holds a secret or a token, and it does not read a Node.js block as a browser one.</summary>
	[Fact]
	public void TheBrowserBlockCheckStillReportsASecretOrATokenInABrowserBlock()
	{
		string leaking = "*Browser:* the call moves.\n\n```typescript\nawait fetch(route, { headers: { Authorization: `Bearer ${accessToken}` } });\n```";
		string leakingAngular = "*Angular `HttpClient`, browser:* the call moves.\n\n```typescript\nthis.http.get(route, { headers: { Authorization: token } });\n```";
		string leakingSecret = "**Variant 1 — Browser with a server-side component**\n\n```typescript\nconst clientSecret = \"x\";\n```";
		string node = "*Node.js:*\n\n```typescript\nawait fetch(route, { headers: { Authorization: `Bearer ${accessToken}` } });\n```";

		Assert.NotEmpty(BrowserBlocksIn(leaking).SelectMany(block => TermsABrowserBlockHolds(block)));
		Assert.NotEmpty(BrowserBlocksIn(leakingAngular).SelectMany(block => TermsABrowserBlockHolds(block)));
		Assert.NotEmpty(BrowserBlocksIn(leakingSecret).SelectMany(block => TermsABrowserBlockHolds(block)));
		Assert.Empty(BrowserBlocksIn(node));
	}

	// ------------------------------------------------------------------------------------------
	// What a client-facing text may carry
	// ------------------------------------------------------------------------------------------

	/// <summary>No client-facing text, composed or declared, leaks an internal identifier.</summary>
	[Fact]
	public void NoComposedTextOrFallbackTextLeaksAnInternalIdentifier()
	{
		foreach (string text in TypeScriptRenderedTexts.AllComposedTexts())
		{
			Assert.Empty(InternalIdentifiersIn(text));
		}

		foreach (CredentialFlow flow in TypeScriptRenderedTexts.Flows)
		{
			Assert.Empty(InternalIdentifiersIn(TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]));
		}
	}

	/// <summary>The leak check still fails when a client-facing text carries an internal identifier: each sample of the public-source guard, injected into a real text, is reported.</summary>
	[Fact]
	public void TheLeakCheckStillReportsEveryInternalIdentifierOfTheGuard()
	{
		string composedText = TypeScriptRenderedTexts.AllComposedTexts().First();
		string fallbackText = TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![TypeScriptRenderedTexts.Flows.First()];

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
		Regex citation = new(@"\bQ-[A-Z]+|\(\s*(?:S|T|CF|VF)\d|\bCF-\w+|\bVF-\d|\bO5-\d|\bOT-\d|\b[TU]F-\d|\bJS-\d{3}\b|\bTS-\d{3}\b|\bitem \d+\b", RegexOptions.CultureInvariant);

		Assert.All(TypeScriptRenderedTexts.AllComposedTexts(), text => Assert.DoesNotMatch(citation, text));
	}

	/// <summary>Every token-acquisition part states where each of its facts comes from, by one of the labels, and the fallback texts do the same for the endpoints.</summary>
	[Fact]
	public void EveryFlowsTokenAcquisitionAndEveryFallbackLabelsTheProvenanceOfItsFacts()
	{
		foreach (CredentialFlow flow in TypeScriptRenderedTexts.Flows)
		{
			string tokenAcquisition = TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).CredentialProvenance;
			string fallback = TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

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

		foreach (string text in TypeScriptRenderedTexts.AllComposedTexts())
		{
			foreach (string block in BlocksIn(text))
			{
				if (block.Contains("client_secret", StringComparison.OrdinalIgnoreCase))
				{
					Assert.Contains("requireEnvironmentValue(\"RELATIVITY_CLIENT_SECRET\")", block, StringComparison.Ordinal);
				}

				Assert.DoesNotMatch(quotedCredentialAssignment, block);
			}
		}
	}

	/// <summary>A sentence that handles a secret names the Part B pointer through the shared constant and never a link.</summary>
	[Fact]
	public void EverySecretHandlingSentenceNamesThePartBPointer()
	{
		foreach (CredentialFlow flow in TypeScriptRenderedTexts.Flows)
		{
			Assert.Contains(SecretManagementPointer.PART_B_NAME, TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).CredentialProvenance, StringComparison.Ordinal);
			Assert.Contains(SecretManagementPointer.PART_B_NAME, TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow], StringComparison.Ordinal);
		}

		Assert.All(TypeScriptRenderedTexts.AllComposedTexts(), text => Assert.DoesNotContain("http://secret", text, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>No template recommends a grant outside the two the tool recommends, and none puts the destination's token endpoint behind a browser.</summary>
	[Fact]
	public void NoTemplateNamesARefusedGrantInItsCode()
	{
		foreach (string text in TypeScriptRenderedTexts.AllComposedTexts())
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
		ILanguageAnalyzer analyzer = TypeScriptRenderedTexts.Analyzer();

		Assert.Equal(TypeScriptRenderedTexts.Flows.Length, analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Count);
		Assert.All(
			analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow!.Values,
			text => Assert.False(string.IsNullOrWhiteSpace(text)));
		Assert.Empty(AnalyzerContractValidator.Validate(analyzer));
	}

	/// <summary>The generic route composes from the language's own fallback text and then the shared engine's OAuth2 Client block.</summary>
	[Fact]
	public void TheGenericRouteComposesTheLanguageFallbackTextOfEachFlow()
	{
		foreach (CredentialFlow flow in TypeScriptRenderedTexts.Flows)
		{
			string fallback = TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.Contains(fallback, TypeScriptRenderedTexts.GenericRouteText(flow), StringComparison.Ordinal);
		}

		Assert.NotEqual(
			TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.ClientCredentials],
			TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.AuthorizationCode]);
	}

	/// <summary>The fallback is a pointer and not an instruction: no code block, no before-and-after form, and no secret in browser code.</summary>
	[Fact]
	public void TheFallbackTextsOfBothFlowsAreAPointerWithNoCodeAndNoBrowserSecret()
	{
		foreach (CredentialFlow flow in TypeScriptRenderedTexts.Flows)
		{
			string fallback = TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.DoesNotContain(_FENCE, fallback, StringComparison.Ordinal);
			Assert.Contains("did not recognize the construct", fallback, StringComparison.Ordinal);
			Assert.Contains("a pointer and not an instruction", fallback, StringComparison.Ordinal);
			Assert.Contains("TypeScript", fallback, StringComparison.Ordinal);
			Assert.DoesNotContain("JavaScript", fallback, StringComparison.Ordinal);
		}

		Assert.Contains("a secret shipped to a browser is public", TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.ClientCredentials], StringComparison.Ordinal);
		Assert.Contains("never in browser code", TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.AuthorizationCode], StringComparison.Ordinal);
	}

	/// <summary>The fallback texts are the `JavaScript` ones with the language named: they differ from them only where the text names the language.</summary>
	[Fact]
	public void TheFallbackTextsAreTheJavaScriptTextsWithTheLanguageNamed()
	{
		Dictionary<CredentialFlow, string> javaScript = JavaScriptRenderedTexts.Flows.ToDictionary(
			flow => flow,
			flow => JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]);
		string clientCredentials = TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.ClientCredentials];
		string authorizationCode = TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![CredentialFlow.AuthorizationCode];

		Assert.Equal(javaScript[CredentialFlow.ClientCredentials].Replace("JavaScript", "TypeScript", StringComparison.Ordinal), clientCredentials);
		Assert.Equal(
			javaScript[CredentialFlow.AuthorizationCode].Replace("for code that runs in a browser", "for TypeScript that runs in a browser", StringComparison.Ordinal),
			authorizationCode);
		Assert.DoesNotContain("JavaScript", clientCredentials, StringComparison.Ordinal);
	}

	/// <summary>
	/// A fixture per flow, at the finding: a High `TypeScript` finding of a class with no pattern-specific template renders the `TypeScript` fallback text of its flow, not the shared engine's
	/// generic text, not the `JavaScript` fallback and not another class's template.
	/// </summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void AHighFindingOfAClassWithNoTemplateRendersTheTypeScriptFallbackOfItsFlow(CredentialFlow flow)
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_FILE_NAME,
			1,
			TypeScriptRenderedTexts.UNDECLARED_PATTERN_CLASS,
			ConfidenceLevel.High,
			language: AssessedLanguage.TypeScript,
			flow: flow,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(TypeScriptRenderedTexts.Catalog(), new GenericRecommendationProvider());

		string composed = RecommendationComposer.ComposeArtifactText(renderer.Render(finding, EmissionDestination.StructuredData))!;
		string fallback = TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];
		string javaScriptFallback = JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];
		string sharedGenericText = flow == CredentialFlow.AuthorizationCode
			? GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT
			: GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT;

		Assert.Equal(RecommendationKind.Generic, renderer.Route(finding));
		Assert.Contains(fallback, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(sharedGenericText, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(javaScriptFallback, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(_TYPESCRIPT_FENCE, composed, StringComparison.Ordinal);
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
			language: AssessedLanguage.TypeScript,
			flow: flow,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(TypeScriptRenderedTexts.Catalog(), new GenericRecommendationProvider());

		string composed = RecommendationComposer.ComposeArtifactText(renderer.Render(finding, EmissionDestination.StructuredData))!;

		Assert.Contains(grant, composed, StringComparison.Ordinal);
		Assert.Contains(TemplateOf(_TOKEN_STEP_PATTERN_CLASS, flow).GenericSourceForm, composed, StringComparison.Ordinal);
	}

	// ------------------------------------------------------------------------------------------
	// The flow a finding renders: the Code use site and the variants, or Client Credentials with the default note
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// An interactive signal renders Authorization Code, with its note, its Code use site and the three labelled variants, the Angular and NestJS shells among them; a fixed signal renders Client
	/// Credentials with its note; no signal renders Client Credentials with the default note and the two variants of that flow.
	/// </summary>
	[Fact]
	public void AnInteractiveSignalRendersTheCodeUseSiteWithItsVariantsAndNoSignalRendersClientCredentialsWithTheDefaultNote()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_INTERACTIVE_FILE_NAME, $"const typed = window.prompt('Enter your value');\n{HighConstruct()}{_NEWLINE}");
		harness.WriteSourceFile(_FIXED_FILE_NAME, $"const entry = process.env.API_PASSWORD;\n{HighConstruct()}{_NEWLINE}");
		harness.WriteSourceFile(_PLAIN_FILE_NAME, $"{HighConstruct()}{_NEWLINE}");

		harness.RunTheWholeWorkflow();

		ArtifactDocument document = ReadArtifact(harness);
		ArtifactFindingDocument interactive = FindingOf(document, _INTERACTIVE_FILE_NAME, _HEADER_PATTERN_CLASS);
		ArtifactFindingDocument fixedCredential = FindingOf(document, _FIXED_FILE_NAME, _HEADER_PATTERN_CLASS);
		ArtifactFindingDocument plain = FindingOf(document, _PLAIN_FILE_NAME, _HEADER_PATTERN_CLASS);

		Assert.Equal(nameof(CredentialFlow.AuthorizationCode), interactive.Flow);
		Assert.Equal(nameof(FlowDecision.InteractiveSignal), interactive.FlowDecision);
		Assert.Equal(FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE, interactive.FlowNote);
		Assert.Contains("grant_type=authorization_code", interactive.Recommendation, StringComparison.Ordinal);
		Assert.DoesNotContain("grant_type=client_credentials", interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains("Bearer ${accessToken}", interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains("**Variant 1 — Browser with a server-side component**", interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains("**Variant 2 — Node.js server**", interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains("**Variant 3 — Node.js command line or desktop**", interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_ANGULAR_SHELL_MARKER, interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_NEST_SHELL_MARKER, interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_TYPESCRIPT_FENCE, interactive.Recommendation, StringComparison.Ordinal);
		Assert.DoesNotContain(_JAVASCRIPT_FENCE, interactive.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), fixedCredential.Flow);
		Assert.Equal(nameof(FlowDecision.FixedSignal), fixedCredential.FlowDecision);
		Assert.Equal(FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE, fixedCredential.FlowNote);
		Assert.Contains("grant_type=client_credentials", fixedCredential.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), plain.Flow);
		Assert.Equal(nameof(FlowDecision.Default), plain.FlowDecision);
		Assert.Equal(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, plain.FlowNote);
		Assert.Contains("grant_type=client_credentials", plain.Recommendation, StringComparison.Ordinal);
		Assert.Contains("Bearer ${accessToken}", plain.Recommendation, StringComparison.Ordinal);
		Assert.Contains("**Variant: a Node.js process acting as itself**", plain.Recommendation, StringComparison.Ordinal);
		Assert.Contains("**Variant: browser code that does not act for a signed-in user**", plain.Recommendation, StringComparison.Ordinal);
	}

	// ------------------------------------------------------------------------------------------
	// The literal credential, true and false
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// THE LITERAL CREDENTIAL, TRUE. The inherited rule: a string literal in a credential position, a template literal with no hole, a URL literal with a password in its userinfo and a literal
	/// token after the scheme word; and the rule `TypeScript` adds: a literal in a typed credential object.
	/// </summary>
	[Theory]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'exampleSecret' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: \"exampleSecret\" } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: `exampleSecret` } });", "JS-001")]
	[InlineData("axios.get<Reply>(url, { auth: { username: userName, password: 'exampleSecret' } });", "JS-001")]
	[InlineData("http.get(url, { auth: 'exampleUser:exampleSecret' }, handler);", "JS-008")]
	[InlineData("const baseUrl = 'https://exampleUser:exampleSecret@host.example/path';", "JS-013")]
	[InlineData("fetch(url, { headers: { Authorization: 'Basic ZXhhbXBsZVVzZXI6ZXhhbXBsZVNlY3JldA==' } });", "JS-004")]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:exampleSecret');", "JS-005")]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: 'exampleSecret' } };", "TS-001")]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: \"exampleSecret\" } };", "TS-001")]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: `exampleSecret` } };", "TS-001")]
	[InlineData("const credentials = { auth: { username: 'exampleUser', password: 'exampleSecret' } } as AxiosRequestConfig;", "TS-001")]
	public void ALiteralCredentialReadsAsLiteral(string source, string patternClass)
	{
		RecognizedConstruct construct = ConstructOf(source, patternClass);

		Assert.True(construct.LiteralCredentialSignal, source);
	}

	/// <summary>
	/// THE LITERAL CREDENTIAL, FALSE. A template with a hole is code in its hole, so it does not read as literal; a placeholder form of the vocabulary does not either; nor does a variable, a read of the
	/// environment or a concatenation of a literal with code. The same holds in a typed credential object.
	/// </summary>
	[Theory]
	[InlineData("axios.get(url, { auth: { username: userName, password: `user:${pass}` } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: secret } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: process.env.ENTRY } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'changeme' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: '<password>' } });", "JS-001")]
	[InlineData("axios.get(url, { auth: { username: userName, password: '' } });", "JS-001")]
	[InlineData("http.get(url, { auth: `${userName}:${secret}` }, handler);", "JS-008")]
	[InlineData("const baseUrl = 'https://exampleUser:changeme@host.example/path';", "JS-013")]
	[InlineData("const header = 'Basic ' + btoa(`exampleUser:${secret}`);", "JS-005")]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: `${secret}` } };", "TS-001")]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: 'changeme' } };", "TS-001")]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: '********' } };", "TS-001")]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: '' } };", "TS-001")]
	public void ANonLiteralCredentialDoesNotReadAsLiteral(string source, string patternClass)
	{
		RecognizedConstruct construct = ConstructOf(source, patternClass);

		Assert.False(construct.LiteralCredentialSignal, source);
	}

	/// <summary>The finding is unchanged by the literal rule: a placeholder, a hole and a literal each still yield the same class at the same level, only the signal differs.</summary>
	[Fact]
	public void TheLiteralRuleChangesTheSignalAndNeverTheFinding()
	{
		RecognizedConstruct literal = ConstructOf("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: 'exampleSecret' } };", "TS-001");
		RecognizedConstruct placeholder = ConstructOf("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: 'changeme' } };", "TS-001");
		RecognizedConstruct withAHole = ConstructOf("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: `${secret}` } };", "TS-001");

		Assert.Equal(ConfidenceLevel.Medium, literal.Level);
		Assert.Equal(literal.Level, placeholder.Level);
		Assert.Equal(literal.Level, withAHole.Level);
	}

	/// <summary>
	/// THE CONSOLE WARNING THROUGH THE REAL PIPELINE. A literal credential in a `TypeScript` source, in a call and in a typed credential object, produces a warning line naming the finding, its
	/// pattern class and the Secret Management pointer (Part B), and never the placed secret itself anywhere in the transcript.
	/// </summary>
	[Theory]
	[InlineData("await axios.get(url, { auth: { username: userName, password: 'exampleSecretValue' } });", "JS-001")]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: 'exampleSecretValue' } };", "TS-001")]
	public void ALiteralCredentialFindingProducesAWarningNamingPartBAndNeverPrintsTheValue(string source, string patternClass)
	{
		const string PLACED_SECRET = "exampleSecretValue";

		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile("Credential.ts", $"{source}{_NEWLINE}");

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.NotEmpty(harness.State.Findings);
		Assert.Contains("Warning:", harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(patternClass, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(PLACED_SECRET, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>The recommendation of a literal-credential `TypeScript` finding carries the security sentence that names Part B, beside the migration text and not inside it.</summary>
	[Theory]
	[InlineData("JS-001")]
	[InlineData("TS-001")]
	public void ALiteralCredentialTypeScriptFindingRendersTheSecuritySentenceThatNamesPartB(string patternClass)
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			"Credential.ts",
			1,
			patternClass,
			ConfidenceLevel.High,
			literalCredentialSignal: true,
			language: AssessedLanguage.TypeScript,
			flow: CredentialFlow.ClientCredentials,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(TypeScriptRenderedTexts.Catalog(), new GenericRecommendationProvider());

		RenderedRecommendation rendered = renderer.Render(finding, EmissionDestination.StructuredData);

		Assert.Equal(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, rendered.SecurityNote);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, rendered.SecurityNote, StringComparison.Ordinal);
		Assert.DoesNotContain(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, RecommendationComposer.ComposeArtifactText(rendered)!, StringComparison.Ordinal);
	}

	/// <summary>The complement: a credential held in a variable produces no warning line at all, in a call and in a typed credential object.</summary>
	[Theory]
	[InlineData("await axios.get(url, { auth: { username: userName, password: secret } });")]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: `${secret}` } };")]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: 'changeme' } };")]
	public void AVariableCredentialFindingProducesNoWarning(string source)
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile("Credential.ts", $"{source}{_NEWLINE}");

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.NotEmpty(harness.State.Findings);
		Assert.DoesNotContain("Warning:", harness.Transcript, StringComparison.Ordinal);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static string HighConstruct()
	{
		return "await fetch(url, { headers: { Authorization: 'Basic ' + encoded } });";
	}

	private static void AssertEachSubstitutionOccursExactlyOnce(string javaScriptConstant, ImmutableArray<(string Name, string Find, string Replacement)> substitutions)
	{
		Assert.NotEmpty(substitutions);

		foreach ((string name, string find, _) in substitutions)
		{
			Assert.True(CountOf(javaScriptConstant, find) == 1, $"The edit '{name}' finds its text {CountOf(javaScriptConstant, find)} times.");
		}
	}

	private static int CountOf(string text, string find)
	{
		int count = 0;
		int index = text.IndexOf(find, StringComparison.Ordinal);

		while (index >= 0)
		{
			count++;
			index = text.IndexOf(find, index + find.Length, StringComparison.Ordinal);
		}

		return count;
	}

	/// <summary>The `typescript` blocks of a rendered text, in order, with their lines joined by line feeds.</summary>
	private static ImmutableArray<string> TypeScriptBlocksIn(string text)
	{
		ImmutableArray<string>.Builder blocks = ImmutableArray.CreateBuilder<string>();
		List<string> current = [];
		bool inside = false;

		foreach (string line in text.Split(_NEWLINE))
		{
			if (!inside && line.StartsWith(_TYPESCRIPT_FENCE, StringComparison.Ordinal))
			{
				inside = true;
				current = [];
			}
			else if (inside && line.StartsWith(_FENCE, StringComparison.Ordinal))
			{
				blocks.Add(string.Join(_NEWLINE, current));
				inside = false;
			}
			else if (inside)
			{
				current.Add(line);
			}
		}

		return blocks.ToImmutable();
	}

	private static RecognizedConstruct ConstructOf(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize($"{source}{_NEWLINE}");

		return Assert.Single(constructs, construct => construct.PatternClassIdentifier == patternClass);
	}

	private static void AssertEveryFenceIsTypeScriptOrItsClosing(string text)
	{
		bool inside = false;

		foreach (string line in text.Split(_NEWLINE))
		{
			if (line.TrimStart().StartsWith(_FENCE, StringComparison.Ordinal))
			{
				Assert.Equal(inside, line.Trim() == _FENCE);
				Assert.True(inside || line.Trim() == _TYPESCRIPT_FENCE, $"A fence that is not `typescript`: {line}");
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
	/// The fenced blocks the text introduces as browser code: the block that follows the browser label of a use site, the Angular label, or a variant whose label names a browser. A block with
	/// another label, or none, is not a browser block.
	/// </summary>
	private static ImmutableArray<string> BrowserBlocksIn(string text)
	{
		ImmutableArray<string>.Builder blocks = ImmutableArray.CreateBuilder<string>();
		bool browserLabel = false;
		bool inside = false;
		StringBuilder current = new();

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
				current.Append(line).Append(_NEWLINE);
			}
			else if (line.Contains(_BROWSER_LABEL, StringComparison.Ordinal) || line.Contains(_ANGULAR_LABEL, StringComparison.Ordinal))
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
		StringBuilder current = new();

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
				current.Append(line).Append(_NEWLINE);
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

	private static ArtifactDocument ReadArtifact(PipelineHarness harness)
	{
		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		return JsonSerializer.Deserialize(artifactText, ReportJsonContext.Default.ArtifactDocument)!;
	}

	private static ArtifactFindingDocument FindingOf(ArtifactDocument document, string fileName, string patternClass)
	{
		return document.Findings.Single(finding => finding.FilePath == fileName && finding.PatternClass == patternClass);
	}

	private static RecommendationTemplate TemplateOf(string patternClass, CredentialFlow flow)
	{
		return TypeScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.TemplateProvider(patternClass, flow)!;
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
