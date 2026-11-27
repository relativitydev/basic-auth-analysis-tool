using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Catalog;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>What one generated block of Part C holds: a pattern class under a flow, or the generic recommendation of a flow.</summary>
internal sealed record PartCBlockKey(string? PatternClassIdentifier, CredentialFlow Flow)
{
	public bool IsGeneric => PatternClassIdentifier is null;
}

/// <summary>
/// WHAT THE GENERATED BLOCKS OF PART C SAY, COMPOSED BY THE SAME
/// <see cref="RecommendationComposer"/> THE REPORT USES AND BY NOTHING ELSE.
///
/// A block's key is <c>{class identifier} {flow slug}</c> for a pattern class and
/// <c>generic {flow slug}</c> for a flow's generic recommendation. A class entry is composed with
/// <see cref="RecommendationComposer.Compose(LanguageCatalog, AssessedLanguage, string, CredentialFlow)"/>
/// for the class and the flow. The generic block is composed the same way for
/// <see cref="CSharpRenderedTexts.UNDECLARED_PATTERN_CLASS"/>, an identifier the analyzer declares no
/// template for: the composer then routes to the language's own text for the flow and to the shared engine's text
/// only where the language declares none, exactly as it does for a High or Medium finding of an
/// unrecognized construct. The block is the composed artifact text of that recommendation, preceded by
/// one generated line that says which flow the entry is for and points to the "Which flow" paragraph of
/// Part A, and with the OAuth2 Client block of the flow replaced by a link to the one section of Part C that
/// holds it. Part C also carries two <c>oauth2-client {flow slug}</c> blocks, one per flow, each holding
/// the engine-owned <see cref="OAuth2ClientConfigurationBlocks"/> text once.
/// </summary>
internal static class PartCBlockComposition
{
	public const string GENERIC_KEY_PREFIX = "generic";
	public const string CLIENT_CREDENTIALS_SLUG = "client-credentials";
	public const string AUTHORIZATION_CODE_SLUG = "authorization-code";
	public const string FLOW_LEAD_MARKER = "**Flow.**";
	public const string OAUTH2_CLIENT_KEY_PREFIX = "oauth2-client";
	public const string CLIENT_CREDENTIALS_SECTION_ANCHOR = "#oauth2-client-for-client-credentials";
	public const string AUTHORIZATION_CODE_SECTION_ANCHOR = "#oauth2-client-for-authorization-code";
	public const string CLIENT_CREDENTIALS_SECTION_TITLE = "OAuth2 Client for Client Credentials";
	public const string AUTHORIZATION_CODE_SECTION_TITLE = "OAuth2 Client for Authorization Code";
	public const string CLASS_ANCHOR_PREFIX = "#";

	/// <summary>The key of the block that holds the table of contents of Part C: every level 2 and level 3 heading, computed from the finished document.</summary>
	public const string TABLE_OF_CONTENTS_KEY = "table-of-contents";

	private const char _KEY_SEPARATOR = ' ';
	private const string _FLOW_LEAD_FORMAT = "{0} This entry is for {1}.{2}";
	private const string _CLASS_CLAUSE_FORMAT = " The \"Which flow\" paragraph of {0} in [Part A]({1}{2}) says when a match belongs to this flow.";
	private const string _SECTION_LINK_FORMAT = "{0}: See [{1}]({2}).";
	private const string _OAUTH2_CLIENT_PIECE_FORMAT = "{0}: {1}";
	private const string _BLOCK_NOT_FOUND_MESSAGE = "The composed text of the block '{0}' does not contain the OAuth2 Client block of {1}, so it cannot be replaced by a link to its section.";
	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	/// <summary>The flows in the order Part C presents them: the default first.</summary>
	public static readonly ImmutableArray<CredentialFlow> Flows = CSharpRenderedTexts.Flows;

	public static string SlugOf(CredentialFlow flow)
	{
		return flow == CredentialFlow.ClientCredentials ? CLIENT_CREDENTIALS_SLUG : AUTHORIZATION_CODE_SLUG;
	}

	public static string KeyText(PartCBlockKey key)
	{
		string subject = key.PatternClassIdentifier ?? GENERIC_KEY_PREFIX;

		return string.Concat(subject, _KEY_SEPARATOR, SlugOf(key.Flow));
	}

	public static PartCBlockKey ParseKey(string keyText)
	{
		string[] parts = keyText.Split(_KEY_SEPARATOR);
		CredentialFlow flow = parts[1] == CLIENT_CREDENTIALS_SLUG ? CredentialFlow.ClientCredentials : CredentialFlow.AuthorizationCode;
		string? patternClassIdentifier = parts[0] == GENERIC_KEY_PREFIX ? null : parts[0];

		return new PartCBlockKey(patternClassIdentifier, flow);
	}

	/// <summary>The class identifiers that receive advice: every registered class whose base level is not Low.</summary>
	public static ImmutableArray<string> ClassesThatProduceAdvice(ILanguageAnalyzer analyzer)
	{
		return
		[
			.. analyzer.DetectionRulesByPatternClass
				.Where(entry => entry.Value.BaseLevel != ConfidenceLevel.Low)
				.Select(entry => entry.Key)
				.Order(StringComparer.Ordinal)
		];
	}

	/// <summary>The classes registered at base Low: manual review, no flow, no recommendation.</summary>
	public static ImmutableArray<string> ManualReviewClasses(ILanguageAnalyzer analyzer)
	{
		return
		[
			.. analyzer.DetectionRulesByPatternClass
				.Where(entry => entry.Value.BaseLevel == ConfidenceLevel.Low)
				.Select(entry => entry.Key)
				.Order(StringComparer.Ordinal)
		];
	}

	/// <summary>Every block Part C must carry: each class that produces advice under each flow, then the generic block of each flow.</summary>
	public static ImmutableArray<PartCBlockKey> ExpectedKeys(ILanguageAnalyzer analyzer)
	{
		ImmutableArray<PartCBlockKey>.Builder keys = ImmutableArray.CreateBuilder<PartCBlockKey>();

		foreach (string patternClassIdentifier in ClassesThatProduceAdvice(analyzer))
		{
			foreach (CredentialFlow flow in Flows)
			{
				keys.Add(new PartCBlockKey(patternClassIdentifier, flow));
			}
		}

		foreach (CredentialFlow flow in Flows)
		{
			keys.Add(new PartCBlockKey(null, flow));
		}

		return keys.ToImmutable();
	}

	/// <summary>The composed recommendation behind a block of <paramref name="language"/>, before the confirmation line is added.</summary>
	public static RenderedRecommendation Composed(DocumentedLanguage language, PartCBlockKey key)
	{
		string patternClassIdentifier = key.PatternClassIdentifier ?? language.UndeclaredPatternClass;

		return RecommendationComposer.Compose(CSharpRenderedTexts.Catalog(), language.Language, patternClassIdentifier, key.Flow);
	}

	/// <summary>The key of the Part C block that holds the OAuth2 Client block of a flow.</summary>
	public static string OAuth2ClientKeyOf(CredentialFlow flow)
	{
		return string.Concat(OAUTH2_CLIENT_KEY_PREFIX, _KEY_SEPARATOR, SlugOf(flow));
	}

	/// <summary>Whether a block key names the table of contents and not a recommendation or an OAuth2 Client section.</summary>
	public static bool IsTableOfContentsKey(string keyText)
	{
		return keyText == TABLE_OF_CONTENTS_KEY;
	}

	/// <summary>Whether a block key names an OAuth2 Client section and not a class or generic entry.</summary>
	public static bool IsOAuth2ClientKey(string keyText)
	{
		return keyText.StartsWith(string.Concat(OAUTH2_CLIENT_KEY_PREFIX, _KEY_SEPARATOR), StringComparison.Ordinal);
	}

	/// <summary>The keys of the two OAuth2 Client section blocks, one per flow.</summary>
	public static ImmutableArray<string> OAuth2ClientKeys()
	{
		return [.. Flows.Select(OAuth2ClientKeyOf)];
	}

	/// <summary>The flow an OAuth2 Client section key is for.</summary>
	public static CredentialFlow FlowOfOAuth2ClientKey(string keyText)
	{
		return keyText.EndsWith(CLIENT_CREDENTIALS_SLUG, StringComparison.Ordinal) ? CredentialFlow.ClientCredentials : CredentialFlow.AuthorizationCode;
	}

	/// <summary>The anchor of the section that holds the OAuth2 Client block of a flow.</summary>
	public static string SectionAnchorOf(CredentialFlow flow)
	{
		return flow == CredentialFlow.ClientCredentials ? CLIENT_CREDENTIALS_SECTION_ANCHOR : AUTHORIZATION_CODE_SECTION_ANCHOR;
	}

	/// <summary>The title of the section that holds the OAuth2 Client block of a flow.</summary>
	public static string SectionTitleOf(CredentialFlow flow)
	{
		return flow == CredentialFlow.ClientCredentials ? CLIENT_CREDENTIALS_SECTION_TITLE : AUTHORIZATION_CODE_SECTION_TITLE;
	}

	/// <summary>The line an entry carries instead of the OAuth2 Client block: the label and a link to the section of its flow.</summary>
	public static string SectionLinkOf(CredentialFlow flow)
	{
		return string.Format(CultureInfo.InvariantCulture, _SECTION_LINK_FORMAT, RecommendationComposer.OAUTH2_CLIENT_LABEL, SectionTitleOf(flow), SectionAnchorOf(flow));
	}

	/// <summary>The piece the composer appends to every recommendation: the label and the block of the flow.</summary>
	public static string OAuth2ClientPieceOf(CredentialFlow flow)
	{
		return string.Format(CultureInfo.InvariantCulture, _OAUTH2_CLIENT_PIECE_FORMAT, RecommendationComposer.OAUTH2_CLIENT_LABEL, OAuth2ClientConfigurationBlocks.For(flow));
	}

	/// <summary>The whole content of one generated block of <paramref name="language"/>: an entry, or the OAuth2 Client block of a flow.</summary>
	public static string ContentFor(DocumentedLanguage language, string keyText)
	{
		string content;

		if (IsOAuth2ClientKey(keyText))
		{
			content = OAuth2ClientConfigurationBlocks.For(FlowOfOAuth2ClientKey(keyText));
		}
		else
		{
			PartCBlockKey key = ParseKey(keyText);
			string composedText = RecommendationComposer.ComposeArtifactText(Composed(language, key))!;

			content = string.Join(_PARAGRAPH_SEPARATOR, FlowLead(language, key), WithSectionLink(keyText, composedText, key.Flow));
		}

		return content;
	}

	/// <summary>The composed text with the OAuth2 Client piece of the flow replaced by the link to its section; throws where the piece is absent.</summary>
	public static string WithSectionLink(string keyText, string composedText, CredentialFlow flow)
	{
		string piece = OAuth2ClientPieceOf(flow);

		return composedText.Contains(piece, StringComparison.Ordinal)
			? composedText.Replace(piece, SectionLinkOf(flow), StringComparison.Ordinal)
			: throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, _BLOCK_NOT_FOUND_MESSAGE, keyText, flow));
	}

	private static string FlowLead(DocumentedLanguage language, PartCBlockKey key)
	{
		string classClause = key.IsGeneric
			? string.Empty
			: string.Format(
				CultureInfo.InvariantCulture,
				_CLASS_CLAUSE_FORMAT,
				key.PatternClassIdentifier,
				language.PartAFileName,
				CLASS_ANCHOR_PREFIX + key.PatternClassIdentifier!.ToLowerInvariant());

		return string.Format(CultureInfo.InvariantCulture, _FLOW_LEAD_FORMAT, FLOW_LEAD_MARKER, FlowNameOf(key.Flow), classClause);
	}

	private static string FlowNameOf(CredentialFlow flow)
	{
		return flow == CredentialFlow.ClientCredentials ? PublicDocumentChecks.CLIENT_CREDENTIALS_NAME : PublicDocumentChecks.AUTHORIZATION_CODE_NAME;
	}
}
