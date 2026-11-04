using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// The generic recommendation: the text read by whoever has a construct the catalog does not cover.
/// It implements <see cref="IGenericRecommendationProvider"/>.
///
/// It carries only what can be said without knowing the construct: that the file performs Basic
/// Authentication against RelativityOne and that Basic Authentication is no longer documented for the
/// REST API; that the destination is a bearer token obtained from the platform's identity endpoint
/// with the client-credentials grant; that the credentials belong in the process environment or a
/// secret store and never in source; and that the tool did not recognize this construct and the change
/// has not been characterized, so the migration is designed by whoever owns the code rather than
/// applied from the text.
///
/// The text is legible as a pointer, not as an instruction: it offers no before-and-after form and
/// names nothing about the construct - not its pattern class, not its confidence level, not its
/// language.
///
/// No network call and no AI provider. The type holds no state, no client and no dependency: what it
/// returns is one of two compile-time constants, identical for every finding of a flow, and
/// <see cref="DescribeFor"/> is the whole implementation.
/// </summary>
public sealed class GenericRecommendationProvider : IGenericRecommendationProvider
{
	/// <summary>
	/// The Client Credentials text, the four points of the generic recommendation in order as one
	/// continuous piece of prose.
	///
	/// It is a single narrative rather than four fields because the generic route is not a template:
	/// <see cref="RecommendationRenderer"/> carries it in <see cref="RenderedRecommendation.Narrative"/>
	/// exactly as it carries <see cref="RecommendationRenderer.MANUAL_REVIEW_TEXT"/>, and the four
	/// template pieces are left empty on that route. It names the Part B pointer through
	/// <see cref="SecretManagementPointer"/> and carries no lifetime: the OAuth2 Client block the composer
	/// appends states it once.
	/// </summary>
	public const string CLIENT_CREDENTIALS_GENERIC_TEXT =
		"This file performs Basic Authentication against RelativityOne, and Basic Authentication is no longer documented for the REST API. The recommended destination is the Client Credentials flow: a bearer token obtained from the platform's identity endpoint with the client-credentials grant, and the client credentials that obtain that token belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source. Beyond those two facts this is a pointer and not an instruction: the tool did not recognize this construct, so the change has not been characterized and no before-and-after form for it is offered here. The migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>
	/// The Authorization Code text, written for a finding whose flow is Authorization Code and whose
	/// class has no template for it. It says what the flow changes for the user - the application
	/// sends the user to sign in and stops collecting or forwarding their password - and keeps the
	/// generic text's fourth point: the tool did not recognize the construct, and this is a pointer, not
	/// an instruction. It never describes the client-credentials grant, because that is the other flow's
	/// text, and it names the Part B pointer through <see cref="SecretManagementPointer"/>.
	/// </summary>
	public const string AUTHORIZATION_CODE_GENERIC_TEXT =
		"This file performs Basic Authentication against RelativityOne, and Basic Authentication is no longer documented for the REST API. The recommended destination is the Authorization Code flow: the application redirects the user to RelativityOne to sign in, receives an authorization code at a registered Redirect URL, and exchanges that code for a bearer token, so the application no longer collects or forwards the user's password. The client secret of the OAuth2 Client, and any refresh token, belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source. Beyond those facts this is a pointer and not an instruction: the tool did not recognize this construct, so the change has not been characterized and no before-and-after form for it is offered here. The migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>
	/// The generic recommendation for a finding whose pattern class carries no template for its
	/// flow: one of two constants, chosen by the flow alone.
	///
	/// The finding is not an input. A path is repository-chosen text that must be escaped for its
	/// destination (<see cref="MigrateAuth.Cli.Emission.EmittedPathText"/>), and this method receives no
	/// destination; the location is already rendered beside every finding; and with no substitution at
	/// all, "this text contains nothing from the analyzed repository" is a property of the type. The
	/// Part C generated blocks and the User Guide compose without a finding.
	/// </summary>
	public string DescribeFor(CredentialFlow flow)
	{
		string text = flow switch
		{
			CredentialFlow.ClientCredentials => CLIENT_CREDENTIALS_GENERIC_TEXT,
			CredentialFlow.AuthorizationCode => AUTHORIZATION_CODE_GENERIC_TEXT,
			_ => throw new ArgumentOutOfRangeException(nameof(flow), flow, "No generic recommendation is declared for this flow.")
		};

		return text;
	}
}
