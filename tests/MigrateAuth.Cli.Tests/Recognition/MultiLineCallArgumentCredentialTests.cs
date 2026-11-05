using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.MatchBoundStress;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// A SHORT C# FILE WHOSE CREDENTIAL IS BUILT FROM TWO CALLS WITH MULTI-LINE ARGUMENT LISTS, HANDED TO A HANDLER THAT ASKS FOR
/// PRE-AUTHENTICATION, AND FOLLOWED BY MORE LINES THAN THE BOUNDED WINDOW OF THE NON-HTTP SINK CLASS LOOKS OVER.
///
/// The shape is written here with generic names (the same text as the match-bound stress shape,
/// <see cref="MatchBoundStressSet.MULTI_LINE_CALL_ARGUMENT_CREDENTIAL_WINDOW"/>). The frozen clock cannot
/// trip the match bound, so what these tests prove is that recognition FINISHES (a matcher that does not finish would run to
/// the expression's own timeout and fail the test by the exception it raises), that the credential is found at its level,
/// and that no file is skipped.
/// </summary>
public sealed class MultiLineCallArgumentCredentialTests
{
	private const string _CREDENTIAL_PATTERN_CLASS = "CS-008";
	private const string _NON_HTTP_SINK_PATTERN_CLASS = "CS-019";
	private const string _SOURCE_OPENING = "namespace Synthetic.Service\n{\ninternal static class ServiceClientFactory\n{\n\tinternal static void Configure()\n\t{\n";
	private const string _SOURCE_CLOSING = "\n\t}\n}\n}\n";
	private const string _FILLER_STATEMENT = "\t\tint total = first + second;";
	private const int _FURTHER_LINES = 7;
	private const string _LINE_BREAK = "\n";

	[Fact]
	public void TheCredentialBuiltFromMultiLineCallsIsOneMediumConstructAndNoFileIsSkipped()
	{
		CSharpRecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(SourceText());

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_CREDENTIAL_PATTERN_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.DoesNotContain(harness.Sink.Received, fact => fact.Category is EmissionCategory.SkippedItem);
	}

	[Fact]
	public void TheNonHttpSinkExpressionFinishesOverTheFileWithNoMatch()
	{
		CSharpRecognitionHarness harness = new();
		Regex declared = harness.Analyzer.DetectionRulesByPatternClass[_NON_HTTP_SINK_PATTERN_CLASS].Expression;
		string normalizedText = RecognitionHarness.Normalize(SourceText());

		Assert.NotEqual(typeof(Regex), declared.GetType());
		Assert.False(declared.Match(normalizedText).Success);
	}

	private static string SourceText()
	{
		string furtherLines = string.Join(_LINE_BREAK, Enumerable.Repeat(_FILLER_STATEMENT, _FURTHER_LINES));

		return $"{_SOURCE_OPENING}{MatchBoundStressSet.MULTI_LINE_CALL_ARGUMENT_CREDENTIAL_WINDOW}{_LINE_BREAK}{furtherLines}{_SOURCE_CLOSING}";
	}
}
