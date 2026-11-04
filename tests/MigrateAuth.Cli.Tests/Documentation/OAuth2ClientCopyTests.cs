using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// ROW 5 OF THE USER GUIDE HOLDS THE OAUTH2 CLIENT FIELD LIST OF EACH FLOW IN A
/// GENERATED BLOCK, AND THIS CHECK SHOWS THE BLOCK IS A COPY OF WHAT THE TOOL SAYS.
///
/// The copy check has three legs: the committed row 5 equals a fresh composition through
/// <see cref="RecommendationComposer"/> (byte for byte inside the markers); the block equals the base-owned
/// <see cref="OAuth2ClientConfigurationBlocks"/> text for the flow; and every block of Part C carries the
/// same text for its flow, so the guide, Part C and the report cannot say different things about a field.
/// </summary>
public sealed class OAuth2ClientCopyTests
{
	private const string _NEVER_CREATES_STATEMENT = "never creates or changes the OAuth2 Client";
	private const string _ALTERED_FIELD_TEXT = "an altered field description";
	private const string _MISSING_BLOCK_MESSAGE = "Row 5 has no generated block for the flow {0}.";
	private const string _BULLET_PREFIX = "- ";
	private const char _LINE_BREAK = '\n';
	private const int _FIRST_FIELD_LINE_INDEX = 1;

	private static readonly ImmutableArray<CredentialFlow> _flows = PartCBlockComposition.Flows;

	/// <summary>The markers of row 5 are balanced and hold exactly one field-list block for each flow.</summary>
	[Fact]
	public void RowFiveHoldsExactlyOneOAuth2ClientBlockForEachFlow()
	{
		string rowFive = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_CONFIGURING_THE_CLIENT);

		string[] expected = [.. _flows.Select(UserGuideBlockComposition.OAuth2ClientKey).Order(StringComparer.Ordinal)];
		string[] actual = [.. GeneratedBlockMarkers.Blocks(rowFive).Select(block => block.Key).Order(StringComparer.Ordinal)];

		Assert.Empty(GeneratedBlockMarkers.BalanceProblems(rowFive));
		Assert.Equal(expected, actual);
	}

	/// <summary>The committed field lists equal a fresh composition through the composer; the failure names each block that differs.</summary>
	[Fact]
	public void RowFiveBlocksEqualAFreshCompositionThroughTheComposer()
	{
		(string committed, string fresh) = UserGuideBlockComposition.CommittedAndFresh(PublicDocuments.UserGuideRowPath(PublicDocuments.ROW_CONFIGURING_THE_CLIENT));

		string[] differing = DifferingBlockKeys(committed, fresh);

		Assert.True(
			differing.Length == 0,
			$"Row 5 differs from a fresh composition in: {string.Join(", ", differing)}. {UserGuideBlockComposition.REGENERATE_HINT}");
		Assert.Equal(fresh, committed);
	}

	/// <summary>Each block is the base-owned block of its flow, field for field, and names every field of that flow.</summary>
	[Fact]
	public void EachRowFiveBlockIsTheToolsOwnBlockForItsFlow()
	{
		string rowFive = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_CONFIGURING_THE_CLIENT);

		foreach (CredentialFlow flow in _flows)
		{
			string block = BlockOf(rowFive, flow);
			string[] fieldLines = [.. OAuth2ClientConfigurationBlocks.For(flow).Split(_LINE_BREAK).Where(line => line.StartsWith(_BULLET_PREFIX, StringComparison.Ordinal))];

			Assert.Equal(OAuth2ClientConfigurationBlocks.For(flow), block);
			Assert.All(fieldLines, line => Assert.Contains(line, block, StringComparison.Ordinal));
			Assert.Contains(_NEVER_CREATES_STATEMENT, block, StringComparison.Ordinal);
		}
	}

	/// <summary>Every generated block of Part C carries the same field list as row 5 for its flow.</summary>
	[Fact]
	public void EveryPartCBlockCarriesTheSameFieldListAsRowFive()
	{
		string partC = File.ReadAllText(PublicDocuments.PartCPath());

		foreach (GeneratedBlock block in GeneratedBlockMarkers.Blocks(partC))
		{
			CredentialFlow flow = PartCBlockComposition.ParseKey(block.Key).Flow;

			Assert.Contains(UserGuideBlockComposition.OAuth2ClientBlockOf(flow), block.Content, StringComparison.Ordinal);
		}
	}

	/// <summary>The copy check itself fails when one field of a block is altered, shown on a copy of the committed row.</summary>
	[Fact]
	public void TheCopyCheckReportsABlockWhoseFieldWasAltered()
	{
		string rowFive = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_CONFIGURING_THE_CLIENT);
		string firstFieldLine = OAuth2ClientConfigurationBlocks.For(CredentialFlow.ClientCredentials).Split(_LINE_BREAK)[_FIRST_FIELD_LINE_INDEX];
		string altered = rowFive.Replace(firstFieldLine, _ALTERED_FIELD_TEXT, StringComparison.Ordinal);
		string fresh = GeneratedBlockMarkers.Rewrite(altered, UserGuideBlockComposition.ContentFor);

		Assert.Empty(DifferingBlockKeys(rowFive, GeneratedBlockMarkers.Rewrite(rowFive, UserGuideBlockComposition.ContentFor)));
		Assert.Equal(
			[UserGuideBlockComposition.OAuth2ClientKey(CredentialFlow.ClientCredentials)],
			DifferingBlockKeys(altered, fresh));
	}

	private static string BlockOf(string document, CredentialFlow flow)
	{
		string key = UserGuideBlockComposition.OAuth2ClientKey(flow);
		GeneratedBlock? block = GeneratedBlockMarkers.Blocks(document).FirstOrDefault(candidate => candidate.Key == key);

		Assert.True(block is not null, string.Format(CultureInfo.InvariantCulture, _MISSING_BLOCK_MESSAGE, flow));

		return block!.Content;
	}

	private static string[] DifferingBlockKeys(string committed, string fresh)
	{
		Dictionary<string, string> freshByKey = GeneratedBlockMarkers.Blocks(fresh).ToDictionary(block => block.Key, block => block.Content);

		return
		[
			.. GeneratedBlockMarkers.Blocks(committed)
				.Where(block => !freshByKey.TryGetValue(block.Key, out string? content) || content != block.Content)
				.Select(block => block.Key)
		];
	}
}
