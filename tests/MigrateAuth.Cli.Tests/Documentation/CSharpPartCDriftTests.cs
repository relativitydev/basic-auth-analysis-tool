using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>The Part A and Part C checks of <see cref="PartCDriftTests"/>, run for C#, and the checks of the entries of CS-017, CS-019 and CS-020 in Part A and Part C.</summary>
public sealed class CSharpPartCDriftTests : PartCDriftTests
{
	private const int _REASON_TAIL_LENGTH = 300;
	private const string _PROXY_FACTORY_CLASS = "CS-020";
	private const string _SINK_CLASS = "CS-019";
	private const string _SETTINGS_PAIR_CLASS = "CS-017";
	private const string _PROXY_ENTRY_KEYWORD = "GetProxy";
	private const string _PAIR_ENTRY_KEYWORD = "Rsapi";
	private const string _STORED_ENTRY_KEYWORD = "NetworkCredential";
	private const string _WINDOW_PHRASE = "30 lines";
	private const string _LINE_LENGTH_PHRASE = "400 characters";
	private const string _SECOND_SIGNAL_PHRASE = "named like a password";
	private const string _TOKEN_NEAR_MISS_PHRASE = "token-named";
	private const int _CORRECTION_ENTRY_COUNT = 3;
	private const string _PART_C_HEADING = "### CS-020 · SdkProxyFactoryWithUserAndPassword";

	public CSharpPartCDriftTests()
		: base(DocumentedLanguage.CSharp)
	{
	}

	/// <summary>Part A restates, word for word, the Reason of the three client-facing entries and the end of the Reason of the custom-helper and retired-client entries.</summary>
	[Fact]
	public void PartARestatesTheReasonOfTheThreeEntriesOfTheCorrectionsAndTheTwoChangedEntries()
	{
		string partA = NormalizedUnsupportedSection();
		ImmutableArray<DeclaredUnsupportedCoverage> declared = [.. DocumentedLanguage.CSharp.Analyzer().LanguageIdentification.DeclaredUnsupportedCoverage];
		string[] keywords = [_PROXY_ENTRY_KEYWORD, _PAIR_ENTRY_KEYWORD, _STORED_ENTRY_KEYWORD];

		Assert.All(keywords, keyword => Assert.Contains(Normalize(Assert.Single(declared, entry => entry.Construct.Contains(keyword, StringComparison.Ordinal)).Reason), partA, StringComparison.OrdinalIgnoreCase));
		Assert.Contains(Normalize(declared.Single(entry => entry.Construct.StartsWith("a project's own custom authentication helper", StringComparison.Ordinal)).Reason[^_REASON_TAIL_LENGTH..]), partA, StringComparison.OrdinalIgnoreCase);
		Assert.Contains(Normalize(declared.Single(entry => entry.Construct.StartsWith("the retired RSAPI client", StringComparison.Ordinal)).Reason[^_REASON_TAIL_LENGTH..]), partA, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The three entries are written in plain words in Part A: no identifier of a pattern class, of a declared form, and no word of the catalog.</summary>
	[Fact]
	public void TheThreeEntriesOfTheCorrectionsInPartAHaveNoIdentifierAndNoWordOfTheCatalog()
	{
		string unsupported = PublicDocumentChecks.SectionOf(File.ReadAllText(DocumentedLanguage.CSharp.PartAPath()), PublicDocumentChecks.UNSUPPORTED_SECTION_HEADING) ?? string.Empty;
		string[] entries =
		[
			.. unsupported.Split('\n').Where(line => line.StartsWith("- **", StringComparison.Ordinal))
				.Where(line => line.Contains($"{_PROXY_ENTRY_KEYWORD} that passes", StringComparison.Ordinal)
					|| line.Contains($"two members whose names start with {_PAIR_ENTRY_KEYWORD}", StringComparison.Ordinal)
					|| line.Contains("stored in a member of a settings object", StringComparison.Ordinal))
		];

		Assert.Equal(_CORRECTION_ENTRY_COUNT, entries.Length);
		Assert.All(entries, entry => Assert.DoesNotMatch(@"\bCS-\d+\b|\bO5-\d+\b", entry));
		Assert.All(entries, entry => Assert.DoesNotContain("catalog", entry, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>Part A's entry of CS-020 states the second-signal rule and the token near-miss, and its entry of CS-019 the thirty-line window.</summary>
	[Fact]
	public void PartAStatesTheSecondSignalRuleTheTokenNearMissAndTheThirtyLineWindow()
	{
		string partA = File.ReadAllText(DocumentedLanguage.CSharp.PartAPath());
		string proxyFactory = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(_PROXY_FACTORY_CLASS, DisplayNameOf(_PROXY_FACTORY_CLASS))) ?? string.Empty;
		string sink = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(_SINK_CLASS, DisplayNameOf(_SINK_CLASS))) ?? string.Empty;
		string settingsPair = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(_SETTINGS_PAIR_CLASS, DisplayNameOf(_SETTINGS_PAIR_CLASS))) ?? string.Empty;

		Assert.Contains(_SECOND_SIGNAL_PHRASE, proxyFactory, StringComparison.Ordinal);
		Assert.Contains(_TOKEN_NEAR_MISS_PHRASE, proxyFactory, StringComparison.Ordinal);
		Assert.Contains(_WINDOW_PHRASE, sink, StringComparison.Ordinal);
		Assert.Contains(_LINE_LENGTH_PHRASE, sink, StringComparison.Ordinal);
		Assert.Contains("RsapiUserName", settingsPair, StringComparison.Ordinal);
	}

	/// <summary>Part C holds a heading and one generated block per flow for the proxy factory class (CS-020), and an anchor its Part A paragraph links to.</summary>
	[Fact]
	public void PartCHoldsTheEntryOfTheProxyFactoryClassForBothFlowsAndItsAnchor()
	{
		string partC = File.ReadAllText(DocumentedLanguage.CSharp.PartCPath());
		string[] blockKeys = [.. GeneratedBlockMarkers.Blocks(partC).Select(block => block.Key)];

		Assert.Contains(_PART_C_HEADING, partC, StringComparison.Ordinal);
		Assert.Contains("<a id=\"cs-020-migration\"></a>", partC, StringComparison.Ordinal);
		Assert.Contains("CS-020 client-credentials", blockKeys);
		Assert.Contains("CS-020 authorization-code", blockKeys);
	}

	private static string DisplayNameOf(string patternClassIdentifier)
	{
		return DocumentedLanguage.CSharp.Analyzer().DetectionRulesByPatternClass[patternClassIdentifier].DisplayName;
	}

	private static string NormalizedUnsupportedSection()
	{
		string section = PublicDocumentChecks.SectionOf(File.ReadAllText(DocumentedLanguage.CSharp.PartAPath()), PublicDocumentChecks.UNSUPPORTED_SECTION_HEADING) ?? string.Empty;

		return Normalize(section.Replace("**", string.Empty, StringComparison.Ordinal));
	}

	private static string Normalize(string text)
	{
		return PublicDocumentChecks.NormalizeWhitespace(text.Replace("`", string.Empty, StringComparison.Ordinal));
	}
}
