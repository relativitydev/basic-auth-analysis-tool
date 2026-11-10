using System.Globalization;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE ENGINE-OWNED OAUTH2 CLIENT BLOCK FOR EACH FLOW, THE FIFTH PIECE OF EVERY COMPOSED
/// RECOMMENDATION.
///
/// A recommendation for a High or Medium finding must tell the developer which OAuth2 Client to create
/// or confirm and how to configure it. That text does not depend on the language or the construct, so
/// it is owned here once rather than repeated inside every template of every language: a change to
/// what the tool says about a field is one edit, and a language analyzer cannot forget it.
/// <see cref="RecommendationComposer"/> appends the block to the composed text as a labelled piece,
/// in addition to the label set <see cref="RecommendationRenderer"/> documents.
///
/// EVERY CLAIM CARRIES ITS SOURCE, and the three sources are never blended into one voice. A fact the
/// vendor documents is labelled <see cref="RELATIVITY_DOCUMENTATION_LABEL"/>; a fact the tool's
/// authors checked against a running instance and the vendor does not document is labelled
/// <see cref="NSERIO_VERIFIED_LABEL"/>; and a choice that is guidance rather than fact is labelled
/// <see cref="NSERIO_RECOMMENDATION_LABEL"/>. A reader deciding whether to depart from a line can then
/// tell whether they would be departing from the vendor, from an observation, or from advice.
///
/// NO BLOCK CLAIMS THAT A FIELD IS MANDATORY. The vendor's documentation marks the fields its form
/// enforces and states a recommended value for the token lifetime without stating that it must be
/// filled in, so the blocks describe settings and recommend values and leave what the form enforces
/// to the form.
///
/// THE LIFETIME NUMBER COMES FROM <see cref="RECOMMENDED_ACCESS_TOKEN_LIFETIME_IN_MINUTES"/> AND FROM
/// NOWHERE ELSE, in either block and in every generic text the shared engine carries.
/// The number is stated in minutes because that is the unit of the field it configures.
/// </summary>
public static class OAuth2ClientConfigurationBlocks
{
	/// <summary>
	/// The one access-token lifetime this product recommends, in minutes. It matches the value the
	/// vendor's documentation recommends for both flows; it is the only lifetime literal in the shared engine.
	/// </summary>
	public const int RECOMMENDED_ACCESS_TOKEN_LIFETIME_IN_MINUTES = 60;

	/// <summary>The label a fact carries when the vendor's own documentation states it.</summary>
	public const string RELATIVITY_DOCUMENTATION_LABEL = "Relativity documentation";

	/// <summary>The label a fact carries when it was checked against a live instance and the vendor does not document it.</summary>
	public const string NSERIO_VERIFIED_LABEL = "verified by NSerio against a live instance; not documented by Relativity";

	/// <summary>The label a navigation path carries when NSerio observed it in the product and the vendor's pages describe it differently.</summary>
	public const string NSERIO_OBSERVATION_LABEL = "NSerio's observation";

	/// <summary>The label a choice carries when it is guidance and not a fact.</summary>
	public const string NSERIO_RECOMMENDATION_LABEL = "NSerio's recommendation";

	/// <summary>The field name of the OAuth2 Client's display name.</summary>
	public const string NAME_FIELD = "Name";

	/// <summary>The field name of the OAuth2 Client's flow.</summary>
	public const string FLOW_GRANT_TYPE_FIELD = "Flow Grant Type";

	/// <summary>The field name of the user every Client Credentials call runs as.</summary>
	public const string CONTEXT_USER_FIELD = "Context User";

	/// <summary>The field name of the access-token lifetime.</summary>
	public const string ACCESS_TOKEN_LIFETIME_FIELD = "Access Token Lifetime";

	/// <summary>The field name of the address restriction on token requests.</summary>
	public const string TRUSTED_IPS_FIELD = "Trusted IPs";

	/// <summary>The field name of the Authorization Code callback registration.</summary>
	public const string REDIRECT_URLS_FIELD = "Redirect URLs";

	/// <summary>The field name of the refresh-token switch.</summary>
	public const string ALLOW_REFRESH_TOKENS_FIELD = "Allow refresh tokens";

	/// <summary>The field name of the client's on/off switch.</summary>
	public const string ENABLED_FIELD = "Enabled";

	/// <summary>The Client Credentials value of <see cref="FLOW_GRANT_TYPE_FIELD"/>.</summary>
	public const string CLIENT_CREDENTIALS_GRANT_TYPE = "Client Credentials";

	/// <summary>The Authorization Code value of <see cref="FLOW_GRANT_TYPE_FIELD"/>, as the RelativityOne form names it.</summary>
	public const string AUTHORIZATION_CODE_GRANT_TYPE = "Code";

	/// <summary>
	/// The fixed-at-creation note of the Client Credentials block: the flow of an existing OAuth2
	/// Client cannot be edited, so a later move to Authorization Code is a new client.
	/// </summary>
	public const string FLOW_FIXED_AT_CREATION_NOTE =
		"Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.";

	/// <summary>The Context User permissions note of the Client Credentials block.</summary>
	public const string CONTEXT_USER_PERMISSIONS_NOTE =
		"Every call made with this OAuth2 Client runs with the Context User's permissions.";

	private const string _BLOCK_LINE_BREAK = "\n";
	private const string _BULLET_PREFIX = "- ";
	private const string _FIELD_FORMAT = "{0}: {1}";

	private const string _INTRODUCTION =
		"Create or confirm this OAuth2 Client in RelativityOne (" + NSERIO_OBSERVATION_LABEL + ": the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.";

	private const string _NAME_VALUE_CLIENT_CREDENTIALS =
		"a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.";

	private const string _NAME_VALUE_AUTHORIZATION_CODE =
		"a descriptive name identifying the application it serves.";

	private const string _CONTEXT_USER_VALUE =
		NSERIO_RECOMMENDATION_LABEL + ": a dedicated integration user with the minimum permissions the integration needs, "
		+ "and not a personal account and not an administrator, because every call runs with that user's permissions "
		+ "and an administrator Context User gives the integration an administrator's reach. "
		+ RELATIVITY_DOCUMENTATION_LABEL + " (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, "
		+ "though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.";

	private const string _LIFETIME_SOURCE_TEXT =
		RELATIVITY_DOCUMENTATION_LABEL + ": tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.";

	private const string _TRUSTED_IPS_VALUE =
		RELATIVITY_DOCUMENTATION_LABEL + ": the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. "
		+ NSERIO_RECOMMENDATION_LABEL + ": populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. "
		+ "It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, "
		+ "which travels in the request header and is valid from anywhere until it expires.";

	private const string _REFRESH_NOT_USED_VALUE =
		"not used by this flow. The integration requests a new access token when the current one expires.";

	private const string _REFRESH_OPTIONAL_VALUE =
		NSERIO_RECOMMENDATION_LABEL + ": leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. "
		+ "If it is on, the refresh tokens are stored as secrets, per " + SecretManagementPointer.PART_B_NAME + ".";

	private const string _ENABLED_VALUE = "on.";

	private const string _REDIRECT_URLS_VALUE =
		RELATIVITY_DOCUMENTATION_LABEL + ": each URL includes the http or https protocol prefix. "
		+ "The exact callback URL is registered by the Relativity administrator. "
		+ "Server-side web applications, " + NSERIO_RECOMMENDATION_LABEL + ": one exact HTTPS URL per environment, with no wildcards. "
		+ "Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime "
		+ "(" + NSERIO_VERIFIED_LABEL + ").";

	private static readonly string _lifetimeValue = string.Format(
		CultureInfo.InvariantCulture,
		"{0} minutes, stated in minutes because the field is in minutes. {1}",
		RECOMMENDED_ACCESS_TOKEN_LIFETIME_IN_MINUTES,
		_LIFETIME_SOURCE_TEXT);

	private static readonly string _clientCredentialsBlock = Join(
		_INTRODUCTION,
		Field(NAME_FIELD, _NAME_VALUE_CLIENT_CREDENTIALS),
		Field(FLOW_GRANT_TYPE_FIELD, CLIENT_CREDENTIALS_GRANT_TYPE),
		Field(CONTEXT_USER_FIELD, _CONTEXT_USER_VALUE),
		Field(ACCESS_TOKEN_LIFETIME_FIELD, _lifetimeValue),
		Field(TRUSTED_IPS_FIELD, _TRUSTED_IPS_VALUE),
		Field(ALLOW_REFRESH_TOKENS_FIELD, _REFRESH_NOT_USED_VALUE),
		Field(ENABLED_FIELD, _ENABLED_VALUE),
		Note(CONTEXT_USER_PERMISSIONS_NOTE),
		Note(FLOW_FIXED_AT_CREATION_NOTE));

	private static readonly string _authorizationCodeBlock = Join(
		_INTRODUCTION,
		Field(NAME_FIELD, _NAME_VALUE_AUTHORIZATION_CODE),
		Field(FLOW_GRANT_TYPE_FIELD, AUTHORIZATION_CODE_GRANT_TYPE),
		Field(REDIRECT_URLS_FIELD, _REDIRECT_URLS_VALUE),
		Field(ACCESS_TOKEN_LIFETIME_FIELD, _lifetimeValue),
		Field(ALLOW_REFRESH_TOKENS_FIELD, _REFRESH_OPTIONAL_VALUE),
		Field(ENABLED_FIELD, _ENABLED_VALUE));

	/// <summary>
	/// The block for one flow: an introduction line, then one bullet per field, in the order the
	/// RelativityOne form presents them. Total over <see cref="CredentialFlow"/>.
	/// </summary>
	public static string For(CredentialFlow flow)
	{
		string block = flow switch
		{
			CredentialFlow.ClientCredentials => _clientCredentialsBlock,
			CredentialFlow.AuthorizationCode => _authorizationCodeBlock,
			_ => throw new ArgumentOutOfRangeException(nameof(flow), flow, "No OAuth2 Client block is declared for this flow.")
		};

		return block;
	}

	private static string Field(string fieldName, string value)
	{
		return _BULLET_PREFIX + string.Format(CultureInfo.InvariantCulture, _FIELD_FORMAT, fieldName, value);
	}

	private static string Note(string text)
	{
		return _BULLET_PREFIX + text;
	}

	private static string Join(params string[] lines)
	{
		return string.Join(_BLOCK_LINE_BREAK, lines);
	}
}
