using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE TERMINATION AND AGREEMENT CHECKS OF THE TWENTY-FIVE `Shell` CLASS EXPRESSIONS, on the shapes of the commands they read. The checks of
/// <see cref="ShellExpressionTerminationTests"/> run every shipped expression over the openings of the lexical profile; these run the class expressions over the
/// openings of their own constructs (a client's command word and options, an assignment, a here-document, a group redirected on its closing line), each opening
/// followed by nothing that closes it, then by a run of continuations, quoted strings, substitutions and separators, so that each window and each look-ahead
/// exhausts its bound and the continuation fails. A text that did not terminate within the policy's bound would raise the timeout and fail the test; no wall-clock
/// figure is asserted.
///
/// (a) Every class expression declares the match bound of the policy. (b) Every class expression terminates on every text. (c) The generated matcher and the
/// interpreter agree on every match and every group, over the short texts and over the fixtures of every class.
/// </summary>
public sealed class ShellClassTerminationTests
{
	private const int _MATCH_ENUMERATION_CAP = 1_000;
	private const int _SHORT_TEXT_LENGTH = 700;
	private const int _LONG_RUN_LENGTH = 3_000;
	private const int _SHELL_CLASS_COUNT = 25;
	private const int _SHORT_FILLER_REPETITIONS = 12;
	private const int _LONG_FILLER_REPETITIONS = 120;
	private const int _CONTINUATION_REPETITIONS = 400;
	private const int _OPENER_REPETITIONS = 60;

	private static readonly ImmutableArray<string> _openers =
	[
		"curl ",
		"curl -sS -u ",
		"curl -H \"Authorization: Basic ",
		"curl --header=\"Authorization: ",
		"curl -K ",
		"wget --auth-no-challenge ",
		"wget --user=",
		"wget -e http_password=",
		"http -a ",
		"aria2c --http-passwd=",
		"lwp-request -C ",
		"ENC=$(printf '%s' ",
		"options=(-sS -u ",
		"CURL=curl\n$CURL ",
		"alias c=curl\nc ",
		"exec 3<>/dev/tcp/h.test/80\nprintf 'GET / HTTP/1.1\\r\\nAuthorization: Basic ",
		"cat > \"$cfg\" <<EOF\nuser = ",
		"{\n\techo \"user = ",
		"pair=\"u:p\"\nENC=$(echo $pair | ",
		"OPTS=\"-u ",
		"AUTH=\"Basic ",
		"export WGETRC=\"$rc\"\n{\n\techo \"http_password = "
	];

	private static readonly ImmutableArray<string> _fillers =
	[
		"x ",
		"\\\n",
		"\"a\" ",
		"'a' ",
		"$(echo ",
		"u:p@h ",
		"\n"
	];

	public static TheoryData<string> OpenerIndexes()
	{
		TheoryData<string> data = [];

		for (int index = 0; index < _openers.Length; index++)
		{
			data.Add(index.ToString(CultureInfo.InvariantCulture));
		}

		return data;
	}

	[Fact]
	public void EveryClassExpressionDeclaresTheMatchBoundOfThePolicy()
	{
		Assert.All(
			ShellRegisteredExpressions.Classes,
			pair => Assert.True(pair.Expression.MatchTimeout == RecognitionEnginePolicy.MATCH_BOUND, $"{pair.Identifier} declares {pair.Expression.MatchTimeout}."));
		Assert.Equal(_SHELL_CLASS_COUNT, ShellRegisteredExpressions.Classes.Count);
	}

	[Theory]
	[MemberData(nameof(OpenerIndexes))]
	public void EveryClassExpressionTerminatesWhereItsShapeExhaustsItsBoundAndTheContinuationFails(string openerIndex)
	{
		string opener = _openers[int.Parse(openerIndex, CultureInfo.InvariantCulture)];

		foreach (string text in WorstCaseTexts(opener))
		{
			foreach ((string identifier, Regex expression) in ShellRegisteredExpressions.Classes)
			{
				int count = 0;

				foreach (Match match in expression.Matches(text))
				{
					count++;

					if (count >= _MATCH_ENUMERATION_CAP)
					{
						break;
					}
				}

				Assert.True(count < _MATCH_ENUMERATION_CAP, $"{identifier} did not terminate on a worst-case text after '{opener}'.");
			}
		}
	}

	[Theory]
	[MemberData(nameof(OpenerIndexes))]
	public void TheGeneratedMatcherAndTheInterpreterAgreeOnEveryMatchAndEveryGroupOfTheShortTexts(string openerIndex)
	{
		string opener = _openers[int.Parse(openerIndex, CultureInfo.InvariantCulture)];
		List<string> texts =
		[
			.. ShellAnalyzerTests.ClassFixtures().Select(fixture => fixture.Source),
			.. WorstCaseTexts(opener).Where(text => text.Length < _SHORT_TEXT_LENGTH)
		];

		foreach ((string identifier, Regex generated) in ShellRegisteredExpressions.Classes)
		{
			Regex interpreted = ShellRegisteredExpressions.Interpreted(generated);

			foreach (string text in texts)
			{
				Assert.Equal(Render(interpreted, text), Render(generated, text));
			}
		}
	}

	private static IEnumerable<string> WorstCaseTexts(string opener)
	{
		foreach (string filler in _fillers)
		{
			foreach (int repetitions in new[] { _SHORT_FILLER_REPETITIONS, _LONG_FILLER_REPETITIONS })
			{
				yield return opener + string.Concat(Enumerable.Repeat(filler, repetitions));
			}
		}

		yield return opener + new string('x', _LONG_RUN_LENGTH);
		yield return opener + string.Concat(Enumerable.Repeat("\\\n", _CONTINUATION_REPETITIONS));
		yield return string.Concat(Enumerable.Repeat(opener, _OPENER_REPETITIONS));
	}

	private static string Render(Regex expression, string text)
	{
		List<string> rendered = [];

		foreach (Match match in expression.Matches(text))
		{
			rendered.Add(string.Join(
				"|",
				match.Groups.Cast<Group>().Select(group => string.Create(CultureInfo.InvariantCulture, $"{group.Name}:{group.Success}:{group.Index}:{group.Length}"))));

			if (rendered.Count >= _MATCH_ENUMERATION_CAP)
			{
				break;
			}
		}

		return string.Join(";", rendered);
	}
}
