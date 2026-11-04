using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// A discarded session state's reason reaches the console as a sentence a client can act on, never as
/// the raw <see cref="SessionStateRejectionReason"/> member name such as <c>"SchemaVersionMismatch"</c>.
/// These tests prove <see cref="SessionStartup.ReportSessionStart"/>
/// prints <see cref="ReportVocabulary.DescriptionOf(SessionStateRejectionReason)"/>'s text for
/// every reason that can actually reach a client, and that the two reasons that are never a discard
/// print nothing.
/// </summary>
public sealed class SessionStartupRejectionMessageTests
{
	private static readonly SessionStateRejectionReason[] _discardableReasons =
	[
		SessionStateRejectionReason.FileUnreadable,
		SessionStateRejectionReason.FileTooLarge,
		SessionStateRejectionReason.DocumentMalformed,
		SessionStateRejectionReason.SchemaVersionMismatch,
		SessionStateRejectionReason.CatalogVersionMismatch,
		SessionStateRejectionReason.RepositoryLabelMismatch,
		SessionStateRejectionReason.CompletedStageUnknown,
		SessionStateRejectionReason.CompletedStagesNotPrefixClosed,
		SessionStateRejectionReason.CandidatePathNotContained,
		SessionStateRejectionReason.FindingNotWellFormed,
		SessionStateRejectionReason.ScanSummaryNotWellFormed,
		SessionStateRejectionReason.SkippedItemTallyNotWellFormed,
		SessionStateRejectionReason.FindingsCapOutcomeNotWellFormed
	];

	/// <summary>
	/// Every discardable reason prints its declared client text, in the sentence
	/// <see cref="SessionStartup.SESSION_STATE_DISCARDED_MESSAGE_FORMAT"/> already shapes, and never
	/// the bare enum member name.
	/// </summary>
	[Theory]
	[MemberData(nameof(DiscardableReasons))]
	public void EveryDiscardableReasonPrintsItsDeclaredClientTextAndNeverTheRawMemberName(
		SessionStateRejectionReason reason)
	{
		using StringWriter output = new();

		SessionStartup.ReportSessionStart(BuildDiscardedLoad(reason), BuildRepository(), output);

		string transcript = output.ToString();
		string expectedLine = string.Format(
			CultureInfo.InvariantCulture,
			SessionStartup.SESSION_STATE_DISCARDED_MESSAGE_FORMAT,
			ReportVocabulary.DescriptionOf(reason));

		Assert.Contains(expectedLine, transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(reason.ToString(), transcript, StringComparison.Ordinal);
	}

	/// <summary>The ordinary first run: no state file, so nothing was discarded and nothing is said about one.</summary>
	[Fact]
	public void AnAbsentStateFilePrintsNoDiscardLine()
	{
		using StringWriter output = new();

		SessionStateLoad load = new()
		{
			State = SessionState.Fresh(LanguageCatalog.CurrentVersion, "{repository}"),
			WasResumed = false,
			RejectionReason = SessionStateRejectionReason.FileAbsent
		};

		SessionStartup.ReportSessionStart(load, BuildRepository(), output);

		Assert.DoesNotContain("was not used", output.ToString(), StringComparison.Ordinal);
	}

	/// <summary>
	/// The accepted case never reaches the discard branch at all - <c>WasResumed</c> true is what
	/// <see cref="SessionStateRejectionReason.None"/> means, and the guard reads that flag first.
	/// </summary>
	[Fact]
	public void AnAcceptedStatePrintsNoDiscardLine()
	{
		using StringWriter output = new();

		SessionStateLoad load = new()
		{
			State = SessionState.Fresh(LanguageCatalog.CurrentVersion, "{repository}"),
			WasResumed = true,
			RejectionReason = SessionStateRejectionReason.None
		};

		SessionStartup.ReportSessionStart(load, BuildRepository(), output);

		Assert.DoesNotContain("was not used", output.ToString(), StringComparison.Ordinal);
	}

	public static TheoryData<SessionStateRejectionReason> DiscardableReasons()
	{
		TheoryData<SessionStateRejectionReason> data = [];

		foreach (SessionStateRejectionReason reason in _discardableReasons)
		{
			data.Add(reason);
		}

		return data;
	}

	private static SessionStateLoad BuildDiscardedLoad(SessionStateRejectionReason reason)
	{
		return new SessionStateLoad
		{
			State = SessionState.Fresh(LanguageCatalog.CurrentVersion, "{repository}"),
			WasResumed = false,
			RejectionReason = reason
		};
	}

	private static RepositoryFolderPath BuildRepository()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		return RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
	}
}
