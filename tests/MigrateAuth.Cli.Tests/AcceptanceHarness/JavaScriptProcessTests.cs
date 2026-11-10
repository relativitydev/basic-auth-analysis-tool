using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// A `JavaScript` repository through a REAL PROCESS, from scan to report: a `.js`, a `.mjs`, a `.cjs` and a `.jsx` file and an extensionless file whose first line names `node` are
/// assessed, found and attributed to the language by name, a `.ts` file beside them is assessed as `TypeScript` and attributed to it by name, and a literal credential in them is never printed. The scripts
/// are invented placeholders.
/// </summary>
public sealed class JavaScriptProcessTests
{
	private const string _LANGUAGE_NAME = "JavaScript";
	private const string _TYPESCRIPT_LANGUAGE_NAME = "TypeScript";
	private const string _SCRIPT_PATH = "src/upload.js";
	private const string _MODULE_PATH = "src/upload.mjs";
	private const string _COMMONJS_PATH = "src/upload.cjs";
	private const string _JSX_PATH = "src/upload.jsx";
	private const string _NODE_SHEBANG_PATH = "tools/run-upload";
	private const string _TYPESCRIPT_PATH = "src/upload.ts";
	private const string _PATTERN_CLASS_PREFIX = "JS-";
	private const int _SHORTEST_SUBSTRING_LENGTH = 4;
	private const int _MINIMUM_DECLARED_ENTRIES_NAMED = 2;
	private const string _DECLARED_LANGUAGE_LINE = "- JavaScript:";
	private const string _DECLARED_ENTRY_LINE_FORMAT = "  - {0}: {1}";

	/// <summary>
	/// A PLACEHOLDER, not a real credential: an invented value that stands where a password would. The random-looking shape is kept on purpose, because the test proves that no substring of
	/// the value, down to four characters, is printed; a readable value would share substrings with the product's own wording and make that proof impossible. The shape is not one
	/// of the placeholder words the analyzer filters, so the literal-credential signal still fires on it.
	/// </summary>
	private const string _PLACEHOLDER_REDACTED_VALUE = "Kf3Qz8WmXv";

	private const string _SCRIPT_CONTENT = $"const response = await got('https://h.test/api/items', {{ username: 'admin', password: '{_PLACEHOLDER_REDACTED_VALUE}' }});\n";
	private const string _MODULE_CONTENT = $"const response = await axios.get(url, {{ auth: {{ username: 'admin', password: '{_PLACEHOLDER_REDACTED_VALUE}' }} }});\n";
	private const string _COMMONJS_CONTENT = $"await superagent.get(url).auth('admin', '{_PLACEHOLDER_REDACTED_VALUE}');\n";
	private const string _JSX_CONTENT = $"const view = <p>It's text</p>;\nawait fetch(url, {{ headers: {{ Authorization: 'Basic {_PLACEHOLDER_REDACTED_VALUE}' }} }});\n";
	private const string _NODE_SHEBANG_CONTENT = $"#!/usr/bin/env node\nconst response = await got(url, {{ username: 'admin', password: '{_PLACEHOLDER_REDACTED_VALUE}' }});\n";
	private const string _TYPESCRIPT_CONTENT = $"const response: Response = await got(url, {{ username: 'admin', password: '{_PLACEHOLDER_REDACTED_VALUE}' }});\n";

	[Fact]
	public void TheFourJavaScriptExtensionsAndAnExtensionlessNodeScriptAreAssessedFoundAndAttributedToJavaScriptAndATypeScriptFileIsAssessedAndAttributedToTypeScriptInARealProcess()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_SCRIPT_PATH, _SCRIPT_CONTENT);
		repository.WriteText(_MODULE_PATH, _MODULE_CONTENT);
		repository.WriteText(_COMMONJS_PATH, _COMMONJS_CONTENT);
		repository.WriteText(_JSX_PATH, _JSX_CONTENT);
		repository.WriteText(_NODE_SHEBANG_PATH, _NODE_SHEBANG_CONTENT);
		repository.WriteText(_TYPESCRIPT_PATH, _TYPESCRIPT_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(repository.RootPath, destination.Path, CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);

		ArtifactDocument artifact = ReadArtifact(destination.Path);

		Assert.Equal(
			new[] { _SCRIPT_PATH, _MODULE_PATH, _COMMONJS_PATH, _JSX_PATH, _NODE_SHEBANG_PATH, _TYPESCRIPT_PATH }.Order(StringComparer.Ordinal),
			artifact.Findings.Select(finding => finding.FilePath).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
		Assert.All(artifact.Findings, finding =>
		{
			string expectedLanguage = finding.FilePath == _TYPESCRIPT_PATH ? _TYPESCRIPT_LANGUAGE_NAME : _LANGUAGE_NAME;

			Assert.Equal(expectedLanguage, finding.Language);
			Assert.StartsWith(_PATTERN_CLASS_PREFIX, finding.PatternClass, StringComparison.Ordinal);
		});

		foreach (string languageName in new[] { _LANGUAGE_NAME, _TYPESCRIPT_LANGUAGE_NAME })
		{
			string[] languageRows = [.. outcome.StandardOutput.Split('\n').Where(line => line.Contains(languageName, StringComparison.Ordinal))];

			Assert.NotEmpty(languageRows);
			Assert.All(languageRows, row => Assert.DoesNotContain(ScanStage.UNASSESSED_ROW_SUFFIX.Trim(), row, StringComparison.Ordinal));
		}
	}

	[Fact]
	public void NoSubstringOfALiteralCredentialIsPrintedInTheReportTheDataFileOrTheConsole()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_SCRIPT_PATH, _SCRIPT_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(repository.RootPath, destination.Path, CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);

		string markdown = ProducedArtifacts.TextOfTheOnly(destination.Path, OutputArtifactKind.MarkdownReport);
		string structuredData = ProducedArtifacts.TextOfTheOnly(destination.Path, OutputArtifactKind.StructuredDataArtifact);

		Assert.NotEmpty(ReadArtifact(destination.Path).Findings);
		Assert.Contains(_SCRIPT_PATH, markdown, StringComparison.Ordinal);

		foreach (string window in SubstringsOfThePlaceholder())
		{
			Assert.DoesNotContain(window, markdown, StringComparison.Ordinal);
			Assert.DoesNotContain(window, structuredData, StringComparison.Ordinal);
			Assert.DoesNotContain(window, outcome.StandardOutput, StringComparison.Ordinal);
			Assert.DoesNotContain(window, outcome.StandardError, StringComparison.Ordinal);
		}
	}

	[Fact]
	public void TheReportAndTheDataFileListTheDeclaredUnsupportedEntriesOfJavaScriptWhenJavaScriptIsEnabled()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_SCRIPT_PATH, _SCRIPT_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(repository.RootPath, destination.Path, CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);

		DeclaredUnsupportedCoverage[] declared = [.. JavaScriptRegisteredExpressions.Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage];
		string markdown = ProducedArtifacts.TextOfTheOnly(destination.Path, OutputArtifactKind.MarkdownReport);
		ArtifactDeclaredUnsupportedConstructDocument[] inTheDataFile = ReadArtifact(destination.Path).Coverage.Languages
			.Single(row => row.Language == _LANGUAGE_NAME).DeclaredUnsupportedConstructs;

		Assert.True(declared.Length >= _MINIMUM_DECLARED_ENTRIES_NAMED);
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
