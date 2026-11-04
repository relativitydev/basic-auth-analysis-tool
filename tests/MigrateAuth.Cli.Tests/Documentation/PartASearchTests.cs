using System.Collections.Immutable;
using System.Reflection;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// THE SEARCHES OF PART A ARE CHECKED AGAINST THE SAME FORMS THE ANALYZER IS TESTED ON.
///
/// A reader who cannot run the tool searches with these commands, so a search that misses a form the analyzer covers
/// sends that reader away with a clean result for code the tool would have reported. Each class's searches, taken
/// together, must find every covered form of the class, and each search must find at least one of them, so that a
/// search which can never match does not pass for one that matches everything. The forms are the analyzer's own
/// detection fixtures plus the placeholder examples Part A shows.
/// </summary>
public abstract class PartASearchTests
{
	private readonly DocumentedLanguage _language;

	protected PartASearchTests(DocumentedLanguage language)
	{
		_language = language;
	}

	/// <summary>Every form the language's analyzer is tested to cover, outside Part A's own examples.</summary>
	internal abstract ImmutableArray<SearchableForm> CoveredForms();

	/// <summary>Every class of the analyzer has at least one search in its section of Part A.</summary>
	[Fact]
	public void EveryClassGivesAtLeastOneSearch()
	{
		ImmutableArray<DocumentedSearch> searches = PartASearchChecks.SearchesOf(File.ReadAllText(_language.PartAPath()), Rules());

		string[] withoutSearch =
		[
			.. Rules().Keys.Where(patternClassIdentifier => searches.All(search => search.PatternClassIdentifier != patternClassIdentifier)).Order(StringComparer.Ordinal)
		];

		Assert.True(withoutSearch.Length == 0, $"Part A gives no search for: {string.Join(", ", withoutSearch)}");
	}

	/// <summary>
	/// The "Find your ..." table sends a reader who knows the situation and not the class to a class: every class of the
	/// analyzer is a link in it, and every link names a class of the analyzer, compared without regard to letter case.
	/// </summary>
	[Fact]
	public void TheScenarioTableLinksEveryClassOfTheAnalyzerAndNothingElse()
	{
		string partA = File.ReadAllText(_language.PartAPath());
		int start = partA.IndexOf("## Find your ", StringComparison.Ordinal);
		Assert.True(start >= 0, "Part A has no \"Find your ...\" table.");
		int end = partA.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
		string section = partA[start..end];

		HashSet<string> linked = new(
			System.Text.RegularExpressions.Regex.Matches(section, @"\]\(#([a-z]+-[0-9]+)\)").Select(match => match.Groups[1].Value),
			StringComparer.OrdinalIgnoreCase);
		HashSet<string> declared = new(Rules().Keys, StringComparer.OrdinalIgnoreCase);

		Assert.True(declared.SetEquals(linked), $"The table links [{string.Join(", ", linked.Order(StringComparer.Ordinal))}] but the analyzer declares [{string.Join(", ", declared.Order(StringComparer.Ordinal))}].");
	}

	/// <summary>Every search is written in the syntax that rg, grep -E and the test's engine read the same way.</summary>
	[Fact]
	public void EverySearchUsesOnlySyntaxThatGrepAndRipgrepShare()
	{
		ImmutableArray<DocumentedSearch> searches = PartASearchChecks.SearchesOf(File.ReadAllText(_language.PartAPath()), Rules());
		ImmutableArray<string> problems = PartASearchChecks.UnsharedSyntaxProblems(searches);

		Assert.True(problems.IsEmpty, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The searches of each class find every form the analyzer is tested to cover for that class, and every example Part A shows.</summary>
	[Fact]
	public void TheSearchesOfEachClassFindEveryFormTheAnalyzerCovers()
	{
		string partA = File.ReadAllText(_language.PartAPath());
		ImmutableArray<DocumentedSearch> searches = PartASearchChecks.SearchesOf(partA, Rules());
		ImmutableArray<SearchableForm> forms = AllForms(partA);

		ImmutableArray<string> problems = PartASearchChecks.MissedFormProblems(searches, forms);

		Assert.NotEmpty(forms);
		Assert.True(problems.IsEmpty, $"{problems.Length} covered form(s) are not found by the documented searches:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
	}

	/// <summary>No search is dead: each finds at least one form of its own class.</summary>
	[Fact]
	public void EverySearchFindsAtLeastOneFormOfItsOwnClass()
	{
		string partA = File.ReadAllText(_language.PartAPath());
		ImmutableArray<DocumentedSearch> searches = PartASearchChecks.SearchesOf(partA, Rules());

		ImmutableArray<string> problems = PartASearchChecks.SearchesThatFindNothingProblems(searches, AllForms(partA));

		Assert.True(problems.IsEmpty, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The check fails on a form no search finds and on a search that finds nothing, shown on synthetic input.</summary>
	[Fact]
	public void TheSearchCheckReportsAFormThatNoSearchFindsAndASearchThatFindsNothing()
	{
		string partA = File.ReadAllText(_language.PartAPath());
		ImmutableArray<DocumentedSearch> searches = PartASearchChecks.SearchesOf(partA, Rules());
		DocumentedSearch first = searches[0];
		SearchableForm missed = new(first.PatternClassIdentifier, "a form written with no word the search reads", "nothing recognizable here");
		DocumentedSearch neverMatches = first with { Expression = "a text that no source contains 9f3c1e" };

		Assert.Contains(PartASearchChecks.MissedFormProblems([first], [missed]), problem => problem.Contains(missed.Label, StringComparison.Ordinal));
		Assert.Contains(PartASearchChecks.SearchesThatFindNothingProblems([neverMatches], AllForms(partA)), problem => problem.Contains(first.PatternClassIdentifier, StringComparison.Ordinal));
		Assert.Empty(PartASearchChecks.MissedFormProblems([first], []));
	}

	/// <summary>The syntax check fails on a look-around, shown on a synthetic search.</summary>
	[Fact]
	public void TheSyntaxCheckReportsALookBehind()
	{
		DocumentedSearch lookBehind = new("XX-001", "(?<![\\w])Credential", IgnoresCase: true);
		DocumentedSearch plain = new("XX-001", "Credential\\s*=", IgnoresCase: true);

		Assert.Single(PartASearchChecks.UnsharedSyntaxProblems([lookBehind, plain]));
	}

	private ImmutableArray<SearchableForm> AllForms(string partA)
	{
		return [.. CoveredForms(), .. PartASearchChecks.ExamplesOf(partA, Rules(), _language.ExampleFenceLabel)];
	}

	private IReadOnlyDictionary<string, DetectionRule> Rules()
	{
		return _language.Analyzer().DetectionRulesByPatternClass;
	}
}

/// <summary>The searches of Part A for C#, checked against the creation forms, the named-constant scheme and the other forms its analyzer is tested to read.</summary>
public sealed class CSharpPartASearchTests : PartASearchTests
{
	public CSharpPartASearchTests()
		: base(DocumentedLanguage.CSharp)
	{
	}

	internal override ImmutableArray<SearchableForm> CoveredForms()
	{
		return [.. CSharpDetectionFormFixtures.CoveredForms.Select(form => new SearchableForm(form.PatternClass, form.Label, form.Source))];
	}
}

/// <summary>
/// The searches of Part A for PowerShell, checked against the forms the analyzer's own tests read: the one fixture of each class and every
/// source of the detection theories of <see cref="PowerShellAnalyzerTests"/>, which are the forms the catalog decides one by one.
/// </summary>
public sealed class PowerShellPartASearchTests : PartASearchTests
{
	/// <summary>The detection theories that assert a class fires, and the class each one asserts: the sources are read from their data rows.</summary>
	private static readonly (string TheoryName, string PatternClass)[] _detectionTheories =
	[
		(nameof(PowerShellAnalyzerTests.TheSdkCredentialIsReadInEveryCreationFormAndEveryTypeSpelling), "PS-001"),
		(nameof(PowerShellAnalyzerTests.TheLegacyClientIsReadWhateverTheSpellingOfItsEndpoint), "PS-002"),
		(nameof(PowerShellAnalyzerTests.TheExplicitSchemeParameterIsReadUnderEveryPrefixDashAndSpelling), "PS-003"),
		(nameof(PowerShellAnalyzerTests.TheHashtableLiteralWithASchemeBearingValueIsReadWhereverItIsUsed), "PS-004"),
		(nameof(PowerShellAnalyzerTests.AWriteOfTheHeaderOnAnyReceiverIsReadInEveryWriteForm), "PS-005"),
		(nameof(PowerShellAnalyzerTests.TheEnumerationWriteIsReadFullyQualifiedPartlyQualifiedOrByItsShortName), "PS-006"),
		(nameof(PowerShellAnalyzerTests.ATypedHeaderValueIsReadInEveryConstructionParseAndCastForm), "PS-007"),
		(nameof(PowerShellAnalyzerTests.TheCredentialCacheEntryIsReadWithTheCredentialAConstructionACallOrAVariable), "PS-008"),
		(nameof(PowerShellAnalyzerTests.ACredentialOnAnAllowListedHttpReceiverIsReadWhereverItIsGiven), "PS-009"),
		(nameof(PowerShellAnalyzerTests.AColonPairOfTwoNonEmptyHalvesReachingTheEncoderIsReadInEverySpelling), "PS-010"),
		(nameof(PowerShellAnalyzerTests.ATokenWithOneEmptyHalfUnderTheBasicSchemeIsTheTokenClassAndNeverThePairClass), "PS-011"),
		(nameof(PowerShellAnalyzerTests.ACredentialOnAWebCmdletOrAWebSessionWithoutASchemeIsLowAndForManualReview), "PS-012"),
		(nameof(PowerShellAnalyzerTests.ACredentialConsumerOfTheClosedNonHttpSetIsALowRowForManualReview), "PS-013"),
		(nameof(PowerShellAnalyzerTests.AFunctionNamedForTheSchemeAndARoleWordIsTheHelperDefinitionAtLow), "PS-014")
	];

	public PowerShellPartASearchTests()
		: base(DocumentedLanguage.PowerShell)
	{
	}

	internal override ImmutableArray<SearchableForm> CoveredForms()
	{
		ImmutableArray<SearchableForm>.Builder forms = ImmutableArray.CreateBuilder<SearchableForm>();

		foreach ((string patternClass, ConfidenceLevel level, string source) in PowerShellAnalyzerTests.ClassFixtures())
		{
			forms.Add(new SearchableForm(patternClass, $"{patternClass} fixture at {level}", source));
		}

		foreach ((string theoryName, string patternClass) in _detectionTheories)
		{
			forms.AddRange(SourcesOf(theoryName).Select((source, index) => new SearchableForm(patternClass, $"{patternClass} {theoryName} row {index + 1}", source)));
		}

		return forms.ToImmutable();
	}

	private static IEnumerable<string> SourcesOf(string theoryName)
	{
		MethodInfo theory = typeof(PowerShellAnalyzerTests).GetMethod(theoryName)
			?? throw new InvalidOperationException($"The detection theory {theoryName} does not exist.");

		return theory
			.GetCustomAttributes<InlineDataAttribute>()
			.SelectMany(row => row.GetData(theory))
			.Select(values => (string)values[0]);
	}
}

/// <summary>
/// The searches of Part A for VB.NET, checked against the forms the analyzer's own tests read: the one fixture of each class and every cell of the
/// type-by-form table of <see cref="VBNetAnalyzerTests"/>, which are the forms the catalog decides one by one.
/// </summary>
public sealed class VBNetPartASearchTests : PartASearchTests
{
	public VBNetPartASearchTests()
		: base(DocumentedLanguage.VBNet)
	{
	}

	internal override ImmutableArray<SearchableForm> CoveredForms()
	{
		ImmutableArray<SearchableForm>.Builder forms = ImmutableArray.CreateBuilder<SearchableForm>();

		foreach ((string patternClass, ConfidenceLevel level, string source) in VBNetAnalyzerTests.ClassFixtures())
		{
			forms.Add(new SearchableForm(patternClass, $"{patternClass} fixture at {level}", source));
		}

		int row = 0;

		foreach (object[] values in VBNetAnalyzerTests.CoveredForms())
		{
			row++;
			string patternClass = (string)values[0];

			forms.Add(new SearchableForm(patternClass, $"{patternClass} covered form {row}", (string)values[2]));
		}

		return forms.ToImmutable();
	}
}
