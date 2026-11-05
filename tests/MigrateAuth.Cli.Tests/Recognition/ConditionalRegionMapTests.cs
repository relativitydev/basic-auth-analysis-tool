using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE REGION MAP OF A `CSharp` FILE, FROM ITS OWN DIRECTIVES AND THE REAL SCANNER'S MASK. Each source holds invented
/// marker statements (<c>H1</c>, <c>H2</c>, ...) in the branches; a test reads which markers lie in an inactive
/// interval. <c>FEAT_OFF</c> is never defined (false), <c>DEBUG</c> is well known (unknown), <c>true</c> and <c>false</c>
/// are the literals.
/// </summary>
public sealed class ConditionalRegionMapTests
{
	private const string _NEWLINE = "\n";
	private const string _MARKER_PREFIX = "H";
	private const int _MARKER_COUNT = 9;
	private const int _MAX_NESTING_DEPTH = ConditionalRegionMap.MAX_NESTING_DEPTH;

	[Theory]
	[InlineData("#if FEAT_OFF\nH1\n#endif", "H1")]
	[InlineData("#if FEAT_OFF\nH1\n#else\nH2\n#endif", "H1")]
	[InlineData("#if true\nH1\n#else\nH2\n#endif", "H2")]
	[InlineData("#if false\nH1\n#else\nH2\n#endif", "H1")]
	[InlineData("#if DEBUG\nH1\n#else\nH2\n#endif", "")]
	[InlineData("#if DEBUG\nH1\n#endif\nH2", "")]
	[InlineData("#if true\nH1\n#endif", "")]
	public void AGroupSwitchesOffExactlyTheBranchesTheFileSaysAreNotTaken(string source, string expectedDeadMarkers)
	{
		Assert.Equal(expectedDeadMarkers, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("#if FEAT_OFF\nH1\n#elif FEAT_OFF\nH2\n#elif true\nH3\n#else\nH4\n#endif", "H1,H2,H4")]
	[InlineData("#if FEAT_OFF\nH1\n#elif DEBUG\nH2\n#elif true\nH3\n#else\nH4\n#endif", "H1,H4")]
	[InlineData("#if DEBUG\nH1\n#elif true\nH2\n#else\nH3\n#endif", "H3")]
	[InlineData("#if true\nH1\n#elif DEBUG\nH2\n#elif FEAT_OFF\nH3\n#else\nH4\n#endif", "H2,H3,H4")]
	[InlineData("#if DEBUG\nH1\n#elif FEAT_OFF\nH2\n#else\nH3\n#endif", "H2")]
	[InlineData("#if DEBUG\nH1\n#elif NET6_0\nH2\n#else\nH3\n#endif", "")]
	[InlineData("#if FEAT_OFF\nH1\n#elif DEBUG\nH2\n#else\nH3\n#endif", "H1")]
	[InlineData("#if FEAT_OFF\nH1\n#elif FEAT_OFF\nH2\n#else\nH3\n#endif", "H1,H2")]
	public void AnElifChainKeepsAnElseAliveUnlessABranchBeforeItWasDefinitelyTaken(string source, string expectedDeadMarkers)
	{
		Assert.Equal(expectedDeadMarkers, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("#if FEAT_OFF\n#if true\nH1\n#endif\nH2\n#endif", "H1,H2")]
	[InlineData("#if DEBUG\n#if FEAT_OFF\nH1\n#endif\nH2\n#endif", "H1")]
	[InlineData("#if DEBUG\n#if NET6_0\nH1\n#endif\nH2\n#endif", "")]
	[InlineData("#if true\n#if FEAT_OFF\nH1\n#else\nH2\n#endif\n#endif", "H1")]
	[InlineData("#if FEAT_OFF\nH1\n#else\n#if FEAT_OFF\nH2\n#else\nH3\n#endif\n#endif", "H1,H2")]
	[InlineData("#if true\n#if true\n#if FEAT_OFF\nH1\n#endif\n#endif\n#endif", "H1")]
	[InlineData("#if FEAT_OFF\nH1\n#endif\n#if FEAT_OFF\nH2\n#endif", "H1,H2")]
	public void NestingCombinesThroughTheEnclosingBranch(string source, string expectedDeadMarkers)
	{
		Assert.Equal(expectedDeadMarkers, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("#define FEAT\n#if FEAT\nH1\n#else\nH2\n#endif", "H2")]
	[InlineData("#define FEAT\n#undef FEAT\n#if FEAT\nH1\n#endif", "H1")]
	[InlineData("#define FEAT\n#undef FEAT\n#define FEAT\n#if FEAT\nH1\n#else\nH2\n#endif", "H2")]
	[InlineData("#if FEAT\nH1\n#endif\n#define FEAT\n#if FEAT\nH2\n#endif", "H1")]
	[InlineData("#undef DEBUG\n#if DEBUG\nH1\n#endif", "H1")]
	[InlineData("#undef NET6_0\n#if NET6_0\nH1\n#endif", "H1")]
	[InlineData("#undef IOS15_0\n#if IOS15_0\nH1\n#endif", "H1")]
	[InlineData("#undef DEBUG\n#define DEBUG\n#if DEBUG\nH1\n#else\nH2\n#endif", "H2")]
	public void ADefineOrAnUndefInLiveCodeActsAtItsOwnPositionInTheFile(string source, string expectedDeadMarkers)
	{
		Assert.Equal(expectedDeadMarkers, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("#if DEBUG\n#define LATER\n#endif\n#if LATER\nH1\n#else\nH2\n#endif", "")]
	[InlineData("#define FEAT\n#if DEBUG\n#undef FEAT\n#endif\n#if FEAT\nH1\n#else\nH2\n#endif", "")]
	[InlineData("#if DEBUG\n#define LATER\n#endif\n#if !LATER\nH1\n#else\nH2\n#endif", "")]
	[InlineData("#if DEBUG\n#define LATER\n#endif\n#if LATER && FEAT_OFF\nH1\n#endif", "H1")]
	public void ADefineOrAnUndefInAMaybeRegionMakesTheSymbolUnknown(string source, string expectedDeadMarkers)
	{
		Assert.Equal(expectedDeadMarkers, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("#if FEAT_OFF\n#define LATER\n#endif\n#if LATER\nH1\n#endif", "H1")]
	[InlineData("#define FEAT\n#if FEAT_OFF\n#undef FEAT\n#endif\n#if FEAT\nH1\n#else\nH2\n#endif", "H2")]
	[InlineData("#if true\nH1\n#else\n#define LATER\n#endif\n#if LATER\nH2\n#endif", "H2")]
	[InlineData("#if FEAT_OFF\n#if true\n#define LATER\n#endif\n#endif\n#if LATER\nH1\n#endif", "H1")]
	public void ADefineOrAnUndefInADeadRegionDoesNothing(string source, string expectedDeadMarkers)
	{
		Assert.Equal(expectedDeadMarkers, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("#if FEAT_OFF &&\nH1\n#endif")]
	[InlineData("#if\nH1\n#endif")]
	[InlineData("#if (FEAT_OFF\nH1\n#endif")]
	[InlineData("#if FEAT_OFF FEAT_OFF\nH1\n#endif")]
	[InlineData("#if FEAT_OFF ? 1 : 2\nH1\n#endif")]
	[InlineData("#if FEAT_OFF &&\nH1\n#else\nH2\n#endif")]
	[InlineData("#if DEBUG\nH1\n#elif FEAT_OFF ||\nH2\n#endif")]
	public void AnUnparsableDirectiveDeactivatesNothing(string source)
	{
		Assert.Equal(string.Empty, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("#endif\nH1", "")]
	[InlineData("#else\nH1\n#endif", "")]
	[InlineData("#elif FEAT_OFF\nH1\n#endif", "")]
	[InlineData("#endif\n#if FEAT_OFF\nH1\n#endif", "H1")]
	[InlineData("#else\n#if FEAT_OFF\nH1\n#endif\n#endif", "H1")]
	[InlineData("#if FEAT_OFF\nH1\n#endif\n#endif\n#if FEAT_OFF\nH2\n#endif", "H1,H2")]
	public void AnElifElseOrEndifWithNoOpenIfIsIgnored(string source, string expectedDeadMarkers)
	{
		Assert.Equal(expectedDeadMarkers, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("#if FEAT_OFF\nH1", "")]
	[InlineData("#if FEAT_OFF\nH1\n#else\nH2", "")]
	[InlineData("#if true\nH1\n#else\nH2", "")]
	[InlineData("#if FEAT_OFF\nH1\n#endif\n#if FEAT_OFF\nH2", "H1")]
	[InlineData("#if DEBUG\n#if FEAT_OFF\nH1\n#endif\nH2", "H1")]
	[InlineData("#if FEAT_OFF\nH1\n#if FEAT_OFF\nH2\n#endif", "")]
	public void AGroupWithNoEndifContributesNoInterval(string source, string expectedDeadMarkers)
	{
		Assert.Equal(expectedDeadMarkers, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("#if FEAT_OFF\nH1\n#else\nH2\n#else\nH3\n#endif")]
	[InlineData("#if FEAT_OFF\nH1\n#else\nH2\n#elif true\nH3\n#endif")]
	public void AGroupWithAnElseFollowedByAnotherBranchContributesNoInterval(string source)
	{
		Assert.Equal(string.Empty, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("#if FEAT_OFF\nvar text = @\"\n#endif\n\";\nH1\n#endif", "H1")]
	[InlineData("#if FEAT_OFF\nvar text = @\"\n#else\n\";\nH1\n#endif", "H1")]
	[InlineData("#if FEAT_OFF\nvar text = \"\"\"\n#endif\n\"\"\";\nH1\n#endif", "H1")]
	[InlineData("#if FEAT_OFF\nvar text = \"\"\"\n#else\nH2\n\"\"\";\nH1\n#endif", "H2,H1")]
	[InlineData("#if FEAT_OFF\n/*\n#endif\n*/\nH1\n#endif", "H1")]
	[InlineData("#if FEAT_OFF\n/*\n#else\n*/\nH1\n#endif", "H1")]
	[InlineData("var text = @\"\n#if FEAT_OFF\n\";\nH1\n", "")]
	[InlineData("var text = \"\"\"\n#if FEAT_OFF\nH1\n\"\"\";\nH2\n", "")]
	[InlineData("/*\n#if FEAT_OFF\n*/\nH1\n", "")]
	public void DirectiveLikeTextInAVerbatimStringAPlainRawStringOrABlockCommentIsNoDirective(string source, string expectedDeadMarkers)
	{
		Assert.Equal(expectedDeadMarkers, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("#if FEAT_OFF // note\nH1\n#else // other\nH2\n#endif // done", "H1")]
	[InlineData("#if FEAT_OFF // #endif\nH1\n#endif", "H1")]
	[InlineData("#if FEAT_OFF /* note */\nH1\n#endif", "H1")]
	[InlineData("#define FEAT // the feature\n#if FEAT\nH1\n#else\nH2\n#endif", "H2")]
	[InlineData("#if DEBUG // FEAT_OFF\nH1\n#endif", "")]
	[InlineData("#if FEAT_OFF || DEBUG // note\nH1\n#endif", "")]
	public void ATrailingCommentIsNotPartOfTheCondition(string source, string expectedDeadMarkers)
	{
		Assert.Equal(expectedDeadMarkers, DeadMarkersOf(source));
	}

	[Theory]
	[InlineData("   \t#if FEAT_OFF\nH1\n  #endif", "H1")]
	[InlineData("#  if FEAT_OFF\nH1\n#  endif", "H1")]
	[InlineData("#if(FEAT_OFF)\nH1\n#endif", "H1")]
	[InlineData("#region Name\n#if FEAT_OFF\nH1\n#endif\n#endregion\n#pragma warning disable", "H1")]
	[InlineData("#ifx FEAT_OFF\nH1\n#endif", "")]
	[InlineData("#nullable enable\n#line 12\n#if FEAT_OFF\nH1\n#endif", "H1")]
	[InlineData("int a = 1; #if FEAT_OFF\nH1\n#endif", "")]
	public void OnlyALineThatStartsWithTheOpeningAndOneOfTheSixKeywordsIsADirective(string source, string expectedDeadMarkers)
	{
		Assert.Equal(expectedDeadMarkers, DeadMarkersOf(source));
	}

	[Fact]
	public void SixtyFourNestedGroupsAreFollowedAndSixtyFiveStopTheReading()
	{
		string atTheCap = Nested("#if DEBUG", _MAX_NESTING_DEPTH - 1, $"#if FEAT_OFF{_NEWLINE}H1{_NEWLINE}#endif");
		string overTheCap = Nested("#if DEBUG", _MAX_NESTING_DEPTH, $"#if FEAT_OFF{_NEWLINE}H1{_NEWLINE}#endif");

		Assert.Equal("H1", DeadMarkersOf(atTheCap));
		Assert.Equal(string.Empty, DeadMarkersOf(overTheCap));
	}

	[Fact]
	public void TheStopKeepsWhatClosedGroupsAlreadyContributedAndDiscardsTheOpenOnes()
	{
		string closedFirst = $"#if FEAT_OFF{_NEWLINE}H1{_NEWLINE}#endif{_NEWLINE}";
		string tooDeep = Nested("#if DEBUG", _MAX_NESTING_DEPTH, $"#if FEAT_OFF{_NEWLINE}H2{_NEWLINE}#endif");

		Assert.Equal("H1", DeadMarkersOf($"{closedFirst}{tooDeep}"));
	}

	[Theory]
	[InlineData("var text = $\"\"\"\n#if FEAT_OFF\nH1\n#endif\n\"\"\";")]
	[InlineData("var text = $$\"\"\"\n#if FEAT_OFF\nH1\n#endif\n\"\"\";")]
	[InlineData("var text = $$$\"\"\"{{{x}}}\"\"\";\n#if FEAT_OFF\nH1\n#endif\n")]
	[InlineData("#if FEAT_OFF\nH1\n#endif\nvar text = $\"\"\"{x}\"\"\";")]
	[InlineData("var text = $$\"{{x}}\";\n#if FEAT_OFF\nH1\n#endif\n")]
	public void AFileThatHoldsAMaskGapLiteralHasNoMapAtAll(string source)
	{
		Assert.Empty(MapOf(source).Inactive);
	}

	[Theory]
	[InlineData("var text = $\"{x}\";\n#if FEAT_OFF\nH1\n#endif\n")]
	[InlineData("var text = \"\"\"\nplain\n\"\"\";\n#if FEAT_OFF\nH1\n#endif\n")]
	[InlineData("var text = \"price $$ and $\\\"\";\n#if FEAT_OFF\nH1\n#endif\n")]
	[InlineData("// $$\"\"\"\n#if FEAT_OFF\nH1\n#endif\n")]
	[InlineData("var text = @\"$$\"\"\";\n#if FEAT_OFF\nH1\n#endif\n")]
	[InlineData("var text = $@\"{x}\";\n#if FEAT_OFF\nH1\n#endif\n")]
	public void AnOrdinaryInterpolatedStringAPlainRawStringAndDollarsInsideStringsOrCommentsKeepTheMap(string source)
	{
		Assert.Equal("H1", DeadMarkersOf(source));
	}

	[Fact]
	public void NothingPastTheTrustExtentIsRead()
	{
		string source = "#if FEAT_OFF\nH1\n#endif\n/* never closed\n#if FEAT_OFF\nH2\n#endif\n";

		Assert.Equal("H1", DeadMarkersOf(source));
	}

	[Fact]
	public void AnIntervalRunsFromTheLineAfterTheDirectiveToTheStartOfTheNextDirectiveLine()
	{
		ImmutableArray<(int Start, int End)> inactive = MapOf("#if FEAT_OFF\nH1\n#endif\n").Inactive;

		(int start, int end) = Assert.Single(inactive);

		Assert.Equal(13, start);
		Assert.Equal(16, end);
	}

	[Fact]
	public void TheDirectiveLinesThemselvesAreNotInsideAnInterval()
	{
		string source = "#if FEAT_OFF\nH1\n#else\nH2\n#endif\n";
		ConditionalRegionMap map = MapOf(source);

		Assert.False(map.IsInactiveAt(source.IndexOf("#if", StringComparison.Ordinal)));
		Assert.False(map.IsInactiveAt(source.IndexOf("#else", StringComparison.Ordinal)));
		Assert.False(map.IsInactiveAt(source.IndexOf("#endif", StringComparison.Ordinal)));
		Assert.True(map.IsInactiveAt(source.IndexOf("H1", StringComparison.Ordinal)));
		Assert.False(map.IsInactiveAt(source.IndexOf("H2", StringComparison.Ordinal)));
	}

	[Fact]
	public void TheIntervalsAreSortedAndDisjointForAnyNesting()
	{
		string source = string.Join(
			_NEWLINE,
			"#if DEBUG", "H1", "#if FEAT_OFF", "H2", "#else", "H3", "#endif", "#elif FEAT_OFF", "H4", "#else", "H5", "#endif",
			"#if FEAT_OFF", "H6", "#if true", "H7", "#endif", "#elif true", "H8", "#else", "H9", "#endif", string.Empty);
		ImmutableArray<(int Start, int End)> inactive = MapOf(source).Inactive;

		Assert.NotEmpty(inactive);

		for (int index = 1; index < inactive.Length; index++)
		{
			Assert.True(inactive[index - 1].End <= inactive[index].Start);
		}
	}

	[Fact]
	public void AProfileThatDeclaresNoElementGetsTheEmptyMap()
	{
		CSharpRecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor("#if FEAT_OFF\nH1\n#endif\n");

		ConditionalRegionMap map = ConditionalRegionMap.Build(null, context.NormalizedText, context.Mask);

		Assert.Same(ConditionalRegionMap.Empty, map);
		Assert.False(map.IsInactiveAt(0));
		Assert.False(map.IsInactiveAt(int.MaxValue));
	}

	[Fact]
	public void OnlyTheCSharpProfileDeclaresTheElement()
	{
		ConditionalCompilationRule rule = Assert.IsType<ConditionalCompilationRule>(
			new CSharpRecognitionHarness().Analyzer.LexicalProfile.ConditionalCompilation);

		Assert.Equal("#", rule.DirectiveOpening);
		Assert.NotEmpty(rule.WellKnownSymbolWords);
	}

	[Fact]
	public void AFileWithNoDirectiveAtAllGetsTheEmptyMap()
	{
		Assert.Same(ConditionalRegionMap.Empty, MapOf("class Plain { }\n"));
	}

	[Fact]
	public void ALookupOutsideEveryIntervalIsFalse()
	{
		ConditionalRegionMap map = MapOf("#if FEAT_OFF\nH1\n#endif\n");

		Assert.False(map.IsInactiveAt(0));
		Assert.False(map.IsInactiveAt(12));
		Assert.True(map.IsInactiveAt(13));
		Assert.True(map.IsInactiveAt(15));
		Assert.False(map.IsInactiveAt(16));
		Assert.False(map.IsInactiveAt(1000));
	}

	private static string Nested(string opening, int depth, string innermost)
	{
		string opened = string.Concat(Enumerable.Repeat($"{opening}{_NEWLINE}", depth));
		string closed = string.Concat(Enumerable.Repeat($"#endif{_NEWLINE}", depth));

		return $"{opened}{innermost}{_NEWLINE}{closed}";
	}

	private static ConditionalRegionMap MapOf(string source)
	{
		CSharpRecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(source);

		return ConditionalRegionMap.Build(harness.Analyzer.LexicalProfile.ConditionalCompilation, context.NormalizedText, context.Mask);
	}

	/// <summary>The markers (<c>H1</c>, <c>H2</c>, ...) of the source that lie in an inactive interval, in the order the source writes them.</summary>
	private static string DeadMarkersOf(string source)
	{
		CSharpRecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(source);
		ConditionalRegionMap map = ConditionalRegionMap.Build(
			harness.Analyzer.LexicalProfile.ConditionalCompilation, context.NormalizedText, context.Mask);
		List<(int Position, string Marker)> dead = [];

		for (int number = 1; number <= _MARKER_COUNT; number++)
		{
			string marker = $"{_MARKER_PREFIX}{number}";
			int position = context.NormalizedText.IndexOf(marker, StringComparison.Ordinal);

			if (position >= 0 && map.IsInactiveAt(position))
			{
				dead.Add((position, marker));
			}
		}

		return string.Join(",", dead.OrderBy(entry => entry.Position).Select(entry => entry.Marker));
	}
}
