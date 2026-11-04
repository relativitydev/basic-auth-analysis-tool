using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE STRESS SET OF THE SHIPPED `TypeScript` COST GATE: every shape of <see cref="TypeScriptMatchBoundStressSet"/> (lines at the 10,000-character ceiling with long annotations
/// and unions, generic-heavy declarations, decorator chains, `.tsx` with nested templates, ten thousand lines of <c>x!/y</c> for the backward predicate, anchor-dense declaration-file shapes up to the 2 MB ceiling with
/// deep nesting, and the shapes of the generic-argument group and of the tail refusal), taken as they stand and not copied, and beside them the shapes that set does not carry and the shipped analyzer needs.
///
/// WHAT THE SHIPPED GATE ADDS. The per-expression set stands every generic-argument-group shape on the `axios` callee and every tail-refusal shape on the `Authorization` key of a bare object. The shipped expressions are the shared `JavaScript` ones
/// that carry the group and the refusal (four of them take the optional group after the callee; one takes the refusal at the object-key carrier), so the added shapes stand the group at its length
/// bound, past it and unclosed on EVERY callee those four expressions read, and the refusal on every tail it refuses and on the forms it declines, each at the line ceiling. It also carries the two extensions the per-expression
/// set does not stand a shape under (`.mts` and `.cts`), the backward predicate's ten thousand lines in a `.tsx` file and with doubled marks, and declaration-file shapes at nesting depths past that set's.
///
/// Every shape is deterministic, legal for the reader (under the file ceiling, no line over the line ceiling) and invented: placeholders and generic names, no fragment of any real text.
/// </summary>
internal static class TypeScriptShippedCostGateStressSet
{
	internal const string MODULE_FILE_EXTENSION = ".mts";
	internal const string COMMONJS_FILE_EXTENSION = ".cts";

	internal const string SHAPE_NAME_PREFIX = "ts-shipped-";

	private const int _BANG_SLASH_LINES = 10_000;
	private const int _GENERIC_BODY_AT_THE_LENGTH_BOUND = 120;
	private const int _GENERIC_BODY_PAST_THE_LENGTH_BOUND = 121;
	private const int _GENERIC_BODY_JUST_UNDER_THE_LENGTH_BOUND = 119;
	private const int _REFUSAL_BODY_AT_THE_BOUND = 300;
	private const int _REFUSAL_BODY_PAST_THE_BOUND = 301;
	private const int _VERY_DEEP_DECLARATION_NESTING_DEPTH = 120;
	private const int _MODULE_FILE_NESTING_DIVISOR = 4;
	private const int _BUILDER_SLACK_IN_CHARACTERS = 4096;
	private const char _FILLER_UNIT = 'x';
	private const string _LINE_BREAK = "\n";
	private const string _BANG_SLASH_LINE_FORMAT = "const quotient{0:D5}: number = value!/scale;\n";
	private const string _DOUBLE_BANG_SLASH_LINE_FORMAT = "const quotient{0:D5}: number = value!!/scale!!/2;\n";
	private const string _MARKUP_BANG_SLASH_LINE_FORMAT = "const cell{0:D5} = <td className={{`c ${{n!/d}}`}}>{{n!/d}}</td>;\n";
	private const string _ORDINARY_MODULE_LINE = "export function total(first: number, second: number): number { return first + second; }\n";
	private const string _ORDINARY_COMMONJS_LINE = "const settings: Record<string, Array<string>> = require('./settings') as Record<string, Array<string>>;\n";
	private const string _MARKUP_GENERIC_LINE = "const pick = <T,>(items: Array<T>, index: number): T => items[index]!; const view = <List<Item> items={rows} render={(row: Row<Item>) => <li>{row.name}</li>} />;\n";
	private const string _DECLARATION_OVERLOAD_GROUP =
		"export declare function read(source: string | Buffer | URL, options?: { encoding: 'utf8' | 'latin1' | 'ascii' }): Promise<string | Buffer>; ";

	/// <summary>The callees the four amended expressions read, each with the text that opens its call.</summary>
	private static readonly ImmutableArray<(string Slug, string Callee, string Arguments)> _AMENDED_CALLEES =
	[
		("axios-get", "axios.get", "(u,{auth:{username:a,password:b}});"),
		("axios-request", "axios.request", "({auth:{username:a,password:b}});"),
		("request-get", "request.get", "(u,{auth:{user:a,pass:b,sendImmediately:false}});"),
		("got-post", "got.post", "(u,{username:a,password:b});"),
		("got", "got", "(u,{username:a,password:b});"),
		("fetch", "fetch", "('https://u:p@h.example/a');"),
		("new-request", "new Request", "(u,{headers:{Authorization:'Basic '+t}});"),
		("http-service-post", "this.httpService.post", "(u,{auth:{username:a,password:b}});")
	];

	/// <summary>The tails the refusal reads at the object-key carrier, and the forms it declines, each one object member.</summary>
	private static readonly ImmutableArray<(string Slug, string Member)> _REFUSAL_TAILS =
	[
		("literal-then-semicolon", "type H={Authorization:'Basic';};"),
		("literal-then-union", "type U={Authorization:'Basic'|'Bearer'};"),
		("literal-then-union-after-blanks", "type U={Authorization:'Basic'   |   'Bearer'};"),
		("template-with-a-string-hole", "type T={Authorization:`Basic ${string}`};"),
		("template-with-a-number-hole", "type T={Authorization:`Basic ${number}`};"),
		("template-whose-hole-only-starts-with-the-word", "const o={Authorization:`Basic ${stringValue}`};"),
		("literal-then-comma-declined", "const o={Authorization:'Basic dGVzdDp0ZXN0',Accept:'a'};"),
		("literal-then-double-bar-declined", "const o={Authorization:'Basic dGVzdDp0ZXN0'||x};"),
		("environment-with-a-basic-fallback", "const o={Authorization:process.env.AUTH_VALUE??'Basic dGVzdDp0ZXN0'};"),
		("environment-with-a-typescript-tail", "const o={Authorization:process.env.AUTH_VALUE! || 'Basic'};")
	];

	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		ImmutableArray<MatchBoundStressShape> shapes = [.. TypeScriptMatchBoundStressSet.Build(), .. BuildTheAddedShapes()];

		return shapes;
	}

	/// <summary>The shapes the shipped gate adds to the per-expression set: every one's name begins with <see cref="SHAPE_NAME_PREFIX"/>.</summary>
	internal static ImmutableArray<MatchBoundStressShape> BuildTheAddedShapes()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		AddEveryCalleeOfTheGenericArgumentGroup(shapes);
		AddEveryTailOfTheRefusal(shapes);
		AddTheExtensionShapes(shapes);
		AddTheBackwardPredicateShapes(shapes);
		AddTheDeepDeclarationShapes(shapes);
		AddTheMemberRecordShapes(shapes);

		shapes.AddRange(TypeScriptC4StressShapes.Build());

		return shapes.ToImmutable();
	}

	internal static string RelativePathOf(MatchBoundStressShape shape)
	{
		return TypeScriptMatchBoundStressSet.RelativePathOf(shape);
	}

	/// <summary>The group at the length bound, one unit past it, and unclosed just under it, on each callee the amended expressions read, each repeated to the line ceiling.</summary>
	private static void AddEveryCalleeOfTheGenericArgumentGroup(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		string atTheBound = new(_FILLER_UNIT, _GENERIC_BODY_AT_THE_LENGTH_BOUND);
		string pastTheBound = new(_FILLER_UNIT, _GENERIC_BODY_PAST_THE_LENGTH_BOUND);
		string justUnderTheBound = new(_FILLER_UNIT, _GENERIC_BODY_JUST_UNDER_THE_LENGTH_BOUND);

		foreach ((string slug, string callee, string arguments) in _AMENDED_CALLEES)
		{
			shapes.Add(Shape($"callee-{slug}-group-at-the-length-bound", LongLines($"{callee}<{atTheBound}>{arguments}"), TypeScriptMatchBoundStressSet.FILE_EXTENSION));
			shapes.Add(Shape($"callee-{slug}-group-past-the-length-bound", LongLines($"{callee}<{pastTheBound}>{arguments}"), TypeScriptMatchBoundStressSet.FILE_EXTENSION));
			shapes.Add(Shape($"callee-{slug}-group-unclosed-under-the-length-bound", LongLines($"{callee}<{justUnderTheBound} "), TypeScriptMatchBoundStressSet.FILE_EXTENSION));
		}
	}

	/// <summary>Each tail the refusal reads, and each form it declines, repeated to the line ceiling; and the refusal body at, and one character past, its bound.</summary>
	private static void AddEveryTailOfTheRefusal(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string member) in _REFUSAL_TAILS)
		{
			shapes.Add(Shape($"refusal-{slug}", LongLines(member), TypeScriptMatchBoundStressSet.FILE_EXTENSION));
		}

		shapes.Add(Shape("refusal-body-at-the-bound-then-semicolon", LongLines($"{{Authorization:'Basic {new string(_FILLER_UNIT, _REFUSAL_BODY_AT_THE_BOUND - 1)}';}}"), TypeScriptMatchBoundStressSet.FILE_EXTENSION));
		shapes.Add(Shape("refusal-body-past-the-bound-then-semicolon", LongLines($"{{Authorization:'Basic {new string(_FILLER_UNIT, _REFUSAL_BODY_PAST_THE_BOUND)}';}}"), TypeScriptMatchBoundStressSet.FILE_EXTENSION));
		shapes.Add(Shape("refusal-body-at-the-bound-then-union", LongLines($"{{Authorization:'Basic {new string(_FILLER_UNIT, _REFUSAL_BODY_AT_THE_BOUND - 1)}'|'Bearer'}}"), TypeScriptMatchBoundStressSet.FILE_EXTENSION));
	}

	/// <summary>The `.mts` and `.cts` extensions, which the per-expression set does not stand a shape under, and a `.tsx` file of generic arrow functions and generic elements.</summary>
	private static void AddTheExtensionShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("module-file-ordinary-code-at-the-ceiling", FillToCeilingWithLines(_ORDINARY_MODULE_LINE), MODULE_FILE_EXTENSION));
		shapes.Add(Shape("module-file-long-line-annotations-and-unions", LongLines("x: 'a' | 'b' | Array<'c' | 'd'>, y: Map<string, 'e' | 'f'>, ", "function f(", "): void {}"), MODULE_FILE_EXTENSION));
		shapes.Add(Shape("commonjs-file-ordinary-code-at-the-ceiling", FillToCeilingWithLines(_ORDINARY_COMMONJS_LINE), COMMONJS_FILE_EXTENSION));
		shapes.Add(Shape("commonjs-file-long-line-decorator-chains", LongLines("@A() @B({a:1,b:'x'}) @C('x',['y']) @D "), COMMONJS_FILE_EXTENSION));
		shapes.Add(Shape("markup-file-generic-arrows-and-elements", FillToCeilingWithLines(_MARKUP_GENERIC_LINE), TypeScriptMatchBoundStressSet.MARKUP_FILE_EXTENSION));
		shapes.Add(Shape("markup-file-long-line-nested-templates", LongLines("`a${`b${`c${d}`}`}`+"), TypeScriptMatchBoundStressSet.MARKUP_FILE_EXTENSION));
	}

	/// <summary>Ten thousand lines of the non-null mark before a slash, in a `.tsx` file, with marks doubled, and in markup, so the backward predicate decides every slash.</summary>
	private static void AddTheBackwardPredicateShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("ten-thousand-lines-of-non-null-assertion-then-slash-in-a-markup-file", NumberedLines(_BANG_SLASH_LINE_FORMAT), TypeScriptMatchBoundStressSet.MARKUP_FILE_EXTENSION));
		shapes.Add(Shape("ten-thousand-lines-of-doubled-non-null-assertion-then-slash", NumberedLines(_DOUBLE_BANG_SLASH_LINE_FORMAT), TypeScriptMatchBoundStressSet.FILE_EXTENSION));
		shapes.Add(Shape("ten-thousand-lines-of-non-null-assertion-then-slash-inside-markup", NumberedLines(_MARKUP_BANG_SLASH_LINE_FORMAT), TypeScriptMatchBoundStressSet.MARKUP_FILE_EXTENSION));
		shapes.Add(Shape("ten-thousand-lines-of-non-null-assertion-then-slash-in-a-module-file", NumberedLines(_BANG_SLASH_LINE_FORMAT), MODULE_FILE_EXTENSION));
	}

	/// <summary>Declaration-file shapes past the per-expression set's nesting depth, and overload groups of unions at the line ceiling.</summary>
	private static void AddTheDeepDeclarationShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("declaration-file-namespaces-nested-to-depth-one-hundred-twenty", FillToCeilingWithLines(NestedNamespaces(_VERY_DEEP_DECLARATION_NESTING_DEPTH)), TypeScriptMatchBoundStressSet.DECLARATION_FILE_EXTENSION));
		shapes.Add(Shape("declaration-file-overload-groups-at-the-line-ceiling", LongLines(_DECLARATION_OVERLOAD_GROUP), TypeScriptMatchBoundStressSet.DECLARATION_FILE_EXTENSION));
		shapes.Add(Shape("module-file-namespaces-nested-to-depth-thirty", FillToCeilingWithLines(NestedNamespaces(_VERY_DEEP_DECLARATION_NESTING_DEPTH / _MODULE_FILE_NESTING_DIVISOR)), MODULE_FILE_EXTENSION));
	}

	/// <summary>The member pass: a file with the word of the member name on one line in five, with every line naming one member and with each line naming its own.</summary>
	private static void AddTheMemberRecordShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape(MemberRecordStressShapes.SAME_NAME_SHAPE_NAME, MemberRecordStressShapes.SameNameText(), TypeScriptMatchBoundStressSet.FILE_EXTENSION));
		shapes.Add(Shape(MemberRecordStressShapes.DISTINCT_NAMES_SHAPE_NAME, MemberRecordStressShapes.DistinctNamesText(), TypeScriptMatchBoundStressSet.FILE_EXTENSION));
	}

	internal static MatchBoundStressShape Shape(string slug, string text, string extension)
	{
		return new MatchBoundStressShape { Name = $"{SHAPE_NAME_PREFIX}{slug}", Text = text, FileExtension = extension };
	}

	/// <summary>Namespaces nested to the given depth with an anchor-dense member at the innermost level, every level closed.</summary>
	private static string NestedNamespaces(int depth)
	{
		StringBuilder block = new();

		for (int level = 0; level < depth; level++)
		{
			block.Append('\t', level).Append(level == 0 ? "declare namespace" : "namespace").Append(string.Format(CultureInfo.InvariantCulture, " Level{0:D3} {{\n", level));
		}

		block.Append('\t', depth).Append("export declare const options: { auth: { username: string; password: string }; headers: { Authorization: 'Basic' | 'Bearer' } };").Append(_LINE_BREAK);

		for (int level = depth - 1; level >= 0; level--)
		{
			block.Append('\t', level).Append("}\n");
		}

		return block.ToString();
	}

	private static string NumberedLines(string lineFormat)
	{
		StringBuilder builder = new(_BANG_SLASH_LINES * lineFormat.Length);

		for (int line = 0; line < _BANG_SLASH_LINES; line++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, lineFormat, line));
		}

		return builder.ToString();
	}

	/// <summary>Lines of the group repeated up to the line-length ceiling, each between the prefix and the suffix, filling the file to just under the file-size ceiling.</summary>
	internal static string LongLines(string group, string prefix = "", string suffix = "")
	{
		StringBuilder builder = new(TypeScriptMatchBoundStressSet.TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + TypeScriptMatchBoundStressSet.LONG_LINE_LENGTH_IN_CHARACTERS < TypeScriptMatchBoundStressSet.TARGET_SIZE_IN_CHARACTERS)
		{
			int lineStart = builder.Length;

			builder.Append(prefix);

			while (builder.Length - lineStart + group.Length + suffix.Length < TypeScriptMatchBoundStressSet.LONG_LINE_LENGTH_IN_CHARACTERS)
			{
				builder.Append(group);
			}

			builder.Append(suffix).Append(_LINE_BREAK);
		}

		return builder.ToString();
	}

	internal static string FillToCeilingWithLines(string line)
	{
		StringBuilder builder = new(TypeScriptMatchBoundStressSet.TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + line.Length < TypeScriptMatchBoundStressSet.TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(line);
		}

		return builder.ToString();
	}
}
