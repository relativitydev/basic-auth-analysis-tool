using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE FILE-SCOPED SYMBOL TABLE, AND IT IS NOT A NAME-KEYED DICTIONARY.
///
/// That distinction is the whole point. A dictionary keyed by name collapses two constants
/// declared in different scopes and then answers "resolved" with no ambiguity signal, so the
/// resolve-downward rule never fires because the engine believes it knows. What is recorded here
/// instead is EVERY declaration of a name, with its position, and resolution succeeds only where
/// that is unambiguous.
///
/// AMBIGUITY IS DECIDED BY COUNTING, NOT BY SCOPE: the engine has a zone mask and nothing
/// else - no block structure, no brace matching, no scope stack.
///
/// | In the analyzed file                                 | Verdict         |
/// |------------------------------------------------------|-----------------|
/// | Exactly one declaration of the name, BEFORE the use   | resolved        |
/// | More than one declaration, anywhere, in any scope     | indeterminate   |
/// | The only declaration positioned AFTER the use         | indeterminate   |
/// | No declaration of the name                            | indeterminate   |
///
/// A file declaring one name in two disjoint scopes therefore returns indeterminate, never a
/// resolved value.
///
/// DECLARATIONS ARE COLLECTED IN CODE ZONES ONLY. A constant written inside a comment or quoted
/// inside a literal is not a declaration, and counting it would let a commented-out line decide a
/// live finding's level.
/// </summary>
public sealed class FileSymbolTable
{
	/// <summary>
	/// The group a profile's constant-declaration expression names its constant in. It is an ENGINE
	/// CONVENTION rather than a contract field, which is worth stating plainly: the analyzer contract
	/// carries the expression and says nothing about its group names, so an expression that omits
	/// this group yields no declarations at all - and therefore an indeterminate verdict, which is
	/// the conservative direction and not a silent wrong answer.
	/// </summary>
	public const string NAME_CAPTURE_GROUP = "name";

	/// <summary>The group the declared value is read from, on the same terms.</summary>
	public const string VALUE_CAPTURE_GROUP = "value";

	/// <summary>
	/// The optional empty group of a member-declaration expression that stands at the first non-blank character of the member's line, so that a quoted key, whose name lies in a string
	/// body, is judged by the code position where the member opens and not by the name. An expression without the group is judged at the name.
	/// </summary>
	public const string MEMBER_OPENING_CAPTURE_GROUP = "opening";

	private readonly Dictionary<string, List<Declaration>> _declarationsByName;

	/// <summary>
	/// The SECOND dictionary: member records (a line-start object-literal member or property assignment), keyed by member name alone and kept apart from the variable records, so the
	/// counting rule, <see cref="DeclarationCountOf"/> and <see cref="TryResolveRecordUnambiguously"/> are those of the variable dictionary and nothing else.
	/// </summary>
	private readonly Dictionary<string, List<Declaration>> _membersByName;

	private FileSymbolTable(Dictionary<string, List<Declaration>> declarationsByName, Dictionary<string, List<Declaration>> membersByName)
	{
		_declarationsByName = declarationsByName;
		_membersByName = membersByName;
	}

	/// <summary>An empty table, which resolves nothing and therefore indeterminates everything.</summary>
	public static FileSymbolTable Empty { get; } = new(new Dictionary<string, List<Declaration>>(StringComparer.Ordinal), new Dictionary<string, List<Declaration>>(StringComparer.Ordinal));

	/// <summary>
	/// Collects one file's string constant declarations, on no match-bound budget of its own - a
	/// convenience for a caller with no per-file bound to charge against, such as an isolated test
	/// exercising symbolic resolution alone. It builds a fresh <see cref="MatchBoundWatch"/> on the
	/// policy bound and the system clock and discards whether that budget was exceeded, so a caller of
	/// this overload cannot tell a complete table from a partial one. <see cref="RecognitionEngine"/> never
	/// calls this overload - it calls <see cref="TryCollect"/> with the file's own, already-open watch.
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
	/// Built ONCE per file: the expression is the profile's own and runs over the whole text.
	///
	/// It enumerates through <see cref="TimedRegexEvaluation"/> exactly as a detection rule does -
	/// <c>Match</c>, then <c>NextMatch</c> until none remains or the bound trips - so a file whose
	/// constant-declaration expression alone exhausts the budget is stopped and counted under
	/// <see cref="Reading.SkippedItemCategory.MatchBoundExceeded"/> before a single detection rule
	/// runs. The malformed-match guard of <see cref="RecognitionEngine.RecognizeOneRule"/> does not
	/// apply here; only the match bound does.
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

		Dictionary<string, List<Declaration>> declarationsByName = new(ComparerFor(profile.IdentifierCaseSensitivity));
		Dictionary<string, List<Declaration>> membersByName = new(ComparerFor(profile.IdentifierCaseSensitivity));
		bool completed = CollectEvery(profile.ConstantDeclarationExpression, normalizedText, matchBoundWatch, declarationsByName, (found, byName) => RecordDeclaration(found, mask, byName));

		if (completed && profile.MemberDeclarationExpression is not null)
		{
			completed = CollectEvery(profile.MemberDeclarationExpression, normalizedText, matchBoundWatch, membersByName, (found, byName) => RecordMember(found, mask, byName));
		}

		table = new FileSymbolTable(declarationsByName, membersByName);

		return completed;
	}

	/// <summary>
	/// Runs one declaration expression over the whole text through the timed door and records every match in the dictionary it is given. False when the file's budget tripped before
	/// the enumeration ended. The variable pass and the member pass are this same enumeration, on the same watch.
	/// </summary>
	private static bool CollectEvery(
		Regex expression,
		string normalizedText,
		MatchBoundWatch matchBoundWatch,
		Dictionary<string, List<Declaration>> recordsByName,
		Action<Match, Dictionary<string, List<Declaration>>> record)
	{
		bool completed = !matchBoundWatch.HasBeenExceeded();
		Match match = completed
			? TimedRegexEvaluation.Match(matchBoundWatch, expression, normalizedText)
			: Match.Empty;

		while (completed && match.Success)
		{
			record(match, recordsByName);
			completed = !matchBoundWatch.HasBeenExceeded();
			match = completed ? TimedRegexEvaluation.NextMatch(matchBoundWatch, match) : Match.Empty;
		}

		return completed;
	}

	/// <summary>The name comparer the profile's identifier case sensitivity selects.</summary>
	private static StringComparer ComparerFor(IdentifierCaseSensitivityMode identifierCaseSensitivity)
	{
		return identifierCaseSensitivity == IdentifierCaseSensitivityMode.Insensitive
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;
	}

	/// <summary>
	/// The declared value of a name, where the counting rule says the file settles it, and
	/// false in every one of the three ambiguous cases - which the caller turns into
	/// <see cref="SymbolicVerdict.Indeterminate"/> without having to know which of the three it hit.
	/// </summary>
	public bool TryResolveUnambiguously(string name, int usePosition, out string declaredValue)
	{
		bool resolved = TryResolveRecordUnambiguously(name, usePosition, out ResolvedSymbol symbol);

		declaredValue = symbol.Value;

		return resolved;
	}

	/// <summary>
	/// The record of a name, with its source kind, on the same counting rule as <see cref="TryResolveUnambiguously"/>: exactly one declaration of the name, of either kind,
	/// positioned before the use. The word of an environment record is the word of its fallback.
	/// </summary>
	public bool TryResolveRecordUnambiguously(string name, int usePosition, out ResolvedSymbol symbol)
	{
		ArgumentNullException.ThrowIfNull(name);

		bool resolved = false;
		symbol = new ResolvedSymbol(string.Empty, SymbolSourceKind.Literal);

		if (_declarationsByName.TryGetValue(name, out List<Declaration>? declarations)
			&& declarations.Count == 1
			&& declarations[0].Position < usePosition)
		{
			symbol = new ResolvedSymbol(declarations[0].Value, declarations[0].SourceKind);
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

	/// <summary>
	/// The member record of a name, ORDER-FREE: it resolves wherever the file assigns the member, before the use or after it. Exactly one collected record of the name resolves, and
	/// only when that record is an environment read; no record, two or more records (of any form: the table does not know which object a member belongs to), or one record that is
	/// not an environment read are all indeterminate. The word of the record is the word of its fallback.
	/// </summary>
	public bool TryResolveMemberRecordUnambiguously(string name, out ResolvedSymbol symbol)
	{
		ArgumentNullException.ThrowIfNull(name);

		bool resolved = false;
		symbol = new ResolvedSymbol(string.Empty, SymbolSourceKind.Literal);

		if (_membersByName.TryGetValue(name, out List<Declaration>? members)
			&& members.Count == 1
			&& members[0].SourceKind == SymbolSourceKind.Environment)
		{
			symbol = new ResolvedSymbol(members[0].Value, members[0].SourceKind);
			resolved = true;
		}

		return resolved;
	}

	/// <summary>How many member records of a name the file holds, of every form. The count is the mechanism, as it is for <see cref="DeclarationCountOf"/>.</summary>
	public int MemberRecordCountOf(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		return _membersByName.TryGetValue(name, out List<Declaration>? members)
			? members.Count
			: 0;
	}

	/// <summary>
	/// One member record: collected only in code, at the position of the member name. The expression collects a member of any value form and marks an environment read with the empty
	/// group <see cref="EnvironmentDeclarationGroups.MARKER"/>; a record that is not an environment read is a literal-kind record with an empty word.
	/// </summary>
	private static void RecordMember(
		Match match,
		ZoneMask mask,
		Dictionary<string, List<Declaration>> membersByName)
	{
		Group nameGroup = match.Groups[NAME_CAPTURE_GROUP];
		Group openingGroup = match.Groups[MEMBER_OPENING_CAPTURE_GROUP];
		bool isEnvironment = match.Groups[EnvironmentDeclarationGroups.MARKER].Success;
		int openingPosition = openingGroup.Success ? openingGroup.Index : nameGroup.Index;

		if (nameGroup.Success && LiesInCode(openingPosition, mask))
		{
			if (!membersByName.TryGetValue(nameGroup.Value, out List<Declaration>? members))
			{
				members = [];
				membersByName[nameGroup.Value] = members;
			}

			members.Add(new Declaration
			{
				Position = nameGroup.Index,
				Value = isEnvironment ? match.Groups[EnvironmentDeclarationGroups.FALLBACK_WORD].Value : string.Empty,
				SourceKind = isEnvironment ? SymbolSourceKind.Environment : SymbolSourceKind.Literal
			});
		}
	}

	private static void RecordDeclaration(
		Match match,
		ZoneMask mask,
		Dictionary<string, List<Declaration>> declarationsByName)
	{
		Group nameGroup = match.Groups[NAME_CAPTURE_GROUP];
		Group valueGroup = match.Groups[VALUE_CAPTURE_GROUP];
		bool isEnvironment = match.Groups[EnvironmentDeclarationGroups.MARKER].Success;

		if (nameGroup.Success && (valueGroup.Success || isEnvironment) && LiesInCode(match.Index, mask))
		{
			if (!declarationsByName.TryGetValue(nameGroup.Value, out List<Declaration>? declarations))
			{
				declarations = [];
				declarationsByName[nameGroup.Value] = declarations;
			}

			declarations.Add(new Declaration
			{
				Position = nameGroup.Index,
				Value = isEnvironment ? match.Groups[EnvironmentDeclarationGroups.FALLBACK_WORD].Value : valueGroup.Value,
				SourceKind = isEnvironment ? SymbolSourceKind.Environment : SymbolSourceKind.Literal
			});
		}
	}

	/// <summary>
	/// Whether a declaration's own opening position is executable code. The delimiter zone counts
	/// as code by the scanner's own statement, so this asks the mask's published question rather than
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

		/// <summary>Whether the declaration is a string constant, which is every record an expression without the environment marker produces, or an environment read.</summary>
		internal SymbolSourceKind SourceKind { get; init; }
	}
}

/// <summary>The record a name resolves to: its word and where it came from.</summary>
public readonly record struct ResolvedSymbol(string Value, SymbolSourceKind SourceKind);
