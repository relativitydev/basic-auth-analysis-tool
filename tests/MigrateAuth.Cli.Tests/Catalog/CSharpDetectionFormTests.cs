using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.MatchBoundStress;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE `CSharp` ANALYZER'S CREATION FORMS, NAMED-CONSTANT SCHEMES, IDENTIFIER-ROOTED PAIR HALVES AND OBJECT-INITIALIZER
/// SINKS, THROUGH THE REAL EMBEDDED ANALYZER.
///
/// One invented fixture per form the catalog says is read, run through the real selection, scan and analysis, so the
/// class, the level and the flow are the ones the finding carries; one fixture per form the catalog declares
/// unsupported, producing exactly what its declaration says; and the near-misses that must stay quiet. The source-
/// generated and the interpreted form of every expression these forms exercise must read alike, over every fixture and over
/// the stress shapes. The fixtures are in <see cref="CSharpDetectionFormFixtures"/>.
/// </summary>
public sealed class CSharpDetectionFormTests
{
	private const string _SOURCE_FILE_NAME = "Program.cs";
	private const int _MAXIMUM_ENUMERATION_ITERATIONS = 100_000;
	private const int _EXPECTED_SUBSUMPTION_RULE_COUNT = 5;
	private const int _EXPECTED_PATTERN_CLASS_COUNT = 19;

	private const string _CS_002 = "CS-002";
	private const string _CS_003 = "CS-003";
	private const string _CS_004 = "CS-004";
	private const string _CS_005 = "CS-005";
	private const string _CS_007 = "CS-007";
	private const string _CS_008 = "CS-008";
	private const string _CS_009 = "CS-009";
	private const string _CS_012 = "CS-012";
	private const string _CS_013 = "CS-013";
	private const string _CS_015 = "CS-015";
	private const string _CS_016 = "CS-016";
	private const string _CS_019 = "CS-019";
	private const string _CS_001 = "CS-001";
	private const string _CS_018 = "CS-018";

	/// <summary>Every class whose expression reads the creation-form matrix, the named-constant scheme, the pair halves or the initializer sinks.</summary>
	private static readonly ImmutableArray<string> _classesWithChangedExpressions =
	[
		_CS_002, _CS_003, _CS_004, _CS_005, _CS_007, _CS_008, _CS_009, _CS_012, _CS_013, _CS_015, _CS_016, _CS_019
	];

	/// <summary>The stress shapes that carry the changed expressions' constructs and windows.</summary>
	private static readonly ImmutableArray<string> _stressShapesCarryingTheChangedForms =
	[
		"every-class-at-match-density",
		"open-proximity-windows",
		"creation-form-near-misses"
	];

	/// <summary>One distinctive phrase of each declared-unsupported entry of these forms in the catalog's coverage statement.</summary>
	private static readonly ImmutableArray<string> _declaredConstructPhrases =
	[
		"renames with a \"using\" alias",
		"\"using static\" directive",
		"the shorthand \"new(...)\" where the type name does not stand in front of the variable",
		"credential cache entry added by a collection initializer",
		"object initializer that carries both the user name and the password",
		"scheme named by a constant written with a type or namespace qualifier",
		"concatenated directly",
		"nested call in its argument, a constructed operand or a parenthesized operand",
		"doubled opening brace"
	];

	public static IEnumerable<object[]> CoveredLabels()
	{
		return CSharpDetectionFormFixtures.CoveredForms.Select(form => new object[] { form.Label });
	}

	public static IEnumerable<object[]> DeclaredLabels()
	{
		return CSharpDetectionFormFixtures.DeclaredForms.Select(form => new object[] { form.Label });
	}

	public static IEnumerable<object[]> NearMissLabels()
	{
		return CSharpDetectionFormFixtures.NearMisses.Select(form => new object[] { form.Label });
	}

	public static IEnumerable<object[]> ChangedClasses()
	{
		return _classesWithChangedExpressions.Select(patternClass => new object[] { patternClass });
	}

	// ------------------------------------------------------------------------------------------
	// Covered forms: the class, the level and the flow the catalog names.
	// ------------------------------------------------------------------------------------------

	[Theory]
	[MemberData(nameof(CoveredLabels))]
	public void ACoveredFormIsOneFindingOfTheCatalogsClassLevelAndFlow(string label)
	{
		CoveredForm form = CSharpDetectionFormFixtures.CoveredForms.Single(candidate => candidate.Label == label);

		Finding finding = Assert.Single(FindingsOf(form.Source));

		Assert.Equal(form.PatternClass, finding.PatternClass.Value);
		Assert.Equal(form.Level, finding.Level);

		if (form.Level is ConfidenceLevel.Low)
		{
			Assert.Null(finding.Flow);
		}
		else
		{
			Assert.Equal(form.ExpectedFlowDecision, finding.FlowDecision);
			Assert.Equal(FlowOf(form.ExpectedFlowDecision), finding.Flow);
		}
	}

	[Fact]
	public void EveryCoveredLabelIsUnique()
	{
		ImmutableArray<string> labels = [.. CSharpDetectionFormFixtures.CoveredForms.Select(form => form.Label)];

		Assert.Equal(labels.Length, labels.Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void EveryChangedClassHasAtLeastOneCoveredFormAndTheMatrixCellsAreAllFixtured()
	{
		HashSet<string> classesWithAFixture = [.. CSharpDetectionFormFixtures.CoveredForms.Select(form => form.PatternClass)];

		Assert.All(_classesWithChangedExpressions, patternClass => Assert.Contains(patternClass, classesWithAFixture));
		Assert.Contains(_CS_001, classesWithAFixture);
	}

	// ------------------------------------------------------------------------------------------
	// Declared forms and near-misses: exactly what the catalog says each produces.
	// ------------------------------------------------------------------------------------------

	[Theory]
	[MemberData(nameof(DeclaredLabels))]
	public void ADeclaredFormProducesExactlyWhatItsDeclarationSays(string label)
	{
		ExpectedOutcomeForm form = CSharpDetectionFormFixtures.DeclaredForms.Single(candidate => candidate.Label == label);

		AssertOutcome(form);
	}

	[Theory]
	[MemberData(nameof(NearMissLabels))]
	public void ANearMissStaysAsQuietAsTheCatalogSays(string label)
	{
		ExpectedOutcomeForm form = CSharpDetectionFormFixtures.NearMisses.Single(candidate => candidate.Label == label);

		AssertOutcome(form);
	}

	[Fact]
	public void EveryDeclaredAndNearMissLabelIsUnique()
	{
		ImmutableArray<string> labels =
		[
			.. CSharpDetectionFormFixtures.DeclaredForms.Select(form => form.Label),
			.. CSharpDetectionFormFixtures.NearMisses.Select(form => form.Label)
		];

		Assert.Equal(labels.Length, labels.Distinct(StringComparer.Ordinal).Count());
	}

	// ------------------------------------------------------------------------------------------
	// The named-constant scheme is resolved by its whole spelling, never by its last segment.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void AQualifiedConstantTheFileDeclaresUnderTheSameLastNameIsIndeterminateAndNotSuppressed()
	{
		string bearerDeclared = CSharpDetectionFormFixtures.Lines(
			"const string SCHEME = \"Bearer\";",
			"request.Headers.Add(HttpRequestHeader.Authorization, Other.SCHEME + \" \" + encoded);");
		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(bearerDeclared));

		Assert.Equal(_CS_012, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void TheSchemeGroupOfAQualifiedConstantCoversTheWholeQualifiedSpelling()
	{
		const string source = "request.Headers.Add(HttpRequestHeader.Authorization, global::App.AuthConstants.SCHEME + \" \" + encoded);";
		Regex expression = new CSharpRecognitionHarness().Analyzer.DetectionRulesByPatternClass[_CS_012].Expression;

		Match match = expression.Match(source);

		Assert.True(match.Success);
		Assert.Equal("global::App.AuthConstants.SCHEME", match.Groups["scheme"].Value);
		Assert.Equal("encoded", match.Groups["credential"].Value);
	}

	[Fact]
	public void TheEnumerationMemberAnchorIsTheMemberItselfAfterAnyQualifier()
	{
		const string source = "request.Headers[global::System.Net.HttpRequestHeader.Authorization] = \"Basic \" + encoded;";
		Regex expression = new CSharpRecognitionHarness().Analyzer.DetectionRulesByPatternClass[_CS_012].Expression;

		Match match = expression.Match(source);

		Assert.True(match.Success);
		Assert.Equal("HttpRequestHeader.Authorization", match.Groups["construct"].Value);
	}

	// ------------------------------------------------------------------------------------------
	// The halves of a pair: the whole second half is the credential, and a literal is never a half.
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("string pair = $\"{userBox.Text}:{secretBox.Entry}\";", "secretBox.Entry")]
	[InlineData("string pair = userBox.Text + \":\" + secretBox.Entry;", "secretBox.Entry")]
	[InlineData("string pair = string.Format(\"{0}:{1}\", userBox.Text, ReadSecret(index)[0]);", "ReadSecret(index)[0]")]
	[InlineData("string pair = string.Join(\":\", userBox.Text, parts[1]);", "parts[1]")]
	[InlineData("string pair = userBox.Text + \":\" + Secrets.Read(index)[0][1];", "Secrets.Read(index)[0][1]")]
	public void TheCredentialPositionOfAPairCoversTheWholeSecondHalf(string pair, string expectedCredential)
	{
		string source = CSharpDetectionFormFixtures.Lines(pair, "var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(pair));");
		Regex expression = new CSharpRecognitionHarness().Analyzer.DetectionRulesByPatternClass[_CS_009].Expression;

		Match match = expression.Match(source);

		Assert.True(match.Success);
		Assert.Equal(expectedCredential, match.Groups["credential"].Value);
	}

	[Theory]
	[InlineData("$\"{userBox.Text}:{secretBox.Entry}\"")]
	[InlineData("userBox.Text + \":\" + secretBox.Entry")]
	[InlineData("string.Format(\"{0}:{1}\", userBox.Text, secretBox.Entry)")]
	[InlineData("string.Join(\":\", userBox.Text, secretBox.Entry)")]
	public void TheIdentifierRootedPairIsNeverReadByBothPairClasses(string pairExpression)
	{
		string source = CSharpDetectionFormFixtures.Lines($"string pair = {pairExpression};", "var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(pair));");

		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source);

		Assert.Equal(new[] { _CS_009 }, recognized.Select(construct => construct.PatternClassIdentifier).ToArray());
	}

	[Theory]
	[InlineData("$\"{\"\"}:{secretBox.Entry}\"")]
	[InlineData("\"\" + \":\" + secretBox.Entry")]
	[InlineData("userBox.Text + \":\" + \"\"")]
	public void AnEmptyLiteralHalfIsNeverReadAsAPairByTheNonEmptyPairClass(string pairExpression)
	{
		string source = CSharpDetectionFormFixtures.Lines($"string pair = {pairExpression};", "var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(pair));");

		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(recognized, construct => construct.PatternClassIdentifier == _CS_009);
		Assert.Equal(new[] { _CS_018 }, recognized.Select(construct => construct.PatternClassIdentifier).ToArray());
	}

	// ------------------------------------------------------------------------------------------
	// The contract: the subsumption relation holds, the sink decides, and every class keeps its bound.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void NoSubsumptionTripleWasAddedByTheCreationFormsOrTheInitializerSinks()
	{
		ILanguageAnalyzer analyzer = new CSharpRecognitionHarness().Analyzer;

		Assert.Equal(_EXPECTED_SUBSUMPTION_RULE_COUNT, analyzer.SubsumptionRelation.Count);
		Assert.Equal(_EXPECTED_PATTERN_CLASS_COUNT, analyzer.DetectionRulesByPatternClass.Count);
	}

	[Fact]
	public void TheInitializerSinksAreDecidedByTheSinkSoACredentialInsideOneIsReportedOnceByTheSink()
	{
		string mailer = "var mailer = new SmtpClient(host, port) { Credentials = new NetworkCredential(userName, password) };";
		string handler = "var handler = new HttpClientHandler() { Credentials = new NetworkCredential(userName, password) };";

		RecognizedConstruct sink = Assert.Single(new CSharpRecognitionHarness().Recognize(mailer));
		RecognizedConstruct http = Assert.Single(new CSharpRecognitionHarness().Recognize(handler));

		Assert.Equal((_CS_019, ConfidenceLevel.Low), (sink.PatternClassIdentifier, sink.Level));
		Assert.Equal((_CS_007, ConfidenceLevel.Medium), (http.PatternClassIdentifier, http.Level));
	}

	[Fact]
	public void EveryRuleStillDeclaresThePolicyMatchBound()
	{
		ILanguageAnalyzer analyzer = new CSharpRecognitionHarness().Analyzer;

		Assert.All(
			analyzer.DetectionRulesByPatternClass,
			entry => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, entry.Value.Expression.MatchTimeout));
	}

	[Fact]
	public void TheContractValidatorFindsNoViolationInTheAnalyzer()
	{
		ILanguageAnalyzer analyzer = new CSharpRecognitionHarness().Analyzer;

		Assert.Empty(AnalyzerContractValidator.Validate(analyzer));
	}

	// ------------------------------------------------------------------------------------------
	// The declared forms appear in the coverage statement, in plain words.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EveryDeclaredFormIsStatedInTheDeclaredUnsupportedCoverage()
	{
		ImmutableArray<string> constructs =
		[
			.. new CSharpRecognitionHarness().Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage.Select(entry => entry.Construct)
		];

		Assert.All(
			_declaredConstructPhrases,
			phrase => Assert.Contains(constructs, construct => construct.Contains(phrase, StringComparison.Ordinal)));
	}

	[Fact]
	public void TheDeclaredCoverageNamesNoInternalTermAndEveryEntryIsWrittenForTheReader()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			.. new CSharpRecognitionHarness().Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage
				.Where(entry => _declaredConstructPhrases.Any(phrase => entry.Construct.Contains(phrase, StringComparison.Ordinal)))
		];

		Assert.NotEmpty(entries);
		Assert.All(entries, entry => Assert.DoesNotMatch(@"\bCS-\d+|\bSection\b|\bDR-\d+", entry.Construct + entry.Reason));
		Assert.All(entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Reason)));
	}

	// ------------------------------------------------------------------------------------------
	// The source-generated and the interpreted form of every expression under test read alike.
	// ------------------------------------------------------------------------------------------

	[Theory]
	[MemberData(nameof(ChangedClasses))]
	public void TheGeneratedAndTheInterpretedFormOfAChangedExpressionAgreeOverEveryFixture(string patternClass)
	{
		Regex generated = new CSharpRecognitionHarness().Analyzer.DetectionRulesByPatternClass[patternClass].Expression;
		Regex interpreted = new(generated.ToString(), generated.Options, Regex.InfiniteMatchTimeout);
		ImmutableArray<string> sources =
		[
			.. CSharpDetectionFormFixtures.CoveredForms.Select(form => form.Source),
			.. CSharpDetectionFormFixtures.DeclaredForms.Select(form => form.Source),
			.. CSharpDetectionFormFixtures.NearMisses.Select(form => form.Source)
		];

		foreach (string source in sources)
		{
			string text = RecognitionHarness.Normalize(source);

			Assert.True(Summarize(interpreted, text).SequenceEqual(Summarize(generated, text)), $"{patternClass}: {source}");
		}

		string everything = RecognitionHarness.Normalize(string.Join("\n", sources));

		Assert.True(Summarize(interpreted, everything).SequenceEqual(Summarize(generated, everything)), $"{patternClass}: every fixture together");
	}

	[Theory]
	[MemberData(nameof(ChangedClasses))]
	public void TheGeneratedAndTheInterpretedFormOfAChangedExpressionAgreeOverTheStressShapes(string patternClass)
	{
		Regex generated = new CSharpRecognitionHarness().Analyzer.DetectionRulesByPatternClass[patternClass].Expression;
		Regex interpreted = new(generated.ToString(), generated.Options, Regex.InfiniteMatchTimeout);

		foreach (string shapeName in _stressShapesCarryingTheChangedForms)
		{
			string text = RecognitionHarness.Normalize(MatchBoundStressSet.Build().Single(shape => shape.Name == shapeName).Text);

			Assert.True(Summarize(interpreted, text).SequenceEqual(Summarize(generated, text)), $"{patternClass} over {shapeName}");
		}
	}

	[Theory]
	[MemberData(nameof(ChangedClasses))]
	public void AChangedExpressionsEnumerationAdvancesOverEveryFixtureAndTerminates(string patternClass)
	{
		Regex expression = new CSharpRecognitionHarness().Analyzer.DetectionRulesByPatternClass[patternClass].Expression;
		string everything = RecognitionHarness.Normalize(string.Join("\n", CSharpDetectionFormFixtures.CoveredForms.Select(form => form.Source)));

		Assert.True(MatchBoundTimingHarness.EnumerationAdvances(expression, everything), patternClass);
	}

	// ------------------------------------------------------------------------------------------
	// helpers
	// ------------------------------------------------------------------------------------------

	private static void AssertOutcome(ExpectedOutcomeForm form)
	{
		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(form.Source);
		IEnumerable<(string PatternClass, ConfidenceLevel Level)> actual = recognized
			.Select(construct => (construct.PatternClassIdentifier, construct.Level))
			.Order();

		Assert.Equal(form.Outcome.Order(), actual);
	}

	private static ImmutableArray<Finding> FindingsOf(string source)
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.WriteSourceFile(_SOURCE_FILE_NAME, source);
		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		return harness.State.Findings;
	}

	private static CredentialFlow FlowOf(FlowDecision decision)
	{
		return decision is FlowDecision.InteractiveSignal ? CredentialFlow.AuthorizationCode : CredentialFlow.ClientCredentials;
	}

	/// <summary>Every match with its position and the position of each named group, so a difference in what a group captured is a difference here.</summary>
	private static ImmutableArray<string> Summarize(Regex expression, string text)
	{
		List<string> summary = [];
		Match match = expression.Match(text);

		while (match.Success && summary.Count < _MAXIMUM_ENUMERATION_ITERATIONS)
		{
			string groups = string.Join(
				";",
				expression.GetGroupNames()
					.Where(name => !int.TryParse(name, out _))
					.Select(name => $"{name}={(match.Groups[name].Success ? $"{match.Groups[name].Index}+{match.Groups[name].Length}" : "none")}"));

			summary.Add($"{match.Index}+{match.Length}|{groups}");
			match = match.NextMatch();
		}

		return [.. summary];
	}
}
