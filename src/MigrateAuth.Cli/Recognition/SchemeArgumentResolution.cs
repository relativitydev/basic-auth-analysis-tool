using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// Symbolic resolution of a scheme argument, from the captured argument to the verdict and from the
/// verdict to the rule's own reading of it.
///
/// A literal is told from an identifier using the mask, not quote characters: an argument whose
/// captured extent carries any <see cref="LexicalZone.StringBody"/> position is a literal and the
/// string-body run is its value, with the <see cref="LexicalZone.Delimiter"/> positions excluded by
/// the mask.
///
/// An argument the mask does not vouch for (above <see cref="ZoneMask.ValidThroughPosition"/>)
/// resolves to <see cref="SymbolicVerdict.Indeterminate"/>, so an untrustworthy region can never
/// suppress a finding.
///
/// The comparison is case-insensitive, tolerant of surrounding whitespace, and uses ordinal case
/// folding so that the answer does not vary with the host's installed locale data.
/// </summary>
internal static class SchemeArgumentResolution
{
	/// <summary>No position inside the captured argument carried a literal body. Not an offset.</summary>
	private const int _NO_LITERAL_BODY_POSITION = -1;

	/// <summary>
	/// The verdict for one match of one rule. A rule that declares no scheme argument never reaches
	/// here; a rule that does gets one of the three values and never a fourth.
	/// </summary>
	internal static SymbolicVerdict Resolve(
		SymbolicVerdictInterpretation interpretation,
		Match match,
		string normalizedText,
		ZoneMask mask,
		FileSymbolTable symbols)
	{
		SymbolicVerdict verdict = SymbolicVerdict.Indeterminate;
		Group argument = ArgumentOf(interpretation, match);

		if (argument.Success && argument.Length > 0 && IsWhollyTrustworthy(argument, mask))
		{
			verdict = ResolveTrustworthyArgument(interpretation, argument, normalizedText, mask, symbols);
		}

		return verdict;
	}

	/// <summary>
	/// What the rule says the verdict means for it. The responses are an enumeration the analyzer
	/// declares and the engine applies, so that a catalog states an interpretation and cannot
	/// execute one.
	/// </summary>
	internal static SymbolicVerdictResponse ResponseTo(
		SymbolicVerdictInterpretation interpretation,
		SymbolicVerdict verdict)
	{
		SymbolicVerdictResponse response = verdict switch
		{
			SymbolicVerdict.ResolvedMatching => interpretation.WhenResolvedMatching,
			SymbolicVerdict.ResolvedNotMatching => interpretation.WhenResolvedNotMatching,
			_ => interpretation.WhenIndeterminate
		};

		return response;
	}

	private static Group ArgumentOf(SymbolicVerdictInterpretation interpretation, Match match)
	{
		return interpretation.SchemeArgumentCaptureGroupName is string argumentGroupName
			? match.Groups[argumentGroupName]
			: Match.Empty;
	}

	private static SymbolicVerdict ResolveTrustworthyArgument(
		SymbolicVerdictInterpretation interpretation,
		Group argument,
		string normalizedText,
		ZoneMask mask,
		FileSymbolTable symbols)
	{
		SymbolicVerdict verdict;

		if (TryReadLiteralBody(argument, normalizedText, mask, out string literalBody))
		{
			// A literal settles itself. There is no ambiguity for the symbol table to resolve: the
			// value is written at the use, so the counting rule has nothing to count.
			verdict = CompareAgainstExpectedToken(literalBody, interpretation.ExpectedSchemeToken);
		}
		else if (symbols.TryResolveUnambiguously(argument.Value, argument.Index, out string declaredValue))
		{
			verdict = CompareAgainstExpectedToken(declaredValue, interpretation.ExpectedSchemeToken);
		}
		else
		{
			verdict = SymbolicVerdict.Indeterminate;
		}

		return verdict;
	}

	/// <summary>
	/// The literal's body, where the captured argument is one. The run of string-body positions
	/// inside the capture IS the body by the mask's own definition, so nothing here strips a quote,
	/// a prefix or a variable-length delimiter - which is what keeps this file free of any
	/// knowledge of what a literal looks like in any particular language.
	/// </summary>
	private static bool TryReadLiteralBody(Group argument, string normalizedText, ZoneMask mask, out string literalBody)
	{
		int firstBodyPosition = _NO_LITERAL_BODY_POSITION;
		int lastBodyPosition = _NO_LITERAL_BODY_POSITION;

		for (int position = argument.Index; position < argument.Index + argument.Length; position++)
		{
			if (mask.ZoneAt(position) is LexicalZone.StringBody)
			{
				firstBodyPosition = firstBodyPosition is _NO_LITERAL_BODY_POSITION ? position : firstBodyPosition;
				lastBodyPosition = position;
			}
		}

		bool isLiteral = firstBodyPosition is not _NO_LITERAL_BODY_POSITION;

		literalBody = isLiteral
			? normalizedText.Substring(firstBodyPosition, lastBodyPosition - firstBodyPosition + 1)
			: string.Empty;

		return isLiteral;
	}

	/// <summary>
	/// Whether the scanner vouches for every position of the captured argument. Partly-vouched-for
	/// is treated as not vouched for at all: half an argument inside the sound region and half
	/// above it would be read with a value the scanner does not stand behind.
	/// </summary>
	private static bool IsWhollyTrustworthy(Group argument, ZoneMask mask)
	{
		bool trustworthy = true;

		for (int position = argument.Index; position < argument.Index + argument.Length; position++)
		{
			if (!mask.IsTrustworthyAt(position))
			{
				trustworthy = false;
				break;
			}
		}

		return trustworthy;
	}

	/// <summary>
	/// The comparison, and the one case where a resolved value still yields indeterminate: a rule
	/// that declares a scheme argument without declaring what to compare it against. The contract
	/// validator already refuses that combination, so reaching it means the analyzer was never
	/// validated - and answering "not matching" there would SUPPRESS findings under a rule like the
	/// reference analyzer's, which is the most destructive answer available to a question nobody
	/// asked properly.
	/// </summary>
	private static SymbolicVerdict CompareAgainstExpectedToken(string value, string? expectedSchemeToken)
	{
		SymbolicVerdict verdict;

		if (string.IsNullOrWhiteSpace(expectedSchemeToken))
		{
			verdict = SymbolicVerdict.Indeterminate;
		}
		else
		{
			verdict = string.Equals(value.Trim(), expectedSchemeToken.Trim(), StringComparison.OrdinalIgnoreCase)
				? SymbolicVerdict.ResolvedMatching
				: SymbolicVerdict.ResolvedNotMatching;
		}

		return verdict;
	}
}
