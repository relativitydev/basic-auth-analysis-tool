using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE CONDITIONAL-COMPILATION BRANCHES OF ONE FILE THAT ITS OWN SYMBOLS SWITCH OFF, as sorted, disjoint half-open
/// intervals of the decoded text: from the start of the line after a directive to the start of the next directive line
/// of a branch that is definitely not taken. A branch that is live, or that may or may not be taken, is not in the map,
/// and neither is anything the reading does not trust (an unparsable directive, an unmatched directive, a group with no
/// closing directive, a nesting over the cap, a file holding a literal the mask cannot vouch for).
///
/// IT IS BUILT ONCE PER FILE, BEFORE THE RULE LOOP, by <see cref="Build"/>, from the optional
/// <see cref="ConditionalCompilationRule"/> of the profile; a profile that declares none gets <see cref="Empty"/>.
/// A lookup is one binary search, O(log r) over the r intervals. The map holds no regular expression and is not charged to
/// the match bound: its construction is a forward pass over the text that visits each character a bounded number of times
/// (see <see cref="ConditionalRegionMapBuilder"/>).
/// </summary>
public sealed class ConditionalRegionMap
{
	/// <summary>The deepest nesting of conditional groups the reading follows; an <c>if</c> that would make it deeper stops the reading.</summary>
	public const int MAX_NESTING_DEPTH = 64;

	private ConditionalRegionMap(ImmutableArray<(int Start, int End)> inactive)
	{
		Inactive = inactive;
	}

	/// <summary>The map of a file that has no directive the reading trusts, or of a profile that declares none.</summary>
	public static ConditionalRegionMap Empty { get; } = new([]);

	/// <summary>Every inactive interval, sorted by start and disjoint; <c>End</c> is exclusive.</summary>
	public ImmutableArray<(int Start, int End)> Inactive { get; }

	/// <summary>
	/// The map of one file. <paramref name="rule"/> is the profile's element (null reads nothing), <paramref name="text"/> the
	/// decoded text the <paramref name="mask"/> indexes. Nothing past the mask's trust extent is read.
	/// </summary>
	public static ConditionalRegionMap Build(ConditionalCompilationRule? rule, string text, ZoneMask mask)
	{
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(mask);

		return rule is null ? Empty : ConditionalRegionMapBuilder.Build(rule, text, mask);
	}

	/// <summary>A map made of the given intervals, which the caller guarantees are sorted and disjoint.</summary>
	internal static ConditionalRegionMap Of(ImmutableArray<(int Start, int End)> inactive)
	{
		return inactive.IsEmpty ? Empty : new ConditionalRegionMap(inactive);
	}

	/// <summary>Whether the offset lies in a branch the file's own symbols switch off.</summary>
	public bool IsInactiveAt(int offset)
	{
		int low = 0;
		int high = Inactive.Length - 1;
		bool inactive = false;

		while (low <= high && !inactive)
		{
			int middle = low + ((high - low) / 2);
			(int start, int end) = Inactive[middle];

			if (offset < start)
			{
				high = middle - 1;
			}
			else if (offset >= end)
			{
				low = middle + 1;
			}
			else
			{
				inactive = true;
			}
		}

		return inactive;
	}
}
