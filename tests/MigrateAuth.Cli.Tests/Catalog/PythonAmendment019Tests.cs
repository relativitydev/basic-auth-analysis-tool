using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// Three readings of the `Python` analyzer, through the analyzer the binary ships and the real recognition engine. First: the colon pair of an `f`-string whose replacement fields hold quotes (`PY-007`), the nested
/// same-quote form of Python 3.12 included. Second: the held-name shape of `PY-007` when the encoder call spans lines. Third: the compound held scheme value of `PY-013`, and the credential position of the declared literal
/// `NAME = "Basic token"`. Each numbered case states the outcome it expects: the constructs that become readable, and the controls that must keep their outcome. Every name is invented, and no fragment of any repository
/// appears.
/// </summary>
public sealed class PythonAmendment019Tests
{
	private const string _NONE = "none";
	private const string _PAIR_MEDIUM = "PY-007@Medium";
	private const string _HELD_SCHEME_MEDIUM = "PY-013@Medium";
	private const string _PAIR_LOW = "PY-007@Low";
	private const string _HELD_SCHEME_LOW = "PY-013@Low";
	private const string _PADDING_LINE = "# xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
	private const int _PADDING_LINE_COUNT = 3;
	private const int _LONG_QUOTED_KEY_CHARACTERS = 50;
	private const int _HALF_BOUND_IN_CHARACTERS = 100;
	private const int _HALF_PAST_THE_BOUND_IN_CHARACTERS = 101;
	private const int _ONE_CHARACTER = 1;
	private const int _ONE_UNIT = 1;
	private const int _FIELD_NAME_CHARACTERS = 15;
	private const int _MAXIMUM_FIELD_NAME_CHARACTERS = 54;
	private const int _MAXIMUM_FIELD_KEY_CHARACTERS = 40;
	private const int _FIELDS_IN_A_HALF_PAST_THE_BOUND = 7;
	private const int _FIELDS_IN_A_HALF_AT_THE_BOUND = 5;
	private const int _SEAM_BLANKS_AT_THE_BOUND = 39;
	private const int _SEAM_BLANKS_PAST_THE_BOUND = 40;
	private const char _FILLER_CHARACTER = 'h';
	private const char _LONG_KEY_CHARACTER = 'u';
	private const char _SEAM_BLANK = ' ';
	private const char _GROUP_UNIT_CHARACTER = 'x';
	private const char _NESTED_UNIT_CHARACTER = 'y';
	private const int _GROUP_BOUND_IN_UNITS = 120;
	private const int _NESTED_GROUP_BOUND_IN_CHARACTERS = 120;
	private const string _CARRIER_MAPPING = """response = requests.get(url, headers={"Authorization": auth_value})""";
	private const string _HELD_CREDENTIALS = "credentials = f\"{service_user}:{service_password}\"";
	private const string _BASIC_COMMENT = "# Basic credentials";
	private const string _TOKEN_DECLARATION = "auth_value = \"Basic dXNlcjpwYXNz\"";
	private const string _ENCODER_PAIR_WITH_QUOTES = """base64.b64encode(f'{cfg["user"]}:{cfg["password"]}'.encode()).decode()""";
	private const string _ENCODER_PAIR_WITH_SAME_QUOTES = """base64.b64encode(f"{cfg["user"]}:{cfg["password"]}".encode()).decode()""";
	private const string _ENCODER_PAIR_WITH_PLAIN_FIELDS = """base64.b64encode(f"{service_user}:{service_password}".encode()).decode()""";

	// ------------------------------------------------------------------------------------------
	// Reading A
	// ------------------------------------------------------------------------------------------

	public static TheoryData<string, string, string> ReadingAPlants() => new()
	{
		{ "A.01", Source($"basic_value = {_ENCODER_PAIR_WITH_QUOTES}"), _PAIR_MEDIUM },
		{ "A.02", Source("""basic_value = b64encode(f"{os.getenv('SERVICE_USER')}:{os.getenv('SERVICE_PASSWORD')}".encode()).decode()"""), _PAIR_MEDIUM },
		{ "A.03", Source("""basic_value = base64.b64encode(f"{settings.get('user')}:{settings.get('pw')}".encode()).decode()"""), _PAIR_MEDIUM },
		{ "A.04", Source("""basic_value = base64.b64encode(f"{os.environ['SERVICE_USER']}:{os.environ['SERVICE_PASSWORD']}".encode()).decode()"""), _PAIR_MEDIUM },
		{ "A.05", Source("""basic_value = base64.b64encode(f'{conf["db"]["user"]}:{conf["db"]["secret"]}'.encode()).decode()"""), _PAIR_MEDIUM },
		{ "A.06", Source("""basic_value = base64.b64encode(f"{os.getenv('SERVICE_USER', 'svc')}:{os.getenv('SERVICE_PASSWORD', 'none')}".encode()).decode()"""), _PAIR_MEDIUM },
		{ "A.07", Source("""basic_value = base64.b64encode(f"{creds.get(keys['user'])}:{creds.get(keys['password'])}".encode()).decode()"""), _PAIR_MEDIUM },
		{ "A.08", Source("import base64 as codec", """basic_value = codec.standard_b64encode(f"{row['name']}:{row['pass']}".encode()).decode()"""), _PAIR_MEDIUM },
		{ "A.09", Source("""basic_value = base64.b64encode(f"svc:{vault.get('pw')}".encode()).decode()"""), _PAIR_MEDIUM },
		{ "A.10", Source("# Authorization value", """token = base64.encodebytes(f'{account["user"]}:{account["token"]}'.encode())"""), _PAIR_MEDIUM },
		{ "A.11", Source($"basic_value = {_ENCODER_PAIR_WITH_SAME_QUOTES}"), _PAIR_MEDIUM },
		{ "A.12", Source("""basic_value = base64.b64encode(f'{os.getenv('SERVICE_USER')}:{os.getenv('SERVICE_PASSWORD')}'.encode()).decode()"""), _PAIR_MEDIUM }
	};

	public static TheoryData<string, string, string> ReadingAControls() => new()
	{
		{ "AC.01", Source($"basic_value = {_ENCODER_PAIR_WITH_PLAIN_FIELDS}"), _PAIR_MEDIUM },
		{ "AC.02", Source("basic_value = base64.b64encode(service_user + \":\" + service_password)"), _PAIR_MEDIUM },
		{ "AC.03", Source("basic_value = base64.b64encode(\"%s:%s\" % (service_user, service_password))"), _PAIR_MEDIUM },
		{ "AC.04", Source("""cache_key = b64encode(f"{row['host']}:{row['port']}".encode())"""), _NONE },
		{ "AC.05", Source("""basic_value = base64.urlsafe_b64encode(f'{cfg["user"]}:{cfg["password"]}'.encode())"""), _NONE },
		{ "AC.06", Source("""basic_value = base64.b16encode(f'{cfg["user"]}:{cfg["password"]}'.encode())"""), _NONE },
		{ "AC.07", Source("# Basic", """decoded = base64.b64decode(f"{header['user']}:{header['pw']}")"""), _NONE },
		{ "AC.08", Source("""basic_value = base64.b64encode(f"{table['a:b']}".encode()).decode()"""), _NONE },
		{ "AC.09", Source("""basic_value = base64.b64encode(t"{cfg['user']}:{cfg['password']}")"""), _NONE },
		{ "AC.10", Source([$"cred_value = {_ENCODER_PAIR_WITH_QUOTES}", .. Padding(), "# Basic"]), _NONE },
		{ "AC.11", Source("""basic_value = base64.b64encode(f"{table["a:b"]}".encode()).decode()"""), _NONE }
	};

	[Theory]
	[MemberData(nameof(ReadingAPlants))]
	[MemberData(nameof(ReadingAControls))]
	public void AColonPairInAnFStringWhoseFieldsHoldQuotesIsReadAndTheControlsKeepTheirOutcome(string row, string source, string expected)
	{
		AssertOutcome(row, source, expected);
	}

	[Fact]
	public void TheQuoteBearingPairIsReadForEitherQuoteKindOfTheLiteralAndOfTheFieldsAndTheFirstColonOutsideTheFieldsIsThePairs()
	{
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""basic_value = base64.b64encode(f"{cfg['user']}:{cfg['password']}".encode())""")));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""basic_value = base64.b64encode(f'{cfg['user']}:{cfg['password']}'.encode())""")));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""basic_value = base64.b64encode(f"{d['a:b']}:{d['p']}".encode())""")));
		Assert.Equal(_NONE, OutcomeOf(Source("""basic_value = base64.b64encode(f"{d['a:b']}{d['p']}".encode())""")));
	}

	[Fact]
	public void TheEdgesTheCatalogStatesAsUnreadStayUnreadInAQuoteBearingPair()
	{
		string[] unread =
		[
			"""basic_value = base64.b64encode(f"{cfg['user'] + suffix}:{cfg['p']}".encode())""",
			"""basic_value = base64.b64encode(f"{cfg['user']!r}:{cfg['p']}".encode())""",
			"""basic_value = base64.b64encode(f"{cfg['user']:>10}:{cfg['p']}".encode())""",
			"""basic_value = base64.b64encode(f"{cfg['user'] if flag else other}:{cfg['p']}".encode())""",
			"""basic_value = base64.b64encode(f"{cfg['a']['b']['c']['d']}:{cfg['p']}".encode())""",
			"""basic_value = base64.b64encode(f"{cfg[keys[names['u']]]}:{cfg['p']}".encode())""",
			"""basic_value = base64.b64encode(f'''{cfg['user']}:{cfg['p']}'''.encode())""",
			"""basic_value = base64.b64encode(f"{cfg['user']}\n:{cfg['p']}".encode())""",
			$"basic_value = base64.b64encode(f\"{{cfg['{new string(_LONG_KEY_CHARACTER, _LONG_QUOTED_KEY_CHARACTERS)}']}}:{{cfg['p']}}\".encode())"
		];

		Assert.All(unread, line => Assert.Equal(_NONE, OutcomeOf(Source(line))));
	}

	[Fact]
	public void AHalfOfTheQuoteBearingPairIsBoundedInCharactersAndTheCharactersOfItsFieldsCountInIt()
	{
		string field = $"{{{new string(_FILLER_CHARACTER, _FIELD_NAME_CHARACTERS)}}}";
		string fieldsAtTheBound = $"{string.Concat(Enumerable.Repeat(field, _FIELDS_IN_A_HALF_AT_THE_BOUND))}{new string(_FILLER_CHARACTER, _HALF_BOUND_IN_CHARACTERS - (_FIELDS_IN_A_HALF_AT_THE_BOUND * field.Length))}";
		string fieldsPastTheBound = string.Concat(Enumerable.Repeat(field, _FIELDS_IN_A_HALF_PAST_THE_BOUND));
		string plainAtTheBound = new(_FILLER_CHARACTER, _HALF_BOUND_IN_CHARACTERS);
		string plainPastTheBound = new(_FILLER_CHARACTER, _HALF_PAST_THE_BOUND_IN_CHARACTERS);
		string quoted = "{cfg['p']}";

		Assert.Equal(_HALF_BOUND_IN_CHARACTERS, fieldsAtTheBound.Length);
		Assert.True(fieldsPastTheBound.Length > _HALF_BOUND_IN_CHARACTERS);
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source(PairLine(plainAtTheBound, quoted))));
		Assert.Equal(_NONE, OutcomeOf(Source(PairLine(plainPastTheBound, quoted))));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source(PairLine(fieldsAtTheBound, quoted))));
		Assert.Equal(_NONE, OutcomeOf(Source(PairLine(fieldsPastTheBound, quoted))));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source(PairLine(quoted, plainAtTheBound))));
		Assert.Equal(_NONE, OutcomeOf(Source(PairLine(quoted, plainPastTheBound))));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source(PairLine(quoted, fieldsAtTheBound))));
		Assert.Equal(_NONE, OutcomeOf(Source(PairLine(quoted, fieldsPastTheBound))));
	}

	[Fact]
	public void ABraceBeforeTheLiteralNeverCountsAsAFieldOfItAndABraceInsideItDoes()
	{
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""payload = {"note": "a", "v": base64.b64encode(f"svc:{cfg['pw']}".encode())}  # Basic""")));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""payload = {"note": "a", "v": base64.b64encode(f"{cfg['u']}:{cfg['pw']}".encode())}  # Basic""")));
		Assert.Equal(_NONE, OutcomeOf(Source("""payload = {"note": "a", "v": base64.b64encode(f"{table['a:b']}".encode())}  # Basic""")));
	}

	[Fact]
	public void APairIsReadOnlyWhenAQuoteStandsInsideAReplacementFieldAndAPlainLiteralWithAQuoteElsewhereStaysUnread()
	{
		// A quote before the colon that stands outside every field: the earlier alternatives do not read it (a quote ends their text), and the quote-bearing alternative must not either.
		string[] unread =
		[
			"""basic_value = base64.b64encode(f'{user}"x":{secret}'.encode())  # Basic""",
			"""basic_value = base64.b64encode(f"{user}'x':{secret}".encode())  # Basic""",
			"""basic_value = base64.b64encode('{user}"x":{secret}'.encode())  # Basic"""
		];

		Assert.All(unread, line => Assert.Equal(_NONE, OutcomeOf(Source(line))));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""basic_value = base64.b64encode(f'{cfg["u"]}"x":{secret}'.encode())  # Basic""")));
	}

	[Fact]
	public void AQuoteBearingFieldOfTheMaximumLengthIsReadAndOneCharacterMoreIsNot()
	{
		string name = new(_FILLER_CHARACTER, _MAXIMUM_FIELD_NAME_CHARACTERS);
		string key = new(_FILLER_CHARACTER, _MAXIMUM_FIELD_KEY_CHARACTERS);

		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source(PairLine($"{{{name}['{key}']}}", "{cfg['p']}"))));
		Assert.Equal(_NONE, OutcomeOf(Source(PairLine($"{{{name}h['{key}']}}", "{cfg['p']}"))));
	}

	[Fact]
	public void AnInnerCallOfAnEncoderWithAQuotedArgumentInsideAFieldIsReadInsideAndOutsideAMapping()
	{
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""basic_value = base64.b64encode(f'{base64.b64encode('x')}:{cfg['p']}'.encode()).decode()  # Basic""")));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""basic_value = base64.b64encode(f"{base64.b64encode("x")}:{cfg["p"]}".encode()).decode()  # Basic""")));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""payload = {"v": base64.b64encode(f'{base64.b64encode('x')}:{cfg['p']}'.encode())}  # Basic""")));
	}

	[Fact]
	public void AnFStringHeldSchemeValueWithFieldsIsReadByItsLeadingWordWhateverTheFieldsHold()
	{
		Assert.Equal(_HELD_SCHEME_MEDIUM, OutcomeOf(Source("auth_value = f\"Basic {enc}\"", _CARRIER_MAPPING)));
		Assert.Equal(_HELD_SCHEME_MEDIUM, OutcomeOf(Source("auth_value = f\"Basic {cfg[\"k\"]}\"", _CARRIER_MAPPING)));
		Assert.Equal(_HELD_SCHEME_MEDIUM, OutcomeOf(Source("auth_value = f'Basic {cfg['k']}'", _CARRIER_MAPPING)));
		Assert.Equal(_HELD_SCHEME_MEDIUM, OutcomeOf(Source("auth_value: str = rf\"Basic {{literal}} {cfg[\"k\"]}\"", _CARRIER_MAPPING)));
		Assert.Equal(_NONE, OutcomeOf(Source("auth_value = f\"Bearer {cfg[\"k\"]}\"", _CARRIER_MAPPING)));
		Assert.Equal(_NONE, OutcomeOf(Source("auth_value = f'Token {cfg['k']}'", _CARRIER_MAPPING)));
	}

	[Fact]
	public void AnEncoderNameInsideAFieldIsNoStartOfAPairLiteralAndIsMatchedAsAWholeWordOnly()
	{
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""basic_value = base64.b64encode(f"{base64.b64encode(k)}:{cfg['p']}".encode())  # Basic""")));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""basic_value = base64.b64encode(f"{my_b64encode('x')}:{cfg['p']}".encode())  # Basic""")));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""payload = {"v": b64encode(f"{my_b64encode('x')}:{cfg['p']}".encode())}  # Basic""")));
	}

	[Fact]
	public void TheQuoteBearingPairIsReadOnlyForAPrefixThatContainsFAndNeverForALiteralWithoutF()
	{
		string[] read = ["f", "F", "rf", "fr", "Rf", "fR"];
		string[] unread = ["", "u", "r", "b", "rb", "Rb"];
		string fields = "\"{cfg['user']}:{cfg['password']}\"";

		Assert.All(read, prefix => Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source($"basic_value = base64.b64encode({prefix}{fields}.encode())  # Basic"))));
		Assert.All(unread, prefix => Assert.Equal(_NONE, OutcomeOf(Source($"basic_value = base64.b64encode({prefix}{fields}.encode())  # Basic"))));
		Assert.Equal(_NONE, OutcomeOf(Source("""payload = {"v": b64encode("{cfg['u']}:{cfg['pw']}")}  # Basic""")));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(Source("""payload = {"v": b64encode(f"{cfg['u']}:{cfg['pw']}")}  # Basic""")));
	}

	[Fact]
	public void TheQuoteBearingPairCarriesNoCredentialPositionAndStaysOneFindingAtTheEncoder()
	{
		RecognizedConstruct construct = Assert.Single(new PythonRecognitionHarness().Recognize(Source($"basic_value = {_ENCODER_PAIR_WITH_QUOTES}")));

		Assert.Equal("PY-007", construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	[Fact]
	public void TheSameQuoteFieldIsAnInteriorOfCodeInsideOneLiteralSoTheScannerReadsTheNestedFormOfPython312WithoutAnyChangeOfTheScanner()
	{
		const string line = """basic_value = base64.b64encode(f"{cfg["user"]}:{cfg["password"]}".encode()).decode()""";
		FileRecognitionContext context = new PythonRecognitionHarness().ContextFor(Source(line));

		Assert.Equal(LexicalZone.StringBody, context.Mask.ZoneAt(line.IndexOf("}:{", StringComparison.Ordinal) + _ONE_CHARACTER));
		Assert.Equal(LexicalZone.Code, context.Mask.ZoneAt(line.IndexOf("cfg[\"password", StringComparison.Ordinal)));
		Assert.Equal(LexicalZone.StringBody, context.Mask.ZoneAt(line.IndexOf("password", StringComparison.Ordinal)));
		Assert.Equal(LexicalZone.Code, context.Mask.ZoneAt(line.IndexOf(".encode", StringComparison.Ordinal)));
	}

	// ------------------------------------------------------------------------------------------
	// Reading B
	// ------------------------------------------------------------------------------------------

	public static TheoryData<string, string, string> ReadingBPlants() => new()
	{
		{ "B.01", HeldSource("cred_value = base64.b64encode(", "\tcredentials.encode()", ").decode()"), _PAIR_MEDIUM },
		{ "B.02", HeldSource("cred_value = base64.b64encode(", "\tcredentials", ")"), _PAIR_MEDIUM },
		{ "B.03", Source("class Sender:", "\tdef __init__(self, service_user, service_password):", "\t\tself.credentials = f\"{service_user}:{service_password}\"", "", "\tdef header(self):", $"\t\t{_BASIC_COMMENT}", "\t\tcred_value = base64.b64encode(", "\t\t\tself.credentials,", "\t\t)"), _PAIR_MEDIUM },
		{ "B.04", HeldSource("cred_value = base64.b64encode(", "\tcredentials.encode(", "\t\t\"utf-8\"", "\t)", ")"), _PAIR_MEDIUM },
		{ "B.05", HeldSource("cred_value = base64.b64encode(", "\t\t\t\tcredentials.encode()", "\t\t)"), _PAIR_MEDIUM },
		{ "B.06", Source("import base64 as codec", _HELD_CREDENTIALS, _BASIC_COMMENT, "cred_value = codec.standard_b64encode(", "\tcredentials", ")"), _PAIR_MEDIUM }
	};

	public static TheoryData<string, string, string> ReadingBControls() => new()
	{
		{ "BC.01", Source(_HELD_CREDENTIALS, _BASIC_COMMENT, "cred_value = base64.b64encode(credentials.encode()).decode()"), _PAIR_MEDIUM },
		{ "BC.02", Source(_HELD_CREDENTIALS, "# Authorization value", "cred_value = base64.b64encode(", "\tcredentials.encode()", ").decode()"), _NONE },
		{ "BC.03", Source(_HELD_CREDENTIALS, "cred_value = base64.b64encode(", "\tcredentials.encode()", ").decode()"), _NONE },
		{ "BC.04", HeldSource("cred_value = base64.urlsafe_b64encode(", "\tcredentials", ")"), _NONE },
		{ "BC.05", HeldSource("decoded = base64.b64decode(", "\tcredentials", ")"), _NONE },
		{ "BC.06", HeldSource("cred_value = base64.b64encode(", "\t\"static-text\"", ")"), _NONE },
		{ "BC.07", HeldSource("cred_value = base64.b64encode(", "\tjoin_parts(prefix, suffix)", ")"), _NONE },
		{ "BC.08", Source([_HELD_CREDENTIALS, "cred_value = base64.b64encode(", "\tcredentials.encode()", ").decode()", .. Padding(), "# Basic"]), _NONE }
	};

	[Theory]
	[MemberData(nameof(ReadingBPlants))]
	[MemberData(nameof(ReadingBControls))]
	public void AHeldNameIsReadWhenTheEncoderCallSpansLinesAndTheControlsKeepTheirOutcome(string row, string source, string expected)
	{
		AssertOutcome(row, source, expected);
	}

	[Fact]
	public void ASeamOfFortyWhitespaceCharactersWithItsLineBreakIsReadAndOneOfFortyOneIsNotAndACommentInASeamIsNeverRead()
	{
		string atTheBound = new(_SEAM_BLANK, _SEAM_BLANKS_AT_THE_BOUND);
		string pastTheBound = new(_SEAM_BLANK, _SEAM_BLANKS_PAST_THE_BOUND);

		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(HeldSource("cred_value = base64.b64encode(", $"{atTheBound}credentials", ")")));
		Assert.Equal(_NONE, OutcomeOf(HeldSource("cred_value = base64.b64encode(", $"{pastTheBound}credentials", ")")));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(HeldSource("cred_value = base64.b64encode(", $"credentials{atTheBound}", ")")));
		Assert.Equal(_NONE, OutcomeOf(HeldSource("cred_value = base64.b64encode(", $"credentials{pastTheBound}", ")")));
		Assert.Equal(_NONE, OutcomeOf(HeldSource("cred_value = base64.b64encode(", "\t# a note", "\tcredentials", ")")));
		Assert.Equal(_NONE, OutcomeOf(HeldSource("cred_value = base64.b64encode(", "\tcredentials  # a note", ")")));
		Assert.Equal(_NONE, OutcomeOf(HeldSource("cred_value = base64.b64encode(", "\tcredentials.encode(", "\t\t# a note", "\t)", ")")));
	}

	[Fact]
	public void TheSeamAfterTheInnerParenthesisOfTheBareEncodeFormAndAroundTheOuterCloseAreRead()
	{
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(HeldSource("cred_value = base64.b64encode(", "\tencode(", "\t\tcredentials", "\t)", ")")));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(HeldSource("cred_value = base64.b64encode(", "\tcredentials.encode()", "\t,", ")")));
		Assert.Equal(_PAIR_MEDIUM, OutcomeOf(HeldSource("cred_value = base64.b64encode(", "\tcredentials.encode(\"utf-8\")", ")")));
	}

	[Fact]
	public void TheHeldNameStaysWithoutACredentialPositionAndTheUseWordMustBeBasicAlone()
	{
		RecognizedConstruct construct = Assert.Single(new PythonRecognitionHarness().Recognize(HeldSource("cred_value = base64.b64encode(", "\tcredentials", ")")));

		Assert.False(construct.LiteralCredentialSignal);
		Assert.Equal(_NONE, OutcomeOf(Source(_HELD_CREDENTIALS, "# Authorization", "cred_value = base64.b64encode(", "\tcredentials", ")")));
	}

	// ------------------------------------------------------------------------------------------
	// Reading C
	// ------------------------------------------------------------------------------------------

	public static TheoryData<string, string, string> ReadingCPlants() => new()
	{
		{ "C.01", Source("""auth_value = "Basic " + token_text""", _CARRIER_MAPPING), _HELD_SCHEME_MEDIUM },
		{ "C.02", Source("auth_value = f\"Basic {token_text}\"", """session.headers["Authorization"] = auth_value"""), _HELD_SCHEME_MEDIUM },
		{ "C.03", Source("""auth_value = "Basic %s" % token_text""", """request.add_header("Authorization", auth_value)"""), _HELD_SCHEME_MEDIUM },
		{ "C.04", Source("""auth_value = "Basic {}".format(token_text)""", "client = http.client.HTTPSConnection(host)", """client.request("GET", path, headers=dict(Authorization=auth_value))"""), _HELD_SCHEME_MEDIUM },
		{ "C.05", Source(_TOKEN_DECLARATION, _CARRIER_MAPPING), _HELD_SCHEME_MEDIUM },
		{ "C.06", Source("""auth_value = "Basic " + token_store.read_token()""", """response = requests.post(url, headers={"Authorization": auth_value})"""), _HELD_SCHEME_MEDIUM },
		{ "C.07", Source("""auth_value = "Basic" + token_text""", """session.headers.update({"Authorization": auth_value})"""), _HELD_SCHEME_MEDIUM },
		{ "C.08", Source("""AUTH_VALUE = u"Basic " + settings_holder.token""", """response = requests.get(url, headers={"Authorization": AUTH_VALUE})"""), _HELD_SCHEME_MEDIUM },
		{ "C.09", Source("""auth_value: str = "Basic " + token_text""", _CARRIER_MAPPING), _HELD_SCHEME_MEDIUM },
		{ "C.10", Source("def build(b64_text, url):", "\tauth_value = \"Basic \" + b64_text.strip()", $"\t{_CARRIER_MAPPING}"), _HELD_SCHEME_MEDIUM },
		{ "C.11", Source("""auth_value = "Basic " + base64.b64encode(token_bytes.strip()).decode("ascii")""", _CARRIER_MAPPING), _HELD_SCHEME_MEDIUM }
	};

	public static TheoryData<string, string, string> ReadingCControls() => new()
	{
		{ "CC.01", Source("auth_value = \"Basic\"", _CARRIER_MAPPING), _HELD_SCHEME_MEDIUM },
		{ "CC.02", Source("""auth_value = "Bearer " + token_text""", _CARRIER_MAPPING), _NONE },
		{ "CC.03", Source("""auth_value = "Token " + token_text""", _CARRIER_MAPPING), _NONE },
		{ "CC.04", Source("""auth_value = "BasicAuth " + token_text""", _CARRIER_MAPPING), _NONE },
		{ "CC.05", Source("""auth_value = "Basic " + token_text"""), _NONE },
		{ "CC.06", Source(_CARRIER_MAPPING, """auth_value = "Basic " + token_text"""), _NONE },
		{ "CC.07", Source("""auth_value = "Basic " + token_text""", """response = requests.get(url, headers={"Authorization": config_holder.auth_value})"""), _NONE },
		{ "CC.08", Source("""auth_value = "Basic " + token_text""", """response = requests.get(url, headers={"Proxy-Authorization": auth_value})"""), _NONE },
		{ "CC.09", Source("""auth_value = "Basic " + token_text""", """response = requests.get(url, headers={"X-Api-Token": auth_value})"""), _NONE },
		{ "CC.10", Source("""challenge_value = 'Basic realm="service"'""", """response.headers["WWW-Authenticate"] = challenge_value"""), _NONE },
		{ "CC.11", Source($"response = requests.get(url, headers={{\"Authorization\": \"Basic \" + {_ENCODER_PAIR_WITH_PLAIN_FIELDS}}})"), "PY-003@High" },
		{ "CC.12", Source($"encoded = {_ENCODER_PAIR_WITH_PLAIN_FIELDS}", """response = requests.get(url, headers={"Authorization": "Basic " + encoded})"""), "PY-003@High,PY-007@Medium" },
		{ "CC.13", Source($"""auth_value = "Basic " + {_ENCODER_PAIR_WITH_PLAIN_FIELDS}""", _CARRIER_MAPPING), "PY-007@Medium,PY-013@Medium" },
		{ "CC.14", Source("""auth_value = "Basic " + base64.b64encode(f"{cfg['user']}:{cfg['password']}".encode()).decode()""", _CARRIER_MAPPING), "PY-007@Medium,PY-013@Medium" },
		{ "CC.15", Source("""self.auth_value = "Basic " + token_text""", """self.client.get(url, headers={"Authorization": self.auth_value})"""), _NONE }
	};

	[Theory]
	[MemberData(nameof(ReadingCPlants))]
	[MemberData(nameof(ReadingCControls))]
	public void ACompoundHeldSchemeValueIsReadAtTheCarrierAndTheControlsKeepTheirOutcome(string row, string source, string expected)
	{
		AssertOutcome(row, source, expected);
	}

	[Fact]
	public void TheHeldLiteralWithATokenIsOneFindingAtTheCarrierThatCarriesTheLiteralCredentialSignalAndNoFindingStandsAtTheDeclaration()
	{
		string source = Source(_TOKEN_DECLARATION, _CARRIER_MAPPING);
		RecognizedConstruct construct = Assert.Single(new PythonRecognitionHarness().Recognize(source));

		Assert.Equal("PY-013", construct.PatternClassIdentifier);
		Assert.True(construct.LiteralCredentialSignal);
		Assert.True(construct.AnchorOffset >= source.IndexOf("response", StringComparison.Ordinal), "the finding stands at the carrier, after the declaration");
	}

	[Fact]
	public void TheCredentialPositionOfTheHeldLiteralIsTheTokenOfTheDeclaredLiteralAndNoOtherFormHasOne()
	{
		Regex expression = PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass["PY-013"].Expression;
		string withToken = Source(_TOKEN_DECLARATION, _CARRIER_MAPPING);
		Group credential = expression.Match(withToken).Groups["credential"];

		Assert.True(credential.Success);
		Assert.Equal("dXNlcjpwYXNz", credential.Value);
		Assert.Equal(withToken.IndexOf("dXNlcjpwYXNz", StringComparison.Ordinal), credential.Index);

		string[] withoutToken =
		[
			Source("""auth_value = "Basic " + token_text""", _CARRIER_MAPPING),
			Source("auth_value = f\"Basic {token_text}\"", _CARRIER_MAPPING),
			Source("""auth_value = "Basic %s" % token_text""", _CARRIER_MAPPING),
			Source("""auth_value = "Basic {}".format(token_text)""", _CARRIER_MAPPING),
			Source("""auth_value = "Basic dXNlcjpwYXNz" + token_text""", _CARRIER_MAPPING),
			Source("auth_value = \"Basic\"", _CARRIER_MAPPING),
			Source("auth_value = \"Basic <token>\"", _CARRIER_MAPPING)
		];

		Assert.All(withoutToken, source => Assert.False(expression.Match(source).Groups["credential"].Success, source));
	}

	[Fact]
	public void TheHeldLiteralCredentialSignalFollowsTheDeclarationAcrossLinesAndAnnotationsAndIsAbsentForAPlaceholderToken()
	{
		PythonRecognitionHarness harness = new();

		Assert.True(Assert.Single(harness.Recognize(Source("auth_value: str = \"Basic dXNlcjpwYXNz\"", "", "def send(url):", $"\t{_CARRIER_MAPPING}"))).LiteralCredentialSignal);
		Assert.True(Assert.Single(harness.Recognize(Source("""auth_value = 'Basic dXNlcjpwYXNz'""", "x = 1  # a note", _CARRIER_MAPPING))).LiteralCredentialSignal);
		Assert.False(Assert.Single(harness.Recognize(Source("auth_value = \"Basic changeme\"", _CARRIER_MAPPING))).LiteralCredentialSignal);
		Assert.False(Assert.Single(harness.Recognize(Source("auth_value = \"Basic abc\"", _CARRIER_MAPPING))).LiteralCredentialSignal);
	}

	[Fact]
	public void TheLeadingWordIsTheVerdictAndBasicConfirmsAtMediumWithoutEverPromoting()
	{
		(string Declaration, string Expected)[] cases =
		[
			("""auth_value = "basic " + token_text""", _HELD_SCHEME_MEDIUM),
			("""auth_value = "BASIC %s" % token_text""", _HELD_SCHEME_MEDIUM),
			("""auth_value = "Basic.v1 " + token_text""", _HELD_SCHEME_MEDIUM),
			("""auth_value = "Digest " + token_text""", _NONE),
			("auth_value = f\"Bearer {token_text}\"", _NONE),
			("""auth_value = "Bearer %s" % token_text""", _NONE),
			("""auth_value = "Token {}".format(token_text)""", _NONE),
			("auth_value = \"Bearer dXNlcjpwYXNz\"", _NONE)
		];

		Assert.All(cases, entry => Assert.Equal(entry.Expected, OutcomeOf(Source(entry.Declaration, _CARRIER_MAPPING))));
	}

	[Fact]
	public void ADeclarationWhoseValueContinuesPastItsOperandOrIsNotOnOneLineIsNotReadAndTheClassStaysSilent()
	{
		string[] unread =
		[
			"""auth_value = "Basic " + token_text + suffix_text""",
			"auth_value = \"Basic \" + token_text if flag else \"x\"",
			"""auth_value = "Basic " + token_text or other_text""",
			"""auth_value = "Basic " + first(a)(b)(c)(d)""",
			"""auth_value = "Basic " + b64encode(x(y(z)), token_text).decode()""",
			"auth_value = \"Basic \" + \\\n\ttoken_text",
			"auth_value = \"Basic \" + (\n\ttoken_text)",
			"auth_value = '''Basic ''' + token_text",
			"auth_value = b\"Basic \" + token_text",
			"auth_value = t\"Basic {token_text}\"",
			"auth_value = \"Basic \" + \"abcd\""
		];

		Assert.All(unread, declaration => Assert.Equal(_NONE, OutcomeOf(Source(declaration, _CARRIER_MAPPING))));
	}

	[Fact]
	public void ACallGroupOfTheOperandIsBoundedAtOneHundredAndTwentyUnitsAndANestedGroupIsOneUnit()
	{
		string atTheBound = new(_GROUP_UNIT_CHARACTER, _GROUP_BOUND_IN_UNITS);
		string pastTheBound = new(_GROUP_UNIT_CHARACTER, _GROUP_BOUND_IN_UNITS + _ONE_UNIT);
		string nested = new(_NESTED_UNIT_CHARACTER, _NESTED_GROUP_BOUND_IN_CHARACTERS);
		string unitsBeforeTheNestedGroup = new(_GROUP_UNIT_CHARACTER, _GROUP_BOUND_IN_UNITS - _ONE_UNIT);
		string pastTheBoundBeforeTheNestedGroup = new(_GROUP_UNIT_CHARACTER, _GROUP_BOUND_IN_UNITS);

		Assert.Equal(_HELD_SCHEME_MEDIUM, OutcomeOf(Source($"auth_value = \"Basic \" + build({atTheBound})", _CARRIER_MAPPING)));
		Assert.Equal(_NONE, OutcomeOf(Source($"auth_value = \"Basic \" + build({pastTheBound})", _CARRIER_MAPPING)));
		Assert.Equal(_HELD_SCHEME_MEDIUM, OutcomeOf(Source($"auth_value = \"Basic \" + build({unitsBeforeTheNestedGroup}({nested}))", _CARRIER_MAPPING)));
		Assert.Equal(_NONE, OutcomeOf(Source($"auth_value = \"Basic \" + build({pastTheBoundBeforeTheNestedGroup}({nested}))", _CARRIER_MAPPING)));
	}

	[Fact]
	public void AnOperandOfThreeCallGroupsAnAttributeChainAndANestedCallIsRead()
	{
		Assert.Equal(_HELD_SCHEME_MEDIUM, OutcomeOf(Source("""auth_value = "Basic " + vault.fetch(a).strip().decode("utf-8")""", _CARRIER_MAPPING)));
		Assert.Equal(_HELD_SCHEME_MEDIUM, OutcomeOf(Source("""auth_value = "Basic " + store.read().value""", _CARRIER_MAPPING)));
		Assert.Equal(_HELD_SCHEME_MEDIUM, OutcomeOf(Source("""auth_value = "Basic " + b64encode(f(pair, 1)).decode()""", _CARRIER_MAPPING)));
	}

	[Fact]
	public void ANameDeclaredTwiceIsIndeterminateAndTheClassStaysSilentWhateverTheFormsOfTheDeclarations()
	{
		Assert.Equal(_NONE, OutcomeOf(Source("""auth_value = "Basic " + token_text""", """auth_value = "Bearer " + other_text""", _CARRIER_MAPPING)));
		Assert.Equal(_NONE, OutcomeOf(Source("""auth_value = "Bearer " + token_text""", """auth_value = "Basic " + other_text""", _CARRIER_MAPPING)));
	}

	[Fact]
	public void ThePlainLiteralDeclarationsReadBeforeKeepTheirValueAndTheEnvironmentFormsAreUnchanged()
	{
		Regex declaration = PythonShippedAnalyzer.Instance.LexicalProfile.ConstantDeclarationExpression;

		Assert.Equal("Basic", declaration.Match("scheme = \"Basic\"").Groups["value"].Value);
		Assert.Equal(" basic ", declaration.Match("scheme = \" basic \"").Groups["value"].Value);
		Assert.Equal(string.Empty, declaration.Match("scheme = \"\"").Groups["value"].Value);
		Assert.Equal("Basic", declaration.Match("scheme = \"Basic dXNlcjpwYXNz\"").Groups["value"].Value);
		Assert.Equal("Basic", declaration.Match("scheme = \"Basic \" + token_text").Groups["value"].Value);
		Assert.Equal("Basic", declaration.Match("scheme: str = f\"Basic {token_text}\"").Groups["value"].Value);
		Assert.Equal("Basic", declaration.Match("mode = os.environ.get(\"AUTH_VALUE\", \"Basic dGVzdDp0ZXN0\")").Groups["fallbackWord"].Value);
		Assert.DoesNotMatch(declaration, "scheme = \"Basic \" + token_text + suffix_text");
		Assert.DoesNotMatch(declaration, "if scheme == \"Basic\":");
	}

	// ------------------------------------------------------------------------------------------
	// Files that a demotion signal lowers
	// ------------------------------------------------------------------------------------------

	public static TheoryData<string, string, string, string, bool> DemotedAndTestOnlyPlants() => new()
	{
		{ "DT.01", "examples/client.py", Source($"basic_value = {_ENCODER_PAIR_WITH_QUOTES}"), _PAIR_LOW, false },
		{ "DT.02", "vendor/client.py", Source("""basic_value = b64encode(f"{os.getenv('SERVICE_USER')}:{os.getenv('SERVICE_PASSWORD')}".encode()).decode()"""), _PAIR_LOW, false },
		{ "DT.03", "demos/client.py", HeldSource("cred_value = base64.b64encode(", "\tcredentials.encode()", ").decode()"), _PAIR_LOW, false },
		{ "DT.04", "mock_client.py", HeldSource("cred_value = base64.b64encode(", "\tcredentials", ")"), _PAIR_LOW, false },
		{ "DT.05", "samples/client.py", Source("""auth_value = "Basic " + token_text""", _CARRIER_MAPPING), _HELD_SCHEME_LOW, false },
		{ "DT.06", "site-packages/client.py", Source("auth_value = f\"Basic {token_text}\"", """session.headers["Authorization"] = auth_value"""), _HELD_SCHEME_LOW, false },
		{ "DT.07", "test_sender.py", Source("""auth_value = "Basic %s" % token_text""", """request.add_header("Authorization", auth_value)"""), _HELD_SCHEME_MEDIUM, true }
	};

	[Theory]
	[MemberData(nameof(DemotedAndTestOnlyPlants))]
	public void APlantInADemotedFileReadsLowAndOneInATestOnlyFileStaysMediumWithTheTestCodeMark(string row, string relativePath, string source, string expected, bool expectedTestCode)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source, relativePath);

		Assert.Equal(expected, Describe(constructs));
		Assert.True(expectedTestCode == constructs.All(construct => construct.TestCode), row);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static void AssertOutcome(string row, string source, string expected)
	{
		string actual = OutcomeOf(source);

		Assert.True(expected == actual, $"{row}: expected [{expected}] and got [{actual}] on [{source}]");
	}

	private static string OutcomeOf(string source)
	{
		return Describe(new PythonRecognitionHarness().Recognize(source));
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return constructs.IsEmpty
			? _NONE
			: string.Join(",", constructs.Select(construct => $"{construct.PatternClassIdentifier}@{construct.Level}").Order(StringComparer.Ordinal));
	}

	private static string PairLine(string left, string right)
	{
		return $"basic_value = base64.b64encode(f\"{left}:{right}\".encode())";
	}

	private static string Source(params string[] lines)
	{
		return $"{string.Join("\n", lines)}\n";
	}

	/// <summary>The held-name cases: the pair held in a name built in another statement, the use word `Basic` in a comment above the encoder statement.</summary>
	private static string HeldSource(params string[] encoderLines)
	{
		return Source([_HELD_CREDENTIALS, _BASIC_COMMENT, .. encoderLines]);
	}

	/// <summary>Comment lines of padding that hold neither use word, together longer than the 300-character statement window.</summary>
	private static string[] Padding()
	{
		return [.. Enumerable.Repeat(_PADDING_LINE, _PADDING_LINE_COUNT)];
	}
}
