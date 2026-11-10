using System.Globalization;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// How many of the reported High and Medium findings target each flow, and how many of them were
/// defaulted.
///
/// The set is the reported one - what remains after the minimum-confidence filter - and the same
/// three numbers are printed by the Suggest console and by the Markdown report from this one type,
/// so they cannot disagree. Client Credentials plus Authorization Code is exactly the High and Medium
/// findings the report lists; defaulted is a subset of that total, counted apart because a defaulted
/// finding is the one whose flow no signal in its file decided.
///
/// A finding not yet assigned a flow (a Low finding, which carries none) contributes to no count.
/// </summary>
public readonly record struct FlowCounts(int ClientCredentials, int AuthorizationCode, int Defaulted)
{
	/// <summary>The one sentence both surfaces print the three counts in.</summary>
	public const string MESSAGE_FORMAT =
		"Recommended flows: {0:N0} Client Credentials, {1:N0} Authorization Code; {2:N0} of them defaulted for lack of a signal in the file.";

	/// <summary>Client Credentials plus Authorization Code: every reported High or Medium finding.</summary>
	public int Total => ClientCredentials + AuthorizationCode;

	/// <summary>Counts the flows of an already-filtered sequence in one pass, retaining nothing.</summary>
	public static FlowCounts Over(IEnumerable<Finding> reportedFindings)
	{
		ArgumentNullException.ThrowIfNull(reportedFindings);

		int clientCredentials = 0;
		int authorizationCode = 0;
		int defaulted = 0;

		foreach (Finding finding in reportedFindings)
		{
			if (finding.Flow is CredentialFlow flow && finding.FlowDecision is FlowDecision decision)
			{
				clientCredentials += flow is CredentialFlow.ClientCredentials ? 1 : 0;
				authorizationCode += flow is CredentialFlow.AuthorizationCode ? 1 : 0;
				defaulted += decision is FlowDecision.Default ? 1 : 0;
			}
		}

		return new FlowCounts(clientCredentials, authorizationCode, defaulted);
	}

	/// <summary>The three counts as the sentence <see cref="MESSAGE_FORMAT"/> gives them.</summary>
	public string Describe()
	{
		return string.Format(CultureInfo.InvariantCulture, MESSAGE_FORMAT, ClientCredentials, AuthorizationCode, Defaulted);
	}
}
