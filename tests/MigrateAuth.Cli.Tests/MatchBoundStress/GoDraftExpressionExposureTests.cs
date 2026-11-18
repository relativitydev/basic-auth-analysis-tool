using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Lexical;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE EXPOSURE SCAN OF THE `Go` DRAFT EXPRESSIONS: the defect it looks for is a lazy bounded loop wrapped in an optional group, which makes the source-generated matcher backtrack past its bound
/// (the structural guard of the generator defect). Each expression's pattern is read for its lazy bounded loops and its optional groups, and none may be inside the other. Beside that it records, for every
/// expression, the figures the budget's row states: the bound of its widest window, the number of atomic groups, and the number of unbounded quantifiers (the constant declaration, which reads one line,
/// is the only expression that carries any). The table is written where <see cref="REPORT_PATH_VARIABLE_NAME"/> points.
///
/// IT ALSO EXPOSES THE DRAFT TO THE ENGINE'S OWN CONSUMER OF THE ANALYZER CONTRACT: <see cref="GoDraftAnalyzerContract"/> is a test-local analyzer built from the test-local `Go` profile, the draft
/// expressions and the catalog's declared data (levels, self-sufficiency, anchors, credential positions, symbolic verdicts, subsumption triples, demotion signals with kinds, preselection anchors, flow signals),
/// and <see cref="AnalyzerContractValidator"/> must report no violation: nothing in the catalog needs a contract element the engine does not have.
/// </summary>
public sealed partial class GoDraftExpressionExposureTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_EXPOSURE_REPORT_GO";

	/// <summary>The widest window the catalog allows: the argument window, bounded at 1,500 units.</summary>
	internal const int WIDEST_WINDOW_BOUND = 1_500;

	private const string _UPPER_BOUND_GROUP = "upper";
	private const int _ESCAPE_STRIDE = 2;
	private const int _CLASS_COUNT = 13;
	private const int _HIGH_OR_MEDIUM_CLASS_COUNT = 10;
	private const int _LOW_CLASS_COUNT = 3;
	private const int _SUBSUMPTION_TRIPLE_COUNT = 3;

	private static readonly string _ARGUMENT_WINDOW_LOOP = $"){{0,{WIDEST_WINDOW_BOUND}}}?";

	private static readonly HashSet<string> _EXPRESSIONS_ALLOWED_AN_UNBOUNDED_QUANTIFIER = new(StringComparer.Ordinal) { "EX-GO-101" };

	private readonly ITestOutputHelper _output;

	public GoDraftExpressionExposureTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[GeneratedRegex(@"\{\d+,(?<upper>\d+)\}", RegexOptions.None, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	private static partial Regex BoundedQuantifier();

	[Fact]
	public void NoGoDraftExpressionCarriesALazyBoundedLoopInsideAnOptionalGroupAndTheExposureTableIsRecorded()
	{
		StringBuilder table = new();
		List<string> exposed = [];

		table.AppendLine("expression\tlazy_bounded_loops\tbounds\toptional_groups\tlazy_loops_in_optional_group\tatomic_groups\tunbounded_quantifiers\tpattern_length\treading");

		foreach ((string identifier, Regex expression) in GoDraftExpressions.MatchBoundExpressions)
		{
			string pattern = expression.ToString();
			PatternShape shape = PatternShape.Analyze(pattern);

			table.Append(identifier).Append('\t').Append(shape.LazyBoundedLoops.Count).Append('\t').Append(string.Join(' ', shape.LazyBoundedLoops.Select(loop => loop.Bounds)))
				.Append('\t').Append(shape.OptionalGroupCount).Append('\t').Append(shape.LazyLoopsInOptionalGroups).Append('\t').Append(CountAtomicGroups(pattern)).Append('\t')
				.Append(CountUnboundedQuantifiers(pattern)).Append('\t').Append(pattern.Length).Append('\t').AppendLine(shape.LazyLoopsInOptionalGroups == 0
					? (shape.LazyBoundedLoops.Count == 0 ? "no lazy bounded loop" : "every lazy bounded loop is in a required position")
					: "EXPOSED");

			if (shape.LazyLoopsInOptionalGroups > 0)
			{
				exposed.Add(identifier);
			}
		}

		_output.WriteLine(table.ToString());

		string? path = Environment.GetEnvironmentVariable(REPORT_PATH_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(path))
		{
			File.WriteAllText(path, table.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}

		Assert.True(exposed.Count == 0, $"Exposed to the lazy-window-in-an-optional-group defect: {string.Join(", ", exposed)}");
	}

	[Fact]
	public void EveryWindowOfEveryGoDraftExpressionIsBoundedAtTheWidestWindowOrLess()
	{
		List<string> unbounded = [];

		foreach ((string identifier, Regex expression) in GoDraftExpressions.MatchBoundExpressions)
		{
			string pattern = expression.ToString();
			int widest = BoundedQuantifier().Matches(pattern).Select(match => int.Parse(match.Groups[_UPPER_BOUND_GROUP].Value, System.Globalization.CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max();

			if (widest > WIDEST_WINDOW_BOUND)
			{
				unbounded.Add($"{identifier}: a bound of {widest}");
			}

			if (CountUnboundedQuantifiers(pattern) > 0 && !_EXPRESSIONS_ALLOWED_AN_UNBOUNDED_QUANTIFIER.Contains(identifier))
			{
				unbounded.Add($"{identifier}: {CountUnboundedQuantifiers(pattern)} unbounded quantifiers");
			}
		}

		Assert.Empty(unbounded);
	}

	[Fact]
	public void EveryWindowUnitOfTheClassExpressionsThatReadAWindowIsAtomic()
	{
		List<string> notAtomic = [];

		foreach ((string identifier, Regex expression) in GoDraftExpressions.MatchBoundExpressions)
		{
			string pattern = expression.ToString();
			bool readsAWindow = pattern.Contains(_ARGUMENT_WINDOW_LOOP, StringComparison.Ordinal);

			if (readsAWindow && CountAtomicGroups(pattern) == 0)
			{
				notAtomic.Add(identifier);
			}
		}

		Assert.Empty(notAtomic);
	}

	[Fact]
	public void TheAnalyzerContractValidatorReportsNoViolationForAnAnalyzerBuiltFromTheDraftProfileAndTheDraftExpressions()
	{
		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(new GoDraftAnalyzerContract());

		Assert.True(violations.IsEmpty, string.Join("; ", violations.Select(violation => $"{violation.Code}:{violation.Subject}")));
	}

	[Fact]
	public void TheDraftContractDeclaresThirteenClassesElevenFlowSignalsOfBothKindsAndThreeSubsumptionTriples()
	{
		GoDraftAnalyzerContract analyzer = new();
		ImmutableArray<FlowSignal> signals = analyzer.CandidateSelectionAndRecommendation.FlowSignals;

		Assert.Equal(_CLASS_COUNT, analyzer.DetectionRulesByPatternClass.Count);
		Assert.Equal(_HIGH_OR_MEDIUM_CLASS_COUNT, analyzer.DetectionRulesByPatternClass.Count(entry => entry.Value.BaseLevel != ConfidenceLevel.Low));
		Assert.Equal(_LOW_CLASS_COUNT, analyzer.DetectionRulesByPatternClass.Count(entry => entry.Value.BaseLevel == ConfidenceLevel.Low));
		Assert.Equal(11, signals.Length);
		Assert.Equal(6, signals.Count(signal => signal.Kind == FlowSignalKind.InteractiveUser));
		Assert.Equal(5, signals.Count(signal => signal.Kind == FlowSignalKind.FixedCredential));
		Assert.Equal(_SUBSUMPTION_TRIPLE_COUNT, analyzer.SubsumptionRelation.Count);
		Assert.Null(analyzer.LanguageIdentification.FirstLineDetectionExpression);
		Assert.Null(analyzer.CandidateSelectionAndRecommendation.FileDialects);
	}

	private static int CountAtomicGroups(string pattern)
	{
		return pattern.Split("(?>", StringSplitOptions.None).Length - 1;
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
				index += _ESCAPE_STRIDE;
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
			index += pattern[index] == '\\' ? _ESCAPE_STRIDE : 1;
		}

		return index + 1;
	}
}

/// <summary>
/// A TEST-LOCAL `Go` ANALYZER, assembled from the test-local lexical profile (<see cref="GoShapedProfile"/>, with the draft constant declaration in place of its stand-in), the draft expressions
/// and the declared data of the draft catalog (the contract declaration, the demotion signals, the preselection anchors and the flow signals). It exists to be handed to <see cref="AnalyzerContractValidator"/>; it is not the analyzer
/// of the product and is never read from <c>src/</c>. No template is declared: the recommendation templates are tested elsewhere.
/// </summary>
internal sealed class GoDraftAnalyzerContract : ILanguageAnalyzer
{
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _CREDENTIAL_GROUP = "credential";
	private const string _SCHEME_GROUP = "scheme";
	private const string _CALLED_NAME_GROUP = "name";
	private const string _BASIC_SCHEME_TOKEN = "Basic";
	private const string _LANGUAGE_NAME = "Go";
	private const string _FILE_EXTENSION = ".go";

	private readonly Lazy<FrozenDictionary<string, DetectionRule>> _rules = new(BuildRules);

	public LexicalProfile LexicalProfile { get; } = BuildProfile();

	public LanguageIdentification LanguageIdentification { get; } = new()
	{
		FileExtensions = FrozenSet.ToFrozenSet([_FILE_EXTENSION], StringComparer.Ordinal),
		DeclaredLanguageNames = FrozenSet.ToFrozenSet([_LANGUAGE_NAME], StringComparer.Ordinal),
		FirstLineDetectionExpression = null,
		LanguageNameDerivation = (_, _) => _LANGUAGE_NAME
	};

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass => _rules.Value;

	public LanguageShapedDemotionSignals DemotionSignals { get; } = BuildDemotionSignals();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } = BuildCandidateSelection();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = BuildSubsumption();

	private static LexicalProfile BuildProfile()
	{
		LexicalProfile shaped = GoShapedProfile.Build();

		return new LexicalProfile
		{
			ProfileName = shaped.ProfileName,
			ConstantDeclarationExpression = GoDraftExpressions.Ex101(),
				MemberDeclarationExpression = GoDraftExpressions.Ex102(),
			KeywordCaseSensitivity = shaped.KeywordCaseSensitivity,
			DelimiterCaseSensitivity = shaped.DelimiterCaseSensitivity,
			IdentifierCaseSensitivity = shaped.IdentifierCaseSensitivity,
			LineCommentRules = shaped.LineCommentRulesInEngineOrder,
			BlockCommentRules = shaped.BlockCommentRulesInEngineOrder,
			QuoteRules = shaped.QuoteRulesInEngineOrder
		};
	}

	private static DetectionRule Rule(string identifier, Regex expression, ConfidenceLevel level, SelfSufficiency selfSufficiency, CredentialPosition credential, SymbolicVerdictInterpretation? symbolic = null)
	{
		return new DetectionRule
		{
			Expression = expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_GROUP),
			BaseLevel = level,
			SelfSufficiency = selfSufficiency,
			SymbolicVerdictInterpretation = symbolic ?? SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = credential,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = identifier
		};
	}

	private static FrozenDictionary<string, DetectionRule> BuildRules()
	{
		CredentialPosition literal = CredentialPosition.AtBareWordOrLiteralCaptureGroup(_CREDENTIAL_GROUP);
		CredentialPosition none = CredentialPosition.None();
		SelfSufficiency declares = SelfSufficiency.DeclaresSchemeAndCredentialCarrier;
		SelfSufficiency requiresPath = SelfSufficiency.RequiresCallPathToBeEstablished;
		SymbolicVerdictInterpretation basicScheme = new()
		{
			SchemeArgumentCaptureGroupName = _SCHEME_GROUP,
			ExpectedSchemeToken = _BASIC_SCHEME_TOKEN,
			WhenResolvedMatching = SymbolicVerdictResponse.ConfirmsPatternClass,
			WhenResolvedNotMatching = SymbolicVerdictResponse.SuppressesFinding,
			WhenIndeterminate = SymbolicVerdictResponse.SuppressesFinding
		};
		SymbolicVerdictInterpretation environmentSource = new()
		{
			SchemeArgumentCaptureGroupName = _SCHEME_GROUP,
			ExpectedSourceKind = SymbolSourceKind.Environment,
			WhenResolvedMatching = SymbolicVerdictResponse.ConfirmsPatternClass,
			WhenResolvedNotMatching = SymbolicVerdictResponse.SuppressesFinding,
			WhenIndeterminate = SymbolicVerdictResponse.SuppressesFinding
		};
		SymbolicVerdictInterpretation localHelper = new()
		{
			SchemeArgumentCaptureGroupName = _CALLED_NAME_GROUP,
			ExpectedSourceKind = SymbolSourceKind.Environment,
			SymbolRecordKind = SymbolRecordKind.Member,
			WhenResolvedMatching = SymbolicVerdictResponse.ConfirmsPatternClass,
			WhenResolvedNotMatching = SymbolicVerdictResponse.SuppressesFinding,
			WhenIndeterminate = SymbolicVerdictResponse.SuppressesFinding
		};
		Dictionary<string, DetectionRule> rules = new(StringComparer.Ordinal)
		{
			["GO-001"] = Rule("RequestSetBasicAuth", GoDraftExpressions.Ex001(), ConfidenceLevel.High, declares, literal),
			["GO-002"] = Rule("AuthorizationHeaderBasicScheme", GoDraftExpressions.Ex002(), ConfidenceLevel.High, declares, literal),
			["GO-003"] = Rule("HeaderLiteralBasicScheme", GoDraftExpressions.Ex003(), ConfidenceLevel.High, declares, literal),
			["GO-004"] = Rule("MapLiteralBasicScheme", GoDraftExpressions.Ex004(), ConfidenceLevel.Medium, requiresPath, literal),
			["GO-005"] = Rule("URLUserinfoInRequestCall", GoDraftExpressions.Ex005(), ConfidenceLevel.High, declares, literal),
			["GO-006"] = Rule("Base64UserPasswordPair", GoDraftExpressions.Ex006(), ConfidenceLevel.Medium, requiresPath, literal),
			["GO-007"] = Rule("UserinfoUserPassword", GoDraftExpressions.Ex007(), ConfidenceLevel.Medium, requiresPath, literal),
			["GO-008"] = Rule("URLUserinfoHeldLiteral", GoDraftExpressions.Ex008(), ConfidenceLevel.Medium, requiresPath, literal),
			["GO-009"] = Rule("AuthorizationHeaderValueHeldInVariable", GoDraftExpressions.Ex009(), ConfidenceLevel.Medium, requiresPath, none, basicScheme),
			["GO-010"] = Rule("RoundTripperBasicAuthDefinition", GoDraftExpressions.Ex010(), ConfidenceLevel.Low, requiresPath, none),
			["GO-011"] = Rule("AuthenticationHelperDefinition", GoDraftExpressions.Ex011(), ConfidenceLevel.Low, requiresPath, none),
			["GO-012"] = Rule("AuthorizationHeaderValueFromEnvironment", GoDraftExpressions.Ex012(), ConfidenceLevel.Low, requiresPath, none, environmentSource),
			["GO-013"] = Rule("AuthorizationHeaderValueFromLocalHelper", GoDraftExpressions.Ex013(), ConfidenceLevel.Medium, requiresPath, none, localHelper)
		};

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	private static FileNameSignal FileName(FileNameMatch match, string text, DemotionKind kind)
	{
		return new FileNameSignal { Match = match, Text = text, Sensitivity = CaseSensitivity.Insensitive, Kind = kind };
	}

	private static FolderNameSignal Folder(string name, DemotionKind kind)
	{
		return new FolderNameSignal { FolderName = name, Match = FolderNameMatch.ExactName, Sensitivity = CaseSensitivity.Insensitive, Kind = kind };
	}

	private static LanguageShapedDemotionSignals BuildDemotionSignals()
	{
		FileNameSignal[] fileNames =
		[
			FileName(FileNameMatch.Suffix, "_test.go", DemotionKind.Test),
			FileName(FileNameMatch.Suffix, "example_test.go", DemotionKind.Sample),
			.. new[] { ".pb.go", ".pb.gw.go", "_generated.go", ".generated.go", "_gen.go", ".gen.go" }.Select(suffix => FileName(FileNameMatch.Suffix, suffix, DemotionKind.Generated)),
			FileName(FileNameMatch.Prefix, "zz_generated.", DemotionKind.Generated),
			.. new[] { "_mock.go", "_mocks.go", "_fake.go", "_stub.go" }.Select(suffix => FileName(FileNameMatch.Suffix, suffix, DemotionKind.Mock)),
			.. new[] { "mock_", "mocks_", "fake_", "stub_" }.Select(prefix => FileName(FileNameMatch.Prefix, prefix, DemotionKind.Mock)),
			.. new[] { "mock.go", "mocks.go", "fake.go", "fakes.go", "stub.go", "stubs.go" }.Select(name => FileName(FileNameMatch.ExactName, name, DemotionKind.Mock))
		];
		FolderNameSignal[] folders =
		[
			Folder("testdata", DemotionKind.Test),
			.. new[] { "test", "tests", "e2e", "fixture", "fixtures", "testutil", "testutils" }.Select(name => Folder(name, DemotionKind.Test)),
			.. new[] { "example", "examples", "_example", "_examples", "sample", "samples", "demo", "demos" }.Select(name => Folder(name, DemotionKind.Sample)),
			.. new[] { "vendor", "third_party", "third-party", "thirdparty", "Godeps" }.Select(name => Folder(name, DemotionKind.Vendored)),
			.. new[] { "mock", "mocks", "fake", "fakes", "stub", "stubs" }.Select(name => Folder(name, DemotionKind.Mock))
		];

		return new LanguageShapedDemotionSignals
		{
			FileNameSignals = fileNames.ToFrozenSet(),
			FolderNameSignals = folders.ToFrozenSet(),
			GenerationMarkerExpressions = [],
			ContentMarkers =
			[
				new ContentMarker { Expression = GoDraftExpressions.Ex110(), Zone = ContentMarkerZone.AnyZone, Kind = DemotionKind.Generated },
				new ContentMarker { Expression = GoDraftExpressions.Ex111(), Zone = ContentMarkerZone.CodeOnly, Kind = DemotionKind.Sample }
			]
		};
	}

	private static FlowSignal Signal(string name, FlowSignalKind kind, FlowSignalZone zone, Regex expression)
	{
		return new FlowSignal { Name = name, Kind = kind, Zone = zone, Expression = expression };
	}

	private static CandidateSelectionAndRecommendation BuildCandidateSelection()
	{
		FlowSignalKind interactive = FlowSignalKind.InteractiveUser;
		FlowSignalKind fixedCredential = FlowSignalKind.FixedCredential;
		FlowSignalZone codeOnly = FlowSignalZone.CodeOnly;
		FlowSignalZone codeOrString = FlowSignalZone.CodeOrStringBody;

		return new CandidateSelectionAndRecommendation
		{
			PreselectionAnchors = new[] { "SetBasicAuth", "Authorization", "UserPassword", "StdEncoding", "://", "RoundTrip" }
				.Select(text => new PreselectionAnchor { Text = text, Sensitivity = CaseSensitivity.Insensitive })
				.ToFrozenSet(),
			TemplateProvider = (_, _) => null,
			FlowSignals =
			[
				Signal("TerminalPasswordRead", interactive, codeOnly, GoDraftExpressions.Ex120()),
				Signal("StdinCredentialPromptRead", interactive, codeOrString, GoDraftExpressions.Ex121()),
				Signal("FmtScanCredentialVariable", interactive, codeOnly, GoDraftExpressions.Ex122()),
				Signal("LoginFormPasswordRead", interactive, codeOnly, GoDraftExpressions.Ex123()),
				Signal("HtmlPasswordInputMarkup", interactive, codeOrString, GoDraftExpressions.Ex124()),
				Signal("LoginRouteRegistration", interactive, codeOnly, GoDraftExpressions.Ex125()),
				Signal("EnvironmentCredentialRead", fixedCredential, codeOnly, GoDraftExpressions.Ex130()),
				Signal("ConfigReceiverCredentialRead", fixedCredential, codeOnly, GoDraftExpressions.Ex131()),
				Signal("SecretManagerClientCall", fixedCredential, codeOnly, GoDraftExpressions.Ex132()),
				Signal("CredentialNamedLiteralAssignment", fixedCredential, codeOnly, GoDraftExpressions.Ex133()),
				Signal("SecretFileRead", fixedCredential, codeOnly, GoDraftExpressions.Ex134())
			]
		};
	}

	private static FrozenSet<SubsumptionRule> BuildSubsumption()
	{
		(string Subsuming, string Subsumed)[] pairs = [("GO-002", "GO-006"), ("GO-003", "GO-006"), ("GO-004", "GO-006")];

		return pairs
			.Select(pair => new SubsumptionRule { SubsumingPatternClassIdentifier = pair.Subsuming, SubsumedPatternClassIdentifier = pair.Subsumed, When = SubsumptionCondition.MatchSpansOverlap })
			.ToFrozenSet();
	}
}
