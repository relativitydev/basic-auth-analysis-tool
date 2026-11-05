using System.Collections.Immutable;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SHAPES OF FOUR SHIPPED EXPRESSIONS AT THEIR COSTLIEST, for the shipped `TypeScript` cost gate, taken into <see cref="TypeScriptShippedCostGateStressSet"/> and used by the shipped gate only: <c>EX-TS-001</c> reads
/// any value form and shorthand members, with four type look-behind and look-ahead alternatives reached on every <c>auth: {</c>; <c>EX-TS-003</c> and <c>EX-JS-001</c> read shorthand members; and <c>EX-JS-005</c> reads a
/// split pair. Each shape below stands one of those costs at its worst: no line over the line ceiling and no file over the file ceiling.
///
/// Every shape is deterministic, legal for the reader and invented: placeholders and generic names, no fragment of any real text.
/// </summary>
internal static class TypeScriptC4StressShapes
{
	private const int _MEMBER_WINDOW_UNITS = 1_500;
	private const int _MEMBER_WINDOW_UNITS_PAST_THE_BOUND = 1_520;
	private const int _SPLIT_PAIR_NAME_LENGTH = 100;
	private const int _SPLIT_PAIR_NAME_LENGTH_PAST_THE_BOUND = 101;
	private const char _FILLER_UNIT = 'x';
	private const string _UNTYPED_AUTH_OBJECT = "auth: { username: x, password: y }, ";
	private const string _UNTYPED_AUTH_OBJECT_LINE = "const options = { auth: { username: x, password: y }, timeout: 5 };\n";
	private const string _NAME_WINDOW_UNIT = "a,";
	private const string _SHORTHAND_AUTH_OBJECT = "auth: { username, password }, ";
	private const string _SHORTHAND_AUTH_OBJECT_COMPACT = "auth:{username,password},";
	private const string _SHORTHAND_USER_AFTER_A_COMMA = "{ a, username, ";
	private const string _SHORTHAND_PASSWORD_AFTER_A_COMMA = "auth: { a, b, password, ";
	private const string _VALUE_OF_ANOTHER_KEY_NEAR_MISS = "auth: { label: username, other: password }, ";
	private const string _VALUE_OF_ANOTHER_KEY_NEAR_MISS_THEN_SHORTHAND_USER = "auth: { label: username, password, ";
	private const string _UNCLOSED_AUTH_OPENER = "auth:{";
	private const string _UNCLOSED_TYPED_AUTH_OPENER = "auth: <AxiosBasicCredentials> {";
	private const string _TYPED_CONFIGURATION_OPENER = "const settings: AxiosRequestConfig = { auth: { username, password } };\n";
	private const string _JS_001_SHORTHAND_MEMBER = "axios.get(u,{auth:{username,password}});";
	private const string _JS_001_DEFAULTS_SHORTHAND_MEMBER = "axios.defaults.auth={username,password};";
	private const string _JS_001_SHORTHAND_NEAR_MISS = "axios.get(u,{auth:{usernames,passwords}});";
	private const string _JS_005_SPLIT_PAIR = "btoa('user:' + 'pw');";
	private const string _JS_005_SPLIT_PAIR_DOUBLE_QUOTED = "btoa(\"user:\"+\"pw\");";
	private const string _JS_005_SPLIT_PAIR_NEAR_MISS = "btoa('user:' + '');";
	private const string _JS_005_OPENER = "btoa(";
	private const string _TS_003_THIS_RECEIVER = "this.httpService.get(u,{auth:{username,password}});";
	private const string _TS_003_GENERIC_RECEIVER = "apiHTTP.post<T>(u,{auth:{username,password}});";
	private const string _TS_003_RECEIVER_WITHOUT_AUTH = "this.httpService.get<Array<Row>>(u,{params:{username,password}});";
	private const string _TS_003_RECEIVER_UNCLOSED = "this.httpService.post(u,{ auth: { username, ";

	/// <summary>The shapes of this set: every one's name begins with <see cref="TypeScriptShippedCostGateStressSet.SHAPE_NAME_PREFIX"/>.</summary>
	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		AddTheUntypedObjectShapes(shapes);
		AddTheMemberWindowShapes(shapes);
		AddTheShorthandShapes(shapes);
		AddTheUnclosedOpenerShapes(shapes);
		AddTheSharedExpressionShapes(shapes);
		AddTheHttpServiceShapes(shapes);

		return shapes.ToImmutable();
	}

	/// <summary>(a) Many untyped credential objects: every <c>auth: {</c> passes both member look-aheads and then fails the four type alternatives, once per object, on one long line and on many short ones.</summary>
	private static void AddTheUntypedObjectShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("c4-a-untyped-auth-objects-on-long-lines", TypeScriptShippedCostGateStressSet.LongLines(_UNTYPED_AUTH_OBJECT)));
		shapes.Add(Shape("c4-a-untyped-auth-objects-one-per-line-to-the-ceiling", TypeScriptShippedCostGateStressSet.FillToCeilingWithLines(_UNTYPED_AUTH_OBJECT_LINE)));
		shapes.Add(Shape("c4-a-typed-configuration-with-shorthand-members-one-per-line", TypeScriptShippedCostGateStressSet.FillToCeilingWithLines(_TYPED_CONFIGURATION_OPENER)));
	}

	/// <summary>(b) Long single lines where each member window of <c>EX-TS-001</c> runs to its 1,500-unit bound, with the member found only past it, and with no member at all.</summary>
	private static void AddTheMemberWindowShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		string fillerAtTheBound = new(_FILLER_UNIT, _MEMBER_WINDOW_UNITS);
		string fillerPastTheBound = new(_FILLER_UNIT, _MEMBER_WINDOW_UNITS_PAST_THE_BOUND);
		string namesAtTheBound = string.Concat(Enumerable.Repeat(_NAME_WINDOW_UNIT, _MEMBER_WINDOW_UNITS / _NAME_WINDOW_UNIT.Length));

		shapes.Add(Shape("c4-b-member-window-at-the-bound-without-a-member", TypeScriptShippedCostGateStressSet.LongLines($"auth: {{ {fillerAtTheBound} ")));
		shapes.Add(Shape("c4-b-member-window-past-the-bound-without-a-member", TypeScriptShippedCostGateStressSet.LongLines($"auth: {{ {fillerPastTheBound} ")));
		shapes.Add(Shape("c4-b-member-window-of-names-and-commas-at-the-bound", TypeScriptShippedCostGateStressSet.LongLines($"auth: {{ {namesAtTheBound} ")));
		shapes.Add(Shape("c4-b-user-member-at-the-end-of-the-window-and-no-password", TypeScriptShippedCostGateStressSet.LongLines($"auth: {{ {namesAtTheBound} username, ")));
		shapes.Add(Shape("c4-b-both-members-past-the-window-then-no-type", TypeScriptShippedCostGateStressSet.LongLines($"auth: {{ {fillerPastTheBound}, username, password }}; ")));
	}

	/// <summary>(c) Shorthand credential members in runs, shorthand members after a comma, and the near-misses where the name is the value of another key.</summary>
	private static void AddTheShorthandShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("c4-c-shorthand-auth-objects", TypeScriptShippedCostGateStressSet.LongLines(_SHORTHAND_AUTH_OBJECT)));
		shapes.Add(Shape("c4-c-shorthand-auth-objects-compact", TypeScriptShippedCostGateStressSet.LongLines(_SHORTHAND_AUTH_OBJECT_COMPACT)));
		shapes.Add(Shape("c4-c-shorthand-user-members-after-a-comma", TypeScriptShippedCostGateStressSet.LongLines(_SHORTHAND_USER_AFTER_A_COMMA)));
		shapes.Add(Shape("c4-c-shorthand-password-members-after-a-comma", TypeScriptShippedCostGateStressSet.LongLines(_SHORTHAND_PASSWORD_AFTER_A_COMMA)));
		shapes.Add(Shape("c4-c-name-as-the-value-of-another-key-near-misses", TypeScriptShippedCostGateStressSet.LongLines(_VALUE_OF_ANOTHER_KEY_NEAR_MISS)));
		shapes.Add(Shape("c4-c-name-as-the-value-of-another-key-then-shorthand-user", TypeScriptShippedCostGateStressSet.LongLines(_VALUE_OF_ANOTHER_KEY_NEAR_MISS_THEN_SHORTHAND_USER)));
	}

	/// <summary>(d) The opener <c>auth:{</c> repeated on one line with no closing brace, so each opener's windows run over the openers after it; plain and with the angle-bracket assertion.</summary>
	private static void AddTheUnclosedOpenerShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("c4-d-unclosed-auth-openers-on-one-line", TypeScriptShippedCostGateStressSet.LongLines(_UNCLOSED_AUTH_OPENER)));
		shapes.Add(Shape("c4-d-unclosed-typed-auth-openers-on-one-line", TypeScriptShippedCostGateStressSet.LongLines(_UNCLOSED_TYPED_AUTH_OPENER)));
		shapes.Add(Shape("c4-d-unclosed-auth-openers-with-shorthand-user", TypeScriptShippedCostGateStressSet.LongLines($"{_UNCLOSED_AUTH_OPENER}username,")));
	}

	/// <summary>(e) The shared expressions read in `TypeScript` text: the shorthand member of <c>EX-JS-001</c> and the split pair of <c>EX-JS-005</c>, repeated densely, and the split pair's name past its bound.</summary>
	private static void AddTheSharedExpressionShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		string nameAtTheBound = new(_FILLER_UNIT, _SPLIT_PAIR_NAME_LENGTH);
		string nameJustPastTheBound = new(_FILLER_UNIT, _SPLIT_PAIR_NAME_LENGTH_PAST_THE_BOUND);

		shapes.Add(Shape("c4-e-js-001-shorthand-members", TypeScriptShippedCostGateStressSet.LongLines(_JS_001_SHORTHAND_MEMBER)));
		shapes.Add(Shape("c4-e-js-001-defaults-shorthand-members", TypeScriptShippedCostGateStressSet.LongLines(_JS_001_DEFAULTS_SHORTHAND_MEMBER)));
		shapes.Add(Shape("c4-e-js-001-shorthand-near-misses", TypeScriptShippedCostGateStressSet.LongLines(_JS_001_SHORTHAND_NEAR_MISS)));
		shapes.Add(Shape("c4-e-js-005-split-pairs", TypeScriptShippedCostGateStressSet.LongLines(_JS_005_SPLIT_PAIR)));
		shapes.Add(Shape("c4-e-js-005-split-pairs-double-quoted", TypeScriptShippedCostGateStressSet.LongLines(_JS_005_SPLIT_PAIR_DOUBLE_QUOTED)));
		shapes.Add(Shape("c4-e-js-005-split-pair-near-misses", TypeScriptShippedCostGateStressSet.LongLines(_JS_005_SPLIT_PAIR_NEAR_MISS)));
		shapes.Add(Shape("c4-e-js-005-split-pair-name-at-the-bound", TypeScriptShippedCostGateStressSet.LongLines($"{_JS_005_OPENER}'{nameAtTheBound}:' + 'pw');")));
		shapes.Add(Shape("c4-e-js-005-split-pair-name-past-the-bound", TypeScriptShippedCostGateStressSet.LongLines($"{_JS_005_OPENER}'{nameJustPastTheBound}:' + 'pw');")));
		shapes.Add(Shape("c4-e-js-005-openers-without-a-pair", TypeScriptShippedCostGateStressSet.LongLines(_JS_005_OPENER)));
	}

	/// <summary>(f) The receivers of <c>EX-TS-003</c> with shorthand members, repeated, and the receiver whose options object holds no auth key and the one that is never closed.</summary>
	private static void AddTheHttpServiceShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("c4-f-ts-003-this-receivers-with-shorthand-members", TypeScriptShippedCostGateStressSet.LongLines(_TS_003_THIS_RECEIVER)));
		shapes.Add(Shape("c4-f-ts-003-generic-receivers-with-shorthand-members", TypeScriptShippedCostGateStressSet.LongLines(_TS_003_GENERIC_RECEIVER)));
		shapes.Add(Shape("c4-f-ts-003-receivers-without-an-auth-key", TypeScriptShippedCostGateStressSet.LongLines(_TS_003_RECEIVER_WITHOUT_AUTH)));
		shapes.Add(Shape("c4-f-ts-003-unclosed-receivers-with-shorthand-user", TypeScriptShippedCostGateStressSet.LongLines(_TS_003_RECEIVER_UNCLOSED)));
	}

	private static MatchBoundStressShape Shape(string slug, string text)
	{
		return TypeScriptShippedCostGateStressSet.Shape(slug, text, TypeScriptMatchBoundStressSet.FILE_EXTENSION);
	}
}
