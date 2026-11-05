using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THROUGH THE REAL PIPELINE.
///
/// Every other display-name test builds a finding by hand or calls a renderer directly - the
/// right fixture for most of this contract, and the wrong one for proving the promise end to
/// end: that a Low finding, which <see cref="Reporting.RecommendationRenderer.Route"/> never
/// consults a template for, still carries its catalog display name into both the Markdown
/// report and the console table when the real four stages run it. This class runs the whole
/// workflow over <see cref="PipelineLanguageAnalyzer"/>'s Low class and reads both surfaces the
/// Report stage actually wrote.
/// </summary>
public sealed class DisplayNameContractPipelineTests
{
	private const string _SOURCE_FILE_NAME = "DisplayNameContract.cs";

	private static readonly string _lowConfidenceContent =
		$"public sealed class Legacy\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.LOW_CONSTRUCT}();\n}}\n";

	[Fact]
	public void ALowFindingsDisplayNameRendersInBothTheReportAndTheConsole()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_SOURCE_FILE_NAME, _lowConfidenceContent);

		harness.RunTheWholeWorkflow();

		string reportText = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);
		string consoleTranscript = harness.Transcript;

		Assert.Contains(PipelineLanguageAnalyzer.LOW_DISPLAY_NAME, reportText, StringComparison.Ordinal);
		Assert.Contains(PipelineLanguageAnalyzer.LOW_PATTERN_CLASS, reportText, StringComparison.Ordinal);
		Assert.Contains(PipelineLanguageAnalyzer.LOW_DISPLAY_NAME, consoleTranscript, StringComparison.Ordinal);
		Assert.Contains(PipelineLanguageAnalyzer.LOW_PATTERN_CLASS, consoleTranscript, StringComparison.Ordinal);
	}
}
