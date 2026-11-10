using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// THE ORDER OF THE LOW-REASON VALUES, HELD IN THE CODE AND IN THE TWO PLACES THE USER GUIDE LISTS THEM.
///
/// The artifact writes the values in one declared order, and the guide's two rows name them in that order so a reader
/// can compare the two without sorting. A value added to the vocabulary and left out of a row, or a row that lists the
/// values in another order, fails here.
/// </summary>
public sealed class LowReasonOrderDocumentationTests
{
	private const string _FINDING_FIELD_ROW_PREFIX = "| `lowReasons` |";
	private const string _ENUMERATED_VALUES_ROW_PREFIX = "| `findings[].lowReasons` |";
	private const string _CODE_SPAN_PATTERN = "`([^`]+)`";
	private const char _LINE_BREAK = '\n';

	private static readonly string[] _declaredOrder =
	[
		"patternClass",
		"commentOrStringLiteral",
		"demotedMockFile",
		"demotedSampleFile",
		"demotedVendoredFile",
		"demotedGeneratedFile",
		"demotedDeploymentFolder",
		"neutralOperandPair",
		"inactivePreprocessorRegion"
	];

	/// <summary>The code declares the values in the order the guide is held to, whatever order the flags are combined in.</summary>
	[Fact]
	public void TheVocabularyDeclaresTheValuesInTheOrderTheGuideIsHeldTo()
	{
		LowConfidenceReasons everyReason = Enum.GetValues<LowConfidenceReasons>().Aggregate(LowConfidenceReasons.None, (all, reason) => all | reason);

		Assert.Equal(_declaredOrder, LowReasonVocabulary.ValuesOf(everyReason));
	}

	[Theory]
	[InlineData(_FINDING_FIELD_ROW_PREFIX)]
	[InlineData(_ENUMERATED_VALUES_ROW_PREFIX)]
	public void TheGuideRowListsEveryLowReasonValueInTheDeclaredOrder(string rowPrefix)
	{
		string guide = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_USING_THE_TOOL);
		string row = guide.Split(_LINE_BREAK).Single(line => line.StartsWith(rowPrefix, StringComparison.Ordinal));

		string[] valuesInOrderOfFirstMention =
		[
			.. Regex.Matches(row, _CODE_SPAN_PATTERN)
				.Select(match => match.Groups[1].Value)
				.Where(span => _declaredOrder.Contains(span))
				.Distinct()
		];

		Assert.Equal(_declaredOrder, valuesInOrderOfFirstMention);
	}
}
