using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// Part A of Python says, for the classes whose rules are subtle, what the tool reads and what it does not, and a reader acts on those sentences. Each sentence below is a
/// claim, a table row: the section whose prose makes it, a phrase copied from that prose, an invented input, and what the analyzer must do with it (read at a level, or not read, by the
/// named class). Every phrase must still stand in its section, so a rewritten sentence is re-checked. Every input is invented from the prose, with placeholder names. The limits of the
/// reading (a format specification, an abandoned scan, a docstring and the drop order of the findings cap) are checked as well, against the analyzer and against the cap's own constants.
/// </summary>
public sealed class PythonPartAProseClaimTests
{
	private const string _HELPER_CLASS = "PY-001";
	private const string _NAMED_PAIR_CLASS = "PY-002";
	private const string _HEADER_CLASS = "PY-003";
	private const string _HANDLER_CLASS = "PY-004";
	private const string _MAKE_HEADERS_CLASS = "PY-005";
	private const string _UNNAMED_PAIR_CLASS = "PY-006";
	private const string _ENCODER_CLASS = "PY-007";
	private const string _HELD_HEADER_CLASS = "PY-013";
	private const string _REQUEST_URL_CLASS = "PY-011";
	private const string _HELD_URL_CLASS = "PY-012";
	private const string _MAPPING_CLASS = "PY-014";
	private const string _HELD_HEADER_REQUEST = "response = requests.get(url, headers={\"Authorization\": scheme_value})\n";
	private const string _PAIR_REQUEST = "response = requests.get(url, auth=(\"user\", \"secret\"))\n";
	private const string _SPACE_SIGN_FORMAT = "label = f\"{count: #x}\"\n";
	private const string _DOCSTRING_SOURCE = "def fetch_items(url):\n\t\"\"\"Fetch the items.\n\n\tThe legacy client sent the request as requests.get(url, auth=(\"user\", \"secret\")).\n\t\"\"\"\n\treturn None\n";

	private sealed record ProseClaim(string Section, string Phrase, string PatternClass, bool IsRead, ConfidenceLevel Level, string Source);

	private static readonly ImmutableArray<ProseClaim> _claims =
	[
		// PY-001: the forms the helper class does not read
		new(_HELPER_CLASS, "`BasicAuth.decode(`", _HELPER_CLASS, false, ConfidenceLevel.High, "parsed = BasicAuth.decode(header_value)\n"),
		new(_HELPER_CLASS, "a subclass definition (`class X(HTTPBasicAuth):`)", _HELPER_CLASS, false, ConfidenceLevel.High, "class CustomAuth(HTTPBasicAuth):\n\tpass\n"),
		new(_HELPER_CLASS, "the different words `HTTPProxyAuth(`", _HELPER_CLASS, false, ConfidenceLevel.High, "auth = HTTPProxyAuth(\"user\", \"secret\")\n"),
		new(_HELPER_CLASS, "A construction that is the value of the `proxy_auth` keyword is a proxy credential and is not read.", _HELPER_CLASS, false, ConfidenceLevel.High, "session = aiohttp.ClientSession(proxy_auth=aiohttp.BasicAuth(\"user\", \"secret\"))\n"),
		// PY-002: a named client reads a pair at High, and the unnamed forms are another class
		new(_NAMED_PAIR_CLASS, "a module alias or a name imported with `from` makes the callee unnamed", _NAMED_PAIR_CLASS, false, ConfidenceLevel.High, "response = rq.get(url, auth=(\"user\", \"secret\"))\n"),
		new(_NAMED_PAIR_CLASS, "a list, a name, a one-element tuple and a three-element tuple are not read", _NAMED_PAIR_CLASS, false, ConfidenceLevel.High, "response = requests.get(url, auth=[\"user\", \"secret\"])\n"),
		new(_NAMED_PAIR_CLASS, "a list, a name, a one-element tuple and a three-element tuple are not read", _NAMED_PAIR_CLASS, false, ConfidenceLevel.High, "response = requests.get(url, auth=(\"a\", \"b\", \"c\"))\n"),
		new(_NAMED_PAIR_CLASS, "`aiohttp` is deliberately not in the list", _NAMED_PAIR_CLASS, false, ConfidenceLevel.High, "response = aiohttp.request(\"GET\", url, auth=(\"user\", \"secret\"))\n"),
		new(_NAMED_PAIR_CLASS, "a call whose text holds the word `grant_type` within the call", _NAMED_PAIR_CLASS, false, ConfidenceLevel.High, "response = requests.post(url, data={\"grant_type\": \"client_credentials\"}, auth=(\"client\", \"secret\"))\n"),
		// PY-006: the unnamed forms, and the forms it does not read
		new(_UNNAMED_PAIR_CLASS, "a call through a module alias", _UNNAMED_PAIR_CLASS, true, ConfidenceLevel.Medium, "response = rq.get(url, auth=(\"user\", \"secret\"))\n"),
		new(_UNNAMED_PAIR_CLASS, "an `aiohttp` call with a tuple", _UNNAMED_PAIR_CLASS, true, ConfidenceLevel.Medium, "response = aiohttp.request(\"GET\", url, auth=(\"user\", \"secret\"))\n"),
		new(_UNNAMED_PAIR_CLASS, "a name that merely ends in `auth` (`proxy_auth`)", _UNNAMED_PAIR_CLASS, false, ConfidenceLevel.Medium, "proxy_auth = (\"user\", \"secret\")\n"),
		new(_UNNAMED_PAIR_CLASS, "a pair on a line that holds the word `grant_type`", _UNNAMED_PAIR_CLASS, false, ConfidenceLevel.Medium, "response = session.post(url, data={\"grant_type\": \"client_credentials\"}, auth=(\"client\", \"secret\"))\n"),
		// PY-003: the header carrier, the values it reads and the values it does not
		new(_HEADER_CLASS, "or the value is a call of `encode_basic_auth(` of `aiohttp`", _HEADER_CLASS, true, ConfidenceLevel.High, "session = aiohttp.ClientSession(headers={\"Authorization\": encode_basic_auth(user_name, secret)})\n"),
		new(_HEADER_CLASS, "a leading word other than `Basic` (`Bearer`, `Token`, `Digest`)", _HEADER_CLASS, false, ConfidenceLevel.High, "response = requests.get(url, headers={\"Authorization\": \"Bearer \" + access_token})\n"),
		new(_HEADER_CLASS, "the names `Proxy-Authorization` and `X-Authorization`", _HEADER_CLASS, false, ConfidenceLevel.High, "response = requests.get(url, headers={\"Proxy-Authorization\": \"Basic \" + encoded})\n"),
		new(_HEADER_CLASS, "a `t`-string value", _HEADER_CLASS, false, ConfidenceLevel.High, "response = requests.get(url, headers={\"Authorization\": t\"Basic {encoded}\"})\n"),
		// PY-004 and PY-005: the proxy forms are other words
		new(_HANDLER_CLASS, "`ProxyBasicAuthHandler(`, which is a proxy credential", _HANDLER_CLASS, false, ConfidenceLevel.High, "handler = urllib.request.ProxyBasicAuthHandler(password_manager)\n"),
		new(_MAKE_HEADERS_CLASS, "the keyword `proxy_basic_auth`, which builds a proxy header", _MAKE_HEADERS_CLASS, false, ConfidenceLevel.High, "headers = urllib3.util.make_headers(proxy_basic_auth=\"user:secret\")\n"),
		// PY-007: the pair, its use word, the alphabets it refuses and the two amended readings
		new(_ENCODER_CLASS, "Without that word the encoder is a signature, a nonce or a cache key and is not read.", _ENCODER_CLASS, false, ConfidenceLevel.Medium, "digest = base64.b64encode(f\"{user_name}:{secret_value}\".encode())\n"),
		new(_ENCODER_CLASS, "Not read: `urlsafe_b64encode`", _ENCODER_CLASS, false, ConfidenceLevel.Medium, "token = base64.urlsafe_b64encode(f\"{user_name}:{secret_value}\".encode())\nheaders = {\"Authorization\": \"Basic \" + token}\n"),
		new(_ENCODER_CLASS, "(`f'{cfg[\"user\"]}:{cfg[\"password\"]}'`", _ENCODER_CLASS, true, ConfidenceLevel.Medium, "token = base64.b64encode(f'{cfg[\"user\"]}:{cfg[\"password\"]}'.encode()).decode()\nheaders = {\"Authorization\": \"Basic \" + token}\n"),
		new(_ENCODER_CLASS, "`f\"{os.getenv('U')}:{os.getenv('P')}\"`", _ENCODER_CLASS, true, ConfidenceLevel.Medium, "token = base64.b64encode(f\"{os.getenv('U')}:{os.getenv('P')}\".encode()).decode()\nheaders = {\"Authorization\": \"Basic \" + token}\n"),
		new(_ENCODER_CLASS, "a held name is read when the encoder call is written over several lines", _ENCODER_CLASS, true, ConfidenceLevel.Medium, "token = base64.b64encode(\n\tcreds.encode()\n).decode()\nheader_value = \"Basic \" + token\n"),
		// PY-013: the forms of a held value that are read, and the forms that stay silent
		new(_HELD_HEADER_CLASS, "(`\"Basic \" + encoded`", _HELD_HEADER_CLASS, true, ConfidenceLevel.Medium, $"scheme_value = \"Basic \" + encoded\n{_HELD_HEADER_REQUEST}"),
		new(_HELD_HEADER_CLASS, "an `f`-string that begins with the word (`f\"Basic {encoded}\"`)", _HELD_HEADER_CLASS, true, ConfidenceLevel.Medium, $"scheme_value = f\"Basic {{encoded}}\"\n{_HELD_HEADER_REQUEST}"),
		new(_HELD_HEADER_CLASS, "the literal and `%` and an operand", _HELD_HEADER_CLASS, true, ConfidenceLevel.Medium, $"scheme_value = \"Basic %s\" % encoded\n{_HELD_HEADER_REQUEST}"),
		new(_HELD_HEADER_CLASS, "the literal and `.format(` with its arguments", _HELD_HEADER_CLASS, true, ConfidenceLevel.Medium, $"scheme_value = \"Basic {{}}\".format(encoded)\n{_HELD_HEADER_REQUEST}"),
		new(_HELD_HEADER_CLASS, "a declaration that continues past the operand (a second `+`", _HELD_HEADER_CLASS, false, ConfidenceLevel.Medium, $"scheme_value = \"Basic \" + first_part + second_part\n{_HELD_HEADER_REQUEST}"),
		new(_HELD_HEADER_CLASS, "A value that begins with another word (`Bearer`, `Token`, `Digest`) is the migrated form and is not reported", _HELD_HEADER_CLASS, false, ConfidenceLevel.Medium, $"scheme_value = \"Bearer \" + access_token\n{_HELD_HEADER_REQUEST}"),
		new(_HELD_HEADER_CLASS, "A declaration that no header uses gives no finding", _HELD_HEADER_CLASS, false, ConfidenceLevel.Medium, "scheme_value = \"Basic dXNlcjpzZWNyZXQ=\"\n"),
		// PY-011 and PY-012: the schemes, the nesting and the proxy refusal
		new(_REQUEST_URL_CLASS, "every other scheme", _REQUEST_URL_CLASS, false, ConfidenceLevel.Medium, "response = requests.get(\"ftp://user:secret@example.test/items\")\n"),
		new(_REQUEST_URL_CLASS, "a literal inside a list, a tuple or a mapping given to the call", _REQUEST_URL_CLASS, false, ConfidenceLevel.Medium, "response = requests.get(url, proxies={\"https\": \"https://user:secret@proxy.example:3128\"})\n"),
		new(_HELD_URL_CLASS, "an attribute target (`self.url = \"...\"`)", _HELD_URL_CLASS, false, ConfidenceLevel.Medium, "self.url = \"https://user:secret@example.test/\"\n"),
		new(_HELD_URL_CLASS, "a name or keyword that contains `proxy` or `proxies`", _HELD_URL_CLASS, false, ConfidenceLevel.Medium, "proxy_url = \"http://user:secret@proxy.example:3128\"\n"),
		// PY-014: always Low, and a Basic encoder call is another class
		new(_MAPPING_CLASS, "**Low**, always, and always for manual review", _MAPPING_CLASS, true, ConfidenceLevel.Low, "def build_headers():\n\treturn {\"Authorization\": \"Basic dXNlcjpzZWNyZXQ=\"}\n"),
		new(_MAPPING_CLASS, "A value that is an `encode_basic_auth(` call is not read by this class.", _MAPPING_CLASS, false, ConfidenceLevel.Low, "def build_headers():\n\treturn {\"Authorization\": encode_basic_auth(user_name, secret)}\n")
	];

	public static TheoryData<int> ClaimIndexes()
	{
		TheoryData<int> indexes = [];

		for (int index = 0; index < _claims.Length; index++)
		{
			indexes.Add(index);
		}

		return indexes;
	}

	[Theory]
	[MemberData(nameof(ClaimIndexes))]
	public void EveryClaimOfPartAIsTrueOfTheRegisteredRule(int index)
	{
		ProseClaim claim = _claims[index];
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(claim.Source);

		if (claim.IsRead)
		{
			Assert.Contains(constructs, construct => construct.PatternClassIdentifier == claim.PatternClass && construct.Level == claim.Level);
		}
		else
		{
			Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == claim.PatternClass);
		}
	}

	/// <summary>The literal rule of Part A: a literal is a credential literal only when it is the whole value, and a literal token of a held declaration raises the in-code secret warning.</summary>
	[Theory]
	[InlineData("response = requests.get(url, auth=(\"user\", \"exampleSecret\"))\n", _NAMED_PAIR_CLASS, true)]
	[InlineData("response = requests.get(url, auth=(\"user\", \"prefix\" + suffix))\n", _NAMED_PAIR_CLASS, false)]
	[InlineData("response = requests.get(url, headers={\"Authorization\": \"Basic dXNlcjpzZWNyZXQ=\"})\n", _HEADER_CLASS, true)]
	[InlineData("response = requests.get(url, headers={\"Authorization\": \"Basic \" + encoded})\n", _HEADER_CLASS, false)]
	[InlineData("scheme_value = \"Basic dXNlcjpzZWNyZXQ=\"\nresponse = requests.get(url, headers={\"Authorization\": scheme_value})\n", _HELD_HEADER_CLASS, true)]
	[InlineData("scheme_value = \"Basic \" + encoded\nresponse = requests.get(url, headers={\"Authorization\": scheme_value})\n", _HELD_HEADER_CLASS, false)]
	public void TheLiteralRuleAndTheSecretWarningOfPartAHold(string source, string patternClass, bool expectedWarning)
	{
		string partA = File.ReadAllText(DocumentedLanguage.Python.PartAPath());
		RecognizedConstruct construct = Assert.Single(new PythonRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == patternClass);

		Assert.Contains("a concatenation of a literal with code is not literal", partA, StringComparison.Ordinal);
		Assert.Contains("the finding then also raises the in-code secret warning", partA, StringComparison.Ordinal);
		Assert.Equal(expectedWarning, construct.LiteralCredentialSignal);
	}

	/// <summary>A match in a docstring is Low (the same text in an executable statement keeps its level, and a replacement field of an f-string is code), and Part A says so.</summary>
	[Fact]
	public void AMatchInADocstringIsLowAndTheSameCallInCodeKeepsItsLevel()
	{
		string partA = File.ReadAllText(DocumentedLanguage.Python.PartAPath());
		PythonRecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> inDocstring = harness.Recognize(_DOCSTRING_SOURCE);
		ImmutableArray<RecognizedConstruct> inCode = harness.Recognize(_PAIR_REQUEST);
		ImmutableArray<RecognizedConstruct> inField = harness.Recognize("message = f\"{requests.get(url, auth=('user', 'secret'))}\"\n");

		Assert.Contains("**A match in a docstring is Low.**", partA, StringComparison.Ordinal);
		Assert.NotEmpty(inDocstring);
		Assert.All(inDocstring, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
		Assert.Contains(inCode, construct => construct.PatternClassIdentifier == _NAMED_PAIR_CLASS && construct.Level == ConfidenceLevel.High);
		Assert.Contains(inField, construct => construct.PatternClassIdentifier == _NAMED_PAIR_CLASS && construct.Level == ConfidenceLevel.High);
	}

	/// <summary>The common forms of a hash in a format specification are read, so a construct after them keeps its level, and Part A states that most forms and not all are covered.</summary>
	[Theory]
	[InlineData("label = f\"{count:#x}\"\n")]
	[InlineData("label = f\"{count:>#10x}\"\n")]
	public void TheCommonFormsOfAHashInAFormatSpecificationAreReadAndAConstructAfterThemKeepsItsLevel(string formatLine)
	{
		string partA = File.ReadAllText(DocumentedLanguage.Python.PartAPath());

		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize($"{formatLine}{_PAIR_REQUEST}");

		Assert.Contains("**A format specification is read for most forms of `#`, not all.**", partA, StringComparison.Ordinal);
		Assert.Contains("Most forms are covered, and not every form", partA, StringComparison.Ordinal);
		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _NAMED_PAIR_CLASS && construct.Level == ConfidenceLevel.High);
	}

	/// <summary>
	/// The three consequences of an abandoned scan, each shown on a file that holds a High construct after a hash that follows a space sign: the construct is read at Low and not at its own level, a literal credential there
	/// carries no in-code secret warning, and the same file with the form taken out reads at High with the warning. Part A states the three consequences.
	/// </summary>
	[Fact]
	public void AnAbandonedScanDemotesTheConstructsAfterItAndRaisesNoSecretWarningThere()
	{
		string partA = File.ReadAllText(DocumentedLanguage.Python.PartAPath());
		PythonRecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> abandoned = harness.Recognize($"{_SPACE_SIGN_FORMAT}{_PAIR_REQUEST}");
		ImmutableArray<RecognizedConstruct> control = harness.Recognize(_PAIR_REQUEST);

		Assert.Contains("**The region after the point is read as string text.**", partA, StringComparison.Ordinal);
		Assert.Contains("**The credential and flow signals are silent after the point.**", partA, StringComparison.Ordinal);
		Assert.Contains("**The content demotion markers are not applied after the point.**", partA, StringComparison.Ordinal);
		Assert.DoesNotContain(abandoned, construct => construct.Level != ConfidenceLevel.Low);
		Assert.Contains(abandoned, construct => construct.PatternClassIdentifier == _NAMED_PAIR_CLASS && construct.Level == ConfidenceLevel.Low && !construct.LiteralCredentialSignal);
		Assert.Contains(control, construct => construct.PatternClassIdentifier == _NAMED_PAIR_CLASS && construct.Level == ConfidenceLevel.High && construct.LiteralCredentialSignal);
	}

	/// <summary>The drop order of the findings cap, as Part A states it: by path and not by confidence, with the two figures the cap's own constants give.</summary>
	[Fact]
	public void TheFindingsCapDropOrderStatedInPartAIsByPathAndTheFiguresAreTheCapsOwn()
	{
		string partA = File.ReadAllText(DocumentedLanguage.Python.PartAPath());
		string perFile = FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP.ToString("N0", CultureInfo.InvariantCulture);
		string global = FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP.ToString("N0", CultureInfo.InvariantCulture);

		Assert.Contains("the drop order is by path prefix and not by confidence", partA, StringComparison.Ordinal);
		Assert.Contains($"more than {global} findings", partA, StringComparison.Ordinal);
		Assert.Contains($"keeps at most {perFile} findings from any one file", partA, StringComparison.Ordinal);
		Assert.Contains("drops the rest, by path and not by confidence", partA, StringComparison.Ordinal);
		Assert.Contains("A Low finding that sorts early is therefore kept ahead of a High finding that sorts late.", partA, StringComparison.Ordinal);
		Assert.Contains("(for example `tests/`, `third_party/` or `vendor/`)", partA, StringComparison.Ordinal);
	}

	[Fact]
	public void EveryPhraseOfEveryClaimStillStandsInTheSectionThatMakesIt()
	{
		string partA = File.ReadAllText(DocumentedLanguage.Python.PartAPath());
		IReadOnlyDictionary<string, DetectionRule> rules = DocumentedLanguage.Python.Analyzer().DetectionRulesByPatternClass;

		foreach (ProseClaim claim in _claims)
		{
			string section = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(claim.Section, rules[claim.Section].DisplayName)) ?? string.Empty;

			Assert.True(section.Contains(claim.Phrase, StringComparison.Ordinal), $"The section of {claim.Section} no longer holds the phrase: {claim.Phrase}");
		}
	}

	[Fact]
	public void TheClaimsCoverTheClassesWhoseRulesAreSubtle()
	{
		Assert.Equal(
			new[] { _HELPER_CLASS, _NAMED_PAIR_CLASS, _HEADER_CLASS, _HANDLER_CLASS, _MAKE_HEADERS_CLASS, _UNNAMED_PAIR_CLASS, _ENCODER_CLASS, _HELD_HEADER_CLASS, _REQUEST_URL_CLASS, _HELD_URL_CLASS, _MAPPING_CLASS }.Order(StringComparer.Ordinal),
			_claims.Select(claim => claim.Section).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
	}
}
