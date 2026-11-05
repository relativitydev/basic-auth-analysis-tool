using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The composed `CSharp` recommendation under both flows, read from the real
/// embedded analyzer through <see cref="RecommendationComposer"/>: the Authorization Code elements,
/// the generic Authorization Code text and the
/// Client Credentials token acquisition, plus the flow-neutral part 1.
/// </summary>
public sealed class CSharpCodeTemplateTests
{
	private const string _SIGN_IN_MARKER = "**Sign in.**";
	private const string _RECEIVE_THE_CODE_MARKER = "**Receive the code.**";
	private const string _EXCHANGE_THE_CODE_MARKER = "**Exchange the code.**";
	private const string _USE_THE_TOKEN_MARKER = "**Use the token.**";
	private const string _ON_EXPIRY_MARKER = "**On expiry.**";
	private const string _PKCE_MARKER = "**PKCE.**";
	private const string _STOPS_COLLECTING_PASSWORD_MARKER = "**The application stops collecting the password.**";
	private const string _SINGLE_SIGN_ON_MARKER = "**Single sign-on.**";
	private const string _SERVER_SIDE_VARIANT_MARKER = "**Server-side variant.**";
	private const string _DESKTOP_VARIANT_MARKER = "**Desktop variant.**";
	private const string _LOOPBACK_BULLET_MARKER = "- **A loopback redirect on a fixed port.**";
	private const string _EXTRACTABLE_SECRET_MARKER = "**The secret is extractable.**";
	private const string _COMPENSATING_GUIDANCE_MARKER = "**Compensating guidance**";

	private const string _AUTHORIZE_ENDPOINT = "<host>/Relativity/Identity/connect/authorize";
	private const string _TOKEN_ENDPOINT = "<host>/Relativity/Identity/connect/token";
	private const string _LOOPBACK_REDIRECT_URL = "http://127.0.0.1:<port>/<path>";
	private const string _EXTRACTABLE_SECRET_STATEMENT = "a secret shipped inside a distributed binary can be extracted by anyone who holds the binary";
	private const string _PKCE_REFRESH_TOKEN_STATEMENT = "PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise";
	private const string _INFERRED = "inferred";
	private const string _EACH_END_USER = "each end user";
	private const string _CONTEXT_USER = "context user";
	private const string _GRANT_TYPE_CLIENT_CREDENTIALS = "grant_type=client_credentials";
	private const string _SYSTEM_USER_INFO_SCOPE = "`SystemUserInfo`";
	private const string _USER_INFO_ACCESS_SCOPE = "`UserInfoAccess`";
	private const string _NO_SINGLE_SCOPE_REQUIRED = "no single scope is required";
	private const string _RE_ACQUIRE_HEADING = "**Cache the token, and re-acquire it on expiry";
	private const string _DO_NOT_REFRESH = "do not refresh";
	private const string _REBUILD_SERVICE_FACTORY_ADVICE = "Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry.";
	private const string _SERVICE_FACTORY_IN_TARGET_FORM = "ServiceFactory";
	private const string _BEARER_TOKEN_CREDENTIALS_IN_TARGET_FORM = "BearerTokenCredentials";

	/// <summary>The catalog's "Generic fallback content for CSharp, Authorization Code", verbatim.</summary>
	private const string _CATALOG_CODE_FALLBACK_TEXT = "The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API, and this finding is assigned the Authorization Code flow, in which the application sends the user to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are verified by NSerio against a live instance; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source or tracked configuration; Code requires a client secret. After the change the application no longer collects or forwards the user's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	private static readonly ImmutableArray<string> _codeElementMarkers =
	[
		_SIGN_IN_MARKER,
		_RECEIVE_THE_CODE_MARKER,
		_EXCHANGE_THE_CODE_MARKER,
		_USE_THE_TOKEN_MARKER,
		_ON_EXPIRY_MARKER,
		_PKCE_MARKER,
		_STOPS_COLLECTING_PASSWORD_MARKER,
		_SINGLE_SIGN_ON_MARKER
	];

	/// <summary>All eight required elements are present, by their markers, in every class.</summary>
	[Theory]
	[MemberData(nameof(TemplatedPatternClasses))]
	public void EveryClassUnderAuthorizationCodeCarriesTheEightElementsThePartBPointerAndTheClientBlock(string patternClassIdentifier)
	{
		string text = CSharpRenderedTexts.FullText(patternClassIdentifier, CredentialFlow.AuthorizationCode);

		Assert.All(_codeElementMarkers, marker => Assert.Contains(marker, text, StringComparison.Ordinal));
		Assert.Contains(SecretManagementPointer.PART_B_NAME, text, StringComparison.Ordinal);
		Assert.Contains(RecommendationComposer.OAUTH2_CLIENT_LABEL, text, StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.For(CredentialFlow.AuthorizationCode), text, StringComparison.Ordinal);
	}

	/// <summary>The authorize endpoint carries the NSerio-verified label and is never called inferred.</summary>
	[Fact]
	public void TheAuthorizeEndpointIsLabelledVerifiedAndNeverInferred()
	{
		string text = CSharpRenderedTexts.FullText("CS-005", CredentialFlow.AuthorizationCode);
		string element = CSharpRenderedTexts.ParagraphStartingWith(text, _SIGN_IN_MARKER);

		Assert.Contains(_AUTHORIZE_ENDPOINT, element, StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL, element, StringComparison.Ordinal);
		Assert.DoesNotContain(_INFERRED, element, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The code exchange carries the NSerio-verified label and is never called inferred.</summary>
	[Fact]
	public void TheCodeExchangeIsLabelledVerifiedAndNeverInferred()
	{
		string text = CSharpRenderedTexts.FullText("CS-005", CredentialFlow.AuthorizationCode);
		string element = CSharpRenderedTexts.ParagraphStartingWith(text, _EXCHANGE_THE_CODE_MARKER);

		Assert.Contains(_TOKEN_ENDPOINT, element, StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL, element, StringComparison.Ordinal);
		Assert.DoesNotContain(_INFERRED, element, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>PKCE is required for a refresh token and not otherwise, and the not-otherwise half is labelled verified.</summary>
	[Fact]
	public void PkceIsPresentedAsRequiredForARefreshTokenAndNotOtherwise()
	{
		string text = CSharpRenderedTexts.FullText("CS-005", CredentialFlow.AuthorizationCode);
		string element = CSharpRenderedTexts.ParagraphStartingWith(text, _PKCE_MARKER);

		Assert.Contains(_PKCE_REFRESH_TOKEN_STATEMENT, element, StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL, element, StringComparison.Ordinal);
	}

	/// <summary>Both variants are present, distinct and in order in every class.</summary>
	[Theory]
	[MemberData(nameof(TemplatedPatternClasses))]
	public void TheServerSideAndDesktopVariantsAreDistinct(string patternClassIdentifier)
	{
		string text = CSharpRenderedTexts.FullText(patternClassIdentifier, CredentialFlow.AuthorizationCode);
		int serverIndex = text.IndexOf(_SERVER_SIDE_VARIANT_MARKER, StringComparison.Ordinal);
		int desktopIndex = text.IndexOf(_DESKTOP_VARIANT_MARKER, StringComparison.Ordinal);

		Assert.True(serverIndex >= 0);
		Assert.True(desktopIndex > serverIndex);

		string serverVariant = text[serverIndex..desktopIndex];

		Assert.DoesNotContain(_LOOPBACK_REDIRECT_URL, serverVariant, StringComparison.Ordinal);
		Assert.DoesNotContain(_EXTRACTABLE_SECRET_MARKER, serverVariant, StringComparison.Ordinal);
	}

	/// <summary>The desktop variant states the fixed-port loopback, the extractable secret and the compensating guidance.</summary>
	[Fact]
	public void TheDesktopVariantCarriesTheLoopbackTheExtractableSecretAndTheCompensatingGuidance()
	{
		string text = CSharpRenderedTexts.FullText("CS-005", CredentialFlow.AuthorizationCode);
		string desktopVariant = text[text.IndexOf(_DESKTOP_VARIANT_MARKER, StringComparison.Ordinal)..];
		string loopbackBullet = desktopVariant
			.Split('\n')
			.First(line => line.StartsWith(_LOOPBACK_BULLET_MARKER, StringComparison.Ordinal));

		Assert.Contains(_LOOPBACK_REDIRECT_URL, loopbackBullet, StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL, loopbackBullet, StringComparison.Ordinal);
		Assert.Contains(_EXTRACTABLE_SECRET_MARKER, desktopVariant, StringComparison.Ordinal);
		Assert.Contains(_EXTRACTABLE_SECRET_STATEMENT, desktopVariant, StringComparison.Ordinal);
		Assert.Contains(_COMPENSATING_GUIDANCE_MARKER, desktopVariant, StringComparison.Ordinal);
	}

	/// <summary>A C# Code finding routed to the generic route renders the catalog's generic Code text verbatim.</summary>
	[Fact]
	public void TheGenericAuthorizationCodeRouteRendersTheCatalogTextVerbatim()
	{
		RenderedRecommendation rendered = CSharpRenderedTexts.GenericRoute(CredentialFlow.AuthorizationCode);

		Assert.Equal(RecommendationKind.Generic, rendered.Kind);
		Assert.Equal(_CATALOG_CODE_FALLBACK_TEXT, rendered.Narrative);
		Assert.Equal(OAuth2ClientConfigurationBlocks.For(CredentialFlow.AuthorizationCode), rendered.OAuth2ClientBlock);
	}

	/// <summary>The generic Code text labels its endpoints and PKCE as the class text does, and never calls them inferred.</summary>
	[Fact]
	public void TheGenericAuthorizationCodeTextCarriesTheEndpointAndPkceLabels()
	{
		string narrative = CSharpRenderedTexts.GenericRoute(CredentialFlow.AuthorizationCode).Narrative;

		Assert.Contains(_AUTHORIZE_ENDPOINT, narrative, StringComparison.Ordinal);
		Assert.Contains(_TOKEN_ENDPOINT, narrative, StringComparison.Ordinal);
		Assert.Contains(OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL, narrative, StringComparison.Ordinal);
		Assert.Contains(_PKCE_REFRESH_TOKEN_STATEMENT, narrative, StringComparison.Ordinal);
		Assert.DoesNotContain(_INFERRED, narrative, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The Client Credentials part names the token endpoint and grant, re-acquires rather than refreshes, and states no single scope.</summary>
	[Theory]
	[MemberData(nameof(TemplatedPatternClasses))]
	public void TheClientCredentialsPartStatesEndpointGrantReacquisitionAndScope(string patternClassIdentifier)
	{
		string text = CSharpRenderedTexts.FullText(patternClassIdentifier, CredentialFlow.ClientCredentials);
		string reacquisition = CSharpRenderedTexts.ParagraphStartingWith(text, _RE_ACQUIRE_HEADING);

		Assert.Contains(_TOKEN_ENDPOINT, text, StringComparison.Ordinal);
		Assert.Contains(_GRANT_TYPE_CLIENT_CREDENTIALS, text, StringComparison.Ordinal);
		Assert.Contains(_DO_NOT_REFRESH, reacquisition, StringComparison.Ordinal);
		Assert.Contains(_SYSTEM_USER_INFO_SCOPE, text, StringComparison.Ordinal);
		Assert.Contains(_USER_INFO_ACCESS_SCOPE, text, StringComparison.Ordinal);
		Assert.Contains(_NO_SINGLE_SCOPE_REQUIRED, text, StringComparison.Ordinal);
	}

	/// <summary>Every class whose target form is built on the SDK's factory advises rebuilding it with a fresh token.</summary>
	[Fact]
	public void SdkClassesAdviseRebuildingTheServiceFactoryWithAFreshToken()
	{
		ILanguageAnalyzer analyzer = CSharpRenderedTexts.Analyzer();
		ImmutableArray<string> sdkClasses = CSharpRenderedTexts.TemplatedPatternClasses
			.Where(patternClassIdentifier => UsesTheSdkFactory(analyzer, patternClassIdentifier))
			.ToImmutableArray();

		Assert.Contains("CS-001", sdkClasses);
		Assert.Contains("CS-002", sdkClasses);

		foreach (string patternClassIdentifier in sdkClasses)
		{
			string text = CSharpRenderedTexts.FullText(patternClassIdentifier, CredentialFlow.ClientCredentials);

			Assert.Contains(_REBUILD_SERVICE_FACTORY_ADVICE, text, StringComparison.Ordinal);
		}
	}

	/// <summary>Part 1 is flow-neutral: no verification note claims whose identity the calls run as.</summary>
	[Fact]
	public void NoVerificationNoteAssertsTheContextUser()
	{
		ILanguageAnalyzer analyzer = CSharpRenderedTexts.Analyzer();

		foreach (CredentialFlow flow in CSharpRenderedTexts.Flows)
		{
			foreach (string patternClassIdentifier in CSharpRenderedTexts.TemplatedPatternClasses)
			{
				RecommendationTemplate template = analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, flow)!;

				Assert.DoesNotContain(_CONTEXT_USER, template.VerificationNote, StringComparison.OrdinalIgnoreCase);
			}
		}
	}

	/// <summary>The per-user wording occurs in no composed text and no generic text, for either flow.</summary>
	[Fact]
	public void NoRenderedTextSaysEachEndUser()
	{
		Assert.All(
			CSharpRenderedTexts.AllComposedTexts(),
			text => Assert.DoesNotContain(_EACH_END_USER, text, StringComparison.OrdinalIgnoreCase));
	}

	public static TheoryData<string> TemplatedPatternClasses()
	{
		TheoryData<string> data = [];

		foreach (string patternClassIdentifier in CSharpRenderedTexts.TemplatedPatternClasses)
		{
			data.Add(patternClassIdentifier);
		}

		return data;
	}

	private static bool UsesTheSdkFactory(ILanguageAnalyzer analyzer, string patternClassIdentifier)
	{
		string targetForm = analyzer.CandidateSelectionAndRecommendation
			.TemplateProvider(patternClassIdentifier, CredentialFlow.ClientCredentials)!
			.TargetFormUnderTokenAuthentication;

		return targetForm.Contains(_SERVICE_FACTORY_IN_TARGET_FORM, StringComparison.Ordinal)
			|| targetForm.Contains(_BEARER_TOKEN_CREDENTIALS_IN_TARGET_FORM, StringComparison.Ordinal);
	}
}
