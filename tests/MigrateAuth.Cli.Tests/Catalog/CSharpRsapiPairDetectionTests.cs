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
/// THE PAIR OF THE RELATIVITY API SETTINGS, <c>Rsapi&lt;x&gt;UserName</c> AND <c>Rsapi&lt;x&gt;Password</c>, READ BY CS-017 NEXT TO ITS FIRST PAIR.
///
/// Every source below is invented. One fixture per form read (two assignments and one initializer, with the user-name member spelled <c>UserName</c> and <c>Username</c>),
/// one per near-miss that stays quiet, and one per declared variant that is not read. The first pair, <c>.Settings.RelativityUsername</c> with <c>.Settings.RelativityPassword</c>,
/// keeps every form it read.
/// </summary>
public sealed class CSharpRsapiPairDetectionTests
{
	private const int _REPEATED_ENTRY_COUNT = 400;
	private const int _SUMMARY_MATCH_LIMIT = 1_000;
	private const string _CS_017 = "CS-017";
	private const string _PRODUCTION_PATH = "source/Module.cs";
	private const string _TEST_PATH = "source/Module.Tests.cs";
	private const string _SOURCE_FILE_NAME = "Program.cs";
	private const string _TEST_SOURCE_FILE_NAME = "Module.Tests.cs";
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _USER_GROUP = "user";
	private const string _CREDENTIAL_GROUP = "credential";
	private const int _WINDOW_LINE_BREAKS = 5;
	private const int _INITIALIZER_MAXIMUM_DISTANCE = 600;
	private const int _EXTRA_DISTANCE = 10;
	private const int _FIRST_AND_LAST_LINES_OF_THE_PAIR_WINDOW = 1;
	private const int _ONE_CHARACTER_OVER = 1;
	private const int _SOURCE_COLUMN = 1;

	public static TheoryData<string, string> ReadForms()
	{
		return new TheoryData<string, string>
		{
			{ "two assignments, UserName", "settings.RsapiUserName = userName;\nsettings.RsapiPassword = password;" },
			{ "two assignments, Username", "settings.RsapiUsername = userName;\nsettings.RsapiPassword = password;" },
			{ "two assignments with a middle part", "settings.RsapiServiceUsername = userName;\nsettings.RsapiServicePassword = password;" },
			{ "two assignments with a middle part and the other spelling", "settings.RsapiServiceUserName = userName;\nsettings.RsapiServicePassword = password;" },
			{ "two assignments on one line", "settings.RsapiUserName = userName; settings.RsapiPassword = password;" },
			{ "two assignments on this", "this.RsapiUserName = userName;\nthis.RsapiPassword = password;" },
			{ "two assignments on a bare name", "RsapiUserName = userName;\nRsapiPassword = password;" },
			{ "two assignments on a dotted receiver chain", "host.Options.Settings.RsapiUserName = userName;\nhost.Options.Settings.RsapiPassword = password;" },
			{ "two assignments with quoted literals", "settings.RsapiUserName = \"account\";\nsettings.RsapiPassword = \"Example1!\";" },
			{ "two assignments with member access values", "settings.RsapiUserName = options.UserName;\nsettings.RsapiPassword = options.Password;" },
			{ "two assignments with call values", "settings.RsapiUserName = Read(\"user\");\nsettings.RsapiPassword = Read(\"password\");" },
			{ "two assignments with a comment line between", "settings.RsapiUserName = userName;\n// the password follows\nsettings.RsapiPassword = password;" },
			{ "two assignments four lines apart", "settings.RsapiUserName = userName;\nvar a = 1;\nvar b = 2;\nvar c = 3;\nvar d = 4;\nsettings.RsapiPassword = password;" },
			{ "one initializer, UserName", "var settings = new ExampleSettings { RsapiUserName = userName, RsapiPassword = password };" },
			{ "one initializer, Username with a middle part", "var settings = new ExampleSettings { RsapiServiceUsername = userName, RsapiServicePassword = password };" },
			{ "one initializer on several lines", "var settings = new ExampleSettings\n{\n\tRsapiUserName = userName,\n\tRsapiPassword = password\n};" },
			{ "one initializer with other members between", "var settings = new ExampleSettings { RsapiUserName = userName, Endpoint = endpoint, Timeout = 30, RsapiPassword = password };" },
			{ "one initializer with a target-typed creation", "ExampleSettings settings = new() { RsapiUserName = userName, RsapiPassword = password };" },
			{ "one initializer with a fully qualified creation", "var settings = new Example.Services.ExampleSettings { RsapiUsername = userName, RsapiPassword = password };" },
			{ "the first pair, unchanged", "importJob.Settings.RelativityUsername = userName;\nimportJob.Settings.RelativityPassword = password;" }
		};
	}

	public static TheoryData<string, string> NearMisses()
	{
		return new TheoryData<string, string>
		{
			{ "a property declaration", "public string RsapiUserName { get; set; }\npublic string RsapiPassword { get; set; }" },
			{ "a property declaration with an initializer", "public string RsapiUserName { get; set; } = \"a\";\npublic string RsapiPassword { get; set; } = \"b\";" },
			{ "a field declaration with an initializer", "public string RsapiUserName = \"a\";\npublic string RsapiPassword = \"b\";" },
			{ "a nullable declaration", "public string? RsapiUserName = null;\npublic string? RsapiPassword = null;" },
			{ "a generic declaration", "public List<string> RsapiUserName = new();\npublic List<string> RsapiPassword = new();" },
			{ "the user name set while the password is nowhere in the file", "settings.RsapiUserName = userName;" },
			{ "the password set while the user name is nowhere in the file", "settings.RsapiPassword = password;" },
			{ "one half in an initializer with the other half nowhere", "var settings = new ExampleSettings { RsapiUserName = userName, Endpoint = endpoint };" },
			{ "a name that continues after the name", "settings.RsapiUserNameLabel = userName;\nsettings.RsapiPassword = password;" },
			{ "a password name that continues after the name", "settings.RsapiUserName = userName;\nsettings.RsapiPasswordHint = password;" },
			{ "a comparison and not an assignment", "var same = settings.RsapiUserName == userName && settings.RsapiPassword == password;" },
			{ "the property of another settings pair", "settings.ServiceUserName = userName;\nsettings.ServicePassword = password;" }
		};
	}

	public static TheoryData<string, string> DeclaredVariantsThatAreNotRead()
	{
		string sixLinesApart = "settings.RsapiUserName = userName;\nvar a = 1;\nvar b = 2;\nvar c = 3;\nvar d = 4;\nvar e = 5;\nsettings.RsapiPassword = password;";
		string farApart = $"var settings = new ExampleSettings {{ RsapiUserName = userName, Note = \"{new string('x', _INITIALIZER_MAXIMUM_DISTANCE + _EXTRA_DISTANCE)}\", RsapiPassword = password }};";

		return new TheoryData<string, string>
		{
			{ "halves more than five lines apart", sixLinesApart },
			{ "one half in an initializer and the other assigned in a later statement", "var settings = new ExampleSettings { RsapiUserName = userName };\nsettings.RsapiPassword = password;" },
			{ "one half assigned and the other in an initializer after it", "settings.RsapiUserName = userName;\nvar other = new ExampleSettings { RsapiPassword = password };" },
			{ "the password before the user name", "settings.RsapiPassword = password;\nsettings.RsapiUserName = userName;" },
			{ "the password before the user name in an initializer", "var settings = new ExampleSettings { RsapiPassword = password, RsapiUserName = userName };" },
			{ "an initializer with a nested brace between the halves", "var settings = new ExampleSettings { RsapiUserName = userName, Inner = new Part { Size = 1 }, RsapiPassword = password };" },
			{ "an initializer with the halves more than 600 characters apart", farApart },
			{ "different middle parts", "settings.RsapiAUserName = userName;\nsettings.RsapiBPassword = password;" },
			{ "different middle parts in an initializer", "var settings = new ExampleSettings { RsapiAUserName = userName, RsapiBPassword = password };" },
			{ "the same extra prefix before the name in both names", "settings.MyRsapiUserName = userName;\nsettings.MyRsapiPassword = password;" },
			{ "a value outside the shared argument form", "settings.RsapiUserName = first + second;\nsettings.RsapiPassword = password;" },
			{ "a password value that is a calculation", "settings.RsapiUserName = userName;\nsettings.RsapiPassword = password + suffix;" },
			{ "a conditional value", "settings.RsapiUserName = userName;\nsettings.RsapiPassword = useDefault ? first : second;" }
		};
	}

	[Theory]
	[MemberData(nameof(ReadForms))]
	public void AFormTheClassReadsIsOneFindingOfTheClassAtHigh(string form, string source)
	{
		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, _PRODUCTION_PATH);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.True(construct.PatternClassIdentifier == _CS_017, form);
		Assert.True(construct.Level is ConfidenceLevel.High, form);
	}

	[Theory]
	[MemberData(nameof(ReadForms))]
	public void AFormTheClassReadsIsMediumInTestCode(string form, string source)
	{
		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, _TEST_PATH);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.True(construct.PatternClassIdentifier == _CS_017, form);
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
	public void TheTwoAssignmentsAreReadUpToFiveLineBreaksApartAndNoFurther()
	{
		string within = $"settings.RsapiUserName = userName;\n{string.Concat(Enumerable.Repeat("var filler = 1;\n", _WINDOW_LINE_BREAKS - _FIRST_AND_LAST_LINES_OF_THE_PAIR_WINDOW))}settings.RsapiPassword = password;";
		string beyond = $"settings.RsapiUserName = userName;\n{string.Concat(Enumerable.Repeat("var filler = 1;\n", _WINDOW_LINE_BREAKS))}settings.RsapiPassword = password;";

		Assert.Single(new CSharpRecognitionHarness().Recognize(within, _PRODUCTION_PATH));
		Assert.Empty(new CSharpRecognitionHarness().Recognize(beyond, _PRODUCTION_PATH));
	}

	[Fact]
	public void TheInitializerIsReadWhenTheHalvesAreSixHundredCharactersApartAndNotSixHundredAndOne()
	{
		string Initializer(int gap) => $"var settings = new ExampleSettings {{ RsapiUserName = userName,{new string(' ', gap)}RsapiPassword = password }};";

		Assert.Single(new CSharpRecognitionHarness().Recognize(Initializer(_INITIALIZER_MAXIMUM_DISTANCE), _PRODUCTION_PATH));
		Assert.Empty(new CSharpRecognitionHarness().Recognize(Initializer(_INITIALIZER_MAXIMUM_DISTANCE + _ONE_CHARACTER_OVER), _PRODUCTION_PATH));
	}

	[Fact]
	public void ADeclarationWrittenOverTwoLinesIsReadAsThePairWhenItsHalvesAreSetNextToEachOther()
	{
		const string source = "public string\nRsapiUserName = \"a\";\npublic string\nRsapiPassword = \"b\";";

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source, _PRODUCTION_PATH));

		Assert.Equal(_CS_017, construct.PatternClassIdentifier);
	}

	[Fact]
	public void TheAnchorIsTheUserNameMemberNameAndTheCredentialIsThePasswordValue()
	{
		Regex expression = ExpressionOfTheClass();

		Match assignments = expression.Match("settings.RsapiServiceUsername = userName;\nsettings.RsapiServicePassword = options.Password;");
		Match initializer = expression.Match("var settings = new ExampleSettings { RsapiUserName = userName, RsapiPassword = \"Example1!\" };");

		Assert.Equal("RsapiServiceUsername", assignments.Groups[_CONSTRUCT_GROUP].Value);
		Assert.Equal("userName", assignments.Groups[_USER_GROUP].Value);
		Assert.Equal("options.Password", assignments.Groups[_CREDENTIAL_GROUP].Value);
		Assert.Equal("RsapiUserName", initializer.Groups[_CONSTRUCT_GROUP].Value);
		Assert.Equal("\"Example1!\"", initializer.Groups[_CREDENTIAL_GROUP].Value);
	}

	[Fact]
	public void TheFirstPairStillReadsEveryFormItReadAndIsNotReadWithoutItsPrefix()
	{
		Regex expression = ExpressionOfTheClass();

		Assert.True(expression.Match("importJob.Settings.RelativityUsername = userName;\nimportJob.Settings.RelativityPassword = password;").Success);
		Assert.True(expression.Match("job.Settings.RelativityUsername = \"a\";\nvar x = 1;\njob.Settings.RelativityPassword = \"b\";").Success);
		Assert.False(expression.Match("importJob.RelativityUsername = userName;\nimportJob.RelativityPassword = password;").Success);
		Assert.False(expression.Match("importJob.Settings.RelativityUsername = userName;").Success);
	}

	[Fact]
	public void TheAnchorRsapiIsDeclaredAndTheClassKeepsItsBaseAndDestination()
	{
		ILanguageAnalyzer analyzer = new CSharpRecognitionHarness().Analyzer;
		DetectionRule rule = analyzer.DetectionRulesByPatternClass[_CS_017];

		Assert.Contains(analyzer.CandidateSelectionAndRecommendation.PreselectionAnchors, anchor => anchor.Text == "Rsapi");
		Assert.Equal(ConfidenceLevel.High, rule.BaseLevel);
		Assert.Equal(DestinationCertainty.Established, rule.DestinationCertainty);
	}

	[Fact]
	public void ThroughTheRealPipelineAFileWhoseOnlyAnchorIsRsapiIsACandidateAndReportsThePair()
	{
		const string source = "settings.RsapiUsername = userName;\nsettings.RsapiPassword = password;";

		Finding production = Assert.Single(FindingsOf(_SOURCE_FILE_NAME, source));
		Finding test = Assert.Single(FindingsOf(_TEST_SOURCE_FILE_NAME, source));

		Assert.Equal((_CS_017, ConfidenceLevel.High), (production.PatternClass.Value, production.Level));
		Assert.Equal((_CS_017, ConfidenceLevel.Medium), (test.PatternClass.Value, test.Level));
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

	[Fact]
	public void ABraceFreeRepeatedUserNameEntryWithNoPasswordTerminatesWithoutAFinding()
	{
		string source = $"var settings = new ExampleSettings {{ {string.Concat(Enumerable.Repeat("RsapiUserName = x, ", _REPEATED_ENTRY_COUNT))}}};";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source, _PRODUCTION_PATH));
	}

	private static Regex ExpressionOfTheClass()
	{
		return new CSharpRecognitionHarness().Analyzer.DetectionRulesByPatternClass[_CS_017].Expression;
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
