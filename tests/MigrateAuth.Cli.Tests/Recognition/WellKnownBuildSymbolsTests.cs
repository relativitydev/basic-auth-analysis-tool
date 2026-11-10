using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE WELL-KNOWN BUILD SYMBOLS OF THE `CSharp` PROFILE: the debug, release and trace
/// symbols exactly; every symbol that starts with the framework prefix; and the platform words with an optional tail of
/// digits and underscores, with or without the greater-or-equal suffix. Every other symbol is not well known. The marker
/// syntax of a profile's words is exercised on an invented rule as well.
/// </summary>
public sealed class WellKnownBuildSymbolsTests
{
	private static readonly string[] _PLATFORM_WORDS = ["WINDOWS", "ANDROID", "IOS", "MACCATALYST", "MACOS", "TVOS", "BROWSER"];

	private static readonly WellKnownBuildSymbols _CSHARP_SYMBOLS = WellKnownBuildSymbols.From(
		new CSharpRecognitionHarness().Analyzer.LexicalProfile.ConditionalCompilation!);

	[Theory]
	[InlineData("DEBUG")]
	[InlineData("RELEASE")]
	[InlineData("TRACE")]
	public void TheDebugReleaseAndTraceSymbolsAreWellKnownExactly(string symbol)
	{
		Assert.True(_CSHARP_SYMBOLS.Contains(symbol));
	}

	[Theory]
	[InlineData("DEBUG2")]
	[InlineData("DEBUG_")]
	[InlineData("DEBUGGER")]
	[InlineData("RELEASE_OR_GREATER")]
	[InlineData("TRACE_ON")]
	[InlineData("Debug")]
	[InlineData("debug")]
	public void ALookalikeOfTheExactSymbolsIsNotWellKnown(string symbol)
	{
		Assert.False(_CSHARP_SYMBOLS.Contains(symbol));
	}

	[Theory]
	[InlineData("NET")]
	[InlineData("NET6_0")]
	[InlineData("NET6_0_OR_GREATER")]
	[InlineData("NET48")]
	[InlineData("NET462_OR_GREATER")]
	[InlineData("NETCOREAPP")]
	[InlineData("NETCOREAPP3_1")]
	[InlineData("NETCOREAPP3_1_OR_GREATER")]
	[InlineData("NETSTANDARD2_0")]
	[InlineData("NETSTANDARD2_1_OR_GREATER")]
	[InlineData("NETFRAMEWORK")]
	[InlineData("NETUNLISTEDFAMILY")]
	[InlineData("NET_FUTURE_8")]
	public void EverySymbolThatStartsWithTheFrameworkPrefixIsWellKnown(string symbol)
	{
		Assert.True(_CSHARP_SYMBOLS.Contains(symbol));
	}

	[Theory]
	[InlineData("NE")]
	[InlineData("ANET6_0")]
	[InlineData("net6_0")]
	[InlineData("Net6_0")]
	public void ASymbolThatDoesNotStartWithTheUpperCaseFrameworkPrefixIsNotWellKnown(string symbol)
	{
		Assert.False(_CSHARP_SYMBOLS.Contains(symbol));
	}

	public static TheoryData<string> EveryPlatformWordWithEveryKindOfTail()
	{
		string[] tails = ["", "7", "10_0", "15_0", "10_0_19041_0", "_OR_GREATER", "7_OR_GREATER", "10_0_OR_GREATER", "_1"];
		TheoryData<string> symbols = [];

		foreach (string word in _PLATFORM_WORDS)
		{
			foreach (string tail in tails)
			{
				symbols.Add($"{word}{tail}");
			}
		}

		return symbols;
	}

	[Theory]
	[MemberData(nameof(EveryPlatformWordWithEveryKindOfTail))]
	public void EveryPlatformWordIsWellKnownWithOrWithoutAVersionTailAndTheGreaterOrEqualSuffix(string symbol)
	{
		Assert.True(_CSHARP_SYMBOLS.Contains(symbol));
	}

	public static TheoryData<string> LookalikesOfThePlatformWords()
	{
		string[] suffixes = ["X", "S", "7A", "_OR_LESS", "7_OR_GREATERX", "_OR_GREATER7", "-1"];
		TheoryData<string> symbols = [];

		foreach (string word in _PLATFORM_WORDS)
		{
			foreach (string suffix in suffixes)
			{
				symbols.Add($"{word}{suffix}");
			}

			symbols.Add(word.ToLowerInvariant());
			symbols.Add($"X{word}");
		}

		return symbols;
	}

	[Theory]
	[MemberData(nameof(LookalikesOfThePlatformWords))]
	public void ALookalikeOfAPlatformWordIsNotWellKnown(string symbol)
	{
		Assert.False(_CSHARP_SYMBOLS.Contains(symbol));
	}

	[Theory]
	[InlineData("FEAT")]
	[InlineData("FEAT_OFF")]
	[InlineData("DEBUGX")]
	[InlineData("ENABLE_DEBUG")]
	[InlineData("")]
	public void AnyOtherSymbolIsNotWellKnown(string symbol)
	{
		Assert.False(_CSHARP_SYMBOLS.Contains(symbol));
	}

	[Fact]
	public void AWordWithNoMarkerNamesExactlyThatSymbol()
	{
		WellKnownBuildSymbols symbols = Symbols("ALPHA");

		Assert.True(symbols.Contains("ALPHA"));
		Assert.False(symbols.Contains("ALPHA1"));
		Assert.False(symbols.Contains("ALPH"));
	}

	[Fact]
	public void AWordWithThePrefixMarkerNamesEverySymbolThatStartsWithIt()
	{
		WellKnownBuildSymbols symbols = Symbols($"ALPHA{ConditionalCompilationRule.PREFIX_WILDCARD_MARKER}");

		Assert.True(symbols.Contains("ALPHA"));
		Assert.True(symbols.Contains("ALPHABETA_1"));
		Assert.False(symbols.Contains("BETA"));
	}

	[Fact]
	public void AWordWithTheVersionTailMarkerNamesTheWordAndItsTailOnly()
	{
		WellKnownBuildSymbols symbols = Symbols($"ALPHA{ConditionalCompilationRule.VERSION_TAIL_MARKER}");

		Assert.True(symbols.Contains("ALPHA"));
		Assert.True(symbols.Contains("ALPHA3_1_OR_GREATER"));
		Assert.False(symbols.Contains("ALPHABETA"));
	}

	[Fact]
	public void ARuleWithNoWordsMakesNoSymbolWellKnown()
	{
		Assert.False(Symbols().Contains("DEBUG"));
		Assert.False(WellKnownBuildSymbols.None.Contains("DEBUG"));
	}

	private static WellKnownBuildSymbols Symbols(params string[] words)
	{
		return WellKnownBuildSymbols.From(new ConditionalCompilationRule { DirectiveOpening = "#", WellKnownSymbolWords = [.. words] });
	}
}
