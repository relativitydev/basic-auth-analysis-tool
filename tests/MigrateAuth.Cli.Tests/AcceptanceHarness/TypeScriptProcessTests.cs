using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// A `TypeScript` repository through a REAL PROCESS, from scan to report: a `.ts`, a `.tsx`, a `.mts` and a `.cts` file are assessed, found and attributed to the language by name, the entries the
/// analyzer declares as not supported are listed in the report and in the data file, and a literal credential in them is never printed. The scripts are invented placeholders.
/// </summary>
public sealed class TypeScriptProcessTests
{
	private const string _LANGUAGE_NAME = "TypeScript";
	private const string _SCRIPT_PATH = "src/upload.ts";
	private const string _JSX_PATH = "src/upload.tsx";
	private const string _MODULE_PATH = "src/upload.mts";
	private const string _COMMONJS_PATH = "src/upload.cts";
	private const string _DECLARATION_PATH = "src/credentials.d.ts";
	private const string _PATTERN_CLASS_PREFIX = "JS-";
	private const string _TYPED_CLASS = "TS-001";
	private const string _DECLARED_LANGUAGE_LINE = "- TypeScript:";
	private const string _DECLARED_ENTRY_LINE_FORMAT = "  - {0}: {1}";
	private const int _SHORTEST_SUBSTRING_LENGTH = 8;

	/// <summary>A PLACEHOLDER, not a real credential: an invented value that stands where a password would. The obvious placeholder every fixture uses; the test proves that no substring of it, down to the shortest length below, is printed.</summary>
	private const string _PLACEHOLDER_REDACTED_VALUE = "exampleSecret";

	private const string _SCRIPT_CONTENT = $"const response: Reply = await got<Reply>(url, {{ username: 'admin', password: '{_PLACEHOLDER_REDACTED_VALUE}' }});\n";
	private const string _JSX_CONTENT = $"const view = <p>It's text</p>;\nawait fetch(url, {{ headers: {{ Authorization: 'Basic {_PLACEHOLDER_REDACTED_VALUE}' }} }});\n";
	private const string _MODULE_CONTENT = $"const response = await axios.get<Reply>(url, {{ auth: {{ username: 'admin', password: '{_PLACEHOLDER_REDACTED_VALUE}' }} }});\n";
	private const string _COMMONJS_CONTENT = $"const config: AxiosRequestConfig = {{ auth: {{ username: 'admin', password: '{_PLACEHOLDER_REDACTED_VALUE}' }} }};\n";
	private const string _DECLARATION_CONTENT = $"const response = await got(url, {{ username: 'admin', password: '{_PLACEHOLDER_REDACTED_VALUE}' }});\n";

	[Fact]
	public void TheFourTypeScriptExtensionsAreAssessedFoundAndAttributedToTypeScriptInARealProcess()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_SCRIPT_PATH, _SCRIPT_CONTENT);
		repository.WriteText(_JSX_PATH, _JSX_CONTENT);
		repository.WriteText(_MODULE_PATH, _MODULE_CONTENT);
		repository.WriteText(_COMMONJS_PATH, _COMMONJS_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(repository.RootPath, destination.Path, CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);

		ArtifactDocument artifact = ReadArtifact(destination.Path);

		Assert.Equal(
			new[] { _SCRIPT_PATH, _JSX_PATH, _MODULE_PATH, _COMMONJS_PATH }.Order(StringComparer.Ordinal),
			artifact.Findings.Select(finding => finding.FilePath).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
		Assert.All(artifact.Findings, finding =>
		{
			Assert.Equal(_LANGUAGE_NAME, finding.Language);
			Assert.True(finding.PatternClass.StartsWith(_PATTERN_CLASS_PREFIX, StringComparison.Ordinal) || finding.PatternClass == _TYPED_CLASS, finding.PatternClass);
		});

		string[] typeScriptRows = [.. outcome.StandardOutput.Split('\n').Where(line => line.Contains(_LANGUAGE_NAME, StringComparison.Ordinal))];

		Assert.NotEmpty(typeScriptRows);
		Assert.All(typeScriptRows, row => Assert.DoesNotContain(ScanStage.UNASSESSED_ROW_SUFFIX.Trim(), row, StringComparison.Ordinal));
	}

	[Fact]
	public void AFindingInADeclarationFileIsReportedAtLowAndAttributedToTypeScript()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_DECLARATION_PATH, _DECLARATION_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(repository.RootPath, destination.Path, CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);

		ArtifactFindingDocument finding = Assert.Single(ReadArtifact(destination.Path).Findings);

		Assert.Equal(_LANGUAGE_NAME, finding.Language);
		Assert.Equal(_DECLARATION_PATH, finding.FilePath);
		Assert.Equal("Low", finding.Confidence, ignoreCase: true);
	}

	[Fact]
	public void NoSubstringOfALiteralCredentialIsPrintedInTheReportTheDataFileOrTheConsole()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_COMMONJS_PATH, _COMMONJS_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(repository.RootPath, destination.Path, CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);

		string markdown = ProducedArtifacts.TextOfTheOnly(destination.Path, OutputArtifactKind.MarkdownReport);
		string structuredData = ProducedArtifacts.TextOfTheOnly(destination.Path, OutputArtifactKind.StructuredDataArtifact);

		Assert.NotEmpty(ReadArtifact(destination.Path).Findings);
		Assert.Contains(_COMMONJS_PATH, markdown, StringComparison.Ordinal);

		foreach (string window in SubstringsOfThePlaceholder())
		{
			Assert.DoesNotContain(window, markdown, StringComparison.Ordinal);
			Assert.DoesNotContain(window, structuredData, StringComparison.Ordinal);
			Assert.DoesNotContain(window, outcome.StandardOutput, StringComparison.Ordinal);
			Assert.DoesNotContain(window, outcome.StandardError, StringComparison.Ordinal);
		}
	}

	[Fact]
	public void TheReportAndTheDataFileListTheDeclaredUnsupportedEntriesOfTypeScriptWhenTypeScriptIsEnabled()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_SCRIPT_PATH, _SCRIPT_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(repository.RootPath, destination.Path, CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);

		DeclaredUnsupportedCoverage[] declared = [.. TypeScriptRegisteredExpressions.Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage];
		string markdown = ProducedArtifacts.TextOfTheOnly(destination.Path, OutputArtifactKind.MarkdownReport);
		ArtifactDeclaredUnsupportedConstructDocument[] inTheDataFile = ReadArtifact(destination.Path).Coverage.Languages
			.Single(row => row.Language == _LANGUAGE_NAME).DeclaredUnsupportedConstructs;

		Assert.NotEmpty(declared);
		Assert.Contains(_DECLARED_LANGUAGE_LINE, markdown, StringComparison.Ordinal);
		Assert.Equal(declared.Length, inTheDataFile.Length);

		foreach (DeclaredUnsupportedCoverage entry in declared)
		{
			Assert.Contains(string.Format(System.Globalization.CultureInfo.InvariantCulture, _DECLARED_ENTRY_LINE_FORMAT, entry.Construct, entry.Reason), markdown, StringComparison.Ordinal);
			Assert.Contains(inTheDataFile, document => document.Construct == entry.Construct && document.Reason == entry.Reason);
		}
	}

	private static IEnumerable<string> SubstringsOfThePlaceholder()
	{
		for (int length = _SHORTEST_SUBSTRING_LENGTH; length <= _PLACEHOLDER_REDACTED_VALUE.Length; length++)
		{
			for (int start = 0; start + length <= _PLACEHOLDER_REDACTED_VALUE.Length; start++)
			{
				yield return _PLACEHOLDER_REDACTED_VALUE.Substring(start, length);
			}
		}
	}

	private static ArtifactDocument ReadArtifact(string destinationFolderPath)
	{
		ArtifactDocument? document = JsonSerializer.Deserialize(
			ProducedArtifacts.TextOfTheOnly(destinationFolderPath, OutputArtifactKind.StructuredDataArtifact),
			ReportJsonContext.Default.ArtifactDocument);

		Assert.NotNull(document);

		return document;
	}
}
