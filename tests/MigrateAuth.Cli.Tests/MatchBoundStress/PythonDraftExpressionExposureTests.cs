using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Lexical;

using Moq;

using Match = System.Text.RegularExpressions.Match;

using Xunit;
using Xunit.Abstractions;

using static MigrateAuth.Cli.Tests.MatchBoundStress.PythonDraftExpressionSet;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE EXPOSURE SCAN AND THE CONTRACT CHECK OF THE DRAFT `Python` EXPRESSIONS. The structural guard against lazy quantifiers (zero defective sites): no draft expression carries a lazy quantifier of any kind (the .NET 10 source generator
/// backtracks wrongly on a lazy bounded loop, wherever it stands), so none stands in an optional group either; every loop is bounded and no window is wider than the widest stated; every window unit is atomic; and every
/// look-behind that carries a bounded loop stands behind a forward guard on a literal of at least four letters (the known problem of a look-behind opened on a frequent match). The one expression allowed an unbounded
/// quantifier is the first-line rule, which is the catalog's own text and reads one line. Beside that the table of figures per expression is recorded where <see cref="REPORT_PATH_VARIABLE_NAME"/> points.
///
/// THE ANALYZER CONTRACT. The draft expressions are accepted by <see cref="AnalyzerContractValidator"/> when they are declared as the catalog declares them (the id, the anchor group, the base level, the
/// self-sufficiency, the symbolic verdicts of `PY-013`, the credential position, the destination `Unknown`; the subsumption relation; the preselection anchors; the flow signals with their
/// kinds and zones; the content markers and the file and folder signals; the first-line expression with the lexical budget as its timeout): the analyzer is a test double assembled from contract types only, over the
/// test-local profile, so no `Python` analyzer is registered and nothing is read from <c>src/</c>. A negative control shows the validator would have said so had a draft expression carried a timeout other than the policy's.
/// </summary>
public sealed partial class PythonDraftExpressionExposureTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_EXPOSURE_REPORT_PYTHON_DRAFT";

	/// <summary>Where the composed text of every draft expression is written, one per line, so that the budget document can record it.</summary>
	internal const string TEXT_REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_EXPRESSION_TEXT_REPORT_PYTHON_DRAFT";

	/// <summary>The widest bound of any loop of any draft expression: the argument window, and the span of the loopback receiver.</summary>
	internal const int WIDEST_BOUND = 1_000;

	private const string _UPPER_BOUND_GROUP = "upper";
	private const string _ATOMIC_GROUP_OPENING = "(?>";
	private const string _WINDOW_LOOP_TEXT = PythonDraftExpressions.WINDOW_QUANTIFIER;
	private const string _PY_PROFILE_NAME = "PythonDraftContractProbe";
	private const string _LANGUAGE_NAME = "Python";
	private const string _PYTHON_WINDOWS_EXTENSION = ".pyw";
	private const string _SCHEME_GROUP = "scheme";
	private const string _SCHEME_TOKEN = "Basic";
	private const int _ESCAPE_WIDTH = 2;
	private const int _OFF_POLICY_TIMEOUT_IN_MILLISECONDS = 1;

	private static readonly HashSet<string> _EXPRESSIONS_ALLOWED_AN_UNBOUNDED_QUANTIFIER = new(StringComparer.Ordinal) { PythonDraftExpressionSet.FIRST_LINE_IDENTIFIER };

	private static readonly ImmutableDictionary<string, (string Name, ConfidenceLevel Level, bool SelfSufficient, string Anchor, bool CarriesACredential)> _CLASSES =
		ImmutableDictionary<string, (string Name, ConfidenceLevel Level, bool SelfSufficient, string Anchor, bool CarriesACredential)>.Empty
			.Add("PY-001", ("BasicAuthHelperConstruction", ConfidenceLevel.High, true, CONSTRUCT_GROUP, true))
			.Add("PY-002", ("NamedClientAuthTuple", ConfidenceLevel.High, true, AUTH_GROUP, true))
			.Add("PY-003", ("AuthorizationHeaderBasicScheme", ConfidenceLevel.High, true, CONSTRUCT_GROUP, true))
			.Add("PY-004", ("UrllibBasicAuthHandlerConstruction", ConfidenceLevel.High, true, CONSTRUCT_GROUP, false))
			.Add("PY-005", ("Urllib3MakeHeadersBasicAuth", ConfidenceLevel.High, true, CONSTRUCT_GROUP, true))
			.Add("PY-006", ("UnnamedAuthTuple", ConfidenceLevel.Medium, false, AUTH_GROUP, true))
			.Add("PY-007", ("Base64UserPasswordPair", ConfidenceLevel.Medium, false, CONSTRUCT_GROUP, true))
			.Add("PY-008", ("AiohttpEncodeBasicAuth", ConfidenceLevel.Medium, false, CONSTRUCT_GROUP, true))
			.Add("PY-009", ("PasswordManagerAddPassword", ConfidenceLevel.Medium, false, CONSTRUCT_GROUP, true))
			.Add("PY-010", ("HttpxNetrcAuth", ConfidenceLevel.Medium, false, CONSTRUCT_GROUP, false))
			.Add("PY-011", ("CredentialsInRequestUrl", ConfidenceLevel.Medium, false, CONSTRUCT_GROUP, true))
			.Add("PY-012", ("UrlUserinfoHeldLiteral", ConfidenceLevel.Medium, false, CONSTRUCT_GROUP, true))
			.Add("PY-013", ("AuthorizationHeaderValueHeldInVariable", ConfidenceLevel.Medium, false, CONSTRUCT_GROUP, false))
			.Add("PY-014", ("BasicLiteralInAuthorizationMapping", ConfidenceLevel.Low, false, CONSTRUCT_GROUP, true));

	/// <summary>The flow signals of section 4A.2 with the kind and the zone each declares.</summary>
	private static readonly ImmutableArray<(string Identifier, string Name, FlowSignalKind Kind, FlowSignalZone Zone)> _SIGNALS =
	[
		("EX-PY-120", "GetpassCall", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("EX-PY-121", "InputPasswordPrompt", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("EX-PY-122", "WebBrowserOpenCall", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("EX-PY-123", "LoopbackRedirectReceiver", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("EX-PY-124", "RequestFormCredentialRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("EX-PY-125", "FastApiCredentialForm", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("EX-PY-126", "DjangoAuthenticateOrLogin", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("EX-PY-127", "FrameworkSessionUse", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("EX-PY-130", "EnvironmentCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("EX-PY-131", "ConfigFileCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("EX-PY-132", "NetrcCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("EX-PY-133", "CredentialNamedLiteralAssignment", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly)
	];

	private static readonly ImmutableArray<string> _PRESELECTION_ANCHORS = ["auth", "b64encode", "encodebytes", "add_password", "://"];

	private readonly ITestOutputHelper _output;

	public PythonDraftExpressionExposureTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[GeneratedRegex(@"\{\d+,(?<upper>\d+)\}")]
	private static partial Regex BoundedQuantifier();

	[GeneratedRegex(@"\(\?<[!=]")]
	private static partial Regex LookBehindOpening();

	[GeneratedRegex(@"\(\?<\w+>")]
	private static partial Regex GroupNameOpening();

	[GeneratedRegex(@"[A-Za-z_]{4,}")]
	private static partial Regex ForwardGuardLiteral();

	[Fact]
	public void NoPythonDraftExpressionCarriesALazyBoundedLoopInsideAnOptionalGroupAndTheExposureTableIsRecorded()
	{
		StringBuilder table = new();
		List<string> exposed = [];

		table.AppendLine("expression\tlazy_bounded_loops\tbounds\toptional_groups\tlazy_loops_in_optional_group\tatomic_groups\tunbounded_quantifiers\tpattern_length\treading");

		foreach ((string identifier, Regex expression) in PythonDraftExpressionSet.AllExpressions)
		{
			string pattern = expression.ToString();
			PatternShape shape = PatternShape.Analyze(pattern);

			table.Append(identifier).Append('\t').Append(shape.LazyBoundedLoops.Count).Append('\t').Append(string.Join(' ', shape.LazyBoundedLoops.Select(loop => loop.Bounds)))
				.Append('\t').Append(shape.OptionalGroupCount).Append('\t').Append(shape.LazyLoopsInOptionalGroups).Append('\t').Append(CountOccurrences(pattern, _ATOMIC_GROUP_OPENING)).Append('\t')
				.Append(CountUnboundedQuantifiers(pattern)).Append('\t').Append(pattern.Length).Append('\t').AppendLine(shape.LazyLoopsInOptionalGroups == 0
					? (shape.LazyBoundedLoops.Count == 0 ? "no lazy bounded loop" : "every lazy bounded loop is in a required position")
					: "EXPOSED");

			if (shape.LazyLoopsInOptionalGroups > 0)
			{
				exposed.Add(identifier);
			}
		}

		_output.WriteLine(table.ToString());

		PythonTimingReportWriter.WriteWhereRequested(REPORT_PATH_VARIABLE_NAME, table.ToString());

		WriteTheExpressionTextWhereRequested();

		Assert.True(exposed.Count == 0, $"Exposed to the lazy-window-in-an-optional-group defect: {string.Join(", ", exposed)}");
	}

	[Fact]
	public void NoPythonDraftExpressionCarriesAnyLazyBoundedQuantifier()
	{
		List<string> sites = [.. PythonDraftExpressionSet.AllExpressions.Select(entry => (entry.Identifier, Count: TypeScriptDraftExpressionExposureTests.CountLazyQuantifiers(entry.Expression.ToString()))).Where(entry => entry.Count > 0).Select(entry => $"{entry.Identifier}: {entry.Count}")];

		Assert.Empty(sites);
	}

	[Fact]
	public void EveryLoopOfEveryPythonDraftExpressionIsBoundedAtTheWidestBoundOrLessAndOnlyTheFirstLineRuleCarriesAnUnboundedQuantifier()
	{
		List<string> unbounded = [];

		foreach ((string identifier, Regex expression) in PythonDraftExpressionSet.AllExpressions)
		{
			string pattern = expression.ToString();
			int widest = BoundedQuantifier().Matches(pattern).Select(match => int.Parse(match.Groups[_UPPER_BOUND_GROUP].Value, CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max();

			if (widest > WIDEST_BOUND)
			{
				unbounded.Add($"{identifier}: a bound of {widest}");
			}

			if (CountUnboundedQuantifiers(pattern) > 0 && !_EXPRESSIONS_ALLOWED_AN_UNBOUNDED_QUANTIFIER.Contains(identifier))
			{
				unbounded.Add($"{identifier}: {CountUnboundedQuantifiers(pattern)} unbounded quantifiers");
			}
		}

		Assert.Empty(unbounded);
		Assert.Equal(PythonDraftExpressions.ARGUMENT_WINDOW_UNITS, WIDEST_BOUND);
	}

	/// <summary>The quantifier texts the patterns are built from are the bounds the constants declare.</summary>
	[Fact]
	public void TheQuantifierTextsOfTheDraftPatternsEqualTheirDeclaredBounds()
	{
		Assert.Equal(PythonDraftExpressions.STRING_BODY_QUANTIFIER, $"{{0,{PythonDraftExpressions.STRING_BODY_CHARACTERS}}}");
		Assert.Equal(PythonDraftExpressions.NESTED_GROUP_QUANTIFIER, $"{{0,{PythonDraftExpressions.NESTED_GROUP_UNITS}}}");
		Assert.Equal(PythonDraftExpressions.WINDOW_QUANTIFIER, $"{{0,{PythonDraftExpressions.ARGUMENT_WINDOW_UNITS}}}");
	}

	[Fact]
	public void EveryWindowUnitOfTheExpressionsThatReadAWindowIsAtomic()
	{
		List<string> notAtomic = [];

		foreach ((string identifier, Regex expression) in PythonDraftExpressionSet.AllExpressions)
		{
			string pattern = expression.ToString();

			if (pattern.Contains(")" + _WINDOW_LOOP_TEXT, StringComparison.Ordinal) && CountOccurrences(pattern, _ATOMIC_GROUP_OPENING) == 0)
			{
				notAtomic.Add(identifier);
			}
		}

		Assert.Empty(notAtomic);
	}

	/// <summary>The window is a loop of atomic units, balanced to a depth of four and bounded at 120 units per level: the unit text of the shared window carries four nested groups, each bounded.</summary>
	[Fact]
	public void TheArgumentWindowIsFourAtomicLevelsEachBoundedAtTheStatedUnits()
	{
		string pattern = PythonDraftExpressionSet.ClassExpressions.Single(entry => entry.Identifier == "EX-PY-005").Expression.ToString();

		Assert.Equal(PythonDraftExpressions.NESTING_DEPTH, CountOccurrences(pattern, $"{{0,{PythonDraftExpressions.NESTED_GROUP_UNITS}}}[)\\]}}]"));
		Assert.Contains($"{{0,{PythonDraftExpressions.ARGUMENT_WINDOW_UNITS}}}", pattern, StringComparison.Ordinal);
		Assert.Contains($"{{0,{PythonDraftExpressions.STRING_BODY_CHARACTERS}}}", pattern, StringComparison.Ordinal);
	}

	/// <summary>
	/// The structural guard for the look-behinds: a look-behind that carries a bounded loop (an expensive one) stands behind a forward guard, that is, a literal of at least four letters that the match has already
	/// read to get there. The literal is read from the text before the look-behind (the names of groups are not counted). A look-behind opened on a frequent match is the defect (the `.get` case).
	/// </summary>
	[Fact]
	public void EveryExpensiveLookBehindOfEveryPythonDraftExpressionStandsBehindAForwardGuardOnALiteral()
	{
		List<string> exposed = [];

		foreach ((string identifier, Regex expression) in PythonDraftExpressionSet.AllExpressions)
		{
			string pattern = expression.ToString();

			foreach (Match opening in LookBehindOpening().Matches(pattern))
			{
				int close = MatchingClose(pattern, opening.Index);
				string body = pattern[opening.Index..close];

				if (body.Contains('{', StringComparison.Ordinal))
				{
					string before = GroupNameOpening().Replace(pattern[..opening.Index], string.Empty);

					if (!ForwardGuardLiteral().IsMatch(before))
					{
						exposed.Add($"{identifier} at {opening.Index}");
					}
				}
			}
		}

		Assert.Empty(exposed);
	}

	[Theory]
	[InlineData(@"a(?<!bcd[^\n]{0,3})e", false)]
	[InlineData(@"(?<!bcd[^\n]{0,3})e", false)]
	[InlineData(@"abcd(?<=x{0,3})", true)]
	[InlineData(@"(?<name>x)(?<!y{0,3})", false)]
	[InlineData(@"(?<!\w)abcde", true)]
	public void TheLookBehindGuardScanReadsWhatItShould(string pattern, bool guarded)
	{
		bool allGuarded = true;

		foreach (Match opening in LookBehindOpening().Matches(pattern))
		{
			string body = pattern[opening.Index..MatchingClose(pattern, opening.Index)];

			if (body.Contains('{', StringComparison.Ordinal) && !ForwardGuardLiteral().IsMatch(GroupNameOpening().Replace(pattern[..opening.Index], string.Empty)))
			{
				allGuarded = false;
			}
		}

		Assert.Equal(guarded, allGuarded);
	}

	/// <summary>Every draft expression carries the timeout of the engine that charges it: the policy match bound for everything the recognition engine evaluates, the lexical budget for the first-line rule.</summary>
	[Fact]
	public void EveryDraftExpressionDeclaresTheTimeoutOfTheEngineThatChargesIt()
	{
		Assert.All(PythonDraftExpressionSet.MatchBoundExpressions, entry => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, entry.Expression.MatchTimeout));
		Assert.All(PythonDraftExpressionSet.LexicalExpressions, entry => Assert.Equal(LexicalEnginePolicy.LEXICAL_BUDGET, entry.Expression.MatchTimeout));
		Assert.All(PythonDraftExpressionSet.AllExpressions, entry => Assert.True(entry.Expression.Options.HasFlag(RegexOptions.CultureInvariant) && entry.Expression.Options.HasFlag(RegexOptions.ExplicitCapture), entry.Identifier));
		Assert.All(PythonDraftExpressionSet.AllExpressions, entry => Assert.False(entry.Expression.Options.HasFlag(RegexOptions.IgnoreCase), entry.Identifier));
	}

	/// <summary>THE CONTRACT CHECK: the draft expressions, declared as the catalog declares them, are accepted by the analyzer-contract validator with no violation.</summary>
	[Fact]
	public void TheDraftExpressionsDeclaredAsTheCatalogDeclaresThemAreAcceptedByTheAnalyzerContractValidatorWithNoViolation()
	{
		ILanguageAnalyzer analyzer = BuildTheDraftAnalyzer(PythonDraftExpressionSet.ClassExpressions);

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(analyzer);

		string firstLineState = analyzer.LanguageIdentification.FirstLineDetectionExpression is null ? "absent" : "declared";

		_output.WriteLine($"AnalyzerContractValidator: {violations.Length} violations over {analyzer.DetectionRulesByPatternClass.Count} classes, {analyzer.CandidateSelectionAndRecommendation.FlowSignals.Length} flow signals, {analyzer.DemotionSignals.ContentMarkers.Length} content markers, {analyzer.SubsumptionRelation.Count} subsumption rules, first-line expression {firstLineState}.");
		Assert.Empty(violations);
	}

	/// <summary>The negative control: a class expression that declares another timeout than the policy bound is reported, so the empty result above is the validator's reading and not a validator that says nothing.</summary>
	[Fact]
	public void TheValidatorReportsADraftClassExpressionThatDeclaresAnotherTimeoutThanThePolicyBound()
	{
		Regex offPolicy = new(PythonDraftExpressionSet.ClassExpressions[0].Expression.ToString(), PythonDraftExpressionSet.ClassExpressions[0].Expression.Options, TimeSpan.FromMilliseconds(_OFF_POLICY_TIMEOUT_IN_MILLISECONDS));
		ImmutableArray<(string Identifier, Regex Expression)> mutated = [(PythonDraftExpressionSet.ClassExpressions[0].Identifier, offPolicy), .. PythonDraftExpressionSet.ClassExpressions.Skip(1)];

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(BuildTheDraftAnalyzer(mutated));

		Assert.Contains(violations, violation => violation.Code == AnalyzerContractViolationCode.DetectionRuleMatchTimeoutNotPolicyBound && violation.Subject == "PY-001");
	}

	[Fact]
	public void TheDraftAnalyzerDeclaresFourteenClassesTwelveSignalsTheTwoContentMarkersAndTheSubsumptionTripleAtIdenticalAuthAnchors()
	{
		ILanguageAnalyzer analyzer = BuildTheDraftAnalyzer(PythonDraftExpressionSet.ClassExpressions);

		Assert.Equal(14, analyzer.DetectionRulesByPatternClass.Count);
		Assert.Equal(12, analyzer.CandidateSelectionAndRecommendation.FlowSignals.Length);
		Assert.Equal(8, analyzer.CandidateSelectionAndRecommendation.FlowSignals.Count(signal => signal.Kind == FlowSignalKind.InteractiveUser));
		Assert.Equal(4, analyzer.CandidateSelectionAndRecommendation.FlowSignals.Count(signal => signal.Kind == FlowSignalKind.FixedCredential));
		Assert.Equal(2, analyzer.DemotionSignals.ContentMarkers.Length);
		Assert.Contains(analyzer.SubsumptionRelation, rule => rule.SubsumingPatternClassIdentifier == "PY-002" && rule.SubsumedPatternClassIdentifier == "PY-006" && rule.When == SubsumptionCondition.AnchorPositionsAreIdentical);
		Assert.Equal(PythonShapedProfile.EXPECTED_QUOTE_RULE_COUNT, analyzer.LexicalProfile.QuoteRulesInEngineOrder.Length);
		Assert.Equal(AUTH_GROUP, analyzer.DetectionRulesByPatternClass["PY-002"].AnchorPosition.CaptureGroupName);
		Assert.Equal(AUTH_GROUP, analyzer.DetectionRulesByPatternClass["PY-006"].AnchorPosition.CaptureGroupName);
		Assert.Equal(SelfSufficiency.DeclaresSchemeAndCredentialCarrier, analyzer.DetectionRulesByPatternClass["PY-002"].SelfSufficiency);
		Assert.Equal(SelfSufficiency.RequiresCallPathToBeEstablished, analyzer.DetectionRulesByPatternClass["PY-006"].SelfSufficiency);
		Assert.Equal(ConfidenceLevel.Medium, analyzer.DetectionRulesByPatternClass["PY-006"].BaseLevel);
	}

	private static ILanguageAnalyzer BuildTheDraftAnalyzer(ImmutableArray<(string Identifier, Regex Expression)> classExpressions)
	{
		Mock<ILanguageAnalyzer> analyzer = new();

		analyzer.SetupGet(supplied => supplied.LexicalProfile).Returns(BuildTheProfile());
		analyzer.SetupGet(supplied => supplied.LanguageIdentification).Returns(BuildTheIdentification());
		analyzer.SetupGet(supplied => supplied.DetectionRulesByPatternClass).Returns(BuildTheRules(classExpressions));
		analyzer.SetupGet(supplied => supplied.DemotionSignals).Returns(BuildTheDemotionSignals());
		analyzer.SetupGet(supplied => supplied.CandidateSelectionAndRecommendation).Returns(BuildTheCandidateSelection());
		analyzer.SetupGet(supplied => supplied.SubsumptionRelation).Returns(BuildTheSubsumptionRelation());

		return analyzer.Object;
	}

	/// <summary>The test-local profile of the Python lexical design with the draft constant declaration in the place of the shaped profile's own.</summary>
	private static LexicalProfile BuildTheProfile()
	{
		LexicalProfile shaped = PythonShapedProfile.Build();

		return new LexicalProfile
		{
			ProfileName = _PY_PROFILE_NAME,
			ConstantDeclarationExpression = PythonDraftExpressionSet.ConstantDeclaration,
			KeywordCaseSensitivity = shaped.KeywordCaseSensitivity,
			DelimiterCaseSensitivity = shaped.DelimiterCaseSensitivity,
			IdentifierCaseSensitivity = shaped.IdentifierCaseSensitivity,
			LineCommentRules = shaped.LineCommentRulesInEngineOrder,
			BlockCommentRules = shaped.BlockCommentRulesInEngineOrder,
			QuoteRules = shaped.QuoteRulesInEngineOrder
		};
	}

	private static LanguageIdentification BuildTheIdentification()
	{
		return new LanguageIdentification
		{
			FirstLineDetectionExpression = PythonDraftExpressionSet.FirstLine,
			FileExtensions = new[] { PythonTestDraftConstants.FILE_EXTENSION, _PYTHON_WINDOWS_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			LanguageNameDerivation = (fileExtension, firstLine) => _LANGUAGE_NAME
		};
	}

	private static FrozenDictionary<string, DetectionRule> BuildTheRules(ImmutableArray<(string Identifier, Regex Expression)> classExpressions)
	{
		Dictionary<string, DetectionRule> rules = new(StringComparer.Ordinal);

		foreach ((string expressionIdentifier, Regex expression) in classExpressions)
		{
			string classIdentifier = expressionIdentifier.Replace("EX-", string.Empty, StringComparison.Ordinal);
			(string name, ConfidenceLevel level, bool selfSufficient, string anchor, bool carriesACredential) = _CLASSES[classIdentifier];
			bool symbolic = classIdentifier == "PY-013";

			rules[classIdentifier] = new DetectionRule
			{
				Expression = expression,
				AnchorPosition = MatchAnchor.AtCaptureGroup(anchor),
				BaseLevel = level,
				SelfSufficiency = selfSufficient ? SelfSufficiency.DeclaresSchemeAndCredentialCarrier : SelfSufficiency.RequiresCallPathToBeEstablished,
				SymbolicVerdictInterpretation = symbolic
					? new SymbolicVerdictInterpretation
					{
						SchemeArgumentCaptureGroupName = _SCHEME_GROUP,
						ExpectedSchemeToken = _SCHEME_TOKEN,
						WhenResolvedMatching = SymbolicVerdictResponse.ConfirmsPatternClass,
						WhenResolvedNotMatching = SymbolicVerdictResponse.SuppressesFinding,
						WhenIndeterminate = SymbolicVerdictResponse.SuppressesFinding
					}
					: SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
				CredentialPosition = carriesACredential ? CredentialPosition.AtCaptureGroup(CREDENTIAL_GROUP) : CredentialPosition.None(),
				DestinationCertainty = DestinationCertainty.Unknown,
				DisplayName = name
			};
		}

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	/// <summary>The demotion signals that the validator reads: the file and folder name signals with their kinds, and the two content markers at the policy bound.</summary>
	private static LanguageShapedDemotionSignals BuildTheDemotionSignals()
	{
		string[] testFolders = ["test", "tests", "testing", "fixture", "fixtures", "e2e"];
		string[] vendoredFolders = ["vendor", "site-packages", "venv", ".venv", "dist-packages", "__pypackages__", ".tox"];

		return new LanguageShapedDemotionSignals
		{
			FileNameSignals = new[]
			{
				new FileNameSignal { Match = FileNameMatch.Prefix, Text = "test_", Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Test },
				new FileNameSignal { Match = FileNameMatch.Suffix, Text = "_test.py", Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Test },
				new FileNameSignal { Match = FileNameMatch.ExactName, Text = "conftest.py", Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Test },
				new FileNameSignal { Match = FileNameMatch.Prefix, Text = "mock_", Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Mock },
				new FileNameSignal { Match = FileNameMatch.Suffix, Text = "_pb2.py", Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Generated }
			}.ToFrozenSet(),
			FolderNameSignals = testFolders.Select(folder => new FolderNameSignal { FolderName = folder, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Test })
				.Concat(vendoredFolders.Select(folder => new FolderNameSignal { FolderName = folder, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Vendored }))
				.ToFrozenSet(),
			GenerationMarkerExpressions = [],
			ContentMarkers =
			[
				new ContentMarker { Expression = PythonDraftExpressionSet.ContentMarkerExpressions[0].Expression, Zone = ContentMarkerZone.CodeOnly, Kind = DemotionKind.Test },
				new ContentMarker { Expression = PythonDraftExpressionSet.ContentMarkerExpressions[1].Expression, Zone = ContentMarkerZone.AnyZone, Kind = DemotionKind.Generated }
			]
		};
	}

	private static CandidateSelectionAndRecommendation BuildTheCandidateSelection()
	{
		ImmutableArray<FlowSignal> signals =
		[
			.. _SIGNALS.Select(signal => new FlowSignal
			{
				Name = signal.Name,
				Kind = signal.Kind,
				Zone = signal.Zone,
				Expression = PythonDraftExpressionSet.FlowSignalExpressions.Single(entry => entry.Identifier == signal.Identifier).Expression
			})
		];

		return new CandidateSelectionAndRecommendation
		{
			PreselectionAnchors = _PRESELECTION_ANCHORS.Select(text => new PreselectionAnchor { Text = text, Sensitivity = CaseSensitivity.Insensitive }).ToFrozenSet(),
			TemplateProvider = (patternClassIdentifier, flow) => null,
			FlowSignals = signals
		};
	}

	private static FrozenSet<SubsumptionRule> BuildTheSubsumptionRelation()
	{
		return PythonDraftExpressionSet.SubsumptionTriples
			.Select(triple => new SubsumptionRule { SubsumingPatternClassIdentifier = triple.Subsuming, SubsumedPatternClassIdentifier = triple.Subsumed, When = triple.When })
			.ToFrozenSet();
	}

	private static void WriteTheExpressionTextWhereRequested()
	{
		string? path = Environment.GetEnvironmentVariable(TEXT_REPORT_PATH_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(path))
		{
			File.WriteAllLines(path, PythonDraftExpressionSet.AllExpressions.Select(entry => $"{entry.Identifier}\t{entry.Expression}"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}

	private static int MatchingClose(string pattern, int opening)
	{
		int depth = 0;
		int index = opening;

		while (index < pattern.Length)
		{
			char current = pattern[index];

			if (current == '\\')
			{
				index += _ESCAPE_WIDTH;
			}
			else if (current == '[')
			{
				index = SkipCharacterClass(pattern, index);
			}
			else
			{
				depth += current == '(' ? 1 : current == ')' ? -1 : 0;
				index++;

				if (depth == 0)
				{
					break;
				}
			}
		}

		return index;
	}

	private static int CountOccurrences(string pattern, string fragment)
	{
		return pattern.Split(fragment, StringSplitOptions.None).Length - 1;
	}

	/// <summary>The number of <c>*</c> and <c>+</c> quantifiers outside character classes and escapes; every other repetition in these expressions carries an explicit bound.</summary>
	private static int CountUnboundedQuantifiers(string pattern)
	{
		int count = 0;
		int index = 0;

		while (index < pattern.Length)
		{
			char current = pattern[index];

			if (current == '\\')
			{
				index += _ESCAPE_WIDTH;
			}
			else if (current == '[')
			{
				index = SkipCharacterClass(pattern, index);
			}
			else
			{
				count += current is '*' or '+' ? 1 : 0;
				index++;
			}
		}

		return count;
	}

	private static int SkipCharacterClass(string pattern, int start)
	{
		int index = start + 1;

		if (index < pattern.Length && pattern[index] == '^')
		{
			index++;
		}

		if (index < pattern.Length && pattern[index] == ']')
		{
			index++;
		}

		while (index < pattern.Length && pattern[index] != ']')
		{
			index += pattern[index] == '\\' ? _ESCAPE_WIDTH : 1;
		}

		return index + 1;
	}
}
