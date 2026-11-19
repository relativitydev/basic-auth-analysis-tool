using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE STRESS SET OF THE SHIPPED `Python` COST GATE: every shape is legal, ordinary-looking `Python` a client repository could contain, filled to just under the reader's size ceiling (2 MiB per decoded file) with no line over
/// the line ceiling (10,000 characters), and none is built to make an expression backtrack catastrophically. What the shapes do is push every honest cost to the ceilings the engine already enforces: files at the size ceiling, lines at
/// the line-length ceiling, anchors and matches at the densest a generated module or a vendored library plausibly carries, and every bounded window of the shipped expressions opened over and over without being closed.
///
/// THE SHAPES THE CATALOG NAMES: long lines of <c>auth=(</c> openers that never close and of named-callee openers; the anchor-dense file (hundreds of <c>Authorization</c> words, the Basic scheme word and the encoder names);
/// f-string-heavy text; nested brackets at the depth bound of the argument window (four levels) and one level past it; a header mapping with thousands of keys; a userinfo-URL line; a vendored-library shape (the file a
/// virtual-environment folder holds, which is walked); and one long-line shape per window family of the shipped expressions.
///
/// WHAT THE SHIPPED GATE ADDS, in two families. THE ARGUMENT-WINDOW SHAPES: the window of one call is capped by a span of characters and never balanced across lines without a cap, so the shapes stand the window at its unit
/// bound (1,000 units), one unit past it, at the line ceiling of characters with every unit a string literal at its bound (300 characters) and one past it, nested to the depth bound and one level past it, with a group at its unit
/// bound (120) and one past it, with the refusal of the token request at the end of a full window, and opened across lines and never closed. THE PER-FILE WORST SHAPES: a generated protocol-stub module
/// (a file named for the stub suffix, with serialized descriptors in bytes literals up to the line ceiling), long dictionary literals (one mapping of seventeen thousand entries and mappings written on one line up to the line
/// ceiling), and a data file at the 2 MiB ceiling.
///
/// Every shape is deterministic and invented: placeholders and generic names, no fragment of any real text. The set is written and compiled with the suite and RUN in the timing window, never in a normal run.
/// </summary>
internal static class PythonShippedCostGateStressSet
{
	internal const string FILE_EXTENSION = ".py";

	internal const string SHAPE_NAME_PREFIX = "py-shipped-";

	internal const string GENERATED_STUB_SHAPE_NAME = SHAPE_NAME_PREFIX + "per-file-generated-protocol-stub_pb2";

	internal const string ARGUMENT_WINDOW_SHAPE_NAME_PREFIX = SHAPE_NAME_PREFIX + "argument-window-";

	/// <summary>What every flow-signal shape of the set is named with, after the prefix of the set: the shapes the timing gate reports for the flow-signal expressions.</summary>
	internal const string FLOW_SIGNAL_SHAPE_SLUG_PREFIX = "flow-";

	internal const string FLOW_SIGNAL_SHAPE_NAME_PREFIX = $"{SHAPE_NAME_PREFIX}{FLOW_SIGNAL_SHAPE_SLUG_PREFIX}";

	internal const int TARGET_SIZE_IN_CHARACTERS = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;

	internal const int LONG_LINE_LENGTH_IN_CHARACTERS = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _LINE_MARGIN_IN_CHARACTERS;

	internal const string LINE_BREAK = "\n";

	/// <summary>The units of the argument window of one call, and the nesting depth, the units per nested level and the characters of a string unit, as the vocabulary of the expressions declares them.</summary>
	internal const int ARGUMENT_WINDOW_UNITS = 1_000;

	internal const int NESTING_DEPTH = 4;

	internal const int NESTED_GROUP_UNITS = 120;

	internal const int STRING_UNIT_CHARACTERS = 300;

	/// <summary>The blanks of a seam of the held-name shape that stand at its bound of 40 with the line break, and the characters of the operand name chain of a compound held declaration at its bound.</summary>
	internal const int SEAM_BLANKS_AT_THE_BOUND = 39;

	internal const int COMPOUND_NAME_CHAIN_CHARACTERS = 64;

	private const int _NEAR_MISS_MEMBER_NAME_CHARACTERS = 60;
	private const int _ANNOTATION_AT_THE_BOUND_CHARACTERS = 98;
	private const int _CLOSING_PARENTHESIS_AND_BREAK_WIDTH = 2;
	private const int _ONE_UNIT_PAST_THE_BOUND = 1;
	private const char _NEAR_MISS_MEMBER_UNIT = 'a';
	private const char _ANNOTATION_UNIT = 'T';

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;
	private const int _LINE_MARGIN_IN_CHARACTERS = 16;
	private const int _BUILDER_SLACK_IN_CHARACTERS = 4096;
	private const int _FILLER_LINES_PER_WINDOW = 4;
	private const int _MAPPING_KEYS = 3_000;
	private const int _MAPPING_KEYS_PER_LINE = 3;
	private const int _GUARD_WINDOW_LINE_LENGTH_IN_CHARACTERS = 390;
	private const int _SERIALIZED_ESCAPE_WIDTH = 4;
	private const int _DATA_VALUES_PER_LINE = 8;
	private const int _DICTIONARY_ENTRIES = 17_000;
	private const char _FILLER_UNIT = 'x';
	private const char _SEAM_BLANK = ' ';
	private const string _FUNCTION_FORMAT = "def step{0:D6}():\n{1}";
	private const string _FILLER_STATEMENT = "\ttotal = first + second\n";
	private const string _SHEBANG_LINE = "#!/usr/bin/env python3\n";
	private const string _COMMENT_OPENING = "#";
	private const string _COMMENT_LINE = "# an ordinary comment line about the code below, with words and numbers 12345\n";
	private const string _COMMENT_LINE_WITH_ANCHORS = "# requests.get(url, auth=(\"user\", \"secret\")) and headers={\"Authorization\": \"Basic \" + token} were removed\n";
	private const string _DOCSTRING_LINE_WITH_ANCHORS = "\t\"\"\"The legacy client sent requests.get(url, auth=(\"user\", \"secret\")) with an Authorization: Basic header.\"\"\"\n";
	private const string _F_STRING_LINE = "\tvalue = f\"{first}:{second!r:>{width}} {mapping['key']} {{literal}} {when:%Y-%m-%d} {nested(f'{inner}')}\"\n";
	private const string _TARGET_TAIL = ", auth=(\"user\", \"secret\"))";
	private const string _NAMED_CALL_OPENING = "requests.get(";
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

	/// <summary>One long-line shape per window family of the shipped expressions, each repeating one group up to the line-length ceiling.</summary>
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
		("quote-bearing-f-string-pairs", "b64encode(f\"{cfg['user']['a']}:{os.getenv('P', \"x\")}\".encode()); "),
		("quote-bearing-f-string-fields-without-a-colon", "b64encode(f\"{d.get(k['u'])}{d['a:b']}{e['x']}"),
		("held-name-openers", "b64encode(credentials.encode("),
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

	/// <summary>
	/// One long-line shape per window family of the flow-signal expressions (<c>EX-PY-120</c> to <c>EX-PY-133</c>), each repeating one group up to the line-length ceiling: the call anchors, the span of the loopback
	/// receiver in both directions, the narrow credential words and their label refusals, the look-behind of the form annotation, the framework names, the receivers of the configuration read, the credential-named
	/// assignment and the credential keys of the environment and the configuration.
	/// </summary>
	private static readonly ImmutableArray<(string Slug, string Group)> _FLOW_LONG_LINE_GROUPS =
	[
		("getpass-calls", "getpass.getpass(\"Password: \"); getpass.getuser(); "),
		("webbrowser-calls", "webbrowser.open_new_tab(url); webbrowser.open(url); webbrowser.get(); "),
		("webbrowser-names-without-a-call", "webbrowser.open webbrowser.open_new webbrowser.open_new_tab "),
		("loopback-servers-with-query-parsers-and-no-code-key", "HTTPServer((\"127.0.0.1\", 1), H); parse_qs(q); parse_qsl(r); "),
		("loopback-servers-after-code-keys-without-a-query-parser", "x['code'] = 1; HTTPServer((\"::1\", 1), H); "),
		("loopback-servers-with-code-reads", "params = parse_qs(query)[\"code\"]; TCPServer((\"localhost\", 1), H); "),
		("request-form-credential-reads", "request.form[\"password\"]; request.POST.get(\"pwd_label\"); request.form['user_password_hint']; "),
		("oauth-form-names", "OAuth2PasswordRequestForm OAuth2PasswordRequestFormStrict OAuth2PasswordRequestForms "),
		("authenticate-and-login-calls", "authenticate(request, username=u, password=p); login(request, u); login(user); "),
		("authenticate-openers-without-a-close", "authenticate(request, a="),
		("framework-session-subscripts", "request.session[\"key\"]; request.sessions[\"key\"]; session_id = 1; "),
		("environment-openers-without-a-key", "os.environ[ os.getenv( os.environ.get( os.environ["),
		("configuration-receivers-with-credential-members", "config.db_password; settings.api_token; cfg.secret_key; parser.passwd; conf.creds; "),
		("configuration-receivers-with-long-near-miss-members", $"settings.{new string(_NEAR_MISS_MEMBER_UNIT, _NEAR_MISS_MEMBER_NAME_CHARACTERS)}; "),
		("configuration-subscripts-and-gets", "config[\"section\"][\"user\"]; settings.get(\"s\", \"timeout\"); parser.get('a', 'b'); conf[\"password\"]; "),
		("configuration-receivers-without-a-key", "config[ settings.get( cfg.get( parser[ config. settings. "),
		("authenticators-calls", "netrc.netrc().authenticators(host); "),
		("credential-named-assignments-with-f-strings", "api_token = f\"{a}\"; secret = f'x{b}'; pw = rb'z'; password = fr\"{c}\"; "),
		("credential-names-with-annotations-at-the-bound", $"db_password: {new string(_ANNOTATION_UNIT, _ANNOTATION_AT_THE_BOUND_CHARACTERS)} = \"x\"; "),
		("credential-name-prefixes-without-an-assignment", "password_manager passwd_hash api_token_url secret_key_name ")
	];

	private static readonly ImmutableArray<string> _FLOW_ANCHOR_DENSE_NEAR_MISSES =
	[
		"\tname = getpass.getuser()",
		"\tbrowser = webbrowser.get()",
		"\tvalue = request.form_data",
		"\tmode = request.formats",
		"\tbearer = OAuth2PasswordBearer(tokenUrl=token_url)",
		"\tauthenticate(request)",
		"\tlogin(user)",
		"\tvalue = request.sessions",
		"\tsession_id = 1",
		"\tvalue = os.environ.copy()",
		"\tpath = os.path.join(first, second)",
		"\ttimeout = config.timeout",
		"\tdebug = settings.DEBUG",
		"\tparser.add_argument(\"--name\")",
		"\tserver = HTTPServer((\"0.0.0.0\", 80), Handler)",
		"\tparams = parse_qs(query)",
		"\tfile = netrc.netrc()",
		"\tpassword_hint = \"remember the first pet\"",
		"\tpassword_label = f\"{prefix}\"",
		"\tuser_password_placeholder = \"\""
	];

	private static readonly ImmutableArray<string> _FLOW_OPEN_WINDOWS =
	[
		"\tuser = authenticate(request,",
		"\tsecret_value = input(",
		"\tserver = HTTPServer((",
		"\tvalue = os.environ[",
		"\tvalue = config.get(",
		"\tcode = parse_qs(",
		"\tdef sign_in(password: str = Form(",
		"\tdetails = netrc.netrc().authenticators(",
		"\twebbrowser.open("
	];

	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		AddCeilingShapes(shapes);
		AddTheShapesTheCatalogNames(shapes);
		AddLongLineShapes(shapes);
		AddWindowFamilyShapes(shapes);
		AddArgumentWindowShapes(shapes);
		AddPerFileWorstShapes(shapes);
		AddFlowSignalShapes(shapes);

		return shapes.ToImmutable();
	}

	/// <summary>The shapes of the flow-signal expressions alone, in the order <see cref="Build"/> adds them. The timing gate times them with the remaining shapes of the set.</summary>
	internal static ImmutableArray<MatchBoundStressShape> BuildTheFlowSignalShapes()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		AddFlowSignalShapes(shapes);

		return shapes.ToImmutable();
	}

	/// <summary>The shapes of the argument window alone, in the order <see cref="Build"/> adds them.</summary>
	internal static ImmutableArray<MatchBoundStressShape> BuildTheArgumentWindowShapes()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		AddArgumentWindowShapes(shapes);

		return shapes.ToImmutable();
	}

	/// <summary>The per-file worst shapes alone: the generated protocol stub, the long dictionary literals and the data file at the ceiling.</summary>
	internal static ImmutableArray<MatchBoundStressShape> BuildThePerFileWorstShapes()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		AddPerFileWorstShapes(shapes);

		return shapes.ToImmutable();
	}

	/// <summary>The path a shape stands at: under its own extension, and a shape named for the generated-stub suffix keeps that suffix as the end of its name.</summary>
	internal static string RelativePathOf(MatchBoundStressShape shape)
	{
		return string.Format(CultureInfo.InvariantCulture, "stress/{0}{1}", shape.Name, shape.FileExtension);
	}

	private static void AddCeilingShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("ordinary-at-ceiling", FillToCeiling(_ORDINARY_STATEMENTS)));
		shapes.Add(Shape("anchor-dense-near-misses", FillToCeiling(_ANCHOR_DENSE_NEAR_MISSES)));
		shapes.Add(Shape("every-class-at-match-density", FillToCeiling(_EVERY_CLASS_CONSTRUCTS)));
		shapes.Add(Shape("open-windows", FillWindowsToCeiling(_OPEN_WINDOWS)));
		shapes.Add(Shape("two-megabytes-of-line-comments", FillToCeilingWithLines(_COMMENT_LINE)));
		shapes.Add(Shape("two-megabytes-of-line-comments-with-anchors", FillToCeilingWithLines(_COMMENT_LINE_WITH_ANCHORS)));
		shapes.Add(Shape("two-megabytes-of-docstrings-with-anchors", FillToCeilingWithLines(_DOCSTRING_LINE_WITH_ANCHORS)));
		shapes.Add(Shape("hashbang-then-ordinary-code", $"{_SHEBANG_LINE}{FillToCeiling(_ORDINARY_STATEMENTS)}"));
	}

	/// <summary>The f-string-heavy file, the header mapping with thousands of keys, the vendored-library shape and the anchor-dense file.</summary>
	private static void AddTheShapesTheCatalogNames(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("f-string-heavy-lines-at-the-ceiling", FillToCeilingWithLines(_F_STRING_LINE)));
		shapes.Add(Shape("header-mapping-with-thousands-of-keys", RepeatToCeiling(HeaderMappingBlock())));
		shapes.Add(Shape("vendored-library-shape", RepeatToCeiling(_VENDORED_LIBRARY_BLOCK)));
		shapes.Add(Shape("anchor-dense-authorization-lines", FillToCeilingWithLines("\theaders = {\"Authorization\": \"Basic \" + token, \"authorization\": \"Basic \" + token}\n")));
	}

	private static void AddLongLineShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string group) in _LONG_LINE_GROUPS)
		{
			shapes.Add(Shape($"long-line-{slug}", LongLines(group)));
		}
	}

	/// <summary>
	/// The widest windows of the shipped expressions at their widest: the thirty-line look-behind of the prompt signal (every line holds a prompt and is padded to the width of a guard line), the 300-character use-word window of the
	/// encoder class, and the 1,000-character span of the loopback receiver (every line a loopback server, padded).
	/// </summary>
	private static void AddWindowFamilyShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("input-prompts-in-full-lookbehind-windows", FillToCeilingWithLines(GuardWindowLine("secret_value = input(\"Enter your password: \"); "))));
		shapes.Add(Shape("encoder-pairs-with-use-words-at-the-window-edge", FillToCeilingWithLines(GuardWindowLine("pair = b64encode(user + \":\" + secret_value); "))));
		shapes.Add(Shape("loopback-servers-in-full-spans", FillToCeilingWithLines(GuardWindowLine("server = HTTPServer((\"127.0.0.1\", 8080), Handler); "))));
		shapes.Add(Shape("held-name-seams-at-the-bound", FillToCeilingWithLines(HeldNameSeamBlock())));
		shapes.Add(Shape("compound-held-declarations", FillToCeilingWithLines(CompoundDeclarationBlock())));
		shapes.Add(Shape("held-scheme-carriers-in-full-lookbehind-windows", FillToCeilingWithLines(GuardWindowLine("response = requests.get(url, headers={\"Authorization\": token_text}); "))));
		shapes.Add(Shape("held-literal-declarations-before-carriers", FillToCeilingWithLines(HeldLiteralBlock())));
	}

	/// <summary>
	/// THE ARGUMENT WINDOW, WIDTH CAPPED BY A SPAN OF CHARACTERS. The window of one call reads at most 1,000 units, a string unit at most 300 characters, a nested group at most 120 units per level to a depth of four: a width
	/// that is bounded in characters by the line ceiling and never balanced across lines without a cap. Each shape fills the file with lines of its own call and repeats it to the ceiling.
	/// </summary>
	private static void AddArgumentWindowShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		string atTheUnitBound = new(_FILLER_UNIT, ARGUMENT_WINDOW_UNITS - 2);
		string pastTheUnitBound = new(_FILLER_UNIT, ARGUMENT_WINDOW_UNITS + 1);
		string stringAtTheBound = new(_FILLER_UNIT, STRING_UNIT_CHARACTERS);
		string stringPastTheBound = new(_FILLER_UNIT, STRING_UNIT_CHARACTERS + 1);

		AddArgumentWindow(shapes, "at-the-unit-bound", FillToCeilingWithLines($"{_NAMED_CALL_OPENING}{atTheUnitBound}{_TARGET_TAIL}\n"));
		AddArgumentWindow(shapes, "past-the-unit-bound", FillToCeilingWithLines($"{_NAMED_CALL_OPENING}{pastTheUnitBound}{_TARGET_TAIL}\n"));
		AddArgumentWindow(shapes, "capped-at-the-line-ceiling-of-characters", LongLines($"{_NAMED_CALL_OPENING}{new string(_FILLER_UNIT, LONG_LINE_LENGTH_IN_CHARACTERS - _NAMED_CALL_OPENING.Length - _TARGET_TAIL.Length - 1)}{_TARGET_TAIL}"));
		AddArgumentWindow(shapes, "strings-at-the-string-unit-bound", LongLines($"\"{stringAtTheBound}\", ", _NAMED_CALL_OPENING, _TARGET_TAIL));
		AddArgumentWindow(shapes, "strings-past-the-string-unit-bound", LongLines($"\"{stringPastTheBound}\", ", _NAMED_CALL_OPENING, _TARGET_TAIL));
		AddArgumentWindow(shapes, "nested-to-the-depth-bound", LongLines("f1(f2[f3{f4(x)}]), ", _NAMED_CALL_OPENING, _TARGET_TAIL));
		AddArgumentWindow(shapes, "nested-past-the-depth-bound", LongLines("f1(f2[f3{f4(f5[x])}]), ", _NAMED_CALL_OPENING, _TARGET_TAIL));
		AddArgumentWindow(shapes, "group-at-the-unit-bound-per-level", LongLines($"f({new string(_FILLER_UNIT, NESTED_GROUP_UNITS)}), ", _NAMED_CALL_OPENING, _TARGET_TAIL));
		AddArgumentWindow(shapes, "group-past-the-unit-bound-per-level", LongLines($"f({new string(_FILLER_UNIT, NESTED_GROUP_UNITS + 1)}), ", _NAMED_CALL_OPENING, _TARGET_TAIL));
		AddArgumentWindow(shapes, "guarded-by-the-token-request-word-at-the-end-of-a-full-window", FillToCeilingWithLines($"requests.post({atTheUnitBound}{_TARGET_TAIL[..^1]}, data={{\"grant_type\": \"x\"}})\n"));
		AddArgumentWindow(shapes, "opened-across-lines-and-never-closed", FillWindowsToCeiling([$"\t{_NAMED_CALL_OPENING}url,", "\trequests.post(url, data=(", "\tclient = httpx.Client("]));
		AddArgumentWindow(shapes, "dense-openers-of-every-named-callee", LongLines("requests.get(requests.post(httpx.Client(httpx.get(requests.request(httpx.AsyncClient("));
	}

	/// <summary>
	/// THE FLOW-SIGNAL SHAPES: one long line per window family of the flow-signal expressions; the file of anchor-dense near misses of their anchors (the call names, the receivers and the credential words, each one
	/// character off a construct); the windows of their calls opened and never closed; the three windows that are widest, in the look-behind of the prompt (thirty lines, each holding a local declaration of the name the
	/// refusal looks for), in the span of the loopback receiver (a thousand characters, every bind with query parsers and no read of the key) and in the argument window of the Django call (at and one past its unit bound);
	/// and the form annotation, whose look-behind reaches a credential word a hundred and fifty characters back from every opening of the call.
	/// </summary>
	private static void AddFlowSignalShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		string atTheUnitBound = new(_FILLER_UNIT, ARGUMENT_WINDOW_UNITS - _CLOSING_PARENTHESIS_AND_BREAK_WIDTH);
		string pastTheUnitBound = new(_FILLER_UNIT, ARGUMENT_WINDOW_UNITS + _ONE_UNIT_PAST_THE_BOUND);

		foreach ((string slug, string group) in _FLOW_LONG_LINE_GROUPS)
		{
			shapes.Add(Shape($"{FLOW_SIGNAL_SHAPE_SLUG_PREFIX}long-line-{slug}", LongLines(group)));
		}

		shapes.Add(Shape($"{FLOW_SIGNAL_SHAPE_SLUG_PREFIX}long-line-form-openers-after-a-credential-annotation", LongLines("Form(a), ", "def f(password: str = ", ")")));
		shapes.Add(Shape($"{FLOW_SIGNAL_SHAPE_SLUG_PREFIX}anchor-dense-near-misses", FillToCeiling(_FLOW_ANCHOR_DENSE_NEAR_MISSES)));
		shapes.Add(Shape($"{FLOW_SIGNAL_SHAPE_SLUG_PREFIX}open-windows", FillWindowsToCeiling(_FLOW_OPEN_WINDOWS)));
		shapes.Add(Shape($"{FLOW_SIGNAL_SHAPE_SLUG_PREFIX}input-prompts-with-local-declarations-in-full-lookbehind-windows", FillToCeilingWithLines(GuardWindowLine("def run(input): secret_value = input(\"Enter your password: \"); "))));
		shapes.Add(Shape($"{FLOW_SIGNAL_SHAPE_SLUG_PREFIX}loopback-servers-with-query-parsers-in-full-spans", FillToCeilingWithLines(GuardWindowLine("server = HTTPServer((\"localhost\", 8080), H); params = parse_qs(query); "))));
		shapes.Add(Shape($"{FLOW_SIGNAL_SHAPE_SLUG_PREFIX}authenticate-window-at-the-unit-bound", FillToCeilingWithLines($"authenticate({atTheUnitBound})\n")));
		shapes.Add(Shape($"{FLOW_SIGNAL_SHAPE_SLUG_PREFIX}authenticate-window-past-the-unit-bound", FillToCeilingWithLines($"authenticate({pastTheUnitBound})\n")));
	}

	private static void AddArgumentWindow(ImmutableArray<MatchBoundStressShape>.Builder shapes, string slug, string text)
	{
		shapes.Add(Shape($"argument-window-{slug}", text));
	}

	/// <summary>
	/// THE PER-FILE WORST SHAPES: a generated protocol-stub module, long dictionary literals and a data file at the 2 MiB ceiling, each the heaviest file of its kind a client repository could hold and the reader would
	/// still accept.
	/// </summary>
	private static void AddPerFileWorstShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("per-file-generated-protocol-stub_pb2", GeneratedProtocolStub()));
		shapes.Add(Shape("per-file-long-dictionary-literal-of-seventeen-thousand-entries", RepeatToCeiling(LongDictionaryBlock())));
		shapes.Add(Shape("per-file-dictionaries-on-one-line-at-the-line-ceiling", LongLines("\"key-00001\": \"value-00001\", \"auth\": {\"author\": \"a\", \"authority\": [1, 2, 3]}, ", "DATA = {", "}")));
		shapes.Add(Shape("per-file-two-megabyte-data-file", DataFileAtTheCeiling()));
	}

	/// <summary>The encoder call of the held-name shape that spans lines, each seam a run of 39 blanks and one line break (the bound of a seam is 40), closed by a comment holding the use word.</summary>
	private static string HeldNameSeamBlock()
	{
		string seam = $"{LINE_BREAK}{new string(_SEAM_BLANK, SEAM_BLANKS_AT_THE_BOUND)}";

		return $"value = b64encode({seam}credentials.encode({seam}\"utf-8\"{seam}){seam}){LINE_BREAK}# Basic{LINE_BREAK}";
	}

	/// <summary>The compound held scheme declarations, each at the bound of its operand: three call groups, a group of 120 units, an attribute chain of the bound, and the forms with a percent, a format call and an f-string.</summary>
	private static string CompoundDeclarationBlock()
	{
		string groupAtTheBound = new(_FILLER_UNIT, NESTED_GROUP_UNITS);
		string chainAtTheBound = new(_FILLER_UNIT, COMPOUND_NAME_CHAIN_CHARACTERS);

		string[] declarations =
		[
			"auth_value = \"Basic \" + base64.b64encode(pair.strip(x(y))).decode(\"ascii\")",
			$"auth_value: str = \"Basic \" + f({groupAtTheBound}).g({groupAtTheBound}).h({groupAtTheBound})",
			$"auth_value = \"Basic \" + {chainAtTheBound}",
			$"auth_value = \"Basic %s\" % {chainAtTheBound}.a(b)",
			$"auth_value = \"Basic {{}}\".format({groupAtTheBound})",
			$"auth_value = f\"Basic {{{chainAtTheBound}}}\"",
			"auth_value = \"Basic \" + first(a)(b)(c)(d)",
			"auth_value = \"Basic \" + token_text + suffix_text"
		];

		return $"{string.Join(LINE_BREAK, declarations)}{LINE_BREAK}";
	}

	/// <summary>A held literal with a token, then a carrier that uses it, then filler: the look-behind of the carrier finds the declaration.</summary>
	private static string HeldLiteralBlock()
	{
		return $"auth_value = \"Basic dXNlcjpwYXNzZm9vYmFy\"{LINE_BREAK}{_FILLER_STATEMENT}{_FILLER_STATEMENT}response = requests.get(url, headers={{\"Authorization\": auth_value}}){LINE_BREAK}";
	}

	private static string GuardWindowLine(string carrier)
	{
		return $"{carrier}{_COMMENT_OPENING} {new string(_FILLER_UNIT, _GUARD_WINDOW_LINE_LENGTH_IN_CHARACTERS - carrier.Length - _COMMENT_OPENING.Length - 1)}{LINE_BREAK}";
	}

	private static MatchBoundStressShape Shape(string slug, string text)
	{
		return new MatchBoundStressShape { Name = $"{SHAPE_NAME_PREFIX}{slug}", Text = text, FileExtension = FILE_EXTENSION };
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

	/// <summary>
	/// A module the way a protocol-buffer compiler writes one: a banner, the imports, and the serialized file descriptor as bytes literals of escaped octets, each statement filling a line up to the line ceiling, to the file
	/// ceiling. The name of the shape ends in the stub suffix, so the file stands at a path the generation marker names.
	/// </summary>
	private static string GeneratedProtocolStub()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);
		int part = 0;

		builder.Append("# -*- coding: utf-8 -*-\n# Generated by the protocol buffer compiler.  DO NOT EDIT!\n");
		builder.Append("from google.protobuf import descriptor as _descriptor\nfrom google.protobuf import descriptor_pool as _descriptor_pool\n\n");

		while (builder.Length + LONG_LINE_LENGTH_IN_CHARACTERS < TARGET_SIZE_IN_CHARACTERS)
		{
			StringBuilder line = new();

			line.Append(string.Format(CultureInfo.InvariantCulture, "_SERIALIZED_PART_{0:D5} = b'", part++));

			for (int octet = 0; line.Length + _SERIALIZED_ESCAPE_WIDTH + 2 < LONG_LINE_LENGTH_IN_CHARACTERS; octet++)
			{
				line.Append(string.Format(CultureInfo.InvariantCulture, "\\x{0:x2}", (octet * 7) % 256));
			}

			builder.Append(line).Append("'\n");
		}

		builder.Append("DESCRIPTOR = _descriptor_pool.Default().AddSerializedFile(_SERIALIZED_PART_00000)\n");

		return builder.ToString();
	}

	/// <summary>One mapping of seventeen thousand entries, each a short nested mapping, filling the file to just under the ceiling.</summary>
	private static string LongDictionaryBlock()
	{
		StringBuilder block = new();

		block.Append("DATA = {\n");

		for (int entry = 0; entry < _DICTIONARY_ENTRIES; entry++)
		{
			block.Append(string.Format(CultureInfo.InvariantCulture, "\t\"key-{0:D5}\": {{\"name\": \"value-{0:D5}\", \"tags\": [\"a\", \"b\", \"c\"], \"weight\": 1.5, \"nested\": {{\"x\": 1, \"y\": [1, 2, 3]}}}},\n", entry));
		}

		block.Append("}\n");

		return block.ToString();
	}

	/// <summary>A data file at the size ceiling: tables of integers, hexadecimal strings and floating-point values, eight to a line, with the words of the anchors as ordinary keys.</summary>
	private static string DataFileAtTheCeiling()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);
		int row = 0;

		builder.Append("AUTHORS = [\"author\", \"authority\", \"oauth\", \"authenticate\"]\nTABLE = [\n");

		while (builder.Length + LONG_LINE_LENGTH_IN_CHARACTERS < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append('\t');

			for (int column = 0; column < _DATA_VALUES_PER_LINE; column++)
			{
				builder.Append(string.Format(CultureInfo.InvariantCulture, "({0}, \"{0:x8}\", {1}.5), ", row * _DATA_VALUES_PER_LINE + column, column));
			}

			builder.Append('\n');
			row++;
		}

		builder.Append("]\n");

		return builder.ToString();
	}
}
