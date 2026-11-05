using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>One begin or end marker found in a document: its kind, its key and where it sits.</summary>
internal sealed record MarkerOccurrence(string Kind, string Key, int Index, int Length);

/// <summary>One generated block: its key and what lies between its markers, without the line break that follows the begin marker and the one that precedes the end marker.</summary>
internal sealed record GeneratedBlock(string Key, string Content);

/// <summary>
/// THE GRAMMAR OF THE DELIMITERS AROUND THE GENERATED BLOCKS OF THE PUBLIC MIGRATION
/// DOCUMENTS, AND THE THREE OPERATIONS THE DRIFT TEST NEEDS OVER IT: FIND THE MARKERS, CHECK THAT THEY
/// ARE BALANCED, AND REWRITE WHAT LIES BETWEEN A PAIR.
///
/// A marker is an HTML comment on a line of its own, so it renders as nothing:
/// <c>&lt;!-- generated:begin KEY --&gt;</c> opens a block and <c>&lt;!-- generated:end KEY --&gt;</c>
/// closes it. The key is the rest of the comment and names what the block holds. A begin marker has
/// exactly one end marker with the same key after it, blocks never nest, and no key appears twice.
/// Only text between a pair is ever rewritten; every other character of the document is hand-written
/// and is copied through unchanged.
/// </summary>
internal static class GeneratedBlockMarkers
{
	public const string BEGIN_KIND = "begin";
	public const string END_KIND = "end";

	private const string _MARKER_FORMAT = "<!-- generated:{0} {1} -->";
	private const string _MARKER_PATTERN = "<!-- generated:(?<kind>begin|end) (?<key>[^\\r\\n]*?) -->";
	private const string _LINE_BREAK = "\n";

	private static readonly Regex _markerExpression = new(_MARKER_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

	public static string BeginMarker(string key)
	{
		return string.Format(CultureInfo.InvariantCulture, _MARKER_FORMAT, BEGIN_KIND, key);
	}

	public static string EndMarker(string key)
	{
		return string.Format(CultureInfo.InvariantCulture, _MARKER_FORMAT, END_KIND, key);
	}

	public static ImmutableArray<MarkerOccurrence> Find(string text)
	{
		ImmutableArray<MarkerOccurrence>.Builder markers = ImmutableArray.CreateBuilder<MarkerOccurrence>();

		foreach (Match match in _markerExpression.Matches(text))
		{
			markers.Add(new MarkerOccurrence(match.Groups["kind"].Value, match.Groups["key"].Value, match.Index, match.Length));
		}

		return markers.ToImmutable();
	}

	/// <summary>
	/// Everything wrong with the marker pairs of <paramref name="text"/>, one sentence each: an end
	/// with no begin, a begin inside another block, an end that closes a different key, a key that
	/// appears twice, and a begin that is never closed. Empty when the pairs are balanced.
	/// </summary>
	public static ImmutableArray<string> BalanceProblems(string text)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		HashSet<string> keysSeen = [];
		string? openKey = null;

		foreach (MarkerOccurrence marker in Find(text))
		{
			openKey = marker.Kind == BEGIN_KIND
				? OpenBlock(marker, openKey, keysSeen, problems)
				: CloseBlock(marker, openKey, problems);
		}

		if (openKey is not null)
		{
			problems.Add($"The block '{openKey}' is opened and never closed.");
		}

		return problems.ToImmutable();
	}

	/// <summary>The blocks of a balanced document, in document order.</summary>
	public static ImmutableArray<GeneratedBlock> Blocks(string text)
	{
		ImmutableArray<MarkerOccurrence> markers = Find(text);
		ImmutableArray<GeneratedBlock>.Builder blocks = ImmutableArray.CreateBuilder<GeneratedBlock>();

		for (int index = 0; index + 1 < markers.Length; index += 2)
		{
			int contentStart = markers[index].Index + markers[index].Length;
			string inner = text[contentStart..markers[index + 1].Index];

			blocks.Add(new GeneratedBlock(markers[index].Key, TrimOneLineBreakEachSide(inner)));
		}

		return blocks.ToImmutable();
	}

	/// <summary>
	/// The document with the content between every marker pair replaced by
	/// <paramref name="contentForKey"/> of that pair's key. Nothing outside a pair is touched.
	/// </summary>
	public static string Rewrite(string text, Func<string, string> contentForKey)
	{
		ImmutableArray<MarkerOccurrence> markers = Find(text);
		StringBuilder rewritten = new();
		int copiedUpTo = 0;

		for (int index = 0; index + 1 < markers.Length; index += 2)
		{
			int contentStart = markers[index].Index + markers[index].Length;

			rewritten.Append(text, copiedUpTo, contentStart - copiedUpTo);
			rewritten.Append(_LINE_BREAK).Append(contentForKey(markers[index].Key)).Append(_LINE_BREAK);
			copiedUpTo = markers[index + 1].Index;
		}

		rewritten.Append(text, copiedUpTo, text.Length - copiedUpTo);

		return rewritten.ToString();
	}

	private static string? OpenBlock(MarkerOccurrence marker, string? openKey, HashSet<string> keysSeen, ImmutableArray<string>.Builder problems)
	{
		if (openKey is not null)
		{
			problems.Add($"The block '{marker.Key}' is opened inside the block '{openKey}', and blocks never nest.");
		}

		if (!keysSeen.Add(marker.Key))
		{
			problems.Add($"The block '{marker.Key}' appears more than once.");
		}

		return marker.Key;
	}

	private static string? CloseBlock(MarkerOccurrence marker, string? openKey, ImmutableArray<string>.Builder problems)
	{
		if (openKey is null)
		{
			problems.Add($"The end marker of '{marker.Key}' has no begin marker before it.");
		}
		else if (openKey != marker.Key)
		{
			problems.Add($"The block '{openKey}' is closed by the end marker of '{marker.Key}'.");
		}

		return null;
	}

	private static string TrimOneLineBreakEachSide(string inner)
	{
		string trimmed = inner;

		if (trimmed.StartsWith(_LINE_BREAK, StringComparison.Ordinal))
		{
			trimmed = trimmed[_LINE_BREAK.Length..];
		}

		if (trimmed.EndsWith(_LINE_BREAK, StringComparison.Ordinal))
		{
			trimmed = trimmed[..^_LINE_BREAK.Length];
		}

		return trimmed;
	}
}
