using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// WHAT THE ENGINE DOES WITH TWO DECLARATIONS OF THE `Shell` ANALYZER THAT DEPEND ON IT: the preselection anchors, one of which holds a blank, and the
/// credential group of the classes that read it from inside a look-ahead.
///
/// A file with no anchor is never analyzed, so every class must bring its anchor: a fixture of each class is a candidate. An anchor is a literal that the
/// candidate test looks for in the decoded text of the file, and the test below shows that a literal holding a blank is accepted by the contract and found by the
/// engine, so HTTPie called with its short option is reached when the file names no other anchor. A group captured inside a look-ahead is part of the match in the
/// regular-expression engine, and the engine reads the credential position from it: the five classes whose credential sits there signal a literal credential.
/// </summary>
public sealed class ShellCandidateSelectionTests
{
	[Fact]
	public void AFixtureOfEveryClassIsACandidateThroughAnAnchor()
	{
		ILanguageAnalyzer analyzer = ShellRegisteredExpressions.Analyzer;

		Assert.All(ShellAnalyzerTests.ClassFixtures(), fixture => Assert.True(CandidatePreselection.IsCandidate(analyzer, fixture.Source), fixture.PatternClass));
	}

	[Theory]
	[InlineData("https -a \"$who:$secret\" \"$host/x\"\n")]
	[InlineData("HTTP -A basic -A BASIC\nHTTPS -A \"$who\" \"$host/x\"\n")]
	[InlineData("http --auth \"$who\" \"$host/x\"\n")]
	[InlineData("http \"$host/x\" 'authorization:Basic dXNlcjpwYXNz'\n")]
	public void AFileThatCallsHttpieAndNamesNoOtherAnchorIsACandidate(string source)
	{
		Assert.True(CandidatePreselection.IsCandidate(ShellRegisteredExpressions.Analyzer, source));
	}

	[Fact]
	public void AFileThatNamesNoAnchorIsNotACandidate()
	{
		Assert.False(CandidatePreselection.IsCandidate(ShellRegisteredExpressions.Analyzer, "echo hello\nls -la\nhttp -v \"$host\"\n"));
	}

	/// <summary>The credential of the classes that read it from a look-ahead is read by the engine: each of the five classes signals a literal credential.</summary>
	[Theory]
	[InlineData("SH-004", "wget --auth-no-challenge --user=u --password=example \"$URL\"\n")]
	[InlineData("SH-004", "wget --auth-no-challenge https://u:example@h.test/x\n")]
	[InlineData("SH-005", "curl https://u:example@h.test/x\n")]
	[InlineData("SH-011", "wget --user=u --password=example \"$URL\"\n")]
	[InlineData("SH-011", "wget https://u:example@h.test/x\n")]
	[InlineData("SH-016", "wget --auth-no-challenge -e http_password=example \"$URL\"\n")]
	[InlineData("SH-017", "wget -e http_password=example \"$URL\"\n")]
	public void ACredentialCapturedInsideALookAheadIsReadByTheEngineAsTheCredentialPosition(string patternClass, string source)
	{
		RecognizedConstruct construct = Assert.Single(new ShellRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == patternClass);

		Assert.True(construct.LiteralCredentialSignal);
	}

	[Fact]
	public void ACredentialGivenInTheOtherOrderOfTheOptionsIsStillRead()
	{
		RecognizedConstruct construct = Assert.Single(
			new ShellRecognitionHarness().Recognize("wget --password=example --user=u --auth-no-challenge \"$URL\"\n"),
			candidate => candidate.PatternClassIdentifier == "SH-004");

		Assert.True(construct.LiteralCredentialSignal);
	}
}
