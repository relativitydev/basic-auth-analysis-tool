using System.Collections.Immutable;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE STRUCTURE OF THE MEMBER PASS'S STRESS FILE, asserted without any clock (the measurement is not run here): the file is legal for the reader, the word stands on exactly one line in five, the
/// authorization lines carry every form the member expression reads and the ones it refuses, the shapes are registered in the `JavaScript` and `TypeScript` stress sets, and the member expression
/// reads records from the file's opening lines (so the pass really does work on it).
/// </summary>
public sealed class MemberRecordStressShapeTests
{
	private const string _WORD = "authorization";
	private const string _MEMBER = "serviceAuthorization";
	private const int _OPENING_LINES_READ = 600;
	private const char _LINE_FEED = '\n';

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void EveryShapeIsLegalForTheReaderAndFillsTheFileToJustUnderTheCeiling(bool distinctNames)
	{
		string text = TextOf(distinctNames);

		Assert.True(Encoding.UTF8.GetByteCount(text) <= ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES);
		Assert.True(Encoding.UTF8.GetByteCount(text) >= ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES / 2, "The file is far under the size ceiling.");
		Assert.DoesNotContain('\r', text);
		Assert.All(text.Split(_LINE_FEED), line => Assert.True(line.Length <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TheWordStandsOnExactlyOneLineInFive(bool distinctNames)
	{
		string[] lines = TextOf(distinctNames).TrimEnd(_LINE_FEED).Split(_LINE_FEED);
		int[] withTheWord = [.. lines.Select((line, index) => (line, index)).Where(entry => entry.line.Contains(_WORD, StringComparison.OrdinalIgnoreCase)).Select(entry => entry.index)];

		Assert.True(withTheWord.Length > 1000, $"{withTheWord.Length} lines carry the word.");
		Assert.All(withTheWord, index => Assert.Equal(MemberRecordStressShapes.LINES_PER_AUTHORIZATION_LINE - 1, index % MemberRecordStressShapes.LINES_PER_AUTHORIZATION_LINE));
		Assert.Equal(withTheWord.Length, lines.Length / MemberRecordStressShapes.LINES_PER_AUTHORIZATION_LINE);
	}

	[Fact]
	public void TheSameNameShapeNamesOneMemberAndTheDistinctNamesShapeNamesManyAndBothHoldTheFormsTheExpressionReadsAndRefuses()
	{
		string same = MemberRecordStressShapes.SameNameText();
		string distinct = MemberRecordStressShapes.DistinctNamesText();

		Assert.Contains(_MEMBER, same, StringComparison.Ordinal);
		Assert.DoesNotContain("service00001Authorization", same, StringComparison.Ordinal);
		Assert.Contains("service00001Authorization", distinct, StringComparison.Ordinal);

		foreach (string text in new[] { same, distinct })
		{
			Assert.Contains("process.env.SERVICE_AUTH_HEADER", text, StringComparison.Ordinal);
			Assert.Contains("process.env['SERVICE_TOKEN']", text, StringComparison.Ordinal);
			Assert.Contains("import.meta.env.VITE_SERVICE_SECRET", text, StringComparison.Ordinal);
			Assert.Contains("Bun.env.SERVICE_AUTH_HEADER", text, StringComparison.Ordinal);
			Assert.Contains("Deno.env.get('SERVICE_AUTH_HEADER')", text, StringComparison.Ordinal);
			Assert.Contains("this.", text, StringComparison.Ordinal);
			Assert.Contains("a.b.c.d.e.f.g.h.", text, StringComparison.Ordinal);
			Assert.Contains("requireAuthorization", text, StringComparison.Ordinal);
			Assert.Contains("process.env.SERVICE_HOST", text, StringComparison.Ordinal);
			Assert.Contains("'x-authorization'", text, StringComparison.Ordinal);
			Assert.Contains("Authorization: settings.", text, StringComparison.Ordinal);
		}
	}

	[Fact]
	public void TheShapesAreRegisteredInTheJavaScriptAndTheTypeScriptStressSetsUnderUniqueNames()
	{
		ImmutableArray<MatchBoundStressShape> javaScript = JavaScriptMatchBoundStressSet.Build();
		ImmutableArray<MatchBoundStressShape> typeScript = TypeScriptShippedCostGateStressSet.Build();

		foreach (string shapeName in new[] { MemberRecordStressShapes.SAME_NAME_SHAPE_NAME, MemberRecordStressShapes.DISTINCT_NAMES_SHAPE_NAME })
		{
			Assert.Single(javaScript, shape => shape.Name == $"js-{shapeName}");
			Assert.Single(typeScript, shape => shape.Name == $"{TypeScriptShippedCostGateStressSet.SHAPE_NAME_PREFIX}{shapeName}");
		}

		Assert.Equal(javaScript.Length, javaScript.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
		Assert.Equal(typeScript.Length, typeScript.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void TheMemberExpressionReadsRecordsFromTheOpeningLinesOfTheFile()
	{
		LexicalProfile profile = JavaScriptRegisteredExpressions.Analyzer.LexicalProfile;
		string opening = $"{string.Join(_LINE_FEED, MemberRecordStressShapes.DistinctNamesText().Split(_LINE_FEED).Take(_OPENING_LINES_READ))}{_LINE_FEED}";
		FileSymbolTable table = FileSymbolTable.Collect(profile, opening, new LexicalScanner(profile).Scan(opening));

		Assert.Equal(1, table.MemberRecordCountOf("service00000Authorization"));
		Assert.True(table.TryResolveMemberRecordUnambiguously("service00000Authorization", out ResolvedSymbol record));
		Assert.Equal(SymbolSourceKind.Environment, record.SourceKind);
		Assert.Equal(1, table.MemberRecordCountOf("service00001Authorization"));
	}

	private static string TextOf(bool distinctNames)
	{
		return distinctNames ? MemberRecordStressShapes.DistinctNamesText() : MemberRecordStressShapes.SameNameText();
	}
}
