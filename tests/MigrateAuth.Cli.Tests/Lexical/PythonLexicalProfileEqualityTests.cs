using System.Reflection;

using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE SHIPPED `Python` PROFILE READS EVERY TEXT AS THE PROBED PROFILE DOES. The probe verified the test-local draft (<see cref="PythonShapedProfile"/>) against the interpreter's own tokenizer; the profile the
/// binary ships must be that profile. Every text of the numbered list of the probe (found by reflection, so a fixture added there is held here too) and a generated set that crosses every plain, format and template
/// prefix with the four quotes, with a nested field, a doubled brace, a format specification and the predecessors of a prefix and of a hash, is scanned by both, and the two masks must agree position by position, with
/// the same valid-through position, anomaly reason and recovered counters.
/// </summary>
public sealed class PythonLexicalProfileEqualityTests
{
	private const string _FIXTURE_LIST_FIELD = "_fixtureList";
	private const string _TEXT_PROPERTY = "Text";
	private const int _MINIMUM_PROBE_FIXTURES = 60;
	private const int _MINIMUM_GENERATED_TEXTS = 500;

	private static readonly string[] _quotes = ["\"\"\"", "'''", "\"", "'"];

	private static readonly string[] _prefixes =
	[
		"", "r", "R", "b", "B", "u", "U", "rb", "rB", "Rb", "RB", "br", "bR", "Br", "BR",
		"f", "F", "rf", "rF", "Rf", "RF", "fr", "fR", "Fr", "FR",
		"t", "T", "rt", "rT", "Rt", "RT", "tr", "tR", "Tr", "TR",
		"ur", "ub", "bu", "fb", "bf", "uf", "x"
	];

	private static readonly string[] _bodies =
	[
		"ab", "a{n}b", "a{{n}}b", "{d[\"k\"]}", "{d['k']}", "{n!r}", "{n:>{w}}", "{n:#x}", "{n:>#10x}", "{n:+#b}", "{n:'>10}", "{255: #x}", "a\\{n}", "\\N{EM DASH}{n}", "{f\"{y}\"}", "# not a comment", "{n # note\n}", "\\\""
	];

	private static readonly string[] _predecessors = [" ", "(", ",", "=", "\n", "_", "1", "x", "f", "i", ":", "<", ">", "^", "+", "-", "}"];

	[Fact]
	public void TheShippedProfileReadsEveryFixtureOfTheProbeListAsTheProbedProfileDoes()
	{
		List<string> texts = [.. ProbeFixtureTexts()];

		Assert.True(texts.Count >= _MINIMUM_PROBE_FIXTURES, $"Only {texts.Count} probe fixtures were found: the reflection no longer reaches the list.");
		Assert.All(texts, AssertTheTwoProfilesAgree);
	}

	[Fact]
	public void TheShippedProfileReadsEveryPrefixQuoteAndBodyCombinationAsTheProbedProfileDoes()
	{
		List<string> texts = [.. GeneratedTexts()];

		Assert.True(texts.Count >= _MINIMUM_GENERATED_TEXTS);
		Assert.All(texts, AssertTheTwoProfilesAgree);
	}

	[Fact]
	public void TheShippedProfileReadsAHashAndAPrefixAfterEveryPredecessorAsTheProbedProfileDoes()
	{
		foreach (string predecessor in _predecessors)
		{
			AssertTheTwoProfilesAgree($"x = 1{predecessor}# note\ny = 'a'\n");
			AssertTheTwoProfilesAgree($"x = a{predecessor}f\"{{n}}\" if{predecessor}\"z\"\ny = 'a'\n");
			AssertTheTwoProfilesAgree($"if{predecessor}\"a\" else{predecessor}'b' or{predecessor}\"c\"\ny = 'a'\n");
		}
	}

	[Fact]
	public void TheShippedProfileReadsATextWithNoLiteralAndAnEmptyTextAsTheProbedProfileDoes()
	{
		AssertTheTwoProfilesAgree(string.Empty);
		AssertTheTwoProfilesAgree("x = 1\n");
		AssertTheTwoProfilesAgree("# only a comment");
		AssertTheTwoProfilesAgree("\"unterminated");
	}

	private static IEnumerable<string> ProbeFixtureTexts()
	{
		FieldInfo list = typeof(PythonProbeFixtureTests).GetField(_FIXTURE_LIST_FIELD, BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new InvalidOperationException("The probe fixture list was not found.");

		foreach (object row in (System.Collections.IEnumerable)list.GetValue(null)!)
		{
			yield return (string)row.GetType().GetProperty(_TEXT_PROPERTY)!.GetValue(row)!;
		}
	}

	private static IEnumerable<string> GeneratedTexts()
	{
		foreach (string prefix in _prefixes)
		{
			foreach (string quote in _quotes)
			{
				foreach (string body in _bodies)
				{
					yield return $"x = {prefix}{quote}{body}{quote}\ny = 'a'\n";
				}
			}
		}
	}

	private static void AssertTheTwoProfilesAgree(string text)
	{
		ZoneMask shaped = PythonShapedProfile.Scan(text);
		ZoneMask shipped = new LexicalScanner(PythonShippedAnalyzer.Instance.LexicalProfile).Scan(text);
		string context = $"Text: {text.Replace("\n", "\\n", StringComparison.Ordinal)}";

		Assert.True(shaped.Zones.SequenceEqual(shipped.Zones), $"{context}{Environment.NewLine}{ZoneAssertions.Render(shipped, text)}");
		Assert.Equal(shaped.ValidThroughPosition, shipped.ValidThroughPosition);
		Assert.Equal(shaped.AnomalyReason, shipped.AnomalyReason);
		Assert.Equal(shaped.NewlineTerminatedLiteralCount, shipped.NewlineTerminatedLiteralCount);
		Assert.Equal(shaped.UnbalancedNestingCount, shipped.UnbalancedNestingCount);
	}
}
