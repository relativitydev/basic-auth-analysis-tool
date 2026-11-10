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
/// A CREDENTIAL STORED IN A MEMBER AND BOUND TO A NON-HTTP SINK THROUGH THAT MEMBER, READ BY CS-019 AT LOW IN PLACE OF THE CS-008 FINDING OF THE CREDENTIAL.
///
/// Every source below is invented. One fixture per form read (the sink created in the binding statement, and a receiver declared between the two links), one per
/// edge of the tempered window of 30 line breaks and 400 characters per line, one per control that goes to an HTTP handler and stays CS-008 at Medium, and one per declared cost
/// that stays CS-008 at Medium. Each source is run through the real selection and recognition, never through a hand-built construct.
/// </summary>
public sealed class CSharpMemberBoundCredentialDetectionTests
{
	private const int _SUMMARY_MATCH_LIMIT = 1_000;
	private const string _CS_008 = "CS-008";
	private const string _CS_019 = "CS-019";
	private const string _PRODUCTION_PATH = "source/Module.cs";
	private const string _TEST_PATH = "source/Module.Tests.cs";
	private const string _SOURCE_FILE_NAME = "Program.cs";
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _CREDENTIAL_GROUP = "credential";
	private const int _WINDOW_LINE_BREAKS = 30;
	private const int _LINE_OF_THE_STORE = 1;
	private const int _ONE_LINE_BEYOND_THE_WINDOW = 1;
	private const int _LAST_LINE_INSIDE_THE_WINDOW = _WINDOW_LINE_BREAKS + _LINE_OF_THE_STORE;
	private const int _FIRST_LINE_OUTSIDE_THE_WINDOW = _LAST_LINE_INSIDE_THE_WINDOW + _ONE_LINE_BEYOND_THE_WINDOW;
	private const int _FILLER_LINES_BEYOND_THE_WINDOW = _WINDOW_LINE_BREAKS + _ONE_LINE_BEYOND_THE_WINDOW;
	private const int _LINES_BEFORE_THE_TARGET = 2;
	private const string _COMMENT_PREFIX = "// ";
	private const int _WINDOW_LINE_LENGTH = 400;
	private const int _ONE_CHARACTER_BEYOND_THE_LIMIT = 1;
	private const int _LINE_LENGTH_JUST_OVER_THE_LIMIT = _WINDOW_LINE_LENGTH + _ONE_CHARACTER_BEYOND_THE_LIMIT;
	private const int _SOURCE_COLUMN = 1;
	private const int _CREDENTIAL_FINDINGS_OF_THE_GATED_CONTROL = 2;
	private const int _SUBSUMPTION_TRIPLE_COUNT = 5;
	private const string _STORE = "settings.MailCredential = new NetworkCredential(userName, password);";
	private const string _FILLER_LINE = "var step = Prepare(value);";
	private const string _NEWLINE = "\n";

	private static readonly ImmutableArray<(string PatternClass, ConfidenceLevel Level)> _sinkAtLow = [(_CS_019, ConfidenceLevel.Low)];
	private static readonly ImmutableArray<(string PatternClass, ConfidenceLevel Level)> _credentialAtMedium = [(_CS_008, ConfidenceLevel.Medium)];

	public static TheoryData<string, string> ReadForms()
	{
		return new TheoryData<string, string>
		{
			{ "the sink in the binding statement, a mail client", Lines(_STORE, "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };") },
			{ "the sink in the binding statement, a proxy", Lines(_STORE, "var proxy = new WebProxy(address) { Credentials = settings.MailCredential };") },
			{ "the sink in the binding statement, written target-typed", Lines(_STORE, "SmtpClient mailer = new(host, port) { Credentials = settings.MailCredential };") },
			{ "the sink in the binding statement, a proxy written target-typed", Lines(_STORE, "WebProxy proxy = new(address) { Credentials = settings.MailCredential };") },
			{ "the sink in the binding statement, with no constructor parentheses", Lines(_STORE, "var mailer = new SmtpClient { Host = host, Credentials = settings.MailCredential };") },
			{ "the sink in the binding statement, with other members before the binding", Lines(_STORE, "var mailer = new SmtpClient(host, port) { EnableSsl = true, Credentials = settings.MailCredential, Timeout = 30 };") },
			{ "the sink in the binding statement, across lines", Lines(_STORE, "var mailer = new SmtpClient(host, port)", "{", "\tCredentials = settings.MailCredential", "};") },
			{ "the sink in the binding statement, written qualified", Lines("settings.MailCredential = new System.Net.NetworkCredential(userName, password);", "var mailer = new System.Net.Mail.SmtpClient(host, port) { Credentials = settings.MailCredential };") },
			{ "the sink in the binding statement, with a member written on this", Lines("this.settings.MailCredential = new NetworkCredential(userName, password);", "var mailer = new SmtpClient(host, port) { Credentials = this.settings.MailCredential };") },
			{ "the sink in the binding statement, on the line of the store", Lines($"{_STORE} var mailer = new SmtpClient(host, port) {{ Credentials = settings.MailCredential }};") },
			{ "the sink in the binding statement, with statements between the links", Lines(_STORE, "var port = 25;", "var host = Resolve(name);", "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };") },
			{ "a receiver declared between the links, a mail client with var", Lines(_STORE, "var mailer = new SmtpClient(host, port);", "mailer.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, an explicit type", Lines(_STORE, "SmtpClient mailer = new SmtpClient(host, port);", "mailer.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, target-typed", Lines(_STORE, "SmtpClient mailer = new(host, port);", "mailer.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, a proxy with var", Lines(_STORE, "var proxy = new WebProxy(address);", "proxy.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, a proxy with an explicit type", Lines(_STORE, "WebProxy proxy = new WebProxy(address);", "proxy.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, a proxy target-typed", Lines(_STORE, "WebProxy proxy = new(address);", "proxy.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, a file-transfer request with a cast", Lines(_STORE, "var request = (FtpWebRequest)WebRequest.Create(uri);", "request.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, a file-transfer request with an explicit type", Lines(_STORE, "FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);", "request.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, a file-transfer request with as", Lines(_STORE, "var request = WebRequest.Create(uri) as FtpWebRequest;", "request.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, a file-transfer request through its own factory", Lines(_STORE, "var request = (FtpWebRequest)FtpWebRequest.Create(uri);", "request.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, a file-transfer request written qualified", Lines(_STORE, "var request = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(uri);", "request.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, in a using header", Lines(_STORE, "using (var mailer = new SmtpClient(host, port))", "{", "\tmailer.Credentials = settings.MailCredential;", "}") },
			{ "a receiver declared between the links, in a using header on one line", Lines(_STORE, "using (var mailer = new SmtpClient(host, port)) { mailer.Credentials = settings.MailCredential; }") },
			{ "a receiver declared between the links, a file-transfer request in a using header", Lines(_STORE, "using (var request = (FtpWebRequest)WebRequest.Create(uri))", "{", "\trequest.Credentials = settings.MailCredential;", "}") },
			{ "a receiver declared between the links, with statements between", Lines(_STORE, "var port = 25;", "var mailer = new SmtpClient(host, port);", "mailer.Timeout = 30;", "mailer.EnableSsl = true;", "mailer.Credentials = settings.MailCredential;") },
			{ "a receiver declared between the links, with a member written on this", Lines("this.settings.MailCredential = new NetworkCredential(userName, password);", "var mailer = new SmtpClient(host, port);", "mailer.Credentials = this.settings.MailCredential;") },
			{ "the member also bound to an HTTP handler elsewhere in the file (declared, the handler binding is not reported)", Lines(_STORE, "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };", "var handler = new HttpClientHandler { Credentials = settings.MailCredential };") }
		};
	}

	public static TheoryData<string, string> ControlsAndDeclaredCostsThatStayAtMedium()
	{
		string secondCredentialBetween = Lines(
			_STORE,
			"other.Credential = new NetworkCredential(otherUser, otherPassword);",
			"var handler = new HttpClientHandler { Credentials = other.Credential };",
			"var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };");

		return new TheoryData<string, string>
		{
			{ "an HTTP handler in an initializer", Lines(_STORE, "var handler = new HttpClientHandler { Credentials = settings.MailCredential };") },
			{ "an HTTP handler by variable", Lines(_STORE, "var handler = new HttpClientHandler();", "handler.Credentials = settings.MailCredential;") },
			{ "an HTTP handler of the other family in an initializer", Lines(_STORE, "var handler = new SocketsHttpHandler { Credentials = settings.MailCredential };") },
			{ "an HTTP handler beyond the window", StoreThenAt(_FIRST_LINE_OUTSIDE_THE_WINDOW, "var handler = new HttpClientHandler { Credentials = settings.MailCredential };") },
			{ "a second credential stored between the links of a chain and bound to an HTTP handler", secondCredentialBetween },
			{ "the sink declared before the credential is stored (declared cost)", Lines("var mailer = new SmtpClient(host, port);", _STORE, "mailer.Credentials = settings.MailCredential;") },
			{ "the binding in the 32nd line after the store, the sink in the binding statement (declared cost)", StoreThenAt(_FIRST_LINE_OUTSIDE_THE_WINDOW, "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };") },
			{ "the receiver declaration beginning on the 32nd line (declared cost)", StoreThenAt(_FIRST_LINE_OUTSIDE_THE_WINDOW, "var mailer = new SmtpClient(host, port);", "mailer.Credentials = settings.MailCredential;") },
			{ "the binding beyond the window of its declaration (declared cost)", Lines(_STORE, "var mailer = new SmtpClient(host, port);", Filler(_FILLER_LINES_BEYOND_THE_WINDOW), "mailer.Credentials = settings.MailCredential;") },
			{ "a line of 401 characters between the links (declared cost)", Lines(_STORE, new string('x', _LINE_LENGTH_JUST_OVER_THE_LIMIT), "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };") },
			{ "a binding after the 400th character of its line (declared cost)", Lines(_STORE, $"{new string(' ', _LINE_LENGTH_JUST_OVER_THE_LIMIT)}var mailer = new SmtpClient(host, port) {{ Credentials = settings.MailCredential }};") },
			{ "a receiver that is a field and not declared between the links (declared cost)", Lines(_STORE, "mailer.Credentials = settings.MailCredential;") },
			{ "a receiver that is a parameter (declared cost)", Lines("void Send(SmtpClient mailer)", "{", $"\t{_STORE}", "\tmailer.Credentials = settings.MailCredential;", "}") },
			{ "a binding through another receiver (declared cost)", Lines(_STORE, "var mailer = new SmtpClient(host, port);", "other.Credentials = settings.MailCredential;") },
			{ "a member written differently at the binding (declared cost)", Lines(_STORE, "var mailer = new SmtpClient(host, port) { Credentials = this.settings.MailCredential };") },
			{ "a member copied through a variable first (declared cost)", Lines(_STORE, "var copy = settings.MailCredential;", "var mailer = new SmtpClient(host, port) { Credentials = copy };") },
			{ "a member that continues with another name at the binding", Lines(_STORE, "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential.Inner };") },
			{ "a member that is a longer name at the binding", Lines(_STORE, "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredentials };") },
			{ "a credential created in an object initializer (declared cost)", Lines("var settings = new ExampleSettings { MailCredential = new NetworkCredential(userName, password) };", "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };") },
			{ "a credential held in a single-name local (declared cost)", Lines("var credential = new NetworkCredential(userName, password);", "var mailer = new SmtpClient(host, port) { Credentials = credential };") },
			{ "a line between the links that holds the text of the credential type, a comment (declared cost)", Lines(_STORE, "// the NetworkCredential is bound below", "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };") },
			{ "a line between the links that holds the text of the credential type, a declaration (declared cost)", Lines(_STORE, "NetworkCredential unused = null;", "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };") },
			{ "a directory searcher is not a sink of this form", Lines(_STORE, "var searcher = new DirectorySearcher(entry);", "searcher.Credentials = settings.MailCredential;") },
			{ "a file-transfer request is not read in the first shape", Lines(_STORE, "var request = new FtpWebRequest { Credentials = settings.MailCredential };") }
		};
	}

	[Theory]
	[MemberData(nameof(ReadForms))]
	public void AFormTheClassReadsIsOneFindingOfTheSinkClassAtLowAndNoCredentialFinding(string form, string source)
	{
		ImmutableArray<(string PatternClass, ConfidenceLevel Level)> outcome = OutcomeOf(source, _PRODUCTION_PATH);

		Assert.True(outcome.SequenceEqual(_sinkAtLow), $"{form}: {string.Join(", ", outcome)}");
	}

	[Theory]
	[MemberData(nameof(ReadForms))]
	public void AFormTheClassReadsStaysAtLowInTestCodeAndNeverReachesHighOrMedium(string form, string source)
	{
		ImmutableArray<(string PatternClass, ConfidenceLevel Level)> outcome = OutcomeOf(source, _TEST_PATH);

		Assert.True(outcome.SequenceEqual(_sinkAtLow), $"{form}: {string.Join(", ", outcome)}");
	}

	[Theory]
	[MemberData(nameof(ControlsAndDeclaredCostsThatStayAtMedium))]
	public void AControlOrADeclaredCostStaysTheCredentialClassAtMedium(string form, string source)
	{
		ImmutableArray<(string PatternClass, ConfidenceLevel Level)> outcome = OutcomeOf(source, _PRODUCTION_PATH);

		Assert.True(outcome.Contains((_CS_008, ConfidenceLevel.Medium)), $"{form}: {string.Join(", ", outcome)}");
		Assert.DoesNotContain(outcome, entry => entry.PatternClass == _CS_019 && entry.Level is not ConfidenceLevel.Low);
	}

	[Theory]
	[MemberData(nameof(ControlsAndDeclaredCostsThatStayAtMedium))]
	public void AControlOrADeclaredCostIsNotReportedAtHighByAnyClass(string form, string source)
	{
		ImmutableArray<(string PatternClass, ConfidenceLevel Level)> outcome = OutcomeOf(source, _PRODUCTION_PATH);

		Assert.True(outcome.All(entry => entry.Level is not ConfidenceLevel.High), form);
	}

	[Fact]
	public void EachHttpHandlerControlIsExactlyOneCredentialFindingAtMedium()
	{
		string[] controls =
		[
			Lines(_STORE, "var handler = new HttpClientHandler { Credentials = settings.MailCredential };"),
			Lines(_STORE, "var handler = new HttpClientHandler();", "handler.Credentials = settings.MailCredential;"),
			StoreThenAt(_FIRST_LINE_OUTSIDE_THE_WINDOW, "var handler = new HttpClientHandler { Credentials = settings.MailCredential };")
		];

		foreach (string control in controls)
		{
			Assert.True(OutcomeOf(control, _PRODUCTION_PATH).SequenceEqual(_credentialAtMedium), control);
		}
	}

	[Fact]
	public void TheSecondCredentialOfTheGatedControlKeepsItsOwnFindingAtMediumWhileTheFirstChainIsNotRead()
	{
		string source = Lines(
			_STORE,
			"other.Credential = new NetworkCredential(otherUser, otherPassword);",
			"var handler = new HttpClientHandler { Credentials = other.Credential };",
			"var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };");

		ImmutableArray<(string PatternClass, ConfidenceLevel Level)> outcome = OutcomeOf(source, _PRODUCTION_PATH);

		Assert.Equal(_CREDENTIAL_FINDINGS_OF_THE_GATED_CONTROL, outcome.Count(entry => entry == (_CS_008, ConfidenceLevel.Medium)));
		Assert.DoesNotContain(outcome, entry => entry.PatternClass == _CS_019);
	}

	[Fact]
	public void AChainThatEndsBeforeTheSecondCredentialIsStoredIsReadAndTheSecondCredentialKeepsItsFinding()
	{
		string source = Lines(
			_STORE,
			"var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };",
			"other.Credential = new NetworkCredential(otherUser, otherPassword);",
			"var handler = new HttpClientHandler { Credentials = other.Credential };");

		ImmutableArray<(string PatternClass, ConfidenceLevel Level)> outcome = OutcomeOf(source, _PRODUCTION_PATH);

		Assert.Equal(new[] { (_CS_008, ConfidenceLevel.Medium), (_CS_019, ConfidenceLevel.Low) }.Order(), outcome.Order());
	}

	[Fact]
	public void TheReceiverDeclarationIsReadWhenItBeginsOnTheThirtyFirstLineAndNotOnTheThirtySecond()
	{
		string read = StoreThenAt(_LAST_LINE_INSIDE_THE_WINDOW, "var mailer = new SmtpClient(host, port);", "mailer.Credentials = settings.MailCredential;");
		string notRead = StoreThenAt(_FIRST_LINE_OUTSIDE_THE_WINDOW, "var mailer = new SmtpClient(host, port);", "mailer.Credentials = settings.MailCredential;");

		Assert.True(OutcomeOf(read, _PRODUCTION_PATH).SequenceEqual(_sinkAtLow));
		Assert.True(OutcomeOf(notRead, _PRODUCTION_PATH).SequenceEqual(_credentialAtMedium));
	}

	[Fact]
	public void TheBindingStatementIsReadWhenItBeginsOnTheThirtyFirstLineAndNotOnTheThirtySecond()
	{
		string read = StoreThenAt(_LAST_LINE_INSIDE_THE_WINDOW, "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };");
		string notRead = StoreThenAt(_FIRST_LINE_OUTSIDE_THE_WINDOW, "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };");

		Assert.True(OutcomeOf(read, _PRODUCTION_PATH).SequenceEqual(_sinkAtLow));
		Assert.True(OutcomeOf(notRead, _PRODUCTION_PATH).SequenceEqual(_credentialAtMedium));
	}

	[Fact]
	public void ALineOfFourHundredCharactersIsCrossedAndALineOfFourHundredAndOneIsNot()
	{
		string binding = "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };";
		string crossed = Lines(_STORE, $"{_COMMENT_PREFIX}{new string('x', _WINDOW_LINE_LENGTH - _COMMENT_PREFIX.Length)}", binding);
		string notCrossed = Lines(_STORE, $"{_COMMENT_PREFIX}{new string('x', _LINE_LENGTH_JUST_OVER_THE_LIMIT - _COMMENT_PREFIX.Length)}", binding);

		Assert.True(OutcomeOf(crossed, _PRODUCTION_PATH).SequenceEqual(_sinkAtLow));
		Assert.True(OutcomeOf(notCrossed, _PRODUCTION_PATH).SequenceEqual(_credentialAtMedium));
	}

	[Fact]
	public void TheSinkIsReadWhenItBeginsAtTheFourHundredthCharacterOfItsLineAndNotAtTheFourHundredAndFirst()
	{
		const string declaration = "var mailer = ";
		const string creation = "new SmtpClient(host, port) { Credentials = settings.MailCredential };";
		string within = Lines(_STORE, $"{new string(' ', _WINDOW_LINE_LENGTH - declaration.Length)}{declaration}{creation}");
		string beyond = Lines(_STORE, $"{new string(' ', _LINE_LENGTH_JUST_OVER_THE_LIMIT - declaration.Length)}{declaration}{creation}");

		Assert.True(OutcomeOf(within, _PRODUCTION_PATH).SequenceEqual(_sinkAtLow));
		Assert.True(OutcomeOf(beyond, _PRODUCTION_PATH).SequenceEqual(_credentialAtMedium));
	}

	[Fact]
	public void ASinkWrittenInsideACommentBetweenTheLinksStillFormsTheChain()
	{
		string source = Lines(_STORE, "// var mailer = new SmtpClient(host, port);", "// mailer.Credentials = settings.MailCredential;");

		ImmutableArray<(string PatternClass, ConfidenceLevel Level)> outcome = OutcomeOf(source, _PRODUCTION_PATH);

		Assert.True(outcome.All(entry => entry.Level is ConfidenceLevel.Low), string.Join(", ", outcome));
	}

	[Fact]
	public void TheAnchorIsTheSinkTypeNameInEachShapeAndTheCredentialIsThePasswordOfTheFirstLink()
	{
		Regex expression = ExpressionOfTheClass();

		Match inBinding = expression.Match(Lines(_STORE, "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };"));
		Match targetTyped = expression.Match(Lines(_STORE, "WebProxy proxy = new(address) { Credentials = settings.MailCredential };"));
		Match declared = expression.Match(Lines(_STORE, "var mailer = new SmtpClient(host, port);", "mailer.Credentials = settings.MailCredential;"));
		Match transfer = expression.Match(Lines(_STORE, "var request = (FtpWebRequest)WebRequest.Create(uri);", "request.Credentials = settings.MailCredential;"));

		Assert.Equal("SmtpClient", inBinding.Groups[_CONSTRUCT_GROUP].Value);
		Assert.Equal("password", inBinding.Groups[_CREDENTIAL_GROUP].Value);
		Assert.Equal("WebProxy", targetTyped.Groups[_CONSTRUCT_GROUP].Value);
		Assert.Equal("SmtpClient", declared.Groups[_CONSTRUCT_GROUP].Value);
		Assert.Equal("FtpWebRequest", transfer.Groups[_CONSTRUCT_GROUP].Value);
	}

	[Fact]
	public void TheExistingTriplesAreUnchangedAndNoTripleWasAdded()
	{
		ILanguageAnalyzer analyzer = new CSharpRecognitionHarness().Analyzer;

		Assert.Equal(_SUBSUMPTION_TRIPLE_COUNT, analyzer.SubsumptionRelation.Count);
		Assert.Contains(analyzer.SubsumptionRelation, triple => triple is { SubsumingPatternClassIdentifier: _CS_019, SubsumedPatternClassIdentifier: _CS_008, When: SubsumptionCondition.MatchSpansOverlap });
	}

	[Fact]
	public void TheFormsTheClassAlreadyReadStayAsTheyWere()
	{
		string[] sources =
		[
			Lines("var mailer = new SmtpClient(host, port);", "mailer.Credentials = new NetworkCredential(userName, password);"),
			"var mailer = new SmtpClient(host, port) { Credentials = new NetworkCredential(userName, password) };",
			Lines("var request = (FtpWebRequest)WebRequest.Create(uri);", "request.Credentials = new NetworkCredential(userName, password);")
		];

		foreach (string source in sources)
		{
			Assert.True(OutcomeOf(source, _PRODUCTION_PATH).SequenceEqual(_sinkAtLow), source);
		}
	}

	[Fact]
	public void ACarriageReturnAndLineFeedTextIsReadAsALineFeedText()
	{
		string source = Lines(_STORE, "var mailer = new SmtpClient(host, port);", "mailer.Credentials = settings.MailCredential;").Replace(_NEWLINE, "\r\n", StringComparison.Ordinal);

		Assert.True(OutcomeOf(source, _PRODUCTION_PATH).SequenceEqual(_sinkAtLow));
	}

	[Fact]
	public void ThroughTheRealPipelineAReadChainIsOneFindingAtLowWithNoFlow()
	{
		string source = Lines(_STORE, "var mailer = new SmtpClient(host, port);", "mailer.Credentials = settings.MailCredential;");

		Finding finding = Assert.Single(FindingsOf(source));

		Assert.Equal(_CS_019, finding.PatternClass.Value);
		Assert.Equal(ConfidenceLevel.Low, finding.Level);
		Assert.Null(finding.Flow);
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfTheExpressionAgreeOverEveryFixtureAndTheStressShapes()
	{
		Regex generated = ExpressionOfTheClass();
		Regex interpreted = new(generated.ToString(), generated.Options, Regex.InfiniteMatchTimeout);
		string[] sources =
		[
			.. ReadForms().Select(row => (string)row[_SOURCE_COLUMN]),
			.. ControlsAndDeclaredCostsThatStayAtMedium().Select(row => (string)row[_SOURCE_COLUMN])
		];

		foreach (string source in sources)
		{
			Assert.True(Summarize(interpreted, source).SequenceEqual(Summarize(generated, source)), source);
		}

		string everything = string.Join(_NEWLINE, sources);

		Assert.True(Summarize(interpreted, everything).SequenceEqual(Summarize(generated, everything)));
		Assert.True(MatchBoundTimingHarness.EnumerationAdvances(generated, RecognitionHarness.Normalize(everything)));
	}

	private static string Lines(params string[] lines)
	{
		return string.Join(_NEWLINE, lines);
	}

	private static string Filler(int count)
	{
		return string.Join(_NEWLINE, Enumerable.Repeat(_FILLER_LINE, count));
	}

	/// <summary>The store on line 1, filler lines up to the line before <paramref name="lineNumber"/>, and the target lines beginning on that line.</summary>
	private static string StoreThenAt(int lineNumber, params string[] targetLines)
	{
		return Lines(_STORE, Filler(lineNumber - _LINES_BEFORE_THE_TARGET), Lines(targetLines));
	}

	private static ImmutableArray<(string PatternClass, ConfidenceLevel Level)> OutcomeOf(string source, string relativePath)
	{
		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, relativePath);

		return [.. recognized.Select(construct => (construct.PatternClassIdentifier, construct.Level)).Order()];
	}

	private static Regex ExpressionOfTheClass()
	{
		return new CSharpRecognitionHarness().Analyzer.DetectionRulesByPatternClass[_CS_019].Expression;
	}

	private static ImmutableArray<Finding> FindingsOf(string source)
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.WriteSourceFile(_SOURCE_FILE_NAME, source);
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
