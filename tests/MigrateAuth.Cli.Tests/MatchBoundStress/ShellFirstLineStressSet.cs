using System.Collections.Immutable;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE FIRST LINES OF THE `Shell` LEXICAL-BUDGET STRESS SET. The `Shell` catalog declares all three expressions the lexical budget covers: the two here-document openings
/// (measured on the scan of <see cref="ShellMatchBoundStressSet"/>) and the first-line detection of an extensionless script, a shebang naming one of the four shells
/// (`EX-SH-04`), run once per extensionless file on the file's first line.
///
/// Every shape is a first line of exactly the reader's line-length ceiling, because that is the longest first line the pipeline can ever hand to the expression: a shebang
/// padded with blanks, with a long argument list, an interpreter path of many segments, flags with no interpreter, near misses of the interpreter names, prose, one repeated
/// character, unterminated openers and the noise a binary-like file decodes to. None is built to make the expression backtrack catastrophically. Every shape is
/// deterministic and invented.
/// </summary>
internal static class ShellFirstLineStressSet
{
	internal const int LINE_LENGTH_IN_CHARACTERS = PowerShellFirstLineStressSet.LINE_LENGTH_IN_CHARACTERS;

	internal const string VALID_SHAPE_NAME_PREFIX = "valid-shebang";

	private const string _SHEBANG_MARKER = "#!";
	private const string _VALID_SHEBANG = "#!/bin/bash";
	private const string _BLANK = " ";
	private const string _SEGMENT = "/seg";
	private const string _EMPTY_SEGMENT = "/";
	private const string _ENV_FLAGS_PREFIX = "#!/usr/bin/env ";
	private const string _ENV_FLAG = "-S ";
	private const string _NEAR_MISS_PREFIX = "#!/usr/bin/";
	private const string _NEAR_MISS_INTERPRETER = "bashx ";
	private const string _SHEBANG_WITH_ARGUMENTS_UNIT = " -e -u -o pipefail";
	private const string _PROSE_UNIT = "this line is ordinary prose and not a shebang at all ";
	private const string _UNTERMINATED_OPENER_UNIT = "$( ";
	private const char _REPEATED_CHARACTER = 'a';

	/// <summary>The shapes, in a fixed order. Each name is what the timing table reports.</summary>
	internal static ImmutableArray<FirstLineStressShape> Build()
	{
		return
		[
			Shape($"{VALID_SHAPE_NAME_PREFIX}-padded-with-blanks", PowerShellFirstLineStressSet.PadWith(_VALID_SHEBANG, _BLANK)),
			Shape($"{VALID_SHAPE_NAME_PREFIX}-with-a-long-argument-list", PowerShellFirstLineStressSet.PadWith(_VALID_SHEBANG, _SHEBANG_WITH_ARGUMENTS_UNIT)),
			Shape("shebang-then-blanks-and-no-interpreter", PowerShellFirstLineStressSet.PadWith(_SHEBANG_MARKER, _BLANK)),
			Shape("shebang-with-an-interpreter-path-of-many-segments", PowerShellFirstLineStressSet.PadWith(_SHEBANG_MARKER, _SEGMENT)),
			Shape("shebang-with-a-path-of-empty-segments", PowerShellFirstLineStressSet.PadWith(_SHEBANG_MARKER, _EMPTY_SEGMENT)),
			Shape("shebang-through-env-with-many-flags-and-no-interpreter", PowerShellFirstLineStressSet.PadWith(_ENV_FLAGS_PREFIX, _ENV_FLAG)),
			Shape("shebang-with-near-miss-interpreter-names", PowerShellFirstLineStressSet.PadWith(_NEAR_MISS_PREFIX, _NEAR_MISS_INTERPRETER)),
			Shape("ordinary-prose-not-a-shebang", PowerShellFirstLineStressSet.PadWith(string.Empty, _PROSE_UNIT)),
			Shape("one-repeated-character", new string(_REPEATED_CHARACTER, LINE_LENGTH_IN_CHARACTERS)),
			Shape("shebang-then-one-repeated-character", PowerShellFirstLineStressSet.PadWith(_SHEBANG_MARKER, _REPEATED_CHARACTER.ToString())),
			Shape("unterminated-openers-with-no-closing", PowerShellFirstLineStressSet.PadWith(string.Empty, _UNTERMINATED_OPENER_UNIT)),
			Shape("binary-like-noise", PowerShellFirstLineStressSet.NoiseLine(string.Empty, PowerShellFirstLineStressSet.NOISE_SEED, LINE_LENGTH_IN_CHARACTERS)),
			Shape("binary-like-noise-after-a-shebang-marker", PowerShellFirstLineStressSet.NoiseLine(_SHEBANG_MARKER, PowerShellFirstLineStressSet.NOISE_SEED, LINE_LENGTH_IN_CHARACTERS))
		];
	}

	private static FirstLineStressShape Shape(string name, string line)
	{
		return new FirstLineStressShape { Name = name, Line = line };
	}
}
