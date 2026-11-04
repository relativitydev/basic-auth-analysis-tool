using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE <see cref="SkippedItemCategory.MalformedMatch"/> CATEGORY, GUARDING AGAINST A
/// <c>[GeneratedRegex]</c> SOURCE-GENERATOR DEFECT THE MATCH BOUND DOES NOT COVER.
///
/// The source generator can return a "successful" <see cref="Match"/> for
/// <c>a(?:(?:[^\n]*\n){0,5}?b)?</c> against <c>"za\n\n\n\n\n"</c> whose span does not contain its
/// own declared group: <see cref="Match.Length"/> ZERO with a participating, non-empty anchor
/// group the empty span cannot contain. Feeding the SAME trigger text to itself twice back to back
/// produces genuine non-advancing repetition, from a whole range of resumed search positions rather
/// than from one - see <see cref="TheEngineResumesPastAGenuinelyStuckMatchAndStillFindsALaterWellFormedOne"/>
/// for that shape and what the engine does about it.
///
/// SEVEN THINGS ARE PROVED. First, that the fixture actually reproduces
/// the defect on this SDK - if a future SDK update fixes the generator, that test fails and tells
/// the team to replace or retire this fixture rather than leave a guard nothing exercises. Second,
/// that the real engine drops the zero-length corrupted match, records that category exactly
/// once, never trips <see cref="SkippedItemCategory.MatchBoundExceeded"/>, and still finds a second,
/// well-formed rule's own finding in the same file. Third, that an anchor group legitimately outside
/// its own match's span - a lookahead or a lookbehind, per the analyzer contract - is classified
/// normally rather than dropped, proved through deterministic, non-generator-bug constructions
/// rather than through the buggy generator, because nothing about a lookaround depends on this SDK's
/// defect. Fourth, that a match which genuinely fails to advance does not abandon the rest of the
/// file for its rule: the engine resumes past the stuck position and still finds a later, well-formed
/// match of the SAME rule, with the file still counted exactly once. Fifth, that the resume mechanism
/// reaches the same correct outcome across MULTIPLE consecutive stuck attempts, not just one - see
/// <see cref="TheEngineResumesThroughMultipleConsecutiveStuckAttemptsAndStillFindsTheLaterWellFormedOne"/>.
/// Sixth, that the interpreted cross-check drops the NON-ZERO-LENGTH corrupted shape as
/// <see cref="SkippedItemCategory.MalformedMatch"/> too, with the genuinely well-formed
/// findings either side of it kept; see
/// <see cref="TheEngineDropsTheObservedNonZeroLengthMalformedMatchAndKeepsTheLegitimateFindingsAroundIt"/>.
/// Seventh, that a BARE, zero-length lookaround with no other content - a shape geometry alone
/// cannot tell apart from the defect - is classified rather than dropped; see
/// <see cref="APureZeroLengthLookaroundMatchIsNowClassifiedRatherThanDropped"/>.
/// </summary>
public sealed class MalformedMatchGuardTests
{
	private const string _ANCHOR_CAPTURE_GROUP = "construct";

	private const string _MALFORMED_PATTERN_CLASS = "MLF-001";
	private const string _WELL_FORMED_PATTERN_CLASS = "MLF-002";

	private const string _LOOKAHEAD_PATTERN_CLASS = "LOA-001";
	private const string _LOOKAHEAD_SAFE_PATTERN_CLASS = "LOA-002";

	private const string _LOOKBEHIND_PATTERN_CLASS = "LOB-001";
	private const string _LOOKBEHIND_SAFE_PATTERN_CLASS = "LOB-002";

	private const string _PURE_LOOKAHEAD_PATTERN_CLASS = "LOA-003";

	/// <summary>The minimal repro: five newlines exhausts the lazy bound.</summary>
	private const string _GENERATOR_DEFECT_TRIGGER_TEXT = "za\n\n\n\n\n";

	private const string _SAFE_MARKER_TEXT = "SAFEMARKER";

	private const string _PROBE_PROFILE_NAME = "MalformedMatchGuardProbe";
	private const string _PROBE_LANGUAGE_NAME = "MalformedMatchGuardProbeLanguage";
	private const string _PROBE_FILE_EXTENSION = ".probe";
	private const string _PROBE_CONSTANT_DECLARATION_PATTERN = @"^\s*const\s+(?<name>\w+)\s*=\s*""(?<value>[^""]*)""";
	private const string _PROBE_PRESELECTION_ANCHOR = "probe";
	private const string _PROBE_RELATIVE_PATH = "source/malformed-match-guard-probe.probe";

	// --------------------------------------------------------------------------------------------
	// Precondition: the fixture must reproduce the defect on THIS SDK, directly against the raw
	// expression, before anything about the engine is asked to prove something over it.
	// --------------------------------------------------------------------------------------------

	[Fact]
	public void TheFixtureExpressionReproducesTheGeneratorDefectOnThisSdk()
	{
		Regex expression = MalformedMatchFixturePatterns.ConstructThenOptionalBoundedLazyContinuation();
		System.Text.RegularExpressions.Match first = expression.Match(_GENERATOR_DEFECT_TRIGGER_TEXT);

		Assert.True(first.Success, "the fixture pattern did not even match - it can no longer stand in for the defect.");

		Group constructGroup = first.Groups[_ANCHOR_CAPTURE_GROUP];
		bool spanContainsGroup = constructGroup.Success
			&& constructGroup.Index >= first.Index
			&& constructGroup.Index + constructGroup.Length <= first.Index + first.Length;

		// On this SDK/runtime the corrupted match reports consuming NO text (Match.Length zero)
		// while its own participating anchor group claims a non-empty capture the empty span cannot
		// contain. Asserting the exact shape, rather than an either/or, is what makes this test fail
		// loudly - rather than silently pass on a coincidence - the day an SDK update changes what the
		// generator returns here.
		Assert.True(
			!spanContainsGroup,
			"The [GeneratedRegex] source-generator defect (a match whose span does not contain its " +
			"own declared group) was NOT reproduced on this SDK/runtime. If a newer SDK fixed it, " +
			"MalformedMatchFixturePatterns no longer demonstrates what this suite exercises and should " +
			"be replaced or retired, and the guard it exercises kept as defence in depth rather than as " +
			"a proof against a live defect.");
		Assert.True(
			first.Length == 0,
			"The corrupted match's own shape has changed on this SDK/runtime: it now reports consuming " +
			"non-empty text while still not containing its own anchor group. This is still an outside-span " +
			"anchor either way, so AnchorGroupParticipatesButLiesOutsideTheMatchSpan still triggers the " +
			"interpreted cross-check - but the SPECIFIC zero-length shape this test pins has changed and " +
			"needs review.");
	}

	/// <summary>
	/// THE SECOND SHAPE, PINNED SEPARATELY FROM THE ZERO-LENGTH ONE ABOVE.
	/// Immediately preceding the trigger with a genuinely well-formed, unseparated "a" changes what
	/// the generator's own <c>NextMatch</c> returns for the corrupted attempt: not a zero-length match
	/// this time, but a NON-zero-length one (Index 1, Length 1) whose own participating anchor group
	/// still sits entirely outside that span (Index 2, Length 1). The interpreted cross-check catches
	/// this shape too - see
	/// <see cref="TheEngineDropsTheObservedNonZeroLengthMalformedMatchAndKeepsTheLegitimateFindingsAroundIt"/>.
	/// </summary>
	[Fact]
	public void TheFixtureExpressionReproducesTheObservedNonZeroLengthShapeOnThisSdk()
	{
		Regex expression = MalformedMatchFixturePatterns.ConstructThenOptionalBoundedLazyContinuation();
		string text = "a" + _GENERATOR_DEFECT_TRIGGER_TEXT + "a";

		System.Text.RegularExpressions.Match first = expression.Match(text);
		Assert.True(first.Success && first.Index == 0 && first.Length == 1, "the leading, genuine \"a\" was not matched as expected.");

		System.Text.RegularExpressions.Match second = first.NextMatch();
		Group constructGroup = second.Groups[_ANCHOR_CAPTURE_GROUP];
		bool spanContainsGroup = constructGroup.Success
			&& constructGroup.Index >= second.Index
			&& constructGroup.Index + constructGroup.Length <= second.Index + second.Length;

		Assert.True(
			second.Success && second.Length == 1 && !spanContainsGroup,
			"The observed non-zero-length corruption (Index 1, Length 1, construct group entirely " +
			"outside that span) was NOT reproduced on this SDK/runtime. If a newer SDK fixed it, the " +
			"fixture this test and " +
			nameof(TheEngineDropsTheObservedNonZeroLengthMalformedMatchAndKeepsTheLegitimateFindingsAroundIt) +
			" exercise no longer demonstrates a live defect and should be reviewed.");
	}

	// --------------------------------------------------------------------------------------------
	// The real engine, the real generator defect: dropped, counted once, the rest of the file kept.
	// --------------------------------------------------------------------------------------------

	[Fact]
	public void TheEngineDropsTheMalformedMatchRecordsItOnceAndStillYieldsTheOtherRulesFinding()
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = BuildGeneratorDefectContext(harness);

		// An advancing clock, not a frozen one - so that IF THE GUARD WERE REMOVED, the infinite
		// NextMatch repetition still trips the pre-existing match bound after a bounded number of
		// fast, CPU-only reads instead of hanging the test suite forever: the failure becomes "a
		// MatchBoundExceeded nobody wanted" rather than a run that never returns. The step is a
		// small fraction of the bound rather than a large one - MatchBoundTests' own two-thirds
		// step is chosen to trip almost immediately, which is right for a test PROVING the bound
		// trips, but here the same step reaches the bound before this fixture's own well-formed
		// second rule ever gets to run, since the clock is read once per match attempt across
		// EVERY rule in the file, not just the malformed one. A small step gives the guard - which
		// needs only a handful of reads to end the malformed rule's own two-match enumeration -
		// comfortable headroom under the bound, while still reaching it well within a few thousand
		// reads if the guard is absent.
		RecognitionEngine engine = new(
			harness.EmissionPoint,
			new AdvancingTimeProvider(TimeSpan.FromMilliseconds(1)));

		ImmutableArray<RecognizedConstruct> recognized = engine.Recognize(context).Constructs;
		ImmutableArray<EmittedFact> skipped = harness.SkippedItems();

		EmittedFact skip = Assert.Single(skipped);
		Assert.Equal(SkippedItemCategory.MalformedMatch, skip.SkippedItem);
		Assert.DoesNotContain(skipped, fact => fact.SkippedItem == SkippedItemCategory.MatchBoundExceeded);

		RecognizedConstruct wellFormedFinding = Assert.Single(recognized);
		Assert.Equal(_WELL_FORMED_PATTERN_CLASS, wellFormedFinding.PatternClassIdentifier);
	}

	// --------------------------------------------------------------------------------------------
	// THE NON-ZERO-LENGTH CORRUPTION IS DROPPED TOO. The interpreted cross-check rejects
	// it regardless of length, resumes past it, and the two genuinely well-formed matches either
	// side of the corruption - the leading "a" at Index 0 and the trailing "a" at the text's own
	// last position - are both kept.
	// --------------------------------------------------------------------------------------------

	[Fact]
	public void TheEngineDropsTheObservedNonZeroLengthMalformedMatchAndKeepsTheLegitimateFindingsAroundIt()
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = BuildObservedNonZeroLengthResidualContext(harness);

		// An advancing clock, for the same reason as the other malformed-match tests: if the cross-
		// check or the resume it drives were ever removed or broken, the failure becomes a bounded
		// MatchBoundExceeded rather than a hang.
		RecognitionEngine engine = new(
			harness.EmissionPoint,
			new AdvancingTimeProvider(TimeSpan.FromMilliseconds(1)));

		ImmutableArray<RecognizedConstruct> recognized = engine.Recognize(context).Constructs;
		ImmutableArray<EmittedFact> skipped = harness.SkippedItems();

		EmittedFact skip = Assert.Single(skipped);
		Assert.Equal(SkippedItemCategory.MalformedMatch, skip.SkippedItem);
		Assert.DoesNotContain(skipped, fact => fact.SkippedItem == SkippedItemCategory.MatchBoundExceeded);

		Assert.Equal(2, recognized.Length);
		Assert.Contains(recognized, finding => finding.PatternClassIdentifier == _MALFORMED_PATTERN_CLASS && finding.AnchorOffset == 0);
		Assert.Contains(
			recognized,
			finding => finding.PatternClassIdentifier == _MALFORMED_PATTERN_CLASS
				&& finding.AnchorOffset == context.NormalizedText.Length - 1);
	}

	// --------------------------------------------------------------------------------------------
	// An anchor group legitimately outside its own match's span - a lookahead, then a lookbehind.
	// Both are ordinary, correctly-functioning matches under the analyzer contract; nothing about
	// either depends on this SDK's generator defect, which is why both are built as deterministic,
	// non-generator-bug constructions rather than against the buggy expression above. Both must be
	// CLASSIFIED, not dropped.
	// --------------------------------------------------------------------------------------------

	[Fact]
	public void ALookaheadAnchoredMatchIsClassifiedRatherThanDroppedAsMalformed()
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = BuildLookaheadContext(harness);
		RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());

		ImmutableArray<RecognizedConstruct> recognized = engine.Recognize(context).Constructs;
		ImmutableArray<EmittedFact> skipped = harness.SkippedItems();

		Assert.Empty(skipped);

		// "xyzxyz\n" carries two occurrences of x(?=(?<construct>yz)): both are found, proving the
		// rule's own enumeration is unaffected by an anchor sitting past the match's own span.
		Assert.Equal(2, recognized.Count(finding => finding.PatternClassIdentifier == _LOOKAHEAD_PATTERN_CLASS));
		Assert.Single(recognized, finding => finding.PatternClassIdentifier == _LOOKAHEAD_SAFE_PATTERN_CLASS);
	}

	[Fact]
	public void ALookbehindAnchoredMatchIsClassifiedRatherThanDroppedAsMalformed()
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = BuildLookbehindContext(harness);
		RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());

		ImmutableArray<RecognizedConstruct> recognized = engine.Recognize(context).Constructs;
		ImmutableArray<EmittedFact> skipped = harness.SkippedItems();

		Assert.Empty(skipped);

		RecognizedConstruct lookbehindFinding = Assert.Single(
			recognized,
			finding => finding.PatternClassIdentifier == _LOOKBEHIND_PATTERN_CLASS);
		Assert.Single(recognized, finding => finding.PatternClassIdentifier == _LOOKBEHIND_SAFE_PATTERN_CLASS);

		// The anchor offset resolved is the group's own index - "Foo"'s, at the very start of the
		// source text this fixture builds - which is what proves the engine reads a lookbehind
		// anchor by its own group rather than by the match's start (the literal "(" several
		// positions later).
		Assert.Equal(0, lookbehindFinding.AnchorOffset);
	}

	// --------------------------------------------------------------------------------------------
	// A PURE, ZERO-LENGTH LOOKAROUND IS CLASSIFIED, NOT DROPPED. This is a deterministic,
	// non-generator-bug construction (a bare lookahead with no other content) that geometry alone
	// (Match.Length zero, anchor group outside the empty span) cannot tell apart from the SDK
	// defect. The interpreted cross-check settles it: since the generator is not actually defective
	// on this shape, the interpreted attempt reproduces it byte-for-byte, and it is confirmed legitimate.
	// --------------------------------------------------------------------------------------------

	[Fact]
	public void APureZeroLengthLookaroundMatchIsNowClassifiedRatherThanDropped()
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = BuildPureZeroLengthLookaroundContext(harness);
		RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());

		ImmutableArray<RecognizedConstruct> recognized = engine.Recognize(context).Constructs;
		ImmutableArray<EmittedFact> skipped = harness.SkippedItems();

		Assert.Empty(skipped);

		RecognizedConstruct pureLookaheadFinding = Assert.Single(
			recognized,
			finding => finding.PatternClassIdentifier == _PURE_LOOKAHEAD_PATTERN_CLASS);
		Assert.Equal(0, pureLookaheadFinding.AnchorOffset);
		Assert.Single(recognized, finding => finding.PatternClassIdentifier == _WELL_FORMED_PATTERN_CLASS);
	}

	// --------------------------------------------------------------------------------------------
	// A match that GENUINELY fails to advance: the same trigger fed
	// to itself twice produces a corrupted result that keeps returning Index=1 from a whole range
	// of resumed search positions, not merely repeating a single one - so the resume must track its
	// own search origin rather than trust the position the matcher reports. Proves the file is
	// still counted exactly once, AND that a later, genuinely well-formed match of the SAME rule
	// past the stuck region is still found rather than abandoned.
	// --------------------------------------------------------------------------------------------

	[Fact]
	public void TheEngineResumesPastAGenuinelyStuckMatchAndStillFindsALaterWellFormedOne()
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = BuildResumeContext(harness);

		// An advancing clock, for the same reason as the zero-length test above: if the resume
		// guard were ever removed, the loop this fixture drives would trip the pre-existing match
		// bound rather than hang the suite. The step stays small so the guard's own bounded number
		// of resume attempts - at most the text's own length - finishes comfortably under it.
		RecognitionEngine engine = new(
			harness.EmissionPoint,
			new AdvancingTimeProvider(TimeSpan.FromMilliseconds(1)));

		ImmutableArray<RecognizedConstruct> recognized = engine.Recognize(context).Constructs;
		ImmutableArray<EmittedFact> skipped = harness.SkippedItems();

		EmittedFact skip = Assert.Single(skipped);
		Assert.Equal(SkippedItemCategory.MalformedMatch, skip.SkippedItem);
		Assert.DoesNotContain(skipped, fact => fact.SkippedItem == SkippedItemCategory.MatchBoundExceeded);

		RecognizedConstruct laterFinding = Assert.Single(
			recognized,
			finding => finding.PatternClassIdentifier == _MALFORMED_PATTERN_CLASS);

		// The fixture's source always ends in a single, genuine "a" at the very last position - the
		// well-formed match past the stuck region this test exists to prove is still found. Asserting
		// the resolved anchor lands exactly there (rather than merely asserting a finding exists)
		// rules out the resume having latched onto the stuck position instead.
		Assert.Equal(context.NormalizedText.Length - 1, laterFinding.AnchorOffset);

		Assert.Single(recognized, finding => finding.PatternClassIdentifier == _WELL_FORMED_PATTERN_CLASS);
	}

	// --------------------------------------------------------------------------------------------
	// THE RESUME MECHANISM THROUGH MULTIPLE CONSECUTIVE STUCK ATTEMPTS. The resume tries
	// Match(text, searchOrigin) - a different call than the NextMatch that produced the stuck
	// result - before advancing by one. Three copies of the trigger are chained together, rather
	// than two, so the resume loop this fixture drives runs more iterations of the same logic,
	// proving the mechanism still recovers the later, genuine finding after multiple consecutive
	// stuck attempts and still reports the file exactly once.
	// --------------------------------------------------------------------------------------------

	[Fact]
	public void TheEngineResumesThroughMultipleConsecutiveStuckAttemptsAndStillFindsTheLaterWellFormedOne()
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = BuildDeepResumeContext(harness);

		// A larger step than the single-stuck-attempt test above: this fixture drives more resume
		// iterations (three copies of the trigger chain together, rather than two), and the step
		// only needs to stay comfortably under the match bound divided by the number of match
		// attempts, which this shape still does by a wide margin.
		RecognitionEngine engine = new(
			harness.EmissionPoint,
			new AdvancingTimeProvider(TimeSpan.FromMilliseconds(1)));

		ImmutableArray<RecognizedConstruct> recognized = engine.Recognize(context).Constructs;
		ImmutableArray<EmittedFact> skipped = harness.SkippedItems();

		EmittedFact skip = Assert.Single(skipped);
		Assert.Equal(SkippedItemCategory.MalformedMatch, skip.SkippedItem);
		Assert.DoesNotContain(skipped, fact => fact.SkippedItem == SkippedItemCategory.MatchBoundExceeded);

		RecognizedConstruct laterFinding = Assert.Single(
			recognized,
			finding => finding.PatternClassIdentifier == _MALFORMED_PATTERN_CLASS);

		// Same shape of assertion as the single-stuck-attempt test above, on a deeper chain: the
		// fixture's source always ends in a single, genuine "a" at the very last position, past
		// THREE copies of the trigger rather than two - proving the resume mechanism's own loop,
		// not merely one iteration of it, still lands on the correct final position.
		Assert.Equal(context.NormalizedText.Length - 1, laterFinding.AnchorOffset);

		Assert.Single(recognized, finding => finding.PatternClassIdentifier == _WELL_FORMED_PATTERN_CLASS);
	}

	// --------------------------------------------------------------------------------------------
	// Fixture construction, mirroring MatchBoundTests' own probe-analyzer shape.
	// --------------------------------------------------------------------------------------------

	private static FileRecognitionContext BuildGeneratorDefectContext(RecognitionHarness harness)
	{
		string source = _GENERATOR_DEFECT_TRIGGER_TEXT + _SAFE_MARKER_TEXT + "\n";
		FrozenDictionary<string, DetectionRule> rules = new Dictionary<string, DetectionRule>(StringComparer.Ordinal)
		{
			[_MALFORMED_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.ConstructThenOptionalBoundedLazyContinuation(),
				"Malformed-match guard fixture: generator-defect repro"),
			[_WELL_FORMED_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.SafeMarker(),
				"Malformed-match guard fixture: well-formed sibling rule")
		}.ToFrozenDictionary(StringComparer.Ordinal);

		return BuildContext(harness, source, rules);
	}

	private static FileRecognitionContext BuildLookaheadContext(RecognitionHarness harness)
	{
		string source = "xyzxyz\n" + _SAFE_MARKER_TEXT + "\n";
		FrozenDictionary<string, DetectionRule> rules = new Dictionary<string, DetectionRule>(StringComparer.Ordinal)
		{
			[_LOOKAHEAD_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.ConstructInsideATrailingLookahead(),
				"Malformed-match guard fixture: construct captured inside a trailing lookahead"),
			[_LOOKAHEAD_SAFE_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.SafeMarker(),
				"Malformed-match guard fixture: well-formed sibling rule")
		}.ToFrozenDictionary(StringComparer.Ordinal);

		return BuildContext(harness, source, rules);
	}

	private static FileRecognitionContext BuildLookbehindContext(RecognitionHarness harness)
	{
		string source = "Foo(\n" + _SAFE_MARKER_TEXT + "\n";
		FrozenDictionary<string, DetectionRule> rules = new Dictionary<string, DetectionRule>(StringComparer.Ordinal)
		{
			[_LOOKBEHIND_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.ConstructInsideALeadingLookbehind(),
				"Malformed-match guard fixture: construct captured inside a leading lookbehind"),
			[_LOOKBEHIND_SAFE_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.SafeMarker(),
				"Malformed-match guard fixture: well-formed sibling rule")
		}.ToFrozenDictionary(StringComparer.Ordinal);

		return BuildContext(harness, source, rules);
	}

	/// <summary>
	/// The leading well-formed "a", immediately followed by the trigger and a single
	/// trailing "a" - no separator, matching the exact text the class-level doc and
	/// <see cref="TheFixtureExpressionReproducesTheObservedNonZeroLengthShapeOnThisSdk"/> pin. No
	/// well-formed sibling rule here: both findings this fixture exists to prove come from the SAME
	/// malformed pattern class, either side of the one match the cross-check drops.
	/// </summary>
	private static FileRecognitionContext BuildObservedNonZeroLengthResidualContext(RecognitionHarness harness)
	{
		string source = "a" + _GENERATOR_DEFECT_TRIGGER_TEXT + "a";
		FrozenDictionary<string, DetectionRule> rules = new Dictionary<string, DetectionRule>(StringComparer.Ordinal)
		{
			[_MALFORMED_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.ConstructThenOptionalBoundedLazyContinuation(),
				"Malformed-match guard fixture: observed non-zero-length corruption, leading and trailing well-formed matches")
		}.ToFrozenDictionary(StringComparer.Ordinal);

		return BuildContext(harness, source, rules);
	}

	/// <summary>
	/// A bare, zero-length lookahead with no other content - a shape geometry alone cannot tell
	/// apart from the defect, and that the cross-check classifies correctly.
	/// </summary>
	private static FileRecognitionContext BuildPureZeroLengthLookaroundContext(RecognitionHarness harness)
	{
		string source = "Foo\n" + _SAFE_MARKER_TEXT + "\n";
		FrozenDictionary<string, DetectionRule> rules = new Dictionary<string, DetectionRule>(StringComparer.Ordinal)
		{
			[_PURE_LOOKAHEAD_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.PureLookaheadZeroLength(),
				"Malformed-match guard fixture: bare zero-length lookahead, no other content"),
			[_WELL_FORMED_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.SafeMarker(),
				"Malformed-match guard fixture: well-formed sibling rule")
		}.ToFrozenDictionary(StringComparer.Ordinal);

		return BuildContext(harness, source, rules);
	}

	/// <summary>
	/// The same trigger the generator-defect context uses, fed to itself twice, which produces
	/// a genuinely non-advancing corrupted match rather than the single zero-length one above (see
	/// the class-level doc). The trailer places exactly one genuine, well-formed "a" past the stuck
	/// region, at the very end of the text, which is the finding
	/// <see cref="TheEngineResumesPastAGenuinelyStuckMatchAndStillFindsALaterWellFormedOne"/> asserts
	/// the engine still recovers.
	/// </summary>
	private static FileRecognitionContext BuildResumeContext(RecognitionHarness harness)
	{
		string source = _GENERATOR_DEFECT_TRIGGER_TEXT + _GENERATOR_DEFECT_TRIGGER_TEXT + _SAFE_MARKER_TEXT + "\na";
		FrozenDictionary<string, DetectionRule> rules = new Dictionary<string, DetectionRule>(StringComparer.Ordinal)
		{
			[_MALFORMED_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.ConstructThenOptionalBoundedLazyContinuation(),
				"Malformed-match guard fixture: generator-defect repro, fed to itself twice"),
			[_WELL_FORMED_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.SafeMarker(),
				"Malformed-match guard fixture: well-formed sibling rule")
		}.ToFrozenDictionary(StringComparer.Ordinal);

		return BuildContext(harness, source, rules);
	}

	/// <summary>
	/// The same trigger fed to itself THREE times, rather than
	/// twice, so the resume mechanism must run through multiple consecutive stuck attempts - each
	/// exercising the "try searchOrigin itself first, then advance by one" logic in
	/// <see cref="RecognitionEngine.TryResumePastAMalformedMatch"/> - before it reaches the trailer's
	/// single, genuine "a".
	/// </summary>
	private static FileRecognitionContext BuildDeepResumeContext(RecognitionHarness harness)
	{
		string source = _GENERATOR_DEFECT_TRIGGER_TEXT
			+ _GENERATOR_DEFECT_TRIGGER_TEXT
			+ _GENERATOR_DEFECT_TRIGGER_TEXT
			+ _SAFE_MARKER_TEXT
			+ "\na";
		FrozenDictionary<string, DetectionRule> rules = new Dictionary<string, DetectionRule>(StringComparer.Ordinal)
		{
			[_MALFORMED_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.ConstructThenOptionalBoundedLazyContinuation(),
				"Malformed-match guard fixture: generator-defect repro, fed to itself three times"),
			[_WELL_FORMED_PATTERN_CLASS] = BuildRule(
				MalformedMatchFixturePatterns.SafeMarker(),
				"Malformed-match guard fixture: well-formed sibling rule")
		}.ToFrozenDictionary(StringComparer.Ordinal);

		return BuildContext(harness, source, rules);
	}

	private static DetectionRule BuildRule(Regex expression, string displayName)
	{
		return new DetectionRule
		{
			Expression = expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(_ANCHOR_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			DestinationCertainty = DestinationCertainty.Established,
			DisplayName = displayName
		};
	}

	private static FileRecognitionContext BuildContext(
		RecognitionHarness harness,
		string source,
		FrozenDictionary<string, DetectionRule> rules)
	{
		LexicalProfile profile = BuildProbeProfile();
		ZoneMask mask = new LexicalScanner(profile).Scan(source);

		return new FileRecognitionContext
		{
			Analyzer = BuildProbeAnalyzer(profile, rules),
			NormalizedText = source,
			Mask = mask,
			Path = harness.PathOf(_PROBE_RELATIVE_PATH)
		};
	}

	private static ILanguageAnalyzer BuildProbeAnalyzer(LexicalProfile profile, FrozenDictionary<string, DetectionRule> rules)
	{
		Mock<ILanguageAnalyzer> analyzer = new();

		analyzer.SetupGet(supplied => supplied.LexicalProfile).Returns(profile);
		analyzer.SetupGet(supplied => supplied.LanguageIdentification).Returns(BuildProbeIdentification());
		analyzer.SetupGet(supplied => supplied.DetectionRulesByPatternClass).Returns(rules);
		analyzer.SetupGet(supplied => supplied.DemotionSignals).Returns(LanguageShapedDemotionSignals.NoneDeclared);
		analyzer.SetupGet(supplied => supplied.CandidateSelectionAndRecommendation)
			.Returns(BuildProbeCandidateSelection());
		analyzer.SetupGet(supplied => supplied.SubsumptionRelation).Returns(FrozenSet<SubsumptionRule>.Empty);

		return analyzer.Object;
	}

	private static LexicalProfile BuildProbeProfile()
	{
		return new LexicalProfile
		{
			ProfileName = _PROBE_PROFILE_NAME,
			ConstantDeclarationExpression = new Regex(_PROBE_CONSTANT_DECLARATION_PATTERN, RegexOptions.CultureInvariant),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = []
		};
	}

	private static LanguageIdentification BuildProbeIdentification()
	{
		return new LanguageIdentification
		{
			FileExtensions = new[] { _PROBE_FILE_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _PROBE_LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			LanguageNameDerivation = DeriveProbeLanguageName
		};
	}

	private static string DeriveProbeLanguageName(string fileExtension, string? firstLine)
	{
		return _PROBE_LANGUAGE_NAME;
	}

	private static CandidateSelectionAndRecommendation BuildProbeCandidateSelection()
	{
		return new CandidateSelectionAndRecommendation
		{
			PreselectionAnchors = new[] { new PreselectionAnchor { Text = _PROBE_PRESELECTION_ANCHOR } }.ToFrozenSet(),
			TemplateProvider = ProvideNoTemplate,
			FlowSignals = []
		};
	}

	private static RecommendationTemplate? ProvideNoTemplate(string patternClassIdentifier, CredentialFlow flow)
	{
		return null;
	}
}

/// <summary>
/// EXPRESSIONS DECLARED AS <c>[GeneratedRegex]</c> BECAUSE THE DEFECT THIS SUITE GUARDS AGAINST IS
/// A PROPERTY OF THE SOURCE GENERATOR, NEVER OF THE INTERPRETER - <c>new Regex(pattern)</c> reads
/// every pattern below correctly, which is exactly why the catalog's own rules must declare
/// <c>[GeneratedRegex]</c> to be exposed to it at all.
/// </summary>
internal static partial class MalformedMatchFixturePatterns
{
	/// <summary>
	/// The minimal repro: a required anchor group followed by an OPTIONAL lazy bounded loop,
	/// <c>(?:(?:[^\n]*\n){0,5}?...)?</c>, exhausted rather than satisfied. Against
	/// <c>"za\n\n\n\n\n"</c> the loop tries five lines looking for <c>b</c>, never finds it, and the
	/// SOURCE-GENERATED matcher - never the interpreter - returns a match whose span does not
	/// contain <c>construct</c>: <see cref="Match.Length"/> zero
	/// against a non-empty, participating group. Fed to itself twice back to back, the same
	/// expression instead produces genuine non-advancing repetition across a whole range of resumed
	/// search positions - see the resume fixture below and the class-level doc for both shapes.
	/// </summary>
	[GeneratedRegex(
		@"(?<construct>a)(?:(?:[^\n]*\n){0,5}?b)?",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ConstructThenOptionalBoundedLazyContinuation();

	/// <summary>
	/// A construct captured entirely inside a trailing, zero-width lookahead - legitimately outside
	/// the overall match's own span under ordinary, non-defective regex semantics. Deterministic
	/// and reproducible on any runtime: this is not the generator defect, it is a controlled way to
	/// present the engine with an anchor group outside the match's span WHILE the match itself
	/// consumes real text (<see cref="Match.Length"/> non-zero), which is an ordinary, legitimate
	/// shape under the analyzer contract and must be classified rather than dropped.
	/// </summary>
	[GeneratedRegex(
		@"x(?=(?<construct>yz))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ConstructInsideATrailingLookahead();

	/// <summary>
	/// The lookbehind mirror of <see cref="ConstructInsideATrailingLookahead"/>: a construct
	/// captured entirely inside a LEADING, zero-width lookbehind,
	/// found strictly before the literal <c>(</c> the match itself consumes. Equally legitimate,
	/// equally deterministic, and exercised separately because a lookahead and a lookbehind place
	/// the anchor on opposite sides of the match - a fix that only checked one side could still drop
	/// the other silently.
	/// </summary>
	[GeneratedRegex(
		@"(?<=(?<construct>Foo)\s*)\(",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ConstructInsideALeadingLookbehind();

	/// <summary>
	/// A BARE, zero-length lookahead with no other content at all - a shape that geometry alone
	/// cannot tell apart from the SDK defect (both report <see cref="Match.Length"/> zero with an
	/// uncontained, participating anchor group) and that the interpreted cross-check classifies
	/// correctly, since the generator is not actually defective on this shape and the interpreted
	/// attempt reproduces it byte-for-byte.
	/// </summary>
	[GeneratedRegex(
		@"(?=(?<construct>Foo))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex PureLookaheadZeroLength();

	/// <summary>The well-formed sibling every fixture context pairs a malformed rule with.</summary>
	[GeneratedRegex(
		@"(?<construct>SAFEMARKER)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex SafeMarker();
}
