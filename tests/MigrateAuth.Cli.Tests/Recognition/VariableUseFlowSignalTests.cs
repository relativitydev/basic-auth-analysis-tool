using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE SAME-FILE WALK OF A FLOW SIGNAL'S VARIABLE. A prompt that fills a variable which a later statement of a
/// bounded window feeds to a password read is not evidence of an interactive user for the integration, so the
/// match is suppressed; the signal can still fire on another match. The expressions are the test-local ones of
/// <see cref="ShellProbe"/>.
/// </summary>
public sealed class VariableUseFlowSignalTests
{
	private const string _PROMPT_LINE = "read -p \"Password: \" PW";
	private const string _BARE_PROMPT_LINE = "read -p \"Password: \"";
	private const string _FEEDING_LINE = "echo \"$PW\" | sudo -S true";
	private const string _BRACED_FEEDING_LINE = "echo \"${PW}\" | sudo -S true";
	private const string _PLAIN_USE_LINE = "echo \"$PW\"";
	private const string _FILLER_LINE = "echo filler";
	private const int _WINDOW_LINES = 40;
	private const int _LINES_PAST_THE_WINDOW = _WINDOW_LINES + 5;
	private const int _LINES_INSIDE_THE_WINDOW = _WINDOW_LINES - 5;
	private const int _LINE_CAP = 30;

	/// <summary>Longer than <see cref="_LINE_CAP"/>, so the password read that follows it falls past the cap.</summary>
	private const int _FILLER_LONGER_THAN_THE_CAP = _LINE_CAP * 2;
	private const int _ONE_MILLISECOND = 1;
	private const string _CATASTROPHIC_PATTERN = @"(?<x>(\w+\s?)*)$";
	private const string _LINE_THAT_CANNOT_MATCH = "echo \"$PW\" the quick brown fox jumps over the lazy dog and keeps running well past the point of reason!";

	[Fact]
	public void APromptWhoseVariableIsLaterFedToAPasswordReadIsSuppressed()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, _FEEDING_LINE);

		Assert.Equal(FlowEvidence.None, evidence);
	}

	[Fact]
	public void ThePromptStillFiresWhenNothingFeedsItsVariableToAPasswordRead()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, _PLAIN_USE_LINE);

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void AUseThatDoesNotMatchTheUsePatternSuppressesNothing()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, "echo \"$PW\" | wc -c", "sudo -n true");

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	/// <summary>
	/// The rest of the match's own logical line, after the match, is read like a later line, because a
	/// prompt and its use are often written on one line.
	/// </summary>
	[Fact]
	public void TheRestOfTheMatchesOwnLogicalLineAfterTheMatchIsAUse()
	{
		FlowEvidence sameLine = EvidenceOf("read -p \"Password: \" PW; echo \"$PW\" | sudo -S true");
		FlowEvidence referenceOnlyBeforeTheMatch = EvidenceOf("echo \"$PW\" | sudo -S true; read -p \"Password: \" PW");

		Assert.Equal(FlowEvidence.None, sameLine);
		Assert.Equal(FlowEvidence.Interactive, referenceOnlyBeforeTheMatch);
	}

	[Fact]
	public void ABracedReferenceIsAReference()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, _BRACED_FEEDING_LINE);

		Assert.Equal(FlowEvidence.None, evidence);
	}

	[Fact]
	public void AUsePatternLineThatDoesNotReferenceTheVariableIsNotAUse()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, "echo other | sudo -S true");

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void AUseInsideTheWindowSuppressesAndAUseBeyondItDoesNot()
	{
		string[] filler = [.. Enumerable.Repeat(_FILLER_LINE, _LINES_INSIDE_THE_WINDOW)];
		string[] farFiller = [.. Enumerable.Repeat(_FILLER_LINE, _LINES_PAST_THE_WINDOW)];

		FlowEvidence inside = EvidenceOf([_PROMPT_LINE, .. filler, _FEEDING_LINE]);
		FlowEvidence beyond = EvidenceOf([_PROMPT_LINE, .. farFiller, _FEEDING_LINE]);

		Assert.Equal(FlowEvidence.None, inside);
		Assert.Equal(FlowEvidence.Interactive, beyond);
	}

	[Fact]
	public void TheWindowIsTheOneTheSignalDeclares()
	{
		ShellProbeAnalyzer narrow = ShellProbe.AnalyzerWith(
			[ShellProbe.PromptFeeding(new FlowSignalVariableUse
			{
				UsePattern = ShellProbeExpressions.PasswordOnStandardInput(),
				WindowLines = 2
			})]);
		string source = ShellProbe.Source(ShellProbe.CLIENT_CALL_LINE, _PROMPT_LINE, _FILLER_LINE, _FILLER_LINE, _FILLER_LINE, _FEEDING_LINE);

		FlowEvidence evidence = ShellProbe.Recognize(narrow, source).Result.FlowEvidence;

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void AVariableCopiedToASecondOneIsFollowedToTheUse()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, "PW2=$PW", "echo \"$PW2\" | sudo -S true");

		Assert.Equal(FlowEvidence.None, evidence);
	}

	[Theory]
	[InlineData("PW2=\"$PW\"")]
	[InlineData("PW2=${PW}")]
	[InlineData("export PW2=$PW")]
	[InlineData("local PW2=\"${PW}\"")]
	[InlineData("declare -x PW2=$PW")]
	public void EachPlainFormOfACopyIsFollowed(string copyLine)
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, copyLine, "echo \"$PW2\" | sudo -S true");

		Assert.Equal(FlowEvidence.None, evidence);
	}

	[Theory]
	[InlineData("PW2=\"$PW-suffix\"")]
	[InlineData("PW2=\"x$PW\"")]
	[InlineData("PW2=$PWX")]
	[InlineData("PW2=$(echo $PW)")]
	public void AValueThatIsMoreThanTheReferenceIsNotACopy(string notACopy)
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, notACopy, "echo \"$PW2\" | sudo -S true");

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void TheCopyChainIsFollowedForTheDeclaredNumberOfHopsAndNoFurther()
	{
		string[] chain = [_PROMPT_LINE, "PW2=$PW", "PW3=$PW2", "PW4=$PW3", "echo \"$PW4\" | sudo -S true"];
		string[] shortChain = [_PROMPT_LINE, "PW2=$PW", "PW3=$PW2", "echo \"$PW3\" | sudo -S true"];

		FlowEvidence tooDeep = EvidenceOf(chain);
		FlowEvidence withinTheDepth = EvidenceOf(shortChain);

		Assert.Equal(FlowEvidence.Interactive, tooDeep);
		Assert.Equal(FlowEvidence.None, withinTheDepth);
	}

	[Fact]
	public void ACopyDoesNotLeadBackwardsToAUseBeforeTheCopy()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, "echo \"$PW2\" | sudo -S true", "PW2=$PW");

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void AReadThatNamesNoVariableReadsTheDefaultVariable()
	{
		FlowEvidence feeding = EvidenceOf(_BARE_PROMPT_LINE, "echo \"$REPLY\" | sudo -S true");
		FlowEvidence unrelated = EvidenceOf(_BARE_PROMPT_LINE, "echo \"$PW\" | sudo -S true");

		Assert.Equal(FlowEvidence.None, feeding);
		Assert.Equal(FlowEvidence.Interactive, unrelated);
	}

	[Fact]
	public void ANamedVariableWinsOverTheDefaultOne()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, "echo \"$REPLY\" | sudo -S true");

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void AReferenceInACommentIsNotAUse()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, "# echo \"$PW\" | sudo -S true");

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void AReferenceToADifferentVariableWithTheSamePrefixIsNotAUse()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, "echo \"$PWD\" | sudo -S true", "echo \"${PWDX}\" | sudo -S true", "echo \"$PW_\" | sudo -S true");

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void ALineContinuedAcrossABackslashNewlineIsOneLogicalLine()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, "echo \"$PW\" | \\", "sudo \\", "-S true");

		Assert.Equal(FlowEvidence.None, evidence);
	}

	[Fact]
	public void OnlyTheFirstCharactersOfALogicalLineAreReadUpToTheDeclaredCap()
	{
		ShellProbeAnalyzer capped = ShellProbe.AnalyzerWith(
			[ShellProbe.PromptFeeding(new FlowSignalVariableUse
			{
				UsePattern = ShellProbeExpressions.PasswordOnStandardInput(),
				MaxLogicalLineLength = _LINE_CAP
			})]);
		string padding = new('x', _FILLER_LONGER_THAN_THE_CAP);
		string source = ShellProbe.Source(ShellProbe.CLIENT_CALL_LINE, _PROMPT_LINE, $"echo \"$PW\" {padding} | sudo -S true");

		FlowEvidence evidence = ShellProbe.Recognize(capped, source).Result.FlowEvidence;

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void ASuppressedMatchLeavesTheSignalFreeToFireOnAnotherMatch()
	{
		FlowEvidence evidence = EvidenceOf(
			_PROMPT_LINE,
			_FEEDING_LINE,
			"read -p \"Token: \" TOK",
			"echo \"$TOK\"");

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void ASignalWithoutAVariableUseDeclarationFiresWhateverFollows()
	{
		ShellProbeAnalyzer plain = ShellProbe.AnalyzerWith([ShellProbe.PromptReadInBashAndDash], ShellProbe.Dialects);
		string source = ShellProbe.Source(ShellProbe.CLIENT_CALL_LINE, _PROMPT_LINE, _FEEDING_LINE);

		FlowEvidence evidence = ShellProbe.Recognize(plain, source).Result.FlowEvidence;

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void AUsePatternThatTimesOutDiscardsTheFlowEvidenceAndKeepsTheConstructs()
	{
		FlowSignalVariableUse timingOut = new()
		{
			UsePattern = new Regex(_CATASTROPHIC_PATTERN, RegexOptions.None, TimeSpan.FromMilliseconds(_ONE_MILLISECOND))
		};
		ShellProbeAnalyzer analyzer = ShellProbe.AnalyzerWith([ShellProbe.PromptFeeding(timingOut)]);
		string source = ShellProbe.Source(ShellProbe.CLIENT_CALL_LINE, _PROMPT_LINE, _LINE_THAT_CANNOT_MATCH);

		(RecognitionResult result, RecognitionEngine engine, RecognitionHarness harness) = ShellProbe.Recognize(analyzer, source);

		Assert.NotEmpty(result.Constructs);
		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
		Assert.True(result.FlowEvaluationTripped);
		Assert.Equal(1, engine.FlowSignalTripCount);
		Assert.Empty(harness.SkippedItems());
	}

	[Fact]
	public void AUsePatternEvaluationIsChargedToTheMatchBoundLikeEveryOtherExpression()
	{
		string withUse = ShellProbe.Source(ShellProbe.CLIENT_CALL_LINE, _PROMPT_LINE, _PLAIN_USE_LINE);
		ShellProbeAnalyzer analyzer = ShellProbe.AnalyzerWith([ShellProbe.PromptFeedingAPasswordRead]);
		CountingTimeProvider withUseClock = new();
		CountingTimeProvider withoutUseClock = new();

		ShellProbe.Recognize(analyzer, withUse, timeProvider: withUseClock);
		ShellProbe.Recognize(analyzer, ShellProbe.Source(ShellProbe.CLIENT_CALL_LINE, _PROMPT_LINE), timeProvider: withoutUseClock);

		Assert.True(withUseClock.Reads > withoutUseClock.Reads);
	}

	[Fact]
	public void ASudoThatPrecedesThePromptOnTheSameLineIsNotAUseOfItsVariable()
	{
		FlowEvidence evidence = EvidenceOf("sudo -S true; read -p \"Password: \" PW; echo \"$PW\"");

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Theory]
	[InlineData("echo \"$PW\" | sudo -S true")]
	[InlineData("sudo -S true <<< \"$PW\"")]
	[InlineData("sudo -S true; echo done; echo \"$PW\" | sudo -S true")]
	public void APipeIsNotASeparatorAndAUseInTheSameSegmentStillSuppresses(string useLine)
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, useLine);

		Assert.Equal(FlowEvidence.None, evidence);
	}

	[Theory]
	[InlineData("sudo -S true; echo \"$PW\"")]
	[InlineData("sudo -S true && echo \"$PW\"")]
	[InlineData("sudo -S true || echo \"$PW\"")]
	[InlineData("sudo -S true & echo \"$PW\"")]
	[InlineData("echo \"$PW\" && sudo -S true")]
	[InlineData("echo \"$PW\" ; sudo -S true")]
	public void EachDeclaredSeparatorEndsTheSegmentThatHoldsTheReference(string useLine)
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, useLine);

		Assert.Equal(FlowEvidence.Interactive, evidence);
	}

	[Fact]
	public void WithoutDeclaredSeparatorsTheWholeLogicalLineIsTestedAsBefore()
	{
		ShellProbeAnalyzer wholeLine = ShellProbe.AnalyzerWith(
			[ShellProbe.PromptFeeding(new FlowSignalVariableUse { UsePattern = ShellProbeExpressions.PasswordOnStandardInput() })]);
		string source = ShellProbe.Source(ShellProbe.CLIENT_CALL_LINE, "sudo -S true; read -p \"Password: \" PW; echo \"$PW\"");

		FlowEvidence evidence = ShellProbe.Recognize(wholeLine, source).Result.FlowEvidence;

		Assert.Equal(FlowEvidence.None, evidence);
	}

	[Fact]
	public void ACopyIsFollowedWithinItsOwnSegment()
	{
		FlowEvidence evidence = EvidenceOf(_PROMPT_LINE, "PW2=$PW; echo \"$PW2\" | sudo -S true");

		Assert.Equal(FlowEvidence.None, evidence);
	}

	private static FlowEvidence EvidenceOf(params string[] lines)
	{
		ShellProbeAnalyzer analyzer = ShellProbe.AnalyzerWith([ShellProbe.PromptFeedingAPasswordRead]);
		string source = ShellProbe.Source([ShellProbe.CLIENT_CALL_LINE, .. lines]);

		return ShellProbe.Recognize(analyzer, source).Result.FlowEvidence;
	}
}
