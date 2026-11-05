using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Lexical;

/// <summary>
/// THE LEXICAL BUDGET, AS A COOPERATIVE PER-FILE WATCH. IT CHARGES THE THREE ANALYZER-DECLARED
/// EXPRESSIONS THAT RUN OUTSIDE THE RECOGNITION ENGINE'S MATCH BOUND: A HERE-DOCUMENT'S OPENING AND AN
/// INTERPOLATION IDENTIFIER'S EXTENT (BOTH EVALUATED BY <see cref="LexicalScanner"/>) AND A
/// LANGUAGE'S FIRST-LINE DETECTION EXPRESSION (EVALUATED BY <c>Scanning.LanguageAttribution</c>). IT ALSO CHARGES THE ARITHMETIC
/// READ-AHEAD OF <see cref="LexicalScanner"/>, THE EVALUATION OF THE ARITHMETIC-CONTEXT OPENING: A DOUBLE PARENTHESIS IS ARITHMETIC ONLY
/// WHERE ITS OWN CLOSING RUN STANDS, AND ONE READ-AHEAD ANSWERS EVERY RUN IT PASSES, SO IT IS CHARGED ONCE PER READ-AHEAD AND NEVER ONCE
/// PER OPENING (<see cref="BeginArithmeticCharge"/>).
///
/// IT IS THE SAME SHAPE, FOR THE SAME REASONS, AS <see cref="Recognition.MatchBoundWatch"/>: elapsed
/// time is read from a <see cref="TimeProvider"/> and accumulated across match attempts only, because
/// a regular expression's timeout is fixed in its constructor and the clock is what makes the budget
/// testable without waiting. It is a separate type rather than a second use of that one because the
/// two are different budgets with different constants, different stages and different trip
/// consequences: a trip here abandons a lexical scan or a first-line attribution, never a
/// recognition, and the two must not be able to spend each other's time.
///
/// THE HONEST LIMIT. This bounds the SUM of match attempts. It cannot interrupt ONE stuck call to
/// <c>Match</c>, which is why each of the three expressions also declares the budget as its own regex
/// timeout (validator codes 35 to 37), and why a <see cref="RegexMatchTimeoutException"/> from one of
/// them is a trip as well: both routes end in the same place, with the same per-stage semantics.
///
/// ONE BUDGET PER FILE PER STAGE. <see cref="BeginFile"/> opens it. Scan opens one per file for
/// first-line attribution and Analyze opens one per file for the lexical scan; Analyze never
/// re-attributes, since it reuses the language Scan persisted, so the two budgets never charge the
/// same expression and a trip cannot attribute one file to two languages.
/// </summary>
internal sealed class LexicalBudgetWatch
{
	private readonly TimeProvider _timeProvider;
	private readonly TimeSpan _budget;

	private TimeSpan _accumulatedMatchingTime;
	private TimeSpan _accumulatedArithmeticTime;
	private bool _hasTimedOut;

	internal LexicalBudgetWatch(TimeProvider timeProvider)
	{
		ArgumentNullException.ThrowIfNull(timeProvider);

		_timeProvider = timeProvider;
		_budget = LexicalEnginePolicy.LEXICAL_BUDGET;
	}

	/// <summary>Whether this file has spent its budget, or one of its expressions exceeded its own timeout. Read between match attempts, never inside one.</summary>
	internal bool HasBeenExceeded => _hasTimedOut || _accumulatedMatchingTime >= _budget;

	/// <summary>
	/// What this file's stage has been charged so far, split by what was charged: the expression searches and the arithmetic
	/// read-aheads. Their sum is what <see cref="HasBeenExceeded"/> weighs against the budget. Read by the timing gate after a scan,
	/// so that the cost of the arithmetic opening has its own row.
	/// </summary>
	internal LexicalCharge Charged => new(_accumulatedMatchingTime - _accumulatedArithmeticTime, _accumulatedArithmeticTime);

	/// <summary>Opens the budget for one file in one stage.</summary>
	internal void BeginFile()
	{
		_accumulatedMatchingTime = TimeSpan.Zero;
		_accumulatedArithmeticTime = TimeSpan.Zero;
		_hasTimedOut = false;
	}

	/// <summary>
	/// The charge for an expression's search over one window of the text. A search that exceeded the
	/// expression's own timeout is a trip and reads as no match, so a caller never has to tell the
	/// two apart to keep going: it checks <see cref="HasBeenExceeded"/> once the attempt is over.
	/// </summary>
	internal Match MatchWithin(Regex expression, string text, int beginning, int length)
	{
		Match result;
		long startedTimestamp = _timeProvider.GetTimestamp();

		try
		{
			result = expression.Match(text, beginning, length);
		}
		catch (RegexMatchTimeoutException)
		{
			_hasTimedOut = true;
			result = Match.Empty;
		}
		finally
		{
			_accumulatedMatchingTime += _timeProvider.GetElapsedTime(startedTimestamp);
		}

		return result;
	}

	/// <summary>
	/// Starts the charge of one arithmetic read-ahead: the stamp to hand to <see cref="EndArithmeticCharge"/> when it is over. The
	/// read-ahead is the evaluation of the arithmetic-context opening (is this double parenthesis closed as arithmetic?); it is
	/// answered once for every run it passes, so it is charged once per read-ahead and never once per opening.
	/// </summary>
	internal long BeginArithmeticCharge()
	{
		return _timeProvider.GetTimestamp();
	}

	/// <summary>Charges the time since <paramref name="startedTimestamp"/> to the file's budget, as the arithmetic read-ahead's.</summary>
	internal void EndArithmeticCharge(long startedTimestamp)
	{
		TimeSpan elapsed = _timeProvider.GetElapsedTime(startedTimestamp);

		_accumulatedMatchingTime += elapsed;
		_accumulatedArithmeticTime += elapsed;
	}

	/// <summary>The charge for a yes/no probe that never needs the match itself, on the same terms as <see cref="MatchWithin"/>.</summary>
	internal bool IsMatch(Regex expression, string text)
	{
		bool result;
		long startedTimestamp = _timeProvider.GetTimestamp();

		try
		{
			result = expression.IsMatch(text);
		}
		catch (RegexMatchTimeoutException)
		{
			_hasTimedOut = true;
			result = false;
		}
		finally
		{
			_accumulatedMatchingTime += _timeProvider.GetElapsedTime(startedTimestamp);
		}

		return result;
	}
}

/// <summary>What one file's stage was charged to the lexical budget: the expression searches and the arithmetic read-aheads.</summary>
public readonly record struct LexicalCharge(TimeSpan Expressions, TimeSpan ArithmeticReadAhead)
{
	/// <summary>The whole charge, which is what the budget weighs.</summary>
	public TimeSpan Total => Expressions + ArithmeticReadAhead;
}
