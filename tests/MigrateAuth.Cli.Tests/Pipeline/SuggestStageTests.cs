using System.Collections.Immutable;
using System.Reflection;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THE STAGE WHOSE WHOLE CONTRACT IS WHAT IT DOES NOT KEEP.
///
/// Suggest resolves each finding's recommendation route and reports the counts; a rendered recommendation is
/// produced at write time and held by nobody. These tests hold it to that - the
/// counts on one side, and the absence of any retained text on the other.
/// </summary>
public sealed class SuggestStageTests
{
	private const string _TEMPLATED_FILE_NAME = "Credentials.cs";
	private const string _LOW_FILE_NAME = "Legacy.cs";
	private const string _UNTEMPLATED_FILE_NAME = "Headers.cs";
	private const string _SUGGEST_STAGE_PREFIX = "suggest: ";

	private static readonly string _templatedContent =
		$"class Credentials\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, secret);\n}}\n";

	private static readonly string _lowContent =
		$"class Legacy\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.LOW_CONSTRUCT}();\n}}\n";

	private static readonly string _untemplatedContent =
		$"class Headers\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.UNTEMPLATED_CONSTRUCT}();\n}}\n";

	/// <summary>
	/// The three recommendation routes, each reached by a finding built to reach it: a High class with a
	/// template, a High class without one, and a Low class that is never looked up at all.
	/// </summary>
	[Fact]
	public void TheThreeRoutesAreCountedSeparately()
	{
		using PipelineHarness harness = PipelineHarness.Create();

		harness.WriteSourceFile(_TEMPLATED_FILE_NAME, _templatedContent);
		harness.WriteSourceFile(_LOW_FILE_NAME, _lowContent);
		harness.WriteSourceFile(_UNTEMPLATED_FILE_NAME, _untemplatedContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.Equal(StageRunOutcome.Completed, harness.Run(CommandId.Suggest));
		Assert.Contains(
			"suggest: recommendations resolved: 1 from a catalog template, 1 generic, 1 routed to manual review.",
			harness.Transcript,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The stage's output opens with the stage name, as the output of <c>scan</c> and <c>analyze</c>
	/// does, so that a reader scrolling a session can tell where each stage's lines begin.
	/// </summary>
	[Fact]
	public void TheStagesFirstLineOpensWithTheStageName()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_TEMPLATED_FILE_NAME, _templatedContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);
		int suggestOutputStart = harness.Transcript.Length;
		harness.Run(CommandId.Suggest);

		string firstLine = harness.Transcript[suggestOutputStart..].Split('\n')[0];

		Assert.StartsWith(_SUGGEST_STAGE_PREFIX, firstLine, StringComparison.Ordinal);
	}

	/// <summary>
	/// The line that makes a catalog gap visible one command before the report is written.
	/// </summary>
	[Fact]
	public void AGenericRouteRaisesTheCoverageNotice()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_UNTEMPLATED_FILE_NAME, _untemplatedContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);
		harness.Run(CommandId.Suggest);

		Assert.Contains(SuggestStage.GENERIC_COVERAGE_NOTICE, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>A catalog that covered everything found says nothing about coverage.</summary>
	[Fact]
	public void AFullyTemplatedSetRaisesNoCoverageNotice()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_TEMPLATED_FILE_NAME, _templatedContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);
		harness.Run(CommandId.Suggest);

		Assert.DoesNotContain(SuggestStage.GENERIC_COVERAGE_NOTICE, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// THE HARD CONSTRAINT, CHECKED ON THE TYPE RATHER THAN ON A RUN. The stage must retain no
	/// rendered recommendation anywhere - not between two runs, and not for the width of the
	/// session. A behavioural test cannot show the absence of a field; the type's own surface can.
	/// Every instance field it holds is either its context or a private counter type, and none of
	/// them is a <see cref="RenderedRecommendation"/> or a collection of one.
	/// </summary>
	[Fact]
	public void TheStageHoldsNoFieldThatCouldRetainARenderedRecommendation()
	{
		IEnumerable<Type> fieldTypes = typeof(SuggestStage)
			.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
			.Select(field => field.FieldType);

		Assert.DoesNotContain(fieldTypes, fieldType =>
			fieldType == typeof(RenderedRecommendation)
			|| (fieldType.IsGenericType && fieldType.GetGenericArguments().Contains(typeof(RenderedRecommendation))));
	}

	/// <summary>
	/// The stage writes nothing to the session state. The data invariant bounds that state to three parts and a
	/// recommendation is not one of them, so a Suggest that contributed to it would be widening the
	/// persisted state past what the invariant admits.
	/// </summary>
	[Fact]
	public void TheStageContributesNothingToTheSessionState()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_TEMPLATED_FILE_NAME, _templatedContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		ImmutableArray<Finding> findingsBefore = harness.State.Findings;
		string[] candidatesBefore = [.. harness.State.CandidateRelativePaths];

		harness.RunAndCommit(CommandId.Suggest);

		Assert.Equal(findingsBefore, harness.State.Findings);
		Assert.Equal(candidatesBefore, harness.State.CandidateRelativePaths);
	}

	/// <summary>The empty case reaches this stage too: no findings is not an error and not a skipped stage.</summary>
	[Fact]
	public void AnEmptyFindingSetCompletesAndSaysSo()
	{
		using PipelineHarness harness = PipelineHarness.Create();

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.Equal(StageRunOutcome.Completed, harness.Run(CommandId.Suggest));
		Assert.Contains(SuggestStage.NO_FINDINGS_MESSAGE, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>An interrupted stage does not complete.</summary>
	[Fact]
	public void ACancelledSuggestDoesNotComplete()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_TEMPLATED_FILE_NAME, _templatedContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		using CancellationTokenSource cancellation = new();

		cancellation.Cancel();

		Assert.Equal(StageRunOutcome.Cancelled, harness.Run(CommandId.Suggest, cancellation.Token));
	}
}
