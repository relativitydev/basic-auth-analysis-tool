using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// FLOW ASSIGNMENT: THE PURE STEP ITSELF, AND THE STAGE THAT CALLS IT.
///
/// The step is a function of one finding's evidence and the effective default, so half of these
/// are plain inputs and outputs. The other half run the real Analysis stage over files that carry
/// the fixture signals, because "assigned between composition and recording" is a claim about the
/// stage and not about the function.
/// </summary>
public sealed class FlowAssignmentTests
{
	private const string _INTERACTIVE_FILE = "Interactive.cs";
	private const string _FIXED_FILE = "Fixed.cs";
	private const string _NEITHER_FILE = "Neither.cs";
	private const string _BOTH_FILE = "Both.cs";
	private const string _TIMED_OUT_FILE = "TimedOut.cs";
	private const string _PATTERN_CLASS = "CS-001";
	private const string _TIMING_OUT_SIGNAL_NAME = "fixture-timing-out";
	private const string _CATASTROPHIC_PATTERN = @"^(?<construct>(\w+\s?)*)$";
	private const string _INPUT_THAT_CANNOT_MATCH = "the quick brown fox jumps over the lazy dog and keeps running well past the point of reason!";
	private const int _ONE_MILLISECOND = 1;

	private static readonly string _highConstruct =
		$"\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, \"a-literal-secret\");";

	private static readonly string _lowConstruct =
		$"\tvoid Legacy() => {PipelineLanguageAnalyzer.LOW_CONSTRUCT}();";

	private static readonly ImmutableArray<FlowSignal> _bothKindsOfSignal =
		[FlowSignalFixtures.InteractiveCodeOnly, FlowSignalFixtures.FixedCodeOnly];

	// ---- the pure function ----

	[Theory]
	[InlineData(FlowEvidence.Interactive, CredentialFlow.ClientCredentials, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal)]
	[InlineData(FlowEvidence.Interactive, CredentialFlow.AuthorizationCode, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal)]
	[InlineData(FlowEvidence.Fixed, CredentialFlow.ClientCredentials, CredentialFlow.ClientCredentials, FlowDecision.FixedSignal)]
	[InlineData(FlowEvidence.Fixed, CredentialFlow.AuthorizationCode, CredentialFlow.ClientCredentials, FlowDecision.FixedSignal)]
	[InlineData(FlowEvidence.None, CredentialFlow.ClientCredentials, CredentialFlow.ClientCredentials, FlowDecision.Default)]
	[InlineData(FlowEvidence.None, CredentialFlow.AuthorizationCode, CredentialFlow.AuthorizationCode, FlowDecision.Default)]
	public void EvidenceAndTheEffectiveDefaultDecideTheFlow(
		FlowEvidence evidence,
		CredentialFlow effectiveDefault,
		CredentialFlow expectedFlow,
		FlowDecision expectedDecision)
	{
		Finding finding = FindingWith(ConfidenceLevel.High, evidence);

		Finding assigned = FlowAssignment.Assign(finding, effectiveDefault);

		Assert.Equal(expectedFlow, assigned.Flow);
		Assert.Equal(expectedDecision, assigned.FlowDecision);
	}

	[Theory]
	[InlineData(ConfidenceLevel.High)]
	[InlineData(ConfidenceLevel.Medium)]
	public void AHighOrMediumFindingWithoutEvidenceIsDecidedByTheDefault(ConfidenceLevel level)
	{
		Finding assigned = FlowAssignment.Assign(FindingWith(level, evidence: null), CredentialFlow.AuthorizationCode);

		Assert.Equal(CredentialFlow.AuthorizationCode, assigned.Flow);
		Assert.Equal(FlowDecision.Default, assigned.FlowDecision);
	}

	[Theory]
	[InlineData(FlowEvidence.None)]
	[InlineData(FlowEvidence.Fixed)]
	[InlineData(FlowEvidence.Interactive)]
	public void ALowFindingCarriesNoFlowWhateverItsFileEstablished(FlowEvidence evidence)
	{
		Finding assigned = FlowAssignment.Assign(
			FindingWith(ConfidenceLevel.Low, evidence),
			CredentialFlow.AuthorizationCode);

		Assert.Null(assigned.Flow);
		Assert.Null(assigned.FlowDecision);
	}

	/// <summary>A literal credential is a security property and never flow evidence.</summary>
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void TheLiteralCredentialSignalNeverChangesTheAssignment(bool literalCredentialSignal)
	{
		Finding finding = FindingWith(ConfidenceLevel.High, FlowEvidence.None) with
		{
			LiteralCredentialSignal = literalCredentialSignal
		};

		Finding assigned = FlowAssignment.Assign(finding, CredentialFlow.ClientCredentials);

		Assert.Equal(CredentialFlow.ClientCredentials, assigned.Flow);
		Assert.Equal(FlowDecision.Default, assigned.FlowDecision);
	}

	[Fact]
	public void AssigningTwiceGivesTheSameFindingsAndTouchesNothingElse()
	{
		ImmutableArray<Finding> findings =
		[
			FindingWith(ConfidenceLevel.High, FlowEvidence.Interactive),
			FindingWith(ConfidenceLevel.Low, FlowEvidence.None),
			FindingWith(ConfidenceLevel.Medium, FlowEvidence.Fixed)
		];

		ImmutableArray<Finding> first = FlowAssignment.AssignAll(findings, CredentialFlow.ClientCredentials);
		ImmutableArray<Finding> second = FlowAssignment.AssignAll(findings, CredentialFlow.ClientCredentials);

		Assert.Equal(first.AsEnumerable(), second.AsEnumerable());
		Assert.Equal(
			findings.Select(finding => finding with { Flow = null, FlowDecision = null }),
			first.Select(finding => finding with { Flow = null, FlowDecision = null }));
	}

	// ---- the stage ----

	/// <summary>Flow assignment over the real Analysis stage.</summary>
	[Fact]
	public void TheAnalysisStageAssignsEachKindOfEvidenceItsFlowAndLeavesLowFindingsWithoutOne()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(_bothKindsOfSignal);

		harness.WriteSourceFile(_INTERACTIVE_FILE, SourceWith(FlowSignalFixtures.INTERACTIVE_TOKEN, includeLow: true));
		harness.WriteSourceFile(_FIXED_FILE, SourceWith(FlowSignalFixtures.FIXED_TOKEN, includeLow: true));
		harness.WriteSourceFile(_NEITHER_FILE, SourceWith(token: null, includeLow: true));
		harness.WriteSourceFile(
			_BOTH_FILE,
			SourceWith($"{FlowSignalFixtures.FIXED_TOKEN}();\n\t{FlowSignalFixtures.INTERACTIVE_TOKEN}", includeLow: false));

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		AssertFlow(harness, _INTERACTIVE_FILE, ConfidenceLevel.High, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal);
		AssertFlow(harness, _FIXED_FILE, ConfidenceLevel.High, CredentialFlow.ClientCredentials, FlowDecision.FixedSignal);
		AssertFlow(harness, _NEITHER_FILE, ConfidenceLevel.High, CredentialFlow.ClientCredentials, FlowDecision.Default);
		AssertFlow(harness, _BOTH_FILE, ConfidenceLevel.High, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal);

		foreach (string path in new[] { _INTERACTIVE_FILE, _FIXED_FILE, _NEITHER_FILE })
		{
			Finding low = FindingIn(harness, path, ConfidenceLevel.Low);

			Assert.Null(low.Flow);
			Assert.Null(low.FlowDecision);
			Assert.Null(low.FlowEvidence);
		}
	}

	/// <summary>The profile's default flow decides a finding no signal decided.</summary>
	[Fact]
	public void ANoSignalFindingTakesTheProfilesDefaultFlowAndIsDecidedDefault()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(
			_bothKindsOfSignal,
			CredentialFlow.AuthorizationCode);

		harness.WriteSourceFile(_NEITHER_FILE, SourceWith(token: null, includeLow: false));
		harness.WriteSourceFile(_FIXED_FILE, SourceWith(FlowSignalFixtures.FIXED_TOKEN, includeLow: false));

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		AssertFlow(harness, _NEITHER_FILE, ConfidenceLevel.High, CredentialFlow.AuthorizationCode, FlowDecision.Default);
		AssertFlow(harness, _FIXED_FILE, ConfidenceLevel.High, CredentialFlow.ClientCredentials, FlowDecision.FixedSignal);
	}

	/// <summary>
	/// A signal that times out after the file produced a High finding
	/// leaves the finding reported, decided Default, counted as a trip, and the session complete.
	/// </summary>
	[Fact]
	public void AFlowSignalTimeoutKeepsTheFindingDecidesItDefaultAndCountsTheTrip()
	{
		FlowSignal timingOut = new()
		{
			Name = _TIMING_OUT_SIGNAL_NAME,
			Kind = FlowSignalKind.InteractiveUser,
			Zone = FlowSignalZone.CodeOnly,
			Expression = new Regex(_CATASTROPHIC_PATTERN, RegexOptions.None, TimeSpan.FromMilliseconds(_ONE_MILLISECOND))
		};

		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals([timingOut]);

		harness.WriteSourceFile(_TIMED_OUT_FILE, $"{_INPUT_THAT_CANNOT_MATCH}\nclass TimedOut\n{{\n{_highConstruct}\n}}\n");

		harness.RunAndCommit(CommandId.Scan);
		StageRunOutcome outcome = harness.RunAndCommit(CommandId.Analyze);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		Assert.Equal(1, harness.State.FlowEvaluationDiagnostics.SignalTripCount);
		AssertFlow(harness, _TIMED_OUT_FILE, ConfidenceLevel.High, CredentialFlow.ClientCredentials, FlowDecision.Default);
		Assert.Equal(FlowEvidence.None, FindingIn(harness, _TIMED_OUT_FILE, ConfidenceLevel.High).FlowEvidence);
	}

	private static void AssertFlow(
		PipelineHarness harness,
		string path,
		ConfidenceLevel level,
		CredentialFlow expectedFlow,
		FlowDecision expectedDecision)
	{
		Finding finding = FindingIn(harness, path, level);

		Assert.Equal(expectedFlow, finding.Flow);
		Assert.Equal(expectedDecision, finding.FlowDecision);
	}

	private static Finding FindingIn(PipelineHarness harness, string path, ConfidenceLevel level)
	{
		return Assert.Single(harness.State.Findings, finding => finding.Path.Value == path && finding.Level == level);
	}

	private static string SourceWith(string? token, bool includeLow)
	{
		string tokenLine = token is null ? string.Empty : $"\tvoid Signal() => {token}();\n";
		string lowLine = includeLow ? $"{_lowConstruct}\n" : string.Empty;

		return $"class Sample\n{{\n{tokenLine}{_highConstruct}\n{lowLine}}}\n";
	}

	private static Finding FindingWith(ConfidenceLevel level, FlowEvidence? evidence)
	{
		Finding finding = ReportHarness.FindingAt(
			sequentialIndex: 1,
			relativePath: "source/sample.cs",
			line: 1,
			patternClassIdentifier: _PATTERN_CLASS,
			level: level);

		return finding with { FlowEvidence = evidence };
	}
}
