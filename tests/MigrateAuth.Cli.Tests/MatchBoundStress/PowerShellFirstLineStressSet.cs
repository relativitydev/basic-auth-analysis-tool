using System.Collections.Immutable;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>One first line of the lexical-budget stress set: a name that says which shape it is, and the line itself.</summary>
internal sealed record FirstLineStressShape
{
	internal required string Name { get; init; }

	internal required string Line { get; init; }
}

/// <summary>
/// THE FIRST LINES OF THE LEXICAL-BUDGET STRESS SET. The `PowerShell` catalog declares exactly one of the three expressions the lexical budget covers: the
/// first-line detection of an extensionless script, a shebang naming <c>pwsh</c> or <c>powershell</c>. It declares no here-document opening expression and
/// no interpolation identifier extent, so what the budget has to bound for this language is that one expression, run once per extensionless file on the
/// file's first line.
///
/// Every shape is a first line of exactly the reader's line-length ceiling, because that is the longest first line the pipeline can ever hand to the
/// expression: a longer line is skipped as a whole before attribution. Shapes are legal text a repository could contain - a shebang padded with blanks, a
/// shebang with a long argument list, an interpreter path of many segments, near misses of the interpreter name - plus the noise a binary-like file
/// decodes to. None is built to make the expression backtrack catastrophically; what they do is push every honest cost to the ceiling. Every shape is
/// deterministic and invented.
/// </summary>
internal static class PowerShellFirstLineStressSet
{
	internal const int LINE_LENGTH_IN_CHARACTERS = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS;

	private const string _SHEBANG_MARKER = "#!";
	private const string _VALID_SHEBANG = "#!/usr/bin/env pwsh";
	private const string _BLANK = " ";
	private const string _PADDING_SEGMENT = "/seg";
	private const string _ENV_FLAGS_PREFIX = "#!/usr/bin/env ";
	private const string _ENV_FLAG = "-S ";
	private const string _NEAR_MISS_PREFIX = "#!/usr/bin/";
	private const string _NEAR_MISS_INTERPRETER = "pwshx ";
	private const string _SHEBANG_WITH_ARGUMENTS_UNIT = " -NoProfile -NonInteractive";
	private const string _PROSE_UNIT = "this line is ordinary prose and not a shebang at all ";
	private const string _UNTERMINATED_COMMENT_UNIT = "<# ";
	private const char _REPEATED_CHARACTER = 'a';
	private const string _NOISE_ALPHABET =
		"!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00D0\u00DE\u00DF\u00E0\u00E9\u00F0\u00FE\u00FF\u2018\u2019\u201C\u201D";
	private const ulong _NOISE_MULTIPLIER = 6364136223846793005UL;
	private const ulong _NOISE_ADDEND = 1442695040888963407UL;
	internal const ulong NOISE_SEED = 20261002UL;
	private const int _NOISE_SHIFT = 33;

	/// <summary>The shapes, in a fixed order. Each name is what the timing table reports.</summary>
	internal static ImmutableArray<FirstLineStressShape> Build()
	{
		return
		[
			Shape("valid-shebang-padded-with-blanks", PadWith(_VALID_SHEBANG, _BLANK)),
			Shape("valid-shebang-with-a-long-argument-list", PadWith(_VALID_SHEBANG, _SHEBANG_WITH_ARGUMENTS_UNIT)),
			Shape("shebang-then-blanks-and-no-interpreter", PadWith(_SHEBANG_MARKER, _BLANK)),
			Shape("shebang-with-an-interpreter-path-of-many-segments", PadWith(_SHEBANG_MARKER, _PADDING_SEGMENT)),
			Shape("shebang-through-env-with-many-flags-and-no-interpreter", PadWith(_ENV_FLAGS_PREFIX, _ENV_FLAG)),
			Shape("shebang-with-near-miss-interpreter-names", PadWith(_NEAR_MISS_PREFIX, _NEAR_MISS_INTERPRETER)),
			Shape("ordinary-prose-not-a-shebang", PadWith(string.Empty, _PROSE_UNIT)),
			Shape("one-repeated-character", new string(_REPEATED_CHARACTER, LINE_LENGTH_IN_CHARACTERS)),
			Shape("comment-openers-with-no-closing", PadWith(string.Empty, _UNTERMINATED_COMMENT_UNIT)),
			Shape("binary-like-noise", NoiseLine(string.Empty, NOISE_SEED, LINE_LENGTH_IN_CHARACTERS)),
			Shape("binary-like-noise-after-a-shebang-marker", NoiseLine(_SHEBANG_MARKER, NOISE_SEED, LINE_LENGTH_IN_CHARACTERS))
		];
	}

	/// <summary>
	/// A deterministic run of the characters a binary file decodes to under the last-resort code page - punctuation, accented letters and typographic
	/// quotes, with no control character and no line break - of exactly the given length, after the given prefix, from the given seed. Also what the
	/// match-bound stress set fills a binary-like extensionless file with.
	/// </summary>
	internal static string NoiseLine(string prefix, ulong seed, int length)
	{
		StringBuilder line = new(prefix);
		ulong state = seed;

		while (line.Length < length)
		{
			state = unchecked((state * _NOISE_MULTIPLIER) + _NOISE_ADDEND);
			line.Append(_NOISE_ALPHABET[(int)((state >> _NOISE_SHIFT) % (ulong)_NOISE_ALPHABET.Length)]);
		}

		return line.ToString();
	}

	/// <summary>The opening followed by the unit repeated, cut to exactly the line-length ceiling.</summary>
	internal static string PadWith(string opening, string unit)
	{
		StringBuilder line = new(opening);

		while (line.Length < LINE_LENGTH_IN_CHARACTERS)
		{
			line.Append(unit);
		}

		line.Length = LINE_LENGTH_IN_CHARACTERS;

		return line.ToString();
	}

	private static FirstLineStressShape Shape(string name, string line)
	{
		return new FirstLineStressShape { Name = name, Line = line };
	}
}
