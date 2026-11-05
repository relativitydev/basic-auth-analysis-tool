using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The recommendation texts the real embedded `VBNet` analyzer composes, read through the shared engine's
/// <see cref="RecommendationComposer"/> exactly as the report does. This is <see cref="CSharpRenderedTexts"/>'s own shape, retargeted at the
/// `VBNet` language, shared by the tests that assert on the composed wording of both flows.
/// </summary>
internal static class VBNetRenderedTexts
{
	/// <summary>A class identifier the `VBNet` analyzer does not declare, so its High or Medium finding takes the generic route.</summary>
	public const string UNDECLARED_PATTERN_CLASS = "VB-404";

	private const string _LANGUAGE_NAME = "VBNet";

	/// <summary>The seventeen classes that give advice; the two Low classes carry no template.</summary>
	public static readonly ImmutableArray<string> TemplatedPatternClasses =
	[
		"VB-001", "VB-002", "VB-003", "VB-004", "VB-005", "VB-006", "VB-007", "VB-008", "VB-009", "VB-010", "VB-011", "VB-012", "VB-013", "VB-014", "VB-015", "VB-016", "VB-017"
	];

	/// <summary>The two Low classes, routed to manual review.</summary>
	public static readonly ImmutableArray<string> UntemplatedPatternClasses = ["VB-018", "VB-019"];

	/// <summary>Both flows, in the order the tests report them.</summary>
	public static readonly ImmutableArray<CredentialFlow> Flows =
	[
		CredentialFlow.ClientCredentials,
		CredentialFlow.AuthorizationCode
	];

	/// <summary>The embedded catalog is immutable, so it is built once and shared by every test that composes text.</summary>
	private static readonly Lazy<LanguageCatalog> _catalog = new(LanguageCatalogRegistry.BuildEmbeddedCatalog);

	public static ILanguageAnalyzer Analyzer()
	{
		return _catalog.Value.AnalyzersByLanguageName[_LANGUAGE_NAME];
	}

	/// <summary>The composed recommendation of one class under one flow.</summary>
	public static RenderedRecommendation Compose(string patternClassIdentifier, CredentialFlow flow)
	{
		return RecommendationComposer.Compose(_catalog.Value, AssessedLanguage.VBNet, patternClassIdentifier, flow);
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

	/// <summary>Every composed text the `VBNet` language can produce for a High or Medium finding.</summary>
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
