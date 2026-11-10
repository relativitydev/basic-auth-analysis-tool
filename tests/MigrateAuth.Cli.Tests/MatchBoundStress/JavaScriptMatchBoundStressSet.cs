using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE `JavaScript` SHAPES OF THE MATCH-BOUND STRESS SET, written beside the `Shell`, `PowerShell` and `VBNet` sets and read by the same kind of test: every shape is legal, ordinary-looking
/// JavaScript a client repository could contain, filled to just under the reader's size ceiling, and none is built to make an expression backtrack catastrophically. What the shapes do is push
/// every honest cost to the ceilings the engine already enforces: files at the size ceiling, lines at the line-length ceiling, anchors and matches at the densest a generated bundle plausibly
/// carries, and every bounded window of the catalog (the argument window, the chain window, the definition window, the statement window) opened over and over without being closed.
///
/// THE SHAPES THE COST GATE NEEDS ARE ALL HERE: lines of 10,000 characters of minified-like code; <c>a/b/c/...</c> division chains; a regular
/// expression literal per line for ten thousand lines; templates nested to depth eight with closing braces and backticks; ten thousand opening braces in one hole; two megabytes of <c>//</c> comments;
/// large object literals; long alternations of Authorization, auth and password; <c>Buffer.from(</c> chains. Beside them, one long-line shape per window family of the expressions.
///
/// THIS SET IS RUN over the draft expressions of <see cref="JavaScriptDraftExpressions"/> by the cost gate. Every shape is deterministic and invented: placeholders and generic names, no fragment of any real text.
/// </summary>
internal static class JavaScriptMatchBoundStressSet
{
	internal const int TARGET_SIZE_IN_CHARACTERS = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;

	internal const int LONG_LINE_LENGTH_IN_CHARACTERS = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _LINE_MARGIN_IN_CHARACTERS;

	internal const string FILE_EXTENSION = ".js";

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;
	private const int _LINE_MARGIN_IN_CHARACTERS = 16;
	private const int _BUILDER_SLACK_IN_CHARACTERS = 4096;
	private const int _FILLER_LINES_PER_WINDOW = 4;
	private const int _DEFINITION_FILLER_LINES = 29;
	private const int _REGEX_LITERAL_LINES = 10_000;
	private const int _TEMPLATE_NESTING_DEPTH = 8;
	private const int _OPENING_BRACES_IN_ONE_HOLE = 10_000;
	private const int _BRACES_PER_LINE = 100;
	private const int _OBJECT_MEMBERS_PER_LITERAL = 1_000;
	private const int _MEMBERS_BEFORE_THE_KEY_AT_THE_WINDOW = 85;
	private const int _MEMBERS_BEFORE_THE_KEY_BEYOND_THE_WINDOW = 100;
	private const string _LINE_BREAK = "\n";
	private const string _FUNCTION_FORMAT = "function step{0:D6}() {{\n{1}}}\n";
	private const string _FILLER_STATEMENT = "\tconst total = first + second;\n";
	private const string _SHEBANG_LINE = "#!/usr/bin/env node\n";
	private const string _COMMENT_LINE = "// an ordinary comment line about the code below, with words and numbers 12345\n";
	private const string _COMMENT_LINE_WITH_ANCHORS = "// axios.get(url, { auth: { username: u, password: p } }) and headers: { Authorization: 'Basic ' + btoa(a + ':' + b) } were removed\n";
	private const string _MEMBER_FORMAT = "\tmember{0:D4}: 'value {0:D4}',\n";
	private const string _REGEX_LINE_FORMAT = "const pattern{0:D5} = /ab[/]c+\\d{{2}}x/gi;\n";
	private const string _HOLE_OPENING = "const hole = `${";
	private const string _HOLE_CLOSING = "}`;\n";

	private static readonly ImmutableArray<string> _ORDINARY_STATEMENTS =
	[
		"\tconst order = await getOrder(orderId);",
		"\tif (!order) { return null; }",
		"\tconst amount = order.lines.map((line) => line.total).reduce((a, b) => a + b, 0);",
		"\tlog(`Order ${orderId} totals ${amount}`);",
		"\tfor (const item of items) { names.push(item.name.toUpperCase()); }",
		"\tconst ratio = first / second / third;",
		"\tconst matcher = /ab[/]c+d/gi;"
	];

	private static readonly ImmutableArray<string> _ANCHOR_DENSE_NEAR_MISSES =
	[
		"\tconst label = credentialName;",
		"\tconsole.log('the Authorization header is disabled');",
		"\tconst author = authority + oauth + authenticate;",
		"\tconst key = btoa(host + ':' + port);",
		"\tconst cacheKey = Buffer.from(text).toString('base64');",
		"\tconst headers = { Accept: 'application/json', 'Content-Type': 'text/plain' };",
		"\tfetch(url, { credentials: 'include' });",
		"\tconst value = localStorage.getItem('theme');",
		"\t// Basic auth is deprecated; password and username appear in prose only",
		"\tconst proxyHeader = { 'Proxy-Authorization': 'Basic ' + proxyToken };",
		"\tconst config = { auth: { token: sessionToken } };",
		"\taxios.get(url, { headers: { Authorization: `Bearer ${token}` } });",
		"\txhr.setRequestHeader('Authorization', 'Bearer ' + token);",
		"\twindow.open(url, '_blank');",
		"\tconst mode = process.env.NODE_ENV;",
		"\tmail.createTransport({ host: mailHost, auth: { user: userName } });"
	];

	private static readonly ImmutableArray<string> _EVERY_CLASS_CONSTRUCTS =
	[
		"\taxios.get(url, { auth: { username: userName, password: 'exampleSecret' } });",
		"\tsuperagent.get(url).auth(userName, 'exampleSecret');",
		"\thttp.get('https://exampleUser:exampleSecret@host.example/path', handler);",
		"\tfetch(url, { headers: { Authorization: `Basic ${encoded}` } });",
		"\tconst header = 'Basic ' + btoa(userName + ':' + secret);",
		"\tconst legacy = 'Basic ' + Buffer.from(`${userName}:${secret}`).toString('base64');",
		"\tfunction buildBasicHeader(userName, secret) { return 'Basic ' + btoa(userName + ':' + secret); }",
		"\thttp.request({ host: hostName, auth: `${userName}:${secret}` }, handler);",
		"\txhr.open('GET', url, true, userName, 'exampleSecret');",
		"\tconst target = new URL(base);\n\ttarget.username = userName;\n\ttarget.password = 'exampleSecret';",
		"\tconst transport = openMailTransport({ host: mailHost, auth: { user: userName, pass: 'exampleSecret' } });",
		"\tsuperagent.get(url).auth(credentialPair);",
		"\tconst baseUrl = 'https://exampleUser:exampleSecret@host.example/path';",
		"\t$.ajax({ url: apiUrl, username: userName, password: 'exampleSecret' });",
		"\tgot(url, { username: userName, password: 'exampleSecret' });",
		"\trequest.get(url, { auth: { user: userName, pass: secret, sendImmediately: false } });",
		"\tconst encodedHeader = 'Basic ' + encodedPair;\n\tfetch(url, { headers: { Authorization: encodedHeader } });",
		"\tconst answer = window.prompt('Enter your password');",
		"\tconst pw = document.getElementById('password').value;",
		"\tconst stored = localStorage.getItem('password');",
		"\tconst pass = req.body.password;",
		"\trl.question('Enter your password: ', handler);",
		"\tconst fromEnv = process.env.API_PASSWORD;",
		"\tconst apiPassword = 'exampleSecretValue';",
		"\tconst value = await client.send(new GetSecretValueCommand({ SecretId: id }));"
	];

	private static readonly ImmutableArray<string> _OPEN_WINDOWS =
	[
		"\taxios.get(url, {",
		"\trequest.get(url).auth(",
		"\tsuperagent.get(url)",
		"\tconst headers = {",
		"\tnew Headers({",
		"\tgot(url, {",
		"\t$.ajax({",
		"\txhr.open('GET', url, true,",
		"\tbtoa(",
		"\tBuffer.from(",
		"\tconst target = new URL(base);",
		"\tfunction build(a) {",
		"describe('suite', () => {",
		"\thttp.request({ host: hostName,",
		"\tfetch('https://example.test/a',",
		"\trl.question('Enter the password'",
		"\tconst parsed = {"
	];

	private static readonly ImmutableArray<string> _LITERAL_AND_COMMENT_LINES =
	[
		"\t// axios.get(url, { auth: { username: userName, password: secret } }) was removed in the migration to tokens",
		"\t/* headers: { Authorization: 'Basic abc' } is deprecated */",
		"\tconst text = 'btoa(a + \":\" + b) and Authorization: Basic abc appear in prose';",
		"\tconst html = '<input type=\"text\" name=\"user\">';",
		"\tconst note = `the password ${name} was refreshed for ${user} at ${when}`;",
		"\t// const target = new URL(base); target.password = secret;"
	];

	private static readonly ImmutableArray<string> _LEXICAL_HEAVY_LINES =
	[
		"\tconst a = `x ${f('}')} ${g(`${h}`)} y`;",
		"\tconst re = /ab[/]cd\\/e/g;",
		"\tconst d = a / b / c;",
		"\tx = y /* a block comment */ / z;",
		"\tconst s = 'it\\'s';",
		"\tconst t = \"say \\\"hi\\\"\";",
		"\tconst u = items.filter((v) => v / 2 > 1).map((v) => /^x/.test(v));"
	];

	private static readonly ImmutableArray<string> _FLOW_LINES =
	[
		"\tconst pw = document.getElementById('password').value;",
		"\tconst pw = loginForm.elements.password.value;",
		"\tconst answer = window.prompt('Enter a value');",
		"\tconst stored = sessionStorage.getItem('userPassword');",
		"\tconst body = `<input type=\"password\" name=\"pw\">`;",
		"\tconst { username, password } = req.body;",
		"\trl._writeToOutput = function (text) { };",
		"\tconst questions = [{ type: 'password', name: 'pw', message: 'Enter' }];",
		"\tconst key = process.env['DB_PASS'];",
		"\tconst fromConfig = config.get('db.password') + nconf.get('secret');",
		"\tconst client = new SecretsClient(); await client.getSecretValue({ id });",
		"\tconst apiToken = 'exampleTokenValue';",
		"\tconst prompted = rl.question('Your password? ', done);",
		"\tprocess.stdin.setRawMode(true); // hidden input for the password"
	];

	/// <summary>One long-line shape per window family of the expressions, each repeating one group up to the line-length ceiling.</summary>
	private static readonly ImmutableArray<(string Slug, string Group)> _LONG_LINE_GROUPS =
	[
		("minified-like-code", "a.b(c,d)&&e[f]||g?h:i;var j=k+l*m-n;"),
		("division-chain", "a/b/c/d/e/f/g/h/"),
		("regex-literals-on-one-line", "x=/ab[/]c+/g;y=/d/;"),
		("template-literals-on-one-line", "t=`a${b}c`;u=`d`;"),
		("brace-runs", "{{{{{{{{"),
		("axios-calls-with-credentials", "axios.get(u,{auth:{username:a,password:b}});"),
		("axios-openings-without-a-close", "axios.get(u,{"),
		("axios-calls-with-nested-groups", "axios.get(u,{a:[{b:(c)}],d:'e'});"),
		("request-chain-openings", "request.get(u).set(a,b).auth("),
		("superagent-chain-links", "superagent.get(u).set('a','b').set('c','d').set('e','f')"),
		("headers-keys-without-a-close", "headers:{Accept:a,"),
		("header-assignments", "h['Authorization']='Basic '+t;h.set('Authorization',x);"),
		("btoa-pairs", "btoa(a+':'+b);"),
		("btoa-openings", "btoa("),
		("buffer-from-chains", "Buffer.from("),
		("new-buffer-chains", "new Buffer("),
		("buffer-pairs", "Buffer.from(a+':'+b).toString('base64');"),
		("userinfo-urls", "fetch('https://u:p@h.example/a');"),
		("xhr-open-openings", "x.open('GET',u,true,"),
		("auth-alternations", "Authorization||auth||password||pass||pwd||passwd||secret||token||"),
		("auth-key-objects", "{auth:{user:a,pass:b}}"),
		("credential-assignments", "password='lit';apiSecret=\"x\";"),
		("process-environment-reads", "process.env.API_PASSWORD;"),
		("apostrophes-in-comments", "axios.get(u,{a:don't, it's, can't, "),
		("unclosed-quotes", "axios.get('a,\"b,`c "),
		("describe-openings", "describe('s',()=>{"),
		("url-bindings-and-assignments", "const t=new URL(b);t.password=p;"),
		("ajax-and-got-openings", "$.ajax({url:a,got(u,{"),
		("nested-groups-past-depth-four", "f(((((x)))))g[[[[[y]]]]]")
	];

	private static readonly ImmutableArray<(string Slug, string Definition, int FillerLines, string Tail)> _DEFINITION_BLOCKS =
	[
		("url-binding-then-assignments", "const target = new URL(base);", _DEFINITION_FILLER_LINES, "target.username = userName;\ntarget.password = secret;\n"),
		("url-binding-then-no-use", "const target = new URL(base);", _DEFINITION_FILLER_LINES, "log(target.href);\n"),
		("function-head-then-no-token", "function build(userName, secret) {", _DEFINITION_FILLER_LINES, "}\n"),
		("function-head-then-token", "function build(userName, secret) {", _DEFINITION_FILLER_LINES, "\treturn 'Basic ' + btoa(userName + ':' + secret);\n}\n"),
		("prompt-declaration-then-call", "const prompt = require('prompt-lib')();", _DEFINITION_FILLER_LINES, "const answer = prompt('Enter your password');\n")
	];

	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		AddCeilingShapes(shapes);
		AddTheShapesTheDesignNames(shapes);
		AddLongLineShapes(shapes);
		AddDefinitionBlockShapes(shapes);

		return shapes.ToImmutable();
	}

	internal static string RelativePathOf(MatchBoundStressShape shape)
	{
		return string.Format(CultureInfo.InvariantCulture, "stress/{0}{1}", shape.Name, shape.FileExtension);
	}

	private static void AddCeilingShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("js-ordinary-at-ceiling", FillToCeiling(_ORDINARY_STATEMENTS)));
		shapes.Add(Shape("js-anchor-dense-near-misses", FillToCeiling(_ANCHOR_DENSE_NEAR_MISSES)));
		shapes.Add(Shape("js-every-class-at-match-density", FillToCeiling(_EVERY_CLASS_CONSTRUCTS)));
		shapes.Add(Shape("js-open-windows", FillWindowsToCeiling(_OPEN_WINDOWS)));
		shapes.Add(Shape("js-literals-and-comments", FillToCeiling(_LITERAL_AND_COMMENT_LINES)));
		shapes.Add(Shape("js-lexical-heavy", FillToCeiling(_LEXICAL_HEAVY_LINES)));
		shapes.Add(Shape("js-flow-signal-forms", FillToCeiling(_FLOW_LINES)));
		shapes.Add(Shape("js-describe-lines-without-it", FillToCeilingWithLines("describe('suite', () => {\n")));
		shapes.Add(Shape("js-generated-marker-lookalike-lines", FillToCeilingWithLines("// generated by hand, not auto, and do not worry\n")));
		shapes.Add(Shape("js-hashbang-then-ordinary-code", _SHEBANG_LINE + FillToCeiling(_ORDINARY_STATEMENTS)));
	}

	/// <summary>The shapes the cost gate needs, each with its own builder.</summary>
	private static void AddTheShapesTheDesignNames(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("js-long-line-minified-at-the-ceiling", LongLines("a.b(c,d)&&e[f]||g?h:i;var j=k+l*m-n;if(o){p()}else{q(r,s)}")));
		shapes.Add(Shape("js-regex-literal-per-line-for-ten-thousand-lines", RegexLiteralPerLine()));
		shapes.Add(Shape("js-templates-nested-to-depth-eight", FillToCeilingWithLines(NestedTemplateLine(_TEMPLATE_NESTING_DEPTH))));
		shapes.Add(Shape("js-ten-thousand-opening-braces-in-one-hole", OpeningBracesInOneHole()));
		shapes.Add(Shape("js-two-megabytes-of-line-comments", FillToCeilingWithLines(_COMMENT_LINE)));
		shapes.Add(Shape("js-two-megabytes-of-line-comments-with-anchors", FillToCeilingWithLines(_COMMENT_LINE_WITH_ANCHORS)));
		shapes.Add(Shape("js-large-object-literals", LargeObjectLiterals()));
		shapes.Add(Shape("js-credential-key-at-the-argument-window-bound", ObjectWithTheKeyAfter(_MEMBERS_BEFORE_THE_KEY_AT_THE_WINDOW)));
		shapes.Add(Shape("js-credential-key-beyond-the-argument-window-bound", ObjectWithTheKeyAfter(_MEMBERS_BEFORE_THE_KEY_BEYOND_THE_WINDOW)));
	}

	private static void AddLongLineShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string group) in _LONG_LINE_GROUPS)
		{
			shapes.Add(Shape($"js-long-line-{slug}", LongLines(group)));
		}
	}

	private static void AddDefinitionBlockShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string definition, int fillerLines, string tail) in _DEFINITION_BLOCKS)
		{
			shapes.Add(Shape($"js-definition-{slug}", DefinitionBlocks(definition, fillerLines, tail)));
		}
	}

	private static MatchBoundStressShape Shape(string name, string text)
	{
		return new MatchBoundStressShape { Name = name, Text = text, FileExtension = FILE_EXTENSION };
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

	private static string LongLines(string group)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + LONG_LINE_LENGTH_IN_CHARACTERS < TARGET_SIZE_IN_CHARACTERS)
		{
			int lineStart = builder.Length;

			while (builder.Length - lineStart + group.Length < LONG_LINE_LENGTH_IN_CHARACTERS)
			{
				builder.Append(group);
			}

			builder.Append(_LINE_BREAK);
		}

		return builder.ToString();
	}

	private static string RegexLiteralPerLine()
	{
		StringBuilder builder = new(_REGEX_LITERAL_LINES * _REGEX_LINE_FORMAT.Length);

		for (int line = 0; line < _REGEX_LITERAL_LINES; line++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _REGEX_LINE_FORMAT, line));
		}

		return builder.ToString();
	}

	/// <summary>A template nested to the given depth, every level closed by its closing brace and its backtick, written as one statement.</summary>
	private static string NestedTemplateLine(int depth)
	{
		string template = "`z`";

		for (int level = 1; level <= depth; level++)
		{
			template = $"`t{level}${{{template}}}`";
		}

		return $"const nested = {template};{_LINE_BREAK}";
	}

	private static string OpeningBracesInOneHole()
	{
		StringBuilder block = new();

		block.Append(_HOLE_OPENING).Append(_LINE_BREAK);

		for (int line = 0; line < _OPENING_BRACES_IN_ONE_HOLE / _BRACES_PER_LINE; line++)
		{
			block.Append('{', _BRACES_PER_LINE).Append(_LINE_BREAK);
		}

		block.Append(_HOLE_CLOSING);

		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + block.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(block);
		}

		return builder.ToString();
	}

	private static string LargeObjectLiterals()
	{
		StringBuilder literal = new();

		literal.Append("const options = {\n");

		for (int member = 0; member < _OBJECT_MEMBERS_PER_LITERAL; member++)
		{
			literal.Append(string.Format(CultureInfo.InvariantCulture, _MEMBER_FORMAT, member));
		}

		literal.Append("\theaders: { Authorization: 'Basic ' + encoded },\n\tauth: { username: userName, password: secret }\n};\n");

		return RepeatToCeiling(literal.ToString());
	}

	/// <summary>A call whose options object holds the given number of ordinary members before the credential key, repeated: the key lands inside the argument window, or beyond it.</summary>
	private static string ObjectWithTheKeyAfter(int members)
	{
		StringBuilder call = new();

		call.Append("axios.post(url, {\n");

		for (int member = 0; member < members; member++)
		{
			call.Append(string.Format(CultureInfo.InvariantCulture, _MEMBER_FORMAT, member));
		}

		call.Append("\tauth: { username: userName, password: secret }\n});\n");

		return RepeatToCeiling(call.ToString());
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
