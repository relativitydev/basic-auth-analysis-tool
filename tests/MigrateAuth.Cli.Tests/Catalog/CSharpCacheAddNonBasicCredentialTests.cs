using System.Linq;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// CS-008 on a credential constructed inside a credential-cache <c>Add</c>. With a scheme other than
/// <c>Basic</c> nothing is reported; with <c>Basic</c> CS-003 reports it at High and CS-008 does not report it
/// separately; a credential held in a variable built elsewhere is unaffected. Every fixture is invented.
/// </summary>
public sealed class CSharpCacheAddNonBasicCredentialTests
{
	private const string _CS_003 = "CS-003";
	private const string _CS_008 = "CS-008";
	private const int _EXPECTED_CS_008_FINDINGS_OUTSIDE_A_CACHE_ADD = 2;

	[Theory]
	[InlineData("cache.Add(endpoint, \"Digest\", new NetworkCredential(userName, password));")]
	[InlineData("cache.Add(new Uri(widgetUrl), \"Negotiate\", new NetworkCredential(userName, password));")]
	[InlineData("cache.Add(widgetHost, 443, \"Digest\", new NetworkCredential(userName, password));")]
	[InlineData("cache.Add(settings[\"Host\"], \"Kerberos\", new System.Net.NetworkCredential(userName, password));")]
	public void ANonBasicSchemeWithAConstructedCredentialIsNotReported(string source)
	{
		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("cache.Add(endpoint, \"Basic\", new NetworkCredential(userName, password));")]
	[InlineData("cache.Add(new Uri(widgetUrl), \"Basic\", new NetworkCredential(userName, password));")]
	[InlineData("cache.Add(widgetHost, 443, \"Basic\", new NetworkCredential(userName, password));")]
	public void ABasicSchemeWithAConstructedCredentialIsReportedOnlyAsCS003AtHigh(string source)
	{
		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(_CS_003, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	[Theory]
	[InlineData("cache.Add(endpoint, \"Digest\", credential);")]
	[InlineData("var credential = new NetworkCredential(userName, password);\ncache.Add(endpoint, \"Digest\", credential);")]
	public void ACredentialHeldInAVariableIsNotAffectedByTheScheme(string source)
	{
		bool constructedElsewhere = source.Contains("new NetworkCredential");

		bool reported = new CSharpRecognitionHarness().Recognize(source).Any(construct => construct.PatternClassIdentifier == _CS_008);

		Assert.Equal(constructedElsewhere, reported);
	}

	[Fact]
	public void ACredentialConstructedOutsideAnAddStillReadsAsCS008()
	{
		const string source = "var credential = new NetworkCredential(userName, password);\nother.Add(widget, new NetworkCredential(userName, password));";

		Assert.Equal(_EXPECTED_CS_008_FINDINGS_OUTSIDE_A_CACHE_ADD,new CSharpRecognitionHarness().Recognize(source).Count(construct => construct.PatternClassIdentifier == _CS_008));
	}
}
