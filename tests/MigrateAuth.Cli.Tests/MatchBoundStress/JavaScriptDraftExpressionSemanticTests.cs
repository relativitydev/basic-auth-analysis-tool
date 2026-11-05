using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SEMANTIC CHECKS OF THE `JavaScript` DRAFT EXPRESSIONS. A reshape for cost must keep what an expression means, so every expression has positive and negative samples here, each a shape the
/// catalog names: the forms of the call and the carriers of a header that a class reads, the near-misses it refuses, the demotion markers and the
/// flow signals with their near-misses. A case says which expression, the text, whether the expression matches it, and for the cases that name one, what a capture group holds.
/// The generated and the interpreted form of every expression must agree on every case. The window boundaries (the argument window, the definition window, the statement window) are
/// tested at the bound and one past it.
/// </summary>
public sealed class JavaScriptDraftExpressionSemanticTests
{
	private const string _LINE_FEED = "\n";
	private const string _FLOW_SIGNAL_PREFIX_120S = "EX-JS-12";
	private const string _FLOW_SIGNAL_PREFIX_130S = "EX-JS-13";
	private const string _SIGNAL_GROUP = "signal";
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _CONSTANT_DECLARATION_ID = "EX-JS-101";
	private const string _TEST_MARKER_ID = "EX-JS-110";
	private const string _BANNER_MARKER_ID = "EX-JS-111";
	private const string _FILLER_LINE = "log(x);\n";
	private const char _FILLER_UNIT = 'x';
	private const int _ABBREVIATION_MAXIMUM_LENGTH = 160;
	private const string _KEY_AFTER_THE_FILLER = " auth: { username: u, password: p } });";
	private const int _FILLER_UNITS_AT_THE_ARGUMENT_WINDOW_BOUND = 1_498;
	private const int _FILLER_UNITS_BEYOND_THE_ARGUMENT_WINDOW_BOUND = 1_500;
	private const int _FILLER_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND = 29;
	private const int _FILLER_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND = 30;
	private const int _FILLER_LINES_BETWEEN_A_DESCRIBE_AND_AN_IT_AT_THE_BOUND = 99;
	private const int _FILLER_LINES_BETWEEN_A_DESCRIBE_AND_AN_IT_BEYOND_THE_BOUND = 100;
	private const int _FILLER_LINES_BEFORE_A_BANNER_AT_THE_BOUND = 9;
	private const int _FILLER_LINES_BEFORE_A_BANNER_BEYOND_THE_BOUND = 10;
	private const int _CHARACTERS_BETWEEN_A_USE_WORD_AND_AN_ENCODER_AT_THE_BOUND = 280;
	private const int _CHARACTERS_BETWEEN_A_USE_WORD_AND_AN_ENCODER_BEYOND_THE_BOUND = 320;

	internal sealed record Case(string Id, string Text, bool Matches, string Group = "", string Value = "", int Start = 0);

	private static readonly ImmutableArray<Case> _CASES =
	[
		.. LexicalAndDeclarationCases(),
		.. CasesOfTheCallAdjacentClients(),
		.. CasesOfTheChainAndTheUrlForms(),
		.. CasesOfTheHeaderCarriers(),
		.. CasesOfTheEncoders(),
		.. CasesOfTheDefinitionBoundClasses(),
		.. CasesOfTheLowClasses(),
		.. CasesOfTheDemotionMarkers(),
		.. FlowCasesOfTheBrowserSignals(),
		.. FlowCasesOfTheServerAndTerminalSignals(),
		.. FlowCasesOfTheFixedSignals()
	];

	private static readonly ImmutableDictionary<string, Regex> _EXPRESSIONS = BuildExpressionIndex();

	/// <summary>Every case of the draft, for the checks that run the same samples against the shipped expressions.</summary>
	internal static ImmutableArray<Case> Cases => _CASES;

	[Fact]
	public void EveryDraftExpressionHasPositiveAndNegativeSamples()
	{
		IEnumerable<string> allIdentifiers = JavaScriptDraftExpressions.LexicalExpressions.Concat(JavaScriptDraftExpressions.MatchBoundExpressions).Select(entry => entry.Identifier);

		Assert.All(allIdentifiers, identifier =>
		{
			Assert.Contains(_CASES, candidate => candidate.Id == identifier && candidate.Matches);
			Assert.Contains(_CASES, candidate => candidate.Id == identifier && !candidate.Matches);
		});
	}

	[Fact]
	public void EveryDraftExpressionMatchesExactlyTheSamplesItShould()
	{
		List<string> wrong = [];

		foreach (Case sample in _CASES)
		{
			Match match = _EXPRESSIONS[sample.Id].Match(sample.Text, sample.Start);
			bool captured = sample.Group.Length == 0 || (match.Success && match.Groups[sample.Group].Value == sample.Value);

			if (match.Success != sample.Matches || !captured)
			{
				wrong.Add($"{sample.Id} on [{Escape(Abbreviate(sample.Text))}] expected {sample.Matches} {sample.Group}={sample.Value}, got {match.Success} {(sample.Group.Length == 0 ? string.Empty : match.Groups[sample.Group].Value)}");
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

			if (generated.Match(sample.Text, sample.Start).Success != interpreted.Match(sample.Text, sample.Start).Success)
			{
				disagreements.Add($"{sample.Id} on {Escape(Abbreviate(sample.Text))}");
			}
		}

		Assert.Empty(disagreements);
	}

	[Fact]
	public void EveryClassExpressionDeclaresTheConstructGroupAndEveryFlowSignalTheSignalGroup()
	{
		List<string> missing = [];

		foreach ((string identifier, Regex expression) in JavaScriptDraftExpressions.MatchBoundExpressions)
		{
			string anchorGroup = identifier.StartsWith(_FLOW_SIGNAL_PREFIX_120S, StringComparison.Ordinal) || identifier.StartsWith(_FLOW_SIGNAL_PREFIX_130S, StringComparison.Ordinal) ? _SIGNAL_GROUP : _CONSTRUCT_GROUP;
			bool hasNoAnchor = identifier is _CONSTANT_DECLARATION_ID or _TEST_MARKER_ID or _BANNER_MARKER_ID;

			if (!hasNoAnchor && !expression.GetGroupNames().Contains(anchorGroup))
			{
				missing.Add($"{identifier} lacks {anchorGroup}");
			}
		}

		Assert.Empty(missing);
	}

	[Fact]
	public void EveryStressShapeIsLegalForTheReaderAndFillsTheFileToJustUnderTheCeiling()
	{
		List<string> refused = [];

		foreach (MatchBoundStressShape shape in JavaScriptMatchBoundStressSet.Build())
		{
			bool tooLarge = Encoding.UTF8.GetByteCount(shape.Text) > ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES;
			bool lineTooLong = shape.Text.Split(_LINE_FEED).Any(line => line.Length > ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);

			if (tooLarge || lineTooLong || shape.Text.Contains('\r') || shape.FileExtension != JavaScriptMatchBoundStressSet.FILE_EXTENSION)
			{
				refused.Add($"{shape.Name}: tooLarge={tooLarge} lineTooLong={lineTooLong}");
			}
		}

		Assert.Empty(refused);
	}

	[Fact]
	public void EveryStressShapeNameIsUnique()
	{
		ImmutableArray<MatchBoundStressShape> shapes = JavaScriptMatchBoundStressSet.Build();

		Assert.Equal(shapes.Length, shapes.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
	}

	private static ImmutableDictionary<string, Regex> BuildExpressionIndex()
	{
		ImmutableDictionary<string, Regex>.Builder builder = ImmutableDictionary.CreateBuilder<string, Regex>(StringComparer.Ordinal);

		foreach ((string identifier, Regex expression) in JavaScriptDraftExpressions.LexicalExpressions.Concat(JavaScriptDraftExpressions.MatchBoundExpressions))
		{
			builder.Add(identifier, expression);
		}

		return builder.ToImmutable();
	}

	private static string Escape(string text)
	{
		return text.Replace(_LINE_FEED, "\\n", StringComparison.Ordinal);
	}

	private static string Abbreviate(string text)
	{
		return text.Length <= _ABBREVIATION_MAXIMUM_LENGTH ? text : string.Concat(text.AsSpan(0, _ABBREVIATION_MAXIMUM_LENGTH), "...");
	}

	private static string Filler(int lines)
	{
		return string.Concat(Enumerable.Repeat(_FILLER_LINE, lines));
	}

	private static Case Yes(string id, string text, string group = "", string value = "")
	{
		return new Case(id, text, true, group, value);
	}

	private static Case No(string id, string text)
	{
		return new Case(id, text, false);
	}

	private static ImmutableArray<Case> LexicalAndDeclarationCases()
	{
		return
		[
			Yes("EX-JS-100", "#!/usr/bin/env node"),
			Yes("EX-JS-100", "#!/usr/bin/node"),
			Yes("EX-JS-100", "#!/usr/bin/env -S node --no-warnings"),
			Yes("EX-JS-100", "#!/usr/local/bin/nodejs"),
			No("EX-JS-100", "#!/bin/bash"),
			No("EX-JS-100", "#!/usr/bin/env nodemon"),
			No("EX-JS-100", "#!/usr/bin/env python3"),
			No("EX-JS-100", "// #!/usr/bin/env node"),
			Yes("EX-JS-101", "const header = 'Basic ' + encoded;", "name", "header"),
			Yes("EX-JS-101", "const header = 'Basic ' + encoded;", "value", "Basic"),
			Yes("EX-JS-101", "let mode = \"Bearer abc\";", "value", "Bearer"),
			Yes("EX-JS-101", "token = `Bearer`;", "value", "Bearer"),
			Yes("EX-JS-101", "export const mode = 'basic';", "name", "mode"),
			Yes("EX-JS-101", "const a$b = `x$y`;", "name", "a$b"),
			No("EX-JS-101", "const header = `Bearer ${token}`;"),
			No("EX-JS-101", "const a = b;"),
			No("EX-JS-101", "if (ready) { mode = 'basic'; }"),
			No("EX-JS-101", "const object = { mode: 'basic' };"),
			No("EX-JS-101", "same === 'basic'")
		];
	}

	private static ImmutableArray<Case> CasesOfTheCallAdjacentClients()
	{
		string atTheBound = $"axios.get(url, {{{new string(_FILLER_UNIT, _FILLER_UNITS_AT_THE_ARGUMENT_WINDOW_BOUND)}{_KEY_AFTER_THE_FILLER}";
		string beyondTheBound = $"axios.get(url, {{{new string(_FILLER_UNIT, _FILLER_UNITS_BEYOND_THE_ARGUMENT_WINDOW_BOUND)}{_KEY_AFTER_THE_FILLER}";

		return
		[
			Yes("EX-JS-001", "axios.get(url, { auth: { username: userName, password: secret } });", "construct", "auth"),
			Yes("EX-JS-001", "const client = axios.create({ baseURL: host, auth: { username: userName, password: 'exampleSecret' } });", "credential", "exampleSecret"),
			Yes("EX-JS-001", "axios.defaults.auth = { username: userName, password: secret };", "construct", "auth"),
			Yes("EX-JS-001", "request.get(url, { auth: { user: userName, pass: secret, sendImmediately: true } });", "construct", "auth"),
			Yes("EX-JS-001", "axios({ url: target, auth: { username: u, password: p } });", "construct", "auth"),
			Yes("EX-JS-001", "axios.get(url, { auth: { username: u, password: 'changeme' } });", "credential", string.Empty),
			Yes("EX-JS-001", atTheBound, "construct", "auth"),
			No("EX-JS-001", beyondTheBound),
			No("EX-JS-001", "axios.get(url, { proxy: { host: h, port: 1, auth: { username: u, password: p } } });"),
			No("EX-JS-001", "request.get(url, { auth: { user: u, pass: p, sendImmediately: false } });"),
			No("EX-JS-001", "request.get(url, { auth: { bearer: token } });"),
			No("EX-JS-001", "mail.createTransport({ host: h, auth: { user: u, pass: p } });"),
			No("EX-JS-001", "axios.get(url, { headers: { Accept: 'x' } });"),
			No("EX-JS-001", "axios.get(url, { auth: { token: t } });"),
			Yes("EX-JS-008", "http.request({ host: hostName, path: '/path', auth: `${userName}:${secret}` }, handler);", "construct", "auth"),
			Yes("EX-JS-008", "https.get(url, { auth: 'exampleUser:exampleSecret' });", "credential", "exampleSecret"),
			Yes("EX-JS-008", "http.get({ auth: userAndPassword });", "construct", "auth"),
			No("EX-JS-008", "mail.send({ auth: 'x:y' });"),
			No("EX-JS-008", "axios.get(url, { auth: { username: u, password: p } });"),
			No("EX-JS-008", "http.request({ host: hostName }, handler);"),
			Yes("EX-JS-015", "const response = await got(url, { username: userName, password: secret });", "construct", "got"),
			Yes("EX-JS-015", "got.get(url, { username: u, password: 'exampleSecret' });", "credential", "exampleSecret"),
			No("EX-JS-015", "got(url, { username: u });"),
			No("EX-JS-015", "got(url, { headers: { Accept: 'x' } });"),
			No("EX-JS-015", "forgot(url, { username: u, password: p });"),
			Yes("EX-JS-014", "$.ajax({ url: apiUrl, username: userName, password: secret });", "construct", "ajax"),
			Yes("EX-JS-014", "jQuery.ajaxSetup({ username: u, password: 'exampleSecret' });", "construct", "ajaxSetup"),
			No("EX-JS-014", "$.ajax({ url: apiUrl });"),
			No("EX-JS-014", "$.get(url, { username: u, password: p });"),
			No("EX-JS-014", "$.ajax({ username: u });")
		];
	}

	private static ImmutableArray<Case> CasesOfTheChainAndTheUrlForms()
	{
		return
		[
			Yes("EX-JS-002", "const response = await superagent.get(url).auth(userName, secret);", "construct", "auth"),
			Yes("EX-JS-002", "superagent.get(url).auth(userName, 'exampleSecret');", "credential", "exampleSecret"),
			Yes("EX-JS-002", "request.get(url).auth(u, p, true);", "construct", "auth"),
			Yes("EX-JS-002", "superagent.post(url).set('X-Id', 'a').auth(u, p);", "construct", "auth"),
			No("EX-JS-002", "superagent.get(url).auth(credentialPair);"),
			No("EX-JS-002", "superagent.get(url).auth(token, { type: 'bearer' });"),
			No("EX-JS-002", "superagent.get(url).auth(u, p, { type: 'auto' });"),
			No("EX-JS-002", "request.get(url).auth(u, p, false);"),
			No("EX-JS-002", "sdk.auth(u, p);"),
			No("EX-JS-002", "request.get(url).auth(null, null, true, token);"),
			Yes("EX-JS-012", "const response = await superagent.get(url).auth(credentialPair);", "construct", "auth"),
			Yes("EX-JS-012", "superagent.get(url).auth(u, p, { type: 'auto' });", "construct", "auth"),
			No("EX-JS-012", "superagent.get(url).auth(u, p);"),
			No("EX-JS-012", "superagent.get(url).auth();"),
			No("EX-JS-012", "superagent.get(url).auth(token, { type: 'bearer' });"),
			No("EX-JS-012", "sdk.auth(session);"),
			Yes("EX-JS-016", "request.get(url, { auth: { user: userName, pass: secret, sendImmediately: false } });", "construct", "auth"),
			Yes("EX-JS-016", "request.get(url).auth(u, p, false);", "construct", "auth"),
			No("EX-JS-016", "request.get(url, { auth: { user: u, pass: p, sendImmediately: true } });"),
			No("EX-JS-016", "axios.get(url, { auth: { user: u, pass: p, sendImmediately: false } });"),
			No("EX-JS-016", "request.get(url).auth(u, p, true);"),
			Yes("EX-JS-003", "const request = http.get('https://exampleUser:exampleSecret@host.example/path', handler);", "credential", "exampleSecret"),
			Yes("EX-JS-003", "const response = await fetch('https://exampleUser:exampleSecret@host.example/path');", "construct", "fetch"),
			Yes("EX-JS-003", "axios.get(`https://u:p@h.example/x`);", "construct", "axios.get"),
			Yes("EX-JS-003", "const r = new Request('http://u:p@h.example');", "construct", "new Request"),
			No("EX-JS-003", "fetch('https://host.example/path');"),
			No("EX-JS-003", "mongodb.connect('mongodb://u:p@h.example');"),
			No("EX-JS-003", "got('postgres://u:p@h.example/db');"),
			No("EX-JS-003", "fetch('https://host.example/a@b');"),
			Yes("EX-JS-003", "fetch('https://u:changeme@h.example');", "credential", string.Empty),
			No("EX-JS-003", "fetch('https://h.example').then(a, b); const x = 'https://v:w';"),
			Yes("EX-JS-013", "const baseUrl = 'https://exampleUser:exampleSecret@host.example/path';", "construct", "baseUrl"),
			Yes("EX-JS-013", "const options = { baseURL: 'https://exampleUser:exampleSecret@host.example' };", "construct", "baseURL"),
			No("EX-JS-013", "const baseUrl = 'mongodb://exampleUser:exampleSecret@host.example';"),
			No("EX-JS-013", "const baseUrl = 'https://host.example/path';"),
			No("EX-JS-013", "const matches = value === 'https://u:p@h.example';")
		];
	}

	private static ImmutableArray<Case> CasesOfTheHeaderCarriers()
	{
		return
		[
			Yes("EX-JS-004", "await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });", "construct", "headers"),
			Yes("EX-JS-004", "xhr.setRequestHeader('Authorization', 'Basic ' + encoded);", "construct", "setRequestHeader"),
			Yes("EX-JS-004", "headers['Authorization'] = 'Basic ' + encoded;", "construct", "headers"),
			Yes("EX-JS-004", "res.setHeader('authorization', 'basic dXNlcjpwYXNzd29yZA==');", "credential", "dXNlcjpwYXNzd29yZA=="),
			Yes("EX-JS-004", "const h = new Headers({ Authorization: 'Basic abc' });", "construct", "Headers"),
			Yes("EX-JS-004", "const h = new Headers([['Authorization', 'Basic abc']]);", "construct", "Headers"),
			Yes("EX-JS-004", "headers.set('Authorization', `Basic ${t}`);", "construct", "set"),
			Yes("EX-JS-004", "request.append('authorization', 'BASIC ' + t);", "construct", "append"),
			Yes("EX-JS-004", "axios.defaults.headers.common['Authorization'] = 'Basic ' + t;", "construct", "headers"),
			Yes("EX-JS-004", "const o = { headers: { 'Authorization': 'Basic x' } };", "construct", "headers"),
			Yes("EX-JS-004", "headers.authorization = 'Basic ' + encoded;", "construct", "headers"),
			Yes("EX-JS-004", "const o = { headers: { Authorization: 'Basic changeme' } };", "credential", string.Empty),
			Yes("EX-JS-004", "const o = { headers: { Authorization: `Basic ${Buffer.from(`${userName}:${secret}`).toString('base64')}` } };", "construct", "headers"),
			No("EX-JS-004", "await fetch(url, { headers: { Authorization: `Bearer ${token}` } });"),
			No("EX-JS-004", "xhr.setRequestHeader('Authorization', 'Bearer ' + token);"),
			No("EX-JS-004", "if (req.headers.authorization === 'Basic abc') { allow(); }"),
			No("EX-JS-004", "const o = { headers: { 'Proxy-Authorization': 'Basic x' } };"),
			No("EX-JS-004", "const BASIC_PLAN = 1;"),
			No("EX-JS-004", "// Basic auth is deprecated"),
			No("EX-JS-004", "const o = { headers: { Authorization: encodedHeader } };"),
			No("EX-JS-004", "const value = headers.get('authorization');"),
			No("EX-JS-004", "const o = { menu: { title: 'Basic' } };"),
			Yes("EX-JS-004", "const o = { myheaders: { Authorization: 'Basic x' } };", "construct", "Authorization"),
			Yes("EX-JS-004", "function buildHeader(u, p) { return { Authorization: 'Basic ' + btoa(u + ':' + p) }; }", "construct", "Authorization"),
			Yes("EX-JS-004", "const h = { 'authorization': `Basic ${t}` };", "construct", "authorization"),
			Yes("EX-JS-004", "const h = { Authorization: basic`Basic ${t}` };", "construct", "Authorization"),
			Yes("EX-JS-004", "const h = { Authorization: basic`Basic ${t}` };", "credential", string.Empty),
			No("EX-JS-004", "const h = { Authorization: basic`Bearer ${t}` };"),
			No("EX-JS-004", "const h = { Authorization: `Bearer ${t}` };"),
			No("EX-JS-004", "const h = { 'X-Authorization': 'Basic x' };"),
			No("EX-JS-004", "const h = { Authorizations: 'Basic x' };"),
			No("EX-JS-004", "const h = { token: req.authorization ? 'Basic x' : y };"),
			No("EX-JS-004", "const h = { Authorization: build('Basic ') };"),
			Yes("EX-JS-017", "await fetch(url, { headers: { Authorization: encodedHeader } });", "scheme", "encodedHeader"),
			Yes("EX-JS-017", "headers['Authorization'] = encodedHeader;", "scheme", "encodedHeader"),
			Yes("EX-JS-017", "xhr.setRequestHeader('Authorization', token);", "scheme", "token"),
			Yes("EX-JS-017", "superagent.get(url).set('Authorization', header);", "scheme", "header"),
			No("EX-JS-017", "const o = { headers: { Authorization: 'Basic x' } };"),
			No("EX-JS-017", "const o = { headers: { Authorization: getToken() } };"),
			No("EX-JS-017", "const o = { headers: { Authorization: this.token } };"),
			No("EX-JS-017", "const o = { headers: { Accept: json } };"),
			No("EX-JS-017", "const o = { headers: { Authorization: 'Bearer x' } };")
		];
	}

	private static ImmutableArray<Case> CasesOfTheEncoders()
	{
		string wordAtTheBound = $"// Authorization {new string(_FILLER_UNIT, _CHARACTERS_BETWEEN_A_USE_WORD_AND_AN_ENCODER_AT_THE_BOUND)}\nconst t = btoa(a + ':' + b);";
		string wordBeyondTheBound = $"// Authorization {new string(_FILLER_UNIT, _CHARACTERS_BETWEEN_A_USE_WORD_AND_AN_ENCODER_BEYOND_THE_BOUND)}\nconst t = btoa(a + ':' + b);";

		return
		[
			Yes("EX-JS-005", "const header = 'Basic ' + btoa(userName + ':' + secret);", "construct", "btoa"),
			Yes("EX-JS-005", "const token = btoa(`${userName}:${secret}`); // sent as Authorization", "construct", "btoa"),
			Yes("EX-JS-005", "const header = 'Basic ' + btoa('exampleUser:exampleSecret');", "credential", "exampleSecret"),
			Yes("EX-JS-005", "const header = 'Basic ' + btoa([userName, secret].join(':'));", "construct", "btoa"),
			Yes("EX-JS-005", wordAtTheBound, "construct", "btoa"),
			No("EX-JS-005", wordBeyondTheBound),
			No("EX-JS-005", "const key = btoa(host + ':' + port);"),
			No("EX-JS-005", "const header = 'Basic ' + btoa(JSON.stringify(payload));"),
			No("EX-JS-005", "const header = 'Basic ' + atob(value);"),
			No("EX-JS-005", "const key = obfusbtoa(a + ':' + b); // Basic"),
			Yes("EX-JS-006", "const header = 'Basic ' + Buffer.from(`${userName}:${secret}`).toString('base64');", "construct", "Buffer"),
			Yes("EX-JS-006", "// Authorization\nconst legacy = new Buffer(userName + ':' + secret).toString('base64');", "construct", "Buffer"),
			Yes("EX-JS-006", "const header = 'Basic ' + Buffer.from('exampleUser:exampleSecret').toString('base64');", "credential", "exampleSecret"),
			Yes("EX-JS-006", "const header = 'Basic ' + Buffer.from(a + ':' + b, 'utf8').toString(\"base64\");", "construct", "Buffer"),
			No("EX-JS-006", "const cacheKey = Buffer.from(cacheText).toString('base64');"),
			No("EX-JS-006", "const header = 'Basic ' + Buffer.from(`${a}:${b}`).toString('base64url');"),
			No("EX-JS-006", "const k = Buffer.from(host + ':' + port).toString('base64');"),
			No("EX-JS-006", "const header = 'Basic ' + Buffer.from(encoded, 'base64').toString();"),
			No("EX-JS-006", "const bytes = Buffer.alloc(8); // Basic")
		];
	}

	private static ImmutableArray<Case> CasesOfTheDefinitionBoundClasses()
	{
		string bindingAtTheBound = $"const target = new URL(base);\n{Filler(_FILLER_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND)}target.password = secret;";
		string bindingBeyondTheBound = $"const target = new URL(base);\n{Filler(_FILLER_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}target.password = secret;";

		return
		[
			Yes("EX-JS-010", "const target = new URL(base);\ntarget.username = userName;\ntarget.password = secret;", "construct", "username"),
			Yes("EX-JS-010", "const target = new URL(base);\ntarget.password = 'exampleSecret';", "credential", "exampleSecret"),
			Yes("EX-JS-010", "let u = new URL(base);\nu.password = secret;", "construct", "password"),
			Yes("EX-JS-010", bindingAtTheBound, "construct", "password"),
			No("EX-JS-010", bindingBeyondTheBound),
			No("EX-JS-010", "user.password = value;"),
			No("EX-JS-010", "const target = new URL('mongodb://host.example');\ntarget.password = secret;"),
			No("EX-JS-010", "const target = new URL(base);\nif (target.password == secret) { ok(); }"),
			Yes("EX-JS-007", "function buildBasicHeader(userName, secret) {\n\treturn { Authorization: 'Basic ' + btoa(userName + ':' + secret) };\n}", "construct", "buildBasicHeader"),
			Yes("EX-JS-007", "const buildHeader = (user, pw) => 'Basic ' + btoa(user + ':' + pw);", "construct", "buildHeader"),
			Yes("EX-JS-007", "const buildHeader = user => 'Basic ' + btoa(user);", "construct", "buildHeader"),
			No("EX-JS-007", "function renderPage(title) {\n\treturn title.toUpperCase();\n}"),
			No("EX-JS-007", "function encode(a) { return btoa(b); }"),
			No("EX-JS-007", "const x = 1;")
		];
	}

	private static ImmutableArray<Case> CasesOfTheLowClasses()
	{
		return
		[
			Yes("EX-JS-009", "xhr.open('GET', url, true, userName, secret);", "construct", "open"),
			Yes("EX-JS-009", "xhr.open('GET', url, true, 'exampleUser', 'exampleSecret');", "credential", "exampleSecret"),
			Yes("EX-JS-009", "xhr.open('GET', 'https://exampleUser:exampleSecret@host.example/path');", "credential", "exampleSecret"),
			No("EX-JS-009", "xhr.open('GET', url, true);"),
			No("EX-JS-009", "xhr.open('GET', url, true, userName);"),
			No("EX-JS-009", "window.open(a, b, c, d, e);"),
			No("EX-JS-009", "document.open();"),
			No("EX-JS-009", "xhr.open('GET', 'https://host.example/path');"),
			Yes("EX-JS-011", "const transport = openMailTransport({ host: mailHost, port: mailPort, auth: { user: userName, pass: secret } });", "construct", "auth"),
			Yes("EX-JS-011", "db.connect({ auth: { username: u, password: 'exampleSecret' } });", "credential", "exampleSecret"),
			No("EX-JS-011", "const o = { auth: { token: t } };"),
			No("EX-JS-011", "const o = { auth: someVariable };"),
			No("EX-JS-011", "const o = { auth: { user: u } };")
		];
	}

	private static ImmutableArray<Case> CasesOfTheDemotionMarkers()
	{
		string describeThenItAtTheBound = $"describe('suite', () => {{\n{Filler(_FILLER_LINES_BETWEEN_A_DESCRIBE_AND_AN_IT_AT_THE_BOUND)}it('works', () => {{}});";
		string describeThenItBeyondTheBound = $"describe('suite', () => {{\n{Filler(_FILLER_LINES_BETWEEN_A_DESCRIBE_AND_AN_IT_BEYOND_THE_BOUND)}it('works', () => {{}});";
		string bannerAtTheBound = $"{Filler(_FILLER_LINES_BEFORE_A_BANNER_AT_THE_BOUND)}// do not edit this file";
		string bannerBeyondTheBound = $"{Filler(_FILLER_LINES_BEFORE_A_BANNER_BEYOND_THE_BOUND)}// do not edit this file";

		return
		[
			Yes("EX-JS-110", "describe('suite', () => {\n\tit('works', () => {});\n});"),
			Yes("EX-JS-110", "describe('suite', () => {\n\ttest('works', () => {});\n});"),
			Yes("EX-JS-110", "describe.skip('suite', () => {\n\tit.only('works', () => {});\n});"),
			Yes("EX-JS-110", describeThenItAtTheBound),
			No("EX-JS-110", describeThenItBeyondTheBound),
			No("EX-JS-110", "describe('suite', () => {});"),
			No("EX-JS-110", "it('works', () => {});"),
			No("EX-JS-110", "describeAll('suite', () => {\n\tit('works', () => {});\n});"),
			Yes("EX-JS-111", "// auto-generated file\nconst x = 1;"),
			Yes("EX-JS-111", "#!/usr/bin/env node\n/* @generated */\nconst x = 1;"),
			Yes("EX-JS-111", "/* Generated by a tool */\nconst x = 1;"),
			Yes("EX-JS-111", "'use strict';\n// Do not edit this file\nconst x = 1;"),
			Yes("EX-JS-111", "/*\n * DO NOT MODIFY\n */"),
			Yes("EX-JS-111", bannerAtTheBound),
			No("EX-JS-111", bannerBeyondTheBound),
			No("EX-JS-111", "const a = 1;\nconst b = 2;\n// generated by hand"),
			No("EX-JS-111", "#!/usr/bin/env node\nconst a = 1;\n// generated by hand"),
			No("EX-JS-111", "const generatedBy = 1;"),
			No("EX-JS-111", "const x = 1; // do not edit")
		];
	}

	private static ImmutableArray<Case> FlowCasesOfTheBrowserSignals()
	{
		string declaredAtTheBound = $"function prompt(message) {{ return message; }}\n{Filler(_FILLER_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND)}const pw = prompt('Enter your password');";
		string declaredBeyondTheBound = $"function prompt(message) {{ return message; }}\n{Filler(_FILLER_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}const pw = prompt('Enter your password');";

		return
		[
			Yes("EX-JS-120", "const pw = document.getElementById('password').value;", "signal", "getElementById"),
			Yes("EX-JS-120", "const pw = document.querySelector('input[type=password]').value;", "signal", "querySelector"),
			Yes("EX-JS-120", "const pw = document.querySelector('#loginPassword').value;", "signal", "querySelector"),
			No("EX-JS-120", "const name = document.getElementById('username').value;"),
			No("EX-JS-120", "const field = document.getElementById('password');"),
			No("EX-JS-120", "const theme = document.getElementById('theme').value;"),
			Yes("EX-JS-121", "const pw = loginForm.elements.password.value;", "signal", "password"),
			Yes("EX-JS-121", "const pw = form.password.value;", "signal", "password"),
			Yes("EX-JS-121", "const pw = document.forms[0].elements['password'].value;", "signal", "password"),
			Yes("EX-JS-121", "const pw = new FormData(form).get('password');", "signal", "get"),
			Yes("EX-JS-121", "const pw = formData.get('password');", "signal", "get"),
			No("EX-JS-121", "const pw = document.password.value;"),
			No("EX-JS-121", "const name = form.username.value;"),
			No("EX-JS-121", "const pw = record.password.value;"),
			Yes("EX-JS-121", "const pw = loginForm.elements.namedItem('password').value;", "signal", "namedItem"),
			Yes("EX-JS-121", "const pw = document.forms[0].elements.namedItem(\"password\").value;", "signal", "namedItem"),
			No("EX-JS-121", "const name = form.elements.namedItem('username').value;"),
			No("EX-JS-121", "const field = form.elements.namedItem('password');"),
			No("EX-JS-121", "const pw = record.namedItem('password').value;"),
			Yes("EX-JS-122", "const answer = window.prompt('Enter a value');", "signal", "prompt"),
			Yes("EX-JS-122", "const pw = prompt('Please enter your password');", "signal", "prompt"),
			Yes("EX-JS-122", declaredBeyondTheBound, "signal", "prompt"),
			No("EX-JS-122", declaredAtTheBound),
			No("EX-JS-122", "const pw = ui.prompt('Enter your password');"),
			No("EX-JS-122", "const color = prompt('Choose a color');"),
			No("EX-JS-122", "function prompt(message) { return message; }\nconst pw = prompt('Enter your password');"),
			Yes("EX-JS-123", "const pw = localStorage.getItem('password');", "signal", "localStorage"),
			Yes("EX-JS-123", "const pw = sessionStorage['userPassword'];", "signal", "sessionStorage"),
			No("EX-JS-123", "const theme = localStorage.getItem('theme');"),
			No("EX-JS-123", "const t = localStorage.getItem('authToken');"),
			Yes("EX-JS-124", "xhr.open('GET', url, true, userName, secret);", "signal", "open"),
			No("EX-JS-124", "xhr.open('GET', url, true, userName);"),
			No("EX-JS-124", "xhr.open('GET', url, true);"),
			No("EX-JS-124", "window.open(a, b, c, d, e);"),
			Yes("EX-JS-125", "const html = '<input type=\"password\" name=\"pw\">';", "signal", "<input"),
			Yes("EX-JS-125", "const html = `<input id=x type=password>`;", "signal", "<input"),
			No("EX-JS-125", "const html = '<input type=\"text\" name=\"user\">';"),
			No("EX-JS-125", "const html = '<input type=\"password-hint\">';")
		];
	}

	private static ImmutableArray<Case> FlowCasesOfTheServerAndTerminalSignals()
	{
		return
		[
			Yes("EX-JS-126", "const pw = req.body.password;", "signal", "body"),
			Yes("EX-JS-126", "const pw = request.body['password'];", "signal", "body"),
			Yes("EX-JS-126", "const pw = ctx.request.body.userPassword;", "signal", "body"),
			Yes("EX-JS-126", "const { username, password } = req.body;", "signal", "body"),
			No("EX-JS-126", "const name = req.body.username;"),
			No("EX-JS-126", "const t = req.query.token;"),
			Yes("EX-JS-126", "const pw = new URLSearchParams(body).get('password');", "signal", "get"),
			Yes("EX-JS-126", $"const params = new URLSearchParams(chunks.join(''));{_LINE_FEED}const pw = params.get('password');", "signal", "get"),
			No("EX-JS-126", $"const params = new URLSearchParams(window.location.search);{_LINE_FEED}const pw = params.get('password');"),
			No("EX-JS-126", $"const params = new URLSearchParams(body);{_LINE_FEED}const name = params.get('username');"),
			No("EX-JS-126", $"const cache = new Map();{_LINE_FEED}const pw = cache.get('password');"),
			No("EX-JS-126", "const pw = new URLSearchParams(defaults).get('password');"),
			Yes("EX-JS-127", "rl._writeToOutput = function (text) { };", "signal", "_writeToOutput"),
			Yes("EX-JS-127", "process.stdin.setRawMode(true); // hidden input for the password", "signal", "setRawMode"),
			No("EX-JS-127", "process.stdin.setRawMode(true);"),
			No("EX-JS-127", "if (rl._writeToOutput === original) { restore(); }"),
			Yes("EX-JS-128", "const questions = [{ type: 'password', name: 'pw', message: 'Enter' }];", "signal", "type"),
			No("EX-JS-128", "input.type = 'text';"),
			No("EX-JS-128", "const q = { type: 'text' };"),
			No("EX-JS-128", "const q = { 'type': 'password' };"),
			Yes("EX-JS-129", "rl.question('Enter your password: ', callback);", "signal", "question"),
			Yes("EX-JS-129", "const answer = prompt('Passphrase?');", "signal", "prompt"),
			No("EX-JS-129", "throw new Error('Password must have eight characters');"),
			No("EX-JS-129", "res.status(400).send('Password too short');"),
			Yes("EX-JS-134", $"const rl = readline.createInterface({{ input: process.stdin, output: process.stdout }});{_LINE_FEED}rl.question('Enter your token: ', done);", "signal", "question"),
			Yes("EX-JS-134", $"const rl = readline.createInterface({{ input, output }});{_LINE_FEED}const ask = (text) => new Promise((resolve) => rl.question(text, resolve));", "signal", "question"),
			Yes("EX-JS-134", $"const rl = await readline.promises.createInterface({{ input, output }});{_LINE_FEED}const answer = await rl.question(label);", "signal", "question"),
			No("EX-JS-134", "rl.question('Enter your token: ', done);"),
			No("EX-JS-134", $"const rl = openLegacyPrompt();{_LINE_FEED}rl.question('Enter your token: ', done);"),
			No("EX-JS-134", $"const rl = readline.createInterface({{ input, output }});{_LINE_FEED}rl.close();"),
			Yes("EX-JS-135", "const mutableStdout = new Writable({ write(chunk, encoding, callback) { if (!this.muted) { process.stdout.write(chunk, encoding); } callback(); } });", "signal", "Writable"),
			Yes("EX-JS-135", "const sink = new Writable({ write: (chunk, encoding, callback) => { if (!isMuted) { out.write(chunk); } callback(); } });", "signal", "Writable"),
			No("EX-JS-135", "const sink = new Writable({ write(chunk, encoding, callback) { collected.push(chunk); callback(); } });"),
			No("EX-JS-135", "const sink = new Writable({ write(chunk, encoding, callback) { mutableBuffer.push(chunk); callback(); } });"),
			No("EX-JS-135", "const mutedFlag = true;"),
			No("EX-JS-135", "const sink = new Writable({ write(chunk, encoding, callback) { if (unmuted) { out.write(chunk); } callback(); } });"),
			No("EX-JS-135", "const sink = new Writable({ write(chunk, encoding, callback) { commuted.push(chunk); callback(); } });"),
			No("EX-JS-129", "rl.question('Your name: ', callback);")
		];
	}

	private static ImmutableArray<Case> FlowCasesOfTheFixedSignals()
	{
		return
		[
			Yes("EX-JS-130", "const secret = process.env.API_PASSWORD;", "name", "API_PASSWORD"),
			Yes("EX-JS-130", "const secret = process.env['DB_PASS'];", "name", "DB_PASS"),
			No("EX-JS-130", "const mode = process.env.NODE_ENV;"),
			No("EX-JS-130", "const port = process.env.PORT;"),
			No("EX-JS-130", "const skip = process.env.BYPASS;"),
			Yes("EX-JS-131", "const value = config.get('db.password');", "signal", "config"),
			Yes("EX-JS-131", "const value = config.apiPassword;", "name", "apiPassword"),
			Yes("EX-JS-131", "const value = nconf.get('secret');", "signal", "nconf"),
			No("EX-JS-131", "const port = config.get('port');"),
			No("EX-JS-131", "const port = config.port;"),
			Yes("EX-JS-132", "const out = await client.send(new GetSecretValueCommand({ SecretId: id }));", "signal", "GetSecretValueCommand"),
			Yes("EX-JS-132", "const out = await client.getSecretValue(params);", "signal", "getSecretValue"),
			Yes("EX-JS-132", "const out = await client.accessSecretVersion({ name });", "signal", "accessSecretVersion"),
			No("EX-JS-132", "const out = getSecretary(params);"),
			No("EX-JS-132", "const secret = 1;"),
			Yes("EX-JS-133", "const apiPassword = 'exampleSecretValue';", "signal", "apiPassword"),
			Yes("EX-JS-133", "const options = { password: 'exampleSecretValue' };", "signal", "password"),
			Yes("EX-JS-133", "config.password = `exampleSecretValue`;", "signal", "password"),
			Yes("EX-JS-133", "const apiPassword = 'changeme';", "signal", "apiPassword"),
			No("EX-JS-133", "fetch(url, { credentials: 'include' });"),
			No("EX-JS-133", "const o = { credentials: 'same-origin' };"),
			No("EX-JS-133", "const passwordLabel = 'Password';"),
			No("EX-JS-133", "const o = { password: '' };"),
			No("EX-JS-133", "const secret = process.env.SECRET;"),
			No("EX-JS-133", "const password = `${typed}`;"),
			No("EX-JS-133", "if (password === 'exampleSecretValue') { ok(); }")
		];
	}
}
