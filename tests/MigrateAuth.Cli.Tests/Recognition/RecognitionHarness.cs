using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Fixtures;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE REFERENCE ANALYZER, A REAL SCAN, AND A PATH THAT WENT THROUGH THE WRITE BOUNDARY.
///
/// The analyzer is reached through the catalog rather than by naming its type, which is the same
/// route the golden test takes and for the same reason: the analyzer class is registered only
/// in-process by the test project, so no engine type can name it.
///
/// The mask is built by the real <see cref="FileLexicalScanner"/> rather than by a hand-written
/// array, because half of what the recognizer is being tested on is what it does with zones - an anchor
/// in a comment, an anchor in a literal body, a constant declaration that only counts in code - and
/// a fabricated mask would be testing the test's idea of the scanner.
///
/// The path goes through <see cref="RepositoryRelativePath.TryFromScannedPath"/> because that is
/// the only way to obtain one. It names a file that does not exist, which is deliberate and is
/// fine: containment is decided from the text of the path, and recognition never opens anything.
/// </summary>
internal sealed class RecognitionHarness
{
	private const string _REFERENCE_LANGUAGE_NAME = "ExemplarOne";
	private const string _REPOSITORY_ROOT = "recognition-harness-repository";

	/// <summary>A folder carrying no demotion signal from either owner, which most tests want.</summary>
	internal const string UNDEMOTED_RELATIVE_PATH = "source/module.ex1";

	private readonly RepositoryFolderPath _repository;

	internal RecognitionHarness()
	{
		_repository = RepositoryFolderPath.FromSuppliedText(Path.Combine(Path.GetTempPath(), _REPOSITORY_ROOT));
		Analyzer = ExemplarFixtureCatalog.Build().AnalyzersByLanguageName[_REFERENCE_LANGUAGE_NAME];
		Sink = new RecordingEmissionSink();
		EmissionPoint = new EmissionPoint([Sink]);
	}

	internal ILanguageAnalyzer Analyzer { get; }

	internal RecordingEmissionSink Sink { get; }

	internal EmissionPoint EmissionPoint { get; }

	/// <summary>Recognition on a clock that never moves, which is every test but the bound's own.</summary>
	internal ImmutableArray<RecognizedConstruct> Recognize(
		string sourceText,
		string relativePath = UNDEMOTED_RELATIVE_PATH)
	{
		return RecognizeOn(new FrozenTimeProvider(), sourceText, relativePath);
	}

	internal ImmutableArray<RecognizedConstruct> RecognizeOn(
		TimeProvider timeProvider,
		string sourceText,
		string relativePath = UNDEMOTED_RELATIVE_PATH)
	{
		RecognitionEngine engine = new(EmissionPoint, timeProvider);

		return engine.Recognize(ContextFor(sourceText, relativePath)).Constructs;
	}

	/// <summary>One file, scanned exactly as the pipeline scans it.</summary>
	internal FileRecognitionContext ContextFor(string sourceText, string relativePath = UNDEMOTED_RELATIVE_PATH)
	{
		string normalizedText = TextNormalization.Normalize(sourceText);
		RepositoryRelativePath path = PathOf(relativePath);
		ZoneMask mask = new FileLexicalScanner(EmissionPoint).Scan(Analyzer.LexicalProfile, normalizedText, path);

		return new FileRecognitionContext
		{
			Analyzer = Analyzer,
			NormalizedText = normalizedText,
			Mask = mask,
			Path = path
		};
	}

	/// <summary>The normalized form, which every offset a test asserts on indexes into.</summary>
	internal static string Normalize(string sourceText)
	{
		return TextNormalization.Normalize(sourceText);
	}

	internal RepositoryRelativePath PathOf(string relativePath)
	{
		if (!RepositoryRelativePath.TryFromScannedPath(_repository, relativePath, out RepositoryRelativePath path))
		{
			throw new InvalidOperationException($"The harness could not build a relative path from '{relativePath}'.");
		}

		return path;
	}

	/// <summary>Every skipped item this harness's emission point received, in arrival order.</summary>
	internal ImmutableArray<EmittedFact> SkippedItems()
	{
		return
		[
			.. Sink.Received.Where(fact => fact.Category is EmissionCategory.SkippedItem)
		];
	}
}

/// <summary>A sink that keeps what it was handed, so a test can assert on emissions rather than on prose.</summary>
internal sealed class RecordingEmissionSink : IEmissionSink
{
	public EmissionDestination Destination => EmissionDestination.StructuredData;

	internal List<EmittedFact> Received { get; } = [];

	public void Receive(EmittedFact fact)
	{
		Received.Add(fact);
	}
}

/// <summary>
/// A CLOCK THAT DOES NOT MOVE. Every test but the match bound's own is about classification, and a
/// classification test whose outcome could change because the machine was busy is a test that
/// eventually gets deleted rather than fixed.
/// </summary>
internal sealed class FrozenTimeProvider : TimeProvider
{
	public override long TimestampFrequency => TimeSpan.TicksPerSecond;

	public override long GetTimestamp()
	{
		return 0;
	}
}

/// <summary>
/// A CLOCK THAT MOVES BY A FIXED STEP EVERY TIME IT IS READ, which is what makes the match bound
/// trip DETERMINISTICALLY rather than on a slow machine.
///
/// The bound is polled between match attempts, so a provider whose every read advances the clock
/// turns "how long did matching take" - which no test can control - into "how many times was the
/// clock read", which the engine's own structure decides and which is identical on every machine.
/// </summary>
internal sealed class AdvancingTimeProvider : TimeProvider
{
	private readonly long _stepInTicks;

	private long _timestamp;

	internal AdvancingTimeProvider(TimeSpan step)
	{
		_stepInTicks = step.Ticks;
	}

	public override long TimestampFrequency => TimeSpan.TicksPerSecond;

	public override long GetTimestamp()
	{
		long current = _timestamp;

		_timestamp += _stepInTicks;

		return current;
	}
}

/// <summary>
/// A CLOCK THAT ADVANCES BY ONE STEP BETWEEN THE TWO READS OF A
/// SINGLE <c>MatchBoundWatch.BeginMatchAttempt</c>/<c>EndMatchAttempt</c> PAIR, AND BY A DIFFERENT
/// STEP BETWEEN ONE PAIR AND THE NEXT.
///
/// <see cref="AdvancingTimeProvider"/> advances by the same step on every read, so it cannot tell
/// "big step, few reads" apart from "small step, many reads" - both just accumulate. This
/// provider separates how much time elapses WITHIN one attempt (what the watch charges) from how
/// much elapses BETWEEN attempts (what nothing reads, because <see cref="MigrateAuth.Cli.Recognition.MatchBoundWatch.HasBeenExceeded"/>
/// is a pure field comparison rather than a clock read). The first read of a pair is a
/// <c>BeginMatchAttempt</c>; the step applied immediately after it governs what the paired
/// <c>EndMatchAttempt</c> read reports as that attempt's own elapsed time. The step applied after the
/// second read governs the gap to the next attempt's own <c>BeginMatchAttempt</c> - a gap the
/// watch never reads at all.
/// </summary>
internal sealed class PairedReadingTimeProvider : TimeProvider
{
	private readonly long _stepBetweenAttemptsInTicks;
	private readonly long _stepWithinAttemptInTicks;

	private long _value;
	private bool _nextIncrementIsWithinAttempt = true;

	internal PairedReadingTimeProvider(TimeSpan stepBetweenAttempts)
		: this(stepBetweenAttempts, TimeSpan.Zero)
	{
	}

	internal PairedReadingTimeProvider(TimeSpan stepBetweenAttempts, TimeSpan stepWithinAttempt)
	{
		_stepBetweenAttemptsInTicks = stepBetweenAttempts.Ticks;
		_stepWithinAttemptInTicks = stepWithinAttempt.Ticks;
	}

	public override long TimestampFrequency => TimeSpan.TicksPerSecond;

	public override long GetTimestamp()
	{
		long current = _value;

		_value += _nextIncrementIsWithinAttempt ? _stepWithinAttemptInTicks : _stepBetweenAttemptsInTicks;
		_nextIncrementIsWithinAttempt = !_nextIncrementIsWithinAttempt;

		return current;
	}
}
