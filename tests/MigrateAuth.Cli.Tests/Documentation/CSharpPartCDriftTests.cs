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
	private const string _PROXY_REASON = "this call is matched only when the second argument is named like a password, or the first argument is named like a user, or both arguments are written as text, and it is never matched when the second argument is named like a token or a key. Other calls that send a user name and a password are not matched, so search for GetProxy and review each call by hand. A call of an unrelated method with the same name and similar arguments can be matched, and should be dismissed on review";
	private const string _PROXY_REASON_IN_PART_A = "this call is matched only when the second argument is named like a password, or the first argument is named like a user, or both arguments are written as text, and it is never matched when the second argument is named like a token or a key. Other calls that send a user name and a password are not matched, so search for GetProxy and review each call by hand. A call of an unrelated method with the same name and similar arguments can match, and should be dismissed on review";
	private const string _PAIR_REASON = "the pair is read only when both members are set next to each other in one of the two plain forms; a pair set any other way can still send a user name and a password and is not matched, so search for Rsapi and review each settings object by hand. A declaration written over two lines can be matched as if it set the pair";
	private const string _PAIR_REASON_IN_PART_A = "the pair is read only when both members are set next to each other in one of the two plain forms; a pair set any other way can still send a user name and a password and is not matched, so search for Rsapi and review each settings object by hand. A declaration written over two lines can match as if it set the pair";
	private const string _STORED_REASON = "the credential is followed only inside the window and the forms described in its entry. Outside them the credential is matched as an ordinary network credential at medium confidence, which is a false match when it goes to a mail, proxy or file-transfer server, so confirm the destination of every such match. A credential also handed to an HTTP client in the same file is given low confidence as the mail, proxy or file-transfer match, and the HTTP use gets no match of its own, so review the HTTP use by hand. A client or a hand-over written inside a comment or a text can be taken for real code";
	private const string _STORED_REASON_IN_PART_A = "the credential is followed only inside the window and the forms described in its entry. Outside them the credential matches as an ordinary network credential at Medium, which is a false match when it goes to a mail, proxy or file-transfer server, so confirm the destination of every such match. A credential also handed to an HTTP client in the same file matches at Low as the mail, proxy or file-transfer credential, and the HTTP use gets no match of its own, so review the HTTP use by hand. A client or a hand-over written inside a comment or a text can be taken for real code";
	private const string _RETIRED_CLIENT_REASON_TAIL = "ugh a factory with no visible credential in that same code is not detected, even though it is still worth reviewing by hand. A factory call that passes a user name and a password, \"GetProxy<T>(user, password)\", is a different case and is read in the forms described in the entry for calls to GetProxy";
	private const string _RETIRED_CLIENT_REASON_TAIL_IN_PART_A = "ugh a factory with no visible credential in that same code is not matched, even though it is still worth reviewing by hand. A factory call that passes a user name and a password, \"GetProxy<T>(user, password)\", is a different case and is read in the forms described in the entry for calls to GetProxy";

	/// <summary>
	/// Where Part A words a declared reason differently from the analyzer, the exact wording Part A is pinned to, keyed by the declared reason itself: Part A says
	/// "matched" where the declared reason says "reported" or "detected", and "Medium" where it says "medium confidence". A reason with no entry is compared verbatim,
	/// and a declared reason that changes has no entry and fails, so a Part A sentence that merely says "matched" cannot satisfy another reason.
	/// </summary>
	private static readonly ImmutableDictionary<string, string> _PART_A_WORDING_OF_DECLARED_REASON = ImmutableDictionary.CreateRange(StringComparer.Ordinal,
	[
		KeyValuePair.Create(_PROXY_REASON, _PROXY_REASON_IN_PART_A),
		KeyValuePair.Create(_PAIR_REASON, _PAIR_REASON_IN_PART_A),
		KeyValuePair.Create(_STORED_REASON, _STORED_REASON_IN_PART_A),
		KeyValuePair.Create(_RETIRED_CLIENT_REASON_TAIL, _RETIRED_CLIENT_REASON_TAIL_IN_PART_A)
	]);

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

		Assert.All(keywords, keyword => Assert.Contains(PartAWordingOf(Assert.Single(declared, entry => entry.Construct.Contains(keyword, StringComparison.Ordinal)).Reason), partA, StringComparison.OrdinalIgnoreCase));
		Assert.Contains(PartAWordingOf(declared.Single(entry => entry.Construct.StartsWith("a project's own custom authentication helper", StringComparison.Ordinal)).Reason[^_REASON_TAIL_LENGTH..]), partA, StringComparison.OrdinalIgnoreCase);
		Assert.Contains(PartAWordingOf(declared.Single(entry => entry.Construct.StartsWith("the retired RSAPI client", StringComparison.Ordinal)).Reason[^_REASON_TAIL_LENGTH..]), partA, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The text Part A is held to for a declared reason: the pinned wording where Part A words it differently, and the declared reason itself, verbatim, where it does not.</summary>
	private static string PartAWordingOf(string declaredReason)
	{
		return Normalize(_PART_A_WORDING_OF_DECLARED_REASON.GetValueOrDefault(declaredReason, declaredReason));
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
