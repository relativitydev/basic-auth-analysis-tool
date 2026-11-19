using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE STRESS SET OF THE SHIPPED `Go` COST GATE, run through the registered analyzer and the real recognition engine by <see cref="GoShippedCostGateTimingTests"/>. It is self-contained: every shape is legal, ordinary-looking
/// Go that a client repository could contain, and none is built to make an expression backtrack catastrophically. What the shapes do is push every honest cost to the ceilings the engine already enforces. The volume rows of
/// the design (the per-file lexical scan and recognition, V1 typical and V2 worst; the long-line files) name the families:
///
///   * THE TYPICAL V1 SHAPES (<see cref="TYPICAL_PREFIX"/>): ordinary decoded files of about <see cref="TYPICAL_SIZE_IN_CHARACTERS"/> characters, a plain service, a client with the constructs of every class at a
///     realistic density, and a file of near-misses.
///   * THE WORST V2 SHAPES (<see cref="WORST_PREFIX"/>): files filled to just under the reader's 2 MiB ceiling with every honest cost: ordinary code, anchor-dense near-misses, every class at match density, every bounded
///     window of the catalog opened over and over without being closed, raw literals and line comments that hold anchors, a generated table, and the definition windows of the two definition classes.
///   * THE ARGUMENT-WINDOW SHAPES (<see cref="WINDOW_PREFIX"/>): the window of every class that reads one (the header literal, the unnamed map literal, the request call and the encoder), with its credential key at the
///     bound, one past it, and the window opened and never closed, repeated up to the ceiling.
///   * THE LONG-LINE SHAPES (<see cref="LONG_LINE_PREFIX"/>): files of <see cref="LONG_LINE_FILE_SIZE_IN_CHARACTERS"/> characters whose lines run to 9,000 to 10,000 characters: a construct-free one, the anchor-dense ones and
///     one per window family of the expressions.
///   * THE SHAPES UNDER A DEMOTED FOLDER (<see cref="VENDORED_PREFIX"/>, <see cref="TEST_DATA_PREFIX"/>): a vendored tree is walked and read like any other, so its files are the same shapes standing at a path under
///     <c>vendor/</c>, and a test-data folder holds the same under <c>testdata/</c>: the cost of a file does not depend on the level its findings end at, which these shapes prove.
///
///   * THE FLOW-SIGNAL SHAPES (<see cref="FLOW_SIGNAL_PREFIX"/>): the eleven flow-signal expressions, which the engine evaluates once per file that holds a High or Medium construct: one long line per window family, the anchor-dense
///     near misses, the forms at match density, the reads opened and never closed, and the widest windows (the standard-input read and prompt, the terminal read, the formatted scan at its unit bound, the markup tag).
///
/// Every shape is deterministic, legal for the reader (under the file ceiling, no line over the line ceiling) and invented: placeholders and generic names, no fragment of any real text.
/// </summary>
internal static class GoShippedCostGateStressSet
{
	internal const string TYPICAL_PREFIX = "go-shipped-v1-";

	internal const string WORST_PREFIX = "go-shipped-v2-";

	internal const string WINDOW_PREFIX = "go-shipped-window-";

	internal const string LONG_LINE_PREFIX = "go-shipped-long-line-";

	internal const string VENDORED_PREFIX = "go-shipped-vendored-";

	internal const string TEST_DATA_PREFIX = "go-shipped-testdata-";

	/// <summary>What every flow-signal shape is named with: the shapes the timing gate reports for the eleven flow-signal expressions (<c>EX-GO-120</c> to <c>EX-GO-125</c> and <c>EX-GO-130</c> to <c>EX-GO-134</c>).</summary>
	internal const string FLOW_SIGNAL_PREFIX = "go-shipped-flow-";

	/// <summary>
	/// The one High finding a flow-signal shape opens with: the engine evaluates the flow signals only over a file that holds a High or Medium construct, so without it the shape would time the detection rules alone. It is a
	/// request method that sets Basic authentication over invented operands, class <c>GO-001</c>.
	/// </summary>
	internal const string FLOW_SHAPE_HIGH_CONSTRUCT = "func open(request *http.Request) {\n\trequest.SetBasicAuth(accountA, accountB)\n}\n";

	internal const string FILE_EXTENSION = ".go";

	internal const string VENDORED_FOLDER = "vendor/example.org/library";

	internal const string TEST_DATA_FOLDER = "testdata/cases";

	internal const string DEFAULT_FOLDER = "stress";

	internal const int TYPICAL_SIZE_IN_CHARACTERS = 64 * 1024;

	internal const int WORST_SIZE_IN_CHARACTERS = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;

	internal const int LONG_LINE_LENGTH_IN_CHARACTERS = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _LINE_MARGIN_IN_CHARACTERS;

	internal const int LONG_LINE_FILE_SIZE_IN_CHARACTERS = 500_000;

	/// <summary>The members before the credential key that leave it inside the argument window (the unit bound is 1,500 and a member takes about 17 units).</summary>
	internal const int MEMBERS_BEFORE_THE_KEY_AT_THE_WINDOW = 85;

	/// <summary>The members before the credential key that put it past the argument window.</summary>
	internal const int MEMBERS_BEFORE_THE_KEY_BEYOND_THE_WINDOW = 100;

	/// <summary>The units of the argument window of the formatted scan, as the vocabulary of the expressions declares them (a lazy loop of at most 1,500 units).</summary>
	internal const int ARGUMENT_WINDOW_UNITS = 1_500;

	/// <summary>The characters of a credential name the expressions read (a name of at most sixty-four characters), of the path of a secret-file read and of the tag of a markup input, each at the bound the expression declares.</summary>
	internal const int CREDENTIAL_NAME_CHARACTERS = 64;

	internal const int CREDENTIAL_LITERAL_CHARACTERS = 400;

	internal const int FILE_PATH_CHARACTERS = 200;

	internal const int MARKUP_TAG_CHARACTERS = 200;

	internal const int TERMINAL_READ_ARGUMENT_CHARACTERS = 190;

	private const int _GUARD_WINDOW_LINE_LENGTH_IN_CHARACTERS = 390;
	private const int _ONE_UNIT_PAST_THE_BOUND = 1;
	private const char _FILLER_UNIT = 'x';
	private const string _CONFIGURATION_RECEIVER_PREFIX = "cfg.";
	private const int _MARKUP_SEPARATING_BLANKS = 2;
	private const string _COMMENT_OPENING = "//";

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;
	private const int _LINE_MARGIN_IN_CHARACTERS = 16;
	private const int _BUILDER_SLACK_IN_CHARACTERS = 4096;
	private const int _FILLER_LINES_PER_WINDOW = 4;
	private const int _DEFINITION_FILLER_LINES = 29;
	private const int _TABLE_ROWS = 20_000;
	private const int _REPEAT_TAIL_MARGIN_IN_CHARACTERS = _BUILDER_SLACK_IN_CHARACTERS / 4;
	private const string _LINE_BREAK = "\n";
	private const string _FUNCTION_FORMAT = "func step{0:D6}() {{\n{1}}}\n";
	private const string _FILLER_STATEMENT = "\ttotal := first + second\n";
	private const string _PACKAGE_LINE = "package sample\n\n";
	private const string _COMMENT_LINE = "// an ordinary comment line about the code below, with words and numbers 12345\n";
	private const string _COMMENT_LINE_WITH_ANCHORS = "// request.SetBasicAuth(u, p) and h.Set(\"Authorization\", \"Basic \"+x) were removed in favour of tokens\n";
	private const string _TABLE_ROW_FORMAT = "\t{{\"entry{0:D5}\", {0}, 0x{0:X4}, \"value {0:D5}\"}},\n";
	private const string _MEMBER_FORMAT = "\t\"member{0:D4}\": \"value {0:D4}\",\n";
	private const string _RAW_BLOCK = "const page{0:D5} = `\n<form method=\"post\" action=\"/save\">\n\t// Authorization: Basic is not sent here\n\trequest.SetBasicAuth(u, p)\n</form>\n`\n";

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
		"\trequest.Header.Set(\"Authorization\", basicHeader(userName, secret))",
		"\trequest.Header[\"Authorization\"] = []string{basicHeader(userName, secret)}",
		"\tfunc basicHeader(userName string, secret string) string {\n\t\tencoded := base64.StdEncoding.EncodeToString(raw)\n\t\treturn \"Basic \" + encoded + userName\n\t}"
	];

	private static readonly ImmutableArray<string> _LITERAL_AND_COMMENT_LINES =
	[
		"\t// request.SetBasicAuth(userName, secret) was removed in the migration to tokens",
		"\t/* h.Set(\"Authorization\", \"Basic abc\") is deprecated */",
		"\ttext := \"base64.StdEncoding.EncodeToString([]byte(a + \\\":\\\" + b)) and Authorization: Basic abc appear in prose\"",
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
		"\trequest.Header.Set(\"Authorization\", basicHeader(",
		"func basicHeader(raw []byte) string {\n\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + "
	];

	/// <summary>The five classes that read an argument window (the header literal, the unnamed map literal, the request call, the encoder and, at 0.5, the call value of the local helper), each with the text that opens it and the text that holds its credential key or its last argument.</summary>
	private static readonly ImmutableArray<(string Slug, string Opening, string Key, string Closing)> _WINDOW_CLASSES =
	[
		("header-literal", "var headers = http.Header{\n", "\t\"Authorization\": {\"Basic \" + encoded},\n", "}\n"),
		("map-literal", "var flat = map[string]string{\n", "\t\"Authorization\": \"Basic \" + encoded,\n", "}\n"),
		("request-call", "response, err := http.Get(\n", "\t\"https://exampleUser:exampleSecret@host.example/path\",\n", ")\n"),
		("encoder", "pair := base64.StdEncoding.EncodeToString([]byte(\n", "\tuserName + \":\" + secret,\n", "))\n"),
		("local-helper-call", "request.Header.Set(\"Authorization\", basicHeader(\n", "\tuserName, secret\n", "))\n")
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
		("raw-literal-openings", "x:=`a b c "),
		("unclosed-quotes", "f(\"a,'b,`c "),
		("round-trip-openings", "func (t *T) RoundTrip(r *http.Request) (*http.Response, error) {"),
		("function-heads", "func f(a string, b string) string {"),
		("proxy-header-words", "Proxy-Authorization proxy-authorization "),
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
		("helper-head-of-eight-parameters", "func build(a1 string, a2 string, a3 string, a4 string, a5 string, a6 string, a7 string, a8 string) string {", _DEFINITION_FILLER_LINES, "\treturn \"Basic \" + encode(a1)\n}\n"),
		("helper-head-then-encoder-and-literal-conjunction", "func build(userName string, secret string) string {", _DEFINITION_FILLER_LINES, "\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + encoded + userName\n}\n"),
		("helper-head-then-encoder-only", "func build(userName string, secret string) string {", _DEFINITION_FILLER_LINES, "\treturn base64.StdEncoding.EncodeToString(raw) + userName\n}\n"),
		("helper-head-then-literal-only", "func build(userName string, secret string) string {", _DEFINITION_FILLER_LINES, "\treturn \"Basic \" + userName\n}\n"),
		("helper-head-then-conjunction-without-a-parameter", "func build(userName string, secret string) string {", _DEFINITION_FILLER_LINES, "\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + encoded\n}\n")
	];

	/// <summary>
	/// One long-line shape per window family of the flow-signal expressions (<c>EX-GO-120</c> to <c>EX-GO-125</c>, <c>EX-GO-130</c> to <c>EX-GO-134</c>), each repeating one group up to the line-length ceiling: the call anchors
	/// and the forms each expression reads, their near misses one character off, the openings that are never closed (the quote of a credential key, the tag of a markup input, the call of a read), the declarations the
	/// expressions refuse, the credential names at the bound of sixty-four characters, the literals at the bound of four hundred and the paths at the bound of two hundred.
	/// </summary>
	private static readonly ImmutableArray<(string Slug, string Group)> _FLOW_LONG_LINE_GROUPS =
	[
		("terminal-password-reads", "term.ReadPassword(int(os.Stdin.Fd())); ReadPassword(fd); "),
		("terminal-password-read-openings-without-a-descriptor", "term.ReadPassword("),
		("terminal-password-declarations-and-method-names", "func ReadPassword(fd int) {}; type ReadPassword struct{}; x.ReadPassword(a); "),
		("standard-input-words-without-a-read-or-a-prompt", "os.Stdin syscall.Stdin Stdin "),
		("standard-input-reads-without-a-prompt", "bufio.NewReader(os.Stdin); bufio.NewScanner(os.Stdin); "),
		("credential-prompts-beside-standard-input", "fmt.Print(\"Enter your password: \"); os.Stdin; "),
		("standard-input-reads-with-credential-prompts", "bufio.NewReader(os.Stdin); fmt.Print(\"Enter your password: \"); "),
		("formatted-scans-into-credential-variables", "fmt.Scanln(&password); fmt.Fscan(os.Stdin, &secret); "),
		("formatted-scan-openings-without-a-close", "fmt.Scan("),
		("formatted-scans-into-labelled-variables", "fmt.Scanln(&name); fmt.Scanf(\"%d\", &count); fmt.Fscan(reader, &password); "),
		("form-credential-reads", "r.FormValue(\"password\"); r.PostForm.Get(\"pwd\"); r.Form[\"secret\"]; "),
		("form-reads-of-other-keys", "r.FormValue(\"name\"); r.PostForm.Get(\"city\"); r.Form[\"page\"]; "),
		("form-read-openings-without-a-close", "FormValue(\"a b c "),
		("markup-password-inputs", "<input type=\"password\"> <input type=text name=a> <input value=\"x\"> "),
		("markup-input-openings-without-a-close", "<input a=b c=d "),
		("login-route-registrations", "mux.HandleFunc(\"/login\", h); mux.Handle(\"/a/sign-in/b\", h); "),
		("route-registrations-of-other-paths", "mux.HandleFunc(\"/items\", h); mux.Handle(\"/logs/{id}\", h); "),
		("route-registration-openings-without-a-close", "HandleFunc(\"/a/b/c "),
		("environment-credential-reads", "os.Getenv(\"DB_PASS\"); os.LookupEnv(\"API_TOKEN\"); "),
		("environment-reads-of-other-names", "os.Getenv(\"APP_MODE\"); os.LookupEnv(\"HOME\"); "),
		("environment-read-openings-without-a-close", "Getenv(\"a b c d "),
		("configuration-credential-members", "cfg.Section.Secret; settings.Database.Password; config.Get(\"db.password\"); "),
		("configuration-members-of-other-names", "cfg.Section.Name; settings.Port; config.Get(\"timeout\"); "),
		("configuration-receivers-without-a-member", "config. settings. cfg. conf. "),
		("configuration-members-with-long-near-miss-names", $"{_CONFIGURATION_RECEIVER_PREFIX}{new string(_FILLER_UNIT, CREDENTIAL_NAME_CHARACTERS - _CONFIGURATION_RECEIVER_PREFIX.Length)}; "),
		("indexed-credential-bindings", "creds := app.Settings[key]; password := conf.Values[name]; "),
		("indexed-bindings-of-other-names", "label := cfg.Names[key]; count := conf.Counts[i]; "),
		("secret-manager-calls", "client.GetSecretValue(ctx, in); svc.AccessSecretVersion(ctx, req); store.GetSecret(name); "),
		("secret-manager-method-declarations", "func (c *Client) GetSecret(name string) {}; func GetSecretValue(n string) {}; "),
		("credential-named-literal-assignments", "apiToken := \"x\"; password = `y`; const secretValue string = \"z\"; "),
		("credential-named-assignments-with-label-names", "passwordLabel := \"x\"; secretName = \"y\"; tokenURL = `z`; "),
		("credential-named-assignments-with-literals-at-the-bound", $"password := \"{new string(_FILLER_UNIT, CREDENTIAL_LITERAL_CHARACTERS)}\"; "),
		("credential-names-without-an-assignment", "password_manager passwd_hash apiToken secret credential "),
		("secret-file-reads", "os.ReadFile(\"secret.txt\"); ioutil.ReadFile(\"/run/secrets/db\"); "),
		("file-reads-of-other-paths", "os.ReadFile(\"data.txt\"); "),
		("file-reads-with-paths-at-the-bound", $"os.ReadFile(\"{new string(_FILLER_UNIT, FILE_PATH_CHARACTERS)}\"); ")
	];

	private static readonly ImmutableArray<string> _FLOW_FORMS_AT_MATCH_DENSITY =
	[
		"\tpassword, err := term.ReadPassword(int(os.Stdin.Fd()))",
		"\tscanner := bufio.NewScanner(os.Stdin)\n\tfmt.Print(\"Enter your password: \")",
		"\tfmt.Fscan(os.Stdin, &secret)",
		"\tfmt.Scanln(&password)",
		"\tname := request.PostForm.Get(\"password\")",
		"\tvalue := request.FormValue(\"pwd\")",
		"\tbody := `<input name=\"pw\" type=\"password\">`",
		"\tmux.Handle(\"POST /login\", handler)",
		"\tkey := os.Getenv(\"DB_PASS\")",
		"\tfromConfig := config.Get(\"db.password\") + cfg.ClientSecret",
		"\tvalue, err := store.GetSecret(name)",
		"\tapiToken := \"exampleTokenValue\"",
		"\tdata, err := os.ReadFile(\"secret.txt\")"
	];

	private static readonly ImmutableArray<string> _FLOW_ANCHOR_DENSE_NEAR_MISSES =
	[
		"\tpassword, err := term.ReadPasswordWithPrompt(prompt)",
		"\tscanner := bufio.NewScanner(file)",
		"\tfmt.Scanln(&name)",
		"\tfmt.Fscan(reader, &count)",
		"\tname := request.FormValue(\"name\")",
		"\tvalue := request.PostForm.Get(\"city\")",
		"\tbody := `<input name=\"pw\" type=\"text\">`",
		"\tmux.HandleFunc(\"/logs\", handler)",
		"\tmode := os.Getenv(\"APP_MODE\")",
		"\ttimeout := config.Get(\"timeout\")",
		"\tport := settings.Server.Port",
		"\tlabel := cfg.Names[key]",
		"\tvalue := vault.GetSecretKey(name)",
		"\tpasswordLabel := \"Password\"",
		"\tsecretName := \"service\"",
		"\tdata, err := os.ReadFile(\"data.txt\")",
		"\tprompt := \"Enter your name: \"",
		"\tout := os.Stdout"
	];

	private static readonly ImmutableArray<string> _FLOW_OPEN_WINDOWS =
	[
		"\tpassword, err := term.ReadPassword(",
		"\tfmt.Scan(",
		"\tfmt.Fscan(os.Stdin,",
		"\tvalue := r.FormValue(",
		"\tvalue := r.PostForm.Get(",
		"\tbody := \"<input ",
		"\tmux.HandleFunc(",
		"\tkey := os.Getenv(",
		"\tdata, err := os.ReadFile(",
		"\tvalue, err := store.GetSecret("
	];

	/// <summary>The families the shipped cost gate is split into, one timing test each.</summary>
	internal enum ShippedShapeFamily
	{
		/// <summary>The typical V1 shapes and the worst V2 shapes.</summary>
		TypicalAndWorstFiles,

		/// <summary>The argument-window shapes and the long-line shapes.</summary>
		ArgumentWindowsAndLongLines,

		/// <summary>The definition-window shapes and the shapes under a vendored or a test-data folder.</summary>
		DefinitionWindowsAndDemotedFolders,

		/// <summary>The shapes of the eleven flow-signal expressions.</summary>
		FlowSignals
	}

	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		return [.. Enum.GetValues<ShippedShapeFamily>().SelectMany(family => BuildTheFamily(family))];
	}

	/// <summary>The shapes of the flow-signal expressions alone, in the order <see cref="Build"/> adds them; the timing gate times them as the fourth part.</summary>
	internal static ImmutableArray<MatchBoundStressShape> BuildTheFlowSignalShapes()
	{
		return BuildTheFamily(ShippedShapeFamily.FlowSignals);
	}

	/// <summary>The shapes of one family. The families are disjoint and together are exactly <see cref="Build"/>, which a non-timing test pins.</summary>
	internal static ImmutableArray<MatchBoundStressShape> BuildTheFamily(ShippedShapeFamily family)
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		switch (family)
		{
			case ShippedShapeFamily.TypicalAndWorstFiles:
				AddTheTypicalShapes(shapes);
				AddTheWorstShapes(shapes);
				break;
			case ShippedShapeFamily.ArgumentWindowsAndLongLines:
				AddTheArgumentWindowShapes(shapes);
				AddTheLongLineShapes(shapes);
				break;
			case ShippedShapeFamily.DefinitionWindowsAndDemotedFolders:
				AddTheDefinitionBlockShapes(shapes);
				AddTheDemotedFolderShapes(shapes);
				break;
			case ShippedShapeFamily.FlowSignals:
				AddTheFlowSignalShapes(shapes);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown shape family.");
		}

		return shapes.ToImmutable();
	}

	/// <summary>The path a shape stands at: under the vendored folder or the test-data folder where its name says so, and under the stress folder otherwise.</summary>
	internal static string RelativePathOf(MatchBoundStressShape shape)
	{
		string folder = shape.Name.StartsWith(VENDORED_PREFIX, StringComparison.Ordinal)
			? VENDORED_FOLDER
			: shape.Name.StartsWith(TEST_DATA_PREFIX, StringComparison.Ordinal) ? TEST_DATA_FOLDER : DEFAULT_FOLDER;

		return string.Format(CultureInfo.InvariantCulture, "{0}/{1}{2}", folder, shape.Name, shape.FileExtension);
	}

	private static void AddTheTypicalShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape($"{TYPICAL_PREFIX}ordinary-service", FillToSize(_ORDINARY_STATEMENTS, TYPICAL_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{TYPICAL_PREFIX}client-with-every-class", FillToSize([.. _ORDINARY_STATEMENTS, .. _EVERY_CLASS_CONSTRUCTS], TYPICAL_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{TYPICAL_PREFIX}near-misses", FillToSize(_ANCHOR_DENSE_NEAR_MISSES, TYPICAL_SIZE_IN_CHARACTERS)));
	}

	private static void AddTheWorstShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape($"{WORST_PREFIX}ordinary-at-ceiling", FillToSize(_ORDINARY_STATEMENTS, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}anchor-dense-near-misses", FillToSize(_ANCHOR_DENSE_NEAR_MISSES, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}every-class-at-match-density", FillToSize(_EVERY_CLASS_CONSTRUCTS, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}open-windows", FillWindowsToCeiling(_OPEN_WINDOWS)));
		shapes.Add(Shape($"{WORST_PREFIX}literals-and-comments", FillToSize(_LITERAL_AND_COMMENT_LINES, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}lexical-heavy", FillToSize(_LEXICAL_HEAVY_LINES, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{WORST_PREFIX}two-megabytes-of-line-comments", FillToCeilingWithLines(_COMMENT_LINE)));
		shapes.Add(Shape($"{WORST_PREFIX}two-megabytes-of-line-comments-with-anchors", FillToCeilingWithLines(_COMMENT_LINE_WITH_ANCHORS)));
		shapes.Add(Shape($"{WORST_PREFIX}raw-literals-holding-anchors", RepeatBlockToCeiling(_RAW_BLOCK)));
		shapes.Add(Shape($"{WORST_PREFIX}generated-table", GeneratedTable()));
	}

	private static void AddTheArgumentWindowShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string opening, string key, string closing) in _WINDOW_CLASSES)
		{
			shapes.Add(Shape($"{WINDOW_PREFIX}{slug}-key-at-the-bound", RepeatBlockToCeiling(WindowBlock(opening, key, closing, MEMBERS_BEFORE_THE_KEY_AT_THE_WINDOW))));
			shapes.Add(Shape($"{WINDOW_PREFIX}{slug}-key-beyond-the-bound", RepeatBlockToCeiling(WindowBlock(opening, key, closing, MEMBERS_BEFORE_THE_KEY_BEYOND_THE_WINDOW))));
			shapes.Add(Shape($"{WINDOW_PREFIX}{slug}-never-closed", RepeatBlockToCeiling(WindowBlock(opening, string.Empty, string.Empty, MEMBERS_BEFORE_THE_KEY_BEYOND_THE_WINDOW))));
		}
	}

	private static void AddTheLongLineShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string group) in _LONG_LINE_GROUPS)
		{
			shapes.Add(Shape($"{LONG_LINE_PREFIX}{slug}", LongLines(group)));
		}
	}

	private static void AddTheDefinitionBlockShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string definition, int fillerLines, string tail) in _DEFINITION_BLOCKS)
		{
			shapes.Add(Shape($"{WORST_PREFIX}definition-{slug}", DefinitionBlocks(definition, fillerLines, tail)));
		}
	}

	/// <summary>The same worst shapes under a vendored folder and a test-data folder: a vendored tree is read like any other and its files are only demoted afterwards.</summary>
	private static void AddTheDemotedFolderShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape($"{VENDORED_PREFIX}every-class-at-match-density", FillToSize(_EVERY_CLASS_CONSTRUCTS, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{VENDORED_PREFIX}anchor-dense-near-misses", FillToSize(_ANCHOR_DENSE_NEAR_MISSES, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{VENDORED_PREFIX}open-windows", FillWindowsToCeiling(_OPEN_WINDOWS)));
		shapes.Add(Shape($"{VENDORED_PREFIX}generated-table", GeneratedTable()));
		shapes.Add(Shape($"{VENDORED_PREFIX}long-lines-of-setbasicauth-calls", LongLines("r.SetBasicAuth(u,p);")));
		shapes.Add(Shape($"{VENDORED_PREFIX}typical-client-with-every-class", FillToSize([.. _ORDINARY_STATEMENTS, .. _EVERY_CLASS_CONSTRUCTS], TYPICAL_SIZE_IN_CHARACTERS)));
		shapes.Add(Shape($"{TEST_DATA_PREFIX}every-class-at-match-density", FillToSize(_EVERY_CLASS_CONSTRUCTS, WORST_SIZE_IN_CHARACTERS)));
	}

	/// <summary>
	/// THE FLOW-SIGNAL SHAPES: one long line per window family of the flow-signal expressions; the file of anchor-dense near misses of their anchors (each one character off a construct); the file of the forms at match
	/// density; the calls of their reads opened and never closed; the windows that are widest, in the look-behind and look-ahead of the standard-input read and the prompt (three hundred characters each way, every line
	/// holding one of them and padded to the width of a guard line), in the look-ahead of the terminal read (a hundred and ninety characters without a descriptor), in the argument window of the formatted scan (at and one past
	/// its unit bound) and in the tag of the markup input (at and one past its two hundred characters).
	/// </summary>
	private static void AddTheFlowSignalShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		string atTheUnitBound = new(_FILLER_UNIT, ARGUMENT_WINDOW_UNITS);
		string pastTheUnitBound = new(_FILLER_UNIT, ARGUMENT_WINDOW_UNITS + _ONE_UNIT_PAST_THE_BOUND);
		string tagAtTheBound = new(_FILLER_UNIT, MARKUP_TAG_CHARACTERS - _MARKUP_SEPARATING_BLANKS);
		string tagPastTheBound = new(_FILLER_UNIT, MARKUP_TAG_CHARACTERS - _MARKUP_SEPARATING_BLANKS + _ONE_UNIT_PAST_THE_BOUND);

		foreach ((string slug, string group) in _FLOW_LONG_LINE_GROUPS)
		{
			shapes.Add(FlowShape($"{FLOW_SIGNAL_PREFIX}long-line-{slug}", LongLines(group)));
		}

		shapes.Add(FlowShape($"{FLOW_SIGNAL_PREFIX}anchor-dense-near-misses", FillToSize(_FLOW_ANCHOR_DENSE_NEAR_MISSES, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(FlowShape($"{FLOW_SIGNAL_PREFIX}forms-at-match-density", FillToSize(_FLOW_FORMS_AT_MATCH_DENSITY, WORST_SIZE_IN_CHARACTERS)));
		shapes.Add(FlowShape($"{FLOW_SIGNAL_PREFIX}open-windows", FillWindowsToCeiling(_FLOW_OPEN_WINDOWS)));
		shapes.Add(FlowShape($"{FLOW_SIGNAL_PREFIX}standard-input-reads-in-full-prompt-windows", FillToCeilingWithLines(GuardWindowLine("reader := bufio.NewReader(os.Stdin); "))));
		shapes.Add(FlowShape($"{FLOW_SIGNAL_PREFIX}credential-prompts-in-full-read-windows", FillToCeilingWithLines(GuardWindowLine("fmt.Print(\"Enter your password: \"); out := os.Stdin; "))));
		shapes.Add(FlowShape($"{FLOW_SIGNAL_PREFIX}terminal-reads-with-long-arguments-and-no-descriptor", FillToCeilingWithLines($"password, err := term.ReadPassword({new string(_FILLER_UNIT, TERMINAL_READ_ARGUMENT_CHARACTERS)})\n")));
		shapes.Add(FlowShape($"{FLOW_SIGNAL_PREFIX}formatted-scan-window-at-the-unit-bound", FillToCeilingWithLines($"fmt.Scan({atTheUnitBound}&password)\n")));
		shapes.Add(FlowShape($"{FLOW_SIGNAL_PREFIX}formatted-scan-window-past-the-unit-bound", FillToCeilingWithLines($"fmt.Scan({pastTheUnitBound}&password)\n")));
		shapes.Add(FlowShape($"{FLOW_SIGNAL_PREFIX}markup-input-tag-at-the-bound", FillToCeilingWithLines($"body := \"<input {tagAtTheBound} type=\\\"password\\\">\"\n")));
		shapes.Add(FlowShape($"{FLOW_SIGNAL_PREFIX}markup-input-tag-past-the-bound", FillToCeilingWithLines($"body := \"<input {tagPastTheBound} type=\\\"password\\\">\"\n")));
	}

	/// <summary>A flow-signal shape: the High construct the engine needs before it evaluates the flow signals, then the text that makes one signal do its most work.</summary>
	private static MatchBoundStressShape FlowShape(string name, string text)
	{
		return Shape(name, string.Concat(FLOW_SHAPE_HIGH_CONSTRUCT, text));
	}

	/// <summary>A line of the width the guard windows of the expressions span: the carrier, then a comment padded with filler up to the width.</summary>
	private static string GuardWindowLine(string carrier)
	{
		return $"{carrier}{_COMMENT_OPENING} {new string(_FILLER_UNIT, _GUARD_WINDOW_LINE_LENGTH_IN_CHARACTERS - carrier.Length - _COMMENT_OPENING.Length - 1)}{_LINE_BREAK}";
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

	/// <summary>One block: the opening, the given number of ordinary members, then the key and the closing (both empty where the window is never closed).</summary>
	private static string WindowBlock(string opening, string key, string closing, int members)
	{
		StringBuilder block = new();

		block.Append(opening);

		for (int member = 0; member < members; member++)
		{
			block.Append(string.Format(CultureInfo.InvariantCulture, _MEMBER_FORMAT, member));
		}

		block.Append(key).Append(closing);

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
