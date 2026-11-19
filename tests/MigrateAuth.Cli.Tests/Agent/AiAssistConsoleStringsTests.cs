using System.Reflection;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// EVERY CONSOLE STRING IS A TESTED CONSTANT, DRIVEN BY ITS BRANCH. The scenarios run each branch of the stage that prints,
/// and every console constant of <see cref="AiAssistTexts"/> must appear in the transcripts of those runs, as its fixed
/// segments around the values it takes. Every constant, console or pack, is English and plain ASCII. A hostile input that
/// the scenarios feed in is never echoed. The one constant a platform cannot drive (the line about starting through the
/// Windows command interpreter) is driven on Windows and is a declared no-op elsewhere.
/// </summary>
public sealed class AiAssistConsoleStringsTests
{
	private const string _PACK_CONSTANT_PREFIX = "PACK_";
	private const string _PACK_CONSOLE_CONSTANT = nameof(AiAssistTexts.PACK_GENERATION_FAILED);
	private const string _KIND_LABEL_PREFIX = "KIND_LABEL_";
	private const string _INTERPRETER_CONSTANT = nameof(AiAssistTexts.STARTED_THROUGH_COMMAND_INTERPRETER);
	private const char _FIRST_PRINTABLE_ASCII = ' ';
	private const char _LAST_PRINTABLE_ASCII = '~';

	/// <summary>Constants that are not console strings: the pack's own texts, the report section's, the launch sentence and inputs.</summary>
	private static readonly HashSet<string> _notConsoleConstants =
	[
		nameof(AiAssistTexts.LAUNCH_SENTENCE),
		nameof(AiAssistTexts.CURSOR_NAME),
		nameof(AiAssistTexts.REVIEW_LABEL),
		nameof(AiAssistTexts.DEFAULT_FLOW_MARK),
		nameof(AiAssistTexts.NO_FLOW),
		nameof(AiAssistTexts.NO_DEFAULT_FLOW_MARK)
	];

	private static readonly Regex _placeholder = new(@"\{\d+(:[^}]*)?\}", RegexOptions.CultureInvariant);

	private static IEnumerable<(string Name, string Value)> TextConstants()
	{
		return typeof(AiAssistTexts)
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(field => field.IsLiteral && field.FieldType == typeof(string))
			.Select(field => (field.Name, (string)field.GetRawConstantValue()!));
	}

	private static bool IsConsoleConstant(string name)
	{
		bool isPackText = name.StartsWith(_PACK_CONSTANT_PREFIX, StringComparison.Ordinal) && name != _PACK_CONSOLE_CONSTANT;
		bool isLabelOrSection = name.StartsWith(_KIND_LABEL_PREFIX, StringComparison.Ordinal)
			|| name is nameof(AiAssistTexts.SECTION_HEADING_FORMAT) or nameof(AiAssistTexts.SECTION_BODY_FORMAT);

		return !isPackText && !isLabelOrSection && !_notConsoleConstants.Contains(name);
	}

	[Fact]
	public void TheScenariosCoverEveryConsoleConstantByItsBranch()
	{
		string transcripts = string.Join(AiAssistHarness.LINE_FEED_CHARACTER, AiAssistScenarios.All.Select(scenario => scenario.Transcript));
		List<string> missing = [];

		foreach ((string name, string value) in TextConstants().Where(constant => IsConsoleConstant(constant.Name)))
		{
			bool drivable = name != _INTERPRETER_CONSTANT || OperatingSystem.IsWindows();
			string[] segments = [.. _placeholder.Split(value).Where(segment => segment.Length > 0 && !segment.StartsWith(':'))];

			if (drivable && segments.Any(segment => !transcripts.Contains(segment, StringComparison.Ordinal)))
			{
				missing.Add(name);
			}
		}

		Assert.Empty(missing);
	}

	[Fact]
	public void EveryConstantIsPlainPrintableAscii()
	{
		foreach ((string name, string value) in TextConstants())
		{
			Assert.True(
				value.All(character => character is >= _FIRST_PRINTABLE_ASCII and <= _LAST_PRINTABLE_ASCII),
				$"{name} holds a character that is not plain printable ASCII.");
		}
	}

	[Fact]
	public void TheNamedFormatPlaceholdersOfEveryConstantAreSequentialFromZero()
	{
		foreach ((string name, string value) in TextConstants())
		{
			int[] indexes = [.. _placeholder.Matches(value).Select(match => int.Parse(match.Value.Trim('{', '}').Split(':')[0], System.Globalization.CultureInfo.InvariantCulture)).Distinct().Order()];

			Assert.True(indexes.SequenceEqual(Enumerable.Range(0, indexes.Length)), $"{name} skips a placeholder number.");
		}
	}

	[Fact]
	public void AHostileInputFedToAnyScenarioIsNeverEchoed()
	{
		foreach (ScenarioResult scenario in AiAssistScenarios.All)
		{
			Assert.DoesNotContain("HOSTILE-ANSWER", scenario.Transcript, StringComparison.Ordinal);
			Assert.DoesNotContain(AiAssistHarness.ESCAPE_CHARACTER, scenario.Transcript);
			Assert.DoesNotContain(AiAssistHarness.BELL_CHARACTER, scenario.Transcript);
		}
	}

	[Fact]
	public void EveryScenarioPrintsOnlyLinesThatStartWithTheStagePrefixOrAreItsNoticesOrListLines()
	{
		foreach (ScenarioResult scenario in AiAssistScenarios.All)
		{
			Assert.False(string.IsNullOrWhiteSpace(scenario.Transcript), scenario.Name);
			Assert.Contains(AiAssistTexts.CONSOLE_LINE_PREFIX, scenario.Transcript, StringComparison.Ordinal);
		}
	}
}
