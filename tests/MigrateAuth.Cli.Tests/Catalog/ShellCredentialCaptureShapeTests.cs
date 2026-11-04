using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE CATALOG SHAPE RULE, ENFORCED OVER EVERY RULE OF THE `Shell` ANALYZER. Because <c>${…}</c>, <c>$var</c> and a backtick inside a double-quoted string are
/// string body, four rules bind every expression, and this file is what checks them:
///
/// (a) every credential and scheme capture group excludes <c>$</c>, the backtick and the backslash, or takes no part in the match;
/// (b) no rule suppresses a finding on a resolved scheme unless its scheme group is free of <c>$</c>: here no rule declares a scheme group at all, so the rule is
///     held vacuously and the test says so by asserting it;
/// (c) the anchor of every rule sits on a command or option word in code, never inside a quote;
/// (d) a probe set is run against every rule: an expansion in the credential position, in any part of the word, must leave the credential group out of the match
///     and the literal-credential signal false.
///
/// The probes are the shapes of an expansion the catalog names: a variable, a braced variable, a command substitution, a backtick substitution, an escaped
/// dollar, a quoted part followed by an expansion, a pair with an expansion in one part, a brace expansion, a glob, a leading tilde and adjacent quoted parts.
/// Each probe is put into the credential position of a template written for each rule, and each template is the invented, placeholder form of that rule's own
/// credential-bearing construct. Whether the word a probe makes expands depends on where it stands: a brace or a glob character expands in a bare word and is
/// literal inside quotes, and a tilde expands only at the start of a word.
/// </summary>
public sealed class ShellCredentialCaptureShapeTests
{
	private const string _CREDENTIAL_GROUP = "credential";
	private const string _SCHEME_GROUP = "scheme";
	private const string _PROBE_PLACEHOLDER = "{P}";
	private const string _DOLLAR = "$";
	private const string _BACKTICK = "`";
	private const string _BACKSLASH = "\\";
	private const string _QUOTING_HERE_DOCUMENT_OPENING = "cat <<'QUOTED'\n";
	private const string _QUOTING_HERE_DOCUMENT_CLOSING = "QUOTED\n";

	/// <summary>When the word a probe makes is expanded by the shell.</summary>
	private enum Expansion
	{
		Always,
		InABareWord,
		AsAWholeWord,
		Never
	}

	/// <summary>One probe: the text put into the credential position and when the word it makes expands.</summary>
	private sealed record Probe(string Text, Expansion Expansion);

	/// <summary>One template: the rule it is written for, its text with the placeholder, whether the placeholder stands inside quotes and whether it is a whole word.</summary>
	private sealed record Template(string PatternClass, string Text, bool Quoted = false, bool WholeWord = false);

	private static readonly ImmutableArray<Probe> _probes =
	[
		new("$P", Expansion.Always),
		new("${P}", Expansion.Always),
		new("$(cat f)", Expansion.Always),
		new("`cat f`", Expansion.Always),
		new("\\$P", Expansion.Always),
		new("'p'$x", Expansion.Always),
		new("p\"$y\"", Expansion.Always),
		new("a${b}c", Expansion.Always),
		new("{a,b}", Expansion.InABareWord),
		new("*.txt", Expansion.InABareWord),
		new("~/x", Expansion.AsAWholeWord),
		new("\"p\"'q'", Expansion.Never)
	];

	/// <summary>One template per rule, written with the placeholder where the credential stands, and a second where the rule reads a second form.</summary>
	private static readonly ImmutableArray<Template> _templates =
	[
		new("SH-001", "curl -sS -u admin:{P} \"$URL\"\n"),
		new("SH-001", "curl -sS -u {P} \"$URL\"\n", WholeWord: true),
		new("SH-002", "curl -u {P} \"$URL\"\n", WholeWord: true),
		new("SH-003", "curl -sS -H \"Authorization: Basic {P}\" \"$URL\"\n", Quoted: true),
		new("SH-004", "wget --auth-no-challenge --user=u --password={P} \"$URL\"\n"),
		new("SH-004", "wget --auth-no-challenge https://u:{P}@h.test/x\n"),
		new("SH-005", "curl https://u:{P}@h.test/x\n"),
		new("SH-006", "curl --netrc-file {P} \"$URL\"\n", WholeWord: true),
		new("SH-007", "ENC=$(echo -n u:{P} | base64)\ncurl -H \"Authorization: Basic $ENC\" \"$URL\"\n"),
		new("SH-007", "curl -H \"Authorization: Basic $(echo -n {P} | base64)\" \"$URL\"\n", WholeWord: true),
		new("SH-008", "AUTH=\"Basic {P}\"\ncurl -H \"Authorization: $AUTH\" \"$URL\"\n", Quoted: true),
		new("SH-009", "opts=(-u {P})\ncurl \"${opts[@]}\" \"$URL\"\n", WholeWord: true),
		new("SH-010", "API_URL=\"https://u:{P}@h.test/x\"\ncurl -sS \"$API_URL\"\n", Quoted: true),
		new("SH-011", "wget --user=u --password={P} \"$URL\"\n"),
		new("SH-012", "get_basic_auth_header() {\n\techo {P}\n}\n", WholeWord: true),
		new("SH-013", "http -a u:{P} \"$URL\"\n"),
		new("SH-013", "http \"$URL\" \"Authorization:Basic {P}\"\n", Quoted: true),
		new("SH-014", "aria2c --http-user=u --http-passwd={P} \"$URL\"\n"),
		new("SH-015", "lwp-request -C u:{P} \"$URL\"\n"),
		new("SH-016", "wget --auth-no-challenge -e http_password={P} \"$URL\"\n"),
		new("SH-017", "wget -e http_password={P} \"$URL\"\n"),
		new("SH-018", "exec 3<>/dev/tcp/h.test/80\nprintf 'GET / HTTP/1.1\\r\\nAuthorization: Basic {P}\\r\\n\\r\\n' >&3\n", Quoted: true),
		new("SH-019", "cat > \"$cfg\" <<EOF\nuser = \"u:{P}\"\nEOF\ncurl -K \"$cfg\" \"$URL\"\n", Quoted: true),
		new("SH-019", "cat > \"$cfg\" <<EOF\nheader = \"Authorization: Basic {P}\"\nEOF\ncurl -K \"$cfg\" \"$URL\"\n", Quoted: true),
		new("SH-020", "printf 'http_password = {P}\\n' > \"$rc\"\nexport WGETRC=\"$rc\"\n", Quoted: true),
		new("SH-021", "CURL=curl\n$CURL -u u:{P} \"$URL\"\n"),
		new("SH-022", "WGET=wget\n$WGET --password={P} \"$URL\"\n"),
		new("SH-023", "curl --anyauth -u u:{P} \"$URL\"\n"),
		new("SH-024", "pair=u:{P}\nENC=$(printf '%s' \"$pair\" | base64)\ncurl -H \"Authorization: Basic $ENC\" \"$URL\"\n", Quoted: true),
		new("SH-025", "OPTS=\"-u u:{P}\"\ncurl $OPTS \"$URL\"\n", Quoted: true)
	];

	private readonly ITestOutputHelper _output;

	public ShellCredentialCaptureShapeTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void EveryRuleHasATemplateAndEveryTemplateNamesARule()
	{
		ILanguageAnalyzer analyzer = ShellRegisteredExpressions.Analyzer;

		Assert.Equal(
			analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal),
			_templates.Select(template => template.PatternClass).Distinct().Order(StringComparer.Ordinal));
		Assert.All(_templates, template => Assert.Contains(_PROBE_PLACEHOLDER, template.Text, StringComparison.Ordinal));
	}

	/// <summary>Rule (a), the group's text: whatever a credential group captured across the whole probe set holds no <c>$</c>, backtick or backslash.</summary>
	[Fact]
	public void NoCredentialCaptureAcrossTheProbeSetHoldsADollarABacktickOrABackslash()
	{
		int rulesRun = 0;
		int captures = 0;
		List<string> offenders = [];

		foreach (string patternClass in ShellRegisteredExpressions.Analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal))
		{
			rulesRun++;
			Regex expression = ShellRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[patternClass].Expression;

			foreach ((string text, _, _) in TextsFor(patternClass))
			{
				foreach (Match match in expression.Matches(text))
				{
					Group group = match.Groups[_CREDENTIAL_GROUP];

					if (group.Success)
					{
						captures++;

						if (group.Value.Contains(_DOLLAR, StringComparison.Ordinal)
							|| group.Value.Contains(_BACKTICK, StringComparison.Ordinal)
							|| group.Value.Contains(_BACKSLASH, StringComparison.Ordinal))
						{
							offenders.Add($"{patternClass} captured [{group.Value}] in [{text}]");
						}
					}
				}
			}
		}

		_output.WriteLine(
			$"rules: {rulesRun}; probes: {_probes.Length}; templates: {_templates.Length}; texts per rule set: {_templates.Length * _probes.Length}; credential captures read: {captures}");
		Assert.Equal(ShellRegisteredExpressions.Analyzer.DetectionRulesByPatternClass.Count, rulesRun);
		Assert.Empty(offenders);
	}

	/// <summary>Rule (d), the whole word: where the word expands in any part, no match of any rule carries a credential group at all.</summary>
	[Fact]
	public void AWordThatExpandsInAnyPartIsNeverCapturedAsACredentialByAnyRule()
	{
		List<string> offenders = [];

		foreach (string patternClass in ShellRegisteredExpressions.Analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal))
		{
			Regex expression = ShellRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[patternClass].Expression;

			foreach ((string text, Probe probe, Template template) in TextsFor(patternClass).Where(entry => Expands(entry.Probe, entry.Template)))
			{
				if (expression.Matches(text).Any(match => match.Groups[_CREDENTIAL_GROUP].Success))
				{
					offenders.Add($"{patternClass} captured a credential from [{probe.Text}] in [{template.Text}]");
				}
			}
		}

		Assert.Empty(offenders);
	}

	/// <summary>Rule (d) through the engine: the literal-credential signal is false for every construct of every rule over every expanding probe.</summary>
	[Fact]
	public void TheLiteralCredentialSignalIsFalseForEveryRuleOverEveryExpandingProbe()
	{
		ShellRecognitionHarness harness = new();
		List<string> offenders = [];

		foreach (string patternClass in ShellRegisteredExpressions.Analyzer.DetectionRulesByPatternClass.Keys)
		{
			foreach ((string text, Probe probe, Template template) in TextsFor(patternClass).Where(entry => Expands(entry.Probe, entry.Template)))
			{
				offenders.AddRange(harness.Recognize(text)
					.Where(construct => construct.LiteralCredentialSignal)
					.Select(construct => $"{patternClass}: {construct.PatternClassIdentifier} signalled a literal credential for [{probe.Text}] in [{template.Text}]"));
			}
		}

		Assert.Empty(offenders);
	}

	/// <summary>Rule (d) for the probe that does not expand: adjacent quoted parts are literal, and the capture, where there is one, holds none of the three characters.</summary>
	[Fact]
	public void AdjacentQuotedPartsThatDoNotExpandMayBeLiteralAndTheirCaptureIsStillFreeOfTheThreeCharacters()
	{
		Probe adjacent = Assert.Single(_probes, probe => probe.Expansion is Expansion.Never);

		foreach (Template template in _templates)
		{
			Regex expression = ShellRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[template.PatternClass].Expression;
			string text = template.Text.Replace(_PROBE_PLACEHOLDER, adjacent.Text, StringComparison.Ordinal);

			Assert.All(
				expression.Matches(text).Where(match => match.Groups[_CREDENTIAL_GROUP].Success),
				match =>
				{
					Assert.DoesNotContain(_DOLLAR, match.Groups[_CREDENTIAL_GROUP].Value, StringComparison.Ordinal);
					Assert.DoesNotContain(_BACKTICK, match.Groups[_CREDENTIAL_GROUP].Value, StringComparison.Ordinal);
					Assert.DoesNotContain(_BACKSLASH, match.Groups[_CREDENTIAL_GROUP].Value, StringComparison.Ordinal);
				});
		}
	}

	/// <summary>Rule (a) for the scheme group and rule (b): no rule declares a scheme group, so no symbolic verdict can suppress a finding on text that holds an expansion.</summary>
	[Fact]
	public void NoRuleDeclaresASchemeGroupAndNoRuleSuppressesAFindingOnAResolvedScheme()
	{
		Assert.All(ShellRegisteredExpressions.Analyzer.DetectionRulesByPatternClass, entry =>
		{
			Assert.DoesNotContain(_SCHEME_GROUP, entry.Value.Expression.GetGroupNames());
			Assert.Null(entry.Value.SymbolicVerdictInterpretation.SchemeArgumentCaptureGroupName);
			Assert.NotEqual(SymbolicVerdictResponse.SuppressesFinding, entry.Value.SymbolicVerdictInterpretation.WhenResolvedNotMatching);
			Assert.NotEqual(SymbolicVerdictResponse.SuppressesFinding, entry.Value.SymbolicVerdictInterpretation.WhenResolvedMatching);
		});
	}

	/// <summary>Rule (c): the anchor of every class fixture is code; the same fixture as the body of a string that nothing runs fires no construct above Low, and none anchors in code.</summary>
	[Fact]
	public void EveryAnchorIsInCodeAndTheSameTextInAStringBodyDoesNotAnchorInCode()
	{
		ShellRecognitionHarness harness = new();

		foreach ((string patternClass, _, string source) in ShellAnalyzerTests.ClassFixtures())
		{
			FileRecognitionContext inCode = harness.ContextFor(source);
			RecognizedConstruct construct = harness.Recognize(source).First(candidate => candidate.PatternClassIdentifier == patternClass);

			Assert.Equal(LexicalZone.Code, inCode.Mask.ZoneAt(construct.AnchorOffset));

			string quoted = $"{_QUOTING_HERE_DOCUMENT_OPENING}{source}{_QUOTING_HERE_DOCUMENT_CLOSING}";
			FileRecognitionContext inBody = harness.ContextFor(quoted);

			foreach (RecognizedConstruct inBodyConstruct in harness.Recognize(quoted).Where(candidate => candidate.PatternClassIdentifier == patternClass))
			{
				Assert.NotEqual(LexicalZone.Code, inBody.Mask.ZoneAt(inBodyConstruct.AnchorOffset));
				Assert.Equal(ConfidenceLevel.Low, inBodyConstruct.Level);
			}
		}
	}

	private static bool Expands(Probe probe, Template template)
	{
		return probe.Expansion switch
		{
			Expansion.Always => true,
			Expansion.InABareWord => !template.Quoted,
			Expansion.AsAWholeWord => template.WholeWord && !template.Quoted,
			_ => false
		};
	}

	private static IEnumerable<(string Text, Probe Probe, Template Template)> TextsFor(string patternClass)
	{
		return _templates
			.Where(template => template.PatternClass == patternClass)
			.SelectMany(template => _probes.Select(probe => (template.Text.Replace(_PROBE_PLACEHOLDER, probe.Text, StringComparison.Ordinal), probe, template)));
	}
}
