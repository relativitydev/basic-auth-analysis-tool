using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE INACTIVE-REGION READING APPLIES TO EVERY `CSharp` CLASS WITH NO PER-CLASS EDIT. Every covered form of the creation-form
/// matrix (an invented source that fires exactly one class at a stated level) is wrapped in a block the file's own symbols
/// switch off and read again: the same class is still recognized, now at Low with the inactive reason. The same form wrapped in
/// a block that only a build setting outside the file can switch off (a well-known symbol) reads at the level the form has
/// outside any directive, with no reason, so nothing was demoted that the file does not itself switch off.
/// </summary>
public sealed class ConditionalCompilationEveryClassTests
{
	private const string _NEWLINE = "\n";
	private const string _INACTIVE_BLOCK_OPENING = "#if FEAT_NEVER_DEFINED_HERE";
	private const string _MAYBE_BLOCK_OPENING = "#if DEBUG";
	private const string _END_OF_GROUP = "#endif";

	public static TheoryData<string> EveryCoveredFormLabel()
	{
		TheoryData<string> labels = [];

		foreach (CoveredForm form in CSharpDetectionFormFixtures.CoveredForms)
		{
			labels.Add(form.Label);
		}

		return labels;
	}

	[Theory]
	[MemberData(nameof(EveryCoveredFormLabel))]
	public void AFormInsideASwitchedOffBlockIsStillRecognizedAndReadsLowWithTheInactiveReason(string label)
	{
		CoveredForm form = FormLabelled(label);

		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrapped(_INACTIVE_BLOCK_OPENING, form.Source));

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == form.PatternClass);
		Assert.All(constructs, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
		Assert.All(constructs, construct => Assert.True(construct.LowReasons.HasFlag(LowConfidenceReasons.InactivePreprocessorRegion)));
	}

	[Theory]
	[MemberData(nameof(EveryCoveredFormLabel))]
	public void AFormInsideABlockOnlyABuildSettingCanSwitchOffKeepsItsLevelAndGetsNoInactiveReason(string label)
	{
		CoveredForm form = FormLabelled(label);

		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrapped(_MAYBE_BLOCK_OPENING, form.Source));
		RecognizedConstruct recognized = constructs.First(construct => construct.PatternClassIdentifier == form.PatternClass);

		Assert.Equal(form.Level, recognized.Level);
		Assert.DoesNotContain(constructs, construct => construct.LowReasons.HasFlag(LowConfidenceReasons.InactivePreprocessorRegion));
	}

	private static CoveredForm FormLabelled(string label)
	{
		return CSharpDetectionFormFixtures.CoveredForms.Single(form => form.Label == label);
	}

	private static string Wrapped(string blockOpening, string source)
	{
		return $"{string.Join(_NEWLINE, blockOpening, source, _END_OF_GROUP)}{_NEWLINE}";
	}
}
