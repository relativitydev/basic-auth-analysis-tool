using System.Collections.Immutable;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE FIRST LINES OF THE `Python` LEXICAL-BUDGET STRESS SET. The `Python` analyzer declares one expression the lexical budget charges: the first-line detection of an extensionless script, a shebang naming
/// <c>python</c>, <c>python2</c>, <c>python3</c> or <c>python3.{digits}</c> (`EX-PY-100`), run once per extensionless file on the file's first line, as the fourth first-line expression of the attribution order. The
/// profile declares no captured terminator, so no opening expression is charged.
///
/// Every shape is a first line of exactly the reader's line-length ceiling, because that is the longest first line the pipeline can ever hand to the expression. The shapes the catalog names are here: a line of
/// <c>/</c> path segments, a line of <c>-</c> flags after <c>env</c>, a line of blanks, and <c>python3.</c> with a long digit run. Beside them stand the shapes of the other three first-line stress sets: a
/// shebang padded with blanks, with a long argument list, an interpreter path of many segments, flags with no interpreter, near misses of the interpreter names, prose, one repeated character and the noise a binary-like
/// file decodes to, and the refused flag after the interpreter. None is built to make the expression backtrack catastrophically; what they do is push every honest cost to the ceiling. Every shape is deterministic and
/// invented.
/// </summary>
internal static class PythonFirstLineStressSet
{
	internal const int LINE_LENGTH_IN_CHARACTERS = PowerShellFirstLineStressSet.LINE_LENGTH_IN_CHARACTERS;

	internal const string VALID_SHAPE_NAME_PREFIX = "valid-shebang";

	private const string _SHEBANG_MARKER = "#!";
	private const string _VALID_SHEBANG = "#!/usr/bin/python3";
	private const string _VALID_ENV_SHEBANG = "#!/usr/bin/env python3";
	private const string _VALID_TAIL = "/python3";
	private const string _INTERPRETER_NAME = "python3";
	private const string _VERSIONED_OPENING = "#!/usr/bin/python3.";
	private const string _FLAG_AFTER_THE_INTERPRETER = "#!/usr/bin/python3 -S";
	private const string _BLANK = " ";
	private const string _SEGMENT = "/seg";
	private const string _EMPTY_SEGMENT = "/";
	private const string _ENV_FLAGS_PREFIX = "#!/usr/bin/env ";
	private const string _ENV_FLAG = "-S ";
	private const string _DASH = "-";
	private const string _DIGIT = "9";
	private const string _DOT_DIGIT_PAIR = ".1";
	private const string _NEAR_MISS_PREFIX = "#!/usr/bin/";
	private const string _NEAR_MISS_INTERPRETER = "python3x ";
	private const string _SHEBANG_WITH_ARGUMENTS_UNIT = " -u -B -W ignore";
	private const string _PROSE_UNIT = "this line is ordinary prose and not a shebang at all ";
	private const char _REPEATED_CHARACTER = 'a';

	/// <summary>The shapes, in a fixed order. Each name is what the timing table reports; the names that begin with <see cref="VALID_SHAPE_NAME_PREFIX"/> are the lines the expression claims.</summary>
	internal static ImmutableArray<FirstLineStressShape> Build()
	{
		return
		[
			Shape($"{VALID_SHAPE_NAME_PREFIX}-padded-with-blanks", PowerShellFirstLineStressSet.PadWith(_VALID_SHEBANG, _BLANK)),
			Shape($"{VALID_SHAPE_NAME_PREFIX}-through-env-with-a-long-argument-list", PowerShellFirstLineStressSet.PadWith(_VALID_ENV_SHEBANG, _SHEBANG_WITH_ARGUMENTS_UNIT)),
			Shape($"{VALID_SHAPE_NAME_PREFIX}-with-blanks-before-the-interpreter", PadBeforeATail(_SHEBANG_MARKER, _BLANK, _INTERPRETER_NAME)),
			Shape($"{VALID_SHAPE_NAME_PREFIX}-with-a-path-of-many-segments", PadBeforeATail(_SHEBANG_MARKER, _SEGMENT, _VALID_TAIL)),
			Shape($"{VALID_SHAPE_NAME_PREFIX}-with-a-versioned-interpreter-and-a-long-digit-run", PowerShellFirstLineStressSet.PadWith(_VERSIONED_OPENING, _DIGIT)),
			Shape("flag-after-the-interpreter-then-blanks", PowerShellFirstLineStressSet.PadWith(_FLAG_AFTER_THE_INTERPRETER, _BLANK)),
			Shape("versioned-interpreter-then-dot-digit-pairs", PowerShellFirstLineStressSet.PadWith(_VERSIONED_OPENING + _DIGIT, _DOT_DIGIT_PAIR)),
			Shape("shebang-then-blanks-and-no-interpreter", PowerShellFirstLineStressSet.PadWith(_SHEBANG_MARKER, _BLANK)),
			Shape("shebang-with-an-interpreter-path-of-many-segments", PowerShellFirstLineStressSet.PadWith(_SHEBANG_MARKER, _SEGMENT)),
			Shape("shebang-with-a-path-of-empty-segments", PowerShellFirstLineStressSet.PadWith(_SHEBANG_MARKER, _EMPTY_SEGMENT)),
			Shape("shebang-through-env-with-many-flags-and-no-interpreter", PowerShellFirstLineStressSet.PadWith(_ENV_FLAGS_PREFIX, _ENV_FLAG)),
			Shape("shebang-through-env-with-a-long-run-of-dashes", PowerShellFirstLineStressSet.PadWith(_ENV_FLAGS_PREFIX, _DASH)),
			Shape("shebang-with-near-miss-interpreter-names", PowerShellFirstLineStressSet.PadWith(_NEAR_MISS_PREFIX, _NEAR_MISS_INTERPRETER)),
			Shape("ordinary-prose-not-a-shebang", PowerShellFirstLineStressSet.PadWith(string.Empty, _PROSE_UNIT)),
			Shape("one-repeated-character", new string(_REPEATED_CHARACTER, LINE_LENGTH_IN_CHARACTERS)),
			Shape("shebang-then-one-repeated-character", PowerShellFirstLineStressSet.PadWith(_SHEBANG_MARKER, _REPEATED_CHARACTER.ToString())),
			Shape("binary-like-noise", PowerShellFirstLineStressSet.NoiseLine(string.Empty, PowerShellFirstLineStressSet.NOISE_SEED, LINE_LENGTH_IN_CHARACTERS)),
			Shape("binary-like-noise-after-a-shebang-marker", PowerShellFirstLineStressSet.NoiseLine(_SHEBANG_MARKER, PowerShellFirstLineStressSet.NOISE_SEED, LINE_LENGTH_IN_CHARACTERS))
		];
	}

	/// <summary>The opening followed by the unit repeated until the line is as long as the ceiling allows once the tail is appended, then the tail: a line of exactly the ceiling that ends in the tail.</summary>
	private static string PadBeforeATail(string opening, string unit, string tail)
	{
		string head = PowerShellFirstLineStressSet.PadWith(opening, unit)[..(LINE_LENGTH_IN_CHARACTERS - tail.Length)];

		return head + tail;
	}

	private static FirstLineStressShape Shape(string name, string line)
	{
		return new FirstLineStressShape { Name = name, Line = line };
	}
}
