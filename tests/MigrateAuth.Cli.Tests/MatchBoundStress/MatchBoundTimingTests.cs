using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// The match-bound timing procedure run as part of the suite, so that the property it
/// guarantees is re-checked on every run and the table it produces can be regenerated on demand.
///
/// WHAT THIS ASSERTS, AND WHAT IT DELIBERATELY DOES NOT. It asserts that every shape respects the
/// reader's ceilings, that no expression's own timeout fired, that no terminating shape's worst
/// per-file time reaches the bound in force - the property this procedure guarantees, which
/// the volume proofs already assert over their own fixtures - and that the enumerations which do
/// not terminate are exactly the ones listed in this fixture, so that a new one
/// fails here and a fixed one fails here too and forces this shape back into the procedure. It
/// does not assert the safety multiple: that is a statement about a DIFFERENT, slower machine, and
/// a test that failed on a fast one for it would be asserting nothing. The multiple, the figures and
/// the value derived from them are recorded with the timing table, which states the basis.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class MatchBoundTimingTests
{
	/// <summary>
	/// The multiple of the worst benign per-file time the bound is set at.
	/// </summary>
	internal const int DECLARED_SAFETY_MULTIPLE = 10;

	/// <summary>The derived value is rounded up to a multiple of this many milliseconds.</summary>
	internal const int ROUNDING_STEP_IN_MILLISECONDS = 250;

	/// <summary>
	/// When set, names the file the timing table is also written to, so a timing run
	/// leaves a record outside the test log. Unset, the table goes to the test output only.
	/// </summary>
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT";

	/// <summary>
	/// When set, names a folder the stress shapes are also written into as source files, so the
	/// published Native AOT binary can be run over the same set as a real process and its artifact
	/// read for match-bound trips at the value in force.
	/// </summary>
	internal const string STRESS_FOLDER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_STRESS_FOLDER";

	private const string _STRESS_FILE_NAME_FORMAT = "{0}.cs";

	private const string _PAIR_SEPARATOR = "/";

	private const string _LINE_BREAK = "\n";

	private const string _CS_007 = "CS-007";

	private const string _CS_009 = "CS-009";

	private const string _CS_017 = "CS-017";

	private const string _CS_019 = "CS-019";

	private const string _CS_020 = "CS-020";

	private const string _MATCH_DENSITY_SHAPE_NAME = "every-class-at-match-density";

	private const string _OPEN_WINDOW_SHAPE_NAME = "open-proximity-windows";

	/// <summary>
	/// The (shape, rule) enumerations that do not terminate. Empty: every rule's enumeration terminates
	/// on every shape. Recorded rather than tolerated: the set must match exactly, so a new
	/// non-terminating pair fails here.
	/// </summary>
	private static readonly ImmutableArray<string> _KNOWN_NON_TERMINATING_ENUMERATIONS = [];

	private readonly ITestOutputHelper _output;

	public MatchBoundTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoTerminatingBenignStressShapeReachesTheMatchBoundAndTheFiguresAreRecorded()
	{
		CSharpRecognitionHarness harness = new();
		bool tableRequested = MatchBoundTimingHarness.TimingTableIsRequested(REPORT_PATH_VARIABLE_NAME);
		ImmutableArray<MatchBoundShapeTiming> timings =
		[
			.. MatchBoundStressSet.Build().Select(shape => MatchBoundTimingHarness.Measure(harness, shape, tableRequested))
		];

		string report = MatchBoundTimingHarness.Describe(timings, DECLARED_SAFETY_MULTIPLE, ROUNDING_STEP_IN_MILLISECONDS);
		_output.WriteLine(report);
		WriteTheReportWhereRequested(report);
		WriteTheStressSetWhereRequested();

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Equal(0, timing.FlowTrips));
		Assert.All(
			timings.Where(timing => timing.ShapeName.StartsWith(MatchBoundStressSet.FLOW_SHAPE_NAME_PREFIX, StringComparison.Ordinal)),
			timing => Assert.Equal(1, timing.FlowEvaluations));
		Assert.All(
			timings.Where(timing => timing.WorstFileMilliseconds is not null),
			timing => Assert.True(
				timing.WorstFileMilliseconds < RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS,
				timing.ShapeName));
		Assert.Equal(_KNOWN_NON_TERMINATING_ENUMERATIONS.ToArray(), NonTerminatingEnumerations(timings).ToArray());
	}

	/// <summary>
	/// The bounded windows added to the catalog's rules, each as its rule,
	/// the statement that opens it and the statement that closes it.
	/// </summary>
	public static IEnumerable<object[]> AddedStressWindows()
	{
		return
		[
			[_CS_007, MatchBoundStressSet.DIRECT_HANDLER_ASSIGNMENT_WINDOW_OPENER, MatchBoundStressSet.DIRECT_HANDLER_ASSIGNMENT_WINDOW_CLOSER],
			[_CS_019, MatchBoundStressSet.FILE_TRANSFER_REQUEST_WINDOW_OPENER, MatchBoundStressSet.FILE_TRANSFER_REQUEST_WINDOW_CLOSER],
			[_CS_019, MatchBoundStressSet.DIRECTORY_CREDENTIAL_WINDOW_OPENER, MatchBoundStressSet.DIRECTORY_CREDENTIAL_WINDOW_CLOSER]
		];
	}

	/// <summary>
	/// The windows added by the target-typed declarations and by the identifier-rooted pair halves, each as its
	/// rule, the statement that opens it and the statement that closes it.
	/// </summary>
	public static IEnumerable<object[]> WindowsAddedByTheTargetTypedAndRootedForms()
	{
		return
		[
			[_CS_007, MatchBoundStressSet.TARGET_TYPED_HANDLER_WINDOW_OPENER, MatchBoundStressSet.TARGET_TYPED_HANDLER_WINDOW_CLOSER],
			[_CS_019, MatchBoundStressSet.TARGET_TYPED_MAIL_WINDOW_OPENER, MatchBoundStressSet.TARGET_TYPED_MAIL_WINDOW_CLOSER],
			[_CS_019, MatchBoundStressSet.TARGET_TYPED_SINK_FIRST_WINDOW_OPENER, MatchBoundStressSet.TARGET_TYPED_SINK_FIRST_WINDOW_CLOSER],
			[_CS_019, MatchBoundStressSet.TARGET_TYPED_CREDENTIAL_FIRST_WINDOW_OPENER, MatchBoundStressSet.TARGET_TYPED_CREDENTIAL_FIRST_WINDOW_CLOSER],
			[_CS_009, MatchBoundStressSet.ROOTED_PAIR_WINDOW_OPENER, MatchBoundStressSet.ROOTED_PAIR_WINDOW_CLOSER]
		];
	}

	/// <summary>
	/// EACH NEW WINDOW IS REGISTERED AS TERMINATING. The window sits in the match-density shape
	/// closed and in the open-window shape never closed, its rule recognizes the closed form, and its
	/// rule's enumeration advances over both shapes - so a regression that made one of these windows
	/// repeat a match fails here by name, and not only through the exact-set assertion above.
	/// </summary>
	[Theory]
	[MemberData(nameof(AddedStressWindows))]
	[MemberData(nameof(WindowsAddedByTheTargetTypedAndRootedForms))]
	public void AnAddedStressWindowIsInTheStressSetAndItsRuleTerminatesThere(string patternClass, string opener, string closer)
	{
		CSharpRecognitionHarness harness = new();
		Regex expression = harness.Analyzer.DetectionRulesByPatternClass[patternClass].Expression;
		ImmutableArray<MatchBoundStressShape> shapes = MatchBoundStressSet.Build();
		MatchBoundStressShape closedShape = shapes.Single(shape => shape.Name == _MATCH_DENSITY_SHAPE_NAME);
		MatchBoundStressShape openShape = shapes.Single(shape => shape.Name == _OPEN_WINDOW_SHAPE_NAME);

		Assert.Contains(opener + closer, closedShape.Text, StringComparison.Ordinal);
		Assert.Contains(opener + _LINE_BREAK, openShape.Text, StringComparison.Ordinal);
		Assert.True(expression.IsMatch(RecognitionHarness.Normalize(opener + closer)), patternClass);
		Assert.All(
			new[] { closedShape, openShape },
			shape => Assert.True(
				MatchBoundTimingHarness.EnumerationAdvances(expression, RecognitionHarness.Normalize(shape.Text)),
				patternClass + _PAIR_SEPARATOR + shape.Name));
	}

	/// <summary>
	/// The shapes of three `CSharp` detection windows, each as its rule, its shape, the text that opens its window or its gap
	/// and a closed form its rule reads: the member store alternating with a sink, on lines of the window's length, and alone; the brace-free run of the
	/// user-name half of the <c>Rsapi</c> pair; and the unterminated block comment in the argument gap of the proxy factory call.
	/// </summary>
	public static IEnumerable<object[]> ShapesOfTheCSharpDetectionCorrections()
	{
		string memberChain = $"{MatchBoundStressSet.MEMBER_STORE_STATEMENT}{_LINE_BREAK}{MatchBoundStressSet.MEMBER_SINK_DECLARATION_STATEMENT}{_LINE_BREAK}\t\tmemberMailer.Credentials = settings.MailCredential;";

		return
		[
			[_CS_019, MatchBoundStressSet.ALTERNATING_MEMBER_STORE_AND_SINK_SHAPE_NAME, MatchBoundStressSet.MEMBER_STORE_STATEMENT, memberChain],
			[_CS_019, MatchBoundStressSet.ALTERNATING_MEMBER_STORE_AND_SINK_ON_LONG_LINES_SHAPE_NAME, MatchBoundStressSet.MEMBER_STORE_STATEMENT, memberChain],
			[_CS_019, MatchBoundStressSet.MEMBER_STORES_ONLY_SHAPE_NAME, MatchBoundStressSet.MEMBER_STORE_STATEMENT, memberChain],
			[_CS_017, MatchBoundStressSet.BRACE_FREE_REPEATED_RSAPI_USER_NAME_SHAPE_NAME, MatchBoundStressSet.RSAPI_USER_NAME_ENTRY.TrimEnd(), "settings.RsapiUserName = userName;\nsettings.RsapiPassword = password;"],
			[_CS_020, MatchBoundStressSet.UNTERMINATED_PROXY_FACTORY_COMMENT_SHAPE_NAME, MatchBoundStressSet.UNTERMINATED_PROXY_FACTORY_COMMENT_CALL, "var proxy = helper.GetProxy<IExampleService>(userName, password);"]
		];
	}

	/// <summary>
	/// EACH OF THESE SHAPES IS REGISTERED AS TERMINATING. The shape is in the stress set and carries the text that opens its window or its gap, its rule reads the
	/// closed form of that window, and its rule's enumeration advances over the shape, so a regression that made one of these windows repeat a match fails here by name.
	/// </summary>
	[Theory]
	[MemberData(nameof(ShapesOfTheCSharpDetectionCorrections))]
	public void AShapeOfTheCSharpDetectionCorrectionsIsInTheStressSetAndItsRuleTerminatesThere(string patternClass, string shapeName, string opening, string closedForm)
	{
		Regex expression = new CSharpRecognitionHarness().Analyzer.DetectionRulesByPatternClass[patternClass].Expression;
		MatchBoundStressShape shape = MatchBoundStressSet.Build().Single(candidate => candidate.Name == shapeName);

		Assert.Contains(opening, shape.Text, StringComparison.Ordinal);
		Assert.True(expression.IsMatch(RecognitionHarness.Normalize(closedForm)), patternClass);
		Assert.True(MatchBoundTimingHarness.EnumerationAdvances(expression, RecognitionHarness.Normalize(shape.Text)), $"{patternClass}{_PAIR_SEPARATOR}{shapeName}");
	}

	[Fact]
	public void EveryStressShapeRespectsTheReaderCeilingsSoTheEngineWouldReceiveIt()
	{
		ImmutableArray<MatchBoundStressShape> shapes = MatchBoundStressSet.Build();

		Assert.All(shapes, shape => Assert.True(
			Encoding.UTF8.GetByteCount(shape.Text) < ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES,
			shape.Name));
		Assert.All(shapes, shape => Assert.True(
			LongestLineLength(shape.Text) <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS,
			shape.Name));
	}

	private static ImmutableArray<string> NonTerminatingEnumerations(ImmutableArray<MatchBoundShapeTiming> timings)
	{
		return
		[
			.. timings
				.SelectMany(timing => timing.NonTerminatingRules.Select(rule => timing.ShapeName + _PAIR_SEPARATOR + rule))
				.Order(StringComparer.Ordinal)
		];
	}

	private static int LongestLineLength(string text)
	{
		return text.Split('\n').Max(line => line.Length);
	}

	private static void WriteTheStressSetWhereRequested()
	{
		string? stressFolder = Environment.GetEnvironmentVariable(STRESS_FOLDER_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(stressFolder))
		{
			Directory.CreateDirectory(stressFolder);

			foreach (MatchBoundStressShape shape in MatchBoundStressSet.Build())
			{
				File.WriteAllText(
					Path.Combine(stressFolder, string.Format(CultureInfo.InvariantCulture, _STRESS_FILE_NAME_FORMAT, shape.Name)),
					shape.Text,
					new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
	}

	private static void WriteTheReportWhereRequested(string report)
	{
		string? reportPath = Environment.GetEnvironmentVariable(REPORT_PATH_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(reportPath))
		{
			File.WriteAllText(reportPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}
}
