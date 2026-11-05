using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE `CSharp` CREATION FORMS, THE NAMED-CONSTANT SCHEME, THE IDENTIFIER-ROOTED PAIR HALVES AND THE
/// OBJECT-INITIALIZER SINKS THROUGH THE REAL, BUILT PROCESS.
///
/// A repository of invented files, each holding one form the catalog says is read, one holding a form it declares
/// unsupported and one near-miss, is scanned and analyzed by the real entry point in its own process. What is read
/// is what a person outside the process sees: the structured artifact's findings, with the level and the flow each
/// carries, and the report's coverage statement, which names the declared forms in plain words. The in-process
/// fixtures of the same forms are in the detection-form fixtures of the catalog tests.
/// </summary>
public sealed class CSharpCreationFormProcessTests
{
	private const string _HIGH_CONFIDENCE_NAME = "High";
	private const string _MEDIUM_CONFIDENCE_NAME = "Medium";
	private const string _LOW_CONFIDENCE_NAME = "Low";
	private const string _CLIENT_CREDENTIALS_NAME = "ClientCredentials";
	private const string _AUTHORIZATION_CODE_NAME = "AuthorizationCode";
	private const string _INTERACTIVE_SIGNAL_NAME = "InteractiveSignal";
	private const string _DEFAULT_DECISION_NAME = "Default";
	private const string _NAMED_CONSTANT_FILE = "src/NamedConstant.cs";
	private const string _ROOTED_HALVES_FILE = "src/RootedHalves.cs";
	private const string _SINK_INITIALIZER_FILE = "src/SinkInitializer.cs";
	private const string _TARGET_TYPED_ENTRY_POINT_FILE = "src/TargetTypedEntryPoint.cs";
	private const string _QUALIFIED_CACHE_FILE = "src/QualifiedCache.cs";
	private const string _HANDLER_FILE = "src/Handler.cs";
	private const string _ASSEMBLED_CREDENTIAL_FILE = "src/AssembledCredential.cs";
	private const string _QUALIFIED_BINDING_FILE = "src/QualifiedBinding.cs";
	private const string _TARGET_TYPED_HEADER_FILE = "src/TargetTypedHeader.cs";
	private const string _ALIAS_FILE = "src/AliasOnly.cs";
	private const string _NEAR_MISS_FILE = "src/NearMiss.cs";
	private const string _DECLARED_ALIAS_COVERAGE_TEXT = "renames with a \"using\" alias";
	private const string _DECLARED_SHORTHAND_COVERAGE_TEXT = "the shorthand \"new(...)\" where the type name does not stand in front of the variable";

	private const string _FILE_FORMAT = "class Fixture\n{{\n\tvoid Run()\n\t{{\n{0}\n\t}}\n}}\n";

	/// <summary>The files of the fixture repository and what each is expected to produce: the class, the confidence, the flow and why that flow.</summary>
	private static readonly (string Path, string Statements, string PatternClass, string Confidence, string? Flow, string? Decision)[] _expectedFindings =
	[
		(
			_NAMED_CONSTANT_FILE,
			"\t\trequest.Headers.Add(HttpRequestHeader.Authorization, AuthConstants.SCHEME + \" \" + encoded);",
			"CS-012", _MEDIUM_CONFIDENCE_NAME, _CLIENT_CREDENTIALS_NAME, _DEFAULT_DECISION_NAME),
		(
			_ROOTED_HALVES_FILE,
			"\t\tPasswordBox passwordBox = new PasswordBox();\n\t\tstring pair = $\"{userBox.Text}:{passwordBox.Password}\";\n\t\tvar encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(pair));",
			"CS-009", _MEDIUM_CONFIDENCE_NAME, _AUTHORIZATION_CODE_NAME, _INTERACTIVE_SIGNAL_NAME),
		(
			_SINK_INITIALIZER_FILE,
			"\t\tvar mailer = new SmtpClient(host, port) { Credentials = new NetworkCredential(userName, password) };",
			"CS-019", _LOW_CONFIDENCE_NAME, null, null),
		(
			_TARGET_TYPED_ENTRY_POINT_FILE,
			"\t\tImportAPI importApi = new(userName, password, webServiceUrl);",
			"CS-015", _HIGH_CONFIDENCE_NAME, _CLIENT_CREDENTIALS_NAME, _DEFAULT_DECISION_NAME),
		(
			_QUALIFIED_CACHE_FILE,
			"\t\tcache.Add(new System.Uri(instanceUrl), \"Basic\", new System.Net.NetworkCredential(userName, password));",
			"CS-003", _HIGH_CONFIDENCE_NAME, _CLIENT_CREDENTIALS_NAME, _DEFAULT_DECISION_NAME),
		(
			_HANDLER_FILE,
			"\t\tHttpClientHandler handler = new() { Credentials = new NetworkCredential(userName, password) };",
			"CS-007", _MEDIUM_CONFIDENCE_NAME, _CLIENT_CREDENTIALS_NAME, _DEFAULT_DECISION_NAME),
		(
			_ASSEMBLED_CREDENTIAL_FILE,
			"\t\tvar credential = new System.Net.NetworkCredential() { UserName = userName };\n\t\tcredential.Password = password;",
			"CS-016", _MEDIUM_CONFIDENCE_NAME, _CLIENT_CREDENTIALS_NAME, _DEFAULT_DECISION_NAME),
		(
			_QUALIFIED_BINDING_FILE,
			"\t\tbinding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Basic;",
			"CS-013", _MEDIUM_CONFIDENCE_NAME, _CLIENT_CREDENTIALS_NAME, _DEFAULT_DECISION_NAME),
		(
			_TARGET_TYPED_HEADER_FILE,
			"\t\tAuthenticationHeaderValue header = new(\"Basic\", encoded);\n\t\tclient.DefaultRequestHeaders.Authorization = header;",
			"CS-005", _MEDIUM_CONFIDENCE_NAME, _CLIENT_CREDENTIALS_NAME, _DEFAULT_DECISION_NAME)
	];

	/// <summary>Files that hold a declared form or a near-miss, so they are scanned and produce no finding at all.</summary>
	private static readonly (string Path, string Statements)[] _expectedSilent =
	[
		(_ALIAS_FILE, "\t\tvar credential = new Net(userName, password);"),
		(_NEAR_MISS_FILE, "\t\tvar mailer = new SmtpClient(host, port) { Credentials = CredentialCache.DefaultCredentials };")
	];

	[Fact]
	public void EveryCoveredFormIsReportedAtTheCatalogsLevelAndFlowByTheBuiltProcess()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		WriteTheFixtureRepository(repository);

		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);

		ArtifactDocument artifact = ReadArtifact(destination.Path);

		Assert.Equal(_expectedFindings.Length, artifact.Findings.Length);
		Assert.Equal(LanguageCatalog.CurrentVersion.DisplayValue, artifact.Provenance.CatalogVersion);

		foreach ((string path, _, string patternClass, string confidence, string? flow, string? decision) in _expectedFindings)
		{
			ArtifactFindingDocument finding = artifact.Findings.Single(candidate => candidate.FilePath == path);

			Assert.Equal(patternClass, finding.PatternClass);
			Assert.Equal(confidence, finding.Confidence);
			Assert.Equal(flow, finding.Flow);
			Assert.Equal(decision, finding.FlowDecision);
		}
	}

	[Fact]
	public void ADeclaredFormAndANearMissProduceNoFindingInTheBuiltProcess()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		WriteTheFixtureRepository(repository);

		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			CliProcessHarness.TheFourStagesThenExit);

		ArtifactDocument artifact = ReadArtifact(destination.Path);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.All(_expectedSilent, silent => Assert.DoesNotContain(artifact.Findings, finding => finding.FilePath == silent.Path));
	}

	[Fact]
	public void TheReportStatesTheDeclaredFormsInPlainWordsInTheBuiltProcess()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		WriteTheFixtureRepository(repository);
		CliProcessHarness.Run(repository.RootPath, destination.Path, CliProcessHarness.TheFourStagesThenExit);

		string report = System.Text.Encoding.UTF8.GetString(ProducedArtifacts.BytesOfTheOnly(destination.Path, OutputArtifactKind.MarkdownReport));

		Assert.Contains(_DECLARED_ALIAS_COVERAGE_TEXT, report, StringComparison.Ordinal);
		Assert.Contains(_DECLARED_SHORTHAND_COVERAGE_TEXT, report, StringComparison.Ordinal);
	}

	private static void WriteTheFixtureRepository(RepositoryFixture repository)
	{
		foreach ((string path, string statements, _, _, _, _) in _expectedFindings)
		{
			repository.WriteText(path, string.Format(System.Globalization.CultureInfo.InvariantCulture, _FILE_FORMAT, statements));
		}

		foreach ((string path, string statements) in _expectedSilent)
		{
			repository.WriteText(path, string.Format(System.Globalization.CultureInfo.InvariantCulture, _FILE_FORMAT, statements));
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
