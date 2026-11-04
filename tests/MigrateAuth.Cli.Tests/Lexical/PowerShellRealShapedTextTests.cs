using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Tests.Scanning;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// The five scanner paths a case-insensitive language needs, each run here on text shaped like real
/// scripts. The text is invented and generic, and the
/// profile is test-local (<see cref="PowerShellShapedProfile"/>): nothing here reads the shipped catalog.
/// The five are the opening context predicate, positional anchoring, case-insensitive preselection anchors,
/// case-insensitive detection expressions, and the first-line detection expression.
/// </summary>
public sealed partial class PowerShellRealShapedTextTests
{
	private const string _LANGUAGE_NAME = "ShapedLanguage";
	private const string _EXTENSION = ".shp";
	private const string _SHEBANG_PATTERN = @"^#!.*\bshaped(?:-core)?\b";
	private const string _UNRELATED_FIRST_LINE = "#!/usr/bin/env unrelated-tool";

	private static readonly string _contextPredicateScript = string.Join(
		"\n",
		"$total = 1 # trailing comment after a space",
		"$list = @(1, 2, 3);# comment right after a semicolon",
		"Get-Item $path |# comment after a pipe",
		"$name = 'a#b'",
		"Write-Host \"tag#1\"",
		"$x = $y#not-a-comment-after-a-word",
		"Write-Host `# an escaped hash is not a comment",
		"Write-Host `\"not a string`\" tail",
		"$z = ``\"abc\"",
		"$end = 1",
		string.Empty);

	private static readonly string _anchoringScript = string.Join(
		"\n",
		"$body = @\"  ",
		"{",
		"  \"name\": \"item\",",
		"  \"kind\": 'x'",
		"}",
		"\"@",
		"Invoke-RestMethod -Uri $uri -Body $body",
		"$sql = @'\t",
		"SELECT * FROM items WHERE kind = 'x'",
		"'@",
		"$note = @\"x\"",
		"$tail = 1",
		string.Empty);

	private static readonly string _detectionScript = string.Join(
		"\n",
		"# Request a token, then call the service.",
		"$uri = \"https://service.invalid/api/items\"",
		"INVOKE-RESTMETHOD -Uri $uri -Method Get",
		"$note = 'Invoke-RestMethod is described here'",
		"invoke-webrequest $uri # same call, other case",
		"<# Invoke-WebRequest in a comment #>",
		string.Empty);

	[GeneratedRegex(@"(?<call>\binvoke-(?:restmethod|webrequest)\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
	private static partial Regex CallExpression();

	[Fact]
	public void TheOpeningContextPredicateDecidesCommentsAndEscapedQuotesInRealShapedText()
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(_contextPredicateScript, countsNestedParentheses: true);
		string script = _contextPredicateScript;

		ZoneAssertions.SpansZone(mask, script, "# trailing comment after a space", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, script, "# comment right after a semicolon", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, script, "# comment after a pipe", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, script, "a#b", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, script, "tag#1", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, script, "$y#not-a-comment-after-a-word", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, script, "Write-Host `# an escaped hash is not a comment", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, script, "Write-Host `\"not a string`\" tail", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, script, "abc", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, script, "$end = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void PositionalAnchoringReadsHereStringsAndTheirNearMissesInRealShapedText()
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(_anchoringScript, countsNestedParentheses: true);
		string script = _anchoringScript;

		ZoneAssertions.SpansZone(mask, script, "  \"name\": \"item\",\n  \"kind\": 'x'\n}\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, script, "\"@", LexicalZone.Delimiter, occurrence: 0);
		ZoneAssertions.SpansZone(mask, script, "Invoke-RestMethod -Uri $uri -Body $body", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, script, "SELECT * FROM items WHERE kind = 'x'", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, script, "'@", LexicalZone.Delimiter);
		int nearMiss = script.IndexOf("@\"x\"", StringComparison.Ordinal);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(nearMiss));
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(nearMiss + 1));
		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(nearMiss + 2));
		ZoneAssertions.SpansZone(mask, script, "$tail = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("Invoke-RestMethod", true)]
	[InlineData("INVOKE-RESTMETHOD -Uri $uri", true)]
	[InlineData("$r = invoke-restmethod -Uri $uri", true)]
	[InlineData("Write-Host 'nothing to see'", false)]
	public void ACaseInsensitiveAnchorSelectsCandidatesInRealShapedText(string line, bool expected)
	{
		ILanguageAnalyzer analyzer = BuildPreselectionAnalyzer();

		Assert.Equal(expected, CandidatePreselection.IsCandidate(analyzer, line + "\n"));
	}

	[Fact]
	public void AnIgnoreCaseExpressionMatchesEveryCasingAndTheMaskPlacesEachMatch()
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(_detectionScript, countsNestedParentheses: true);
		MatchCollection generated = CallExpression().Matches(_detectionScript);
		Regex interpreted = new(CallExpression().ToString(), CallExpression().Options);
		MatchCollection interpretedMatches = interpreted.Matches(_detectionScript);

		LexicalZone[] zones = [.. generated.Select(match => mask.ZoneAt(match.Groups["call"].Index))];

		Assert.Equal(4, generated.Count);
		Assert.Equal(
			[LexicalZone.Code, LexicalZone.StringBody, LexicalZone.Code, LexicalZone.Comment],
			zones);
		Assert.Equal(
			generated.Select(match => (match.Index, match.Length)),
			interpretedMatches.Select(match => (match.Index, match.Length)));
	}

	[Theory]
	[InlineData("#!/usr/bin/env shaped", true)]
	[InlineData("#!/usr/bin/env SHAPED", true)]
	[InlineData("#!/opt/tools/Shaped-Core -NoProfile", true)]
	[InlineData(_UNRELATED_FIRST_LINE, false)]
	[InlineData("#!/usr/bin/env shapedother", false)]
	public void TheFirstLineExpressionAttributesAnExtensionlessFile(string firstLine, bool attributed)
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(BuildAttributionCatalog());

		AttributedLanguage result = attribution.Attribute(harness.PathOf("run-task"), firstLine);

		Assert.Equal(attributed, result.IsRecognized);
		Assert.Equal(attributed ? _LANGUAGE_NAME : LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, result.LanguageName);
	}

	private static ILanguageAnalyzer BuildPreselectionAnalyzer()
	{
		CandidateSelectionAndRecommendation selection = new()
		{
			PreselectionAnchors = new[]
			{
				new PreselectionAnchor { Text = "Invoke-RestMethod", Sensitivity = CaseSensitivity.Insensitive },
				new PreselectionAnchor { Text = "Invoke-WebRequest", Sensitivity = CaseSensitivity.Insensitive }
			}.ToFrozenSet(),
			TemplateProvider = (_, _) => null,
			FlowSignals = ImmutableArray<FlowSignal>.Empty
		};
		Mock<ILanguageAnalyzer> analyzer = new();

		analyzer.SetupGet(supplied => supplied.CandidateSelectionAndRecommendation).Returns(selection);

		return analyzer.Object;
	}

	private static LanguageCatalog BuildAttributionCatalog()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			FirstLineDetectionExpression = new Regex(
				_SHEBANG_PATTERN,
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
				LexicalEnginePolicy.LEXICAL_BUDGET),
			LanguageNameDerivation = (_, _) => _LANGUAGE_NAME
		};
		Mock<ILanguageAnalyzer> analyzer = new();

		analyzer.SetupGet(supplied => supplied.LanguageIdentification).Returns(identification);

		return new LanguageCatalog(LanguageCatalog.CurrentVersion, [analyzer.Object]);
	}
}
