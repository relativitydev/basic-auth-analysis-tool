using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;

using MigrateAuth.Cli.Tests.Progress;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THROUGH THE REAL PIPELINE: THE <c>Deployment</c> FOLDER DEMOTES FOR <c>CSharp</c> AND FOR NO OTHER
/// LANGUAGE UNLESS THAT LANGUAGE'S OWN CATALOG SAYS SO.
///
/// The <c>CSharp</c> half runs over the embedded catalog. The other half runs over a test-local analyzer that
/// is a different language (<c>PowerShell</c>) declaring no folder signal.
/// </summary>
public sealed class DeploymentFolderPerLanguagePipelineTests
{
	private const string _CSHARP_DEPLOYMENT_FILE = "Deployment/Library.cs";
	private const string _CSHARP_OWN_CODE_FILE = "src/Library.cs";
	private const string _OTHER_LANGUAGE_FILE_EXTENSION = ".fx1";
	private const string _OTHER_LANGUAGE_DEPLOYMENT_FILE = "Deployment/Script.fx1";
	private const string _OTHER_LANGUAGE_OWN_CODE_FILE = "src/Script.fx1";

	private const string _CSHARP_CONTENT =
		"class HighFindingFixture\n{\n\tvoid Build()\n\t{\n\t\tvar credentials = new UsernamePasswordCredentials(options.UserName, options.Password);\n\t}\n}\n";

	private static readonly string _otherLanguageContent =
		$"class A\n{{\n\tvoid B() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, credential);\n}}\n";

	[Fact]
	public void ACSharpHighConstructUnderDeploymentReportsLowDemotedFile()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.WriteSourceFile(_CSHARP_DEPLOYMENT_FILE, _CSHARP_CONTENT);
		harness.WriteSourceFile(_CSHARP_OWN_CODE_FILE, _CSHARP_CONTENT);
		harness.RunTheWholeWorkflow();

		Finding demoted = FindingIn(harness.State.Findings, _CSHARP_DEPLOYMENT_FILE);

		Assert.Equal(ConfidenceLevel.Low, demoted.Level);
		Assert.Equal(LowConfidenceReasons.DemotedFile, demoted.LowReasons);
		Assert.Equal(ConfidenceLevel.High, FindingIn(harness.State.Findings, _CSHARP_OWN_CODE_FILE).Level);
	}

	[Fact]
	public void ANonCSharpHighConstructUnderDeploymentStaysHigh()
	{
		LanguageCatalog catalog = new(LanguageCatalog.CurrentVersion, [new OtherLanguageWithNoFolderSignal()]);
		using PipelineHarness harness = PipelineHarness.CreateOverCatalog(catalog, new SteppingTimeProvider());

		harness.WriteSourceFile(_OTHER_LANGUAGE_DEPLOYMENT_FILE, _otherLanguageContent);
		harness.WriteSourceFile(_OTHER_LANGUAGE_OWN_CODE_FILE, _otherLanguageContent);
		harness.RunTheWholeWorkflow();

		Finding underDeployment = FindingIn(harness.State.Findings, _OTHER_LANGUAGE_DEPLOYMENT_FILE);

		Assert.Equal(ConfidenceLevel.High, underDeployment.Level);
		Assert.Equal(LowConfidenceReasons.None, underDeployment.LowReasons);
		Assert.Equal(ConfidenceLevel.High, FindingIn(harness.State.Findings, _OTHER_LANGUAGE_OWN_CODE_FILE).Level);
	}

	private static Finding FindingIn(ImmutableArray<Finding> findings, string relativePath)
	{
		return findings.Single(finding => finding.Path.Value == relativePath);
	}

	/// <summary>
	/// The pipeline fixture analyzer re-identified as another language, with the demotion signals of an
	/// analyzer that declares none. Every other member is the fixture analyzer's own.
	/// </summary>
	private sealed class OtherLanguageWithNoFolderSignal : ILanguageAnalyzer
	{
		private readonly PipelineLanguageAnalyzer _inner = new();

		public LexicalProfile LexicalProfile => _inner.LexicalProfile;

		public LanguageIdentification LanguageIdentification { get; } = new()
		{
			FileExtensions = new[] { _OTHER_LANGUAGE_FILE_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { AssessedLanguages.CanonicalName(AssessedLanguage.PowerShell) }
				.ToFrozenSet(StringComparer.Ordinal),
			LanguageNameDerivation = (_, _) => AssessedLanguages.CanonicalName(AssessedLanguage.PowerShell)
		};

		public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass => _inner.DetectionRulesByPatternClass;

		public LanguageShapedDemotionSignals DemotionSignals => LanguageShapedDemotionSignals.NoneDeclared;

		public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation => _inner.CandidateSelectionAndRecommendation;

		public FrozenSet<SubsumptionRule> SubsumptionRelation => _inner.SubsumptionRelation;
	}
}
