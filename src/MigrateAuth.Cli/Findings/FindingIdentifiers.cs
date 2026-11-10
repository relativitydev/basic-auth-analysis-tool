using System.Globalization;

namespace MigrateAuth.Cli.Findings;

/// <summary>
/// THE SEQUENTIAL IDENTIFIER.
///
/// The identifier is a zero-padded three-digit string, sequential per scan
/// session. At a thousand findings THE FORMAT WIDENS rather than wraps, truncates or fails,
/// because a large repository exceeds 999 findings easily.
///
/// Widening is implemented by padding rather than by a fixed-width format so that the property is
/// structural: padding to a minimum width cannot shorten a number, whereas a width specifier is one
/// revision away from becoming one that can.
///
/// AND THE STRING IS NOT THE ONLY IDENTITY A FINDING CARRIES. The data artifact carries a PARALLEL
/// NUMERIC INDEX for a reason this class is the demonstration of: sorting on the
/// string is lexicographic, and lexicographically "1000" precedes "999". A consumer that sorted
/// the artifact on the identifier would see finding 1000 before finding 999 and conclude the tool
/// emits them out of order. <see cref="Finding.SequentialIndex"/> is the number the string is
/// rendered from, carried beside it rather than re-parsed out of it.
/// </summary>
public static class FindingIdentifiers
{
	/// <summary>
	/// The index of the first finding in a scan session.
	///
	/// ONE, AND NOT ZERO, BECAUSE A HUMAN READS IT, beside a line and a column that count from one.
	/// </summary>
	public const int FIRST_INDEX = 1;

	/// <summary>The identifier's width. A minimum, never a maximum - see the type's own note.</summary>
	private const int _MINIMUM_DIGIT_COUNT = 3;

	private const char _PADDING_DIGIT = '0';

	/// <summary>
	/// The identifier a finding at this index carries. Three digits up to 999, four at 1000, and
	/// as many as the number needs after that.
	/// </summary>
	public static string Of(int sequentialIndex)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(sequentialIndex, FIRST_INDEX);

		return sequentialIndex
			.ToString(CultureInfo.InvariantCulture)
			.PadLeft(_MINIMUM_DIGIT_COUNT, _PADDING_DIGIT);
	}
}
