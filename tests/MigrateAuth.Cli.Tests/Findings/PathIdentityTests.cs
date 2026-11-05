using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// THE ADMITTED SET: it follows the ordering key whatever order
/// the files arrive in, and two raw paths that map to one key are told apart by a raw-ordinal
/// tie-break and a fingerprint disambiguator.
///
/// NO PATH THE PRODUCT BUILDS CAN COLLIDE ON A KEY, because the key is the separator-normalized
/// path and the path is already that. The collision cases therefore run on synthetic pairs and on
/// findings whose fingerprint key is set by hand; the load-time derivation is checked by stripping
/// the disambiguators and deriving them again, which is the whole of what a load does, and the
/// store round trip is checked on the real products of a real run.
/// </summary>
public sealed class PathIdentityTests
{
	private const int _LINE_LENGTH = 24;
	private const int _LINE_COUNT = 100;
	private const int _CONSTRUCTS_PER_FILE = 1_500;
	private const int _FILE_COUNT = 34;
	private const int _SHUFFLE_SEED = 20260928;
	private const int _SECOND_SHUFFLE_SEED = 7;
	private const string _FILE_NAME_FORMAT = "source/file-{0:D2}.ex1";
	private const string _COMPOSED_NAME = "source/café.ex1";
	private const string _DECOMPOSED_NAME = "source/café.ex1";
	private const string _SHARED_KEY_NAME = "source/shared.ex1";
	private const string _PLAIN_NAME = "source/plain.ex1";
	private const string _PATTERN_CLASS = FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS;

	/// <summary>
	/// With the global cap reached, the admitted set is the leading prefix of the files under
	/// the key - the cap falls inside the last admitted file here - whatever order they arrived in.
	/// Ascending, descending and two shuffles give the same findings, fingerprints and counters.
	/// </summary>
	[Fact]
	public void TheAdmittedSetIsTheKeyOrderedPrefixWhateverOrderTheFilesArriveIn()
	{
		FindingHarness harness = new();
		RecognizedFile[] files = [.. Enumerable.Range(0, _FILE_COUNT).Select(index => FileNumbered(harness, index))];

		ComposedFindingSet ascending = FindingComposition.ComposeWithCapOutcome(files);
		ComposedFindingSet[] permuted =
		[
			FindingComposition.ComposeWithCapOutcome(files.Reverse()),
			FindingComposition.ComposeWithCapOutcome(Shuffled(files, _SHUFFLE_SEED)),
			FindingComposition.ComposeWithCapOutcome(Shuffled(files, _SECOND_SHUFFLE_SEED))
		];

		AssertTheAdmittedSetIsThePrefix(ascending);
		Assert.All(permuted, composed =>
		{
			Assert.Equal(FindingHarness.IdentifiersOf(ascending.Findings), FindingHarness.IdentifiersOf(composed.Findings));
			Assert.Equal(FindingHarness.FingerprintsOf(ascending.Findings), FindingHarness.FingerprintsOf(composed.Findings));
			Assert.Equal(PlacesOf(ascending.Findings), PlacesOf(composed.Findings));
			Assert.Equal(ascending.CapOutcome, composed.CapOutcome);
		});
	}

	/// <summary>
	/// THE ORDER. Two raw paths mapping to one key order by raw-ordinal comparison, whichever
	/// was offered first: the decomposed spelling sorts before the composed one ordinally.
	/// </summary>
	[Fact]
	public void TwoRawPathsOfOneKeyKeepAStableOrderByRawOrdinalComparison()
	{
		FindingHarness harness = new();
		NormalizedPathKey sharedKey = NormalizedPathKey.From(harness.PathOf(_SHARED_KEY_NAME));

		FindingOrderKey decomposed = OrderKeyOf(sharedKey, _DECOMPOSED_NAME);
		FindingOrderKey composed = OrderKeyOf(sharedKey, _COMPOSED_NAME);
		FindingOrderKey[] sorted = [.. new[] { composed, decomposed }.Order()];
		FindingOrderKey[] sortedFromTheOtherSide = [.. new[] { decomposed, composed }.Order()];

		Assert.Equal([decomposed, composed], sorted);
		Assert.Equal(sorted, sortedFromTheOtherSide);
	}

	/// <summary>
	/// THE DERIVATION. The disambiguator numbers the raw paths of one key from one in raw
	/// ordinal order, whatever order they are supplied in, gives nothing to a key with one raw path,
	/// and gives nothing twice to a raw path supplied twice.
	/// </summary>
	[Fact]
	public void TheDisambiguatorNumbersCollidingRawPathsInRawOrdinalOrderAndLeavesOthersAlone()
	{
		(string KeyText, string RawPath)[] paths =
		[
			(_SHARED_KEY_NAME, _COMPOSED_NAME),
			(_PLAIN_NAME, _PLAIN_NAME),
			(_SHARED_KEY_NAME, _DECOMPOSED_NAME),
			(_SHARED_KEY_NAME, _COMPOSED_NAME)
		];

		Dictionary<string, int> forward = PathDisambiguation.Derive(paths);
		Dictionary<string, int> backward = PathDisambiguation.Derive(paths.Reverse());

		Assert.Equal(2, forward.Count);
		Assert.Equal(1, forward[_DECOMPOSED_NAME]);
		Assert.Equal(2, forward[_COMPOSED_NAME]);
		Assert.False(forward.ContainsKey(_PLAIN_NAME));
		Assert.Equal(
			forward.OrderBy(pair => pair.Key, StringComparer.Ordinal),
			backward.OrderBy(pair => pair.Key, StringComparer.Ordinal));
	}

	/// <summary>
	/// THE FINGERPRINT. Two findings whose raw paths share a key get two distinct
	/// fingerprints, the disambiguator follows the raw paths' order, and a fingerprint that needed
	/// none carries no disambiguator.
	/// </summary>
	[Fact]
	public void TwoFindingsOfOneKeyGetTwoDistinctFingerprintsAndAnUncollidedOneIsUnchanged()
	{
		ImmutableArray<Finding> findings = CollidingFindings();

		ImmutableArray<Finding> disambiguated = PathDisambiguation.Apply(findings);

		Assert.NotEqual(disambiguated[0].Fingerprint.Text, disambiguated[1].Fingerprint.Text);
		Assert.Equal($"{_SHARED_KEY_NAME}~2#{_PATTERN_CLASS}#1", disambiguated[0].Fingerprint.Text);
		Assert.Equal($"{_SHARED_KEY_NAME}~1#{_PATTERN_CLASS}#1", disambiguated[1].Fingerprint.Text);
		Assert.Equal(findings[2].Fingerprint.Text, disambiguated[2].Fingerprint.Text);
		Assert.Equal($"{_PLAIN_NAME}#{_PATTERN_CLASS}#1", disambiguated[2].Fingerprint.Text);
	}

	/// <summary>
	/// AFTER LOAD. A load rebuilds each fingerprint without a disambiguator and derives them
	/// over the whole set; doing that to the composed set gives the composed set back. A set with no
	/// collision is returned as it came.
	/// </summary>
	[Fact]
	public void DerivingTheDisambiguatorsAgainOverAStrippedSetGivesTheSameFingerprints()
	{
		ImmutableArray<Finding> composed = PathDisambiguation.Apply(CollidingFindings());
		ImmutableArray<Finding> stripped =
		[
			.. composed.Select(finding => finding with { Fingerprint = finding.Fingerprint with { PathDisambiguator = FindingFingerprint.NO_PATH_DISAMBIGUATOR } })
		];
		ImmutableArray<Finding> uncollided = [ReportHarness.FindingAt(1, _PLAIN_NAME, 1, _PATTERN_CLASS, ConfidenceLevel.High)];

		ImmutableArray<Finding> loaded = PathDisambiguation.Apply(stripped);

		Assert.Equal(FindingHarness.FingerprintsOf(composed), FindingHarness.FingerprintsOf(loaded));
		Assert.Equal(composed.AsEnumerable(), loaded.AsEnumerable());
		Assert.Equal(uncollided.AsEnumerable(), PathDisambiguation.Apply(uncollided).AsEnumerable());
	}

	/// <summary>
	/// THE STORE. The fingerprints a real run composed are the fingerprints the store gives
	/// back after a save and a load - the same text, in the same order - so the derivation at load
	/// changes nothing where nothing collides.
	/// </summary>
	[Fact]
	public void FingerprintsComposedByARunSurviveASaveAndALoad()
	{
		using PipelineHarness harness = PipelineHarness.Create();

		harness.WriteSourceFile("Root.cs", ConstructSource());
		harness.WriteSourceFile("zeta/Alpha.cs", ConstructSource());
		harness.WriteSourceFile("alpha/Zeta.cs", ConstructSource());
		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		SessionStateLoad reloaded = harness.ReloadState();

		Assert.True(reloaded.WasResumed, reloaded.RejectionReason.ToString());
		Assert.NotEmpty(harness.State.Findings);
		Assert.Equal(FindingHarness.FingerprintsOf(harness.State.Findings), FindingHarness.FingerprintsOf(reloaded.State.Findings));
		Assert.Equal(harness.State.Findings.AsEnumerable(), reloaded.State.Findings.AsEnumerable());
	}

	private static void AssertTheAdmittedSetIsThePrefix(ComposedFindingSet composed)
	{
		int cap = FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP;
		int lastFileIndex = _FILE_COUNT - 1;
		int expectedInLastFile = cap - (lastFileIndex * _CONSTRUCTS_PER_FILE);
		string lastFileName = string.Format(CultureInfo.InvariantCulture, _FILE_NAME_FORMAT, lastFileIndex);
		IGrouping<string, Finding>[] byPath = [.. composed.Findings.GroupBy(finding => finding.Path.Value, StringComparer.Ordinal)];

		Assert.Equal(cap, composed.Findings.Length);
		Assert.Equal(_FILE_COUNT, byPath.Length);
		Assert.Equal(expectedInLastFile, byPath.Single(group => group.Key == lastFileName).Count());
		Assert.True(composed.CapOutcome.GlobalCapTripped);
		Assert.Equal((_FILE_COUNT * _CONSTRUCTS_PER_FILE) - cap, composed.CapOutcome.GlobalOmittedCount);
		Assert.Equal(cap, composed.CapOutcome.RetainedCount);
	}

	private static (string Path, int Line, int Column)[] PlacesOf(ImmutableArray<Finding> findings)
	{
		return [.. findings.Select(finding => (finding.Path.Value, finding.Line, finding.Column))];
	}

	private static RecognizedFile FileNumbered(FindingHarness harness, int index)
	{
		RecognizedConstruct[] constructs =
		[
			.. Enumerable.Range(0, _CONSTRUCTS_PER_FILE).Select(offset => FindingHarness.ConstructAt(_PATTERN_CLASS, offset))
		];

		return harness.FileOf(
			string.Format(CultureInfo.InvariantCulture, _FILE_NAME_FORMAT, index),
			FindingHarness.FillerText(_LINE_COUNT, _LINE_LENGTH),
			constructs);
	}

	private static RecognizedFile[] Shuffled(RecognizedFile[] files, int seed)
	{
		Random random = new(seed);

		return [.. files.OrderBy(_ => random.Next())];
	}

	private static FindingOrderKey OrderKeyOf(NormalizedPathKey key, string rawPath)
	{
		return new FindingOrderKey
		{
			Path = key,
			Line = 1,
			Column = 1,
			PatternClassIdentifier = _PATTERN_CLASS,
			RawPath = rawPath
		};
	}

	/// <summary>
	/// Three findings: two whose raw paths differ but whose fingerprint keys are the same text, which
	/// no product-built path yields today, and one plain one.
	/// </summary>
	private static ImmutableArray<Finding> CollidingFindings()
	{
		NormalizedPathKey sharedKey = NormalizedPathKey.From(ReportHarness.PathOf(_SHARED_KEY_NAME));

		return
		[
			WithSharedKey(ReportHarness.FindingAt(1, _COMPOSED_NAME, 1, _PATTERN_CLASS, ConfidenceLevel.High), sharedKey),
			WithSharedKey(ReportHarness.FindingAt(2, _DECOMPOSED_NAME, 1, _PATTERN_CLASS, ConfidenceLevel.High), sharedKey),
			ReportHarness.FindingAt(3, _PLAIN_NAME, 1, _PATTERN_CLASS, ConfidenceLevel.High)
		];
	}

	private static Finding WithSharedKey(Finding finding, NormalizedPathKey sharedKey)
	{
		return finding with { Fingerprint = finding.Fingerprint with { Path = sharedKey } };
	}

	private static string ConstructSource()
	{
		return $"class Sample\n{{\n\tvoid A() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, \"secret\");\n}}\n";
	}
}
