using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE `Go` SHAPES OF THE DRAFT COST GATE, written beside the `JavaScript`, `TypeScript` and `Shell` sets and read by the same kind of test: every shape is legal, ordinary-looking Go that a client
/// repository could contain, and none is built to make an expression backtrack catastrophically. What the shapes do is push every honest cost to the ceilings the engine already enforces.
/// The volume rows of the design (C3, per-file lexical scan and recognition: V1 typical, V2 worst; C8, long-line files) name the three families:
///
///   * THE TYPICAL V1 SHAPES (<see cref="TYPICAL_PREFIX"/>): ordinary decoded files of about <see cref="TYPICAL_SIZE_IN_CHARACTERS"/> characters, a plain service, a client with the constructs of every
///     class at a realistic density, and a test file with its near-misses.
///   * THE WORST V2 SHAPES (<see cref="WORST_PREFIX"/>): files filled to just under the reader's 2 MiB ceiling with every honest cost: ordinary code, anchor-dense near-misses, every class at match
///     density, every bounded window of the catalog opened over and over without being closed, raw literals that hold anchors, line comments that hold anchors, a generated table, the definition windows.
///   * THE LONG-LINE SHAPES (<see cref="LONG_LINE_PREFIX"/>): files of <see cref="LONG_LINE_FILE_SIZE_IN_CHARACTERS"/> characters (the design's 500,000 bytes in all) whose lines run to
///     9,000 to 10,000 characters: a construct-free one, the anchor-dense ones (<c>SetBasicAuth(</c> runs, <c>Authorization</c> runs) and one per window family of the expressions.
///
/// The stress set is run over the draft expressions of <see cref="GoDraftExpressions"/> by <see cref="GoDraftCostGateTimingTests"/>, and over the lexical profile of the draft. Every shape is deterministic and
/// invented: placeholders and generic names, no fragment of any real text.
/// </summary>
internal static class GoDraftCostGateStressSet
{
	internal const string TYPICAL_PREFIX = "go-v1-";

	internal const string WORST_PREFIX = "go-v2-";

	internal const string LONG_LINE_PREFIX = "go-long-line-";

	internal const string FILE_EXTENSION = ".go";

	internal const int TYPICAL_SIZE_IN_CHARACTERS = 64 * 1024;

	internal const int WORST_SIZE_IN_CHARACTERS = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;

	internal const int LONG_LINE_LENGTH_IN_CHARACTERS = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _LINE_MARGIN_IN_CHARACTERS;

	internal const int LONG_LINE_FILE_SIZE_IN_CHARACTERS = 500_000;

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;
	private const int _LINE_MARGIN_IN_CHARACTERS = 16;
	private const int _BUILDER_SLACK_IN_CHARACTERS = 4096;
	private const int _FILLER_LINES_PER_WINDOW = 4;
	private const int _DEFINITION_FILLER_LINES = 29;
	private const int _TABLE_ROWS = 20_000;
	private const int _LARGE_LITERAL_MEMBERS = 1_000;
	private const int _REPEAT_TAIL_MARGIN_IN_CHARACTERS = _BUILDER_SLACK_IN_CHARACTERS / 4;
	private const int _MEMBERS_BEFORE_THE_KEY_AT_THE_WINDOW = 85;
	private const int _MEMBERS_BEFORE_THE_KEY_BEYOND_THE_WINDOW = 100;
	private const string _LINE_BREAK = "\n";
	private const string _FUNCTION_FORMAT = "func step{0:D6}() {{\n{1}}}\n";
	private const string _FILLER_STATEMENT = "\ttotal := first + second\n";
	private const string _PACKAGE_LINE = "package sample\n\n";
	private const string _COMMENT_LINE = "// an ordinary comment line about the code below, with words and numbers 12345\n";
	private const string _COMMENT_LINE_WITH_ANCHORS = "// request.SetBasicAuth(u, p) and h.Set(\"Authorization\", \"Basic \"+x) were removed in favour of tokens\n";
	private const string _TABLE_ROW_FORMAT = "\t{{\"entry{0:D5}\", {0}, 0x{0:X4}, \"value {0:D5}\"}},\n";
	private const string _MEMBER_FORMAT = "\t\"member{0:D4}\": \"value {0:D4}\",\n";
	private const string _RAW_BLOCK = "const page{0:D5} = `\n<form method=\"post\" action=\"/save\">\n\t<input type=\"text\" name=\"name\">\n\t// Authorization: Basic is not sent here\n\trequest.SetBasicAuth(u, p)\n</form>\n`\n";

	private static readonly ImmutableArray<string> _ORDINARY_STATEMENTS =
	[
		"\torder, err := store.Order(orderID)",
		"\tif err != nil {\n\t\treturn nil, err\n\t}",
		"\tamount := 0\n\tfor _, line := range order.Lines {\n\t\tamount += line.Total\n\t}",
		"\tlog.Printf(\"order %d totals %d\", orderID, amount)",
		"\tnames := make([]string, 0, len(items))\n\tfor _, item := range items {\n\t\tnames = append(names, strings.ToUpper(item.Name))\n\t}",
		"\tratio := float64(first) / float64(second) / float64(third)",
		"\tpattern := regexp.MustCompile(`^ab[/]c+d$`)"
	];

	private static readonly ImmutableArray<string> _TYPICAL_CLIENT_STATEMENTS =
	[
		"\trequest, err := http.NewRequest(http.MethodGet, endpoint, nil)",
		"\trequest.Header.Set(\"Accept\", \"application/json\")",
		"\trequest.Header.Set(\"Authorization\", \"Bearer \"+accessToken)",
		"\tpayload := base64.RawURLEncoding.EncodeToString(digest[:])",
		"\tport := os.Getenv(\"PORT\")",
		"\tparsed, err := url.Parse(endpoint)",
		"\tmux.HandleFunc(\"/healthz\", health)",
		"\tresponse, err := client.Do(request)",
		"\tdefer response.Body.Close()",
		"\tdecoder := json.NewDecoder(response.Body)"
	];

	private static readonly ImmutableArray<string> _ANCHOR_DENSE_NEAR_MISSES =
	[
		"\tlabel := credentialName",
		"\tfmt.Println(\"the Authorization header is disabled\")",
		"\tauthor := authority + oauth + authenticate",
		"\tkey := base64.StdEncoding.EncodeToString([]byte(host + \":\" + port))",
		"\tcacheKey := base64.URLEncoding.EncodeToString(raw)",
		"\theaders := http.Header{\"Accept\": {\"application/json\"}, \"Content-Type\": {\"text/plain\"}}",
		"\tuser, secret, found := request.BasicAuth()",
		"\tvalue := request.Header.Get(\"Authorization\")",
		"\t// Basic auth is deprecated; password and username appear in prose only",
		"\trequest.Header.Set(\"Proxy-Authorization\", \"Basic \"+proxyToken)",
		"\trequest.Header.Set(\"Authorization\", \"Bearer \"+token)",
		"\tresponse, err := http.Get(\"https://host.example/path\")",
		"\twriter.Header().Set(\"WWW-Authenticate\", \"Basic realm=\\\"restricted\\\"\")",
		"\taddress.User = url.User(\"name\")",
		"\tmode := os.Getenv(\"APP_MODE\")",
		"\tconn, err := sql.Open(\"driver\", \"user=name password=secret host=db.example\")",
		"\trequest.Header.Set(\"Authorization\", client.basicHeader(userName, secret))",
		"\trequest.Header.Set(\"Authorization\", bearerHeader(token))",
		"\tchallenge := \"Basic realm=\" + realm + nonce",
		"\tfunc describe(userName string) string {\n\t\treturn \"Basic \" + stored\n\t}",
		"\tkey := base64.URLEncoding.EncodeToString(raw) + \"Basic \" + label"
	];

	private static readonly ImmutableArray<string> _EVERY_CLASS_CONSTRUCTS =
	[
		"\trequest.SetBasicAuth(userName, \"exampleSecret\")",
		"\trequest.Header.Set(\"Authorization\", \"Basic \"+encoded)",
		"\theaders := http.Header{\"Authorization\": {\"Basic \" + encoded}}",
		"\tflat := map[string]string{\"Authorization\": \"Basic \" + encoded}",
		"\tresponse, err := http.Get(\"https://exampleUser:exampleSecret@host.example/path\")",
		"\tpair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret)) // Authorization",
		"\taddress.User = url.UserPassword(userName, \"exampleSecret\")",
		"\taddress, err := url.Parse(\"https://exampleUser:exampleSecret@host.example/path\")",
		"\trequest.Header.Set(\"Authorization\", header)",
		"\tfunc (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\t\trequest.SetBasicAuth(userName, secret)\n\t\treturn nil, nil\n\t}",
		"\tfunc basicHeader(userName string, secret string) string {\n\t\treturn \"Basic \" + encode(userName, secret)\n\t}",
		"\trequest.Header.Set(\"Authorization\", os.Getenv(\"SERVICE_AUTH_HEADER\"))",
		"\tvalue, found := os.LookupEnv(\"SERVICE_AUTH_HEADER\")",
		"\tpassword, err := term.ReadPassword(int(os.Stdin.Fd()))",
		"\tfmt.Print(\"Password: \")\n\treader := bufio.NewReader(os.Stdin)",
		"\tfmt.Scanln(&password)",
		"\tpassword := request.FormValue(\"password\")",
		"\tpage := `<input type=\"password\" name=\"pw\">`",
		"\thttp.HandleFunc(\"/login\", loginHandler)",
		"\tapiPassword := \"exampleSecretValue\"",
		"\tsecret := config.ClientSecret",
		"\tdata, err := os.ReadFile(\"/run/secrets/database\")",
		"\tvalue, err := client.GetSecretValue(ctx, input)",
		"\trequest.Header.Set(\"Authorization\", basicHeader(userName, secret))",
		"\trequest.Header[\"Authorization\"] = []string{basicHeader(userName, secret)}",
		"\tfunc basicHeader(userName string, secret string) string {\n\t\tencoded := base64.StdEncoding.EncodeToString(raw)\n\t\treturn \"Basic \" + encoded + userName\n\t}"
	];

	private static readonly ImmutableArray<string> _OPEN_WINDOWS =
	[
		"\tresponse, err := http.Get(",
		"\theaders := http.Header{",
		"\tflat := map[string]string{",
		"\trequest, err := http.NewRequest(method,",
		"\tpair := base64.StdEncoding.EncodeToString([]byte(",
		"\taddress.User = url.UserPassword(",
		"\taddress, err := url.Parse(",
		"func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {",
		"func basicHeader(userName string, secret string) string {",
		"\trequest.Header.Set(\"Authorization\",",
		"\trequest.Header[\"Authorization\"] = append(",
		"\tpassword, err := term.ReadPassword(",
		"\tmux.HandleFunc(\"/login\",",
		"\trequest.Header.Set(\"Authorization\", basicHeader(",
		"func basicHeader(raw []byte) string {\n\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + "
	];

	private static readonly ImmutableArray<string> _LITERAL_AND_COMMENT_LINES =
	[
		"\t// request.SetBasicAuth(userName, secret) was removed in the migration to tokens",
		"\t/* h.Set(\"Authorization\", \"Basic abc\") is deprecated */",
		"\ttext := \"base64.StdEncoding.EncodeToString([]byte(a + \\\":\\\" + b)) and Authorization: Basic abc appear in prose\"",
		"\thtml := \"<input type=\\\"text\\\" name=\\\"user\\\">\"",
		"\tnote := `the password was refreshed for the user at noon`",
		"\t// address.User = url.UserPassword(userName, secret)"
	];

	private static readonly ImmutableArray<string> _LEXICAL_HEAVY_LINES =
	[
		"\ta := `x \\ \" ' // /* y` + \"it's\" + 'q'",
		"\tre := regexp.MustCompile(`ab[/]cd\\/e`)",
		"\td := a / b / c",
		"\tx = y /* a block comment */ / z",
		"\ts := \"it\\'s\"",
		"\tt := \"say \\\"hi\\\"\"",
		"\tu := items[0] / 2 * 3 // trailing comment with /* an opener"
	];

	private static readonly ImmutableArray<string> _FLOW_LINES =
	[
		"\tpassword, err := term.ReadPassword(int(os.Stdin.Fd()))",
		"\tfmt.Fscan(os.Stdin, &secret)",
		"\tname := request.PostForm.Get(\"password\")",
		"\tbody := `<input name=\"pw\" type=\"password\">`",
		"\tmux.Handle(\"POST /login\", handler)",
		"\tkey := os.Getenv(\"DB_PASS\")",
		"\tfromConfig := config.Get(\"db.password\") + cfg.ClientSecret",
		"\tvalue, err := store.GetSecret(name)",
		"\tapiToken := \"exampleTokenValue\"",
		"\tdata, err := os.ReadFile(\"secret.txt\")",
		"\tscanner := bufio.NewScanner(os.Stdin)"
	];

	private static readonly ImmutableArray<string> _NESTED_MEMBER_FLOW_LINES =
	[
		"	secret := cfg.Section.Secret",
		"	key := settings.Database.Password",
		"	value := conf.Service.ApiToken",
		"	label := cfg.Section.Name",
		"	port := config.Server.Port"
	];

	private static readonly ImmutableArray<string> _INDEXED_BINDING_FLOW_LINES =
	[
		"	creds := app.Settings[key]",
		"	password := settings.Values[name]",
		"	secret := config.Entries[index]",
		"	label := cfg.Names[key]",
		"	count := conf.Counts[index]"
	];

	/// <summary>One long-line shape per window family of the expressions, each repeating one group up to the line-length ceiling.</summary>
	private static readonly ImmutableArray<(string Slug, string Group)> _LONG_LINE_GROUPS =
	[
		("construct-free-code", "a.b(c,d);e[f]=g+h*i;j:=k(l)-m;n=o/p/q;"),
		("setbasicauth-calls", "r.SetBasicAuth(u,p);"),
		("setbasicauth-openings-without-a-close", "r.SetBasicAuth("),
		("authorization-words", "Authorization Authorization authorization "),
		("authorization-header-sets", "h.Set(\"Authorization\",\"Basic \"+t);"),
		("authorization-header-set-openings", "h.Set(\"Authorization\","),
		("authorization-index-assignments", "h[\"Authorization\"]=[]string{\"Basic \"+t};"),
		("header-literal-openings-without-a-close", "http.Header{"),
		("header-literals-with-keys-without-a-close", "http.Header{\"Accept\":{\"x\"},"),
		("map-literal-openings", "map[string]string{"),
		("request-call-openings", "http.Get("),
		("request-calls-with-nested-groups", "http.Get(f(g[h{i}]),"),
		("userinfo-urls", "http.Get(\"https://u:p@h.example/a\");"),
		("encoder-openings", "base64.StdEncoding.EncodeToString([]byte("),
		("encoder-pairs", "base64.StdEncoding.EncodeToString([]byte(a+\":\"+b));"),
		("userinfo-openings", "url.UserPassword("),
		("parse-calls", "url.Parse(\"https://u:p@h.example/a\");"),
		("environment-reads", "os.Getenv(\"API_PASSWORD\");"),
		("form-reads", "r.FormValue(\"password\");"),
		("raw-literal-openings", "x:=`a b c "),
		("unclosed-quotes", "f(\"a,'b,`c "),
		("round-trip-openings", "func (t *T) RoundTrip(r *http.Request) (*http.Response, error) {"),
		("function-heads", "func f(a string, b string) string {"),
		("stdin-and-prompt-words", "os.Stdin Password: bufio.NewReader("),
		("login-route-openings", "mux.HandleFunc(\"/login"),
		("credential-name-assignments", "password=\"lit\";apiSecret=`x`;"),
		("nested-member-credential-reads", "s:=cfg.Section.Secret;"),
		("indexed-binding-credential-reads", "c:=app.Settings[k];"),
		("nested-groups-past-depth-four", "f(((((x)))))g[[[[[y]]]]]"),
		("encoded-basic-helper-heads-with-an-encoder", "func f(a string){x:=base64.StdEncoding.EncodeToString(a)};"),
		("encoded-basic-helper-heads-with-a-literal", "func f(a string){return \"Basic \"+a};"),
		("encoded-basic-helper-conjunctions", "func f(a string){x:=base64.StdEncoding.EncodeToString(a);return \"Basic \"+x};"),
		("local-helper-calls", "h.Set(\"Authorization\",f(a,b));"),
		("local-helper-call-openings", "h.Set(\"Authorization\",f("),
		("local-helper-calls-with-nested-groups", "h.Set(\"Authorization\",f(g[h{i}],j));")
	];

	private static readonly ImmutableArray<(string Slug, string Definition, int FillerLines, string Tail)> _DEFINITION_BLOCKS =
	[
		("round-tripper-then-header", "func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {", _DEFINITION_FILLER_LINES, "\trequest.SetBasicAuth(userName, secret)\n\treturn nil, nil\n}\n"),
		("round-tripper-then-no-construct", "func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {", _DEFINITION_FILLER_LINES, "\treturn nil, nil\n}\n"),
		("helper-head-then-no-token", "func build(userName string, secret string) string {", _DEFINITION_FILLER_LINES, "}\n"),
		("helper-head-then-token", "func build(userName string, secret string) string {", _DEFINITION_FILLER_LINES, "\treturn \"Basic \" + encode(userName, secret)\n}\n"),
		("helper-head-then-encoder-and-literal-conjunction", "func build(userName string, secret string) string {", _DEFINITION_FILLER_LINES, "\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + encoded + userName\n}\n"),
		("helper-head-then-encoder-only", "func build(userName string, secret string) string {", _DEFINITION_FILLER_LINES, "\treturn base64.StdEncoding.EncodeToString(raw) + userName\n}\n"),
		("helper-head-then-literal-only", "func build(userName string, secret string) string {", _DEFINITION_FILLER_LINES, "\treturn \"Basic \" + userName\n}\n"),
		("helper-head-then-conjunction-without-a-parameter", "func build(userName string, secret string) string {", _DEFINITION_FILLER_LINES, "\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + encoded\n}\n")
	];

	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		AddTheTypicalShapes(shapes);
		AddTheWorstShapes(shapes);
		AddTheLongLineShapes(shapes);
		AddDefinitionBlockShapes(shapes);

		return shapes.ToImmutable();
	}

	internal static string RelativePathOf(MatchBoundStressShape shape)
	{
		return string.Format(CultureInfo.InvariantCulture, "stress/{0}{1}", shape.Name, shape.FileExtension);
	}

	private static void AddTheTypicalShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape($"{TYPICAL_PREFIX}ordinary-service", FillToSize(_ORDINARY_STATEMENTS, TYPICAL_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{TYPICAL_PREFIX}http-client-without-constructs", FillToSize(_TYPICAL_CLIENT_STATEMENTS, TYPICAL_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{TYPICAL_PREFIX}client-with-every-class", FillToSize([.. _ORDINARY_STATEMENTS, .. _EVERY_CLASS_CONSTRUCTS], TYPICAL_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{TYPICAL_PREFIX}near-misses-and-flow-forms", FillToSize([.. _ANCHOR_DENSE_NEAR_MISSES, .. _FLOW_LINES], TYPICAL_SIZE_IN_CHARACTERS)));
	}

	private static void AddTheWorstShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape($"{WORST_PREFIX}ordinary-at-ceiling", FillToSize(_ORDINARY_STATEMENTS, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}anchor-dense-near-misses", FillToSize(_ANCHOR_DENSE_NEAR_MISSES, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}every-class-at-match-density", FillToSize(_EVERY_CLASS_CONSTRUCTS, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}open-windows", FillWindowsToCeiling(_OPEN_WINDOWS)));
		shapes.Add(Shape($"{WORST_PREFIX}literals-and-comments", FillToSize(_LITERAL_AND_COMMENT_LINES, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}lexical-heavy", FillToSize(_LEXICAL_HEAVY_LINES, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}flow-signal-forms", FillToSize(_FLOW_LINES, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}flow-signal-nested-member-forms", FillToSize(_NESTED_MEMBER_FLOW_LINES, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}flow-signal-indexed-binding-forms", FillToSize(_INDEXED_BINDING_FLOW_LINES, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}two-megabytes-of-line-comments", FillToCeilingWithLines(_COMMENT_LINE)));
		shapes.Add(Shape($"{WORST_PREFIX}two-megabytes-of-line-comments-with-anchors", FillToCeilingWithLines(_COMMENT_LINE_WITH_ANCHORS)));
		shapes.Add(Shape($"{WORST_PREFIX}raw-literals-holding-anchors", RepeatBlockToCeiling(_RAW_BLOCK)));
		shapes.Add(Shape($"{WORST_PREFIX}generated-table", GeneratedTable()));
		shapes.Add(Shape($"{WORST_PREFIX}large-literals-with-the-credential-key-last", LargeLiterals()));
		shapes.Add(Shape($"{WORST_PREFIX}credential-key-at-the-argument-window-bound", ObjectWithTheKeyAfter(_MEMBERS_BEFORE_THE_KEY_AT_THE_WINDOW)));
		shapes.Add(Shape($"{WORST_PREFIX}credential-key-beyond-the-argument-window-bound", ObjectWithTheKeyAfter(_MEMBERS_BEFORE_THE_KEY_BEYOND_THE_WINDOW)));
	}

	private static void AddTheLongLineShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string group) in _LONG_LINE_GROUPS)
		{
			shapes.Add(Shape($"{LONG_LINE_PREFIX}{slug}", LongLines(group)));
		}
	}

	private static void AddDefinitionBlockShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string definition, int fillerLines, string tail) in _DEFINITION_BLOCKS)
		{
			shapes.Add(Shape($"{WORST_PREFIX}definition-{slug}", DefinitionBlocks(definition, fillerLines, tail)));
		}
	}

	private static MatchBoundStressShape Shape(string name, string text)
	{
		return new MatchBoundStressShape { Name = name, Text = text, FileExtension = FILE_EXTENSION };
	}

	private static string FillToSize(ImmutableArray<string> statements, int targetSize)
	{
		StringBuilder builder = new(targetSize + _BUILDER_SLACK_IN_CHARACTERS);
		StringBuilder body = new();
		int functionNumber = 0;

		builder.Append(_PACKAGE_LINE);

		foreach (string statement in statements)
		{
			body.Append(statement).Append(_LINE_BREAK);
		}

		while (builder.Length + body.Length + _FUNCTION_FORMAT.Length < targetSize)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _FUNCTION_FORMAT, functionNumber++, body));
		}

		return builder.ToString();
	}

	private static string FillToCeilingWithLines(string line)
	{
		StringBuilder builder = new(WORST_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + line.Length < WORST_SIZE_IN_CHARACTERS)
		{
			builder.Append(line);
		}

		return builder.ToString();
	}

	private static string FillWindowsToCeiling(ImmutableArray<string> openers)
	{
		StringBuilder builder = new(WORST_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);
		int opener = 0;

		while (builder.Length + LONG_LINE_LENGTH_IN_CHARACTERS < WORST_SIZE_IN_CHARACTERS)
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
		StringBuilder builder = new(LONG_LINE_FILE_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + LONG_LINE_LENGTH_IN_CHARACTERS < LONG_LINE_FILE_SIZE_IN_CHARACTERS)
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

	private static string GeneratedTable()
	{
		StringBuilder builder = new(_TABLE_ROWS * _TABLE_ROW_FORMAT.Length);

		builder.Append("// Code generated by a tool. DO NOT EDIT.\n\npackage sample\n\nvar table = []entry{\n");

		for (int row = 0; row < _TABLE_ROWS && builder.Length < WORST_SIZE_IN_CHARACTERS - _BUILDER_SLACK_IN_CHARACTERS; row++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _TABLE_ROW_FORMAT, row));
		}

		builder.Append("}\n");

		return builder.ToString();
	}

	private static string LargeLiterals()
	{
		StringBuilder literal = new();

		literal.Append("var options = map[string]string{\n");

		for (int member = 0; member < _LARGE_LITERAL_MEMBERS; member++)
		{
			literal.Append(string.Format(CultureInfo.InvariantCulture, _MEMBER_FORMAT, member));
		}

		literal.Append("\t\"Authorization\": \"Basic \" + encoded,\n}\n");

		return RepeatBlockToCeiling(literal.ToString());
	}

	/// <summary>A call whose literal holds the given number of ordinary members before the credential key, repeated: the key lands inside the argument window, or beyond it.</summary>
	private static string ObjectWithTheKeyAfter(int members)
	{
		StringBuilder call = new();

		call.Append("var headers = http.Header{\n");

		for (int member = 0; member < members; member++)
		{
			call.Append(string.Format(CultureInfo.InvariantCulture, _MEMBER_FORMAT, member));
		}

		call.Append("\t\"Authorization\": {\"Basic \" + encoded},\n}\n");

		return RepeatBlockToCeiling(call.ToString());
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

		return RepeatBlockToCeiling(block.ToString());
	}

	/// <summary>A block, numbered where it carries a placeholder, repeated up to the worst size.</summary>
	private static string RepeatBlockToCeiling(string block)
	{
		StringBuilder builder = new(WORST_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);
		int number = 0;

		while (builder.Length + block.Length + _REPEAT_TAIL_MARGIN_IN_CHARACTERS < WORST_SIZE_IN_CHARACTERS)
		{
			builder.Append(block.Contains("{0", StringComparison.Ordinal) ? string.Format(CultureInfo.InvariantCulture, block, number++) : block);
		}

		return builder.ToString();
	}
}
