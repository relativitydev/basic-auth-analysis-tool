using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// PART C IS ONE HAND-WRITTEN DOCUMENT WHOSE RECOMMENDATIONS SIT IN DELIMITED BLOCKS
/// THIS TEST REGENERATES FROM THE EMBEDDED CATALOG, AND PART A IS HAND-WRITTEN FROM THE CATALOG AND
/// CHECKED AGAINST IT.
///
/// The drift check calls <see cref="RecommendationComposer"/> directly, rewrites only what lies between
/// the marker pairs of the committed Part C, and fails on any difference, naming the block. It also
/// fails on an unbalanced marker pair and on a registered class identifier missing from Part A or Part C
/// or from its unsupported-construct list.
///
/// THE DEFAULT RUN NEVER WRITES. Setting the environment variable named by
/// <see cref="REGENERATE_ENVIRONMENT_VARIABLE"/> to <c>1</c> makes the drift test write the regenerated
/// Part C first (UTF-8 without a byte-order mark, LF line endings) and then compare, so the run that
/// regenerates passes and the next run, without the variable, proves the file on disk is what the tool says.
/// </summary>
public abstract class PartCDriftTests
{
	private const string _REGENERATE_ENABLED_VALUE = "1";
	private const string _ATTRIBUTES_RULE = "docs/public/** text eol=lf";
	private const string _CARRIAGE_RETURN = "\r";
	private const string _FENCE_MARKER = "```";
	private const string _SECTION_HEADING_PREFIX = "## ";
	private const string _LOW_CONFIDENCE_SECTION = "## Judging a Low-confidence match";
	private const int _MINIMUM_WORDS_IN_A_JUDGEMENT_PARAGRAPH = 30;
	private const int _EXCERPT_CONTEXT_LENGTH = 40;
	private const char _WORD_SEPARATOR = ' ';
	private const string _PARAGRAPH_SEPARATOR = "\n\n";
	private const char _LINE_BREAK = '\n';
	private const string _LINE_BREAK_TEXT = "\n";
	private const string _CLEAN_TEXT = "Clean text.";
	private const string _SOURCE_FORM_LEAD = "Source form: x";
	private const string _GENERATED_BLOCK_FORBIDDEN_PATTERN = @"\bfindings?\b|\bflow notes?\b";
	private const string _SYNTHETIC_DOCUMENT_BEGIN = "# Title";
	private const string _REGENERATE_HINT = "Regenerate the blocks by running this test with the environment variable MIGRATE_AUTH_REGENERATE_PART_C=1.";

	private static readonly string[] _lowCauseHeadings =
	[
		"### Comments",
		"### Documentation strings",
		"### Test fixtures and mocks",
		"### Sample and example code",
		"### Vendored and generated files",
		"### Deployment folders"
	];

	private static readonly string[] _fenceLabelsExemptFromTabs = [string.Empty, "bash", "text", "markdown"];

	private static readonly (string Phrase, string Pattern)[] _forbiddenHandWrittenPhrases =
	[
		("the tool", @"(?<!\w)the tool(?!\w)"),
		("migrate-auth", @"migrate-auth"),
		(".migrate-auth.json", @"\.migrate-auth\.json"),
		("defaultFlow", @"defaultFlow"),
		("the report", @"(?<!\w)the report(?!\w)"),
		("the scan", @"(?<!\w)the scan"),
		("flow note", @"flow note"),
		("Confirming the flow", @"Confirming the flow"),
		("#confirming-the-flow", @"#confirming-the-flow")
	];

	private static readonly Regex _entryCountStatement = new(
		@"Each of the (?<count>\d+) pattern classes that produce a recommendation",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

	/// <summary>The environment variable that makes the drift test rewrite Part C instead of only comparing.</summary>
	public const string REGENERATE_ENVIRONMENT_VARIABLE = "MIGRATE_AUTH_REGENERATE_PART_C";

	private readonly DocumentedLanguage _language;

	/// <summary>The checks of this class run for one language: its two documents and its embedded analyzer.</summary>
	protected PartCDriftTests(DocumentedLanguage language)
	{
		_language = language;
	}

	/// <summary>Every marker pair of Part C exists and is balanced: no orphan, no nesting, no duplicate, no unclosed block.</summary>
	[Fact]
	public void EveryGeneratedBlockMarkerPairInPartCIsBalanced()
	{
		string partC = File.ReadAllText(_language.PartCPath());

		ImmutableArray<string> problems = GeneratedBlockMarkers.BalanceProblems(partC);

		Assert.True(problems.IsEmpty, string.Join(Environment.NewLine, problems));
		Assert.NotEmpty(GeneratedBlockMarkers.Find(partC));
	}

	/// <summary>The balance check itself fails on each way a marker pair can be wrong, shown on synthetic text.</summary>
	[Fact]
	public void TheBalanceCheckReportsOrphanNestedDuplicateMismatchedAndUnclosedMarkers()
	{
		string begin = GeneratedBlockMarkers.BeginMarker("A");
		string end = GeneratedBlockMarkers.EndMarker("A");
		string otherBegin = GeneratedBlockMarkers.BeginMarker("B");
		string otherEnd = GeneratedBlockMarkers.EndMarker("B");

		Assert.Empty(GeneratedBlockMarkers.BalanceProblems($"{begin}\nx\n{end}\n{otherBegin}\ny\n{otherEnd}"));
		Assert.Contains("no begin marker", Assert.Single(GeneratedBlockMarkers.BalanceProblems($"{end}")));
		Assert.Contains("never closed", Assert.Single(GeneratedBlockMarkers.BalanceProblems($"{begin}\nx")));
		Assert.Contains(GeneratedBlockMarkers.BalanceProblems($"{begin}\n{otherBegin}\n{otherEnd}\n{end}"), problem => problem.Contains("nest", StringComparison.Ordinal));
		Assert.Contains(GeneratedBlockMarkers.BalanceProblems($"{begin}\n{end}\n{begin}\n{end}"), problem => problem.Contains("more than once", StringComparison.Ordinal));
		Assert.Contains(GeneratedBlockMarkers.BalanceProblems($"{begin}\n{otherEnd}"), problem => problem.Contains("is closed by", StringComparison.Ordinal));
	}

	/// <summary>Part C holds exactly one block per (class, flow) of a class that produces advice, plus one generic block per flow.</summary>
	[Fact]
	public void PartCHoldsExactlyTheClassFlowEntriesAndTheGenericBlockOfEachFlow()
	{
		string partC = File.ReadAllText(_language.PartCPath());
		ILanguageAnalyzer analyzer = _language.Analyzer();

		string[] expected = [.. PartCBlockComposition.ExpectedKeys(analyzer).Select(PartCBlockComposition.KeyText).Order(StringComparer.Ordinal)];
		string[] actual = [.. EntryBlocks(partC).Select(block => block.Key).Order(StringComparer.Ordinal)];

		Assert.Equal(expected, actual);
		Assert.Equal(
			PartCBlockComposition.ClassesThatProduceAdvice(analyzer).Length * PartCBlockComposition.Flows.Length,
			actual.Count(key => !key.StartsWith(PartCBlockComposition.GENERIC_KEY_PREFIX, StringComparison.Ordinal)));
	}

	/// <summary>
	/// No class is composed on the generic route, so the thirty-two entries stand without a
	/// per-class reason; a class that ever took the generic route would fail here until the reason is stated.
	/// </summary>
	[Fact]
	public void EveryClassEntryIsComposedFromItsOwnTemplateSoNoPerClassReasonIsNeeded()
	{
		ILanguageAnalyzer analyzer = _language.Analyzer();

		string[] genericallyComposed =
		[
			.. PartCBlockComposition.ExpectedKeys(analyzer)
				.Where(key => !key.IsGeneric)
				.Where(key => PartCBlockComposition.Composed(_language, key).Kind != RecommendationKind.FromTemplate)
				.Select(PartCBlockComposition.KeyText)
		];

		Assert.Empty(genericallyComposed);
	}

	/// <summary>The committed blocks equal a fresh composition through the composer; the failure names each block that differs.</summary>
	[Fact]
	public void PartCGeneratedBlocksEqualAFreshCompositionFromTheEmbeddedCatalog()
	{
		string path = _language.PartCPath();
		string committed = File.ReadAllText(path);
		FrozenDictionary<string, string> composedByKey = ComposedContentByKey();
		string fresh = FreshText(committed, key => ComposedContentOrFail(composedByKey, key));

		if (Environment.GetEnvironmentVariable(REGENERATE_ENVIRONMENT_VARIABLE) == _REGENERATE_ENABLED_VALUE && fresh != committed)
		{
			File.WriteAllText(path, fresh, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			committed = fresh;
		}

		string[] differingBlocks = DifferingBlockKeys(committed, fresh);

		Assert.True(
			differingBlocks.Length == 0,
			$"Part C differs from a fresh composition in: {string.Join(", ", differingBlocks)}. {_REGENERATE_HINT}");
		Assert.Equal(fresh, committed);
	}

	/// <summary>Part C carries exactly one table of contents block, balanced, and it lists every level 2 and level 3 heading.</summary>
	[Fact]
	public void PartCCarriesOneTableOfContentsThatListsEveryLevelTwoAndLevelThreeHeading()
	{
		string partC = File.ReadAllText(_language.PartCPath());
		ImmutableArray<GeneratedBlock> blocks = [.. GeneratedBlockMarkers.Blocks(partC).Where(block => PartCBlockComposition.IsTableOfContentsKey(block.Key))];
		ImmutableArray<MarkdownHeading> headings = [.. MarkdownAnchors.HeadingsOf(partC).Where(heading => heading.Level is >= MarkdownAnchors.TABLE_OF_CONTENTS_TOP_LEVEL and <= MarkdownAnchors.TABLE_OF_CONTENTS_BOTTOM_LEVEL)];

		Assert.Empty(GeneratedBlockMarkers.BalanceProblems(partC));
		Assert.Single(blocks);
		Assert.Equal(headings.Length, blocks[0].Content.Split(_LINE_BREAK).Length);
		Assert.All(headings, heading => Assert.Contains($"[{heading.Text}](#{heading.Anchor})", blocks[0].Content, StringComparison.Ordinal));
	}

	/// <summary>The generator lists level 2 and level 3 headings with GitHub anchors, repeated headings suffixed, skips level 4, and ignores a heading line inside a fence, shown on synthetic text; a drifted committed table is detected.</summary>
	[Fact]
	public void TheTableOfContentsGeneratorListsTheHeadingsAndADriftedTableIsDetected()
	{
		string begin = GeneratedBlockMarkers.BeginMarker(PartCBlockComposition.TABLE_OF_CONTENTS_KEY);
		string end = GeneratedBlockMarkers.EndMarker(PartCBlockComposition.TABLE_OF_CONTENTS_KEY);
		string sample = string.Join(
			_LINE_BREAK_TEXT,
			_SYNTHETIC_DOCUMENT_BEGIN,
			begin,
			end,
			"## Same",
			"### Same",
			"#### Deep",
			"```text",
			"# not a heading",
			"```",
			"## Same",
			"## C#");

		string fresh = FreshText(sample, _ => string.Empty);
		string drifted = fresh.Replace("(#same-1)", "(#same)", StringComparison.Ordinal);
		string[] expected = ["- [Same](#same)", "\t- [Same](#same-1)", "- [Same](#same-2)", "- [C#](#c)"];

		Assert.Equal(expected, GeneratedBlockMarkers.Blocks(fresh).Single().Content.Split(_LINE_BREAK));
		Assert.Equal(fresh, FreshText(fresh, _ => string.Empty));
		Assert.NotEqual(fresh, drifted);
		Assert.Equal([PartCBlockComposition.TABLE_OF_CONTENTS_KEY], DifferingBlockKeys(drifted, fresh).Select(difference => difference.Split(_WORD_SEPARATOR)[0]));
	}

	/// <summary>Every class entry carries a marker for each item 2 element of Part C, and the generic block carries the shared ones.</summary>
	[Fact]
	public void EveryPartCEntryCarriesAMarkerForEachItemTwoElement()
	{
		string partC = File.ReadAllText(_language.PartCPath());

		string[] problems =
		[
			.. EntryBlocks(partC).SelectMany(block =>
				PublicDocumentChecks.ItemTwoProblems(block, PartCBlockComposition.ParseKey(block.Key), _language))
		];

		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The check for the item 2 markers fails on a Code entry with PKCE, a variant or the Part B pointer taken out.</summary>
	[Fact]
	public void TheItemTwoCheckReportsAnEntryWhoseMarkerWasRemoved()
	{
		ILanguageAnalyzer analyzer = _language.Analyzer();
		string patternClassIdentifier = PartCBlockComposition.ClassesThatProduceAdvice(analyzer).First();
		PartCBlockKey key = new(patternClassIdentifier, CredentialFlow.AuthorizationCode);
		string complete = PartCBlockComposition.ContentFor(_language, PartCBlockComposition.KeyText(key));
		string withoutPkce = complete.Replace(PublicDocumentChecks.PKCE_MARKER, string.Empty, StringComparison.Ordinal);
		string withoutPartB = complete.Replace(SecretManagementPointer.PART_B_NAME, string.Empty, StringComparison.Ordinal);
		string withoutLink = complete.Replace(PartCBlockComposition.SectionLinkOf(key.Flow), string.Empty, StringComparison.Ordinal);
		string withBlockRepeated = string.Concat(complete, _PARAGRAPH_SEPARATOR, OAuth2ClientConfigurationBlocks.For(key.Flow));

		Assert.Empty(PublicDocumentChecks.ItemTwoProblems(new GeneratedBlock(PartCBlockComposition.KeyText(key), complete), key, _language));
		Assert.Contains(
			PublicDocumentChecks.ItemTwoProblems(new GeneratedBlock(PartCBlockComposition.KeyText(key), withoutPkce), key, _language),
			problem => problem.Contains(PublicDocumentChecks.PKCE_MARKER, StringComparison.Ordinal));
		Assert.Contains(
			PublicDocumentChecks.ItemTwoProblems(new GeneratedBlock(PartCBlockComposition.KeyText(key), withoutPartB), key, _language),
			problem => problem.Contains("Part B pointer", StringComparison.Ordinal));
		Assert.Contains(
			PublicDocumentChecks.ItemTwoProblems(new GeneratedBlock(PartCBlockComposition.KeyText(key), withoutLink), key, _language),
			problem => problem.Contains("link to the OAuth2 Client section", StringComparison.Ordinal));
		Assert.Contains(
			PublicDocumentChecks.ItemTwoProblems(new GeneratedBlock(PartCBlockComposition.KeyText(key), withBlockRepeated), key, _language),
			problem => problem.Contains("repeats the OAuth2 Client block", StringComparison.Ordinal));
	}

	/// <summary>Part C holds exactly one OAuth2 Client section per flow, each a heading and one block, and no more.</summary>
	[Fact]
	public void PartCHoldsExactlyOneOAuth2ClientSectionForEachFlow()
	{
		string partC = File.ReadAllText(_language.PartCPath());
		string[] sectionKeys = [.. GeneratedBlockMarkers.Blocks(partC).Select(block => block.Key).Where(PartCBlockComposition.IsOAuth2ClientKey).Order(StringComparer.Ordinal)];

		Assert.Equal([.. PartCBlockComposition.OAuth2ClientKeys().Order(StringComparer.Ordinal)], sectionKeys);

		foreach (CredentialFlow flow in PartCBlockComposition.Flows)
		{
			string heading = string.Concat(_SECTION_HEADING_PREFIX, PartCBlockComposition.SectionTitleOf(flow));
			string? section = PublicDocumentChecks.SectionOf(partC, heading);

			Assert.Equal(1, partC.Split(_LINE_BREAK).Count(line => line.TrimEnd() == heading));
			Assert.True(section is not null, $"Part C has no section headed '{heading}'.");
			Assert.Single(GeneratedBlockMarkers.Blocks(section!));
			Assert.Equal(PartCBlockComposition.OAuth2ClientKeyOf(flow), GeneratedBlockMarkers.Blocks(section!)[0].Key);
		}
	}

	/// <summary>The block of each OAuth2 Client section of Part C equals the engine-owned block of its flow, and nothing else.</summary>
	[Fact]
	public void EachOAuth2ClientSectionHoldsTheEngineOwnedBlockOfItsFlow()
	{
		string partC = File.ReadAllText(_language.PartCPath());

		foreach (CredentialFlow flow in PartCBlockComposition.Flows)
		{
			GeneratedBlock block = GeneratedBlockMarkers.Blocks(partC).Single(candidate => candidate.Key == PartCBlockComposition.OAuth2ClientKeyOf(flow));

			Assert.Equal(OAuth2ClientConfigurationBlocks.For(flow), block.Content);
		}
	}

	/// <summary>Every class entry and every generic block links to the OAuth2 Client section of its flow, once, and holds no copy of the block.</summary>
	[Fact]
	public void EveryEntryLinksToTheOAuth2ClientSectionOfItsFlowAndHoldsNoCopyOfTheBlock()
	{
		string partC = File.ReadAllText(_language.PartCPath());

		foreach (GeneratedBlock block in EntryBlocks(partC))
		{
			CredentialFlow flow = PartCBlockComposition.ParseKey(block.Key).Flow;
			CredentialFlow otherFlow = flow == CredentialFlow.ClientCredentials ? CredentialFlow.AuthorizationCode : CredentialFlow.ClientCredentials;

			Assert.Equal(1, CountOf(block.Content, PartCBlockComposition.SectionAnchorOf(flow)));
			Assert.DoesNotContain(PartCBlockComposition.SectionAnchorOf(otherFlow), block.Content, StringComparison.Ordinal);
			Assert.DoesNotContain(OAuth2ClientConfigurationBlocks.For(flow), block.Content, StringComparison.Ordinal);
			Assert.DoesNotContain(OAuth2ClientConfigurationBlocks.For(otherFlow), block.Content, StringComparison.Ordinal);
		}
	}

	/// <summary>The generator fails loudly where a composed entry does not hold the OAuth2 Client block it is to replace with a link.</summary>
	[Fact]
	public void TheGeneratorFailsWhereTheComposedEntryHoldsNoOAuth2ClientBlockToReplace()
	{
		string withBlock = string.Concat(_SOURCE_FORM_LEAD, _PARAGRAPH_SEPARATOR, PartCBlockComposition.OAuth2ClientPieceOf(CredentialFlow.ClientCredentials));

		Assert.Equal(
			string.Concat(_SOURCE_FORM_LEAD, _PARAGRAPH_SEPARATOR, PartCBlockComposition.SectionLinkOf(CredentialFlow.ClientCredentials)),
			PartCBlockComposition.WithSectionLink("key", withBlock, CredentialFlow.ClientCredentials));
		Assert.Throws<InvalidOperationException>(() => PartCBlockComposition.WithSectionLink("key", _SOURCE_FORM_LEAD, CredentialFlow.ClientCredentials));
	}

	/// <summary>Each generic block is the language's own generic text for its flow, composed on the generic route.</summary>
	[Fact]
	public void EachGenericBlockCarriesTheLanguageFallbackTextOfItsFlow()
	{
		string partC = File.ReadAllText(_language.PartCPath());
		ILanguageAnalyzer analyzer = _language.Analyzer();
		FrozenDictionary<CredentialFlow, string>? declaredFallbackByFlow = analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow;

		Assert.True(declaredFallbackByFlow is not null, $"The {_language.DisplayName} analyzer declares no generic fallback text by flow, so the generic blocks of its Part C have nothing to carry.");

		FrozenDictionary<CredentialFlow, string> fallbackByFlow = declaredFallbackByFlow!;

		foreach (CredentialFlow flow in PartCBlockComposition.Flows)
		{
			PartCBlockKey key = new(null, flow);
			GeneratedBlock block = GeneratedBlockMarkers.Blocks(partC).Single(candidate => candidate.Key == PartCBlockComposition.KeyText(key));

			Assert.Equal(RecommendationKind.Generic, PartCBlockComposition.Composed(_language, key).Kind);
			Assert.Contains(fallbackByFlow[flow], block.Content, StringComparison.Ordinal);
		}
	}

	/// <summary>Every class the analyzer registers appears in Part A with its heading and all four labelled elements.</summary>
	[Fact]
	public void EveryRegisteredClassAppearsInPartAWithAllItsElements()
	{
		string partA = File.ReadAllText(_language.PartAPath());

		ImmutableArray<string> problems = PublicDocumentChecks.PartAClassProblems(partA, RegisteredRules(), ExampleFence());

		Assert.True(problems.IsEmpty, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The Part A check fails when one registered class loses its heading, shown on a copy of the real text.</summary>
	[Fact]
	public void ThePartACheckReportsAClassWhoseHeadingWasRemoved()
	{
		string partA = File.ReadAllText(_language.PartAPath());
		KeyValuePair<string, DetectionRule> removed = RegisteredRules().OrderBy(entry => entry.Key, StringComparer.Ordinal).First();
		string heading = PublicDocumentChecks.ClassHeading(removed.Key, removed.Value.DisplayName);

		string withoutHeading = partA.Replace(heading, string.Empty, StringComparison.Ordinal);

		ImmutableArray<string> problems = PublicDocumentChecks.PartAClassProblems(withoutHeading, RegisteredRules(), ExampleFence());

		Assert.Contains(problems, problem => problem.Contains(removed.Key, StringComparison.Ordinal));
		Assert.NotEqual(partA, withoutHeading);
	}

	/// <summary>Every registered class, the two manual-review ones included, appears in Part C.</summary>
	[Fact]
	public void EveryRegisteredClassAppearsInPartC()
	{
		string partC = File.ReadAllText(_language.PartCPath());

		ImmutableArray<string> problems = PublicDocumentChecks.PartCClassProblems(partC, RegisteredRules());

		Assert.True(problems.IsEmpty, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The Part C check fails when a class loses both its heading and its blocks, shown on a copy of the real text.</summary>
	[Fact]
	public void ThePartCCheckReportsAClassWhoseHeadingAndBlocksWereRemoved()
	{
		string partC = File.ReadAllText(_language.PartCPath());
		string patternClassIdentifier = PartCBlockComposition.ManualReviewClasses(_language.Analyzer()).First();
		DetectionRule rule = RegisteredRules()[patternClassIdentifier];

		string withoutClass = partC.Replace(PublicDocumentChecks.ClassHeading(patternClassIdentifier, rule.DisplayName), string.Empty, StringComparison.Ordinal);

		ImmutableArray<string> problems = PublicDocumentChecks.PartCClassProblems(withoutClass, RegisteredRules());

		Assert.Contains(problems, problem => problem.Contains(patternClassIdentifier, StringComparison.Ordinal));
		Assert.Empty(PublicDocumentChecks.PartCClassProblems(partC, RegisteredRules()));
	}

	/// <summary>Every declared-unsupported construct the analyzer emits appears in Part A's unsupported section.</summary>
	[Fact]
	public void EveryDeclaredUnsupportedConstructAppearsInPartA()
	{
		string partA = File.ReadAllText(_language.PartAPath());
		ILanguageAnalyzer analyzer = _language.Analyzer();

		ImmutableArray<string> problems = PublicDocumentChecks.UnsupportedProblems(partA, analyzer.LanguageIdentification.DeclaredUnsupportedCoverage);

		Assert.NotEmpty(analyzer.LanguageIdentification.DeclaredUnsupportedCoverage);
		Assert.True(problems.IsEmpty, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The unsupported check fails on a construct that is not listed.</summary>
	[Fact]
	public void TheUnsupportedCheckReportsAConstructThatIsNotListed()
	{
		string partA = File.ReadAllText(_language.PartAPath());
		DeclaredUnsupportedCoverage unlisted = new() { Construct = "a construct the document does not mention", Reason = "not used" };

		ImmutableArray<string> problems = PublicDocumentChecks.UnsupportedProblems(partA, [unlisted]);

		Assert.Contains(problems, problem => problem.Contains(unlisted.Construct, StringComparison.Ordinal));
	}

	/// <summary>
	/// Every class has a "Which flow" paragraph that links to the signal section, names both flows,
	/// states the cross-file limit and, for the two manual-review classes, says there is no flow; and the signal
	/// section lists every signal the analyzer registers under the heading of its own kind.
	/// </summary>
	[Fact]
	public void EveryWhichFlowParagraphAgreesWithTheSignalSectionAndTheCrossFileLimit()
	{
		string partA = File.ReadAllText(_language.PartAPath());
		ILanguageAnalyzer analyzer = _language.Analyzer();

		ImmutableArray<string> paragraphProblems = PublicDocumentChecks.WhichFlowProblems(partA, RegisteredRules());
		ImmutableArray<string> signalProblems = PublicDocumentChecks.FlowSignalProblems(partA, analyzer.CandidateSelectionAndRecommendation.FlowSignals);

		Assert.True(paragraphProblems.IsEmpty, string.Join(Environment.NewLine, paragraphProblems));
		Assert.True(signalProblems.IsEmpty, string.Join(Environment.NewLine, signalProblems));
	}

	/// <summary>The paragraph check fails when the cross-file limit is left out of a class's paragraph.</summary>
	[Fact]
	public void TheWhichFlowCheckReportsAParagraphThatOmitsTheCrossFileLimit()
	{
		string partA = File.ReadAllText(_language.PartAPath());
		string withoutLimit = partA.Replace(PublicDocumentChecks.CROSS_FILE_LIMIT_PHRASE, "other place", StringComparison.OrdinalIgnoreCase);

		ImmutableArray<string> problems = PublicDocumentChecks.WhichFlowProblems(withoutLimit, RegisteredRules());

		Assert.Contains(problems, problem => problem.Contains(PublicDocumentChecks.CROSS_FILE_LIMIT_PHRASE, StringComparison.Ordinal));
	}

	/// <summary>Each Low cause of the rubric and each of the two Low classes has a paragraph of its own in Part C.</summary>
	[Fact]
	public void PartCHasAParagraphForEachLowCauseAndEachLowClass()
	{
		string partC = File.ReadAllText(_language.PartCPath());
		ILanguageAnalyzer analyzer = _language.Analyzer();

		string[] headings =
		[
			.. _lowCauseHeadings,
			.. PartCBlockComposition.ManualReviewClasses(analyzer)
				.Select(patternClassIdentifier => PublicDocumentChecks.ClassHeading(patternClassIdentifier, RegisteredRules()[patternClassIdentifier].DisplayName))
		];

		Assert.NotNull(PublicDocumentChecks.SectionOf(partC, _LOW_CONFIDENCE_SECTION));

		foreach (string heading in headings)
		{
			string? section = PublicDocumentChecks.SectionOf(partC, heading);

			Assert.True(section is not null, $"Part C has no section headed '{heading}'.");
			Assert.True(
				PublicDocumentChecks.NormalizeWhitespace(section!).Split(_WORD_SEPARATOR).Length >= _MINIMUM_WORDS_IN_A_JUDGEMENT_PARAGRAPH,
				$"The section '{heading}' is too short to say how to judge the match and when it needs a migration.");
		}
	}

	/// <summary>Part C stands alone: its hand-written text names neither the tool, its configuration, its report nor its flow notes.</summary>
	[Fact]
	public void TheHandWrittenTextOfPartCNamesNeitherTheToolNorItsConfigurationNorItsReport()
	{
		string partC = File.ReadAllText(_language.PartCPath());

		string[] problems = HandWrittenTextProblems(partC);

		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>Part C stands alone in its generated blocks too: they do not refer to a finding or to a flow note, which belong to the tool's report.</summary>
	[Fact]
	public void TheGeneratedBlocksOfPartCReferToNeitherFindingsNorFlowNotes()
	{
		string partC = File.ReadAllText(_language.PartCPath());

		string[] problems =
		[
			.. GeneratedBlockMarkers.Blocks(partC)
				.Where(block => !PartCBlockComposition.IsTableOfContentsKey(block.Key))
					.Where(block => Regex.IsMatch(block.Content, _GENERATED_BLOCK_FORBIDDEN_PATTERN, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
				.Select(block => $"The generated block {block.Key} of Part C refers to a finding or a flow note, which belong to the tool's report.")
		];

		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The stand-alone check fails on each forbidden phrase in hand-written text and ignores the same phrase inside a generated block, shown on synthetic text.</summary>
	[Fact]
	public void TheStandAloneCheckReportsEachForbiddenPhraseOutsideTheGeneratedBlocksOnly()
	{
		string begin = GeneratedBlockMarkers.BeginMarker("A");
		string end = GeneratedBlockMarkers.EndMarker("A");

		foreach ((string phrase, _) in _forbiddenHandWrittenPhrases)
		{
			string insideOnly = string.Join(_LINE_BREAK_TEXT, _CLEAN_TEXT, begin, phrase, end, _CLEAN_TEXT);
			string outside = string.Join(_LINE_BREAK_TEXT, _CLEAN_TEXT, begin, "x", end, $"See {phrase} here.");

			Assert.Empty(HandWrittenTextProblems(insideOnly));
			Assert.Contains(HandWrittenTextProblems(outside), problem => problem.Contains($"'{phrase}'", StringComparison.Ordinal));
		}

		Assert.Empty(HandWrittenTextProblems("Open the reported file and the tooling around it."));
	}

	/// <summary>Every statement of how many pattern classes produce a recommendation names the real number.</summary>
	[Fact]
	public void EveryEntryCountStatementInPartCNamesTheNumberOfClassesThatProduceAdvice()
	{
		string handWritten = GeneratedBlockMarkers.Rewrite(File.ReadAllText(_language.PartCPath()), _ => string.Empty);
		int expected = PartCBlockComposition.ClassesThatProduceAdvice(_language.Analyzer()).Length;

		MatchCollection statements = _entryCountStatement.Matches(handWritten);

		Assert.NotEmpty(statements);
		Assert.All(statements, statement => Assert.Equal(expected, int.Parse(statement.Groups["count"].Value, CultureInfo.InvariantCulture)));
	}

	/// <summary>The repository gives docs/public LF line endings, and neither committed document carries a carriage return.</summary>
	[Fact]
	public void PublicDocumentsAreCheckedOutWithLfEndingsAndCarryNoCarriageReturn()
	{
		string[] attributeLines = File.ReadAllLines(PublicDocuments.GitAttributesPath());

		Assert.Contains(attributeLines, line => line.Trim() == _ATTRIBUTES_RULE);
		Assert.DoesNotContain(_CARRIAGE_RETURN, File.ReadAllText(_language.PartCPath()), StringComparison.Ordinal);
		Assert.DoesNotContain(_CARRIAGE_RETURN, File.ReadAllText(_language.PartAPath()), StringComparison.Ordinal);
	}

	/// <summary>Code blocks in both documents use tab indentation; bash, text, markdown and unlabelled blocks are exempt.</summary>
	[Fact]
	public void CodeBlocksInThePublicDocumentsAreTabIndented()
	{
		string[] violations =
		[
			.. SpaceIndentedCodeLines(_language.PartAPath()),
			.. SpaceIndentedCodeLines(_language.PartCPath())
		];

		Assert.Empty(violations);
	}

	private static string[] HandWrittenTextProblems(string partC)
	{
		string handWritten = GeneratedBlockMarkers.Rewrite(partC, _ => string.Empty);

		return
		[
			.. _forbiddenHandWrittenPhrases
				.Where(forbidden => Regex.IsMatch(handWritten, forbidden.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
				.Select(forbidden => $"The hand-written text of Part C contains '{forbidden.Phrase}', which belongs to the tool and not to a stand-alone guide.")
		];
	}

	private string ExampleFence()
	{
		return string.Concat(_FENCE_MARKER, _language.ExampleFenceLabel);
	}

	private static ImmutableArray<GeneratedBlock> EntryBlocks(string partC)
	{
		return [.. GeneratedBlockMarkers.Blocks(partC).Where(block => !PartCBlockComposition.IsOAuth2ClientKey(block.Key) && !PartCBlockComposition.IsTableOfContentsKey(block.Key))];
	}

	private static int CountOf(string text, string value)
	{
		return text.Split(value).Length - 1;
	}

	private IReadOnlyDictionary<string, DetectionRule> RegisteredRules()
	{
		return _language.Analyzer().DetectionRulesByPatternClass;
	}

	private FrozenDictionary<string, string> ComposedContentByKey()
	{
		return PartCBlockComposition
			.ExpectedKeys(_language.Analyzer())
			.Select(PartCBlockComposition.KeyText)
			.Concat(PartCBlockComposition.OAuth2ClientKeys())
			.ToFrozenDictionary(key => key, keyText => PartCBlockComposition.ContentFor(_language, keyText));
	}

	private static string ComposedContentOrFail(FrozenDictionary<string, string> composedByKey, string key)
	{
		return composedByKey.TryGetValue(key, out string? content)
			? content
			: throw new InvalidOperationException($"Part C holds a block '{key}' that no composition produces.");
	}

	/// <summary>
	/// The document as the generator writes it: every block but the table of contents composed by
	/// <paramref name="contentForKey"/>, then the table of contents computed from that finished text.
	/// </summary>
	private static string FreshText(string committed, Func<string, string> contentForKey)
	{
		string withoutTableOfContents = GeneratedBlockMarkers.Rewrite(
			committed,
			key => PartCBlockComposition.IsTableOfContentsKey(key) ? string.Empty : contentForKey(key));
		string tableOfContents = MarkdownAnchors.TableOfContentsOf(withoutTableOfContents);

		return GeneratedBlockMarkers.Rewrite(
			withoutTableOfContents,
			key => PartCBlockComposition.IsTableOfContentsKey(key) ? tableOfContents : BlockContentOf(withoutTableOfContents, key));
	}

	private static string BlockContentOf(string text, string key)
	{
		return GeneratedBlockMarkers.Blocks(text).Single(block => block.Key == key).Content;
	}

	private static string[] DifferingBlockKeys(string committed, string fresh)
	{
		Dictionary<string, string> freshByKey = GeneratedBlockMarkers.Blocks(fresh).ToDictionary(block => block.Key, block => block.Content);

		return
		[
			.. GeneratedBlockMarkers.Blocks(committed)
				.Where(block => freshByKey[block.Key] != block.Content)
				.Select(block => $"{block.Key} ({FirstDifference(block.Content, freshByKey[block.Key])})")
		];
	}

	private static string FirstDifference(string committedContent, string freshContent)
	{
		string[] committedLines = committedContent.Split('\n');
		string[] freshLines = freshContent.Split('\n');
		int lineIndex = 0;

		while (lineIndex < committedLines.Length && lineIndex < freshLines.Length && committedLines[lineIndex] == freshLines[lineIndex])
		{
			lineIndex++;
		}

		string committedLine = lineIndex < committedLines.Length ? committedLines[lineIndex] : string.Empty;
		string freshLine = lineIndex < freshLines.Length ? freshLines[lineIndex] : string.Empty;

		int differenceAt = 0;

		while (differenceAt < committedLine.Length && differenceAt < freshLine.Length && committedLine[differenceAt] == freshLine[differenceAt])
		{
			differenceAt++;
		}

		return $"line {lineIndex + 1} of the block, from character {differenceAt + 1}, committed: \"{Excerpt(committedLine, differenceAt)}\" but composed: \"{Excerpt(freshLine, differenceAt)}\"";
	}

	private static string Excerpt(string line, int differenceAt)
	{
		int start = Math.Max(0, differenceAt - _EXCERPT_CONTEXT_LENGTH);
		int length = Math.Min(line.Length - start, _EXCERPT_CONTEXT_LENGTH * 2);

		return line.Substring(start, length);
	}

	private static IEnumerable<string> SpaceIndentedCodeLines(string path)
	{
		string[] lines = File.ReadAllLines(path);
		string? openLabel = null;
		List<string> violations = [];

		for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
		{
			string line = lines[lineNumber];

			if (line.TrimStart().StartsWith(_FENCE_MARKER, StringComparison.Ordinal))
			{
				openLabel = openLabel is null ? line.Trim()[_FENCE_MARKER.Length..] : null;
			}
			else if (openLabel is not null && !_fenceLabelsExemptFromTabs.Contains(openLabel) && line.StartsWith(' '))
			{
				violations.Add($"{Path.GetFileName(path)} line {lineNumber + 1} is indented with spaces inside a {openLabel} block.");
			}
		}

		return violations;
	}
}
