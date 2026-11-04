using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE `TypeScript` SHAPES OF THE MATCH-BOUND STRESS SET, written beside the `JavaScript`, `Shell`, `PowerShell` and `VBNet` sets and read by the same kind of test: every shape is legal, ordinary-looking `TypeScript` a client
/// repository could contain, filled to just under the reader's size ceiling, and none is built to make an expression backtrack catastrophically. What the shapes do is push every honest cost to the ceilings the engine already
/// enforces: files at the size ceiling, lines at the line-length ceiling, anchors and matches at the densest a generated bundle or a declaration file plausibly carries, and every bounded window of the draft expressions opened over
/// and over without being closed.
///
/// THE SET CARRIES lines at the 10,000-character ceiling with long annotations and unions; generic-heavy declarations; decorator chains; a `.tsx` file with nested templates; ten
/// thousand lines of <c>x!/y</c> for the backward predicate; anchor-dense declaration-file shapes up to the file-size ceiling with deep nesting; and, because the shared windows treat angle brackets as plain characters, the shapes of
/// the generic-argument group (nested to depth three, at the depth bound, past it, at the length bound, past it, never closed) and of the tail refusal of the object-key carrier. Beside them, one long-line shape per window family
/// of the `TypeScript`-only expressions, and the long-line shapes that cost the inherited expressions most in the `JavaScript` gate, written as `TypeScript`.
///
/// THE INHERITED AUTHORIZATION-GUARD EXPRESSION IS CHARGED IN FULL (conservative-charge rule), so the set carries the typed shapes of identifier-valued, environment-valued and tail-carrying `Authorization` carriers at the line ceiling, and
/// the same carriers in full guard windows (one per line, every line the width of a guard line), because the guard of that expression reads thirty-one lines at every carrier.
///
/// THIS SET IS RUN over the draft expressions of <see cref="TypeScriptDraftExpressions"/> (the shipped inherited expressions and the twelve `TypeScript` ones) by the cost gate. Every shape is deterministic and invented: placeholders and generic names, no fragment of any real text.
/// </summary>
internal static class TypeScriptMatchBoundStressSet
{
	internal const int TARGET_SIZE_IN_CHARACTERS = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;

	internal const int LONG_LINE_LENGTH_IN_CHARACTERS = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _LINE_MARGIN_IN_CHARACTERS;

	internal const string FILE_EXTENSION = ".ts";
	internal const string MARKUP_FILE_EXTENSION = ".tsx";
	internal const string DECLARATION_FILE_EXTENSION = ".d.ts";

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;
	private const int _LINE_MARGIN_IN_CHARACTERS = 16;
	private const int _BUILDER_SLACK_IN_CHARACTERS = 4096;
	private const int _FILLER_LINES_PER_WINDOW = 4;
	private const int _DEFINITION_FILLER_LINES = 29;
	private const int _BANG_SLASH_LINES = 10_000;
	private const int _TEMPLATE_NESTING_DEPTH = 8;
	private const int _DECLARATION_NESTING_DEPTH = 40;
	private const int _TEMPLATE_TYPE_NESTING_DEPTH = 7;
	private const int _GENERIC_BODY_AT_THE_LENGTH_BOUND = 120;
	private const int _GENERIC_BODY_PAST_THE_LENGTH_BOUND = 121;
	private const int _GENERIC_BODY_JUST_UNDER_THE_LENGTH_BOUND = 119;
	private const int _REFUSAL_BODY_AT_THE_BOUND = 300;
	private const int _REFUSAL_BODY_FILLER_LENGTH = _REFUSAL_BODY_AT_THE_BOUND - 1;
	private const int _MODERATE_DECLARATION_NESTING_DEPTH = _DECLARATION_NESTING_DEPTH / 4;
	private const int _NESTED_GENERIC_BODY_LENGTH = _GENERIC_BODY_AT_THE_LENGTH_BOUND / 2;
	private const int _FLAT_PREFIX_AT_THE_BOUND = 290;
	private const char _FILLER_UNIT = 'x';
	private const int _GUARD_WINDOW_LINE_LENGTH_IN_CHARACTERS = 390;
	private const int _COMMENT_OPENER_LENGTH = 3;
	private const string _GUARD_WINDOW_IDENTIFIER_CARRIER = "const typedCarrier: RequestInit = { headers: { Authorization: tokenValue } }; ";
	private const string _GUARD_WINDOW_ENVIRONMENT_CARRIER = "const envCarrier: RequestInit = { headers: { Authorization: process.env.AUTH_VALUE ?? '' } }; ";
	private const string _LINE_BREAK = "\n";
	private const string _FUNCTION_FORMAT = "function step{0:D6}(): void {{\n{1}}}\n";
	private const string _FILLER_STATEMENT = "\tconst total: number = first + second;\n";
	private const string _SHEBANG_LINE = "#!/usr/bin/env node\n";
	private const string _COMMENT_LINE = "// an ordinary comment line about the typed code below, with words and numbers 12345\n";
	private const string _COMMENT_LINE_WITH_ANCHORS = "// axios.get<Reply>(url, { auth: { username: u, password: p } }) and headers: { Authorization: 'Basic ' + btoa(a + ':' + b) } were removed\n";
	private const string _BANG_SLASH_LINE_FORMAT = "const quotient{0:D5}: number = value!/scale;\n";
	private const string _GENERIC_HEAVY_LINE =
		"function convert<TInput extends Record<string, Array<Map<string, number>>>, TOutput = Promise<Array<TInput>>>(value: TInput): TOutput { return value as unknown as TOutput; }\n";

	private static readonly ImmutableArray<string> _ORDINARY_STATEMENTS =
	[
		"\tconst order: Order | undefined = await getOrder(orderId);",
		"\tif (!order) { return null; }",
		"\tconst amount: number = order.lines.map((line: Line): number => line.total).reduce((a: number, b: number): number => a + b, 0);",
		"\tlog(`Order ${orderId} totals ${amount}`);",
		"\tfor (const item of items) { names.push(item.name!.toUpperCase()); }",
		"\tconst ratio: number = first / second / third;",
		"\tconst matcher: RegExp = /ab[/]c+d/gi;",
		"\tconst scaled: number = lookup.get(key)!/scale;"
	];

	private static readonly ImmutableArray<string> _ANCHOR_DENSE_NEAR_MISSES =
	[
		"\tinterface Credentials { username: string; password: string; }",
		"\ttype Scheme = 'Basic' | 'Bearer';",
		"\ttype Header = { Authorization: 'Basic' };",
		"\ttype Hole = `Basic ${string}`;",
		"\tconst label: string = credentialName;",
		"\tconsole.log('the Authorization header is disabled');",
		"\tconst author: string = authority + oauth + authenticate;",
		"\tconst key: string = btoa(host + ':' + port);",
		"\tconst cacheKey: string = Buffer.from(text).toString('base64');",
		"\tconst headers: Record<string, string> = { Accept: 'application/json', 'Content-Type': 'text/plain' };",
		"\tfetch(url, { credentials: 'include' });",
		"\tthis.httpService.get<Reply>(url, { params: { id } });",
		"\tconst proxyHeader = { 'Proxy-Authorization': 'Basic ' + proxyToken };",
		"\tconst config: AxiosRequestConfig = { auth: { token: sessionToken } };",
		"\tconst mode: string = process.env.NODE_ENV!;",
		"\tmail.createTransport({ host: mailHost, auth: { user: userName } });"
	];

	private static readonly ImmutableArray<string> _EVERY_CLASS_CONSTRUCTS =
	[
		"\taxios.get<Reply>(url, { auth: { username: userName, password: 'exampleSecret' } });",
		"\tsuperagent.get(url).auth(userName, 'exampleSecret');",
		"\thttp.get<Reply<Item<Row>>>('https://exampleUser:exampleSecret@host.example/path', handler);",
		"\tfetch(url, { headers: { Authorization: `Basic ${encoded}` } });",
		"\tconst header: string = 'Basic ' + btoa(userName + ':' + secret);",
		"\tconst legacy = 'Basic ' + Buffer.from(`${userName}:${secret}`).toString('base64');",
		"\tfunction buildBasicHeader(userName: string, secret: string): string { return 'Basic ' + btoa(userName + ':' + secret); }",
		"\txhr.open('GET', url, true, userName, 'exampleSecret');",
		"\tconst target = new URL(base);\n\ttarget.username = userName;\n\ttarget.password = 'exampleSecret';",
		"\tgot<Reply>(url, { username: userName, password: 'exampleSecret' });",
		"\trequest<Reply>(url, { auth: { user: userName, pass: secret, sendImmediately: false } });",
		"\tconst options: AxiosRequestConfig = { url: apiUrl, auth: { username: 'exampleUser', password: 'exampleSecret' } };",
		"\tconst typed = { auth: { username: 'exampleUser', password: 'exampleSecret' } } as AxiosRequestConfig;",
		"\tconst checked = { auth: { username: 'exampleUser', password: 'exampleSecret' } } satisfies AxiosRequestConfig;",
		"\tconst asserted = <AxiosRequestConfig>{ auth: { username: 'exampleUser', password: 'exampleSecret' } };",
		"\tthis.httpService.post<Reply>(url, { auth: { username: 'exampleUser', password: 'exampleSecret' } });",
		"\tconst headers = new HttpHeaders({ Authorization: 'Basic ' + token });",
		"\tconst encodedHeader = 'Basic ' + encodedPair;\n\tfetch(url, { headers: { Authorization: encodedHeader } });",
		"\tconst password: string = this.loginForm.get('password')?.value;",
		"\tconst fromControls = this.form.controls['password'].value;",
		"\tasync login(@Body('password') secret: string) { return secret; }",
		"\tconst fromConfig = this.configService.get<string>('DB_PASSWORD');",
		"\tprivate readonly apiPassword: string = 'exampleSecretValue';",
		"\tconst answer = window.prompt('Enter your password');",
		"\tconst stored = localStorage.getItem('password');",
		"\tconst fromEnv = process.env.API_PASSWORD!;",
		"\tconst envHeader: string = process.env.AUTH_VALUE ?? 'Basic dGVzdDp0ZXN0';",
		"\tconst bearerHeader: string = process.env.AUTH_VALUE || 'Bearer';",
		"\tconst typedCarrier: RequestInit = { headers: { Authorization: tokenValue } };",
		"\tconst envCarrier: RequestInit = { headers: { Authorization: process.env.AUTH_VALUE ?? '' } };",
		"\tconst basicFallback: RequestInit = { headers: { Authorization: process.env.AUTH_VALUE ?? 'Basic dGVzdDp0ZXN0' } };"
	];

	private static readonly ImmutableArray<string> _OPEN_WINDOWS =
	[
		"\taxios.get<Reply>(url, {",
		"\trequest.get(url).auth(",
		"\tsuperagent.get(url)",
		"\tconst headers: HeadersInit = {",
		"\tnew Headers({",
		"\tgot<Reply>(url, {",
		"\tthis.httpService.post<Reply>(url, {",
		"\tconst options: AxiosRequestConfig = { auth: {",
		"\txhr.open('GET', url, true,",
		"\tbtoa(",
		"\tBuffer.from(",
		"\tconst target = new URL(base);",
		"\tfunction build(a: string) {",
		"describe('suite', () => {",
		"\thttp.request({ host: hostName,",
		"\tfetch('https://example.test/a',",
		"\tconst parsed: Parsed = {"
	];

	private static readonly ImmutableArray<string> _LITERAL_AND_COMMENT_LINES =
	[
		"\t// axios.get<Reply>(url, { auth: { username: userName, password: secret } }) was removed in the migration to tokens",
		"\t/* headers: { Authorization: 'Basic abc' } is deprecated */",
		"\tconst text: string = 'btoa(a + \":\" + b) and Authorization: Basic abc appear in prose';",
		"\tconst html: string = '<input type=\"text\" name=\"user\">';",
		"\tconst note: string = `the password ${name} was refreshed for ${user} at ${when}`;",
		"\t// const target = new URL(base); target.password = secret;",
		"\ttype Label = `Basic ${string}` | `Bearer ${string}`;"
	];

	private static readonly ImmutableArray<string> _LEXICAL_HEAVY_LINES =
	[
		"\tconst a: string = `x ${f('}')} ${g(`${h}`)} y`;",
		"\tconst re: RegExp = /ab[/]cd\\/e/g;",
		"\tconst d: number = a / b / c;",
		"\tconst e: number = a! / b! / c!;",
		"\tconst f: boolean = !/re/.test(s);",
		"\tconst g: number = id<number>(8) / 2;",
		"\tconst h: number = (value as number) / 2;",
		"\tconst i = <string>value;",
		"\tx = y /* a block comment */ / z;",
		"\tconst u = items.filter((v: number) => v! / 2 > 1).map((v: string) => /^x/.test(v));",
		"\t@Input({ required: true }) /re/.test(x);"
	];

	private static readonly ImmutableArray<string> _FLOW_LINES =
	[
		"\tconst pw: string = this.loginForm.get('password')!.value;",
		"\tconst pw2 = this.form.controls.password.value;",
		"\tconst pw3 = this.form.value.password;",
		"\tasync login(@Body() { username, password }: LoginDto) { return password; }",
		"\tconst fromConfig = this.configService.getOrThrow<string>('API_SECRET');",
		"\tprivate readonly apiToken: string = 'exampleTokenValue';",
		"\tconst pw4 = document.getElementById('password').value;",
		"\tconst answer = window.prompt('Enter a value');",
		"\tconst stored = sessionStorage.getItem('userPassword');",
		"\tconst body = `<input type=\"password\" name=\"pw\">`;",
		"\tconst { username, password } = req.body;",
		"\tconst key = process.env['DB_PASS'];",
		"\tconst prompted = rl.question('Your password? ', done);"
	];

	/// <summary>One long-line shape per window family of the `TypeScript` expressions, each repeating one group up to the line-length ceiling.</summary>
	private static readonly ImmutableArray<(string Slug, string Group)> _LONG_LINE_GROUPS =
	[
		("annotations", "const a: Record<string, Array<Map<string, number>>> = {};"),
		("generic-calls-at-depth-three", "axios.get<A<B<C>>>(u,{auth:{username:a,password:b}});"),
		("generic-calls-past-depth-three", "axios.get<A<B<C<D>>>>(u,{auth:{username:a,password:b}});"),
		("generic-openings-without-a-close", "axios.get<A<B<C<"),
		("generic-openings-at-one-level", "axios.get<"),
		("generic-arrow-types", "axios.get<(a:A)=>B>(u,{auth:{username:a,password:b}});"),
		("generic-comparison-chains", "if(a<b&&c>(d)){e=f<g>(h);}"),
		("generic-calls-of-every-callee", "got<A>(u,{username:a,password:b});request<A>(u,{auth:{user:a,pass:b,sendImmediately:false}});fetch<A>('https://u:p@h.example/a');"),
		("generic-calls-to-http-named-receivers", "this.httpService.post<A<B>>(u,{auth:{username:a,password:b}});"),
		("http-named-identifiers", "httpHttpHttp.getAll(u);this.http.fetchAll(u);"),
		("http-named-openings", "this.http.get<A>(u,{"),
		("n4-type-position-literals", "type H={Authorization:'Basic';};type U={Authorization:'Basic'|'Bearer'};type T={Authorization:`Basic ${string}`};"),
		("n4-key-openings-without-a-close", "{Authorization:'Basic"),
		("n4-keys-with-a-bar-or-a-semicolon-after-other-text", "{Authorization:'Basic'||x};{Authorization:'Basic ' + t;"),
		("typed-objects-with-credentials", "const o:AxiosRequestConfig={auth:{username:'a',password:'b'}};"),
		("typed-object-openings", "const o:AxiosRequestConfig={auth:{"),
		("typed-objects-with-assertions-after", "const o={auth:{username:'a',password:'b'}} as AxiosRequestConfig;const p={auth:{username:'a',password:'b'}} satisfies AxiosRequestConfig;"),
		("auth-keys-with-literal-members-and-no-type", "const o={auth:{username:'a',password:'b'}};"),
		("angular-form-reads", "this.form.get('password')?.value;this.form.controls['password'].value;this.form.value.password;"),
		("angular-form-openings", "this.form.get('password'"),
		("nest-body-reads", "@Body('password') p:string,@Body() {username,password}:Dto,"),
		("nest-body-openings", "@Body("),
		("nest-config-reads", "this.configService.get<string>('DB_PASSWORD');this.configService.getOrThrow('API_SECRET');"),
		("config-named-identifiers", "configConfigConfig.getAll(u);this.config.set('x');"),
		("typed-credential-assignments", "private apiPassword: string = 'lit';readonly secret: string = \"x\";"),
		("typed-credential-openings-without-an-equals-sign", "private apiPassword: string;readonly secret: Secret|undefined,"),
		("typed-constants", "export const mode: string = 'basic';readonly scheme: 'Basic'|'Bearer' = 'Basic';"),
		("typed-constant-modifier-runs", "export declare public private protected readonly static const x: string = 'v';"),
		("typed-environment-declarations", "const t:string=process.env.API_TOKEN??'Basic';readonly u:string=Bun.env.X||'';static v:Header=`${process.env.Y}`;"),
		("typed-environment-declaration-openings-without-a-close", "const t:string=process.env.API_TOKEN??'Basic"),
		("typed-environment-declarations-with-typescript-tails", "const t:string=process.env.API_TOKEN!;const u=process.env.X as string;const v:string=<string>process.env.Y;"),
		("typed-identifier-authorization-carriers", "const o:RequestInit={headers:{Authorization:tok}};x.setHeader('Authorization',tok);headers.Authorization=tok;h.set('Authorization',tok);"),
		("typed-environment-authorization-carriers", "const o:RequestInit={headers:{Authorization:process.env.API_TOKEN??''}};headers.Authorization=import.meta.env.API_TOKEN||'';"),
		("typed-basic-fallback-authorization-carriers", "const o:Headers={Authorization:process.env.API_TOKEN??'Basic dGVzdDp0ZXN0'};h.set('Authorization',Deno.env.get('T')||'Basic');"),
		("typed-authorization-carriers-with-typescript-tails", "const o:RequestInit={headers:{Authorization:tok!}};h.set('Authorization',tok as string);headers.Authorization=process.env.API_TOKEN!;"),
		("bang-slash-chains", "a!/b!/c!/d!/e!/f!/"),
		("bang-regex-openers", "!/re/.test(s)&&!/x[/]y/.test(t)&&"),
		("bang-slash-inside-argument-windows", "axios.get(a!/b,c!/d,{auth:{username:u,password:p}});"),
		("non-null-assertions", "a!.b!.c!.d!(e!);f![0]!;"),
		("decorator-chains", "@A() @B({a:1,b:'x'}) @C('x',['y']) @D "),
		("btoa-pairs", "btoa(a+':'+b);"),
		("request-chain-openings", "request.get(u).set(a,b).auth("),
		("unclosed-quotes", "axios.get('a,\"b,`c "),
		("buffer-pairs", "Buffer.from(a+':'+b).toString('base64');"),
		("headers-keys-without-a-close", "headers:{Accept:a,"),
		("axios-openings-without-a-close", "axios.get(u,{")
	];

	private static readonly ImmutableArray<(string Slug, string Definition, int FillerLines, string Tail)> _DEFINITION_BLOCKS =
	[
		("typed-url-binding-then-assignments", "const target: URL = new URL(base);", _DEFINITION_FILLER_LINES, "target.username = userName;\ntarget.password = secret;\n"),
		("function-head-then-no-token", "function build(userName: string, secret: string): string {", _DEFINITION_FILLER_LINES, "}\n"),
		("function-head-then-token", "function build(userName: string, secret: string): string {", _DEFINITION_FILLER_LINES, "\treturn 'Basic ' + btoa(userName + ':' + secret);\n}\n"),
		("typed-prompt-declaration-then-call", "const prompt: Prompter = require('prompt-lib')();", _DEFINITION_FILLER_LINES, "const answer = prompt('Enter your password');\n")
	];

	private static readonly ImmutableArray<string> _DECLARATION_MEMBERS =
	[
		"interface RequestOptions { auth: { username: string; password: string }; headers: { Authorization: 'Basic'; Accept: string }; url: `https://${string}:${string}@host.example/path`; }",
		"export declare function send<TReply = Record<string, Array<Map<string, Set<number>>>>>(options: RequestOptions): Promise<TReply>;",
		"export declare const basicScheme: 'Basic' | 'Bearer';",
		"export declare function btoa(value: string): string;",
		"export declare class HttpService { get<T>(url: string, config?: AxiosRequestConfig): Observable<T>; post<T>(url: string, body?: unknown, config?: AxiosRequestConfig): Observable<T>; }",
		"export declare const apiPassword: string;",
		"readonly password: string; readonly secret: string; readonly token: string;",
		"export declare function fromBuffer(value: Buffer): string; // Buffer.from( appears in prose"
	];

	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		AddCeilingShapes(shapes);
		AddTheShapesTheDesignNames(shapes);
		AddDeclarationFileShapes(shapes);
		AddLongLineShapes(shapes);
		AddDefinitionBlockShapes(shapes);
		AddGuardWindowShapes(shapes);

		return shapes.ToImmutable();
	}

	internal static string RelativePathOf(MatchBoundStressShape shape)
	{
		return string.Format(CultureInfo.InvariantCulture, "stress/{0}{1}", shape.Name, shape.FileExtension);
	}

	private static void AddCeilingShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("ts-ordinary-at-ceiling", FillToCeiling(_ORDINARY_STATEMENTS)));
		shapes.Add(Shape("ts-anchor-dense-near-misses", FillToCeiling(_ANCHOR_DENSE_NEAR_MISSES)));
		shapes.Add(Shape("ts-every-class-at-match-density", FillToCeiling(_EVERY_CLASS_CONSTRUCTS)));
		shapes.Add(Shape("ts-open-windows", FillWindowsToCeiling(_OPEN_WINDOWS)));
		shapes.Add(Shape("ts-literals-and-comments", FillToCeiling(_LITERAL_AND_COMMENT_LINES)));
		shapes.Add(Shape("ts-lexical-heavy", FillToCeiling(_LEXICAL_HEAVY_LINES)));
		shapes.Add(Shape("ts-flow-signal-forms", FillToCeiling(_FLOW_LINES)));
		shapes.Add(Shape("ts-describe-lines-without-it", FillToCeilingWithLines("describe('suite', () => {\n")));
		shapes.Add(Shape("ts-generated-marker-lookalike-lines", FillToCeilingWithLines("// generated by hand, not auto, and do not worry\n")));
		shapes.Add(Shape("ts-hashbang-then-ordinary-code", _SHEBANG_LINE + FillToCeiling(_ORDINARY_STATEMENTS)));
	}

	/// <summary>The named shapes of the cost gate, each with its own builder.</summary>
	private static void AddTheShapesTheDesignNames(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("ts-long-line-annotations-at-the-ceiling", LongLines("const a: Record<string, Array<Map<string, number>>> = {}; let b: Promise<Array<Set<string>>>; ")));
		shapes.Add(Shape("ts-long-line-unions-at-the-ceiling", LongLines("'alpha' | 'beta' | 'gamma' | 'delta' | ", "type Choice = ", "'omega';")));
		shapes.Add(Shape("ts-long-line-annotations-and-unions-mixed", LongLines("x: 'a' | 'b' | Array<'c' | 'd'>, y: Map<string, 'e' | 'f'>, ", "function f(", "): void {}")));
		shapes.Add(Shape("ts-generic-heavy-declarations", FillToCeilingWithLines(_GENERIC_HEAVY_LINE)));
		shapes.Add(Shape("ts-decorator-chains", FillToCeilingWithLines(DecoratedClass())));
		shapes.Add(Shape("ts-long-line-decorator-chains-at-the-ceiling", LongLines("@Input({ alias: 'a' }) @Output() @HostListener('click', ['$event']) @ViewChild('v', { static: true }) ")));
		shapes.Add(MarkupShape("tsx-nested-templates", FillToCeilingWithLines(MarkupLineWithNestedTemplates())));
		shapes.Add(MarkupShape("tsx-templates-nested-to-depth-eight", FillToCeilingWithLines(NestedTemplateLine(_TEMPLATE_NESTING_DEPTH))));
		shapes.Add(Shape("ts-templates-nested-to-depth-eight", FillToCeilingWithLines(NestedTemplateLine(_TEMPLATE_NESTING_DEPTH))));
		shapes.Add(Shape("ts-ten-thousand-lines-of-non-null-assertion-then-slash", BangSlashLines()));
		shapes.Add(Shape("ts-two-megabytes-of-line-comments", FillToCeilingWithLines(_COMMENT_LINE)));
		shapes.Add(Shape("ts-two-megabytes-of-line-comments-with-anchors", FillToCeilingWithLines(_COMMENT_LINE_WITH_ANCHORS)));
	}

	/// <summary>The anchor-dense declaration-file shapes up to the file-size ceiling: deep nesting with every anchor word in a member, lines at the ceiling, and template-literal types nested to the scanner's bound.</summary>
	private static void AddDeclarationFileShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(DeclarationShape("dts-anchor-dense-deep-nesting", RepeatToCeiling(DeclarationBlock(_DECLARATION_NESTING_DEPTH))));
		shapes.Add(DeclarationShape("dts-anchor-dense-moderate-nesting", RepeatToCeiling(DeclarationBlock(_MODERATE_DECLARATION_NESTING_DEPTH))));
		shapes.Add(DeclarationShape("dts-anchor-dense-long-lines", LongLines("auth: { username: string; password: string }; Authorization: 'Basic'; readonly secret: string; ", "declare interface Options { ", "}")));
		shapes.Add(DeclarationShape("dts-anchor-dense-members-per-line", FillToCeilingWithLines($"{string.Concat(_DECLARATION_MEMBERS.Select(member =>
		{
			return $"{member} ";
		}))}{_LINE_BREAK}")));
		shapes.Add(DeclarationShape("dts-template-literal-types-nested", FillToCeilingWithLines(NestedTemplateTypeLine(_TEMPLATE_TYPE_NESTING_DEPTH))));
		shapes.Add(DeclarationShape("dts-generic-signatures-nested-to-depth-three", FillToCeilingWithLines("export declare function f<A = Map<string, Array<Set<number>>>>(a: A): Promise<Record<string, Array<Map<string, A>>>>;\n")));
		shapes.Add(DeclarationShape("dts-generic-signatures-nested-past-depth-three", FillToCeilingWithLines("export declare function g<A = Map<string, Array<Set<Record<string, number>>>>>(a: A): Promise<Record<string, Array<Map<string, Set<A>>>>>;\n")));
	}

	private static void AddLongLineShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string group) in _LONG_LINE_GROUPS)
		{
			shapes.Add(Shape($"ts-long-line-{slug}", LongLines(group)));
		}

		shapes.Add(Shape("ts-long-line-n3-generic-body-at-the-length-bound", LongLines($"axios.get<{new string(_FILLER_UNIT, _GENERIC_BODY_AT_THE_LENGTH_BOUND)}>(u,{{auth:{{username:a,password:b}}}});")));
		shapes.Add(Shape("ts-long-line-n3-generic-body-past-the-length-bound", LongLines($"axios.get<{new string(_FILLER_UNIT, _GENERIC_BODY_PAST_THE_LENGTH_BOUND)}>(u,{{auth:{{username:a,password:b}}}});")));
		shapes.Add(Shape("ts-long-line-n3-generic-body-unclosed-at-the-length-bound", LongLines($"axios.get<{new string(_FILLER_UNIT, _GENERIC_BODY_JUST_UNDER_THE_LENGTH_BOUND)} ")));
		shapes.Add(Shape("ts-long-line-n3-nested-bodies-at-the-length-bound", LongLines($"axios.get<{new string(_FILLER_UNIT, _NESTED_GENERIC_BODY_LENGTH)}<{new string(_FILLER_UNIT, _NESTED_GENERIC_BODY_LENGTH)}<{new string(_FILLER_UNIT, _NESTED_GENERIC_BODY_LENGTH)}>>>(u);")));
		shapes.Add(Shape("ts-long-line-n4-refusal-body-at-the-bound", LongLines($"{{Authorization:'Basic {new string(_FILLER_UNIT, _REFUSAL_BODY_FILLER_LENGTH)}';}}")));
		shapes.Add(Shape("ts-long-line-ts001-flat-prefix-at-the-bound", LongLines($"const o:AxiosRequestConfig={{{new string(_FILLER_UNIT, _FLAT_PREFIX_AT_THE_BOUND)},auth:{{username:'a',password:'b'}}}};")));
	}

	private static void AddDefinitionBlockShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string definition, int fillerLines, string tail) in _DEFINITION_BLOCKS)
		{
			shapes.Add(Shape($"ts-definition-{slug}", DefinitionBlocks(definition, fillerLines, tail)));
		}
	}

	/// <summary>
	/// The guard of the inherited authorization-guard expression (a look-behind and a look-ahead of thirty-one lines of at most 400 characters at every identifier-valued or environment-valued carrier) at its widest: every line holds one typed
	/// carrier and is padded by a comment to the width of a guard line, so that the window at each carrier covers a full thirty-one lines and no bearer write stands in it.
	/// </summary>
	private static void AddGuardWindowShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("ts-typed-identifier-authorization-carriers-in-full-guard-windows", FillToCeilingWithLines(GuardWindowLine(_GUARD_WINDOW_IDENTIFIER_CARRIER))));
		shapes.Add(Shape("ts-typed-environment-authorization-carriers-in-full-guard-windows", FillToCeilingWithLines(GuardWindowLine(_GUARD_WINDOW_ENVIRONMENT_CARRIER))));
	}

	private static string GuardWindowLine(string carrier)
	{
		return $"{carrier}// {new string(_FILLER_UNIT, _GUARD_WINDOW_LINE_LENGTH_IN_CHARACTERS - carrier.Length - _COMMENT_OPENER_LENGTH)}{_LINE_BREAK}";
	}

	private static MatchBoundStressShape Shape(string name, string text)
	{
		return new MatchBoundStressShape { Name = name, Text = text, FileExtension = FILE_EXTENSION };
	}

	private static MatchBoundStressShape MarkupShape(string name, string text)
	{
		return new MatchBoundStressShape { Name = name, Text = text, FileExtension = MARKUP_FILE_EXTENSION };
	}

	private static MatchBoundStressShape DeclarationShape(string name, string text)
	{
		return new MatchBoundStressShape { Name = name, Text = text, FileExtension = DECLARATION_FILE_EXTENSION };
	}

	private static string FillToCeiling(ImmutableArray<string> statements)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);
		StringBuilder body = new();
		int functionNumber = 0;

		foreach (string statement in statements)
		{
			body.Append(statement).Append(_LINE_BREAK);
		}

		while (builder.Length + body.Length + _FUNCTION_FORMAT.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _FUNCTION_FORMAT, functionNumber++, body));
		}

		return builder.ToString();
	}

	private static string FillToCeilingWithLines(string line)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + line.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(line);
		}

		return builder.ToString();
	}

	private static string FillWindowsToCeiling(ImmutableArray<string> openers)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);
		int opener = 0;

		while (builder.Length + LONG_LINE_LENGTH_IN_CHARACTERS < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(openers[opener % openers.Length]).Append(_LINE_BREAK);

			for (int filler = 0; filler < _FILLER_LINES_PER_WINDOW; filler++)
			{
				builder.Append(_FILLER_STATEMENT);
			}

			opener++;
		}

		return builder.ToString();
	}

	/// <summary>Lines of the group repeated up to the line-length ceiling, each between the prefix and the suffix, filling the file to just under the file-size ceiling.</summary>
	private static string LongLines(string group, string prefix = "", string suffix = "")
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + LONG_LINE_LENGTH_IN_CHARACTERS < TARGET_SIZE_IN_CHARACTERS)
		{
			int lineStart = builder.Length;

			builder.Append(prefix);

			while (builder.Length - lineStart + group.Length + suffix.Length < LONG_LINE_LENGTH_IN_CHARACTERS)
			{
				builder.Append(group);
			}

			builder.Append(suffix).Append(_LINE_BREAK);
		}

		return builder.ToString();
	}

	private static string BangSlashLines()
	{
		StringBuilder builder = new(_BANG_SLASH_LINES * _BANG_SLASH_LINE_FORMAT.Length);

		for (int line = 0; line < _BANG_SLASH_LINES; line++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _BANG_SLASH_LINE_FORMAT, line));
		}

		return builder.ToString();
	}

	private static string DecoratedClass()
	{
		return "@Component({ selector: 'app-item', template: `<div>${title}</div>` })\n@Injectable()\nexport class ItemComponent {\n"
			+ "\t@Input() name!: string;\n\t@Output() changed = new EventEmitter<string>();\n"
			+ "\t@HostListener('window:resize', ['$event']) onResize(event: UIEvent): boolean { return /^x/.test(this.name) && !/y/.test(this.name); }\n}\n";
	}

	private static string MarkupLineWithNestedTemplates()
	{
		return "const view = <div className={`box ${active ? `on ${mode}` : `off`}`}>{items.map((i: Item) => <li key={`k${i.id}`}>{`${i.a}/${i.b}`}</li>)}</div>;\n";
	}

	/// <summary>A template nested to the given depth, every level closed by its closing brace and its backtick, written as one statement.</summary>
	private static string NestedTemplateLine(int depth)
	{
		string template = "`z`";

		for (int level = 1; level <= depth; level++)
		{
			template = $"`t{level}${{{template}}}`";
		}

		return $"const nested: string = {template};{_LINE_BREAK}";
	}

	/// <summary>A template-literal type nested to the given depth, written as one declaration of a declaration file.</summary>
	private static string NestedTemplateTypeLine(int depth)
	{
		string template = "`z${string}`";

		for (int level = 1; level <= depth; level++)
		{
			template = $"`t{level}${{{template}}}`";
		}

		return $"export type Nested = {template};{_LINE_BREAK}";
	}

	/// <summary>Namespaces nested to the given depth with the anchor-dense members at the innermost level, every level closed.</summary>
	private static string DeclarationBlock(int depth)
	{
		StringBuilder block = new();

		for (int level = 0; level < depth; level++)
		{
			block.Append('\t', level).Append(level == 0 ? "declare namespace" : "namespace").Append(string.Format(CultureInfo.InvariantCulture, " Level{0:D2} {{\n", level));
		}

		foreach (string member in _DECLARATION_MEMBERS)
		{
			block.Append('\t', depth).Append(member).Append(_LINE_BREAK);
		}

		for (int level = depth - 1; level >= 0; level--)
		{
			block.Append('\t', level).Append("}\n");
		}

		return block.ToString();
	}

	private static string DefinitionBlocks(string definition, int fillerLines, string tail)
	{
		StringBuilder block = new();

		block.Append(definition).Append(_LINE_BREAK);

		for (int filler = 0; filler < fillerLines; filler++)
		{
			block.Append(_FILLER_STATEMENT);
		}

		block.Append(tail);

		return RepeatToCeiling(block.ToString());
	}

	private static string RepeatToCeiling(string block)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + block.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(block);
		}

		return builder.ToString();
	}
}
