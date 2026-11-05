using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// THE TEST-CODE MARKER OF A FINDING, CARRIED FROM THE CONSTRUCT TO THE FINDING AND NEVER INTO ITS IDENTITY.
///
/// The marker says the Test demotion kind fired on the finding's file. It is a field beside the level, so what it
/// must not do is as much the property under test as what it does: two runs over one repository, one of which marks
/// a file as test code and one of which does not, name every finding the same and order it the same, because a
/// fingerprint that moved with the marker would break the resume of a session whose catalog changed what counts as a
/// test.
/// </summary>
public sealed class TestCodeMarkerTests
{
	private const string _RELATIVE_PATH = "src/Credentials.cs";
	private const int _LINE_COUNT = 4;
	private const int _LINE_LENGTH = 16;
	private const int _FIRST_ANCHOR_OFFSET = 3;
	private const int _SECOND_ANCHOR_OFFSET = 40;

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void TheMarkerOfAConstructReachesItsFinding(bool testCode)
	{
		ImmutableArray<Finding> findings = Compose(testCode, ConfidenceLevel.Medium);

		Assert.All(findings, finding => Assert.Equal(testCode, finding.TestCode));
	}

	[Theory]
	[InlineData(ConfidenceLevel.High)]
	[InlineData(ConfidenceLevel.Medium)]
	[InlineData(ConfidenceLevel.Low)]
	public void TheMarkerIsCarriedAtEveryLevel(ConfidenceLevel level)
	{
		ImmutableArray<Finding> findings = Compose(testCode: true, level);

		Assert.All(findings, finding => Assert.True(finding.TestCode));
		Assert.All(findings, finding => Assert.Equal(level, finding.Level));
	}

	/// <summary>The identifier, the fingerprint and the order key of every finding are the same with the marker set and unset.</summary>
	[Fact]
	public void TheMarkerIsNotPartOfAFingerprintAnIdentifierOrTheOrder()
	{
		ImmutableArray<Finding> marked = Compose(testCode: true, ConfidenceLevel.Medium);
		ImmutableArray<Finding> unmarked = Compose(testCode: false, ConfidenceLevel.Medium);

		Assert.Equal(FindingHarness.IdentifiersOf(unmarked), FindingHarness.IdentifiersOf(marked));
		Assert.Equal(FindingHarness.FingerprintsOf(unmarked), FindingHarness.FingerprintsOf(marked));
		Assert.Equal(unmarked.Select(finding => finding.OrderKey), marked.Select(finding => finding.OrderKey));
		Assert.Equal(unmarked.Select(finding => finding.SequentialIndex), marked.Select(finding => finding.SequentialIndex));
	}

	private static ImmutableArray<Finding> Compose(bool testCode, ConfidenceLevel level)
	{
		FindingHarness harness = new();
		RecognizedFile file = harness.FileOf(
			_RELATIVE_PATH,
			FindingHarness.FillerText(_LINE_COUNT, _LINE_LENGTH),
			FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, _FIRST_ANCHOR_OFFSET, level, testCode: testCode),
			FindingHarness.ConstructAt(FindingHarness.LEGACY_CLIENT_PATTERN_CLASS, _SECOND_ANCHOR_OFFSET, level, testCode: testCode));

		return FindingComposition.Compose([file]);
	}
}
