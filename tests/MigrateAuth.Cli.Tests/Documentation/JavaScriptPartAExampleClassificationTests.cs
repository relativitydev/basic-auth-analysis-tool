using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// Part A of JavaScript shows a placeholder example for each class, and a reader compares their own code with it, so each example must be code the tool
/// classifies as the class whose section shows it. The examples are read from the committed document and run through the real embedded analyzer and recognition
/// engine, one example block at a time, exactly as the examples of the other languages are.
/// </summary>
public sealed class JavaScriptPartAExampleClassificationTests
{
	private const string _JAVASCRIPT_FENCE_PATTERN = "```javascript\n(?<code>.*?)\n```";
	private const string _CODE_GROUP_NAME = "code";

	/// <summary>Every class has an example, and every example block of its section is recognized as that class.</summary>
	[Fact]
	public void EveryPartAExampleBlockIsClassifiedAsItsOwnClass()
	{
		foreach (KeyValuePair<string, DetectionRule> entry in DocumentedLanguage.JavaScript.Analyzer().DetectionRulesByPatternClass.OrderBy(pair => pair.Key, StringComparer.Ordinal))
		{
			ImmutableArray<string> examples = ExamplesOf(entry.Key, entry.Value);

			Assert.NotEmpty(examples);

			foreach (string example in examples)
			{
				ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(example);

				Assert.True(
					constructs.Any(construct => construct.PatternClassIdentifier == entry.Key),
					$"Part A's example for {entry.Key} is not classified as {entry.Key}: {example}");
			}
		}
	}

	private static ImmutableArray<string> ExamplesOf(string patternClassIdentifier, DetectionRule rule)
	{
		string partA = File.ReadAllText(DocumentedLanguage.JavaScript.PartAPath());
		string section = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(patternClassIdentifier, rule.DisplayName)) ?? string.Empty;
		ImmutableArray<string>.Builder examples = ImmutableArray.CreateBuilder<string>();

		foreach (Match block in Regex.Matches(section, _JAVASCRIPT_FENCE_PATTERN, RegexOptions.Singleline))
		{
			examples.Add(block.Groups[_CODE_GROUP_NAME].Value.Trim());
		}

		return examples.ToImmutable();
	}
}
