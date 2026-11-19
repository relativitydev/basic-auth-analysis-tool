using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// EVERY CLASS IDENTIFIER AND EVERY DECLARED-UNSUPPORTED ENTRY OF GO APPEARS IN ITS PART A.
///
/// These are the Part A checks of <see cref="PartCDriftTests"/>, run for Go on their own, so that a change to Part A is caught without composing the generated blocks of Part C. A class that loses its heading or one of its four labelled elements, an entry of the declared
/// unsupported coverage that the document does not list, a "Which flow" paragraph that does not agree with the signal section, and a signal the section does
/// not list under its own kind, each fail here.
/// </summary>
public sealed class GoPartAIdentifierTests
{
	private const string _GO_FENCE = "```go";
	private const string _REMOVED_TEXT = "removed";

	private static readonly DocumentedLanguage _language = DocumentedLanguage.Go;

	/// <summary>Every class the analyzer registers appears in Part A with its heading, its example, its search, its confidence and its flow.</summary>
	[Fact]
	public void EveryRegisteredClassAppearsInPartAWithAllItsElements()
	{
		ImmutableArray<string> problems = PublicDocumentChecks.PartAClassProblems(PartA(), Rules(), _GO_FENCE);

		Assert.NotEmpty(Rules());
		Assert.True(problems.IsEmpty, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The check fails when one class loses its heading, shown on a copy of the real text.</summary>
	[Fact]
	public void TheClassCheckReportsAClassWhoseHeadingWasRemoved()
	{
		string partA = PartA();
		KeyValuePair<string, DetectionRule> removed = Rules().OrderBy(entry => entry.Key, StringComparer.Ordinal).First();
		string withoutHeading = partA.Replace(PublicDocumentChecks.ClassHeading(removed.Key, removed.Value.DisplayName), _REMOVED_TEXT, StringComparison.Ordinal);

		Assert.Contains(PublicDocumentChecks.PartAClassProblems(withoutHeading, Rules(), _GO_FENCE), problem => problem.Contains(removed.Key, StringComparison.Ordinal));
		Assert.NotEqual(partA, withoutHeading);
	}

	/// <summary>Every declared-unsupported entry the analyzer emits is listed in the unsupported section of Part A.</summary>
	[Fact]
	public void EveryDeclaredUnsupportedConstructAppearsInPartA()
	{
		ILanguageAnalyzer analyzer = _language.Analyzer();

		ImmutableArray<string> problems = PublicDocumentChecks.UnsupportedProblems(PartA(), analyzer.LanguageIdentification.DeclaredUnsupportedCoverage);

		Assert.NotEmpty(analyzer.LanguageIdentification.DeclaredUnsupportedCoverage);
		Assert.True(problems.IsEmpty, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The check fails on an entry that is not listed.</summary>
	[Fact]
	public void TheUnsupportedCheckReportsAnEntryThatIsNotListed()
	{
		DeclaredUnsupportedCoverage unlisted = new() { Construct = "a construct the document does not mention", Reason = "not used" };

		Assert.Contains(PublicDocumentChecks.UnsupportedProblems(PartA(), [unlisted]), problem => problem.Contains(unlisted.Construct, StringComparison.Ordinal));
	}

	/// <summary>The entries the work names are in the list: a cgo preamble, assembly files, SQL text and a first line that is a Go comment which is also a shell command.</summary>
	[Fact]
	public void TheEntriesOfTheForeignTextAreListedInPartA()
	{
		ILanguageAnalyzer analyzer = _language.Analyzer();
		string[] foreignText =
		[
			"the C code of a cgo preamble",
			"assembly files ending in .s",
			"SQL text inside a string literal",
			"a polyglot run script with no file extension whose first line is a Go comment that is also a shell command"
		];

		Assert.All(foreignText, text => Assert.Contains(analyzer.LanguageIdentification.DeclaredUnsupportedCoverage, entry => entry.Construct.Contains(text, StringComparison.Ordinal)));
		Assert.All(foreignText, text => Assert.Contains(text, PartA().Replace("`", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal));
		Assert.Contains("//usr/bin/env go run", PartA(), StringComparison.Ordinal);
	}

	/// <summary>
	/// Every class has a "Which flow" paragraph that links to the signal section, names both flows, states the cross-file limit and, for a class that is Low by
	/// definition, says there is no flow; and the section lists every signal the analyzer registers under the heading of its own kind.
	/// </summary>
	[Fact]
	public void EveryWhichFlowParagraphAgreesWithTheSignalSectionAndTheCrossFileLimit()
	{
		ILanguageAnalyzer analyzer = _language.Analyzer();

		ImmutableArray<string> paragraphProblems = PublicDocumentChecks.WhichFlowProblems(PartA(), Rules());
		ImmutableArray<string> signalProblems = PublicDocumentChecks.FlowSignalProblems(PartA(), analyzer.CandidateSelectionAndRecommendation.FlowSignals);

		Assert.True(paragraphProblems.IsEmpty, string.Join(Environment.NewLine, paragraphProblems));
		Assert.True(signalProblems.IsEmpty, string.Join(Environment.NewLine, signalProblems));
	}

	/// <summary>The paragraph check fails when the cross-file limit is left out of a paragraph.</summary>
	[Fact]
	public void TheWhichFlowCheckReportsAParagraphThatOmitsTheCrossFileLimit()
	{
		string withoutLimit = PartA().Replace(PublicDocumentChecks.CROSS_FILE_LIMIT_PHRASE, "other place", StringComparison.OrdinalIgnoreCase);

		Assert.Contains(PublicDocumentChecks.WhichFlowProblems(withoutLimit, Rules()), problem => problem.Contains(PublicDocumentChecks.CROSS_FILE_LIMIT_PHRASE, StringComparison.Ordinal));
	}

	/// <summary>Eleven signals are registered, six of the interactive kind and five of the fixed kind, and Part A says so.</summary>
	[Fact]
	public void TheSignalCountsPartAStatesAreTheAnalyzersOwn()
	{
		ImmutableArray<FlowSignal> signals = _language.Analyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.Equal(GoPartATestConstants.INTERACTIVE_SIGNAL_COUNT, signals.Count(signal => signal.Kind == FlowSignalKind.InteractiveUser));
		Assert.Equal(GoPartATestConstants.FIXED_SIGNAL_COUNT, signals.Count(signal => signal.Kind == FlowSignalKind.FixedCredential));
		Assert.Contains("**Eleven signals are registered for Go, six interactive-user and five fixed-credential.**", PartA(), StringComparison.Ordinal);
	}

	/// <summary>The classes that are Low by definition are the ones whose "Which flow" paragraph says there is no flow.</summary>
	[Fact]
	public void TheClassesThatAreLowByDefinitionAreTheOnesThatSayThereIsNoFlow()
	{
		string[] lowClasses = [.. Rules().Where(entry => entry.Value.BaseLevel == ConfidenceLevel.Low).Select(entry => entry.Key).Order(StringComparer.Ordinal)];

		Assert.Equal([GoPartATestConstants.TRANSPORT_CLASS, GoPartATestConstants.HELPER_CLASS, GoPartATestConstants.ENVIRONMENT_CLASS], lowClasses);
	}

	/// <summary>Thirteen classes are registered, and Part A says that ten can produce a recommendation.</summary>
	[Fact]
	public void TheClassCountsPartAStatesAreTheAnalyzersOwn()
	{
		Assert.Equal(GoPartATestConstants.REGISTERED_CLASS_COUNT, Rules().Count);
		Assert.Contains("Thirteen classes are registered for Go. Ten of them can produce a recommendation.", PartA(), StringComparison.Ordinal);
		Assert.Contains(GoPartATestConstants.HELPER_CALL_CLASS, Rules().Keys);
	}

	private static string PartA()
	{
		return File.ReadAllText(_language.PartAPath());
	}

	private static IReadOnlyDictionary<string, DetectionRule> Rules()
	{
		return _language.Analyzer().DetectionRulesByPatternClass;
	}
}
