using System.Collections.Immutable;
using System.Text.Json;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// Two consecutive runs, identical output.
///
/// The process-level run is over the shipped catalog. The embedded catalog holds a real analyzer, so
/// the first claim below runs the real process over files that analyzer recognizes: section 4 of both
/// artifacts holds findings, and the comparison is not degenerate.
///
/// The claim is made twice, at two different strengths.
///
/// The first is over the real process: two independent sessions over one repository, each with its
/// own destination and therefore its own fresh session state, producing byte-identical artifacts.
/// That establishes that the coverage statement, the scan summary's counts, the skipped tallies, the
/// provenance record, the ordering of every language row and the findings themselves are stable
/// between processes.
///
/// The second is over the in-process composition with an analyzer that declares a real language. It
/// runs the whole workflow twice over a repository holding several findings across several files, and
/// compares both the artifacts and the composed finding sets: the total finding order and the
/// numbering are what that checks.
///
/// The one value excluded from the comparison is the file name. The date is in the file name and every
/// clock-derived value is kept out of the content, so the content is compared in full, with nothing
/// masked. Two runs into one destination differ by the collision ordinal; two runs spanning midnight
/// would differ by the date. Neither is a content difference: the second test below runs into one
/// destination so the collision path is the one exercised, and it identifies the second run's file by
/// which name is new rather than by assuming one.
/// </summary>
public sealed class DeterminismTests
{
	private const string _ANCHORED_FILE_NAME = "Credentials.cs";
	private const string _PLAIN_FILE_NAME = "Ordinary.cs";
	private const string _NESTED_FILE_NAME = "module/Nested.cs";
	private const string _EXPECTED_FINDING_PATTERN_CLASS = "CS-001";

	private const string _ANCHORED_CONTENT =
		"class Anchored\n{\n\tvoid Build()\n\t{\n\t\tvar credentials = new UsernamePasswordCredentials(options.UserName, options.Password);\n\t}\n}\n";

	private const string _PLAIN_CONTENT = "class Plain\n{\n\tint total = 1;\n}\n";

	private const string _NESTED_CONTENT =
		"class Nested\n{\n\tvoid Build()\n\t{\n\t\tvar sink = new SmtpClient(host, port);\n\t\tsink.Credentials = new NetworkCredential(settings.UserName, settings.Credentials.Secret);\n\t}\n}\n";

	private const string _INTERACTIVE_EVIDENCE_FILE_NAME = "InteractiveEvidence.cs";
	private const string _FIXED_EVIDENCE_FILE_NAME = "FixedEvidence.cs";
	private const string _NO_EVIDENCE_FILE_NAME = "NoEvidence.cs";
	private const int _EVERY_KIND_OF_EVIDENCE_FILE_COUNT = 3;
	private const string _FINDINGS_FIELD_NAME = "findings";
	private const string _FILE_PATH_FIELD_NAME = "filePath";
	private const string _FLOW_FIELD_NAME = "flow";
	private const string _FLOW_DECISION_FIELD_NAME = "flowDecision";
	private const string _FLOW_NOTE_FIELD_NAME = "flowNote";
	private const string _FLOW_FIELD_SEPARATOR = "|";
	private const string _ROOT_FINDING_FILE_NAME = "Root.cs";
	private const string _NESTED_FINDING_FILE_NAME = "zeta/Alpha.cs";
	private const string _SIBLING_FINDING_FILE_NAME = "alpha/Zeta.cs";

	private static readonly string _threeConstructsInOneFile =
		$"class Root\n{{\n\tvoid A() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, \"secret\");\n\tvoid B() => {PipelineLanguageAnalyzer.LOW_CONSTRUCT}();\n\tvoid C() => {PipelineLanguageAnalyzer.UNTEMPLATED_CONSTRUCT}();\n}}\n";

	private static readonly string _twoConstructsInOneFile =
		$"class Alpha\n{{\n\tvoid A() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, password);\n\tvoid B() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(other, secret);\n}}\n";

	private static readonly string _oneConstructInOneFile =
		$"class Zeta\n{{\n\tvoid A() => {PipelineLanguageAnalyzer.UNTEMPLATED_CONSTRUCT}();\n}}\n";

	/// <summary>
	/// TWO PROCESSES, ONE REPOSITORY, IDENTICAL BYTES. The repository is not empty and not
	/// candidate-free: it carries a preselection anchor, so the scan selects a candidate and the
	/// analysis reads and recognizes it, which puts real counts in Sections 2 and 3 rather than
	/// zeroes. The file names match as well as the contents, because each run writes into its own
	/// destination and the collision rule therefore never engages.
	///
	/// The two anchored files each yield a finding under the real <c>CSharp</c> analyzer, so the
	/// digest comparison covers a populated section 4.
	/// </summary>
	[Fact]
	public void TwoProcessRunsOverOneRepositoryProduceIdenticalArtifacts()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();

		repository.WriteText(_ANCHORED_FILE_NAME, _ANCHORED_CONTENT);
		repository.WriteText(_PLAIN_FILE_NAME, _PLAIN_CONTENT);
		repository.WriteText(_NESTED_FILE_NAME, _NESTED_CONTENT);

		using TemporaryFolder firstDestination = TemporaryFolder.Create();
		using TemporaryFolder secondDestination = TemporaryFolder.Create();

		RunOneSession(repository, firstDestination);
		RunOneSession(repository, secondDestination);

		// The proof must not pass by producing no findings twice.
		Assert.Contains(
			_EXPECTED_FINDING_PATTERN_CLASS,
			ProducedArtifacts.TextOfTheOnly(firstDestination.Path, OutputArtifactKind.StructuredDataArtifact),
			StringComparison.Ordinal);

		string[] firstRunNames = [.. ProducedArtifacts.NamesIn(firstDestination.Path)];
		string[] secondRunNames = [.. ProducedArtifacts.NamesIn(secondDestination.Path)];

		Assert.Equal(firstRunNames, secondRunNames);

		foreach (string artifactName in firstRunNames)
		{
			Assert.Equal(
				ProducedArtifacts.DigestOf(firstDestination.Path, artifactName),
				ProducedArtifacts.DigestOf(secondDestination.Path, artifactName));
		}
	}

	/// <summary>
	/// The run that actually has findings to order. Six findings over three files in two folders,
	/// planted so that the walk's own order is not the reported order - the deepest file sorts first
	/// by path and the file with three constructs has to keep them in line order within itself.
	///
	/// Both artifacts are compared, not just the report: the order and the identifiers reach the data
	/// artifact through a different renderer, and a determinism claim that checked only one of them
	/// would leave the other free to disagree.
	/// </summary>
	[Fact]
	public void TwoConsecutiveRunsWithFindingsProduceIdenticalArtifactsAndIdenticalFindings()
	{
		using PipelineHarness harness = PipelineHarness.Create();

		harness.WriteSourceFile(_ROOT_FINDING_FILE_NAME, _threeConstructsInOneFile);
		harness.WriteSourceFile(_NESTED_FINDING_FILE_NAME, _twoConstructsInOneFile);
		harness.WriteSourceFile(_SIBLING_FINDING_FILE_NAME, _oneConstructInOneFile);

		harness.RunTheWholeWorkflow();

		Finding[] firstRunFindings = [.. harness.State.Findings];
		ImmutableArray<string> afterTheFirstRun = ProducedArtifacts.NamesIn(harness.Destination.CanonicalPath);

		harness.RunTheWholeWorkflow();

		Finding[] secondRunFindings = [.. harness.State.Findings];

		// The proof must not pass by producing nothing twice.
		Assert.NotEmpty(firstRunFindings);
		Assert.Equal(firstRunFindings, secondRunFindings);

		AssertTheSecondRunWroteTheSameBytes(harness, afterTheFirstRun, OutputArtifactKind.MarkdownReport);
		AssertTheSecondRunWroteTheSameBytes(harness, afterTheFirstRun, OutputArtifactKind.StructuredDataArtifact);
	}

	/// <summary>
	/// The identifiers the second run assigned are the one-based sequence over the whole set, which is
	/// the property a stable order exists to protect: an identifier that moved between two runs would
	/// make every reference to a finding in a client's report worthless a week later.
	/// </summary>
	[Fact]
	public void TheIdentifierSequenceIsTheSameOnBothRuns()
	{
		using PipelineHarness harness = PipelineHarness.Create();

		harness.WriteSourceFile(_ROOT_FINDING_FILE_NAME, _threeConstructsInOneFile);
		harness.WriteSourceFile(_NESTED_FINDING_FILE_NAME, _twoConstructsInOneFile);

		harness.RunTheWholeWorkflow();

		string[] firstRunIdentifiers = [.. harness.State.Findings.Select(finding => finding.Identifier)];

		harness.RunTheWholeWorkflow();

		string[] secondRunIdentifiers = [.. harness.State.Findings.Select(finding => finding.Identifier)];
		string[] expectedIdentifiers =
		[
			.. Enumerable
				.Range(FindingIdentifiers.FIRST_INDEX, firstRunIdentifiers.Length)
				.Select(FindingIdentifiers.Of)
		];

		Assert.Equal(expectedIdentifiers, firstRunIdentifiers);
		Assert.Equal(firstRunIdentifiers, secondRunIdentifiers);
	}

	/// <summary>
	/// Over the real process: the flow, the decision and the note of every finding are the
	/// same on two independent sessions. Compared as parsed fields, so a difference is named by the
	/// field it is in rather than by a digest.
	/// </summary>
	[Fact]
	public void TwoProcessRunsAssignTheSameFlowDecisionAndNoteToEveryFinding()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();

		repository.WriteText(_ANCHORED_FILE_NAME, _ANCHORED_CONTENT);
		repository.WriteText(_NESTED_FILE_NAME, _NESTED_CONTENT);

		using TemporaryFolder firstDestination = TemporaryFolder.Create();
		using TemporaryFolder secondDestination = TemporaryFolder.Create();

		RunOneSession(repository, firstDestination);
		RunOneSession(repository, secondDestination);

		string[] firstRunFlowFields = FlowFieldsOf(ProducedArtifacts.TextOfTheOnly(firstDestination.Path, OutputArtifactKind.StructuredDataArtifact));
		string[] secondRunFlowFields = FlowFieldsOf(ProducedArtifacts.TextOfTheOnly(secondDestination.Path, OutputArtifactKind.StructuredDataArtifact));

		Assert.NotEmpty(firstRunFlowFields);
		Assert.Equal(firstRunFlowFields, secondRunFlowFields);
	}

	/// <summary>With all three decisions present, across two independent compositions.</summary>
	[Fact]
	public void TwoIndependentRunsOverEveryKindOfEvidenceAssignTheSameFlowDecisionAndNote()
	{
		string[] firstRunFlowFields = FlowFieldsOfARunWithEveryKindOfEvidence();
		string[] secondRunFlowFields = FlowFieldsOfARunWithEveryKindOfEvidence();

		Assert.Equal(_EVERY_KIND_OF_EVIDENCE_FILE_COUNT, firstRunFlowFields.Length);
		Assert.Equal(firstRunFlowFields, secondRunFlowFields);
		Assert.Contains(firstRunFlowFields, fields => fields.Contains(CredentialFlows.INTERACTIVE_SIGNAL_NAME, StringComparison.Ordinal));
		Assert.Contains(firstRunFlowFields, fields => fields.Contains(CredentialFlows.FIXED_SIGNAL_NAME, StringComparison.Ordinal));
		Assert.Contains(firstRunFlowFields, fields => fields.Contains(CredentialFlows.DEFAULT_DECISION_NAME, StringComparison.Ordinal));
	}

	/// <summary>
	/// <c>filePath</c> is the path as the platform returned it: a file written to disk
	/// under a decomposed name is reported under that decomposed name, byte for byte, in the structured
	/// artifact and in the report, and the composed spelling of the same name is not substituted. The
	/// expected value is asked of the platform rather than typed, so the test states the property on a
	/// file system that itself folds names; where the platform returns the decomposed form - NTFS
	/// does - the test also asserts that the composed form is absent.
	/// </summary>
	[Fact]
	public void TheReportedFilePathIsThePathAsThePlatformReturnedItWithNoUnicodeNormalization()
	{
		const string decomposedFileName = "Café.cs";
		const string composedFileName = "Café.cs";

		using PipelineHarness harness = PipelineHarness.Create();

		harness.WriteSourceFile(decomposedFileName, EvidenceSourceOf(token: null));
		harness.RunTheWholeWorkflow();

		string returnedByThePlatform = Directory.EnumerateFiles(harness.RepositoryRootPath)
			.Select(Path.GetFileName)
			.OfType<string>()
			.Single();
		string structuredFilePath = FilePathsOf(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact)).Single();
		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.Equal(returnedByThePlatform, structuredFilePath);
		Assert.Contains(returnedByThePlatform, report, StringComparison.Ordinal);
		Assert.Equal(returnedByThePlatform, harness.State.Findings.Single().Path.Value);

		if (string.Equals(returnedByThePlatform, decomposedFileName, StringComparison.Ordinal))
		{
			Assert.DoesNotContain(composedFileName, report, StringComparison.Ordinal);
			Assert.NotEqual(composedFileName, structuredFilePath);
		}
	}

	private static string[] FilePathsOf(string structuredDataArtifactText)
	{
		using JsonDocument artifact = JsonDocument.Parse(structuredDataArtifactText);

		return
		[
			.. artifact.RootElement.GetProperty(_FINDINGS_FIELD_NAME).EnumerateArray()
				.Select(finding => finding.GetProperty(_FILE_PATH_FIELD_NAME).GetString() ?? string.Empty)
		];
	}

	private static string[] FlowFieldsOfARunWithEveryKindOfEvidence()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(
			[FlowSignalFixtures.InteractiveCodeOnly, FlowSignalFixtures.FixedCodeOnly]);

		harness.WriteSourceFile(_INTERACTIVE_EVIDENCE_FILE_NAME, EvidenceSourceOf(FlowSignalFixtures.INTERACTIVE_TOKEN));
		harness.WriteSourceFile(_FIXED_EVIDENCE_FILE_NAME, EvidenceSourceOf(FlowSignalFixtures.FIXED_TOKEN));
		harness.WriteSourceFile(_NO_EVIDENCE_FILE_NAME, EvidenceSourceOf(token: null));
		harness.RunTheWholeWorkflow();

		return FlowFieldsOf(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));
	}

	private static string EvidenceSourceOf(string? token)
	{
		string tokenLine = token is null ? string.Empty : $"\tvoid Signal() => {token}();\n";

		return $"class Sample\n{{\n{tokenLine}\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, \"secret\");\n}}\n";
	}

	/// <summary>One string per finding: its path, then its flow, decision and note, each as written.</summary>
	private static string[] FlowFieldsOf(string structuredDataArtifactText)
	{
		using JsonDocument artifact = JsonDocument.Parse(structuredDataArtifactText);

		return
		[
			.. artifact.RootElement.GetProperty(_FINDINGS_FIELD_NAME).EnumerateArray().Select(finding => string.Join(
				_FLOW_FIELD_SEPARATOR,
				finding.GetProperty(_FILE_PATH_FIELD_NAME).GetString(),
				finding.GetProperty(_FLOW_FIELD_NAME).GetString(),
				finding.GetProperty(_FLOW_DECISION_FIELD_NAME).GetString(),
				finding.GetProperty(_FLOW_NOTE_FIELD_NAME).GetString()))
		];
	}

	private static void RunOneSession(RepositoryFixture repository, TemporaryFolder destination)
	{
		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
	}

	/// <summary>
	/// The second run's artifact is identified by which name is NEW, rather than by the collision
	/// ordinal - so the test states what it is comparing instead of encoding the file-name suffix
	/// rule a second time in a place that could come out differently.
	/// </summary>
	private static void AssertTheSecondRunWroteTheSameBytes(
		PipelineHarness harness,
		ImmutableArray<string> afterTheFirstRun,
		OutputArtifactKind kind)
	{
		string destinationPath = harness.Destination.CanonicalPath;
		string firstRunName = ProducedArtifacts.NamesOf(destinationPath, kind)
			.Single(afterTheFirstRun.Contains);
		string secondRunName = ProducedArtifacts.NamesOf(destinationPath, kind)
			.Single(name => !afterTheFirstRun.Contains(name));

		Assert.Equal(
			ProducedArtifacts.DigestOf(destinationPath, firstRunName),
			ProducedArtifacts.DigestOf(destinationPath, secondRunName));
	}
}
