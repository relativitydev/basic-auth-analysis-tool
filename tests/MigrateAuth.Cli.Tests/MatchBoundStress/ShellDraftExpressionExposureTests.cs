using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.Catalog;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE EXPOSURE SCAN OF THE `Shell` DRAFT EXPRESSIONS: the defect it looks for is a lazy bounded loop wrapped in an optional group, which makes the source-generated matcher
/// backtrack past its bound. Each expression's pattern is read for its lazy bounded loops and its optional groups, and none may be inside the other. The table is written where
/// <see cref="REPORT_PATH_VARIABLE_NAME"/> points.
/// </summary>
public sealed class ShellDraftExpressionExposureTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_EXPOSURE_REPORT_SHELL";

	private readonly ITestOutputHelper _output;

	public ShellDraftExpressionExposureTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoShellDraftExpressionCarriesALazyBoundedLoopInsideAnOptionalGroupAndTheExposureTableIsRecorded()
	{
		StringBuilder table = new();
		List<string> exposed = [];

		table.AppendLine("expression\tlazy_bounded_loops\tbounds\toptional_groups\tlazy_loops_in_optional_group\treading");

		foreach ((string identifier, Regex expression) in ShellDraftExpressions.LexicalExpressions.Concat(ShellDraftExpressions.MatchBoundExpressions))
		{
			PatternShape shape = PatternShape.Analyze(expression.ToString());

			table.Append(identifier).Append('\t').Append(shape.LazyBoundedLoops.Count).Append('\t').Append(string.Join(' ', shape.LazyBoundedLoops.Select(loop => loop.Bounds)))
				.Append('\t').Append(shape.OptionalGroupCount).Append('\t').Append(shape.LazyLoopsInOptionalGroups).Append('\t').AppendLine(shape.LazyLoopsInOptionalGroups == 0
					? (shape.LazyBoundedLoops.Count == 0 ? "no lazy bounded loop" : "every lazy bounded loop is in a required position")
					: "EXPOSED");

			if (shape.LazyLoopsInOptionalGroups > 0)
			{
				exposed.Add(identifier);
			}
		}

		_output.WriteLine(table.ToString());

		string? path = Environment.GetEnvironmentVariable(REPORT_PATH_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(path))
		{
			File.WriteAllText(path, table.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}

		Assert.True(exposed.Count == 0, $"Exposed to the lazy-window-in-an-optional-group defect: {string.Join(", ", exposed)}");
	}
}
