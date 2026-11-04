using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// WHAT A CONSTRUCT IN AN EXECUTED STRING IS RECOGNIZED AS, over a real scan and the real recognition engine, with the
/// test-local analyzer whose two rules sit at the High level. The construct inside a string that a shell runs takes the
/// rule's own level; the same text inside a string no command runs, or under a profile that does not declare the
/// context, takes the Low level that an anchor inside quotes proposes. A literal pair in the body sets the
/// literal-credential signal as it would in code.
/// </summary>
public sealed class ExecutedStringRecognitionTests
{
	[Theory]
	[InlineData("eval \"curl -u user:pass host\"\n")]
	[InlineData("eval 'curl -u user:pass host'\n")]
	[InlineData("bash -c 'curl -u user:pass host'\n")]
	[InlineData("sh -c \"curl -u user:pass host\"\n")]
	[InlineData("ssh host \"curl -u user:pass host\"\n")]
	[InlineData("su -c \"curl -u user:pass host\"\n")]
	[InlineData("echo \"out: `curl -u user:pass host`\"\n")]
	[InlineData("echo \"${V:-curl -u user:pass host}\"\n")]
	[InlineData("bash <<EOF\ncurl -u user:pass host\nEOF\n")]
	[InlineData("sh <<'EOF'\ncurl -u user:pass host\nEOF\n")]
	[InlineData("sudo -u deploy bash <<EOF\ncurl -u user:pass host\nEOF\n")]
	[InlineData("ssh host <<EOF\ncurl -u user:pass host\nEOF\n")]
	[InlineData("cat > run.sh <<EOF\ncurl -u user:pass host\nEOF\n")]
	[InlineData("cat > notes.txt <<EOF\n#!/bin/sh\ncurl -u user:pass host\nEOF\n")]
	[InlineData("ssh host \"bash -c 'curl -u user:pass host'\"\n")]
	public void AConstructInAnExecutedStringTakesItsClassLevel(string source)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(source, declares: true), found => found.PatternClassIdentifier == ExecutedStringsAnalyzer.BARE_WORD_PATTERN_CLASS);
		RecognizedConstruct control = Assert.Single(Recognize(source, declares: false), found => found.PatternClassIdentifier == ExecutedStringsAnalyzer.BARE_WORD_PATTERN_CLASS);

		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
		Assert.Equal(ConfidenceLevel.Low, control.Level);
		Assert.Equal(LowConfidenceReasons.CommentOrStringLiteral, control.LowReasons);
	}

	[Theory]
	[InlineData("echo \"curl -u user:pass host\"\n")]
	[InlineData("cat <<EOF\ncurl -u user:pass host\nEOF\n")]
	[InlineData("cat > notes.txt <<EOF\ncurl -u user:pass host\nEOF\n")]
	[InlineData("python3 <<EOF\ncurl -u user:pass host\nEOF\n")]
	[InlineData("bash script.sh <<EOF\ncurl -u user:pass host\nEOF\n")]
	[InlineData("cat > notes.txt <<EOF\n#!/usr/bin/env python3\ncurl -u user:pass host\nEOF\n")]
	[InlineData("echo bash -c \"curl -u user:pass host\"\n")]
	public void AConstructInATextNoCommandRunsStaysLow(string source)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(source, declares: true), found => found.PatternClassIdentifier == ExecutedStringsAnalyzer.BARE_WORD_PATTERN_CLASS);

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.CommentOrStringLiteral, construct.LowReasons);
	}

	[Fact]
	public void ALiteralPairInAnExecutedBodySetsTheLiteralCredentialSignalLikeAnyCode()
	{
		RecognizedConstruct bareWord = Single("eval \"curl -u user:pass host\"\n", ExecutedStringsAnalyzer.BARE_WORD_PATTERN_CLASS, declares: true);

		Assert.True(bareWord.LiteralCredentialSignal);
	}

	[Fact]
	public void AnOrdinaryRuleReadsABareWordPairInAnExecutedBodyAsCodeAndSoNotLiteral()
	{
		RecognizedConstruct inBody = Single("eval \"curl -u user:pass host\"\n", ExecutedStringsAnalyzer.ORDINARY_PATTERN_CLASS, declares: true);
		RecognizedConstruct inString = Single("eval \"curl -u user:pass host\"\n", ExecutedStringsAnalyzer.ORDINARY_PATTERN_CLASS, declares: false);

		Assert.False(inBody.LiteralCredentialSignal);
		Assert.True(inString.LiteralCredentialSignal);
	}

	[Fact]
	public void AConstructAfterTheExecutedBodyInTheSameFileIsUnaffected()
	{
		const string source = "bash <<EOF\ncurl -u user:pass host\nEOF\ncurl -u other:word host\n";

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source, declares: true);
		ImmutableArray<RecognizedConstruct> bareWord = [.. constructs.Where(found => found.PatternClassIdentifier == ExecutedStringsAnalyzer.BARE_WORD_PATTERN_CLASS)];

		Assert.Equal(2, bareWord.Length);
		Assert.All(bareWord, found => Assert.Equal(ConfidenceLevel.High, found.Level));
	}

	[Fact]
	public void TheAnchorOffsetOfAConstructInAnEscapedBodyIsItsPositionInTheFile()
	{
		const string source = "bash -c \"echo \\\"x\\\"; curl -u user:pass host\"\n";

		RecognizedConstruct construct = Single(source, ExecutedStringsAnalyzer.BARE_WORD_PATTERN_CLASS, declares: true);

		Assert.Equal(source.IndexOf("curl", StringComparison.Ordinal), construct.AnchorOffset);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	private static RecognizedConstruct Single(string source, string patternClass, bool declares)
	{
		return Assert.Single(Recognize(source, declares), found => found.PatternClassIdentifier == patternClass);
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(string source, bool declares)
	{
		ExecutedStringsAnalyzer analyzer = new(declares);
		RecognitionHarness harness = new();
		string normalizedText = TextNormalization.Normalize(source);
		RepositoryRelativePath path = harness.PathOf("source/script.esx");
		ZoneMask mask = new FileLexicalScanner(harness.EmissionPoint).Scan(analyzer.LexicalProfile, normalizedText, path);
		FileRecognitionContext context = new()
		{
			Analyzer = analyzer,
			NormalizedText = normalizedText,
			Mask = mask,
			Path = path
		};

		return new RecognitionEngine(harness.EmissionPoint, new FrozenTimeProvider()).Recognize(context).Constructs;
	}
}
