using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE GENERATED-AGAINST-INTERPRETED COMPARISON OF `CS-009`. The shipped expression is source-generated; the interpreted one is built from
/// the same pattern text, the way the engine builds its own cross-check. Over a text the two must agree on every match, on its index and
/// length, and on the participation, index and length of every group the rule reads: the left operand, the credential and the encoder. A
/// group the condition reads that the two engines place differently would let a wrong half decide the level.
/// </summary>
internal static class CSharpNeutralPairDifferential
{
	internal const string LEFT_OPERAND_GROUP = "leftOperand";

	private const string _CSHARP_LANGUAGE_NAME = "CSharp";
	private const string _CS_009 = "CS-009";
	private const string _CREDENTIAL_GROUP = "credential";
	private const string _ENCODER_GROUP = "encoder";
	private const string _TIMEOUT_READING = "TIMEOUT";
	private const string _READING_FORMAT = "{0}:{1}{2}";
	private const string _GROUP_READING_FORMAT = "|{0}={1}:{2}:{3}";
	private const int _MATCH_ENUMERATION_CAP = 100_000;
	private const int _REFERENCE_TIMEOUT_IN_MINUTES = 10;
	private const int _DIFFERENCES_SHOWN = 8;

	private static readonly ImmutableArray<string> _COMPARED_GROUPS = [LEFT_OPERAND_GROUP, _CREDENTIAL_GROUP, _ENCODER_GROUP];

	/// <summary>What one comparison read: the matches compared, how many of them had a left operand, and the differences found.</summary>
	internal sealed record Outcome(long Compared, long WithALeftOperand, ImmutableArray<string> Differences);

	/// <summary>The source-generated expression the analyzer ships for the class.</summary>
	internal static Regex Generated()
	{
		return LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_CSHARP_LANGUAGE_NAME].DetectionRulesByPatternClass[_CS_009].Expression;
	}

	/// <summary>The same pattern read by the interpreter, with a timeout long enough for the largest text.</summary>
	internal static Regex Interpreted(Regex generated)
	{
		return new Regex(generated.ToString(), generated.Options, TimeSpan.FromMinutes(_REFERENCE_TIMEOUT_IN_MINUTES));
	}

	/// <summary>Reads every match of both expressions over the text and names each difference.</summary>
	internal static Outcome Compare(string name, string text, Regex generated, Regex interpreted)
	{
		List<string> reference = Readings(interpreted, text);
		List<string> actual = Readings(generated, text);
		ImmutableArray<string> differences = [];

		if (!reference.SequenceEqual(actual))
		{
			differences = [$"{name}: {reference.Count} matches against {actual.Count}; first difference at {FirstDifference(reference, actual)}"];
		}

		return new Outcome(reference.Count, reference.Count(reading => reading.Contains($"|{LEFT_OPERAND_GROUP}=True:", StringComparison.Ordinal)), differences);
	}

	/// <summary>The differences of several outcomes, ready for an assertion message.</summary>
	internal static string Describe(IEnumerable<string> differences)
	{
		return string.Join(Environment.NewLine, differences.Take(_DIFFERENCES_SHOWN));
	}

	private static List<string> Readings(Regex expression, string text)
	{
		List<string> readings = [];

		try
		{
			Match match = expression.Match(text);

			while (match.Success && readings.Count < _MATCH_ENUMERATION_CAP)
			{
				readings.Add(Describe(match));
				match = match.NextMatch();
			}
		}
		catch (RegexMatchTimeoutException)
		{
			readings.Add(_TIMEOUT_READING);
		}

		return readings;
	}

	private static string Describe(Match match)
	{
		StringBuilder groups = new();

		foreach (string groupName in _COMPARED_GROUPS)
		{
			Group group = match.Groups[groupName];

			groups.Append(string.Format(CultureInfo.InvariantCulture, _GROUP_READING_FORMAT, groupName, group.Success, group.Index, group.Length));
		}

		return string.Format(CultureInfo.InvariantCulture, _READING_FORMAT, match.Index, match.Length, groups);
	}

	private static string FirstDifference(List<string> reference, List<string> actual)
	{
		int index = 0;

		while (index < reference.Count && index < actual.Count && reference[index] == actual[index])
		{
			index++;
		}

		string expected = index < reference.Count ? reference[index] : "(none)";
		string found = index < actual.Count ? actual[index] : "(none)";

		return $"match {index}: expected {expected}, found {found}";
	}
}
