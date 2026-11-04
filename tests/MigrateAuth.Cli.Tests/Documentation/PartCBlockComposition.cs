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
/// one generated line that says where to confirm the flow and carries the tool's own flow note for the
/// flow, so the "how to confirm the flow and the default" element is produced from the tool's constants
/// and never written by hand.
/// </summary>
internal static class PartCBlockComposition
{
	public const string GENERIC_KEY_PREFIX = "generic";
	public const string CLIENT_CREDENTIALS_SLUG = "client-credentials";
	public const string AUTHORIZATION_CODE_SLUG = "authorization-code";
	public const string CONFIRMING_THE_FLOW_MARKER = "**Confirming the flow.**";

	public const string CONFIRMING_THE_FLOW_ANCHOR = "#confirming-the-flow";
	public const string CLASS_ANCHOR_PREFIX = "#";

	private const char _KEY_SEPARATOR = ' ';
	private const string _CONFIRMATION_LINE_FORMAT = "{0} {1} See [Confirming the flow]({2}){3}.";
	private const string _CLIENT_CREDENTIALS_FRAMING_FORMAT = "When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: \"{0}\" A fixed-credential signal assigns it with this note: \"{1}\"";
	private const string _AUTHORIZATION_CODE_FRAMING_FORMAT = "An interactive-user signal in a file assigns Authorization Code, with this note: \"{0}\" Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow]({1}).";
	private const string _CLASS_CLAUSE_FORMAT = " and the \"Which flow\" paragraph of {0} in [Part A]({1}{2})";
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

	/// <summary>The whole content of one generated block of <paramref name="language"/>.</summary>
	public static string ContentFor(DocumentedLanguage language, string keyText)
	{
		PartCBlockKey key = ParseKey(keyText);
		string composedText = RecommendationComposer.ComposeArtifactText(Composed(language, key))!;

		return string.Join(_PARAGRAPH_SEPARATOR, ConfirmationLine(language, key), composedText);
	}

	private static string ConfirmationLine(DocumentedLanguage language, PartCBlockKey key)
	{
		string classClause = key.IsGeneric
			? string.Empty
			: string.Format(
				CultureInfo.InvariantCulture,
				_CLASS_CLAUSE_FORMAT,
				key.PatternClassIdentifier,
				language.PartAFileName,
				CLASS_ANCHOR_PREFIX + key.PatternClassIdentifier!.ToLowerInvariant());

		return string.Format(
			CultureInfo.InvariantCulture,
			_CONFIRMATION_LINE_FORMAT,
			CONFIRMING_THE_FLOW_MARKER,
			FlowNoteOf(key.Flow),
			CONFIRMING_THE_FLOW_ANCHOR,
			classClause);
	}

	/// <summary>
	/// What the tool says for the decisions that assign this flow, quoted from <see cref="FlowNotes"/> and
	/// framed as the tool's behavior, since an entry in a document has no "this file" to point at.
	/// </summary>
	private static string FlowNoteOf(CredentialFlow flow)
	{
		return flow == CredentialFlow.ClientCredentials
			? string.Format(
				CultureInfo.InvariantCulture,
				_CLIENT_CREDENTIALS_FRAMING_FORMAT,
				FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE,
				FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE)
			: string.Format(
				CultureInfo.InvariantCulture,
				_AUTHORIZATION_CODE_FRAMING_FORMAT,
				FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE,
				CONFIRMING_THE_FLOW_ANCHOR);
	}
}
