using System.Collections.Immutable;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// THE SEQUENTIAL IDENTIFIER, ITS PARALLEL NUMERIC INDEX, AND THE RUN-STABILITY REQUIREMENT AS THE LANGUAGE-AGNOSTIC ENGINE CAN HOLD IT.
///
/// The run-stability requirement - two runs over one repository produce identical order and identifiers - is
/// shown here for THE ORDERING PRIMITIVE AND THE IDENTITY SCHEME, which are stable and
/// platform-independent over a synthetic set. These tests do not run a real scan.
/// </summary>
public sealed class FindingIdentityTests
{
	private const int _MORE_FINDINGS_THAN_THREE_DIGITS_HOLD = 1002;
	private const int _LINE_LENGTH = 16;

	/// <summary>
	/// THE SAME INPUT TWICE, BUILT TWICE, WITH THE FILES ARRIVING IN OPPOSITE ORDERS.
	///
	/// Reversing the input is what makes this a test of the ORDERING rather than of the walk: a
	/// component that numbered in arrival order would pass a repeat of the identical sequence and
	/// fail this. Identifiers, fingerprints and order keys are all compared, because two runs
	/// agreeing on the sequence while disagreeing on what anything is called would satisfy neither
	/// the documented identifier and ordering rules.
	/// </summary>
	[Fact]
	public void TwoRunsOverOneSetProduceIdenticalOrderAndIdentifiers()
	{
		FindingHarness harness = new();

		ImmutableArray<Finding> first = FindingComposition.Compose(ThreeFilesIn(harness, reversed: false));
		ImmutableArray<Finding> second = FindingComposition.Compose(ThreeFilesIn(harness, reversed: true));

		Assert.Equal(FindingHarness.IdentifiersOf(first), FindingHarness.IdentifiersOf(second));
		Assert.Equal(FindingHarness.FingerprintsOf(first), FindingHarness.FingerprintsOf(second));
		Assert.Equal(first.Select(finding => finding.OrderKey), second.Select(finding => finding.OrderKey));
		Assert.Equal(first.ToArray(), second.ToArray());
	}

	/// <summary>
	/// The three-digit identifier, honoured, and the widening, which the three digits do not cover.
	///
	/// A repository sized for more than 50,000 files exceeds 999 findings easily, and the two
	/// failure modes a fixed width has are both worse than widening: wrapping reuses an identifier
	/// within one run, and truncating makes finding 1000 indistinguishable from finding 100.
	/// </summary>
	[Fact]
	public void TheIdentifierWidensPastThreeDigitsRatherThanTruncating()
	{
		FindingHarness harness = new();

		ImmutableArray<Finding> findings = FindingComposition.Compose(OneFileOfManyFindings(harness));

		Assert.Equal(_MORE_FINDINGS_THAN_THREE_DIGITS_HOLD, findings.Length);
		Assert.Equal("001", findings[0].Identifier);
		Assert.Equal("042", findings[41].Identifier);
		Assert.Equal("999", findings[998].Identifier);
		Assert.Equal("1000", findings[999].Identifier);
		Assert.Equal("1002", findings[^1].Identifier);
	}

	/// <summary>
	/// THE REASON THE PARALLEL NUMERIC INDEX EXISTS, DEMONSTRATED RATHER THAN ASSERTED. Sorting on
	/// the string identifier is lexicographic and places "1000" before "999"; sorting on the index
	/// does not. A consumer of the data artifact needs the second, which is why the
	/// artifact must carry both.
	/// </summary>
	[Fact]
	public void TheParallelNumericIndexOrdersNumericallyWhereTheIdentifierDoesNot()
	{
		FindingHarness harness = new();

		ImmutableArray<Finding> findings = FindingComposition.Compose(OneFileOfManyFindings(harness));

		Finding nineHundredNinetyNine = findings[998];
		Finding oneThousand = findings[999];

		Assert.Equal(999, nineHundredNinetyNine.SequentialIndex);
		Assert.Equal(1000, oneThousand.SequentialIndex);
		Assert.True(nineHundredNinetyNine.SequentialIndex < oneThousand.SequentialIndex);
		Assert.True(
			StringComparer.Ordinal.Compare(oneThousand.Identifier, nineHundredNinetyNine.Identifier) < 0,
			"The lexicographic order of the identifiers is what the numeric index exists to correct.");
	}

	/// <summary>
	/// THE INDEX IS CONTIGUOUS FROM ONE OVER THE FULL SET, which is the rule "numbering runs over
	/// the full ordered set" stated as a property. The accepted gaps appear in the OUTPUT
	/// after the threshold filter has removed rows; they cannot appear here, because this component does
	/// not filter and must not be the place a gap comes from.
	/// </summary>
	[Fact]
	public void EveryFindingIsNumberedAndNothingIsFilteredOut()
	{
		FindingHarness harness = new();

		ImmutableArray<Finding> findings = FindingComposition.Compose(ThreeFilesIn(harness, reversed: false));

		Assert.Equal(6, findings.Length);
		Assert.Equal(
			Enumerable.Range(FindingIdentifiers.FIRST_INDEX, findings.Length),
			findings.Select(finding => finding.SequentialIndex));
		Assert.All(findings, finding => Assert.Equal(FindingIdentifiers.Of(finding.SequentialIndex), finding.Identifier));
	}

	/// <summary>A repository with nothing in it composes to nothing, not to a null or a failure.</summary>
	[Fact]
	public void ARepositoryWithNoConstructsComposesToAnEmptySet()
	{
		ImmutableArray<Finding> findings = FindingComposition.Compose([]);

		Assert.Empty(findings);
	}

	/// <summary>
	/// Three files, six constructs, deliberately not in path order as written so that composition
	/// has something to reorder. Reversing the sequence is how the same set arrives differently.
	/// </summary>
	private static IEnumerable<RecognizedFile> ThreeFilesIn(FindingHarness harness, bool reversed)
	{
		string text = FindingHarness.FillerText(lineCount: 12, lineLength: _LINE_LENGTH);

		List<RecognizedFile> files =
		[
			harness.FileOf(
				"source/middle.ex1",
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 20),
				FindingHarness.ConstructAt(FindingHarness.HAND_ASSEMBLED_HEADER_PATTERN_CLASS, anchorOffset: 70)),
			harness.FileOf(
				"source/alpha.ex1",
				text,
				FindingHarness.ConstructAt(FindingHarness.LEGACY_CLIENT_PATTERN_CLASS, anchorOffset: 5),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 55)),
			harness.FileOf(
				"source/zulu.ex1",
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 40),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 90))
		];

		if (reversed)
		{
			files.Reverse();
		}

		return files;
	}

	/// <summary>
	/// One construct per line, over more lines than three digits hold. One file rather than many,
	/// so that the identifiers under test are decided by line alone and the assertion indices above
	/// are readable.
	/// </summary>
	private static IEnumerable<RecognizedFile> OneFileOfManyFindings(FindingHarness harness)
	{
		string text = FindingHarness.FillerText(
			lineCount: _MORE_FINDINGS_THAN_THREE_DIGITS_HOLD,
			lineLength: _LINE_LENGTH);

		RecognizedConstruct[] constructs =
		[
			.. Enumerable
				.Range(0, _MORE_FINDINGS_THAN_THREE_DIGITS_HOLD)
				.Select(lineIndex => FindingHarness.ConstructAt(
					FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS,
					anchorOffset: lineIndex * (_LINE_LENGTH + 1)))
		];

		return [harness.FileOf("source/many.ex1", text, constructs)];
	}
}
