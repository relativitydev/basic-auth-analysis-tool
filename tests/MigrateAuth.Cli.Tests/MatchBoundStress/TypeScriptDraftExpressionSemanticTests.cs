using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SEMANTIC CHECKS OF THE DRAFT `TypeScript` EXPRESSIONS. A reshape for cost must keep what an expression means, so every new or amended expression has positive and negative samples here, each a shape the catalog part
/// names: the typed constant declaration; the three typed forms of an object literal that `TS-001` reads (annotation or typed default, angle-bracket assertion, the `as` and `satisfies` forms) and the ones it refuses; the injected
/// service call that `TS-003` reads; the four callee expressions with the generic-argument group, at depth three, at depth four and at the length bound and one past it; the object-key carrier with its tail refusal, in the three forms it
/// separates and the three it cannot; and the four flow signals with their near-misses. Three further checks tie the amended expressions to the inherited ones: on every sample of the `JavaScript` draft, an amended expression
/// reports what the inherited one reports, so that the shared amendments move no `JavaScript` reading on text without a generic list or a type-position literal. The generated and the interpreted form of every expression must agree.
/// </summary>
public sealed class TypeScriptDraftExpressionSemanticTests
{
	private const string _LINE_FEED = "\n";
	private const string _SIGNAL_GROUP = "signal";
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _TYPED_CONSTANT_ID = "EX-TS-101";
	private const int _ABBREVIATION_MAXIMUM_LENGTH = 160;
	private const int _GENERIC_BODY_AT_THE_BOUND = 120;
	private const int _GENERIC_BODY_PAST_THE_BOUND = 121;
	private const int _FLAT_PREFIX_AT_THE_BOUND = 300;
	private const int _FLAT_PREFIX_PAST_THE_BOUND = 301;
	private const int _REFUSAL_BODY_AT_THE_BOUND = 300;
	private const int _REFUSAL_BODY_PAST_THE_BOUND = 301;
	private const int _PREDICATE_SHAPE_SCAN_SIZE_IN_CHARACTERS = 64 * 1024;
	private const char _FILLER_UNIT = 'x';
	private const int _DECLARATIONS_OF_A_CONFLICT = 2;
	private const string _AUTH_OBJECT = "{ auth: { username: 'u', password: 'p' } }";

	/// <summary>The public methods of the wrapped-client service, at version 12.0.1 of its package. The list has no `options` method.</summary>
	private static readonly ImmutableArray<string> _CITED_HTTP_SERVICE_METHODS = ["request", "get", "delete", "head", "post", "put", "patch", "postForm", "putForm", "patchForm", "query"];

	private static readonly ImmutableArray<string> _ALLOWED_SHAPE_EXTENSIONS = [TypeScriptMatchBoundStressSet.FILE_EXTENSION, TypeScriptMatchBoundStressSet.MARKUP_FILE_EXTENSION, TypeScriptMatchBoundStressSet.DECLARATION_FILE_EXTENSION];

	/// <summary>Each amended or replaced expression, with the inherited expression it stands in place of.</summary>
	private static readonly ImmutableDictionary<string, string> _INHERITED_OF_THE_AMENDED = ImmutableDictionary<string, string>.Empty
		.Add("EX-JS-001", "EX-TS-G-001")
		.Add("EX-JS-003", "EX-TS-G-003")
		.Add("EX-JS-015", "EX-TS-G-015")
		.Add("EX-JS-016", "EX-TS-G-016")
		.Add("EX-JS-004", "EX-TS-T-004")
		.Add("EX-JS-101", "EX-TS-101");

	private static readonly ImmutableArray<JavaScriptDraftExpressionSemanticTests.Case> _CASES =
	[
		.. TypedConstantCases(),
		.. TypedObjectCases(),
		.. HttpServiceCases(),
		.. GenericArgumentGroupCases(),
		.. TailRefusalCases(),
		.. FlowSignalCases()
	];

	private static readonly ImmutableDictionary<string, Regex> _EXPRESSIONS = BuildExpressionIndex();

	[Fact]
	public void EveryNewOrAmendedDraftExpressionHasPositiveAndNegativeSamples()
	{
		Assert.All(TypeScriptDraftExpressions.NewAndAmendedExpressions.Select(entry => entry.Identifier), identifier =>
		{
			Assert.Contains(_CASES, candidate => candidate.Id == identifier && candidate.Matches);
			Assert.Contains(_CASES, candidate => candidate.Id == identifier && !candidate.Matches);
		});
	}

	[Fact]
	public void EveryNewOrAmendedDraftExpressionMatchesExactlyTheSamplesItShould()
	{
		List<string> wrong = [];

		foreach (JavaScriptDraftExpressionSemanticTests.Case sample in _CASES)
		{
			Match match = _EXPRESSIONS[sample.Id].Match(sample.Text, sample.Start);
			bool captured = sample.Group.Length == 0 || (match.Success && match.Groups[sample.Group].Value == sample.Value);

			if (match.Success != sample.Matches || !captured)
			{
				wrong.Add($"{sample.Id} on [{Escape(Abbreviate(sample.Text))}] expected {sample.Matches} {sample.Group}={sample.Value}, got {match.Success} {(sample.Group.Length == 0 ? string.Empty : match.Groups[sample.Group].Value)}");
			}
		}

		Assert.Empty(wrong);
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfEveryNewOrAmendedExpressionAgreeOnEverySample()
	{
		List<string> disagreements = [];

		foreach (JavaScriptDraftExpressionSemanticTests.Case sample in _CASES)
		{
			Regex generated = _EXPRESSIONS[sample.Id];
			Regex interpreted = new(generated.ToString(), generated.Options, generated.MatchTimeout);

			if (generated.Match(sample.Text, sample.Start).Success != interpreted.Match(sample.Text, sample.Start).Success)
			{
				disagreements.Add($"{sample.Id} on {Escape(Abbreviate(sample.Text))}");
			}
		}

		Assert.Empty(disagreements);
	}

	[Fact]
	public void EveryClassExpressionDeclaresTheConstructGroupAndEveryFlowSignalTheSignalGroup()
	{
		List<string> missing = [];

		foreach ((string identifier, Regex expression) in TypeScriptDraftExpressions.NewAndAmendedExpressions)
		{
			string anchorGroup = TypeScriptDraftExpressions.IsAFlowSignal(identifier) ? _SIGNAL_GROUP : _CONSTRUCT_GROUP;

			if (identifier != _TYPED_CONSTANT_ID && !expression.GetGroupNames().Contains(anchorGroup))
			{
				missing.Add($"{identifier} lacks {anchorGroup}");
			}
		}

		Assert.Empty(missing);
	}

	/// <summary>The shared amendments move no inherited reading: on every sample of the `JavaScript` draft that an amended expression stands in place of, it reports the same match and the same capture as the SHIPPED inherited expression.</summary>
	[Fact]
	public void EveryAmendedExpressionReportsOnEverySampleOfTheJavaScriptDraftWhatTheInheritedExpressionReports()
	{
		List<string> moved = [];
		int compared = 0;

		foreach (JavaScriptDraftExpressionSemanticTests.Case sample in JavaScriptDraftExpressionSemanticTests.Cases.Where(candidate => _INHERITED_OF_THE_AMENDED.ContainsKey(candidate.Id)))
		{
			Match inherited = ShippedExpressionsIndex()[sample.Id].Match(sample.Text, sample.Start);
			Match amended = _EXPRESSIONS[_INHERITED_OF_THE_AMENDED[sample.Id]].Match(sample.Text, sample.Start);
			string inheritedGroup = sample.Group.Length == 0 ? string.Empty : inherited.Groups[sample.Group].Value;
			string amendedGroup = sample.Group.Length == 0 ? string.Empty : amended.Groups[sample.Group].Value;

			compared++;

			if (inherited.Success != amended.Success || inheritedGroup != amendedGroup || inherited.Index != amended.Index || inherited.Length != amended.Length)
			{
				moved.Add($"{sample.Id} on [{Escape(Abbreviate(sample.Text))}]: inherited {inherited.Success} {inherited.Index},{inherited.Length} {inheritedGroup}; amended {amended.Success} {amended.Index},{amended.Length} {amendedGroup}");
			}
		}

		Assert.True(compared > 0);
		Assert.Empty(moved);
	}

	/// <summary>The draft typed constant declaration reads what the test-local shaped profile's typed constant declaration reads on typed declarations whose literal ends the statement (the same name); it differs in also reading a literal that a concatenation continues (as the inherited expression does) and in being free of a lazy bounded loop in an optional group.</summary>
	[Theory]
	[InlineData("const header: string = 'Basic ';", "header")]
	[InlineData("export const scheme: 'Basic' | 'Bearer' = 'Basic';", "scheme")]
	[InlineData("private readonly mode: string = 'basic';", "mode")]
	[InlineData("static token = `Bearer`;", "token")]
	[InlineData("declare const x: Map<string, Array<number>> = 'v';", "x")]
	public void TheDraftTypedConstantDeclarationAgreesWithTheShapedProfileOnTheNameItReads(string text, string name)
	{
		Match draft = _EXPRESSIONS[_TYPED_CONSTANT_ID].Match(text);
		Match profile = TypeScriptShapedProfile.ConstantDeclaration().Match(text);

		Assert.True(draft.Success);
		Assert.True(profile.Success);
		Assert.Equal(name, draft.Groups["name"].Value);
		Assert.Equal(name, profile.Groups["name"].Value);
	}

	/// <summary>The exposure scan finds a lazy bounded loop inside an optional group in the typed constant declaration of the test-local shaped profile (the annotation), and finds none in the draft, whose annotation is greedy and cannot cross the equals sign.</summary>
	[Fact]
	public void TheDraftTypedConstantDeclarationAvoidsTheLazyLoopInAnOptionalGroupThatTheShapedProfilesCarries()
	{
		PatternShape profile = PatternShape.Analyze(TypeScriptShapedProfile.ConstantDeclaration().ToString());
		PatternShape draft = PatternShape.Analyze(_EXPRESSIONS[_TYPED_CONSTANT_ID].ToString());

		Assert.True(profile.LazyLoopsInOptionalGroups > 0);
		Assert.Equal(0, draft.LazyLoopsInOptionalGroups);
	}

	[Fact]
	public void EveryStressShapeIsLegalForTheReaderAndFillsTheFileToJustUnderTheCeiling()
	{
		List<string> refused = [];

		foreach (MatchBoundStressShape shape in TypeScriptMatchBoundStressSet.Build())
		{
			bool tooLarge = Encoding.UTF8.GetByteCount(shape.Text) > ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES;
			bool lineTooLong = shape.Text.Split(_LINE_FEED).Any(line => line.Length > ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);

			if (tooLarge || lineTooLong || shape.Text.Contains('\r') || !_ALLOWED_SHAPE_EXTENSIONS.Contains(shape.FileExtension))
			{
				refused.Add($"{shape.Name}: tooLarge={tooLarge} lineTooLong={lineTooLong} extension={shape.FileExtension}");
			}
		}

		Assert.Empty(refused);
	}

	[Fact]
	public void EveryStressShapeNameIsUniqueAndTheShapesCoverEveryNamedFamily()
	{
		ImmutableArray<MatchBoundStressShape> shapes = TypeScriptMatchBoundStressSet.Build();
		string[] families =
		[
			"annotations-at-the-ceiling", "unions-at-the-ceiling", "generic-heavy-declarations", "decorator-chains", "tsx-nested-templates", "ten-thousand-lines-of-non-null-assertion-then-slash",
			"dts-anchor-dense-deep-nesting", "n3-generic-body-at-the-length-bound", "n3-generic-body-past-the-length-bound", "generic-calls-at-depth-three", "generic-calls-past-depth-three", "n4-type-position-literals",
			"typed-identifier-authorization-carriers", "typed-environment-authorization-carriers", "typed-environment-declarations", "authorization-carriers-in-full-guard-windows"
		];

		Assert.Equal(shapes.Length, shapes.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
		Assert.All(families, family => Assert.Contains(shapes, shape => shape.Name.Contains(family, StringComparison.Ordinal)));
		Assert.Contains(shapes, shape => shape.FileExtension == TypeScriptMatchBoundStressSet.MARKUP_FILE_EXTENSION);
		Assert.Contains(shapes, shape => shape.FileExtension == TypeScriptMatchBoundStressSet.DECLARATION_FILE_EXTENSION);
	}

	[Fact]
	public void TheNonNullAssertionThenSlashShapeHoldsTenThousandLines()
	{
		MatchBoundStressShape shape = TypeScriptMatchBoundStressSet.Build().Single(candidate => candidate.Name == "ts-ten-thousand-lines-of-non-null-assertion-then-slash");

		Assert.Equal(10_000, shape.Text.Split(_LINE_FEED, StringSplitOptions.RemoveEmptyEntries).Count(line => line.Contains("!/", StringComparison.Ordinal)));
	}

	/// <summary>The predicate shapes that the timing gate scans at the file-size ceiling are scanned here at 64 KiB with no clock: each must scan to its end through the test-local profile.</summary>
	[Fact]
	public void EveryPredicateShapeOfTheThroughputGateScansToItsEndThroughTheTypeScriptShapedProfile()
	{
		List<string> abandoned = [];
		LexicalScanner scanner = new(TypeScriptShapedProfile.Build());

		foreach ((string label, string unit) in ScannerThroughputTimingTests.TypeScriptPredicateShapeUnits)
		{
			StringBuilder source = new(_PREDICATE_SHAPE_SCAN_SIZE_IN_CHARACTERS);

			while (source.Length < _PREDICATE_SHAPE_SCAN_SIZE_IN_CHARACTERS)
			{
				source.Append(unit);
			}

			if (scanner.Scan(source.ToString()).WasAbandonedMidFile)
			{
				abandoned.Add(label);
			}
		}

		Assert.NotEmpty(ScannerThroughputTimingTests.TypeScriptPredicateShapeUnits);
		Assert.Empty(abandoned);
	}

	/// <summary>`TS-001` encodes the cited request-configuration type names and reads each one in every creation form the catalog covers: the annotation, the `as` and `satisfies` forms, the angle-bracket assertion, a typed parameter default and a qualified name.</summary>
	[Theory]
	[InlineData("AxiosRequestConfig")]
	[InlineData("InternalAxiosRequestConfig")]
	[InlineData("CreateAxiosDefaults")]
	public void TheDraftTypedCredentialObjectReadsEveryCitedConfigurationTypeNameInEveryCreationFormItCovers(string typeName)
	{
		string[] forms =
		[
			$"const o: {typeName} = {_AUTH_OBJECT};",
			$"const o = {_AUTH_OBJECT} as {typeName};",
			$"const o = {_AUTH_OBJECT} satisfies {typeName};",
			$"const o = <{typeName}>{_AUTH_OBJECT};",
			$"function send(options: {typeName} = {_AUTH_OBJECT}) {{ return options; }}",
			$"const o: axios.{typeName} = {_AUTH_OBJECT};"
		];

		Assert.All(forms, form =>
		{
			Match match = _EXPRESSIONS["EX-TS-001"].Match(form);

			Assert.True(match.Success, form);
			Assert.Equal("auth", match.Groups[_CONSTRUCT_GROUP].Value);
		});
	}

	/// <summary>`TS-001` encodes the cited credentials type name and reads it on the value of the `auth` key: the angle-bracket assertion, the `as` and `satisfies` forms and a qualified name.</summary>
	[Fact]
	public void TheDraftTypedCredentialObjectReadsTheCitedCredentialsTypeNameInEveryCreationFormItCovers()
	{
		string[] forms =
		[
			"const o = { auth: <AxiosBasicCredentials>{ username: 'u', password: 'p' } };",
			"const o = { auth: { username: 'u', password: 'p' } as AxiosBasicCredentials };",
			"const o = { auth: { username: 'u', password: 'p' } satisfies AxiosBasicCredentials };",
			"const o = { auth: { username: 'u', password: 'p' } as axios.AxiosBasicCredentials };"
		];

		Assert.All(forms, form =>
		{
			Match match = _EXPRESSIONS["EX-TS-001"].Match(form);

			Assert.True(match.Success, form);
			Assert.Equal("auth", match.Groups[_CONSTRUCT_GROUP].Value);
		});
	}

	/// <summary>A name that merely contains a cited name, a prefix of one, or a type that the citations do not name is not read, in the annotation and the `as` forms alike.</summary>
	[Theory]
	[InlineData("RawAxiosRequestConfig")]
	[InlineData("AxiosRequestConfigLike")]
	[InlineData("MyInternalAxiosRequestConfig")]
	[InlineData("CreateAxiosDefaultsOptions")]
	[InlineData("AxiosDefaults")]
	[InlineData("ClientOptions")]
	public void TheDraftTypedCredentialObjectDoesNotReadAnUncitedOrLookAlikeTypeName(string typeName)
	{
		string[] forms =
		[
			$"const o: {typeName} = {_AUTH_OBJECT};",
			$"const o = {_AUTH_OBJECT} as {typeName};",
			$"const o = {_AUTH_OBJECT} satisfies {typeName};",
			$"const o = <{typeName}>{_AUTH_OBJECT};"
		];

		Assert.All(forms, form => Assert.False(_EXPRESSIONS["EX-TS-001"].IsMatch(form), form));
	}

	/// <summary>`TS-003` encodes the eleven methods of the wrapped-client service as cited at version 12.0.1: each is read at the `auth` key of its options object, whether the configuration is the second argument or the only one.</summary>
	[Fact]
	public void TheDraftHttpServiceCallReadsEveryCitedMethodOfTheWrappedClient()
	{
		Assert.All(_CITED_HTTP_SERVICE_METHODS, method =>
		{
			string[] calls =
			[
				$"this.httpService.{method}(url, {_AUTH_OBJECT});",
				$"this.httpService.{method}<Reply>(url, {_AUTH_OBJECT});",
				$"this.httpService.{method}({{ url, auth: {{ username: 'u', password: 'p' }} }});"
			];

			Assert.All(calls, call =>
			{
				Match match = _EXPRESSIONS["EX-TS-003"].Match(call);

				Assert.True(match.Success, call);
				Assert.Equal("auth", match.Groups[_CONSTRUCT_GROUP].Value);
			});
		});
	}

	/// <summary>The service has no `options` method and its public getter returns the underlying client, whose own methods are not the service's: none of them is read through the receiver rule.</summary>
	[Theory]
	[InlineData("options")]
	[InlineData("axiosRef")]
	[InlineData("subscribe")]
	[InlineData("pipe")]
	public void TheDraftHttpServiceCallDoesNotReadAMemberThatTheWrappedClientDoesNotDocumentAsAMethod(string member)
	{
		string call = $"this.httpService.{member}(url, {_AUTH_OBJECT});";

		Assert.False(_EXPRESSIONS["EX-TS-003"].IsMatch(call), call);
	}

	/// <summary>`TS-003` over `TS-001`: a call on the wrapped client whose options are asserted as a cited configuration type yields both readings, and both anchor on the same `auth` key, so the declared triple drops the second.</summary>
	[Fact]
	public void TheDraftHttpServiceCallWithAnAssertedConfigurationYieldsTheWrappedClientAndTheTypedReadingAtOneAnchor()
	{
		string call = "this.httpService.post<Reply>(url, body, { auth: { username: 'u', password: 'p' } } as AxiosRequestConfig);";

		Match wrappedClient = _EXPRESSIONS["EX-TS-003"].Match(call);
		Match typedObject = _EXPRESSIONS["EX-TS-001"].Match(call);

		Assert.True(wrappedClient.Success);
		Assert.True(typedObject.Success);
		Assert.Equal(wrappedClient.Groups[_CONSTRUCT_GROUP].Index, typedObject.Groups[_CONSTRUCT_GROUP].Index);
		Assert.Equal(wrappedClient.Groups[_CONSTRUCT_GROUP].Length, typedObject.Groups[_CONSTRUCT_GROUP].Length);
	}

	/// <summary>
	/// The merged vocabulary that the draft carries as copies (no test-local file can reach the shipped private constants) is pinned to the shipped expressions: the shipped pattern of each expression that composes a
	/// constant must contain the copy verbatim, so a change to the shipped vocabulary fails here and the gate is never charged the old text.
	/// </summary>
	[Fact]
	public void EveryCopiedConstantOfTheMergedVocabularyIsStillTheTextOfTheShippedExpressionsThatComposeIt()
	{
		(string Identifier, string Copy)[] pins =
		[
			("EX-JS-013", JavaScriptDraftExpressions.MERGED_VALUE_TERMINATOR),
			("EX-JS-013", JavaScriptDraftExpressions.MERGED_USERINFO_URL),
			("EX-JS-003", JavaScriptDraftExpressions.MERGED_USERINFO_URL),
			("EX-JS-011", JavaScriptDraftExpressions.MERGED_CREDENTIAL_LITERAL),
			("EX-JS-001", LazyFormOfTheShippedWindow(JavaScriptDraftExpressions.MERGED_OBJECT_PASSWORD_MEMBER)),
			("EX-JS-015", LazyFormOfTheShippedWindow(JavaScriptDraftExpressions.MERGED_OBJECT_PASSWORD_KEY)),
			("EX-JS-004", JavaScriptDraftExpressions.MERGED_BASIC_VALUE),
			("EX-JS-004", JavaScriptDraftExpressions.MERGED_ENVIRONMENT_READ_WITH_A_BASIC_FALLBACK),
			("EX-JS-101", JavaScriptDraftExpressions.MERGED_ENVIRONMENT_DECLARATION),
			("EX-JS-018", JavaScriptDraftExpressions.MERGED_ENVIRONMENT_READ),
			("EX-JS-018", JavaScriptDraftExpressions.MERGED_FALLBACK_OPERATOR)
		];
		ImmutableDictionary<string, Regex> shipped = ShippedExpressionsIndex();
		List<string> drifted = [.. pins.Where(pin => !shipped[pin.Identifier].ToString().Contains(pin.Copy, StringComparison.Ordinal)).Select(pin => $"{pin.Identifier} does not contain {Abbreviate(pin.Copy)}")];

		Assert.Empty(drifted);
	}

	/// <summary>The charge set is the shipped inherited set in full (conservative-charge rule): the constant declaration, the member declaration, the nineteen classes, the two markers and the sixteen flow signals, `EX-JS-018` and `EX-JS-019` included, less the six expressions that a `TypeScript` expression replaces.</summary>
	[Fact]
	public void TheChargedSetHoldsEveryShippedInheritedExpressionEXJS018IncludedAndTheTwelveTypeScriptOnes()
	{
		const int SHIPPED_COUNT = 1 + TypeScriptDraftExpressions.SHIPPED_MEMBER_DECLARATION_COUNT + TypeScriptDraftExpressions.SHIPPED_CLASS_COUNT + TypeScriptDraftExpressions.SHIPPED_CONTENT_MARKER_COUNT + TypeScriptDraftExpressions.SHIPPED_FLOW_SIGNAL_COUNT;
		IEnumerable<string> charged = TypeScriptDraftExpressions.MatchBoundExpressions.Select(entry => entry.Identifier);

		Assert.Equal(SHIPPED_COUNT, TypeScriptDraftExpressions.ShippedJavaScriptExpressions.Select(entry => entry.Identifier).Distinct(StringComparer.Ordinal).Count());
		Assert.Contains("EX-JS-018", charged);
		Assert.Contains("EX-JS-019", charged);
		Assert.Contains("EX-JS-102", charged);
		Assert.Equal(SHIPPED_COUNT - TypeScriptDraftExpressions.ReplacedInheritedIdentifiers.Length + TypeScriptDraftExpressions.NewAndAmendedExpressions.Length, TypeScriptDraftExpressions.MatchBoundExpressions.Length);
		Assert.All(TypeScriptDraftExpressions.ReplacedInheritedIdentifiers, replaced => Assert.DoesNotContain(replaced, TypeScriptDraftExpressions.InheritedShippedExpressions.Select(entry => entry.Identifier)));
	}

	/// <summary>`EX-TS-101` declares the two groups of the environment record, named exactly as the engine keys on them (validator code 74 refuses `environment` without `fallbackWord`).</summary>
	[Fact]
	public void TheDraftTypedConstantDeclarationDeclaresTheEnvironmentMarkerAndTheFallbackWordGroupsAndNeverOneWithoutTheOther()
	{
		string[] groups = _EXPRESSIONS[_TYPED_CONSTANT_ID].GetGroupNames();

		Assert.Contains("environment", groups);
		Assert.Contains("fallbackWord", groups);
		Assert.Contains("name", groups);
		Assert.Contains("value", groups);
	}

	/// <summary>A typed declaration assigned from an environment value is an environment record with the word of its fallback: `Basic`, `Bearer`, or empty (another word, none, a hole-only template).</summary>
	[Theory]
	[InlineData("const header: string = process.env.AUTH_VALUE ?? 'Basic abc';", "header", "Basic")]
	[InlineData("const header: string = process.env.AUTH_VALUE || \"basic abc\";", "header", "basic")]
	[InlineData("export const header: string = import.meta.env.AUTH_VALUE ?? `Bearer abc`;", "header", "Bearer")]
	[InlineData("private readonly header: string = Bun.env['AUTH_VALUE'] ?? '';", "header", "")]
	[InlineData("public static readonly header: string | undefined = process.env.AUTH_VALUE ?? 'Token abc';", "header", "")]
	[InlineData("static header: string | undefined = Deno.env.get('AUTH_VALUE');", "header", "")]
	[InlineData("export readonly header: Header = `${process.env.AUTH_VALUE}`;", "header", "")]
	[InlineData("let header: Record<string, string>[] = process.env.AUTH_VALUE ?? 'Bearer';", "header", "Bearer")]
	[InlineData("header: string = process.env.AUTH_VALUE ?? 'Basic',", "header", "Basic")]
	public void ATypedDeclarationAssignedFromAnEnvironmentValueIsAnEnvironmentRecordWithTheWordOfItsFallback(string text, string name, string fallbackWord)
	{
		Match match = _EXPRESSIONS[_TYPED_CONSTANT_ID].Match(text);

		Assert.True(match.Success, text);
		Assert.Equal(name, match.Groups["name"].Value);
		Assert.True(match.Groups["environment"].Success, text);
		Assert.False(match.Groups["value"].Success, text);
		Assert.True(match.Groups["fallbackWord"].Success, text);
		Assert.Equal(fallbackWord, match.Groups["fallbackWord"].Value);
	}

	/// <summary>The TypeScript tails (`E!`, `E as T`, `<T>E`, `E satisfies T`), a concatenation, a chain of fallbacks and an alias of the read are not counted as a declaration: the record is not made and the engine counts no declaration for the name.</summary>
	[Theory]
	[InlineData("const header: string = process.env.AUTH_VALUE!;")]
	[InlineData("const header = process.env.AUTH_VALUE as string;")]
	[InlineData("const header: string = <string>process.env.AUTH_VALUE;")]
	[InlineData("const header = process.env.AUTH_VALUE satisfies string;")]
	[InlineData("const header: string = process.env.AUTH_VALUE ?? 'Basic' + suffix;")]
	[InlineData("const header: string = process.env.AUTH_VALUE ?? other ?? 'Basic';")]
	[InlineData("const header: string = env.AUTH_VALUE ?? 'Basic';")]
	[InlineData("const header: string = process.env.AUTH_VALUE ?? fallback;")]
	[InlineData("const header: string = (process.env.AUTH_VALUE as string) ?? 'Basic';")]
	[InlineData("const header: (a: string) => string = process.env.AUTH_VALUE;")]
	public void ATypeScriptTailOrAnyOtherShapeThanTheDeclaredEnvironmentFormsIsNotCountedAsADeclaration(string text)
	{
		Assert.False(_EXPRESSIONS[_TYPED_CONSTANT_ID].IsMatch(text), text);
	}

	/// <summary>A name assigned from an environment value and reassigned to a literal is two counted declarations, so the engine resolves it to no single source (indeterminate), as on `JavaScript`; a name declared once is one.</summary>
	[Theory]
	[InlineData("const header: string = process.env.AUTH_VALUE ?? '';\nheader = 'Basic abc';", 2)]
	[InlineData("let header: string = 'Basic abc';\nheader = process.env.AUTH_VALUE ?? 'Bearer';", 2)]
	[InlineData("const header: string = process.env.AUTH_VALUE ?? 'Basic abc';", 1)]
	[InlineData("const header: string = process.env.AUTH_VALUE!;\nheader = 'Basic abc';", 1)]
	public void ANameAssignedFromTheEnvironmentAndReassignedToALiteralIsCountedAsTwoDeclarations(string text, int declarationCount)
	{
		IEnumerable<Match> declarations = _EXPRESSIONS[_TYPED_CONSTANT_ID].Matches(text).Where(match => match.Groups["name"].Value == "header");

		Assert.Equal(declarationCount, declarations.Count());

		if (declarationCount == _DECLARATIONS_OF_A_CONFLICT)
		{
			Assert.Single(declarations, match => match.Groups["environment"].Success);
			Assert.Single(declarations, match => match.Groups["value"].Success);
		}
	}

	/// <summary>On an untyped declaration the draft reads exactly what the shipped constant declaration reads (the name, the kind of record, the fallback word and the literal word), so the typed form adds the annotation and changes nothing else.</summary>
	[Theory]
	[InlineData("const header = process.env.AUTH_VALUE ?? 'Basic abc';")]
	[InlineData("export const header = import.meta.env.AUTH_VALUE || \"Bearer abc\";")]
	[InlineData("let header = Bun.env.AUTH_VALUE;")]
	[InlineData("var header = Deno.env.get('AUTH_VALUE') ?? '';")]
	[InlineData("header = `${process.env.AUTH_VALUE}`;")]
	[InlineData("const header = process.env.AUTH_VALUE!;")]
	[InlineData("const header = process.env.AUTH_VALUE ?? 'Basic' + suffix;")]
	[InlineData("const header = 'Basic abc';")]
	[InlineData("const header = 'Basic ' + encoded;")]
	[InlineData("header = \"Bearer abc\"; // comment")]
	public void OnAnUntypedDeclarationTheDraftReadsWhatTheShippedConstantDeclarationReads(string text)
	{
		Match draft = _EXPRESSIONS[_TYPED_CONSTANT_ID].Match(text);
		Match shipped = ShippedExpressionsIndex()["EX-JS-101"].Match(text);

		Assert.Equal(shipped.Success, draft.Success);

		foreach (string group in new[] { "name", "value", "environment", "fallbackWord" })
		{
			Assert.Equal(shipped.Groups[group].Success, draft.Groups[group].Success);
			Assert.Equal(shipped.Groups[group].Value, draft.Groups[group].Value);
		}
	}

	/// <summary>
	/// By reading the shipped `EX-JS-013`: the match opens at the colon or the equals sign and reads the name by a look-behind of the characters directly before that sign, so in a typed declaration
	/// <c>const NAME: T = '&lt;userinfo URL&gt;'</c> the sign that opens the match is the equals sign and the name that is read, and tested for the proxy refusal, is the type word. A proxy-named typed declaration is therefore read;
	/// the same name untyped, a key, and a typed declaration whose type word denotes a proxy are refused.
	/// </summary>
	[Theory]
	[InlineData("const proxyUrl: string = 'https://exampleUser:exampleSecret@host.example/p';", "string")]
	[InlineData("private proxyServer: URL = \"https://exampleUser:exampleSecret@host.example/p\";", "URL")]
	[InlineData("export const upstreamProxies: string = `https://exampleUser:exampleSecret@host.example/p`;", "string")]
	public void AProxyNamedTypedDeclarationIsReadByTheProxyRefusalAtItsTypeWordNotItsName(string text, string readName)
	{
		Match match = ShippedExpressionsIndex()["EX-JS-013"].Match(text);

		Assert.True(match.Success, text);
		Assert.Equal(readName, match.Groups[_CONSTRUCT_GROUP].Value);
	}

	[Theory]
	[InlineData("const proxyUrl = 'https://exampleUser:exampleSecret@host.example/p';")]
	[InlineData("const options = { proxyUrl: 'https://exampleUser:exampleSecret@host.example/p' };")]
	[InlineData("const serviceUrl: ProxyOptions = 'https://exampleUser:exampleSecret@host.example/p';")]
	public void TheProxyRefusalHoldsForAnUntypedDeclarationAKeyAndATypeWordThatDenotesAProxy(string text)
	{
		Assert.False(ShippedExpressionsIndex()["EX-JS-013"].IsMatch(text), text);
	}

	/// <summary>
	/// By reading the terminator set: a credential literal followed by <c>as T</c>, <c>satisfies T</c> or a postfix <c>!</c> is not whole (the terminator is a comma, a closing bracket of any kind, a semicolon, a line end,
	/// a comment or the end of the text), so the group `credential` is not captured and the in-code secret warning is not raised, although the finding itself is read unchanged. Shown on the draft expressions and on the shipped one.
	/// </summary>
	[Theory]
	[InlineData(" as string")]
	[InlineData(" satisfies string")]
	[InlineData("!")]
	public void ACredentialLiteralFollowedByATypeScriptTailIsNotWholeSoNoCredentialIsCapturedAndTheFindingIsUnchanged(string tail)
	{
		string whole = "axios.get<Reply>(url, { auth: { username: u, password: 'exampleSecret' } });";
		string tailed = $"axios.get<Reply>(url, {{ auth: {{ username: u, password: 'exampleSecret'{tail} }} }});";
		string typedWhole = "const o: AxiosRequestConfig = { auth: { username: 'u', password: 'exampleSecret' } };";
		string typedTailed = $"const o: AxiosRequestConfig = {{ auth: {{ username: 'u', password: 'exampleSecret'{tail} }} }};";
		string plainTailed = $"axios.get(url, {{ auth: {{ username: u, password: 'exampleSecret'{tail} }} }});";

		AssertCredentialIsCapturedOnlyWhenWhole(_EXPRESSIONS["EX-TS-G-001"], whole, tailed);
		AssertCredentialIsCapturedOnlyWhenWhole(_EXPRESSIONS["EX-TS-001"], typedWhole, typedTailed);
		AssertCredentialIsCapturedOnlyWhenWhole(ShippedExpressionsIndex()["EX-JS-001"], "axios.get(url, { auth: { username: u, password: 'exampleSecret' } });", plainTailed);
	}

	/// <summary>The copied password member and key use the greedy window; the shipped text carries the lazy form of the same window, so the pin compares with the greedy window put back to its lazy form.</summary>
	private static string LazyFormOfTheShippedWindow(string copy)
	{
		return copy.Replace(JavaScriptDraftExpressions.ARGUMENT_WINDOW_WITHOUT_LAZINESS, JavaScriptDraftExpressions.LAZY_ARGUMENT_WINDOW_OF_THE_SHIPPED_TEXT, StringComparison.Ordinal);
	}

	private static void AssertCredentialIsCapturedOnlyWhenWhole(Regex expression, string whole, string tailed)
	{
		Match wholeMatch = expression.Match(whole);
		Match tailedMatch = expression.Match(tailed);

		Assert.True(wholeMatch.Success, whole);
		Assert.Equal("exampleSecret", wholeMatch.Groups["credential"].Value);
		Assert.True(tailedMatch.Success, tailed);
		Assert.False(tailedMatch.Groups["credential"].Success, tailed);
		Assert.Equal(wholeMatch.Groups[_CONSTRUCT_GROUP].Index, tailedMatch.Groups[_CONSTRUCT_GROUP].Index);
	}

	private static ImmutableDictionary<string, Regex> BuildExpressionIndex()
	{
		ImmutableDictionary<string, Regex>.Builder builder = ImmutableDictionary.CreateBuilder<string, Regex>(StringComparer.Ordinal);

		foreach ((string identifier, Regex expression) in TypeScriptDraftExpressions.NewAndAmendedExpressions)
		{
			builder.Add(identifier, expression);
		}

		return builder.ToImmutable();
	}

	private static ImmutableDictionary<string, Regex> ShippedExpressionsIndex()
	{
		return TypeScriptDraftExpressions.ShippedJavaScriptExpressions.ToImmutableDictionary(entry => entry.Identifier, entry => entry.Expression, StringComparer.Ordinal);
	}

	private static string Escape(string text)
	{
		return text.Replace(_LINE_FEED, "\\n", StringComparison.Ordinal);
	}

	private static string Abbreviate(string text)
	{
		return text.Length <= _ABBREVIATION_MAXIMUM_LENGTH ? text : string.Concat(text.AsSpan(0, _ABBREVIATION_MAXIMUM_LENGTH), "...");
	}

	private static JavaScriptDraftExpressionSemanticTests.Case Yes(string id, string text, string group = "", string value = "")
	{
		return new JavaScriptDraftExpressionSemanticTests.Case(id, text, true, group, value);
	}

	private static JavaScriptDraftExpressionSemanticTests.Case No(string id, string text)
	{
		return new JavaScriptDraftExpressionSemanticTests.Case(id, text, false);
	}

	private static ImmutableArray<JavaScriptDraftExpressionSemanticTests.Case> TypedConstantCases()
	{
		return
		[
			Yes("EX-TS-101", "const header: string = 'Basic ' + encoded;", "name", "header"),
			Yes("EX-TS-101", "const header: string = 'Basic ' + encoded;", "value", "Basic"),
			Yes("EX-TS-101", "private readonly mode: string = \"Bearer abc\";", "name", "mode"),
			Yes("EX-TS-101", "private readonly mode: string = \"Bearer abc\";", "value", "Bearer"),
			Yes("EX-TS-101", "export const scheme: 'Basic' | 'Bearer' = 'Basic';", "name", "scheme"),
			Yes("EX-TS-101", "static token = `Bearer`;", "value", "Bearer"),
			Yes("EX-TS-101", "export declare readonly kind: Kind = 'basic';", "name", "kind"),
			Yes("EX-TS-101", "const a$b: string = `x$y`;", "name", "a$b"),
			No("EX-TS-101", "const header: string = `Bearer ${token}`;"),
			No("EX-TS-101", "const a: string = b;"),
			No("EX-TS-101", "if (ready) { mode = 'basic'; }"),
			No("EX-TS-101", "const object: Options = { mode: 'basic' };"),
			No("EX-TS-101", "const f: (a: string) => string = 'x';"),
			No("EX-TS-101", "same === 'basic'"),
			No("EX-TS-101", "type Scheme = 'Basic';")
		];
	}

	private static ImmutableArray<JavaScriptDraftExpressionSemanticTests.Case> TypedObjectCases()
	{
		string prefixAtTheBound = $"const o: AxiosRequestConfig = {{{new string(_FILLER_UNIT, _FLAT_PREFIX_AT_THE_BOUND - 1)} auth: {{ username: 'u', password: 'p' }} }};";
		string prefixPastTheBound = $"const o: AxiosRequestConfig = {{{new string(_FILLER_UNIT, _FLAT_PREFIX_PAST_THE_BOUND - 1)} auth: {{ username: 'u', password: 'p' }} }};";

		return
		[
			Yes("EX-TS-001", $"const o: AxiosRequestConfig = {_AUTH_OBJECT};", "construct", "auth"),
			Yes("EX-TS-001", "const o: AxiosRequestConfig = { url: u, timeout: 5, auth: { username: 'u', password: 'p' } };", "construct", "auth"),
			Yes("EX-TS-001", "const o: AxiosRequestConfig = { auth: { username: 'u', password: 'exampleSecret' } };", "credential", "exampleSecret"),
			Yes("EX-TS-001", "const o: AxiosRequestConfig = { auth: { username: 'u', password: 'changeme' } };", "credential", string.Empty),
			Yes("EX-TS-001", "const o: AxiosRequestConfig = { auth: { username: 'u', password: 'exampleSecret' + suffix } };", "construct", "auth"),
			Yes("EX-TS-001", "const o: AxiosRequestConfig = { auth: { username: 'u', password: 'exampleSecret' + suffix } };", "credential", string.Empty),
			Yes("EX-TS-001", "const o: AxiosRequestConfig = { auth: { username: 'u', password: 'exampleSecret' as string } };", "credential", string.Empty),
			Yes("EX-TS-001", "const o: axios.AxiosRequestConfig = { auth: { username: 'u', password: 'p' } };", "construct", "auth"),
			Yes("EX-TS-001", "const o: InternalAxiosRequestConfig = { auth: { username: 'u', password: 'p' } };", "construct", "auth"),
			Yes("EX-TS-001", "function send(options: AxiosRequestConfig = { auth: { username: 'u', password: 'p' } }) { return options; }", "construct", "auth"),
			Yes("EX-TS-001", $"const o = {_AUTH_OBJECT} as AxiosRequestConfig;", "construct", "auth"),
			Yes("EX-TS-001", $"const o = {_AUTH_OBJECT} satisfies AxiosRequestConfig;", "construct", "auth"),
			Yes("EX-TS-001", "const o = { timeout: 5, auth: { username: 'u', password: 'p' }, url: u } as axios.AxiosRequestConfig;", "construct", "auth"),
			Yes("EX-TS-001", $"const o = <AxiosRequestConfig>{_AUTH_OBJECT};", "construct", "auth"),
			Yes("EX-TS-001", "const o = { auth: <AxiosBasicCredentials>{ username: 'u', password: 'p' } };", "construct", "auth"),
			Yes("EX-TS-001", "const o = { auth: { username: 'u', password: 'p' } as AxiosBasicCredentials };", "construct", "auth"),
			Yes("EX-TS-001", "const defaults: CreateAxiosDefaults = { baseURL: u, auth: { username: 'u', password: 'p' } };", "construct", "auth"),
			Yes("EX-TS-001", $"const o = {_AUTH_OBJECT} satisfies CreateAxiosDefaults;", "construct", "auth"),
			Yes("EX-TS-001", $"const o = {_AUTH_OBJECT} as InternalAxiosRequestConfig;", "construct", "auth"),
			Yes("EX-TS-001", "const o = { auth: { username: 'u', password: 'p' } satisfies AxiosBasicCredentials };", "construct", "auth"),
			Yes("EX-TS-001", $"const o = {_AUTH_OBJECT} as AxiosRequestConfig<Reply>;", "construct", "auth"),
			Yes("EX-TS-001", prefixAtTheBound, "construct", "auth"),
			No("EX-TS-001", prefixPastTheBound),
			No("EX-TS-001", $"const o = {_AUTH_OBJECT};"),
			No("EX-TS-001", $"const o: ClientOptions = {_AUTH_OBJECT};"),
			No("EX-TS-001", "const o: AxiosRequestConfig = { auth: { token: 't' } };"),
			No("EX-TS-001", "const o: AxiosRequestConfig = { auth: { username: u, password: p } };"),
			No("EX-TS-001", "const o: AxiosRequestConfig = { auth: { username: 'u' } };"),
			No("EX-TS-001", "interface Config extends AxiosRequestConfig { auth: { username: string; password: string } }"),
			No("EX-TS-001", "const o: AxiosRequestConfig = { headers: { a: 'b' }, auth: { username: 'u', password: 'p' } };"),
			No("EX-TS-001", "const o: AxiosRequestConfig = { proxy: { auth: { username: 'u', password: 'p' } } };"),
			No("EX-TS-001", "const label: string = 'AxiosRequestConfig'; const o = { auth: { username: 'u', password: 'p' } };"),
			No("EX-TS-001", $"const o: CreateAxiosDefaultsOptions = {_AUTH_OBJECT};"),
			No("EX-TS-001", $"const o: MyInternalAxiosRequestConfig = {_AUTH_OBJECT};"),
			No("EX-TS-001", $"const o: RawAxiosRequestConfig = {_AUTH_OBJECT};"),
			No("EX-TS-001", $"const o: AxiosRequestConfig<Reply> = {_AUTH_OBJECT};"),
			No("EX-TS-001", "const o: AxiosBasicCredentials = { username: 'u', password: 'p' };"),
			No("EX-TS-001", "const o = { auth: <AxiosBasicCredentialsLike>{ username: 'u', password: 'p' } };")
		];
	}

	private static ImmutableArray<JavaScriptDraftExpressionSemanticTests.Case> HttpServiceCases()
	{
		return
		[
			Yes("EX-TS-003", "this.httpService.post<Reply>(url, { auth: { username: 'u', password: 'p' } });", "construct", "auth"),
			Yes("EX-TS-003", "this.http.get(url, { auth: { user: u, pass: p } });", "construct", "auth"),
			Yes("EX-TS-003", "const r = this.myHTTPClient.request<A<B>>(url, { auth: { username: u, password: p } });", "construct", "auth"),
			Yes("EX-TS-003", "return this.httpService.delete(url, { auth: { username: u, password: 'exampleSecret' } });", "credential", "exampleSecret"),
			Yes("EX-TS-003", "this.httpService.post(url, { auth: { username: u, password: 'exampleSecret' + suffix } });", "construct", "auth"),
			Yes("EX-TS-003", "this.httpService.post(url, { auth: { username: u, password: 'exampleSecret' + suffix } });", "credential", string.Empty),
			No("EX-TS-003", "this.service.post(url, { auth: { username: 'u', password: 'p' } });"),
			No("EX-TS-003", "this.httpService.get(url, { headers: { Accept: 'x' } });"),
			No("EX-TS-003", "this.httpService.post(url, { auth: { token: t } });"),
			Yes("EX-TS-003", "this.httpService.postForm(url, data, { auth: { username: 'u', password: 'p' } });", "construct", "auth"),
			Yes("EX-TS-003", "this.httpService.query<Reply>(url, { auth: { username: 'u', password: 'p' } });", "construct", "auth"),
			Yes("EX-TS-003", "this.httpService.request({ url, method: 'GET', auth: { username: 'u', password: 'p' } });", "construct", "auth"),
			No("EX-TS-003", "this.httpService.options(url, { auth: { username: 'u', password: 'p' } });"),
			No("EX-TS-003", "this.httpService.axiosRef.get(url, { auth: { username: 'u', password: 'p' } });"),
			No("EX-TS-003", "this.client.get(url, { auth: { username: 'u', password: 'p' } });"),
			No("EX-TS-003", "this.httpService.fetchAll(url, { auth: { username: u, password: p } });"),
			No("EX-TS-003", "const u = 'https://host.example/a'; foo.get(u, { auth: { username: u, password: p } });")
		];
	}

	private static ImmutableArray<JavaScriptDraftExpressionSemanticTests.Case> GenericArgumentGroupCases()
	{
		string bodyAtTheBound = new(_FILLER_UNIT, _GENERIC_BODY_AT_THE_BOUND);
		string bodyPastTheBound = new(_FILLER_UNIT, _GENERIC_BODY_PAST_THE_BOUND);

		return
		[
			Yes("EX-TS-G-001", "axios.post<Reply>(url, { auth: { username: 'u', password: 'p' } });", "construct", "auth"),
			Yes("EX-TS-G-001", "axios<Reply>(url, { auth: { username: 'u', password: 'p' } });", "construct", "auth"),
			Yes("EX-TS-G-001", "request<Reply>(url, { auth: { username: 'u', password: 'p' } });", "construct", "auth"),
			Yes("EX-TS-G-001", "axios.get<A, B>(url, { auth: { username: u, password: p } });", "construct", "auth"),
			Yes("EX-TS-G-001", "axios.get<A<B>>(url, { auth: { username: u, password: p } });", "construct", "auth"),
			Yes("EX-TS-G-001", "axios.get<A<B<C>>>(url, { auth: { username: u, password: p } });", "construct", "auth"),
			Yes("EX-TS-G-001", "axios.get<(a: A) => B>(url, { auth: { username: u, password: p } });", "construct", "auth"),
			Yes("EX-TS-G-001", "axios.get<{ a: string; b: number }>(url, { auth: { username: u, password: p } });", "construct", "auth"),
			Yes("EX-TS-G-001", $"axios.get<{bodyAtTheBound}>(url, {{ auth: {{ username: u, password: p }} }});", "construct", "auth"),
			Yes("EX-TS-G-001", "axios.get(url, { auth: { username: u, password: p } });", "construct", "auth"),
			Yes("EX-TS-G-001", "axios.get<Reply>(url, { auth: { username: u, password: 'exampleSecret' } });", "credential", "exampleSecret"),
			Yes("EX-TS-G-001", "axios.get<Reply>(url, { auth: { username: u, password: 'exampleSecret' + suffix } });", "construct", "auth"),
			Yes("EX-TS-G-001", "axios.get<Reply>(url, { auth: { username: u, password: 'exampleSecret' + suffix } });", "credential", string.Empty),
			No("EX-TS-G-001", "axios.get<A<B<C<D>>>>(url, { auth: { username: u, password: p } });"),
			No("EX-TS-G-001", $"axios.get<{bodyPastTheBound}>(url, {{ auth: {{ username: u, password: p }} }});"),
			No("EX-TS-G-001", "axios.get<A\nB>(url, { auth: { username: u, password: p } });"),
			No("EX-TS-G-001", "axios.get<A(url, { auth: { username: u, password: p } });"),
			No("EX-TS-G-001", "axios.get<Reply>(url, { auth: { token: t } });"),
			No("EX-TS-G-001", "axios.get<Reply>(url, { headers: { Accept: 'x' } });"),
			Yes("EX-TS-G-003", "axios.get<Reply>('https://exampleUser:exampleSecret@host.example/path');", "credential", "exampleSecret"),
			Yes("EX-TS-G-003", "const r = await fetch<Reply<Item>>('https://exampleUser:exampleSecret@host.example/path');", "construct", "fetch"),
			Yes("EX-TS-G-003", "http.get('https://exampleUser:exampleSecret@host.example/path', handler);", "construct", "http.get"),
			Yes("EX-TS-G-003", "axios.get<Reply>('https://exampleUser:exampleSecret@host.example/path' + query);", "construct", "axios.get"),
			Yes("EX-TS-G-003", "axios.get<Reply>('https://exampleUser:exampleSecret@host.example/path' + query);", "credential", string.Empty),
			No("EX-TS-G-003", "axios.get<A<B<C<D>>>>('https://exampleUser:exampleSecret@host.example/path');"),
			No("EX-TS-G-003", "axios.get<Reply>('https://host.example/path');"),
			Yes("EX-TS-G-015", "got<Reply>(url, { username: u, password: 'p' });", "construct", "got"),
			Yes("EX-TS-G-015", "got.post<Reply<Item>>(url, { username: u, password: p });", "construct", "got"),
			Yes("EX-TS-G-015", "got<Reply>(url, { username: u, password: 'exampleSecret' });", "credential", "exampleSecret"),
			Yes("EX-TS-G-015", "got<Reply>(url, { username: u, password: 'exampleSecret' + suffix });", "construct", "got"),
			Yes("EX-TS-G-015", "got<Reply>(url, { username: u, password: 'exampleSecret' + suffix });", "credential", string.Empty),
			No("EX-TS-G-015", "got<Reply>(url, { username: u });"),
			No("EX-TS-G-015", "got<A<B<C<D>>>>(url, { username: u, password: p });"),
			Yes("EX-TS-G-016", "request<Reply>(url, { auth: { user: u, pass: p, sendImmediately: false } });", "construct", "auth"),
			Yes("EX-TS-G-016", "request.get<A<B>>(url, { auth: { user: u, pass: p, sendImmediately: false } });", "construct", "auth"),
			No("EX-TS-G-016", "request<Reply>(url, { auth: { user: u, pass: p, sendImmediately: true } });"),
			No("EX-TS-G-016", "request<A<B<C<D>>>>(url, { auth: { user: u, pass: p, sendImmediately: false } });")
		];
	}

	private static ImmutableArray<JavaScriptDraftExpressionSemanticTests.Case> TailRefusalCases()
	{
		string bodyAtTheBound = new(_FILLER_UNIT, _REFUSAL_BODY_AT_THE_BOUND - 1);
		string bodyPastTheBound = new(_FILLER_UNIT, _REFUSAL_BODY_PAST_THE_BOUND - 1);

		return
		[
			No("EX-TS-T-004", "interface Req { Authorization: 'Basic'; }"),
			No("EX-TS-T-004", "type H = { Authorization: 'Basic' | 'Bearer' };"),
			No("EX-TS-T-004", "type H = { Authorization: `Basic ${string}` };"),
			No("EX-TS-T-004", "type H = { Authorization: `Basic ${number}` };"),
			No("EX-TS-T-004", "type H = { Authorization: 'Basic' ;};"),
			No("EX-TS-T-004", "type H = { Authorization: \"Basic\"|'Bearer' };"),
			No("EX-TS-T-004", "type H = { Authorization: 'Basic abc'; };"),
			No("EX-TS-T-004", $"type H = {{ Authorization: 'Basic {bodyAtTheBound}'; }};"),
			Yes("EX-TS-T-004", $"type H = {{ Authorization: 'Basic {bodyPastTheBound}'; }};", "construct", "Authorization"),
			Yes("EX-TS-T-004", "const h = { Authorization: 'Basic ' + t };", "construct", "Authorization"),
			Yes("EX-TS-T-004", "const h = { Authorization: `Basic ${t}` };", "construct", "Authorization"),
			No("EX-TS-T-004", "const h = { Authorization: `Basic ${string}Value` };"),
			Yes("EX-TS-T-004", "const h = { Authorization: `Basic ${stringValue}` };", "construct", "Authorization"),
			Yes("EX-TS-T-004", "const h = { Authorization: 'Basic' || fallback };", "construct", "Authorization"),
			Yes("EX-TS-T-004", "const h = { Authorization: basic`Basic ${t}` };", "construct", "Authorization"),
			Yes("EX-TS-T-004", "headers.Authorization = 'Basic';", "construct", "headers"),
			Yes("EX-TS-T-004", "interface Req { headers: { Authorization: 'Basic'; } }", "construct", "headers"),
			Yes("EX-TS-T-004", "type H = { Authorization: 'Basic' };", "construct", "Authorization"),
			Yes("EX-TS-T-004", "type H = { Authorization: 'Basic', other: string };", "construct", "Authorization"),
			Yes("EX-TS-T-004", "interface Req {\n\tAuthorization: 'Basic'\n}", "construct", "Authorization"),
			Yes("EX-TS-T-004", "headers.Authorization = 'Basic ZXhhbXBsZTpwdw==';", "credential", "ZXhhbXBsZTpwdw=="),
			Yes("EX-TS-T-004", "headers.Authorization = 'Basic ZXhhbXBsZTpwdw==' + extra;", "construct", "headers"),
			Yes("EX-TS-T-004", "headers.Authorization = 'Basic ZXhhbXBsZTpwdw==' + extra;", "credential", string.Empty),
			Yes("EX-TS-T-004", "const h = { Authorization: process.env.AUTH ?? 'Basic ZXhhbXBsZTpwdw==' };", "construct", "Authorization"),
			Yes("EX-TS-T-004", "const h = { Authorization: process.env.AUTH ?? 'Basic ZXhhbXBsZTpwdw==' };", "credential", "ZXhhbXBsZTpwdw=="),
			Yes("EX-TS-T-004", "const h: HeadersInit = { Authorization: process.env.AUTH || 'Basic' };", "construct", "Authorization"),
			Yes("EX-TS-T-004", "const h = { Authorization: import.meta.env.AUTH ?? \"Basic\" };", "construct", "Authorization"),
			Yes("EX-TS-T-004", "const h = { Authorization: Deno.env.get('AUTH') ?? `Basic` };", "construct", "Authorization"),
			Yes("EX-TS-T-004", "const h = { Authorization: Bun.env['AUTH'] || 'Basic' };", "construct", "Authorization"),
			Yes("EX-TS-T-004", "headers.Authorization = process.env.AUTH || 'Basic';", "construct", "headers"),
			No("EX-TS-T-004", "const h = { Authorization: process.env.AUTH ?? 'Bearer abc' };"),
			No("EX-TS-T-004", "const h = { Authorization: process.env.AUTH ?? 'Basic' + suffix };"),
			No("EX-TS-T-004", "const h = { Authorization: env.AUTH ?? 'Basic' };"),
			No("EX-TS-T-004", "const h = { Authorization: process.env.AUTH! || 'Basic' };"),
			No("EX-TS-T-004", "const h = { Authorization: (process.env.AUTH as string) || 'Basic' };"),
			No("EX-TS-T-004", "const h = { Authorization: `Bearer ${t}` };"),
			No("EX-TS-T-004", "const h = { 'Proxy-Authorization': 'Basic x' };")
		];
	}

	private static ImmutableArray<JavaScriptDraftExpressionSemanticTests.Case> FlowSignalCases()
	{
		return
		[
			Yes("EX-TS-120", "const pw = this.loginForm.get('password')?.value;", _SIGNAL_GROUP, "get"),
			Yes("EX-TS-120", "const pw = this.form.get(\"password\")!.value;", _SIGNAL_GROUP, "get"),
			Yes("EX-TS-120", "const pw = this.form.controls['password'].value;", _SIGNAL_GROUP, "controls"),
			Yes("EX-TS-120", "const pw = this.form.controls.password.value;", _SIGNAL_GROUP, "controls"),
			Yes("EX-TS-120", "const pw = this.loginForm.value.password;", _SIGNAL_GROUP, "value"),
			No("EX-TS-120", "const pw = this.settings.get('password')?.value;"),
			No("EX-TS-120", "const name = this.form.get('username')?.value;"),
			No("EX-TS-120", "const pw = formData.get('password');"),
			No("EX-TS-120", "this.form.controls.password.setValue(x);"),
			No("EX-TS-120", "const pw = this.settings.value.password;"),
			Yes("EX-TS-121", "async login(@Body('password') secret: string) { return secret; }", _SIGNAL_GROUP, "Body"),
			Yes("EX-TS-121", "async login(@Body() { username, password }: LoginDto) { return password; }", _SIGNAL_GROUP, "Body"),
			Yes("EX-TS-121", "async login(@Body(\"userPassword\") p: string) { return p; }", _SIGNAL_GROUP, "Body"),
			No("EX-TS-121", "async login(@Body() dto: LoginDto) { return dto; }"),
			No("EX-TS-121", "async login(@Body('name') name: string) { return name; }"),
			No("EX-TS-121", "async login(@Query('password') p: string) { return p; }"),
			No("EX-TS-121", "async login(@Body() { name, title }: Dto) { return name; }"),
			Yes("EX-TS-122", "const dbPassword = this.configService.get<string>('DB_PASSWORD');", _SIGNAL_GROUP, "get"),
			Yes("EX-TS-122", "const key = this.configService.getOrThrow('API_SECRET');", _SIGNAL_GROUP, "getOrThrow"),
			Yes("EX-TS-122", "const p = configService.get('db.password');", _SIGNAL_GROUP, "get"),
			No("EX-TS-122", "const port = this.configService.get<number>('PORT');"),
			No("EX-TS-122", "const p = this.settingsService.get('DB_PASSWORD');"),
			No("EX-TS-122", "this.configService.set('DB_PASSWORD', value);"),
			Yes("EX-TS-123", "private readonly apiPassword: string = 'exampleSecretValue';", _SIGNAL_GROUP, "apiPassword"),
			Yes("EX-TS-123", "const dbToken: string | undefined = \"abc\";", _SIGNAL_GROUP, "dbToken"),
			Yes("EX-TS-123", "static secret: Secret = `abc`;", _SIGNAL_GROUP, "secret"),
			No("EX-TS-123", "const apiPassword = 'exampleSecretValue';"),
			No("EX-TS-123", "const apiPasswordLabel: string = 'Password';"),
			No("EX-TS-123", "private apiPassword: string;"),
			No("EX-TS-123", "const mode: string = 'basic';"),
			No("EX-TS-123", "credentials: RequestCredentials = 'include';"),
			No("EX-TS-123", "const apiPassword: string = '';")
		];
	}
}
