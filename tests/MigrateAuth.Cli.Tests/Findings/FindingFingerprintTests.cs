using System.Collections.Immutable;

using MigrateAuth.Cli.Findings;

using Xunit;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// THE FINGERPRINT: PATH, PATTERN CLASS, AND ORDINAL OF THAT CLASS'S OCCURRENCE WITHIN THE
/// FILE. Two fingerprints are equal exactly when all three parts are, and nothing in either of
/// them came from the file's text.
///
/// THE ORDINAL'S BEHAVIOUR HAS TWO HALVES AND BOTH ARE PINNED HERE:
///
/// - IT SURVIVES INSERTING OR DELETING LINES. Offsets move, line numbers move, the
///   fingerprint does not.
/// - IT DOES NOT SURVIVE INSERTING OR DELETING AN OCCURRENCE OF THE SAME CLASS EARLIER IN THE SAME
///   FILE. Every later occurrence of that class shifts by one, and the set that results is still
///   internally consistent - contiguous from one, in recognition order, with no two occurrences
///   sharing a fingerprint.
/// </summary>
public sealed class FindingFingerprintTests
{
	private const string _SUBJECT_FILE = "source/subject.ex1";
	private const int _LINE_LENGTH = 20;

	/// <summary>How far down the file the line-insertion case pushes everything.</summary>
	private const int _INSERTED_LINE_COUNT = 11;

	/// <summary>
	/// The ordinal counts occurrences OF ONE CLASS, so the second occurrence of a class is its
	/// second wherever it sits and whatever lies between the two.
	/// </summary>
	[Fact]
	public void TheOrdinalCountsOccurrencesOfOneClassWithinOneFile()
	{
		FindingHarness harness = new();
		string text = FindingHarness.FillerText(lineCount: 8, lineLength: _LINE_LENGTH);

		ImmutableArray<Finding> findings = FindingComposition.Compose(
		[
			harness.FileOf(
				_SUBJECT_FILE,
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 10),
				FindingHarness.ConstructAt(FindingHarness.HAND_ASSEMBLED_HEADER_PATTERN_CLASS, anchorOffset: 30),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 50))
		]);

		Assert.Equal(
			new[]
			{
				$"{_SUBJECT_FILE}#{FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS}#1",
				$"{_SUBJECT_FILE}#{FindingHarness.HAND_ASSEMBLED_HEADER_PATTERN_CLASS}#1",
				$"{_SUBJECT_FILE}#{FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS}#2"
			},
			FindingHarness.FingerprintsOf(findings));
	}

	/// <summary>
	/// The counter is per file. Two files each holding one occurrence of a class both carry ordinal
	/// one, and their fingerprints differ on the path alone - which is what makes the ordinal
	/// meaningful as a within-file discriminator rather than a second global sequence number.
	/// </summary>
	[Fact]
	public void TheOrdinalRestartsInEveryFile()
	{
		FindingHarness harness = new();
		string text = FindingHarness.FillerText(lineCount: 4, lineLength: _LINE_LENGTH);

		ImmutableArray<Finding> findings = FindingComposition.Compose(
		[
			harness.FileOf(
				"source/first.ex1",
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 10)),
			harness.FileOf(
				"source/second.ex1",
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 10))
		]);

		Assert.All(
			findings,
			finding => Assert.Equal(
				FindingFingerprint.FIRST_OCCURRENCE_ORDINAL,
				finding.Fingerprint.OccurrenceOrdinal));
		Assert.NotEqual(findings[0].Fingerprint, findings[1].Fingerprint);
		Assert.Equal(findings[0].Fingerprint.PatternClass, findings[1].Fingerprint.PatternClass);
	}

	/// <summary>
	/// EQUAL ON ALL THREE PARTS, AND DIFFERENT ON ANY ONE OF THEM. The three cases are built by
	/// varying exactly one component at a time, because a fingerprint that ignored a component
	/// would pass a test that varied two.
	/// </summary>
	[Fact]
	public void TwoFingerprintsAreEqualOnlyWhenAllThreePartsAre()
	{
		FindingHarness harness = new();

		FindingFingerprint subject = FingerprintOf(harness, _SUBJECT_FILE, FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, ordinal: 2);
		FindingFingerprint identical = FingerprintOf(harness, _SUBJECT_FILE, FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, ordinal: 2);
		FindingFingerprint otherPath = FingerprintOf(harness, "source/other.ex1", FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, ordinal: 2);
		FindingFingerprint otherClass = FingerprintOf(harness, _SUBJECT_FILE, FindingHarness.LEGACY_CLIENT_PATTERN_CLASS, ordinal: 2);
		FindingFingerprint otherOrdinal = FingerprintOf(harness, _SUBJECT_FILE, FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, ordinal: 3);

		Assert.Equal(subject, identical);
		Assert.Equal(subject.Text, identical.Text);
		Assert.NotEqual(subject, otherPath);
		Assert.NotEqual(subject, otherClass);
		Assert.NotEqual(subject, otherOrdinal);
		Assert.Equal(3, new[] { otherPath.Text, otherClass.Text, otherOrdinal.Text }.Except([subject.Text]).Count());
	}

	/// <summary>
	/// THE STABILITY UNDER LINE EDITS. The same three constructs, with eleven lines inserted above them, so
	/// that every offset and every line number moves and nothing else does. The line numbers are
	/// asserted to have moved, because a test where they had not would prove nothing.
	/// </summary>
	[Fact]
	public void InsertingLinesMovesEveryLineNumberAndNoFingerprint()
	{
		FindingHarness harness = new();

		ImmutableArray<Finding> before = FindingComposition.Compose(ThreeConstructsAfter(harness, insertedLines: 0));
		ImmutableArray<Finding> after = FindingComposition.Compose(
			ThreeConstructsAfter(harness, _INSERTED_LINE_COUNT));

		Assert.Equal(FindingHarness.FingerprintsOf(before), FindingHarness.FingerprintsOf(after));
		Assert.All(
			before.Zip(after),
			pair => Assert.Equal(pair.First.Line + _INSERTED_LINE_COUNT, pair.Second.Line));
	}

	/// <summary>
	/// THE LIMIT, PINNED AS EXPLICITLY AS THE STABILITY. A new occurrence of the same class is
	/// inserted before the existing ones, and what is asserted is:
	///
	/// - the occurrence BEFORE the insertion point keeps its fingerprint;
	/// - each occurrence AFTER it shifts by one, so it no longer carries the fingerprint it had,
	///   and the fingerprint it had now names the occurrence one place earlier. Both halves
	///   are asserted, because "the fingerprint changed" and "the old fingerprint went somewhere
	///   specific" are different claims and only the second one describes the scheme;
	/// - and the resulting set is still internally consistent: ordinals contiguous from one, in
	///   recognition order, no two of them equal.
	///
	/// That last property is the one that has to hold, because it is what the ordinal is FOR -
	/// resolving collision between two identical occurrences in one file.
	/// </summary>
	[Fact]
	public void InsertingAnEarlierOccurrenceOfTheSameClassShiftsTheOnesAfterItAndNoOthers()
	{
		FindingHarness harness = new();
		string text = FindingHarness.FillerText(lineCount: 10, lineLength: _LINE_LENGTH);

		ImmutableArray<Finding> before = FindingComposition.Compose(
		[
			harness.FileOf(
				_SUBJECT_FILE,
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 10),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 90),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 150))
		]);

		ImmutableArray<Finding> after = FindingComposition.Compose(
		[
			harness.FileOf(
				_SUBJECT_FILE,
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 10),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 50),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 90),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 150))
		]);

		Assert.Equal(before[0].Line, after[0].Line);
		Assert.Equal(before[0].Fingerprint, after[0].Fingerprint);

		Assert.Equal(before[1].Line, after[2].Line);
		Assert.NotEqual(before[1].Fingerprint, after[2].Fingerprint);
		Assert.Equal(before[1].Fingerprint, after[1].Fingerprint);

		Assert.Equal(before[2].Line, after[3].Line);
		Assert.NotEqual(before[2].Fingerprint, after[3].Fingerprint);
		Assert.Equal(before[2].Fingerprint, after[2].Fingerprint);

		Assert.Equal(
			Enumerable.Range(FindingFingerprint.FIRST_OCCURRENCE_ORDINAL, after.Length),
			after.Select(finding => finding.Fingerprint.OccurrenceOrdinal));
		Assert.Equal(after.Length, after.Select(finding => finding.Fingerprint).Distinct().Count());
	}

	/// <summary>
	/// Inserting an occurrence of a DIFFERENT class changes nothing, because the counter is per
	/// class as well as per file. This is the other half of the concession above: it bounds what is
	/// given up to occurrences of the same class rather than to any edit that adds a finding.
	/// </summary>
	[Fact]
	public void InsertingAnEarlierOccurrenceOfAnotherClassShiftsNothing()
	{
		FindingHarness harness = new();
		string text = FindingHarness.FillerText(lineCount: 10, lineLength: _LINE_LENGTH);

		ImmutableArray<Finding> before = FindingComposition.Compose(
		[
			harness.FileOf(
				_SUBJECT_FILE,
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 90),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 150))
		]);

		ImmutableArray<Finding> after = FindingComposition.Compose(
		[
			harness.FileOf(
				_SUBJECT_FILE,
				text,
				FindingHarness.ConstructAt(FindingHarness.HAND_ASSEMBLED_HEADER_PATTERN_CLASS, anchorOffset: 10),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 90),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 150))
		]);

		Assert.Equal(before[0].Fingerprint, after[1].Fingerprint);
		Assert.Equal(before[1].Fingerprint, after[2].Fingerprint);
	}

	/// <summary>
	/// THE STRING FORM IS READ FROM THE RIGHT, and this is the case that makes that a requirement
	/// rather than a preference: a path containing the separator character. The three parts are
	/// still recoverable, because a pattern class identifier and an ordinal cannot contain it.
	/// </summary>
	[Fact]
	public void TheStringFormSurvivesASeparatorCharacterInsideThePath()
	{
		FindingHarness harness = new();

		FindingFingerprint fingerprint = FingerprintOf(
			harness,
			"source/od#d name.ex1",
			FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS,
			ordinal: 4);

		string[] parts = fingerprint.Text.Split('#');

		Assert.Equal($"source/od#d name.ex1#{FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS}#4", fingerprint.Text);
		Assert.Equal("4", parts[^1]);
		Assert.Equal(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, parts[^2]);
		Assert.Equal("source/od#d name.ex1", string.Join('#', parts[..^2]));
	}

	private static FindingFingerprint FingerprintOf(
		FindingHarness harness,
		string relativePath,
		string patternClassIdentifier,
		int ordinal)
	{
		string text = FindingHarness.FillerText(lineCount: 10, lineLength: _LINE_LENGTH);
		int occurrenceCount = ordinal - FindingFingerprint.FIRST_OCCURRENCE_ORDINAL + 1;

		ImmutableArray<Finding> findings = FindingComposition.Compose(
		[
			harness.FileOf(
				relativePath,
				text,
				[
					.. Enumerable
						.Range(0, occurrenceCount)
						.Select(occurrence => FindingHarness.ConstructAt(
							patternClassIdentifier,
							anchorOffset: 10 + (occurrence * _LINE_LENGTH)))
				])
		]);

		return findings[^1].Fingerprint;
	}

	/// <summary>The same three constructs, pushed down the file by a given number of inserted lines.</summary>
	private static IEnumerable<RecognizedFile> ThreeConstructsAfter(FindingHarness harness, int insertedLines)
	{
		int offsetShift = insertedLines * (_LINE_LENGTH + 1);
		string text = FindingHarness.FillerText(lineCount: 10 + insertedLines, lineLength: _LINE_LENGTH);

		return
		[
			harness.FileOf(
				_SUBJECT_FILE,
				text,
				FindingHarness.ConstructAt(
					FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS,
					anchorOffset: offsetShift + 10),
				FindingHarness.ConstructAt(
					FindingHarness.HAND_ASSEMBLED_HEADER_PATTERN_CLASS,
					anchorOffset: offsetShift + 50),
				FindingHarness.ConstructAt(
					FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS,
					anchorOffset: offsetShift + 90))
		];
	}
}
