using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE FILE-SCOPED SYMBOL TABLE, AND IT IS NOT A NAME-KEYED DICTIONARY.
///
/// A dictionary keyed by name collapses two constants
/// declared in different scopes and then answers "resolved" with no ambiguity signal, so the
/// resolve-downward rule never fires because the engine believes it knows. What is recorded here
/// instead is EVERY declaration of a name, with its position, and resolution succeeds only where
/// that is unambiguous.
///
/// AMBIGUITY IS DECIDED BY COUNTING, NOT BY SCOPE, because the engine has a zone mask and nothing
/// else - no block structure, no brace matching, no scope stack:
///
/// | In the analyzed file                                 | Verdict         |
/// |------------------------------------------------------|-----------------|
/// | Exactly one declaration of the name, BEFORE the use   | resolved        |
/// | More than one declaration, anywhere, in any scope     | indeterminate   |
/// | The only declaration positioned AFTER the use         | indeterminate   |
/// | No declaration of the name                            | indeterminate   |
///
/// A file declaring one name in two genuinely disjoint scopes returns indeterminate where a
/// scope-aware engine would resolve - a demotion to Medium, which still carries a recommendation.
/// What it never does is emit a confident wrong answer. The declaration-after-use row is the same
/// trade: hoisting rules differ by language and the profile vocabulary does not carry them.
///
/// DECLARATIONS ARE COLLECTED IN CODE ZONES ONLY. A constant written inside a comment or quoted
/// inside a literal is not a declaration, and counting it would let a commented-out line decide a
/// live finding's level.
/// </summary>
public sealed class FileSymbolTable
{
	/// <summary>
	/// The group a profile's constant-declaration expression names its constant in. It is an ENGINE
	/// CONVENTION rather than a contract field: an expression that omits
	/// this group yields no declarations at all - and therefore an indeterminate verdict, which is
	/// the conservative direction and not a silent wrong answer.
	/// </summary>
	public const string NAME_CAPTURE_GROUP = "name";

	/// <summary>The group the declared value is read from, on the same terms.</summary>
	public const string VALUE_CAPTURE_GROUP = "value";

	private readonly Dictionary<string, List<Declaration>> _declarationsByName;

	private FileSymbolTable(Dictionary<string, List<Declaration>> declarationsByName)
	{
		_declarationsByName = declarationsByName;
	}

	/// <summary>An empty table, which resolves nothing and therefore indeterminates everything.</summary>
	public static FileSymbolTable Empty { get; } = new(new Dictionary<string, List<Declaration>>(StringComparer.Ordinal));

	/// <summary>
	/// Collects one file's string constant declarations, on no match-bound budget of its own - a
	/// convenience for a caller with no per-file bound to charge against, such as an isolated test
	/// exercising symbolic resolution alone. It builds a fresh <see cref="MatchBoundWatch"/> on the
	/// policy bound and the system clock and discards whether that budget was exceeded.
	/// <see cref="RecognitionEngine"/> never calls this overload - it calls
	/// <see cref="TryCollect"/> with the file's own, already-open watch, so this convenience method
	/// cannot silently let a real scan's symbol-table pass run unbounded.
	///
	/// Because this overload runs on <see cref="TimeProvider.System"/> and discards
	/// <see cref="TryCollect"/>'s own <c>bool</c> result, a caller has no way to tell a complete table
	/// from a partial one: on an overloaded machine a real wall-clock delay could in principle trip
	/// the budget mid-enumeration and this method would return a partial <see cref="FileSymbolTable"/>.
	/// It is meant only for small inputs.
	/// </summary>
	public static FileSymbolTable Collect(LexicalProfile profile, string normalizedText, ZoneMask mask)
	{
		MatchBoundWatch matchBoundWatch = new(TimeProvider.System);

		matchBoundWatch.BeginFile();
		TryCollect(profile, normalizedText, mask, matchBoundWatch, out FileSymbolTable table);

		return table;
	}

	/// <summary>
	/// Collects one file's string constant declarations against the file's own match-bound budget.
	/// Built ONCE per file: the expression is the profile's own and runs over the whole text, so
	/// rebuilding it per rule would pay that pass against every rule of every candidate file for an
	/// answer that cannot have changed.
	///
	/// It enumerates through <see cref="TimedRegexEvaluation"/> exactly as a detection rule does -
	/// <c>Match</c>, then <c>NextMatch</c> until none remains or the bound trips - so a file whose
	/// constant-declaration expression alone exhausts the budget is stopped and counted under
	/// <see cref="Reading.SkippedItemCategory.MatchBoundExceeded"/> before a single detection rule
	/// ever runs, on the same terms a rule that exhausted it would be.
	///
	/// The malformed-match guard (<see cref="RecognitionEngine.RecognizeOneRule"/>) is not applied
	/// here: a constant-declaration expression that met the SDK 10.0.201 / runtime 10.0.5
	/// source-generator defect would be caught only by the wall-clock bound above.
	/// </summary>
	internal static bool TryCollect(
		LexicalProfile profile,
		string normalizedText,
		ZoneMask mask,
		MatchBoundWatch matchBoundWatch,
		out FileSymbolTable table)
	{
		ArgumentNullException.ThrowIfNull(profile);
		ArgumentNullException.ThrowIfNull(normalizedText);
		ArgumentNullException.ThrowIfNull(mask);
		ArgumentNullException.ThrowIfNull(matchBoundWatch);

		Dictionary<string, List<Declaration>> declarationsByName = new(StringComparer.Ordinal);
		bool completed = !matchBoundWatch.HasBeenExceeded();
		Match match = completed
			? TimedRegexEvaluation.Match(matchBoundWatch, profile.ConstantDeclarationExpression, normalizedText)
			: Match.Empty;

		while (completed && match.Success)
		{
			RecordDeclaration(match, mask, declarationsByName);
			completed = !matchBoundWatch.HasBeenExceeded();
			match = completed ? TimedRegexEvaluation.NextMatch(matchBoundWatch, match) : Match.Empty;
		}

		table = new FileSymbolTable(declarationsByName);

		return completed;
	}

	/// <summary>
	/// The declared value of a name, where the counting rule says the file settles it, and
	/// false in every one of the three ambiguous cases - which the caller turns into
	/// <see cref="SymbolicVerdict.Indeterminate"/> without having to know which of the three it hit.
	/// </summary>
	public bool TryResolveUnambiguously(string name, int usePosition, out string declaredValue)
	{
		ArgumentNullException.ThrowIfNull(name);

		bool resolved = false;
		declaredValue = string.Empty;

		if (_declarationsByName.TryGetValue(name, out List<Declaration>? declarations)
			&& declarations.Count == 1
			&& declarations[0].Position < usePosition)
		{
			declaredValue = declarations[0].Value;
			resolved = true;
		}

		return resolved;
	}

	/// <summary>
	/// How many declarations of a name the file holds. Exposed because the COUNT is the mechanism -
	/// a reader of this class should be able to see that two declarations produce indeterminacy
	/// without inferring it from a resolution that failed for some unstated reason.
	/// </summary>
	public int DeclarationCountOf(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		return _declarationsByName.TryGetValue(name, out List<Declaration>? declarations)
			? declarations.Count
			: 0;
	}

	private static void RecordDeclaration(
		Match match,
		ZoneMask mask,
		Dictionary<string, List<Declaration>> declarationsByName)
	{
		Group nameGroup = match.Groups[NAME_CAPTURE_GROUP];
		Group valueGroup = match.Groups[VALUE_CAPTURE_GROUP];

		if (nameGroup.Success && valueGroup.Success && LiesInCode(match.Index, mask))
		{
			if (!declarationsByName.TryGetValue(nameGroup.Value, out List<Declaration>? declarations))
			{
				declarations = [];
				declarationsByName[nameGroup.Value] = declarations;
			}

			declarations.Add(new Declaration
			{
				Position = nameGroup.Index,
				Value = valueGroup.Value
			});
		}
	}

	/// <summary>
	/// Whether a declaration's own opening position is executable code. The delimiter zone counts
	/// as code, so this asks the mask's published question rather than
	/// comparing against a single enum value.
	/// </summary>
	private static bool LiesInCode(int position, ZoneMask mask)
	{
		return position >= 0
			&& position < mask.Length
			&& LexicalZones.CountsAsCode(mask.ZoneAt(position));
	}

	/// <summary>
	/// One declaration: where it is, and what it says. The position is what the before-and-after
	/// rule compares against, and it is the NAME's position rather than the match's, so that
	/// leading whitespace an expression happens to consume cannot move it.
	/// </summary>
	private readonly record struct Declaration
	{
		internal required int Position { get; init; }

		internal required string Value { get; init; }
	}
}
