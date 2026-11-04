using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE TERMINATION AND AGREEMENT CHECKS OF THE `VBNet` EXPRESSIONS, run over the REGISTERED rules of the shipped analyzer
/// (<see cref="VBNetRegisteredExpressions"/>), on the pattern of <see cref="CSharpExpressionTerminationTests"/>. The interpreted matcher is built from the
/// pattern text of each registered rule, so what is proved is about the binary and not about a copy.
///
/// THE THREE CHECKS. (a) Every expression TERMINATES on the worst-case inputs of its own windows: a window that exhausts its bound and then fails its
/// continuation, repeated, at the line-length ceiling. (b) Every expression AGREES between the generator and the interpreter on the same pattern, in the
/// index and length of every match and of every named group, over the positive fixtures, the near-misses and the window openers. (c) A structural scan of
/// every pattern states which lazy bounded loops it carries and whether any sits in an OPTIONAL group (a group followed by `?`, `*`, `{0,` or holding an empty
/// alternative): the `[GeneratedRegex]` defect, a lazy bounded loop wrapped in an optional group, is never present.
/// </summary>
public sealed partial class VBNetExpressionTerminationTests
{
	internal const string EXPOSURE_REPORT_VARIABLE_NAME = "MIGRATE_AUTH_EXPOSURE_REPORT_VBNET";

	private const int _MATCH_ENUMERATION_CAP = 5_000;

	private const int _LINE_LENGTH_CEILING = 9_900;

	private readonly ITestOutputHelper _output;

	public VBNetExpressionTerminationTests(ITestOutputHelper output)
	{
		_output = output;
	}

	// ------------------------------------------------------------------------------------------
	// The fixtures: one positive form per class and the near-misses the catalog names, generic text
	// ------------------------------------------------------------------------------------------

	public static TheoryData<string, string, string, string> Fixtures()
	{
		TheoryData<string, string, string, string> data = [];

		foreach ((string expression, string text, string group, string expected) in _fixtures)
		{
			data.Add(expression, text, group, expected);
		}

		return data;
	}

	/// <summary>Expression, text, the group to read ("-" for none), and the expected capture ("" for a match with no capture asserted, "NOMATCH" for none).</summary>
	private static readonly ImmutableArray<(string Expression, string Text, string Group, string Expected)> _fixtures =
	[
		("VB-001", "Dim c As New UsernamePasswordCredentials(userName, password)", "credential", "password"),
		("VB-001", "Dim c As Credentials = New Relativity.Sdk.UsernamePasswordCredentials(\"u\", \"p\")", "credential", "\"p\""),
		("VB-001", "Dim c = New UsernamePasswordCredentials(userName,\n\tpassword)", "credential", "password"),
		("VB-001", "Dim c = New UsernamePasswordCredentials(userName)", "-", "NOMATCH"),
		("VB-002", "Dim c As New RSAPIClient(rsapiUri, New UsernamePasswordCredentials(userName, password))", "credential", "password"),
		("VB-002", "Dim c As New RSAPIClient(rsapiUri, New BearerTokenCredentials(token))", "-", "NOMATCH"),
		("VB-003", "cache.Add(New Uri(root), \"Basic\", New NetworkCredential(user, pass))", "scheme", "Basic"),
		("VB-003", "cache.Add(baseUri, AuthType, GetCredential())", "scheme", "AuthType"),
		("VB-003", "cache.Add(New Uri(root), \"Digest\", New NetworkCredential(user, pass))", "scheme", "Digest"),
		("VB-003", "cache.Add(host, 587, \"Basic\", New NetworkCredential(user, pass))", "-", "NOMATCH"),
		("VB-004", "client.Authenticator = New HttpBasicAuthenticator(user, pass)", "credential", "pass"),
		("VB-005", "Dim r = request.WithBasicAuth(user, pass)", "credential", "pass"),
		("VB-006", "h.Authorization = New AuthenticationHeaderValue(\"Basic\", encoded)", "scheme", "Basic"),
		("VB-006", "h.Authorization = New AuthenticationHeaderValue(\n\t\"BASIC\",\n\tEncode(user, pass))", "credential", "Encode(user, pass)"),
		("VB-006", "m.Headers.Authorization = AuthenticationHeaderValue.Parse(\"Basic \" & Encode(user, pass))", "scheme", "Basic"),
		("VB-006", "If Not AuthenticationHeaderValue.TryParse(headerValue, parsed) Then Return False", "-", "NOMATCH"),
		("VB-007", "client.Headers.Add(\"Authorization\", \"Basic \" & encoded)", "scheme", "Basic"),
		("VB-007", "web.Headers(\"Authorization\") = $\"Basic {token}\"", "credential", "token"),
		("VB-007", "message.Headers.TryAddWithoutValidation(\"Authorization\", \"Basic \" & Encode(user, pass))", "credential", "Encode(user, pass)"),
		("VB-007", "req.Headers.Add(\"Authorization: Basic \" & Token(user, password))", "scheme", "Basic"),
		("VB-007", "xhr.setRequestHeader(\"Authorization\", _\n\t\"Basic \" & Convert.ToBase64String(bytes))", "scheme", "Basic"),
		("VB-007", "restRequest.AddHeader(AUTHORIZATION_HEADER, String.Format(\"{0} {1}\", scheme, value))", "scheme", "scheme"),
		("VB-007", "client.Headers.Add(\"Authorization\", authToken)", "credential", "authToken"),
		("VB-007", "client.Headers.Add(\"Authorization\", value)", "-", "NOMATCH"),
		("VB-007", "client.Headers.Add(\"Proxy-Authorization\", \"Basic \" & encoded)", "-", "NOMATCH"),
		("VB-007", "client.Headers.Add(\"Authorization\", \"Bearer \" & token)", "scheme", "Bearer"),
		("VB-007", "Dim v = String.Format(\"Basic {0}\", encoded)\n\tclient.Headers.Add(\"Authorization\", v)", "scheme", "Basic"),
		("VB-008", "client.Headers(HttpRequestHeader.Authorization) = \"Basic \" & encoded", "scheme", "Basic"),
		("VB-008", "req.Headers.Add(HttpRequestHeader.Authorization, String.Format(\"{0} {1}\", \"Basic\", Token(user, password)))", "scheme", "Basic"),
		("VB-008", "client.Headers(HttpRequestHeader.ContentType) = \"text/xml\"", "-", "NOMATCH"),
		("VB-009", "binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic", "construct", "HttpClientCredentialType.Basic"),
		("VB-009", "binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None", "-", "NOMATCH"),
		("VB-010", "Dim api As New ImportAPI(user, pass, url)", "credential", "pass"),
		("VB-010", "api = New ExtendedImportAPI(user, pass, url)", "credential", "pass"),
		("VB-010", "api = New IAPI.ImportAPI(user, pass, url)", "credential", "pass"),
		("VB-010", "api = New ImportAPI(url)", "-", "NOMATCH"),
		("VB-011", "With job.Settings\n\t.RelativityUsername = u\n\t.RelativityPassword = p\nEnd With", "credential", "p"),
		("VB-012", "Dim h As New HttpClientHandler With {.Credentials = New NetworkCredential(user, pass)}", "credential", "pass"),
		("VB-012", "Using client As New WebClient()\n\tclient.Credentials = New NetworkCredential(user, pass)", "credential", "pass"),
		("VB-013", "Dim c As New NetworkCredential(user, pass)", "credential", "pass"),
		("VB-013", "Dim c As New NetworkCredential(user, pass, domain)", "-", "NOMATCH"),
		("VB-013", "cache.Add(New Uri(root), \"Digest\", New NetworkCredential(user, pass))", "-", "NOMATCH"),
		("VB-014", "Dim pair = user & \":\" & password\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "password"),
		("VB-014", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(user & \":\" & pass))", "credential", "pass"),
		("VB-014", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{user}:{pass}\"))", "credential", "pass"),
		("VB-014", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Concat(user, \":\", pass)))", "credential", "pass"),
		("VB-014", "Dim authToken As String = String.Format(\"Basic {0}\", Base64Encode(String.Concat(userName, \":\", password)))", "credential", "password"),
		("VB-015", "Dim pair = \"\" & \":\" & accessToken\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "accessToken"),
		("VB-016", "My.Computer.Network.DownloadFile(address, destination, user, pass)", "credential", "pass"),
		("VB-016", "My.Computer.Network.DownloadFile(\n\tNew Uri(address),\n\tdestination,\n\tuser,\n\tpass,\n\tFalse,\n\t30000,\n\tTrue)", "credential", "pass"),
		("VB-016", "My.Computer.Network.UploadFile(path, address, networkCredential, False, 30000)", "-", ""),
		("VB-017", "xhr.open(\"POST\", endpoint, False, user, pwd)", "credential", "pwd"),
		("VB-017", "xhr.Open(\"GET\", url, False)", "-", "NOMATCH"),
		("VB-018", "Dim m As New SmtpClient(host, 25) With {.Credentials = New NetworkCredential(user, pass)}", "credential", "pass"),
		("VB-018", "Dim r = CType(WebRequest.Create(root & \"x\"), FtpWebRequest)\nr.Method = \"RETR\"\nr.Credentials = New NetworkCredential(user, pass)", "credential", "pass"),
		("VB-018", "cache.Add(host, 587, \"Basic\", New NetworkCredential(user, pass))", "credential", "pass"),
		("VB-019", "\tPublic Function BuildBasicAuthHeader(user As String) As String", "declaration", "BuildBasicAuthHeader"),
		("VB-019", "\tPublic Function Basically(user As String) As String", "-", "NOMATCH"),
		("ConstantDeclaration", "Private Const Scheme As String = \"Basic\"", "name", "Scheme"),
		("ConstantDeclaration", "Dim s$ = \"x\"", "name", "s"),
		("ConstantDeclaration", "Private Const Scheme As String = \"Basic\"", "value", "Basic"),
		("ConstantDeclaration", "Const Doubled = \"a\"\"b\"", "value", "a\"\"b"),
		("ConstantDeclaration", "Const Joined = \"a\" & \"b\"", "-", "NOMATCH"),
		("ConstantDeclaration", "Dim scheme As String\n\tscheme = \"Bearer\"", "value", "Bearer"),
		("ConstantDeclaration", "Dim scheme As String : scheme = \"Bearer\"", "value", "Bearer"),
		("ConstantDeclaration", "If scheme = \"Bearer\" Then", "-", "NOMATCH"),
		("ConstantDeclaration", "x.scheme = \"Bearer\"", "-", "NOMATCH"),
		("ConstantDeclaration", "Static scheme = \"Basic\"", "-", "NOMATCH"),
		("ConstantDeclaration", "Private Const [Scheme] As String = \"Basic\"", "name", "Scheme"),
		("VB-014", "Dim pair = String.Concat(user, \":\", pass)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "pass"),
		("VB-014", "Dim pair = String.Format(\"{0}:{1}\", user, pass)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "pass"),
		("VB-014", "Dim pair = $\"{user}:{pass}\"\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "pass"),
		("VB-014", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Format(\"{0}:{1}\", user, pass)))", "credential", "pass"),
		("VB-014", "Dim pair = name & \":\" & file.Length\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "Dim pair = user & \":\" & password\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "Dim pair = \":\" & apiKey\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "apiKey"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", String.Join(\" \", \"Basic\", sessionBlob))", "scheme", "Basic"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", String.Join(\" \", \"Basic\", sessionBlob))", "credential", "sessionBlob"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", Join(\" \", \"Basic\", sessionBlob))", "credential", "sessionBlob"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", String.Join(\", \", \"Basic\", sessionBlob))", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", String.Join(\" \", sessionBlob, \"Basic\"))", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(HttpRequestHeader.ProxyAuthorization, \"Basic \" & sessionBlob)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(System.Net.HttpRequestHeader.Authorization, \"Basic \" & sessionBlob)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(AUTH_KEY_AUTHORIZATION, \"Basic \" & sessionBlob)", "scheme", "Basic"),
		("VB-008", "With widgetClient.Headers\n\t.Add(HttpRequestHeader.Authorization, \"Basic \" & sessionBlob)\nEnd With", "scheme", "Basic"),
		("VB-008", "With widgetClient.Headers\n\t.Set(HttpRequestHeader.Authorization, String.Format(\"Basic {0}\", sessionBlob))\nEnd With", "credential", "sessionBlob"),
		("VB-008", "With widgetClient\n\t.Headers.Set(HttpRequestHeader.Authorization, \"Basic \" & sessionBlob)\nEnd With", "scheme", "Basic"),
		("VB-008", "With widgetClient.Headers\n\t.Add(HttpRequestHeader.ContentType, \"Basic \" & sessionBlob)\nEnd With", "-", "NOMATCH"),
		("VB-008", "widgetClient.Headers.Add(header:=HttpRequestHeader.Authorization, value:=\"Basic \" & sessionBlob)", "scheme", "Basic"),
		("VB-008", "widgetClient.Headers.Set(header:=System.Net.HttpRequestHeader.Authorization, value:=\"Basic \" & sessionBlob)", "credential", "sessionBlob"),
		("VB-008", "widgetClient.Headers.Add(HttpRequestHeader.Authorization, value:=\"Basic \" & sessionBlob)", "scheme", "Basic"),
		("VB-008", "Dim hdrValue As String = \"Basic \" & sessionBlob\nwidgetClient.Headers(HttpRequestHeader.Authorization) = hdrValue", "scheme", "Basic"),
		("VB-008", "Dim hdrValue As String = \"Basic \" & sessionBlob\nwidgetClient.Headers.Add(HttpRequestHeader.Authorization, hdrValue)", "credential", "sessionBlob"),
		("VB-008", "Dim hdrValue As String = \"Basic \" & sessionBlob\nwidgetClient.Headers.Set(header:=HttpRequestHeader.Authorization, value:=hdrValue)", "scheme", "Basic"),
		("VB-008", "hdrValue = \"Basic \" & sessionBlob\n\tDim filler0 = 0\n\tDim filler1 = 1\n\tDim filler2 = 2\n\tDim filler3 = 3\nwidgetClient.Headers(HttpRequestHeader.Authorization) = hdrValue", "scheme", "Basic"),
		("VB-008", "hdrValue = \"Basic \" & sessionBlob\n\tDim filler0 = 0\n\tDim filler1 = 1\n\tDim filler2 = 2\n\tDim filler3 = 3\n\tDim filler4 = 4\nwidgetClient.Headers(HttpRequestHeader.Authorization) = hdrValue", "-", "NOMATCH"),
		("VB-008", "Dim hdrValue As String = \"Bearer \" & sessionBlob\nwidgetClient.Headers(HttpRequestHeader.Authorization) = hdrValue", "scheme", "Bearer"),
		("VB-008", "Dim hdrValue As String = sessionBlob\nwidgetClient.Headers(HttpRequestHeader.Authorization) = hdrValue", "-", "NOMATCH"),
		("VB-008", "Dim hdrValue As String = \"Basic \" & sessionBlob\nwidgetClient.Headers(HttpRequestHeader.Authorization) = otherValue", "-", "NOMATCH"),
		("VB-015", "Dim seed As String = \"\"\nDim pair = seed & \":\" & sessionBlob\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim seed As String = \"\"\n\tDim filler0 = 0\nDim pair = seed & \":\" & sessionBlob\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "seed = \"\"\n\tDim filler0 = 0\n\tDim filler1 = 1\n\tDim filler2 = 2\n\tDim filler3 = 3\n\tDim filler4 = 4\n\tDim filler5 = 5\n\tDim filler6 = 6\n\tDim filler7 = 7\npair = seed & \":\" & sessionBlob\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "seed = \"\"\n\tDim filler0 = 0\n\tDim filler1 = 1\n\tDim filler2 = 2\n\tDim filler3 = 3\n\tDim filler4 = 4\n\tDim filler5 = 5\n\tDim filler6 = 6\n\tDim filler7 = 7\n\tDim filler8 = 8\n\tDim filler9 = 9\n\tDim filler10 = 10\npair = seed & \":\" & sessionBlob\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "Dim seed = String.Empty\nDim pair = sessionBlob & \":\" & seed\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim seed = vbNullString\nDim pair = String.Format(\"{0}:{1}\", seed, sessionBlob)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim seed = \"\"\nDim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(seed & \":\" & sessionBlob))", "credential", "sessionBlob"),
		("VB-015", "Dim seed = \"\"\nDim pair = seed & \":\" & sessionBlob\nDim other = 1", "-", "NOMATCH"),
		("VB-015", "If seed = \"\" Then\n\tpair = seed & \":\" & sessionBlob\nEnd If\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "Dim seed = \"x\"\nDim pair = seed & \":\" & sessionBlob\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "Dim seed = \"\" & other\nDim pair = seed & \":\" & sessionBlob\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "Dim pair = String.Format(\"{0}:{1}\", String.Empty, sessionBlob)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = String.Format(\"{0}:{1}\", sessionBlob, String.Empty)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = String.Format(\"{0}:{1}\", \"\", sessionBlob)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = String.Format(\"{0}:{1}\", vbNullString, sessionBlob)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = $\"{String.Empty}:{sessionBlob}\"\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = $\"{sessionBlob}:{vbNullString}\"\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = String.Concat(\"\", \":\", sessionBlob)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = String.Concat(sessionBlob, \":\", String.Empty)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = String.Join(\":\", String.Empty, sessionBlob)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = sessionBlob & \":\" & String.Empty\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = sessionBlob + \":\" + \"\"\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Format(\"{0}:{1}\", String.Empty, sessionBlob)))", "credential", "sessionBlob"),
		("VB-015", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{sessionBlob}:{String.Empty}\"))", "credential", "sessionBlob"),
		("VB-015", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(sessionBlob & \":\" & \"\"))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = String.Format(\"{0}:{1}\", leftName, sessionBlob)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "Dim pair = String.Concat(leftName, \":\", sessionBlob)\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "Dim pair = String.Format(\"{0}:{1}\", String.Empty, sessionBlob)", "-", "NOMATCH"),
		("VB-015", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(\":\" & sessionBlob))", "credential", "sessionBlob"),
		("VB-015", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(\":\" + FetchBlob()))", "credential", "FetchBlob"),
		("VB-015", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(\":\" & sessionBlob & suffix))", "-", "NOMATCH"),
		("VB-015", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(leftName & \":\" & sessionBlob))", "-", "NOMATCH"),
		("VB-015", "Dim pair = sessionBlob & \":\"\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = FetchBlob() & \":\"\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "FetchBlob"),
		("VB-015", "Dim pair = sessionBlob + \":\"\n\tDim filler0 = 0\n\tDim filler1 = 1\n\tDim filler2 = 2\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-015", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(sessionBlob & \":\"))", "credential", "sessionBlob"),
		("VB-015", "Dim pair = sessionBlob & \":\" & suffix\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "Dim pair = sessionBlob & \":\" & _\n\tsuffix\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "Dim pair = prefixName & sessionBlob & \":\"\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "Dim label = sessionBlob & \":\"\nDim shown = label.Trim()", "-", "NOMATCH"),
		("VB-015", "Dim e = Convert.ToBase64String(Encoding.UTF8.GetBytes(sessionBlob & \":\" & suffix))", "-", "NOMATCH"),
		("VB-015", "pair = pair & \":\"\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-015", "pair = sessionBlob & \":\"\nDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "credential", "sessionBlob"),
		("VB-012", "Dim h As New SocketsHttpHandler With {.Credentials = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret}}", "credential", "widgetSecret"),
		("VB-012", "Dim h As New SocketsHttpHandler With {.Credentials = New NetworkCredential With {.Password = widgetSecret, .UserName = widgetUser}}", "credential", "widgetSecret"),
		("VB-012", "Dim h As New HttpClientHandler With {.Credentials = New NetworkCredential() With {.UserName = widgetUser, .Password = widgetSecret}}", "credential", "widgetSecret"),
		("VB-012", "Dim h As New HttpClientHandler With {.Credentials = New NetworkCredential With {\n\t.UserName = widgetUser,\n\t.Password = widgetSecret\n}}", "credential", "widgetSecret"),
		("VB-012", "Dim w = New WebClient()\nw.Credentials = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret}", "credential", "widgetSecret"),
		("VB-012", "Dim h As New HttpClientHandler With {.Credentials = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret, .Domain = widgetDomain}}", "-", "NOMATCH"),
		("VB-012", "Dim h As New HttpClientHandler With {.Credentials = New NetworkCredential With {.UserName = widgetUser}}", "-", "NOMATCH"),
		("VB-012", "Dim h As New WidgetHandler With {.Credentials = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret}}", "-", "NOMATCH"),
		("VB-012", "With New WebClient()\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "credential", "widgetSecret"),
		("VB-012", "With CType(widgetRequest, HttpWebRequest)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "credential", "widgetSecret"),
		("VB-012", "With DirectCast(widgetRequest, System.Net.HttpWebRequest)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "credential", "widgetSecret"),
		("VB-012", "With TryCast(widgetRequest, HttpWebRequest)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "credential", "widgetSecret"),
		("VB-012", "With CType(widgetFactory.Create(address), HttpWebRequest)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "credential", "widgetSecret"),
		("VB-012", "CType(widgetRequest, HttpWebRequest).Credentials = New NetworkCredential(widgetUser, widgetSecret)", "credential", "widgetSecret"),
		("VB-012", "DirectCast(widgetRequest, HttpWebRequest).Credentials = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret}", "credential", "widgetSecret"),
		("VB-012", "Dim proxy As SoapHttpClientProtocol = New WidgetProxy()\nproxy.Credentials = New NetworkCredential(widgetUser, widgetSecret)", "credential", "widgetSecret"),
		("VB-012", "Dim proxy As HttpWebClientProtocol = New WidgetProxy()\nproxy.Credentials = New NetworkCredential(widgetUser, widgetSecret)", "credential", "widgetSecret"),
		("VB-012", "Dim proxy As New SoapHttpClientProtocol()\nproxy.Credentials = New NetworkCredential With {.Password = widgetSecret, .UserName = widgetUser}", "credential", "widgetSecret"),
		("VB-012", "With New WidgetClient()\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "-", "NOMATCH"),
		("VB-012", "With CType(widgetRequest, WidgetRequest)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "-", "NOMATCH"),
		("VB-012", "CType(widgetRequest, SmtpClient).Credentials = New NetworkCredential(widgetUser, widgetSecret)", "-", "NOMATCH"),
		("VB-012", "With widgetClient\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "-", "NOMATCH"),
		("VB-012", "With New WebClient()\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret, widgetDomain)\nEnd With", "-", "NOMATCH"),
		("VB-012", "Dim proxy As SoapHttpClientProtocol = New WidgetProxy()\nproxy.Credentials = CredentialCache.DefaultCredentials", "-", "NOMATCH"),
		("VB-013", "Dim c = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret}", "credential", "widgetSecret"),
		("VB-013", "Dim c = New NetworkCredential With {.Password = widgetSecret, .UserName = widgetUser}", "credential", "widgetSecret"),
		("VB-013", "Dim c As New NetworkCredential() With {.UserName = widgetUser, .Password = widgetSecret}", "credential", "widgetSecret"),
		("VB-013", "Dim c As New NetworkCredential With {\n\t.UserName = widgetUser,\n\t.Password = widgetSecret\n}", "credential", "widgetSecret"),
		("VB-013", "consumer.Connect(address, New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret})", "credential", "widgetSecret"),
		("VB-013", "Dim c = New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret, .Domain = widgetDomain}", "-", "NOMATCH"),
		("VB-013", "Dim c = New NetworkCredential With {.UserName = widgetUser}", "-", "NOMATCH"),
		("VB-013", "Dim c = New NetworkCredential With {.Password = widgetSecret}", "-", "NOMATCH"),
		("VB-013", "cache.Add(New Uri(root), \"Digest\", New NetworkCredential With {.UserName = widgetUser, .Password = widgetSecret})", "-", "NOMATCH"),
		("VB-017", "http.Open(bstrMethod:=\"GET\", bstrUrl:=address, varAsync:=False, bstrUser:=widgetUser, bstrPassword:=widgetPwd)", "credential", "widgetPwd"),
		("VB-017", "http.Open(bstrMethod:=\"PUT\", _\n\tbstrUrl:=address, _\n\tvarAsync:=False, _\n\tbstrUser:=widgetUser, _\n\tbstrPassword:=widgetPwd)", "credential", "widgetPwd"),
		("VB-017", "http.Open(\"POST\", address, varAsync:=True, bstrUser:=widgetUser, bstrPassword:=widgetPwd)", "credential", "widgetPwd"),
		("VB-017", "http.SetCredentials(bstrUserName:=widgetUser, bstrPassword:=widgetPwd, Flags:=0)", "credential", "widgetPwd"),
		("VB-017", "http.SetCredentials(widgetUser, bstrPassword:=widgetPwd, Flags:=HTTPREQUEST_SETCREDENTIALS_FOR_SERVER)", "credential", "widgetPwd"),
		("VB-017", "http.Open(bstrMethod:=\"LOCK\", bstrUrl:=address, varAsync:=False, bstrUser:=widgetUser, bstrPassword:=widgetPwd)", "-", "NOMATCH"),
		("VB-017", "http.Open(bstrMethod:=\"GET\", bstrUrl:=address, varAsync:=False, bstrUser:=widgetUser)", "-", "NOMATCH"),
		("VB-017", "http.Open(bstrMethod:=verb, bstrUrl:=address, varAsync:=False, bstrUser:=widgetUser, bstrPassword:=widgetPwd)", "-", "NOMATCH"),
		("VB-017", "http.SetCredentials(bstrUserName:=widgetUser)", "-", "NOMATCH"),
		("VB-018", "With New SmtpClient(widgetHost, 25)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "credential", "widgetSecret"),
		("VB-018", "With New WebProxy(address)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "credential", "widgetSecret"),
		("VB-018", "With New System.Net.Mail.SmtpClient(widgetHost, 25)\n\t.EnableSsl = True\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "credential", "widgetSecret"),
		("VB-018", "Dim r = DirectCast(FtpWebRequest.Create(address), FtpWebRequest)\nr.Credentials = New NetworkCredential(widgetUser, widgetSecret)", "credential", "widgetSecret"),
		("VB-018", "Dim r = TryCast(System.Net.FtpWebRequest.Create(address), FtpWebRequest)\nr.Credentials = New NetworkCredential(widgetUser, widgetSecret)", "credential", "widgetSecret"),
		("VB-018", "Dim r = CType(WebRequest.Create(address), FtpWebRequest)\nr.Credentials = New NetworkCredential(widgetUser, widgetSecret)", "credential", "widgetSecret"),
		("VB-018", "Dim r = DirectCast(FtpWebRequest.Create(address), WidgetRequest)\nr.Credentials = New NetworkCredential(widgetUser, widgetSecret)", "-", "NOMATCH"),
		("VB-018", "With New WidgetClient(widgetHost, 25)\n\t.Credentials = New NetworkCredential(widgetUser, widgetSecret)\nEnd With", "-", "NOMATCH"),
		("VB-018", "Dim entry As New DirectoryEntry(widgetPath, widgetUser, widgetSecret)", "credential", "widgetSecret"),
		("VB-018", "Dim entry = New System.DirectoryServices.DirectoryEntry(widgetPath, widgetUser, widgetSecret, AuthenticationTypes.Secure)", "credential", "widgetSecret"),
		("VB-018", "Dim entry As New DirectoryEntry(path:=widgetPath, username:=widgetUser, password:=widgetSecret)", "credential", "widgetSecret"),
		("VB-018", "Dim finder As New DirectorySearcher(New DirectoryEntry(widgetPath, widgetUser, widgetSecret))", "credential", "widgetSecret"),
		("VB-018", "Dim entry As New DirectoryEntry(widgetPath)", "-", "NOMATCH"),
		("VB-018", "Dim entry As New DirectoryEntry(widgetPath, widgetUser)", "-", "NOMATCH"),
		("VB-018", "Dim finder As New DirectorySearcher(widgetEntry)", "-", "NOMATCH"),
		("VB-018", "Dim psi As New ProcessStartInfo(widgetExe) With {.UserName = widgetUser, .Password = widgetSecret}", "credential", "widgetSecret"),
		("VB-018", "Dim psi As New ProcessStartInfo(widgetExe) With {.Password = widgetSecret, .UserName = widgetUser}", "credential", "widgetSecret"),
		("VB-018", "Dim psi As New ProcessStartInfo(widgetExe)\npsi.UserName = widgetUser\npsi.Password = widgetSecret", "credential", "widgetSecret"),
		("VB-018", "Dim psi As ProcessStartInfo = New ProcessStartInfo(widgetExe)\npsi.Password = widgetSecret\npsi.UserName = widgetUser", "credential", "widgetSecret"),
		("VB-018", "With New ProcessStartInfo(widgetExe)\n\t.UserName = widgetUser\n\t.Password = widgetSecret\nEnd With", "credential", "widgetSecret"),
		("VB-018", "Process.Start(widgetExe, widgetUser, widgetSecret, widgetDomain)", "credential", "widgetSecret"),
		("VB-018", "Dim psi As New ProcessStartInfo(widgetExe)\npsi.UserName = widgetUser", "-", "NOMATCH"),
		("VB-018", "Dim psi As New ProcessStartInfo(widgetExe) With {.FileName = widgetExe, .Arguments = widgetArgs}", "-", "NOMATCH"),
		("VB-018", "Process.Start(widgetExe, widgetArgs)", "-", "NOMATCH"),
		("VB-018", "Dim psi As New WidgetInfo(widgetExe)\npsi.UserName = widgetUser\npsi.Password = widgetSecret", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", authToken)", "-", ""),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", token)", "-", ""),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", sessionBearishToken)", "-", ""),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", unbearableToken)", "-", ""),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", accessoryToken)", "-", ""),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", accessCredential)", "-", ""),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", encodedCredential)", "-", ""),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", settings.apiKey)", "-", ""),
		("VB-007", "widgetClient.Headers(\"Authorization\") = authToken", "-", ""),
		("VB-014", "Dim pair = widgetHost & \":\" & passwordPassport\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", ""),
		("VB-014", "Dim pair = widgetHost & \":\" & passwordPortal\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", ""),
		("VB-014", "Dim pair = widgetHost & \":\" & PasswordPassport\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", ""),
		("VB-014", "Dim pair = widgetHost & \":\" & secretPortalName\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", ""),
		("VB-014", "Dim pair = widgetHost & \":\" & tokenSupportCode\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", ""),
		("VB-014", "Dim pair = widgetHost & \":\" & widgetPassword\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", ""),
		("VB-014", "Dim pair = widgetHost & \":\" & secretReport\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", ""),
		("VB-014", "Dim encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{widgetHost}:{passwordPassport}\"))", "-", ""),
		("VB-001", "Public Class WidgetHolder\nEnd Class\nDim c = New Vendor.Sdk.UsernamePasswordCredentials(userName, password)", "-", ""),
		("VB-001", "Public Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\nDim c = New Vendor.Sdk.UsernamePasswordCredentials(userName, password)", "-", ""),
		("VB-001", "Public Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\nDim c = New Global.Vendor.Sdk.UsernamePasswordCredentials(userName, password)", "-", ""),
		("VB-001", "Public Class UsernamePasswordCredentialsFactory\nEnd Class\nDim c = New UsernamePasswordCredentials(userName, password)", "-", ""),
		("VB-002", "Public Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\nDim c As New RSAPIClient(rsapiUri, New Vendor.Sdk.UsernamePasswordCredentials(userName, password))", "-", ""),
		("VB-010", "Dim api As New ImportAPI(userName, password, webServiceUrl)", "-", ""),
		("VB-010", "Dim api As New ImportAPI(userName, Me.ImportPassword, webServiceUrl)", "-", ""),
		("VB-010", "Dim api As New ImportAPI(userName, tokenPassword, webServiceUrl)", "-", ""),
		("VB-010", "Dim api As New ImportAPI(userName, userSecretToken, webServiceUrl)", "-", ""),
		("VB-010", "Dim api As New ImportAPI(userName, pwd, webServiceUrl)", "-", ""),
		("VB-010", "Dim api As New ImportAPI(userName, \"literal\", webServiceUrl)", "-", ""),
		("VB-010", "Dim api As New ImportAPI(userName, password:=secret, webServiceUrl)", "-", ""),
		("VB-016", "My.Computer.Network.DownloadFile(\"https://host.test/file.bin\", destination, userName, password)", "-", ""),
		("VB-016", "My.Computer.Network.DownloadFile(\"http://host.test/file.bin\", destination, userName, password)", "-", ""),
		("VB-016", "My.Computer.Network.DownloadFile(widgetAddress, destination, userName, password)", "-", ""),
		("VB-016", "My.Computer.Network.DownloadFile(New Uri(\"https://host.test/file.bin\"), destination, userName, password)", "-", ""),
		("VB-016", "My.Computer.Network.DownloadFile(address:=\"https://host.test/f\", destination, userName, password)", "-", ""),
		("VB-016", "Private Const widgetFeed As String = \"https://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)", "-", ""),
		("VB-016", "Private Const widgetFeed As String = \"ftp://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(otherFeed, destination, userName, password)", "-", ""),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", bearerToken)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", BearerHeader)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", accessToken)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", AccessToken)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", access_token)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", ACCESS_TOKEN)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", bearer_value)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", userBearer)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", BEARER_TOKEN)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", settings.AccessToken)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", Me.userBearer)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", bearer)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers.Add(\"Authorization\", Access_Token)", "-", "NOMATCH"),
		("VB-007", "widgetClient.Headers(\"Authorization\") = bearerToken", "-", "NOMATCH"),
		("VB-014", "Dim pair = widgetHost & \":\" & port\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-014", "Dim pair = widgetHost & \":\" & portNumber\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-014", "Dim pair = widgetHost & \":\" & portNo\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-014", "Dim pair = widgetHost & \":\" & PortNum\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-014", "Dim pair = widgetHost & \":\" & server_port\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-014", "Dim pair = widgetHost & \":\" & SERVER_PORT\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-014", "Dim pair = widgetHost & \":\" & Me.WidgetPort\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-014", "Dim pair = widgetHost & \":\" & settings.PORT_NUMBER\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-014", "Dim encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(widgetHost & \":\" & serverPort))", "-", "NOMATCH"),
		("VB-014", "Dim encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{widgetHost}:{portNumber}\"))", "-", "NOMATCH"),
		("VB-014", "Dim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format(\"{0}:{1}\", widgetHost, portNo)))", "-", "NOMATCH"),
		("VB-014", "Dim pair = String.Concat(widgetHost, \":\", PortNum)\nDim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))", "-", "NOMATCH"),
		("VB-001", "Public Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\nDim c = New UsernamePasswordCredentials(userName, password)", "-", "NOMATCH"),
		("VB-001", "Public Structure UsernamePasswordCredentials\nEnd Structure\nDim c As New UsernamePasswordCredentials(userName, password)", "-", "NOMATCH"),
		("VB-001", "Dim c = New UsernamePasswordCredentials(userName, password)\nPublic Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\n", "-", "NOMATCH"),
		("VB-002", "Public Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\nDim c As New RSAPIClient(rsapiUri, New UsernamePasswordCredentials(userName, password))", "-", "NOMATCH"),
		("VB-010", "Dim api As New ImportAPI(userName, systemToken, webServiceUrl)", "-", "NOMATCH"),
		("VB-010", "Dim api As New ImportAPI(userName, bearerToken, webServiceUrl)", "-", "NOMATCH"),
		("VB-010", "Dim api As New ImportAPI(userName, Me.AuthToken, webServiceUrl)", "-", "NOMATCH"),
		("VB-010", "Dim api As New ImportAPI(userName, TOKEN, webServiceUrl)", "-", "NOMATCH"),
		("VB-010", "Dim api As New ImportAPI(userName, sessionToken, webServiceUrl)", "-", "NOMATCH"),
		("VB-010", "Dim api As New ImportAPI(userName, password:=systemToken, webServiceUrl)", "-", "NOMATCH"),
		("VB-016", "My.Computer.Network.DownloadFile(\"ftp://host.test/file.bin\", destination, userName, password)", "-", "NOMATCH"),
		("VB-016", "My.Computer.Network.DownloadFile(\"ftps://host.test/file.bin\", destination, userName, password)", "-", "NOMATCH"),
		("VB-016", "My.Computer.Network.DownloadFile(\"sftp://host.test/file.bin\", destination, userName, password)", "-", "NOMATCH"),
		("VB-016", "My.Computer.Network.DownloadFile(\"FTP://host.test/file.bin\", destination, userName, password)", "-", "NOMATCH"),
		("VB-016", "My.Computer.Network.DownloadFile(New Uri(\"ftp://host.test/file.bin\"), destination, userName, password)", "-", "NOMATCH"),
		("VB-016", "My.Computer.Network.DownloadFile(New System.Uri(\"ftps://host.test/file.bin\"), destination, userName, password)", "-", "NOMATCH"),
		("VB-016", "My.Computer.Network.DownloadFile(address:=\"ftp://host.test/file.bin\", destination, userName, password)", "-", "NOMATCH"),
		("VB-016", "Private Const widgetFeed As String = \"ftp://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)", "-", "NOMATCH"),
		("VB-016", "Dim widgetFeed As String = \"sftp://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)", "-", "NOMATCH"),
		("VB-016", "Dim widgetFeed = \"ftps://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)", "-", "NOMATCH"),
		("VB-016", "Public Const Widget_FEED As String = \"ftp://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widget_feed, destination, userName, password)", "-", "NOMATCH"),
		("VB-016", "My.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)\nPrivate Const widgetFeed As String = \"ftp://host.test/file.bin\"", "-", "NOMATCH"),
		("VB-016", "Private Const widgetFeed As String = \"ftp://host.test/file.bin\"\nPublic Sub Pull()\n\tDim filler0 = 0\n\tDim filler1 = 1\n\tDim filler2 = 2\n\tMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)\nEnd Sub", "-", "NOMATCH"),
		("VB-016", "Private Const widgetFeed As String = \"https://host.test/file.bin\"\nPrivate Const otherFeed As String = \"ftp://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)", "-", ""),
		("VB-016", "Private Const widgetFeed As String = \"https://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)\nPrivate Const widgetFeedBackup As String = \"ftp://host.test/file.bin\"", "-", ""),
		("VB-016", "With My.Computer.Network\n\t.DownloadFile(\"ftp://host.test/file.bin\", destination, userName, password)\nEnd With", "-", "NOMATCH"),
		("VB-016", "Private Const widgetFeed As String = \"https://host.test/file.bin\"\nMy.Computer.Network.DownloadFile(widgetFeed, destination, userName, password)", "address", "widgetFeed"),
		("VB-010", "Dim api As New ImportAPI(userName, password, webServiceUrl)", "credential", "password"),
		("VB-001", "Public Class UsernamePasswordCredentials\n\tPublic Sub New(a As String, b As String)\n\tEnd Sub\nEnd Class\nDim c = New Vendor.Sdk.UsernamePasswordCredentials(userName, password)", "credential", "password"),
		("VB-017", "http.SetCredentials(user, pwd, 0)", "credential", "pwd"),
		("VB-012", "Dim h = New System.Net.Http.HttpClientHandler()\nh.Credentials = New NetworkCredential(u, p)", "credential", "p"),
		("ConsolePasswordPromptText", "Console.Write(\"Enter your password: \")", "-", ""),
		("ConsoleReadLineIntoCredentialVariable", "Dim password As String = Console.ReadLine()", "-", ""),
		("InputBoxCredentialPrompt", "Dim p = InputBox(\"Image server password:\", \"Sign in\")", "-", ""),
		("WpfPasswordBoxRead", "Dim box As PasswordBox = Nothing\nIf ok Then box.Password = \"\"", "-", ""),
		("WebFormsLoginHandlerBinding", "Sub Click_Handler(s As Object, e As EventArgs) Handles btnConnect.Click\n\tDim p = txtPassword.Text", "-", ""),
		("ConfigurationManagerCredentialRead", "Dim p = ConfigurationManager.AppSettings(\"Vendor.Password\")", "-", ""),
		("ConfigurationIndexerCredentialRead", "Dim p = _settings(PasswordKey)", "-", ""),
		("PlatformInstanceSettingCredentialRead", "Dim v = bundle.GetString(Section, \"Password\")", "-", ""),
		("RelativitySecretStoreRead", "Dim secret = Helper.GetSecretStore().Get(path)", "-", ""),
		("EnvironmentVariableCredentialRead", "Dim p = Environment.GetEnvironmentVariable(\"SERVICE_PASSWORD\")", "-", "")
	];

	[Theory]
	[MemberData(nameof(Fixtures))]
	public void TheRegisteredExpressionReadsTheFormTheCatalogStatesAndDeclinesTheNearMiss(string expression, string text, string group, string expected)
	{
		Regex regex = VBNetRegisteredExpressions.All.Single(pair => pair.Identifier == expression).Expression;
		string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
		ImmutableArray<Match> matches = Enumerate(regex, normalized);

		if (expected == "NOMATCH")
		{
			Assert.True(matches.IsEmpty, $"{expression} matched '{text}' as '{(matches.IsEmpty ? string.Empty : matches[0].Value)}'.");
		}
		else
		{
			Assert.True(!matches.IsEmpty, $"{expression} did not match '{text}'.");

			if (group != "-" && expected.Length > 0)
			{
				Assert.Contains(matches, match => match.Groups[group].Success && Regex.Replace(match.Groups[group].Value, @"\s+", " ").Trim().Equals(expected, StringComparison.OrdinalIgnoreCase));
			}
		}
	}

	// ------------------------------------------------------------------------------------------
	// (a) Termination on the worst-case inputs of each window
	// ------------------------------------------------------------------------------------------

	/// <summary>The text that opens each window of each class, in the form a client file could hold, with nothing that closes it.</summary>
	private static readonly ImmutableArray<string> _openers =
	[
		"client.Headers.Add(\"Authorization\", ",
		"client.Headers(\"Authorization\") = ",
		"client.Headers.Add(HttpRequestHeader.Authorization, ",
		"Dim pair As String = user & \":\" & pass",
		"Dim empty As String = \"\" & \":\" & token",
		"Convert.ToBase64String(Encoding.UTF8.GetBytes(",
		"cache.Add(New Uri(root), \"Basic\", ",
		"Dim handler As New HttpClientHandler",
		"Dim wc As New WebClient()",
		"Dim mailer As New SmtpClient(host, 25)",
		"With New SmtpClient(host, 25)\n\t.Credentials = New NetworkCredential(a, ",
		"With CType(request, HttpWebRequest)\n\t.Credentials = New NetworkCredential With {.UserName = a, ",
		"Dim psi As New ProcessStartInfo(exe) With {.UserName = a, ",
		"Dim entry As New DirectoryEntry(path, a, ",
		"http.Open(bstrMethod:=\"GET\", bstrUrl:=address, varAsync:=False, bstrUser:=a, ",
		"Dim request = CType(WebRequest.Create(address), FtpWebRequest)",
		"New NetworkCredential(a, ",
		"New UsernamePasswordCredentials(a, ",
		"New AuthenticationHeaderValue(\"Basic\", ",
		"With job.Settings\n\t.RelativityUsername = a",
		"binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic",
		"My.Computer.Network.DownloadFile(a, b, ",
		"Private Const addr As String = \"ftp://host.test/f\"\nMy.Computer.Network.DownloadFile(addr, b, c, ",
		"Public Class UsernamePasswordCredentials\nEnd Class\nDim c = New UsernamePasswordCredentials(a, ",
		"Dim api As New ImportAPI(a, bearerToken, ",
		"Dim pair = host & \":\" & portNumber",
		"client.Headers.Add(\"Authorization\", accessToken",
		"http.Open(\"GET\", url, False, ",
		"Dim v = String.Format(\"Basic {0}\", e)",
		"Dim box As PasswordBox = Nothing",
		"Console.Write(\"Enter the password",
		"Dim s = \"multi\nline literal that never closes properly"
	];

	private static readonly ImmutableArray<string> _fillers =
	[
		"\t\tDim total As Integer = first + second\n",
		"\n",
		"\t\tIf a Then b = c ' filler\n",
		"x, y, z, f(g(h(i)))), (((a + b) - c) * d), \"s,s\", _\n",
		"a & _\n\t\t",
		"{ \"k\", ( \"v\" ) } , {\n"
	];

	public static TheoryData<string> OpenerIndexes()
	{
		TheoryData<string> data = [];

		for (int index = 0; index < _openers.Length; index++)
		{
			data.Add(index.ToString(CultureInfo.InvariantCulture));
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(OpenerIndexes))]
	public void EveryRegisteredExpressionTerminatesWhereAWindowExhaustsItsBoundAndTheContinuationFails(string openerIndex)
	{
		string opener = _openers[int.Parse(openerIndex, CultureInfo.InvariantCulture)];

		foreach (string text in WorstCaseTexts(opener))
		{
			foreach ((string identifier, Regex expression) in VBNetRegisteredExpressions.All)
			{
				ImmutableArray<Match> matches = Enumerate(expression, text);

				Assert.True(matches.Length < _MATCH_ENUMERATION_CAP, $"{identifier} did not terminate on a worst-case text after '{opener}'.");
			}
		}
	}

	private static IEnumerable<string> WorstCaseTexts(string opener)
	{
		foreach (string filler in _fillers)
		{
			foreach (int repetitions in new[] { 3, 6, 40, 400 })
			{
				yield return Repeat(opener + "\n" + filler, repetitions);
			}
		}

		yield return opener + new string('x', _LINE_LENGTH_CEILING);
		yield return opener + string.Concat(Enumerable.Repeat("f(", 1_500));
		yield return Repeat(opener, 400);
		yield return Repeat(opener + " _\n", 400);
		yield return Repeat(opener + ",\n", 400);
	}

	/// <summary>
	/// A DIAGNOSTIC, never a gate: for every expression and every worst-case text, the time of the interpreter under a two-second timeout and whether the
	/// source-generated matcher threw, so a slow or defective window is named before the termination check fails on it. Written to the test output.
	/// </summary>
	private static readonly HashSet<string>? DiagnosticFilter = Environment.GetEnvironmentVariable("MIGRATE_AUTH_VBNET_DIAG_ONLY") is { Length: > 0 } only ? [.. only.Split(',')] : null;

	[Fact]
	public void DiagnoseTheSlowestAndTheDefectiveExpressions()
	{
		StringBuilder report = new();

		for (int index = 0; index < _openers.Length; index++)
		{
			int variant = 0;

			foreach (string text in WorstCaseTexts(_openers[index]))
			{
				variant++;

				foreach ((string identifier, Regex generated) in VBNetRegisteredExpressions.All.Where(pair => DiagnosticFilter is null || DiagnosticFilter.Contains(pair.Identifier)))
				{
					Regex interpreted = new(generated.ToString(), generated.Options, TimeSpan.FromSeconds(1));
					System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
					string outcome = "ok";

					try
					{
						_ = Enumerate(interpreted, text);
					}
					catch (RegexMatchTimeoutException)
					{
						outcome = "TIMEOUT>2s";
					}
					catch (Exception exception) when (exception is not Xunit.Sdk.XunitException)
					{
						outcome = exception.GetType().Name;
					}

					watch.Stop();

					string generatedOutcome = "ok";

					try
					{
						_ = generated.Match(text.Length > 3_000 ? text[..3_000] : text);
					}
					catch (RegexMatchTimeoutException)
					{
						generatedOutcome = "TIMEOUT";
					}
					catch (Exception exception)
					{
						generatedOutcome = exception.GetType().Name;
					}

					if (watch.ElapsedMilliseconds > 300 || outcome != "ok" || generatedOutcome != "ok")
					{
						string line = $"{identifier} opener {index} variant {variant} interpreter {watch.ElapsedMilliseconds} ms {outcome} generated {generatedOutcome} length {text.Length}";

						report.AppendLine(line);

						if (Environment.GetEnvironmentVariable("MIGRATE_AUTH_VBNET_DIAG_OUT") is { Length: > 0 } diagnosticPath)
						{
							File.AppendAllText(diagnosticPath, line + "\n");
						}
					}
				}
			}
		}

		_output.WriteLine(report.ToString());
	}

	// ------------------------------------------------------------------------------------------
	// (b) Agreement between the source-generated matcher and the interpreter
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EveryRegisteredExpressionAgreesBetweenTheGeneratorAndTheInterpreterOnTheFixturesAndTheWorstCaseTexts()
	{
		List<string> texts = [.. _fixtures.Select(fixture => fixture.Text.Replace("\r\n", "\n", StringComparison.Ordinal))];

		foreach (string opener in _openers)
		{
			texts.AddRange(WorstCaseTexts(opener).Select(text => text.Length > 60_000 ? text[..60_000] : text));
		}

		texts.Add(string.Join("\n", _fixtures.Select(fixture => fixture.Text)));

		foreach ((string identifier, Regex generated) in VBNetRegisteredExpressions.All)
		{
			Regex interpreted = VBNetRegisteredExpressions.Interpreted(generated);
			string[] groupNames = generated.GetGroupNames();

			foreach (string text in texts)
			{
				ImmutableArray<Match> fromGenerator = Enumerate(generated, text);
				ImmutableArray<Match> fromInterpreter = Enumerate(interpreted, text);

				Assert.True(fromGenerator.Length == fromInterpreter.Length, $"{identifier}: {fromGenerator.Length} matches generated, {fromInterpreter.Length} interpreted.");

				for (int index = 0; index < fromGenerator.Length; index++)
				{
					Assert.Equal(Describe(fromGenerator[index], groupNames), Describe(fromInterpreter[index], groupNames));
				}
			}
		}
	}

	private static string Describe(Match match, string[] groupNames)
	{
		StringBuilder description = new();

		description.Append(match.Index).Append(':').Append(match.Length);

		foreach (string name in groupNames)
		{
			Group group = match.Groups[name];

			description.Append('|').Append(name).Append('=').Append(group.Success ? group.Index.ToString(CultureInfo.InvariantCulture) + ":" + group.Length.ToString(CultureInfo.InvariantCulture) : "-");
		}

		return description.ToString();
	}

	// ------------------------------------------------------------------------------------------
	// (c) The exposure scan: lazy bounded loops, and whether any sits in an optional group
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void NoRegisteredExpressionCarriesALazyBoundedLoopInsideAnOptionalGroupAndTheExposureTableIsRecorded()
	{
		StringBuilder table = new();
		List<string> exposed = [];

		table.AppendLine("expression\tlazy_bounded_loops\tbounds\toptional_groups\tlazy_loops_in_optional_group\treason");

		foreach ((string identifier, Regex expression) in VBNetRegisteredExpressions.All)
		{
			PatternShape shape = PatternShape.Analyze(expression.ToString());

			table.Append(identifier).Append('\t').Append(shape.LazyBoundedLoops.Count).Append('\t').Append(string.Join(' ', shape.LazyBoundedLoops.Select(loop => loop.Bounds)))
				.Append('\t').Append(shape.OptionalGroupCount).Append('\t').Append(shape.LazyLoopsInOptionalGroups).Append('\t').AppendLine(shape.LazyLoopsInOptionalGroups == 0
					? (shape.LazyBoundedLoops.Count == 0 ? "no lazy bounded loop" : "every lazy bounded loop is in a required position")
					: "EXPOSED");

			if (shape.LazyLoopsInOptionalGroups > 0)
			{
				exposed.Add(identifier);
			}
		}

		_output.WriteLine(table.ToString());

		string? path = Environment.GetEnvironmentVariable(EXPOSURE_REPORT_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(path))
		{
			File.WriteAllText(path, table.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}

		Assert.True(exposed.Count == 0, "Exposed to the lazy-window-in-an-optional-group defect: " + string.Join(", ", exposed));
	}

	[Fact]
	public void TheShapeScanFindsALazyBoundedLoopInAnOptionalGroupWhereOneIsWritten()
	{
		Assert.Equal(1, PatternShape.Analyze(@"a(?:(?:[^\n]*\n){0,5}?b)?c").LazyLoopsInOptionalGroups);
		Assert.Equal(1, PatternShape.Analyze(@"a(?:(?:[^\n]*\n){0,5}?b|)c").LazyLoopsInOptionalGroups);
		Assert.Equal(0, PatternShape.Analyze(@"a(?:[^\n]*\n){0,5}?bc").LazyLoopsInOptionalGroups);
		Assert.Equal(0, PatternShape.Analyze(@"a[{0,5}?](?:b)?c").LazyLoopsInOptionalGroups);
	}

	// ------------------------------------------------------------------------------------------
	// Support
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<Match> Enumerate(Regex expression, string text)
	{
		ImmutableArray<Match>.Builder matches = ImmutableArray.CreateBuilder<Match>();
		int previousEnd = -1;

		for (Match match = expression.Match(text); match.Success; match = match.NextMatch())
		{
			Assert.True(match.Index >= previousEnd, "An enumeration produced a match that begins before the previous one ended.");
			previousEnd = match.Index + Math.Max(match.Length, 1);
			matches.Add(match);

			if (matches.Count >= _MATCH_ENUMERATION_CAP)
			{
				break;
			}
		}

		return matches.ToImmutable();
	}

	private static string Repeat(string text, int repetitions)
	{
		StringBuilder builder = new(text.Length * repetitions);

		for (int index = 0; index < repetitions; index++)
		{
			builder.Append(text);
		}

		return builder.ToString();
	}
}

/// <summary>A structural scan of a regular-expression pattern: its lazy bounded loops (<c>{m,n}?</c>), its optional groups, and which loops sit in an optional group.</summary>
internal sealed partial record PatternShape(IReadOnlyList<PatternShape.LazyLoop> LazyBoundedLoops, int OptionalGroupCount, int LazyLoopsInOptionalGroups)
{
	internal sealed record LazyLoop(int Position, string Bounds);

	private sealed class OpenGroup
	{
		internal int Start;
		internal bool EmptyAlternative;
		internal bool AtAlternativeStart = true;
	}

	[GeneratedRegex(@"\G\{(\d+),(\d+)\}\?")]
	private static partial Regex LazyBoundedQuantifier();

	internal static PatternShape Analyze(string pattern)
	{
		List<(int Start, int End, bool Optional)> groups = [];
		Stack<OpenGroup> open = new();
		List<LazyLoop> loops = [];
		int index = 0;

		while (index < pattern.Length)
		{
			char current = pattern[index];

			if (current == '(')
			{
				MarkContent(open);
				open.Push(new OpenGroup { Start = index });
				index += index + 1 < pattern.Length && pattern[index + 1] == '?' ? 3 : 1;
			}
			else if (current == ')' && open.Count > 0)
			{
				OpenGroup group = open.Pop();

				groups.Add((group.Start, index, IsOptionalQuantifier(pattern, index + 1) || group.EmptyAlternative || group.AtAlternativeStart));
				MarkContent(open);
				index++;
			}
			else if (current == '|' && open.Count > 0)
			{
				OpenGroup group = open.Peek();

				group.EmptyAlternative |= group.AtAlternativeStart;
				group.AtAlternativeStart = true;
				index++;
			}
			else
			{
				MarkContent(open);

				if (current == '\\')
				{
					index += 2;
				}
				else if (current == '[')
				{
					index = SkipCharacterClass(pattern, index);
				}
				else
				{
					Match loop = current == '{' ? LazyBoundedQuantifier().Match(pattern, index) : Match.Empty;

					if (loop.Success)
					{
						loops.Add(new LazyLoop(index, loop.Groups[1].Value + "," + loop.Groups[2].Value));
						index += loop.Length;
					}
					else
					{
						index++;
					}
				}
			}
		}

		int exposed = loops.Count(loop => groups.Any(group => group.Optional && group.Start < loop.Position && loop.Position < group.End));

		return new PatternShape(loops, groups.Count(group => group.Optional), exposed);
	}

	private static void MarkContent(Stack<OpenGroup> open)
	{
		if (open.Count > 0)
		{
			open.Peek().AtAlternativeStart = false;
		}
	}

	private static int SkipCharacterClass(string pattern, int start)
	{
		int index = start + 1;

		if (index < pattern.Length && pattern[index] == '^')
		{
			index++;
		}

		if (index < pattern.Length && pattern[index] == ']')
		{
			index++;
		}

		while (index < pattern.Length && pattern[index] != ']')
		{
			index += pattern[index] == '\\' ? 2 : 1;
		}

		return index + 1;
	}

	private static bool IsOptionalQuantifier(string pattern, int position)
	{
		return position < pattern.Length
			&& (pattern[position] is '?' or '*'
				|| pattern[position] == '{' && position + 2 < pattern.Length && pattern[position + 1] == '0' && pattern[position + 2] == ',');
	}
}
