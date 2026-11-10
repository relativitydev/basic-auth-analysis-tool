using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// THE PER-FILE CAP, GLOBAL CAP AND TRUNCATION COUNTER, OVER SYNTHETIC CONSTRUCT SETS BUILT THE
/// SAME WAY <see cref="FindingIdentityTests"/> BUILDS ITS THOUSAND-FINDING FIXTURE.
///
/// THE SETS ARE HAND-BUILT RATHER THAN RECOGNIZED, for the reason <see cref="FindingHarness"/>
/// gives for itself: what is under test is a property of the SET's shape - how many constructs one
/// file or one run may contribute - and not of how any construct came to be recognized. Building
/// tens of thousands of <see cref="RecognizedConstruct"/> values in memory is cheap; running that
/// many through the regex-based recognition engine is the volume proof's job, not this one's.
/// </summary>
public sealed class FindingsCapTests
{
	private const int _LINE_LENGTH = 16;
	private const string _OVER_CAP_MARKER_PATTERN_CLASS = FindingHarness.HAND_ASSEMBLED_HEADER_PATTERN_CLASS;

	/// <summary>
	/// ONE FILE OVER ITS OWN CAP, AND NOTHING ELSE NEAR EITHER LIMIT.
	///
	/// The file offers exactly five hundred more constructs than the per-file cap allows, all of the
	/// SAME pattern class so the marker class below is unambiguous. Only the per-file counter is
	/// expected to move: five hundred is nowhere near the global cap, so this is the case where one
	/// file's own overflow is the whole story.
	/// </summary>
	[Fact]
	public void OneFileOverItsOwnCapTripsThePerFileCounterAndNoOther()
	{
		FindingHarness harness = new();
		const int offeredCount = FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP + 500;

		ComposedFindingSet composed = FindingComposition.ComposeWithCapOutcome(
			OneFileOfferingSurvivorsThenAMarkedExcess(harness, offeredCount));

		Assert.Equal(FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP, composed.Findings.Length);
		Assert.Equal(offeredCount, composed.CapOutcome.TotalRecognizedCount);
		Assert.Equal(FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP, composed.CapOutcome.RetainedCount);
		Assert.Equal(1, composed.CapOutcome.PerFileCapTrippedFileCount);
		Assert.Equal(500, composed.CapOutcome.PerFileOmittedCount);
		Assert.False(composed.CapOutcome.GlobalCapTripped);
		Assert.Equal(0, composed.CapOutcome.GlobalOmittedCount);
		Assert.True(composed.CapOutcome.IsTruncated);
	}

	/// <summary>
	/// THE SURVIVOR SET IS THE FILE'S OWN LEADING PREFIX IN THE LOCAL ORDER, NOT AN ARRIVAL
	/// ACCIDENT. The offered set carries a marker class on every construct past the cap; if the cap
	/// admitted constructs out of order, at least one marked construct would survive alongside an
	/// unmarked one that did not.
	/// </summary>
	[Fact]
	public void ThePerFileCapKeepsTheLowestOffsetConstructsAndDropsTheMarkedExcess()
	{
		FindingHarness harness = new();
		const int offeredCount = FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP + 500;

		ComposedFindingSet composed = FindingComposition.ComposeWithCapOutcome(
			OneFileOfferingSurvivorsThenAMarkedExcess(harness, offeredCount));

		Assert.All(
			composed.Findings,
			finding => Assert.Equal(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, finding.PatternClass.Value));
		Assert.DoesNotContain(
			composed.Findings,
			finding => finding.PatternClass.Value == _OVER_CAP_MARKER_PATTERN_CLASS);
	}

	/// <summary>
	/// MANY FILES, EACH AT OR UNDER ITS OWN CAP, TOGETHER OVER THE GLOBAL BUDGET.
	///
	/// Twenty-five files of two thousand constructs each exactly spend the global cap; five more of
	/// the same size offer ten thousand further constructs that the run has no budget left for. No
	/// single file ever exceeds the per-file cap, so the per-file counters must stay at zero and the
	/// whole omission must land on the global counter.
	/// </summary>
	[Fact]
	public void ManyFilesTogetherTripTheGlobalCounterWithNoFileOverItsOwnCap()
	{
		FindingHarness harness = new();
		const int fileCount = 30;
		const int constructsPerFile = FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP;
		const int fittingFileCount = FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP / constructsPerFile;

		ComposedFindingSet composed = FindingComposition.ComposeWithCapOutcome(
			ManyFilesOfEqualSize(harness, fileCount, constructsPerFile));

		Assert.Equal(FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP, composed.Findings.Length);
		Assert.Equal(fileCount * constructsPerFile, composed.CapOutcome.TotalRecognizedCount);
		Assert.Equal(FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP, composed.CapOutcome.RetainedCount);
		Assert.Equal(0, composed.CapOutcome.PerFileCapTrippedFileCount);
		Assert.Equal(0, composed.CapOutcome.PerFileOmittedCount);
		Assert.True(composed.CapOutcome.GlobalCapTripped);
		Assert.Equal((fileCount - fittingFileCount) * constructsPerFile, composed.CapOutcome.GlobalOmittedCount);
		Assert.True(composed.CapOutcome.IsTruncated);
	}

	/// <summary>
	/// THE GLOBAL CAP'S SURVIVORS ARE THE LEADING FILES BY PATH, NOT AN ARRIVAL ACCIDENT. The files
	/// are offered in ascending path order - the order the persisted candidate set already
	/// guarantees - so the first twenty-five contribute every one of their findings and the last
	/// five contribute none at all.
	/// </summary>
	[Fact]
	public void TheGlobalCapKeepsTheEarliestFilesByPathAndDropsTheRestEntirely()
	{
		FindingHarness harness = new();
		const int fileCount = 30;
		const int constructsPerFile = FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP;
		const int fittingFileCount = FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP / constructsPerFile;

		ComposedFindingSet composed = FindingComposition.ComposeWithCapOutcome(
			ManyFilesOfEqualSize(harness, fileCount, constructsPerFile));

		string[] survivingPaths = [.. composed.Findings.Select(finding => finding.Path.Value).Distinct().Order(StringComparer.Ordinal)];

		Assert.Equal(fittingFileCount, survivingPaths.Length);
		Assert.Equal(FileNameOf(0), survivingPaths[0]);
		Assert.Equal(FileNameOf(fittingFileCount - 1), survivingPaths[^1]);
		Assert.DoesNotContain(composed.Findings, finding => finding.Path.Value == FileNameOf(fittingFileCount));
		Assert.DoesNotContain(composed.Findings, finding => finding.Path.Value == FileNameOf(fileCount - 1));
	}

	/// <summary>
	/// THE "STATE ZERO" CASE: a set nowhere near either cap trips neither counter, and every field
	/// the truncation declaration carries says so explicitly rather than by omission.
	/// </summary>
	[Fact]
	public void ASetWellUnderBothCapsIsNotTruncated()
	{
		FindingHarness harness = new();
		string text = FindingHarness.FillerText(lineCount: 12, lineLength: _LINE_LENGTH);

		IEnumerable<RecognizedFile> files =
		[
			harness.FileOf(
				"source/alpha.ex1",
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 10),
				FindingHarness.ConstructAt(FindingHarness.LEGACY_CLIENT_PATTERN_CLASS, anchorOffset: 30)),
			harness.FileOf(
				"source/zulu.ex1",
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 20))
		];

		ComposedFindingSet composed = FindingComposition.ComposeWithCapOutcome(files);

		Assert.Equal(3, composed.Findings.Length);
		Assert.Equal(3, composed.CapOutcome.TotalRecognizedCount);
		Assert.Equal(3, composed.CapOutcome.RetainedCount);
		Assert.Equal(0, composed.CapOutcome.PerFileCapTrippedFileCount);
		Assert.Equal(0, composed.CapOutcome.PerFileOmittedCount);
		Assert.False(composed.CapOutcome.GlobalCapTripped);
		Assert.Equal(0, composed.CapOutcome.GlobalOmittedCount);
		Assert.False(composed.CapOutcome.IsTruncated);
	}

	/// <summary>
	/// <see cref="FindingComposition.Compose"/> WITHOUT A CAP: a set this small is nowhere near either
	/// provisional value, so the simpler overload returns every construct as a finding.
	/// </summary>
	[Fact]
	public void ComposeWithoutTheCapOutcomeStillReturnsEveryFindingUnderTheCap()
	{
		FindingHarness harness = new();
		string text = FindingHarness.FillerText(lineCount: 4, lineLength: _LINE_LENGTH);

		ImmutableArray<Finding> findings = FindingComposition.Compose(
			[harness.FileOf("source/small.ex1", text, FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 5))]);

		Assert.Single(findings);
	}

	private static string FileNameOf(int fileIndex)
	{
		return $"source/file-{fileIndex:D3}.ex1";
	}

	/// <summary>
	/// One file offering more constructs than the per-file cap allows: the leading
	/// <see cref="FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP"/> of the credential-type
	/// class, packed at the lowest offsets, followed by five hundred more of a DIFFERENT class at
	/// higher offsets - the marker that lets a test tell a correctly-ordered admission from an
	/// arbitrary one.
	/// </summary>
	private static IEnumerable<RecognizedFile> OneFileOfferingSurvivorsThenAMarkedExcess(
		FindingHarness harness,
		int offeredCount)
	{
		string text = FindingHarness.FillerText(lineCount: offeredCount, lineLength: _LINE_LENGTH);

		RecognizedConstruct[] constructs =
		[
			.. Enumerable
				.Range(0, offeredCount)
				.Select(lineIndex => FindingHarness.ConstructAt(
					lineIndex < FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP
						? FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS
						: _OVER_CAP_MARKER_PATTERN_CLASS,
					anchorOffset: lineIndex * (_LINE_LENGTH + 1)))
		];

		return [harness.FileOf("source/one-over-cap-file.ex1", text, constructs)];
	}

	/// <summary>
	/// Several files of identical size, named so their path order is also their offered order - the
	/// shape the persisted candidate set already guarantees in production.
	/// </summary>
	private static IEnumerable<RecognizedFile> ManyFilesOfEqualSize(
		FindingHarness harness,
		int fileCount,
		int constructsPerFile)
	{
		string text = FindingHarness.FillerText(lineCount: constructsPerFile, lineLength: _LINE_LENGTH);

		for (int fileIndex = 0; fileIndex < fileCount; fileIndex++)
		{
			RecognizedConstruct[] constructs =
			[
				.. Enumerable
					.Range(0, constructsPerFile)
					.Select(lineIndex => FindingHarness.ConstructAt(
						FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS,
						anchorOffset: lineIndex * (_LINE_LENGTH + 1)))
			];

			yield return harness.FileOf(FileNameOf(fileIndex), text, constructs);
		}
	}
}
