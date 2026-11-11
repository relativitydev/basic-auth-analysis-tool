using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE CONSTRUCTION RULE FOR THE PACK'S CONTENT, exercised rather than inspected.
///
/// <see cref="AgentBoundarySignatureTests"/> asserts the signature; these assert what the
/// constructor does with it. Two things are worth running. The four finding-record fields must
/// survive unchanged - an entry that quietly normalized a path or recomposed a level would be a
/// second place those values are decided. And the static text must be resolved from the route rather
/// than carried in, so that the value in every instance that can exist is a constant of this
/// assembly.
/// </summary>
public sealed class PromptPackFindingEntryTests
{
	private const string _PATTERN_CLASS_IDENTIFIER = "CS-001";
	private const string _RELATIVE_PATH = "src/Client.cs";
	private const int _ANCHOR_LINE = 42;
	private const int _SEQUENTIAL_INDEX = 7;

	[Fact]
	public void AnEntryCarriesTheFourFindingRecordFieldsUnchanged()
	{
		PromptPackFindingEntry entry = BuildEntry(RecommendationKind.FromTemplate);

		Assert.Equal(_PATTERN_CLASS_IDENTIFIER, entry.PatternClass.Value);
		Assert.Equal(_RELATIVE_PATH, entry.Path.Value);
		Assert.Equal(_ANCHOR_LINE, entry.Line);
		Assert.Equal(ConfidenceLevel.High, entry.Level);
	}

	[Theory]
	[InlineData(RecommendationKind.FromTemplate, PromptPackInstructionText.CHARACTERIZED_CONSTRUCT_TEXT)]
	[InlineData(RecommendationKind.ManualReview, PromptPackInstructionText.MANUAL_REVIEW_TEXT)]
	[InlineData(RecommendationKind.Generic, PromptPackInstructionText.UNCHARACTERIZED_CONSTRUCT_TEXT)]
	public void TheRouteSelectsTheStaticTextAndNothingElseDoes(RecommendationKind guidance, string expectedText)
	{
		PromptPackFindingEntry entry = BuildEntry(guidance);

		Assert.Equal(guidance, entry.Guidance);
		Assert.Equal(expectedText, entry.InstructionText);
	}

	/// <summary>
	/// The three texts are distinct. Identical text on two routes would make the route a field
	/// nobody reads, and an agent could not tell a characterized construct from one the catalog
	/// never covered - which is the distinction the generic route exists to preserve.
	/// </summary>
	[Fact]
	public void TheThreeRoutesSayThreeDifferentThings()
	{
		string[] texts =
		[
			PromptPackInstructionText.CHARACTERIZED_CONSTRUCT_TEXT,
			PromptPackInstructionText.MANUAL_REVIEW_TEXT,
			PromptPackInstructionText.UNCHARACTERIZED_CONSTRUCT_TEXT
		];

		Assert.Equal(texts.Length, texts.Distinct(StringComparer.Ordinal).Count());
		Assert.All(texts, text => Assert.False(string.IsNullOrWhiteSpace(text)));
	}

	[Fact]
	public void AnEntryWithoutAPatternClassIsRefused()
	{
		Assert.Throws<ArgumentException>(() => new PromptPackFindingEntry(
			default,
			BuildRelativePath(),
			_ANCHOR_LINE,
			ConfidenceLevel.High,
			RecommendationKind.FromTemplate,
			_SEQUENTIAL_INDEX,
			CredentialFlow.ClientCredentials,
			false));
	}

	[Fact]
	public void AnEntryWithoutARelativePathIsRefused()
	{
		Assert.Throws<ArgumentException>(() => new PromptPackFindingEntry(
			BuildPatternClass(),
			default,
			_ANCHOR_LINE,
			ConfidenceLevel.High,
			RecommendationKind.FromTemplate,
			_SEQUENTIAL_INDEX,
			CredentialFlow.ClientCredentials,
			false));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void AnEntryWithoutALineIsRefused(int line)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new PromptPackFindingEntry(
			BuildPatternClass(),
			BuildRelativePath(),
			line,
			ConfidenceLevel.High,
			RecommendationKind.FromTemplate,
			_SEQUENTIAL_INDEX,
			CredentialFlow.ClientCredentials,
			false));
	}

	[Fact]
	public void ARouteThatIsNotOneOfTheThreeIsRefused()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new PromptPackFindingEntry(
			BuildPatternClass(),
			BuildRelativePath(),
			_ANCHOR_LINE,
			ConfidenceLevel.High,
			(RecommendationKind)int.MaxValue,
			_SEQUENTIAL_INDEX,
			CredentialFlow.ClientCredentials,
			false));
	}

	private static PromptPackFindingEntry BuildEntry(RecommendationKind guidance)
	{
		return new PromptPackFindingEntry(
			BuildPatternClass(),
			BuildRelativePath(),
			_ANCHOR_LINE,
			ConfidenceLevel.High,
			guidance,
			_SEQUENTIAL_INDEX,
			CredentialFlow.ClientCredentials,
			false);
	}

	private static RuleIdentifier BuildPatternClass()
	{
		Assert.True(RuleIdentifier.TryFromPatternClass(_PATTERN_CLASS_IDENTIFIER, out RuleIdentifier patternClass));

		return patternClass;
	}

	/// <summary>
	/// The path goes through the write boundary's containment primitive, which is the only route
	/// into the type and therefore the only route a pack entry could ever take.
	/// </summary>
	private static RepositoryRelativePath BuildRelativePath()
	{
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(AppContext.BaseDirectory);

		Assert.True(RepositoryRelativePath.TryFromScannedPath(repository, _RELATIVE_PATH, out RepositoryRelativePath path));

		return path;
	}
}
