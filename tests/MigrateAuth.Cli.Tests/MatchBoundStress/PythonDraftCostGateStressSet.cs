using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE STRESS SET OF THE `Python` DRAFT COST GATE, written beside the `TypeScript`, `JavaScript`, `Shell`, `PowerShell` and `VBNet` sets and read by the same kind of test: every
/// shape is legal, ordinary-looking `Python` a client repository could contain, filled to just under the reader's size ceiling (2 MiB per decoded file) with no line over the line ceiling (10,000 characters), and none is built to make
/// an expression backtrack catastrophically. What the shapes do is push every honest cost to the ceilings the engine already enforces: files at the size ceiling, lines at the line-length ceiling, anchors and matches at the densest a
/// generated module or a vendored library plausibly carries, and every bounded window of the draft expressions opened over and over without being closed.
///
/// THE `Python` SHAPES THE CATALOG NAMES ARE ALL HERE: long lines of `auth=(` openers that never close and of named-callee openers; the anchor-dense file (hundreds of `Authorization` words, the Basic scheme word and the encoder
/// names); f-string-heavy text (nested fields, format specifications, doubled braces); nested brackets at the depth bound of the argument window (four levels) and one level past it; a header mapping with thousands of keys; a
/// userinfo-URL line; a vendored-library shape (the file a virtual-environment folder holds, which is walked). Beside them, one long-line shape per window family of the draft expressions (the dense method calls
/// of the callee classes, the credential names of the flow signal that reads one, the prompt calls whose local-declaration look-behind reads thirty lines, the encoder pairs whose use-word window reads 300 characters either side,
/// the loopback servers whose span reads 1,000 characters either side), and the shapes of an ordinary, an anchor-dense near-miss, a match-dense and an open-window file.
///
/// THIS SET IS RUN over the draft expressions of <see cref="PythonDraftExpressionSet"/> by the draft cost gate. It is NOT a shipped stress set (no file here is named for a shipped gate, and the draft is never used to close an analyzer
/// set). Every shape is deterministic and invented: placeholders and generic names, no fragment of any real text.
/// </summary>
internal static class PythonDraftCostGateStressSet
{
	internal const int TARGET_SIZE_IN_CHARACTERS = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;

	internal const int LONG_LINE_LENGTH_IN_CHARACTERS = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _LINE_MARGIN_IN_CHARACTERS;

	internal const string LINE_BREAK = "\n";

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;
	private const int _LINE_MARGIN_IN_CHARACTERS = 16;
	private const int _BUILDER_SLACK_IN_CHARACTERS = 4096;
	private const int _FILLER_LINES_PER_WINDOW = 4;
	private const int _MAPPING_KEYS = 3_000;
	private const int _MAPPING_KEYS_PER_LINE = 3;
	private const int _GUARD_WINDOW_LINE_LENGTH_IN_CHARACTERS = 390;
	private static readonly int _COMMENT_OPENER_LENGTH = PythonTestDraftConstants.LINE_COMMENT_OPENING.Length + 1;
	private const char _FILLER_UNIT = 'x';
	private const string _FUNCTION_FORMAT = "def step{0:D6}():\n{1}";
	private const string _FILLER_STATEMENT = "\ttotal = first + second\n";
	private const string _SHEBANG_LINE = "#!/usr/bin/env python3\n";
	private const string _COMMENT_LINE = "# an ordinary comment line about the code below, with words and numbers 12345\n";
	private const string _COMMENT_LINE_WITH_ANCHORS = "# requests.get(url, auth=(\"user\", \"secret\")) and headers={\"Authorization\": \"Basic \" + token} were removed\n";
	private const string _DOCSTRING_LINE_WITH_ANCHORS = "\t\"\"\"The legacy client sent requests.get(url, auth=(\"user\", \"secret\")) with an Authorization: Basic header.\"\"\"\n";
	private const string _F_STRING_LINE = "\tvalue = f\"{first}:{second!r:>{width}} {mapping['key']} {{literal}} {when:%Y-%m-%d} {nested(f'{inner}')}\"\n";
	private const string _VENDORED_LIBRARY_BLOCK =
		"\"\"\"A small client helper in the shape of a library module a virtual-environment folder holds.\"\"\"\n"
		+ "import base64\nimport urllib.request\nfrom typing import Any, Dict, Optional\n\n"
		+ "class AuthorizationHelper:\n"
		+ "\t\"\"\"Builds the oauth token request for the author of a call; see https://example.test/docs/authority.\"\"\"\n\n"
		+ "\tdef __init__(self, base_url: str, timeout: float = 5.0) -> None:\n"
		+ "\t\tself.base_url = base_url\n\t\tself.timeout = timeout\n\t\tself.authenticated = False\n\n"
		+ "\tdef authenticate(self, token: Optional[str] = None) -> Dict[str, Any]:\n"
		+ "\t\ttry:\n\t\t\tpayload = base64.b64encode(token.encode() if token else b\"\")\n\t\texcept (ValueError, AttributeError) as error:\n\t\t\traise RuntimeError(f\"cannot encode {error}\") from error\n"
		+ "\t\treturn {\"token\": payload.decode(), \"url\": f\"{self.base_url}/token\", \"headers\": {\"Accept\": \"application/json\"}}\n\n";

	private static readonly ImmutableArray<string> _ORDINARY_STATEMENTS =
	[
		"\torder = get_order(order_id)",
		"\tif order is None:\n\t\treturn None",
		"\tamount = sum(line.total for line in order.lines)",
		"\tlog.info(\"Order %s totals %s\", order_id, amount)",
		"\tfor item in items:\n\t\tnames.append(item.name.upper())",
		"\tratio = first / second / third",
		"\tmatcher = re.compile(r\"ab[/]c+d\", re.I)",
		"\tscaled = lookup.get(key, 0) / scale"
	];

	private static readonly ImmutableArray<string> _ANCHOR_DENSE_NEAR_MISSES =
	[
		"\tcredentials = request.authorization",
		"\tvalue = request.headers[\"Authorization\"]",
		"\tsecurity = HTTPBasic()",
		"\tproxy_auth = HTTPProxyAuth(\"user\", \"secret\")",
		"\tdigest = HTTPDigestAuth(\"user\", \"secret\")",
		"\theaders = {\"Authorization\": f\"Bearer {token}\", \"X-API-Key\": api_key}",
		"\theaders = {\"Proxy-Authorization\": \"Basic \" + proxy_token}",
		"\tnote = \"use HTTPBasicAuth later; Authorization: Basic is described\"",
		"\tauthor = authority + oauth + authenticate",
		"\tkey = base64.b64encode(f\"{host}:{port}\".encode())",
		"\tcache_key = base64.urlsafe_b64encode(raw)",
		"\tpayload = base64.b64decode(header_value.split()[1])",
		"\tconnect(host=\"db.example.test\", user=\"user\", password=\"secret\")",
		"\tdatabase = connect(\"scheme-one://user:secret@db.example.test/data\")",
		"\trequests.post(token_url, data={\"grant_type\": \"client_credentials\", \"client_id\": client_id})",
		"\tresponse = requests.get(url, auth=creds)",
		"\tresponse = requests.get(url, auth=[user, password])",
		"\tserver.login(user, password)",
		"\tname = getpass.getuser()"
	];

	private static readonly ImmutableArray<string> _EVERY_CLASS_CONSTRUCTS =
	[
		"\tresponse = requests.get(\"https://example.test/items\", auth=HTTPBasicAuth(\"user\", \"secret\"))",
		"\tclient = httpx.Client(auth=httpx.BasicAuth(username=\"user\", password=\"secret\"))",
		"\tresponse = requests.get(\"https://example.test/items\", auth=(\"user\", \"secret\"))",
		"\tresponse = session.get(\"https://example.test/items\", auth=(\"user\", \"secret\"))",
		"\tsession.auth = (\"user\", \"secret\")",
		"\theaders = {\"Authorization\": \"Basic \" + encoded}",
		"\tsession.headers[\"Authorization\"] = f\"Basic {encoded}\"",
		"\trequest.add_header(\"Authorization\", \"Basic %s\" % encoded)",
		"\theaders = dict(Authorization=\"Basic {}\".format(encoded))",
		"\thandler = urllib.request.HTTPBasicAuthHandler(password_manager)",
		"\theaders = urllib3.util.make_headers(basic_auth=\"user:secret\")",
		"\ttoken = base64.b64encode(f\"{user}:{password}\".encode()).decode()  # Basic",
		"\tvalue = aiohttp.encode_basic_auth(\"user\", \"secret\")",
		"\tmanager.add_password(None, \"https://example.test/\", \"user\", \"secret\")",
		"\tclient = httpx.Client(auth=httpx.NetRCAuth())",
		"\tresponse = requests.get(\"https://user:secret@example.test/items\")",
		"\tBASE_URL = \"https://user:secret@example.test/items\"",
		"\tscheme_value = \"Basic \" + encoded",
		"\theaders = {\"Authorization\": scheme_value}",
		"\treturn {\"Authorization\": \"Basic dXNlcjpzZWNyZXQ=\"}",
		"\tpassword = getpass.getpass(\"Password: \")",
		"\tsecret_value = input(\"Enter your password: \")",
		"\twebbrowser.open(\"https://example.test/authorize\")",
		"\tserver = HTTPServer((\"127.0.0.1\", 8080), Handler)\n\tparams = parse_qs(query)\n\tauthorization_code = params[\"code\"][0]",
		"\tsecret_value = request.form[\"password\"]",
		"\tdef sign_in(password: str = Form(...)):\n\t\treturn password",
		"\tuser = authenticate(request, username=name, password=secret_value)",
		"\tvalue = request.session[\"user_id\"]",
		"\tvalue = os.environ[\"API_PASSWORD\"]",
		"\tvalue = config.get(\"service\", \"password\")",
		"\tlogin_name, account, secret_value = netrc.netrc().authenticators(host)",
		"\tapi_token = \"exampleTokenValue\"",
		"\tscheme = os.environ.get(\"AUTH_VALUE\", \"Basic dGVzdDp0ZXN0\")"
	];

	private static readonly ImmutableArray<string> _OPEN_WINDOWS =
	[
		"\tresponse = requests.get(url,",
		"\tclient = httpx.Client(",
		"\theaders = {",
		"\tsession.headers.update({",
		"\trequest.add_header(",
		"\theaders = dict(",
		"\turllib3.util.make_headers(",
		"\tvalue = base64.b64encode(",
		"\tmanager.add_password(None,",
		"\tresponse = session.post(url, data=(",
		"\tserver = HTTPServer((",
		"\tuser = authenticate(request,",
		"\tsecret_value = input(",
		"\tvalue = config.get(\"service\","
	];

	/// <summary>One long-line shape per window family of the draft expressions, each repeating one group up to the line-length ceiling.</summary>
	private static readonly ImmutableArray<(string Slug, string Group)> _LONG_LINE_GROUPS =
	[
		("auth-tuple-openers-that-never-close", "requests.get(url, auth=("),
		("named-callee-openers", "requests.get("),
		("unnamed-tuple-openers", "client.auth=("),
		("named-calls-with-tuples", "requests.get(url,auth=(a,b));"),
		("authorization-words", "Authorization authorization AUTHORIZATION Basic basic BASIC "),
		("authorization-keys-with-basic-values", "{\"Authorization\": \"Basic abc\"}, "),
		("carrier-openers-without-a-close", "headers={\"Accept\": \"a\", "),
		("f-string-heavy", "f\"{a}:{b!r:>{w}} {c['k']} {{x}} {d:%Y}\" f'{e}' rf\"{f}\" "),
		("nested-brackets-at-the-depth-bound", "requests.get(f1(f2[f3{f4(x)}]), auth=(\"u\", \"p\"));"),
		("nested-brackets-past-the-depth-bound", "requests.get(f1(f2[f3{f4(f5[x])}]), auth=(\"u\", \"p\"));"),
		("nested-brackets-never-closed", "requests.get(f1(f2[f3{f4(f5["),
		("userinfo-urls", "get(\"https://user:secret@example.test/a\"); "),
		("userinfo-openers-without-a-close", "get(\"http://u:p"),
		("dense-method-calls", "x.get(a).post(b).head(c).put(d).patch(e).delete(f).options(g).request(h).stream(i).Request(j);"),
		("encoder-pairs-without-a-use-word", "b64encode(f\"{h}:{p}\"); "),
		("encoder-pair-openers", "b64encode("),
		("credential-names", "password = \"x\"; api_token = 'y'; db_pass: str = \"z\"; "),
		("credential-name-openers-without-a-literal", "password = value; api_token: str = ; "),
		("input-prompts", "input(\"name\"); input(\"Enter your password: \"); "),
		("form-calls", "def f(password: str = Form(...)): pass; def g(note: str = Form(...)): pass; "),
		("loopback-servers", "HTTPServer((\"127.0.0.1\", 1), H); "),
		("unclosed-quotes", "requests.get('a, \"b, "),
		("config-reads", "config[\"a\"][\"password\"]; config.get(\"s\", \"token\"); "),
		("environment-reads", "os.environ[\"API_PASSWORD\"]; os.getenv(\"DB_PASS\"); "),
		("assignments-and-annotations", "a: Dict[str, List[int]] = b; c = 1; d = 'e'; "),
		("password-forms", "request.form[\"password\"]; request.POST.get(\"passwd\"); "),
		("session-and-django", "request.session[\"a\"]; authenticate(request, password=p); login(request, u); ")
	];

	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		AddCeilingShapes(shapes);
		AddTheShapesTheCatalogNames(shapes);
		AddLongLineShapes(shapes);
		AddWindowFamilyShapes(shapes);

		return shapes.ToImmutable();
	}

	internal static string RelativePathOf(MatchBoundStressShape shape)
	{
		return string.Format(CultureInfo.InvariantCulture, "stress/{0}{1}", shape.Name, shape.FileExtension);
	}

	private static void AddCeilingShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("py-ordinary-at-ceiling", FillToCeiling(_ORDINARY_STATEMENTS)));
		shapes.Add(Shape("py-anchor-dense-near-misses", FillToCeiling(_ANCHOR_DENSE_NEAR_MISSES)));
		shapes.Add(Shape("py-every-class-at-match-density", FillToCeiling(_EVERY_CLASS_CONSTRUCTS)));
		shapes.Add(Shape("py-open-windows", FillWindowsToCeiling(_OPEN_WINDOWS)));
		shapes.Add(Shape("py-two-megabytes-of-line-comments", FillToCeilingWithLines(_COMMENT_LINE)));
		shapes.Add(Shape("py-two-megabytes-of-line-comments-with-anchors", FillToCeilingWithLines(_COMMENT_LINE_WITH_ANCHORS)));
		shapes.Add(Shape("py-two-megabytes-of-docstrings-with-anchors", FillToCeilingWithLines(_DOCSTRING_LINE_WITH_ANCHORS)));
		shapes.Add(Shape("py-hashbang-then-ordinary-code", $"{_SHEBANG_LINE}{FillToCeiling(_ORDINARY_STATEMENTS)}"));
	}

	/// <summary>The shapes the draft cost gate names: the f-string-heavy file, the header mapping with thousands of keys, the vendored-library shape and the anchor-dense file.</summary>
	private static void AddTheShapesTheCatalogNames(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("py-f-string-heavy-lines-at-the-ceiling", FillToCeilingWithLines(_F_STRING_LINE)));
		shapes.Add(Shape("py-header-mapping-with-thousands-of-keys", RepeatToCeiling(HeaderMappingBlock())));
		shapes.Add(Shape("py-vendored-library-shape", RepeatToCeiling(_VENDORED_LIBRARY_BLOCK)));
		shapes.Add(Shape("py-anchor-dense-authorization-lines", FillToCeilingWithLines("\theaders = {\"Authorization\": \"Basic \" + token, \"authorization\": \"Basic \" + token}\n")));
	}

	private static void AddLongLineShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string group) in _LONG_LINE_GROUPS)
		{
			shapes.Add(Shape($"py-long-line-{slug}", LongLines(group)));
		}
	}

	/// <summary>
	/// The widest windows of the draft expressions at their widest: the thirty-line look-behind of the prompt signal (every line holds a prompt and is padded to the width of a guard line), the 300-character use-word window of the
	/// encoder class, and the 1,000-character span of the loopback receiver (every line a loopback server, padded).
	/// </summary>
	private static void AddWindowFamilyShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("py-input-prompts-in-full-lookbehind-windows", FillToCeilingWithLines(GuardWindowLine("secret_value = input(\"Enter your password: \"); "))));
		shapes.Add(Shape("py-encoder-pairs-with-use-words-at-the-window-edge", FillToCeilingWithLines(GuardWindowLine("pair = b64encode(user + \":\" + secret_value); "))));
		shapes.Add(Shape("py-loopback-servers-in-full-spans", FillToCeilingWithLines(GuardWindowLine("server = HTTPServer((\"127.0.0.1\", 8080), Handler); "))));
	}

	private static string GuardWindowLine(string carrier)
	{
		return $"{carrier}{PythonTestDraftConstants.LINE_COMMENT_OPENING} {new string(_FILLER_UNIT, _GUARD_WINDOW_LINE_LENGTH_IN_CHARACTERS - carrier.Length - _COMMENT_OPENER_LENGTH)}{LINE_BREAK}";
	}

	private static MatchBoundStressShape Shape(string name, string text)
	{
		return new MatchBoundStressShape { Name = name, Text = text, FileExtension = PythonTestDraftConstants.FILE_EXTENSION};
	}

	private static string FillToCeiling(ImmutableArray<string> statements)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);
		StringBuilder body = new();
		int functionNumber = 0;

		foreach (string statement in statements)
		{
			body.Append(statement).Append(LINE_BREAK);
		}

		while (builder.Length + body.Length + _FUNCTION_FORMAT.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _FUNCTION_FORMAT, functionNumber++, body));
		}

		return builder.ToString();
	}

	internal static string FillToCeilingWithLines(string line)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + line.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(line);
		}

		return builder.ToString();
	}

	/// <summary>The text cut at the last line feed within the limit, so that a scan reads whole lines only; a text within the limit is returned whole.</summary>
	internal static string PrefixAtALineBoundary(string text, int limitInCharacters)
	{
		string prefix = text;

		if (text.Length > limitInCharacters)
		{
			prefix = text[..(text.LastIndexOf(LINE_BREAK, limitInCharacters, StringComparison.Ordinal) + 1)];
		}

		return prefix;
	}

	private static string FillWindowsToCeiling(ImmutableArray<string> openers)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);
		int opener = 0;

		while (builder.Length + LONG_LINE_LENGTH_IN_CHARACTERS < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(openers[opener % openers.Length]).Append(LINE_BREAK);

			for (int filler = 0; filler < _FILLER_LINES_PER_WINDOW; filler++)
			{
				builder.Append(_FILLER_STATEMENT);
			}

			opener++;
		}

		return builder.ToString();
	}

	/// <summary>Lines of the group repeated up to the line-length ceiling, each between the prefix and the suffix, filling the file to just under the file-size ceiling.</summary>
	internal static string LongLines(string group, string prefix = "", string suffix = "")
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

			builder.Append(suffix).Append(LINE_BREAK);
		}

		return builder.ToString();
	}

	/// <summary>One mapping of thousands of keys, three to a line, closed by an authorization member; repeated to the ceiling.</summary>
	private static string HeaderMappingBlock()
	{
		StringBuilder block = new();

		block.Append("headers = {\n");

		for (int key = 0; key < _MAPPING_KEYS; key += _MAPPING_KEYS_PER_LINE)
		{
			block.Append('\t');

			for (int offset = 0; offset < _MAPPING_KEYS_PER_LINE; offset++)
			{
				block.Append(string.Format(CultureInfo.InvariantCulture, "\"x-key-{0:D5}\": \"value-{0:D5}\", ", key + offset));
			}

			block.Append(LINE_BREAK);
		}

		block.Append("\t\"Authorization\": \"Basic dXNlcjpzZWNyZXQ=\",\n}\n");

		return block.ToString();
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
