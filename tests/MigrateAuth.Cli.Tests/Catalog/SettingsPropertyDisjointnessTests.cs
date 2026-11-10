using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// NO SPAN CARRIES BOTH `JS-018` AND `JS-019`: the two classes are disjoint by the expressions themselves (the value of one is an environment read or an identifier that no dot follows, the value of
/// the other a dotted chain that is not an environment read), not by a subsumption triple, so no triple joins them. This class holds the helper that reads the spans of the two shipped
/// expressions over a text, the invented fixtures of every shape on which the two could meet, the byte equality of the guard the two share, and the check that the relation of neither analyzer
/// subsumes one over the other. The same check over the committed scripts of the earlier folders is <see cref="SettingsPropertyEarlierFilesNoOverlapTests"/>.
/// </summary>
public sealed class SettingsPropertyDisjointnessTests
{
	private const string _ENVIRONMENT_CLASS = "JS-018";
	private const string _SETTINGS_CLASS = "JS-019";
	private const string _TYPESCRIPT_LANGUAGE = "TypeScript";
	private const int _MINIMUM_GUARD_LENGTH = 400;
	private const string _MEMBER = "const settings = {\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER\n};\n";

	/// <summary>Texts invented from the forms of the two classes and the shapes at which they could meet: the five carriers, every environment form, a copied name, a dotted chain, a `headers` chain, a quoted key.</summary>
	internal static ImmutableArray<string> InventedFixtures { get; } =
	[
		$"{_MEMBER}await fetch(url, {{ headers: {{ Authorization: settings.serviceAuthorization }} }});\n",
		"await fetch(url, { headers: { Authorization: process.env.SERVICE_AUTHORIZATION } });\n",
		"await fetch(url, { headers: { Authorization: process.env['SERVICE_AUTHORIZATION'] } });\n",
		"await fetch(url, { headers: { Authorization: import.meta.env.VITE_AUTHORIZATION } });\n",
		"await fetch(url, { headers: { Authorization: Bun.env.SERVICE_AUTHORIZATION } });\n",
		"await fetch(url, { headers: { Authorization: Deno.env.get('SERVICE_AUTHORIZATION') } });\n",
		$"{_MEMBER}client.setRequestHeader('Authorization', settings.serviceAuthorization);\n",
		$"{_MEMBER}const h = new Headers({{ Authorization: settings.serviceAuthorization }});\n",
		$"{_MEMBER}h.set('Authorization', settings.serviceAuthorization);\n",
		$"{_MEMBER}axios.defaults.headers.common['Authorization'] = settings.serviceAuthorization;\n",
		$"{_MEMBER}headers.authorization = settings.serviceAuthorization;\n",
		"const serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\nawait fetch(url, { headers: { Authorization: serviceAuthorization } });\n",
		$"{_MEMBER}await fetch(url, {{ headers: {{ Authorization: process.env.SERVICE_AUTHORIZATION }} }});\nawait fetch(url, {{ headers: {{ Authorization: settings.serviceAuthorization }} }});\n",
		"const o = { headers: { Authorization: settings.serviceAuthorization, 'X-Other': process.env.SERVICE_AUTHORIZATION } };\n",
		"await fetch(url, { headers: { Authorization: config.headers.serviceAuthorization } });\n",
		"await fetch(url, { headers: { Authorization: a.b.c.d.e.f.g.h.serviceAuthorization } });\n",
		"await fetch(url, { headers: { Authorization: process.env.env.serviceAuthorization } });\n",
		"await fetch(url, { headers: { Authorization: window.process.env.SERVICE_AUTHORIZATION } });\n",
		"headers['Authorization'] = process.env.SERVICE_AUTHORIZATION;\nheaders['Authorization'] = settings.serviceAuthorization;\n",
		"await fetch(url, { headers: { Authorization: `${process.env.SERVICE_AUTHORIZATION}` } });\n",
		"await fetch(url, { headers: { Authorization: process.env.SERVICE_AUTHORIZATION || 'neutral' } });\n"
	];

	[Theory]
	[InlineData("JavaScript")]
	[InlineData(_TYPESCRIPT_LANGUAGE)]
	public void NoSpanOfTheTwoClassesOverlapsOverTheInventedFixtures(string language)
	{
		(Regex environment, Regex settings) = ExpressionsOf(language);
		int matches = 0;

		foreach (string fixture in InventedFixtures)
		{
			ImmutableArray<string> overlaps = OverlappingSpans(environment, settings, fixture);

			matches += environment.Matches(fixture).Count + settings.Matches(fixture).Count;
			Assert.True(overlaps.IsEmpty, $"{language}: [{fixture}] has overlapping spans: {string.Join("; ", overlaps)}");
		}

		Assert.True(matches > 0, "The fixtures matched nothing at all: the check compared nothing.");
	}

	[Theory]
	[InlineData("JavaScript")]
	[InlineData(_TYPESCRIPT_LANGUAGE)]
	public void TheTwoClassesAreNeverSubsumedOneByTheOtherAndTheGuardTheyShareIsByteIdentical(string language)
	{
		ILanguageAnalyzer analyzer = language == _TYPESCRIPT_LANGUAGE ? TypeScriptRegisteredExpressions.Analyzer : JavaScriptRegisteredExpressions.Analyzer;
		string environmentPattern = analyzer.DetectionRulesByPatternClass[_ENVIRONMENT_CLASS].Expression.ToString();
		string settingsPattern = analyzer.DetectionRulesByPatternClass[_SETTINGS_CLASS].Expression.ToString();

		Assert.DoesNotContain(
			analyzer.SubsumptionRelation,
			rule => rule.SubsumingPatternClassIdentifier is _ENVIRONMENT_CLASS or _SETTINGS_CLASS || rule.SubsumedPatternClassIdentifier is _ENVIRONMENT_CLASS or _SETTINGS_CLASS);
		Assert.True(CommonSuffixLength(environmentPattern, settingsPattern) >= _MINIMUM_GUARD_LENGTH, "The two expressions do not end in the same guard.");
	}

	/// <summary>The environment-class and settings-class expressions of the language's analyzer.</summary>
	internal static (Regex Environment, Regex Settings) ExpressionsOf(string language)
	{
		ILanguageAnalyzer analyzer = language == _TYPESCRIPT_LANGUAGE ? TypeScriptRegisteredExpressions.Analyzer : JavaScriptRegisteredExpressions.Analyzer;

		return (analyzer.DetectionRulesByPatternClass[_ENVIRONMENT_CLASS].Expression, analyzer.DetectionRulesByPatternClass[_SETTINGS_CLASS].Expression);
	}

	/// <summary>Every pair of matches, one of each expression, whose spans share a character, described by their positions. An empty result means the two classes never read the same span.</summary>
	internal static ImmutableArray<string> OverlappingSpans(Regex first, Regex second, string text)
	{
		ImmutableArray<string>.Builder overlaps = ImmutableArray.CreateBuilder<string>();
		List<(int Start, int End)> firstSpans = [.. first.Matches(text).Select(match => (match.Index, match.Index + match.Length))];

		foreach (Match match in second.Matches(text))
		{
			foreach ((int start, int end) in firstSpans)
			{
				if (match.Index < end && start < match.Index + match.Length)
				{
					overlaps.Add($"{start}-{end} and {match.Index}-{match.Index + match.Length}");
				}
			}
		}

		return overlaps.ToImmutable();
	}

	private static int CommonSuffixLength(string first, string second)
	{
		int length = 0;

		while (length < first.Length && length < second.Length && first[first.Length - 1 - length] == second[second.Length - 1 - length])
		{
			length++;
		}

		return length;
	}
}
