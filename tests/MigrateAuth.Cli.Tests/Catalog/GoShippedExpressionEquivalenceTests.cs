using System.Collections.Frozen;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.MatchBoundStress;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SHIPPED CLASS EXPRESSIONS AGAINST THE DRAFT THAT WAS VERIFIED. The draft expressions (<see cref="GoDraftExpressions"/>) were run through the semantic samples and the cost gate before the catalog was
/// final, and what the analyzer ships must mean what the draft meant. Every sample of a draft expression is run against the expression the analyzer declares for that class, read from the binary: the same
/// verdict, and the same text in the capture group where the sample names one.
///
/// THE ONE DIFFERENCE IS THE ZERO-WIDTH POINT. The shipped argument window appends <c>(?:\b|\B)</c> inside the body of its lazy bounded loop, because the regular-expression source generator of the toolchain emits a defective cleanup
/// of its backtracking stack for such a loop when the body has no backtracking construct of its own. The point matches at every position and changes no match, so each shipped pattern is the draft's text plus a wrapper
/// group and the point at every window, and nothing else: a test states that by the length of the two texts. The generated and the interpreted form of each shipped expression must agree on every sample.
///
/// THE OTHER DIFFERENCE IS THE NESTED ENCODER TAIL OF THE THREE HEADER CLASSES (the whole-literal pair credential is added at catalog version 0.5). GO-002, GO-003 and GO-004 end with an optional, atomic tail that
/// extends the match over the encoder call when the value is built with the encoder in the same expression, so that the declared overlap with GO-006 holds. Each of the three is the draft's text plus that tail, and
/// on every draft sample it gives the same verdict, the same index and the same captures, and a match that is the draft's match or the draft's match followed by the tail.
/// </summary>
public sealed class GoShippedExpressionEquivalenceTests
{
	private const string _BACKTRACK_POINT = @"(?:\b|\B)";

	/// <summary>The wrapper group the point comes with: <c>(?:</c> and <c>)</c> around the unit.</summary>
	private const int _WRAPPER_LENGTH = 4;

	/// <summary>The three header classes name the group that opens the Sprintf call, which the tail's conditional reads: <c>(?&lt;sprintf&gt;</c> where the draft has <c>(?:</c>.</summary>
	private const int _SPRINTF_GROUP_NAME_LENGTH = 8;

	/// <summary>The placeholder vocabulary the pair credential refuses, as the analyzer declares it.</summary>
	private const string _PLACEHOLDER_BODY =
		@"(?i:[*x.\-_#]{3,}|<[^>\n]{0,100}>|\[[^\]\n]{0,100}\]|\{\{[^}\n]{0,100}\}\}|%[^%\n]{0,100}%|placeholder|changeme|change-me|change_me|replace-me|replaceme|your-password|your_password|redacted|todo|fixme|dummy|not-a-real-secret)";

	/// <summary>The credential of the nested encoder (0.5): the operand `[]byte(` and one whole-literal pair, the pair rule of GO-006, and the closing parenthesis of the conversion, exactly as the analyzer declares it.</summary>
	private const string _NESTED_PAIR_CREDENTIAL =
		@"(?:(?=(?>\s{0,40})\[\]byte(?>\s{0,40})\((?>\s{0,40})(?=(?:\x22[^\x22\\\n:\s]{1,100}:[^\x22\\\n\s]{1,200}\x22|`[^`:\s]{1,100}:[^`\s]{1,200}`))(?:\x22[^\x22\\\n:]{0,100}:(?!" + _PLACEHOLDER_BODY + @"\x22)(?<credential>[^\x22\\\n]{1,200})\x22|`[^`:]{0,100}:(?!" + _PLACEHOLDER_BODY + @"`)(?<credential>[^`]{1,200})`)(?>\s{0,40})\)))?";

	/// <summary>The tail the three header classes carry after the draft text, exactly as the analyzer declares it: the nested encoder, up to its opening parenthesis, and the credential of its whole-literal pair.</summary>
	private const string _NESTED_ENCODER_TAIL_HEAD =
		@"[^\x22`\n]{0,60}[\x22`](?>\s{0,40})(?(sprintf),|\+)(?>\s{0,40})(?:[\p{L}_][\p{L}\p{N}_]{0,40}\.)?(?:RawStdEncoding|StdEncoding)(?>\s{0,40})\.(?>\s{0,40})(?:EncodeToString|AppendEncode|Encode)(?![\p{L}\p{N}_])(?>\s{0,40})\(";

	/// <summary>The tail of GO-003 and GO-004.</summary>
	private const string _NESTED_ENCODER_TAIL = @"(?>" + _NESTED_ENCODER_TAIL_HEAD + @")?";

	/// <summary>The tail of GO-002 only: the same tail and the credential of the whole-literal pair operand.</summary>
	private const string _NESTED_ENCODER_TAIL_WITH_PAIR = @"(?>" + _NESTED_ENCODER_TAIL_HEAD + _NESTED_PAIR_CREDENTIAL + @")?";

	private static string TailOf(string draftIdentifier)
	{
		return draftIdentifier == "EX-GO-002" ? _NESTED_ENCODER_TAIL_WITH_PAIR : _NESTED_ENCODER_TAIL;
	}

	private static readonly FrozenSet<string> _widenedClasses = new[] { "EX-GO-002", "EX-GO-003", "EX-GO-004" }.ToFrozenSet(StringComparer.Ordinal);

	private static readonly FrozenDictionary<string, string> _shippedByDraftIdentifier = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["EX-GO-101"] = GoRegisteredExpressions.CONSTANT_DECLARATION_IDENTIFIER,
		["EX-GO-001"] = "GO-001",
		["EX-GO-002"] = "GO-002",
		["EX-GO-003"] = "GO-003",
		["EX-GO-004"] = "GO-004",
		["EX-GO-005"] = "GO-005",
		["EX-GO-006"] = "GO-006",
		["EX-GO-007"] = "GO-007",
		["EX-GO-008"] = "GO-008",
		["EX-GO-009"] = "GO-009",
		["EX-GO-010"] = "GO-010",
		["EX-GO-011"] = "GO-011",
		["EX-GO-012"] = "GO-012",
		["EX-GO-013"] = "GO-013",
		["EX-GO-102"] = GoRegisteredExpressions.MEMBER_DECLARATION_IDENTIFIER,
		["EX-GO-110"] = GoRegisteredExpressions.CONTENT_MARKER_IDENTIFIER_PREFIX + "0",
		["EX-GO-111"] = GoRegisteredExpressions.CONTENT_MARKER_IDENTIFIER_PREFIX + "1"
	}.ToFrozenDictionary(StringComparer.Ordinal);

	private static readonly GoDraftExpressionSemanticTests.Case[] _cases =
	[
		.. GoDraftExpressionSemanticTests.Cases.Where(sample => _shippedByDraftIdentifier.ContainsKey(sample.Id))
	];

	[Fact]
	public void EveryShippedExpressionHasPositiveAndNegativeSamplesFromTheDraft()
	{
		Assert.All(_shippedByDraftIdentifier.Keys, identifier =>
		{
			Assert.Contains(_cases, sample => sample.Id == identifier && sample.Matches);
			Assert.Contains(_cases, sample => sample.Id == identifier && !sample.Matches);
		});
	}

	[Fact]
	public void EveryShippedExpressionGivesTheVerdictAndTheCaptureTheDraftSampleStates()
	{
		List<string> wrong = [];

		foreach (GoDraftExpressionSemanticTests.Case sample in _cases)
		{
			Match match = ShippedExpressionOf(sample.Id).Match(sample.Text, sample.Start);
			bool captured = sample.Group.Length == 0 || (match.Success && match.Groups[sample.Group].Value == sample.Value);

			if (match.Success != sample.Matches || !captured)
			{
				wrong.Add($"{_shippedByDraftIdentifier[sample.Id]} on [{Escape(sample.Text)}] expected {sample.Matches} {sample.Group}={sample.Value}, got {match.Success}");
			}
		}

		Assert.Empty(wrong);
	}

	[Fact]
	public void TheShippedAndTheDraftExpressionOfEveryClassAgreeOnEveryDraftSample()
	{
		List<string> disagreements = [];

		foreach (GoDraftExpressionSemanticTests.Case sample in _cases)
		{
			Match shipped = ShippedExpressionOf(sample.Id).Match(sample.Text, sample.Start);
			Match draft = DraftExpressionOf(sample.Id).Match(sample.Text, sample.Start);

			if (shipped.Success != draft.Success || shipped.Index != draft.Index || !HasTheDraftMatchSpan(sample.Id, shipped, draft))
			{
				disagreements.Add($"{_shippedByDraftIdentifier[sample.Id]} on [{Escape(sample.Text)}]");
			}
		}

		Assert.Empty(disagreements);
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfEveryShippedExpressionAgreeOnEverySample()
	{
		List<string> disagreements = [];

		foreach (GoDraftExpressionSemanticTests.Case sample in _cases)
		{
			Regex generated = ShippedExpressionOf(sample.Id);
			Regex interpreted = GoRegisteredExpressions.Interpreted(generated);

			if (generated.Match(sample.Text, sample.Start).Success != interpreted.Match(sample.Text, sample.Start).Success)
			{
				disagreements.Add($"{_shippedByDraftIdentifier[sample.Id]} on {Escape(sample.Text)}");
			}
		}

		Assert.Empty(disagreements);
	}

	[Fact]
	public void EachShippedPatternIsTheDraftPatternPlusTheWrapperAndTheZeroWidthPointAtEveryArgumentWindowAndNothingElse()
	{
		List<string> different = [];

		foreach ((string draftIdentifier, string shippedIdentifier) in _shippedByDraftIdentifier)
		{
			string draft = DraftExpressionOf(draftIdentifier).ToString();
			string shipped = ShippedExpressionOf(draftIdentifier).ToString();
			int points = CountOf(shipped, _BACKTRACK_POINT);
			int tailLength = _widenedClasses.Contains(draftIdentifier) ? TailOf(draftIdentifier).Length + _SPRINTF_GROUP_NAME_LENGTH : 0;
			int expectedLength = draft.Length + (points * (_BACKTRACK_POINT.Length + _WRAPPER_LENGTH)) + tailLength;
			bool tailIsRight = tailLength == 0 || shipped.EndsWith(TailOf(draftIdentifier), StringComparison.Ordinal);

			if (CountOf(draft, _BACKTRACK_POINT) != 0 || shipped.Length != expectedLength || !tailIsRight)
			{
				different.Add($"{shippedIdentifier}: {points} points, shipped {shipped.Length} characters, draft {draft.Length}, expected {expectedLength}, tail right {tailIsRight}");
			}
		}

		Assert.True(different.Count == 0, string.Join("; ", different));
	}

	[Fact]
	public void OnlyTheFiveExpressionsThatReadAnArgumentWindowCarryTheZeroWidthPoint()
	{
		string[] carriers =
		[
			.. _shippedByDraftIdentifier.Values
				.Where(identifier => CountOf(GoRegisteredExpressions.All.Single(pair => pair.Identifier == identifier).Expression.ToString(), _BACKTRACK_POINT) > 0)
				.Order(StringComparer.Ordinal)
		];

		Assert.Equal(["GO-003", "GO-004", "GO-005", "GO-006", "GO-013"], carriers);
	}

	/// <summary>
	/// The span the draft sample gives: the same for every class but the three header classes, whose shipped match is the draft's match, or the draft's match followed by the tail that reaches the encoder
	/// call (and then ends at the opening parenthesis of the encoder).
	/// </summary>
	private static bool HasTheDraftMatchSpan(string draftIdentifier, Match shipped, Match draft)
	{
		bool sameSpan = shipped.Length == draft.Length;

		if (_widenedClasses.Contains(draftIdentifier) && shipped.Success && draft.Success)
		{
			string rest = shipped.Value[Math.Min(draft.Length, shipped.Length)..];
			bool extendsTheDraftMatch = shipped.Length > draft.Length && shipped.Value.StartsWith(draft.Value, StringComparison.Ordinal)
				&& Regex.IsMatch(rest, GroupDefinition(shipped) + @"\A(?:" + _NESTED_ENCODER_TAIL + @")\z", RegexOptions.CultureInvariant);

			sameSpan = sameSpan || extendsTheDraftMatch;
		}

		return sameSpan;
	}

	/// <summary>Defines the group the tail's conditional reads: set when the shipped match opens with a `Sprintf` call, unset otherwise.</summary>
	private static string GroupDefinition(Match shipped)
	{
		return shipped.Groups["sprintf"].Success ? "(?<sprintf>)" : "(?<sprintf>(?!))?";
	}

	private static Regex ShippedExpressionOf(string draftIdentifier)
	{
		return GoRegisteredExpressions.All.Single(pair => pair.Identifier == _shippedByDraftIdentifier[draftIdentifier]).Expression;
	}

	private static Regex DraftExpressionOf(string draftIdentifier)
	{
		return GoDraftExpressions.MatchBoundExpressions.Single(pair => pair.Identifier == draftIdentifier).Expression;
	}

	private static int CountOf(string text, string fragment)
	{
		int count = 0;
		int index = text.IndexOf(fragment, StringComparison.Ordinal);

		while (index >= 0)
		{
			count++;
			index = text.IndexOf(fragment, index + fragment.Length, StringComparison.Ordinal);
		}

		return count;
	}

	private static string Escape(string text)
	{
		return text.Replace("\n", "\\n", StringComparison.Ordinal);
	}
}
