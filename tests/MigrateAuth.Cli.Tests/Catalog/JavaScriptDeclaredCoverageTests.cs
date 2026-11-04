using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Release;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE DECLARED UNSUPPORTED COVERAGE OF THE SHIPPED `JavaScript` ANALYZER, and the report section that lists it.
///
/// Forty-one entries are declared: the thirty-seven the catalog lists and its four internal limits. Each identifier below is bound to exactly one entry of the analyzer by a
/// word that only that entry's construct carries, so a dropped, duplicated or merged entry fails here. A form the catalog says is not supported must yield no finding, and the
/// tests below write such a form. Every script is invented, with placeholder names.
/// </summary>
public sealed class JavaScriptDeclaredCoverageTests
{
	private const int _DECLARED_ENTRY_COUNT = 41;
	private const int _ENTRIES_OF_THE_CATALOG_LIST = 37;
	private const int _INTERNAL_LIMIT_COUNT = 4;
	private const int _UNSUPPORTED_ENTRY_COUNT = 16;
	private const int _COST_ENTRY_COUNT = 12;
	private const int _NOT_APPLICABLE_ENTRY_COUNT = 8;
	private const string _LANGUAGE_NAME = "JavaScript";
	private const int _NESTED_TEMPLATE_DEPTH_BEYOND_THE_BOUND = 9;

	/// <summary>The identifier of every declared entry, with the words that only its construct carries.</summary>
	internal static readonly ImmutableArray<(string Id, string Keyword)> DeclaredEntries =
	[
		("unsupported:O5-1", "written by hand to a socket"),
		("unsupported:O5-2", "held in a variable, a module or a default export"),
		("unsupported:O5-3", "'Basic ...' literal held in a variable"),
		("unsupported:O5-4", "header-setting methods and hooks"),
		("unsupported:O5-5", "given directly to a jQuery"),
		("unsupported:O5-6", "calls to a wrapper function are not followed"),
		("unsupported:O5-7", "computed property names"),
		("unsupported:O5-8", "generated API clients"),
		("na:O5-9", "proxy credentials"),
		("unsupported:O5-10", "variable use across statements"),
		("unsupported:O5-11", "inside an executed string"),
		("unsupported:O5-12", "own wrapper beyond what one statement shows"),
		("unsupported:O5-13", "credentials assembled across modules"),
		("cost:O5-14", "HTML-like comments"),
		("na:O5-15", "Unicode line separator"),
		("unsupported:O5-16", "runtimes other than Node.js"),
		("na:O5-17", "typed in another module"),
		("cost:O5-18", "after a trailing line comment or after a block comment"),
		("cost:O5-19", "upper-case folders Dist and BUILD"),
		("O5-20", "any line over 10,000 characters"),
		("cost:O5-21", "JSX beyond the two rules"),
		("na:O5-22", "TypeScript files"),
		("cost:O5-23", "a quoted carrier key"),
		("unsupported:O5-24", "more than 300 characters from it"),
		("cost:O5-25", "written after a closing parenthesis"),
		("cost:O5-26", "nested more than eight deep"),
		("na:O5-27", "embedded in other files"),
		("na:O5-28", "sign-in evidence the tool does not list"),
		("cost:O5-29", "with a neutral name"),
		("cost:O5-30", "built and never sent"),
		("cost:O5-31", "a flow misread"),
		("unsupported:O5-32", "assembled from parts"),
		("na:O5-33", "where a credential lives"),
		("cost:O5-34", "follows a brace or a semicolon"),
		("na:O5-35", "folder the repository walk never enters"),
		("unsupported:O5-36", "flat array"),
		("cost:O5-37", "scheme is not visible"),
		("K-1", "takes too long"),
		("K-2", "findings cap"),
		("K-3", "a call goes to RelativityOne"),
		("K-4", "split across statements")
	];

	[Fact]
	public void TheAnalyzerDeclaresTheThirtySevenEntriesOfTheCatalogAndItsFourLimitsAndEveryEntryHasAReasonAndNoInternalTerm()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> declared = [.. JavaScriptRegisteredExpressions.Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage];

		Assert.Equal(_DECLARED_ENTRY_COUNT, declared.Length);
		Assert.Equal(_DECLARED_ENTRY_COUNT, DeclaredEntries.Length);
		Assert.Equal(_ENTRIES_OF_THE_CATALOG_LIST, DeclaredEntries.Count(entry => !entry.Id.StartsWith("K-", StringComparison.Ordinal)));
		Assert.Equal(_INTERNAL_LIMIT_COUNT, DeclaredEntries.Count(entry => entry.Id.StartsWith("K-", StringComparison.Ordinal)));
		Assert.Equal(_DECLARED_ENTRY_COUNT, declared.Select(entry => entry.Construct).Distinct(StringComparer.Ordinal).Count());
		Assert.All(declared, entry =>
		{
			Assert.False(string.IsNullOrWhiteSpace(entry.Construct));
			Assert.False(string.IsNullOrWhiteSpace(entry.Reason));
			Assert.Empty(InternalTermsIn(entry.Construct));
			Assert.Empty(InternalTermsIn(entry.Reason));
		});
	}

	[Fact]
	public void EveryIdentifierIsBoundToExactlyOneEntryAndEveryEntryToExactlyOneIdentifier()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> declared = [.. JavaScriptRegisteredExpressions.Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage];

		Assert.All(DeclaredEntries, entry =>
			Assert.Single(declared, candidate => candidate.Construct.Contains(entry.Keyword, StringComparison.Ordinal)));
		Assert.All(declared, candidate =>
			Assert.Single(DeclaredEntries, entry => candidate.Construct.Contains(entry.Keyword, StringComparison.Ordinal)));
	}

	[Fact]
	public void TheIdentifiersAreUniqueAndTheGroupsHoldSixteenUnsupportedTwelveCostAndEightNotApplicableEntries()
	{
		Assert.Equal(DeclaredEntries.Length, DeclaredEntries.Select(entry => entry.Id).Distinct(StringComparer.Ordinal).Count());
		Assert.Equal(_UNSUPPORTED_ENTRY_COUNT, DeclaredEntries.Count(entry => entry.Id.StartsWith("unsupported:", StringComparison.Ordinal)));
		Assert.Equal(_COST_ENTRY_COUNT, DeclaredEntries.Count(entry => entry.Id.StartsWith("cost:", StringComparison.Ordinal)));
		Assert.Equal(_NOT_APPLICABLE_ENTRY_COUNT, DeclaredEntries.Count(entry => entry.Id.StartsWith("na:", StringComparison.Ordinal)));
	}

	// ------------------------------------------------------------------------------------------
	// The report lists the entries
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheCoverageStatementCarriesTheEveryDeclaredEntryOfJavaScriptSortedOrdinallyWhenJavaScriptIsEnabled()
	{
		ImmutableArray<LanguageCoverageRow> rows = CoverageStatement.Compose(
			LanguageCatalogRegistry.BuildEmbeddedCatalog(),
			new HashSet<AssessedLanguage> { AssessedLanguage.JavaScript });
		LanguageCoverageRow row = rows.Single(candidate => candidate.LanguageName == _LANGUAGE_NAME);

		Assert.Equal(LanguageCoverageState.Supported, row.State);
		Assert.Equal(JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass.Count, row.PatternClassesSearched);
		Assert.Equal(_DECLARED_ENTRY_COUNT, row.DeclaredUnsupportedConstructs.Length);
		Assert.Equal(
			JavaScriptRegisteredExpressions.Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage.Select(entry => entry.Construct).Order(StringComparer.Ordinal),
			row.DeclaredUnsupportedConstructs.Select(entry => entry.Construct));
	}

	[Fact]
	public void AJavaScriptRowIsNoLongerDeclaredUnsupportedAndATypeScriptRowStillIs()
	{
		ImmutableArray<LanguageCoverageRow> rows = CoverageStatement.Compose(
			LanguageCatalogRegistry.BuildEmbeddedCatalog(),
			new HashSet<AssessedLanguage> { AssessedLanguage.JavaScript, AssessedLanguage.TypeScript });

		Assert.Equal(LanguageCoverageState.Supported, rows.Single(row => row.Language == AssessedLanguage.JavaScript).State);
		Assert.Equal(LanguageCoverageState.DeclaredUnsupported, rows.Single(row => row.Language == AssessedLanguage.TypeScript).State);
	}

	// ------------------------------------------------------------------------------------------
	// A declared form yields no finding where the catalog says so
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("socket.write('GET /x HTTP/1.1\\r\\nAuthorization: Basic dXNlcjpwYXNz\\r\\n\\r\\n');\n")]
	[InlineData("const api = axios.create(options);\nconst response = await api.get(url);\n")]
	[InlineData("const header = 'Basic ' + encoded;\nsend(header);\nconst auth = true;\n")]
	[InlineData("const name = 'Authorization';\nheaders[name] = 'Basic ' + encoded;\n")]
	[InlineData("const o = { [key]: 'Basic ' + encoded };\nconst auth = 1;\n")]
	[InlineData("exec(\"curl -u exampleUser:exampleSecret https://host.example/api\");\n")]
	[InlineData("const response = await client.users.list({ page: 1 });\nconst auth = session;\n")]
	[InlineData("await request(url, { headers: ['Authorization', 'Basic ' + encoded] });\n")]
	[InlineData("const target = 'https://exampleUser:' + secret + '@host.example/path';\n")]
	[InlineData("const options = loadOptions();\nawait fetch(url, options);\nconst auth = options;\n")]
	public void AFormTheCatalogDeclaresNotSupportedYieldsNoFinding(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{string.Join(", ", constructs.Select(construct => construct.PatternClassIdentifier))}]");
	}

	[Fact]
	public void AMockWithANeutralNameReportsAtItsFullLevelAsTheDeclaredCostSays()
	{
		const string source = "await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });\n";

		RecognizedConstruct construct = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source, "src/helper.js"));

		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	[Fact]
	public void AFileWithATemplateNestedBeyondTheBoundIsAbandonedAndAConstructAfterItIsNeverReadAboveLow()
	{
		string template = NestedTemplate(_NESTED_TEMPLATE_DEPTH_BEYOND_THE_BOUND);
		string source = $"const t = {template};\nawait fetch(url, {{ headers: {{ Authorization: `Basic ${{encoded}}` }} }});\n";

		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.All(constructs, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
	}

	[Theory]
	[InlineData("#!/bin/dash\n")]
	[InlineData("#!/usr/bin/env deno\n")]
	[InlineData("#!/usr/bin/env bun\n")]
	[InlineData("#!/usr/bin/env python3\n")]
	[InlineData("#!/usr/bin/env nodemon\n")]
	public void AFirstLineThatNamesAnotherRuntimeIsNotAttributedToJavaScript(string firstLine)
	{
		Assert.DoesNotMatch(JavaScriptRegisteredExpressions.Analyzer.LanguageIdentification.FirstLineDetectionExpression!, firstLine);
	}

	[Theory]
	[InlineData(".ts")]
	[InlineData(".tsx")]
	[InlineData(".mts")]
	[InlineData(".cts")]
	[InlineData(".html")]
	[InlineData(".vue")]
	[InlineData(".json")]
	public void AnExtensionTheCatalogDoesNotAttributeIsNotAJavaScriptExtension(string extension)
	{
		ISet<string> extensions = JavaScriptRegisteredExpressions.Analyzer.LanguageIdentification.FileExtensions;

		Assert.DoesNotContain(extension, extensions);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static string NestedTemplate(int depth)
	{
		return depth == 0 ? "f('}`')" : $"`a${{{NestedTemplate(depth - 1)}}}z`";
	}

	private static ImmutableArray<string> InternalTermsIn(string text)
	{
		return
		[
			.. PublicSourceIdentifierTests.InternalCoverageTerms().Where(term => text.Contains(term, StringComparison.Ordinal)),
			.. PublicSourceIdentifierTests.InternalIdentifierPatterns().SelectMany(pattern => pattern.Matches(text)).Select(match => match.Value)
		];
	}
}
