using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE BOUNDS OF THE WINDOW OF THE BEARER GUARD OF `JS-018`. An explicit Bearer value written within thirty lines of the match, before it or after it, each line read up to
/// 400 characters, silences the class: with 29 filler lines between the Bearer write and the environment header the class is silent, with 30 it reads; a filler line of 400
/// characters keeps the window and one of 401 breaks it. A Bearer fallback must have its operator on the same line as its literal, and an object literal that only holds the key is
/// not read by the class. Every script is invented with placeholder names.
/// </summary>
public sealed class JavaScriptGuardWindowTests
{
	private const string _LOW_CLASS = "JS-018";
	private const string _BEARER_WRITE = "await fetch(first, { headers: { Authorization: `Bearer ${token}` } });\n";
	private const string _ENVIRONMENT_HEADER = "await fetch(second, { headers: { Authorization: process.env.API_AUTH } });\n";
	private const string _FILLER_LINE = "const unrelated = compute();\n";
	private const int _MAXIMUM_LINE_LENGTH = 400;

	[Theory]
	[InlineData(29, false)]
	[InlineData(30, true)]
	public void ABearerWriteBeforeTheHeaderSilencesTheClassWithTwentyNineFillerLinesAndNotWithThirty(int fillerLines, bool classReads)
	{
		AssertReadsOnlyWhen(_BEARER_WRITE + Filler(fillerLines) + _ENVIRONMENT_HEADER, classReads);
	}

	[Theory]
	[InlineData(29, false)]
	[InlineData(30, true)]
	public void ABearerWriteAfterTheHeaderSilencesTheClassWithTwentyNineFillerLinesAndNotWithThirty(int fillerLines, bool classReads)
	{
		AssertReadsOnlyWhen(_ENVIRONMENT_HEADER + Filler(fillerLines) + _BEARER_WRITE, classReads);
	}

	[Theory]
	[InlineData(_MAXIMUM_LINE_LENGTH, false)]
	[InlineData(_MAXIMUM_LINE_LENGTH + 1, true)]
	public void AFillerLineOfFourHundredCharactersKeepsTheWindowAndOneOfFourHundredAndOneBreaksItBeforeTheHeader(int lineLength, bool classReads)
	{
		AssertReadsOnlyWhen(_BEARER_WRITE + LongLine(lineLength) + _ENVIRONMENT_HEADER, classReads);
	}

	[Theory]
	[InlineData(_MAXIMUM_LINE_LENGTH, false)]
	[InlineData(_MAXIMUM_LINE_LENGTH + 1, true)]
	public void AFillerLineOfFourHundredCharactersKeepsTheWindowAndOneOfFourHundredAndOneBreaksItAfterTheHeader(int lineLength, bool classReads)
	{
		AssertReadsOnlyWhen(_ENVIRONMENT_HEADER + LongLine(lineLength) + _BEARER_WRITE, classReads);
	}

	[Theory]
	[InlineData("const first = process.env.OLD_AUTH || 'Bearer exampleToken';\n", false)]
	[InlineData("const first = process.env.OLD_AUTH ||\n\t'Bearer exampleToken';\n", true)]
	public void ABearerFallbackSilencesTheClassOnlyWhenItsOperatorIsOnTheSameLineAsItsLiteral(string declaration, bool classReads)
	{
		AssertReadsOnlyWhen(declaration + _ENVIRONMENT_HEADER, classReads);
	}

	[Theory]
	[InlineData("const o = { Authorization: process.env.X };\n")]
	[InlineData("const o = { authorization: Bun.env.X };\n")]
	public void AnObjectLiteralThatOnlyHoldsTheKeyIsNotReadByTheClass(string source)
	{
		Assert.DoesNotContain(new JavaScriptRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _LOW_CLASS);
	}

	[Fact]
	public void TheSameHeaderInsideACarrierIsReadSoTheKeyOnlyObjectIsTheOnlyDifference()
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize("const o = { headers: { Authorization: process.env.X } };\n");

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _LOW_CLASS && construct.Level == ConfidenceLevel.Low);
	}

	private static void AssertReadsOnlyWhen(string source, bool classReads)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);
		bool read = constructs.Any(construct => construct.PatternClassIdentifier == _LOW_CLASS && construct.Level == ConfidenceLevel.Low);

		Assert.True(read == classReads, $"expected the class to read: {classReads}; got [{string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"))}] on [{source}]");
	}

	private static string Filler(int lines)
	{
		return string.Concat(Enumerable.Repeat(_FILLER_LINE, lines));
	}

	private static string LongLine(int length)
	{
		return "//" + new string('x', length - 2) + "\n";
	}
}
