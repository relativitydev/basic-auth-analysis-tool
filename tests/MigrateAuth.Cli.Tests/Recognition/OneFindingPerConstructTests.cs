using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// ONE FINDING PER CONSTRUCT, on a TEST-LOCAL profile: the mechanism ships and no shipped profile declares it.
/// "The same construct" is the same statement, which the profile declares through its separators and nestings, and
/// never overlapping spans. The survivor of a statement is chosen by level, then by the profile's ordered class list,
/// then by class identifier in ordinal order, and it keeps the winner's fields with the literal-credential signal
/// ORed across what it suppressed.
///
/// Every source below is invented, with placeholder names throughout.
/// </summary>
public sealed class OneFindingPerConstructTests
{
	private const string _OPTION_PAIR = PrecedenceProfileAnalyzer.OPTION_PAIR_PATTERN_CLASS;
	private const string _URL_USERINFO = PrecedenceProfileAnalyzer.URL_USERINFO_PATTERN_CLASS;
	private const string _HEADER = PrecedenceProfileAnalyzer.HEADER_PATTERN_CLASS;
	private const string _ENCODER = PrecedenceProfileAnalyzer.ENCODER_PATTERN_CLASS;
	private const string _UNLISTED = PrecedenceProfileAnalyzer.UNLISTED_PATTERN_CLASS;
	private const string _STRONG = PrecedenceProfileAnalyzer.STRONG_PATTERN_CLASS;
	private const string _QUOTED_PAIR = PrecedenceProfileAnalyzer.QUOTED_PAIR_PATTERN_CLASS;
	private const string _URL_TEXT = "https://";
	private const int _LARGE_STATEMENT_COUNT = 20_000;
	private const int _LONG_LINE_LENGTH = 200_000;
	private const int _TWO_FINDINGS = 2;
	private const int _THREE_FINDINGS = 3;

	[Fact]
	public void TwoClassesReadingOneCommandAreOneFinding()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize("sendu a:b https://c:d@h\n");

		RecognizedConstruct survivor = Assert.Single(constructs);
		Assert.Equal(_URL_USERINFO, survivor.PatternClassIdentifier);
	}

	[Fact]
	public void TheSurvivorKeepsTheWinnersAnchorAndLevel()
	{
		const string source = "sendu a:b https://c:d@h\n";

		RecognizedConstruct survivor = Assert.Single(Recognize(source));

		Assert.Equal(source.IndexOf(_URL_TEXT, StringComparison.Ordinal), survivor.AnchorOffset);
		Assert.Equal(ConfidenceLevel.Medium, survivor.Level);
	}

	[Fact]
	public void WithNoPrecedenceDeclaredBothClassesAreReported()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize("sendu a:b https://c:d@h\n", precedence: null);

		Assert.Equal([_OPTION_PAIR, _URL_USERINFO], constructs.Select(construct => construct.PatternClassIdentifier).Order(StringComparer.Ordinal));
	}

	[Theory]
	[InlineData("sendu a:b h\nsendu c:d h\n", 2)]
	[InlineData("sendu a:b https://c:d@h\nsendu e:f https://g:h@h\n", 2)]
	[InlineData("sendu a:b ; https://c:d@h\n", 2)]
	[InlineData("sendu a:b && https://c:d@h\n", 2)]
	[InlineData("sendu a:b || https://c:d@h\n", 2)]
	[InlineData("sendu a:b | https://c:d@h\n", 2)]
	[InlineData("sendu a:b\nhttps://c:d@h\n", 2)]
	[InlineData("sendu a:b \\\nhttps://c:d@h\n", 1)]
	[InlineData("sendu a:b \\; https://c:d@h\n", 1)]
	[InlineData("sendu a:b \\\\; https://c:d@h\n", 2)]
	public void ADeclaredSeparatorOrALineFeedEndsTheStatementAndAnEscapedOneDoesNot(string source, int expectedFindings)
	{
		Assert.Equal(expectedFindings, Recognize(source).Length);
	}

	[Theory]
	[InlineData("(sendu a:b ; https://c:d@h)\n", 1)]
	[InlineData("sendu a:b $(echo x ; https://c:d@h)\n", 1)]
	[InlineData("sendu a:b $(echo x) ; https://c:d@h\n", 2)]
	[InlineData("sendu a:b $(echo x | https://c:d@h)\n", 1)]
	[InlineData("x (\nsendu a:b ; https://c:d@h\n", 2)]
	[InlineData(") sendu a:b ; https://c:d@h\n", 2)]
	[InlineData("(sendu a:b ) ; https://c:d@h\n", 2)]
	public void ASeparatorInsideANestingDoesNotEndTheStatementAndANestingNeverOutlastsItsLine(string source, int expectedFindings)
	{
		Assert.Equal(expectedFindings, Recognize(source).Length);
	}

	[Fact]
	public void AHeaderBuiltInASubstitutionIsOneFinding()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize("hdr \"Authorization: Basic $(printf %s u:p | base64)\"\n");

		RecognizedConstruct survivor = Assert.Single(constructs);
		Assert.Equal(_HEADER, survivor.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, survivor.Level);
	}

	[Fact]
	public void TheSameHeaderWithNoPrecedenceIsTwoFindings()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize("hdr \"Authorization: Basic $(printf %s u:p | base64)\"\n", precedence: null);

		Assert.Equal([_HEADER, _ENCODER], constructs.Select(construct => construct.PatternClassIdentifier).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void TheHigherLevelWinsEvenWhereItsClassIsListedLast()
	{
		RecognizedConstruct survivor = Assert.Single(Recognize("sendu a:b strong x https://c:d@h\n"));

		Assert.Equal(_STRONG, survivor.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, survivor.Level);
	}

	[Fact]
	public void AtEqualLevelTheClassListedFirstWinsAndNotTheLowerIdentifier()
	{
		Assert.True(string.CompareOrdinal(_OPTION_PAIR, _URL_USERINFO) < 0);

		RecognizedConstruct survivor = Assert.Single(Recognize("sendu a:b https://c:d@h\n"));

		Assert.Equal(_URL_USERINFO, survivor.PatternClassIdentifier);
	}

	[Fact]
	public void ClassesTheListDoesNotNameAreToldApartByTheOrdinal()
	{
		ConstructPrecedence listsOnlyTheHeader = PrecedenceProfileAnalyzer.Declare([_HEADER], [";"], []);

		RecognizedConstruct survivor = Assert.Single(Recognize("sendu a:b https://c:d@h\n", listsOnlyTheHeader));

		Assert.Equal(_OPTION_PAIR, survivor.PatternClassIdentifier);
	}

	[Fact]
	public void AListedClassOutranksAnUnlistedOneAtTheSameLevel()
	{
		ConstructPrecedence listsTheUrl = PrecedenceProfileAnalyzer.Declare([_URL_USERINFO], [";"], []);

		RecognizedConstruct survivor = Assert.Single(Recognize("sendu a:b https://c:d@h\n", listsTheUrl));

		Assert.Equal(_URL_USERINFO, survivor.PatternClassIdentifier);
	}

	[Fact]
	public void TheLiteralCredentialSignalOfASuppressedFindingIsCarriedIntoTheWinner()
	{
		ImmutableArray<RecognizedConstruct> control = Recognize("sendu a:b sendq 'c:d'\n", precedence: null);
		RecognizedConstruct survivor = Assert.Single(Recognize("sendu a:b sendq 'c:d'\n"));

		Assert.False(control.Single(construct => construct.PatternClassIdentifier == _OPTION_PAIR).LiteralCredentialSignal);
		Assert.True(control.Single(construct => construct.PatternClassIdentifier == _QUOTED_PAIR).LiteralCredentialSignal);
		Assert.Equal(_OPTION_PAIR, survivor.PatternClassIdentifier);
		Assert.True(survivor.LiteralCredentialSignal);
	}

	[Fact]
	public void AWinnerThatIsLiteralStaysLiteralWhenItSuppressesANonLiteralFinding()
	{
		RecognizedConstruct survivor = Assert.Single(Recognize("sendq 'c:d' sendu a:b\n", PrecedenceProfileAnalyzer.WithQuotedPairFirst()));

		Assert.Equal(_QUOTED_PAIR, survivor.PatternClassIdentifier);
		Assert.True(survivor.LiteralCredentialSignal);
	}

	[Fact]
	public void TwoFindingsOfOneClassInOneStatementAreBothKept()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize("sendu a:b sendu c:d\n");

		Assert.Equal(_TWO_FINDINGS, constructs.Length);
		Assert.All(constructs, construct => Assert.Equal(_OPTION_PAIR, construct.PatternClassIdentifier));
	}

	[Fact]
	public void TwoFindingsOfALosingClassAreBothSuppressed()
	{
		RecognizedConstruct survivor = Assert.Single(Recognize("sendu a:b sendu c:d https://e:f@h\n"));

		Assert.Equal(_URL_USERINFO, survivor.PatternClassIdentifier);
	}

	[Fact]
	public void AClassTheListDoesNotNameTakesPartAndRanksAfterTheListedOnes()
	{
		RecognizedConstruct survivor = Assert.Single(Recognize("unlisted x sendu a:b https://c:d@h\n"));

		Assert.Equal(_URL_USERINFO, survivor.PatternClassIdentifier);
		Assert.DoesNotContain(_UNLISTED, DefaultListedClasses());
	}

	[Theory]
	[InlineData("# sendu a:b https://c:d@h\n")]
	[InlineData("echo \"sendu a:b https://c:d@h\"\n")]
	[InlineData("cat <<EOF\nsendu a:b https://c:d@h\nEOF\n")]
	public void FindingsAnchoredOutsideCodeAreNeverMerged(string source)
	{
		Assert.Equal(_TWO_FINDINGS, Recognize(source).Length);
	}

	[Fact]
	public void ACodeFindingBesideAnOutsideCodeFindingIsNotMergedWithIt()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize("sendu a:b \"x https://c:d@h\"\n");

		Assert.Equal(_TWO_FINDINGS, constructs.Length);
	}

	[Fact]
	public void ACommandAfterAHereDocumentIsItsOwnStatement()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize("cat <<EOF\nsendu a:b\nEOF\nsendu c:d https://e:f@h\n");

		Assert.Equal(_TWO_FINDINGS, constructs.Length);
		Assert.Single(constructs, construct => construct.PatternClassIdentifier == _URL_USERINFO);
	}

	[Fact]
	public void TheSurvivorsStayInAnchorOrder()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize("sendu a:b https://c:d@h\nunlisted x\nhdr \"Authorization: Basic x\"\n");

		Assert.Equal(constructs.OrderBy(construct => construct.AnchorOffset).ToArray(), constructs.ToArray());
		Assert.Equal(_THREE_FINDINGS, constructs.Length);
	}

	[Fact]
	public void TheResultDoesNotDependOnTheOrderTheRulesWereDeclaredIn()
	{
		const string source = "sendu a:b https://c:d@h ; sendu e:f sendq 'g:h' strong x\nhdr \"Authorization: Basic $(printf %s u:p | base64)\" unlisted x\n";

		string forward = Describe(Recognize(source, PrecedenceProfileAnalyzer.DefaultPrecedence, reverseRuleDeclaration: false));
		string reversed = Describe(Recognize(source, PrecedenceProfileAnalyzer.DefaultPrecedence, reverseRuleDeclaration: true));

		Assert.Equal(forward, reversed);
	}

	[Theory]
	[InlineData("tr-TR")]
	[InlineData("de-DE")]
	[InlineData("en-US")]
	[InlineData("")]
	public void TheResultDoesNotDependOnTheCulture(string cultureName)
	{
		const string source = "sendu a:b https://c:d@h\nsendu i:i https://i:i@i\nhdr \"Authorization: Basic x\"\nunlisted x\n";
		string expected = Describe(Recognize(source));
		CultureInfo original = CultureInfo.CurrentCulture;

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

			Assert.Equal(expected, Describe(Recognize(source)));
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[Fact]
	public void ManyStatementsEachKeepExactlyOneFinding()
	{
		StringBuilder source = new();

		for (int index = 0; index < _LARGE_STATEMENT_COUNT; index++)
		{
			source.Append("sendu a:b https://c:d@h\n");
		}

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source.ToString());

		Assert.Equal(_LARGE_STATEMENT_COUNT, constructs.Length);
		Assert.All(constructs, construct => Assert.Equal(_URL_USERINFO, construct.PatternClassIdentifier));
	}

	[Fact]
	public void ADeclarationOverLongLinesWithoutSeparatorsStillKeepsOneFinding()
	{
		string source = $"sendu a:b {new string('x', _LONG_LINE_LENGTH)} https://c:d@h\n";

		Assert.Single(Recognize(source));
	}

	[Fact]
	public void TheFixtureDeclarationValidatesWithNoViolationAtAll()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(new PrecedenceProfileAnalyzer(PrecedenceProfileAnalyzer.DefaultPrecedence, reverseRuleDeclaration: false)));
	}

	[Theory]
	[InlineData("OFC-001,OFC-999", AnalyzerContractViolationCode.ConstructPrecedenceListsAnUnknownOrRepeatedPatternClass)]
	[InlineData("OFC-001,OFC-001", AnalyzerContractViolationCode.ConstructPrecedenceListsAnUnknownOrRepeatedPatternClass)]
	public void ADeclarationOverAnUnknownOrRepeatedClassIsRefused(string listed, AnalyzerContractViolationCode expected)
	{
		string[] classes = listed.Length == 0 ? [] : listed.Split(',');
		PrecedenceProfileAnalyzer analyzer = new(PrecedenceProfileAnalyzer.Declare(classes, [";"], []), reverseRuleDeclaration: false);

		Assert.Contains(AnalyzerContractValidator.Validate(analyzer), violation => violation.Code == expected);
	}

	[Theory]
	[InlineData("", "(", ")")]
	[InlineData("a\nb", "(", ")")]
	[InlineData(";", "", ")")]
	[InlineData(";", "(", "")]
	[InlineData(";", "(\n", ")")]
	[InlineData(";", "(", ")\n")]
	[InlineData(";", "|", "|")]
	public void AnEmptyOrLineFeedTokenOrAnUncountableNestingIsRefused(string separator, string opening, string closing)
	{
		PrecedenceProfileAnalyzer analyzer = new(
			PrecedenceProfileAnalyzer.Declare([_OPTION_PAIR, _URL_USERINFO], [separator], [(opening, closing)]),
			reverseRuleDeclaration: false);

		Assert.Contains(
			AnalyzerContractValidator.Validate(analyzer),
			violation => violation.Code == AnalyzerContractViolationCode.ConstructPrecedenceTokenMalformed);
	}

	private static IEnumerable<string> DefaultListedClasses()
	{
		return PrecedenceProfileAnalyzer.DefaultPrecedence.PatternClassesInPrecedenceOrder;
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(
			"|",
			constructs.Select(construct => $"{construct.PatternClassIdentifier}@{construct.AnchorOffset}:{construct.Level}:{construct.LiteralCredentialSignal}"));
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(string source)
	{
		return Recognize(source, PrecedenceProfileAnalyzer.DefaultPrecedence);
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(
		string source,
		ConstructPrecedence? precedence,
		bool reverseRuleDeclaration = false)
	{
		ILanguageAnalyzer analyzer = new PrecedenceProfileAnalyzer(precedence, reverseRuleDeclaration);
		RecognitionHarness harness = new();
		string normalizedText = TextNormalization.Normalize(source);
		RepositoryRelativePath path = harness.PathOf("source/script.hdx");
		ZoneMask mask = new FileLexicalScanner(harness.EmissionPoint).Scan(analyzer.LexicalProfile, normalizedText, path);
		FileRecognitionContext context = new()
		{
			Analyzer = analyzer,
			NormalizedText = normalizedText,
			Mask = mask,
			Path = path
		};

		return new RecognitionEngine(harness.EmissionPoint, new FrozenTimeProvider()).Recognize(context).Constructs;
	}
}

internal static partial class PrecedenceProfileExpressions
{
	[GeneratedRegex(
		@"\b(?<construct>sendu)[ \t]+(?<credential>[A-Za-z0-9_.@-]+:[A-Za-z0-9_.@-]+)(?=[ \t]|$)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex OptionPair();

	[GeneratedRegex(
		@"(?<construct>https?://)(?<credential>[^\s/@:'""]+:[^\s/@'""]+)@",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UrlUserinfo();

	[GeneratedRegex(
		@"\b(?<construct>hdr)[ \t]+""Authorization: Basic ",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Header();

	[GeneratedRegex(
		@"\b(?<construct>printf)[ \t]+%s[ \t]+(?<credential>[A-Za-z0-9_.@-]+:[A-Za-z0-9_.@-]+)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Encoder();

	[GeneratedRegex(
		@"\b(?<construct>unlisted)[ \t]+x\b",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Unlisted();

	[GeneratedRegex(
		@"\b(?<construct>strong)[ \t]+x\b",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Strong();

	[GeneratedRegex(
		@"\b(?<construct>sendq)[ \t]+'(?<credential>[^'\n]+:[^'\n]+)'",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex QuotedPair();
}

/// <summary>
/// The exemplar's lexical profile with seven rules of its own and a declared precedence. The order of the list is
/// deliberately not the order of the identifiers, so that a rank and an ordinal never agree by accident.
/// </summary>
internal sealed class PrecedenceProfileAnalyzer : ILanguageAnalyzer
{
	public const string OPTION_PAIR_PATTERN_CLASS = "OFC-001";
	public const string URL_USERINFO_PATTERN_CLASS = "OFC-002";
	public const string HEADER_PATTERN_CLASS = "OFC-003";
	public const string ENCODER_PATTERN_CLASS = "OFC-004";
	public const string UNLISTED_PATTERN_CLASS = "OFC-005";
	public const string STRONG_PATTERN_CLASS = "OFC-006";
	public const string QUOTED_PAIR_PATTERN_CLASS = "OFC-007";

	private const char _CODE_ESCAPE_CHARACTER = '\\';
	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";
	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";

	private static readonly string[] _statementSeparators = [";", "&&", "||", "|"];
	private static readonly (string Opening, string Closing)[] _statementNestings = [("$(", ")"), ("(", ")")];

	private readonly HereDocumentExemplarAnalyzer _inner = new();

	public PrecedenceProfileAnalyzer(ConstructPrecedence? constructPrecedence, bool reverseRuleDeclaration)
	{
		ConstructPrecedence = constructPrecedence;

		List<KeyValuePair<string, DetectionRule>> rules =
		[
			new(OPTION_PAIR_PATTERN_CLASS, Rule(PrecedenceProfileExpressions.OptionPair(), ConfidenceLevel.Medium, CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP))),
			new(URL_USERINFO_PATTERN_CLASS, Rule(PrecedenceProfileExpressions.UrlUserinfo(), ConfidenceLevel.Medium, CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP))),
			new(HEADER_PATTERN_CLASS, Rule(PrecedenceProfileExpressions.Header(), ConfidenceLevel.High, CredentialPosition.None())),
			new(ENCODER_PATTERN_CLASS, Rule(PrecedenceProfileExpressions.Encoder(), ConfidenceLevel.Medium, CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP))),
			new(UNLISTED_PATTERN_CLASS, Rule(PrecedenceProfileExpressions.Unlisted(), ConfidenceLevel.Medium, CredentialPosition.None())),
			new(STRONG_PATTERN_CLASS, Rule(PrecedenceProfileExpressions.Strong(), ConfidenceLevel.High, CredentialPosition.None())),
			new(QUOTED_PAIR_PATTERN_CLASS, Rule(PrecedenceProfileExpressions.QuotedPair(), ConfidenceLevel.Medium, CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP)))
		];

		if (reverseRuleDeclaration)
		{
			rules.Reverse();
		}

		DetectionRulesByPatternClass = rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	/// <summary>Declares the list with the strongest class last, an order no ordinal sort would give.</summary>
	public static ConstructPrecedence DefaultPrecedence { get; } = Declare(
		[HEADER_PATTERN_CLASS, URL_USERINFO_PATTERN_CLASS, OPTION_PAIR_PATTERN_CLASS, ENCODER_PATTERN_CLASS, QUOTED_PAIR_PATTERN_CLASS, STRONG_PATTERN_CLASS],
		_statementSeparators,
		_statementNestings);

	public ConstructPrecedence? ConstructPrecedence { get; }

	public LexicalProfile LexicalProfile { get; } = HereDocumentExemplarAnalyzer.BuildProfileWith(
	[
		HereDocumentExemplarAnalyzer.BuildHereDocumentRule(
			"<<-",
			HereDocumentExemplarPatternExpressions.TabStrippingOpening(),
			PositionalAnchoring.WholeLineAfterLeadingTabs,
			HereDocumentBodyStart.AfterTheOpeningLine),
		HereDocumentExemplarAnalyzer.BuildHereDocumentRule(
			"<<",
			HereDocumentExemplarPatternExpressions.PlainOpening(),
			PositionalAnchoring.WholeLine,
			HereDocumentBodyStart.AfterTheOpeningLine)
	],
	_CODE_ESCAPE_CHARACTER);

	public LanguageIdentification LanguageIdentification => _inner.LanguageIdentification;

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; }

	public LanguageShapedDemotionSignals DemotionSignals => _inner.DemotionSignals;

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation => _inner.CandidateSelectionAndRecommendation;

	public FrozenSet<SubsumptionRule> SubsumptionRelation => _inner.SubsumptionRelation;

	public static ConstructPrecedence Declare(
		IEnumerable<string> classesInPrecedenceOrder,
		IEnumerable<string> separators,
		IEnumerable<(string Opening, string Closing)> nestings)
	{
		return new ConstructPrecedence
		{
			PatternClassesInPrecedenceOrder = [.. classesInPrecedenceOrder],
			StatementSeparators = separators.ToFrozenSet(StringComparer.Ordinal),
			StatementNestings = nestings
				.Select(nesting => new StatementNesting { Opening = nesting.Opening, Closing = nesting.Closing })
				.ToFrozenSet()
		};
	}

	/// <summary>The same list with the quoted-pair class first, so a literal winner can be told from a literal suppressed one.</summary>
	public static ConstructPrecedence WithQuotedPairFirst()
	{
		return Declare(
			[QUOTED_PAIR_PATTERN_CLASS, OPTION_PAIR_PATTERN_CLASS],
			_statementSeparators,
			_statementNestings);
	}

	private static DetectionRule Rule(Regex expression, ConfidenceLevel baseLevel, CredentialPosition credentialPosition)
	{
		DetectionRule rule = new()
		{
			Expression = expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = baseLevel,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = credentialPosition,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = $"One finding per construct fixture, {expression}"
		};

		return rule;
	}
}
