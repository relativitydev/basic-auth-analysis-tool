using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// Nested openers inside a bracketed interpolation hole. The hole closes at its
/// closing token only at depth zero; a token inside a nested literal or comment is skipped by the existing
/// recursion; and an opener that never balances falls back to the first closing, records one anomaly and
/// leaves the rest of the file masked. The profile is test-local and shaped like the second language.
/// </summary>
public sealed class InterpolationNestingTests
{
	private const string _BALANCED_SOURCE = "$a = \"id $(Get-Item (Join-Path (Get-Root) 'x')) end\"\n$b = 2\n";
	private const string _UNBALANCED_SOURCE = "$a = \"id $(Get-Item (Join-Path 1 ) end\"\n$b = \"ok\"\n$c = 3\n";
	private const string _UNBALANCED_REPEAT = "$a = \"id $(Get-Item (Join-Path 1 ) end\"\n";
	private const int _REPEATED_UNBALANCED_LINES = 40;

	[Fact]
	public void NestedOpenersCloseTheHoleAtDepthZero()
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(_BALANCED_SOURCE, countsNestedParentheses: true);

		ZoneAssertions.SpansZone(mask, _BALANCED_SOURCE, "$(", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, _BALANCED_SOURCE, "Get-Item (Join-Path (Get-Root) ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _BALANCED_SOURCE, "x", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _BALANCED_SOURCE, " end", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _BALANCED_SOURCE, "$b = 2", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
	}

	[Fact]
	public void WithoutANestingOpeningTheFirstClosingEndsTheHole()
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(_BALANCED_SOURCE, countsNestedParentheses: false);

		ZoneAssertions.SpansZone(mask, _BALANCED_SOURCE, "Get-Item (Join-Path (Get-Root", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _BALANCED_SOURCE, ")", LexicalZone.Delimiter, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _BALANCED_SOURCE, " 'x')) end", LexicalZone.StringBody);
	}

	[Fact]
	public void AClosingOrOpeningInsideANestedLiteralOrCommentIsNotCounted()
	{
		const string source = "$a = \"v $(Get-Item ')' (Get-Root '(') <# ) #> # (\n) tail\"\n$b = 1\n";

		ZoneMask mask = PowerShellShapedProfile.Scan(source, countsNestedParentheses: true);

		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(source.IndexOf("')'", StringComparison.Ordinal) + 1));
		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(source.IndexOf("'('", StringComparison.Ordinal) + 1));
		ZoneAssertions.SpansZone(mask, source, "<# ) #>", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "# (", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "$b = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(0, mask.UnbalancedNestingCount);
	}

	[Fact]
	public void AnUnbalancedOpenerFallsBackToTheFirstClosingAndCountsOneAnomaly()
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(_UNBALANCED_SOURCE, countsNestedParentheses: true);

		Assert.Equal(1, mask.UnbalancedNestingCount);
		Assert.Equal(LexicalAnomalyReason.UnbalancedInterpolationNesting, mask.AnomalyReason);
		Assert.True(mask.IsCompletelyValid);
		Assert.False(mask.WasAbandonedMidFile);
		ZoneAssertions.SpansZone(mask, _UNBALANCED_SOURCE, "Get-Item (Join-Path 1 ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _UNBALANCED_SOURCE, " end", LexicalZone.StringBody);
	}

	[Fact]
	public void TheRestOfTheFileAfterARecoveryIsMaskedAsIfNestingWereNotDeclared()
	{
		ZoneMask recovered = PowerShellShapedProfile.Scan(_UNBALANCED_SOURCE, countsNestedParentheses: true);
		ZoneMask plain = PowerShellShapedProfile.Scan(_UNBALANCED_SOURCE, countsNestedParentheses: false);

		Assert.True(plain.Zones.SequenceEqual(recovered.Zones), ZoneAssertions.Render(recovered, _UNBALANCED_SOURCE));
		ZoneAssertions.SpansZone(recovered, _UNBALANCED_SOURCE, "$c = 3", LexicalZone.Code);
		ZoneAssertions.SpansZone(recovered, _UNBALANCED_SOURCE, "ok", LexicalZone.StringBody);
	}

	[Fact]
	public void AHoleThatNeverCountedANestedOpenerKeepsTheAbandonPolicy()
	{
		const string source = "$a = \"id $(Get-Item 1 end\n";

		ZoneMask mask = PowerShellShapedProfile.Scan(source, countsNestedParentheses: true);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.True(mask.WasAbandonedMidFile);
		Assert.Equal(0, mask.UnbalancedNestingCount);
	}

	[Fact]
	public void RecoveriesPerFileAreBoundedSoManyUnbalancedHolesDoNotMultiplyTheScan()
	{
		StringBuilder source = new();

		for (int line = 0; line < _REPEATED_UNBALANCED_LINES; line++)
		{
			source.Append(_UNBALANCED_REPEAT);
		}

		ZoneMask mask = PowerShellShapedProfile.Scan(source.ToString(), countsNestedParentheses: true);

		Assert.InRange(mask.UnbalancedNestingCount, 1, LexicalEnginePolicy.MAXIMUM_NESTING_RECOVERIES_PER_FILE);
		Assert.True(mask.IsCompletelyValid);
	}
}
