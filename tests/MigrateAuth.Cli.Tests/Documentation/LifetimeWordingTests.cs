using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// EVERY PLACE THAT RECOMMENDS AN ACCESS TOKEN LIFETIME SAYS THE SAME THING.
///
/// The wording is the tool's own: the lifetime line of the OAuth2 Client block, which both flows carry and
/// which is rendered from <see cref="OAuth2ClientConfigurationBlocks.RECOMMENDED_ACCESS_TOKEN_LIFETIME_IN_MINUTES"/>.
/// The check has four legs: the two blocks as the composer renders them carry the identical sentence; every
/// generated block of Part C carries it; rows 5 and 7 of the User Guide carry it; and in every Markdown file
/// under <c>docs/public/</c> every line that speaks of a lifetime and gives a number of minutes gives the
/// recommended one, so no document recommends a different value.
/// </summary>
public sealed class LifetimeWordingTests
{
	private const string _LIFETIME_LINE_PREFIX = "- " + OAuth2ClientConfigurationBlocks.ACCESS_TOKEN_LIFETIME_FIELD + ": ";
	private const string _LIFETIME_WORD = "lifetime";
	private const string _MINUTES_PATTERN = "\\b(?<number>\\d+)\\s+minutes\\b";
	private const string _MINUTES_STATEMENT_START = "minutes, stated in minutes because the field is in minutes.";
	private const string _NO_LIFETIME_LINE_MESSAGE = "The {0} block has no lifetime line.";
	private const string _DIFFERENT_VALUE_MESSAGE = "{0} line {1} recommends {2} minutes for a lifetime.";
	private const string _SELF_CHECK_LINE = "Set the Access Token Lifetime to 30 minutes.";
	private const string _SELF_CHECK_GOOD_LINE = "Use an access token lifetime of 60 minutes.";
	private const string _LINE_BREAK = "\n";

	private static readonly Regex _minutes = new(_MINUTES_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

	/// <summary>The one sentence every place says: the value of the lifetime line, taken from the composed Client Credentials block.</summary>
	private static string CanonicalWording()
	{
		return LifetimeWordingOf(UserGuideBlockComposition.OAuth2ClientBlockOf(CredentialFlow.ClientCredentials));
	}

	/// <summary>The two blocks as the composer renders them state the recommended value, in minutes, with the identical sentence, and claim no requirement.</summary>
	[Fact]
	public void BothOAuth2ClientBlocksCarryTheIdenticalLifetimeSentence()
	{
		string clientCredentials = LifetimeWordingOf(UserGuideBlockComposition.OAuth2ClientBlockOf(CredentialFlow.ClientCredentials));
		string authorizationCode = LifetimeWordingOf(UserGuideBlockComposition.OAuth2ClientBlockOf(CredentialFlow.AuthorizationCode));
		string expectedStart = $"{OAuth2ClientConfigurationBlocks.RECOMMENDED_ACCESS_TOKEN_LIFETIME_IN_MINUTES.ToString(CultureInfo.InvariantCulture)} {_MINUTES_STATEMENT_START}";

		Assert.Equal(clientCredentials, authorizationCode);
		Assert.StartsWith(expectedStart, clientCredentials, StringComparison.Ordinal);
		Assert.DoesNotContain("required", clientCredentials, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Every generated block of Part C carries the sentence, so Part C says what the blocks say.</summary>
	[Fact]
	public void EveryPartCBlockCarriesTheSentence()
	{
		string partC = File.ReadAllText(PublicDocuments.PartCPath());
		string canonical = CanonicalWording();

		ImmutableArray<GeneratedBlock> blocks = GeneratedBlockMarkers.Blocks(partC);

		Assert.NotEmpty(blocks);
		Assert.All(blocks, block => Assert.Contains(canonical, block.Content, StringComparison.Ordinal));
	}

	/// <summary>Row 5 carries the sentence in both of its blocks and row 7 quotes it.</summary>
	[Fact]
	public void RowsFiveAndSevenCarryTheSentence()
	{
		string canonical = CanonicalWording();
		string rowFive = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_CONFIGURING_THE_CLIENT);
		string rowSeven = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_TOKEN_EXPIRY);

		ImmutableArray<GeneratedBlock> blocks = GeneratedBlockMarkers.Blocks(rowFive);

		Assert.Equal(PartCBlockComposition.Flows.Length, blocks.Length);
		Assert.All(blocks, block => Assert.Contains(canonical, block.Content, StringComparison.Ordinal));
		Assert.Contains(canonical, rowSeven, StringComparison.Ordinal);
	}

	/// <summary>No line of any public document that speaks of a lifetime gives a number of minutes other than the recommended one.</summary>
	[Fact]
	public void NoPublicDocumentRecommendsAnotherLifetime()
	{
		string[] problems =
		[
			.. PublicDocuments.AllPublicMarkdownPaths().SelectMany(path => LifetimeProblemsIn(path, File.ReadAllText(path)))
		];

		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The check for another value fails on a line that gives one, and passes a line that gives the recommended one.</summary>
	[Fact]
	public void TheLifetimeCheckReportsALineThatRecommendsAnotherValue()
	{
		Assert.Single(LifetimeProblemsIn("synthetic.md", _SELF_CHECK_LINE));
		Assert.Empty(LifetimeProblemsIn("synthetic.md", _SELF_CHECK_GOOD_LINE));
	}

	private static string LifetimeWordingOf(string oauth2ClientBlock)
	{
		string? line = oauth2ClientBlock.Split(_LINE_BREAK).FirstOrDefault(candidate => candidate.StartsWith(_LIFETIME_LINE_PREFIX, StringComparison.Ordinal));

		Assert.True(line is not null, string.Format(CultureInfo.InvariantCulture, _NO_LIFETIME_LINE_MESSAGE, nameof(OAuth2ClientConfigurationBlocks)));

		return line![_LIFETIME_LINE_PREFIX.Length..];
	}

	private static ImmutableArray<string> LifetimeProblemsIn(string path, string text)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		string[] lines = text.Split(_LINE_BREAK);

		for (int index = 0; index < lines.Length; index++)
		{
			if (lines[index].Contains(_LIFETIME_WORD, StringComparison.OrdinalIgnoreCase))
			{
				problems.AddRange(
					_minutes.Matches(lines[index])
						.Select(match => match.Groups["number"].Value)
						.Where(number => number != OAuth2ClientConfigurationBlocks.RECOMMENDED_ACCESS_TOKEN_LIFETIME_IN_MINUTES.ToString(CultureInfo.InvariantCulture))
						.Select(number => string.Format(CultureInfo.InvariantCulture, _DIFFERENT_VALUE_MESSAGE, Path.GetFileName(path), index + 1, number)));
			}
		}

		return problems.ToImmutable();
	}
}
