using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Documentation;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE FLOW A FILE WITHOUT A SIGNAL RECEIVES IS THE DEFAULT FLOW, WHICH IS CONFIGURABLE. A declared-coverage entry that describes such a file says "the default flow" and states what it is;
/// it never names Client Credentials or Authorization Code as the outcome, because the report would then contradict its own findings when another default is configured. The text names no
/// configuration key, file or profile: the User Guide alone does. The same holds for the bullets of Part A that mirror the entries.
/// </summary>
public sealed class DeclaredCoverageDefaultFlowTests
{
	private const int _REGEX_TIMEOUT_IN_SECONDS = 1;

	private const int _QUALIFIED_ENTRY_COUNT = 21;

	private const string _USER_GUIDE_DEFAULT_FLOW_WITH_BUILT_IN = "Client Credentials unless `defaultFlow` sets another";

	private const string _BUILT_IN_NOTE_ROW_PREFIX = "| [N";

	private const int _USING_THE_TOOL_ROW = 1;

	private const int _LIMITS_ROW = 3;

	private const string _CLIENT_CREDENTIALS_NAME = "Client Credentials";

	private const string _DEFAULT_FLOW_PHRASE = "the default flow";

	private const string _DEFAULT_FLOW_WITH_BUILT_IN = "the default flow (Client Credentials unless another default is configured)";

	private const string _NO_SIGNAL_REASON = "nothing in it shows that a person signs in";

	private const string _NO_SIGNAL_REASON_SHORT = "nothing shows that a person signs in";

	private static readonly ImmutableArray<string> _configurationWording = ["defaultFlow", "configuration file", "profile"];

	private static readonly ImmutableDictionary<string, int> _qualifiedEntriesByLanguage = ImmutableDictionary<string, int>.Empty
		.Add("CSharp", 1)
		.Add("VBNet", 2)
		.Add("JavaScript", 3)
		.Add("TypeScript", 4)
		.Add("Python", 4)
		.Add("Go", 3)
		.Add("PowerShell", 2)
		.Add("Shell", 2);

	private static readonly Regex _fixedFlowAsOutcome = new(
		@"(given|read as|flow is|assigned|takes|receives)\s+(Client Credentials|Authorization Code)\b[^;]*because nothing",
		RegexOptions.CultureInvariant,
		TimeSpan.FromSeconds(_REGEX_TIMEOUT_IN_SECONDS));

	private static readonly Regex _fixedFlowVerbOutcome = new(
		@"\b(given|read as|flow is|assigned|takes|receives|points? to|recommends?|recommended|leads? to|results? in|ends? up|yields?|gives?|decided as)\s+(the flow\s+)?(Client Credentials|Authorization Code)\b",
		RegexOptions.CultureInvariant,
		TimeSpan.FromSeconds(_REGEX_TIMEOUT_IN_SECONDS));

	private static readonly Regex _noSignalSubject = new(
		@"\bno (interactive|flow|sign-in)( user)?( signal| evidence)|\bno signal\b|nothing (in it )?shows|\bholds no\b",
		RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
		TimeSpan.FromSeconds(_REGEX_TIMEOUT_IN_SECONDS));

	private static readonly Regex _clientCredentialsMention = new(_CLIENT_CREDENTIALS_NAME, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(_REGEX_TIMEOUT_IN_SECONDS));

	private static readonly Regex _statedBuiltInDefault = new(
		Regex.Escape(_DEFAULT_FLOW_WITH_BUILT_IN),
		RegexOptions.CultureInvariant,
		TimeSpan.FromSeconds(_REGEX_TIMEOUT_IN_SECONDS));

	private static readonly Regex _crossClauseOutcome = new(
		@"\b(points? to|recommends?|leads? to|results? in|ends? up|yields?|gives?|given|read as|flow is)\s+(Client Credentials|Authorization Code)\b[^.;]*\bnothing (in it )?shows",
		RegexOptions.CultureInvariant,
		TimeSpan.FromSeconds(_REGEX_TIMEOUT_IN_SECONDS));

	private static readonly Regex _fallbackOutcome = new(
		@"\b(falls? back to|recommended flow (therefore )?is)\s+(Client Credentials|Authorization Code)\b",
		RegexOptions.CultureInvariant,
		TimeSpan.FromSeconds(_REGEX_TIMEOUT_IN_SECONDS));

	private static readonly Regex _sentenceBoundary = new(@"[;.,]\s", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(_REGEX_TIMEOUT_IN_SECONDS));

	/// <summary>True when one sentence names a subject with no flow signal and a fixed flow as what that subject receives; reader guidance ("use ...") and signal-driven outcomes do not match.</summary>
	private static bool NamesAFixedFlowForAFileWithoutASignal(string text)
	{
		return _fallbackOutcome.IsMatch(text)
			|| _crossClauseOutcome.IsMatch(text)
			|| _sentenceBoundary.Split(text).Any(sentence => _noSignalSubject.IsMatch(sentence) && _fixedFlowVerbOutcome.IsMatch(sentence));
	}

	private static ImmutableArray<string> EveryEntryText()
	{
		return [.. DocumentedLanguage.All.SelectMany(language => EntryTexts(language))];
	}

	private static ImmutableArray<string> EntryTexts(DocumentedLanguage language)
	{
		ImmutableArray<string>.Builder texts = ImmutableArray.CreateBuilder<string>();

		foreach (DeclaredUnsupportedCoverage entry in language.Analyzer().LanguageIdentification.DeclaredUnsupportedCoverage)
		{
			texts.Add(entry.Construct);
			texts.Add(entry.Reason);
		}

		return texts.ToImmutable();
	}

	private static int QualifiedEntryCount(DocumentedLanguage language)
	{
		int count = 0;

		foreach (DeclaredUnsupportedCoverage entry in language.Analyzer().LanguageIdentification.DeclaredUnsupportedCoverage)
		{
			if (entry.Construct.Contains(_DEFAULT_FLOW_WITH_BUILT_IN, StringComparison.Ordinal)
				|| entry.Reason.Contains(_DEFAULT_FLOW_WITH_BUILT_IN, StringComparison.Ordinal))
			{
				count++;
			}
		}

		return count;
	}

	[Fact]
	public void AnEntryThatDescribesAFileWithoutASignalNamesTheDefaultFlowAndNoFixedFlowAsTheOutcome()
	{
		ImmutableArray<string> texts = EveryEntryText();

		Assert.NotEmpty(texts);
		Assert.All(texts, text =>
		{
			Assert.DoesNotMatch(_fixedFlowAsOutcome, text);
			Assert.False(NamesAFixedFlowForAFileWithoutASignal(text));

			if (text.Contains(_NO_SIGNAL_REASON, StringComparison.Ordinal) || text.Contains(_NO_SIGNAL_REASON_SHORT, StringComparison.Ordinal))
			{
				Assert.Contains(_DEFAULT_FLOW_PHRASE, text, StringComparison.Ordinal);
			}
		});
	}

	[Fact]
	public void EveryMentionOfClientCredentialsInAnEntryIsTheBuiltInValueOfTheDefaultFlow()
	{
		Assert.All(EveryEntryText(), text =>
		{
			int mentions = _clientCredentialsMention.Count(text);
			int stated = _statedBuiltInDefault.Count(text);

			Assert.Equal(stated, mentions);
		});
	}

	[Fact]
	public void TheTwentyOneEntriesThatNameTheDefaultFlowAreCountedPerLanguage()
	{
		int total = 0;

		foreach (DocumentedLanguage language in DocumentedLanguage.All)
		{
			int count = QualifiedEntryCount(language);

			Assert.Equal(_qualifiedEntriesByLanguage[language.LanguageName], count);
			total += count;
		}

		Assert.Equal(_QUALIFIED_ENTRY_COUNT, total);
	}

	[Fact]
	public void AnEntryThatNamesTheDefaultFlowNamesNoConfigurationKeyFileOrProfile()
	{
		Assert.All(EveryEntryText().Where(text => text.Contains(_DEFAULT_FLOW_PHRASE, StringComparison.Ordinal)), text =>
		{
			Assert.All(_configurationWording, wording => Assert.DoesNotContain(wording, text, StringComparison.OrdinalIgnoreCase));
		});
	}

	[Fact]
	public void PartAOfEveryLanguageNamesNoFixedFlowAsTheOutcomeForAFileWithoutASignal()
	{
		Assert.All(DocumentedLanguage.All, language =>
		{
			string[] lines = File.ReadAllLines(language.PartAPath());

			Assert.All(lines, line =>
			{
				Assert.DoesNotMatch(_fixedFlowAsOutcome, line);
				Assert.False(NamesAFixedFlowForAFileWithoutASignal(line));
				Assert.DoesNotContain($"{_CLIENT_CREDENTIALS_NAME}, the flow used when", line, StringComparison.Ordinal);
			});
		});
	}

	[Fact]
	public void TheUserGuideNamesClientCredentialsNextToTheDefaultFlowOnlyWithTheDefaultFlowQualifier()
	{
		ImmutableArray<int> rows = [_USING_THE_TOOL_ROW, _LIMITS_ROW];

		Assert.All(rows, row =>
		{
			Assert.All(File.ReadAllLines(PublicDocuments.UserGuideRowPath(row)).Where(line => !line.StartsWith(_BUILT_IN_NOTE_ROW_PREFIX, StringComparison.Ordinal)), line =>
			{
				Assert.DoesNotMatch(_fallbackOutcome, line);
				Assert.False(NamesAFixedFlowForAFileWithoutASignal(line));

				foreach (string clause in _sentenceBoundary.Split(line))
				{
					if (clause.Contains(_DEFAULT_FLOW_PHRASE, StringComparison.Ordinal) && clause.Contains(_CLIENT_CREDENTIALS_NAME, StringComparison.Ordinal))
					{
						Assert.Contains(_USER_GUIDE_DEFAULT_FLOW_WITH_BUILT_IN, clause, StringComparison.Ordinal);
					}
				}
			});
		});
	}
}
