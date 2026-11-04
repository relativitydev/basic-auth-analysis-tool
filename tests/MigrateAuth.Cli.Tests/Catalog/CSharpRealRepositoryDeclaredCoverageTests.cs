using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Release;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE DECLARED COVERAGE OF CS-020, OF THE SECOND PAIR OF CS-017 AND OF THE MEMBER-BOUND CREDENTIAL OF CS-019: THE TWENTY-TWO FORMS <c>O5-40</c> TO <c>O5-61</c>,
/// EACH WITH ITS GROUP AND ITS OUTCOME, AND THE THREE CLIENT-FACING ENTRIES THE ANALYZER CARRIES FOR THEM.
///
/// The twenty-two forms are the declared forms of the catalog. A form of the group <c>unsupported</c> fails by not being read: the finding the catalog does not
/// promise is absent. A form of the group <c>cost</c> fails by being read into a finding that is wrong for it: a Medium credential finding that does not go to the migrating host, or a High
/// finding of a method that is not the helper's. Every source below is invented; the one method name is <c>GetProxy</c> and the one member vocabulary is <c>Rsapi</c>.
/// </summary>
public sealed class CSharpRealRepositoryDeclaredCoverageTests
{
	private const string _CS_008 = "CS-008";
	private const string _CS_017 = "CS-017";
	private const string _CS_019 = "CS-019";
	private const string _CS_020 = "CS-020";
	private const string _PRODUCTION_PATH = "source/Module.cs";
	private const string _UNSUPPORTED_GROUP = "unsupported";
	private const string _COST_GROUP = "cost";
	private const int _FIRST_NUMBER = 40;
	private const int _LAST_NUMBER = 61;
	private const int _UNSUPPORTED_COUNT = 12;
	private const int _COST_COUNT = 10;
	private const int _DECLARED_ENTRY_COUNT_OF_THE_ANALYZER = 30;
	private const int _WINDOW_LINE_BREAKS = 30;
	private const int _WINDOW_LINE_LENGTH = 400;
	private const int _INITIALIZER_MAXIMUM_DISTANCE = 600;
	private const int _EXTRA_DISTANCE = 10;
	private const int _NUMBER_INCLUSIVE_BOUND = 1;
	private const int _NUMBER_COUNT = _LAST_NUMBER - _FIRST_NUMBER + _NUMBER_INCLUSIVE_BOUND;
	private const int _LINES_BEFORE_THE_TARGET = 2;
	private const int _LINE_OF_THE_STORE = 1;
	private const int _ONE_LINE_BEYOND_THE_WINDOW = 1;
	private const int _ONE_CHARACTER_BEYOND_THE_LIMIT = 1;
	private const int _FIRST_LINE_OUTSIDE_THE_WINDOW = _WINDOW_LINE_BREAKS + _LINE_OF_THE_STORE + _ONE_LINE_BEYOND_THE_WINDOW;
	private const int _LINE_LENGTH_JUST_OVER_THE_LIMIT = _WINDOW_LINE_LENGTH + _ONE_CHARACTER_BEYOND_THE_LIMIT;
	private const int _FILLER_LINES_BEYOND_THE_PAIR_WINDOW = 6;
	private const string _STORE = "settings.MailCredential = new NetworkCredential(userName, password);";
	private const string _FILLER_LINE = "var step = Prepare(value);";
	private const string _NEWLINE = "\n";
	private const string _PROXY_ENTRY_KEYWORD = "GetProxy";
	private const string _PAIR_ENTRY_KEYWORD = "Rsapi";
	private const string _STORED_ENTRY_KEYWORD = "NetworkCredential";
	private const string _WRAPPER_ENTRY_PREFIX = "a project's own custom authentication helper or wrapper";
	private const string _RETIRED_CLIENT_ENTRY_PREFIX = "the retired RSAPI client obtained through a generic factory method";
	private const string _WRAPPER_NARROWING = "is also read in the forms described in the entry for settings objects with the Rsapi pair";
	private const string _RETIRED_CLIENT_NARROWING = "is read in the forms described in the entry for calls to GetProxy";

	/// <summary>What a source of a declared form produces, observed through the real selection and recognition.</summary>
	public enum DeclaredOutcome
	{
		/// <summary>No finding at all: the form is not read.</summary>
		NotReported,

		/// <summary>One finding of the sink class at Low and no credential finding: the form is read by the sink class.</summary>
		SinkAtLow,

		/// <summary>The credential class at Medium and the sink class at no level above Low: the form is read as an ordinary network credential.</summary>
		CredentialAtMedium,

		/// <summary>Every finding is at Low, and there is at least one: a sink or a binding written in a comment still forms the chain.</summary>
		OnlyLowFindings,

		/// <summary>One finding of the proxy factory class at High: a call of an unrelated method read as the helper's.</summary>
		ProxyFactoryAtHigh,

		/// <summary>One finding of the settings pair class at High: a declaration over two lines read as the pair.</summary>
		SettingsPairAtHigh
	}

	/// <summary>One declared form: its group and number, the words that only its entry carries, the sources that show it and what they produce.</summary>
	public sealed record DeclaredForm(string Group, int Number, string[] Sources, DeclaredOutcome Outcome)
	{
		/// <summary>The identifier the catalog gives the entry, group prefix included.</summary>
		public string Id => $"{Group}:O5-{Number}";
	}

	private static readonly string _longInitializerGap = new('x', _INITIALIZER_MAXIMUM_DISTANCE + _EXTRA_DISTANCE);
	private static readonly ImmutableArray<(string PatternClass, ConfidenceLevel Level)> _sinkAtLow = [(_CS_019, ConfidenceLevel.Low)];
	private static readonly ImmutableArray<(string PatternClass, ConfidenceLevel Level)> _proxyFactoryAtHigh = [(_CS_020, ConfidenceLevel.High)];
	private static readonly ImmutableArray<(string PatternClass, ConfidenceLevel Level)> _settingsPairAtHigh = [(_CS_017, ConfidenceLevel.High)];

	/// <summary>The twenty-two entries, in the order of the catalog.</summary>
	private static readonly ImmutableArray<DeclaredForm> _declaredForms =
	[
		new(_UNSUPPORTED_GROUP, 40, ["var proxy = helper.GetProxy<IExampleService>(first, second);", "var proxy = helper.GetProxy<IExampleService>(account, \"Example1!\");", "var proxy = helper.GetProxy<IExampleService>(userName, tokenPassword);"], DeclaredOutcome.NotReported),
		new(_UNSUPPORTED_GROUP, 41, ["var proxy = helper.GetProxy(userName, password);", "var proxy = helper.GetProxy<IExampleService>(credentials);", "var proxy = helper.GetProxy<IExampleService>(userName, password, endpoint);"], DeclaredOutcome.NotReported),
		new(_UNSUPPORTED_GROUP, 42, ["var proxy = helper.GetProxy<IExampleService>(password: pass, userName: user);"], DeclaredOutcome.NotReported),
		new(_UNSUPPORTED_GROUP, 43, ["var proxy = helper.GetProxy<IExampleService>(userName, Read(password).Value);", "var proxy = helper.GetProxy<IExampleService>(userName, password + suffix);", "var proxy = helper.GetProxy<IExampleService>(userName, new Secret(password));", "settings.RsapiUserName = first + second;\nsettings.RsapiPassword = password;"], DeclaredOutcome.NotReported),
		new(_UNSUPPORTED_GROUP, 44, ["var proxy = helper.GetProxy<IExampleService, IOther>(userName, password);", "var proxy = helper.GetProxy<IExampleService[]>(userName, password);", "var proxy = helper.GetProxy<IExampleService?>(userName, password);", "var proxy = helper.GetProxy<IExampleService<Item<Part>>>(userName, password);"], DeclaredOutcome.NotReported),
		new(_UNSUPPORTED_GROUP, 45, [Lines("settings.RsapiUserName = userName;", Lines(Enumerable.Repeat("var filler = 1;", _FILLER_LINES_BEYOND_THE_PAIR_WINDOW).ToArray()), "settings.RsapiPassword = password;")], DeclaredOutcome.NotReported),
		new(_UNSUPPORTED_GROUP, 46, ["var settings = new ExampleSettings { RsapiUserName = userName };\nsettings.RsapiPassword = password;"], DeclaredOutcome.NotReported),
		new(_UNSUPPORTED_GROUP, 47, ["settings.RsapiPassword = password;\nsettings.RsapiUserName = userName;", "var settings = new ExampleSettings { RsapiPassword = password, RsapiUserName = userName };"], DeclaredOutcome.NotReported),
		new(_UNSUPPORTED_GROUP, 48, ["var settings = new ExampleSettings { RsapiUserName = userName, Inner = new Part { Size = 1 }, RsapiPassword = password };", $"var settings = new ExampleSettings {{ RsapiUserName = userName, Note = \"{_longInitializerGap}\", RsapiPassword = password }};"], DeclaredOutcome.NotReported),
		new(_UNSUPPORTED_GROUP, 49, ["settings.RsapiAUserName = userName;\nsettings.RsapiBPassword = password;", "var settings = new ExampleSettings { RsapiAUserName = userName, RsapiBPassword = password };"], DeclaredOutcome.NotReported),
		new(_COST_GROUP, 50, [Lines("var mailer = new SmtpClient(host, port);", _STORE, "mailer.Credentials = settings.MailCredential;")], DeclaredOutcome.CredentialAtMedium),
		new(_COST_GROUP, 51, [StoreThenAt(_FIRST_LINE_OUTSIDE_THE_WINDOW, "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };"), Lines(_STORE, new string('x', _LINE_LENGTH_JUST_OVER_THE_LIMIT), "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };")], DeclaredOutcome.CredentialAtMedium),
		new(_COST_GROUP, 52, [Lines(_STORE, "mailer.Credentials = settings.MailCredential;"), Lines("void Send(SmtpClient mailer)", "{", $"\t{_STORE}", "\tmailer.Credentials = settings.MailCredential;", "}")], DeclaredOutcome.CredentialAtMedium),
		new(_COST_GROUP, 53, [Lines(_STORE, "var mailer = new SmtpClient(host, port) { Credentials = this.settings.MailCredential };"), Lines(_STORE, "var copy = settings.MailCredential;", "var mailer = new SmtpClient(host, port) { Credentials = copy };")], DeclaredOutcome.CredentialAtMedium),
		new(_COST_GROUP, 54, [Lines("var settings = new ExampleSettings { MailCredential = new NetworkCredential(userName, password) };", "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };")], DeclaredOutcome.CredentialAtMedium),
		new(_COST_GROUP, 55, [Lines("var credential = new NetworkCredential(userName, password);", "var mailer = new SmtpClient(host, port) { Credentials = credential };")], DeclaredOutcome.CredentialAtMedium),
		new(_COST_GROUP, 56, [Lines(_STORE, "// the NetworkCredential is bound below", "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };"), Lines(_STORE, "NetworkCredential unused = null;", "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };")], DeclaredOutcome.CredentialAtMedium),
		new(_COST_GROUP, 57, [Lines(_STORE, "// var mailer = new SmtpClient(host, port);", "// mailer.Credentials = settings.MailCredential;")], DeclaredOutcome.OnlyLowFindings),
		new(_UNSUPPORTED_GROUP, 58, [Lines(_STORE, "var mailer = new SmtpClient(host, port) { Credentials = settings.MailCredential };", "var handler = new HttpClientHandler { Credentials = settings.MailCredential };")], DeclaredOutcome.SinkAtLow),
		new(_COST_GROUP, 59, ["var proxy = helper.GetProxy<IThing>(userContext, binding);", "var proxy = helper.GetProxy<IThing>(\"a\", \"b\");", "var proxy = helper.GetProxy<IThing>(userId.ToString(), options);"], DeclaredOutcome.ProxyFactoryAtHigh),
		new(_COST_GROUP, 60, ["public string\nRsapiUserName = \"a\";\npublic string\nRsapiPassword = \"b\";"], DeclaredOutcome.SettingsPairAtHigh),
		new(_UNSUPPORTED_GROUP, 61, ["settings.MyRsapiUserName = userName;\nsettings.MyRsapiPassword = password;"], DeclaredOutcome.NotReported)
	];

	public static TheoryData<string> DeclaredFormIds()
	{
		TheoryData<string> ids = new();

		foreach (DeclaredForm form in _declaredForms)
		{
			ids.Add(form.Id);
		}

		return ids;
	}

	[Fact]
	public void EveryNumberFromFortyToSixtyOneIsDeclaredOnceAndTheGroupsHoldTwelveUnsupportedAndTenCostEntries()
	{
		Assert.Equal(Enumerable.Range(_FIRST_NUMBER, _NUMBER_COUNT), _declaredForms.Select(form => form.Number));
		Assert.Equal(_UNSUPPORTED_COUNT, _declaredForms.Count(form => form.Group == _UNSUPPORTED_GROUP));
		Assert.Equal(_COST_COUNT, _declaredForms.Count(form => form.Group == _COST_GROUP));
	}

	[Theory]
	[MemberData(nameof(DeclaredFormIds))]
	public void EveryDeclaredFormHasTheOutcomeItsGroupStates(string id)
	{
		DeclaredForm form = _declaredForms.Single(candidate => candidate.Id == id);

		Assert.All(form.Sources, source => AssertOutcome(form, source));
	}

	[Theory]
	[InlineData("O5-43", _UNSUPPORTED_GROUP, DeclaredOutcome.NotReported)]
	[InlineData("O5-48", _UNSUPPORTED_GROUP, DeclaredOutcome.NotReported)]
	[InlineData("O5-49", _UNSUPPORTED_GROUP, DeclaredOutcome.NotReported)]
	[InlineData("O5-50", _COST_GROUP, DeclaredOutcome.CredentialAtMedium)]
	[InlineData("O5-51", _COST_GROUP, DeclaredOutcome.CredentialAtMedium)]
	[InlineData("O5-52", _COST_GROUP, DeclaredOutcome.CredentialAtMedium)]
	[InlineData("O5-53", _COST_GROUP, DeclaredOutcome.CredentialAtMedium)]
	[InlineData("O5-54", _COST_GROUP, DeclaredOutcome.CredentialAtMedium)]
	[InlineData("O5-55", _COST_GROUP, DeclaredOutcome.CredentialAtMedium)]
	[InlineData("O5-56", _COST_GROUP, DeclaredOutcome.CredentialAtMedium)]
	[InlineData("O5-57", _COST_GROUP, DeclaredOutcome.OnlyLowFindings)]
	[InlineData("O5-58", _UNSUPPORTED_GROUP, DeclaredOutcome.SinkAtLow)]
	[InlineData("O5-59", _COST_GROUP, DeclaredOutcome.ProxyFactoryAtHigh)]
	[InlineData("O5-60", _COST_GROUP, DeclaredOutcome.SettingsPairAtHigh)]
	public void TheEntryNamedByItsIdIsInItsGroupAndItsFormProducesTheOutcomeTheCatalogStatesForIt(string entryId, string expectedGroup, DeclaredOutcome expectedOutcome)
	{
		DeclaredForm form = _declaredForms.Single(candidate => candidate.Id == $"{expectedGroup}:{entryId}");

		Assert.Equal(expectedGroup, form.Group);
		Assert.Equal(expectedOutcome, form.Outcome);
		Assert.NotEmpty(form.Sources);
		Assert.All(form.Sources, source => AssertOutcome(form, source));
	}

	[Fact]
	public void TheAnalyzerDeclaresTheThreeClientFacingEntriesWithNoInternalTermAndTheTwoChangedReasons()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> declared = [.. CSharpRenderedTexts.Analyzer().LanguageIdentification.DeclaredUnsupportedCoverage];
		DeclaredUnsupportedCoverage[] corrections =
		[
			EntryWhoseConstructContains(declared, _PROXY_ENTRY_KEYWORD),
			EntryWhoseConstructContains(declared, _PAIR_ENTRY_KEYWORD),
			EntryWhoseConstructContains(declared, _STORED_ENTRY_KEYWORD)
		];

		Assert.Equal(_DECLARED_ENTRY_COUNT_OF_THE_ANALYZER, declared.Length);
		Assert.Equal(declared.Length, declared.Select(entry => entry.Construct).Distinct(StringComparer.Ordinal).Count());
		Assert.All(corrections, entry => Assert.Empty(InternalTermsIn($"{entry.Construct} {entry.Reason}")));
		Assert.All(corrections, entry => Assert.DoesNotContain("catalog", $"{entry.Construct} {entry.Reason}", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(_WRAPPER_NARROWING, declared.Single(entry => entry.Construct.StartsWith(_WRAPPER_ENTRY_PREFIX, StringComparison.Ordinal)).Reason, StringComparison.Ordinal);
		Assert.Contains(_RETIRED_CLIENT_NARROWING, declared.Single(entry => entry.Construct.StartsWith(_RETIRED_CLIENT_ENTRY_PREFIX, StringComparison.Ordinal)).Reason, StringComparison.Ordinal);
	}

	[Fact]
	public void TheThirtyLineWindowAndTheFourHundredCharacterLineAreNamedInTheStoredCredentialEntry()
	{
		DeclaredUnsupportedCoverage stored = EntryWhoseConstructContains([.. CSharpRenderedTexts.Analyzer().LanguageIdentification.DeclaredUnsupportedCoverage], _STORED_ENTRY_KEYWORD);

		Assert.Contains($"{_WINDOW_LINE_BREAKS} lines", stored.Construct, StringComparison.Ordinal);
		Assert.Contains($"{_WINDOW_LINE_LENGTH} characters", stored.Construct, StringComparison.Ordinal);
	}

	private static void AssertOutcome(DeclaredForm form, string source)
	{
		ImmutableArray<(string PatternClass, ConfidenceLevel Level)> outcome = OutcomeOf(source);
		string description = $"{form.Id}: {string.Join(", ", outcome)} for {source}";

		switch (form.Outcome)
		{
			case DeclaredOutcome.NotReported:
				Assert.True(outcome.IsEmpty, description);
				break;
			case DeclaredOutcome.SinkAtLow:
				Assert.True(outcome.SequenceEqual(_sinkAtLow), description);
				break;
			case DeclaredOutcome.CredentialAtMedium:
				Assert.True(outcome.Contains((_CS_008, ConfidenceLevel.Medium)), description);
				Assert.DoesNotContain(outcome, entry => entry.PatternClass == _CS_019 && entry.Level is not ConfidenceLevel.Low);
				break;
			case DeclaredOutcome.OnlyLowFindings:
				Assert.True(!outcome.IsEmpty && outcome.All(entry => entry.Level is ConfidenceLevel.Low), description);
				break;
			case DeclaredOutcome.ProxyFactoryAtHigh:
				Assert.True(outcome.SequenceEqual(_proxyFactoryAtHigh), description);
				break;
			default:
				Assert.True(outcome.SequenceEqual(_settingsPairAtHigh), description);
				break;
		}
	}

	private static DeclaredUnsupportedCoverage EntryWhoseConstructContains(ImmutableArray<DeclaredUnsupportedCoverage> declared, string keyword)
	{
		return Assert.Single(declared, entry => entry.Construct.Contains(keyword, StringComparison.Ordinal));
	}

	private static ImmutableArray<string> InternalTermsIn(string text)
	{
		return
		[
			.. PublicSourceIdentifierTests.InternalCoverageTerms().Where(term => text.Contains(term, StringComparison.Ordinal)),
			.. PublicSourceIdentifierTests.InternalIdentifierPatterns().SelectMany(pattern => pattern.Matches(text)).Select(match => match.Value),
			.. Regex.Matches(text, @"\bCS-\d+\b|\bO5-\d+\b").Select(match => match.Value)
		];
	}

	private static string Lines(params string[] lines)
	{
		return string.Join(_NEWLINE, lines);
	}

	/// <summary>The store on line 1, filler lines up to the line before <paramref name="lineNumber"/>, and the target lines beginning on that line.</summary>
	private static string StoreThenAt(int lineNumber, params string[] targetLines)
	{
		return Lines(_STORE, string.Join(_NEWLINE, Enumerable.Repeat(_FILLER_LINE, lineNumber - _LINES_BEFORE_THE_TARGET)), Lines(targetLines));
	}

	private static ImmutableArray<(string PatternClass, ConfidenceLevel Level)> OutcomeOf(string source)
	{
		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source, _PRODUCTION_PATH);

		return [.. recognized.Select(construct => (construct.PatternClassIdentifier, construct.Level)).Order()];
	}
}
