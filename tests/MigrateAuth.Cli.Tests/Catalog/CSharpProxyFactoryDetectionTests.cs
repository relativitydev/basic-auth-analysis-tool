using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE CLASS CS-020, A CALL OF <c>GetProxy</c> WITH ONE TYPE ARGUMENT AND EXACTLY TWO ARGUMENTS, READ ONLY WHEN A SECOND SIGNAL HOLDS.
///
/// Every source below is invented, written from the catalog's description of the shapes: one fixture per form read, one per near-miss
/// that must stay quiet, and one per declared variant that is not read. The names are generic apart from <c>GetProxy</c>, the one
/// method name the class is about.
/// </summary>
public sealed class CSharpProxyFactoryDetectionTests
{
	private const int _SUMMARY_MATCH_LIMIT = 1_000;
	private const string _CS_020 = "CS-020";
	private const int _FOLLOWING_LINE_COUNT = 40;
	private const int _SOURCE_COLUMN = 1;
	private const int _EXPECTED_PROXY_FACTORY_FINDINGS = 2;
	private const int _EXPECTED_RECOGNIZED_CONSTRUCTS = 2;
	private const string _CS_008 = "CS-008";
	private const string _PRODUCTION_PATH = "source/Module.cs";
	private const string _TEST_PATH = "source/Module.Tests.cs";
	private const string _SOURCE_FILE_NAME = "Program.cs";
	private const string _TEST_SOURCE_FILE_NAME = "Module.Tests.cs";
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _CREDENTIAL_GROUP = "credential";

	public static TheoryData<string, string> ReadForms()
	{
		return new TheoryData<string, string>
		{
			{ "a user name and a password", "var proxy = helper.GetProxy<IExampleService>(userName, password);" },
			{ "a credential-named second argument alone", "var proxy = helper.GetProxy<IExampleService>(endpoint, password);" },
			{ "a user-named first argument alone", "var proxy = helper.GetProxy<IExampleService>(userName, binding);" },
			{ "two ordinary quoted literals", "var proxy = helper.GetProxy<IExampleService>(\"account\", \"Example1!\");" },
			{ "a quoted literal first and a credential-named second", "var proxy = helper.GetProxy<IExampleService>(\"account\", examplePwd);" },
			{ "a user-named first and a quoted literal second", "var proxy = helper.GetProxy<IExampleService>(userName, \"Example1!\");" },
			{ "the second argument named with a trailing capital word", "var proxy = helper.GetProxy<IExampleService>(endpoint, userPass);" },
			{ "the second argument named with a short word", "var proxy = helper.GetProxy<IExampleService>(endpoint, adminPw);" },
			{ "the second argument named with a secret word", "var proxy = helper.GetProxy<IExampleService>(endpoint, clientSecret);" },
			{ "a user-named first argument of another spelling", "var proxy = helper.GetProxy<IExampleService>(loginName, binding);" },
			{ "the same word as a user abbreviation", "var proxy = helper.GetProxy<IExampleService>(usr, binding);" },
			{ "an unrelated method read at High by the first argument (declared cost)", "var proxy = helper.GetProxy<IThing>(userContext, binding);" },
			{ "an unrelated method read at High by two literals (declared cost)", "var proxy = helper.GetProxy<IThing>(\"a\", \"b\");" },
			{ "a first argument that is a user-named call (declared cost)", "var proxy = helper.GetProxy<IThing>(userId.ToString(), options);" },
			{ "a bare call", "var proxy = GetProxy<IExampleService>(userName, password);" },
			{ "a call on this", "var proxy = this.GetProxy<IExampleService>(userName, password);" },
			{ "a call on base", "var proxy = base.GetProxy<IExampleService>(userName, password);" },
			{ "a dotted receiver chain", "var proxy = host.Services.Factory.GetProxy<IExampleService>(userName, password);" },
			{ "a qualified type argument", "var proxy = helper.GetProxy<Example.Services.IExampleService>(userName, password);" },
			{ "a global qualified type argument", "var proxy = helper.GetProxy<global::Example.Services.IExampleService>(userName, password);" },
			{ "a type argument with one nested level", "var proxy = helper.GetProxy<IExampleService<Item>>(userName, password);" },
			{ "a nested level written with its own qualifier", "var proxy = helper.GetProxy<Example.IExampleService<global::Example.Item>>(userName, password);" },
			{ "an explicit type", "IExampleService proxy = helper.GetProxy<IExampleService>(userName, password);" },
			{ "named arguments in order", "var proxy = helper.GetProxy<IExampleService>(userName: user, password: pass);" },
			{ "member access arguments", "var proxy = helper.GetProxy<IExampleService>(options.UserName, options.Password);" },
			{ "call arguments", "var proxy = helper.GetProxy<IExampleService>(Read(\"user\"), Read(\"password\"));" },
			{ "indexer arguments", "var proxy = helper.GetProxy<IExampleService>(settings[\"user\"], settings[\"password\"]);" },
			{ "spaces around the tokens", "var proxy = helper . GetProxy < IExampleService > ( userName , password ) ;" },
			{ "line breaks between the tokens", "var proxy = helper.GetProxy<IExampleService>(\n\tuserName,\n\tpassword\n);" },
			{ "line breaks before the type argument and the parenthesis", "var proxy = helper.GetProxy\n\t<IExampleService>\n\t(userName, password);" },
			{ "a line comment between the arguments", "var proxy = helper.GetProxy<IExampleService>(userName, // the account\n\tpassword);" },
			{ "block comments between the tokens", "var proxy = helper.GetProxy<IExampleService>(/* account */ userName /* and */ , /* secret */ password /* end */);" }
		};
	}

	public static TheoryData<string, string> NearMisses()
	{
		return new TheoryData<string, string>
		{
			{ "an address and a binding", "var proxy = helper.GetProxy<IExampleService>(address, binding);" },
			{ "an address and a token", "var proxy = helper.GetProxy<IExampleService>(address, token);" },
			{ "an identity and an id", "var proxy = helper.GetProxy<IExampleService>(identity, id);" },
			{ "a token-named second with a user-named first", "var proxy = helper.GetProxy<IExampleService>(userName, accessToken);" },
			{ "a key-named second with a user-named first", "var proxy = helper.GetProxy<IExampleService>(userName, apiKey);" },
			{ "an underscored key-named second with a user-named first", "var proxy = helper.GetProxy<IExampleService>(userName, api_key);" },
			{ "a bearer-named second with a user-named first", "var proxy = helper.GetProxy<IExampleService>(userName, bearerValue);" },
			{ "a second argument that names both a token and a password", "var proxy = helper.GetProxy<IExampleService>(userName, tokenPassword);" },
			{ "a token-named second with a credential-named word", "var proxy = helper.GetProxy<IExampleService>(endpoint, tokenSecret);" },
			{ "a token-named literal pair", "var proxy = helper.GetProxy<IExampleService>(\"account\", \"example-token\");" },
			{ "a user id and a tenant name", "var proxy = helper.GetProxy<IExampleService>(userId, tenantName);" },
			{ "a user ID and a tenant name", "var proxy = helper.GetProxy<IExampleService>(userID, tenantName);" },
			{ "a user key and a tenant name", "var proxy = helper.GetProxy<IExampleService>(userKey, tenantName);" },
			{ "a user guid and a tenant name", "var proxy = helper.GetProxy<IExampleService>(userGuid, tenantName);" },
			{ "a one-argument call that carries an id", "var proxy = helper.GetProxy<IExampleService>(tenantId);" },
			{ "a word that contains a short credential word", "var proxy = helper.GetProxy<IExampleService>(endpoint, bypassFlag);" },
			{ "a word that contains pass inside", "var proxy = helper.GetProxy<IExampleService>(endpoint, compass);" },
			{ "a word that begins with pass", "var proxy = helper.GetProxy<IExampleService>(endpoint, passport);" },
			{ "a declaration", "public T GetProxy<T>(string userName, string password)\n{\n\treturn factory.Create<T>();\n}" },
			{ "a method named with a longer name", "var proxy = helper.GetProxyAsync<IExampleService>(userName, password);" },
			{ "a method whose name ends with the name", "var proxy = helper.MyGetProxy<IExampleService>(userName, password);" },
			{ "a literal second with a first that is neither a literal nor user-named", "var proxy = helper.GetProxy<IExampleService>(account, \"Example1!\");" },
			{ "two arguments with no sign of a user or a password", "var proxy = helper.GetProxy<IExampleService>(first, second);" }
		};
	}

	public static TheoryData<string, string> DeclaredVariantsThatAreNotRead()
	{
		return new TheoryData<string, string>
		{
			{ "a call with no type argument", "var proxy = helper.GetProxy(userName, password);" },
			{ "a one-argument call that carries a credential object", "var proxy = helper.GetProxy<IExampleService>(credentials);" },
			{ "a call with three arguments", "var proxy = helper.GetProxy<IExampleService>(userName, password, endpoint);" },
			{ "named arguments in reverse order", "var proxy = helper.GetProxy<IExampleService>(password: pass, userName: user);" },
			{ "an argument that is a concatenation", "var proxy = helper.GetProxy<IExampleService>(userName, password + suffix);" },
			{ "an argument that is a construction", "var proxy = helper.GetProxy<IExampleService>(userName, new Secret(password));" },
			{ "an argument that is a call with a comma", "var proxy = helper.GetProxy<IExampleService>(userName, Read(password, 1));" },
			{ "an argument that is a call followed by a member", "var proxy = helper.GetProxy<IExampleService>(userName, Read(password).Value);" },
			{ "two type arguments", "var proxy = helper.GetProxy<IExampleService, IOther>(userName, password);" },
			{ "an array type argument", "var proxy = helper.GetProxy<IExampleService[]>(userName, password);" },
			{ "a nullable type argument", "var proxy = helper.GetProxy<IExampleService?>(userName, password);" },
			{ "a type argument nested twice", "var proxy = helper.GetProxy<IExampleService<Item<Part>>>(userName, password);" }
		};
	}

	[Theory]
	[MemberData(nameof(ReadForms))]
	public void AFormTheClassReadsIsOneFindingOfTheClassAtHigh(string form, string source)
	{
		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, _PRODUCTION_PATH);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.True(construct.PatternClassIdentifier == _CS_020, form);
		Assert.True(construct.Level is ConfidenceLevel.High, form);
	}

	[Theory]
	[MemberData(nameof(ReadForms))]
	public void AFormTheClassReadsIsMediumInTestCode(string form, string source)
	{
		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, _TEST_PATH);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.True(construct.PatternClassIdentifier == _CS_020, form);
		Assert.True(construct.Level is ConfidenceLevel.Medium, form);
	}

	[Theory]
	[MemberData(nameof(NearMisses))]
	public void ANearMissIsReportedByNoClass(string form, string source)
	{
		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, _PRODUCTION_PATH);

		Assert.True(recognized.IsEmpty, form);
	}

	[Theory]
	[MemberData(nameof(DeclaredVariantsThatAreNotRead))]
	public void ADeclaredVariantThatMayPerformBasicIsNotRead(string form, string source)
	{
		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, _PRODUCTION_PATH);

		Assert.True(recognized.IsEmpty, form);
	}

	[Fact]
	public void TheAnchorIsTheMethodNameAndTheCredentialIsTheSecondArgumentValueWithoutItsLabel()
	{
		Regex expression = ExpressionOfTheClass();

		Match plain = expression.Match("var proxy = helper.GetProxy<IExampleService>(userName, password);");
		Match labelled = expression.Match("var proxy = helper.GetProxy<IExampleService>(userName: user, password: examplePassword.Value);");

		Assert.Equal("GetProxy", plain.Groups[_CONSTRUCT_GROUP].Value);
		Assert.Equal("password", plain.Groups[_CREDENTIAL_GROUP].Value);
		Assert.Equal("GetProxy", labelled.Groups[_CONSTRUCT_GROUP].Value);
		Assert.Equal("examplePassword.Value", labelled.Groups[_CREDENTIAL_GROUP].Value);
	}

	[Fact]
	public void TheClassIsHighWithAnUnknownDestinationAndNoTripleWasAdded()
	{
		ILanguageAnalyzer analyzer = new CSharpRecognitionHarness().Analyzer;
		DetectionRule rule = analyzer.DetectionRulesByPatternClass[_CS_020];

		Assert.Equal(ConfidenceLevel.High, rule.BaseLevel);
		Assert.Equal(DestinationCertainty.Unknown, rule.DestinationCertainty);
		Assert.Equal("SdkProxyFactoryWithUserAndPassword", rule.DisplayName);
		Assert.DoesNotContain(analyzer.SubsumptionRelation, triple => triple.SubsumingPatternClassIdentifier == _CS_020 || triple.SubsumedPatternClassIdentifier == _CS_020);
	}

	[Fact]
	public void TheClassHasAnAnchorOfItsOwnAndTheFileOfOnlyThatAnchorIsACandidate()
	{
		ILanguageAnalyzer analyzer = new CSharpRecognitionHarness().Analyzer;

		Assert.Contains(analyzer.CandidateSelectionAndRecommendation.PreselectionAnchors, anchor => anchor.Text == "GetProxy");
	}

	[Fact]
	public void ACredentialWordInsideATrailingCommentIsNotPartOfTheArgument()
	{
		const string source = "var proxy = helper.GetProxy<IExampleService>(address /* password */, binding);";

		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, _PRODUCTION_PATH);

		Assert.True(recognized.IsEmpty);
	}

	[Fact]
	public void AnUnterminatedBlockCommentInTheCallEndsTheMatchWithoutAFinding()
	{
		string source = $"var proxy = helper.GetProxy<IExampleService>(userName, /* never closed\n{string.Concat(Enumerable.Repeat("var other = Build(first, second);\n", _FOLLOWING_LINE_COUNT))}";

		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, _PRODUCTION_PATH);

		Assert.True(recognized.IsEmpty);
	}

	[Fact]
	public void TheReadCallAndItsNeighbourCallsAreReportedOncePerCallWhenSeveralStandInOneFile()
	{
		string source = string.Join(
			"\n",
			"var first = helper.GetProxy<IExampleService>(userName, password);",
			"var second = helper.GetProxy<IExampleService>(address, binding);",
			"var third = helper.GetProxy<IOtherService>(\"account\", \"Example1!\");");

		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, _PRODUCTION_PATH);

		Assert.Equal(_EXPECTED_PROXY_FACTORY_FINDINGS, recognized.Count(construct => construct.PatternClassIdentifier == _CS_020));
		Assert.Equal(_EXPECTED_RECOGNIZED_CONSTRUCTS, recognized.Length);
	}

	[Fact]
	public void ThroughTheRealPipelineACallInProductionCodeIsOneHighFindingAndOneInTestCodeIsMedium()
	{
		const string source = "var proxy = helper.GetProxy<IExampleService>(userName, password);";

		Finding production = Assert.Single(FindingsOf(_SOURCE_FILE_NAME, source));
		Finding test = Assert.Single(FindingsOf(_TEST_SOURCE_FILE_NAME, source));

		Assert.Equal(_CS_020, production.PatternClass.Value);
		Assert.Equal(ConfidenceLevel.High, production.Level);
		Assert.Equal(_CS_020, test.PatternClass.Value);
		Assert.Equal(ConfidenceLevel.Medium, test.Level);
	}

	[Fact]
	public void ThroughTheRealPipelineAFileWhoseOnlyAnchorIsTheMethodNameIsACandidateAndReportsTheCall()
	{
		const string source = "var proxy = helper.GetProxy<IExampleService>(endpoint, secretText);";

		Finding finding = Assert.Single(FindingsOf(_SOURCE_FILE_NAME, source));

		Assert.Equal(_CS_020, finding.PatternClass.Value);
		Assert.Equal(ConfidenceLevel.High, finding.Level);
	}

	[Fact]
	public void ACallOfTheClassIsNotAlsoReportedByTheNetworkCredentialClass()
	{
		const string source = "var proxy = helper.GetProxy<IExampleService>(userName, password);";

		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, _PRODUCTION_PATH);

		Assert.DoesNotContain(recognized, construct => construct.PatternClassIdentifier == _CS_008);
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfTheExpressionAgreeOverEveryFixture()
	{
		Regex generated = ExpressionOfTheClass();
		Regex interpreted = new(generated.ToString(), generated.Options, Regex.InfiniteMatchTimeout);
		string[] sources =
		[
			.. ReadForms().Select(row => (string)row[_SOURCE_COLUMN]),
			.. NearMisses().Select(row => (string)row[_SOURCE_COLUMN]),
			.. DeclaredVariantsThatAreNotRead().Select(row => (string)row[_SOURCE_COLUMN])
		];

		foreach (string source in sources)
		{
			Assert.True(Summarize(interpreted, source).SequenceEqual(Summarize(generated, source)), source);
		}

		string everything = string.Join("\n", sources);

		Assert.True(Summarize(interpreted, everything).SequenceEqual(Summarize(generated, everything)));
	}

	private static Regex ExpressionOfTheClass()
	{
		return new CSharpRecognitionHarness().Analyzer.DetectionRulesByPatternClass[_CS_020].Expression;
	}

	private static ImmutableArray<Finding> FindingsOf(string fileName, string source)
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.WriteSourceFile(fileName, source);
		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		return harness.State.Findings;
	}

	private static ImmutableArray<string> Summarize(Regex expression, string text)
	{
		List<string> summary = [];
		Match match = expression.Match(RecognitionHarness.Normalize(text));

		while (match.Success && summary.Count < _SUMMARY_MATCH_LIMIT)
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
