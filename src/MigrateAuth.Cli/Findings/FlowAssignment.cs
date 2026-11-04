using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Findings;

/// <summary>
/// WHICH OAUTH 2.0 FLOW A FINDING TARGETS, AS A PURE FUNCTION OF THE FILE'S FLOW EVIDENCE AND
/// THE EFFECTIVE DEFAULT FLOW.
///
/// It runs between composition and recording, which is what lets <see cref="FindingComposition"/>
/// stay free of configuration: composition copies the file's evidence onto each High and Medium
/// finding and this step alone reads the default. An
/// interactive signal wins, with or without a fixed one; otherwise a fixed-credential signal assigns
/// Client Credentials; otherwise the effective default applies and the decision is
/// <see cref="FlowDecision.Default"/>. A file whose flow-signal evaluation timed out carries
/// <see cref="FlowEvidence.None"/> and therefore lands in the last row, with its findings kept.
/// A Low finding carries no flow evidence and no flow: it stays in manual review.
///
/// IT NEVER READS <see cref="Finding.LiteralCredentialSignal"/>. A literal credential is a security
/// property and not evidence of which flow applies.
///
/// One finding at a time and no state: the same input gives the same output on every platform,
/// and the pass over a full set holds one extra finding of memory rather than a second set.
/// </summary>
public static class FlowAssignment
{
	/// <summary>The finding with its flow and decision assigned, or with both null where it is Low.</summary>
	public static Finding Assign(Finding finding, CredentialFlow effectiveDefaultFlow)
	{
		bool carriesFlow = finding.Level is ConfidenceLevel.High or ConfidenceLevel.Medium;
		Finding assigned;

		if (carriesFlow)
		{
			(CredentialFlow flow, FlowDecision decision) = Decide(
				finding.FlowEvidence ?? FlowEvidence.None,
				effectiveDefaultFlow);

			assigned = finding with { Flow = flow, FlowDecision = decision };
		}
		else
		{
			assigned = finding with { Flow = null, FlowDecision = null };
		}

		return assigned;
	}

	/// <summary>Every finding of the set assigned, in the set's own order.</summary>
	public static ImmutableArray<Finding> AssignAll(
		ImmutableArray<Finding> findings,
		CredentialFlow effectiveDefaultFlow)
	{
		ImmutableArray<Finding>.Builder assigned = ImmutableArray.CreateBuilder<Finding>(findings.Length);

		foreach (Finding finding in findings)
		{
			assigned.Add(Assign(finding, effectiveDefaultFlow));
		}

		return assigned.MoveToImmutable();
	}

	private static (CredentialFlow Flow, FlowDecision Decision) Decide(
		FlowEvidence evidence,
		CredentialFlow effectiveDefaultFlow)
	{
		(CredentialFlow Flow, FlowDecision Decision) decided = evidence switch
		{
			FlowEvidence.Interactive => (CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal),
			FlowEvidence.Fixed => (CredentialFlow.ClientCredentials, FlowDecision.FixedSignal),
			_ => (effectiveDefaultFlow, FlowDecision.Default)
		};

		return decided;
	}
}
