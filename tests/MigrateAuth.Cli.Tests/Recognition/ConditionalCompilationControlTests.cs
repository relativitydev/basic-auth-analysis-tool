using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE CONTROL TABLE OF THE CONDITIONAL-COMPILATION READING, ONE UNIT TEST PER ROW (ONE PER VARIANT OF THE OPERATOR ROW AND OF THE
/// NESTING ROW, AND ONE FOR EACH OF THE TWO LITERALS OF THE LAST ROW), IN THE TABLE'S ORDER, THROUGH THE REAL EMBEDDED `CSharp` ANALYZER.
/// The test <c>ControlNN</c> is the table's row NN. A header-carrying write of a High class is written once, and a second one where the row
/// has two; every name in a fixture is invented. A row expecting the class level reads High with no reason; a row expecting Low reads Low
/// with the reason <see cref="LowConfidenceReasons.InactivePreprocessorRegion"/> and nothing else. <c>FEAT_OFF</c> is never defined in the file.
/// </summary>
public sealed class ConditionalCompilationControlTests
{
	private const string _NEWLINE = "\n";
	private const string _HEADER_WRITE = "var firstAuthenticator = new Vendor.Http.HttpBasicAuthenticator(firstUserName, firstPassword);";
	private const string _SECOND_HEADER_WRITE = "var secondAuthenticator = new Vendor.Http.HttpBasicAuthenticator(secondUserName, secondPassword);";
	private const string _END_OF_GROUP = "#endif";

	[Fact]
	public void Control01ADefineBeforeTheIfMakesTheBlockLiveAndTheClassLevelStays()
	{
		AssertClassLevel(Lines("#define FEAT", "#if FEAT", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control02ADebugSymbolBlockIsMaybeAndTheClassLevelStays()
	{
		AssertClassLevel(Lines("#if DEBUG", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control03AFrameworkFamilySymbolBlockIsMaybeAndTheClassLevelStays()
	{
		AssertClassLevel(Lines("#if NET6_0_OR_GREATER", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control04APlatformWordBlockIsMaybeAndTheClassLevelStays()
	{
		AssertClassLevel(Lines("#if ANDROID", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control05AnExactVersionPlatformSymbolBlockIsMaybeAndNotDead()
	{
		AssertClassLevel(Lines("#if IOS15_0", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control06TheElseOfANeverDefinedSymbolIsLiveAndTheIfBranchIsLow()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(
			Lines("#if FEAT_OFF", _HEADER_WRITE, "#else", _SECOND_HEADER_WRITE, _END_OF_GROUP));

		Assert.Equal(2, constructs.Length);
		AssertInactive(constructs[0]);
		AssertClassLevel(constructs[1]);
	}

	[Fact]
	public void Control07BothBranchesOfADebugSymbolGroupAreMaybeAndKeepTheClassLevel()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(
			Lines("#if DEBUG", _HEADER_WRITE, "#else", _SECOND_HEADER_WRITE, _END_OF_GROUP));

		Assert.Equal(2, constructs.Length);
		Assert.All(constructs, AssertClassLevel);
	}

	[Fact]
	public void Control08ABranchAfterADefinitelyTrueOneIsDeadAndLow()
	{
		AssertInactive(Lines("#if true", "var unrelated = 1;", "#else", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control09AnElifAfterADefinitelyTrueBranchIsDeadAndLow()
	{
		AssertInactive(Lines("#if true", "var unrelated = 1;", "#elif DEBUG", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control10ASymbolUndefinedInLiveCodeMakesItsBlockDeadAndLow()
	{
		AssertInactive(Lines("#undef DEBUG", "#if DEBUG", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control11ADefineInADeadRegionDefinesNothing()
	{
		AssertInactive(Lines("#if FEAT_OFF", "#define LATER", _END_OF_GROUP, "#if LATER", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control12ADefineInAMaybeRegionMakesTheSymbolUnknownSoBothBranchesKeepTheClassLevel()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(
			Lines("#if DEBUG", "#define LATER", _END_OF_GROUP, "#if LATER", _HEADER_WRITE, "#else", _SECOND_HEADER_WRITE, _END_OF_GROUP));

		Assert.Equal(2, constructs.Length);
		Assert.All(constructs, AssertClassLevel);
	}

	[Fact]
	public void Control13AFalseElifAfterAMaybeBranchIsDeadAndLow()
	{
		AssertInactive(Lines("#if DEBUG", "var unrelated = 1;", "#elif FEAT_OFF", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control14AnUnknownElifAfterAMaybeBranchIsMaybeAndKeepsTheClassLevel()
	{
		AssertClassLevel(Lines("#if DEBUG", "var unrelated = 1;", "#elif NET6_0", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control15aAnOrWithAnUnknownSideIsMaybeAndKeepsTheClassLevel()
	{
		AssertClassLevel(Lines("#if FEAT_OFF || DEBUG", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control15bAnAndWithAFalseSideIsDeadAndLow()
	{
		AssertInactive(Lines("#if FEAT_OFF && DEBUG", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control15cTheNegationOfANeverDefinedSymbolIsLiveAndKeepsTheClassLevel()
	{
		AssertClassLevel(Lines("#if !FEAT_OFF", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control15dAnEqualityWithFalseOnANeverDefinedSymbolIsLiveAndKeepsTheClassLevel()
	{
		AssertClassLevel(Lines("#if FEAT_OFF == false", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control15eAnInequalityWithTrueOnAnUnknownSymbolIsMaybeAndKeepsTheClassLevel()
	{
		AssertClassLevel(Lines("#if DEBUG != true", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control16aANeverDefinedSymbolNestedInAMaybeBlockIsDeadAndLow()
	{
		AssertInactive(Lines("#if DEBUG", "#if FEAT_OFF", _HEADER_WRITE, _END_OF_GROUP, _END_OF_GROUP));
	}

	[Fact]
	public void Control16bAnUnknownSymbolNestedInAMaybeBlockIsMaybeAndKeepsTheClassLevel()
	{
		AssertClassLevel(Lines("#if DEBUG", "#if NET6_0", _HEADER_WRITE, _END_OF_GROUP, _END_OF_GROUP));
	}

	[Fact]
	public void Control17AGroupWithNoEndifBeforeTheEndOfTheFileContributesNoIntervalAndKeepsTheClassLevel()
	{
		AssertClassLevel(Lines("#if FEAT_OFF", _HEADER_WRITE));
	}

	[Fact]
	public void Control18AnUnparsableConditionDeactivatesNothing()
	{
		AssertClassLevel(Lines("#if FEAT_OFF &&", _HEADER_WRITE, _END_OF_GROUP));
	}

	[Fact]
	public void Control19aADirectiveLikeTextInAVerbatimStringIsNotADirective()
	{
		AssertClassLevel(Lines("var note = @\"", "#if FEAT_OFF", "\";", _HEADER_WRITE));
	}

	[Fact]
	public void Control19bADirectiveLikeTextInAPlainRawStringIsNotADirective()
	{
		AssertClassLevel(Lines("var note = \"\"\"", "#if FEAT_OFF", "\"\"\";", _HEADER_WRITE));
	}

	private static string Lines(params string[] lines)
	{
		return $"{string.Join(_NEWLINE, lines)}{_NEWLINE}";
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(string source)
	{
		return new CSharpRecognitionHarness().Recognize(source);
	}

	private static void AssertClassLevel(string source)
	{
		AssertClassLevel(Assert.Single(Recognize(source)));
	}

	private static void AssertInactive(string source)
	{
		AssertInactive(Assert.Single(Recognize(source)));
	}

	private static void AssertClassLevel(RecognizedConstruct construct)
	{
		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
	}

	private static void AssertInactive(RecognizedConstruct construct)
	{
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.InactivePreprocessorRegion, construct.LowReasons);
	}
}
