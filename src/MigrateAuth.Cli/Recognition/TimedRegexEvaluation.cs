using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE ONE DOOR EVERY ANALYZER-DECLARED EXPRESSION'S EVALUATION PASSES THROUGH TO CHARGE
/// <see cref="MatchBoundWatch"/>'S PER-FILE BUDGET.
///
/// Its callers include <see cref="RecognitionEngine"/>'s detection-rule loop and its interpreted
/// cross-check (<see cref="RecognitionEngine.InterpretedCrossCheckConfirmsTheMatch"/>, which re-evaluates
/// a rule's pattern through an interpreted <see cref="Regex"/> when a match's anchor group sits outside
/// its own span), <see cref="FileSymbolTable.Collect"/>'s constant-declaration expression and
/// <see cref="FileDemotionSignals"/>'s generation-marker and content-marker expressions, all over the
/// same <c>normalizedText</c> a detection rule scans.
///
/// The lexical scan (<c>Lexical.LexicalScanner</c>) and the first-line language attribution
/// (<c>Scanning.LanguageAttribution</c>) evaluate analyzer-declared expressions before
/// <see cref="RecognitionEngine.Recognize"/> opens the file's budget
/// (<see cref="MatchBoundWatch.BeginFile"/>), so they are charged instead to
/// <c>Lexical.LexicalBudgetWatch</c>: the here-document-opening and interpolation-identifier-extent
/// expressions (<c>LexicalScanner.MatchAnchored</c>) and the first-line detection expression
/// (<c>LanguageAttribution.MatchFirstLine</c>) share a budget of their own, one per file per stage,
/// which each also declares as its own regex timeout. A trip there maps to the <c>LexicalAnomaly</c>
/// skipped-item category in the lexical scan and to a counted extension fallback in attribution, never
/// to the match-bound category. Every analyzer-declared expression evaluated in <c>src/</c> is
/// therefore charged either to this door or to the lexical budget.
/// </summary>
internal static class TimedRegexEvaluation
{
	/// <summary>The bound's own charge for an expression's opening attempt over the whole text.</summary>
	internal static Match Match(MatchBoundWatch matchBoundWatch, Regex expression, string text)
	{
		Match result;
		matchBoundWatch.BeginMatchAttempt();

		try
		{
			result = expression.Match(text);
		}
		finally
		{
			matchBoundWatch.EndMatchAttempt();
		}

		return result;
	}

	/// <summary>
	/// The bound's own charge for an attempt starting at an absolute position in the SAME, whole
	/// input string - never a <c>Substring</c> first, on the terms <see cref="RecognitionEngine"/>'s
	/// own resumed-match doc comment gives: a lookbehind must still see the text before <paramref
	/// name="startAt"/>, which slicing the text would deny it.
	/// </summary>
	internal static Match Match(MatchBoundWatch matchBoundWatch, Regex expression, string text, int startAt)
	{
		Match result;
		matchBoundWatch.BeginMatchAttempt();

		try
		{
			result = expression.Match(text, startAt);
		}
		finally
		{
			matchBoundWatch.EndMatchAttempt();
		}

		return result;
	}

	/// <summary>The bound's own charge for every attempt after the first in one enumeration.</summary>
	internal static Match NextMatch(MatchBoundWatch matchBoundWatch, Match match)
	{
		Match result;
		matchBoundWatch.BeginMatchAttempt();

		try
		{
			result = match.NextMatch();
		}
		finally
		{
			matchBoundWatch.EndMatchAttempt();
		}

		return result;
	}

	/// <summary>The bound's own charge for a yes/no probe that never needs the match itself.</summary>
	internal static bool IsMatch(MatchBoundWatch matchBoundWatch, Regex expression, string text)
	{
		bool result;
		matchBoundWatch.BeginMatchAttempt();

		try
		{
			result = expression.IsMatch(text);
		}
		finally
		{
			matchBoundWatch.EndMatchAttempt();
		}

		return result;
	}
}
