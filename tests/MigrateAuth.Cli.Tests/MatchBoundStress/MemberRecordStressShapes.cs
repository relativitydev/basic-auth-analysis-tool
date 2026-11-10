using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE STRESS FILE OF THE MEMBER PASS, written as test data for the cost gate's window and never run by the normal suite: a file at the size ceiling in which the word <c>authorization</c>
/// stands on one line in every five, the other four being ordinary statements, so that the member-declaration expression is tried and succeeds or fails at every fifth line and the
/// second dictionary of the file symbol table grows to its widest. The authorization line cycles through the forms the member expression reads and the ones it refuses: an object-literal member
/// filled from each of the five environment forms, a property assignment, the receiver <c>this</c>, a receiver chain at its eight-segment bound, a literal, a call, a flag key, an environment name
/// that holds no credential word, a quoted key, a key that is not an identifier and a use of the property as an <c>Authorization</c> header value. Two shapes are built: one in which every line names
/// the same member (the dictionary holds one list of many thousand records) and one in which each line names a member of its own (the dictionary holds many thousand names).
/// The structural test in <see cref="MemberRecordStressShapeTests"/> reads the text without any clock; this file is test data for the cost gate window.
/// </summary>
internal static class MemberRecordStressShapes
{
	internal const string SAME_NAME_SHAPE_NAME = "member-records-one-line-in-five-same-name";

	internal const string DISTINCT_NAMES_SHAPE_NAME = "member-records-one-line-in-five-distinct-names";

	/// <summary>One line in this many carries the word.</summary>
	internal const int LINES_PER_AUTHORIZATION_LINE = 5;

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;
	private const string _SAME_MEMBER = "serviceAuthorization";
	private const string _DISTINCT_MEMBER_FORMAT = "service{0:D5}Authorization";

	private static readonly string[] _ORDINARY_LINES =
	[
		"const total = first + second;\n",
		"function step(a, b) { return a * b + offset; }\n",
		"if (value > limit) { retry(count + 1); }\n",
		"items.forEach((item) => handle(item.name, item.size));\n"
	];

	/// <summary>The authorization lines, each with the member name where the line takes one.</summary>
	private static readonly string[] _AUTHORIZATION_LINE_FORMATS =
	[
		"\t{0}: process.env.SERVICE_AUTH_HEADER,\n",
		"settings.{0} = process.env['SERVICE_TOKEN'];\n",
		"\tthis.{0} = import.meta.env.VITE_SERVICE_SECRET;\n",
		"\t{0}: Bun.env.SERVICE_AUTH_HEADER || 'neutral',\n",
		"settings.{0} = Deno.env.get('SERVICE_AUTH_HEADER') ?? 'Basic dXNlcjpwYXNz';\n",
		"a.b.c.d.e.f.g.h.{0} = loadValue();\n",
		"\t{0}: 'static',\n",
		"\t'{0}': process.env.SERVICE_AUTH_HEADER,\n",
		"\t{0}: process.env.SERVICE_HOST,\n",
		"\trequireAuthorization: process.env.SERVICE_AUTH_FLAG,\n",
		"\t'x-authorization': process.env.SERVICE_AUTH_HEADER,\n",
		"await fetch(url, {{ headers: {{ Authorization: settings.{0} }} }});\n"
	];

	/// <summary>The target size in characters, as the other stress sets state it: under the file-size ceiling with a margin.</summary>
	internal static int TargetSizeInCharacters { get; } = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;

	/// <summary>The file in which every authorization line names the same member.</summary>
	internal static string SameNameText()
	{
		return Fill(_ => _SAME_MEMBER);
	}

	/// <summary>The file in which each authorization line names a member of its own.</summary>
	internal static string DistinctNamesText()
	{
		return Fill(number => string.Format(CultureInfo.InvariantCulture, _DISTINCT_MEMBER_FORMAT, number));
	}

	private static string Fill(Func<int, string> memberNameOf)
	{
		StringBuilder builder = new(TargetSizeInCharacters);
		int authorizationLine = 0;

		while (builder.Length + LongestBlock() < TargetSizeInCharacters)
		{
			foreach (string ordinary in _ORDINARY_LINES)
			{
				builder.Append(ordinary);
			}

			string format = _AUTHORIZATION_LINE_FORMATS[authorizationLine % _AUTHORIZATION_LINE_FORMATS.Length];

			builder.Append(string.Format(CultureInfo.InvariantCulture, format, memberNameOf(authorizationLine)));
			authorizationLine++;
		}

		return builder.ToString();
	}

	private static int LongestBlock()
	{
		return _ORDINARY_LINES.Sum(line => line.Length) + _AUTHORIZATION_LINE_FORMATS.Max(format => format.Length) + _SAME_MEMBER.Length + _DISTINCT_MEMBER_FORMAT.Length;
	}
}
