using System.Collections.Immutable;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// Every engine-owned place that points a reader at the Secret Management material names
/// it through <see cref="SecretManagementPointer.PART_B_NAME"/>: "Secret Management (Part B of the
/// Migration Documentation)". None says the material is delivered separately from the report, none
/// calls it a guide, and none carries a location - the tool is offline and a citation that guessed a
/// path or a URL would be a promise this build cannot keep.
///
/// The engine-owned texts are the literal-credential security note, both generic texts, the Analysis
/// stage's secret warning and both OAuth2 Client blocks. The `CSharp` language-owned texts are asserted
/// too: every composed recommendation of every class under both flows, and both generic
/// fallback texts, never say the material is delivered separately or call it a guide, and every High
/// or Medium recommendation names Part B. The no-location check does not apply to them: a composed
/// text legitimately carries the loopback redirect URL.
/// </summary>
public sealed class SecretManagementGuideCitationTests
{
	private const string _EXACT_NAME = "Secret Management (Part B of the Migration Documentation)";
	private const string _RETIRED_DELIVERY_PHRASE = "delivered separately from this report";
	private const string _RETIRED_GUIDE_PHRASE = "Secret Management guide";

	/// <summary>Sixteen classes and one generic text, for each of the two flows.</summary>
	private const int _EXPECTED_COMPOSED_TEXT_COUNT = 34;

	/// <summary>Eleven classes and one generic text, for each of the two flows.</summary>
	private const int _EXPECTED_POWERSHELL_COMPOSED_TEXT_COUNT = 24;

	private static readonly string[] _citations =
	[
		RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT,
		GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT,
		GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT,
		AnalyzeConsoleReport.LITERAL_CREDENTIAL_WARNING_MESSAGE_FORMAT
	];

	[Fact]
	public void ThePointerConstantIsTheExactPartBName()
	{
		Assert.Equal(_EXACT_NAME, SecretManagementPointer.PART_B_NAME);
	}

	[Theory]
	[MemberData(nameof(Citations))]
	public void EveryCitationNamesPartBOfTheMigrationDocumentation(string citation)
	{
		Assert.Contains("Secret Management", citation, StringComparison.Ordinal);
		Assert.Contains("Part B of the Migration Documentation", citation, StringComparison.Ordinal);
		Assert.Contains(_EXACT_NAME, citation, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(Citations))]
	public void NoCitationSaysDeliveredSeparatelyOrCallsItAGuide(string citation)
	{
		Assert.DoesNotContain(_RETIRED_DELIVERY_PHRASE, citation, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(_RETIRED_GUIDE_PHRASE, citation, StringComparison.OrdinalIgnoreCase);
	}

	[Theory]
	[MemberData(nameof(Citations))]
	public void NoCitationPromisesALocation(string citation)
	{
		Assert.DoesNotContain("http://", citation, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("https://", citation, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("www.", citation, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("hosted at", citation, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>No composed `CSharp` recommendation and no generic fallback text says the material is delivered separately or calls it a guide.</summary>
	[Fact]
	public void NoComposedCSharpTextSaysDeliveredSeparatelyOrCallsItAGuide()
	{
		ImmutableArray<string> texts = CSharpRenderedTexts.AllComposedTexts();

		Assert.Equal(_EXPECTED_COMPOSED_TEXT_COUNT, texts.Length);

		foreach (string text in texts)
		{
			Assert.DoesNotContain(_RETIRED_DELIVERY_PHRASE, text, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain(_RETIRED_GUIDE_PHRASE, text, StringComparison.OrdinalIgnoreCase);
		}

		foreach (CredentialFlow flow in CSharpRenderedTexts.Flows)
		{
			string fallback = CSharpRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.DoesNotContain(_RETIRED_DELIVERY_PHRASE, fallback, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain(_RETIRED_GUIDE_PHRASE, fallback, StringComparison.OrdinalIgnoreCase);
		}

		Assert.DoesNotContain(_RETIRED_DELIVERY_PHRASE, RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(_RETIRED_GUIDE_PHRASE, RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Every High and Medium `CSharp` recommendation, template or generic, names Part B.</summary>
	[Fact]
	public void EveryComposedCSharpTextNamesPartBOfTheMigrationDocumentation()
	{
		Assert.All(
			CSharpRenderedTexts.AllComposedTexts(),
			text => Assert.Contains(_EXACT_NAME, text, StringComparison.Ordinal));
	}

	/// <summary>No composed `PowerShell` recommendation and no generic fallback text says the material is delivered separately or calls it a guide.</summary>
	[Fact]
	public void NoComposedPowerShellTextSaysDeliveredSeparatelyOrCallsItAGuide()
	{
		ImmutableArray<string> texts = PowerShellRenderedTexts.AllComposedTexts();

		Assert.Equal(_EXPECTED_POWERSHELL_COMPOSED_TEXT_COUNT, texts.Length);

		foreach (CredentialFlow flow in PowerShellRenderedTexts.Flows)
		{
			string fallback = PowerShellRenderedTexts.Analyzer().CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow![flow];

			Assert.DoesNotContain(_RETIRED_DELIVERY_PHRASE, fallback, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain(_RETIRED_GUIDE_PHRASE, fallback, StringComparison.OrdinalIgnoreCase);
			Assert.Contains(_EXACT_NAME, fallback, StringComparison.Ordinal);
		}

		foreach (string text in texts)
		{
			Assert.DoesNotContain(_RETIRED_DELIVERY_PHRASE, text, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain(_RETIRED_GUIDE_PHRASE, text, StringComparison.OrdinalIgnoreCase);
		}
	}

	/// <summary>Every High and Medium `PowerShell` recommendation, template or generic, names Part B.</summary>
	[Fact]
	public void EveryComposedPowerShellTextNamesPartBOfTheMigrationDocumentation()
	{
		Assert.All(
			PowerShellRenderedTexts.AllComposedTexts(),
			text => Assert.Contains(_EXACT_NAME, text, StringComparison.Ordinal));
	}

	public static TheoryData<string> Citations()
	{
		TheoryData<string> data = [];

		foreach (string citation in _citations)
		{
			data.Add(citation);
		}

		return data;
	}
}
