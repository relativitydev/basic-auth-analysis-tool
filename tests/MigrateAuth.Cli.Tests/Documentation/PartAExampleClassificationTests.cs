using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// Part A's examples are the code a reader compares their own against, so each one must be code the tool
/// classifies as the class whose section shows it. The examples are read from the committed document and run
/// through the real embedded analyzer and recognition engine, one example block at a time.
/// </summary>
public sealed class PartAExampleClassificationTests
{
	private const string _LEGACY_CLIENT_PATTERN_CLASS = "CS-002";
	private const string _CSHARP_FENCE_PATTERN = "```csharp\n(?<code>.*?)\n```";
	private const string _CODE_GROUP_NAME = "code";

	/// <summary>The legacy client example, with its address written inline, is classified as the legacy client class and as nothing else.</summary>
	[Fact]
	public void PartAsLegacyClientExampleIsClassifiedAsTheLegacyClientClass()
	{
		ImmutableArray<string> examples = ExamplesOf(_LEGACY_CLIENT_PATTERN_CLASS);

		Assert.NotEmpty(examples);

		foreach (string example in examples)
		{
			RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(example));

			Assert.Equal(_LEGACY_CLIENT_PATTERN_CLASS, construct.PatternClassIdentifier);
			Assert.Equal(ConfidenceLevel.High, construct.Level);
		}
	}

	/// <summary>Every example block in a class's section is recognized as that class.</summary>
	[Fact]
	public void EveryPartAExampleBlockIsClassifiedAsItsOwnClass()
	{
		foreach (KeyValuePair<string, DetectionRule> entry in CSharpRenderedTexts.Analyzer().DetectionRulesByPatternClass.OrderBy(pair => pair.Key, StringComparer.Ordinal))
		{
			foreach (string example in ExamplesOf(entry.Key))
			{
				ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(example);

				Assert.True(
					constructs.Any(construct => construct.PatternClassIdentifier == entry.Key),
					$"Part A's example for {entry.Key} is not classified as {entry.Key}: {example}");
			}
		}
	}

	private static ImmutableArray<string> ExamplesOf(string patternClassIdentifier)
	{
		string partA = File.ReadAllText(PublicDocuments.PartAPath());
		DetectionRule rule = CSharpRenderedTexts.Analyzer().DetectionRulesByPatternClass[patternClassIdentifier];
		string section = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(patternClassIdentifier, rule.DisplayName)) ?? string.Empty;
		ImmutableArray<string>.Builder examples = ImmutableArray.CreateBuilder<string>();

		foreach (Match block in Regex.Matches(section, _CSHARP_FENCE_PATTERN, RegexOptions.Singleline).Cast<Match>())
		{
			examples.Add(block.Groups[_CODE_GROUP_NAME].Value.Trim());
		}

		return examples.ToImmutable();
	}
}
