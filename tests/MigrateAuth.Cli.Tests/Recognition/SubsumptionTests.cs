using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// SUBSUMPTION, IN BOTH DIRECTIONS: WHAT A DECLARED RELATION SUPPRESSES AND WHAT THE
/// ABSENCE OF ONE LETS THROUGH.
///
/// The construct is a legacy client constructed with a credential object, which matches both the
/// client class and the credential class. Emitting both double-counts one planted pattern, which
/// corrupts both the recall and the false-positive figures.
///
/// The second half matters as much as the first. Absent a declared relation both classes are
/// emitted, deliberately, so that a catalog's omission shows up as a doubled finding somebody
/// reads rather than as a dropped one nobody does.
/// </summary>
public sealed class SubsumptionTests
{
	private const string _CREDENTIAL_TYPE_PATTERN_CLASS = "EXM-001";
	private const string _HAND_ASSEMBLED_HEADER_PATTERN_CLASS = "EXM-002";
	private const string _LEGACY_CLIENT_PATTERN_CLASS = "EXM-003";

	private const int _TWO_FINDINGS = 2;

	/// <summary>One construct, matched by two classes whose spans overlap.</summary>
	private const string _ONE_CONSTRUCT_MATCHED_BY_TWO_CLASSES = """
		let client = NewExemplarClient(NewExemplarCredentials(operatorName, "hunter2"))
		""";

	/// <summary>
	/// The same overlapping construct, plus a second credential construction that no client
	/// wraps. The subsumption is a relation over PAIRS of matches and not over classes, so the
	/// unwrapped one has to survive.
	/// </summary>
	private const string _AN_OVERLAPPING_CONSTRUCT_AND_A_STANDALONE_ONE = """
		let client = NewExemplarClient(NewExemplarCredentials(operatorName, "hunter2"))
		let other = NewExemplarCredentials(serviceName, "hunter3")
		""";

	/// <summary>Two classes between which the catalog declares nothing.</summary>
	private const string _TWO_CLASSES_WITH_NO_RELATION_BETWEEN_THEM = """
		const AUTH_SCHEME = "Basic"
		let client = NewExemplarClient(NewExemplarCredentials(operatorName, "hunter2"))
		let request = SetHeader("Authorization", BuildScheme(AUTH_SCHEME))
		""";

	[Fact]
	public void OneConstructMatchedByTwoClassesWithADeclaredRelationEmitsOneFinding()
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(_ONE_CONSTRUCT_MATCHED_BY_TWO_CLASSES);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_LEGACY_CLIENT_PATTERN_CLASS, construct.PatternClassIdentifier);
	}

	/// <summary>
	/// The subsumed class is suppressed WHERE THE CONDITION HOLDS and nowhere else. A reduction
	/// that removed the class rather than the match would lose the second construction here, which
	/// is a real finding in a real file and has nothing to do with the first.
	/// </summary>
	[Fact]
	public void TheSuppressionIsPerPairAndNotPerClass()
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized =
			harness.Recognize(_AN_OVERLAPPING_CONSTRUCT_AND_A_STANDALONE_ONE);

		Assert.Equal(_TWO_FINDINGS, recognized.Length);
		Assert.Equal(_LEGACY_CLIENT_PATTERN_CLASS, recognized[0].PatternClassIdentifier);
		Assert.Equal(_CREDENTIAL_TYPE_PATTERN_CLASS, recognized[1].PatternClassIdentifier);
	}

	/// <summary>
	/// Two classes the catalog says nothing about, and both are emitted. The relation is asserted
	/// to be genuinely absent first, so that this does not quietly become a test of a relation
	/// whose condition happened not to hold.
	/// </summary>
	[Fact]
	public void TwoClassesWithNoDeclaredRelationBothEmit()
	{
		RecognitionHarness harness = new();

		Assert.DoesNotContain(
			harness.Analyzer.SubsumptionRelation,
			subsumption => RelatesTheseTwo(subsumption, _LEGACY_CLIENT_PATTERN_CLASS, _HAND_ASSEMBLED_HEADER_PATTERN_CLASS));

		ImmutableArray<RecognizedConstruct> recognized =
			harness.Recognize(_TWO_CLASSES_WITH_NO_RELATION_BETWEEN_THEM);

		Assert.Equal(_TWO_FINDINGS, recognized.Length);
		Assert.Contains(recognized, construct => construct.PatternClassIdentifier == _LEGACY_CLIENT_PATTERN_CLASS);
		Assert.Contains(recognized, construct => construct.PatternClassIdentifier == _HAND_ASSEMBLED_HEADER_PATTERN_CLASS);
	}

	/// <summary>
	/// The order key restricted to one file: anchor offset, then pattern class identifier. Nothing
	/// downstream should have to re-sort what this component returns, and nothing should ever see
	/// it in an order that depends on how a frozen dictionary enumerated.
	/// </summary>
	[Fact]
	public void WhatSurvivesIsOrderedOnTheKeyTheTotalOrderingUses()
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized =
			harness.Recognize(_TWO_CLASSES_WITH_NO_RELATION_BETWEEN_THEM);

		List<RecognizedConstruct> inTotalOrder =
		[
			.. recognized
				.OrderBy(construct => construct.AnchorOffset)
				.ThenBy(construct => construct.PatternClassIdentifier, StringComparer.Ordinal)
		];

		Assert.Equal(inTotalOrder, recognized.ToList());
	}

	private static bool RelatesTheseTwo(SubsumptionRule subsumption, string firstClass, string secondClass)
	{
		bool relates =
			(subsumption.SubsumingPatternClassIdentifier == firstClass
				&& subsumption.SubsumedPatternClassIdentifier == secondClass)
			|| (subsumption.SubsumingPatternClassIdentifier == secondClass
				&& subsumption.SubsumedPatternClassIdentifier == firstClass);

		return relates;
	}
}
