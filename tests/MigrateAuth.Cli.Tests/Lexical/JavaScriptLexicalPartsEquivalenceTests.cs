using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE EQUIVALENCE OF THE SHIPPED `JavaScript` LEXICAL PROFILE, however it is composed. Two things live here.
///
/// First, the equivalence tests: the shipped profile, however it is composed, reads every fixture the probe holds exactly as the independent test-local profile of
/// <see cref="JavaScriptShapedProfile"/> does, and declares the same quote rules in the same engine order. Second, the zone-mask dump: when
/// <see cref="OUTPUT_VARIABLE_NAME"/> names a path, the zone mask of the SHIPPED profile over every probe fixture, and over every `JavaScript` file under each folder named by
/// <see cref="ROOTS_VARIABLE_NAME"/>, is written to it as one tab-separated row per item with the SHA-256 of the zones. Two dumps taken at two builds are compared with a plain
/// text diff. Without the output variable the dump test does nothing and passes, as the other dump and census harnesses do. Nothing here reads a file unless a root is named.
/// </summary>
public sealed class JavaScriptLexicalPartsEquivalenceTests
{
	internal const string OUTPUT_VARIABLE_NAME = "MIGRATE_AUTH_JS_ZONEMASK_DUMP";

	/// <summary>A list of <c>label=folder</c> entries separated by a semicolon; the label is what the dump prints, so a folder path never reaches the dump.</summary>
	internal const string ROOTS_VARIABLE_NAME = "MIGRATE_AUTH_JS_ZONEMASK_ROOTS";

	private const char _ENTRY_SEPARATOR = ';';
	private const char _LABEL_SEPARATOR = '=';
	private const char _PATH_SEPARATOR = '/';
	private const char _WINDOWS_PATH_SEPARATOR = '\\';
	private const int _ONE_CHARACTER = 1;
	private const int _FIRST_INDEX = 0;
	private const int _ID_COLUMN = 0;
	private const int _PREFIX_COLUMN = 1;
	private const string _PROBED_ID_FORMAT = "D2";
	private const char _TAB = '\t';
	private const string _SKIPPED = "skipped";
	private const string _PROBE_GROUP = "probe";
	private const int _MINIMUM_REFLECTED_FIXTURES = 50;
	private const int _MINIMUM_ENGINE_ORDERED_QUOTE_RULES = 4;
	private const int _NO_GAP_FIXTURE_LENGTH = 0;
	private const string _GAP_PREFIX = "a";
	private const string _GAP_SUFFIX = "/re/ 1;\n";
	private const char _GAP_CHARACTER = '\n';
	private const string _MARKED_HASHBANG = "﻿#!/usr/bin/env node\n'use strict';\nconst b = 2;\n";
	private const string _UNNORMALIZED_CRLF = "const a = 'x \\\r\nnext'; // c\r\nb();\r\n";

	private static readonly string[] _extensions = [".js", ".mjs", ".cjs", ".jsx"];

	[Fact]
	public void TheShippedProfileReadsEveryProbeFixtureAsTheTestLocalProfileDoes()
	{
		List<(string Id, string Source)> fixtures = ProbeFixtures().ToList();

		Assert.True(fixtures.Count >= _MINIMUM_REFLECTED_FIXTURES, $"Only {fixtures.Count} fixtures were found: the reflection over the probe fixtures no longer reaches them.");

		foreach ((string id, string source) in fixtures)
		{
			ZoneMask shipped = ShippedScan(source);
			ZoneMask probed = JavaScriptShapedProfile.Scan(source);

			Assert.True(SameMask(probed, shipped), $"The shipped profile reads {id} differently from the test-local profile: {ZoneAssertions.Render(shipped, source)}");
		}
	}

	[Fact]
	public void TheShippedProfileDeclaresTheQuoteRulesOfTheTestLocalProfileInTheSameEngineOrder()
	{
		LexicalProfile shipped = JavaScriptRegisteredExpressions.Analyzer.LexicalProfile;
		LexicalProfile probed = JavaScriptShapedProfile.Build();

		Assert.True(shipped.QuoteRulesInEngineOrder.Count() >= _MINIMUM_ENGINE_ORDERED_QUOTE_RULES);
		Assert.Equal(DescribeQuoteRules(probed), DescribeQuoteRules(shipped));
		Assert.Equal(DescribeLineCommentRules(probed), DescribeLineCommentRules(shipped));
	}

	[Fact]
	public void TheZoneMaskOfTheShippedProfileIsDumpedWhenAnOutputPathIsNamed()
	{
		string? destination = Environment.GetEnvironmentVariable(OUTPUT_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(destination))
		{
			StringBuilder dump = new();

			dump.AppendLine("group\titem\tlength\tvalid-through\tanomaly\tnewline-terminated\tunbalanced\tzones-sha256");

			foreach ((string id, string source) in ProbeFixtures())
			{
				dump.AppendLine(DumpRow(_PROBE_GROUP, id, source));
			}

			foreach ((string label, string folder) in NamedRoots())
			{
				AppendTheRoot(dump, label, folder);
			}

			File.WriteAllText(destination, dump.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			Assert.True(File.Exists(destination));
		}
	}

	private static void AppendTheRoot(StringBuilder dump, string label, string folder)
	{
		foreach (string path in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
		{
			string relative = Path.GetRelativePath(folder, path).Replace(_WINDOWS_PATH_SEPARATOR, _PATH_SEPARATOR);

			if (!IsInAnExcludedFolder(folder, relative) && _extensions.Any(extension => relative.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
			{
				dump.AppendLine(DumpFile(label, relative, File.ReadAllBytes(path)));
			}
		}
	}

	/// <summary>One file read the way the product reads it: size ceiling, decode, normalize, line ceiling, then the scan.</summary>
	private static string DumpFile(string label, string relative, byte[] bytes)
	{
		string row;

		if (bytes.Length <= ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES && FileDecoding.TryDecode(bytes, out DecodedContent content))
		{
			string text = TextNormalization.Normalize(content.Text);

			row = TextNormalization.TryMeasure(text, out TextMeasurement _) ? DumpRow(label, relative, text) : SkippedRow(label, relative);
		}
		else
		{
			row = SkippedRow(label, relative);
		}

		return row;
	}

	private static string SkippedRow(string label, string item)
	{
		return string.Join(_TAB, label, item, _SKIPPED);
	}

	private static string DumpRow(string group, string item, string source)
	{
		ZoneMask mask = ShippedScan(source);
		byte[] zones = new byte[mask.Length];

		for (int position = _FIRST_INDEX; position < mask.Length; position++)
		{
			zones[position] = (byte)mask.ZoneAt(position);
		}

		return string.Join(
			_TAB,
			group,
			item,
			mask.Length.ToString(CultureInfo.InvariantCulture),
			mask.ValidThroughPosition.ToString(CultureInfo.InvariantCulture),
			mask.AnomalyReason.ToString(),
			mask.NewlineTerminatedLiteralCount.ToString(CultureInfo.InvariantCulture),
			mask.UnbalancedNestingCount.ToString(CultureInfo.InvariantCulture),
			Convert.ToHexString(SHA256.HashData(zones)));
	}

	private static IEnumerable<(string Label, string Folder)> NamedRoots()
	{
		string roots = Environment.GetEnvironmentVariable(ROOTS_VARIABLE_NAME) ?? string.Empty;

		foreach (string entry in roots.Split(_ENTRY_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			int separator = entry.IndexOf(_LABEL_SEPARATOR, StringComparison.Ordinal);

			Assert.True(separator > _FIRST_INDEX, "A root entry is label=folder.");
			yield return (entry[..separator], entry[(separator + _ONE_CHARACTER)..]);
		}
	}

	private static bool IsInAnExcludedFolder(string folder, string relativePath)
	{
		return WalkExclusionEmulation.IsInExcludedFolder(folder, relativePath);
	}

	/// <summary>Every fixture the probe tests hold: each string constant of the probe class (found by reflection, in name order), each row of the backward predicate, each fixture of the shipped-profile tests, the two whitespace gaps and the encoding cases.</summary>
	private static IEnumerable<(string Id, string Source)> ProbeFixtures()
	{
		IEnumerable<(string Id, string Source)> constants = typeof(JavaScriptProbeFixtureTests)
			.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
			.Where(field => field.IsLiteral && field.FieldType == typeof(string))
			.OrderBy(field => field.Name, StringComparer.Ordinal)
			.Select(field => ($"const{field.Name}", (string)field.GetRawConstantValue()!));
		IEnumerable<(string Id, string Source)> predicateRows = JavaScriptProbeFixtureTests.PredicateRows
			.Select(row => ((string)row[_ID_COLUMN], $"{(string)row[_PREFIX_COLUMN]}{_GAP_SUFFIX}"));
		IEnumerable<(string Id, string Source)> probed = JavaScriptLexicalProfileTests.ProbedFixtures
			.Select((row, index) => ($"probed{index.ToString(_PROBED_ID_FORMAT, CultureInfo.InvariantCulture)}", (string)row[_ID_COLUMN]));
		IEnumerable<(string Id, string Source)> gaps =
		[
			("gap-under-the-ceiling", GapFixture(ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _ONE_CHARACTER)),
			("gap-at-the-ceiling", GapFixture(ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS)),
			("gap-none", GapFixture(_NO_GAP_FIXTURE_LENGTH))
		];
		IEnumerable<(string Id, string Source)> encodings =
		[
			("mark-reaching-the-scanner", _MARKED_HASHBANG),
			("crlf-unnormalized", _UNNORMALIZED_CRLF),
			("crlf-normalized", TextNormalization.Normalize(_UNNORMALIZED_CRLF))
		];

		return constants.Concat(predicateRows).Concat(probed).Concat(gaps).Concat(encodings);
	}

	private static string GapFixture(int gapLength)
	{
		return $"{_GAP_PREFIX}{new string(_GAP_CHARACTER, gapLength)}{_GAP_SUFFIX}";
	}

	private static ZoneMask ShippedScan(string source)
	{
		return new LexicalScanner(JavaScriptRegisteredExpressions.Analyzer.LexicalProfile).Scan(source);
	}

	private static bool SameMask(ZoneMask expected, ZoneMask actual)
	{
		return expected.Length == actual.Length
			&& expected.IsCompletelyValid == actual.IsCompletelyValid
			&& expected.ValidThroughPosition == actual.ValidThroughPosition
			&& expected.AnomalyReason == actual.AnomalyReason
			&& expected.NewlineTerminatedLiteralCount == actual.NewlineTerminatedLiteralCount
			&& expected.UnbalancedNestingCount == actual.UnbalancedNestingCount
			&& expected.Zones.SequenceEqual(actual.Zones);
	}

	private static string[] DescribeQuoteRules(LexicalProfile profile)
	{
		return profile.QuoteRulesInEngineOrder
			.Select(rule => string.Join(
				_TAB,
				rule.Opening,
				rule.Closing,
				rule.NewlinePolicy,
				rule.EscapeCharacter,
				rule.InterpolationOpening,
				rule.InterpolationClosing,
				rule.InterpolationNestingOpening,
				rule.SuspendedSubRange?.Opening,
				rule.SuspendedSubRange?.Closing,
				rule.OpeningContextPredicate is not null))
			.ToArray();
	}

	private static string[] DescribeLineCommentRules(LexicalProfile profile)
	{
		return profile.LineCommentRulesInEngineOrder
			.Select(rule => string.Join(_TAB, rule.Opening, rule.OpeningContextPredicate is not null))
			.ToArray();
	}
}
