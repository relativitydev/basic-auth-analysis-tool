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
/// A `Shell` repository through a REAL PROCESS, from scan to report: a `.sh` file and an extensionless file whose first line names a shell are assessed, found,
/// attributed to the language by name and written to the artifact, and a literal credential in them is never printed. The scripts are invented placeholders.
/// </summary>
public sealed class ShellProcessTests
{
	private const string _LANGUAGE_NAME = "Shell";
	private const string _SCRIPT_PATH = "src/upload.sh";
	private const string _SHEBANG_PATH = "tools/run-upload";
	private const string _ORDINARY_PATH = "src/notes.sh";
	private const string _PATTERN_CLASS_PREFIX = "SH-";
	private const int _SHORTEST_SUBSTRING_LENGTH = 4;
	private const int _MINIMUM_DECLARED_ENTRIES_NAMED = 2;
	private const string _DECLARED_LANGUAGE_LINE = "- Shell:";
	private const string _DECLARED_ENTRY_LINE_FORMAT = "  - {0}: {1}";

	/// <summary>An invented password that occurs nowhere else in the fixture, in the product's wording or in a path.</summary>
	private const string _PLACEHOLDER_CREDENTIAL = "Kf3Qz8WmXv";

	private const string _SCRIPT_CONTENT = $"#!/bin/sh\ncurl -u admin:{_PLACEHOLDER_CREDENTIAL} https://h.test/api/items\n";
	private const string _SHEBANG_CONTENT = $"#!/bin/bash\ncurl -u admin:{_PLACEHOLDER_CREDENTIAL} https://h.test/api/other\n";
	private const string _ORDINARY_CONTENT = "echo 'nothing about authentication here'\n";

	[Fact]
	public void AShFileAndAnExtensionlessBashShebangFileAreAssessedFoundAndAttributedToShellInARealProcess()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_SCRIPT_PATH, _SCRIPT_CONTENT);
		repository.WriteText(_SHEBANG_PATH, _SHEBANG_CONTENT);
		repository.WriteText(_ORDINARY_PATH, _ORDINARY_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(repository.RootPath, destination.Path, CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);

		ArtifactDocument artifact = ReadArtifact(destination.Path);

		Assert.Equal(
			new[] { _SHEBANG_PATH, _SCRIPT_PATH }.Order(StringComparer.Ordinal),
			artifact.Findings.Select(finding => finding.FilePath).Order(StringComparer.Ordinal));
		Assert.All(artifact.Findings, finding =>
		{
			Assert.Equal(_LANGUAGE_NAME, finding.Language);
			Assert.StartsWith(_PATTERN_CLASS_PREFIX, finding.PatternClass, StringComparison.Ordinal);
		});

		string[] shellRows = [.. outcome.StandardOutput.Split('\n').Where(line => line.Contains(_LANGUAGE_NAME, StringComparison.Ordinal))];

		Assert.NotEmpty(shellRows);
		Assert.All(shellRows, row => Assert.DoesNotContain(ScanStage.UNASSESSED_ROW_SUFFIX.Trim(), row, StringComparison.Ordinal));
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
	public void TheReportAndTheDataFileListTheDeclaredUnsupportedEntriesOfShellWhenShellIsEnabled()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_SCRIPT_PATH, _SCRIPT_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(repository.RootPath, destination.Path, CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);

		DeclaredUnsupportedCoverage[] declared = [.. ShellRegisteredExpressions.Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage];
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
		for (int length = _SHORTEST_SUBSTRING_LENGTH; length <= _PLACEHOLDER_CREDENTIAL.Length; length++)
		{
			for (int start = 0; start + length <= _PLACEHOLDER_CREDENTIAL.Length; start++)
			{
				yield return _PLACEHOLDER_CREDENTIAL.Substring(start, length);
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
