using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Lexical;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// Every expression of the TEST-LOCAL probe analyzer for the dialect, same-file variable use and same-file wrapper
/// mechanisms. The tokens are invented and the shapes are those of a POSIX-family shell, built here from the contract
/// types alone: nothing below is read from, or shipped in, any catalog under <c>src/</c>.
/// </summary>
internal static partial class ShellProbeExpressions
{
	private const int _BOUND = RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS;

	/// <summary>A client call carrying its credential after the user option.</summary>
	[GeneratedRegex(
		@"\b(?<construct>curl)\b[ \t]+(?:-[A-Za-z]+[ \t]+)*(?:-u|--user)[ \t]+(?<credential>""[^""\n]*""|'[^'\n]*'|[^\s""']+)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex ClientCall();

	/// <summary>A construct that builds a credential into a named variable, or into none.</summary>
	[GeneratedRegex(
		@"\b(?<construct>mkauth)\b(?:[ \t]+into[ \t]+(?<var>[A-Za-z_][A-Za-z0-9_]*))?",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex BuiltCredential();

	/// <summary>A construct whose only reference to its own variable is inside its own span.</summary>
	[GeneratedRegex(
		@"\b(?<construct>mkauthself)[ \t]+(?<var>[A-Za-z_][A-Za-z0-9_]*)[ \t]+""\$\{?\k<var>\}?""",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex SelfReferencingCredential();

	[GeneratedRegex(
		@"\bread[ \t]+-[A-Za-z]*s[A-Za-z]*\b",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex SilentRead();

	[GeneratedRegex(
		@"\bread[ \t]+-p[ \t]+""[^""\n]*""",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex PromptRead();

	/// <summary>A prompt that names its variable, or none.</summary>
	[GeneratedRegex(
		@"\bread[ \t]+-p[ \t]+""[^""\n]*""(?:[ \t]+(?<var>[A-Za-z_][A-Za-z0-9_]*))?",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex NamedPromptRead();

	/// <summary>The statement that sends the variable: a privilege-escalation call reading its password on standard input.</summary>
	[GeneratedRegex(
		@"\bsudo\b[^\n]*[ \t]-S\b",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex PasswordOnStandardInput();

	[GeneratedRegex(
		@"\bUSE_SERVICE_TOKEN\b",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex ServiceTokenMarker();

	[GeneratedRegex(
		@"\bselect_prompt\b",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex SelectPrompt();

	[GeneratedRegex(
		@"^[ \t]*(?:function[ \t]+)?(?<name>[A-Za-z_][A-Za-z0-9_]*)[ \t]*\(\)[ \t]*\n?[ \t]*(?<open>\{)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex FunctionDefinition();

	[GeneratedRegex(
		@"\$(?:\{(?<index>[1-9@*])\}|(?<index>[1-9@*]))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex PositionalReference();

	[GeneratedRegex(
		@"(?:(?:local|declare|typeset)[ \t]+(?:-[A-Za-z]+[ \t]+)*)?(?<alias>[A-Za-z_][A-Za-z0-9_]*)=\$(?:\{(?<index>[1-9@*])\}|(?<index>[1-9@*]))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex ParameterAlias();

	[GeneratedRegex(
		@"(?:^|(?<=[;&|(]))[ \t]*(?<name>[A-Za-z_][A-Za-z0-9_]*)(?<args>(?:[ \t]+[^\n;&|)]*)?)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex CallSite();

	[GeneratedRegex(
		@"""(?:[^""\\]|\\.)*""|'[^']*'|[^\s""']+",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex Argument();

	/// <summary>Matches any word holding a dollar sign, and a user-and-secret word: proof that an expansion's text is not tested.</summary>
	[GeneratedRegex(
		@"\$|^""?\w+:\w+""?$",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex AnythingWithADollarOrAUserAndSecret();

	/// <summary>A word of the user-and-secret form.</summary>
	[GeneratedRegex(
		@"^""?\w+:\w+""?$",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: _BOUND)]
	internal static partial Regex UserAndSecretWord();
}

/// <summary>
/// The test-local probe analyzer: the Shell-shaped lexical profile of the scanner tests, a few detection rules, and
/// whatever flow signals, dialects and wrapper tracking a test declares. It wraps the reference analyzer only for the
/// elements the engine never reads in these tests (language identification and demotion signals).
/// </summary>
internal sealed class ShellProbeAnalyzer : ILanguageAnalyzer
{
	internal const string EXEMPLAR_LANGUAGE_NAME = "ExemplarOne";
	private const string _CLIENT_CALL_CLASS_DISPLAY_NAME = "Client call with a user option";
	private const string _BUILT_CREDENTIAL_CLASS_DISPLAY_NAME = "Credential built into a variable";
	private const string _SELF_REFERENCING_CLASS_DISPLAY_NAME = "Credential built into a self-referencing variable";

	internal const string CLIENT_CALL_CLASS = "SHP-001";
	internal const string BUILT_CREDENTIAL_CLASS = "SHP-002";
	internal const string SELF_REFERENCING_CLASS = "SHP-003";
	internal const string CONSTRUCT_GROUP = "construct";
	internal const string CREDENTIAL_GROUP = "credential";

	private static readonly LexicalProfile _lexicalProfile = ShellShapedProfile.Build();

	private readonly ILanguageAnalyzer _inner = ExemplarFixtureCatalogAnalyzer();

	internal ShellProbeAnalyzer(
		CandidateSelectionAndRecommendation element,
		RuleVariableUse? builtCredentialVariableUse = null)
	{
		CandidateSelectionAndRecommendation = element;
		DetectionRulesByPatternClass = BuildRules(builtCredentialVariableUse);
	}

	public LexicalProfile LexicalProfile => _lexicalProfile;

	public LanguageIdentification LanguageIdentification => _inner.LanguageIdentification;

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; }

	public LanguageShapedDemotionSignals DemotionSignals => _inner.DemotionSignals;

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; }

	public FrozenSet<SubsumptionRule> SubsumptionRelation => FrozenSet<SubsumptionRule>.Empty;

	private static ILanguageAnalyzer ExemplarFixtureCatalogAnalyzer()
	{
		return Fixtures.ExemplarFixtureCatalog.Build().AnalyzersByLanguageName[EXEMPLAR_LANGUAGE_NAME];
	}

	private static FrozenDictionary<string, DetectionRule> BuildRules(RuleVariableUse? builtCredentialVariableUse)
	{
		return new Dictionary<string, DetectionRule>
		{
			[CLIENT_CALL_CLASS] = BuildRule(ShellProbeExpressions.ClientCall(), _CLIENT_CALL_CLASS_DISPLAY_NAME, null, CredentialPosition.AtCaptureGroup(CREDENTIAL_GROUP)),
			[BUILT_CREDENTIAL_CLASS] = BuildRule(ShellProbeExpressions.BuiltCredential(), _BUILT_CREDENTIAL_CLASS_DISPLAY_NAME, builtCredentialVariableUse, CredentialPosition.None()),
			[SELF_REFERENCING_CLASS] = BuildRule(
				ShellProbeExpressions.SelfReferencingCredential(),
				_SELF_REFERENCING_CLASS_DISPLAY_NAME,
				new RuleVariableUse(),
				CredentialPosition.None())
		}.ToFrozenDictionary(StringComparer.Ordinal);
	}

	private static DetectionRule BuildRule(
		Regex expression,
		string displayName,
		RuleVariableUse? variableUse,
		CredentialPosition credentialPosition)
	{
		return new DetectionRule
		{
			Expression = expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(CONSTRUCT_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = displayName,
			CredentialPosition = credentialPosition,
			VariableUse = variableUse
		};
	}
}

/// <summary>The declarations the probe tests share, and the one way they run a file through the real scanner and engine.</summary>
internal static class ShellProbe
{
	internal const string BASH = "bash";
	internal const string DASH = "dash";
	internal const string ZSH = "zsh";
	internal const string KSH93 = "ksh93";
	internal const string MKSH = "mksh";
	internal const string DEFAULT_PATH = "scripts/run.sh";
	internal const string CLIENT_CALL_LINE = "curl -u svc:pw https://example.invalid/";

	private const string _NEWLINE = "\n";
	private const string _SILENT_READ_NAME = "probe-silent-read";
	private const string _PROMPT_READ_NAME = "probe-prompt-read";
	private const string _PROMPT_FEEDING_NAME = "probe-prompt-feeding-a-password-read";
	private const string _SERVICE_TOKEN_NAME = "probe-service-token";
	private const string _SELECT_PROMPT_NAME = "probe-select-prompt";
	private const string _DEFAULT_PROMPT_VARIABLE = "REPLY";
	private const string _SH_INTERPRETER = "sh";
	private const string _KSH_INTERPRETER = "ksh";
	private const string _ZSH_EXTENSION = ".zsh";
	private const string _KSH_EXTENSION = ".ksh";
	private const string _SHELL_PROBE_EXTENSION = ".ex1";

	internal static FileDialects Dialects { get; } = new()
	{
		DefaultDialect = BASH,
		ShebangInterpreters = new Dictionary<string, string>
		{
			[BASH] = BASH,
			[_SH_INTERPRETER] = BASH,
			[DASH] = DASH,
			[ZSH] = ZSH,
			[KSH93] = KSH93,
			[_KSH_INTERPRETER] = KSH93,
			[MKSH] = MKSH
		}.ToFrozenDictionary(StringComparer.Ordinal),
		ExtensionDialects = new Dictionary<string, string>
		{
			[_ZSH_EXTENSION] = ZSH,
			[_KSH_EXTENSION] = KSH93
		}.ToFrozenDictionary(StringComparer.Ordinal)
	};

	/// <summary>An interactive signal evidenced by a silent read, in the one dialect where the option exists.</summary>
	internal static FlowSignal SilentReadInBash { get; } = new()
	{
		Name = _SILENT_READ_NAME,
		Kind = FlowSignalKind.InteractiveUser,
		Zone = FlowSignalZone.CodeOnly,
		Expression = ShellProbeExpressions.SilentRead(),
		Dialects = [BASH]
	};

	/// <summary>An interactive signal evidenced by a read with a prompt, in the two dialects where the option exists.</summary>
	internal static FlowSignal PromptReadInBashAndDash { get; } = new()
	{
		Name = _PROMPT_READ_NAME,
		Kind = FlowSignalKind.InteractiveUser,
		Zone = FlowSignalZone.CodeOnly,
		Expression = ShellProbeExpressions.PromptRead(),
		Dialects = [BASH, DASH]
	};

	/// <summary>The same prompt, suppressed where the variable it fills is later fed to a password read.</summary>
	internal static FlowSignal PromptFeedingAPasswordRead { get; } = PromptFeeding(new FlowSignalVariableUse
	{
		UsePattern = ShellProbeExpressions.PasswordOnStandardInput(),
		DefaultVariableName = _DEFAULT_PROMPT_VARIABLE,
		StatementSeparators = [";", "&&", "||", "&"]
	});

	internal static FlowSignal ServiceToken { get; } = new()
	{
		Name = _SERVICE_TOKEN_NAME,
		Kind = FlowSignalKind.FixedCredential,
		Zone = FlowSignalZone.CodeOnly,
		Expression = ShellProbeExpressions.ServiceTokenMarker()
	};

	/// <summary>Interactive everywhere but zsh.</summary>
	internal static FlowSignal SelectPromptExceptInZsh { get; } = new()
	{
		Name = _SELECT_PROMPT_NAME,
		Kind = FlowSignalKind.InteractiveUser,
		Zone = FlowSignalZone.CodeOnly,
		Expression = ShellProbeExpressions.SelectPrompt(),
		ExcludedDialects = [ZSH]
	};

	internal static FlowSignal PromptFeeding(FlowSignalVariableUse use)
	{
		return new FlowSignal
		{
			Name = _PROMPT_FEEDING_NAME,
			Kind = FlowSignalKind.InteractiveUser,
			Zone = FlowSignalZone.CodeOnly,
			Expression = ShellProbeExpressions.NamedPromptRead(),
			VariableUse = use
		};
	}

	internal static WrapperTracking Wrappers(int maxBodyLines = WrapperTracking.DEFAULT_MAX_BODY_LINES, int maxCalls = WrapperTracking.DEFAULT_MAX_CALLS)
	{
		return new WrapperTracking
		{
			FunctionDefinitionExpression = ShellProbeExpressions.FunctionDefinition(),
			PositionalReferenceExpression = ShellProbeExpressions.PositionalReference(),
			ParameterAliasExpression = ShellProbeExpressions.ParameterAlias(),
			CallSiteExpression = ShellProbeExpressions.CallSite(),
			ArgumentExpression = ShellProbeExpressions.Argument(),
			CredentialBearingArgument = ShellProbeExpressions.UserAndSecretWord(),
			MaxBodyLines = maxBodyLines,
			MaxCalls = maxCalls
		};
	}

	internal static CandidateSelectionAndRecommendation Element(
		ImmutableArray<FlowSignal> signals,
		FileDialects? dialects = null,
		WrapperTracking? wrappers = null)
	{
		ILanguageAnalyzer inner = Fixtures.ExemplarFixtureCatalog.Build().AnalyzersByLanguageName[ShellProbeAnalyzer.EXEMPLAR_LANGUAGE_NAME];

		return new CandidateSelectionAndRecommendation
		{
			PreselectionAnchors = inner.CandidateSelectionAndRecommendation.PreselectionAnchors,
			TemplateProvider = inner.CandidateSelectionAndRecommendation.TemplateProvider,
			FlowSignals = signals,
			FileDialects = dialects,
			WrapperTracking = wrappers
		};
	}

	internal static string Source(params string[] lines)
	{
		return string.Join(_NEWLINE, lines);
	}

	/// <summary>The path a shell script of the probe is named by, with an extension the probe dialects do not map.</summary>
	internal static string PathWithUnmappedExtension => $"scripts/run{_SHELL_PROBE_EXTENSION}";

	internal static (RecognitionResult Result, RecognitionEngine Engine, RecognitionHarness Harness) Recognize(
		ShellProbeAnalyzer analyzer,
		string source,
		string relativePath = DEFAULT_PATH,
		TimeProvider? timeProvider = null)
	{
		RecognitionHarness harness = new();
		RecognitionEngine engine = new(harness.EmissionPoint, timeProvider ?? new FrozenTimeProvider());

		return (engine.Recognize(ContextFor(harness, analyzer, source, relativePath)), engine, harness);
	}

	internal static FileRecognitionContext ContextFor(RecognitionHarness harness, ILanguageAnalyzer analyzer, string source, string relativePath)
	{
		string normalizedText = RecognitionHarness.Normalize(source);
		RepositoryRelativePath path = harness.PathOf(relativePath);
		ZoneMask mask = new FileLexicalScanner(harness.EmissionPoint).Scan(analyzer.LexicalProfile, normalizedText, path);

		return new FileRecognitionContext
		{
			Analyzer = analyzer,
			NormalizedText = normalizedText,
			Mask = mask,
			Path = path
		};
	}

	internal static ShellProbeAnalyzer AnalyzerWith(
		ImmutableArray<FlowSignal> signals,
		FileDialects? dialects = null,
		WrapperTracking? wrappers = null,
		RuleVariableUse? builtCredentialVariableUse = null)
	{
		return new ShellProbeAnalyzer(Element(signals, dialects, wrappers), builtCredentialVariableUse);
	}
}
