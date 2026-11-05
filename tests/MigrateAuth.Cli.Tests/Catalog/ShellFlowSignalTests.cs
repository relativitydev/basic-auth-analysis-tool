using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Reporting;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE `Shell` ANALYZER'S FIFTEEN FLOW SIGNALS, THROUGH THE REAL EMBEDDED ANALYZER, THE REAL LEXICAL SCANNER AND THE REAL RECOGNITION ENGINE, AND THE FLOW A
/// FINDING RENDERS UNDER THEM THROUGH THE REAL FOUR-STAGE PIPELINE.
///
/// Each signal is read ALONE (an analyzer that declares the one signal and everything else the shipped analyzer declares), so a fixture proves the signal it
/// names and not whichever other signal happened to fire; the whole analyzer is read beside it for the evidence a file takes. Every source is invented from the
/// catalog's own prose, with placeholder names, and carries no fragment, path or third-party name from any repository.
/// </summary>
public sealed class ShellFlowSignalTests
{
	private const string _NEWLINE = "\n";

	/// <summary>A High construct that sends no credential-named expansion and no literal credential, so the file is evaluated for flow and nothing else fires.</summary>
	private const string _HIGH_CONSTRUCT = "curl --silent -H \"Authorization: Basic ${ENCODED}\" \"${HOST}/x\"";

	private const string _INTERACTIVE_FILE_NAME = "Interactive.sh";
	private const string _FIXED_FILE_NAME = "Fixed.sh";
	private const string _PLAIN_FILE_NAME = "Plain.sh";
	private const string _BOTH_KINDS_FILE_NAME = "BothKinds.sh";
	private const string _LITERAL_ONLY_FILE_NAME = "LiteralOnly.sh";
	private const string _CREDENTIAL_FILE_NAME = "Credential.sh";

	private const string _AUTHORIZATION_CODE_GRANT = "grant_type=authorization_code";
	private const string _CLIENT_CREDENTIALS_GRANT = "grant_type=client_credentials";
	private const string _CODE_USE_SITE = "Authorization: Bearer";
	private const string _SHELL_TOKEN_ACQUISITION = "IFS= read -r returned_url";
	private const string _PLACED_SECRET = "a-literal-secret";
	private const string _WARNING_PREFIX = "Warning:";

	private const string _KSH_SHEBANG = "#!/bin/ksh\n";
	private const string _KSH93_SHEBANG = "#!/usr/bin/env ksh93\n";
	private const string _MKSH_SHEBANG = "#!/bin/mksh\n";
	private const string _ZSH_SHEBANG = "#!/usr/bin/env zsh\n";
	private const string _BASH_SHEBANG = "#!/bin/bash\n";
	private const string _SH_SHEBANG = "#!/bin/sh\n";
	private const string _UNDEMOTED_PATH = "scripts/run.sh";
	private const string _KSH_EXTENSION_PATH = "scripts/run.ksh";
	private const string _ZSH_EXTENSION_PATH = "scripts/run.zsh";

	private const int _LINES_PAST_THE_VARIABLE_WINDOW = 45;
	private const int _SIGNAL_COUNT = 15;
	private const int _INTERACTIVE_SIGNAL_COUNT = 10;
	private const int _FIXED_SIGNAL_COUNT = 5;
	private const int _SIGNALS_THAT_TRACK_THE_VARIABLE = 4;

	private static readonly ImmutableArray<(string Name, FlowSignalKind Kind, FlowSignalZone Zone)> _declaredTable =
	[
		("SilentRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("SttyEcho", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("CredentialNamedRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("PasswordDialog", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("HiddenAnswerDialog", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("CurlUserWithoutPassword", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("WgetAskPassword", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("PromptWordingOfThePromptOption", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("PromptWording", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("HttpieAuthWithoutPassword", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("CredentialNamedExpansion", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOrStringBody),
		("SecretStoreTool", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("NetrcOption", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("RedirectedCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("LoopReadFromFile", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly)
	];

	/// <summary>One or more fixtures per signal: the signal's name, the evidence it gives and a source in which it reads.</summary>
	private static readonly ImmutableArray<(string Signal, FlowEvidence Evidence, string Source)> _signalFixtures =
	[
		("SilentRead", FlowEvidence.Interactive, "read -rs -p \"Prompt: \" ANSWER"),
		("SilentRead", FlowEvidence.Interactive, "read -s ANSWER"),
		("SttyEcho", FlowEvidence.Interactive, "stty -echo\nIFS= read -r ANSWER\nstty echo"),
		("CredentialNamedRead", FlowEvidence.Interactive, "read -r API_PASSWORD"),
		("CredentialNamedRead", FlowEvidence.Interactive, "read -r dbPass"),
		("PasswordDialog", FlowEvidence.Interactive, "ANSWER=$(systemd-ask-password \"Prompt\")"),
		("PasswordDialog", FlowEvidence.Interactive, "ANSWER=$(whiptail --passwordbox \"Prompt\" 8 40 3>&1 1>&2 2>&3)"),
		("PasswordDialog", FlowEvidence.Interactive, "ANSWER=$(zenity --entry --hide-text --text \"Prompt\")"),
		("HiddenAnswerDialog", FlowEvidence.Interactive, "ANSWER=$(osascript -e 'display dialog \"Prompt\" default answer \"\" with hidden answer')"),
		("CurlUserWithoutPassword", FlowEvidence.Interactive, "curl --silent -u admin \"${HOST}/x\""),
		("CurlUserWithoutPassword", FlowEvidence.Interactive, "CURL=curl\n${CURL} --silent --user admin \"${HOST}/x\""),
		("WgetAskPassword", FlowEvidence.Interactive, "wget --user=admin --ask-password \"${HOST}/x\""),
		("PromptWordingOfThePromptOption", FlowEvidence.Interactive, "read -p \"Enter your password: \" ANSWER"),
		("PromptWording", FlowEvidence.Interactive, "read \"answer?Enter the password: \""),
		("PromptWording", FlowEvidence.Interactive, "printf 'Enter the passphrase: '\nread -r ANSWER"),
		("HttpieAuthWithoutPassword", FlowEvidence.Interactive, "http -a admin \"${HOST}/x\""),
		("CredentialNamedExpansion", FlowEvidence.Fixed, "curl --silent \"${HOST}/x?key=${API_TOKEN}\""),
		("CredentialNamedExpansion", FlowEvidence.Fixed, "curl --silent -u \"${CREDS}\" \"${HOST}/x\""),
		("SecretStoreTool", FlowEvidence.Fixed, "ANSWER=$(vault kv get -field=value store/path)"),
		("NetrcOption", FlowEvidence.Fixed, "curl --silent --netrc \"${HOST}/x\""),
		("RedirectedCredentialRead", FlowEvidence.Fixed, "read -r API_PASSWORD < \"${SECRET_FILE}\""),
		("LoopReadFromFile", FlowEvidence.Fixed, "while IFS= read -r API_PASSWORD; do\n\tuse_it\ndone < \"${SECRETS_FILE}\"")
	];

	public static TheoryData<string, FlowEvidence, string> SignalFixtures()
	{
		TheoryData<string, FlowEvidence, string> data = [];

		foreach ((string signal, FlowEvidence evidence, string source) in _signalFixtures)
		{
			data.Add(signal, evidence, source);
		}

		return data;
	}

	// ------------------------------------------------------------------------------------------
	// The declaration
	// ------------------------------------------------------------------------------------------

	/// <summary>Ten interactive signals and five fixed-credential ones, each with the kind and the zone the catalog declares, and both kinds exist.</summary>
	[Fact]
	public void TheDeclaredSignalsCarryTheCatalogsKindAndZoneEachAndBothKindsExist()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.Equal(
			_declaredTable.Select(row => (row.Name, row.Kind, row.Zone)).ToArray(),
			signals.Select(signal => (signal.Name, signal.Kind, signal.Zone)).ToArray());
		Assert.Equal(_SIGNAL_COUNT, signals.Length);
		Assert.Equal(_INTERACTIVE_SIGNAL_COUNT, signals.Count(signal => signal.Kind == FlowSignalKind.InteractiveUser));
		Assert.Equal(_FIXED_SIGNAL_COUNT, signals.Count(signal => signal.Kind == FlowSignalKind.FixedCredential));
	}

	[Fact]
	public void EverySignalExpressionDeclaresThePolicyMatchTimeoutAndTheAnalyzerFitsTheContract()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.All(signals, signal => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, signal.Expression.MatchTimeout));
		Assert.Empty(AnalyzerContractValidator.Validate(ReadAnalyzer()));
	}

	[Fact]
	public void EverySignalOfTheTableHasAtLeastOneFixture()
	{
		Assert.Equal(
			_declaredTable.Select(row => row.Name).Order(StringComparer.Ordinal),
			_signalFixtures.Select(fixture => fixture.Signal).Distinct().Order(StringComparer.Ordinal));
	}

	/// <summary>
	/// The dialects of the declaration: a silent read hides nothing in <c>ksh93</c> and <c>mksh</c>, so it is excluded there; the <c>-p</c> option is a prompt
	/// in <c>bash</c> alone, so that signal is included for it alone. No other signal narrows its dialects.
	/// </summary>
	[Fact]
	public void TheDialectDeclarationsAreTheOnesTheCatalogStates()
	{
		FrozenDictionary<string, FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals.ToFrozenDictionary(signal => signal.Name, StringComparer.Ordinal);

		Assert.Equal(["ksh93", "mksh"], signals["SilentRead"].ExcludedDialects.Order(StringComparer.Ordinal));
		Assert.Empty(signals["SilentRead"].Dialects);
		Assert.Equal(["bash"], signals["PromptWordingOfThePromptOption"].Dialects.Order(StringComparer.Ordinal));
		Assert.Empty(signals["PromptWordingOfThePromptOption"].ExcludedDialects);
		Assert.Equal(
			2,
			ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals.Count(signal => !signal.Dialects.IsDefaultOrEmpty || !signal.ExcludedDialects.IsDefaultOrEmpty));
	}

	/// <summary>The four signals that capture the variable a prompt fills track it, over the statement separators the profile declares and the engine's default window.</summary>
	[Fact]
	public void TheFourPromptSignalsTrackTheVariableTheyFillOverTheDeclaredSeparators()
	{
		ImmutableArray<FlowSignal> tracking = [.. ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals.Where(signal => signal.VariableUse is not null)];

		Assert.Equal(_SIGNALS_THAT_TRACK_THE_VARIABLE, tracking.Length);
		Assert.Equal(
			["CredentialNamedRead", "PromptWording", "PromptWordingOfThePromptOption", "SilentRead"],
			tracking.Select(signal => signal.Name).Order(StringComparer.Ordinal));
		Assert.All(tracking, signal =>
		{
			Assert.Equal(["&", "&&", ";", "||"], signal.VariableUse!.StatementSeparators.Order(StringComparer.Ordinal));
			Assert.Equal("REPLY", signal.VariableUse.DefaultVariableName);
			Assert.Equal(FlowSignalVariableUse.DEFAULT_WINDOW_LINES, signal.VariableUse.WindowLines);
		});
	}

	// ------------------------------------------------------------------------------------------
	// Each signal fires in its zone, and not outside it
	// ------------------------------------------------------------------------------------------

	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ASignalGivesItsEvidenceOnItsOwnFixtureWhenItIsReadAlone(string signal, FlowEvidence expected, string source)
	{
		FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(source));

		Assert.True(expected == alone, $"{signal}: expected {expected}, got {alone}.");
	}

	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void TheWholeAnalyzerGivesTheFixturesEvidenceToo(string signal, FlowEvidence expected, string source)
	{
		RecognitionResult result = new ShellRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.True(expected == result.FlowEvidence, $"{signal}: expected {expected}, got {result.FlowEvidence}.");
	}

	/// <summary>A signal commented out is not evidence, in either zone kind.</summary>
	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ASignalInACommentIsNotEvidence(string signal, FlowEvidence unused, string source)
	{
		string commentedOut = string.Join(_NEWLINE, source.Split(_NEWLINE).Select(line => $"# {line}"));

		FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(commentedOut));

		Assert.True(alone == FlowEvidence.None, $"{signal} read inside a comment: {alone}. {unused}");
	}

	/// <summary>A code-only signal quoted inside a single-quoted string is not evidence. The string-body signals are read there by design, and a fixture that holds a quote of its own is not wrapped.</summary>
	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ACodeOnlySignalInAStringBodyIsNotEvidence(string signal, FlowEvidence unused, string source)
	{
		FlowSignal declared = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals.Single(candidate => candidate.Name == signal);

		if (declared.Zone == FlowSignalZone.CodeOnly && !source.Contains('\''))
		{
			string quoted = $"note='{source}'";

			FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(quoted));

			Assert.True(alone == FlowEvidence.None, $"{signal} read inside a string: {alone}. {unused}");
		}
	}

	/// <summary>The string-body signals do read the wording inside a string: that is the zone they declare.</summary>
	[Theory]
	[InlineData("PromptWording", "echo \"Enter the password: \"\nread -r ANSWER")]
	[InlineData("PromptWordingOfThePromptOption", "read -p \"Enter your password: \" ANSWER")]
	[InlineData("CredentialNamedExpansion", "curl --silent \"${HOST}/x?key=${API_TOKEN}\"")]
	public void AStringBodySignalReadsItsWordingInsideAString(string signal, string source)
	{
		FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(source));

		Assert.NotEqual(FlowEvidence.None, alone);
	}

	// ------------------------------------------------------------------------------------------
	// The dialects of a file
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData(_KSH_SHEBANG, _UNDEMOTED_PATH, FlowEvidence.None)]
	[InlineData(_KSH93_SHEBANG, _UNDEMOTED_PATH, FlowEvidence.None)]
	[InlineData(_MKSH_SHEBANG, _UNDEMOTED_PATH, FlowEvidence.None)]
	[InlineData("", _KSH_EXTENSION_PATH, FlowEvidence.None)]
	[InlineData(_BASH_SHEBANG, _UNDEMOTED_PATH, FlowEvidence.Interactive)]
	[InlineData(_SH_SHEBANG, _UNDEMOTED_PATH, FlowEvidence.Interactive)]
	[InlineData(_ZSH_SHEBANG, _UNDEMOTED_PATH, FlowEvidence.Interactive)]
	[InlineData("", _UNDEMOTED_PATH, FlowEvidence.Interactive)]
	[InlineData("", _ZSH_EXTENSION_PATH, FlowEvidence.Interactive)]
	public void ASilentReadIsEvidenceExceptWhereItsOptionDoesNotHideTheInput(string shebang, string path, FlowEvidence expected)
	{
		string source = $"{shebang}read -rs ANSWER\n{_HIGH_CONSTRUCT}{_NEWLINE}";

		Assert.Equal(expected, EvidenceOfSignalAlone("SilentRead", source, path));
		Assert.Equal(expected, new ShellRecognitionHarness().RecognizeFully(source, path).FlowEvidence);
	}

	[Theory]
	[InlineData(_BASH_SHEBANG, _UNDEMOTED_PATH, FlowEvidence.Interactive)]
	[InlineData(_SH_SHEBANG, _UNDEMOTED_PATH, FlowEvidence.Interactive)]
	[InlineData("", _UNDEMOTED_PATH, FlowEvidence.Interactive)]
	[InlineData(_ZSH_SHEBANG, _UNDEMOTED_PATH, FlowEvidence.None)]
	[InlineData(_KSH_SHEBANG, _UNDEMOTED_PATH, FlowEvidence.None)]
	[InlineData(_MKSH_SHEBANG, _UNDEMOTED_PATH, FlowEvidence.None)]
	[InlineData("", _ZSH_EXTENSION_PATH, FlowEvidence.None)]
	[InlineData("", _KSH_EXTENSION_PATH, FlowEvidence.None)]
	public void APromptOptionWordingIsEvidenceOnlyWhereTheOptionIsAPrompt(string shebang, string path, FlowEvidence expected)
	{
		string source = $"{shebang}read -p \"Enter your password: \" ANSWER\n{_HIGH_CONSTRUCT}{_NEWLINE}";

		Assert.Equal(expected, EvidenceOfSignalAlone("PromptWordingOfThePromptOption", source, path));
	}

	/// <summary>The form that names the variable and the prompt in one word is read in every dialect, and is the prompt of the shells where <c>-p</c> is not.</summary>
	[Theory]
	[InlineData(_ZSH_SHEBANG)]
	[InlineData(_KSH_SHEBANG)]
	[InlineData(_MKSH_SHEBANG)]
	[InlineData(_BASH_SHEBANG)]
	[InlineData("")]
	public void TheOneWordPromptFormIsEvidenceInEveryDialect(string shebang)
	{
		string source = $"{shebang}read \"answer?Enter the password: \"\n{_HIGH_CONSTRUCT}{_NEWLINE}";

		Assert.Equal(FlowEvidence.Interactive, EvidenceOfSignalAlone("PromptWording", source));
	}

	/// <summary>A terminal-echo prompt is the evidence in a <c>ksh</c> file, where the silent option is not.</summary>
	[Fact]
	public void SttyEchoIsEvidenceInAKshFileWhereTheSilentOptionIsNot()
	{
		string source = $"{_KSH_SHEBANG}stty -echo\nIFS= read -r ANSWER\nstty echo\n{_HIGH_CONSTRUCT}{_NEWLINE}";

		Assert.Equal(FlowEvidence.Interactive, new ShellRecognitionHarness().RecognizeFully(source).FlowEvidence);
	}

	// ------------------------------------------------------------------------------------------
	// Near-misses and the direct-form exclusion
	// ------------------------------------------------------------------------------------------

	/// <summary>Reads that are not a person at a terminal, and prompts whose answer goes straight to a privilege tool, a remote login or a passphrase argument.</summary>
	[Theory]
	[InlineData("read -r ANSWER")]
	[InlineData("read -p \"Enter the name: \" ANSWER")]
	[InlineData("echo \"Enter the value:\"\nread ANSWER")]
	[InlineData("stty sane")]
	[InlineData("curl --silent -u \"${WHO}\" \"${HOST}/x\"")]
	[InlineData("http -a \"${WHO}\" \"${HOST}/x\"")]
	[InlineData("curl --silent -u admin:placeholder \"${HOST}/x\"")]
	[InlineData("read -rs P | sudo -S ./setup.sh")]
	[InlineData("sudo -S ./setup.sh <<< \"$(systemd-ask-password 'sudo password')\"")]
	[InlineData("sshpass -p \"$(systemd-ask-password 'ssh password')\" ssh deploy@host.example")]
	[InlineData("read -rs P\nprintf '%s\\n' \"$P\" | sudo -S ./setup.sh")]
	[InlineData("read -rs P\nP2=$P\nprintf '%s\\n' \"$P2\" | sudo -S ./setup.sh")]
	[InlineData("read -rs\nprintf '%s\\n' \"$REPLY\" | sudo -S ./setup.sh")]
	[InlineData("read -rs P; printf '%s\\n' \"$P\" | sudo -S ./setup.sh")]
	[InlineData("read -rs P && sshpass -p \"$P\" ssh deploy@host.example")]
	[InlineData("read -p \"Passphrase: \" PW\nopenssl enc -aes-256-cbc -pass \"pass:$PW\" -in a -out b")]
	[InlineData("cat \"${SECRETS_FILE}\" | while read -r API_PASSWORD; do :; done")]
	public void ANearMissOrTheDirectFormCarriesNoFlowEvidence(string source)
	{
		RecognitionResult result = new ShellRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	/// <summary>The answer used somewhere that is not one of the exclusion's tools, and the use that falls past the window, leave the signal firing.</summary>
	[Theory]
	[InlineData("read -rs P\ncurl --silent -u \"admin:$P\" \"${HOST}/x\"")]
	[InlineData("read -p \"ssh password: \" ANSWER")]
	[InlineData("read -rs P\nprintf '%s\\n' \"$P\" | wc -c")]
	[InlineData("read -rs P; echo done; printf '%s\\n' \"$P\" | sudo -n true")]
	public void AnAnswerThatIsNotHandedToTheExclusionsToolsStaysInteractive(string source)
	{
		RecognitionResult result = new ShellRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(FlowEvidence.Interactive, result.FlowEvidence);
	}

	[Fact]
	public void AUseOfTheAnswerPastTheWindowOfLinesIsNotFollowed()
	{
		string filler = string.Join(_NEWLINE, Enumerable.Repeat("echo filler", _LINES_PAST_THE_VARIABLE_WINDOW));
		string source = $"read -rs P\n{filler}\nprintf '%s\\n' \"$P\" | sudo -S ./setup.sh";

		RecognitionResult result = new ShellRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(FlowEvidence.Interactive, result.FlowEvidence);
	}

	/// <summary>
	/// THE FLOW NEAR-MISSES OF THE CATALOG. A credential taken from a variable, a file or a loop over a file is fixed, and never reads as a prompt: the
	/// <c>read</c> whose input is redirected on its line or on the <c>done</c> of its loop is a stored value.
	/// </summary>
	[Theory]
	[InlineData("curl --silent -u \"${CREDS}\" \"${HOST}/x\"")]
	[InlineData("read PASS < secret.txt")]
	[InlineData("read -r API_PASSWORD < \"${SECRET_FILE}\"")]
	[InlineData("while IFS= read -r PASS; do :; done < \"${SECRETS_FILE}\"")]
	[InlineData("until read -r API_TOKEN; do :; done < <(secret-source)")]
	public void AFixedCredentialShapeIsFixedAndNeverInteractive(string source)
	{
		RecognitionResult result = new ShellRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(FlowEvidence.Fixed, result.FlowEvidence);
	}

	/// <summary>A read from the terminal behind a redirection of the terminal itself is still a person.</summary>
	[Theory]
	[InlineData("read -rs ANSWER < /dev/tty")]
	[InlineData("read -r API_PASSWORD </dev/tty")]
	public void AReadFromTheTerminalItselfStaysInteractive(string source)
	{
		RecognitionResult result = new ShellRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(FlowEvidence.Interactive, result.FlowEvidence);
	}

	/// <summary>
	/// THE LITERAL RULE. A credential written into source is not flow evidence: the engine's literal-credential signal stays a security property. A file whose
	/// only credential is a literal decides the default, and the literal signal is still raised.
	/// </summary>
	[Fact]
	public void ALiteralCredentialIsNotFlowEvidenceAndItsFileDecidesTheDefault()
	{
		string source = LiteralOnlyContent();
		RecognitionResult result = new ShellRecognitionHarness().RecognizeFully(source);

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
		Assert.Contains(result.Constructs, construct => construct.LiteralCredentialSignal);

		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_LITERAL_ONLY_FILE_NAME, source);

		harness.RunTheWholeWorkflow();

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _LITERAL_ONLY_FILE_NAME);

		Assert.Equal(nameof(FlowDecision.Default), finding.FlowDecision);
		Assert.Equal(nameof(CredentialFlow.ClientCredentials), finding.Flow);
	}

	// ------------------------------------------------------------------------------------------
	// The flow a finding renders
	// ------------------------------------------------------------------------------------------

	/// <summary>An interactive signal renders Authorization Code, with its note, its Code use site and its token acquisition; no signal renders Client Credentials with the default note.</summary>
	[Fact]
	public void AnInteractiveSignalRendersAuthorizationCodeAndNoSignalRendersClientCredentialsWithTheDefaultNote()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_INTERACTIVE_FILE_NAME, $"read -rs -p \"Password: \" ANSWER\n{_HIGH_CONSTRUCT}{_NEWLINE}");
		harness.WriteSourceFile(_FIXED_FILE_NAME, $"read -r API_PASSWORD < \"${{SECRET_FILE}}\"\n{_HIGH_CONSTRUCT}{_NEWLINE}");
		harness.WriteSourceFile(_PLAIN_FILE_NAME, $"{_HIGH_CONSTRUCT}{_NEWLINE}");

		harness.RunTheWholeWorkflow();

		ArtifactDocument document = ReadArtifact(harness);
		ArtifactFindingDocument interactive = FindingOf(document, _INTERACTIVE_FILE_NAME);
		ArtifactFindingDocument fixedCredential = FindingOf(document, _FIXED_FILE_NAME);
		ArtifactFindingDocument plain = FindingOf(document, _PLAIN_FILE_NAME);

		Assert.Equal(nameof(CredentialFlow.AuthorizationCode), interactive.Flow);
		Assert.Equal(nameof(FlowDecision.InteractiveSignal), interactive.FlowDecision);
		Assert.Equal(FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE, interactive.FlowNote);
		Assert.Contains(_AUTHORIZATION_CODE_GRANT, interactive.Recommendation, StringComparison.Ordinal);
		Assert.DoesNotContain(_CLIENT_CREDENTIALS_GRANT, interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_CODE_USE_SITE, interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_SHELL_TOKEN_ACQUISITION, interactive.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), fixedCredential.Flow);
		Assert.Equal(nameof(FlowDecision.FixedSignal), fixedCredential.FlowDecision);
		Assert.Equal(FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE, fixedCredential.FlowNote);
		Assert.Contains(_CLIENT_CREDENTIALS_GRANT, fixedCredential.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), plain.Flow);
		Assert.Equal(nameof(FlowDecision.Default), plain.FlowDecision);
		Assert.Equal(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, plain.FlowNote);
		Assert.Contains(_CLIENT_CREDENTIALS_GRANT, plain.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_CODE_USE_SITE, plain.Recommendation, StringComparison.Ordinal);
	}

	/// <summary>
	/// KNOWN LIMIT (the mixed idiom). A file that holds both an interactive and a fixed-credential signal is assigned Authorization Code, because the shared
	/// rule lets an interactive signal win. The mismatch with the expected flow is disclosed; this test pins it so that no change moves it unseen.
	/// </summary>
	[Fact]
	public void AFileWithBothKindsOfEvidenceIsAssignedAuthorizationCodeAndThatMismatchIsAnAcceptedLimit()
	{
		string source =
			"if [ -z \"${API_PASSWORD:-}\" ]; then\n"
			+ "\tread -rs -p \"Password: \" API_PASSWORD\n"
			+ "fi\n"
			+ "curl --silent -u \"${API_USER}:${API_PASSWORD}\" \"${HOST}/x\"\n";

		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_BOTH_KINDS_FILE_NAME, source);

		harness.RunTheWholeWorkflow();

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _BOTH_KINDS_FILE_NAME);

		Assert.Equal(nameof(CredentialFlow.AuthorizationCode), finding.Flow);
		Assert.Equal(nameof(FlowDecision.InteractiveSignal), finding.FlowDecision);
	}

	// ------------------------------------------------------------------------------------------
	// The literal-credential warning
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// THE CONSOLE WARNING THROUGH THE REAL PIPELINE. A literal credential in a `Shell` source produces a warning line naming the finding, its pattern class and
	/// the Secret Management pointer (Part B), and never the placed secret itself anywhere in the transcript.
	/// </summary>
	[Fact]
	public void ALiteralCredentialFindingProducesAWarningNamingPartBAndNeverPrintsTheValue()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, LiteralOnlyContent());

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Finding finding = Assert.Single(harness.State.Findings);

		Assert.Contains(_WARNING_PREFIX, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(finding.Identifier, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains("SH-001", harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(_PLACED_SECRET, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>The recommendation of a literal-credential `Shell` finding carries the security sentence that names Part B, beside the migration text and not inside it.</summary>
	[Fact]
	public void ALiteralCredentialShellFindingRendersTheSecuritySentenceThatNamesPartB()
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_CREDENTIAL_FILE_NAME,
			1,
			"SH-001",
			ConfidenceLevel.High,
			literalCredentialSignal: true,
			language: AssessedLanguage.Shell,
			flow: CredentialFlow.ClientCredentials,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(ShellRenderedTexts.Catalog(), new GenericRecommendationProvider());

		RenderedRecommendation rendered = renderer.Render(finding, MigrateAuth.Cli.Emission.EmissionDestination.StructuredData);

		Assert.Equal(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, rendered.SecurityNote);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, rendered.SecurityNote, StringComparison.Ordinal);
		Assert.DoesNotContain(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, RecommendationComposer.ComposeArtifactText(rendered)!, StringComparison.Ordinal);
	}

	/// <summary>The complement: a credential held in a variable produces no warning line at all.</summary>
	[Fact]
	public void AVariableCredentialFindingProducesNoWarning()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, $"{_HIGH_CONSTRUCT}{_NEWLINE}");

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.Single(harness.State.Findings);
		Assert.DoesNotContain(_WARNING_PREFIX, harness.Transcript, StringComparison.Ordinal);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static string LiteralOnlyContent()
	{
		return $"curl --silent -u admin:{_PLACED_SECRET} \"${{HOST}}/x\"\n";
	}

	private static string WithTheHighConstruct(string source)
	{
		return $"{source}{_NEWLINE}{_HIGH_CONSTRUCT}{_NEWLINE}";
	}

	/// <summary>The evidence one signal gives alone: the shipped analyzer, narrowed to that signal, on a file the shipped scanner and engine read.</summary>
	private static FlowEvidence EvidenceOfSignalAlone(string signalName, string source, string relativePath = _UNDEMOTED_PATH)
	{
		ShellRecognitionHarness harness = new();
		FlowSignal signal = harness.Analyzer.CandidateSelectionAndRecommendation.FlowSignals.Single(candidate => candidate.Name == signalName);
		FileRecognitionContext context = harness.ContextFor(source, relativePath);
		FileRecognitionContext alone = context with { Analyzer = new SingleSignalShellAnalyzer(harness.Analyzer, signal) };
		RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());

		return engine.Recognize(alone).FlowEvidence;
	}

	private static ArtifactDocument ReadArtifact(PipelineHarness harness)
	{
		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		return JsonSerializer.Deserialize(artifactText, ReportJsonContext.Default.ArtifactDocument)!;
	}

	private static ArtifactFindingDocument FindingOf(ArtifactDocument document, string fileName)
	{
		return document.Findings.Single(finding => finding.FilePath == fileName);
	}

	private static ILanguageAnalyzer ReadAnalyzer()
	{
		return ShellRenderedTexts.Analyzer();
	}

	/// <summary>The shipped analyzer with every flow signal but one removed: the same profile, rules, demotion, dialects and wrappers, so one signal can be read on its own.</summary>
	private sealed class SingleSignalShellAnalyzer : ILanguageAnalyzer
	{
		private readonly ILanguageAnalyzer _inner;

		internal SingleSignalShellAnalyzer(ILanguageAnalyzer inner, FlowSignal signal)
		{
			_inner = inner;
			CandidateSelectionAndRecommendation = new CandidateSelectionAndRecommendation
			{
				PreselectionAnchors = inner.CandidateSelectionAndRecommendation.PreselectionAnchors,
				TemplateProvider = inner.CandidateSelectionAndRecommendation.TemplateProvider,
				FlowSignals = [signal],
				LanguageFallbackTextByFlow = inner.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow,
				FileDialects = inner.CandidateSelectionAndRecommendation.FileDialects,
				WrapperTracking = inner.CandidateSelectionAndRecommendation.WrapperTracking
			};
		}

		public LexicalProfile LexicalProfile => _inner.LexicalProfile;

		public LanguageIdentification LanguageIdentification => _inner.LanguageIdentification;

		public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass => _inner.DetectionRulesByPatternClass;

		public LanguageShapedDemotionSignals DemotionSignals => _inner.DemotionSignals;

		public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; }

		public FrozenSet<SubsumptionRule> SubsumptionRelation => _inner.SubsumptionRelation;

		public ConstructPrecedence? ConstructPrecedence => _inner.ConstructPrecedence;
	}
}
