using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE TWO READERS OF THE CONDITIONAL-REGION MAP BESIDE THE USE SITE, THROUGH THE REAL EMBEDDED `CSharp` ANALYZER: the file symbol table (a constant
/// declared in a branch the file switches off is not collected) and the flow evidence (a signal written in such a branch is not evidence).
/// One test per row of the constant table of the amendment, one per kind of dead branch for the signals, the controls that stay unchanged
/// (a branch the tool cannot decide, a live branch), and the engine's own two facts: the map is built once and shared, and it is empty for
/// every language that declares no conditional-compilation element. <c>FEAT_OFF</c> is never defined in a file; <c>DEBUG</c> is a branch the tool cannot decide.
/// Every name in a fixture is invented.
/// </summary>
public sealed class ConditionalRegionReadersTests
{
	private const string _NEWLINE = "\n";
	private const string _CS_003 = "CS-003";
	private const string _SCHEME_NAME = "SCHEME";
	private const string _BASIC_DECLARATION = "const string SCHEME = \"Basic\";";
	private const string _DIGEST_DECLARATION = "const string SCHEME = \"Digest\";";
	private const string _CACHE_ADD_BY_NAME = "cache.Add(endpoint, SCHEME, new NetworkCredential(userName, password));";
	private const string _CONSTRUCT_WITH_LITERAL_SCHEME = "cache.Add(new Uri(instanceUrl), \"Basic\", new NetworkCredential(userName, password));";
	private const string _INTERACTIVE_SIGNAL = "string typedPassword = Console.ReadLine();";
	private const string _FIXED_SIGNAL = "string storedSecret = configuration[\"Service:ClientSecret\"];";
	private const string _END_OF_GROUP = "#endif";
	private const string _LANGUAGE_NAME_WITH_THE_ELEMENT = "CSharp";
	private const string _SOURCE_WITH_A_DIRECTIVE = "#if FEAT_OFF\nx\n#endif\n";
	private const int _TWO_DECLARATIONS = 2;
	private const int _ONE_DECLARATION = 1;
	private const int _NO_DECLARATION = 0;
	private const int _ONE_FILE_RECOGNIZED = 1;
	private const int _TWO_FILES_RECOGNIZED = 2;

	// ---- the symbol table: one test per row of the table ----

	[Fact]
	public void Row1ANameDeclaredOnceInALiveBranchAndOnceInADeadBranchResolvesToTheLiveOne()
	{
		string deadDigest = Lines("#if FEAT_OFF", _DIGEST_DECLARATION, "#else", _BASIC_DECLARATION, _END_OF_GROUP, _CACHE_ADD_BY_NAME);
		string deadBasic = Lines("#if FEAT_OFF", _BASIC_DECLARATION, "#else", _DIGEST_DECLARATION, _END_OF_GROUP, _CACHE_ADD_BY_NAME);

		Assert.Equal(_ONE_DECLARATION, TableOf(deadDigest).DeclarationCountOf(_SCHEME_NAME));
		AssertSingleLevel(deadDigest, ConfidenceLevel.High);
		Assert.Empty(Recognize(deadBasic));
	}

	[Fact]
	public void Row1TheDeadBranchMayBeAnIfFalseADefinedSymbolOrANestedDeadBranch()
	{
		string ifFalse = Lines("#if false", _DIGEST_DECLARATION, "#else", _BASIC_DECLARATION, _END_OF_GROUP, _CACHE_ADD_BY_NAME);
		string defined = Lines("#define FEAT_ON", "#if FEAT_ON", _BASIC_DECLARATION, "#else", _DIGEST_DECLARATION, _END_OF_GROUP, _CACHE_ADD_BY_NAME);
		string nested = Lines("#if DEBUG", "#if FEAT_OFF", _DIGEST_DECLARATION, _END_OF_GROUP, _END_OF_GROUP, _BASIC_DECLARATION, _CACHE_ADD_BY_NAME);

		AssertSingleLevel(ifFalse, ConfidenceLevel.High);
		AssertSingleLevel(defined, ConfidenceLevel.High);
		AssertSingleLevel(nested, ConfidenceLevel.High);
	}

	[Fact]
	public void Row2ANameDeclaredOnlyInADeadBranchIsIndeterminate()
	{
		string source = Lines("#if FEAT_OFF", _BASIC_DECLARATION, _END_OF_GROUP, _CACHE_ADD_BY_NAME);

		Assert.Equal(_NO_DECLARATION, TableOf(source).DeclarationCountOf(_SCHEME_NAME));
		Assert.False(TableOf(source).TryResolveUnambiguously(_SCHEME_NAME, source.Length, out _));
		AssertSingleLevel(source, ConfidenceLevel.Medium);
	}

	[Fact]
	public void Row3ANameDeclaredInEachBranchOfAnUndecidableConditionalStaysIndeterminate()
	{
		string source = Lines("#if DEBUG", _BASIC_DECLARATION, "#else", _DIGEST_DECLARATION, _END_OF_GROUP, _CACHE_ADD_BY_NAME);

		Assert.Equal(_TWO_DECLARATIONS, TableOf(source).DeclarationCountOf(_SCHEME_NAME));
		AssertSingleLevel(source, ConfidenceLevel.Medium);
	}

	[Fact]
	public void Row4ANameDeclaredOnceInAnUndecidableOrALiveBranchResolves()
	{
		string undecidable = Lines("#if DEBUG", _BASIC_DECLARATION, _END_OF_GROUP, _CACHE_ADD_BY_NAME);
		string live = Lines("#define FEAT_ON", "#if FEAT_ON", _BASIC_DECLARATION, _END_OF_GROUP, _CACHE_ADD_BY_NAME);

		Assert.Equal(_ONE_DECLARATION, TableOf(undecidable).DeclarationCountOf(_SCHEME_NAME));
		AssertSingleLevel(undecidable, ConfidenceLevel.High);
		AssertSingleLevel(live, ConfidenceLevel.High);
	}

	[Fact]
	public void Row5ANameDeclaredTwiceOutsideAnyDirectiveStaysIndeterminate()
	{
		string source = Lines(_BASIC_DECLARATION, _DIGEST_DECLARATION, _CACHE_ADD_BY_NAME);

		Assert.Equal(_TWO_DECLARATIONS, TableOf(source).DeclarationCountOf(_SCHEME_NAME));
		AssertSingleLevel(source, ConfidenceLevel.Medium);
	}

	[Fact]
	public void Row6AFileWithNoRegionMapCountsEveryDeclarationAsBefore()
	{
		string source = Lines("var note = $$$\"\"\"{{{x}}}\"\"\";", "#if FEAT_OFF", _DIGEST_DECLARATION, "#else", _BASIC_DECLARATION, _END_OF_GROUP, _CACHE_ADD_BY_NAME);
		CSharpRecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(source);

		Assert.Same(ConditionalRegionMap.Empty, RegionsOf(harness, context));
		Assert.Equal(_TWO_DECLARATIONS, TableOf(source).DeclarationCountOf(_SCHEME_NAME));
		AssertSingleLevel(source, ConfidenceLevel.Medium);
	}

	[Fact]
	public void ADeclarationAfterTheUseInALiveBranchStillDoesNotResolve()
	{
		string source = Lines("#if FEAT_OFF", _DIGEST_DECLARATION, "#else", _CACHE_ADD_BY_NAME, _BASIC_DECLARATION, _END_OF_GROUP);

		AssertSingleLevel(source, ConfidenceLevel.Medium);
	}

	[Fact]
	public void AFindingAndItsConstantBothInOneDeadBlockReportsAtLowByRegion()
	{
		string source = Lines("#if FEAT_OFF", _DIGEST_DECLARATION, _CACHE_ADD_BY_NAME, _END_OF_GROUP);

		RecognizedConstruct construct = Assert.Single(Recognize(source));

		Assert.Equal(_CS_003, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.InactivePreprocessorRegion, construct.LowReasons);
	}

	[Fact]
	public void TheUseSiteIsUnchangedAConstructInADeadBlockStillReadsLowByRegion()
	{
		string source = Lines("#if FEAT_OFF", _CONSTRUCT_WITH_LITERAL_SCHEME, _END_OF_GROUP);

		RecognizedConstruct construct = Assert.Single(Recognize(source));

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.InactivePreprocessorRegion, construct.LowReasons);
	}

	[Fact]
	public void ADeclarationIsCollectedOrNotByTheMapPassedToTheTable()
	{
		string source = Lines("#if FEAT_OFF", _BASIC_DECLARATION, _END_OF_GROUP);
		CSharpRecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(source);
		ConditionalRegionMap regions = RegionsOf(harness, context);

		Assert.NotEmpty(regions.Inactive);
		Assert.Equal(_NO_DECLARATION, FileSymbolTable.Collect(context.Analyzer.LexicalProfile, context.NormalizedText, context.Mask, regions).DeclarationCountOf(_SCHEME_NAME));
		Assert.Equal(_ONE_DECLARATION, FileSymbolTable.Collect(context.Analyzer.LexicalProfile, context.NormalizedText, context.Mask).DeclarationCountOf(_SCHEME_NAME));
		Assert.Equal(_ONE_DECLARATION, FileSymbolTable.Collect(context.Analyzer.LexicalProfile, context.NormalizedText, context.Mask, ConditionalRegionMap.Empty).DeclarationCountOf(_SCHEME_NAME));
	}

	// ---- the flow evidence ----

	[Theory]
	[InlineData("#if FEAT_OFF")]
	[InlineData("#if false")]
	[InlineData("#if !true")]
	public void AnInteractiveSignalInADeadBranchIsNotEvidence(string opening)
	{
		string source = Lines(_CONSTRUCT_WITH_LITERAL_SCHEME, opening, _INTERACTIVE_SIGNAL, _END_OF_GROUP);

		Assert.Equal(FlowEvidence.None, EvidenceOf(source));
	}

	[Fact]
	public void AnInteractiveSignalInTheDeadBranchOfAnIfWithAnElseIsNotEvidenceAndTheLiveFixedSignalDecides()
	{
		string source = Lines(_CONSTRUCT_WITH_LITERAL_SCHEME, "#if FEAT_OFF", _INTERACTIVE_SIGNAL, "#else", _FIXED_SIGNAL, _END_OF_GROUP);

		Assert.Equal(FlowEvidence.Fixed, EvidenceOf(source));
	}

	[Fact]
	public void TheDeadSignalIsNotEvidenceWhateverTheLiveSignalOfTheOtherKindIs()
	{
		string deadInteractiveLiveFixed = Lines(_CONSTRUCT_WITH_LITERAL_SCHEME, "#if FEAT_OFF", _INTERACTIVE_SIGNAL, _END_OF_GROUP, _FIXED_SIGNAL);
		string deadFixedLiveInteractive = Lines(_CONSTRUCT_WITH_LITERAL_SCHEME, "#if FEAT_OFF", _FIXED_SIGNAL, _END_OF_GROUP, _INTERACTIVE_SIGNAL);
		string deadFixedAlone = Lines(_CONSTRUCT_WITH_LITERAL_SCHEME, "#if FEAT_OFF", _FIXED_SIGNAL, _END_OF_GROUP);

		Assert.Equal(FlowEvidence.Fixed, EvidenceOf(deadInteractiveLiveFixed));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOf(deadFixedLiveInteractive));
		Assert.Equal(FlowEvidence.None, EvidenceOf(deadFixedAlone));
	}

	[Fact]
	public void ASignalInAnUndecidableBranchStillCounts()
	{
		Assert.Equal(FlowEvidence.Interactive, EvidenceOf(Lines(_CONSTRUCT_WITH_LITERAL_SCHEME, "#if DEBUG", _INTERACTIVE_SIGNAL, _END_OF_GROUP)));
	}

	[Fact]
	public void ASignalInALiveBranchStillCounts()
	{
		Assert.Equal(FlowEvidence.Interactive, EvidenceOf(Lines(_CONSTRUCT_WITH_LITERAL_SCHEME, "#define FEAT_ON", "#if FEAT_ON", _INTERACTIVE_SIGNAL, _END_OF_GROUP)));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOf(Lines(_CONSTRUCT_WITH_LITERAL_SCHEME, "#if !FEAT_OFF", _INTERACTIVE_SIGNAL, _END_OF_GROUP)));
	}

	[Fact]
	public void ASignalInTheElseOfADeadBranchStillCounts()
	{
		Assert.Equal(FlowEvidence.Interactive, EvidenceOf(Lines(_CONSTRUCT_WITH_LITERAL_SCHEME, "#if FEAT_OFF", _FIXED_SIGNAL, "#else", _INTERACTIVE_SIGNAL, _END_OF_GROUP)));
	}

	[Fact]
	public void ASignalInAFileWithNoRegionMapStillCountsWhereverItIs()
	{
		string source = Lines(_CONSTRUCT_WITH_LITERAL_SCHEME, "var note = $$$\"\"\"{{{x}}}\"\"\";", "#if FEAT_OFF", _INTERACTIVE_SIGNAL, _END_OF_GROUP);

		Assert.Equal(FlowEvidence.Interactive, EvidenceOf(source));
	}

	// ---- the engine ----

	[Fact]
	public void TheEngineBuildsTheRegionMapOncePerFileAndSharesItWithTheTableAndTheFlowEvaluation()
	{
		CSharpRecognitionHarness harness = new();
		string source = Lines("#if FEAT_OFF", _INTERACTIVE_SIGNAL, _END_OF_GROUP, _BASIC_DECLARATION, _CACHE_ADD_BY_NAME);
		RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());

		RecognitionResult first = engine.Recognize(harness.ContextFor(source));

		Assert.Equal(_ONE_FILE_RECOGNIZED, engine.ConditionalRegionMapsBuilt);
		Assert.Equal(_ONE_FILE_RECOGNIZED, engine.FilesEvaluatedForFlowSignals);
		Assert.Equal(FlowEvidence.None, first.FlowEvidence);
		Assert.Equal(ConfidenceLevel.High, Assert.Single(first.Constructs).Level);

		engine.Recognize(harness.ContextFor(source));

		Assert.Equal(_TWO_FILES_RECOGNIZED, engine.ConditionalRegionMapsBuilt);
	}

	[Fact]
	public void EveryLanguageButTheOneThatDeclaresTheElementHasAnEmptyRegionMap()
	{
		ImmutableArray<ILanguageAnalyzer> others = [.. LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName
			.Where(entry => entry.Key != _LANGUAGE_NAME_WITH_THE_ELEMENT)
			.Select(entry => entry.Value)];

		Assert.NotEmpty(others);

		foreach (ILanguageAnalyzer analyzer in others)
		{
			Assert.Null(analyzer.LexicalProfile.ConditionalCompilation);
			Assert.Same(ConditionalRegionMap.Empty, ConditionalRegionMap.Build(analyzer.LexicalProfile.ConditionalCompilation, _SOURCE_WITH_A_DIRECTIVE, new LexicalScanner(analyzer.LexicalProfile).Scan(_SOURCE_WITH_A_DIRECTIVE)));
		}

		Assert.NotNull(new CSharpRecognitionHarness().Analyzer.LexicalProfile.ConditionalCompilation);
	}

	private static string Lines(params string[] lines)
	{
		return $"{string.Join(_NEWLINE, lines.Where(line => line.Length > 0))}{_NEWLINE}";
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(string source)
	{
		return new CSharpRecognitionHarness().Recognize(source);
	}

	private static void AssertSingleLevel(string source, ConfidenceLevel expected)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(source));

		Assert.Equal(_CS_003, construct.PatternClassIdentifier);
		Assert.Equal(expected, construct.Level);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
	}

	private static FlowEvidence EvidenceOf(string source)
	{
		CSharpRecognitionHarness harness = new();
		RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());
		RecognitionResult result = engine.Recognize(harness.ContextFor(source));

		Assert.Equal(_ONE_FILE_RECOGNIZED, engine.FilesEvaluatedForFlowSignals);
		Assert.False(result.FlowEvaluationTripped);

		return result.FlowEvidence;
	}

	private static ConditionalRegionMap RegionsOf(CSharpRecognitionHarness harness, FileRecognitionContext context)
	{
		return ConditionalRegionMap.Build(harness.Analyzer.LexicalProfile.ConditionalCompilation, context.NormalizedText, context.Mask);
	}

	private static FileSymbolTable TableOf(string source)
	{
		CSharpRecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(source);

		return FileSymbolTable.Collect(context.Analyzer.LexicalProfile, context.NormalizedText, context.Mask, RegionsOf(harness, context));
	}
}
