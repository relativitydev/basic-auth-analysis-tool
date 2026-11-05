using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// THE ACCEPTANCE TEST: A REPOSITORY WITH ALL EIGHT LANGUAGES, THROUGH THE REAL SCAN
/// STAGE, WITH THREE OF THEM SHIPPING AN ANALYZER.
///
/// The embedded catalog this harness builds carries the real `CSharp`, `PowerShell` and `VBNet` analyzers
/// and the invented reference profile - nothing for the other five. This class proves each file of those five in this tree is named by <see cref="BaseLanguageDetection"/>, marked unassessed, excluded
/// from candidate selection, and that the two genuinely unrecognized files - no extension and no
/// shebang, and a `.txt` file - are the only ones still counted as unrecognized.
/// </summary>
public sealed class BaseLanguageInventoryScanTests
{
	private const string _CSHARP_FILE_NAME = "Program.cs";
	private const string _VBNET_FILE_NAME = "Module1.vb";
	private const string _JAVASCRIPT_FILE_NAME = "app.js";
	private const string _TYPESCRIPT_FILE_NAME = "app.ts";
	private const string _PYTHON_FILE_NAME = "script.py";
	private const string _GO_FILE_NAME = "main.go";
	private const string _POWERSHELL_FILE_NAME = "script.ps1";
	private const string _SHELL_FILE_NAME = "script.sh";

	private const string _PYTHON_SHEBANG_FILE_NAME = "run-python";
	private const string _BASH_SHEBANG_FILE_NAME = "run-bash";
	private const string _PWSH_SHEBANG_FILE_NAME = "run-pwsh";
	private const string _EXTENSIONLESS_NO_SHEBANG_FILE_NAME = "README";
	private const string _TEXT_FILE_NAME = "notes.txt";

	/// <summary>Carries a real `CSharp` preselection anchor, so exactly one file becomes a candidate.</summary>
	private const string _CSHARP_CONTENT = "public sealed class Ordinary\n{\n\t// CredentialCache marker\n}\n";

	private const string _ORDINARY_CONTENT = "ordinary content\n";
	private const string _PYTHON_SHEBANG_CONTENT = "#!/usr/bin/env python3\nprint('hi')\n";
	private const string _BASH_SHEBANG_CONTENT = "#!/bin/bash\necho hi\n";
	private const string _PWSH_SHEBANG_CONTENT = "#!/usr/bin/env pwsh\nWrite-Host hi\n";

	private const int _TOTAL_FILE_COUNT = 13;

	/// <summary>
	/// JavaScript, TypeScript and Go carry one file each; Python and Shell carry two - their
	/// own named-extension file plus the shebang file that resolves to the same language. PowerShell
	/// also carries two, and is assessed, as is VBNet with its one. 1 + 1 + 1 + 2 + 2 = 7.
	/// </summary>
	private const int _UNASSESSED_FILE_COUNT = 7;

	private const int _UNRECOGNIZED_FILE_COUNT = 2;
	private const int _SINGLE_FILE_LANGUAGE_COUNT = 1;
	private const int _EXTENSION_AND_SHEBANG_LANGUAGE_COUNT = 2;

	[Fact]
	public void EveryLanguageIsCountedByNameAndOnlyTheGenuinelyUnrecognizedFilesAreUnrecognized()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		PlantMixedLanguageTree(harness);

		InventoryResult inventory = RunBothPasses(harness);

		Assert.Equal(_TOTAL_FILE_COUNT, inventory.Inventory.TotalFileCount);
		AssertRowFileCount(inventory, AssessedLanguages.CanonicalName(AssessedLanguage.CSharp), _SINGLE_FILE_LANGUAGE_COUNT);
		AssertRowFileCount(inventory, AssessedLanguages.CanonicalName(AssessedLanguage.VBNet), _SINGLE_FILE_LANGUAGE_COUNT);
		AssertRowFileCount(inventory, AssessedLanguages.CanonicalName(AssessedLanguage.JavaScript), _SINGLE_FILE_LANGUAGE_COUNT);
		AssertRowFileCount(inventory, AssessedLanguages.CanonicalName(AssessedLanguage.TypeScript), _SINGLE_FILE_LANGUAGE_COUNT);
		AssertRowFileCount(inventory, AssessedLanguages.CanonicalName(AssessedLanguage.Go), _SINGLE_FILE_LANGUAGE_COUNT);

		// script.py plus the extensionless python3 shebang file.
		AssertRowFileCount(inventory, AssessedLanguages.CanonicalName(AssessedLanguage.Python), _EXTENSION_AND_SHEBANG_LANGUAGE_COUNT);

		// script.ps1 plus the extensionless pwsh shebang file.
		AssertRowFileCount(inventory, AssessedLanguages.CanonicalName(AssessedLanguage.PowerShell), _EXTENSION_AND_SHEBANG_LANGUAGE_COUNT);

		// script.sh plus the extensionless bash shebang file.
		AssertRowFileCount(inventory, AssessedLanguages.CanonicalName(AssessedLanguage.Shell), _EXTENSION_AND_SHEBANG_LANGUAGE_COUNT);

		AssertRowFileCount(inventory, LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, _UNRECOGNIZED_FILE_COUNT);
	}

	[Fact]
	public void OnlyTheCSharpFileBecomesACandidate()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		PlantMixedLanguageTree(harness);

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.Equal([_CSHARP_FILE_NAME], harness.State.CandidateRelativePaths);
	}

	[Fact]
	public void EveryUnshippedLanguageIsMarkedNotAssessedAndTheThreeShippedOnesAreNot()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		PlantMixedLanguageTree(harness);

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		string[] lines = SplitLines(harness.Output.ToString());

		AssertLanguageLineIsAssessed(lines, AssessedLanguages.CanonicalName(AssessedLanguage.CSharp), assessed: true);
		AssertLanguageLineIsAssessed(lines, AssessedLanguages.CanonicalName(AssessedLanguage.VBNet), assessed: true);
		AssertLanguageLineIsAssessed(lines, AssessedLanguages.CanonicalName(AssessedLanguage.JavaScript), assessed: false);
		AssertLanguageLineIsAssessed(lines, AssessedLanguages.CanonicalName(AssessedLanguage.TypeScript), assessed: false);
		AssertLanguageLineIsAssessed(lines, AssessedLanguages.CanonicalName(AssessedLanguage.Python), assessed: false);
		AssertLanguageLineIsAssessed(lines, AssessedLanguages.CanonicalName(AssessedLanguage.Go), assessed: false);
		AssertLanguageLineIsAssessed(lines, AssessedLanguages.CanonicalName(AssessedLanguage.PowerShell), assessed: true);
		AssertLanguageLineIsAssessed(lines, AssessedLanguages.CanonicalName(AssessedLanguage.Shell), assessed: false);
	}

	/// <summary>
	/// The unrecognized bucket carries no "not assessed" suffix: it was
	/// never searched, which is a third state distinct from a detected language this release does not
	/// assess - see <see cref="Reporting.LanguageAssessmentState.NoLanguageDetected"/>.
	/// </summary>
	[Fact]
	public void TheUnrecognizedBucketCarriesNoUnassessedSuffix()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		PlantMixedLanguageTree(harness);

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		string[] lines = SplitLines(harness.Output.ToString());

		AssertLanguageLineIsAssessed(lines, LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, assessed: true);
	}

	[Fact]
	public void TheUnassessedLanguagesSummaryLineIsPrinted()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		PlantMixedLanguageTree(harness);

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		string expectedSummary = string.Format(
			CultureInfo.InvariantCulture,
			ScanStage.UNASSESSED_LANGUAGES_SUMMARY_FORMAT,
			_UNASSESSED_FILE_COUNT,
			_TOTAL_FILE_COUNT);

		Assert.Contains(expectedSummary, harness.Output.ToString(), StringComparison.Ordinal);
	}

	private static void PlantMixedLanguageTree(ScanHarness harness)
	{
		harness.Fixture.WriteText(_CSHARP_FILE_NAME, _CSHARP_CONTENT);
		harness.Fixture.WriteText(_VBNET_FILE_NAME, _ORDINARY_CONTENT);
		harness.Fixture.WriteText(_JAVASCRIPT_FILE_NAME, _ORDINARY_CONTENT);
		harness.Fixture.WriteText(_TYPESCRIPT_FILE_NAME, _ORDINARY_CONTENT);
		harness.Fixture.WriteText(_PYTHON_FILE_NAME, _ORDINARY_CONTENT);
		harness.Fixture.WriteText(_GO_FILE_NAME, _ORDINARY_CONTENT);
		harness.Fixture.WriteText(_POWERSHELL_FILE_NAME, _ORDINARY_CONTENT);
		harness.Fixture.WriteText(_SHELL_FILE_NAME, _ORDINARY_CONTENT);
		harness.Fixture.WriteText(_PYTHON_SHEBANG_FILE_NAME, _PYTHON_SHEBANG_CONTENT);
		harness.Fixture.WriteText(_BASH_SHEBANG_FILE_NAME, _BASH_SHEBANG_CONTENT);
		harness.Fixture.WriteText(_PWSH_SHEBANG_FILE_NAME, _PWSH_SHEBANG_CONTENT);
		harness.Fixture.WriteText(_EXTENSIONLESS_NO_SHEBANG_FILE_NAME, _ORDINARY_CONTENT);
		harness.Fixture.WriteText(_TEXT_FILE_NAME, _ORDINARY_CONTENT);
	}

	private static void AssertRowFileCount(InventoryResult inventory, string languageName, int expectedFileCount)
	{
		LanguageInventoryRow row = inventory.Inventory.RowsInReportOrder()
			.Single(candidate => candidate.LanguageName == languageName);

		Assert.Equal(expectedFileCount, row.FileCount);
	}

	private static void AssertLanguageLineIsAssessed(string[] lines, string languageName, bool assessed)
	{
		string line = lines.Single(candidate => candidate.TrimStart().StartsWith(languageName, StringComparison.Ordinal));

		if (assessed)
		{
			Assert.DoesNotContain(ScanStage.UNASSESSED_ROW_SUFFIX, line, StringComparison.Ordinal);
		}
		else
		{
			Assert.Contains(ScanStage.UNASSESSED_ROW_SUFFIX, line, StringComparison.Ordinal);
		}
	}

	private static string[] SplitLines(string transcript)
	{
		return transcript.Split(["\r\n", "\n"], StringSplitOptions.None);
	}

	private static InventoryResult RunBothPasses(ScanHarness harness)
	{
		RepositoryInventoryScan scan = harness.BuildScan();
		EnumerationResult enumeration = scan.Enumerate(harness.BuildProgress(null), CancellationToken.None);

		return scan.ReadInventory(
			enumeration.FilePaths,
			harness.BuildProgress(enumeration.FilePaths.Count),
			CancellationToken.None);
	}
}
