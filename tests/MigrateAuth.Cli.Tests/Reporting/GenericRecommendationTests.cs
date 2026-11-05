using System.Reflection;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// THE GENERIC RECOMMENDATION: THE FOUR POINTS IT MUST MAKE, AND THE ONE THING IT MUST NOT.
///
/// The four points are asserted by content rather than by an exact comparison, because the text is
/// prose and an exact comparison would turn every rewording into a failing test while proving
/// nothing about what the text says. What is pinned is the ideas the text must carry - RelativityOne,
/// the bearer token, where the credentials live, and the statement that nothing here was
/// characterized - plus their order, since they are stated as an order.
///
/// The thing it must not do has more tests than the things it must, and deliberately. One failure
/// mode matters most: a generic recommendation that reads like a specific one
/// invites a developer to apply a transformation the tool never verified applies. The cheapest way
/// to produce that failure is to reach for a field the finding happens to carry - the pattern
/// class, the level, the language - and write it into a sentence. So those are tested for absence.
/// </summary>
public sealed class GenericRecommendationTests
{
	private const string _RELATIVITY_ONE = "RelativityOne";
	private const string _NO_LONGER_DOCUMENTED = "no longer documented";
	private const string _BEARER_TOKEN = "bearer token";
	private const string _CLIENT_CREDENTIALS_GRANT = "client-credentials grant";
	private const string _PROCESS_ENVIRONMENT_OR_SECRET_STORE = "process environment or a secret store";
	private const string _NEVER_IN_SOURCE = "never in source";
	private const string _DID_NOT_RECOGNIZE = "did not recognize this construct";
	private const string _NOT_CHARACTERIZED = "has not been characterized";

	[Theory]
	[InlineData(_RELATIVITY_ONE)]
	[InlineData(_NO_LONGER_DOCUMENTED)]
	[InlineData(_BEARER_TOKEN)]
	[InlineData("identity endpoint")]
	[InlineData(_CLIENT_CREDENTIALS_GRANT)]
	[InlineData(_PROCESS_ENVIRONMENT_OR_SECRET_STORE)]
	[InlineData(_NEVER_IN_SOURCE)]
	[InlineData(_DID_NOT_RECOGNIZE)]
	[InlineData(_NOT_CHARACTERIZED)]
	public void TheTextMakesEachOfTheFourPoints(string expectedIdea)
	{
		string text = Describe();

		Assert.Contains(expectedIdea, text, StringComparison.Ordinal);
	}

	/// <summary>
	/// The four points are given as a sequence, and the sequence is the argument: the two facts
	/// that can be stated without knowing the construct come first, and the admission that the
	/// construct is unknown comes last, where it governs everything said before it. Reversed, the
	/// text would open by disclaiming itself and then read as advice anyway.
	/// </summary>
	[Fact]
	public void TheFourPointsAreMadeInTheirDeclaredOrder()
	{
		string text = Describe();

		int whatTheFileDoes = text.IndexOf(_RELATIVITY_ONE, StringComparison.Ordinal);
		int whereItIsGoing = text.IndexOf(_BEARER_TOKEN, StringComparison.Ordinal);
		int whereTheCredentialsLive = text.IndexOf(_PROCESS_ENVIRONMENT_OR_SECRET_STORE, StringComparison.Ordinal);
		int whatWasNotCharacterized = text.IndexOf(_DID_NOT_RECOGNIZE, StringComparison.Ordinal);

		Assert.True(whatTheFileDoes < whereItIsGoing, "The file's current scheme is stated before the destination.");
		Assert.True(whereItIsGoing < whereTheCredentialsLive, "The destination is stated before the credentials.");
		Assert.True(
			whereTheCredentialsLive < whatWasNotCharacterized,
			"The admission that nothing was characterized comes last, where it governs the rest.");
	}

	/// <summary>
	/// The tone requirement, tested as the absence of the material that would break it. A sentence
	/// naming the pattern class, the level or the language reads as though the construct were
	/// understood well enough to advise on, which is the exact claim the fourth point withdraws. The
	/// provider is finding-free, so nothing about a finding can reach the text; the
	/// absence is still asserted for both flows' texts.
	/// </summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void TheTextNamesNeitherAPatternClassNorALevelNorALanguage(CredentialFlow flow)
	{
		string text = Describe(flow);

		foreach (string patternClassIdentifier in new[]
		{
			ReportHarness.UNTEMPLATED_PATTERN_CLASS,
			ReportHarness.TEMPLATED_PATTERN_CLASS,
			ReportHarness.SECOND_PATTERN_CLASS
		})
		{
			Assert.DoesNotContain(patternClassIdentifier, text, StringComparison.OrdinalIgnoreCase);
		}

		foreach (ConfidenceLevel level in new[] { ConfidenceLevel.High, ConfidenceLevel.Medium })
		{
			Assert.DoesNotContain(level.ToString(), text, StringComparison.OrdinalIgnoreCase);
		}

		foreach (AssessedLanguage language in AssessedLanguages.AllInDeclaredOrder)
		{
			Assert.DoesNotContain(AssessedLanguages.CanonicalName(language), text, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// THE TEXT CARRIES NO PART OF A FINDING.
	///
	/// The provider substitutes neither the relative path nor the line number:
	/// <see cref="IGenericRecommendationProvider.DescribeFor"/> receives neither a finding nor an
	/// <see cref="EmissionDestination"/> and so could not escape a path the way
	/// <c>RecommendationRenderer.Substitute</c> does. What follows is that the text is the same for
	/// every finding, and that no part of any analyzed path can appear in it - which is the data
	/// invariant reduced to something a single assertion can check.
	/// </summary>
	[Fact]
	public void TheTextIsTheSameForEveryFindingAndCarriesNoPathOrLine()
	{
		GenericRecommendationProvider provider = new();

		string first = provider.DescribeFor(CredentialFlow.ClientCredentials);
		string second = RenderNarrative(
			ReportHarness.FindingAt(2, "other/second-file.cs", 4321, ReportHarness.UNTEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium));

		Assert.Same(first, GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT);
		Assert.Equal(first, second);
		Assert.DoesNotContain("first-file", first, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("second-file", second, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("4321", second, StringComparison.Ordinal);
	}

	/// <summary>
	/// The recommendation is deterministic, with no call to an external service
	/// and no AI provider. It is trivially true of a constant, so what is asserted is the STRUCTURE
	/// that keeps it true - a type with no instance field has nowhere to hold a client, and a type
	/// with only a parameterless constructor has no way to be handed one. A future edit that
	/// introduced either would fail here rather than at the first network call in front of a client.
	/// </summary>
	[Fact]
	public void TheProviderHoldsNoDependencyThroughWhichAnyCallCouldBeMade()
	{
		Type providerType = typeof(GenericRecommendationProvider);

		FieldInfo[] instanceFields = providerType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		ConstructorInfo[] constructors = providerType.GetConstructors();

		Assert.Empty(instanceFields);
		Assert.Single(constructors);
		Assert.Empty(constructors[0].GetParameters());
	}

	/// <summary>
	/// The three routes of <see cref="RecommendationRenderer"/> with the real provider in place. The
	/// middle case is the generic route; the other two are asserted to show the provider leaves them
	/// unchanged.
	/// </summary>
	[Fact]
	public void AHighFindingWithNoTemplateRendersTheGenericTextThroughTheRealRenderer()
	{
		RenderedRecommendation rendered = Render(
			ReportHarness.FindingAt(1, "source.cs", 5, ReportHarness.UNTEMPLATED_PATTERN_CLASS, ConfidenceLevel.High));

		Assert.Equal(RecommendationKind.Generic, rendered.Kind);
		Assert.Equal(GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT, rendered.Narrative);
		Assert.Equal(string.Empty, rendered.GenericSourceForm);
		Assert.Equal(string.Empty, rendered.TargetFormUnderTokenAuthentication);
		Assert.Equal(string.Empty, rendered.CredentialProvenance);
		Assert.Equal(string.Empty, rendered.VerificationNote);
	}

	[Fact]
	public void AHighFindingWithATemplateStillRendersTheTemplateAndNotTheGenericText()
	{
		RenderedRecommendation rendered = Render(
			ReportHarness.FindingAt(1, "source.cs", 5, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High));

		Assert.Equal(RecommendationKind.FromTemplate, rendered.Kind);
		Assert.Equal(ReportHarness.TEMPLATE_SOURCE_FORM, rendered.GenericSourceForm);
		Assert.Equal(string.Empty, rendered.Narrative);
	}

	/// <summary>A Low finding is routed to a human, and the generic text is not its answer.</summary>
	[Fact]
	public void ALowFindingStillGoesToManualReviewAndNotToTheGenericText()
	{
		RenderedRecommendation rendered = Render(
			ReportHarness.FindingAt(1, "source.cs", 5, ReportHarness.UNTEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low));

		Assert.Equal(RecommendationKind.ManualReview, rendered.Kind);
		Assert.StartsWith(RecommendationRenderer.MANUAL_REVIEW_LEAD_TEXT, rendered.Narrative, StringComparison.Ordinal);
		Assert.EndsWith("in Part C for C# of the Migration Documentation.", rendered.Narrative, StringComparison.Ordinal);
	}

	/// <summary>
	/// The fifth piece rides on the literal-credential signal and not on the route, and the
	/// generic route is where it matters most: the embedded-secret signal comes
	/// from the zone mask rather than from recognizing the construct, so a construct we failed to
	/// characterize can still carry a published secret.
	/// </summary>
	[Fact]
	public void TheSecurityPieceStillRidesAlongsideTheGenericText()
	{
		RenderedRecommendation rendered = Render(
			ReportHarness.FindingAt(
				1,
				"source.cs",
				5,
				ReportHarness.UNTEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.High,
				DestinationCertainty.Established,
				literalCredentialSignal: true));

		Assert.Equal(RecommendationKind.Generic, rendered.Kind);
		Assert.Equal(GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT, rendered.Narrative);
		Assert.Equal(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, rendered.SecurityNote);
	}

	private static string Describe(CredentialFlow flow = CredentialFlow.ClientCredentials)
	{
		return new GenericRecommendationProvider().DescribeFor(flow);
	}

	private static string RenderNarrative(Finding finding)
	{
		return Render(finding).Narrative;
	}

	private static RenderedRecommendation Render(Finding finding)
	{
		return new RecommendationRenderer(ReportHarness.CatalogWithCSharpOnly(), new GenericRecommendationProvider())
			.Render(finding, EmissionDestination.Markdown);
	}
}
