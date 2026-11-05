using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SEMANTIC CHECKS OF THE `Shell` DRAFT EXPRESSIONS. A reshape for cost must keep what an expression means, so every expression has positive and negative samples here,
/// each a shape the catalog names: the credential forms, the near-misses an expression refuses, the exclusion forms and the flow near-misses. A case says which expression,
/// the text, whether the expression matches it, and for the cases that name one, what a capture group holds. The generated and the interpreted form of every expression must
/// agree on every case.
/// </summary>
public sealed class ShellDraftExpressionSemanticTests
{
	private const string _LINE_FEED = "\n";
	private const string _FILLER_LINE = "echo x\n";
	private const int _LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND = 29;
	private const int _LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND = 30;
	private const int _LINES_BETWEEN_A_LOOP_AND_ITS_DONE_AT_THE_BOUND = 19;
	private const int _LINES_BETWEEN_A_LOOP_AND_ITS_DONE_BEYOND_THE_BOUND = 20;
	private const int _LINES_BEFORE_A_READ_BEYOND_THE_DIALECT_WINDOW = 25;
	private const int _WORDS_BEFORE_A_URL_WORD_AT_THE_LINE_BOUND = 88;
	private const int _WORDS_BEFORE_A_URL_WORD_BEYOND_THE_LINE_BOUND = 89;
	private const int _LINES_BETWEEN_A_GROUP_AND_ITS_REDIRECT_AT_THE_BOUND = 29;
	private const int _LINES_BETWEEN_A_GROUP_AND_ITS_REDIRECT_BEYOND_THE_BOUND = 30;

	internal sealed record Case(string Id, string Text, bool Matches, string Group = "", string Value = "", int Start = 0);

	private static readonly ImmutableArray<Case> _CASES =
	[
		.. LexicalCases(),
		.. ClassCasesOfTheCurlUserOption(),
		.. ClassCasesOfTheCurlPromptAndTheSharedRefusals(),
		.. ClassCasesOfTheHeaderTheWgetAndTheNetrcForms(),
		.. ClassCasesOfTheEncodedHeaders(),
		.. ClassCasesOfTheHeaderVariablesAndArrays(),
		.. ClassCasesOfTheUrlVariablesAndTheDemotionMarkers(),
		.. FlowCasesOfTheSilentAndTheStoppedRead(),
		.. FlowCasesOfTheCredentialNamedReadAndThePromptTools(),
		.. FlowCasesOfTheWordingTheExpansionAndTheStoredForms(),
		.. ClassCasesOfTheHttpieAndTheLowClients(),
		.. ClassCasesOfTheWgetExecuteAndTheHandBuiltRequests(),
		.. ClassCasesOfTheWrittenConfigurations(),
		.. ClassCasesOfTheAliasesAndTheAnyauth(),
		.. ClassCasesOfTheEncodedPairsLoopsAndOptionStrings()
	];

	private static readonly ImmutableDictionary<string, Regex> _EXPRESSIONS = BuildExpressionIndex();

	/// <summary>Every case of the draft, for the checks that run the same samples against the shipped expressions.</summary>
	internal static ImmutableArray<Case> Cases => _CASES;

	[Fact]
	public void EveryDraftExpressionHasPositiveAndNegativeSamples()
	{
		IEnumerable<string> allIdentifiers = ShellDraftExpressions.LexicalExpressions.Concat(ShellDraftExpressions.MatchBoundExpressions).Select(entry => entry.Identifier);

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
				wrong.Add($"{sample.Id} on [{Escape(sample.Text)}] expected {sample.Matches} {sample.Group}={sample.Value}, got {match.Success}");
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
				disagreements.Add($"{sample.Id} on {Escape(sample.Text)}");
			}
		}

		Assert.Empty(disagreements);
	}

	private static string Escape(string text)
	{
		return text.Replace(_LINE_FEED, "\\n", StringComparison.Ordinal);
	}

	private static string Filler(int lines)
	{
		StringBuilder builder = new();

		for (int line = 0; line < lines; line++)
		{
			builder.Append(_FILLER_LINE);
		}

		return builder.ToString();
	}

	private static ImmutableDictionary<string, Regex> BuildExpressionIndex()
	{
		return ShellDraftExpressions.LexicalExpressions
			.Concat(ShellDraftExpressions.MatchBoundExpressions)
			.ToImmutableDictionary(entry => entry.Identifier, entry => entry.Expression, StringComparer.Ordinal);
	}

	private static ImmutableArray<Case> LexicalCases()
	{
		return
		[
			new("EX-SH-01", "<<EOF", true, "terminator", "EOF"),
			new("EX-SH-01", "<< EOF", true, "terminator", "EOF"),
			new("EX-SH-01", "<<'EOF'", true, "terminator", "EOF"),
			new("EX-SH-01", "<<\"EOF\"", true, "terminator", "EOF"),
			new("EX-SH-01", "<<\\EOF", true, "terminator", "EOF"),
			new("EX-SH-01", "<<''", true, "terminator", ""),
			new("EX-SH-01", "cat <<EOF", true, "terminator", "EOF", 4),
			new("EX-SH-01", "cat <<EOF)", true, "terminator", "EOF", 4),
			new("EX-SH-01", "<<<EOF", false),
			new("EX-SH-01", "<<-EOF", false),
			new("EX-SH-01", "<<E\"O\"F", false),
			new("EX-SH-01", "<<E'O'F", false),
			new("EX-SH-01", "cat <<EOF", false),
			new("EX-SH-01", "ab <<EOF", false, Start: 1),
			new("EX-SH-02", "<<-EOF", true, "terminator", "EOF"),
			new("EX-SH-02", "<<- 'EOF'", true, "terminator", "EOF"),
			new("EX-SH-02", "cat <<-EOF", true, "terminator", "EOF", 4),
			new("EX-SH-02", "<<-<EOF", false),
			new("EX-SH-02", "<<EOF", false),
			new("EX-SH-02", "cat <<-EOF", false),
			new("EX-SH-03", "export TOKEN=abc", true, "name", "TOKEN"),
			new("EX-SH-03", "readonly -r NAME=\"value\"", true, "name", "NAME"),
			new("EX-SH-03", "NAME='x y'", true, "value", "x y"),
			new("EX-SH-03", "NAME=a;echo b", true, "value", "a"),
			new("EX-SH-03", "NAME=$(cmd)", false),
			new("EX-SH-03", "NAME=\"a$b\"", false),
			new("EX-SH-03", "echo NAME=1", false),
			new("EX-SH-04", "#!/bin/bash", true),
			new("EX-SH-04", "#!/usr/bin/env bash", true),
			new("EX-SH-04", "#!/usr/bin/env -S bash -e", true),
			new("EX-SH-04", "#!/bin/sh", true),
			new("EX-SH-04", "#!/usr/bin/zsh -f", true),
			new("EX-SH-04", "#!/usr/bin/python", false),
			new("EX-SH-04", "#!/bin/bashx", false),
			new("EX-SH-04", "#!/usr/bin/env pwsh", false),
			new("EX-SH-04", "# !/bin/bash", false)
		];
	}

	private static ImmutableArray<Case> ClassCasesOfTheCurlUserOption()
	{
		return
		[
			new("EX-SH-05", "curl -u admin:secretvalue https://h.test/x", true, "credential", "secretvalue"),
			new("EX-SH-05", "curl -s -u \"admin:secretvalue\" https://h.test/x", true, "credential", "secretvalue"),
			new("EX-SH-05", "curl --user admin:secretvalue https://h.test/x", true, "credential", "secretvalue"),
			new("EX-SH-05", "curl --user=admin:secretvalue https://h.test/x", true, "credential", "secretvalue"),
			new("EX-SH-05", "curl -su admin:secretvalue https://h.test/x", true, "credential", "secretvalue"),
			new("EX-SH-05", "curl -u \"$USER_NAME:$USER_SECRET\" https://h.test/x", true),
			new("EX-SH-05", "curl -u $CREDS https://h.test/x", true),
			new("EX-SH-05", "curl -u admin https://h.test/x", false),
			new("EX-SH-05", "curl --digest -u a:b https://h.test/x", false),
			new("EX-SH-05", "curl -u a:b ftp://h.test/f", false),
			new("EX-SH-05", "curl -u a:b https://h.test/x --ntlm", false),
			new("EX-SH-05", "mycurl -u a:b https://h.test/x", false),
			new("EX-SH-05", "curl-config -u a:b", false),
			new("EX-SH-05", "curl -H 'X: y' https://h.test/x", false),
			new("EX-SH-05", "curl x curl -u a:b https://h.test/x", true, "credential", "b"),
			new("EX-SH-05", "curl https://h.test/curl -u a:b", true, "credential", "b"),
			new("EX-SH-05", "curl.exe -u a:b \"$URL\"", true, "credential", "b"),
			new("EX-SH-05", "/c/tools/curl.exe -u a:b \"$URL\"", true, "credential", "b"),
			new("EX-SH-05", "curl.exe.bak -u a:b https://h.test/x", false),
			new("EX-SH-05", "curlimages/curl -u a:b https://h.test/x", false),
			new("EX-SH-05", "curl --aws-sigv4 \"aws:amz:us-east-1:s3\" --user \"$KEY:$SECRET\" \"$URL\"", false),
			new("EX-SH-05", "curl -u \"first last:$PW\" https://h.test/x", true),
			new("EX-SH-05", "curl --user 'first last:secret' https://h.test/x", true, "user", "first last"),
			new("EX-SH-05", "curl --user 'first last:secret' https://h.test/x", true, "credential", "secret"),
			new("EX-SH-05", "curl -u \"first last\" https://h.test/x", false),
			new("EX-SH-05", "FTP_URL=https://host.example/x" + _LINE_FEED + "curl -u a:b \"$FTP_URL\"", true),
			new("EX-SH-05", "FTP_URL=ftp://host.example/x" + _LINE_FEED + "curl -u a:b \"$FTP_URL\"", false),
			new("EX-SH-05", "export FTP_URL=\"sftp://host.example/x\"" + _LINE_FEED + "curl -u a:b ${FTP_URL}", false),
			new("EX-SH-05", $"FTP_URL=ftp://host.example/x{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND)}curl -u a:b \"$FTP_URL\"", false),
			new("EX-SH-05", $"FTP_URL=ftp://host.example/x{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}curl -u a:b \"$FTP_URL\"", true),
			new("EX-SH-05", "curl -u a:b \\" + _LINE_FEED + "ftp://h.test/f", false),
			new("EX-SH-05", "FTP_BASE=ftp://host.example/x" + _LINE_FEED + "curl -s -u a:b \"$FTP_BASE/\"", false),
			new("EX-SH-05", "FTP_BASE=\"ftp://host.example/x\"" + _LINE_FEED + "curl -s -u a:b -O \"$FTP_BASE/$name\" && echo ok", false),
			new("EX-SH-05", "export FTP_BASE=sftp://host.example/x" + _LINE_FEED + "curl -u a:b \"${FTP_BASE}/list\"", false),
			new("EX-SH-05", "FTP_BASE=ftp://host.example/x" + _LINE_FEED + "curl -u a:b $FTP_BASE/list", false),
			new("EX-SH-05", $"FTP_BASE=ftp://host.example/x{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}curl -u a:b \"$FTP_BASE/\"", true),
			new("EX-SH-05", $"FTP_BASE=ftp://host.example/x{_LINE_FEED}curl -u a:b {string.Concat(Enumerable.Repeat("x ", _WORDS_BEFORE_A_URL_WORD_BEYOND_THE_LINE_BOUND))}\"$FTP_BASE/\"", true),
			new("EX-SH-05", $"FTP_BASE=ftp://host.example/x{_LINE_FEED}curl -u a:b {string.Concat(Enumerable.Repeat("x ", _WORDS_BEFORE_A_URL_WORD_AT_THE_LINE_BOUND))}\"$FTP_BASE/\"", false),
			new("EX-SH-05", "API_BASE=https://host.example/x" + _LINE_FEED + "curl -u a:b \"$API_BASE/\"", true),
			new("EX-SH-05", "API_BASE=\"http://host.example/x\"" + _LINE_FEED + "curl -u a:b \"${API_BASE}/$name\"", true),
			new("EX-SH-05", "FTP_BASE=ftp://host.example/x" + _LINE_FEED + "API_BASE=https://host.example/x" + _LINE_FEED + "curl -u a:b \"$API_BASE/y\"", true)
		];
	}

	private static ImmutableArray<Case> ClassCasesOfTheCurlPromptAndTheSharedRefusals()
	{
		return
		[
			new("EX-SH-06", "curl -u admin https://h.test/x", true),
			new("EX-SH-06", "curl -u 'admin' https://h.test/x", true),
			new("EX-SH-06", "curl -u admin:pw https://h.test/x", false),
			new("EX-SH-06", "curl -u $U https://h.test/x", false),
			new("EX-SH-06", "curl --digest -u admin https://h.test/x", false),
			new("EX-SH-06", "curl -u \"first last\" https://h.test/x", true),
			new("EX-SH-06", "curl -u \"first last:$PW\" https://h.test/x", false),
			new("EX-SH-05", "curl -u a:b https://h.test/x wget --ntlm https://h.test/y", true, "credential", "b"),
			new("EX-SH-05", "wget --digest https://h.test/y curl -u a:b https://h.test/x", true, "credential", "b"),
			new("EX-SH-05", "curl -u a:b --anyauth https://h.test/x", false),
			new("EX-SH-43", "curl -u a:b --anyauth https://h.test/x wget --ntlm https://h.test/y", true),
			new("EX-SH-43", "wget --anyauth https://h.test/y curl -u a:b https://h.test/x", false),
			new("EX-SH-06", "curl --aws-sigv4 x -u admin https://h.test/x", false),
			new("EX-SH-06", "FTP_BASE=ftp://host.example/x" + _LINE_FEED + "curl -u admin \"$FTP_BASE/\"", false),
			new("EX-SH-06", "API_BASE=https://host.example/x" + _LINE_FEED + "curl -u admin \"$API_BASE/\"", true)
		];
	}

	private static ImmutableArray<Case> ClassCasesOfTheHeaderTheWgetAndTheNetrcForms()
	{
		return
		[
			new("EX-SH-07", "curl -H \"Authorization: Basic dXNlcjpwYXNz\" https://h.test/x", true, "credential", "dXNlcjpwYXNz"),
			new("EX-SH-07", "curl --header 'authorization: basic dXNlcjpwYXNz' https://h.test/x", true, "credential", "dXNlcjpwYXNz"),
			new("EX-SH-07", "wget --header=\"Authorization: Basic dXNlcjpwYXNz\" https://h.test/x", true, "credential", "dXNlcjpwYXNz"),
			new("EX-SH-07", "curl -H \"Authorization: Basic $ENCODED\" https://h.test/x", true),
			new("EX-SH-07", "curl -H \"Authorization: Bearer abcdefgh\" https://h.test/x", false),
			new("EX-SH-07", "curl -H \"Authorization: Basic ab\" https://h.test/x", false),
			new("EX-SH-07", "curl -H \"Accept: text/plain\" https://h.test/x", false),
			new("EX-SH-07", "curl --expand-header 'Authorization: Basic {{creds:b64}}' \"$URL\"", true),
			new("EX-SH-07", "curl --expand-header=\"Authorization: Basic dXNlcjpwYXNz\" \"$URL\"", true, "credential", "dXNlcjpwYXNz"),
			new("EX-SH-07", "curl --expand-header 'Authorization: Bearer {{t}}' \"$URL\"", false),
			new("EX-SH-08", "wget --auth-no-challenge --user=a --password=b https://h.test/x", true),
			new("EX-SH-08", "wget --auth-no-challenge https://h.test/x", false),
			new("EX-SH-08", "wget --user a --password b https://h.test/x", false),
			new("EX-SH-08", "wget --auth-no-challenge https://a:b@h.test/x", true),
			new("EX-SH-08", "wget -e auth_no_challenge=on --user=u --password=secret \"$URL\"", true, "credential", "secret"),
			new("EX-SH-08", "wget --auth-no-challenge \\" + _LINE_FEED + "\"https://u:p@host.example/x\"", true),
			new("EX-SH-08", "wget -e auth_no_challenge=off --user=u --password=p \"$URL\"", false),
			new("EX-SH-09", "wget --user a --password b https://h.test/x", true),
			new("EX-SH-09", "wget https://a:b@h.test/x", true),
			new("EX-SH-09", "wget https://h.test/x", false),
			new("EX-SH-09", "wget -e auth_no_challenge=off --user=u --password=p \"$URL\"", true),
			new("EX-SH-09", "wget.exe --user=u --password=p \"$URL\"", true),
			new("EX-SH-10", "curl https://admin:secretvalue@h.test/x", true),
			new("EX-SH-10", "curl -u a:b https://h.test/x", false),
			new("EX-SH-10", "curl https://h.test/x@y", false),
			new("EX-SH-10", "curl --digest https://a:b@h.test/x", false),
			new("EX-SH-10", "curl x curl https://admin:pw@h.test/x", true),
			new("EX-SH-10", "curl -sS \\" + _LINE_FEED + "\"https://u:p@host.example/x\"", true),
			new("EX-SH-10", "curl --aws-sigv4 x \"https://u:p@host.example/x\"", false),
			new("EX-SH-10", "curl.exe https://u:p@h.test/x", true),
			new("EX-SH-11", "curl -n https://h.test/x", true),
			new("EX-SH-11", "curl --netrc https://h.test/x", true),
			new("EX-SH-11", "curl --netrc-file f https://h.test/x", true),
			new("EX-SH-11", "curl -sSn https://h.test/x", true),
			new("EX-SH-11", "curl -nope https://h.test/x", false),
			new("EX-SH-11", "curl -N https://h.test/x", false)
		];
	}

	private static ImmutableArray<Case> ClassCasesOfTheEncodedHeaders()
	{
		return
		[
			new("EX-SH-12", "curl -H \"Authorization: Basic $(printf '%s:%s' \"$U\" \"$P\" | tr -d '\\n' | base64)\" \"$URL\"", true),
			new("EX-SH-12", "ENC=$(echo -n \"$U:$P\" | openssl base64 -A)" + _LINE_FEED + "curl -H \"Authorization: Basic $ENC\" \"$URL\"", true),
			new("EX-SH-12", "ENC=$(base64 <<< \"$U:$P\")" + _LINE_FEED + "wget --header=\"Authorization: Basic $ENC\" \"$URL\"", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(python3 -c 'import base64; print(base64.b64encode(b\"u:p\").decode())')\" \"$URL\"", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(printf '%s' \"user:pass\" | base64)\" https://h.test/x", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(echo -n user:pass | base64)\" https://h.test/x", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(echo \"$USER:$PASS\" | openssl base64)\" https://h.test/x", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(echo \"$U:$P\" | tr a b | tr c d | base64)\" \"$URL\"", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(echo -n \"$U:$P\" | gbase64)\" \"$URL\"", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(echo -n \"$U:$P\" | basenc --base64)\" \"$URL\"", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(echo -n \"$U:$P\" | openssl enc -base64)\" \"$URL\"", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(printf '%s:%s' \"$U\" \"$P\" | python3 -c 'import sys,base64; print(base64.b64encode(sys.stdin.buffer.read()))')\" \"$URL\"", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic `echo -n \"$U:$P\" | base64`\" \"$URL\"", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(base64 <<< \"$U:$P\")\" \"$URL\"", true),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(echo -n \"$SERVER:$PASSWORD\" | base64)\" \"$URL\"", true),
			new("EX-SH-12", $"ENC=$(echo -n \"$U:$P\" | base64){_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND)}curl -H \"Authorization: Basic $ENC\" \"$URL\"", true),
			new("EX-SH-12", "printf '%s:%s' \"$1\" \"$2\" | base64", false),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(printf '%s:%s' \"$HOST\" \"$PORT\" | base64)\" \"$URL\"", false),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(printf 'a:8080' | base64)\" \"$URL\"", false),
			new("EX-SH-12", "curl -H \"Authorization: Basic $(echo -n \"a:b\" | base64 -d)\" \"$URL\"", false),
			new("EX-SH-12", "curl -H \"x: $(base64 -d <<< \"$U:$P\")\" \"$URL\"", false),
			new("EX-SH-12", "curl -H \"x: $(echo hello | base64)\" \"$URL\"", false),
			new("EX-SH-12", "curl -H \"x: $(echo -n \"$SERVER:$VALUE\" | base64)\" \"$URL\"", false),
			new("EX-SH-12", "ENC=$(echo -n \"$U:$P\" | base64)" + _LINE_FEED + "echo done", false),
			new("EX-SH-12", $"ENC=$(echo -n \"$U:$P\" | base64){_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}curl -H \"Authorization: Basic $ENC\" \"$URL\"", false)
		];
	}

	private static ImmutableArray<Case> ClassCasesOfTheHeaderVariablesAndArrays()
	{
		return
		[
			new("EX-SH-13", "AUTH=\"Basic dXNlcjpw\"" + _LINE_FEED + "curl -H \"Authorization: $AUTH\" https://h.test/x", true),
			new("EX-SH-13", "AUTH=\"Basic dXNlcjpw\"" + _LINE_FEED + "echo one" + _LINE_FEED + "curl -H \"Authorization: $AUTH\" https://h.test/x", true),
			new("EX-SH-13", "AUTH=\"Basic ${E}\"" + _LINE_FEED + "curl -H \"Authorization: ${AUTH}\" \"$URL\"", true),
			new("EX-SH-13", "HEADER=\"Authorization: Basic ${E}\"" + _LINE_FEED + "curl -sS -H \"${HEADER}\" \"$URL\"", true),
			new("EX-SH-13", "HEADER=\"authorization: basic $(make_token)\"" + _LINE_FEED + "curl -H $HEADER \"$URL\"", true),
			new("EX-SH-13", "HEADER=\"Authorization: Basic dXNlcjpw\"" + _LINE_FEED + "wget --header=\"$HEADER\" \"$URL\"", true),
			new("EX-SH-13", "HEADER=\"Authorization: Basic dXNlcjpw\"" + _LINE_FEED + "curl --expand-header \"$HEADER\" \"$URL\"", true),
			new("EX-SH-13", "SCHEME=Basic" + _LINE_FEED + "curl -H \"Authorization: $SCHEME $TOKEN\" \"$URL\"", true),
			new("EX-SH-13", $"AUTH=\"Basic dXNlcjpw\"{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND)}curl -H \"Authorization: $AUTH\" https://h.test/x", true),
			new("EX-SH-13", "AUTH=\"Basic dXNlcjpw\"" + _LINE_FEED + "curl -H \"Accept: $AUTH\" https://h.test/x", false),
			new("EX-SH-13", "AUTH=\"Bearer ${T}\"" + _LINE_FEED + "curl -H \"Authorization: ${AUTH}\" \"$URL\"", false),
			new("EX-SH-13", "HEADER=\"Authorization: Bearer ${T}\"" + _LINE_FEED + "curl -H \"$HEADER\" \"$URL\"", false),
			new("EX-SH-13", "HEADER=\"Authorization: Basic ${E}\"" + _LINE_FEED + "curl -H @\"$FILE\" \"$URL\"", false),
			new("EX-SH-13", $"AUTH=\"Basic dXNlcjpw\"{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}curl -H \"Authorization: $AUTH\" https://h.test/x", false),
			new("EX-SH-14", "ARGS=(-u admin:pw https://h.test/x)" + _LINE_FEED + "curl \"${ARGS[@]}\"", true),
			new("EX-SH-14", "ARGS=(--user=a:b)" + _LINE_FEED + "wget \"${ARGS[@]}\"", true),
			new("EX-SH-14", "options=(-sS)" + _LINE_FEED + "options+=(-u \"$U:$P\")" + _LINE_FEED + "curl \"${options[@]}\" \"$URL\"", true),
			new("EX-SH-14", "declare -A h=([auth]=\"-u $U:$P\")" + _LINE_FEED + "curl ${h[auth]} \"$URL\"", true),
			new("EX-SH-14", "opts[auth]=\"-u $U:$P\"" + _LINE_FEED + "curl \"${opts[auth]}\" \"$URL\"", true),
			new("EX-SH-14", "a=(-a \"$U:$P\")" + _LINE_FEED + "http \"${a[@]}\" \"$URL\"", true),
			new("EX-SH-14", "a=(--http-passwd \"$P\")" + _LINE_FEED + "aria2c \"${a[*]}\" \"$URL\"", true),
			new("EX-SH-14", $"ARGS=(-u admin:pw){_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND)}curl \"${{ARGS[@]}}\"", true),
			new("EX-SH-14", "hdr=(-H \"Authorization: Basic $TOKEN\")" + _LINE_FEED + "curl \"${hdr[@]}\" \"$URL\"", true),
			new("EX-SH-14", "hdr+=(--header=\"Authorization: basic abc\")" + _LINE_FEED + "wget \"${hdr[@]}\"", true),
			new("EX-SH-14", "local -a hdr=()" + _LINE_FEED + "hdr+=(-H \"${key}: ${value}\")" + _LINE_FEED + "hdr+=(-H \"Authorization: ${auth}\")" + _LINE_FEED + "curl \"${hdr[@]}\" \"$URL\"", false),
			new("EX-SH-14", "hdr=(-H 'Accept: application/json')" + _LINE_FEED + "curl \"${hdr[@]}\" \"$URL\"", false),
			new("EX-SH-14", "ARGS=(-s https://h.test/x)" + _LINE_FEED + "curl \"${ARGS[@]}\"", false),
			new("EX-SH-14", $"ARGS=(-u admin:pw){_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}curl \"${{ARGS[@]}}\"", false),
			new("EX-SH-14", "declare -n ref=options" + _LINE_FEED + "curl \"${ref[@]}\"", false)
		];
	}

	private static ImmutableArray<Case> ClassCasesOfTheUrlVariablesAndTheDemotionMarkers()
	{
		return
		[
			new("EX-SH-15", "API_URL=\"https://${U}:${P}@host.example/api\"" + _LINE_FEED + "curl -sS \"${API_URL}/x\"", true),
			new("EX-SH-15", "URL=\"https://admin:pw@h.test/api\"" + _LINE_FEED + "curl -sS \"$URL\"", true, "credential", "pw"),
			new("EX-SH-15", "export REPO=https://user:tok@h.test/x.git" + _LINE_FEED + "wget \"$REPO\"", true, "credential", "tok"),
			new("EX-SH-15", $"URL=\"https://admin:pw@h.test/api\"{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND)}curl -sS \"$URL\"", true),
			new("EX-SH-15", "API_URL=\"https://${U}:${P}@host.example/api\"" + _LINE_FEED + "git clone \"$API_URL\"", false),
			new("EX-SH-15", "URL=\"https://admin:pw@h.test/api\"", false),
			new("EX-SH-15", "URL=\"https://h.test/api\"" + _LINE_FEED + "curl \"$URL\"", false),
			new("EX-SH-15", $"URL=\"https://admin:pw@h.test/api\"{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}curl -sS \"$URL\"", false),
			new("EX-SH-16", "function basicAuthHeader() {", true, "construct", "basicAuthHeader"),
			new("EX-SH-16", "get_basic_auth_header() {", true, "construct", "get_basic_auth_header"),
			new("EX-SH-16", "function build() {", false),
			new("EX-SH-16", "basic() {", false),
			new("EX-SH-17", "#!/bin/sh" + _LINE_FEED + "# This file is auto-generated" + _LINE_FEED + "echo x", true),
			new("EX-SH-17", "# Generated by a tool" + _LINE_FEED + "echo x", true),
			new("EX-SH-17", "# This file was auto-generated", true),
			new("EX-SH-17", "#!/bin/sh" + _LINE_FEED + "# Generated by build.sh", true),
			new("EX-SH-17", "1" + _LINE_FEED + "2" + _LINE_FEED + "3" + _LINE_FEED + "4" + _LINE_FEED + "5" + _LINE_FEED + "# DO NOT EDIT - changes will be overwritten", true),
			new("EX-SH-17", "echo auto-generated", false),
			new("EX-SH-17", "1" + _LINE_FEED + "2" + _LINE_FEED + "3" + _LINE_FEED + "4" + _LINE_FEED + "5" + _LINE_FEED + "6" + _LINE_FEED + "7" + _LINE_FEED + "8" + _LINE_FEED + "9" + _LINE_FEED + "10" + _LINE_FEED + "# auto-generated", false),
			new("EX-SH-17", "#!/bin/sh" + _LINE_FEED + "echo hi" + _LINE_FEED + "# Report generated by this script", false),
			new("EX-SH-17", "echo \"generated by\"", false),
			new("EX-SH-17", "#!/bin/sh" + _LINE_FEED + "echo hi" + _LINE_FEED + "echo there" + _LINE_FEED + "# generated by hand", false),
			new("EX-SH-18", "source shunit2", true),
			new("EX-SH-18", ". ./lib/assert.sh", true),
			new("EX-SH-18", "source /usr/lib/bats-assert/load.bash", true),
			new("EX-SH-18", "source ./common.sh", false),
			new("EX-SH-19", "Describe 'a'" + _LINE_FEED + "Describe 'b'" + _LINE_FEED + "  It 'c'", true),
			new("EX-SH-19", "Describe 'x'" + _LINE_FEED + "  It 'y'", true),
			new("EX-SH-19", "Describe 'x'" + _LINE_FEED + "  echo y", false)
		];
	}

	private static ImmutableArray<Case> FlowCasesOfTheSilentAndTheStoppedRead()
	{
		return
		[
			new("EX-SH-20", "read -s PASS", true),
			new("EX-SH-20", "read -rs", true),
			new("EX-SH-20", "read -rs PASS", true),
			new("EX-SH-20", "read -r -s -p \"x: \" PASS", true),
			new("EX-SH-20", "read -s PASS </dev/tty", true),
			new("EX-SH-20", "read -rs -p \"Password: \" API_PASSWORD", true, "name", "API_PASSWORD"),
			new("EX-SH-20", "#!/bin/bash" + _LINE_FEED + "read -rs P", true),
			new("EX-SH-20", "read -rs P" + _LINE_FEED + "curl -u \"$U:$P\" \"$URL\"", true),
			new("EX-SH-20", "sudo -S true" + _LINE_FEED + "read -s PASS", true),
			new("EX-SH-20", "read -s P" + _LINE_FEED + "P2=$P" + _LINE_FEED + "printf '%s\\n' \"$P2\" | sudo -S ./setup.sh", true),
			new("EX-SH-20", $"#!/bin/ksh{_LINE_FEED}{Filler(_LINES_BEFORE_A_READ_BEYOND_THE_DIALECT_WINDOW)}read -rs P", true),
			new("EX-SH-20", "read -s PASS < file", false),
			new("EX-SH-20", "read -r line", false),
			new("EX-SH-20", "echo x | sshpass -p y read -s PASS", true),
			new("EX-SH-20", "#!/bin/ksh" + _LINE_FEED + "read -rs P", false),
			new("EX-SH-20", "#!/bin/mksh" + _LINE_FEED + "read -rs P", false),
			new("EX-SH-20", "#!/usr/bin/env ksh93" + _LINE_FEED + "read -s P", false),
			new("EX-SH-20", "read -rs P | sudo -S ./setup.sh", false),
			new("EX-SH-20", "read -s P | sudo -S x", false),
			new("EX-SH-20", "read -rs P" + _LINE_FEED + "printf '%s\\n' \"$P\" | sudo -S ./setup.sh", false),
			new("EX-SH-20", "read -rs P" + _LINE_FEED + "sudo -S ./x <<< \"$P\"", false),
			new("EX-SH-21", "stty -echo" + _LINE_FEED + "read PASS", true),
			new("EX-SH-21", "stty -echo" + _LINE_FEED + "echo a" + _LINE_FEED + "read PASS", true),
			new("EX-SH-21", "stty -F /dev/x -echo" + _LINE_FEED + "read P", true),
			new("EX-SH-21", "stty sane" + _LINE_FEED + "read PASS", false),
			new("EX-SH-21", "stty -echo" + _LINE_FEED + "1" + _LINE_FEED + "2" + _LINE_FEED + "3" + _LINE_FEED + "4" + _LINE_FEED + "5" + _LINE_FEED + "6" + _LINE_FEED + "read PASS", false)
		];
	}

	private static ImmutableArray<Case> FlowCasesOfTheCredentialNamedReadAndThePromptTools()
	{
		return
		[
			new("EX-SH-22", "read PASSWORD", true, "name", "PASSWORD"),
			new("EX-SH-22", "read -r TOKEN", true, "name", "TOKEN"),
			new("EX-SH-22", "read \"pass?Enter: \"", true),
			new("EX-SH-22", "read -r PASS", true),
			new("EX-SH-22", "while read -r PASS; do" + _LINE_FEED + "echo x" + _LINE_FEED + "done </dev/tty", true),
			new("EX-SH-22", $"while read -r PASS; do{_LINE_FEED}{Filler(_LINES_BETWEEN_A_LOOP_AND_ITS_DONE_BEYOND_THE_BOUND)}done < \"$F\"", true),
			new("EX-SH-22", "read -r PASS" + _LINE_FEED + "curl -u \"$U:$PASS\" \"$URL\"", true),
			new("EX-SH-22", "read LINE", false),
			new("EX-SH-22", "read PASSWORD < file", false),
			new("EX-SH-22", "while IFS= read -r PASS; do" + _LINE_FEED + "echo x" + _LINE_FEED + "done < \"$F\"", false),
			new("EX-SH-22", $"while read -r PASS; do{_LINE_FEED}{Filler(_LINES_BETWEEN_A_LOOP_AND_ITS_DONE_AT_THE_BOUND)}done < \"$F\"", false),
			new("EX-SH-22", "cat f | while read -r PASS; do echo x; done", false),
			new("EX-SH-22", "printf '%s\\n' x | read -r PASS", false),
			new("EX-SH-22", "read -r PASS" + _LINE_FEED + "printf '%s\\n' \"$PASS\" | sudo -S ./x", false),
			new("EX-SH-23", "systemd-ask-password \"Pass\"", true),
			new("EX-SH-23", "whiptail --passwordbox \"x\" 8 40", true),
			new("EX-SH-23", "zenity --password", true),
			new("EX-SH-23", "kdialog --password x", true),
			new("EX-SH-23", "dialog --passwordbox x 8 40", true),
			new("EX-SH-23", "whiptail --yesno x 8 40", false),
			new("EX-SH-23", "zenity --info", false),
			new("EX-SH-23", "sudo -S ./x <<< \"$(systemd-ask-password p)\"", false),
			new("EX-SH-24", "osascript -e 'display dialog \"x\" with hidden answer'", true),
			new("EX-SH-24", "osascript -e 'display dialog \"x\"'", false),
			new("EX-SH-25", "curl -u admin https://h.test/x", true),
			new("EX-SH-25", "alias c=curl" + _LINE_FEED + "c -u admin https://h.test/x", true),
			new("EX-SH-25", "CURL=curl" + _LINE_FEED + "$CURL -u admin \"$URL\"", true),
			new("EX-SH-25", "curl -u admin:pw https://h.test/x", false),
			new("EX-SH-25", "CURL=curl" + _LINE_FEED + "$CURL -u admin:pw \"$URL\"", false),
			new("EX-SH-25", "curl -u \"first last\" https://h.test/x", true),
			new("EX-SH-25", "curl -u \"$who\" https://h.test/x", false),
			new("EX-SH-25", "FTP_BASE=ftp://host.example/x" + _LINE_FEED + "curl -u admin \"$FTP_BASE/\"", false),
			new("EX-SH-25", "API_BASE=https://host.example/x" + _LINE_FEED + "curl -u admin \"$API_BASE/\"", true)
		];
	}

	private static ImmutableArray<Case> FlowCasesOfTheWordingTheExpansionAndTheStoredForms()
	{
		return
		[
			new("EX-SH-26", "wget --ask-password https://h.test/x", true),
			new("EX-SH-26", "wget https://h.test/x", false),
			new("EX-SH-27", "read -p \"Enter your password: \" X", true),
			new("EX-SH-27", "read -p \"Password: \" P", true),
			new("EX-SH-27", "#!/bin/bash" + _LINE_FEED + "read -p \"Password: \" P", true),
			new("EX-SH-27", "#!/bin/zsh" + _LINE_FEED + "read \"pw?Password: \"", true),
			new("EX-SH-27", "read 'var?Enter the passphrase: '", true),
			new("EX-SH-27", "echo \"Password:\"" + _LINE_FEED + "read X", true),
			new("EX-SH-27", "echo \"Enter the password:\"" + _LINE_FEED + "read -s P", true),
			new("EX-SH-27", "echo \"Password:\"" + _LINE_FEED + "  true; read X", true),
			new("EX-SH-27", "echo \"Password:\"" + _LINE_FEED + "foo | read X", true),
			new("EX-SH-27", "read -p \"ssh password: \" P", true),
			new("EX-SH-27", "read -p \"Name: \" X", false),
			new("EX-SH-27", "#!/bin/zsh" + _LINE_FEED + "read -p \"Password: \" P", false),
			new("EX-SH-27", "#!/bin/ksh" + _LINE_FEED + "read -p \"Password: \" P", false),
			new("EX-SH-27", "#!/usr/bin/env mksh" + _LINE_FEED + "read -p \"Password: \" P", false),
			new("EX-SH-27", "read -p \"Password: \" P | sudo -S ./x", false),
			new("EX-SH-28", "curl -H \"X-Key: $API_TOKEN\" https://h.test/x", true),
			new("EX-SH-28", "wget https://h.test/x --header ${PASSWORD}", true),
			new("EX-SH-28", "curl ${VAR:?}", true),
			new("EX-SH-28", "curl -sS \\" + _LINE_FEED + "--user \"$API_USER:$API_PASSWORD\" \\" + _LINE_FEED + "\"$URL\"", true),
			new("EX-SH-28", "http -a \"$U:$API_TOKEN\" \"$HOST\"", true),
			new("EX-SH-28", "aria2c --http-passwd \"$API_PASSWORD\" \"$URL\"", true),
			new("EX-SH-28", "lwp-request -C \"$U:$SECRET\" \"$URL\"", true),
			new("EX-SH-28", "curl $URL", false),
			new("EX-SH-28", "echo \"$API_PASSWORD\"", false),
			new("EX-SH-28", "curl -s https://h.test/x" + _LINE_FEED + "-H \"$API_TOKEN\"", false),
			new("EX-SH-29", "security find-generic-password -s x", true),
			new("EX-SH-29", "pass show x", true),
			new("EX-SH-29", "vault kv get x", true),
			new("EX-SH-29", "passwd show x", false),
			new("EX-SH-29", "pass", false),
			new("EX-SH-30", "curl -n https://h.test/x", true),
			new("EX-SH-30", "curl --netrc-optional https://h.test/x", true),
			new("EX-SH-30", "curl -N https://h.test/x", false),
			new("EX-SH-31", "read PASSWORD < /run/secret", true),
			new("EX-SH-31", "read -r TOKEN < file", true),
			new("EX-SH-31", "read PASSWORD <<EOF", false),
			new("EX-SH-31", "read LINE < f", false)
		];
	}

	private static ImmutableArray<Case> ClassCasesOfTheHttpieAndTheLowClients()
	{
		return
		[
			new("EX-SH-32", "http -a admin:secret \"https://host.example/x\"", true, "credential", "secret"),
			new("EX-SH-32", "https --auth \"$U:$P\" GET \"$HOST/x\"", true),
			new("EX-SH-32", "http --auth-type=basic -a \"$u:$p\" \"$HOST\"", true),
			new("EX-SH-32", "http \"$HOST/x\" \"Authorization:Basic dXNlcjpwYXNz\"", true, "credential", "dXNlcjpwYXNz"),
			new("EX-SH-32", "http \"https://u:p@host.example/\"", true, "credential", "p"),
			new("EX-SH-32", "http -auser:pass \"$HOST\"", true, "credential", "pass"),
			new("EX-SH-32", "http -a admin \"$HOST\"", true),
			new("EX-SH-32", "http -A bearer -a \"$TOKEN\" \"$HOST\"", false),
			new("EX-SH-32", "http -A digest -a u:p \"$HOST\"", false),
			new("EX-SH-32", "http \"$HOST\" \"Authorization:Bearer $T\"", false),
			new("EX-SH-32", "curl \"http://host.example/x\"", false),
			new("EX-SH-32", "echo \"see http://host.example\"", false),
			new("EX-SH-32", "ls -a", false),
			new("EX-SH-33", "http -a admin \"$HOST\"", true),
			new("EX-SH-33", "https --auth=admin \"$HOST\"", true),
			new("EX-SH-33", "http -a \"$who\" \"$HOST\"", false),
			new("EX-SH-33", "http -a admin:secret \"$HOST\"", false),
			new("EX-SH-33", "http -A digest -a admin \"$HOST\"", false),
			new("EX-SH-34", "aria2c --http-user=u --http-passwd=secret \"$URL\"", true, "credential", "secret"),
			new("EX-SH-34", "aria2c --http-user \"$U\" --http-passwd \"$P\" \"$URL\"", true),
			new("EX-SH-34", "aria2c --http-auth-challenge=false --http-passwd=secret \"$URL\"", true),
			new("EX-SH-34", "aria2c --header=\"Authorization: Basic dXNlcjpwYXNz\" \"$URL\"", true, "credential", "dXNlcjpwYXNz"),
			new("EX-SH-34", "aria2c \"https://u:p@host.example/f\"", true, "credential", "p"),
			new("EX-SH-34", "aria2c --http-auth-challenge --http-passwd=p \"$URL\"", false),
			new("EX-SH-34", "aria2c --http-proxy-passwd=x \"$URL\"", false),
			new("EX-SH-34", "aria2c --header=\"Authorization: Bearer x\" \"$URL\"", false),
			new("EX-SH-34", "aria2c --http-user=u \"$URL\"", false),
			new("EX-SH-35", "lwp-request -C admin:secret \"$URL\"", true, "credential", "secret"),
			new("EX-SH-35", "lwp-request -m GET -C \"$U:$P\" \"$URL\"", true),
			new("EX-SH-35", "lwp-request -H \"Authorization: Basic dXNlcjpwYXNz\" \"$URL\"", true, "credential", "dXNlcjpwYXNz"),
			new("EX-SH-35", "aria2c --http-auth-challenge=true --http-user=u --http-passwd=secret \"$URL\"", true, "credential", "secret"),
			new("EX-SH-35", "lwp-request \"https://u:p@host.example/x\"", true, "credential", "p"),
			new("EX-SH-35", "lwp-request \"$URL\"", false),
			new("EX-SH-35", "lwp-request -m POST \"$URL\"", false),
			new("EX-SH-35", "aria2c --http-passwd=p \"$URL\"", false)
		];
	}

	private static ImmutableArray<Case> ClassCasesOfTheWgetExecuteAndTheHandBuiltRequests()
	{
		return
		[
			new("EX-SH-36", "wget --auth-no-challenge -e http_user=u -e http_password=secret \"$URL\"", true, "credential", "secret"),
			new("EX-SH-36", "wget -e \"auth_no_challenge=on\" -e \"http_password=${P}\" \"$URL\"", true),
			new("EX-SH-36", "wget --execute=http_password=secret --auth-no-challenge \"$URL\"", true, "credential", "secret"),
			new("EX-SH-36", "wget -e http_password=secret \"$URL\"", false),
			new("EX-SH-36", "wget -e robots=off --auth-no-challenge \"$URL\"", false),
			new("EX-SH-36", "wget -e http_proxy=http://proxy.example:3128 \"$URL\"", false),
			new("EX-SH-36", "wget --proxy-password=x \"$URL\"", false),
			new("EX-SH-37", "wget -e http_password=secret \"$URL\"", true, "credential", "secret"),
			new("EX-SH-37", "wget --execute \"http_password=${P}\" \"$URL\"", true),
			new("EX-SH-37", "wget -e robots=off \"$URL\"", false),
			new("EX-SH-37", "wget -e http_proxy=http://proxy.example:3128 \"$URL\"", false),
			new("EX-SH-37", "wget --proxy-password=x \"$URL\"", false),
			new("EX-SH-38", "exec 3<>/dev/tcp/host.example/80" + _LINE_FEED + "printf 'GET /x HTTP/1.1\\r\\nAuthorization: Basic dXNlcjpwYXNz\\r\\n\\r\\n' >&3", true, "credential", "dXNlcjpwYXNz"),
			new("EX-SH-38", "printf 'GET /x HTTP/1.1\\r\\nHost: h\\r\\nAuthorization: Basic %s\\r\\n\\r\\n' \"$e\" | nc host.example 80", true),
			new("EX-SH-38", "printf 'GET /x HTTP/1.1\\r\\nAuthorization: Basic dXNlcjpwYXNz\\r\\n\\r\\n' | openssl s_client -quiet -connect host.example:443", true, "credential", "dXNlcjpwYXNz"),
			new("EX-SH-38", "nc -l 8080 <<EOF" + _LINE_FEED + "HTTP/1.1 401 Unauthorized" + _LINE_FEED + "WWW-Authenticate: Basic realm=\"x\"" + _LINE_FEED + "EOF", false),
			new("EX-SH-38", "grep -i '^Authorization: Basic' request.log", false),
			new("EX-SH-38", "printf 'GET / HTTP/1.1\\r\\nHost: h\\r\\n\\r\\n' | nc host.example 80", false),
			new("EX-SH-38", "case \"$line\" in \"Authorization: Basic \"*) ;; esac", false),
			new("EX-SH-38", "exec 3<>/dev/tcp/h/80" + _LINE_FEED + "printf 'GET / HTTP/1.1\\r\\nAuthorization: Bearer x\\r\\n\\r\\n' >&3", false),
			new("EX-SH-38", "printf 'GET /x HTTP/1.1\\r\\nAuthorization: Basic dXNlcjpwYXNz\\r\\n\\r\\n' | nc -l 8080", false),
			new("EX-SH-38", "exec 3<>\"/dev/tcp/${host}/${port}\"" + _LINE_FEED + "printf '%s\\r\\n' \\" + _LINE_FEED + "\t\"GET ${path} HTTP/1.1\" \\" + _LINE_FEED + "\t\"Authorization: Basic ${creds}\" >&3", true, "construct", "<>"),
			new("EX-SH-38", "exec 3<> \"/dev/tcp/h/80\"" + _LINE_FEED + "printf 'GET /x HTTP/1.1\\r\\nAuthorization: Basic dXNlcjpwYXNz\\r\\n\\r\\n' >&3", true, "credential", "dXNlcjpwYXNz"),
			new("EX-SH-38", "exec 3<>'/dev/tcp/h/80'" + _LINE_FEED + "printf 'GET /x HTTP/1.1\\r\\nAuthorization: Basic dXNlcjpwYXNz\\r\\n\\r\\n' >&3", true, "construct", "<>"),
			new("EX-SH-38", "exec 3<>/dev/tcp/host.example/80" + _LINE_FEED + "printf 'GET /x HTTP/1.1\\r\\nAuthorization: Basic dXNlcjpwYXNz\\r\\n\\r\\n' >&3", true, "construct", "/dev/tcp"),
			new("EX-SH-38", "echo \"see /dev/tcp/h/80\"" + _LINE_FEED + "printf 'GET /x HTTP/1.1\\r\\nAuthorization: Basic dXNlcjpwYXNz\\r\\n\\r\\n' >&3", true, "construct", "/dev/tcp"),
			new("EX-SH-38", "exec 3<>\"/dev/tcp/h/80\"" + _LINE_FEED + "printf 'GET /x HTTP/1.1\\r\\nAuthorization: Bearer x\\r\\n\\r\\n' >&3", false),
			new("EX-SH-38", "exec 3<>\"/dev/tcp/h/80\"" + _LINE_FEED + "printf 'GET / HTTP/1.1\\r\\nHost: h\\r\\n\\r\\n' >&3", false)
		];
	}

	private static ImmutableArray<Case> ClassCasesOfTheWrittenConfigurations()
	{
		return
		[
			new("EX-SH-39", "curl -sS -K - \"$URL\" <<EOF" + _LINE_FEED + "user = \"u:p\"" + _LINE_FEED + "EOF", true),
			new("EX-SH-39", "cat > \"$cfg\" <<EOF" + _LINE_FEED + "user = \"${U}:${P}\"" + _LINE_FEED + "EOF" + _LINE_FEED + "curl -K \"$cfg\" \"$URL\"", true),
			new("EX-SH-39", "printf 'user = \"%s:%s\"\\n' \"$U\" \"$P\" > \"$cfg\"" + _LINE_FEED + "curl --config \"$cfg\" \"$URL\"", true),
			new("EX-SH-39", "echo 'header = \"Authorization: Basic dXNlcjpwYXNz\"' > \"$cfg\"" + _LINE_FEED + "curl -K \"$cfg\" \"$URL\"", true),
			new("EX-SH-39", "curl -K <(printf 'user = \"u:p\"\\n') \"$URL\"", true),
			new("EX-SH-39", $"printf 'user = \"%s:%s\"\\n' \"$U\" \"$P\" > /tmp/curl.cfg{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND)}curl -K /tmp/curl.cfg \"$URL\"", true),
			new("EX-SH-39", "curl -K \"$cfg\" \"$URL\"", false),
			new("EX-SH-39", "{" + _LINE_FEED + "\techo \"user = u:p\"" + _LINE_FEED + "} >\"$cfg\"" + _LINE_FEED + "curl -K \"$cfg\" \"$URL\"", true),
			new("EX-SH-39", "{" + _LINE_FEED + "\tprintf 'user = \"%s:%s\"\\n' \"$U\" \"$P\"" + _LINE_FEED + "\techo \"silent\"" + _LINE_FEED + "} > \"$cfg\"" + _LINE_FEED + "curl --config \"$cfg\" \"$URL\"", true),
			new("EX-SH-39", "(" + _LINE_FEED + "\techo \"user = u:p\"" + _LINE_FEED + ") >\"$cfg\"" + _LINE_FEED + "curl -K \"$cfg\" \"$URL\"", true),
			new("EX-SH-39", "{ echo 'header = \"Authorization: Basic dXNlcjpwYXNz\"'" + _LINE_FEED + "} | tee \"$cfg\"" + _LINE_FEED + "curl -K \"$cfg\" \"$URL\"", true),
			new("EX-SH-39", "{" + _LINE_FEED + "\techo \"user = u:p\"" + _LINE_FEED + "} >\"$other\"" + _LINE_FEED + "curl -K \"$cfg\" \"$URL\"", false),
			new("EX-SH-39", "{" + _LINE_FEED + "\techo \"user = u:p\"" + _LINE_FEED + "} >&2" + _LINE_FEED + "curl -K \"$cfg\" \"$URL\"", false),
			new("EX-SH-39", "{" + _LINE_FEED + "\techo \"user = u:p\"" + _LINE_FEED + "}" + _LINE_FEED + "echo done >\"$cfg\"" + _LINE_FEED + "curl -K \"$cfg\" \"$URL\"", false),
			new("EX-SH-39", $"{{{_LINE_FEED}\techo \"user = u:p\"{_LINE_FEED}{Filler(_LINES_BETWEEN_A_GROUP_AND_ITS_REDIRECT_BEYOND_THE_BOUND)}}} >\"$cfg\"{_LINE_FEED}curl -K \"$cfg\" \"$URL\"", false),
			new("EX-SH-39", "printf 'user = \"%s:%s\"\\n' \"$U\" \"$P\" > \"$other\"" + _LINE_FEED + "curl -K \"$cfg\" \"$URL\"", false),
			new("EX-SH-39", "printf 'url = \"https://host.example/x\"\\n' > \"$cfg\"" + _LINE_FEED + "curl -K \"$cfg\"", false),
			new("EX-SH-39", "curl -K - <<EOF" + _LINE_FEED + "header = \"Authorization: Bearer x\"" + _LINE_FEED + "EOF", false),
			new("EX-SH-39", $"printf 'user = \"%s:%s\"\\n' \"$U\" \"$P\" > \"$cfg\"{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}curl -K \"$cfg\" \"$URL\"", false),
			new("EX-SH-40", "printf 'http_password = %s\\n' \"$P\" > \"$rc\"" + _LINE_FEED + "export WGETRC=\"$rc\"", true),
			new("EX-SH-40", "echo \"password=$P\" >> \"$rc\"" + _LINE_FEED + "WGETRC=\"$rc\" wget \"$URL\"", true),
			new("EX-SH-40", "cat > \"$rc\" <<EOF" + _LINE_FEED + "http_user=u" + _LINE_FEED + "http_password=p" + _LINE_FEED + "EOF" + _LINE_FEED + "wget --config=\"$rc\" \"$URL\"", true),
			new("EX-SH-40", "export WGETRC=\"$rc\"" + _LINE_FEED + "printf 'http_password = %s\\n' \"$P\" > \"$rc\"", true),
			new("EX-SH-40", "printf 'tries = 3\\n' > \"$rc\"" + _LINE_FEED + "export WGETRC=\"$rc\"", false),
			new("EX-SH-40", "printf 'http_password = %s\\n' \"$P\" > \"$other\"" + _LINE_FEED + "export WGETRC=\"$rc\"", false),
			new("EX-SH-40", "unset WGETRC", false),
			new("EX-SH-40", "{" + _LINE_FEED + "\techo \"http_user = u\"" + _LINE_FEED + "\techo \"http_password = p\"" + _LINE_FEED + "\techo \"tries = 5\"" + _LINE_FEED + "} >\"$rcfile\"" + _LINE_FEED + "export WGETRC=$rcfile", true),
			new("EX-SH-40", "export WGETRC=\"$rc\"" + _LINE_FEED + "{" + _LINE_FEED + "\tprintf 'http_password = %s\\n' \"$P\"" + _LINE_FEED + "} >>\"$rc\"", true),
			new("EX-SH-40", "(" + _LINE_FEED + "\techo \"password=$P\"" + _LINE_FEED + ") > \"$rc\"" + _LINE_FEED + "wget --config=\"$rc\" \"$URL\"", true),
			new("EX-SH-40", "for k in a b; do" + _LINE_FEED + "\techo \"http_password = $k\"" + _LINE_FEED + "done >\"$rc\"" + _LINE_FEED + "WGETRC=\"$rc\" wget \"$URL\"", true),
			new("EX-SH-40", $"export WGETRC=\"$rc\"{_LINE_FEED}{{{_LINE_FEED}\techo \"http_password = p\"{_LINE_FEED}{Filler(_LINES_BETWEEN_A_GROUP_AND_ITS_REDIRECT_AT_THE_BOUND)}}} >\"$rc\"", true),
			new("EX-SH-40", "{" + _LINE_FEED + "\techo \"http_user = u\"" + _LINE_FEED + "} >\"$other\"" + _LINE_FEED + "export WGETRC=$rc", false),
			new("EX-SH-40", "{" + _LINE_FEED + "\techo \"http_user = u\"" + _LINE_FEED + "} >&2" + _LINE_FEED + "export WGETRC=$rc", false),
			new("EX-SH-40", "{" + _LINE_FEED + "\techo \"tries = 5\"" + _LINE_FEED + "} >\"$rc\"" + _LINE_FEED + "export WGETRC=$rc", false),
			new("EX-SH-40", $"export WGETRC=\"$rc\"{_LINE_FEED}{{{_LINE_FEED}\techo \"http_password = p\"{_LINE_FEED}{Filler(_LINES_BETWEEN_A_GROUP_AND_ITS_REDIRECT_BEYOND_THE_BOUND)}}} >\"$rc\"", false)
		];
	}

	private static ImmutableArray<Case> ClassCasesOfTheAliasesAndTheAnyauth()
	{
		return
		[
			new("EX-SH-41", "CURL=curl" + _LINE_FEED + "$CURL -sS -u \"$U:$P\" \"$URL\"", true),
			new("EX-SH-41", "alias c='curl -sS'" + _LINE_FEED + "c -u a:b \"$URL\"", true),
			new("EX-SH-41", "CURL=curl.exe" + _LINE_FEED + "$CURL -u a:b \"$URL\"", true),
			new("EX-SH-41", "CURL=`which curl`" + _LINE_FEED + "$CURL -u a:b \"$URL\"", true),
			new("EX-SH-41", "CURL=$(type -p curl)" + _LINE_FEED + "$CURL -u a:b \"$URL\"", true),
			new("EX-SH-41", "alias c=curl" + _LINE_FEED + "c -u admin:secret \"$URL\"", true),
			new("EX-SH-41", "CURL=$(command -v curl)" + _LINE_FEED + "\"${CURL}\" -H \"Authorization: Basic dXNlcjpwYXNz\" \"$URL\"", true),
			new("EX-SH-41", "CURL=/usr/bin/curl" + _LINE_FEED + "${CURL} \"https://u:p@host.example/x\"", true),
			new("EX-SH-41", $"CURL=curl{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND)}$CURL -u a:b \"$URL\"", true),
			new("EX-SH-41", "CURL=curl" + _LINE_FEED + "$CURL --digest -u u:p \"$URL\"", false),
			new("EX-SH-41", "CURL=curl" + _LINE_FEED + "$CURL -sS \"$URL\"", false),
			new("EX-SH-41", "CURL=wget" + _LINE_FEED + "$CURL -u u:p \"$URL\"", false),
			new("EX-SH-41", "CURL=curl" + _LINE_FEED + "$OTHER -u a:b \"$URL\"", false),
			new("EX-SH-41", $"CURL=curl{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}$CURL -u a:b \"$URL\"", false),
			new("EX-SH-41", "alias c=curl; c -u a:b \"$URL\"", false),
			new("EX-SH-42", "WGET=wget" + _LINE_FEED + "${WGET} --user=\"$U\" --password=\"$P\" \"$URL\"", true),
			new("EX-SH-42", "alias w=wget" + _LINE_FEED + "w --password=secret \"$URL\"", true),
			new("EX-SH-42", "WGET=wget" + _LINE_FEED + "$WGET \"https://u:p@host.example/x\"", true),
			new("EX-SH-42", "WGET=wget" + _LINE_FEED + "$WGET -q \"$URL\"", false),
			new("EX-SH-42", "WGET=curl" + _LINE_FEED + "$WGET --user=u \"$URL\"", false),
			new("EX-SH-42", $"WGET=wget{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}$WGET --user=u \"$URL\"", false),
			new("EX-SH-43", "curl --anyauth -u admin:secret \"$URL\"", true, "credential", "secret"),
			new("EX-SH-43", "curl -u \"$U:$P\" --anyauth \"$URL\"", true),
			new("EX-SH-43", "curl --anyauth \"$URL\"", false),
			new("EX-SH-43", "curl --anyauth --digest -u u:p \"$URL\"", false),
			new("EX-SH-43", "curl -u u:p \"$URL\"", false),
			new("EX-SH-43", "curl --anyauth -u a:b ftp://host.example/f", false)
		];
	}

	private static ImmutableArray<Case> ClassCasesOfTheEncodedPairsLoopsAndOptionStrings()
	{
		return
		[
			new("EX-SH-44", "pair=\"${U}:${P}\"" + _LINE_FEED + "ENC=$(printf '%s' \"$pair\" | base64 | tr -d '\\n')" + _LINE_FEED + "curl -H \"Authorization: Basic $ENC\" \"$URL\"", true),
			new("EX-SH-44", "pair=$U:$P" + _LINE_FEED + "curl -sS -H \"Authorization: Basic $(base64 <<< \"$pair\")\" \"$URL\"", true),
			new("EX-SH-44", "pair=\"u:p\"" + _LINE_FEED + "ENC=$(echo -n \"$pair\" | openssl base64 -A)" + _LINE_FEED + "wget --header=\"Authorization: Basic $ENC\" \"$URL\"", true),
			new("EX-SH-44", "pair=\"${U}:${P}\"" + _LINE_FEED + "ENC=$(printf '%s' \"$pair\" | base64)" + _LINE_FEED + "echo done", false),
			new("EX-SH-44", "pair=\"${HOST}:${PORT}\"" + _LINE_FEED + "ENC=$(printf '%s' \"$pair\" | base64)" + _LINE_FEED + "curl -H \"Authorization: Basic $ENC\" \"$URL\"", false),
			new("EX-SH-44", $"pair=\"${{U}}:${{P}}\"{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}ENC=$(printf '%s' \"$pair\" | base64){_LINE_FEED}curl -H \"Authorization: Basic $ENC\" \"$URL\"", false),
			new("EX-SH-44", "pair=\"${U}:${P}\"" + _LINE_FEED + "ENC=$(printf '%s' \"$pair\" | base64 -d)" + _LINE_FEED + "curl -H \"Authorization: Basic $ENC\" \"$URL\"", false),
			new("EX-SH-45", "while IFS= read -r PASS; do" + _LINE_FEED + "echo x" + _LINE_FEED + "done < \"$SECRETS\"", true, "name", "PASS"),
			new("EX-SH-45", "while read -r api_token; do" + _LINE_FEED + "echo x" + _LINE_FEED + "done < <(pass show x)", true, "name", "api_token"),
			new("EX-SH-45", $"while read -r PASS; do{_LINE_FEED}{Filler(_LINES_BETWEEN_A_LOOP_AND_ITS_DONE_AT_THE_BOUND)}done < \"$F\"", true),
			new("EX-SH-45", "while read -r PASS; do" + _LINE_FEED + "echo x" + _LINE_FEED + "done </dev/tty", false),
			new("EX-SH-45", "while read -r line; do" + _LINE_FEED + "echo x" + _LINE_FEED + "done < f", false),
			new("EX-SH-45", "while read -r PASS; do" + _LINE_FEED + "echo x" + _LINE_FEED + "done", false),
			new("EX-SH-45", $"while read -r PASS; do{_LINE_FEED}{Filler(_LINES_BETWEEN_A_LOOP_AND_ITS_DONE_BEYOND_THE_BOUND)}done < \"$F\"", false),
			new("EX-SH-45", "read -r PASS" + _LINE_FEED + "done < f", false),
			new("EX-SH-46", "CURL_OPTS=\"-sS -u $U:$P\"" + _LINE_FEED + "curl $CURL_OPTS \"$URL\"", true),
			new("EX-SH-46", "OPTS='--user admin:secret'" + _LINE_FEED + "curl ${OPTS} \"$URL\"", true),
			new("EX-SH-46", "WGET_OPTS=\"--user=u --password=p\"" + _LINE_FEED + "wget $WGET_OPTS \"$URL\"", true),
			new("EX-SH-46", "H_OPTS=\"-a $U:$P\"" + _LINE_FEED + "http $H_OPTS \"$URL\"", true),
			new("EX-SH-46", "CURL_OPTS=\"-sS -u $U:$P\"" + _LINE_FEED + "curl \"$CURL_OPTS\" \"$URL\"", false),
			new("EX-SH-46", "CURL_OPTS=\"-sS --fail\"" + _LINE_FEED + "curl $CURL_OPTS \"$URL\"", false),
			new("EX-SH-46", $"CURL_OPTS=\"-sS -u $U:$P\"{_LINE_FEED}{Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_BEYOND_THE_BOUND)}curl $CURL_OPTS \"$URL\"", false)
		];
	}
}
