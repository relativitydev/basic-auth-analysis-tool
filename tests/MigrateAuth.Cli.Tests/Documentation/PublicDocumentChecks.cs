using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// THE CHECKS THE DOCUMENTATION TESTS RUN OVER
/// THE TEXT OF PART A AND PART C, AS FUNCTIONS OF TEXT THAT RETURN THEIR PROBLEMS.
///
/// Each check takes the text as an argument instead of reading the committed file, so the same check
/// that passes over the real document can be shown to fail over a copy with something taken out. A
/// check that has never been seen to fail proves nothing about the document it passes.
/// </summary>
internal static class PublicDocumentChecks
{
	public const string HEADING_SEPARATOR = " · ";
	public const string CLASS_HEADING_PREFIX = "### ";
	public const string EXAMPLE_LABEL = "**Example.**";
	public const string HOW_TO_SEARCH_LABEL = "**How to search.**";
	public const string CONFIDENCE_LABEL = "**Confidence.**";
	public const string WHICH_FLOW_LABEL = "**Which flow.**";
	public const string EXAMPLE_FENCE = "```csharp";
	public const string SEARCH_FENCE = "```bash";
	public const string UNSUPPORTED_SECTION_HEADING = "## Declared unsupported coverage";
	public const string FLOW_SIGNALS_SECTION_HEADING = "## Flow signals";
	public const string INTERACTIVE_SIGNALS_HEADING = "### Interactive-user signals";
	public const string FIXED_SIGNALS_HEADING = "### Fixed-credential signals";
	public const string FLOW_SIGNALS_LINK = "#flow-signals";
	public const string CROSS_FILE_LIMIT_PHRASE = "same file";
	public const string NO_FLOW_PHRASE = "no flow";
	public const string CLIENT_CREDENTIALS_NAME = "Client Credentials";
	public const string AUTHORIZATION_CODE_NAME = "Authorization Code";

	public const string CACHE_AND_RE_ACQUIRE_MARKER = "**Cache the token, and re-acquire it on expiry";
	public const string ON_EXPIRY_MARKER = "**On expiry.**";
	public const string REFRESH_TOKEN_REQUEST_MARKER = "grant_type=refresh_token";
	public const string PKCE_MARKER = "**PKCE.**";
	public const string SERVER_SIDE_VARIANT_MARKER = "**Server-side variant.**";
	public const string DESKTOP_VARIANT_MARKER = "**Desktop variant.**";

	private const string _FENCE_MARKER = "```";
	private const string _LABEL_TERMINATOR = ":";
	private const string _SECTION_END_PREFIX_LEVEL_ONE = "# ";
	private const string _SECTION_END_PREFIX_LEVEL_TWO = "## ";
	private const string _SECTION_END_PREFIX_LEVEL_THREE = "### ";
	private const string _LINE_BREAK = "\n";
	private const string _WHITESPACE_RUN = "\\s+";
	private const string _SINGLE_SPACE = " ";

	private static readonly Regex _whitespaceRun = new(_WHITESPACE_RUN, RegexOptions.CultureInvariant);

	/// <summary>Collapses every run of whitespace to one space, so a construct still matches where a document wraps a line.</summary>
	public static string NormalizeWhitespace(string text)
	{
		return _whitespaceRun.Replace(text, _SINGLE_SPACE);
	}

	/// <summary>
	/// The text from <paramref name="headingLine"/> up to the next heading of the same or a higher
	/// level, or null where the heading is absent. The heading line itself is not part of the section.
	/// </summary>
	public static string? SectionOf(string text, string headingLine)
	{
		string[] lines = text.Split(_LINE_BREAK);
		int headingIndex = Array.FindIndex(lines, line => line.TrimEnd() == headingLine);
		string? section = null;

		if (headingIndex >= 0)
		{
			List<string> body = [];
			bool insideFence = false;

			foreach (string line in lines.Skip(headingIndex + 1))
			{
				insideFence ^= line.TrimStart().StartsWith(_FENCE_MARKER, StringComparison.Ordinal);

				if (!insideFence && EndsSectionOf(headingLine, line))
				{
					break;
				}

				body.Add(line);
			}

			section = string.Join(_LINE_BREAK, body);
		}

		return section;
	}

	/// <summary>The paragraph of <paramref name="section"/> that opens with <paramref name="label"/>, or null.</summary>
	public static string? ParagraphOf(string section, string label)
	{
		string[] paragraphs = section.Split(_LINE_BREAK + _LINE_BREAK);

		return paragraphs.FirstOrDefault(paragraph => paragraph.TrimStart().StartsWith(label, StringComparison.Ordinal));
	}

	/// <summary>The heading line Part A and Part C give a class.</summary>
	public static string ClassHeading(string patternClassIdentifier, string displayName)
	{
		return string.Concat(CLASS_HEADING_PREFIX, patternClassIdentifier, HEADING_SEPARATOR, displayName);
	}

	/// <summary>Everything a registered class lacks in Part A: its heading, or any of the four labelled elements.</summary>
	public static ImmutableArray<string> PartAClassProblems(string partA, IReadOnlyDictionary<string, DetectionRule> rulesByPatternClass, string exampleFence = EXAMPLE_FENCE)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		foreach ((string patternClassIdentifier, DetectionRule rule) in rulesByPatternClass.OrderBy(entry => entry.Key, StringComparer.Ordinal))
		{
			string? section = SectionOf(partA, ClassHeading(patternClassIdentifier, rule.DisplayName));

			if (section is null)
			{
				problems.Add($"Part A has no heading for {patternClassIdentifier} ({rule.DisplayName}).");
			}
			else
			{
				problems.AddRange(MissingElementsOf(patternClassIdentifier, section, exampleFence));
			}
		}

		return problems.ToImmutable();
	}

	/// <summary>Every registered class must appear in Part C: as a heading, or as the key of a generated block.</summary>
	public static ImmutableArray<string> PartCClassProblems(string partC, IReadOnlyDictionary<string, DetectionRule> rulesByPatternClass)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		HashSet<string> blockSubjects = [.. GeneratedBlockMarkers.Find(partC).Select(marker => marker.Key.Split(' ')[0])];

		foreach ((string patternClassIdentifier, DetectionRule rule) in rulesByPatternClass.OrderBy(entry => entry.Key, StringComparer.Ordinal))
		{
			bool hasHeading = SectionOf(partC, ClassHeading(patternClassIdentifier, rule.DisplayName)) is not null;

			if (!hasHeading && !blockSubjects.Contains(patternClassIdentifier))
			{
				problems.Add($"Part C does not mention {patternClassIdentifier} ({rule.DisplayName}).");
			}
		}

		return problems.ToImmutable();
	}

	/// <summary>Every declared-unsupported construct the analyzer emits must appear in Part A's unsupported section.</summary>
	public static ImmutableArray<string> UnsupportedProblems(string partA, IEnumerable<DeclaredUnsupportedCoverage> declared)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		string? section = SectionOf(partA, UNSUPPORTED_SECTION_HEADING);
		string normalizedSection = NormalizeWhitespace(section ?? string.Empty);

		if (section is null)
		{
			problems.Add($"Part A has no section headed '{UNSUPPORTED_SECTION_HEADING}'.");
		}

		foreach (DeclaredUnsupportedCoverage entry in declared)
		{
			if (!normalizedSection.Contains(NormalizeWhitespace(entry.Construct), StringComparison.OrdinalIgnoreCase))
			{
				problems.Add($"Part A does not list the unsupported construct: {entry.Construct}");
			}
		}

		return problems.ToImmutable();
	}

	/// <summary>
	/// A "Which flow" paragraph consistent with the catalog's signal section and the cross-file limit:
	/// it links to the signal section, states that signals are read in the same file as the match, and
	/// for a class that produces advice names both flows; for a manual-review class it says there is no flow.
	/// </summary>
	public static ImmutableArray<string> WhichFlowProblems(string partA, IReadOnlyDictionary<string, DetectionRule> rulesByPatternClass)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		foreach ((string patternClassIdentifier, DetectionRule rule) in rulesByPatternClass.OrderBy(entry => entry.Key, StringComparer.Ordinal))
		{
			string section = SectionOf(partA, ClassHeading(patternClassIdentifier, rule.DisplayName)) ?? string.Empty;
			string? paragraph = ParagraphOf(section, WHICH_FLOW_LABEL);

			if (paragraph is null)
			{
				problems.Add($"{patternClassIdentifier} has no 'Which flow' paragraph.");
			}
			else
			{
				problems.AddRange(WhichFlowParagraphProblems(patternClassIdentifier, rule, NormalizeWhitespace(paragraph)));
			}
		}

		return problems.ToImmutable();
	}

	/// <summary>Each analyzer signal is listed in Part A under the heading of its own kind, and not under the other.</summary>
	public static ImmutableArray<string> FlowSignalProblems(string partA, IEnumerable<FlowSignal> signals)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		string interactive = SectionOf(partA, INTERACTIVE_SIGNALS_HEADING) ?? string.Empty;
		string fixedCredential = SectionOf(partA, FIXED_SIGNALS_HEADING) ?? string.Empty;
		string signalsSection = SectionOf(partA, FLOW_SIGNALS_SECTION_HEADING) ?? string.Empty;

		if (!NormalizeWhitespace(signalsSection).Contains(CROSS_FILE_LIMIT_PHRASE, StringComparison.OrdinalIgnoreCase))
		{
			problems.Add($"The flow signals section does not state the cross-file limit ('{CROSS_FILE_LIMIT_PHRASE}').");
		}

		foreach (FlowSignal signal in signals)
		{
			bool isInteractive = signal.Kind == FlowSignalKind.InteractiveUser;
			string owner = isInteractive ? interactive : fixedCredential;
			string other = isInteractive ? fixedCredential : interactive;

			if (!owner.Contains($"`{signal.Name}`", StringComparison.Ordinal) || other.Contains($"`{signal.Name}`", StringComparison.Ordinal))
			{
				problems.Add($"The signal {signal.Name} is not listed under its own kind, {signal.Kind}, alone.");
			}
		}

		return problems.ToImmutable();
	}

	/// <summary>
	/// The markers of Part C item 2 in one class entry: before, after, token caching and expiry,
	/// how to confirm the flow, the OAuth2 Client settings and the Part B pointer; and for a Code entry
	/// also refresh-token handling, PKCE and both variants.
	/// </summary>
	public static ImmutableArray<string> ItemTwoProblems(GeneratedBlock block, PartCBlockKey key)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		string oauth2Client = OAuth2ClientConfigurationBlocks.For(key.Flow);

		foreach ((string element, string marker) in CommonElementMarkers(key))
		{
			AddIfMissing(problems, block, element, marker);
		}

		AddIfMissing(problems, block, "the OAuth2 Client settings of the flow", oauth2Client);

		if (key.Flow == CredentialFlow.AuthorizationCode && !key.IsGeneric)
		{
			AddIfMissing(problems, block, "refresh-token handling", REFRESH_TOKEN_REQUEST_MARKER);
			AddIfMissing(problems, block, "PKCE", PKCE_MARKER);
			AddIfMissing(problems, block, "the server-side variant", SERVER_SIDE_VARIANT_MARKER);
			AddIfMissing(problems, block, "the desktop variant", DESKTOP_VARIANT_MARKER);
		}

		return problems.ToImmutable();
	}

	private static IEnumerable<(string Element, string Marker)> CommonElementMarkers(PartCBlockKey key)
	{
		yield return ("how to confirm the flow and the default", PartCBlockComposition.CONFIRMING_THE_FLOW_MARKER);
		yield return ("the OAuth2 Client label", RecommendationComposer.OAUTH2_CLIENT_LABEL + _LABEL_TERMINATOR);
		yield return ("the Part B pointer", SecretManagementPointer.PART_B_NAME);

		if (!key.IsGeneric)
		{
			yield return ("the before form", RecommendationRenderer.SOURCE_FORM_LABEL + _LABEL_TERMINATOR);
			yield return ("the after form", RecommendationRenderer.TARGET_FORM_LABEL + _LABEL_TERMINATOR);
			yield return (
				"token caching and expiry",
				key.Flow == CredentialFlow.ClientCredentials ? CACHE_AND_RE_ACQUIRE_MARKER : ON_EXPIRY_MARKER);
		}
	}

	private static void AddIfMissing(ImmutableArray<string>.Builder problems, GeneratedBlock block, string element, string marker)
	{
		if (!block.Content.Contains(marker, StringComparison.Ordinal))
		{
			problems.Add($"The entry '{block.Key}' has no marker for {element}: {marker}");
		}
	}

	private static ImmutableArray<string> MissingElementsOf(string patternClassIdentifier, string section, string exampleFence)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		int exampleAt = section.IndexOf(EXAMPLE_LABEL, StringComparison.Ordinal);
		int searchAt = section.IndexOf(HOW_TO_SEARCH_LABEL, StringComparison.Ordinal);

		if (exampleAt < 0 || section.IndexOf(exampleFence, Math.Max(exampleAt, 0), StringComparison.Ordinal) < 0)
		{
			problems.Add($"{patternClassIdentifier} has no placeholder example ('{EXAMPLE_LABEL}' followed by a {exampleFence} block).");
		}

		if (searchAt < 0 || section.IndexOf(SEARCH_FENCE, Math.Max(searchAt, 0), StringComparison.Ordinal) < 0)
		{
			problems.Add($"{patternClassIdentifier} has no way to search ('{HOW_TO_SEARCH_LABEL}' followed by a {SEARCH_FENCE} block).");
		}

		foreach (string label in new[] { CONFIDENCE_LABEL, WHICH_FLOW_LABEL })
		{
			if (!section.Contains(label, StringComparison.Ordinal))
			{
				problems.Add($"{patternClassIdentifier} has no '{label}' element.");
			}
		}

		return problems.ToImmutable();
	}

	private static ImmutableArray<string> WhichFlowParagraphProblems(string patternClassIdentifier, DetectionRule rule, string paragraph)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		string[] required = rule.BaseLevel == ConfidenceLevel.Low
			? [NO_FLOW_PHRASE]
			: [CLIENT_CREDENTIALS_NAME, AUTHORIZATION_CODE_NAME, CROSS_FILE_LIMIT_PHRASE, FLOW_SIGNALS_LINK];

		foreach (string phrase in required)
		{
			if (!paragraph.Contains(phrase, StringComparison.OrdinalIgnoreCase))
			{
				problems.Add($"The 'Which flow' paragraph of {patternClassIdentifier} does not contain '{phrase}'.");
			}
		}

		return problems.ToImmutable();
	}

	private static bool EndsSectionOf(string headingLine, string line)
	{
		string levelPrefix = headingLine.StartsWith(_SECTION_END_PREFIX_LEVEL_THREE, StringComparison.Ordinal)
			? _SECTION_END_PREFIX_LEVEL_THREE
			: _SECTION_END_PREFIX_LEVEL_TWO;
		bool endsAtSameLevel = line.StartsWith(levelPrefix, StringComparison.Ordinal);
		bool endsAtHigherLevel = line.StartsWith(_SECTION_END_PREFIX_LEVEL_ONE, StringComparison.Ordinal)
			|| (levelPrefix == _SECTION_END_PREFIX_LEVEL_THREE && line.StartsWith(_SECTION_END_PREFIX_LEVEL_TWO, StringComparison.Ordinal));

		return endsAtSameLevel || endsAtHigherLevel;
	}
}
