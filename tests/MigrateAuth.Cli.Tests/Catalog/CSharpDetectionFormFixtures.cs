using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>One form the catalog says the `CSharp` analyzer reads: an invented source, the single class it must produce, its level and its flow decision.</summary>
internal sealed record CoveredForm(
	string Label,
	string PatternClass,
	ConfidenceLevel Level,
	string Source,
	FlowDecision ExpectedFlowDecision = FlowDecision.Default);

/// <summary>One form the catalog declares unsupported, or a near-miss of one it supports: an invented source and exactly what the analyzer produces for it.</summary>
internal sealed record ExpectedOutcomeForm(
	string Label,
	string Source,
	ImmutableArray<(string PatternClass, ConfidenceLevel Level)> Outcome);

/// <summary>
/// THE INVENTED FIXTURES OF THE CREATION-FORM MATRIX, THE NAMED-CONSTANT SCHEME, THE IDENTIFIER-ROOTED HALVES
/// AND THE OBJECT-INITIALIZER SINKS.
///
/// Three sets, each in one place so that every test that reads them reads the same text: the forms the catalog
/// says the analyzer reads (one fixture per form, firing the class the catalog names at the level it names), the
/// forms it declares unsupported (each producing exactly what the declaration says, which is often nothing and
/// sometimes the residue class of the credential), and the near-misses that must stay quiet. Every name is
/// invented; no fragment comes from any repository.
///
/// A covered fixture carries a word the file selection reads (a credential, an authorization or a Base64
/// token), because the pipeline tests run the file through the selection first, as a scan does.
/// </summary>
internal static class CSharpDetectionFormFixtures
{
	private const string _CS_001 = "CS-001";
	private const string _CS_002 = "CS-002";
	private const string _CS_003 = "CS-003";
	private const string _CS_004 = "CS-004";
	private const string _CS_005 = "CS-005";
	private const string _CS_007 = "CS-007";
	private const string _CS_008 = "CS-008";
	private const string _CS_009 = "CS-009";
	private const string _CS_012 = "CS-012";
	private const string _CS_013 = "CS-013";
	private const string _CS_015 = "CS-015";
	private const string _CS_016 = "CS-016";
	private const string _CS_018 = "CS-018";
	private const string _CS_019 = "CS-019";

	private const string _NEWLINE = "\n";

	/// <summary>The credential bound to the file-transfer request of the fixtures that carry it.</summary>
	private const string _FILE_TRANSFER_CREDENTIAL = "request.Credentials = new NetworkCredential(userName, password);";

	private const string _ENCODE_THE_PAIR = "var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(pair));";
	private const string _ASSIGN_THE_HEADER_VALUE = "client.DefaultRequestHeaders.Authorization = header;";

	/// <summary>A line that makes a password control a signal of the interactive flow, so the finding beside it targets the authorization code flow.</summary>
	private const string _INTERACTIVE_SIGNAL = "PasswordBox passwordBox = new PasswordBox();";

	/// <summary>A line that makes a credential read from the environment a signal of the fixed-credential flow.</summary>
	private const string _FIXED_SIGNAL = "var secretText = Environment.GetEnvironmentVariable(\"SERVICE_PASSWORD\");";

	/// <summary>Every form the catalog says the analyzer reads, one fixture per form.</summary>
	internal static readonly ImmutableArray<CoveredForm> CoveredForms =
	[
		.. CreationFormsOfTheCredentialTypes(),
		.. CreationFormsOfTheClientsAndHeaders(),
		.. CreationFormsOfTheHandlersAndCredentials(),
		.. NamedConstantSchemeForms(),
		.. HeaderEnumerationInterpolatedAndBoundedForms(),
		.. IdentifierRootedPairForms(),
		.. ObjectInitializerSinkForms(),
		.. FileTransferRequestSpellingForms(),
		.. ResidueAndEmptyHalfForms()
	];

	/// <summary>Every form the catalog declares unsupported, with what the analyzer produces for it.</summary>
	internal static readonly ImmutableArray<ExpectedOutcomeForm> DeclaredForms =
	[
		.. DeclaredAliasForms(),
		.. DeclaredTargetTypedForms(),
		.. DeclaredNamedConstantForms(),
		.. DeclaredInitializerAndSinkForms()
	];

	/// <summary>Near-misses of the covered forms: text that looks like one and must not become a finding of the class it resembles.</summary>
	internal static readonly ImmutableArray<ExpectedOutcomeForm> NearMisses =
	[
		.. NamedConstantNearMisses(),
		.. HeaderEnumerationBoundNearMisses(),
		.. FileTransferRequestNearMisses(),
		.. IdentifierRootedPairNearMisses(),
		.. CreationFormNearMisses()
	];

	internal static string Lines(params string[] lines)
	{
		return string.Join(_NEWLINE, lines);
	}

	private static CoveredForm Covered(string label, string patternClass, ConfidenceLevel level, string source, FlowDecision flowDecision = FlowDecision.Default)
	{
		return new CoveredForm(label, patternClass, level, source, flowDecision);
	}

	private static ExpectedOutcomeForm Outcome(string label, string source, params (string PatternClass, ConfidenceLevel Level)[] outcome)
	{
		return new ExpectedOutcomeForm(label, source, [.. outcome]);
	}

	// ------------------------------------------------------------------------------------------
	// The credential and authenticator types: CS-001, CS-002, CS-003, CS-004, CS-005.
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<CoveredForm> CreationFormsOfTheCredentialTypes()
	{
		return
		[
			Covered("cs001-fully-qualified", _CS_001, ConfidenceLevel.High, "Credentials credentials = new Sdk.Security.UsernamePasswordCredentials(userName, password);"),
			Covered("cs001-global", _CS_001, ConfidenceLevel.High, "Credentials credentials = new global::Sdk.Security.UsernamePasswordCredentials(userName, password);"),
			Covered("cs001-partial-namespace", _CS_001, ConfidenceLevel.High, "Credentials credentials = new Security.UsernamePasswordCredentials(userName, password);"),
			Covered("cs001-target-typed-declaration", _CS_001, ConfidenceLevel.High, "UsernamePasswordCredentials credentials = new(userName, password);"),
			Covered("cs001-qualified-target-typed-declaration", _CS_001, ConfidenceLevel.High, "Sdk.Security.UsernamePasswordCredentials credentials = new(userName, password);"),
			Covered("cs002-client-fully-qualified", _CS_002, ConfidenceLevel.High, "var client = new Sdk.Rsapi.RSAPIClient(rsapiUri, new UsernamePasswordCredentials(userName, password));"),
			Covered("cs002-client-global", _CS_002, ConfidenceLevel.High, "var client = new global::Sdk.Rsapi.RSAPIClient(rsapiUri, new UsernamePasswordCredentials(userName, password));"),
			Covered("cs002-nested-credential-fully-qualified", _CS_002, ConfidenceLevel.High, "var client = new RSAPIClient(rsapiUri, new Sdk.Security.UsernamePasswordCredentials(userName, password));"),
			Covered("cs002-nested-credential-global", _CS_002, ConfidenceLevel.High, "var client = new RSAPIClient(rsapiUri, new global::Sdk.UsernamePasswordCredentials(userName, password));"),
			Covered("cs002-nested-credential-partial-namespace", _CS_002, ConfidenceLevel.High, "var client = new RSAPIClient(rsapiUri, new Security.UsernamePasswordCredentials(userName, password));"),
			Covered("cs002-client-and-nested-credential-qualified", _CS_002, ConfidenceLevel.High, "var client = new Sdk.Rsapi.RSAPIClient(new Uri(rootUrl), new Sdk.Security.UsernamePasswordCredentials(userName, password));"),
			Covered("cs002-target-typed-client", _CS_002, ConfidenceLevel.High, "RSAPIClient client = new(rsapiUri, new UsernamePasswordCredentials(userName, password));"),
			Covered("cs002-target-typed-client-with-a-qualified-nested-credential", _CS_002, ConfidenceLevel.High, "RSAPIClient client = new(rsapiUri, new Sdk.UsernamePasswordCredentials(userName, password));"),
			Covered("cs003-credential-fully-qualified", _CS_003, ConfidenceLevel.High, "cache.Add(new Uri(instanceUrl), \"Basic\", new System.Net.NetworkCredential(userName, password));"),
			Covered("cs003-credential-global", _CS_003, ConfidenceLevel.High, "cache.Add(new Uri(instanceUrl), \"Basic\", new global::System.Net.NetworkCredential(userName, password));"),
			Covered("cs003-credential-partial-namespace", _CS_003, ConfidenceLevel.High, "cache.Add(new Uri(instanceUrl), \"Basic\", new Net.NetworkCredential(userName, password));"),
			Covered("cs003-address-created-with-a-qualified-type", _CS_003, ConfidenceLevel.High, "cache.Add(new System.Uri(instanceUrl), \"Basic\", new NetworkCredential(userName, password));"),
			Covered("cs003-address-created-with-the-target-typed-new", _CS_003, ConfidenceLevel.High, "cache.Add(new(instanceUrl), \"Basic\", new NetworkCredential(userName, password));"),
			Covered("cs003-address-with-a-nested-call-in-its-arguments", _CS_003, ConfidenceLevel.High, "cache.Add(new(Combine(rootUrl, path)), \"Basic\", new NetworkCredential(userName, password));"),
			Covered("cs003-address-and-credential-qualified", _CS_003, ConfidenceLevel.High, "cache.Add(new System.Uri(instanceUrl), \"Basic\", new System.Net.NetworkCredential(userName, password));"),
			Covered("cs003-four-argument-form-with-a-qualified-credential", _CS_003, ConfidenceLevel.High, "cache.Add(host, port, \"Basic\", new System.Net.NetworkCredential(userName, password));"),
			Covered("cs004-target-typed-declaration", _CS_004, ConfidenceLevel.High, "HttpBasicAuthenticator authenticator = new(userName, password);"),
			Covered("cs004-qualified-target-typed-declaration", _CS_004, ConfidenceLevel.High, "Vendor.Http.HttpBasicAuthenticator authenticator = new(userName, password);"),
			Covered("cs004-bare-name-target-typed-declaration", _CS_004, ConfidenceLevel.High, "BasicAuthenticator authenticator = new(userName, password);"),
			Covered("cs004-fully-qualified", _CS_004, ConfidenceLevel.High, "var authenticator = new Vendor.Http.HttpBasicAuthenticator(userName, password);"),
			Covered("cs004-global", _CS_004, ConfidenceLevel.High, "var authenticator = new global::Vendor.HttpBasicAuthenticator(userName, password);"),
			Covered("cs004-partial-namespace", _CS_004, ConfidenceLevel.High, "var authenticator = new Http.HttpBasicAuthenticator(userName, password);"),
			Covered("cs005-fully-qualified", _CS_005, ConfidenceLevel.Medium, Lines("var header = new System.Net.Http.Headers.AuthenticationHeaderValue(\"Basic\", encoded);", _ASSIGN_THE_HEADER_VALUE)),
			Covered("cs005-global", _CS_005, ConfidenceLevel.Medium, Lines("var header = new global::System.Net.Http.Headers.AuthenticationHeaderValue(\"Basic\", encoded);", _ASSIGN_THE_HEADER_VALUE)),
			Covered("cs005-partial-namespace", _CS_005, ConfidenceLevel.Medium, Lines("var header = new Headers.AuthenticationHeaderValue(\"Basic\", encoded);", _ASSIGN_THE_HEADER_VALUE)),
			Covered("cs005-target-typed-declaration", _CS_005, ConfidenceLevel.Medium, Lines("AuthenticationHeaderValue header = new(\"Basic\", encoded);", _ASSIGN_THE_HEADER_VALUE)),
			Covered("cs005-qualified-target-typed-declaration", _CS_005, ConfidenceLevel.Medium, Lines("Headers.AuthenticationHeaderValue header = new(\"Basic\", encoded);", _ASSIGN_THE_HEADER_VALUE))
		];
	}

	// ------------------------------------------------------------------------------------------
	// The clients and the SDK entry point: CS-015 and the header-enumeration member of CS-012.
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<CoveredForm> CreationFormsOfTheClientsAndHeaders()
	{
		return
		[
			Covered("cs015-fully-qualified", _CS_015, ConfidenceLevel.High, "var importApi = new Vendor.Import.ImportAPI(userName, password, webServiceUrl);"),
			Covered("cs015-global", _CS_015, ConfidenceLevel.High, "var importApi = new global::Vendor.ImportAPI(userName, password, webServiceUrl);"),
			Covered("cs015-partial-namespace", _CS_015, ConfidenceLevel.High, "var importApi = new Import.ImportAPI(userName, password, webServiceUrl);"),
			Covered("cs015-target-typed-declaration", _CS_015, ConfidenceLevel.High, "ImportAPI importApi = new(userName, password, webServiceUrl);"),
			Covered("cs015-qualified-target-typed-declaration", _CS_015, ConfidenceLevel.High, "Vendor.ImportAPI importApi = new(userName, password, webServiceUrl);"),
			Covered("cs013-member-fully-qualified", _CS_013, ConfidenceLevel.Medium, "binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Basic;"),
			Covered("cs013-member-global", _CS_013, ConfidenceLevel.Medium, "binding.Security.Transport.ClientCredentialType = global::System.ServiceModel.HttpClientCredentialType.Basic;"),
			Covered("cs013-member-partial-namespace", _CS_013, ConfidenceLevel.Medium, "binding.Security.Transport.ClientCredentialType = ServiceModel.HttpClientCredentialType.Basic;"),
			Covered(
				"cs013-qualified-member-with-the-password-in-the-window",
				_CS_013,
				ConfidenceLevel.Medium,
				Lines("binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Basic;", "client.ClientCredentials.UserName.Password = password;")),
			Covered("cs012-indexer-with-the-enumeration-member-fully-qualified", _CS_012, ConfidenceLevel.Medium, "request.Headers[System.Net.HttpRequestHeader.Authorization] = \"Basic \" + encoded;"),
			Covered("cs012-indexer-with-the-enumeration-member-global", _CS_012, ConfidenceLevel.Medium, "request.Headers[global::System.Net.HttpRequestHeader.Authorization] = \"Basic \" + encoded;"),
			Covered("cs012-indexer-with-the-enumeration-member-partial-namespace", _CS_012, ConfidenceLevel.Medium, "request.Headers[Net.HttpRequestHeader.Authorization] = \"Basic \" + encoded;"),
			Covered("cs012-add-with-the-enumeration-member-fully-qualified", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(System.Net.HttpRequestHeader.Authorization, \"Basic \" + encoded);"),
			Covered("cs012-set-with-the-enumeration-member-global", _CS_012, ConfidenceLevel.Medium, "request.Headers.Set(global::System.Net.HttpRequestHeader.Authorization, \"Basic \" + encoded);")
		];
	}

	// ------------------------------------------------------------------------------------------
	// The credential and its handler: CS-007, CS-008, CS-016.
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<CoveredForm> CreationFormsOfTheHandlersAndCredentials()
	{
		return
		[
			Covered("cs007-handler-and-credential-fully-qualified", _CS_007, ConfidenceLevel.Medium, "var handler = new System.Net.Http.HttpClientHandler { Credentials = new System.Net.NetworkCredential(userName, password) };"),
			Covered("cs007-handler-and-credential-global", _CS_007, ConfidenceLevel.Medium, "var handler = new global::System.Net.Http.HttpClientHandler { Credentials = new global::System.Net.NetworkCredential(userName, password) };"),
			Covered("cs007-handler-and-credential-partial-namespace", _CS_007, ConfidenceLevel.Medium, "var handler = new Http.HttpClientHandler { Credentials = new Net.NetworkCredential(userName, password) };"),
			Covered("cs007-constructor-parentheses-before-the-initializer", _CS_007, ConfidenceLevel.Medium, "var handler = new HttpClientHandler() { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs007-constructor-arguments-before-the-initializer", _CS_007, ConfidenceLevel.Medium, "var handler = new SocketsHttpHandler(settings) { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs007-constructor-parentheses-with-a-nested-call", _CS_007, ConfidenceLevel.Medium, "var handler = new WinHttpHandler(Build(settings)) { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs007-qualified-handler-with-constructor-parentheses", _CS_007, ConfidenceLevel.Medium, "var handler = new System.Net.Http.HttpClientHandler() { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs007-target-typed-with-an-empty-constructor-and-an-initializer", _CS_007, ConfidenceLevel.Medium, "HttpClientHandler handler = new() { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs007-target-typed-with-constructor-arguments-and-an-initializer", _CS_007, ConfidenceLevel.Medium, "HttpClientHandler handler = new(options) { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs007-target-typed-qualified-declaration-with-an-initializer", _CS_007, ConfidenceLevel.Medium, "Http.HttpClientHandler handler = new() { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs007-target-typed-followed-by-the-property-write", _CS_007, ConfidenceLevel.Medium, Lines("HttpClientHandler handler = new();", "handler.Credentials = new NetworkCredential(userName, password);")),
			Covered("cs007-target-typed-with-arguments-followed-by-the-property-write", _CS_007, ConfidenceLevel.Medium, Lines("SocketsHttpHandler handler = new(settings);", "handler.Credentials = new NetworkCredential(userName, password);")),
			Covered("cs007-qualified-handler-followed-by-the-property-write", _CS_007, ConfidenceLevel.Medium, Lines("var handler = new System.Net.Http.HttpClientHandler();", "handler.Credentials = new System.Net.NetworkCredential(userName, password);")),
			Covered("cs008-fully-qualified", _CS_008, ConfidenceLevel.Medium, "var credential = new System.Net.NetworkCredential(userName, password);"),
			Covered("cs008-global", _CS_008, ConfidenceLevel.Medium, "var credential = new global::System.Net.NetworkCredential(userName, password);"),
			Covered("cs008-partial-namespace", _CS_008, ConfidenceLevel.Medium, "var credential = new Net.NetworkCredential(userName, password);"),
			Covered("cs008-target-typed-declaration", _CS_008, ConfidenceLevel.Medium, "NetworkCredential credential = new(userName, password);"),
			Covered("cs008-qualified-target-typed-declaration", _CS_008, ConfidenceLevel.Medium, "System.Net.NetworkCredential credential = new(userName, password);"),
			Covered("cs016-fully-qualified", _CS_016, ConfidenceLevel.Medium, Lines("var credential = new System.Net.NetworkCredential { UserName = userName };", "credential.Password = password;")),
			Covered("cs016-global", _CS_016, ConfidenceLevel.Medium, Lines("var credential = new global::System.Net.NetworkCredential { UserName = userName };", "credential.Password = password;")),
			Covered("cs016-partial-namespace", _CS_016, ConfidenceLevel.Medium, Lines("var credential = new Net.NetworkCredential { UserName = userName };", "credential.Password = password;")),
			Covered("cs016-constructor-parentheses-before-the-initializer", _CS_016, ConfidenceLevel.Medium, Lines("var credential = new NetworkCredential() { UserName = userName };", "credential.Password = password;")),
			Covered("cs016-qualified-with-constructor-parentheses", _CS_016, ConfidenceLevel.Medium, Lines("var credential = new System.Net.NetworkCredential() { UserName = userName };", "credential.Password = password;"))
		];
	}

	// ------------------------------------------------------------------------------------------
	// The scheme held in a named constant: CS-012.
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<CoveredForm> NamedConstantSchemeForms()
	{
		return
		[
			Covered("cs012-bare-constant-in-the-add-write", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, SCHEME + \" \" + encoded);"),
			Covered("cs012-bare-constant-in-the-set-write", _CS_012, ConfidenceLevel.Medium, "request.Headers.Set(HttpRequestHeader.Authorization, SCHEME + \" \" + encoded);"),
			Covered("cs012-bare-constant-in-the-indexer-write", _CS_012, ConfidenceLevel.Medium, "request.Headers[HttpRequestHeader.Authorization] = SCHEME + \" \" + encoded;"),
			Covered("cs012-qualified-constant-in-the-add-write", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, AuthConstants.SCHEME + \" \" + encoded);"),
			Covered("cs012-qualified-constant-in-the-set-write", _CS_012, ConfidenceLevel.Medium, "request.Headers.Set(HttpRequestHeader.Authorization, AuthConstants.SCHEME + \" \" + encoded);"),
			Covered("cs012-qualified-constant-in-the-indexer-write", _CS_012, ConfidenceLevel.Medium, "request.Headers[HttpRequestHeader.Authorization] = AuthConstants.SCHEME + \" \" + encoded;"),
			Covered("cs012-namespace-qualified-constant", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, App.Security.AuthConstants.SCHEME + \" \" + encoded);"),
			Covered("cs012-global-qualified-constant", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, global::App.AuthConstants.SCHEME + \" \" + encoded);"),
			Covered("cs012-global-bare-constant", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, global::SCHEME + \" \" + encoded);"),
			Covered("cs012-qualified-constant-with-a-member-access-credential", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, AuthConstants.SCHEME + \" \" + options.Token);"),
			Covered("cs012-qualified-constant-with-an-indexer-credential", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, AuthConstants.SCHEME + \" \" + parts[1]);"),
			Covered("cs012-constant-in-an-interpolation-hole-in-the-add-write", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, $\"{SCHEME} {encoded}\");"),
			Covered("cs012-constant-in-an-interpolation-hole-in-the-set-write", _CS_012, ConfidenceLevel.Medium, "request.Headers.Set(HttpRequestHeader.Authorization, $\"{SCHEME} {encoded}\");"),
			Covered("cs012-constant-in-an-interpolation-hole-in-the-indexer-write", _CS_012, ConfidenceLevel.Medium, "request.Headers[HttpRequestHeader.Authorization] = $\"{SCHEME} {encoded}\";"),
			Covered("cs012-constant-in-an-interpolation-hole-with-a-member-credential", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, $\"{SCHEME} {options.Token}\");"),
			Covered("cs012-declared-constant-that-resolves-to-the-basic-scheme", _CS_012, ConfidenceLevel.Medium, Lines("const string SCHEME = \"Basic\";", "request.Headers.Add(HttpRequestHeader.Authorization, SCHEME + \" \" + encoded);")),
			Covered("cs012-declared-constant-in-an-interpolation-hole-that-resolves-to-the-basic-scheme", _CS_012, ConfidenceLevel.Medium, Lines("const string SCHEME = \"Basic\";", "request.Headers.Add(HttpRequestHeader.Authorization, $\"{SCHEME} {encoded}\");")),
			Covered(
				"cs012-qualified-constant-the-file-declares-under-the-same-last-name-stays-unresolved",
				_CS_012,
				ConfidenceLevel.Medium,
				Lines("const string SCHEME = \"Bearer\";", "request.Headers.Add(HttpRequestHeader.Authorization, Other.SCHEME + \" \" + encoded);"))
		];
	}

	// ------------------------------------------------------------------------------------------
	// CS-012: the interpolated literal scheme, the qualified constant in a hole, and the bound.
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<CoveredForm> HeaderEnumerationInterpolatedAndBoundedForms()
	{
		return
		[
			Covered("cs012-interpolated-literal-scheme-in-the-add-write", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, $\"Basic {encoded}\");"),
			Covered("cs012-interpolated-literal-scheme-in-the-indexer-write", _CS_012, ConfidenceLevel.Medium, "client.Headers[HttpRequestHeader.Authorization] = $\"Basic {credentials.Token}\";"),
			Covered("cs012-interpolated-literal-scheme-in-the-set-write-with-a-member-credential", _CS_012, ConfidenceLevel.Medium, "request.Headers.Set(HttpRequestHeader.Authorization, $\"Basic {options.Token}\");"),
			Covered(
				"cs012-interpolated-literal-scheme-with-the-enumeration-member-fully-qualified-and-a-call-credential",
				_CS_012,
				ConfidenceLevel.Medium,
				"request.Headers.Add(System.Net.HttpRequestHeader.Authorization, $\"Basic {Encode(userName)}\");"),
			Covered("cs012-qualified-constant-in-an-interpolation-hole-in-the-add-write", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, $\"{AuthConstants.SCHEME} {encoded}\");"),
			Covered("cs012-global-qualified-constant-in-an-interpolation-hole-in-the-indexer-write", _CS_012, ConfidenceLevel.Medium, "request.Headers[HttpRequestHeader.Authorization] = $\"{global::App.AuthConstants.SCHEME} {encoded}\";"),
			Covered("cs012-partial-namespace-constant-in-an-interpolation-hole-in-the-set-write", _CS_012, ConfidenceLevel.Medium, "request.Headers.Set(HttpRequestHeader.Authorization, $\"{Security.AuthConstants.SCHEME} {encoded}\");"),
			Covered("cs012-global-qualified-constant-with-no-scheme-word-in-the-concatenated-spelling", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, global::Ns.AuthConstants.SCHEME + \" \" + encoded);"),
			Covered("cs012-partial-namespace-constant-with-no-scheme-word-in-the-concatenated-spelling", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, Ns.AuthConstants.SCHEME + \" \" + encoded);"),
			Covered("cs012-bare-constant-naming-a-bounded-scheme-word-the-file-does-not-declare-stays-indeterminate", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, BEARER_SCHEME + \" \" + encoded);"),
			Covered("cs012-bare-constant-naming-a-bounded-scheme-word-in-a-hole-stays-indeterminate", _CS_012, ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, $\"{BEARER_SCHEME} {encoded}\");")
		];
	}

	private static ImmutableArray<ExpectedOutcomeForm> HeaderEnumerationBoundNearMisses()
	{
		return
		[
			Outcome("cs012-interpolated-bearer-scheme-with-add", "request.Headers.Add(HttpRequestHeader.Authorization, $\"Bearer {token}\");"),
			Outcome("cs012-interpolated-bearer-scheme-with-the-indexer", "request.Headers[HttpRequestHeader.Authorization] = $\"Bearer {token}\";"),
			Outcome("cs012-qualified-constant-whose-last-segment-names-another-scheme-in-a-hole", "request.Headers.Add(HttpRequestHeader.Authorization, $\"{AuthSchemes.Bearer} {token}\");"),
			Outcome("cs012-qualified-constant-whose-type-names-another-scheme-concatenated", "request.Headers.Add(HttpRequestHeader.Authorization, BearerDefaults.AuthenticationScheme + \" \" + token);"),
			Outcome("cs012-global-qualified-constant-naming-another-scheme-in-a-hole", "request.Headers.Add(HttpRequestHeader.Authorization, $\"{global::Ns.AuthSchemes.Bearer} {token}\");"),
			Outcome("cs012-global-qualified-constant-naming-another-scheme-concatenated", "request.Headers.Add(HttpRequestHeader.Authorization, global::Ns.BearerDefaults.Value + \" \" + token);"),
			Outcome("cs012-qualified-constant-naming-another-scheme-in-mixed-case", "request.Headers.Add(HttpRequestHeader.Authorization, OAuthBEARERSchemes.Value + \" \" + token);"),
			Outcome("cs012-qualified-constant-naming-negotiate-in-a-hole", "request.Headers.Add(HttpRequestHeader.Authorization, $\"{Schemes.Negotiate} {token}\");"),
			Outcome("cs012-qualified-constant-naming-digest-in-a-hole", "request.Headers.Add(HttpRequestHeader.Authorization, $\"{Schemes.Digest} {token}\");"),
			Outcome("cs012-qualified-constant-naming-ntlm-in-a-hole", "request.Headers.Add(HttpRequestHeader.Authorization, $\"{Schemes.NtlmValue} {token}\");"),
			Outcome("cs012-qualified-constant-naming-kerberos-in-a-hole", "request.Headers.Add(HttpRequestHeader.Authorization, $\"{Schemes.Kerberos} {token}\");"),
			Outcome("cs012-qualified-constant-naming-negotiate-concatenated", "request.Headers.Add(HttpRequestHeader.Authorization, Schemes.Negotiate + \" \" + token);"),
			Outcome("cs012-qualified-constant-naming-digest-concatenated", "request.Headers.Add(HttpRequestHeader.Authorization, Schemes.Digest + \" \" + token);"),
			Outcome("cs012-qualified-constant-naming-ntlm-concatenated", "request.Headers.Add(HttpRequestHeader.Authorization, Schemes.Ntlm + \" \" + token);"),
			Outcome("cs012-qualified-constant-naming-kerberos-concatenated", "request.Headers.Add(HttpRequestHeader.Authorization, Schemes.Kerberos + \" \" + token);"),
			Outcome("cs012-declared-bearer-constant-in-a-hole-is-suppressed", Lines("const string BEARER = \"Bearer\";", "request.Headers.Add(HttpRequestHeader.Authorization, $\"{BEARER} {token}\");")),
			Outcome("cs012-interpolated-literal-scheme-on-another-enumeration-member", "request.Headers.Add(HttpRequestHeader.ContentType, $\"Basic {x}\");"),
			Outcome("cs012-read-of-the-authorization-header", "var current = request.Headers[HttpRequestHeader.Authorization];"),
			Outcome("cs012-set-on-another-enumeration-member", "request.Headers.Set(HttpRequestHeader.Accept, $\"Basic {x}\");")
		];
	}

	// ------------------------------------------------------------------------------------------
	// CS-019: the file-transfer request's two other spellings.
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<CoveredForm> FileTransferRequestSpellingForms()
	{
		return
		[
			Covered("cs019-file-transfer-request-obtained-by-an-as-cast", _CS_019, ConfidenceLevel.Low, Lines("var request = WebRequest.Create(transferUri) as FtpWebRequest;", _FILE_TRANSFER_CREDENTIAL)),
			Covered("cs019-file-transfer-request-created-through-the-derived-type-with-a-cast", _CS_019, ConfidenceLevel.Low, Lines("var request = (FtpWebRequest)FtpWebRequest.Create(transferUri);", _FILE_TRANSFER_CREDENTIAL)),
			Covered("cs019-file-transfer-request-created-through-the-derived-type-with-as", _CS_019, ConfidenceLevel.Low, Lines("var request = FtpWebRequest.Create(transferUri) as FtpWebRequest;", _FILE_TRANSFER_CREDENTIAL)),
			Covered(
				"cs019-file-transfer-request-obtained-by-a-qualified-as-cast",
				_CS_019,
				ConfidenceLevel.Low,
				Lines("var request = System.Net.WebRequest.Create(transferUri) as System.Net.FtpWebRequest;", _FILE_TRANSFER_CREDENTIAL)),
			Covered("cs019-file-transfer-request-with-the-declared-type-and-an-as-cast", _CS_019, ConfidenceLevel.Low, Lines("FtpWebRequest request = WebRequest.Create(transferUri) as FtpWebRequest;", _FILE_TRANSFER_CREDENTIAL)),
			Covered(
				"cs019-file-transfer-request-with-an-as-cast-and-a-nested-call-in-the-address",
				_CS_019,
				ConfidenceLevel.Low,
				Lines("var request = WebRequest.Create(Build(transferUri)) as FtpWebRequest;", _FILE_TRANSFER_CREDENTIAL))
		];
	}

	private static ImmutableArray<ExpectedOutcomeForm> FileTransferRequestNearMisses()
	{
		return
		[
			Outcome(
				"an-http-request-obtained-by-an-as-cast-stays-cs008",
				Lines("var request = WebRequest.Create(transferUri) as HttpWebRequest;", _FILE_TRANSFER_CREDENTIAL),
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome(
				"an-http-request-obtained-by-a-cast-stays-cs008",
				Lines("var request = (HttpWebRequest)WebRequest.Create(transferUri);", _FILE_TRANSFER_CREDENTIAL),
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome(
				"a-file-transfer-request-whose-variable-does-not-take-the-credential-is-not-cs019",
				Lines("var request = WebRequest.Create(transferUri) as FtpWebRequest;", "other.Credentials = new NetworkCredential(userName, password);"),
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome("a-file-transfer-request-with-no-credential-bound-is-nothing", Lines("var request = (FtpWebRequest)FtpWebRequest.Create(transferUri);", "request.Method = \"LIST\";"))
		];
	}

	// ------------------------------------------------------------------------------------------
	// The halves of a colon-joined pair: CS-009.
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<CoveredForm> IdentifierRootedPairForms()
	{
		return
		[
			Covered("cs009-interpolated-member-access-halves", _CS_009, ConfidenceLevel.Medium, Lines("string pair = $\"{userBox.Text}:{secretBox.Entry}\";", _ENCODE_THE_PAIR)),
			Covered("cs009-concatenated-member-access-halves", _CS_009, ConfidenceLevel.Medium, Lines("string pair = userBox.Text + \":\" + secretBox.Entry;", _ENCODE_THE_PAIR)),
			Covered("cs009-formatted-member-access-halves", _CS_009, ConfidenceLevel.Medium, Lines("string pair = string.Format(\"{0}:{1}\", userBox.Text, secretBox.Entry);", _ENCODE_THE_PAIR)),
			Covered("cs009-joined-member-access-halves", _CS_009, ConfidenceLevel.Medium, Lines("string pair = string.Join(\":\", userBox.Text, secretBox.Entry);", _ENCODE_THE_PAIR)),
			Covered("cs009-interpolated-call-halves", _CS_009, ConfidenceLevel.Medium, Lines("string pair = $\"{Console.ReadLine()}:{ReadSecret()}\";", _ENCODE_THE_PAIR)),
			Covered("cs009-concatenated-call-halves", _CS_009, ConfidenceLevel.Medium, Lines("string pair = Console.ReadLine() + \":\" + ReadSecret();", _ENCODE_THE_PAIR)),
			Covered("cs009-formatted-call-halves", _CS_009, ConfidenceLevel.Medium, Lines("string pair = string.Format(\"{0}:{1}\", Console.ReadLine(), ReadSecret());", _ENCODE_THE_PAIR)),
			Covered("cs009-joined-call-halves", _CS_009, ConfidenceLevel.Medium, Lines("string pair = string.Join(\":\", Console.ReadLine(), ReadSecret());", _ENCODE_THE_PAIR)),
			Covered("cs009-interpolated-indexer-halves", _CS_009, ConfidenceLevel.Medium, Lines("string pair = $\"{parts[0]}:{parts[1]}\";", _ENCODE_THE_PAIR)),
			Covered("cs009-concatenated-indexer-halves", _CS_009, ConfidenceLevel.Medium, Lines("string pair = parts[0] + \":\" + parts[1];", _ENCODE_THE_PAIR)),
			Covered("cs009-call-with-one-level-of-nested-parentheses", _CS_009, ConfidenceLevel.Medium, Lines("string pair = $\"{Get(Wrap(1))}:{Get(Wrap(2))}\";", _ENCODE_THE_PAIR)),
			Covered("cs009-credential-half-that-is-a-call-and-an-indexer", _CS_009, ConfidenceLevel.Medium, Lines("string pair = userBox.Text + \":\" + Secrets.Read(index)[0];", _ENCODE_THE_PAIR)),
			Covered("cs009-inline-interpolated-member-access-halves", _CS_009, ConfidenceLevel.Medium, "var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{userBox.Text}:{secretBox.Entry}\"));"),
			Covered("cs009-inline-concatenated-member-access-halves", _CS_009, ConfidenceLevel.Medium, "var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(userBox.Text + \":\" + secretBox.Entry));"),
			Covered("cs009-inline-formatted-call-halves", _CS_009, ConfidenceLevel.Medium, "var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format(\"{0}:{1}\", Console.ReadLine(), ReadSecret())));"),
			Covered("cs009-inline-joined-indexer-halves", _CS_009, ConfidenceLevel.Medium, "var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(\":\", parts[0], parts[1])));"),
			Covered(
				"cs009-halves-read-from-a-desktop-password-control-target-the-authorization-code-flow",
				_CS_009,
				ConfidenceLevel.Medium,
				Lines(_INTERACTIVE_SIGNAL, "string pair = $\"{userBox.Text}:{passwordBox.Password}\";", _ENCODE_THE_PAIR),
				FlowDecision.InteractiveSignal),
			Covered(
				"cs009-halves-read-from-the-console-target-the-authorization-code-flow",
				_CS_009,
				ConfidenceLevel.Medium,
				Lines("string userPassword = Console.ReadLine();", "string pair = Console.ReadLine() + \":\" + userPassword;", _ENCODE_THE_PAIR),
				FlowDecision.InteractiveSignal),
			Covered(
				"cs009-member-access-halves-beside-a-fixed-credential-read-target-the-client-credentials-flow",
				_CS_009,
				ConfidenceLevel.Medium,
				Lines(_FIXED_SIGNAL, "string pair = settings.User + \":\" + settings.Secret;", _ENCODE_THE_PAIR),
				FlowDecision.FixedSignal)
		];
	}

	// ------------------------------------------------------------------------------------------
	// The object-initializer sinks and the target-typed sinks: CS-019 at Low.
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<CoveredForm> ObjectInitializerSinkForms()
	{
		return
		[
			Covered("cs019-mail-client-initializer-with-constructor-arguments", _CS_019, ConfidenceLevel.Low, "var mailer = new SmtpClient(host, port) { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs019-mail-client-initializer-with-empty-constructor-parentheses", _CS_019, ConfidenceLevel.Low, "var mailer = new SmtpClient() { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs019-mail-client-initializer-without-constructor-parentheses", _CS_019, ConfidenceLevel.Low, "var mailer = new SmtpClient { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs019-proxy-initializer", _CS_019, ConfidenceLevel.Low, "var route = new WebProxy(address) { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs019-proxy-initializer-with-a-nested-call", _CS_019, ConfidenceLevel.Low, "var route = new WebProxy(Build(address)) { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs019-initializer-with-other-members-around-the-credential", _CS_019, ConfidenceLevel.Low, "var mailer = new SmtpClient(host, port) { EnableSsl = true, Credentials = new NetworkCredential(userName, password), Timeout = limit };"),
			Covered("cs019-initializer-spread-over-lines", _CS_019, ConfidenceLevel.Low, Lines("var mailer = new SmtpClient(host, port)", "{", "\tCredentials = new NetworkCredential(userName, password)", "};")),
			Covered("cs019-sink-and-credential-fully-qualified", _CS_019, ConfidenceLevel.Low, "var mailer = new System.Net.Mail.SmtpClient(host, port) { Credentials = new System.Net.NetworkCredential(userName, password) };"),
			Covered("cs019-sink-and-credential-global", _CS_019, ConfidenceLevel.Low, "var mailer = new global::System.Net.Mail.SmtpClient(host, port) { Credentials = new global::System.Net.NetworkCredential(userName, password) };"),
			Covered("cs019-sink-and-credential-partial-namespace", _CS_019, ConfidenceLevel.Low, "var route = new Net.WebProxy(address) { Credentials = new Net.NetworkCredential(userName, password) };"),
			Covered("cs019-target-typed-declaration-with-an-initializer", _CS_019, ConfidenceLevel.Low, "SmtpClient mailer = new(host, port) { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs019-target-typed-declaration-with-empty-parentheses-and-an-initializer", _CS_019, ConfidenceLevel.Low, "SmtpClient mailer = new() { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs019-target-typed-proxy-with-an-initializer", _CS_019, ConfidenceLevel.Low, "WebProxy route = new(address) { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs019-target-typed-qualified-declaration-with-an-initializer", _CS_019, ConfidenceLevel.Low, "Mail.SmtpClient mailer = new(host, port) { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs019-target-typed-declaration-followed-by-the-binding", _CS_019, ConfidenceLevel.Low, Lines("SmtpClient mailer = new(host, port);", "mailer.Credentials = new NetworkCredential(userName, password);")),
			Covered("cs019-target-typed-proxy-followed-by-the-binding", _CS_019, ConfidenceLevel.Low, Lines("WebProxy route = new(address);", "route.Credentials = new NetworkCredential(userName, password);")),
			Covered("cs019-qualified-sink-followed-by-the-binding", _CS_019, ConfidenceLevel.Low, Lines("var mailer = new System.Net.Mail.SmtpClient(host, port);", "mailer.Credentials = new System.Net.NetworkCredential(userName, password);")),
			Covered("cs019-qualified-proxy-followed-by-the-binding", _CS_019, ConfidenceLevel.Low, Lines("var route = new global::System.Net.WebProxy(address);", "route.Credentials = new NetworkCredential(userName, password);")),
			Covered("cs019-file-transfer-request-with-a-qualified-cast-and-factory", _CS_019, ConfidenceLevel.Low, Lines("var request = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(transferUri);", "request.Credentials = new System.Net.NetworkCredential(userName, password);")),
			Covered("cs019-file-transfer-request-with-a-global-cast-and-factory", _CS_019, ConfidenceLevel.Low, Lines("var request = (global::System.Net.FtpWebRequest)global::System.Net.WebRequest.Create(transferUri);", "request.Credentials = new NetworkCredential(userName, password);")),
			Covered("cs019-file-transfer-request-with-a-partial-namespace-cast", _CS_019, ConfidenceLevel.Low, Lines("var request = (Net.FtpWebRequest)Net.WebRequest.Create(transferUri);", "request.Credentials = new NetworkCredential(userName, password);")),
			Covered("cs019-directory-entry-with-a-qualified-type-and-credential", _CS_019, ConfidenceLevel.Low, Lines("var directoryCredential = new System.Net.NetworkCredential(userName, password);", "var root = new System.DirectoryServices.DirectoryEntry(path, directoryCredential.UserName, directoryCredential.Password);")),
			Covered(
				"cs019-credential-assembled-across-statements-with-a-qualified-type-bound-by-an-initializer",
				_CS_019,
				ConfidenceLevel.Low,
				Lines("var credential = new System.Net.NetworkCredential { UserName = userName };", "credential.Password = password;", "var mailer = new System.Net.Mail.SmtpClient(host, port) { Credentials = credential };")),
			Covered(
				"cs019-credential-assembled-across-statements-with-constructor-parentheses-bound-by-an-initializer",
				_CS_019,
				ConfidenceLevel.Low,
				Lines("var credential = new NetworkCredential() { UserName = userName };", "credential.Password = password;", "var mailer = new SmtpClient(host, port) { Credentials = credential };")),
			Covered(
				"cs019-credential-assembled-across-statements-with-constructor-parentheses-then-a-binding",
				_CS_019,
				ConfidenceLevel.Low,
				Lines("var mailer = new SmtpClient(host, port);", "var credential = new NetworkCredential() { UserName = userName };", "credential.Password = password;", "mailer.Credentials = credential;")),
			Covered(
				"cs019-credential-assembled-across-statements-bound-by-a-target-typed-initializer",
				_CS_019,
				ConfidenceLevel.Low,
				Lines("var credential = new NetworkCredential { UserName = userName };", "credential.Password = password;", "SmtpClient mailer = new(host, port) { Credentials = credential };")),
			Covered(
				"cs019-target-typed-sink-then-a-credential-assembled-across-statements-then-a-binding",
				_CS_019,
				ConfidenceLevel.Low,
				Lines("SmtpClient mailer = new(host, port);", "var credential = new NetworkCredential { UserName = userName };", "credential.Password = password;", "mailer.Credentials = credential;")),
			Covered(
				"cs019-qualified-sink-then-a-qualified-credential-assembled-across-statements-then-a-binding",
				_CS_019,
				ConfidenceLevel.Low,
				Lines("var mailer = new System.Net.Mail.SmtpClient(host, port);", "var credential = new System.Net.NetworkCredential { UserName = userName };", "credential.Password = password;", "mailer.Credentials = credential;"))
		];
	}

	// ------------------------------------------------------------------------------------------
	// Further forms of the same classes, so the shared expressions stay pinned.
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<CoveredForm> ResidueAndEmptyHalfForms()
	{
		return
		[
			Covered("cs008-residue-for-a-sink-outside-both-lists", _CS_008, ConfidenceLevel.Medium, "var client = new WebClient { Credentials = new NetworkCredential(userName, password) };"),
			Covered("cs008-target-typed-declaration-with-indexer-arguments", _CS_008, ConfidenceLevel.Medium, "NetworkCredential credential = new(settings[\"User\"], values[0][1]);"),
			Covered("cs008-qualified-with-indexer-arguments-and-calls", _CS_008, ConfidenceLevel.Medium, "var credential = new System.Net.NetworkCredential(settings[\"User\"], Secrets.Read(index)[0]);"),
			Covered("cs007-target-typed-initializer-with-indexer-arguments", _CS_007, ConfidenceLevel.Medium, "HttpClientHandler handler = new() { Credentials = new NetworkCredential(settings[\"User\"], values[0][1]) };"),
			Covered("cs007-target-typed-property-write-with-indexer-arguments", _CS_007, ConfidenceLevel.Medium, Lines("HttpClientHandler handler = new();", "handler.Credentials = new NetworkCredential(settings[\"User\"], values[0][1]);")),
			Covered("cs019-initializer-with-indexer-arguments", _CS_019, ConfidenceLevel.Low, "var mailer = new SmtpClient(host, port) { Credentials = new NetworkCredential(settings[\"User\"], values[0][1]) };"),
			Covered("cs019-target-typed-binding-with-indexer-arguments", _CS_019, ConfidenceLevel.Low, Lines("SmtpClient mailer = new(host, port);", "mailer.Credentials = new NetworkCredential(settings[\"User\"], values[0][1]);")),
			Covered("cs019-qualified-file-transfer-request-with-indexer-arguments", _CS_019, ConfidenceLevel.Low, Lines("var request = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(transferUri);", "request.Credentials = new NetworkCredential(settings[\"User\"], values[0][1]);")),
			Covered("cs004-target-typed-declaration-with-indexer-arguments", _CS_004, ConfidenceLevel.High, "HttpBasicAuthenticator authenticator = new(settings[\"User\"], values[0][1]);"),
			Covered("cs015-target-typed-declaration-with-indexer-arguments", _CS_015, ConfidenceLevel.High, "ImportAPI importApi = new(settings[\"User\"], values[0][1], webServiceUrl);"),
			Covered("cs005-target-typed-declaration-with-an-indexer-credential", _CS_005, ConfidenceLevel.Medium, Lines("AuthenticationHeaderValue header = new(\"Basic\", parts[1]);", _ASSIGN_THE_HEADER_VALUE)),
			Covered("cs001-target-typed-declaration-with-indexer-arguments", _CS_001, ConfidenceLevel.High, "UsernamePasswordCredentials credentials = new(settings[\"User\"], values[0][1]);"),
			Covered("cs018-an-empty-half-literal-stays-the-token-class", _CS_018, ConfidenceLevel.Medium, Lines("string pair = $\"{\"\"}:{token}\";", _ENCODE_THE_PAIR))
		];
	}

	// ------------------------------------------------------------------------------------------
	// Declared forms: every one produces exactly what the catalog's declaration says.
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<ExpectedOutcomeForm> DeclaredAliasForms()
	{
		return
		[
			Outcome("alias-of-the-credential-type-cs001", Lines("using Creds = Sdk.UsernamePasswordCredentials;", "Creds credentials = new Creds(userName, password);")),
			Outcome("alias-of-the-client-keeps-the-nested-credential-as-cs001", Lines("using Rsapi = Sdk.RSAPIClient;", "var client = new Rsapi(rsapiUri, new UsernamePasswordCredentials(userName, password));"), (_CS_001, ConfidenceLevel.High)),
			Outcome("alias-of-the-nested-credential-leaves-nothing-to-read", Lines("using Creds = Sdk.UsernamePasswordCredentials;", "var client = new RSAPIClient(rsapiUri, new Creds(userName, password));")),
			Outcome("alias-of-the-credential-in-a-cache-entry-cs003", Lines("using Net = System.Net.NetworkCredential;", "cache.Add(new Uri(instanceUrl), \"Basic\", new Net(userName, password));")),
			Outcome("alias-of-the-authenticator-type-cs004", Lines("using Auth = Vendor.HttpBasicAuthenticator;", "var authenticator = new Auth(userName, password);")),
			Outcome("alias-of-the-header-type-cs005", Lines("using Header = System.Net.Http.Headers.AuthenticationHeaderValue;", "var header = new Header(\"Basic\", encoded);", _ASSIGN_THE_HEADER_VALUE)),
			Outcome(
				"alias-of-the-handler-type-leaves-the-credential-as-cs008",
				Lines("using Handler = System.Net.Http.HttpClientHandler;", "var handler = new Handler { Credentials = new NetworkCredential(userName, password) };"),
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome("alias-of-the-credential-type-cs008", Lines("using Net = System.Net.NetworkCredential;", "var credential = new Net(userName, password);")),
			Outcome("alias-of-the-sdk-entry-point-cs015", Lines("using Import = Vendor.ImportAPI;", "var importApi = new Import(userName, password, webServiceUrl);")),
			Outcome("alias-of-the-credential-type-cs016", Lines("using Net = System.Net.NetworkCredential;", "var credential = new Net { UserName = userName };", "credential.Password = password;")),
			Outcome(
				"alias-of-the-mail-client-leaves-the-credential-as-cs008",
				Lines("using Mailer = System.Net.Mail.SmtpClient;", "var mailer = new Mailer(host, port) { Credentials = new NetworkCredential(userName, password) };"),
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome(
				"alias-of-the-credential-type-in-a-mail-client-initializer",
				Lines("using Net = System.Net.NetworkCredential;", "var mailer = new SmtpClient(host, port) { Credentials = new Net(userName, password) };")),
			Outcome("enumeration-member-reached-through-a-using-static-directive-cs012", Lines("using static System.Net.HttpRequestHeader;", "request.Headers[Authorization] = \"Basic \" + encoded;")),
			Outcome("enumeration-member-reached-through-a-using-static-directive-cs013", Lines("using static System.ServiceModel.HttpClientCredentialType;", "binding.Security.Transport.ClientCredentialType = Basic;")),
			Outcome("encoder-reached-through-a-using-static-directive", Lines("using static System.Convert;", "string pair = $\"{userBox.Text}:{secretBox.Entry}\";", "var encoded = ToBase64String(Encoding.UTF8.GetBytes(pair));"))
		];
	}

	private static ImmutableArray<ExpectedOutcomeForm> DeclaredTargetTypedForms()
	{
		return
		[
			Outcome("target-typed-nested-credential-of-the-legacy-client", "var client = new RSAPIClient(rsapiUri, new(userName, password));"),
			Outcome("target-typed-sdk-credential-assigned-to-an-existing-variable", "credentials = new(userName, password);"),
			Outcome("target-typed-credential-as-the-last-argument-of-a-cache-entry", "cache.Add(new Uri(instanceUrl), \"Basic\", new(userName, password));"),
			Outcome("target-typed-authenticator-assigned-to-an-existing-variable", "authenticator = new(userName, password);"),
			Outcome("target-typed-authenticator-returned", "return new(userName, password);"),
			Outcome("target-typed-header-assigned-to-the-header-property", Lines("client.DefaultRequestHeaders.Authorization = new(\"Basic\", encoded);")),
			Outcome("target-typed-credential-assigned-to-an-existing-variable", "credential = new(userName, password);"),
			Outcome("target-typed-credential-returned", "return new(userName, password);"),
			Outcome("target-typed-credential-passed-as-an-argument", "Send(request, new(userName, password));"),
			Outcome("target-typed-sdk-entry-point-assigned-to-an-existing-variable", "importApi = new(userName, password, webServiceUrl);"),
			Outcome(
				"target-typed-handler-assigned-to-an-existing-variable-leaves-the-credential-as-cs008",
				"handler = new() { Credentials = new NetworkCredential(userName, password) };",
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome(
				"target-typed-mail-client-assigned-to-an-existing-variable-leaves-the-credential-as-cs008",
				"mailer = new(host, port) { Credentials = new NetworkCredential(userName, password) };",
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome("target-typed-credential-in-a-handler-initializer", "var handler = new HttpClientHandler { Credentials = new(userName, password) };"),
			Outcome("target-typed-credential-in-a-mail-client-initializer", "var mailer = new SmtpClient(host, port) { Credentials = new(userName, password) };"),
			Outcome("target-typed-credential-with-an-object-initializer-and-a-later-password", Lines("NetworkCredential credential = new() { UserName = userName };", "credential.Password = password;"))
		];
	}

	private static ImmutableArray<ExpectedOutcomeForm> DeclaredNamedConstantForms()
	{
		return
		[
			Outcome("cs012-constant-that-carries-the-scheme-and-the-space-concatenated-directly", "request.Headers.Add(HttpRequestHeader.Authorization, SCHEME_WITH_SPACE + encoded);"),
			Outcome("cs012-constant-that-carries-the-scheme-and-the-space-in-the-indexer-write", "request.Headers[HttpRequestHeader.Authorization] = SCHEME_WITH_SPACE + encoded;"),
			Outcome("cs012-qualified-constant-that-carries-the-space-concatenated-directly", "request.Headers.Add(HttpRequestHeader.Authorization, AuthConstants.SCHEME_WITH_SPACE + encoded);"),
			Outcome("cs012-formatted-value-over-a-constant", "request.Headers.Add(HttpRequestHeader.Authorization, string.Format(\"{0} {1}\", SCHEME, encoded));"),
			Outcome("cs012-formatted-value-over-a-qualified-constant", "request.Headers[HttpRequestHeader.Authorization] = string.Format(\"{0} {1}\", AuthConstants.SCHEME, encoded);"),
			Outcome(
				"cs003-scheme-written-as-a-qualified-constant-leaves-the-credential-as-cs008",
				"cache.Add(new Uri(instanceUrl), AuthConstants.SCHEME, new NetworkCredential(userName, password));",
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome("cs005-scheme-written-as-a-qualified-constant", Lines("var header = new AuthenticationHeaderValue(AuthConstants.SCHEME, encoded);", _ASSIGN_THE_HEADER_VALUE)),
			Outcome("cs006-scheme-written-as-a-qualified-constant-in-a-concatenation", "client.DefaultRequestHeaders.Add(\"Authorization\", AuthConstants.SCHEME + \" \" + encoded);"),
			Outcome("cs006-scheme-written-as-a-qualified-constant-in-an-interpolation-hole", "client.DefaultRequestHeaders.Add(\"Authorization\", $\"{AuthConstants.SCHEME} {encoded}\");")
		];
	}

	private static ImmutableArray<ExpectedOutcomeForm> DeclaredInitializerAndSinkForms()
	{
		return
		[
			Outcome(
				"cache-entry-added-by-a-collection-initializer-leaves-the-credential-as-cs008",
				"var cache = new CredentialCache { { new Uri(instanceUrl), \"Basic\", new NetworkCredential(userName, password) } };",
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome("credential-with-both-halves-in-one-initializer", "var credential = new NetworkCredential { UserName = userName, Password = password };"),
			Outcome(
				"sink-outside-both-lists-in-an-initializer-leaves-the-credential-as-cs008",
				"var client = new WebClient { Credentials = new NetworkCredential(userName, password) };",
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome(
				"credential-assigned-from-a-variable-to-a-handler-is-cs008",
				Lines("var credential = new NetworkCredential(userName, password);", "handler.Credentials = credential;"),
				(_CS_008, ConfidenceLevel.Medium))
		];
	}

	// ------------------------------------------------------------------------------------------
	// Near-misses.
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<ExpectedOutcomeForm> NamedConstantNearMisses()
	{
		return
		[
			Outcome("cs012-constant-the-file-resolves-to-another-scheme-is-suppressed", Lines("const string SCHEME = \"Bearer\";", "request.Headers.Add(HttpRequestHeader.Authorization, SCHEME + \" \" + encoded);")),
			Outcome("cs012-interpolated-constant-the-file-resolves-to-another-scheme-is-suppressed", Lines("const string SCHEME = \"Bearer\";", "request.Headers.Add(HttpRequestHeader.Authorization, $\"{SCHEME} {encoded}\");")),
			Outcome("cs012-qualified-constant-with-a-literal-that-is-not-a-space-is-not-the-scheme", "request.Headers.Add(HttpRequestHeader.Authorization, AuthConstants.SCHEME + \"x\" + encoded);"),
			Outcome("cs012-qualified-constant-on-another-enumeration-member", "request.Headers.Add(HttpRequestHeader.ContentType, AuthConstants.SCHEME + \" \" + encoded);"),
			Outcome("cs012-qualified-constant-in-a-read-of-the-header", "var current = request.Headers[HttpRequestHeader.Authorization]; var text = AuthConstants.SCHEME + \" \" + encoded;"),
			Outcome("cs012-interpolated-constant-on-another-enumeration-member", "request.Headers.Add(HttpRequestHeader.ContentType, $\"{SCHEME} {encoded}\");"),
			Outcome("cs012-helper-whose-name-only-begins-with-the-scheme-word", "request.Headers.Add(HttpRequestHeader.Authorization, Basically(userName, password));"),
			Outcome("cs012-qualified-enumeration-member-on-another-member", "request.Headers[System.Net.HttpRequestHeader.ContentType] = \"Basic \" + encoded;"),
			Outcome("cs012-qualified-enumeration-member-in-a-read", "var current = request.Headers[System.Net.HttpRequestHeader.Authorization];")
		];
	}

	private static ImmutableArray<ExpectedOutcomeForm> IdentifierRootedPairNearMisses()
	{
		return
		[
			Outcome("cs009-interpolated-pair-with-an-empty-literal-half-is-the-token-class", Lines("string pair = $\"{\"\"}:{token}\";", _ENCODE_THE_PAIR), (_CS_018, ConfidenceLevel.Medium)),
			Outcome("cs009-concatenated-pair-with-an-empty-literal-half-is-the-token-class", Lines("string pair = \"\" + \":\" + token;", _ENCODE_THE_PAIR), (_CS_018, ConfidenceLevel.Medium)),
			Outcome("cs009-concatenated-pair-with-an-empty-right-half-is-the-token-class", Lines("string pair = token + \":\" + \"\";", _ENCODE_THE_PAIR), (_CS_018, ConfidenceLevel.Medium)),
			Outcome("cs009-formatted-pair-with-a-literal-half-is-not-a-pair", Lines("string pair = string.Format(\"{0}:{1}\", \"operator\", secretBox.Entry);", _ENCODE_THE_PAIR)),
			Outcome("cs009-formatted-pair-with-a-literal-second-half-is-not-a-pair", Lines("string pair = string.Format(\"{0}:{1}\", userBox.Text, \"\");", _ENCODE_THE_PAIR)),
			Outcome("cs009-joined-pair-with-a-literal-half-is-not-a-pair", Lines("string pair = string.Join(\":\", \"operator\", secretBox.Entry);", _ENCODE_THE_PAIR)),
			Outcome("cs009-concatenated-pair-with-a-literal-second-half-is-not-a-pair", Lines("string pair = userBox.Text + \":\" + \"literalSecret\";", _ENCODE_THE_PAIR)),
			Outcome("cs009-interpolated-pair-with-a-literal-second-half-is-not-a-pair", Lines("string pair = $\"{userBox.Text}:{\"literalSecret\"}\";", _ENCODE_THE_PAIR)),
			Outcome("cs009-interpolated-half-that-is-an-arithmetic-expression", Lines("string pair = $\"{first + second}:{secretBox.Entry}\";", _ENCODE_THE_PAIR)),
			Outcome("cs009-interpolated-half-that-is-a-conditional-expression", Lines("string pair = $\"{flag ? first : second}:{secretBox.Entry}\";", _ENCODE_THE_PAIR)),
			Outcome("cs009-second-half-that-is-an-arithmetic-expression", Lines("string pair = $\"{userBox.Text}:{count + 1}\";", _ENCODE_THE_PAIR)),
			Outcome("cs009-interpolated-half-nested-deeper-than-one-level-of-parentheses", Lines("string pair = $\"{Get(Wrap(Inner(1)))}:{secretBox.Entry}\";", _ENCODE_THE_PAIR)),
			Outcome("cs009-formatted-half-nested-deeper-than-one-level-of-parentheses", Lines("string pair = string.Format(\"{0}:{1}\", userBox.Text, Get(Wrap(Inner(1))));", _ENCODE_THE_PAIR)),
			Outcome("cs009-pair-with-a-half-and-no-encoder", "string pair = $\"{userBox.Text}:{secretBox.Entry}\";"),
			Outcome("cs009-encoder-call-that-closed-before-the-concatenation-is-not-a-left-half", "var text = Convert.ToBase64String(payloadBytes) + \":\" + secretBox.Entry;"),
			Outcome("cs009-two-encoder-calls-joined-by-a-colon-are-not-a-pair-without-a-later-encoder", "var text = Convert.ToBase64String(payloadBytes) + \":\" + Convert.ToBase64String(otherBytes);"),
			Outcome("cs009-inline-pair-with-a-literal-half", "var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format(\"{0}:{1}\", \"operator\", secretBox.Entry)));"),
			Outcome("cs009-inline-pair-with-a-conditional-half", "var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{flag ? first : second}:{secretBox.Entry}\"));")
		];
	}

	private static ImmutableArray<ExpectedOutcomeForm> CreationFormNearMisses()
	{
		return
		[
			Outcome("a-longer-credential-type-name-is-not-the-credential-type", "var credential = new System.Net.NetworkCredentialFactory(userName, password);"),
			Outcome("a-longer-name-ending-in-the-credential-type-is-not-the-credential-type", "var credential = new MyNetworkCredential(userName, password);"),
			Outcome("a-longer-header-type-name-is-not-the-header-type", Lines("var header = new System.Net.Http.Headers.AuthenticationHeaderValueFactory(\"Basic\", encoded);", _ASSIGN_THE_HEADER_VALUE)),
			Outcome("a-longer-sdk-entry-point-name-is-not-the-entry-point", "var importApi = new Vendor.ImportAPIWrapper(userName, password, webServiceUrl);"),
			Outcome("a-two-argument-target-typed-sdk-entry-point-is-not-the-entry-point", "ImportAPI importApi = new(userName, webServiceUrl);"),
			Outcome("a-four-argument-target-typed-sdk-entry-point-is-not-the-entry-point", "ImportAPI importApi = new(userName, password, webServiceUrl, option);"),
			Outcome("a-target-typed-authenticator-with-one-argument-is-not-a-credential-pair", "HttpBasicAuthenticator authenticator = new(userName);"),
			Outcome("a-target-typed-type-with-only-the-scheme-word-is-not-an-authenticator", "BasicParser parser = new(userName, password);"),
			Outcome("a-target-typed-header-with-another-type-is-not-the-header-type", Lines("HeaderCollection header = new(\"Basic\", encoded);", _ASSIGN_THE_HEADER_VALUE)),
			Outcome("a-target-typed-credential-with-one-argument-is-not-a-credential", "NetworkCredential credential = new(userName);"),
			Outcome("a-qualified-credential-with-one-argument-is-not-a-credential", "var credential = new System.Net.NetworkCredential(userName);"),
			Outcome(
				"a-handler-initializer-carrying-the-default-credentials-is-integrated-authentication",
				"var handler = new System.Net.Http.HttpClientHandler() { Credentials = CredentialCache.DefaultCredentials };"),
			Outcome("a-handler-initializer-using-the-default-credentials-flag-is-integrated-authentication", "HttpClientHandler handler = new() { UseDefaultCredentials = true };"),
			Outcome("a-mail-client-initializer-carrying-the-default-credentials-is-integrated-authentication", "var mailer = new SmtpClient(host, port) { Credentials = CredentialCache.DefaultCredentials };"),
			Outcome("a-mail-client-initializer-using-the-default-credentials-flag-is-integrated-authentication", "SmtpClient mailer = new(host, port) { UseDefaultCredentials = true };"),
			Outcome("a-mail-client-initializer-binding-a-credential-held-in-a-variable-is-read-where-the-variable-is-built", "var mailer = new SmtpClient(host, port) { Credentials = credential };"),
			Outcome("a-target-typed-mail-client-followed-by-the-default-credentials", Lines("SmtpClient mailer = new(host, port);", "mailer.Credentials = CredentialCache.DefaultCredentials;")),
			Outcome(
				"a-longer-mail-client-name-is-not-the-mail-client-and-leaves-the-credential-as-cs008",
				"var mailer = new System.Net.Mail.SmtpClientWrapper(host) { Credentials = new NetworkCredential(userName, password) };",
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome(
				"a-longer-handler-name-is-not-a-handler-and-leaves-the-credential-as-cs008",
				"var handler = new Vendor.HttpClientHandlerFactory { Credentials = new NetworkCredential(userName, password) };",
				(_CS_008, ConfidenceLevel.Medium)),
			Outcome("a-declared-type-longer-than-the-handler-allow-list-is-not-a-handler", "HttpClientFactory handler = new() { Credentials = CredentialCache.DefaultCredentials };"),
			Outcome("a-client-created-with-another-credential-is-not-the-legacy-client", "RSAPIClient client = new(rsapiUri, bearerCredentials);"),
			Outcome("a-wcf-binding-set-to-another-credential-type-is-not-the-basic-binding", "binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Windows;")
		];
	}
}
