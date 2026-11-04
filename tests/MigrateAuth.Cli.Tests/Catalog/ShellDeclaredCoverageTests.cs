using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Release;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE DECLARED UNSUPPORTED COVERAGE OF THE SHIPPED `Shell` ANALYZER AGAINST THE CATALOG THAT STATES IT.
///
/// The tests that read the catalog document itself are in `ShellDeclaredCoverageCatalogTests`; they fail when either side moves: the identifiers and the count of
/// the declared set are the document's, and each identifier is bound to exactly one entry of the analyzer by a word that only that entry's construct carries.
/// One entry is declared beyond the document's list (a dead branch and code after an exit, which the document states stay declared), so the analyzer's set is
/// the document's plus that one. A form the catalog says is not supported must yield no finding, and the tests below write such a form.
/// </summary>
public sealed class ShellDeclaredCoverageTests
{
	private const int _DECLARED_ENTRY_COUNT = 23;

	/// <summary>The identifier of every declared entry, with the words that only its construct carries.</summary>
	internal static readonly ImmutableArray<(string Id, string Keyword)> DeclaredEntries =
	[
		("na:O5-1", "fish, csh"),
		("na:O5-2", "embedded in other files"),
		("na:O5-3", ".bats"),
		("na:O5-42", "single-byte encoding"),
		("na:O5-4", "a file the script only reads"),
		("unsupported:O5-6", "token or scheme supplied by another file"),
		("na:O5-7", "signs in that is supplied from another file"),
		("na:O5-8", "sign-in evidence the tool does not list"),
		("na:O5-40", "environment variable first"),
		("unsupported:O5-9", "passed to the client as a header file"),
		("unsupported:O5-47", "a name reached by indirection"),
		("unsupported:O5-17", "any other client"),
		("cost:O5-31", "with a neutral name"),
		("K-6", "dead branch"),
		("K-5", "a call goes to RelativityOne"),
		("na:O5-41", "ANSI-C quoting"),
		("na:O5-44", "continued across a backslash"),
		("na:O5-45", "quoted across a raw line feed"),
		("na:O5-46", "command substitution that closes on the opening line"),
		("K-1", "takes too long"),
		("K-2", "findings cap"),
		("K-7", "5,000 here-document openings"),
		("K-8", "double parenthesis")
	];

	[Fact]
	public void TheAnalyzerDeclaresTheCatalogsEntriesAndOneMoreAndEveryEntryHasAReasonAndNoInternalTerm()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> declared = [.. ShellRegisteredExpressions.Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage];

		Assert.Equal(_DECLARED_ENTRY_COUNT, declared.Length);
		Assert.Equal(_DECLARED_ENTRY_COUNT, DeclaredEntries.Length);
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
		ImmutableArray<DeclaredUnsupportedCoverage> declared = [.. ShellRegisteredExpressions.Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage];

		Assert.All(DeclaredEntries, entry =>
			Assert.Single(declared, candidate => candidate.Construct.Contains(entry.Keyword, StringComparison.Ordinal)));
		Assert.All(declared, candidate =>
			Assert.Single(DeclaredEntries, entry => candidate.Construct.Contains(entry.Keyword, StringComparison.Ordinal)));
	}

	// ------------------------------------------------------------------------------------------
	// A declared form yields no finding where the catalog says so
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("curl -H @headers.txt \"$URL\"\n")]
	[InlineData("xh -a admin:example \"$URL\"\n")]
	[InlineData("axel --header=\"Authorization: Basic dXNlcjpwYXNz\" \"$URL\"\n")]
	[InlineData("python3 -c 'import urllib.request'\n")]
	[InlineData("n=CURL\nCURL=curl\n\"${!n}\" -sS -u admin:example \"$URL\"\n")]
	[InlineData(". ./credentials.sh\ncurl -H \"$AUTH_HEADER\" \"$URL\"\n")]
	[InlineData("curl -K ./existing.conf \"$URL\"\n")]
	[InlineData("docker login -u admin -p example registry.test\n")]
	public void AFormTheCatalogDeclaresNotSupportedYieldsNoFinding(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{string.Join(", ", constructs.Select(construct => construct.PatternClassIdentifier))}]");
	}

	[Fact]
	public void AMockWithANeutralNameReportsAtItsFullLevelAsTheDeclaredCostSays()
	{
		const string source = "curl -sS -u admin:example \"$HOST/api\"\n";

		RecognizedConstruct construct = Assert.Single(new ShellRecognitionHarness().Recognize(source, "scripts/helper.sh"));

		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	[Theory]
	[InlineData("#!/bin/dash\n")]
	[InlineData("#!/bin/ash\n")]
	[InlineData("#!/bin/mksh\n")]
	[InlineData("#!/usr/bin/env fish\n")]
	[InlineData("#!/bin/csh\n")]
	[InlineData("#!/bin/tcsh\n")]
	public void AFirstLineThatNamesAShellTheCatalogDoesNotAttributeIsNotAssessed(string firstLine)
	{
		Assert.DoesNotMatch(ShellRegisteredExpressions.Analyzer.LanguageIdentification.FirstLineDetectionExpression!, firstLine);
	}

	[Theory]
	[InlineData(".bats")]
	[InlineData(".fish")]
	[InlineData(".csh")]
	[InlineData(".bat")]
	[InlineData(".cmd")]
	[InlineData(".bashrc")]
	[InlineData(".profile")]
	[InlineData(".zshrc")]
	[InlineData(".envrc")]
	public void AnExtensionTheCatalogDoesNotAttributeIsNotAShellExtension(string extension)
	{
		ISet<string> extensions = ShellRegisteredExpressions.Analyzer.LanguageIdentification.FileExtensions;

		Assert.DoesNotContain(extension, extensions);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<string> InternalTermsIn(string text)
	{
		return
		[
			.. PublicSourceIdentifierTests.InternalCoverageTerms().Where(term => text.Contains(term, StringComparison.Ordinal)),
			.. PublicSourceIdentifierTests.InternalIdentifierPatterns().SelectMany(pattern => pattern.Matches(text)).Select(match => match.Value)
		];
	}
}
