using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// NO INTERNAL IDENTIFIER, APPENDIX MATERIAL OR INTERNAL QUESTION TEXT REACHES THE PUBLIC TEXT.
///
/// Every forbidden pattern is ASSEMBLED FROM FRAGMENTS, so the literal strings never appear in this file and
/// this file does not trip the very check it defines. The release notes are checked against the patterns
/// of internal identifiers plus the decision-record prefixes; the whole User Guide and the README are checked against
/// those and against the design vocabulary that must not appear in a client's text (requirement and
/// decision numbers, set and issue names).
/// </summary>
public sealed class ReleaseNotesIdentifierTests
{
	private const string _FOUND_MESSAGE = "{0}: found the forbidden text '{1}' in: {2}";
	private const string _EXCERPT_ELLIPSIS = "...";
	private const int _EXCERPT_CONTEXT_LENGTH = 30;
	private const string _SELF_CHECK_SAMPLE_TEMPLATE = "See {0} and also {1} for the reasons.";
	private const string _CLEAN_SAMPLE = "The release notes mention Part C, the SHA256SUMS file and version 0.1.0-alpha.";

	/// <summary>The literal internal identifiers checked in the release notes, each as two fragments that are joined at run time.</summary>
	private static readonly (string Head, string Tail)[] _acceptanceFragments =
	[
		("Q", "-REL"),
		("Q", "-PM"),
		("Q", "-PO"),
		("Q", "-ARCH"),
		("REL", "-"),
		("Appendix", " A"),
		("DO NOT", " SHARE"),
		("CA", "-"),
		("D", "-R"),
		("D", "-P")
	];

	/// <summary>Design vocabulary that does not belong in a client's text, as fragments.</summary>
	private static readonly (string Head, string Tail)[] _designFragments =
	[
		("PL", "-"),
		("DR", "-"),
		("R-", "M0"),
		("AC", "-"),
		("FR", "D"),
		("A", "DR"),
		("Set", " 11"),
		("sub", "-issue"),
		("pass", " 2")
	];

	/// <summary>Whole-word forms: a set number, a task number.</summary>
	private static readonly string[] _wholeWordPatterns = ["\\bS([1-9]|1[0-9]|2[0-3])\\b", "\\bT[1-8]\\b"];

	/// <summary>The release notes contain none of the internal identifier patterns.</summary>
	[Fact]
	public void TheReleaseNotesContainNoInternalIdentifier()
	{
		string notes = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_RELEASE_NOTES);

		Assert.Empty(ProblemsIn(nameof(PublicDocuments.ROW_RELEASE_NOTES), notes, AcceptancePatterns()));
	}

	/// <summary>The whole User Guide, the index included, and the README contain no internal identifier and no design vocabulary.</summary>
	[Fact]
	public void TheWholeUserGuideAndTheReadmeContainNoInternalIdentifierOrDesignVocabulary()
	{
		string[] paths = [.. PublicDocuments.UserGuidePaths(), PublicDocuments.ReadmePath()];

		string[] problems = [.. paths.SelectMany(path => ProblemsIn(Path.GetFileName(path), File.ReadAllText(path), AllPatterns()))];

		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The check fails on each pattern it is built from, shown on samples assembled at run time, and passes clean text.</summary>
	[Fact]
	public void TheIdentifierCheckReportsEveryPatternItIsBuiltFromAndPassesCleanText()
	{
		foreach (Regex pattern in AllPatterns())
		{
			string offender = SampleThatMatches(pattern);
			string sample = string.Format(CultureInfo.InvariantCulture, _SELF_CHECK_SAMPLE_TEMPLATE, offender, _CLEAN_SAMPLE);

			Assert.NotEmpty(ProblemsIn("synthetic", sample, [pattern]));
		}

		Assert.Empty(ProblemsIn("synthetic", _CLEAN_SAMPLE, AllPatterns()));
	}

	private static ImmutableArray<Regex> AcceptancePatterns()
	{
		return [.. _acceptanceFragments.Select(fragments => LiteralPattern(fragments.Head, fragments.Tail))];
	}

	private static ImmutableArray<Regex> AllPatterns()
	{
		return
		[
			.. AcceptancePatterns(),
			.. _designFragments.Select(fragments => LiteralPattern(fragments.Head, fragments.Tail)),
			.. _wholeWordPatterns.Select(pattern => new Regex(pattern, RegexOptions.CultureInvariant))
		];
	}

	private static Regex LiteralPattern(string head, string tail)
	{
		return new Regex(Regex.Escape(head + tail), RegexOptions.CultureInvariant);
	}

	private static ImmutableArray<string> ProblemsIn(string name, string text, ImmutableArray<Regex> patterns)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		foreach (Regex pattern in patterns)
		{
			foreach (Match match in pattern.Matches(text))
			{
				problems.Add(string.Format(CultureInfo.InvariantCulture, _FOUND_MESSAGE, name, match.Value, Excerpt(text, match)));
			}
		}

		return problems.ToImmutable();
	}

	private static string Excerpt(string text, Match match)
	{
		int start = Math.Max(0, match.Index - _EXCERPT_CONTEXT_LENGTH);
		int end = Math.Min(text.Length, match.Index + match.Length + _EXCERPT_CONTEXT_LENGTH);

		return _EXCERPT_ELLIPSIS + text[start..end].Replace('\n', ' ') + _EXCERPT_ELLIPSIS;
	}

	/// <summary>A string the pattern matches: its own literal text, or a representative for a whole-word pattern.</summary>
	private static string SampleThatMatches(Regex pattern)
	{
		string source = pattern.ToString();

		return source switch
		{
			"\\bT[1-8]\\b" => "T3",
			"\\bS([1-9]|1[0-9]|2[0-3])\\b" => "S" + "12",
			_ => Regex.Unescape(source)
		};
	}
}
