using System.Collections.Immutable;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE FIRST LINES OF THE `JavaScript` LEXICAL-BUDGET STRESS SET. The `JavaScript` catalog declares ONE expression the lexical budget charges: the first-line
/// detection of an extensionless script, a hashbang naming `node` or `nodejs` (`EX-JS-100`), run once per extensionless file on the file's first line. The profile declares no
/// captured terminator, so no opening expression is charged; the backward predicate of the regular-expression opening is not charged either (it is gated by its own throughput proof).
///
/// Every shape is a first line of exactly the reader's line-length ceiling, because that is the longest first line the pipeline can ever hand to the expression: a hashbang padded with
/// blanks, with a long argument list, an interpreter path of many segments, flags with no interpreter, near misses of the interpreter names, prose, one repeated character, unterminated
/// openers and the noise a binary-like file decodes to. None is built to make the expression backtrack catastrophically. Every shape is deterministic and invented.
/// </summary>
internal static class JavaScriptFirstLineStressSet
{
	internal const int LINE_LENGTH_IN_CHARACTERS = PowerShellFirstLineStressSet.LINE_LENGTH_IN_CHARACTERS;

	internal const string VALID_SHAPE_NAME_PREFIX = "valid-hashbang";

	private const string _HASHBANG_MARKER = "#!";
	private const string _VALID_HASHBANG = "#!/usr/bin/node";
	private const string _BLANK = " ";
	private const string _SEGMENT = "/seg";
	private const string _EMPTY_SEGMENT = "/";
	private const string _ENV_FLAGS_PREFIX = "#!/usr/bin/env ";
	private const string _ENV_FLAG = "-S ";
	private const string _NEAR_MISS_PREFIX = "#!/usr/bin/";
	private const string _NEAR_MISS_INTERPRETER = "nodex ";
	private const string _HASHBANG_WITH_ARGUMENTS_UNIT = " --no-warnings --max-old-space-size=4096";
	private const string _PROSE_UNIT = "this line is ordinary prose and not a hashbang at all ";
	private const string _UNTERMINATED_OPENER_UNIT = "${ ";
	private const char _REPEATED_CHARACTER = 'a';

	/// <summary>The shapes, in a fixed order. Each name is what the timing table reports.</summary>
	internal static ImmutableArray<FirstLineStressShape> Build()
	{
		return
		[
			Shape($"{VALID_SHAPE_NAME_PREFIX}-padded-with-blanks", PowerShellFirstLineStressSet.PadWith(_VALID_HASHBANG, _BLANK)),
			Shape($"{VALID_SHAPE_NAME_PREFIX}-with-a-long-argument-list", PowerShellFirstLineStressSet.PadWith(_VALID_HASHBANG, _HASHBANG_WITH_ARGUMENTS_UNIT)),
			Shape("hashbang-then-blanks-and-no-interpreter", PowerShellFirstLineStressSet.PadWith(_HASHBANG_MARKER, _BLANK)),
			Shape("hashbang-with-an-interpreter-path-of-many-segments", PowerShellFirstLineStressSet.PadWith(_HASHBANG_MARKER, _SEGMENT)),
			Shape("hashbang-with-a-path-of-empty-segments", PowerShellFirstLineStressSet.PadWith(_HASHBANG_MARKER, _EMPTY_SEGMENT)),
			Shape("hashbang-through-env-with-many-flags-and-no-interpreter", PowerShellFirstLineStressSet.PadWith(_ENV_FLAGS_PREFIX, _ENV_FLAG)),
			Shape("hashbang-with-near-miss-interpreter-names", PowerShellFirstLineStressSet.PadWith(_NEAR_MISS_PREFIX, _NEAR_MISS_INTERPRETER)),
			Shape("ordinary-prose-not-a-hashbang", PowerShellFirstLineStressSet.PadWith(string.Empty, _PROSE_UNIT)),
			Shape("one-repeated-character", new string(_REPEATED_CHARACTER, LINE_LENGTH_IN_CHARACTERS)),
			Shape("hashbang-then-one-repeated-character", PowerShellFirstLineStressSet.PadWith(_HASHBANG_MARKER, _REPEATED_CHARACTER.ToString())),
			Shape("unterminated-openers-with-no-closing", PowerShellFirstLineStressSet.PadWith(string.Empty, _UNTERMINATED_OPENER_UNIT)),
			Shape("binary-like-noise", PowerShellFirstLineStressSet.NoiseLine(string.Empty, PowerShellFirstLineStressSet.NOISE_SEED, LINE_LENGTH_IN_CHARACTERS)),
			Shape("binary-like-noise-after-a-hashbang-marker", PowerShellFirstLineStressSet.NoiseLine(_HASHBANG_MARKER, PowerShellFirstLineStressSet.NOISE_SEED, LINE_LENGTH_IN_CHARACTERS))
		];
	}

	private static FirstLineStressShape Shape(string name, string line)
	{
		return new FirstLineStressShape { Name = name, Line = line };
	}
}
