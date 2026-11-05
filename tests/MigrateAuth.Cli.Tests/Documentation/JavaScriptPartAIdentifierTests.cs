using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// EVERY CLASS IDENTIFIER AND EVERY DECLARED-UNSUPPORTED ENTRY OF SHELL APPEARS IN ITS PART A.
///
/// These are the Part A checks of <see cref="PartCDriftTests"/>, run for JavaScript on their own, apart from the checks of Part C that compose the generated blocks.
/// A class that loses its heading or one of its four labelled elements, an entry of the declared
/// unsupported coverage that the document does not list, a "Which flow" paragraph that does not agree with the signal section, and a signal the section does
/// not list under its own kind, each fail here.
/// </summary>
public sealed class JavaScriptPartAIdentifierTests
{
	private const string _JAVASCRIPT_FENCE = "```javascript";
	private const string _REMOVED_TEXT = "removed";

	private static readonly DocumentedLanguage _language = DocumentedLanguage.JavaScript;

	/// <summary>Every class the analyzer registers appears in Part A with its heading, its example, its search, its confidence and its flow.</summary>
	[Fact]
	public void EveryRegisteredClassAppearsInPartAWithAllItsElements()
	{
		ImmutableArray<string> problems = PublicDocumentChecks.PartAClassProblems(PartA(), Rules(), _JAVASCRIPT_FENCE);

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

		Assert.Contains(PublicDocumentChecks.PartAClassProblems(withoutHeading, Rules(), _JAVASCRIPT_FENCE), problem => problem.Contains(removed.Key, StringComparison.Ordinal));
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

	/// <summary>The five classes that are Low by definition are the ones whose "Which flow" paragraph says there is no flow.</summary>
	[Fact]
	public void TheClassesThatAreLowByDefinitionAreTheOnesThatSayThereIsNoFlow()
	{
		string[] lowClasses = [.. Rules().Where(entry => entry.Value.BaseLevel == ConfidenceLevel.Low).Select(entry => entry.Key).Order(StringComparer.Ordinal)];

		Assert.Equal(["JS-007", "JS-009", "JS-011", "JS-014", "JS-016"], lowClasses);
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
