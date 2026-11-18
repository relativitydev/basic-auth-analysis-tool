using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Release;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE DECLARED UNSUPPORTED COVERAGE OF THE SHIPPED `Python` ANALYZER AGAINST THE CATALOG THAT STATES IT.
///
/// The statement is written for the client reading the report, so no entry carries an internal term, and each entry is bound to the form of the catalog's own list of what is not covered by a word that only that entry's
/// construct carries. A form the catalog says is not supported must yield no finding where it is the whole of the text, and the tests below write such a form. A form the catalog says is read at a level that over-reads
/// (a tuple given to a non-HTTP client, a construct that is never sent) is not declared as missed and is tested as read.
/// </summary>
public sealed class PythonDeclaredCoverageTests
{
	/// <summary>The words that only one entry's construct carries, one row per entry of the declared set, in the order of the analyzer's own groups.</summary>
	private static readonly ImmutableArray<string> _entryKeywords =
	[
		"type stub files",
		"Python code held in a file of another type",
		"python2 with a minor version",
		"syntax that exists only in Python 2",
		"format specification in an f-string",
		"stopped reading at such a form",
		"comment written with no space",
		"another language written inside a string",
		"held in a variable, module or argument",
		"merging, copying or a hook",
		"built by a project function",
		"assembled through a t-string",
		"implicit use of a .netrc",
		"HTTP clients the tool does not document",
		"socket, a TLS stream",
		"reflection or text",
		"non-HTTP protocols of the standard library",
		"whether an auth pair",
		"confirmation that a call goes to RelativityOne",
		"neutral name and no marker",
		"product file named like a test",
		"sign-in evidence the tool does not list",
		"sign-in evidence written in a form",
		"opens a browser to a help page",
		"takes too long",
		"findings cap",
		"triple-quoted string"
	];

	[Fact]
	public void TheAnalyzerDeclaresTheCatalogsEntriesAndEveryEntryHasAReasonAndNoInternalTerm()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> declared = Declared();

		Assert.Equal(_entryKeywords.Length, declared.Length);
		Assert.Equal(declared.Length, declared.Select(entry => entry.Construct).Distinct(StringComparer.Ordinal).Count());
		Assert.All(declared, entry =>
		{
			Assert.False(string.IsNullOrWhiteSpace(entry.Construct));
			Assert.False(string.IsNullOrWhiteSpace(entry.Reason));
			Assert.Empty(InternalTermsIn(entry.Construct));
			Assert.Empty(InternalTermsIn(entry.Reason));
		});
	}

	[Fact]
	public void EveryEntryIsBoundToExactlyOneKeywordAndEveryKeywordToExactlyOneEntry()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> declared = Declared();

		Assert.All(_entryKeywords, keyword => Assert.Single(declared, candidate => candidate.Construct.Contains(keyword, StringComparison.Ordinal)));
		Assert.All(declared, candidate => Assert.Single(_entryKeywords, keyword => candidate.Construct.Contains(keyword, StringComparison.Ordinal)));
	}

	[Fact]
	public void TheStatementNamesNoClassIdentifierNoSyntheticSetAndNoPlantIdentifier()
	{
		string text = string.Join(' ', Declared().Select(entry => $"{entry.Construct} {entry.Reason}"));

		Assert.DoesNotMatch(@"\bPY-\d{3}\b", text);
		Assert.DoesNotMatch(@"\b[OEF]5-[A-Z]?\d", text);
		Assert.DoesNotContain("S-PY", text, StringComparison.Ordinal);
		Assert.DoesNotContain("R-PY", text, StringComparison.Ordinal);
	}

	// ------------------------------------------------------------------------------------------
	// A declared form yields no finding where the catalog says so
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("response = requests.get(url, auth=creds)\n")]
	[InlineData("response = requests.get(url, headers=default_headers)\n")]
	[InlineData("response = requests.get(*args, **kwargs)\n")]
	[InlineData("response = requests.get(url, auth=[\"user\", \"secret\"])\n")]
	[InlineData("response = requests.get(url, auth=(\"user\",))\n")]
	[InlineData("response = requests.get(url, auth=(\"user\", \"secret\", \"extra\"))\n")]
	[InlineData("from requests.auth import HTTPBasicAuth as B\nauth = B(\"user\", \"secret\")\n")]
	[InlineData("class CustomAuth(HTTPBasicAuth):\n\tpass\n")]
	[InlineData("login, account, secret_value = netrc.netrc().authenticators(host)\n")]
	[InlineData("result = getattr(client, name)(url)\nexec(code_text)\n")]
	[InlineData("headers = {**base_headers, \"Accept\": \"text/plain\"}\n")]
	[InlineData("headers = base_headers | extra_headers\n")]
	[InlineData("headers.setdefault(\"Accept\", \"text/plain\")\n")]
	[InlineData("def build_headers():\n\treturn make_basic_header(user_name, secret_value)\n")]
	[InlineData("server = smtplib.SMTP(\"mail.example.test\")\nserver.login(\"user\", \"secret\")\n")]
	[InlineData("auth = HTTPDigestAuth(\"user\", \"secret\")\n")]
	[InlineData("sock.sendall(request_text)\n")]
	public void AFormTheCatalogDeclaresNotSupportedYieldsNoFinding(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{string.Join(", ", constructs.Select(construct => construct.PatternClassIdentifier))}]");
	}

	[Fact]
	public void ATestMockSampleOrGeneratedFileWithANeutralNameReportsAtItsFullLevelAsTheDeclaredCostSays()
	{
		const string source = "response = requests.get(url, auth=HTTPBasicAuth(\"user\", \"secret\"))\n";

		RecognizedConstruct construct = Assert.Single(new PythonRecognitionHarness().Recognize(source, "src/helper.py"));

		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	[Fact]
	public void AProductFileNamedLikeATestReadsMediumAndASourceFolderNamedLikeAnEnvironmentReadsLowAsTheDeclaredCostSays()
	{
		const string source = "response = requests.get(url, auth=HTTPBasicAuth(\"user\", \"secret\"))\n";
		PythonRecognitionHarness harness = new();

		Assert.Equal(ConfidenceLevel.Medium, Assert.Single(harness.Recognize(source, "src/test_link.py")).Level);
		Assert.Equal(ConfidenceLevel.Low, Assert.Single(harness.Recognize(source, "venv/client.py")).Level);
		Assert.Equal(ConfidenceLevel.High, Assert.Single(harness.Recognize(source, "env/client.py")).Level);
	}

	[Theory]
	[InlineData("#!/usr/bin/python2.7\n")]
	[InlineData("#!/usr/bin/pypy\n")]
	[InlineData("#!/usr/bin/env VAR=1 python3\n")]
	[InlineData("#!/usr/bin/python -S\n")]
	[InlineData("#!/usr/bin/env python -S\n")]
	[InlineData("#!/bin/sh\n")]
	public void AFirstLineTheCatalogDoesNotAttributeIsNotAssessed(string firstLine)
	{
		Assert.DoesNotMatch(PythonShippedAnalyzer.Instance.LanguageIdentification.FirstLineDetectionExpression!, firstLine.TrimEnd('\n'));
	}

	[Theory]
	[InlineData(".pyi")]
	[InlineData(".ipynb")]
	[InlineData(".pyx")]
	[InlineData(".md")]
	[InlineData(".json")]
	public void AnExtensionTheCatalogDoesNotAttributeIsNotAPythonExtension(string extension)
	{
		ISet<string> extensions = PythonShippedAnalyzer.Instance.LanguageIdentification.FileExtensions;

		Assert.DoesNotContain(extension, extensions);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<DeclaredUnsupportedCoverage> Declared()
	{
		return [.. PythonShippedAnalyzer.Instance.LanguageIdentification.DeclaredUnsupportedCoverage];
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
