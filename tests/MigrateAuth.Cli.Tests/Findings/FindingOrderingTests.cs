using System.Collections.Immutable;

using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// THE TOTAL ORDER KEY, TERM BY TERM: path, then line, then column, then pattern class identifier.
///
/// Each test below ties on every term before the one it is about, which is the only way to
/// demonstrate a key's PRECEDENCE rather than merely its contents - a comparison that consulted
/// the terms in the wrong order still sorts, and still sorts consistently, and is still wrong.
/// </summary>
public sealed class FindingOrderingTests
{
	private const int _LINE_LENGTH = 24;
	private const int _LINE_COUNT = 10;
	private const string _KEY_SOURCE_RELATIVE_PATH = "src/MigrateAuth.Cli/Findings/NormalizedPathKey.cs";

	/// <summary>
	/// The path is consulted first, and ordinally. Three files whose constructs sit at descending
	/// line numbers as written, so a comparison that reached for the line first would invert them.
	/// </summary>
	[Fact]
	public void ThePathIsTheFirstTermAndIsComparedOrdinally()
	{
		FindingHarness harness = new();

		ImmutableArray<Finding> findings = FindingComposition.Compose(
		[
			FileWithOneConstructAt(harness, "source/zulu.ex1", anchorOffset: 10),
			FileWithOneConstructAt(harness, "source/Alpha.ex1", anchorOffset: 130),
			FileWithOneConstructAt(harness, "source/alpha.ex1", anchorOffset: 80)
		]);

		Assert.Equal(
			["source/Alpha.ex1", "source/alpha.ex1", "source/zulu.ex1"],
			findings.Select(finding => finding.Path.Value));
	}

	/// <summary>
	/// The line is consulted before the column. Both constructs are in one file, the earlier line
	/// carrying the LATER column, so a comparison reaching for the column first would swap them.
	/// </summary>
	[Fact]
	public void TheLinePrecedesTheColumn()
	{
		FindingHarness harness = new();
		string text = FindingHarness.FillerText(_LINE_COUNT, _LINE_LENGTH);

		ImmutableArray<Finding> findings = FindingComposition.Compose(
		[
			harness.FileOf(
				"source/one.ex1",
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 20),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 26))
		]);

		Assert.Equal([(1, 21), (2, 2)], findings.Select(finding => (finding.Line, finding.Column)));
	}

	/// <summary>
	/// The pattern class identifier breaks a tie on path, line AND column - which is only reachable
	/// where two classes anchor at the same position, the case the subsumption relation exists
	/// for and does not always resolve: absent a declared relation both are emitted.
	/// </summary>
	[Fact]
	public void ThePatternClassIdentifierBreaksATieOnPathLineAndColumn()
	{
		FindingHarness harness = new();
		string text = FindingHarness.FillerText(_LINE_COUNT, _LINE_LENGTH);

		ImmutableArray<Finding> findings = FindingComposition.Compose(
		[
			harness.FileOf(
				"source/one.ex1",
				text,
				FindingHarness.ConstructAt(FindingHarness.LEGACY_CLIENT_PATTERN_CLASS, anchorOffset: 30),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 30))
		]);

		Assert.Equal(
			[FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, FindingHarness.LEGACY_CLIENT_PATTERN_CLASS],
			findings.Select(finding => finding.PatternClass.Value));
		Assert.All(findings, finding => Assert.Equal(findings[0].Line, finding.Line));
		Assert.All(findings, finding => Assert.Equal(findings[0].Column, finding.Column));
	}

	/// <summary>
	/// The same visible name in composed and in decomposed form is two paths and two keys: the key
	/// is separator normalization only, so the test host - which has the platform's normalization
	/// tables - keys exactly as the shipped binary, where the tables are absent, does.
	///
	/// Ordinally the decomposed name sorts BEFORE the plain name and the composed one AFTER it, so the
	/// two spellings land in different positions and each keeps its own spelling in the fingerprint.
	/// Nothing is folded.
	/// </summary>
	[Fact]
	public void TwoUnicodeFormsOfOnePathAreTwoDistinctKeysAndTwoPositions()
	{
		const string composedName = "source/caf\u00e9.ex1";
		const string decomposedName = "source/cafe\u0301.ex1";
		const string plainName = "source/cafz.ex1";

		FindingHarness harness = new();

		ImmutableArray<Finding> findings = FindingComposition.Compose(
		[
			FileWithOneConstructAt(harness, composedName, anchorOffset: 10),
			FileWithOneConstructAt(harness, plainName, anchorOffset: 10),
			FileWithOneConstructAt(harness, decomposedName, anchorOffset: 10)
		]);

		NormalizedPathKey composedKey = NormalizedPathKey.From(harness.PathOf(composedName));
		NormalizedPathKey decomposedKey = NormalizedPathKey.From(harness.PathOf(decomposedName));

		Assert.NotEqual(composedKey, decomposedKey);
		Assert.Equal(composedName, composedKey.Value);
		Assert.Equal(decomposedName, decomposedKey.Value);
		Assert.Equal(
			[decomposedName, plainName, composedName],
			findings.Select(finding => finding.Fingerprint.Path.Value));
		Assert.Equal(["001", "002", "003"], FindingHarness.IdentifiersOf(findings));
	}

	/// <summary>
	/// The key's own comparison, exercised directly rather than through a composed set, so that a
	/// consumer building one by hand is comparing what composition compares.
	/// </summary>
	[Fact]
	public void TheOrderKeyOfAFindingIsTheKeyTheOrderWasBuiltOn()
	{
		FindingHarness harness = new();
		string text = FindingHarness.FillerText(_LINE_COUNT, _LINE_LENGTH);

		ImmutableArray<Finding> findings = FindingComposition.Compose(
		[
			harness.FileOf(
				"source/one.ex1",
				text,
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 10),
				FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 60))
		]);

		Assert.True(findings[0].OrderKey.CompareTo(findings[1].OrderKey) < 0);
		Assert.Equal(0, findings[0].OrderKey.CompareTo(findings[0].OrderKey));
		Assert.Equal(findings[0].Fingerprint.Path, findings[0].OrderKey.Path);
	}

	/// <summary>
	/// THE KEY IS BUILT ON THE NORMALIZED PATH AND NOT ON THE EMITTED ONE. A finding's
	/// <see cref="Finding.Path"/> is what the report prints; its key's path is what the order was
	/// decided on. They agree wherever normalization changes nothing, which is every ASCII path,
	/// and this pins that they are sourced from one normalization rather than computed twice.
	/// </summary>
	[Fact]
	public void TheNormalizedKeyIsDerivedFromTheEmittedPath()
	{
		FindingHarness harness = new();
		RepositoryRelativePath path = harness.PathOf("source/one.ex1");

		NormalizedPathKey key = NormalizedPathKey.From(path);

		Assert.True(key.IsDeclared);
		Assert.Equal(path.Value, key.Value);
		Assert.Equal(key, NormalizedPathKey.From(path));
	}

	/// <summary>
	/// THE SOURCE HALF. The key applies no Unicode normalization: the file that builds it
	/// names no normalization call and no normalization form. The behavioural half is
	/// <see cref="TwoUnicodeFormsOfOnePathAreTwoDistinctKeysAndTwoPositions"/>; this half is what
	/// stops an edit adding the call on a host where it would pass unnoticed.
	/// </summary>
	[Fact]
	public void TheKeySourceContainsNoNormalizationCall()
	{
		string sourceText = File.ReadAllText(RepositorySourceFile.PathOf(_KEY_SOURCE_RELATIVE_PATH));

		Assert.DoesNotContain(".Normalize(", sourceText, StringComparison.Ordinal);
		Assert.DoesNotContain("NormalizationForm", sourceText, StringComparison.Ordinal);
	}

	/// <summary>
	/// The final term of the order key: two keys equal in every other term are ordered by the
	/// raw path compared ordinally, and the raw path is consulted last - a lower line beats a lower
	/// raw path.
	/// </summary>
	[Fact]
	public void TheRawPathIsTheFinalTermOfTheOrderKeyAndIsComparedOrdinally()
	{
		FindingHarness harness = new();
		NormalizedPathKey sharedKey = NormalizedPathKey.From(harness.PathOf("source/one.ex1"));
		const string decomposedRawPath = "source/cafe\u0301.ex1";
		const string composedRawPath = "source/caf\u00e9.ex1";

		FindingOrderKey decomposed = OrderKeyOf(sharedKey, line: 5, decomposedRawPath);
		FindingOrderKey composed = OrderKeyOf(sharedKey, line: 5, composedRawPath);
		FindingOrderKey earlierLineLaterRawPath = OrderKeyOf(sharedKey, line: 4, composedRawPath);

		Assert.True(decomposed.CompareTo(composed) < 0);
		Assert.True(composed.CompareTo(decomposed) > 0);
		Assert.Equal(0, decomposed.CompareTo(decomposed));
		Assert.True(earlierLineLaterRawPath.CompareTo(decomposed) < 0);
	}

	private static RecognizedFile FileWithOneConstructAt(
		FindingHarness harness,
		string relativePath,
		int anchorOffset)
	{
		return harness.FileOf(
			relativePath,
			FindingHarness.FillerText(_LINE_COUNT, _LINE_LENGTH),
			FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset));
	}

	private static FindingOrderKey OrderKeyOf(NormalizedPathKey key, int line, string rawPath)
	{
		return new FindingOrderKey
		{
			Path = key,
			Line = line,
			Column = 1,
			PatternClassIdentifier = FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS,
			RawPath = rawPath
		};
	}
}
