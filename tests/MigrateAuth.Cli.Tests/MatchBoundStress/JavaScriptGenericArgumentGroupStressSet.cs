using System.Collections.Immutable;
using System.Text;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE ADVERSARIAL TEXTS OF THE GENERIC-ARGUMENT GROUP OF THE CALLEE EXPRESSIONS OF `JS-001`, `JS-003`, `JS-015` AND `JS-016`. Each shape repeats one adversarial line, at no more
/// than the line ceiling, until the text reaches the size asked for: a list that opens and never closes, a list one unit past the bound at each level, every level at the bound with no parenthesis after it,
/// openings nested past depth three, arrow units to the bound and one past it, comparison chains, anchors with no continuation, and the hot path in which the group matches and the window continues. The size is a
/// parameter: the stress test (no clock) runs a small one, the timing gate the file ceiling. Every text is invented from generic names and carries nothing of any repository.
/// </summary>
internal static class JavaScriptGenericArgumentGroupStressSet
{
	/// <summary>The size of one text for the stress test, which asserts no clock.</summary>
	internal const int STRESS_TEXT_CHARACTERS = 120_000;

	/// <summary>The size of one text for the timing gate: the file ceiling of two million characters.</summary>
	internal const int FILE_CEILING_CHARACTERS = 2_000_000;

	private const int _LINE_CEILING = 9_900;
	private const int _UNITS_PER_LEVEL = 120;
	private const int _ONE_UNIT = 1;
	private const char _FILLER = 'x';
	private const string _CLIENT_TAIL = "(u,{auth:{username:a,password:b}});";
	private const string _URL_TAIL = "('https://exampleUser:exampleSecret@host.example/a');";
	private const string _GOT_TAIL = "(u,{username:a,password:b});";
	private const string _REQUEST_TAIL = "(u,{auth:{user:a,pass:b,sendImmediately:false}});";
	private const string _ARROW = "=>";
	private const int _BLANKS_BEFORE_THE_END = 60;
	private const int _MINIMUM_REPETITIONS = 1;
	private const char _LINE_FEED = '\n';

	internal sealed record Shape(string Name, string Text);

	internal static ImmutableArray<Shape> Build(int totalCharacters)
	{
		string levelOne = new(_FILLER, _UNITS_PER_LEVEL);
		string levelTwo = $"{new string(_FILLER, _UNITS_PER_LEVEL - _ONE_UNIT)}<{levelOne}>";
		string levelThree = $"{new string(_FILLER, _UNITS_PER_LEVEL - _ONE_UNIT)}<{levelTwo}>";
		string pastTheBound = new(_FILLER, _UNITS_PER_LEVEL + _ONE_UNIT);
		string arrowsAtTheBound = Repeat(_ARROW, _UNITS_PER_LEVEL / _ARROW.Length);
		string arrowsPastTheBound = Repeat(_ARROW, (_UNITS_PER_LEVEL / _ARROW.Length) + _ONE_UNIT);
		string openingBrackets = new('<', _LINE_CEILING);
		string closingBrackets = new('>', _LINE_CEILING);
		string longWord = new(_FILLER, _LINE_CEILING);
		string blanks = new(' ', _BLANKS_BEFORE_THE_END);

		return
		[
			Fill("group-opened-at-the-bound-and-never-closed", $"axios.get<{levelOne} ", totalCharacters),
			Fill("group-one-unit-past-the-bound-then-the-call", $"axios.get<{pastTheBound}>{_CLIENT_TAIL}", totalCharacters),
			Fill("every-level-at-the-bound-then-no-parenthesis", $"request<{levelThree}> ;", totalCharacters),
			Fill("every-level-at-the-bound-then-the-call-of-each-class", $"axios.get<{levelThree}>{_CLIENT_TAIL}fetch<{levelThree}>{_URL_TAIL}got<{levelThree}>{_GOT_TAIL}request<{levelThree}>{_REQUEST_TAIL}", totalCharacters),
			Fill("level-two-one-unit-past-the-bound", $"axios.get<A<{levelOne}<B>>>{_CLIENT_TAIL}", totalCharacters),
			Fill("openings-nested-past-depth-three-never-closed", "axios.get<A<B<C<D<E<F<", totalCharacters),
			Fill("depth-four-then-the-call", $"axios.get<A<B<C<D>>>>{_CLIENT_TAIL}", totalCharacters),
			Fill("arrow-units-at-the-bound", $"axios.get<{arrowsAtTheBound}>{_CLIENT_TAIL}", totalCharacters),
			Fill("arrow-units-one-past-the-bound", $"axios.get<{arrowsPastTheBound}>{_CLIENT_TAIL}", totalCharacters),
			Fill("comparison-chains", "if(a<b&&c>(d)){e=f<g>(h);request<a>(b);}", totalCharacters),
			Fill("callee-and-list-with-no-continuation", "axios<A>got<A>request<A>fetch<A>", totalCharacters),
			Fill("callee-then-one-line-of-opening-brackets", $"axios.get{openingBrackets}", totalCharacters),
			Fill("callee-then-one-line-of-closing-brackets", $"axios.get<{closingBrackets}", totalCharacters),
			Fill("callee-then-one-long-word-and-no-close", $"axios.get<{longWord}", totalCharacters),
			Fill("group-then-blank-then-no-parenthesis", $"request<A<B>>{blanks}x;", totalCharacters),
			Fill("the-call-of-each-class-with-a-group-within-depth-three", $"axios.create<A<B<C>>>{_CLIENT_TAIL}fetch<A<B>>{_URL_TAIL}got.post<A>{_GOT_TAIL}request.get<A>{_REQUEST_TAIL}", totalCharacters)
		];
	}

	private static Shape Fill(string name, string line, int totalCharacters)
	{
		StringBuilder text = new();
		string fitted = line.Length > _LINE_CEILING ? line : Repeat(line, Math.Max(_MINIMUM_REPETITIONS, _LINE_CEILING / line.Length));

		while (text.Length < totalCharacters)
		{
			text.Append(fitted).Append(_LINE_FEED);
		}

		return new Shape(name, text.ToString());
	}

	private static string Repeat(string unit, int count)
	{
		StringBuilder repeated = new(unit.Length * count);

		for (int index = 0; index < count; index++)
		{
			repeated.Append(unit);
		}

		return repeated.ToString();
	}
}
