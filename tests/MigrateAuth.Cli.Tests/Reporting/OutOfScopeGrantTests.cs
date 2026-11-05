using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The tool recommends two flows and no others: no `CSharp` recommendation, template
/// or generic, under either flow, names a grant the tool does not recommend.
/// </summary>
public sealed class OutOfScopeGrantTests
{
	private static readonly string[] _outOfScopeGrantMarkers =
	[
		"grant_type=password",
		"response_type=token",
		"Implicit",
		"Resource Owner",
		"Device Code"
	];

	/// <summary>Every (class, flow) recommendation and both generic texts, composed exactly as the report composes them.</summary>
	[Fact]
	public void NoComposedCSharpRecommendationNamesAnOutOfScopeGrant()
	{
		foreach (string text in CSharpRenderedTexts.AllComposedTexts())
		{
			AssertNoOutOfScopeGrant(text);
		}
	}

	/// <summary>The analyzer's own declared fallback texts, before the shared engine composes them with the OAuth2 Client block.</summary>
	[Fact]
	public void NoDeclaredCSharpFallbackTextNamesAnOutOfScopeGrant()
	{
		foreach (CredentialFlow flow in CSharpRenderedTexts.Flows)
		{
			AssertNoOutOfScopeGrant(CSharpRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]);
		}
	}

	/// <summary>Every (class, flow) `PowerShell` recommendation and both generic texts, composed exactly as the report composes them.</summary>
	[Fact]
	public void NoComposedPowerShellRecommendationNamesAnOutOfScopeGrant()
	{
		foreach (string text in PowerShellRenderedTexts.AllComposedTexts())
		{
			AssertNoOutOfScopeGrant(text);
		}
	}

	/// <summary>The `PowerShell` analyzer's own declared fallback texts, before the shared engine composes them with the OAuth2 Client block.</summary>
	[Fact]
	public void NoDeclaredPowerShellFallbackTextNamesAnOutOfScopeGrant()
	{
		foreach (CredentialFlow flow in PowerShellRenderedTexts.Flows)
		{
			AssertNoOutOfScopeGrant(PowerShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]);
		}
	}

	private static void AssertNoOutOfScopeGrant(string text)
	{
		foreach (string marker in _outOfScopeGrantMarkers)
		{
			Assert.DoesNotContain(marker, text, StringComparison.Ordinal);
		}
	}
}
