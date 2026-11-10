using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The tool recommends two flows and no others: no `CSharp`, `PowerShell`, `VBNet`, `Shell` or `JavaScript` recommendation, template
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

	/// <summary>Every (class, flow) `VBNet` recommendation and both generic texts, composed exactly as the report composes them.</summary>
	[Fact]
	public void NoComposedVBNetRecommendationNamesAnOutOfScopeGrant()
	{
		foreach (string text in VBNetRenderedTexts.AllComposedTexts())
		{
			AssertNoOutOfScopeGrant(text);
		}
	}

	/// <summary>The `VBNet` analyzer's own declared fallback texts, before the shared engine composes them with the OAuth2 Client block.</summary>
	[Fact]
	public void NoDeclaredVBNetFallbackTextNamesAnOutOfScopeGrant()
	{
		foreach (CredentialFlow flow in VBNetRenderedTexts.Flows)
		{
			AssertNoOutOfScopeGrant(VBNetRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]);
		}
	}

	/// <summary>Every (class, flow) `Shell` recommendation and both generic texts, composed exactly as the report composes them.</summary>
	[Fact]
	public void NoComposedShellRecommendationNamesAnOutOfScopeGrant()
	{
		foreach (string text in ShellRenderedTexts.AllComposedTexts())
		{
			AssertNoOutOfScopeGrant(text);
		}
	}

	/// <summary>The `Shell` analyzer's own declared fallback texts, before the shared engine composes them with the OAuth2 Client block.</summary>
	[Fact]
	public void NoDeclaredShellFallbackTextNamesAnOutOfScopeGrant()
	{
		foreach (CredentialFlow flow in ShellRenderedTexts.Flows)
		{
			AssertNoOutOfScopeGrant(ShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]);
		}
	}

	/// <summary>The composed `Shell` texts are not empty, so the two checks above cannot pass over nothing.</summary>
	[Fact]
	public void TheShellTextsTheGrantChecksReadAreNotEmpty()
	{
		Assert.Equal((ShellRenderedTexts.TemplatedPatternClasses.Length + 1) * ShellRenderedTexts.Flows.Length, ShellRenderedTexts.AllComposedTexts().Length);
		Assert.All(ShellRenderedTexts.AllComposedTexts(), text => Assert.False(string.IsNullOrWhiteSpace(text)));
	}

	/// <summary>Every (class, flow) `JavaScript` recommendation and both generic texts, composed exactly as the report composes them.</summary>
	[Fact]
	public void NoComposedJavaScriptRecommendationNamesAnOutOfScopeGrant()
	{
		foreach (string text in JavaScriptRenderedTexts.AllComposedTexts())
		{
			AssertNoOutOfScopeGrant(text);
		}
	}

	/// <summary>The `JavaScript` analyzer's own declared fallback texts, before the shared engine composes them with the OAuth2 Client block.</summary>
	[Fact]
	public void NoDeclaredJavaScriptFallbackTextNamesAnOutOfScopeGrant()
	{
		foreach (CredentialFlow flow in JavaScriptRenderedTexts.Flows)
		{
			AssertNoOutOfScopeGrant(JavaScriptRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow]);
		}
	}

	/// <summary>The composed `JavaScript` texts are not empty, so the two checks above cannot pass over nothing.</summary>
	[Fact]
	public void TheJavaScriptTextsTheGrantChecksReadAreNotEmpty()
	{
		Assert.Equal((JavaScriptRenderedTexts.TemplatedPatternClasses.Length + 1) * JavaScriptRenderedTexts.Flows.Length, JavaScriptRenderedTexts.AllComposedTexts().Length);
		Assert.All(JavaScriptRenderedTexts.AllComposedTexts(), text => Assert.False(string.IsNullOrWhiteSpace(text)));
	}

	private static void AssertNoOutOfScopeGrant(string text)
	{
		foreach (string marker in _outOfScopeGrantMarkers)
		{
			Assert.DoesNotContain(marker, text, StringComparison.Ordinal);
		}
	}
}
