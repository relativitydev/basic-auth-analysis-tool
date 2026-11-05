using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE GENERIC RECOMMENDATION — THE TEXT READ BY WHOEVER HAS THE CONSTRUCT WE DID NOT FORESEE.
///
/// It is the far side of <see cref="IGenericRecommendationProvider"/>. It matters more than any
/// single template: a specific template is read by whoever has that construct, this one by
/// everyone whose construct the catalog does not cover, which over eight languages is the larger
/// population.
///
/// WHAT IT SAYS, IN A FIXED ORDER. It carries only what can be said without knowing the
/// construct — that the file performs Basic Authentication against RelativityOne and that Basic
/// Authentication is no longer documented for the REST API; that the destination is a bearer token
/// obtained from the platform's identity endpoint with the client-credentials grant; that the
/// credentials belong in the process environment or a secret store and never in source; and the
/// part no specific template needs, an explicit statement that the tool did not recognize this
/// construct and the change has not been characterized, so the migration is designed by whoever
/// owns the code rather than applied from the text.
///
/// THE TONE IS PART OF THE CONTENT. A generic recommendation that reads
/// like a specific one invites a developer to apply a transformation the tool never verified
/// applies, so it must be legible as a pointer, not as an instruction. That is why the text offers no
/// before-and-after form and names nothing about the construct — not its pattern class, not its
/// confidence level, not its language. Every one of those fields is available on the
/// <see cref="Finding"/> and every one of them, written into a sentence, would make the text read
/// as though something had been characterized. The fourth point would then be contradicted by the
/// three before it.
///
/// NO NETWORK CALL AND NO AI PROVIDER. The type holds no state, no client
/// and no dependency: what it returns is one of two compile-time constants, identical for every finding of a flow, and
/// <see cref="DescribeFor"/> is the whole implementation. There is nothing here that could reach a
/// network even if the rule were forgotten, which is the form of that guarantee this design prefers
/// — structural rather than observed.
/// </summary>
public sealed class GenericRecommendationProvider : IGenericRecommendationProvider
{
	/// <summary>
	/// The Client Credentials text, the four points listed above, in that order, as one continuous
	/// piece of prose.
	///
	/// It is a single narrative rather than four fields because the generic route is not a template:
	/// <see cref="RecommendationRenderer"/> carries it in <see cref="RenderedRecommendation.Narrative"/>
	/// exactly as it carries <see cref="RecommendationRenderer.MANUAL_REVIEW_TEXT"/>, and the four
	/// template pieces are left empty on that route. Splitting it into a source form and a target
	/// form would be the specific shape applied to the case that has no specific answer, which is
	/// the reading this text must avoid. It names the Part B pointer through <see cref="SecretManagementPointer"/>
	/// and carries no lifetime: the OAuth2 Client block the composer appends states it once.
	/// </summary>
	public const string CLIENT_CREDENTIALS_GENERIC_TEXT =
		"This file performs Basic Authentication against RelativityOne, and Basic Authentication is no longer documented for the REST API. The recommended destination is the Client Credentials flow: a bearer token obtained from the platform's identity endpoint with the client-credentials grant, and the client credentials that obtain that token belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source. Beyond those two facts this is a pointer and not an instruction: the tool did not recognize this construct, so the change has not been characterized and no before-and-after form for it is offered here. The migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>
	/// The Authorization Code text, written for a finding whose flow is Authorization Code and whose
	/// class has no template for it. It says what the flow changes for the user - the application
	/// sends the user to sign in and stops collecting or forwarding their password - and keeps the
	/// fourth point: the tool did not recognize the construct, and this is a pointer, not an
	/// instruction. It never describes the client-credentials grant, because that is the other flow's
	/// text, and it names the Part B pointer through <see cref="SecretManagementPointer"/>.
	/// </summary>
	public const string AUTHORIZATION_CODE_GENERIC_TEXT =
		"This file performs Basic Authentication against RelativityOne, and Basic Authentication is no longer documented for the REST API. The recommended destination is the Authorization Code flow: the application redirects the user to RelativityOne to sign in, receives an authorization code at a registered Redirect URL, and exchanges that code for a bearer token, so the application no longer collects or forwards the user's password. The client secret of the OAuth2 Client, and any refresh token, belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source. Beyond those facts this is a pointer and not an instruction: the tool did not recognize this construct, so the change has not been characterized and no before-and-after form for it is offered here. The migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>
	/// The generic recommendation for a finding whose pattern class carries no template for its
	/// flow: one of two constants, chosen by the flow alone.
	///
	/// WHY THE FINDING IS NOT AN INPUT, even though the relative path and the line
	/// number are known. Three reasons, and the first is decisive on its own.
	///
	/// 1. THE SIGNATURE CANNOT EXPRESS A CORRECT ESCAPE. A path is repository-chosen text and is
	///    escaped for its destination — <see cref="MigrateAuth.Cli.Emission.EmittedPathText"/>,
	///    which is what <see cref="RecommendationRenderer"/> calls when it substitutes one into a
	///    template. This method receives no destination, and the renderer passes a narrative through
	///    unsubstituted and unescaped, straight into the Markdown bullet. A path written here would
	///    therefore reach the report unescaped, or would force a second escaping mechanism beside
	///    the one that already exists. Both are worse than not substituting.
	///
	/// 2. THE LOCATION IS ALREADY ON THE PAGE. Every finding is rendered with its own File:Line
	///    column, so a path repeated inside the prose adds nothing a reader does not already have in
	///    front of them.
	///
	/// 3. A CONSTANT IS CHECKABLE. With no substitution at all, "this text contains nothing from the
	///    analyzed repository" is a property of the type rather than a property of a code path
	///    someone has to re-verify after every edit.
	///
	/// The composer that calls this is finding-free, so the interface takes the flow
	/// and nothing else: the Part C generated blocks and the User Guide compose without a finding.
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
