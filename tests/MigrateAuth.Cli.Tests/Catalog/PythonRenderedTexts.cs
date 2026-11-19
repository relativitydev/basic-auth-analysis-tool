using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The recommendation texts the real embedded `Python` analyzer composes, read through the shared engine's <see cref="RecommendationComposer"/> exactly as the report does. This is <see cref="ShellRenderedTexts"/>'s
/// shape retargeted at the `Python` language, shared by the tests that assert on the composed wording of both flows.
/// </summary>
internal static class PythonRenderedTexts
{
	/// <summary>A class identifier the `Python` analyzer does not declare, so its High or Medium finding takes the generic route.</summary>
	public const string UNDECLARED_PATTERN_CLASS = "PY-404";

	private const string _LANGUAGE_NAME = "Python";

	/// <summary>The thirteen classes that give advice; the one Low class carries no template.</summary>
	public static readonly ImmutableArray<string> TemplatedPatternClasses =
	[
		"PY-001", "PY-002", "PY-003", "PY-004", "PY-005", "PY-006", "PY-007", "PY-008", "PY-009", "PY-010", "PY-011", "PY-012", "PY-013"
	];

	/// <summary>The one Low class, routed to manual review.</summary>
	public static readonly ImmutableArray<string> UntemplatedPatternClasses = ["PY-014"];

	/// <summary>Both flows, in the order the tests report them.</summary>
	public static readonly ImmutableArray<CredentialFlow> Flows =
	[
		CredentialFlow.ClientCredentials,
		CredentialFlow.AuthorizationCode
	];

	/// <summary>The embedded catalog is immutable, so it is built once and shared by every test that composes text.</summary>
	private static readonly Lazy<LanguageCatalog> _catalog = new(LanguageCatalogRegistry.BuildEmbeddedCatalog);

	public static LanguageCatalog Catalog()
	{
		return _catalog.Value;
	}

	public static ILanguageAnalyzer Analyzer()
	{
		return _catalog.Value.AnalyzersByLanguageName[_LANGUAGE_NAME];
	}

	/// <summary>The composed recommendation of one class under one flow.</summary>
	public static RenderedRecommendation Compose(string patternClassIdentifier, CredentialFlow flow)
	{
		return RecommendationComposer.Compose(_catalog.Value, AssessedLanguage.Python, patternClassIdentifier, flow);
	}

	/// <summary>The whole composed text of one templated class under one flow, as the artifact carries it.</summary>
	public static string FullText(string patternClassIdentifier, CredentialFlow flow)
	{
		return RecommendationComposer.ComposeArtifactText(Compose(patternClassIdentifier, flow))!;
	}

	/// <summary>The generic route's composed text for a class the analyzer does not declare.</summary>
	public static string GenericRouteText(CredentialFlow flow)
	{
		return RecommendationComposer.ComposeArtifactText(Compose(UNDECLARED_PATTERN_CLASS, flow))!;
	}

	/// <summary>Every composed text the `Python` language can produce for a High or Medium finding.</summary>
	public static ImmutableArray<string> AllComposedTexts()
	{
		ImmutableArray<string>.Builder texts = ImmutableArray.CreateBuilder<string>();

		foreach (CredentialFlow flow in Flows)
		{
			foreach (string patternClassIdentifier in TemplatedPatternClasses)
			{
				texts.Add(FullText(patternClassIdentifier, flow));
			}

			texts.Add(GenericRouteText(flow));
		}

		return texts.ToImmutable();
	}
}
