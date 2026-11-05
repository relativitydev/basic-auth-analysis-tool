using System.Collections.Frozen;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.MatchBoundStress;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SHIPPED CLASS EXPRESSIONS AGAINST THE SAMPLES OF THE DRAFT EXPRESSIONS. Each sample names a shape (the credential forms, the near-misses an
/// expression refuses, the windows at and beyond their bounds), and what the analyzer ships must mean what the draft meant. Every sample of a pattern class
/// is run against the expression the analyzer declares for that class, read from the binary: the same verdict, and the same text in the capture group where
/// the sample names one. The generated and the interpreted form of each expression must agree on every sample.
/// </summary>
public sealed class ShellShippedExpressionSemanticTests
{
	/// <summary>The draft identifier of the expression each pattern class ships, as the catalog's declaration table states it.</summary>
	private static readonly FrozenDictionary<string, string> _classByDraftIdentifier = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["EX-SH-05"] = "SH-001",
		["EX-SH-06"] = "SH-002",
		["EX-SH-07"] = "SH-003",
		["EX-SH-08"] = "SH-004",
		["EX-SH-10"] = "SH-005",
		["EX-SH-11"] = "SH-006",
		["EX-SH-12"] = "SH-007",
		["EX-SH-13"] = "SH-008",
		["EX-SH-14"] = "SH-009",
		["EX-SH-15"] = "SH-010",
		["EX-SH-09"] = "SH-011",
		["EX-SH-16"] = "SH-012",
		["EX-SH-32"] = "SH-013",
		["EX-SH-34"] = "SH-014",
		["EX-SH-35"] = "SH-015",
		["EX-SH-36"] = "SH-016",
		["EX-SH-37"] = "SH-017",
		["EX-SH-38"] = "SH-018",
		["EX-SH-39"] = "SH-019",
		["EX-SH-40"] = "SH-020",
		["EX-SH-41"] = "SH-021",
		["EX-SH-42"] = "SH-022",
		["EX-SH-43"] = "SH-023",
		["EX-SH-44"] = "SH-024",
		["EX-SH-46"] = "SH-025"
	}.ToFrozenDictionary(StringComparer.Ordinal);

	private static readonly ShellDraftExpressionSemanticTests.Case[] _classCases =
	[
		.. ShellDraftExpressionSemanticTests.Cases.Where(sample => _classByDraftIdentifier.ContainsKey(sample.Id))
	];

	[Fact]
	public void EveryPatternClassHasPositiveAndNegativeSamples()
	{
		Assert.All(_classByDraftIdentifier.Keys, identifier =>
		{
			Assert.Contains(_classCases, sample => sample.Id == identifier && sample.Matches);
			Assert.Contains(_classCases, sample => sample.Id == identifier && !sample.Matches);
		});
	}

	[Fact]
	public void EveryShippedClassExpressionGivesTheVerdictAndTheCaptureTheSampleStates()
	{
		List<string> wrong = [];

		foreach (ShellDraftExpressionSemanticTests.Case sample in _classCases)
		{
			Match match = ExpressionOf(sample.Id).Match(sample.Text, sample.Start);
			bool captured = sample.Group.Length == 0 || (match.Success && match.Groups[sample.Group].Value == sample.Value);

			if (match.Success != sample.Matches || !captured)
			{
				wrong.Add($"{_classByDraftIdentifier[sample.Id]} on [{Escape(sample.Text)}] expected {sample.Matches} {sample.Group}={sample.Value}, got {match.Success}");
			}
		}

		Assert.Empty(wrong);
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfEveryShippedClassExpressionAgreeOnEverySample()
	{
		List<string> disagreements = [];

		foreach (ShellDraftExpressionSemanticTests.Case sample in _classCases)
		{
			Regex generated = ExpressionOf(sample.Id);
			Regex interpreted = ShellRegisteredExpressions.Interpreted(generated);

			if (generated.Match(sample.Text, sample.Start).Success != interpreted.Match(sample.Text, sample.Start).Success)
			{
				disagreements.Add($"{_classByDraftIdentifier[sample.Id]} on {Escape(sample.Text)}");
			}
		}

		Assert.Empty(disagreements);
	}

	private static Regex ExpressionOf(string draftIdentifier)
	{
		return ShellRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[_classByDraftIdentifier[draftIdentifier]].Expression;
	}

	private static string Escape(string text)
	{
		return text.Replace("\n", "\\n", StringComparison.Ordinal);
	}
}
