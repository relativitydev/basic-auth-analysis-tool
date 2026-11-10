using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// SUBSUMPTION IGNORES THE LEVEL, AND A MATCH THAT STRADDLES A DIRECTIVE TAKES ITS REGION STATUS FROM ITS OWN ANCHOR.
/// One test per rule of the `CSharp` subsumption relation (CS-002 over CS-001, and CS-003, CS-007 and CS-019 over CS-008, and
/// CS-019 over CS-016), through the real embedded analyzer. Where the two anchors lie in different statements the
/// directive sits between them, so one anchor is in an inactive region and the other is not; the subsumed match is dropped
/// either way and the survivor's level follows the survivor's anchor. Where the two anchors share a statement, a directive
/// cannot sit between them (the expressions read the statement contiguously), so the statement as a whole is placed inside
/// and outside an inactive region. Every name is invented; <c>FEAT_OFF</c> is never defined, so its block is inactive.
/// </summary>
public sealed class ConditionalCompilationSubsumptionTests
{
	private const string _NEWLINE = "\n";
	private const string _SUBSUMED_CS_001 = "CS-001";
	private const string _SUBSUMED_CS_008 = "CS-008";
	private const string _SUBSUMED_CS_016 = "CS-016";
	private const string _CS_002 = "CS-002";
	private const string _CS_003 = "CS-003";
	private const string _CS_007 = "CS-007";
	private const string _CS_019 = "CS-019";
	private const string _INACTIVE_BLOCK_OPENING = "#if FEAT_OFF";
	private const string _END_OF_GROUP = "#endif";
	private const string _CS_002_STATEMENT = "var client = new RSAPIClient(rsapiUri, new UsernamePasswordCredentials(userName, password));";
	private const string _CS_003_STATEMENT = "cache.Add(new Uri(instanceUrl), \"Basic\", new NetworkCredential(userName, password));";
	private const string _HANDLER_DECLARATION = "HttpClientHandler handler = new();";
	private const string _HANDLER_CREDENTIAL_WRITE = "handler.Credentials = new NetworkCredential(userName, password);";
	private const string _CREDENTIAL_CREATION = "var credential = new NetworkCredential(userName, password);";
	private const string _DIRECTORY_SINK = "var entry = new DirectoryEntry(path, credential);";
	private const string _ASSEMBLED_USER_NAME = "var credential = new NetworkCredential() { UserName = userName };";
	private const string _ASSEMBLED_PASSWORD = "credential.Password = password;";
	private const string _MAIL_SINK = "var mailer = new SmtpClient(host) { Credentials = credential };";

	private static readonly LowConfidenceReasons _NON_HTTP_SINK_WHEN_INACTIVE =
		LowConfidenceReasons.PatternClass | LowConfidenceReasons.InactivePreprocessorRegion;

	[Fact]
	public void TheRsapiClientOverTheCredentialTypeKeepsOneMatchAtTheClassLevelWhenTheStatementIsLive()
	{
		RecognizedConstruct survivor = Assert.Single(Recognize(Lines(_INACTIVE_BLOCK_OPENING, "var unrelated = 1;", _END_OF_GROUP, _CS_002_STATEMENT)));

		Assert.Equal(_CS_002, survivor.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, survivor.Level);
		Assert.Equal(LowConfidenceReasons.None, survivor.LowReasons);
	}

	[Fact]
	public void TheRsapiClientOverTheCredentialTypeKeepsOneMatchAtLowWhenTheStatementIsInactiveAndDropsTheSubsumedClass()
	{
		ImmutableArray<RecognizedConstruct> survivors = Recognize(Lines(_INACTIVE_BLOCK_OPENING, _CS_002_STATEMENT, _END_OF_GROUP));

		RecognizedConstruct survivor = Assert.Single(survivors);
		Assert.Equal(_CS_002, survivor.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, survivor.Level);
		Assert.Equal(LowConfidenceReasons.InactivePreprocessorRegion, survivor.LowReasons);
		Assert.DoesNotContain(survivors, construct => construct.PatternClassIdentifier == _SUBSUMED_CS_001);
	}

	[Fact]
	public void TheCredentialCacheOverTheBareCredentialKeepsOneMatchAtLowWhenTheStatementIsInactiveAndDropsTheSubsumedClass()
	{
		ImmutableArray<RecognizedConstruct> survivors = Recognize(Lines(_INACTIVE_BLOCK_OPENING, _CS_003_STATEMENT, _END_OF_GROUP));

		RecognizedConstruct survivor = Assert.Single(survivors);
		Assert.Equal(_CS_003, survivor.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, survivor.Level);
		Assert.Equal(LowConfidenceReasons.InactivePreprocessorRegion, survivor.LowReasons);
		Assert.DoesNotContain(survivors, construct => construct.PatternClassIdentifier == _SUBSUMED_CS_008);
	}

	[Fact]
	public void TheCredentialCacheOverTheBareCredentialKeepsOneMatchAtTheClassLevelWhenTheStatementIsLive()
	{
		RecognizedConstruct survivor = Assert.Single(Recognize(Lines(_INACTIVE_BLOCK_OPENING, "var unrelated = 1;", _END_OF_GROUP, _CS_003_STATEMENT)));

		Assert.Equal(_CS_003, survivor.PatternClassIdentifier);
		Assert.Equal(LowConfidenceReasons.None, survivor.LowReasons);
	}

	[Fact]
	public void TheHandlerOverTheBareCredentialWithADirectiveBetweenTheStatementsReadsTheSurvivorFromItsInactiveAnchor()
	{
		ImmutableArray<RecognizedConstruct> survivors = Recognize(
			Lines(_HANDLER_DECLARATION, _INACTIVE_BLOCK_OPENING, _HANDLER_CREDENTIAL_WRITE, _END_OF_GROUP));

		RecognizedConstruct survivor = Assert.Single(survivors);
		Assert.Equal(_CS_007, survivor.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, survivor.Level);
		Assert.Equal(LowConfidenceReasons.InactivePreprocessorRegion, survivor.LowReasons);
		Assert.DoesNotContain(survivors, construct => construct.PatternClassIdentifier == _SUBSUMED_CS_008);
	}

	[Fact]
	public void TheHandlerOverTheBareCredentialWithADirectiveBetweenTheStatementsReadsTheSurvivorFromItsLiveAnchor()
	{
		ImmutableArray<RecognizedConstruct> survivors = Recognize(
			Lines(_INACTIVE_BLOCK_OPENING, _HANDLER_DECLARATION, _END_OF_GROUP, _HANDLER_CREDENTIAL_WRITE));

		RecognizedConstruct survivor = Assert.Single(survivors);
		Assert.Equal(_CS_007, survivor.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, survivor.Level);
		Assert.Equal(LowConfidenceReasons.None, survivor.LowReasons);
		Assert.DoesNotContain(survivors, construct => construct.PatternClassIdentifier == _SUBSUMED_CS_008);
	}

	[Fact]
	public void TheNonHttpSinkOverTheBareCredentialWithADirectiveBetweenTheAnchorsReadsTheSurvivorFromItsInactiveAnchor()
	{
		ImmutableArray<RecognizedConstruct> survivors = Recognize(
			Lines(_CREDENTIAL_CREATION, _INACTIVE_BLOCK_OPENING, _DIRECTORY_SINK, _END_OF_GROUP));

		RecognizedConstruct survivor = Assert.Single(survivors);
		Assert.Equal(_CS_019, survivor.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, survivor.Level);
		Assert.Equal(_NON_HTTP_SINK_WHEN_INACTIVE, survivor.LowReasons);
		Assert.DoesNotContain(survivors, construct => construct.PatternClassIdentifier == _SUBSUMED_CS_008);
	}

	[Fact]
	public void TheNonHttpSinkOverTheBareCredentialWithADirectiveBetweenTheAnchorsReadsTheSurvivorFromItsLiveAnchor()
	{
		ImmutableArray<RecognizedConstruct> survivors = Recognize(
			Lines(_INACTIVE_BLOCK_OPENING, _CREDENTIAL_CREATION, _END_OF_GROUP, _DIRECTORY_SINK));

		RecognizedConstruct survivor = Assert.Single(survivors);
		Assert.Equal(_CS_019, survivor.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, survivor.Level);
		Assert.Equal(LowConfidenceReasons.PatternClass, survivor.LowReasons);
		Assert.DoesNotContain(survivors, construct => construct.PatternClassIdentifier == _SUBSUMED_CS_008);
	}

	[Fact]
	public void TheNonHttpSinkOverTheAssembledCredentialWithADirectiveBetweenTheAnchorsReadsTheSurvivorFromItsInactiveAnchor()
	{
		ImmutableArray<RecognizedConstruct> survivors = Recognize(
			Lines(_ASSEMBLED_USER_NAME, _ASSEMBLED_PASSWORD, _INACTIVE_BLOCK_OPENING, _MAIL_SINK, _END_OF_GROUP));

		RecognizedConstruct survivor = Assert.Single(survivors);
		Assert.Equal(_CS_019, survivor.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, survivor.Level);
		Assert.Equal(_NON_HTTP_SINK_WHEN_INACTIVE, survivor.LowReasons);
		Assert.DoesNotContain(survivors, construct => construct.PatternClassIdentifier == _SUBSUMED_CS_016);
	}

	[Fact]
	public void TheNonHttpSinkOverTheAssembledCredentialWithADirectiveBetweenTheAnchorsReadsTheSurvivorFromItsLiveAnchor()
	{
		ImmutableArray<RecognizedConstruct> survivors = Recognize(
			Lines(_INACTIVE_BLOCK_OPENING, _ASSEMBLED_USER_NAME, _ASSEMBLED_PASSWORD, _END_OF_GROUP, _MAIL_SINK));

		RecognizedConstruct survivor = Assert.Single(survivors);
		Assert.Equal(_CS_019, survivor.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, survivor.Level);
		Assert.Equal(LowConfidenceReasons.PatternClass, survivor.LowReasons);
		Assert.DoesNotContain(survivors, construct => construct.PatternClassIdentifier == _SUBSUMED_CS_016);
	}

	private static string Lines(params string[] lines)
	{
		return $"{string.Join(_NEWLINE, lines)}{_NEWLINE}";
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(string source)
	{
		return new CSharpRecognitionHarness().Recognize(source);
	}
}
