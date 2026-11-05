using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The engine-owned OAuth2 Client block for each flow, the fifth piece of
/// every composed recommendation. The wording is asserted by the ideas it must carry and the claims it
/// must not make, not by an exact comparison of prose.
/// </summary>
public sealed class OAuth2ClientConfigurationBlockTests
{
	private const string _LIFETIME_PATTERN = @"\b(\d+)\s*(?:minutes?|hours?|seconds?)\b";

	private static readonly ImmutableArray<string> _clientCredentialsFields =
	[
		OAuth2ClientConfigurationBlocks.NAME_FIELD,
		OAuth2ClientConfigurationBlocks.FLOW_GRANT_TYPE_FIELD,
		OAuth2ClientConfigurationBlocks.CONTEXT_USER_FIELD,
		OAuth2ClientConfigurationBlocks.ACCESS_TOKEN_LIFETIME_FIELD,
		OAuth2ClientConfigurationBlocks.TRUSTED_IPS_FIELD,
		OAuth2ClientConfigurationBlocks.ALLOW_REFRESH_TOKENS_FIELD,
		OAuth2ClientConfigurationBlocks.ENABLED_FIELD
	];

	private static readonly ImmutableArray<string> _authorizationCodeFields =
	[
		OAuth2ClientConfigurationBlocks.NAME_FIELD,
		OAuth2ClientConfigurationBlocks.FLOW_GRANT_TYPE_FIELD,
		OAuth2ClientConfigurationBlocks.REDIRECT_URLS_FIELD,
		OAuth2ClientConfigurationBlocks.ACCESS_TOKEN_LIFETIME_FIELD,
		OAuth2ClientConfigurationBlocks.ALLOW_REFRESH_TOKENS_FIELD,
		OAuth2ClientConfigurationBlocks.ENABLED_FIELD
	];

	/// <summary>
	/// Every class of the real embedded `CSharp` analyzer, composed for each flow through the
	/// composer, carries the flow's block with every field name - on the template route and on the
	/// generic route alike, and in the text the JSON artifact carries.
	/// </summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void EveryCSharpClassComposedForAFlowCarriesThatFlowsBlockWithEveryFieldName(CredentialFlow flow)
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();
		ILanguageAnalyzer analyzer = catalog.AnalyzersByLanguageName["CSharp"];
		ImmutableArray<string> fieldNames = flow is CredentialFlow.ClientCredentials ? _clientCredentialsFields : _authorizationCodeFields;

		Assert.NotEmpty(analyzer.DetectionRulesByPatternClass.Keys);

		foreach (string patternClassIdentifier in analyzer.DetectionRulesByPatternClass.Keys)
		{
			RenderedRecommendation composed = RecommendationComposer.Compose(catalog, AssessedLanguage.CSharp, patternClassIdentifier, flow);
			string? artifactText = RecommendationComposer.ComposeArtifactText(composed);

			Assert.Equal(OAuth2ClientConfigurationBlocks.For(flow), composed.OAuth2ClientBlock);
			Assert.NotNull(artifactText);
			Assert.Contains(RecommendationComposer.OAUTH2_CLIENT_LABEL + ": ", artifactText, StringComparison.Ordinal);
			Assert.Contains(OAuth2ClientConfigurationBlocks.For(flow), artifactText, StringComparison.Ordinal);

			foreach (string fieldName in fieldNames)
			{
				Assert.Contains($"- {fieldName}: ", artifactText, StringComparison.Ordinal);
			}
		}
	}

	/// <summary>The blocks are total over the flows and differ: neither flow is shown the other's fields.</summary>
	[Fact]
	public void EachBlockCarriesOnlyItsOwnFlowsFields()
	{
		string clientCredentials = OAuth2ClientConfigurationBlocks.For(CredentialFlow.ClientCredentials);
		string authorizationCode = OAuth2ClientConfigurationBlocks.For(CredentialFlow.AuthorizationCode);

		Assert.DoesNotContain("- Redirect URLs: ", clientCredentials, StringComparison.Ordinal);
		Assert.DoesNotContain("- Context User: ", authorizationCode, StringComparison.Ordinal);
		Assert.DoesNotContain("- Trusted IPs: ", authorizationCode, StringComparison.Ordinal);
	}

	/// <summary>
	/// Trusted IPs restrict where a token can be requested, are checked when the token is
	/// issued, and do not restrict a token already issued.
	/// </summary>
	[Fact]
	public void TrustedIpsAreDescribedAsAnIssuanceTimeCheckThatDoesNotRestrictAnIssuedToken()
	{
		string line = LineOf(OAuth2ClientConfigurationBlocks.For(CredentialFlow.ClientCredentials), OAuth2ClientConfigurationBlocks.TRUSTED_IPS_FIELD);

		Assert.Contains("restricts where a token can be requested", line, StringComparison.Ordinal);
		Assert.Contains("checked when the token is issued", line, StringComparison.Ordinal);
		Assert.Contains("does not restrict a token already issued", line, StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.RELATIVITY_DOCUMENTATION_LABEL, line, StringComparison.Ordinal);
	}

	/// <summary>Neither block claims that any field is required.</summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void NeitherBlockClaimsAnyFieldIsRequired(CredentialFlow flow)
	{
		string block = OAuth2ClientConfigurationBlocks.For(flow);

		Assert.DoesNotContain("required", block, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("mandatory", block, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The flow is fixed when the client is created, so moving to Code is a new client.</summary>
	[Fact]
	public void TheClientCredentialsBlockStatesTheFlowIsFixedAtCreation()
	{
		string block = OAuth2ClientConfigurationBlocks.For(CredentialFlow.ClientCredentials);

		Assert.Contains(OAuth2ClientConfigurationBlocks.FLOW_FIXED_AT_CREATION_NOTE, block, StringComparison.Ordinal);
		Assert.Contains("fixed when the OAuth2 Client is created", block, StringComparison.Ordinal);
		Assert.Contains("Authorization Code later means creating a new OAuth2 Client", block, StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.CONTEXT_USER_PERMISSIONS_NOTE, block, StringComparison.Ordinal);
	}

	/// <summary>The lifetime line reads 60 minutes, quotes Relativity, is in minutes, and is never 30.</summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void TheLifetimeLineReadsSixtyMinutesAsRelativityRecommends(CredentialFlow flow)
	{
		string line = LineOf(OAuth2ClientConfigurationBlocks.For(flow), OAuth2ClientConfigurationBlocks.ACCESS_TOKEN_LIFETIME_FIELD);
		string expectedValue = string.Format(
			CultureInfo.InvariantCulture,
			"{0} minutes",
			OAuth2ClientConfigurationBlocks.RECOMMENDED_ACCESS_TOKEN_LIFETIME_IN_MINUTES);

		Assert.Equal(60, OAuth2ClientConfigurationBlocks.RECOMMENDED_ACCESS_TOKEN_LIFETIME_IN_MINUTES);
		Assert.Contains(expectedValue, line, StringComparison.Ordinal);
		Assert.Contains("stated in minutes", line, StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.RELATIVITY_DOCUMENTATION_LABEL, line, StringComparison.Ordinal);
		Assert.DoesNotContain("30", line, StringComparison.Ordinal);
	}

	/// <summary>
	/// The Context User recommendation is a non-administrator account, labelled as NSerio's
	/// recommendation with the reason, beside Relativity's own description of it as usually an
	/// administrator.
	/// </summary>
	[Fact]
	public void TheContextUserRecommendsANonAdministratorWithTheReasonBesideRelativitysDescription()
	{
		string line = LineOf(OAuth2ClientConfigurationBlocks.For(CredentialFlow.ClientCredentials), OAuth2ClientConfigurationBlocks.CONTEXT_USER_FIELD);

		Assert.Contains(OAuth2ClientConfigurationBlocks.NSERIO_RECOMMENDATION_LABEL, line, StringComparison.Ordinal);
		Assert.Contains("not an administrator", line, StringComparison.Ordinal);
		Assert.Contains("every call runs with that user's permissions", line, StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.RELATIVITY_DOCUMENTATION_LABEL, line, StringComparison.Ordinal);
		Assert.Contains("Relativity Administrators group", line, StringComparison.Ordinal);
		Assert.True(
			line.IndexOf(OAuth2ClientConfigurationBlocks.NSERIO_RECOMMENDATION_LABEL, StringComparison.Ordinal)
				< line.IndexOf(OAuth2ClientConfigurationBlocks.RELATIVITY_DOCUMENTATION_LABEL, StringComparison.Ordinal),
			"NSerio's recommendation is stated first, and Relativity's description sits beside it.");
	}

	/// <summary>
	/// The Redirect URLs line covers both forms: an exact HTTPS URL per environment with no
	/// wildcards for server-side web applications, and a plain-HTTP loopback URL on a fixed port for
	/// desktop and CLI applications, the latter labelled as verified by NSerio and not documented.
	/// </summary>
	[Fact]
	public void TheRedirectUrlsLineMatchesBothForms()
	{
		string line = LineOf(OAuth2ClientConfigurationBlocks.For(CredentialFlow.AuthorizationCode), OAuth2ClientConfigurationBlocks.REDIRECT_URLS_FIELD);

		Assert.Contains("Server-side web applications", line, StringComparison.Ordinal);
		Assert.Contains("one exact HTTPS URL per environment", line, StringComparison.Ordinal);
		Assert.Contains("no wildcards", line, StringComparison.Ordinal);
		Assert.Contains("Desktop and CLI applications", line, StringComparison.Ordinal);
		Assert.Contains("http://127.0.0.1:<port>/<path>", line, StringComparison.Ordinal);
		Assert.Contains("fixed port", line, StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL, line, StringComparison.Ordinal);
		Assert.Contains("verified by NSerio against a live instance; not documented by Relativity", line, StringComparison.Ordinal);
		Assert.Contains("registered by the Relativity administrator", line, StringComparison.Ordinal);
	}

	/// <summary>
	/// The lifetime lives in one named constant, every block takes it from there, and no other
	/// lifetime value appears in either block or in either generic text: every duration these texts state
	/// is the constant's.
	/// </summary>
	[Fact]
	public void NoLifetimeValueOtherThanTheOneConstantAppearsInTheBlocksOrTheGenericTexts()
	{
		string[] texts =
		[
			OAuth2ClientConfigurationBlocks.For(CredentialFlow.ClientCredentials),
			OAuth2ClientConfigurationBlocks.For(CredentialFlow.AuthorizationCode),
			GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT,
			GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT
		];
		string recommended = OAuth2ClientConfigurationBlocks.RECOMMENDED_ACCESS_TOKEN_LIFETIME_IN_MINUTES.ToString(CultureInfo.InvariantCulture);

		foreach (string text in texts)
		{
			foreach (Match match in Regex.Matches(text, _LIFETIME_PATTERN, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
			{
				Assert.Equal(recommended, match.Groups[1].Value);
				Assert.EndsWith("minutes", match.Value, StringComparison.OrdinalIgnoreCase);
			}
		}

		FieldInfo? constant = typeof(OAuth2ClientConfigurationBlocks).GetField(nameof(OAuth2ClientConfigurationBlocks.RECOMMENDED_ACCESS_TOKEN_LIFETIME_IN_MINUTES));

		Assert.NotNull(constant);
		Assert.True(constant.IsLiteral);
		Assert.Contains(recommended + " minutes", texts[0], StringComparison.Ordinal);
		Assert.Contains(recommended + " minutes", texts[1], StringComparison.Ordinal);
	}

	/// <summary>
	/// Both generic texts are named constants; the Authorization Code text names the flow, the
	/// Redirect URL and signing in, and never the client-credentials grant; the Client Credentials text
	/// names its flow and never a Redirect URL; both name the Part B pointer.
	/// </summary>
	[Fact]
	public void TheTwoGenericTextsAreNamedConstantsThatMeetTheContainmentRules()
	{
		string authorizationCode = GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT;
		string clientCredentials = GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT;

		Assert.True(typeof(GenericRecommendationProvider).GetField(nameof(GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT))!.IsLiteral);
		Assert.True(typeof(GenericRecommendationProvider).GetField(nameof(GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT))!.IsLiteral);

		Assert.Contains("Authorization Code", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("Redirect URL", authorizationCode, StringComparison.Ordinal);
		Assert.Contains("sign in", authorizationCode, StringComparison.Ordinal);
		Assert.DoesNotContain("client-credentials grant", authorizationCode, StringComparison.OrdinalIgnoreCase);

		Assert.Contains("Client Credentials", clientCredentials, StringComparison.Ordinal);
		Assert.DoesNotContain("Redirect URL", clientCredentials, StringComparison.Ordinal);

		Assert.Contains(SecretManagementPointer.PART_B_NAME, authorizationCode, StringComparison.Ordinal);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, clientCredentials, StringComparison.Ordinal);
		Assert.Equal(authorizationCode, new GenericRecommendationProvider().DescribeFor(CredentialFlow.AuthorizationCode));
		Assert.Equal(clientCredentials, new GenericRecommendationProvider().DescribeFor(CredentialFlow.ClientCredentials));
	}

	private static string LineOf(string block, string fieldName)
	{
		string prefix = $"- {fieldName}: ";
		string? line = block.Split('\n').SingleOrDefault(candidate => candidate.StartsWith(prefix, StringComparison.Ordinal));

		Assert.NotNull(line);

		return line;
	}
}
