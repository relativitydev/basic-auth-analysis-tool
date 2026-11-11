using System.Collections.Immutable;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Tests.Reporting;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Tests.Writing;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// What the pack tests share: a fixed set of six findings that reaches every section and every instruction kind,
/// the catalog that gives two of them a template and the rest none, and a writer that puts the pack in a folder.
///
/// The set, in the report's order (established destination first, then unknown):
/// 001 High, flow decided, templated: section three.
/// 002 High, flow defaulted, templated: section two.
/// 004 Low: section one, manual review.
/// 005 Medium, Authorization Code flow, no template for it: section one, no template.
/// 003 Medium, flow defaulted, class without a template: section one, no template, with the default-flow mark.
/// 006 Medium, flow decided, templated: section three.
/// </summary>
internal static class PromptPackFixture
{
	internal const string REPORT_FILE_NAME = "basic-auth-migration-2026-09-18.md";

	internal static LanguageCatalog Catalog => ReportHarness.CatalogWithCSharpOnly();

	/// <summary>The six findings, numbered as composition would number them.</summary>
	internal static ImmutableArray<Finding> Findings()
	{
		return
		[
			MakeFinding(1, "src/A.cs", 10, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High, DestinationCertainty.Established, CredentialFlow.ClientCredentials, FlowDecision.FixedSignal),
			MakeFinding(2, "src/B.cs", 20, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High, DestinationCertainty.Established, CredentialFlow.ClientCredentials, FlowDecision.Default),
			MakeFinding(3, "src/C.cs", 30, ReportHarness.UNTEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium, DestinationCertainty.Unknown, CredentialFlow.ClientCredentials, FlowDecision.Default),
			MakeFinding(4, "src/D.cs", 40, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low, DestinationCertainty.Established, null, null),
			MakeFinding(5, "src/E.cs", 50, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium, DestinationCertainty.Established, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal),
			MakeFinding(6, "src/F.cs", 60, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium, DestinationCertainty.Unknown, CredentialFlow.ClientCredentials, FlowDecision.FixedSignal)
		];
	}

	internal static Finding MakeFinding(
		int sequentialIndex,
		string relativePath,
		int line,
		string patternClass,
		ConfidenceLevel level,
		DestinationCertainty certainty,
		CredentialFlow? flow,
		FlowDecision? decision)
	{
		return ReportHarness.FindingAt(sequentialIndex, relativePath, line, patternClass, level, certainty, flow: flow, flowDecision: decision);
	}

	internal static PromptPackScope Scope(ConfidenceLevel minimum = ConfidenceLevel.Low)
	{
		return new PromptPackScope(Findings(), minimum, Catalog);
	}

	/// <summary>The pack body alone, as the writer produces it.</summary>
	internal static string Body(AgentEnvironment environment, PromptPackScope scope, string? reportFileName = REPORT_FILE_NAME)
	{
		using StringWriter writer = new();

		PromptPackWriter.WriteBody(writer, environment, scope, reportFileName);

		return writer.ToString();
	}

	/// <summary>Writes the pack through the atomic writer into a fresh destination and returns its bytes.</summary>
	internal static byte[] WritePackFile(AgentEnvironment environment, PromptPackScope scope, string? reportFileName = REPORT_FILE_NAME)
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repository, destination);
		ArtifactWriteResult result = writer.WritePromptPackStreaming(
			AgentEnvironmentCatalog.Describe(environment).PackFileNameStem,
			sink => PromptPackWriter.WriteBody(sink, environment, scope, reportFileName));

		Assert.True(result.Wrote);

		return File.ReadAllBytes(destination.Combine(result.FileNameWritten));
	}

	internal static RepositoryRelativePath RelativePath(string text)
	{
		return ReportHarness.PathOf(text);
	}
}
