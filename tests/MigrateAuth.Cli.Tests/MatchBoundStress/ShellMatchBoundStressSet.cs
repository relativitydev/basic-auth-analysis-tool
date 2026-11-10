using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE `Shell` SHAPES OF THE MATCH-BOUND STRESS SET, written beside the `PowerShell` and `VBNet` sets and read by the same kind of test: every shape is
/// legal, ordinary-looking shell text a client repository could contain, filled to just under the reader's size ceiling, and none is built to make an expression
/// backtrack catastrophically. What the shapes do is push every honest cost to the ceilings the engine already enforces: files at the size ceiling, lines at the
/// line-length ceiling, anchors and matches at the densest a generated script plausibly carries, and every bounded window of the catalog (the unit windows that open at
/// a client name, the character windows, the multi-line variable and array windows, the BDD and prompt windows) opened over and over without being closed.
///
/// THE SET CARRIES lines at the line-length ceiling of `\`-continued `curl` commands with long `-d` payloads, here-documents of
/// ten thousand lines, two thousand `&lt;&lt;` openings on one line, `$( )` nested eight deep and long argument arrays. Beside them, one long-line shape per lexical shape
/// of the profile (comments, quotes, the ANSI-C prefix, substitution holes, backslash runs, here-document openings, here-strings, arithmetic) and one long-line shape per
/// window family of the expressions.
///
/// THIS SET IS RUN over the draft expressions of <see cref="ShellDraftExpressions"/> by the cost gate and through the lexical scan by the lexical-budget gate. Every shape
/// is deterministic and invented: placeholders and generic names, no fragment of any real text.
/// </summary>
internal static class ShellMatchBoundStressSet
{
	internal const int TARGET_SIZE_IN_CHARACTERS = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;

	internal const int LONG_LINE_LENGTH_IN_CHARACTERS = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _LINE_MARGIN_IN_CHARACTERS;

	internal const string FLOW_SHAPE_NAME_PREFIX = "sh-flow-";

	internal const string LEXICAL_SHAPE_NAME_PREFIX = "sh-lexical-";

	internal const string DEFINITION_SHAPE_NAME_PREFIX = "sh-definition-";

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;
	private const int _LINE_MARGIN_IN_CHARACTERS = 16;
	private const int _LONG_STRING_LENGTH_IN_CHARACTERS = 380;
	private const int _BUILDER_SLACK_IN_CHARACTERS = 4096;
	private const int _CLOSING_QUOTE_RESERVE_IN_CHARACTERS = 8;
	private const int _FILLER_LINES_PER_WINDOW = 4;
	private const int _HERE_DOCUMENT_LINES = 10_000;
	private const int _OPENINGS_ON_ONE_LINE = 2_000;
	private const int _CONTINUED_COMMAND_LINES = 20;
	private const int _ARRAY_LINES = 600;
	private const int _ARRAY_QUERY_PAIRS = 399;
	private const int _BASIC_VARIABLE_FILLER_LINES = 6;
	private const int _BASIC_VARIABLE_FILLER_LINE_LENGTH = 590;
	private const int _DEFINITION_FILLER_LINES = 29;
	private const int _HERE_DOCUMENT_CONFIGURATION_LINES = 39;
	private const int _LOOP_FILLER_LINES = 20;
	private const int _TRACKED_VARIABLE_FILLER_LINES = 40;
	private const int _DEFINITION_FILLER_LINE_LENGTH = 395;
	private const string _KSH_SHEBANG_LINE = "#!/bin/ksh";
	private const string _ORDINARY_FILLER_STATEMENTS = "\ttotal=$((first + second))\n\tlog \"Order ${order_id} totals ${amount}\"\n\torder=$(get_order \"$order_id\")\n\tif [ -z \"$order\" ]; then return 0; fi\n";
	private const string _LINE_BREAK = "\n";
	private const string _SHEBANG_LINE = "#!/bin/bash";
	private const string _ANCHOR_LINE = "encoded=$(printf '%s' \"$payload\" | base64)";
	private const string _FUNCTION_FORMAT = "step{0:D6}() {{\n{1}}}\n";
	private const string _FILLER_STATEMENT = "\ttotal=$((first + second))\n";
	private const string _HIGH_CONSTRUCT_LINE = "curl -H \"Authorization: Basic dXNlcjpwYXNzd29yZA==\" https://example.test/api\n";
	private const string _NESTED_SUBSTITUTION_LINE = "echo \"$(echo \"$(echo \"$(echo \"$(echo \"$(echo \"$(echo \"$(echo value)\")\")\")\")\")\")\")\"\n";
	private const string _HERE_DOCUMENT_BODY_LINE_FORMAT = "line {0:D6} of a generated configuration with curl and wget and read -s in prose\n";
	private const string _PAYLOAD_UNIT = "{\"field\":\"value\",\"items\":[1,2,3],\"flag\":true},";
	private const string _BASIC_VARIABLE_LINE = "AUTH_VALUE=\"Basic dXNlcjpwYXNzd29yZA==\"\n";
	private const string _ARRAY_ELEMENTS_LINE = "\t--connect-timeout 5 --max-time 30 --retry 3 'quoted value' \"double value\" --silent --show-error\n";

	private static readonly ImmutableArray<string> _ORDINARY_STATEMENTS =
	[
		"\torder=$(get_order \"$order_id\")",
		"\tif [ -z \"$order\" ]; then return 0; fi",
		"\tamount=$(echo \"$order\" | jq '.lines | map(.total) | add')",
		"\tlog \"Order ${order_id} totals ${amount}\"",
		"\tfor item in \"${items[@]}\"; do names+=(\"${item^^}\"); done"
	];

	private static readonly ImmutableArray<string> _ANCHOR_DENSE_NEAR_MISSES =
	[
		"\tlabel=$CREDENTIAL_NAME",
		"\techo \"the Authorization header is disabled\"",
		"\tcurl --version",
		"\twget --help",
		"\tcurl -s https://example.test/health",
		"\twget -q https://example.test/file.tar.gz",
		"\tprintf '%s\\n' \"basic\"",
		"\tpayload_encoded=$(printf '%s' \"$payload\" | base64)",
		"\tread -r line < \"$input_file\"",
		"\tstty sane",
		"\t# Authorization and Basic appear in prose only, never in code",
		"\tcurl -o out.txt -H 'Accept: application/json' https://example.test/a",
		"\tssh -p 22 host uptime"
	];

	private static readonly ImmutableArray<string> _EVERY_CLASS_CONSTRUCTS =
	[
		"\tcurl -u admin:secretvalue https://example.test/api",
		"\tcurl -s -u \"$USER_NAME:$USER_SECRET\" https://example.test/api",
		"\tcurl -u admin https://example.test/api",
		"\tcurl -H \"Authorization: Basic dXNlcjpwYXNzd29yZA==\" https://example.test/api",
		"\twget --auth-no-challenge --user=admin --password=secretvalue https://example.test/x",
		"\twget --user=admin --password=secretvalue https://example.test/x",
		"\tcurl https://admin:secretvalue@example.test/api",
		"\tcurl --netrc https://example.test/api",
		"\tprintf '%s' \"admin:secretvalue\" | base64",
		"\tAUTH_HEADER=\"Basic dXNlcjpwYXNzd29yZA==\"\n\tcurl -H \"Authorization: $AUTH_HEADER\" https://example.test/api",
		"\tARGS=(-u admin:secretvalue https://example.test/api)\n\tcurl \"${ARGS[@]}\"",
		"\tREMOTE_URL=\"https://admin:secretvalue@example.test/repo.git\"",
		"basic_auth_header() { printf 'Basic %s' \"$1\"; }",
		"\tread -r -s -p 'Value: ' answer",
		"\tstty -echo\n\tread -r answer\n\tstty echo",
		"\tread -r PASSWORD",
		"\tzenity --password",
		"\twget --ask-password https://example.test/x",
		"\tvalue=$(pass show service/entry)",
		"\tread -r PASSWORD < /run/secrets/entry",
		"\tosascript -e 'display dialog \"p\" with hidden answer'",
		"\tcurl -s \"https://example.test/a\" -H \"X: ${API_TOKEN}\""
	];

	private static readonly ImmutableArray<string> _OPEN_WINDOWS =
	[
		"\tcurl -s \\",
		"\twget -q",
		"\tread -r",
		"\tAUTH_VALUE=\"Basic abc\"",
		"\tARGS=(-s -S",
		"\tstty -echo",
		"Describe 'suite'",
		"\techo \"Enter the password\"",
		"\tosascript -e 'display dialog \"x\"'",
		"\tprintf '%s' \"user:value\" |"
	];

	private static readonly ImmutableArray<string> _LITERAL_AND_COMMENT_LINES =
	[
		"\t# curl -u admin:secretvalue was removed in the migration to tokens",
		"\techo \"curl -H 'Authorization: Basic abc' is deprecated\"",
		"\tpath='/data/credentials/archive/Authorization'",
		"\tmessage=\"Credential ${name} refreshed for ${user} at $(date -u)\"",
		"\t: 'wget --user=admin --password=secretvalue url is disabled'"
	];

	private static readonly ImmutableArray<string> _LEXICAL_HEAVY_LINES =
	[
		"\tcat <<EOF\nbody line one\nbody line two with $variable\nEOF\n\tafter_one=1",
		"\tcat <<-'EOT'\n\tbody $(not run) line\n\tEOT\n\tafter_two=2",
		"\tquoted=$'a\\'b c'",
		"\techo \"$(echo \"$(echo nested)\")\"",
		"\techo $(( 1 << 3 ))",
		"\tcat <<<\"$text\"",
		"\tcat <<''\nblank-terminated body\n\n\tafter_three=3",
		"\tcount=${#items[@]} ; hex=16#ff ; echo $# ; echo a#b"
	];

	/// <summary>
	/// THE BENIGN LONG-LINE SHAPES OF THE EXPRESSIONS: each file is lines of just under the line-length ceiling that repeat one ordinary group, after a first line that
	/// carries a preselection anchor so that every class runs over it. No group holds a construct of any class. What each exercises is a window that opens at a frequent
	/// token and is scanned forward for a rarer one, which on a line at the ceiling costs the square of the line length times the window. Each is named
	/// <c>sh-long-line-</c> and its slug.
	/// </summary>
	private static readonly ImmutableArray<(string Slug, string Group)> _LONG_LINE_GROUPS =
	[
		("curl-with-plain-words", "curl x "),
		("curl-with-quoted-strings", "curl 'a b' \"c d\" "),
		("curl-with-url-and-no-userinfo", "curl https://example.test/p "),
		("curl-with-header-flags-and-no-authorization", "curl -H 'Accept: x' "),
		("curl-with-expansions", "curl \"$a\" $b "),
		("wget-with-plain-words", "wget x "),
		("wget-with-url-and-no-credentials", "wget -q https://example.test/p "),
		("echo-pairs-without-a-pipe", "echo a:b "),
		("printf-pairs-without-a-pipe", "printf '%s' \"a:b\" "),
		("reads-with-flags", "read -r x "),
		("reads-with-prompts", "read -p 'x' y "),
		("prompt-wording-without-a-read", "echo \"Password\" "),
		("dialogs-without-password-forms", "whiptail --yesno x zenity --info y "),
		("osascript-without-hidden-answer", "osascript -e x "),
		("ssh-and-sudo-exclusion-tokens", "sudo -S ssh read -s x "),
		("user-flags-of-other-commands", "ssh -u a mount -u b "),
		("unclosed-arrays", "A=( -a -b "),
		("urls-with-at-signs", "https://example.test/p@q "),
		("stty-without-echo", "stty -F x sane "),
		("secret-store-near-misses", "pass x passwd show gopass "),
		("mixed-command-words", "curl x wget y read -r z echo w printf v stty q osascript o whiptail p "),
		("units-with-long-strings", $"curl '{new string('z', _LONG_STRING_LENGTH_IN_CHARACTERS)}' "),
		("http-with-plain-words", "http x https y "),
		("aria2c-with-plain-words", "aria2c x "),
		("lwp-request-with-plain-words", "lwp-request x "),
		("hand-built-request-sinks", "nc x ncat y /dev/tcp openssl s_client "),
		("curl-configuration-options", "curl -K x --config y "),
		("wget-configuration-assignments", "WGETRC=x wget --config y "),
		("quoted-hand-built-request-paths", "exec 3<>\"/dev/tcp/h/80\" >> '/dev/tcp/h/81' > \"/dev/tcp\" "),
		("variables-with-path-suffixes", "curl -u a:b \"$B/x\" \"${C}/y/$z\" "),
		("loop-reads", "while read -r p; do "),
		("encoder-pipes-without-a-pair", "echo hello | base64 printf x | tr a b | base64 "),
		("python-and-perl-programs", "python3 -c 'x' perl -e 'y' "),
		("ssh-sudo-and-prompt-reads", "sudo -S ssh read -rs v read -p 'Password: ' w "),
		("mixed-clients-with-the-added-ones", "curl x wget y http z aria2c w lwp-request v "),
		("expansions-in-client-windows", "curl \"$a\" http \"$b\" aria2c $c "),
		("continuations-then-clients", "curl \\\nwget \\\nhttp \\\n")
	];

	/// <summary>
	/// THE DEFINITION FAMILIES: each file is a block repeated to the size ceiling, and a block is one definition line (an alias, a command variable, a variable that
	/// holds a scheme, a header, an array, an option string, a pair, a URL with userinfo, a written configuration, a loop that reads) followed by filler lines at the
	/// definition window's width and then a use that is benign or absent, so the expression opened at the definition reads the whole window and fails. Each is named
	/// <c>sh-definition-</c> and its slug.
	/// </summary>
	private static readonly ImmutableArray<(string Slug, string Definition, int FillerLines, string Tail)> _DEFINITION_BLOCKS =
	[
		("alias-then-client-without-credentials", "alias c=curl", _DEFINITION_FILLER_LINES, "c -sS \"$url\"\n"),
		("command-variable-then-client-without-credentials", "CURL=$(command -v curl)", _DEFINITION_FILLER_LINES, "$CURL -sS \"$url\"\n"),
		("wget-command-variable-then-client-without-credentials", "WGET=wget", _DEFINITION_FILLER_LINES, "$WGET -q \"$url\"\n"),
		("non-http-scheme-variable-then-user-option", "FTP_URL=ftp://host.example/x", _DEFINITION_FILLER_LINES, "curl -u a:b \"$FTP_URL\"\n"),
		("http-scheme-variable-then-user-option", "BASE_URL=https://host.example/x", _DEFINITION_FILLER_LINES, "curl -u a:b \"$BASE_URL\"\n"),
		("header-variable-then-no-use", "HEADER=\"Authorization: Basic dXNlcjpwYXNz\"", _DEFINITION_FILLER_LINES, "echo done\n"),
		("array-with-a-credential-option-then-no-use", "opts=(-u \"$U:$P\")", _DEFINITION_FILLER_LINES, "echo done\n"),
		("option-string-then-no-use", "OPTS=\"-sS -u $U:$P\"", _DEFINITION_FILLER_LINES, "echo done\n"),
		("pair-then-no-encoder", "pair=\"$U:$P\"", _DEFINITION_FILLER_LINES, "echo done\n"),
		("url-with-userinfo-then-no-client", "API_URL=\"https://u:p@host.example/api\"", _DEFINITION_FILLER_LINES, "echo done\n"),
		("encoded-assignment-then-no-client", "ENC=$(echo -n \"$U:$P\" | base64)", _DEFINITION_FILLER_LINES, "echo done\n"),
		("configuration-write-then-other-path", "printf 'user = \"%s:%s\"\\n' \"$U\" \"$P\" > \"$cfg\"", _DEFINITION_FILLER_LINES, "curl -K \"$other\" \"$url\"\n"),
		("here-document-configuration-body", "cat > \"$cfg\" <<EOF", _HERE_DOCUMENT_CONFIGURATION_LINES, "EOF\ncurl -K \"$other\" \"$url\"\n"),
		("wget-rc-assignment-then-no-write", "export WGETRC=\"$rc\"", _DEFINITION_FILLER_LINES, "echo done\n"),
		("group-configuration-text-then-no-redirect", "\techo \"http_user = x\"", _DEFINITION_FILLER_LINES, "}\necho done\n"),
		("non-http-scheme-variable-then-path-suffix-use", "FTP_URL=ftp://host.example/x", _DEFINITION_FILLER_LINES, "curl -u a:b \"$FTP_URL/f\"\n"),
		("http-scheme-variable-then-path-suffix-use", "BASE_URL=https://host.example/x", _DEFINITION_FILLER_LINES, "curl -u a:b \"${BASE_URL}/f\"\n"),
		("loop-read-with-a-far-done", "while read -r PASS; do", _LOOP_FILLER_LINES, "done\n"),
		("silent-read-then-forty-lines", "read -rs P", _TRACKED_VARIABLE_FILLER_LINES, "echo done\n"),
		("credential-named-read-then-forty-lines", "read -r PASS", _TRACKED_VARIABLE_FILLER_LINES, "echo done\n"),
		("prompt-read-then-forty-lines", "read -p 'Password: ' P", _TRACKED_VARIABLE_FILLER_LINES, "echo done\n")
	];

	private static readonly ImmutableArray<(string Slug, string Block)> _CONTINUATION_CHAINS =
	[
		("continued-mixed-clients", "curl -sS \\\n\twget -q \\\n\thttp --follow \\\n\taria2c -x4 \\\n\tlwp-request -m GET \\\n\tnc -w1 host \\\n\tread -r v \\\n\techo \"x\" \\\n\thttps://example.test/api\n"),
		("continued-clients-with-expansions", "curl \\\n\t\"$a\" \\\n\thttp \\\n\t\"$b\" \\\n\taria2c \\\n\t$c \\\n\tx\n")
	];

	/// <summary>One long-line shape per lexical shape of the profile, each repeating one group: the construct whose opening the lexical scan has to recognize.</summary>
	private static readonly ImmutableArray<(string Slug, string Group)> _LEXICAL_LONG_LINE_GROUPS =
	[
		("comments-in-words", "a#b ${#v} $# 16#ff "),
		("single-quotes", "'x' 'y z' "),
		("double-quotes", "\"x\" \"y z\" "),
		("ansi-c-quotes", "$'a\\'b' "),
		("substitution-holes", "\"$(a)\" \"${b}\" \"$c\" "),
		("backslash-runs-before-quotes", "\\\\\\\\\\\\\\\\\"x\" \\\\\\\\\\\\\\\\'y' "),
		("here-document-openings", "<<a "),
		("tab-stripping-openings", "<<-a "),
		("here-strings", "cat <<< x "),
		("closed-arithmetic", "(( a << 1 )) $(( 1 << 2 )) "),
		("let-lines", "let x=1<<2 ")
	];

	/// <summary>
	/// The shapes that make the arithmetic read-ahead - the arithmetic opening's evaluation, which the lexical budget charges - work at the file-size ceiling: a closed expression on every line (one short read-ahead each),
	/// an unclosed double parenthesis on every line (one read-ahead to the end of the text, then every run answered from it), nested groups, and a double parenthesis opened before a here-document whose body holds an odd quote, so that the read-ahead's
	/// own reading of quotes disagrees with the walk's zones and each run it did not pass in code starts a read-ahead of its own, up to the per-file bound on what the read-ahead may read.
	/// </summary>
	private static readonly ImmutableArray<(string Slug, string Block)> _ARITHMETIC_READ_AHEAD_BLOCKS =
	[
		("closed-on-every-line", "x=$(( a + 1 ))\n(( b++ ))\n"),
		("unclosed-on-every-line", "(( x\n"),
		("nested-groups", "(( ((a)) (( b )) )) $(( ((c)) ))\n"),
		("read-ahead-and-zones-disagree", "(( x <<E\n\" u\nE\n")
	];

	private const string _HERE_DOCUMENT_OPENING = "<<";
	private const string _TERMINATOR_LINE = "a\n";
	private const string _OPENINGS_AT_THE_BOUND_GROUP = "<<a";
	private const string _UNCLOSED_ARITHMETIC_PREFIX = "(( x ";
	private const string _UNCLOSED_ARITHMETIC_OPENING_GROUP = "<<a ";

	private static readonly ImmutableArray<(string Name, string Block)> _FLOW_BLOCKS =
	[
		("silent-read-flag-forms", "\tread -rs -p 'next value: ' value\n"),
		("stty-echo-toggles", "\tstty -echo\n\tread value\n\tstty echo\n"),
		("credential-named-reads", "\tread -r PASSWORD\n"),
		("password-dialogs", "\twhiptail --passwordbox 'x' 8 40 2>&1\n"),
		("prompt-wording", "\techo \"Enter the password:\"\n\tread value\n"),
		("credential-named-expansions", "\tcurl -s \"https://example.test/$PATH_PART\" -H \"X: ${API_TOKEN}\"\n"),
		("secret-store-tools", "\tvalue=$(pass show service/entry)\n"),
		("redirected-reads", "\tread -r PASSWORD < /run/secrets/entry\n"),
		("exclusion-tokens", "\techo x | sshpass -p y ssh host read -s v\n"),
		("loop-reads-from-files", "\twhile IFS= read -r PASS; do echo x; done < \"$secrets\"\n"),
		("http-prompts", "\thttp -a admin \"$host\"\n" + _ORDINARY_FILLER_STATEMENTS),
		("curl-prompts", "\tcurl -u admin \"$url\"\n" + _ORDINARY_FILLER_STATEMENTS),
		("alias-prompts", "\talias c=curl\n\tc -u admin \"$url\"\n" + _ORDINARY_FILLER_STATEMENTS),
		("hand-built-requests", "\tprintf 'GET /x HTTP/1.1\\r\\nHost: h\\r\\n\\r\\n' | nc host.example 80\n"),
		("encoded-pairs", "\tenc=$(printf '%s:%s' \"$u\" \"$p\" | base64)\n\techo \"$enc\"\n" + _ORDINARY_FILLER_STATEMENTS)
	];

	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		AddCeilingShapes(shapes);
		AddLongLineShapes(shapes);
		AddFlowDefinitionAndContinuationShapes(shapes);

		return shapes.ToImmutable();
	}

	private static void AddCeilingShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		shapes.Add(Shape("sh-ordinary-at-ceiling", FillToCeiling(_ORDINARY_STATEMENTS)));
		shapes.Add(Shape("sh-anchor-dense-near-misses", FillToCeiling(_ANCHOR_DENSE_NEAR_MISSES)));
		shapes.Add(Shape("sh-every-class-at-match-density", FillToCeiling(_EVERY_CLASS_CONSTRUCTS)));
		shapes.Add(Shape("sh-open-windows", FillWindowsToCeiling(_OPEN_WINDOWS)));
		shapes.Add(Shape("sh-literals-and-comments", FillToCeiling(_LITERAL_AND_COMMENT_LINES)));
		shapes.Add(Shape("sh-lexical-heavy", FillToCeiling(_LEXICAL_HEAVY_LINES)));
		shapes.Add(Shape("sh-continued-curl-commands-with-long-payloads", ContinuedCurlCommands()));
		shapes.Add(Shape("sh-here-documents-of-ten-thousand-lines", HereDocumentsOfTenThousandLines()));
		shapes.Add(Shape("sh-two-thousand-openings-on-one-line", TwoThousandOpeningsOnOneLine()));
		shapes.Add(Shape("sh-substitution-nested-eight-deep", FillToCeilingWithLines(_NESTED_SUBSTITUTION_LINE)));
		shapes.Add(Shape("sh-long-argument-arrays", LongArgumentArrays()));
		shapes.Add(Shape("sh-unfinished-array-lines-to-the-size-ceiling", FillToCeilingWithLines("x=(\n")));
		shapes.Add(Shape("sh-one-line-arrays-of-four-hundred-pairs", OneLineArraysOfFourHundredPairs()));
		shapes.Add(Shape("sh-basic-variable-then-long-lines", BasicVariableThenLongLines()));
		shapes.Add(Shape("sh-describe-lines-without-it", FillToCeilingWithLines("Describe 'suite'\n")));
		shapes.Add(Shape("sh-stty-echo-lines-without-read", FillToCeilingWithLines("stty -echo\n")));
		shapes.Add(Shape("sh-generated-marker-lookalike-lines", FillToCeilingWithLines("# generated by hand, not auto\n")));
		shapes.Add(Shape("sh-group-configuration-text-lines-without-a-redirect", FillToCeilingWithLines("\techo \"http_user = x\"\n")));
	}

	private static void AddLongLineShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string slug, string group) in _LONG_LINE_GROUPS)
		{
			shapes.Add(Shape($"sh-long-line-{slug}", LongLines(group)));
		}

		foreach ((string slug, string group) in _LEXICAL_LONG_LINE_GROUPS)
		{
			shapes.Add(Shape($"{LEXICAL_SHAPE_NAME_PREFIX}long-line-{slug}", LongLines(group, TerminatorLineOf(group))));
		}

		shapes.Add(Shape($"{LEXICAL_SHAPE_NAME_PREFIX}long-line-openings-at-the-queue-bound", LongLinesOfOpeningsAtTheBound()));
		shapes.Add(Shape($"{LEXICAL_SHAPE_NAME_PREFIX}long-line-unclosed-arithmetic-then-openings", LongLinesOfUnclosedArithmeticThenOpenings()));

		foreach ((string slug, string block) in _ARITHMETIC_READ_AHEAD_BLOCKS)
		{
			shapes.Add(Shape($"{LEXICAL_SHAPE_NAME_PREFIX}arithmetic-{slug}", FillToCeilingWithLines(block)));
		}
	}

	private static void AddFlowDefinitionAndContinuationShapes(ImmutableArray<MatchBoundStressShape>.Builder shapes)
	{
		foreach ((string name, string block) in _FLOW_BLOCKS)
		{
			shapes.Add(Shape($"{FLOW_SHAPE_NAME_PREFIX}{name}", FlowShape(block)));
		}

		foreach ((string slug, string definition, int fillerLines, string tail) in _DEFINITION_BLOCKS)
		{
			shapes.Add(Shape($"{DEFINITION_SHAPE_NAME_PREFIX}{slug}", DefinitionBlocks(definition, fillerLines, tail)));
		}

		foreach ((string slug, string block) in _CONTINUATION_CHAINS)
		{
			shapes.Add(Shape($"sh-continuation-{slug}", FillToCeilingWithLines(block)));
		}

		shapes.Add(Shape($"{FLOW_SHAPE_NAME_PREFIX}silent-reads-in-a-ksh-file", FlowShapeWithShebang(_KSH_SHEBANG_LINE, "\tread -rs -p 'next value: ' value\n")));
		shapes.Add(Shape($"{FLOW_SHAPE_NAME_PREFIX}prompt-reads-in-a-ksh-file", FlowShapeWithShebang(_KSH_SHEBANG_LINE, "\tread -p 'Password: ' value\n")));
	}

	internal static string RelativePathOf(MatchBoundStressShape shape)
	{
		return string.Format(CultureInfo.InvariantCulture, "stress/{0}{1}", shape.Name, shape.FileExtension);
	}

	private static MatchBoundStressShape Shape(string name, string text)
	{
		return new MatchBoundStressShape { Name = name, Text = text, FileExtension = ".sh" };
	}

	private static string Header()
	{
		return Header(_SHEBANG_LINE);
	}

	private static string Header(string shebangLine)
	{
		return $"{shebangLine}{_LINE_BREAK}set -eu\n{_ANCHOR_LINE}{_LINE_BREAK}";
	}

	private static string FillToCeiling(ImmutableArray<string> statements)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);
		int functionNumber = 0;

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			StringBuilder body = new();

			foreach (string statement in statements)
			{
				body.Append(statement).Append(_LINE_BREAK);
			}

			builder.Append(string.Format(CultureInfo.InvariantCulture, _FUNCTION_FORMAT, functionNumber++, body));
		}

		return builder.ToString();
	}

	private static string FillToCeilingWithLines(string line)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(line);
		}

		return builder.ToString();
	}

	private static string FillWindowsToCeiling(ImmutableArray<string> openers)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);
		int functionNumber = 0;

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			StringBuilder body = new();

			foreach (string opener in openers)
			{
				body.Append(opener).Append(_LINE_BREAK);

				for (int filler = 0; filler < _FILLER_LINES_PER_WINDOW; filler++)
				{
					body.Append(_FILLER_STATEMENT);
				}
			}

			builder.Append(string.Format(CultureInfo.InvariantCulture, _FUNCTION_FORMAT, functionNumber++, body));
		}

		return builder.ToString();
	}

	private static string LongLines(string group)
	{
		return LongLines(group, string.Empty);
	}

	/// <summary>Lines of one repeated group, each followed by one terminator line per repeat when the group opens here-documents, so the bodies close and the next long line is read as code.</summary>
	private static string LongLines(string group, string terminatorLine)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			int lineStart = builder.Length;
			int repeats = 0;

			while (builder.Length - lineStart + group.Length < LONG_LINE_LENGTH_IN_CHARACTERS)
			{
				builder.Append(group);
				repeats++;
			}

			builder.Append(_LINE_BREAK);

			for (int body = 0; body < repeats && terminatorLine.Length > 0; body++)
			{
				builder.Append(terminatorLine);
			}
		}

		return builder.ToString();
	}

	/// <summary>The line that closes the here-document a group opens, or nothing for a group that opens none (a here-string, an arithmetic context and a `let` line are refused as openings).</summary>
	private static string TerminatorLineOf(string group)
	{
		bool opensHereDocuments = group.StartsWith(_HERE_DOCUMENT_OPENING, StringComparison.Ordinal);

		return opensHereDocuments ? _TERMINATOR_LINE : string.Empty;
	}

	/// <summary>Lines of back-to-back openings with no blank between them, as many as the line holds: the densest a line can carry, at the queue bound the scanner enforces.</summary>
	private static string LongLinesOfOpeningsAtTheBound()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			int lineStart = builder.Length;
			int repeats = 0;

			builder.Append("cat ");

			while (builder.Length - lineStart + _OPENINGS_AT_THE_BOUND_GROUP.Length < LONG_LINE_LENGTH_IN_CHARACTERS)
			{
				builder.Append(_OPENINGS_AT_THE_BOUND_GROUP);
				repeats++;
			}

			builder.Append(_LINE_BREAK);

			for (int body = 0; body < repeats; body++)
			{
				builder.Append(_TERMINATOR_LINE);
			}
		}

		return builder.ToString();
	}

	/// <summary>A line that opens an arithmetic context and never closes it, then carries as many here-document openings as it holds: each opening's predicate reads the whole prefix.</summary>
	private static string LongLinesOfUnclosedArithmeticThenOpenings()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			int lineStart = builder.Length;

			builder.Append(_UNCLOSED_ARITHMETIC_PREFIX);

			while (builder.Length - lineStart + _UNCLOSED_ARITHMETIC_OPENING_GROUP.Length < LONG_LINE_LENGTH_IN_CHARACTERS)
			{
				builder.Append(_UNCLOSED_ARITHMETIC_OPENING_GROUP);
			}

			builder.Append(_LINE_BREAK);
		}

		return builder.ToString();
	}

	/// <summary>`\`-continued commands whose every line is at the line-length ceiling: a `-d` payload on each, then a final line with the URL and no credential option.</summary>
	private static string ContinuedCurlCommands()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append("curl -X POST \\\n");

			for (int line = 0; line < _CONTINUED_COMMAND_LINES; line++)
			{
				int lineStart = builder.Length;

				builder.Append("\t-d '");

				while (builder.Length - lineStart + _PAYLOAD_UNIT.Length < LONG_LINE_LENGTH_IN_CHARACTERS - _CLOSING_QUOTE_RESERVE_IN_CHARACTERS)
				{
					builder.Append(_PAYLOAD_UNIT);
				}

				builder.Append("' \\\n");
			}

			builder.Append("\thttps://example.test/api\n");
		}

		return builder.ToString();
	}

	private static string HereDocumentsOfTenThousandLines()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append("cat <<EOF > generated.conf\n");

			for (int line = 0; line < _HERE_DOCUMENT_LINES; line++)
			{
				builder.Append(string.Format(CultureInfo.InvariantCulture, _HERE_DOCUMENT_BODY_LINE_FORMAT, line));
			}

			builder.Append("EOF\n");
		}

		return builder.ToString();
	}

	private static string TwoThousandOpeningsOnOneLine()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append("cat");

			for (int opening = 0; opening < _OPENINGS_ON_ONE_LINE; opening++)
			{
				builder.Append($" {_OPENINGS_AT_THE_BOUND_GROUP}");
			}

			builder.Append(_LINE_BREAK);

			for (int body = 0; body < _OPENINGS_ON_ONE_LINE; body++)
			{
				builder.Append(_TERMINATOR_LINE);
			}
		}

		return builder.ToString();
	}

	/// <summary>Long argument arrays: one array of hundreds of elements over many lines, closed, then the command that expands it.</summary>
	private static string LongArgumentArrays()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append("REQUEST_ARGS=(\n");

			for (int line = 0; line < _ARRAY_LINES; line++)
			{
				builder.Append(_ARRAY_ELEMENTS_LINE);
			}

			builder.Append(")\ncurl \"${REQUEST_ARGS[@]}\" https://example.test/api\n");
		}

		return builder.ToString();
	}

	/// <summary>Arrays on one physical line of about nine thousand characters, each with a credential option at its start, closed, then the command that expands it.</summary>
	private static string OneLineArraysOfFourHundredPairs()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append("fields=(--silent -u user:pass");

			for (int pair = 0; pair < _ARRAY_QUERY_PAIRS; pair++)
			{
				builder.Append(" --url-query 'col=0001'");
			}

			builder.Append(")\ncurl \"${fields[@]}\" https://example.test/api\n");
		}

		return builder.ToString();
	}

	/// <summary>A variable that holds a Basic value, then lines at the window's ceiling that never use it in a header.</summary>
	private static string BasicVariableThenLongLines()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(_BASIC_VARIABLE_LINE);

			for (int line = 0; line < _BASIC_VARIABLE_FILLER_LINES; line++)
			{
				builder.Append("echo ").Append(new string('x', _BASIC_VARIABLE_FILLER_LINE_LENGTH)).Append(_LINE_BREAK);
			}
		}

		return builder.ToString();
	}

	/// <summary>A definition line, filler lines of the definition window's width, then a benign or absent use: the block repeated to the size ceiling.</summary>
	private static string DefinitionBlocks(string definition, int fillerLines, string tail)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);
		string filler = $"echo {new string('x', _DEFINITION_FILLER_LINE_LENGTH)}{_LINE_BREAK}";

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(definition).Append(_LINE_BREAK);

			for (int line = 0; line < fillerLines; line++)
			{
				builder.Append(filler);
			}

			builder.Append(tail);
		}

		return builder.ToString();
	}

	/// <summary>A first line that carries a High construct, so the flow signals are evaluated, then the block each flow signal does its most work on.</summary>
	private static string FlowShape(string block)
	{
		return FlowShapeWithShebang(_SHEBANG_LINE, block);
	}

	private static string FlowShapeWithShebang(string shebangLine, string block)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		builder.Append(Header(shebangLine));
		builder.Append(_HIGH_CONSTRUCT_LINE);

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(block);
		}

		return builder.ToString();
	}
}
