using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// Part A of VB.NET says, for each class, what the tool reads and what it does not, and a reader acts on those sentences: a form said to be unread is
/// searched for by hand, and a form said to be read is trusted to be reported. Each sentence of that kind must therefore be true of the registered rule.
///
/// The claims below are a table: the section whose prose makes the claim, a phrase copied from that prose, an invented input that shows the claim, and
/// what the analyzer must do with it (read at a level, or not read, by the named class). Three guards keep the table honest. Every claim's phrase must
/// still stand in its section, so a rewritten sentence is re-checked; every class must have a claim of what it reads; and every clause of a section that
/// says a form is not read, or is still read, must hold a claim or an explicit entry in the table of claims that no input can show, so a new sentence of
/// that kind fails until it is verified or excused.
///
/// Every input is invented from the prose, with placeholder names.
/// </summary>
public sealed class VBNetPartAProseClaimTests
{
	private const string _VB_001 = "VB-001";
	private const string _VB_002 = "VB-002";
	private const string _VB_003 = "VB-003";
	private const string _VB_004 = "VB-004";
	private const string _VB_005 = "VB-005";
	private const string _VB_006 = "VB-006";
	private const string _VB_007 = "VB-007";
	private const string _VB_008 = "VB-008";
	private const string _VB_009 = "VB-009";
	private const string _VB_010 = "VB-010";
	private const string _VB_011 = "VB-011";
	private const string _VB_012 = "VB-012";
	private const string _VB_013 = "VB-013";
	private const string _VB_014 = "VB-014";
	private const string _VB_015 = "VB-015";
	private const string _VB_016 = "VB-016";
	private const string _VB_017 = "VB-017";
	private const string _VB_018 = "VB-018";
	private const string _VB_019 = "VB-019";

	private const string _PARAGRAPH_BREAK = "\n\n";
	private const string _LINE_BREAK = "\n";
	private const string _FENCE_MARKER = "```";
	private const string _CLAUSE_BOUNDARY_PATTERN = @"(?<=\.)\s+|;\s+";

	/// <summary>The clause shapes that say a form is, or is not, read: the claims the table must cover.</summary>
	private const string _READ_CLAIM_PATTERN =
		@"\b(?:not|never)\s+(?:be\s+)?(?:read|reported|match|matched|a half|a credential half|this header)\b|\bdoes not match\b|\bdo not stop\b|\bno (?:finding|match)\b|\bproduces no finding\b|\bstill (?:read|reported)\b|\bis read (?:as|only)\b|\bare read\b|\bis not Basic\b";

	/// <summary>Paragraphs of a class section that are not prose about what is read: the labelled blocks and the anchors.</summary>
	private static readonly ImmutableArray<string> _nonProseParagraphPrefixes =
	[
		"**Example.**", "**How to search.**", "**Which flow.**", "<a id="
	];

	private static readonly Regex _clauseBoundary = new(_CLAUSE_BOUNDARY_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _readClaim = new(_READ_CLAIM_PATTERN, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

	/// <summary>One checkable sentence: the section it is in, the phrase that locates it, the input, and what the named class must do with it.</summary>
	private sealed record ProseClaim(string Section, string Phrase, string PatternClass, bool IsRead, ConfidenceLevel Level, string Source);

	/// <summary>A clause no input can show, stated in the open so the guard does not pass over it silently.</summary>
	private sealed record UncheckableClaim(string Section, string Phrase, string Reason);

	private static ProseClaim Reads(string section, string phrase, ConfidenceLevel level, string source)
	{
		return new ProseClaim(section, phrase, section, true, level, source);
	}

	private static ProseClaim ReadsAs(string section, string phrase, string patternClass, ConfidenceLevel level, string source)
	{
		return new ProseClaim(section, phrase, patternClass, true, level, source);
	}

	private static ProseClaim DoesNotRead(string section, string phrase, string source)
	{
		return new ProseClaim(section, phrase, section, false, ConfidenceLevel.Low, source);
	}

	private static ProseClaim DoesNotReadAs(string section, string phrase, string patternClass, string source)
	{
		return new ProseClaim(section, phrase, patternClass, false, ConfidenceLevel.Low, source);
	}

	private static readonly ImmutableArray<ProseClaim> _claims =
	[
		// VB-001
		Reads(_VB_001, "Both argument forms count", ConfidenceLevel.High, "Dim credentials As Credentials = New UsernamePasswordCredentials(userName, password)"),
		Reads(_VB_001, "values written literally into the code", ConfidenceLevel.High, "Dim credentials As Credentials = New UsernamePasswordCredentials(\"widget\", \"placeholder\")"),
		Reads(_VB_001, "The type may be written bare, qualified, with `Global.` in front", ConfidenceLevel.High, "Dim a = New Vendor.Sdk.UsernamePasswordCredentials(userName, password)\nDim b = New Global.Vendor.UsernamePasswordCredentials(userName, password)"),
		Reads(_VB_001, "through an `Imports` alias of its namespace", ConfidenceLevel.High, "Imports Sdk = Vendor.Credentials\nDim a = New Sdk.UsernamePasswordCredentials(userName, password)"),
		Reads(_VB_001, "created standing alone, as an argument", ConfidenceLevel.High, "factory.Connect(New UsernamePasswordCredentials(userName, password))"),
		Reads(_VB_001, "after `As New`", ConfidenceLevel.High, "Dim a As New UsernamePasswordCredentials(userName, password)"),
		Reads(_VB_001, "by an assignment after a bare `Dim`", ConfidenceLevel.High, "Dim a\na = New UsernamePasswordCredentials(userName, password)"),
		Reads(_VB_001, "Arguments may be named in order", ConfidenceLevel.High, "Dim a = New UsernamePasswordCredentials(userName:=widgetUser, password:=widgetPwd)"),
		Reads(_VB_001, "the call may be continued over several lines", ConfidenceLevel.High, "Dim a = New UsernamePasswordCredentials(userName, _\n\tpassword)"),
		DoesNotRead(_VB_001, "A credential type of the same SDK that carries a bearer token does not match", "Dim a = New BearerTokenCredentials(accessToken)"),
		ReadsAs(_VB_001, "VB-002 is reported instead of this class", _VB_002, ConfidenceLevel.High, "Dim a = New RSAPIClient(rsapiUri, New UsernamePasswordCredentials(userName, password))"),
		DoesNotRead(_VB_001, "VB-002 is reported instead of this class", "Dim a = New RSAPIClient(rsapiUri, New UsernamePasswordCredentials(userName, password))"),
		DoesNotRead(_VB_001, "is not reported when it is written without a namespace", "Class UsernamePasswordCredentials\nEnd Class\nDim a = New UsernamePasswordCredentials(userName, password)"),
		Reads(_VB_001, "written with a namespace it is still read", ConfidenceLevel.High, "Class UsernamePasswordCredentials\nEnd Class\nDim a = New Vendor.UsernamePasswordCredentials(userName, password)"),
		DoesNotRead(_VB_001, "with an object initializer, or with `Activator.CreateInstance`, is not read", "Dim a = New UsernamePasswordCredentials With {.UserName = userName, .Password = password}"),
		DoesNotRead(_VB_001, "with an object initializer, or with `Activator.CreateInstance`, is not read", "Dim a = Activator.CreateInstance(GetType(UsernamePasswordCredentials), userName, password)"),

		// VB-002
		Reads(_VB_002, "built with a nested SDK user name and password credential", ConfidenceLevel.High, "Dim client As New RSAPIClient(rsapiUri, New UsernamePasswordCredentials(userName, password))"),
		DoesNotRead(_VB_002, "A client built with another kind of credential, or obtained without one, does not match", "Dim client As New RSAPIClient(rsapiUri, New BearerTokenCredentials(accessToken))"),
		DoesNotRead(_VB_002, "a client obtained from a generic typed proxy factory with no credential is not read", "Dim client = factory.GetProxy(Of IWidgetManager)()"),

		// VB-003
		Reads(_VB_003, "A credential added to a credential cache under the `Basic` scheme", ConfidenceLevel.High, "cache.Add(New Uri(instanceUrl), \"Basic\", New NetworkCredential(userName, password))"),
		Reads(_VB_003, "The third argument may be a `New NetworkCredential(...)` construction, a call or a variable", ConfidenceLevel.High, "cache.Add(widgetUri, \"Basic\", BuildCredential())\ncache.Add(widgetUri, \"Basic\", credential)"),
		Reads(_VB_003, "The scheme may be a quoted literal in any letter case", ConfidenceLevel.High, "cache.Add(widgetUri, \"BASIC\", credential)"),
		Reads(_VB_003, "a bare constant that the file declares once as `\"Basic\"`", ConfidenceLevel.High, "Const SCHEME As String = \"Basic\"\ncache.Add(widgetUri, SCHEME, credential)"),
		DoesNotRead(_VB_003, "A cache entry whose scheme is another one, such as `Digest` or `NTLM`, is not Basic and is not reported", "cache.Add(widgetUri, \"Digest\", credential)\ncache.Add(widgetUri, \"NTLM\", credential)"),
		ReadsAs(_VB_003, "The four-argument form that takes a host and a port is a mail-server entry and is VB-018", _VB_018, ConfidenceLevel.Low, "cache.Add(widgetHost, 25, \"Basic\", New NetworkCredential(userName, password))"),
		DoesNotRead(_VB_003, "The four-argument form that takes a host and a port is a mail-server entry and is VB-018", "cache.Add(widgetHost, 25, \"Basic\", New NetworkCredential(userName, password))"),
		Reads(_VB_003, "A scheme held in a constant that the tool cannot resolve", ConfidenceLevel.Medium, "cache.Add(widgetUri, SCHEME, credential)"),
		DoesNotRead(_VB_003, "or no match for a qualified name", "cache.Add(widgetUri, Constants.SCHEME, credential)"),

		// VB-004
		Reads(_VB_004, "built with a user name and a password", ConfidenceLevel.High, "Dim authenticator As New HttpBasicAuthenticator(userName, password)"),
		Reads(_VB_004, "The type may be written bare or qualified", ConfidenceLevel.High, "Dim authenticator As New Vendor.Http.HttpBasicAuthenticator(userName, password)"),
		Reads(_VB_004, "assigned to a property such as `client.Authenticator`", ConfidenceLevel.High, "client.Authenticator = New HttpBasicAuthenticator(userName, password)"),
		Reads(_VB_004, "Extra arguments, such as an encoding, do not stop the match", ConfidenceLevel.High, "Dim authenticator As New HttpBasicAuthenticator(userName, password, Encoding.UTF8)"),
		DoesNotRead(_VB_004, "Requiring both words is what keeps it safe, because `Basic` alone is too common", "Dim settings As New BasicConfigurator(userName, password)"),

		// VB-005
		Reads(_VB_005, "It is read as an ordinary call", ConfidenceLevel.High, "Dim configured = request.WithBasicAuth(userName, password)"),
		Reads(_VB_005, "the leading-dot call inside a `With` block", ConfidenceLevel.High, "With request\n\t.WithBasicAuth(userName, password)\nEnd With"),
		DoesNotRead(_VB_005, "with two credential arguments", "Dim configured = request.WithBasicAuth(userName)"),
		DoesNotRead(_VB_005, "a method name that carries the word `Basic` and an authentication word", "Dim configured = request.WithBasicLayout(userName, password)"),

		// VB-006
		Reads(_VB_006, "created with the scheme `Basic`, either with the constructor", ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue(\"Basic\", encodedCredentials)"),
		Reads(_VB_006, "`AuthenticationHeaderValue.Parse(\"Basic \" & credential)`", ConfidenceLevel.Medium, "Dim header = AuthenticationHeaderValue.Parse(\"Basic \" & encodedCredentials)"),
		Reads(_VB_006, "a literal in any letter case or a constant that the file declares once", ConfidenceLevel.Medium, "Dim header = New AuthenticationHeaderValue(\"BASIC\", encodedCredentials)\nConst SCHEME As String = \"Basic\"\nDim other = New AuthenticationHeaderValue(SCHEME, encodedCredentials)"),
		Reads(_VB_006, "inside a `With` block", ConfidenceLevel.Medium, "With request.Headers\n\t.Authorization = New AuthenticationHeaderValue(\"Basic\", encodedCredentials)\nEnd With"),
		DoesNotRead(_VB_006, "A `TryParse`", "Dim header As AuthenticationHeaderValue = Nothing\nAuthenticationHeaderValue.TryParse(\"Basic \" & encodedCredentials, header)"),
		DoesNotRead(_VB_006, "a `Parse` of a variable that the tool cannot see the scheme of", "Dim header = AuthenticationHeaderValue.Parse(headerText)"),
		DoesNotRead(_VB_006, "A scheme that resolves to `Bearer` gives no finding", "Dim header = New AuthenticationHeaderValue(\"Bearer\", accessToken)"),

		// VB-007
		Reads(_VB_007, "A write of the `Authorization` header by name, with a Basic value", ConfidenceLevel.Medium, "client.Headers.Add(\"Authorization\", \"Basic \" & encodedCredentials)"),
		Reads(_VB_007, "`.TryAddWithoutValidation`", ConfidenceLevel.Medium, "client.DefaultRequestHeaders.TryAddWithoutValidation(\"Authorization\", \"Basic \" & encodedCredentials)"),
		Reads(_VB_007, "`.SetRequestHeader` on a late-bound HTTP object", ConfidenceLevel.Medium, "http.SetRequestHeader(\"Authorization\", \"Basic \" & encodedCredentials)"),
		Reads(_VB_007, "a parenthesized index", ConfidenceLevel.Medium, "request.Headers(\"Authorization\") = String.Format(\"Basic {0}\", encodedCredentials)"),
		Reads(_VB_007, "the single-string overload", ConfidenceLevel.Medium, "client.Headers.Add(\"Authorization: Basic \" & encodedCredentials)"),
		Reads(_VB_007, "a named constant whose name carries the word `Authorization`", ConfidenceLevel.Medium, "client.Headers.Add(AUTHORIZATION_HEADER, \"Basic \" & encodedCredentials)"),
		Reads(_VB_007, "an interpolated string", ConfidenceLevel.Medium, "client.Headers.Add(\"Authorization\", $\"Basic {encodedCredentials}\")"),
		Reads(_VB_007, "or a variable assigned one of these in the five statements above the write", ConfidenceLevel.Medium, "Dim value As String = \"Basic \" & encodedCredentials\nclient.Headers.Add(\"Authorization\", value)"),
		Reads(_VB_007, "`.Set`, `.Append`", ConfidenceLevel.Medium, "headers.Set(\"Authorization\", \"Basic \" & encodedCredentials)\nheaders.Append(\"Authorization\", \"Basic \" & encodedCredentials)"),
		Reads(_VB_007, "`.AddHeader`", ConfidenceLevel.Medium, "restRequest.AddHeader(\"Authorization\", \"Basic \" & encodedCredentials)"),
		Reads(_VB_007, "a literal scheme joined to an expression with `&` or `+`", ConfidenceLevel.Medium, "client.Headers.Add(\"Authorization\", \"Basic \" + encodedCredentials)"),
		Reads(_VB_007, "`String.Format`, `String.Concat` or `String.Join`", ConfidenceLevel.Medium, "client.Headers.Add(\"Authorization\", String.Concat(\"Basic \", encodedCredentials))\nclient.Headers.Add(\"Authorization\", String.Join(\" \", \"Basic\", encodedCredentials))"),
		Reads(_VB_007, "a constant scheme joined to a credential", ConfidenceLevel.Medium, "Const SCHEME As String = \"Basic\"\nclient.Headers.Add(\"Authorization\", SCHEME & \" \" & encodedCredentials)"),
		Reads(_VB_007, "a call to a function whose name carries the scheme word", ConfidenceLevel.Medium, "client.Headers.Add(\"Authorization\", BuildBasicValue(userName))"),
		Reads(_VB_007, "each optionally through `.DefaultRequestHeaders`", ConfidenceLevel.Medium, "client.DefaultRequestHeaders.Add(\"Authorization\", \"Basic \" & encodedCredentials)"),
		DoesNotRead(_VB_007, "a read of `Request.Headers(\"Authorization\")` is inbound authentication and is not reported", "Dim incoming = Request.Headers(\"Authorization\")"),
		DoesNotRead(_VB_007, "so `Proxy-Authorization` and `X-Authorization` do not match", "client.Headers.Add(\"Proxy-Authorization\", \"Basic \" & encodedCredentials)\nclient.Headers.Add(\"X-Authorization\", \"Basic \" & encodedCredentials)"),
		Reads(_VB_007, "is read only where the name carries a credential word", ConfidenceLevel.Medium, "client.Headers.Add(\"Authorization\", widgetPassword)"),
		DoesNotRead(_VB_007, "A name that carries the word `bearer` or `access_token` is read as a bearer value and is not reported", "client.Headers.Add(\"Authorization\", bearerPassword)\nclient.Headers.Add(\"Authorization\", access_token)"),
		DoesNotRead(_VB_007, "A value held under a neutral name is not read", "client.Headers.Add(\"Authorization\", widgetValue)"),

		// VB-008
		Reads(_VB_008, "made through the framework's header enumeration", ConfidenceLevel.Medium, "client.Headers(HttpRequestHeader.Authorization) = \"Basic \" & encoded"),
		Reads(_VB_008, "or `.Add(HttpRequestHeader.Authorization, ...)`", ConfidenceLevel.Medium, "request.Headers.Add(HttpRequestHeader.Authorization, String.Format(\"Basic {0}\", encoded))"),
		Reads(_VB_008, "may be written qualified or partly qualified", ConfidenceLevel.Medium, "client.Headers(System.Net.HttpRequestHeader.Authorization) = \"Basic \" & encoded\nclient.Headers(Net.HttpRequestHeader.Authorization) = \"Basic \" & encoded"),
		Reads(_VB_008, "the leading-dot form inside a `With` block is read", ConfidenceLevel.Medium, "With client.Headers\n\t.Add(HttpRequestHeader.Authorization, \"Basic \" & encoded)\nEnd With"),
		DoesNotRead(_VB_008, "another member of the enumeration, such as `ProxyAuthorization`, is not this header", "client.Headers(HttpRequestHeader.ProxyAuthorization) = \"Basic \" & encoded"),
		DoesNotRead(_VB_008, "A member reached through an `Imports` of the enumeration type, without its name, is not read", "Imports System.Net.HttpRequestHeader\nclient.Headers(Authorization) = \"Basic \" & encoded"),

		// VB-009
		Reads(_VB_009, "whose transport credential type is Basic", ConfidenceLevel.Medium, "binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic"),
		Reads(_VB_009, "A binding whose credential lives in a configuration file is still a migration target and is still reported", ConfidenceLevel.Medium, "binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic\nfactory.Open()"),
		Reads(_VB_009, "The member may be written qualified or partly qualified", ConfidenceLevel.Medium, "binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Basic\nbinding.Security.Transport.ClientCredentialType = ServiceModel.HttpClientCredentialType.Basic"),
		Reads(_VB_009, "the leading-dot form inside `With binding.Security.Transport` is read", ConfidenceLevel.Medium, "With binding.Security.Transport\n\t.ClientCredentialType = HttpClientCredentialType.Basic\nEnd With"),
		DoesNotRead(_VB_009, "A member reached through an `Imports` of the enumeration, without its name, is not read", "Imports System.ServiceModel.HttpClientCredentialType\nbinding.Security.Transport.ClientCredentialType = Basic"),

		// VB-010
		Reads(_VB_010, "built directly from a user name and a password", ConfidenceLevel.High, "Dim importApi As New ImportAPI(userName, password, webServiceUrl)"),
		Reads(_VB_010, "The type name may be written bare, qualified, through an `Imports` alias of its namespace, or as a longer name that ends in `ImportAPI`", ConfidenceLevel.High, "Dim a As New Vendor.ImportAPI(userName, password, webServiceUrl)\nDim b As New WidgetImportAPI(userName, password, webServiceUrl)"),
		DoesNotRead(_VB_010, "The class reads the three-argument constructor only", "Dim importApi As New ImportAPI(webServiceUrl)"),
		DoesNotRead(_VB_010, "the static factory `CreateByRsaBearerToken(webServiceUrl)`", "Dim importApi = ImportAPI.CreateByRsaBearerToken(webServiceUrl)"),
		DoesNotRead(_VB_010, "a token-named value is not the second half of a pair", "Dim importApi As New ImportAPI(userName, accessToken, webServiceUrl)"),
		Reads(_VB_010, "through an `Imports` alias of its namespace", ConfidenceLevel.High, "Imports Sdk = Vendor.Import\nDim importApi As New Sdk.ImportAPI(userName, password, webServiceUrl)"),
		Reads(_VB_010, "A type of another vendor whose name ends in `ImportAPI` and takes three arguments is read as this one", ConfidenceLevel.High, "Dim a As New OtherVendorImportAPI(first, second, third)"),
		Reads(_VB_010, "declared as a class or structure in the same file, is still reported here", ConfidenceLevel.High, "Class ImportAPI\nEnd Class\nDim importApi As New ImportAPI(userName, password, webServiceUrl)"),
		Reads(_VB_010, "when it is spelled as a call whose name carries `Token`, is still reported as a user-and-password pair", ConfidenceLevel.High, "Dim importApi As New ImportAPI(userName, GetSystemToken(), webServiceUrl)"),

		// VB-011
		Reads(_VB_011, "written on a receiver", ConfidenceLevel.High, "settings.RelativityUsername = userName\nsettings.RelativityPassword = password"),
		Reads(_VB_011, "as the leading-dot assignment inside a `With` block", ConfidenceLevel.High, "With importJob.Settings\n\t.RelativityUsername = userName\n\t.RelativityPassword = password\nEnd With"),
		Reads(_VB_011, "or as an object initializer", ConfidenceLevel.High, "Dim settings As New ImportSettings With {.RelativityUsername = userName, .RelativityPassword = password}"),
		Reads(_VB_011, "in either order", ConfidenceLevel.High, "settings.RelativityPassword = password\nsettings.RelativityUsername = userName"),
		DoesNotRead(_VB_011, "A file that assigns only one of the two properties is not reported", "settings.RelativityUsername = userName"),

		// VB-012
		Reads(_VB_012, "given to an HTTP type", ConfidenceLevel.Medium, "Dim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(userName, password)}"),
		Reads(_VB_012, "so the type is listed", ConfidenceLevel.Medium, "Dim a As New WebRequestHandler()\na.Credentials = New NetworkCredential(userName, password)"),
		Reads(_VB_012, "`SocketsHttpHandler`", ConfidenceLevel.Medium, "Dim a As New SocketsHttpHandler()\na.Credentials = New NetworkCredential(userName, password)"),
		Reads(_VB_012, "`WinHttpHandler`", ConfidenceLevel.Medium, "Dim a As New WinHttpHandler()\na.Credentials = New NetworkCredential(userName, password)"),
		Reads(_VB_012, "`WebClient`", ConfidenceLevel.Medium, "Dim client As New WebClient()\nclient.Credentials = New NetworkCredential(userName, password)"),
		Reads(_VB_012, "`HttpWebRequest`", ConfidenceLevel.Medium, "Dim request As HttpWebRequest = WebRequest.Create(widgetUri)\nrequest.Credentials = New NetworkCredential(userName, password)"),
		Reads(_VB_012, "`HttpWebClientProtocol`", ConfidenceLevel.Medium, "Dim proxy As HttpWebClientProtocol = factory.Build()\nproxy.Credentials = New NetworkCredential(userName, password)"),
		Reads(_VB_012, "`SoapHttpClientProtocol`", ConfidenceLevel.Medium, "Dim proxy As SoapHttpClientProtocol = factory.Build()\nproxy.Credentials = New NetworkCredential(userName, password)"),
		Reads(_VB_012, "a `CType`, `DirectCast` or `TryCast`", ConfidenceLevel.Medium, "Dim request = DirectCast(WebRequest.Create(widgetUri), HttpWebRequest)\nrequest.Credentials = New NetworkCredential(userName, password)"),
		Reads(_VB_012, "a `With New T` or `With CType(..., T)` block", ConfidenceLevel.Medium, "With New WebClient()\n\t.Credentials = New NetworkCredential(userName, password)\nEnd With"),
		Reads(_VB_012, "The credential may also be built with an object initializer that carries both halves", ConfidenceLevel.Medium, "Dim client As New WebClient()\nclient.Credentials = New NetworkCredential With {.UserName = userName, .Password = password}"),
		DoesNotRead(_VB_012, "A credential that is `CredentialCache.DefaultCredentials`, or `UseDefaultCredentials = True`, is integrated Windows authentication and is not reported", "Dim client As New WebClient()\nclient.Credentials = CredentialCache.DefaultCredentials\nclient.UseDefaultCredentials = True"),
		DoesNotRead(_VB_012, "A receiver whose type is not written in the five statements above is not read as HTTP", "widgetThing.Credentials = New NetworkCredential(userName, password)"),
		ReadsAs(_VB_012, "and its credential falls to VB-013", _VB_013, ConfidenceLevel.Medium, "widgetThing.Credentials = New NetworkCredential(userName, password)"),
		DoesNotRead(_VB_012, "A credential held in a variable and assigned later", "Dim credential = GetCredential()\nDim client As New WebClient()\nclient.Credentials = credential"),
		DoesNotRead(_VB_012, "a credential built across several statements, are not read", "Dim client As New WebClient()\nDim credential = New NetworkCredential()\ncredential.UserName = userName\ncredential.Password = password\nclient.Credentials = credential"),

		// VB-013
		Reads(_VB_013, "A `NetworkCredential` built from a user name and a password that no other class in the file accounts for", ConfidenceLevel.Medium, "Dim credential = New NetworkCredential(userName, password)"),
		Reads(_VB_013, "with named arguments", ConfidenceLevel.Medium, "Dim credential = New NetworkCredential(userName:=widgetUser, password:=widgetPwd)"),
		Reads(_VB_013, "with an object initializer that carries both halves", ConfidenceLevel.Medium, "Dim credential = New NetworkCredential With {.UserName = userName, .Password = password}"),
		DoesNotRead(_VB_013, "a credential that is an argument of a credential-cache `Add` with a scheme", "cache.Add(widgetUri, \"Basic\", New NetworkCredential(userName, password))"),
		DoesNotRead(_VB_013, "a credential built with a third argument, which is a domain and marks integrated Windows authentication", "Dim credential = New NetworkCredential(userName, password, domainName)"),
		DoesNotRead(_VB_013, "a credential that VB-003, VB-012, VB-016 or VB-018 already accounts for", "Dim mailer As New SmtpClient(host, port)\nmailer.Credentials = New NetworkCredential(userName, password)"),
		DoesNotRead(_VB_013, "a credential that VB-003, VB-012, VB-016 or VB-018 already accounts for", "Dim client As New WebClient()\nclient.Credentials = New NetworkCredential(userName, password)"),
		DoesNotRead(_VB_013, "a credential that VB-003, VB-012, VB-016 or VB-018 already accounts for", "My.Computer.Network.DownloadFile(address, destinationPath, New NetworkCredential(userName, password), False, 100000, True)"),
		Reads(_VB_013, "a share, a mail server or a directory object whose type the tool does not list, is reported at Medium", ConfidenceLevel.Medium, "shareConnection.Connect(New NetworkCredential(userName, password))"),
		Reads(_VB_013, "A credential used only as a local container is reported too", ConfidenceLevel.Medium, "Dim container = New NetworkCredential(userName, password)\nDim shown = container.UserName"),

		// VB-014
		Reads(_VB_014, "A user name and a password joined by a colon and encoded to Base64", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\" & password))"),
		Reads(_VB_014, "The pair may be assembled by `&` or `+`", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + \":\" + password))"),
		Reads(_VB_014, "by `String.Format(\"{0}:{1}\", ...)`", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format(\"{0}:{1}\", userName, password)))"),
		Reads(_VB_014, "by `String.Concat`", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Concat(userName, \":\", password)))"),
		Reads(_VB_014, "by `String.Join(\":\", ...)`", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Join(\":\", userName, password)))"),
		Reads(_VB_014, "or by an interpolated string", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes($\"{userName}:{password}\"))"),
		Reads(_VB_014, "in a variable assigned in the five statements above it", ConfidenceLevel.Medium, "Dim pair As String = userName & \":\" & password\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		Reads(_VB_014, "with any encoding between the pair and the encoder", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.UTF8.GetBytes(userName & \":\" & password))"),
		Reads(_VB_014, "A half is a variable, a member access, a call or an index", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(settings.UserName & \":\" & GetPassword() & \"\"))"),
		DoesNotRead(_VB_014, "a quoted literal is not a half, which is what separates this class from VB-015", "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(\"widget\" & \":\" & password))"),
		DoesNotRead(_VB_014, "A half whose last member is `Port` is not a credential half", "Dim pair As String = userName.Port & \":\" & password\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		DoesNotRead(_VB_014, "and neither is a second half whose last member is `Length`, `Count` or `Size`", "Dim pair As String = userName & \":\" & password.Length\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		DoesNotRead(_VB_014, "and neither is a second half whose last member is `Length`, `Count` or `Size`", "Dim pair As String = String.Format(\"{0}:{1}\", userName, password.Length)\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		Reads(_VB_014, "the check is not applied to a first half ending in `Length`, `Count` or `Size` joined with `&` or `+`", ConfidenceLevel.Medium, "Dim pair As String = hostName.Length & \":\" & password\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		Reads(_VB_014, "the check is not applied to a first half ending in `Length`, `Count` or `Size` joined with `&` or `+`", ConfidenceLevel.Medium, "Dim pair As String = hostName.Length + \":\" + password\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		Reads(_VB_014, "to a half followed by `.ToString()`", ConfidenceLevel.Medium, "Dim pair As String = userName & \":\" & password.Length.ToString()\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		Reads(_VB_014, "to a second half ending in `Port` written inside the encoder call", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\" & password.Port))"),
		Reads(_VB_014, "or to a second half ending in `Length`, `Count` or `Size` written inside the encoder call with `&`, `+`, an interpolated string or `String.Concat`", ConfidenceLevel.Medium, "Dim a As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\" & password.Length))\nDim b As String = Convert.ToBase64String(Encoding.ASCII.GetBytes($\"{userName}:{password.Length}\"))\nDim c As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Concat(userName, \":\", password.Count)))"),
		DoesNotRead(_VB_014, "inside the encoder call it is not read at all", "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\"c & password))"),
		DoesNotReadAs(_VB_014, "inside the encoder call it is not read at all", _VB_015, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\"c & password))"),
		DoesNotRead(_VB_014, "in a pair assembled before the encoder it is reported as VB-015 instead", "Dim pair As String = userName & \":\"c & password\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		ReadsAs(_VB_014, "in a pair assembled before the encoder it is reported as VB-015 instead", _VB_015, ConfidenceLevel.Medium, "Dim pair As String = userName & \":\"c & password\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		DoesNotRead(_VB_014, "so a pair encoded by your own helper is not read", "Dim encoded As String = WidgetEncoder.Encode(userName & \":\" & password)"),
		Reads(_VB_014, "A value of the same shape that is not a credential, such as a host and a port, can be reported", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(hostName & \":\" & endpointNumber))"),
		DoesNotRead(_VB_014, "A pair assembled across several statements is not read", "Dim pair As String = userName\npair &= \":\"\npair &= password\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),

		// VB-015
		Reads(_VB_015, "a pair with one empty half is encoded to Base64", ConfidenceLevel.Medium, "Dim pair As String = \"\" & \":\" & accessToken\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		Reads(_VB_015, "`String.Empty`", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Empty & \":\" & accessToken))"),
		Reads(_VB_015, "`vbNullString`", ConfidenceLevel.Medium, "Dim pair As String = vbNullString & \":\" & accessToken\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		DoesNotRead(_VB_015, "`vbNullString` as the first operand of a pair joined with `&` or `+` inside the encoder call is not read as an empty half", "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(vbNullString & \":\" & accessToken))"),
		ReadsAs(_VB_015, "the pair is reported as VB-014, an encoded user name and password", _VB_014, ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(vbNullString & \":\" & accessToken))"),
		Reads(_VB_015, "A user name and password joined with a character-literal colon and assembled before the encoder are reported here, although neither half is empty", ConfidenceLevel.Medium, "Dim pair As String = userName & \":\"c & password\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		Reads(_VB_015, "a variable assigned an empty literal in the ten statements above", ConfidenceLevel.Medium, "Dim blank As String = \"\"\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(blank & \":\" & accessToken))"),
		Reads(_VB_015, "or no operand at all, with the colon first or last", ConfidenceLevel.Medium, "Dim first As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(\":\" & accessToken))\nDim last As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(accessToken & \":\"))"),
		Reads(_VB_015, "including `String.Format`, `String.Concat`, `String.Join` and interpolation", ConfidenceLevel.Medium, "Dim a = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format(\"{0}:{1}\", \"\", accessToken)))\nDim b = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Concat(\"\", \":\", accessToken)))\nDim c = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Join(\":\", \"\", accessToken)))\nDim d = Convert.ToBase64String(Encoding.ASCII.GetBytes($\":{accessToken}\"))"),
		DoesNotReadAs(_VB_015, "This class takes the place of VB-014 on the same encoder", _VB_014, "Dim pair As String = \"\" & \":\" & accessToken\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))"),
		DoesNotRead(_VB_015, "An encoder called by your own helper is not read", "Dim encoded As String = WidgetEncoder.Encode(\"\" & \":\" & accessToken)"),
		Reads(_VB_015, "A value of the same shape that is not a credential, such as a host and a port, can be reported", ConfidenceLevel.Medium, "Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(\":\" & endpointNumber))"),

		// VB-016
		Reads(_VB_016, "`My.Computer.Network.DownloadFile(address, destinationPath, userName, password)`", ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(address, destinationPath, userName, password, False, 100000, True)"),
		Reads(_VB_016, "or `UploadFile`", ConfidenceLevel.Medium, "My.Computer.Network.UploadFile(sourcePath, address, userName, password)"),
		Reads(_VB_016, "the leading-dot call inside a `With My.Computer.Network` block", ConfidenceLevel.Medium, "With My.Computer.Network\n\t.DownloadFile(address, destinationPath, userName, password)\nEnd With"),
		Reads(_VB_016, "with named arguments", ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(address:=widgetAddress, destinationFileName:=destinationPath, userName:=widgetUser, password:=widgetPwd)"),
		Reads(_VB_016, "with an overload that takes a credential object", ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(address, destinationPath, New NetworkCredential(userName, password), False, 100000, True)\nMy.Computer.Network.UploadFile(sourcePath, address, networkCredential, False, 100000)"),
		DoesNotRead(_VB_016, "A call to an `ftp://` address, written as a literal or held in a constant, is not reported", "My.Computer.Network.DownloadFile(\"ftp://example.invalid/widget\", destinationPath, userName, password)"),
		DoesNotRead(_VB_016, "A call to an `ftp://` address, written as a literal or held in a constant, is not reported", "Const FEED As String = \"ftp://example.invalid/widget\"\nMy.Computer.Network.DownloadFile(FEED, destinationPath, userName, password)"),
		Reads(_VB_016, "or a variable whose name carries a credential word", ConfidenceLevel.Medium, "My.Computer.Network.DownloadFile(address, destinationPath, widgetCredential, False, 100000, True)"),
		DoesNotRead(_VB_016, "the network class of the runtime library created directly are not read", "Dim transfer As New Microsoft.VisualBasic.Devices.Network()\ntransfer.DownloadFile(address, destinationPath, userName, password)"),

		// VB-017
		Reads(_VB_017, "`http.Open(\"GET\", url, False, userName, password)`", ConfidenceLevel.Medium, "Dim http As Object = CreateObject(\"MSXML2.ServerXMLHTTP.6.0\")\nhttp.Open(\"GET\", url, False, userName, password)"),
		Reads(_VB_017, "or `http.SetCredentials(userName, password, 0)`", ConfidenceLevel.Medium, "http.SetCredentials(userName, password, 0)"),
		Reads(_VB_017, "whatever the object is created with", ConfidenceLevel.Medium, "Dim http = Server.CreateObject(\"MSXML2.ServerXMLHTTP\")\nhttp.Open(\"GET\", url, False, userName, password)"),
		Reads(_VB_017, "whatever the object is created with", ConfidenceLevel.Medium, "Dim http = Activator.CreateInstance(Type.GetTypeFromProgID(\"MSXML2.XMLHTTP\"))\nhttp.Open(\"GET\", url, False, userName, password)"),
		Reads(_VB_017, "whatever the object is created with", ConfidenceLevel.Medium, "With CreateObject(\"MSXML2.XMLHTTP\")\n\t.Open(\"GET\", url, False, userName, password)\nEnd With"),
		Reads(_VB_017, "Arguments may be named", ConfidenceLevel.Medium, "http.Open(method:=\"GET\", url:=widgetUrl, async:=False, user:=widgetUser, password:=widgetPwd)"),
		DoesNotRead(_VB_017, "An `Open` whose method is held in a variable", "http.Open(verb, url, False, userName, password)"),
		DoesNotRead(_VB_017, "an `Open` with fewer than five arguments, are not read", "http.Open(\"GET\", url, False)"),
		Reads(_VB_017, "a five-argument `Open` with an HTTP verb is read whatever the object comes from", ConfidenceLevel.Medium, "connection.Open(\"POST\", url, False, userName, password)"),
		ReadsAs(_VB_017, "The `setRequestHeader(\"Authorization\", ...)` call on such an object is VB-007", _VB_007, ConfidenceLevel.Medium, "http.setRequestHeader(\"Authorization\", \"Basic \" & encoded)"),
		DoesNotRead(_VB_017, "The `setRequestHeader(\"Authorization\", ...)` call on such an object is VB-007", "http.setRequestHeader(\"Authorization\", \"Basic \" & encoded)"),

		// VB-018
		Reads(_VB_018, "a mail client (`SmtpClient`)", ConfidenceLevel.Low, "Dim mailer As New SmtpClient(host, port)\nmailer.Credentials = New NetworkCredential(userName, password)"),
		Reads(_VB_018, "a directory object (`DirectoryEntry`, `DirectorySearcher`)", ConfidenceLevel.Low, "Dim searcher As New DirectorySearcher(New DirectoryEntry(directoryPath, userName, password))"),
		Reads(_VB_018, "a web proxy (`WebProxy`)", ConfidenceLevel.Low, "Dim proxy As New WebProxy(proxyAddress)\nproxy.Credentials = New NetworkCredential(userName, password)"),
		Reads(_VB_018, "also obtained through a cast of `WebRequest.Create`", ConfidenceLevel.Low, "Dim request = DirectCast(WebRequest.Create(widgetUri), FtpWebRequest)\nrequest.Credentials = New NetworkCredential(userName, password)"),
		Reads(_VB_018, "a directory object (`DirectoryEntry`, `DirectorySearcher`)", ConfidenceLevel.Low, "Dim entry As New DirectoryEntry(directoryPath, userName, password)"),
		Reads(_VB_018, "a process started with a user name and a password (`ProcessStartInfo`, `Process.Start`)", ConfidenceLevel.Low, "Dim info As New ProcessStartInfo()\ninfo.UserName = userName\ninfo.Password = password"),
		Reads(_VB_018, "a process started with a user name and a password (`ProcessStartInfo`, `Process.Start`)", ConfidenceLevel.Low, "Process.Start(fileName, userName, password, domainName)"),
		Reads(_VB_018, "It also covers the credential-cache entry that carries a host and a port", ConfidenceLevel.Low, "cache.Add(widgetHost, 25, \"Basic\", New NetworkCredential(userName, password))"),
		DoesNotRead(_VB_018, "A credential built across several statements and then given to a mail client produces no finding", "Dim mailer As New SmtpClient(host, port)\nDim credential = New NetworkCredential()\ncredential.UserName = userName\ncredential.Password = password\nmailer.Credentials = credential"),
		ReadsAs(_VB_018, "A non-HTTP consumer outside this closed list yields a VB-013 finding at Medium", _VB_013, ConfidenceLevel.Medium, "shareConnection.Connect(New NetworkCredential(userName, password))"),
		DoesNotReadAs(_VB_018, "This class takes the place of VB-013 on the same credential", _VB_013, "Dim mailer As New SmtpClient(host, port)\nmailer.Credentials = New NetworkCredential(userName, password)"),

		// VB-019
		Reads(_VB_019, "The definition of the code's own authentication helper", ConfidenceLevel.Low, "Private Function BuildBasicAuthHeader(userName As String) As String\nEnd Function\nDim credential As String = Nothing"),
		Reads(_VB_019, "a `Function` or `Sub` whose name carries the word `Basic` and an authentication word", ConfidenceLevel.Low, "Public Sub ApplyBasicHeader(request As Object)\nEnd Sub\nDim credential As String = Nothing"),
		Reads(_VB_019, "with any of the modifiers `Private`, `Public`, `Friend`, `Shared` and `Async`", ConfidenceLevel.Low, "Friend Shared Async Function GetBasicAuthorization() As Task(Of String)\nEnd Function\nDim credential As String = Nothing"),
		DoesNotRead(_VB_019, "The class reads the declaration, not the calls", "Dim header = GetBasicAuthorization(userName)\nDim credential As String = Nothing"),
		DoesNotRead(_VB_019, "a `Function` or `Sub` whose name carries the word `Basic` and an authentication word", "Private Function BuildBasicLayout(userName As String) As String\nEnd Function\nDim credential As String = Nothing")
	];

	/// <summary>Clauses of a section that say a form is read or not read and that no input can show.</summary>
	private static readonly ImmutableArray<UncheckableClaim> _uncheckableClaims =
	[
		new(_VB_001, "which is separate from the level and is not removed by a Low level", "A statement about the secret warning and the level, checked with the reporting and demotion tests, not a recognition outcome."),
		new(_VB_009, "within five statements, that assignment is the credential the finding points at", "A statement about which location a finding points at, not whether the class reads."),
		new(_VB_013, "or the BCL performs the exchange and no `Basic` and no Base64 appear in the file", "A description of what the consumer is, not of a form the rule reads or declines."),
		new(_VB_015, "The defect is the scheme, not the token", "A statement about the defect, not a recognition outcome."),
		new(_VB_015, "Some spellings of the empty half or of the assembly are not read", "A pointer to the declared unsupported coverage, whose entries the declared-coverage tests hold to the analyzer's own list."),
		new(_VB_018, "The tool does not believe the credential is Basic Authentication towards RelativityOne", "A statement about the tool's judgement of the credential, checked as the class's Low level."),
		new(_VB_019, "so a helper in a file with no other authentication word may not be reported", "Hedged with 'may', and file preselection is a step of the scan pipeline that the recognition harness does not apply."),
		new(_VB_019, "note that its call sites do not appear as findings, because the helper hides them", "Advice to the reader, not a recognition outcome; the call itself is claimed not read above."),
		new(_VB_019, "A helper that builds the header also matches the header classes, at another location", "A statement about two findings at two places; the header classes are claimed with their own forms.")
	];

	/// <summary>Every claim of the table holds of the registered rules: what is said to be read is read at the level stated, and what is said to be unread is not.</summary>
	[Fact]
	public void EveryProseClaimHoldsOfTheRegisteredRules()
	{
		ImmutableArray<string>.Builder failures = ImmutableArray.CreateBuilder<string>();

		foreach (ProseClaim claim in _claims)
		{
			ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(claim.Source);
			bool isRead = constructs.Any(construct => construct.PatternClassIdentifier == claim.PatternClass && (!claim.IsRead || construct.Level == claim.Level));

			if (isRead != claim.IsRead)
			{
				string expectation = claim.IsRead ? $"read at {claim.Level}" : "not read";
				failures.Add($"{claim.Section} says \"{claim.Phrase}\": {claim.PatternClass} should be {expectation} on [{claim.Source}]; got [{Describe(constructs)}]");
			}
		}

		Assert.True(failures.Count == 0, string.Join(_LINE_BREAK, failures));
	}

	/// <summary>Each claim's phrase still stands in its own section, so a sentence that is reworded is verified again.</summary>
	[Fact]
	public void EveryClaimPhraseStillStandsInItsSectionOfPartA()
	{
		ImmutableArray<string>.Builder missing = ImmutableArray.CreateBuilder<string>();

		foreach (string phrase in _claims.Select(claim => (claim.Section, claim.Phrase)).Concat(_uncheckableClaims.Select(claim => (claim.Section, claim.Phrase))).Distinct().Select(pair => $"{pair.Section}|{pair.Phrase}"))
		{
			string[] parts = phrase.Split('|', 2);

			if (!ProseOf(parts[0]).Contains(PublicDocumentChecks.NormalizeWhitespace(parts[1]), StringComparison.Ordinal))
			{
				missing.Add($"{parts[0]}: \"{parts[1]}\"");
			}
		}

		Assert.True(missing.Count == 0, "These phrases are no longer in Part A:\n" + string.Join(_LINE_BREAK, missing));
	}

	/// <summary>Every class says what it reads, and the table has a claim of it that the analyzer confirms.</summary>
	[Fact]
	public void EveryPartAClassHasAClaimOfWhatItReads()
	{
		ImmutableArray<string> withoutClaim =
		[
			.. DocumentedLanguage.VBNet.Analyzer().DetectionRulesByPatternClass.Keys
				.Where(patternClass => !_claims.Any(claim => claim.Section == patternClass && claim.IsRead && claim.PatternClass == patternClass))
				.Order(StringComparer.Ordinal)
		];

		Assert.Empty(withoutClaim);
		Assert.Equal(19, _claims.Where(claim => claim.IsRead && claim.PatternClass == claim.Section).Select(claim => claim.Section).Distinct().Count());
	}

	/// <summary>Every clause of a section that says a form is read or not read holds a claim of the table, or an entry that excuses it with its reason.</summary>
	[Fact]
	public void EveryClauseSayingAFormIsReadOrNotReadIsCoveredByAClaim()
	{
		ImmutableArray<string>.Builder uncovered = ImmutableArray.CreateBuilder<string>();

		foreach (string patternClass in DocumentedLanguage.VBNet.Analyzer().DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal))
		{
			foreach (string clause in ClaimClausesOf(patternClass))
			{
				bool covered =
					_claims.Any(claim => claim.Section == patternClass && clause.Contains(PublicDocumentChecks.NormalizeWhitespace(claim.Phrase), StringComparison.Ordinal))
					|| _uncheckableClaims.Any(claim => claim.Section == patternClass && clause.Contains(PublicDocumentChecks.NormalizeWhitespace(claim.Phrase), StringComparison.Ordinal));

				if (!covered)
				{
					uncovered.Add($"{patternClass}: {clause}");
				}
			}
		}

		Assert.True(uncovered.Count == 0, "These clauses of Part A say what is read or not read and no claim covers them:\n" + string.Join(_LINE_BREAK, uncovered));
	}

	/// <summary>The clauses of a section's description and Confidence paragraphs that say a form is read or not read.</summary>
	private static ImmutableArray<string> ClaimClausesOf(string patternClass)
	{
		return
		[
			.. _clauseBoundary.Split(ProseOf(patternClass))
				.Select(clause => clause.Trim())
				.Where(clause => _readClaim.IsMatch(clause))
		];
	}

	/// <summary>The prose of one class section, whitespace-normalized: its description and its Confidence paragraph, without the labelled blocks and the code.</summary>
	private static string ProseOf(string patternClass)
	{
		DetectionRule rule = DocumentedLanguage.VBNet.Analyzer().DetectionRulesByPatternClass[patternClass];
		string partA = File.ReadAllText(DocumentedLanguage.VBNet.PartAPath());
		string section = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(patternClass, rule.DisplayName)) ?? string.Empty;
		List<string> prose = [];

		foreach (string paragraph in section.Split(_PARAGRAPH_BREAK))
		{
			string trimmed = paragraph.Trim();

			if (trimmed.Length > 0 && !trimmed.StartsWith(_FENCE_MARKER, StringComparison.Ordinal) && !_nonProseParagraphPrefixes.Any(prefix => trimmed.StartsWith(prefix, StringComparison.Ordinal)))
			{
				prose.Add(trimmed);
			}
		}

		return PublicDocumentChecks.NormalizeWhitespace(string.Join(' ', prose));
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));
	}
}
