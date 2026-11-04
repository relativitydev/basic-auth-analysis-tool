using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The recommendation texts the real embedded `CSharp` analyzer composes, read through the shared engine's
/// <see cref="RecommendationComposer"/> exactly as the report does. Shared by the tests that assert
/// on the composed wording of both flows.
/// </summary>
internal static class CSharpRenderedTexts
{
	/// <summary>A class identifier the `CSharp` analyzer does not declare, so its High or Medium finding takes the generic route.</summary>
	public const string UNDECLARED_PATTERN_CLASS = "CS-404";

	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	/// <summary>The seventeen classes that have a recommendation template.</summary>
	public static readonly ImmutableArray<string> TemplatedPatternClasses =
	[
		"CS-001", "CS-002", "CS-003", "CS-004", "CS-005", "CS-006", "CS-007", "CS-008", "CS-009",
		"CS-012", "CS-013", "CS-014", "CS-015", "CS-016", "CS-017", "CS-018", "CS-020"
	];

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
		return Catalog().AnalyzersByLanguageName["CSharp"];
	}

	/// <summary>The composed recommendation of one templated class under one flow.</summary>
	public static RenderedRecommendation Compose(string patternClassIdentifier, CredentialFlow flow)
	{
		return RecommendationComposer.Compose(Catalog(), AssessedLanguage.CSharp, patternClassIdentifier, flow);
	}

	/// <summary>The whole composed text of one templated class under one flow, as the artifact carries it.</summary>
	public static string FullText(string patternClassIdentifier, CredentialFlow flow)
	{
		return RecommendationComposer.ComposeArtifactText(Compose(patternClassIdentifier, flow))!;
	}

	/// <summary>The generic route's composed text for a class the analyzer does not declare.</summary>
	public static RenderedRecommendation GenericRoute(CredentialFlow flow)
	{
		return Compose(UNDECLARED_PATTERN_CLASS, flow);
	}

	/// <summary>Every composed text the `CSharp` language can produce for a High or Medium finding.</summary>
	public static ImmutableArray<string> AllComposedTexts()
	{
		ImmutableArray<string>.Builder texts = ImmutableArray.CreateBuilder<string>();

		foreach (CredentialFlow flow in Flows)
		{
			foreach (string patternClassIdentifier in TemplatedPatternClasses)
			{
				texts.Add(FullText(patternClassIdentifier, flow));
			}

			texts.Add(RecommendationComposer.ComposeArtifactText(GenericRoute(flow))!);
		}

		return texts.ToImmutable();
	}

	/// <summary>The paragraph of <paramref name="text"/> that opens with <paramref name="leadIn"/>.</summary>
	public static string ParagraphStartingWith(string text, string leadIn)
	{
		string paragraph = text
			.Split(_PARAGRAPH_SEPARATOR, StringSplitOptions.None)
			.First(candidate => candidate.StartsWith(leadIn, StringComparison.Ordinal));

		return paragraph;
	}
}
