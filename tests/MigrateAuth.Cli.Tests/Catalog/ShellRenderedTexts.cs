using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The recommendation texts the real embedded `Shell` analyzer composes, read through the shared engine's <see cref="RecommendationComposer"/> exactly as the
/// report does. This is <see cref="VBNetRenderedTexts"/>'s shape retargeted at the `Shell` language, shared by the tests that assert on the composed wording of
/// both flows and by the documentation tests that name the language's undeclared class.
/// </summary>
internal static class ShellRenderedTexts
{
	/// <summary>A class identifier the `Shell` analyzer does not declare, so its High or Medium finding takes the generic route.</summary>
	public const string UNDECLARED_PATTERN_CLASS = "SH-404";

	private const string _LANGUAGE_NAME = "Shell";

	/// <summary>The eighteen classes that give advice; the seven Low classes carry no template.</summary>
	public static readonly ImmutableArray<string> TemplatedPatternClasses =
	[
		"SH-001", "SH-002", "SH-003", "SH-004", "SH-005", "SH-006", "SH-007", "SH-008", "SH-009", "SH-010", "SH-013", "SH-014", "SH-016", "SH-018", "SH-019", "SH-021", "SH-024", "SH-025"
	];

	/// <summary>The seven Low classes, routed to manual review.</summary>
	public static readonly ImmutableArray<string> UntemplatedPatternClasses = ["SH-011", "SH-012", "SH-015", "SH-017", "SH-020", "SH-022", "SH-023"];

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
		return RecommendationComposer.Compose(_catalog.Value, AssessedLanguage.Shell, patternClassIdentifier, flow);
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

	/// <summary>Every composed text the `Shell` language can produce for a High or Medium finding.</summary>
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
