using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Documentation;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE DECLARED COVERAGE OF `CSharp`, AND ITS PARITY WITH PART A.
///
/// The entries include the raw-string clause, the colon-joined non-credential value, a conditional block in a file whose raw
/// literal the scanner cannot close, and a declaration or a flow signal in a switched-off block that still counts outside it. The
/// forms the engine reports at Low although they may be genuine (a pair of neutral halves, a credential word outside the lists, a
/// symbol that only the project file defines) are stated in the class text and Part A and are NOT declared entries: an entry would
/// leave them out of the recall denominator.
/// </summary>
public sealed class CSharpDeclaredCoverageTests
{
	private const string _LANGUAGE_NAME = "CSharp";
	private const string _SWITCHED_OFF_BLOCK_ENTRY_PHRASE = "conditional-compilation blocks in a file that also holds an interpolated raw string";
	private const string _DEAD_REGION_EVIDENCE_ENTRY_PHRASE = "a declaration or a flow signal inside a switched-off conditional-compilation block";
	private const string _RAW_STRING_ENTRY_PHRASE = "certain unusual string literal forms";
	private const string _NON_CREDENTIAL_PAIR_ENTRY_PHRASE = "separated by a colon that is not actually a credential";
	private const string _OLD_PREPROCESSOR_CLAUSE = "preprocessor conditional block";
	private const string _PROJECT_SYMBOL_PHRASE = "project file or the build command";
	private const string _SECTION_HEADING = "## Declared unsupported coverage";
	private const int _EXPECTED_ENTRY_COUNT = 30;

	private static readonly Regex _SENTENCE_END = new(@"[;.] ", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

	/// <summary>The entries as the analyzer declares them.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> Entries()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		return [.. catalog.AnalyzersByLanguageName[_LANGUAGE_NAME].LanguageIdentification.DeclaredUnsupportedCoverage];
	}

	private static string PartA()
	{
		return File.ReadAllText(DocumentedLanguage.CSharp.PartAPath());
	}

	private static DeclaredUnsupportedCoverage EntryNamed(string phrase)
	{
		return Entries().Single(entry => entry.Construct.Contains(phrase, StringComparison.Ordinal));
	}

	[Fact]
	public void TheDeclaredCoverageHoldsTheNarrowedAndTheNewEntries()
	{
		Assert.Equal(_EXPECTED_ENTRY_COUNT, Entries().Length);
		Assert.Equal(_EXPECTED_ENTRY_COUNT, Entries().Select(entry => entry.Construct).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void TheRawStringEntryKeepsOnlyTheRawStringClauseAndNoLongerNamesTheConditionalBlock()
	{
		DeclaredUnsupportedCoverage entry = EntryNamed(_RAW_STRING_ENTRY_PHRASE);

		Assert.DoesNotContain(_OLD_PREPROCESSOR_CLAUSE, entry.Construct, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("#if", entry.Construct, StringComparison.Ordinal);
		Assert.Contains("more than one \"$\"", entry.Construct, StringComparison.Ordinal);
	}

	[Fact]
	public void NoDeclaredEntryStillSaysThatABlockSwitchedOffByAFalseConditionIsReadAsLive()
	{
		string partA = PartA();

		Assert.DoesNotContain("reported as ordinary code", partA, StringComparison.Ordinal);
		Assert.DoesNotContain(Entries(), entry => entry.Construct.Contains(_OLD_PREPROCESSOR_CLAUSE, StringComparison.OrdinalIgnoreCase));
		Assert.DoesNotContain(Entries(), entry => entry.Reason.Contains("literal `false`", StringComparison.Ordinal));
	}

	[Fact]
	public void TheColonJoinedNonCredentialEntryStatesLowUnlessAHalfIsCredentialNamed()
	{
		DeclaredUnsupportedCoverage entry = EntryNamed(_NON_CREDENTIAL_PAIR_ENTRY_PHRASE);

		Assert.Contains("at Low", entry.Reason, StringComparison.Ordinal);
		Assert.Contains("at Medium", entry.Reason, StringComparison.Ordinal);
		Assert.Contains("neither part is named like a user name or a password", entry.Reason, StringComparison.Ordinal);
	}

	[Fact]
	public void TheTwoNewConditionalCompilationEntriesAreDeclaredWithAReviewInstruction()
	{
		DeclaredUnsupportedCoverage rawLiteralFile = EntryNamed(_SWITCHED_OFF_BLOCK_ENTRY_PHRASE);
		DeclaredUnsupportedCoverage deadRegionEvidence = EntryNamed(_DEAD_REGION_EVIDENCE_ENTRY_PHRASE);

		Assert.Contains("reported at the level of its class", rawLiteralFile.Reason, StringComparison.Ordinal);
		Assert.Contains("review code in conditional blocks of such files by hand", rawLiteralFile.Reason, StringComparison.Ordinal);
		Assert.Contains("counted twice", deadRegionEvidence.Reason, StringComparison.Ordinal);
		Assert.Contains("review such files by hand", deadRegionEvidence.Reason, StringComparison.Ordinal);
	}

	[Fact]
	public void TheClaimedAtLowFormsAreNotDeclaredEntries()
	{
		foreach (DeclaredUnsupportedCoverage entry in Entries())
		{
			Assert.DoesNotContain("project file", entry.Construct, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("neutrally named", entry.Construct, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("outside the lists", entry.Construct, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public void TheClaimedAtLowFormsAreStatedInPartAOutsideTheDeclaredCoverageSection()
	{
		string partA = PartA();
		int sectionStart = partA.IndexOf(_SECTION_HEADING, StringComparison.Ordinal);
		string beforeTheSection = partA[..sectionStart];

		Assert.Contains(_PROJECT_SYMBOL_PHRASE, beforeTheSection, StringComparison.Ordinal);
		Assert.Contains("both named neutrally", beforeTheSection, StringComparison.Ordinal);
		Assert.Contains("outside the lists above", beforeTheSection, StringComparison.Ordinal);
		Assert.DoesNotContain(_PROJECT_SYMBOL_PHRASE, partA[sectionStart..], StringComparison.Ordinal);
	}

	[Fact]
	public void EveryEntryIsListedInPartAWithItsConstructText()
	{
		ImmutableArray<string> problems = PublicDocumentChecks.UnsupportedProblems(PartA(), Entries());

		Assert.True(problems.IsEmpty, string.Join(Environment.NewLine, problems));
	}

	[Fact]
	public void PartAListsTheFourChangedEntriesWithTheirReasons()
	{
		string partA = Regex.Replace(PartA(), @"\s+", " ");

		foreach (string phrase in new[] { _RAW_STRING_ENTRY_PHRASE, _NON_CREDENTIAL_PAIR_ENTRY_PHRASE, _SWITCHED_OFF_BLOCK_ENTRY_PHRASE, _DEAD_REGION_EVIDENCE_ENTRY_PHRASE })
		{
			DeclaredUnsupportedCoverage entry = EntryNamed(phrase);

			Assert.Contains(_SENTENCE_END.Split(entry.Reason)[0], partA, StringComparison.OrdinalIgnoreCase);
		}
	}
}
