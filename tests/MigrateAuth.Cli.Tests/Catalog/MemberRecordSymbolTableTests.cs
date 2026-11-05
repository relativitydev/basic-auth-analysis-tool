using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE MEMBER RECORDS OF THE FILE SYMBOL TABLE (a second dictionary, keyed by member name, beside the variable dictionary). A member record is a line-start
/// object-literal member or property assignment whose property name ends with <c>authorization</c> in any case; every such assignment of the name is collected in any form and only an
/// environment read can resolve; two or more records are indeterminate; resolution is order-free; a flag key and an environment name that holds no credential word are not records;
/// the variable dictionary, <see cref="FileSymbolTable.DeclarationCountOf"/> and <see cref="FileSymbolTable.TryResolveRecordUnambiguously"/> are not touched by any of it. The
/// validator refuses a member expression without the match bound or without the group <c>name</c>, and a member cell in a profile that declares no member expression. Every script is
/// invented with placeholder names, through the shipped `JavaScript` and `TypeScript` profiles.
/// </summary>
public sealed class MemberRecordSymbolTableTests
{
	private const string _MEMBER = "serviceAuthorization";
	private const string _VARIABLE = "authValue";
	private const string _SETTINGS_PROPERTY_CLASS = "JS-019";
	private const string _ENVIRONMENT_CLASS = "JS-018";
	private const string _LITERAL_CLASS = "JS-017";
	private const string _JAVASCRIPT_PROFILE = "JavaScript";
	private const string _TYPESCRIPT_PROFILE = "TypeScript";
	private static readonly TimeSpan _NO_BACKTRACKING_GUARD_TIMEOUT = TimeSpan.FromSeconds(30);
	private const int _MEMBER_BOUND_CODE = 79;
	private const int _MEMBER_MALFORMED_CODE = 80;
	private const int _MEMBER_KIND_WITHOUT_EXPRESSION_CODE = 81;
	private const RegexOptions _OPTIONS = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline;
	private const string _LITERAL_SHAPE = "\t{0}: {1},\n";
	private const string _ASSIGNMENT_SHAPE = "settings.{0} = {1};\n";
	private const string _THIS_SHAPE = "\tthis.{0} = {1};\n";
	private const string _QUOTED_KEY_SHAPE = "\t'{0}': {1},\n";

	/// <summary>The five forms of the environment read, with a credential word in the variable name.</summary>
	private static readonly string[] _environmentForms =
	[
		"process.env.SERVICE_AUTH_HEADER",
		"process.env['SERVICE_AUTH_HEADER']",
		"import.meta.env.VITE_SERVICE_TOKEN",
		"Bun.env.SERVICE_SECRET",
		"Deno.env.get('SERVICE_AUTH_HEADER')"
	];

	private static readonly string[] _shapes = [_LITERAL_SHAPE, _ASSIGNMENT_SHAPE, _THIS_SHAPE, _QUOTED_KEY_SHAPE];

	public static IEnumerable<object[]> EveryEnvironmentFormInEveryShape()
	{
		foreach (string environmentForm in _environmentForms)
		{
			foreach (string shape in _shapes)
			{
				yield return [string.Format(CultureInfo.InvariantCulture, shape, _MEMBER, environmentForm)];
			}
		}
	}

	// ---- The record ----

	[Theory]
	[MemberData(nameof(EveryEnvironmentFormInEveryShape))]
	public void AnEnvironmentReadOfEachOfTheFiveFormsInEveryShapeIsOneEnvironmentRecordWithAnEmptyWord(string source)
	{
		FileSymbolTable table = TableOf(source);

		Assert.Equal(1, table.MemberRecordCountOf(_MEMBER));
		Assert.True(table.TryResolveMemberRecordUnambiguously(_MEMBER, out ResolvedSymbol record), source);
		Assert.Equal(SymbolSourceKind.Environment, record.SourceKind);
		Assert.Equal(string.Empty, record.Value);
	}

	[Theory]
	[InlineData("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER || 'Basic dXNlcjpwYXNz',\n", "Basic")]
	[InlineData("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER ?? \"bearer exampleToken\",\n", "bearer")]
	[InlineData("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER || 'neutral',\n", "")]
	[InlineData("\tserviceAuthorization: `${process.env.SERVICE_AUTH_HEADER}`,\n", "")]
	[InlineData("settings.serviceAuthorization = Deno.env.get('SERVICE_AUTH_HEADER') ?? 'Basic dXNlcjpwYXNz';\n", "Basic")]
	[InlineData("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER // read at start\n", "")]
	public void TheRecordCarriesTheWordOfItsFallbackAndATemplateWithOnlyTheReadIsARecord(string source, string word)
	{
		FileSymbolTable table = TableOf(source);

		Assert.True(table.TryResolveMemberRecordUnambiguously(_MEMBER, out ResolvedSymbol record), source);
		Assert.Equal(word, record.Value);
	}

	[Theory]
	[InlineData("\tSERVICE_AUTHORIZATION: process.env.SERVICE_AUTH_HEADER,\n", "SERVICE_AUTHORIZATION")]
	[InlineData("\tAuthorization: process.env.SERVICE_AUTH_HEADER,\n", "Authorization")]
	[InlineData("settings.proxyauthorization = process.env.SERVICE_AUTH_HEADER;\n", "proxyauthorization")]
	public void ThePropertyNameIsTheWholeWordThatEndsWithAuthorizationInAnyCase(string source, string name)
	{
		Assert.True(TableOf(source).TryResolveMemberRecordUnambiguously(name, out _), source);
	}

	// ---- What is not a record ----

	[Theory]
	[InlineData("\trequireAuthorization: process.env.SERVICE_AUTH_FLAG,\n", "requireAuthorization")]
	[InlineData("\tskipAuthorization: process.env.SERVICE_AUTH_FLAG,\n", "skipAuthorization")]
	[InlineData("settings.enableAuthorization = process.env.SERVICE_AUTH_FLAG;\n", "enableAuthorization")]
	[InlineData("settings.disableAuthorization = process.env.SERVICE_AUTH_FLAG;\n", "disableAuthorization")]
	[InlineData("\tisAuthorization: process.env.SERVICE_AUTH_FLAG,\n", "isAuthorization")]
	[InlineData("\thasAuthorization: process.env.SERVICE_AUTH_FLAG,\n", "hasAuthorization")]
	[InlineData("\tuseAuthorization: process.env.SERVICE_AUTH_FLAG,\n", "useAuthorization")]
	[InlineData("\trequireAuthorization: 'yes',\n", "requireAuthorization")]
	public void AFlagKeyIsNoRecordInAnyForm(string source, string name)
	{
		FileSymbolTable table = TableOf(source);

		Assert.Equal(0, table.MemberRecordCountOf(name));
		Assert.False(table.TryResolveMemberRecordUnambiguously(name, out _));
	}

	[Theory]
	[InlineData("\tserviceAuthorization: process.env.SERVICE_HOST,\n")]
	[InlineData("\tserviceAuthorization: process.env['SERVICE_ADDRESS'],\n")]
	[InlineData("settings.serviceAuthorization = import.meta.env.VITE_ENDPOINT;\n")]
	[InlineData("settings.serviceAuthorization = Deno.env.get('SERVICE_PORT');\n")]
	[InlineData("\tserviceAuthorization: `${Bun.env.SERVICE_HOST}`,\n")]
	public void AnEnvironmentNameThatHoldsNoCredentialWordMakesTheAssignmentNoRecordAtAll(string source)
	{
		FileSymbolTable table = TableOf(source);

		Assert.Equal(0, table.MemberRecordCountOf(_MEMBER));
		Assert.False(table.TryResolveMemberRecordUnambiguously(_MEMBER, out _));
	}

	[Theory]
	[InlineData("\tauthorizationHeader: process.env.SERVICE_AUTH_HEADER,\n")]
	[InlineData("\tauth: process.env.SERVICE_AUTH_HEADER,\n")]
	[InlineData("settings.authHeader = process.env.SERVICE_AUTH_HEADER;\n")]
	[InlineData("\t'x-authorization': process.env.SERVICE_AUTH_HEADER,\n")]
	[InlineData("\t'service-authorization': process.env.SERVICE_AUTH_HEADER,\n")]
	public void APropertyThatDoesNotEndWithTheWordAndAKeyThatIsNotAnIdentifierAreNoRecord(string source)
	{
		FileSymbolTable table = TableOf(source);

		Assert.Equal(0, table.MemberRecordCountOf(_MEMBER));
		Assert.Equal(0, table.MemberRecordCountOf("authorizationHeader"));
		Assert.Equal(0, table.MemberRecordCountOf("authHeader"));
		Assert.Equal(0, table.MemberRecordCountOf("auth"));
	}

	[Theory]
	[InlineData("const settings = { serviceAuthorization: process.env.SERVICE_AUTH_HEADER };\n")]
	[InlineData("const serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\nconst settings = { serviceAuthorization };\n")]
	[InlineData("getSettings().serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n")]
	[InlineData("// settings.serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n")]
	[InlineData("/*\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER,\n*/\n")]
	[InlineData("const text = `\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER,\n`;\n")]
	[InlineData("\tserviceAuthorization:\n\t\tprocess.env.SERVICE_AUTH_HEADER,\n")]
	public void AMemberThatIsNotAtALineStartInCodeOnItsOwnLineIsNotRead(string source)
	{
		FileSymbolTable table = TableOf(source);

		Assert.Equal(0, table.MemberRecordCountOf(_MEMBER));
	}

	[Theory]
	[InlineData("class Settings {\n\tserviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n}\n")]
	[InlineData("class Settings {\n\tprivate serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n}\n")]
	[InlineData("class Settings {\n\tpublic static readonly serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n}\n")]
	[InlineData("class Settings {\n\tprotected serviceAuthorization: string = process.env.SERVICE_AUTH_HEADER;\n}\n")]
	public void AClassFieldWithAnInitializerIsCollectedWithOrWithoutModifiers(string source)
	{
		FileSymbolTable table = TableOf(source);

		Assert.Equal(1, table.MemberRecordCountOf(_MEMBER));
	}

	[Fact]
	public void AClassFieldIsCollectedForTheCountButNeverResolvesWhateverItsInitializerIs()
	{
		FileSymbolTable environment = TableOf("class Settings {\n\tserviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n}\n");
		FileSymbolTable twice = TableOf("class Settings {\n\tserviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n}\nsettings.serviceAuthorization = 'fixed';\n");
		FileSymbolTable literal = TableOf("class Settings {\n\tstatic serviceAuthorization = 'fixed';\n}\n");

		FileSymbolTable annotated = TableOf("class Settings {\n\tserviceAuthorization: string = process.env.SERVICE_AUTH_HEADER;\n}\n");
		FileSymbolTable withObjectLiteral = TableOf("class Settings {\n\tserviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n}\nconst settings = {\n\tserviceAuthorization: process.env.OTHER_AUTH_HEADER\n};\n");

		// A class field assigned from an environment value is a declared form that is not read: it is collected for the count, as another form, and is never the resolving record.
		Assert.Equal(1, environment.MemberRecordCountOf(_MEMBER));
		Assert.False(environment.TryResolveMemberRecordUnambiguously(_MEMBER, out _));
		Assert.Equal(1, annotated.MemberRecordCountOf(_MEMBER));
		Assert.False(annotated.TryResolveMemberRecordUnambiguously(_MEMBER, out _));
		Assert.Equal(2, withObjectLiteral.MemberRecordCountOf(_MEMBER));
		Assert.False(withObjectLiteral.TryResolveMemberRecordUnambiguously(_MEMBER, out _));
		Assert.Equal(2, twice.MemberRecordCountOf(_MEMBER));
		Assert.False(twice.TryResolveMemberRecordUnambiguously(_MEMBER, out _));
		Assert.Equal(1, literal.MemberRecordCountOf(_MEMBER));
		Assert.False(literal.TryResolveMemberRecordUnambiguously(_MEMBER, out _));
	}

	[Theory]
	[InlineData("interface Settings {\n\tserviceAuthorization: AuthHeader\n}\n")]
	[InlineData("interface Settings {\n\tserviceAuthorization: Models.AuthHeader<string, number>\n}\n")]
	[InlineData("type Settings = {\n\tserviceAuthorization: 'Basic' | 'Bearer'\n};\n")]
	[InlineData("interface Settings {\n\tserviceAuthorization: AuthHeader | undefined\n}\n")]
	[InlineData("interface Settings {\n\tserviceAuthorization: string[]\r\n}\r\n")]
	[InlineData("class Settings {\n\tprivate readonly serviceAuthorization: AuthHeader;\n}\n")]
	public void AMemberThatOnlyDeclaresATypeIsNoRecordWithOrWithoutASemicolon(string source)
	{
		Assert.Equal(0, TableOf(source).MemberRecordCountOf(_MEMBER));
	}

	[Theory]
	[InlineData("\tserviceAuthorization: 'fixed'\n")]
	[InlineData("\tserviceAuthorization: 'fixed',\n")]
	[InlineData("\tserviceAuthorization: other.path,\n")]
	[InlineData("\tserviceAuthorization: { value: 1 }\n")]
	[InlineData("\tserviceAuthorization: [first]\n")]
	[InlineData("\tserviceAuthorization: 42\n")]
	[InlineData("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER\n")]
	[InlineData("\tserviceAuthorization: loadValue()\n")]
	[InlineData("\tserviceAuthorization: string = 'fixed'\n")]
	public void AValueThatIsALiteralHoldsACallOrAnEqualsSignOrEndsWithACommaIsStillCollected(string source)
	{
		Assert.Equal(1, TableOf(source).MemberRecordCountOf(_MEMBER));
	}

	[Fact]
	public void SeveralMembersOnOneLineReadOnlyTheFirstAndAnEnvironmentReadWithATrailingExpressionIsCollectedAsAnotherForm()
	{
		FileSymbolTable several = TableOf("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER, otherAuthorization: process.env.OTHER_AUTH_HEADER,\n");
		FileSymbolTable trailing = TableOf("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER + suffix,\n");

		Assert.Equal(1, several.MemberRecordCountOf(_MEMBER));
		Assert.Equal(0, several.MemberRecordCountOf("otherAuthorization"));
		Assert.Equal(1, trailing.MemberRecordCountOf(_MEMBER));
		Assert.False(trailing.TryResolveMemberRecordUnambiguously(_MEMBER, out _));
	}

	// ---- The counting rule: any form is collected, only an environment read resolves ----

	[Theory]
	[InlineData("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER,\n\tserviceAuthorization: process.env.OTHER_AUTH_HEADER,\n", 2)]
	[InlineData("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER,\nsettings.serviceAuthorization = 'fixed';\n", 2)]
	[InlineData("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER,\nsettings.serviceAuthorization = loadValue();\n", 2)]
	[InlineData("\tserviceAuthorization: `Bearer ${token}`,\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER,\n", 2)]
	[InlineData("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER,\nthis.serviceAuthorization = other.serviceAuthorization;\n", 2)]
	public void ANameAssignedTwiceInAnyFormIsCountedTwiceAndIsIndeterminate(string source, int count)
	{
		FileSymbolTable table = TableOf(source);

		Assert.Equal(count, table.MemberRecordCountOf(_MEMBER));
		Assert.False(table.TryResolveMemberRecordUnambiguously(_MEMBER, out _));
	}

	[Theory]
	[InlineData("\tserviceAuthorization: 'fixed',\n")]
	[InlineData("settings.serviceAuthorization = loadValue();\n")]
	[InlineData("settings.serviceAuthorization = `Bearer ${token}`;\n")]
	public void ASingleRecordThatIsNotAnEnvironmentReadIsCollectedAndDoesNotResolve(string source)
	{
		FileSymbolTable table = TableOf(source);

		Assert.Equal(1, table.MemberRecordCountOf(_MEMBER));
		Assert.False(table.TryResolveMemberRecordUnambiguously(_MEMBER, out _));
	}

	[Fact]
	public void ANameThatIsNeverAssignedHasNoRecordAndIndeterminates()
	{
		FileSymbolTable table = TableOf("const unrelated = compute();\n");

		Assert.Equal(0, table.MemberRecordCountOf(_MEMBER));
		Assert.False(table.TryResolveMemberRecordUnambiguously(_MEMBER, out _));
		Assert.False(FileSymbolTable.Empty.TryResolveMemberRecordUnambiguously(_MEMBER, out _));
	}

	[Fact]
	public void ResolutionIsOrderFreeSoTheRecordResolvesWhereverItStandsInTheFile()
	{
		const string useSite = "await fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n";
		const string assignment = "const settings = {\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER\n};\n";

		Assert.True(TableOf(assignment + useSite).TryResolveMemberRecordUnambiguously(_MEMBER, out _));
		Assert.True(TableOf(useSite + assignment).TryResolveMemberRecordUnambiguously(_MEMBER, out _));
	}

	[Fact]
	public void TheMemberDictionaryIsKeyedByTheCaseOfTheNameAsTheVariableDictionaryIs()
	{
		FileSymbolTable table = TableOf("\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER,\n");

		Assert.False(table.TryResolveMemberRecordUnambiguously("SERVICEAUTHORIZATION", out _));
	}

	// ---- The variable dictionary is untouched ----

	[Fact]
	public void AMemberLineIsNeverAVariableDeclarationAndAVariableDeclarationIsNeverAMemberRecord()
	{
		const string source = "const authValue = process.env.SERVICE_AUTH_HEADER;\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER,\nsettings.otherAuthorization = process.env.SERVICE_AUTH_HEADER;\n";
		FileSymbolTable table = TableOf(source);

		Assert.Equal(1, table.DeclarationCountOf(_VARIABLE));
		Assert.Equal(0, table.DeclarationCountOf(_MEMBER));
		Assert.Equal(0, table.DeclarationCountOf("otherAuthorization"));
		Assert.False(table.TryResolveRecordUnambiguously(_MEMBER, source.Length, out _));
		Assert.Equal(0, table.MemberRecordCountOf(_VARIABLE));
		Assert.False(table.TryResolveMemberRecordUnambiguously(_VARIABLE, out _));
	}

	[Fact]
	public void AProfileWithNoMemberExpressionCollectsNoMemberRecordAndEveryOtherProfileDeclaresNone()
	{
		LexicalProfile withoutMembers = WithoutTheMemberExpression();
		const string source = "\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER,\n";
		FileSymbolTable table = FileSymbolTable.Collect(withoutMembers, source, new LexicalScanner(withoutMembers).Scan(source));

		Assert.Equal(0, table.MemberRecordCountOf(_MEMBER));
		Assert.NotNull(JavaScriptRegisteredExpressions.Analyzer.LexicalProfile.MemberDeclarationExpression);
		Assert.NotNull(TypeScriptRegisteredExpressions.Analyzer.LexicalProfile.MemberDeclarationExpression);
		Assert.All(
			LanguageCatalogRegistry.BuildEmbeddedCatalog().Analyzers.Where(analyzer => analyzer.LexicalProfile.ProfileName is not (_JAVASCRIPT_PROFILE or _TYPESCRIPT_PROFILE)),
			analyzer => Assert.Null(analyzer.LexicalProfile.MemberDeclarationExpression));
	}

	[Fact]
	public void TheMemberExpressionOfBothProfilesIsAnchoredAtALineStartHasNoLookBehindNoUnboundedQuantifierAndNoLazyLoop()
	{
		foreach (LexicalProfile profile in new[] { JavaScriptRegisteredExpressions.Analyzer.LexicalProfile, TypeScriptRegisteredExpressions.Analyzer.LexicalProfile })
		{
			string pattern = profile.MemberDeclarationExpression!.ToString();

			Assert.StartsWith("^", pattern, StringComparison.Ordinal);
			Assert.True(profile.MemberDeclarationExpression.Options.HasFlag(RegexOptions.Multiline));
			Assert.DoesNotContain("(?<=", pattern, StringComparison.Ordinal);
			Assert.DoesNotContain("(?<!", pattern, StringComparison.Ordinal);
			Assert.DoesNotMatch(@"(?<!\\)(?:[*+]|\{\d+,\})", pattern);
			Assert.DoesNotContain("}?", pattern, StringComparison.Ordinal);
			Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, profile.MemberDeclarationExpression.MatchTimeout);
		}
	}

	[Fact]
	public void TheTypeScriptProfileDeclaresTheMemberExpressionItselfAndTheTwoAnalyzersCarryTheSameOne()
	{
		Assert.Same(JavaScriptRegisteredExpressions.Analyzer.LexicalProfile.MemberDeclarationExpression, TypeScriptRegisteredExpressions.Analyzer.LexicalProfile.MemberDeclarationExpression);
		Assert.Equal(SymbolRecordKind.Member, JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[_SETTINGS_PROPERTY_CLASS].SymbolicVerdictInterpretation.SymbolRecordKind);
		Assert.Equal(SymbolRecordKind.Member, TypeScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[_SETTINGS_PROPERTY_CLASS].SymbolicVerdictInterpretation.SymbolRecordKind);
		Assert.Equal(SymbolRecordKind.Variable, JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[_ENVIRONMENT_CLASS].SymbolicVerdictInterpretation.SymbolRecordKind);
		Assert.Equal(SymbolRecordKind.Variable, JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[_LITERAL_CLASS].SymbolicVerdictInterpretation.SymbolRecordKind);
	}

	// ---- The validator ----

	[Fact]
	public void TheCodesKeepTheirNumbersAndTheShippedAnalyzersAreValid()
	{
		Assert.Equal(_MEMBER_BOUND_CODE, (int)AnalyzerContractViolationCode.MemberDeclarationExpressionMatchTimeoutNotPolicyBound);
		Assert.Equal(_MEMBER_MALFORMED_CODE, (int)AnalyzerContractViolationCode.MemberDeclarationExpressionMalformed);
		Assert.Equal(_MEMBER_KIND_WITHOUT_EXPRESSION_CODE, (int)AnalyzerContractViolationCode.MemberRecordKindWithoutMemberDeclarationExpression);
		Assert.Empty(AnalyzerContractValidator.Validate(JavaScriptRegisteredExpressions.Analyzer));
		Assert.Empty(AnalyzerContractValidator.Validate(TypeScriptRegisteredExpressions.Analyzer));
	}

	[Fact]
	public void AMemberExpressionWithoutTheMatchBoundIsAViolation()
	{
		Regex unbounded = new("^(?<name>[A-Za-z]+):", _OPTIONS, _NO_BACKTRACKING_GUARD_TIMEOUT);

		Assert.Contains(
			AnalyzerContractValidator.Validate(WithMemberExpression(unbounded)),
			violation => violation.Code == AnalyzerContractViolationCode.MemberDeclarationExpressionMatchTimeoutNotPolicyBound);
	}

	[Theory]
	[InlineData("^[A-Za-z]+:")]
	[InlineData("^(?<name>[A-Za-z]+):(?<environment>)")]
	public void AMemberExpressionWithoutTheNameGroupOrWithTheMarkerButNoFallbackGroupIsAViolation(string pattern)
	{
		Regex malformed = new(pattern, _OPTIONS, RecognitionEnginePolicy.MATCH_BOUND);

		Assert.Contains(
			AnalyzerContractValidator.Validate(WithMemberExpression(malformed)),
			violation => violation.Code == AnalyzerContractViolationCode.MemberDeclarationExpressionMalformed);
	}

	[Fact]
	public void AMemberCellInAProfileThatDeclaresNoMemberExpressionIsAViolation()
	{
		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithMemberExpression(null));

		Assert.Contains(violations, violation => violation.Code == AnalyzerContractViolationCode.MemberRecordKindWithoutMemberDeclarationExpression && violation.Subject == _SETTINGS_PROPERTY_CLASS);
	}

	private static FileSymbolTable TableOf(string source)
	{
		LexicalProfile profile = JavaScriptRegisteredExpressions.Analyzer.LexicalProfile;
		ZoneMask mask = new LexicalScanner(profile).Scan(source);

		return FileSymbolTable.Collect(profile, source, mask);
	}

	private static LexicalProfile WithoutTheMemberExpression()
	{
		return ProfileWith(null);
	}

	private static LexicalProfile ProfileWith(Regex? memberExpression)
	{
		LexicalProfile original = JavaScriptRegisteredExpressions.Analyzer.LexicalProfile;

		return new LexicalProfile
		{
			ProfileName = nameof(MemberRecordSymbolTableTests),
			ConstantDeclarationExpression = original.ConstantDeclarationExpression,
			MemberDeclarationExpression = memberExpression,
			LineCommentRules = original.LineCommentRulesInEngineOrder,
			BlockCommentRules = original.BlockCommentRulesInEngineOrder,
			QuoteRules = original.QuoteRulesInEngineOrder
		};
	}

	private static ILanguageAnalyzer WithMemberExpression(Regex? memberExpression)
	{
		ILanguageAnalyzer javaScript = JavaScriptRegisteredExpressions.Analyzer;
		Mock<ILanguageAnalyzer> analyzer = new();

		analyzer.SetupGet(supplied => supplied.LexicalProfile).Returns(ProfileWith(memberExpression));
		analyzer.SetupGet(supplied => supplied.LanguageIdentification).Returns(javaScript.LanguageIdentification);
		analyzer.SetupGet(supplied => supplied.DetectionRulesByPatternClass).Returns(javaScript.DetectionRulesByPatternClass);
		analyzer.SetupGet(supplied => supplied.DemotionSignals).Returns(javaScript.DemotionSignals);
		analyzer.SetupGet(supplied => supplied.CandidateSelectionAndRecommendation).Returns(javaScript.CandidateSelectionAndRecommendation);
		analyzer.SetupGet(supplied => supplied.SubsumptionRelation).Returns(javaScript.SubsumptionRelation);
		analyzer.SetupGet(supplied => supplied.ConstructPrecedence).Returns(javaScript.ConstructPrecedence);

		return analyzer.Object;
	}
}
