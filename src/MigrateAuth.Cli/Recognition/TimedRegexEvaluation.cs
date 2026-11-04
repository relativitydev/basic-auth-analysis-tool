using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// The one door every analyzer-declared expression's evaluation passes through to charge
/// <see cref="MatchBoundWatch"/>'s per-file budget. <see cref="RecognitionEngine"/>'s detection-rule
/// loop and its interpreted cross-check (<see cref="RecognitionEngine.InterpretedCrossCheckConfirmsTheMatch"/>),
/// <see cref="FileSymbolTable.Collect"/> and <see cref="FileDemotionSignals"/> all evaluate through it,
/// so no phase of recognition runs an expression untimed.
///
/// The lexical scan (<c>Lexical.LexicalScanner</c>) and the first-line language attribution
/// (<c>Scanning.LanguageAttribution</c>) evaluate analyzer-declared expressions before
/// <see cref="RecognitionEngine.Recognize"/> opens the file's budget
/// (<see cref="MatchBoundWatch.BeginFile"/>), so they cannot share this door's budget. They are charged
/// instead to <c>Lexical.LexicalBudgetWatch</c>, a budget of their own, one per file per stage, which
/// each of those expressions also declares as its own regex timeout. A trip is mapped to the
/// <c>LexicalAnomaly</c> skipped-item category in the lexical scan and to a counted extension fallback
/// in attribution, never to the match-bound category. A structural test asserts that every
/// analyzer-declared expression evaluated in <c>src/</c> is charged to this door or to the lexical budget.
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
