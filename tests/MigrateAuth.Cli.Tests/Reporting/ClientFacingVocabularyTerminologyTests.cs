using System.Collections.Immutable;

using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The base's counterpart of the CSharp catalog's
/// <c>NoDeclaredUnsupportedConstructOrReasonContainsInternalTerminology</c>: the client-facing
/// texts <see cref="ReportVocabulary"/> maps from engine vocabulary - one per skipped-item category
/// and one per session-state rejection reason - are held to the same list of internal fragments,
/// so an edit cannot silently reintroduce engineering language into what a client reads. "Detection
/// catalog" itself is not in the list: it is the deliverable's own name
/// and the version the banner prints, so only references into the catalog's internals are.
/// </summary>
public sealed class ClientFacingVocabularyTerminologyTests
{
	private static readonly ImmutableArray<string> _internalTerminologyFragments =
	[
		"per catalog", "catalog section", "Section ", "subsumption", "BCL", "CS-0", "catch", "rule loop",
		"suspected but not proven", "truncation counter", "DR-", "ER-", "CA" + "-", "R-CS", "inc DR",
		"lexical", "anchor", "match bound", "per-file", "profile", "ledger", "enum"
	];

	[Fact]
	public void NoSkippedItemCategoryDescriptionContainsInternalTerminology()
	{
		foreach (SkippedItemCategory category in SkippedItemCategories.ALL)
		{
			AssertCarriesNoInternalTerminology(ReportVocabulary.DescriptionOf(category));
		}
	}

	[Fact]
	public void NoSessionStateRejectionDescriptionContainsInternalTerminology()
	{
		foreach (SessionStateRejectionReason reason in Enum.GetValues<SessionStateRejectionReason>())
		{
			if (reason is not SessionStateRejectionReason.None and not SessionStateRejectionReason.FileAbsent)
			{
				AssertCarriesNoInternalTerminology(ReportVocabulary.DescriptionOf(reason));
			}
		}
	}

	private static void AssertCarriesNoInternalTerminology(string clientText)
	{
		foreach (string fragment in _internalTerminologyFragments)
		{
			Assert.DoesNotContain(fragment, clientText, StringComparison.OrdinalIgnoreCase);
		}
	}
}
