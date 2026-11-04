using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE `VBNet` SHAPES OF THE MATCH-BOUND STRESS SET, written beside the `PowerShell` set and read by the same kind of
/// test: every shape is legal, ordinary-looking VB.NET text a client repository could contain, filled to just under the reader's size ceiling, and
/// none is built to make an expression backtrack catastrophically. What the shapes do is push every honest cost to the ceilings the engine already
/// enforces: files at the size ceiling, lines at the line-length ceiling, anchors and matches at the densest a generated file plausibly carries, and
/// every bounded window of the catalog (the logical-line window, the five-logical-line windows, the bracket groups, the multi-line literal and the
/// explicit and implicit continuations) opened over and over without being closed.
///
/// THIS SET IS RUN over the registered expressions of <see cref="MigrateAuth.Cli.Tests.Catalog.VBNetRegisteredExpressions"/> by the cost gate. Every shape is deterministic and invented: placeholders and
/// generic names, no fragment of any real text.
/// </summary>
internal static class VBNetMatchBoundStressSet
{
	internal const int TARGET_SIZE_IN_CHARACTERS = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;

	internal const int LONG_LINE_LENGTH_IN_CHARACTERS = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _LINE_MARGIN_IN_CHARACTERS;

	internal const string FLOW_SHAPE_NAME_PREFIX = "vb-flow-";

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;
	private const int _LINE_MARGIN_IN_CHARACTERS = 16;
	private const int _FILLER_LINES_PER_WINDOW = 4;
	private const string _LINE_BREAK = "\n";
	private const string _ANCHOR_LINE = "Dim encoded As String = Convert.ToBase64String(documentBytes)";
	private const string _METHOD_FORMAT = "\tPublic Sub Step{0:D6}()\n{1}\tEnd Sub\n";
	private const string _FILLER_STATEMENT = "\t\tDim total As Integer = first + second\n";

	private static readonly ImmutableArray<string> _ORDINARY_STATEMENTS =
	[
		"\t\tDim order = GetOrder(orderIdentifier)",
		"\t\tIf order Is Nothing Then Return",
		"\t\tDim amount As Decimal = order.Lines.Sum(Function(line) line.Total)",
		"\t\tLogger.Verbose(\"Order \" & order.Identifier & \" totals \" & amount.ToString())",
		"\t\tFor Each item In items : names.Add(item.Name.ToUpper()) : Next"
	];

	private static readonly ImmutableArray<string> _ANCHOR_DENSE_NEAR_MISSES =
	[
		"\t\tDim label = options.CredentialName",
		"\t\tDim has = request.Headers.ContainsKey(\"Authorization\")",
		"\t\tDim mode = settings.AuthenticationMode",
		"\t\tDim payload = Convert.ToBase64String(documentBytes)",
		"\t\tDim fallback = CredentialCache.DefaultCredentials",
		"\t\tclient.UseDefaultCredentials = True",
		"\t\tDim name = \"Basic\"",
		"\t\t' Authorization and Credential in prose only, never in code",
		"\t\tDim d = CreateObject(\"Scripting.FileSystemObject\")"
	];

	private static readonly ImmutableArray<string> _EVERY_CLASS_CONSTRUCTS =
	[
		"\t\tDim credentials As New UsernamePasswordCredentials(loginName, loginSecret)",
		"\t\tDim proxy As New RSAPIClient(rsapiUri, New UsernamePasswordCredentials(loginName, loginSecret))",
		"\t\tcache.Add(New Uri(address), \"Basic\", New NetworkCredential(loginName, loginSecret))",
		"\t\tDim auth As New HttpBasicAuthenticator(loginName, loginSecret)",
		"\t\tDim built = request.WithBasicAuth(loginName, loginSecret)",
		"\t\tclient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue(\"Basic\", encoded)",
		"\t\tclient.Headers.Add(\"Authorization\", \"Basic \" & encoded)",
		"\t\trequest.Headers(HttpRequestHeader.Authorization) = \"Basic \" & encoded",
		"\t\tbinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic\n\t\tfactory.Credentials.UserName.Password = loginSecret",
		"\t\tDim importApi As New ImportAPI(loginName, loginSecret, serviceUrl)",
		"\t\tWith job.Settings\n\t\t\t.RelativityUsername = loginName\n\t\t\t.RelativityPassword = loginSecret\n\t\tEnd With",
		"\t\tDim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(loginName, loginSecret)}",
		"\t\tDim container As New NetworkCredential(loginName, loginSecret)",
		"\t\tDim pair As String = loginName & \":\" & loginSecret\n\t\tDim pairEncoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))",
		"\t\tDim tokenPair As String = \"\" & \":\" & accessToken\n\t\tDim tokenEncoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(tokenPair))",
		"\t\tMy.Computer.Network.DownloadFile(address, destination, loginName, loginSecret)",
		"\t\tDim http As Object = CreateObject(\"MSXML2.ServerXMLHTTP\")\n\t\thttp.Open(\"GET\", address, False, loginName, loginSecret)",
		"\t\tDim mailer As New SmtpClient(host, port) With {.Credentials = New NetworkCredential(loginName, loginSecret)}",
		"\tPublic Function BuildBasicAuthHeader(loginName As String, loginSecret As String) As String\n\tEnd Function"
	];

	private static readonly ImmutableArray<string> _OPEN_WINDOWS =
	[
		"\t\tDim wc As New WebClient()",
		"\t\tDim mailer As New SmtpClient(host, port)",
		"\t\tDim request = CType(WebRequest.Create(address), FtpWebRequest)",
		"\t\tDim pair As String = loginName & \":\" & loginSecret",
		"\t\tDim empty As String = \"\"",
		"\t\tDim value As String = \"Basic \" & encoded",
		"\t\tDim cache As New CredentialCache()",
		"\t\tDim http As Object = CreateObject(\"MSXML2.XMLHTTP\")",
		"\t\tclient.Headers.Add(\"Accept\", \"application/json\")",
		"\t\tWith job.Settings\n\t\t\t.RelativityUsername = loginName"
	];

	private static readonly ImmutableArray<string> _LITERAL_AND_COMMENT_LINES =
	[
		"\t\t' Credential, Authorization and Authentication appear here only in prose, never in code.",
		"\t\tDim path = \"C:\\data\\credentials\\archive\\Authorization\"",
		"\t\tDim message = $\"Credential {name} refreshed for {user.Name} at {Date.Now:o}\"",
		"\t\tREM client.Headers.Add(\"Authorization\", \"Basic \" & encoded) was removed in the migration",
		"\t\tDim note = \"Basic authentication is disabled; use a bearer token instead.\""
	];

	private static readonly ImmutableArray<string> _LEXICAL_HEAVY_LINES =
	[
		"\t\tDim body = \"{\n  \"\"name\"\": \"\"item\"\",\n  \"\"kind\"\": 'x'\n}\"",
		"\t\tDim stamp As Date = #1/15/2004 11:30:00 PM#",
		"\t\tDim nested = $\"nested {String.Join(\",\", names.Select(Function(n) $\"{n}!\"))} done\"",
		"\t\tDim smart = \u201Ctext with ' and #\u201D & \"plain\"",
		"\t\tDim xml = <root>text with an ' and a \" in prose</root>\n\t\tDim after As Integer = 1",
		"#If DEBUG Then\n\t\tDim disabled As String = \"stray\n#End If\n\t\tDim afterDisabled As Integer = 2",
		"\t\tDim tmpl = $\"{{\"\"k\"\":{value}}}\"",
		"#Region \"title's\"\n\t\tREM comment after a region\n#End Region"
	];

	/// <summary>
	/// THE BENIGN LONG-LINE SHAPES: each file is lines of just under the line-length ceiling that repeat one ordinary statement group, after a first line that
	/// carries a preselection anchor so that every class runs over it. No group holds a construct of any class. What each exercises is a window that opens at
	/// a frequent token and is scanned forward for a rarer one, which on a line at the ceiling costs the square of the line length. Each is named <c>vb-long-line-</c>
	/// and its slug.
	/// </summary>
	private static readonly ImmutableArray<(string Slug, string Group)> _LONG_LINE_GROUPS =
	[
		("dictionary-initializers", "Dim p As New Dictionary(Of String, String) From {{\"a\", b}} : "),
		("credentials-assignment-beside-client", "x.Credentials = y : Dim wc As New WebClient() : "),
		("mail-credentials-assignment", "m.Credentials = y : Dim s As New SmtpClient(h, 25) : "),
		("encoder-calls-followed-by-values", "Convert.ToBase64String(bytes) & value, "),
		("credential-creations-and-collection-adds", "New NetworkCredential(a, b) list.Add(item) "),
		("header-adds-without-authorization", "r.Headers.Add(\"Accept\", \"x\") : r.Headers(\"X-Trace\") = t : "),
		("bracket-groups", "f(g(h(i(j(k)))), (a + (b - (c * d)))) : "),
		("with-blocks-and-leading-dots", "With x : .Credentials = y : .Timeout = 5 : End With : "),
		("string-literals-with-colons", "Dim s As String = \"a:b:c\" & \"d:e\" : t = u & \":\" & v : "),
		("web-calls-without-credentials", "client.GetStringAsync(uri) : wc.DownloadString(address) : ")
	];

	private const string _LONG_LINE_SHAPE_NAME_PREFIX = "vb-long-line-";

	private static readonly ImmutableArray<(string Name, string Block)> _FLOW_BLOCKS =
	[
		("console-prompts", "\t\tConsole.Write(\"Enter the value for the next field, then the password: \") : Dim v = Console.ReadLine()\n"),
		("configuration-reads", "\t\tDim a = settings(\"Section:Key\") : Dim b = ConfigurationManager.AppSettings(\"Other\") : Dim c = configuration(\"Plain\")\n"),
		("password-boxes", "\t\tDim box As PasswordBox = DirectCast(sender, PasswordBox) : Dim shown = box.Tag\n"),
		("web-form-handlers", "\tProtected Sub Click_Handler(sender As Object, e As EventArgs) Handles btnGo.Click\n\t\tDim x = Request.Form(\"field\")\n\tEnd Sub\n"),
		("instance-settings", "\t\tDim v = bundle.GetString(Section, \"Name\") : Dim w = bundle.GetInt32(Section, \"Other\")\n")
	];

	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		shapes.Add(Shape("vb-ordinary-at-ceiling", FillToCeiling(_ORDINARY_STATEMENTS)));
		shapes.Add(Shape("vb-anchor-dense-near-misses", FillToCeiling(_ANCHOR_DENSE_NEAR_MISSES)));
		shapes.Add(Shape("vb-every-class-at-match-density", FillToCeiling(_EVERY_CLASS_CONSTRUCTS)));
		shapes.Add(Shape("vb-open-windows", FillWindowsToCeiling(_OPEN_WINDOWS)));
		shapes.Add(Shape("vb-literals-and-comments", FillToCeiling(_LITERAL_AND_COMMENT_LINES)));
		shapes.Add(Shape("vb-lexical-heavy", FillToCeiling(_LEXICAL_HEAVY_LINES)));
		shapes.Add(Shape("vb-explicit-continuation-chain", ContinuationChain(explicitUnderscore: true)));
		shapes.Add(Shape("vb-implicit-continuation-chain", ContinuationChain(explicitUnderscore: false)));
		shapes.Add(Shape("vb-multi-line-literal-then-constructs", MultiLineLiteralThenConstructs()));
		shapes.Add(Shape("vb-unclosed-brackets", UnclosedBrackets()));

		foreach ((string slug, string group) in _LONG_LINE_GROUPS)
		{
			shapes.Add(Shape(_LONG_LINE_SHAPE_NAME_PREFIX + slug, LongLines(group)));
		}

		foreach ((string name, string block) in _FLOW_BLOCKS)
		{
			shapes.Add(Shape(FLOW_SHAPE_NAME_PREFIX + name, FlowShape(block)));
		}

		return shapes.ToImmutable();
	}

	private static MatchBoundStressShape Shape(string name, string text)
	{
		return new MatchBoundStressShape { Name = name, Text = text, FileExtension = ".vb" };
	}

	private static string Header()
	{
		return "Option Strict On\nImports System.Net\nImports System.Text\nNamespace Stress\n\tPublic Class Subject\n" + "\t\t" + _ANCHOR_LINE + _LINE_BREAK;
	}

	private static string FillToCeiling(ImmutableArray<string> statements)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + 4096);
		int methodNumber = 0;

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			StringBuilder body = new();

			foreach (string statement in statements)
			{
				body.Append(statement).Append(_LINE_BREAK);
			}

			builder.Append(string.Format(CultureInfo.InvariantCulture, _METHOD_FORMAT, methodNumber++, body));
		}

		return builder.ToString();
	}

	private static string FillWindowsToCeiling(ImmutableArray<string> openers)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + 4096);
		int methodNumber = 0;

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			StringBuilder body = new();

			foreach (string opener in openers)
			{
				body.Append(opener).Append(_LINE_BREAK);

				for (int filler = 0; filler < _FILLER_LINES_PER_WINDOW; filler++)
				{
					body.Append(_FILLER_STATEMENT);
				}
			}

			builder.Append(string.Format(CultureInfo.InvariantCulture, _METHOD_FORMAT, methodNumber++, body));
		}

		return builder.ToString();
	}

	private static string LongLines(string group)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + 4096);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			int lineStart = builder.Length;

			while (builder.Length - lineStart + group.Length < LONG_LINE_LENGTH_IN_CHARACTERS)
			{
				builder.Append(group);
			}

			builder.Append(_LINE_BREAK);
		}

		return builder.ToString();
	}

	/// <summary>One enormous statement over many physical lines, continued by `_` or implicitly after commas: the logical-line window at its bound.</summary>
	private static string ContinuationChain(bool explicitUnderscore)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + 4096);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append("\t\tDim list = New List(Of String) From {");

			for (int part = 0; part < 4_000; part++)
			{
				builder.Append(explicitUnderscore ? "\"item\" , _\n\t\t\t" : "\"item\",\n\t\t\t");
			}

			builder.Append("\"last\"}\n");
			builder.Append("\t\tclient.Headers.Add(\"Accept\", x & _\n\t\t\ty & _\n\t\t\tz)\n");
		}

		return builder.ToString();
	}

	private static string MultiLineLiteralThenConstructs()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + 4096);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append("\t\tDim text = \"line of a long multi-line literal that mentions Authorization and Basic\n");

			for (int line = 0; line < 200; line++)
			{
				builder.Append("  Convert.ToBase64String(x) ").Append(line.ToString(CultureInfo.InvariantCulture)).Append(_LINE_BREAK);
			}

			builder.Append("\"\n\t\tDim pair = user & \":\" & pass\n\t\tDim e = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))\n");
		}

		return builder.ToString();
	}

	private static string UnclosedBrackets()
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + 4096);

		builder.Append(Header());

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append("\t\tclient.Headers.Add(\"Authorization\", \"Basic \" & Convert.ToBase64String(Encoding.UTF8.GetBytes(f(g(h(i(j(\n");

			for (int line = 0; line < 20; line++)
			{
				builder.Append("\t\t\tDim filler").Append(line.ToString(CultureInfo.InvariantCulture)).Append(" = 1\n");
			}
		}

		return builder.ToString();
	}

	/// <summary>A first line that carries a High construct, so the flow signals are evaluated, then the block each flow signal does its most work on.</summary>
	private static string FlowShape(string block)
	{
		StringBuilder builder = new(TARGET_SIZE_IN_CHARACTERS + 4096);

		builder.Append(Header());
		builder.Append("\t\tclient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue(\"Basic\", encoded)\n");

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(block);
		}

		return builder.ToString();
	}

	internal static string RelativePathOf(MatchBoundStressShape shape)
	{
		return string.Format(CultureInfo.InvariantCulture, "stress/{0}.vb", shape.Name);
	}
}
