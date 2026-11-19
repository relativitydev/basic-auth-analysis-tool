using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Lexical;

using Xunit;

using static MigrateAuth.Cli.Tests.MatchBoundStress.PythonDraftExpressionSet;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SEMANTIC CHECKS OF THE DRAFT `Python` EXPRESSIONS. Every draft expression of <see cref="PythonDraftExpressionSet"/> has positive and near-miss samples here, each a shape the catalog names, written with
/// generic names (the example.test domain, `user`, `secret`) and no fragment of any real text: the fourteen classes with the near-miss families they must keep out (inbound reads, bearer values, digest and proxy helpers, non-HTTP
/// userinfo, a base64 of something that is not a credential, a token request), the first-line rule with the declared `python -S` refusal, the constant declaration, the two content markers, and the twelve flow signals with the
/// near-miss form of each. The subsumption fixture is a test of its own: the named and the unnamed tuple-keyword classes report the same `auth` position, so that the declared triple removes the duplicate.
/// The generated and the interpreted form of every expression must agree on every sample. These tests run in the normal suite (no clock).
/// </summary>
public sealed class PythonDraftExpressionSemanticTests
{
	private const string _LINE_FEED = "\n";
	private const int _ABBREVIATION_MAXIMUM_LENGTH = 160;
	private const int _FILLER_LINES_BEFORE_A_TEN_LINE_BANNER = 10;
	private const int _FILLER_LINES_INSIDE_A_TEN_LINE_BANNER = 9;
	private const int _IDENTIFIER_DENSE_GROUP_LENGTH_MARGIN = 200;
	private const int _FIRST_LINE_LENGTH_MARGIN = 200;
	private const int _LEXICAL_SMOKE_SCAN_SIZE_IN_CHARACTERS = 64 * 1024;

	internal sealed record Case(string Id, string Text, bool Matches, string Group = "", string Value = "", string? Credential = null);

	private static readonly ImmutableArray<string> _ALLOWED_SHAPE_EXTENSIONS = [PythonTestDraftConstants.FILE_EXTENSION];

	private static readonly ImmutableArray<Case> _CASES =
	[
		.. FirstLineCases(),
		.. ConstantDeclarationCases(),
		.. HelperConstructionCases(),
		.. TupleKeywordCases(),
		.. HeaderCarrierCases(),
		.. UrllibAndUrllib3Cases(),
		.. EncoderCases(),
		.. UserinfoCases(),
		.. ContentMarkerCases(),
		.. InteractiveFlowCases(),
		.. FixedFlowCases()
	];

	private static readonly ImmutableDictionary<string, Regex> _EXPRESSIONS = PythonDraftExpressionSet.AllExpressions.ToImmutableDictionary(entry => entry.Identifier, entry => entry.Expression, StringComparer.Ordinal);

	[Fact]
	public void EveryDraftExpressionHasPositiveAndNearMissSamples()
	{
		Assert.Equal(30, PythonDraftExpressionSet.AllExpressions.Length);
		Assert.All(PythonDraftExpressionSet.AllExpressions.Select(entry => entry.Identifier), identifier =>
		{
			Assert.Contains(_CASES, candidate => candidate.Id == identifier && candidate.Matches);
			Assert.Contains(_CASES, candidate => candidate.Id == identifier && !candidate.Matches);
		});
	}

	[Fact]
	public void EveryDraftExpressionMatchesExactlyTheSamplesItShouldAndCapturesWhatTheCatalogSays()
	{
		List<string> wrong = [];

		foreach (Case sample in _CASES)
		{
			Match match = _EXPRESSIONS[sample.Id].Match(sample.Text);
			bool anchored = sample.Group.Length == 0 || (match.Success && match.Groups[sample.Group].Value == sample.Value);
			bool credentialed = sample.Credential is null || (match.Success && match.Groups[CREDENTIAL_GROUP].Value == sample.Credential);

			if (match.Success != sample.Matches || !anchored || !credentialed)
			{
				wrong.Add($"{sample.Id} on [{Escape(Abbreviate(sample.Text))}] expected {sample.Matches} {sample.Group}={sample.Value} credential={sample.Credential}, got {match.Success} {(sample.Group.Length == 0 ? string.Empty : match.Groups[sample.Group].Value)} credential={match.Groups[CREDENTIAL_GROUP].Value}");
			}
		}

		Assert.Empty(wrong);
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfEveryDraftExpressionAgreeOnEverySample()
	{
		List<string> disagreements = [];

		foreach (Case sample in _CASES)
		{
			Regex generated = _EXPRESSIONS[sample.Id];
			Regex interpreted = new(generated.ToString(), generated.Options, generated.MatchTimeout);
			Match fromGenerated = generated.Match(sample.Text);
			Match fromInterpreted = interpreted.Match(sample.Text);

			if (fromGenerated.Success != fromInterpreted.Success || fromGenerated.Index != fromInterpreted.Index || fromGenerated.Length != fromInterpreted.Length)
			{
				disagreements.Add($"{sample.Id} on {Escape(Abbreviate(sample.Text))}");
			}
		}

		Assert.Empty(disagreements);
	}

	[Fact]
	public void EveryClassExpressionDeclaresTheAnchorGroupOfTheCatalogAndEveryFlowSignalTheAnchorGroupOfTheEngine()
	{
		List<string> missing = [];

		foreach ((string identifier, Regex expression) in PythonDraftExpressionSet.ClassExpressions)
		{
			string anchorGroup = identifier is PythonDraftExpressionSet.NAMED_TUPLE_IDENTIFIER or PythonDraftExpressionSet.UNNAMED_TUPLE_IDENTIFIER ? AUTH_GROUP : CONSTRUCT_GROUP;

			if (!expression.GetGroupNames().Contains(anchorGroup))
			{
				missing.Add($"{identifier} lacks {anchorGroup}");
			}
		}

		foreach ((string identifier, Regex expression) in PythonDraftExpressionSet.FlowSignalExpressions)
		{
			if (!expression.GetGroupNames().Contains(ANCHOR_GROUP))
			{
				missing.Add($"{identifier} lacks {ANCHOR_GROUP}");
			}
		}

		Assert.Empty(missing);
	}

	[Fact]
	public void TheConstantDeclarationDeclaresTheTwoGroupsOfTheEnvironmentRecordAsTheEngineKeysOnThem()
	{
		string[] groups = _EXPRESSIONS[PythonDraftExpressionSet.CONSTANT_DECLARATION_IDENTIFIER].GetGroupNames();

		Assert.Contains("name", groups);
		Assert.Contains("value", groups);
		Assert.Contains("environment", groups);
		Assert.Contains("fallbackWord", groups);
	}

	/// <summary>
	/// THE SUBSUMPTION FIXTURE. The named-callee class and the unnamed class report the SAME `auth` position on a named call, which is what makes the triple at identical anchors apply; the unnamed receivers are read
	/// by `PY-006` alone. Without the triple a named call would be emitted twice (a High and a Medium at one `auth`): the negative control below shows both match there.
	/// </summary>
	[Fact]
	public void TheNamedAndTheUnnamedTupleClassesReportTheSameAuthPositionOnANamedCallAndOnlyTheUnnamedOneReadsTheOtherReceivers()
	{
		Regex named = _EXPRESSIONS["EX-PY-002"];
		Regex unnamed = _EXPRESSIONS["EX-PY-006"];
		string namedCall = """requests.get("https://example.test/a", auth=("user", "secret"))""";
		string sessionCall = """session.get("https://example.test/b", auth=("user", "secret"))""";
		string sessionAttribute = """session.auth = ("user", "secret")""";
		Match namedFromTheNamedClass = named.Match(namedCall);
		Match namedFromTheUnnamedClass = unnamed.Match(namedCall);

		Assert.True(namedFromTheNamedClass.Success);
		Assert.True(namedFromTheUnnamedClass.Success);
		Assert.Equal(namedFromTheNamedClass.Groups[AUTH_GROUP].Index, namedFromTheUnnamedClass.Groups[AUTH_GROUP].Index);
		Assert.Equal(AUTH_GROUP, namedFromTheNamedClass.Groups[AUTH_GROUP].Value);
		Assert.DoesNotMatch(named, sessionCall);
		Assert.Matches(unnamed, sessionCall);
		Assert.DoesNotMatch(named, sessionAttribute);
		Assert.Matches(unnamed, sessionAttribute);
		Assert.Contains(PythonDraftExpressionSet.SubsumptionTriples, triple => triple.Subsuming == "PY-002" && triple.Subsumed == "PY-006" && triple.When == MigrateAuth.Cli.Catalog.SubsumptionCondition.AnchorPositionsAreIdentical);
	}

	/// <summary>The form-by-form cells of the catalog's table: a module alias, a name imported with `from` and a session-level default are read by `PY-006` and never by `PY-002`, whatever the client.</summary>
	[Theory]
	[InlineData("""rq.get(url, auth=("user", "secret"))""")]
	[InlineData("""get(url, auth=("user", "secret"))""")]
	[InlineData("""client.post(url, auth=("user", "secret"))""")]
	[InlineData("""client.auth = ("user", "secret")""")]
	public void AnAliasedImportedOrSessionLevelTupleIsReadByTheUnnamedClassOnly(string text)
	{
		Assert.DoesNotMatch(_EXPRESSIONS["EX-PY-002"], text);
		Assert.Matches(_EXPRESSIONS["EX-PY-006"], text);
	}

	[Fact]
	public void TheDraftConstantDeclarationReadsTheSameNameAsTheShapedProfileOnTheDeclarationsItCountsAndAvoidsTheLazyLoopInAnOptionalGroupThatTheShapedProfileCarries()
	{
		string[] declarations = ["scheme = \"Basic\"", "SCHEME: str = 'bearer'", "mode = os.getenv(\"AUTH_VALUE\")"];

		Assert.All(declarations, text =>
		{
			Match draft = _EXPRESSIONS[PythonDraftExpressionSet.CONSTANT_DECLARATION_IDENTIFIER].Match(text);
			Match profile = PythonShapedProfile.ConstantDeclaration().Match(text);

			Assert.True(draft.Success, text);
			Assert.True(profile.Success, text);
			Assert.Equal(profile.Groups["name"].Value, draft.Groups["name"].Value);
		});

		PatternShape profileShape = PatternShape.Analyze(PythonShapedProfile.ConstantDeclaration().ToString());
		PatternShape draftShape = PatternShape.Analyze(_EXPRESSIONS[PythonDraftExpressionSet.CONSTANT_DECLARATION_IDENTIFIER].ToString());

		Assert.True(profileShape.LazyLoopsInOptionalGroups > 0);
		Assert.Equal(0, draftShape.LazyLoopsInOptionalGroups);
	}

	[Fact]
	public void EveryStressShapeIsLegalForTheReaderAndFillsTheFileToJustUnderTheCeiling()
	{
		List<string> refused = [];

		foreach (MatchBoundStressShape shape in PythonDraftCostGateStressSet.Build())
		{
			bool tooLarge = Encoding.UTF8.GetByteCount(shape.Text) > ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES;
			bool lineTooLong = shape.Text.Split(_LINE_FEED).Any(line => line.Length > ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);

			if (tooLarge || lineTooLong || shape.Text.Contains('\r') || !_ALLOWED_SHAPE_EXTENSIONS.Contains(shape.FileExtension))
			{
				refused.Add($"{shape.Name}: tooLarge={tooLarge} lineTooLong={lineTooLong} extension={shape.FileExtension}");
			}
		}

		Assert.Empty(refused);
	}

	[Fact]
	public void EveryStressShapeNameIsUniqueAndTheShapesCoverEveryNamedFamily()
	{
		ImmutableArray<MatchBoundStressShape> shapes = PythonDraftCostGateStressSet.Build();
		string[] families =
		[
			"long-line-auth-tuple-openers-that-never-close", "long-line-named-callee-openers", "long-line-authorization-words", "long-line-f-string-heavy", "long-line-nested-brackets-at-the-depth-bound",
			"long-line-nested-brackets-past-the-depth-bound", "header-mapping-with-thousands-of-keys", "vendored-library-shape", "long-line-userinfo-urls",
			"long-line-dense-method-calls", "anchor-dense-near-misses", "every-class-at-match-density", "open-windows", "ordinary-at-ceiling"
		];

		Assert.Equal(shapes.Length, shapes.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
		Assert.All(families, family => Assert.Contains(shapes, shape => shape.Name.Contains(family, StringComparison.Ordinal)));
	}

	/// <summary>The shapes of the lexical-budget measurement are legal for the reader, the first of them is the identifier-dense 2 MiB file with lines at the ceiling, and a 64 KiB prefix of each scans through the shaped profile (no clock).</summary>
	[Fact]
	public void EveryLexicalBudgetShapeIsLegalAndTheIdentifierDenseOneIsTheFileTheDecisionNames()
	{
		ImmutableArray<MatchBoundStressShape> shapes = PythonLexicalBudgetMeasurements.BuildShapes();
		MatchBoundStressShape identifierDense = shapes[0];
		List<string> refused = [];
		LexicalScanner scanner = new(PythonShapedProfile.Build());

		foreach (MatchBoundStressShape shape in shapes)
		{
			bool tooLarge = Encoding.UTF8.GetByteCount(shape.Text) > ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES;
			bool lineTooLong = shape.Text.Split(_LINE_FEED).Any(line => line.Length > ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);

			if (tooLarge || lineTooLong || shape.Text.Contains('\r'))
			{
				refused.Add($"{shape.Name}: tooLarge={tooLarge} lineTooLong={lineTooLong}");
			}

			_ = scanner.Scan(PythonDraftCostGateStressSet.PrefixAtALineBoundary(shape.Text, _LEXICAL_SMOKE_SCAN_SIZE_IN_CHARACTERS));
		}

		Assert.Empty(refused);
		Assert.Equal(PythonLexicalBudgetMeasurements.IDENTIFIER_DENSE_SHAPE_NAME, identifierDense.Name);
		Assert.InRange(identifierDense.Text.Length, ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - (32 * 1024), ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES);
		Assert.Contains(identifierDense.Text.Split(_LINE_FEED), line => line.Length >= PythonDraftCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS - _IDENTIFIER_DENSE_GROUP_LENGTH_MARGIN);
		Assert.Equal(PythonLexicalBudgetMeasurements.BuildShapes().Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count(), shapes.Length);
	}

	/// <summary>The first lines of the lexical-budget measurement are at the line ceiling, and the expression reads each as the catalog says (the accepted forms match, the others do not).</summary>
	[Fact]
	public void EveryFirstLineOfTheLexicalBudgetMeasurementIsLegalAndIsReadAsTheCatalogSays()
	{
		ImmutableArray<PythonFirstLineShape> firstLines = PythonLexicalBudgetMeasurements.BuildFirstLineShapes();

		Assert.All(firstLines, firstLine => Assert.True(firstLine.Line.Length <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS, firstLine.Name));
		Assert.All(firstLines, firstLine => Assert.Equal(firstLine.Accepted, PythonDraftExpressionSet.FirstLine.IsMatch(firstLine.Line)));
		Assert.Contains(firstLines, firstLine => firstLine.Line.Length >= PythonDraftCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS - _FIRST_LINE_LENGTH_MARGIN);
	}

	private static string Abbreviate(string text)
	{
		return text.Length <= _ABBREVIATION_MAXIMUM_LENGTH ? text : string.Concat(text.AsSpan(0, _ABBREVIATION_MAXIMUM_LENGTH), "...");
	}

	private static string Escape(string text)
	{
		return text.Replace("\n", "\\n", StringComparison.Ordinal).Replace("\t", "\\t", StringComparison.Ordinal);
	}

	private static Case Yes(string id, string text, string group = "", string value = "", string? credential = null)
	{
		return new Case(id, text, true, group, value, credential);
	}

	private static Case No(string id, string text)
	{
		return new Case(id, text, false);
	}

	private static string Lines(params string[] lines)
	{
		return string.Join(_LINE_FEED, lines);
	}

	private static IEnumerable<Case> FirstLineCases()
	{
		string id = "EX-PY-100";

		string[] accepted =
		[
			"#!/usr/bin/python", "#!/usr/bin/python2", "#!/usr/bin/python3", "#!/usr/bin/python3.11", "#!/usr/bin/env python3", "#!/usr/bin/env -S python3", "#! /usr/bin/env python", "#!python3",
			"#!/usr/bin/python3.12 ", "#!/usr/bin/env python3 -u", "#!/usr/bin/python -u"
		];

		string[] refused =
		[
			"#!/usr/bin/python -S", "#!/usr/bin/env python -S", "#!/usr/bin/python3 -S", "#!/usr/bin/python3.11 -S", "#!/usr/bin/python2.7", "#!/usr/bin/pypy", "#!/usr/bin/env VAR=1 python", "#!/bin/sh",
			"#!/usr/bin/python30", "#!/usr/bin/Python3", "#!/usr/bin/python3.x", "#!/usr/bin/python3.1a", "#!/usr/bin/python3.٣", "# python3", "import python3"
		];

		return [.. accepted.Select(line => Yes(id, line)), .. refused.Select(line => No(id, line))];
	}

	private static IEnumerable<Case> ConstantDeclarationCases()
	{
		string id = "EX-PY-101";

		return
		[
			Yes(id, """scheme = "Basic" """.TrimEnd(), "name", "scheme"),
			Yes(id, """SCHEME: str = 'bearer'""", "name", "SCHEME"),
			Yes(id, """raw_value = r'a\b'""", "name", "raw_value"),
			Yes(id, """mode = os.environ.get("AUTH_VALUE", "Basic dGVzdDp0ZXN0")""", "fallbackWord", "Basic"),
			Yes(id, """mode = os.getenv("AUTH_VALUE")""", "fallbackWord", ""),
			Yes(id, """mode = os.environ["AUTH_VALUE"]  # read once""", "name", "mode"),
			No(id, """if scheme == "Basic":"""),
			No(id, """total = 1 + 2"""),
			No(id, """scheme = "Basic" + suffix"""),
			No(id, """text = f"Basic {value}" """.TrimEnd()),
			No(id, """mode = os.environ["AUTH_VALUE"] or fallback""")
		];
	}

	private static IEnumerable<Case> HelperConstructionCases()
	{
		string id = "EX-PY-001";

		return
		[
			Yes(id, """response = requests.get("https://example.test/items", auth=HTTPBasicAuth("user", "secret"))""", CONSTRUCT_GROUP, "HTTPBasicAuth", "secret"),
			Yes(id, """client = httpx.Client(auth=httpx.BasicAuth(username="user", password="secret"))""", CONSTRUCT_GROUP, "BasicAuth", "secret"),
			Yes(id, """session = aiohttp.ClientSession(auth=aiohttp.BasicAuth("user", "secret"))""", CONSTRUCT_GROUP, "BasicAuth", "secret"),
			Yes(id, """auth = rq.auth.HTTPBasicAuth(user_name, secret_value)""", CONSTRUCT_GROUP, "HTTPBasicAuth", ""),
			Yes(id, """auth = HTTPBasicAuth("user", password="secret")""", CONSTRUCT_GROUP, "HTTPBasicAuth", "secret"),
			Yes(id, """auth = HTTPBasicAuth(username="user", password="secret")""", CONSTRUCT_GROUP, "HTTPBasicAuth", "secret"),
			Yes(id, """auth = HTTPBasicAuth("user", "<password>")""", CONSTRUCT_GROUP, "HTTPBasicAuth", ""),
			Yes(id, """auth = HTTPBasicAuth("user", f"{secret_value}")""", CONSTRUCT_GROUP, "HTTPBasicAuth", ""),
			No(id, """auth = HTTPProxyAuth("user", "secret")"""),
			No(id, """auth = HTTPDigestAuth("user", "secret")"""),
			No(id, """auth = httpx.DigestAuth("user", "secret")"""),
			No(id, """credentials = BasicAuth.decode(header_value)"""),
			No(id, """credentials = aiohttp.BasicAuth.from_url(url)"""),
			No(id, """if isinstance(auth, HTTPBasicAuth):"""),
			No(id, """class CustomAuth(HTTPBasicAuth):"""),
			No(id, """security = HTTPBasic()"""),
			No(id, """auth = HTTPBasicAuth()"""),
			No(id, """async with session.get(url, proxy_auth=aiohttp.BasicAuth("user", "secret")) as response:""")
		];
	}

	private static IEnumerable<Case> TupleKeywordCases()
	{
		return
		[
			Yes("EX-PY-002", """response = requests.get("https://example.test/items", auth=("user", "secret"))""", AUTH_GROUP, "auth", "secret"),
			Yes("EX-PY-002", """client = httpx.AsyncClient(auth=("user", "secret"))""", AUTH_GROUP, "auth", "secret"),
			Yes("EX-PY-002", Lines("response = requests.post(", "\t\"https://example.test/items\",", "\tjson={\"name\": [1, 2]},", "\tauth=(\"user\", \"secret\"),", ")"), AUTH_GROUP, "auth", "secret"),
			Yes("EX-PY-002", """response = requests.request("GET", url, auth=(user_name, secret_value))""", AUTH_GROUP, "auth", ""),
			Yes("EX-PY-002", """response = httpx.get(url, auth=("user", os.environ.get("SERVICE_VALUE", "none")))""", AUTH_GROUP, "auth", ""),
			Yes("EX-PY-002", """response = requests.get(url, auth=("user", "secret",))""", AUTH_GROUP, "auth", "secret"),
			No("EX-PY-002", """response = rq.get(url, auth=("user", "secret"))"""),
			No("EX-PY-002", """response = session.get(url, auth=("user", "secret"))"""),
			No("EX-PY-002", """response = get(url, auth=("user", "secret"))"""),
			No("EX-PY-002", """response = aiohttp.get(url, auth=("user", "secret"))"""),
			No("EX-PY-002", """response = requests.get(url, auth=("user", "secret", "extra"))"""),
			No("EX-PY-002", """response = requests.get(url, auth=("user",))"""),
			No("EX-PY-002", """response = requests.get(url, auth=["user", "secret"])"""),
			No("EX-PY-002", """response = requests.get(url, auth=credentials)"""),
			No("EX-PY-002", """response = requests.get(url, auth=HTTPDigestAuth("user", "secret"))"""),
			No("EX-PY-002", """response = requests.post(token_url, auth=("client_id", "client_value"), data={"grant_type": "client_credentials"})"""),
			No("EX-PY-002", """response = requests.post(token_url, data={"grant_type": "client_credentials"}, auth=("client_id", "client_value"))"""),
			No("EX-PY-002", Lines("response = requests.post(token_url, auth=(\"client_id\", \"client_value\"), data={", "\t\"scope\": \"read\",", "\t\"grant_type\": \"client_credentials\",", "})")),

			Yes("EX-PY-006", """response = session.get("https://example.test/items", auth=("user", "secret"))""", AUTH_GROUP, "auth", "secret"),
			Yes("EX-PY-006", """session.auth = ("user", "secret")""", AUTH_GROUP, "auth", "secret"),
			Yes("EX-PY-006", """auth = ("user", "secret")""", AUTH_GROUP, "auth", "secret"),
			Yes("EX-PY-006", """response = rq.get(url, auth=("user", "secret"))""", AUTH_GROUP, "auth", "secret"),
			Yes("EX-PY-006", """client.auth = (user_name, secret_value)""", AUTH_GROUP, "auth", ""),
			Yes("EX-PY-006", """response = requests.get(url, auth=("user", "secret"))""", AUTH_GROUP, "auth", "secret"),
			No("EX-PY-006", """auth = ("user", "secret", "extra")"""),
			No("EX-PY-006", """oauth = ("user", "secret")"""),
			No("EX-PY-006", """if auth == ("user", "secret"):"""),
			No("EX-PY-006", """session.auth = HTTPBasicAuth("user", "secret")"""),
			No("EX-PY-006", """session.auth = ["user", "secret"]"""),
			No("EX-PY-006", """response = rq.post(token_url, data={"grant_type": "client_credentials"}, auth=("client_id", "client_value"))"""),
			No("EX-PY-006", """response = rq.post(token_url, auth=("client_id", "client_value"), data={"grant_type": "client_credentials"})"""),
			No("EX-PY-006", """response = rq.post(token_url, auth=("client_id", "grant_type"))"""),
			No("EX-PY-006", """response = rq.post(token_url, auth=(grant_type, "client_value"))"""),
			No("EX-PY-006", """response = rq.post(token_url, auth=(("a", "grant_type"), "client_value"))"""),
			No("EX-PY-006", """session.auth = ("client_id", "client_value") # grant_type"""),
			Yes("EX-PY-006", $"session.auth = (\"client_id\", \"client_value\") # {new string('x', 320)} grant_type", AUTH_GROUP, "auth", "client_value"),
			Yes("EX-PY-006", Lines("session.auth = (\"client_id\", \"client_value\")", "other = \"grant_type\""), AUTH_GROUP, "auth", "client_value"),
			No("EX-PY-002", """response = requests.post(token_url, auth=("client_id", "grant_type"))"""),
			No("EX-PY-002", """response = requests.post(token_url, auth=(grant_type, "client_value"))"""),
			No("EX-PY-002", """response = requests.post(token_url, auth=(("a", "grant_type"), "client_value"))"""),
			No("EX-PY-002", """response = requests.post(token_url, auth=("client_id", get("grant_type")))""")
		];
	}

	private static IEnumerable<Case> HeaderCarrierCases()
	{
		return
		[
			Yes("EX-PY-003", """response = requests.get(url, headers={"Authorization": "Basic " + encoded})""", CONSTRUCT_GROUP, "headers", ""),
			Yes("EX-PY-003", """session.headers["Authorization"] = f"Basic {encoded}" """.TrimEnd(), CONSTRUCT_GROUP, "headers", ""),
			Yes("EX-PY-003", """request.add_header("Authorization", "Basic %s" % encoded)""", CONSTRUCT_GROUP, "add_header", ""),
			Yes("EX-PY-003", """headers = dict(Authorization="Basic {}".format(encoded))""", CONSTRUCT_GROUP, "Authorization", ""),
			Yes("EX-PY-003", """async with aiohttp.ClientSession(headers={"Authorization": encode_basic_auth("user", "secret")}) as session:""", CONSTRUCT_GROUP, "headers", ""),
			Yes("EX-PY-003", """session.headers.update({"authorization": "basic dXNlcjpzZWNyZXQ="})""", CONSTRUCT_GROUP, "headers", "dXNlcjpzZWNyZXQ="),
			Yes("EX-PY-003", """connection.putheader("Authorization", "Basic dXNlcjpzZWNyZXQ=")""", CONSTRUCT_GROUP, "putheader", "dXNlcjpzZWNyZXQ="),
			Yes("EX-PY-003", """request.add_unredirected_header("Authorization", "Basic " + encoded)""", CONSTRUCT_GROUP, "add_unredirected_header", ""),
			Yes("EX-PY-003", """headers = {"Accept": "text/plain", "Authorization": "Basic dXNlcjpzZWNyZXQ="}""", CONSTRUCT_GROUP, "headers", "dXNlcjpzZWNyZXQ="),
			Yes("EX-PY-003", """headers: dict = {"Authorization": b"Basic " + encoded}""", CONSTRUCT_GROUP, "headers", ""),
			No("EX-PY-003", """headers = {"Authorization": f"Bearer {token}"}"""),
			No("EX-PY-003", """headers = {"Proxy-Authorization": "Basic " + token}"""),
			No("EX-PY-003", """headers = {"X-Authorization": "Basic " + token}"""),
			No("EX-PY-003", """value = request.headers["Authorization"]"""),
			No("EX-PY-003", """if request.headers["Authorization"] == "Basic abc":"""),
			No("EX-PY-003", """request.add_header("Authorization", "Bearer " + token)"""),
			No("EX-PY-003", """headers = {"Authorization": t"Basic {encoded}"}"""),
			No("EX-PY-003", """headers = {"Authorization": token}"""),
			No("EX-PY-003", """# headers = {Authorization: Basic ...}"""),
			No("EX-PY-003", """response["WWW-Authenticate"] = 'Basic realm="example"'"""),
			No("EX-PY-003", """raise HTTPException(401, headers={"WWW-Authenticate": "Basic"})"""),
			No("EX-PY-003", """SAMPLE_RECEIVED = "Basic dXNlcjpzZWNyZXQ=" """.TrimEnd()),
			No("EX-PY-003", """credentials = parse_basic("Basic dXNlcjpzZWNyZXQ=")"""),

			Yes("EX-PY-013", """response = requests.get(url, headers={"Authorization": scheme_value})""", CONSTRUCT_GROUP, "headers"),
			Yes("EX-PY-013", """session.headers["Authorization"] = scheme_value""", CONSTRUCT_GROUP, "headers"),
			Yes("EX-PY-013", """request.add_header("Authorization", scheme_value)""", CONSTRUCT_GROUP, "add_header"),
			Yes("EX-PY-013", """headers = dict(Authorization=scheme_value)""", CONSTRUCT_GROUP, "Authorization"),
			Yes("EX-PY-013", """headers = {"Authorization": scheme_value}""", "scheme", "scheme_value"),
			No("EX-PY-013", """headers = {"Authorization": "Basic " + token}"""),
			No("EX-PY-013", """headers = {"Authorization": build_value()}"""),
			No("EX-PY-013", """headers = {"Authorization": token.value}"""),
			No("EX-PY-013", """headers = {"Proxy-Authorization": scheme_value}"""),
			No("EX-PY-013", """response = requests.get(url, headers={API_KEY_HEADER: api_key})"""),
			No("EX-PY-013", """response = requests.get(url, headers={"X-API-Key": api_key})"""),

			Yes("EX-PY-014", """return {"Authorization": "Basic dXNlcjpzZWNyZXQ="}""", CONSTRUCT_GROUP, "Authorization", "dXNlcjpzZWNyZXQ="),
			Yes("EX-PY-014", """headers = {'authorization': 'basic dXNlcjpzZWNyZXQ='}""", CONSTRUCT_GROUP, "authorization", "dXNlcjpzZWNyZXQ="),
			Yes("EX-PY-014", """return {"Authorization": "Basic " + token}""", CONSTRUCT_GROUP, "Authorization", ""),
			No("EX-PY-014", """return {"Authorization": f"Bearer {token}"}"""),
			No("EX-PY-014", """return {"Proxy-Authorization": "Basic dXNlcjpzZWNyZXQ="}"""),
			No("EX-PY-014", """return {"Authorization": token}"""),
			No("EX-PY-014", """# Basic auth is deprecated"""),
			No("EX-PY-014", """return {"headers": {"WWW-Authenticate": "Basic"}}"""),
			No("EX-PY-014", """SCHEMES = {"basic": "Basic (username and password)"}"""),
			No("EX-PY-014", """usage = "Send the header Authorization: Basic with the encoded pair" """.TrimEnd()),
			No("EX-PY-014", """SAMPLE_RECEIVED = "Basic dXNlcjpzZWNyZXQ=" """.TrimEnd())
		];
	}

	private static IEnumerable<Case> UrllibAndUrllib3Cases()
	{
		return
		[
			Yes("EX-PY-004", """handler = urllib.request.HTTPBasicAuthHandler(password_manager)""", CONSTRUCT_GROUP, "HTTPBasicAuthHandler"),
			Yes("EX-PY-004", """opener = build_opener(HTTPBasicAuthHandler())""", CONSTRUCT_GROUP, "HTTPBasicAuthHandler"),
			No("EX-PY-004", """handler = urllib.request.ProxyBasicAuthHandler(password_manager)"""),
			No("EX-PY-004", """handler = urllib.request.HTTPDigestAuthHandler(password_manager)"""),
			No("EX-PY-004", """class CustomHandler(AbstractBasicAuthHandler):"""),
			No("EX-PY-004", """if isinstance(handler, HTTPBasicAuthHandler):"""),

			Yes("EX-PY-005", """headers = urllib3.util.make_headers(basic_auth="user:secret")""", CONSTRUCT_GROUP, "make_headers", "secret"),
			Yes("EX-PY-005", """http = urllib3.PoolManager(headers=make_headers(basic_auth=f"{user_name}:{secret_value}"))""", CONSTRUCT_GROUP, "make_headers", ""),
			Yes("EX-PY-005", """headers = make_headers(keep_alive=True, basic_auth="user:secret")""", CONSTRUCT_GROUP, "make_headers", "secret"),
			Yes("EX-PY-005", """headers = make_headers(basic_auth=pair_value)""", CONSTRUCT_GROUP, "make_headers", ""),
			No("EX-PY-005", """headers = urllib3.util.make_headers(proxy_basic_auth="user:secret")"""),
			No("EX-PY-005", """headers = urllib3.util.make_headers(keep_alive=True)"""),

			Yes("EX-PY-009", """manager.add_password(None, "https://example.test/", "user", "secret")""", CONSTRUCT_GROUP, "add_password", "secret"),
			Yes("EX-PY-009", """manager.add_password(None, "https://example.test/", "user", "secret", is_authenticated=True)""", CONSTRUCT_GROUP, "add_password", "secret"),
			Yes("EX-PY-009", """manager.add_password(realm, url, user_name, secret_value)""", CONSTRUCT_GROUP, "add_password", ""),
			No("EX-PY-009", """manager.add_password(None, url, user_name)"""),
			No("EX-PY-009", """add_password(None, url, user_name, secret_value)"""),
			No("EX-PY-009", """manager.add_passwords(None, url, user_name, secret_value)"""),

			Yes("EX-PY-010", """client = httpx.Client(auth=httpx.NetRCAuth())""", CONSTRUCT_GROUP, "NetRCAuth"),
			Yes("EX-PY-010", """client = httpx.Client(auth=NetRCAuth(file="/path/to/.netrc"))""", CONSTRUCT_GROUP, "NetRCAuth"),
			No("EX-PY-010", """login, account, secret = netrc.netrc().authenticators(host)"""),
			No("EX-PY-010", """client = httpx.Client(auth=NetRCAuthority())""")
		];
	}

	private static IEnumerable<Case> EncoderCases()
	{
		return
		[
			Yes("EX-PY-007", Lines("token = base64.b64encode(f\"{user_name}:{secret_value}\".encode()).decode()", "headers = {\"Authorization\": \"Basic \" + token}"), CONSTRUCT_GROUP, "b64encode", ""),
			Yes("EX-PY-007", """pair = b64encode(user_name + ":" + secret_value)  # Basic header""", CONSTRUCT_GROUP, "b64encode"),
			Yes("EX-PY-007", """value = base64.standard_b64encode(b"user:secret")  # Authorization""", CONSTRUCT_GROUP, "standard_b64encode", "secret"),
			Yes("EX-PY-007", """value = base64.encodebytes("%s:%s" % (user_name, secret_value))  # sent as a Basic header""", CONSTRUCT_GROUP, "encodebytes"),
			Yes("EX-PY-007", """value = b64encode(":".join((user_name, secret_value)))  # Authorization""", CONSTRUCT_GROUP, "b64encode"),
			Yes("EX-PY-007", """value = b64encode("{}:{}".format(user_name, secret_value))  # Basic""", CONSTRUCT_GROUP, "b64encode"),
			No("EX-PY-007", """value = base64.b64encode(json_payload)"""),
			No("EX-PY-007", """value = base64.b64encode(f"{host_name}:{port_number}".encode())"""),
			No("EX-PY-007", """value = base64.urlsafe_b64encode(cache_key)"""),
			No("EX-PY-007", """value = base64.b32encode(identifier)"""),
			No("EX-PY-007", """value = base64.b64decode(header_value.split()[1])"""),
			No("EX-PY-007", """value = b64encode(f"{user_name}:{secret_value}")"""),

			// 0.6: the held-name shape needs the word Basic in the statement window; Authorization alone is not enough.
			Yes("EX-PY-007", Lines("pair = f\"{user_name}:{secret_value}\"", "encoded = base64.b64encode(pair.encode())", "headers = {\"Authorization\": \"Basic \" + encoded.decode()}"), CONSTRUCT_GROUP, "b64encode"),
			Yes("EX-PY-007", """encoded = b64encode(PAIR)  # sent as a Basic header""", CONSTRUCT_GROUP, "b64encode"),
			Yes("EX-PY-007", """encoded = base64.standard_b64encode(self.raw_pair)  # basic scheme""", CONSTRUCT_GROUP, "standard_b64encode"),
			Yes("EX-PY-007", """encoded = base64.b64encode(raw.encode("utf-8"))  # Basic""", CONSTRUCT_GROUP, "b64encode"),
			Yes("EX-PY-007", """encoded = base64.encodebytes(encode(raw))  # Basic""", CONSTRUCT_GROUP, "encodebytes"),
			No("EX-PY-007", """encoded = base64.b64encode(PAIR)"""),
			No("EX-PY-007", """signature = base64.b64encode(digest)  # Authorization header of a signed request"""),
			No("EX-PY-007", """encoded = base64.b64encode(raw.encode())  # Authorization"""),
			No("EX-PY-007", """encoded = base64.urlsafe_b64encode(PAIR)  # Basic"""),
			No("EX-PY-007", """decoded = base64.b64decode(PAIR)  # Basic"""),

			Yes("EX-PY-008", """value = aiohttp.encode_basic_auth("user", "secret")""", CONSTRUCT_GROUP, "encode_basic_auth", "secret"),
			Yes("EX-PY-008", """proxy_headers = {"Proxy-Authorization": encode_basic_auth("user", password="secret")}""", CONSTRUCT_GROUP, "encode_basic_auth", "secret"),
			Yes("EX-PY-008", """value = encode_basic_auth(login_name, secret_value)""", CONSTRUCT_GROUP, "encode_basic_auth", ""),
			No("EX-PY-008", """value = encode_basic_auth()"""),
			No("EX-PY-008", """value = encode_basic_authority("user", "secret")""")
		];
	}

	private static IEnumerable<Case> UserinfoCases()
	{
		return
		[
			Yes("EX-PY-011", """response = requests.get("https://user:secret@example.test/items")""", CONSTRUCT_GROUP, "get", "secret"),
			Yes("EX-PY-011", """async with session.get("https://user:secret@example.test/items") as response:""", CONSTRUCT_GROUP, "get", "secret"),
			Yes("EX-PY-011", """response = urllib.request.urlopen("https://user:secret@example.test/items")""", CONSTRUCT_GROUP, "urlopen", "secret"),
			Yes("EX-PY-011", """response = requests.get(url="https://user:secret@example.test/items", timeout=5)""", CONSTRUCT_GROUP, "get", "secret"),
			Yes("EX-PY-011", """response = requests.get(f"https://user:secret@example.test/items")""", CONSTRUCT_GROUP, "get", "secret"),
			Yes("EX-PY-011", """request = urllib.request.Request("http://user:secret@example.test/items", headers=extra)""", CONSTRUCT_GROUP, "Request", "secret"),
			No("EX-PY-011", """database = connect("scheme-one://user:secret@db.example.test/data")"""),
			No("EX-PY-011", """response = requests.get(url, proxies={"https": "http://user:secret@proxy.example.test:3128"})"""),
			No("EX-PY-011", """response = requests.get(url, proxy="http://user:secret@proxy.example.test:3128")"""),
			No("EX-PY-011", """response = requests.get("ftp://user:secret@example.test/file")"""),
			No("EX-PY-011", """response = requests.get("https://example.test/items")"""),
			No("EX-PY-011", """response = requests.get("https://user:<password>@example.test/items")"""),

			Yes("EX-PY-012", """BASE_URL = "https://user:secret@example.test/items" """.TrimEnd(), CONSTRUCT_GROUP, "BASE_URL", "secret"),
			Yes("EX-PY-012", """client = Client(base_url="https://user:secret@example.test/")""", CONSTRUCT_GROUP, "base_url", "secret"),
			Yes("EX-PY-012", """if (target := "https://user:secret@example.test/"):""", CONSTRUCT_GROUP, "target", "secret"),
			No("EX-PY-012", """proxy_url = "http://user:secret@proxy.example.test:3128" """.TrimEnd()),
			No("EX-PY-012", """HTTP_PROXY = "http://user:secret@proxy.example.test:3128" """.TrimEnd()),
			No("EX-PY-012", """broker_url = "scheme-two://user:secret@broker.example.test" """.TrimEnd()),
			No("EX-PY-012", """if target == "https://user:secret@example.test/":"""),
			No("EX-PY-012", """target = requests.get("https://user:secret@example.test/")""")
		];
	}

	private static IEnumerable<Case> ContentMarkerCases()
	{
		string tenFillerLines = string.Join(_LINE_FEED, Enumerable.Repeat("value = 1", _FILLER_LINES_BEFORE_A_TEN_LINE_BANNER));
		string nineFillerLines = string.Join(_LINE_FEED, Enumerable.Repeat("value = 1", _FILLER_LINES_INSIDE_A_TEN_LINE_BANNER));

		return
		[
			Yes("EX-PY-110", """class ClientTests(unittest.TestCase):"""),
			Yes("EX-PY-110", """class Case(TestCase):"""),
			Yes("EX-PY-110", """class AsyncCase(IsolatedAsyncioTestCase):"""),
			Yes("EX-PY-110", """class Mixed(Helper, unittest.TestCase):"""),
			No("EX-PY-110", """class Client(Base):"""),
			No("EX-PY-110", """# class Case(TestCase):"""),
			No("EX-PY-110", """class Case(MyTestCase):"""),
			No("EX-PY-110", """class TestCaseFactory(Base):"""),

			Yes("EX-PY-111", """# generated by example.test"""),
			Yes("EX-PY-111", Lines("#!/usr/bin/env python3", "# -*- coding: utf-8 -*-", "# auto-generated file")),
			Yes("EX-PY-111", Lines("# @generated", "value = 1")),
			Yes("EX-PY-111", Lines(nineFillerLines, "# do not edit this file by hand")),
			Yes("EX-PY-111", Lines("value = 1", "# Changes will be overwritten")),
			No("EX-PY-111", Lines(tenFillerLines, "# do not edit this file by hand")),
			No("EX-PY-111", Lines("value = 1", "value = 2", "value = 3", "# auto-generated file")),
			No("EX-PY-111", """x = 1  # generated by hand"""),
			No("EX-PY-111", Lines("\"\"\"This module is auto-generated.\"\"\"", "value = 1")),
			No("EX-PY-111", Lines("# a plain comment", "value = 1"))
		];
	}

	private static IEnumerable<Case> InteractiveFlowCases()
	{
		return
		[
			.. InteractivePromptCases(),
			.. InteractiveBrowserAndListenerCases(),
			.. InteractiveFormCases(),
			.. InteractiveLoginAndSessionCases()
		];
	}

	private static IEnumerable<Case> InteractivePromptCases()
	{
		return
		[
			Yes("EX-PY-120", """password = getpass.getpass("Password: ")""", ANCHOR_GROUP, "getpass"),
			Yes("EX-PY-120", Lines("from getpass import getpass", "secret_value = getpass()"), ANCHOR_GROUP, "getpass"),
			No("EX-PY-120", """user_name = getpass.getuser()"""),
			No("EX-PY-120", """value = getpassword("x")"""),

			Yes("EX-PY-121", """secret_value = input("Enter your password: ")""", ANCHOR_GROUP, "input"),
			Yes("EX-PY-121", """secret_value = input('Password? ')""", ANCHOR_GROUP, "input"),
			No("EX-PY-121", """name = input("Name: ")"""),
			No("EX-PY-121", """secret_value = ui.input("Enter your password: ")"""),
			No("EX-PY-121", """secret_value = input(prompt_text)"""),
			No("EX-PY-121", Lines("def input(prompt):", "\treturn prompt", "secret_value = input(\"Enter your password: \")")),
			No("EX-PY-121", Lines("input = ui.read_line", "secret_value = input(\"Enter your password: \")")),
		];
	}

	private static IEnumerable<Case> InteractiveBrowserAndListenerCases()
	{
		return
		[
			Yes("EX-PY-122", """webbrowser.open("https://example.test/authorize?client_id=abc")""", ANCHOR_GROUP, "open"),
			Yes("EX-PY-122", """webbrowser.open_new_tab(url)""", ANCHOR_GROUP, "open_new_tab"),
			Yes("EX-PY-122", """webbrowser.open_new(url)""", ANCHOR_GROUP, "open_new"),
			No("EX-PY-122", """browser = webbrowser.get("firefox")"""),
			No("EX-PY-122", """webbrowser.opener(url)"""),
			No("EX-PY-122", """browser.open(url)"""),

			Yes("EX-PY-123", Lines("server = HTTPServer((\"127.0.0.1\", 8080), Handler)", "params = parse_qs(query)", "authorization_code = params[\"code\"][0]"), ANCHOR_GROUP, "HTTPServer"),
			Yes("EX-PY-123", Lines("params = parse_qs(query)", "authorization_code = params[\"code\"][0]", "server = ThreadingHTTPServer((\"localhost\", 8080), Handler)"), ANCHOR_GROUP, "ThreadingHTTPServer"),
			No("EX-PY-123", Lines("server = HTTPServer((\"0.0.0.0\", 8080), Handler)", "authorization_code = parse_qs(query)[\"code\"]")),
			No("EX-PY-123", Lines("server = HTTPServer((\"127.0.0.1\", 9000), MockTokenHandler)", "server.serve_forever()")),
		];
	}

	private static IEnumerable<Case> InteractiveFormCases()
	{
		return
		[
			Yes("EX-PY-124", """secret_value = request.form["password"]""", ANCHOR_GROUP, "form"),
			Yes("EX-PY-124", """secret_value = request.POST.get("password")""", ANCHOR_GROUP, "POST"),
			Yes("EX-PY-124", """secret_value = request.form.get('passwd')""", ANCHOR_GROUP, "form"),
			No("EX-PY-124", """credentials = request.authorization"""),
			No("EX-PY-124", """note = request.form["comment"]"""),
			No("EX-PY-124", """label = request.form["password_label"]"""),

			Yes("EX-PY-125", """async def sign_in(password: Annotated[str, Form()]):""", ANCHOR_GROUP, "Form"),
			Yes("EX-PY-125", """def sign_in(password: str = Form(...)):""", ANCHOR_GROUP, "Form"),
			Yes("EX-PY-125", """def sign_in(form_data: OAuth2PasswordRequestForm = Depends()):""", ANCHOR_GROUP, "OAuth2PasswordRequestForm"),
			No("EX-PY-125", """def post_note(comment: str = Form(...)):"""),
			No("EX-PY-125", """security = HTTPBasic()"""),
		];
	}

	private static IEnumerable<Case> InteractiveLoginAndSessionCases()
	{
		return
		[
			Yes("EX-PY-126", """user = authenticate(request, username=name, password=secret_value)""", ANCHOR_GROUP, "authenticate"),
			Yes("EX-PY-126", """login(request, user)""", ANCHOR_GROUP, "login"),
			No("EX-PY-126", """server.login("user", "secret")"""),
			No("EX-PY-126", """user = authenticate(token)"""),
			No("EX-PY-126", """login(requested_name)"""),

			Yes("EX-PY-127", """value = request.session["user_id"]""", ANCHOR_GROUP, "session"),
			Yes("EX-PY-127", """from flask import Flask, request, session""", ANCHOR_GROUP, "session"),
			No("EX-PY-127", """session = requests.Session()"""),
			No("EX-PY-127", """session.headers["Accept"] = "text/plain" """.TrimEnd()),
			No("EX-PY-127", """from flask import Flask, request"""),
			No("EX-PY-127", """from requests import session_factory""")

		];
	}

	private static IEnumerable<Case> FixedFlowCases()
	{
		return
		[
			Yes("EX-PY-130", """value = os.environ["API_PASSWORD"]""", ANCHOR_GROUP, "environ"),
			Yes("EX-PY-130", """value = os.environ.get("SERVICE_SECRET")""", ANCHOR_GROUP, "environ"),
			Yes("EX-PY-130", """value = os.getenv("DB_PASS")""", ANCHOR_GROUP, "getenv"),
			Yes("EX-PY-130", """value = os.environ["SSH_PASSPHRASE"]""", ANCHOR_GROUP, "environ"),
			No("EX-PY-130", """value = os.environ["HOME"]"""),
			No("EX-PY-130", """value = os.getenv("LOG_LEVEL")"""),
			No("EX-PY-130", """value = os.getenv("BYPASS_MODE")"""),

			Yes("EX-PY-131", """value = config["service"]["password"]""", ANCHOR_GROUP, "config"),
			Yes("EX-PY-131", """value = config.get("service", "password")""", ANCHOR_GROUP, "config"),
			Yes("EX-PY-131", """value = settings.get("API_TOKEN")""", ANCHOR_GROUP, "settings"),
			No("EX-PY-131", """value = config["service"]["timeout"]"""),
			No("EX-PY-131", """value = configuration["password"]"""),
			Yes("EX-PY-131", """value = settings.SERVICE_PASSWORD""", ANCHOR_GROUP, "settings"),
			Yes("EX-PY-131", """value = settings.api_token""", ANCHOR_GROUP, "settings"),
			Yes("EX-PY-131", """value = config.db_pass""", ANCHOR_GROUP, "config"),
			Yes("EX-PY-131", """value = settings.SIGNING_PASSPHRASE""", ANCHOR_GROUP, "settings"),
			No("EX-PY-131", """value = settings.LOG_LEVEL"""),
			No("EX-PY-131", """value = settings.BYPASS_MODE"""),
			No("EX-PY-131", """value = options.SERVICE_PASSWORD"""),
			No("EX-PY-131", """value = configuration.password"""),
			No("EX-PY-131", """value = settings.reset_password(user_name)"""),

			Yes("EX-PY-132", """login_name, account, secret_value = netrc.netrc().authenticators(host)""", ANCHOR_GROUP, "authenticators"),
			No("EX-PY-132", """authenticators = []"""),
			No("EX-PY-132", """value = registry.authenticator(host)"""),

			Yes("EX-PY-133", """password = "exampleSecretValue" """.TrimEnd(), ANCHOR_GROUP, "password"),
			Yes("EX-PY-133", """api_token: str = 'exampleTokenValue'""", ANCHOR_GROUP, "api_token"),
			Yes("EX-PY-133", """client = connect(password="exampleSecretValue")""", ANCHOR_GROUP, "password"),
			Yes("EX-PY-133", """secret_value = f"exampleSecretValue" """.TrimEnd(), ANCHOR_GROUP, "secret_value"),
			No("EX-PY-133", """password_prompt = "Password: " """.TrimEnd()),
			No("EX-PY-133", """PASSWORD_LABEL = "Password" """.TrimEnd()),
			No("EX-PY-133", """password = os.environ["SERVICE_PASSWORD"]"""),
			No("EX-PY-133", """password = f"{secret_value}" """.TrimEnd()),
			No("EX-PY-133", """password = "" """.TrimEnd()),
			No("EX-PY-133", """token = "<token>" """.TrimEnd()),
			No("EX-PY-133", """if password == "exampleSecretValue":"""),
			No("EX-PY-133", """bypass = "yes" """.TrimEnd()),
			Yes("EX-PY-133", """def connect(host, secret="exampleSecretValue"):""", ANCHOR_GROUP, "secret"),
			Yes("EX-PY-133", """def connect(host, passphrase: str = "exampleValue", retries: int = 3):""", ANCHOR_GROUP, "passphrase"),
			Yes("EX-PY-133", Lines("class Settings:", "	passphrase: str = \"exampleValue\""), ANCHOR_GROUP, "passphrase"),
			Yes("EX-PY-133", """PASSPHRASE = "exampleValue" """.TrimEnd(), ANCHOR_GROUP, "PASSPHRASE"),
			No("EX-PY-133", """def connect(host, secret=None):"""),
			No("EX-PY-133", """def connect(host, passphrase: str = ""):"""),
			No("EX-PY-133", """def connect(host, secret_label="Secret"):"""),
			No("EX-PY-133", Lines("class Settings:", "	passphrase: str = os.environ[\"SERVICE_PASSPHRASE\"]")),
			No("EX-PY-133", """compass = "north" """.TrimEnd())
		];
	}
}
