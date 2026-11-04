using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// TWO READINGS OF THE `CSharp` CATALOG. (1) The port, the argument before the scheme of the four-argument overload of the
/// credential cache's <c>Add</c>, never decides the class: an integer literal, a named constant, a cast or a short expression is read exactly as a variable is.
/// (2) A mail client or a proxy created with an object initializer that does not itself set the credential, and given its credential by a later statement, is the
/// non-HTTP sink's class at Low, which subsumes the plain credential's class: the credential does not go to the migrating host. Each reading has its positive
/// fixtures and its neighbour controls, through the shipped analyzer and the real recognition engine. Every script is invented with placeholder names.
/// </summary>
public sealed class CSharpPortAndInitializerSinkTests
{
	private const string _CACHE_CLASS = "CS-003";
	private const string _SINK_CLASS = "CS-019";
	private const string _PLAIN_CREDENTIAL_CLASS = "CS-008";
	private const string _USING_NETWORK = "using System.Net;\n";

	// ---- The port: any single argument ----

	[Theory]
	[InlineData("cache.Add(\"files.example.com\", 443, \"Basic\", new NetworkCredential(\"placeholder-user\", \"placeholder-pass\"));")]
	[InlineData("cache.Add(\"files.example.com\", 443, \"Basic\", new System.Net.NetworkCredential(\"placeholder-user\", \"placeholder-pass\"));")]
	[InlineData("cache.Add(host, 0x1BB, \"Basic\", new System.Net.NetworkCredential(user, password));")]
	[InlineData("cache.Add(host, 8_080, \"Basic\", new NetworkCredential(user, password));")]
	[InlineData("cache.Add(host, 443u, \"Basic\", new NetworkCredential(user, password));")]
	[InlineData("cache.Add(host, Defaults.Port, \"Basic\", new NetworkCredential(user, password));")]
	[InlineData("cache.Add(host, (int)portText, \"Basic\", new NetworkCredential(user, password));")]
	[InlineData("cache.Add(host, basePort + 1, \"Basic\", new NetworkCredential(user, password));")]
	[InlineData("cache.Add(host, GetPort(host), \"Basic\", new NetworkCredential(user, password));")]
	[InlineData("cache.Add(host, port, \"Basic\", new NetworkCredential(user, password));")]
	[InlineData("cache.Add(host, \"443\", \"Basic\", new NetworkCredential(user, password));")]
	public void AFourArgumentAddIsReadAtHighWhateverTheSingleArgumentOfThePortIs(string statement)
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrap(statement));

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _CACHE_CLASS && construct.Level == ConfidenceLevel.High);
	}

	[Fact]
	public void ALiteralCredentialOfAFourArgumentAddWithANumericPortRaisesTheLiteralCredentialSignal()
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(
			Wrap("cache.Add(\"files.example.com\", 443, \"Basic\", new NetworkCredential(\"placeholder-user\", \"placeholder-pass\"));"));

		Assert.True(constructs.Single(construct => construct.PatternClassIdentifier == _CACHE_CLASS).LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("cache.Add(host, 443, \"Digest\", new NetworkCredential(user, password));")]
	[InlineData("cache.Add(host, 443, \"Basic\", credential);")]
	[InlineData("cache.Add(host, 443, scheme, credential);")]
	[InlineData("cache.Add(host, 443, extra, \"Basic\", new NetworkCredential(user, password));")]
	[InlineData("cache.Add(host, 443, \"Basic\", new NetworkCredential(user, password), more);")]
	public void ANeighbourOfTheFourArgumentAddThatTheCatalogKeepsOutIsStillNotRead(string statement)
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrap(statement));

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _CACHE_CLASS);
	}

	[Fact]
	public void AThreeArgumentAddIsStillReadAtHigh()
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(
			Wrap("cache.Add(new Uri(\"https://files.example.com\"), \"Basic\", new NetworkCredential(user, password));"));

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _CACHE_CLASS && construct.Level == ConfidenceLevel.High);
	}

	[Fact]
	public void AListOrADictionaryAddWithTwoArgumentsIsNotRead()
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrap("entries.Add(key, value);\nnames.Add(host, 443);"));

		Assert.Empty(constructs);
	}

	// ---- The sink created with an initializer that does not carry the credential ----

	[Theory]
	[InlineData("var client = new SmtpClient(\"smtp.example.com\", 587) { EnableSsl = true };\nclient.Credentials = new NetworkCredential(relayUser, relaySecret);")]
	[InlineData("var proxy = new WebProxy(\"proxy.example\", 3128) { BypassProxyOnLocal = true, UseDefaultCredentials = false };\nproxy.Credentials = new NetworkCredential(proxyUser, proxySecret);")]
	[InlineData("SmtpClient mailer = new(\"smtp.example.com\", 587) { EnableSsl = true };\nmailer.Credentials = new NetworkCredential(relayUser, relaySecret);")]
	[InlineData("var client = new SmtpClient { Host = host, EnableSsl = true };\nclient.Credentials = new NetworkCredential(relayUser, relaySecret);")]
	[InlineData("var client = new System.Net.Mail.SmtpClient(host, 25) { EnableSsl = true };\nclient.Credentials = new NetworkCredential(relayUser, relaySecret);")]
	[InlineData("var client = new SmtpClient(host, 587)\n{\n\tEnableSsl = true,\n\tTimeout = 5000\n};\nclient.Credentials = new NetworkCredential(relayUser, relaySecret);")]
	[InlineData("var client = new SmtpClient(host, 587) { EnableSsl = true };\nclient.Timeout = 5000;\nclient.Credentials = new NetworkCredential(relayUser, relaySecret);")]
	public void ASinkCreatedWithAnInitializerThatSetsOtherPropertiesAndBoundLaterIsTheSinkClassAtLowAndTheCredentialIsNotReportedBeside(string statements)
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrap(statements));

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _SINK_CLASS && construct.Level == ConfidenceLevel.Low);
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _PLAIN_CREDENTIAL_CLASS);
	}

	[Theory]
	[InlineData("var client = new SmtpClient(host, 587);\nclient.Credentials = new NetworkCredential(relayUser, relaySecret);")]
	[InlineData("var client = new SmtpClient(host, 587) { Credentials = new NetworkCredential(relayUser, relaySecret) };")]
	[InlineData("var client = new SmtpClient(host, 587) { EnableSsl = true, Credentials = new NetworkCredential(relayUser, relaySecret) };")]
	public void TheFormsThatWereAlreadyTheSinkClassAreStillTheSinkClassAtLow(string statements)
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrap(statements));

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _SINK_CLASS && construct.Level == ConfidenceLevel.Low);
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _PLAIN_CREDENTIAL_CLASS);
	}

	[Theory]
	[InlineData("var client = new SmtpClient(host, 587) { EnableSsl = true };\nclient.Credentials = CredentialCache.DefaultNetworkCredentials;")]
	[InlineData("var client = new SmtpClient(host, 587) { EnableSsl = true, UseDefaultCredentials = true };")]
	[InlineData("var client = new SmtpClient(host, 587) { EnableSsl = true };\nclient.Credentials = credential;")]
	public void ASinkWithAnInitializerAndNoConstructedCredentialIsNotRead(string statements)
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrap(statements));

		Assert.Empty(constructs);
	}

	[Fact]
	public void AnInitializerThatItselfSetsTheCredentialToSomethingElseAndABindingLaterIsNotTheSinkClass()
	{
		const string statements = "var client = new SmtpClient(host, 587) { Credentials = CredentialCache.DefaultNetworkCredentials };\nclient.Credentials = new NetworkCredential(relayUser, relaySecret);";

		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrap(statements));

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _SINK_CLASS);
		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _PLAIN_CREDENTIAL_CLASS);
	}

	[Theory]
	[InlineData("var handler = new HttpClientHandler { UseCookies = true };\nhandler.Credentials = new NetworkCredential(user, password);")]
	[InlineData("var client = new OtherServiceClient(host) { Retries = 3 };\nclient.Credentials = new NetworkCredential(user, password);")]
	public void ASinkOutsideTheClosedListWithAnInitializerAndALaterBindingStaysThePlainCredentialsClassAndNeverTheSinkClass(string statements)
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrap(statements));

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _SINK_CLASS);
		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _PLAIN_CREDENTIAL_CLASS);
	}

	private static string Wrap(string statements)
	{
		return _USING_NETWORK + "class Worker\n{\n\tvoid Run()\n\t{\n" + statements + "\n\t}\n}\n";
	}
}
