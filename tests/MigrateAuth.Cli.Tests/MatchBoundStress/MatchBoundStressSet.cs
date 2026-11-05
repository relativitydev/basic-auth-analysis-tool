using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// One synthetic stress file of the match-bound timing procedure: a name that says which shape it is, and
/// the source text the recognition engine is timed against.
/// </summary>
internal sealed record MatchBoundStressShape
{
	internal required string Name { get; init; }

	internal required string Text { get; init; }

	/// <summary>
	/// The extension the shape's file stands under, with its dot, or empty for an extensionless file. Left unset by a language whose every shape stands
	/// under that language's own extension, which names it itself.
	/// </summary>
	internal string FileExtension { get; init; } = string.Empty;
}

/// <summary>
/// THE SEPARATE SYNTHETIC STRESS SET OF THE MATCH-BOUND TIMING PROCEDURE.
///
/// WHAT "BENIGN" MEANS HERE. Every shape is legal, ordinary-looking C# that a client repository
/// could contain - nothing is built to make an expression backtrack catastrophically, because that
/// is what the bound exists to STOP, not what it must let through. What the shapes do is push every
/// honest cost to the ceilings the engine already enforces: files at the size ceiling, lines at
/// its line-length ceiling, anchors and matches at the densest a generated file plausibly carries,
/// and the five-line proximity windows of CS-009, CS-013 and CS-016 opened over and over without
/// closing, which is this catalog's one
/// bounded-but-multiplying region. The windows of CS-007's direct assignment and CS-019's
/// file-transfer request and directory entry are in both the match-density shape, closed, and
/// the open-window shape, never closed.
///
/// Every shape is deterministic - the same text on every run and every machine - so two
/// timing runs differ only by the machine that ran them.
/// </summary>
internal static class MatchBoundStressSet
{
	/// <summary>
	/// Each shape is filled to just under the per-file size ceiling. The margin keeps a shape
	/// whose last repetition runs long from ever crossing the ceiling the reader would skip it at.
	/// </summary>
	internal const int TARGET_SIZE_IN_CHARACTERS = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;

	/// <summary>The long-line shape stays this far below the line-length ceiling.</summary>
	internal const int LONG_LINE_LENGTH_IN_CHARACTERS = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _LINE_MARGIN_IN_CHARACTERS;

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;
	private const int _LINE_MARGIN_IN_CHARACTERS = 16;

	/// <summary>
	/// The statement that opens CS-007's direct-assignment window: an
	/// allow-listed handler constructed into a variable, whose <c>Credentials</c> write may follow
	/// within five lines. Shared by the match-density shape, which closes the window, and the
	/// open-window shape, which never does.
	/// </summary>
	internal const string DIRECT_HANDLER_ASSIGNMENT_WINDOW_OPENER = "\t\tvar directHandler = new HttpClientHandler();";

	/// <summary>The statement that closes CS-007's direct-assignment window.</summary>
	internal const string DIRECT_HANDLER_ASSIGNMENT_WINDOW_CLOSER = "\n\t\tdirectHandler.Credentials = new NetworkCredential(userName, password);";

	/// <summary>
	/// The statement that opens CS-019's file-transfer request window: the
	/// request obtained by a cast from the factory, whose <c>Credentials</c> write may follow within
	/// five lines.
	/// </summary>
	internal const string FILE_TRANSFER_REQUEST_WINDOW_OPENER = "\t\tvar fileTransferRequest = (FtpWebRequest)WebRequest.Create(transferUri);";

	/// <summary>The statement that closes CS-019's file-transfer request window.</summary>
	internal const string FILE_TRANSFER_REQUEST_WINDOW_CLOSER = "\n\t\tfileTransferRequest.Credentials = new NetworkCredential(userName, password);";

	/// <summary>
	/// The statement that opens CS-019's directory-entry window: a credential
	/// constructed into a variable, which a directory entry constructed within five lines may read.
	/// </summary>
	internal const string DIRECTORY_CREDENTIAL_WINDOW_OPENER = "\t\tvar directoryCredential = new NetworkCredential(userName, password);";

	/// <summary>The statement that closes CS-019's directory-entry window.</summary>
	internal const string DIRECTORY_CREDENTIAL_WINDOW_CLOSER = "\n\t\tvar directoryRoot = new DirectoryEntry(directoryPath, directoryCredential.UserName, directoryCredential.Password);";

	/// <summary>
	/// The statement that opens CS-019's mail-client windows, credential first: a credential built from an
	/// object initializer into a variable, whose password assignment and mail-client binding may follow.
	/// </summary>
	internal const string MAIL_CREDENTIAL_WINDOW_OPENER = "\t\tNetworkCredential mailCredential = new NetworkCredential { UserName = userName };";

	/// <summary>The statements that close the credential-first mail-client window.</summary>
	internal const string MAIL_CREDENTIAL_WINDOW_CLOSER = "\n\t\tmailCredential.Password = password;\n\t\tvar mailer = new SmtpClient(host, port) { Credentials = mailCredential };";

	/// <summary>The statement that opens CS-019's mail-client windows, sink first.</summary>
	internal const string MAIL_SINK_WINDOW_OPENER = "\t\tvar sinkFirstMailer = new SmtpClient(host, port);";

	/// <summary>The statements that close the sink-first mail-client window.</summary>
	internal const string MAIL_SINK_WINDOW_CLOSER = "\n\t\tNetworkCredential sinkCredential = new NetworkCredential { UserName = userName };\n\t\tsinkCredential.Password = password;\n\t\tsinkFirstMailer.Credentials = sinkCredential;";

	/// <summary>
	/// The statement that opens the property-write window of a handler declared with the target-typed creation,
	/// <c>HttpClientHandler handler = new();</c>, whose <c>Credentials</c> write may follow within five lines.
	/// </summary>
	internal const string TARGET_TYPED_HANDLER_WINDOW_OPENER = "\t\tHttpClientHandler targetHandler = new();";

	/// <summary>The statement that closes the target-typed handler window.</summary>
	internal const string TARGET_TYPED_HANDLER_WINDOW_CLOSER = "\n\t\ttargetHandler.Credentials = new NetworkCredential(userName, password);";

	/// <summary>The statement that opens the binding window of a mail client declared with the target-typed creation.</summary>
	internal const string TARGET_TYPED_MAIL_WINDOW_OPENER = "\t\tSmtpClient targetMailer = new(host, port);";

	/// <summary>The statement that closes the target-typed mail-client binding window.</summary>
	internal const string TARGET_TYPED_MAIL_WINDOW_CLOSER = "\n\t\ttargetMailer.Credentials = new NetworkCredential(userName, password);";

	/// <summary>The statement that opens the sink-first window of a target-typed mail client with a credential assembled across statements.</summary>
	internal const string TARGET_TYPED_SINK_FIRST_WINDOW_OPENER = "\t\tSmtpClient targetSinkFirst = new(host, port);";

	/// <summary>The statements that close the target-typed sink-first window.</summary>
	internal const string TARGET_TYPED_SINK_FIRST_WINDOW_CLOSER = "\n\t\tNetworkCredential targetSinkCredential = new NetworkCredential { UserName = userName };\n\t\ttargetSinkCredential.Password = password;\n\t\ttargetSinkFirst.Credentials = targetSinkCredential;";

	/// <summary>The statement that opens the credential-first window whose sink is declared with the target-typed creation.</summary>
	internal const string TARGET_TYPED_CREDENTIAL_FIRST_WINDOW_OPENER = "\t\tNetworkCredential targetFirst = new NetworkCredential { UserName = userName };";

	/// <summary>The statements that close the credential-first window whose sink is declared with the target-typed creation.</summary>
	internal const string TARGET_TYPED_CREDENTIAL_FIRST_WINDOW_CLOSER = "\n\t\ttargetFirst.Password = password;\n\t\tSmtpClient targetBound = new(host, port) { Credentials = targetFirst };";

	/// <summary>
	/// The statement that stores a credential in a dotted member, the first link of the member-bound chain of CS-019: the window that follows it reads up to
	/// thirty lines of at most four hundred characters each, stopping at a line that holds the text of the credential type.
	/// </summary>
	internal const string MEMBER_STORE_STATEMENT = "\t\tsettings.MailCredential = new NetworkCredential(userName, password);";

	/// <summary>The statement that declares a mail client, the receiver of the second shape of the member-bound chain, which no statement ever binds.</summary>
	internal const string MEMBER_SINK_DECLARATION_STATEMENT = "\t\tvar memberMailer = new SmtpClient(host, port);";

	/// <summary>The name of the shape that alternates the member store with the sink declaration, so that every store opens a window that crosses one sink and meets the next store.</summary>
	internal const string ALTERNATING_MEMBER_STORE_AND_SINK_SHAPE_NAME = "member-bound-alternating-store-and-sink";

	/// <summary>The name of the same alternation with every line at the four hundred characters the window allows.</summary>
	internal const string ALTERNATING_MEMBER_STORE_AND_SINK_ON_LONG_LINES_SHAPE_NAME = "member-bound-alternating-store-and-sink-on-long-lines";

	/// <summary>The name of the shape of stores only: every store is followed by the thirty lines of its window, none of which holds the credential type, and no sink.</summary>
	internal const string MEMBER_STORES_ONLY_SHAPE_NAME = "member-bound-stores-only";

	/// <summary>The entry repeated by the brace-free shape: the user-name half of the <c>Rsapi</c> pair, with no password half anywhere.</summary>
	internal const string RSAPI_USER_NAME_ENTRY = "RsapiUserName = x, ";

	/// <summary>The name of the shape that repeats <see cref="RSAPI_USER_NAME_ENTRY"/> inside one initializer that no brace ever closes before the end of the file.</summary>
	internal const string BRACE_FREE_REPEATED_RSAPI_USER_NAME_SHAPE_NAME = "brace-free-repeated-rsapi-user-name-entries";

	/// <summary>The call of the proxy factory whose argument list holds a block comment that no text ever closes, the gap of CS-020 that reads to the end of the file.</summary>
	internal const string UNTERMINATED_PROXY_FACTORY_COMMENT_CALL = "\t\tvar proxy = helper.GetProxy<IExampleService>(userName, /* the account";

	/// <summary>The name of the shape that repeats <see cref="UNTERMINATED_PROXY_FACTORY_COMMENT_CALL"/>.</summary>
	internal const string UNTERMINATED_PROXY_FACTORY_COMMENT_SHAPE_NAME = "unterminated-block-comment-in-the-proxy-factory-gap";

	/// <summary>The statement that opens the separate-statement pair of the identifier-rooted halves, whose encoder may follow within five lines.</summary>
	internal const string ROOTED_PAIR_WINDOW_OPENER = "\t\tstring rootedPair = $\"{userBox.Text}:{Secrets.Read(index)[0]}\";";

	/// <summary>The statement that closes the identifier-rooted pair window.</summary>
	internal const string ROOTED_PAIR_WINDOW_CLOSER = "\n\t\tstring rootedEncoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(rootedPair));";

	/// <summary>
	/// A credential built from two calls whose argument lists span several lines, assigned to a variable and handed to
	/// a handler that also asks for pre-authentication: the form whose directory-entry alternative of the non-HTTP
	/// sink class reads the credential first and then looks forward over a bounded window of lines for a directory
	/// entry that never comes, so the bounded window runs out of lines. The expression is evaluated
	/// through the interpreted engine. Written with generic names.
	/// </summary>
	internal const string MULTI_LINE_CALL_ARGUMENT_CREDENTIAL_WINDOW =
		"\t\tvar serviceCredential = new NetworkCredential(\n"
		+ "\t\t\tEnvironment.GetEnvironmentVariable(\n"
		+ "\t\t\t\t\"SERVICE_ACCOUNT_NAME\"),\n"
		+ "\t\t\tEnvironment.GetEnvironmentVariable(\n"
		+ "\t\t\t\t\"SERVICE_ACCOUNT_SECRET\"));\n"
		+ "\t\tvar serviceHandler = new HttpClientHandler();\n"
		+ "\t\tserviceHandler.Credentials = serviceCredential;\n"
		+ "\t\tserviceHandler.PreAuthenticate = true;";

	/// <summary>The name of the shape that repeats <see cref="MULTI_LINE_CALL_ARGUMENT_CREDENTIAL_WINDOW"/>.</summary>
	internal const string MULTI_LINE_CALL_ARGUMENT_CREDENTIAL_SHAPE_NAME = "multi-line-call-argument-credentials-with-an-exhausted-window";

	/// <summary>The name of the shape of many <c>+ ":" +</c> sites, each after a long neutrally named left half.</summary>
	internal const string NEUTRAL_COLON_PAIR_SHAPE_NAME = "neutral-colon-pairs-with-long-left-halves";

	/// <summary>The name of the shape of many plus signs and no colon literal at all, the case the look-behind of the concatenated pair must not be run at.</summary>
	internal const string PLUS_SIGNS_WITHOUT_A_COLON_LITERAL_SHAPE_NAME = "many-plus-signs-without-a-colon-literal";

	/// <summary>The shape of a pair whose halves are camel-case runs of credential words, each directly followed by an identifier suffix.</summary>
	internal const string IDENTIFIER_SUFFIX_CAMEL_RUNS_SHAPE_NAME = "identifier-suffix-camel-word-runs";

	/// <summary>The shape of a pair whose halves are underscore runs of credential words, each directly followed by an identifier suffix.</summary>
	internal const string IDENTIFIER_SUFFIX_UNDERSCORE_RUNS_SHAPE_NAME = "identifier-suffix-underscore-word-runs";

	/// <summary>The shape of a pair whose halves are suffixed runs in which only the last credential word has no suffix, the walk's worst case before it stops.</summary>
	internal const string IDENTIFIER_SUFFIX_LAST_WORD_UNSUFFIXED_SHAPE_NAME = "identifier-suffix-runs-with-only-the-last-word-unsuffixed";

	/// <summary>The shape of a pair whose halves are acronym runs, and a credential word followed by one long segment no boundary cuts.</summary>
	internal const string IDENTIFIER_SUFFIX_ACRONYM_RUNS_SHAPE_NAME = "identifier-suffix-acronym-runs";

	/// <summary>The shape of a pair whose two halves fill a line to the line-length ceiling.</summary>
	internal const string IDENTIFIER_SUFFIX_LINE_CEILING_SHAPE_NAME = "identifier-suffix-halves-at-the-line-ceiling";

	private const int _LEFT_HALF_SEGMENT_COUNT = 60;
	private const int _PLUS_SIGNS_PER_LINE = 80;
	private const string _PLUS_SEPARATOR = " + ";
	private const string _MEMBER_ACCESS_SEPARATOR = ".";
	private const string _NUMERIC_OPERAND = "partialValue";
	private const string _QUOTED_OPERAND = "\"part\"";
	private const string _INDEXED_CALL_OPERAND = "Part(index)[0]";
	private const int _FURTHER_LINES_AFTER_THE_WINDOW = 7;
	private const int _MEMBER_WINDOW_LINES = 30;
	private const int _MEMBER_WINDOW_LINE_LENGTH_IN_CHARACTERS = 400;
	private const int _RSAPI_ENTRIES_PER_LINE = 20;
	private const string _RSAPI_INITIALIZER_OPENING = "namespace Synthetic.Stress\n{\ninternal static class StressSubject\n{\n\tinternal static void Member000000()\n\t{\n\t\tvar settings = new ExampleSettings\n\t\t{\n";
	private const string _RSAPI_INITIALIZER_CLOSING = "\t\t};\n\t}\n}\n}\n";
	private const string _RSAPI_ENTRY_LINE_INDENT = "\t\t\t";
	private const string _LINE_BREAK = "\n";
	private const char _LINE_BREAK_CHARACTER = '\n';
	private const string _MEMBER_FORMAT = "\tinternal static void Member{0:D6}()\n\t{{\n{1}\t}}\n";
	private const string _FILLER_STATEMENT = "\t\tint total = first + second;\n";

	/// <summary>
	/// A long, neutrally named left half: a dotted chain of sixty segments, then a call and an indexer. It is the longest
	/// identifier-rooted argument the pair's left half reads, so the look-behind and the word test have the most text to read.
	/// </summary>
	private static readonly string _LONG_NEUTRAL_LEFT_HALF =
		$"{string.Join(_MEMBER_ACCESS_SEPARATOR, Enumerable.Range(0, _LEFT_HALF_SEGMENT_COUNT).Select(index => $"segment{index.ToString(CultureInfo.InvariantCulture)}"))}(argumentValue)[0]";

	private static readonly ImmutableArray<string> _NEUTRAL_COLON_PAIRS =
	[
		$"\t\tstring ratioPair = {_LONG_NEUTRAL_LEFT_HALF} + \":\" + denominatorValue;\n\t\tstring ratioEncoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(ratioPair));",
		$"\t\tstring chained = firstValue + {_LONG_NEUTRAL_LEFT_HALF} + \":\" + secondValue + thirdValue;",
		$"\t\tstring interpolated = $\"{{{_LONG_NEUTRAL_LEFT_HALF}}}:{{denominatorValue}}\";",
		$"\t\tstring formatted = string.Format(\"{{0}}:{{1}}\", {_LONG_NEUTRAL_LEFT_HALF}, denominatorValue);",
		$"\t\tstring joined = string.Join(\":\", {_LONG_NEUTRAL_LEFT_HALF}, denominatorValue);",
		$"\t\tstring inlinePair = Convert.ToBase64String(Encoding.ASCII.GetBytes({_LONG_NEUTRAL_LEFT_HALF} + \":\" + denominatorValue));"
	];

	private const int _SUFFIX_HALF_LENGTH_IN_CHARACTERS = 3000;
	private const string _UNDERSCORE = "_";
	private const string _ACRONYM_USER_RUN = "USERIDACCOUNTIDGUID";
	private const string _ACRONYM_SECRET_RUN = "PASSWORDIDSECRETIDGUID";
	private const string _SUFFIXED_PAIR_STATEMENT_FORMAT = "\t\tstring {0} = {1} + \":\" + {2};\n\t\tstring {0}Encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes({0}));";

	private static readonly ImmutableArray<string> _SUFFIXED_CAMEL_USER_WORDS = ["UserId", "AccountIds", "LoginIdentifier", "EmailGuid", "MailUuid", "UserNumber", "AccountIndex", "LoginCode"];

	private static readonly ImmutableArray<string> _SUFFIXED_CAMEL_SECRET_WORDS = ["PasswordId", "SecretIds", "TokenIdentifier", "CredentialGuid", "PasswdUuid", "PwdNumber", "PassphraseIndex", "PasscodeCode"];

	private static readonly ImmutableArray<string> _SUFFIXED_UNDERSCORE_USER_WORDS = ["user_id", "account_ids", "login_identifier", "email_guid", "mail_uuid", "user_number", "account_index", "login_code"];

	private static readonly ImmutableArray<string> _SUFFIXED_UNDERSCORE_SECRET_WORDS = ["password_id", "secret_ids", "token_identifier", "credential_guid", "passwd_uuid", "pwd_number", "passphrase_index", "passcode_code"];

	private static readonly ImmutableArray<string> _SUFFIXED_ACRONYM_USER_WORDS = ["USERId", "ACCOUNTGuid", "LOGINCode", "EMAILUuid"];

	private static readonly ImmutableArray<string> _SUFFIXED_ACRONYM_SECRET_WORDS = ["PASSWORDId", "SECRETGuid", "TOKENCode", "CREDENTIALIndex"];

	private static readonly ImmutableArray<string> _MANY_PLUS_SIGNS_WITHOUT_A_COLON_LITERAL =
	[
		$"\t\tdecimal total = {string.Join(_PLUS_SEPARATOR, Enumerable.Repeat(_NUMERIC_OPERAND, _PLUS_SIGNS_PER_LINE))};",
		$"\t\tstring text = {string.Join(_PLUS_SEPARATOR, Enumerable.Repeat(_QUOTED_OPERAND, _PLUS_SIGNS_PER_LINE))};",
		$"\t\tstring mixed = {string.Join(_PLUS_SEPARATOR, Enumerable.Repeat(_INDEXED_CALL_OPERAND, _PLUS_SIGNS_PER_LINE))};"
	];

	private static readonly ImmutableArray<string> _ORDINARY_STATEMENTS =
	[
		"\t\tvar order = repository.Find(orderIdentifier);",
		"\t\tif (order is null) { return; }",
		"\t\tdecimal amount = order.Lines.Sum(line => line.Quantity * line.UnitPrice);",
		"\t\tlogger.LogInformation(\"Order {Identifier} totals {Amount}\", order.Identifier, amount);",
		"\t\tawait publisher.PublishAsync(new OrderTotalled(order.Identifier, amount), cancellationToken);"
	];

	private static readonly ImmutableArray<string> _ANCHOR_DENSE_NEAR_MISSES =
	[
		"\t\tstring credentialLabel = options.CredentialName ?? defaultCredentialName;",
		"\t\tbool hasAuthorization = request.Headers.Contains(\"Authorization\");",
		"\t\tstring displayedUserName = form.UserName.Trim();",
		"\t\tstring payload = Convert.ToBase64String(documentBytes);",
		"\t\tICredentials fallback = CredentialCache.DefaultCredentials;",
		"\t\tstring authenticatorName = nameof(Authenticator);",
		"\t\tvar settingsLabel = typeof(ServiceFactorySettings).Name;",
		"\t\tstring schemeName = headerValue.Scheme;",
		"\t\tbool isBase64 = Base64Validator.IsValid(candidate);"
	];

	private static readonly ImmutableArray<string> _EVERY_CLASS_CONSTRUCTS =
	[
		"\t\tCredentials credentials = new UsernamePasswordCredentials(userName, password);",
		"\t\tvar client = new RSAPIClient(rsapiUri, new UsernamePasswordCredentials(userName, password));",
		"\t\tcache.Add(new Uri(instanceUrl), \"Basic\", new NetworkCredential(userName, password));",
		"\t\tvar authenticator = new HttpBasicAuthenticator(userName, password);",
		"\t\tclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(\"Basic\", encodedCredentials);",
		"\t\tclient.DefaultRequestHeaders.Add(\"Authorization\", $\"Basic {encodedCredentials}\");",
		"\t\tvar handler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) };",
		"\t\tvar credential = new NetworkCredential(userName, password);",
		"\t\tstring pair = identifier + \":\" + secret;\n\t\tstring encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));",
		"\t\tclient.Headers[HttpRequestHeader.Authorization] = \"Basic \" + encoded;",
		"\t\tbinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;\n\t\tclient.ClientCredentials.UserName.UserName = userName;\n\t\tclient.ClientCredentials.UserName.Password = password;",
		"\t\tvar result = client.WithBasicAuth(userName, password);",
		"\t\tvar importApi = new ImportAPI(userName, password, webServiceUrl);",
		"\t\tNetworkCredential assembled = new NetworkCredential { UserName = userName };\n\t\tassembled.Password = password;",
		"\t\timportJob.Settings.RelativityUsername = userName;\n\t\timportJob.Settings.RelativityPassword = password;",
		"\t\tstring tokenPair = \"\" + \":\" + accessToken;\n\t\tstring tokenEncoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(tokenPair));",
		"\t\tvar sink = new SmtpClient(host, port);\n\t\tsink.Credentials = new NetworkCredential(userName, password);",
		DIRECT_HANDLER_ASSIGNMENT_WINDOW_OPENER + DIRECT_HANDLER_ASSIGNMENT_WINDOW_CLOSER,
		FILE_TRANSFER_REQUEST_WINDOW_OPENER + FILE_TRANSFER_REQUEST_WINDOW_CLOSER,
		DIRECTORY_CREDENTIAL_WINDOW_OPENER + DIRECTORY_CREDENTIAL_WINDOW_CLOSER,
		"\t\tstring headerValue = string.Format(\"Basic {0}\", EncodingHelper.ToBase64(pair));\n\t\trequest.AddHeader(\"Authorization\", headerValue);",
		"\t\tstring inlineEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{\"\"}:{accessToken}\"));",
		"\t\tclient.Headers[HttpRequestHeader.Authorization] = CreateBasicValue(userName, password);",
		"\t\tvar multiLineApi = new ImportAPI(\n\t\t\tuserName, // operator\n\t\t\tpassword,\n\t\t\twebServiceUrl);",
		MAIL_CREDENTIAL_WINDOW_OPENER + MAIL_CREDENTIAL_WINDOW_CLOSER,
		MAIL_SINK_WINDOW_OPENER + MAIL_SINK_WINDOW_CLOSER,
		"\t\tvar configured = new UsernamePasswordCredentials(settings[\"User\"], settings[\"Password\"]);",
		"\t\tvar pairCredential = new NetworkCredential(values[0][1], parts[index + 1]);",
		"\t\trequest.Headers.Add(HttpRequestHeader.Authorization, \"Basic \" + parts[1]);",
		"\t\trequest.Headers.Set(HttpRequestHeader.Authorization, \"Basic \" + encoded);",
		"\t\tstring inlinePair = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + \":\" + password));",
		"\t\tstring formattedPair = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format(\"{0}:{1}\", userName, password)));",
		"\t\tcache.Add(endpoint, \"Basic\", new NetworkCredential(userName, password));",
		"\t\tcache.Add(host, port, \"Basic\", new NetworkCredential(userName, password));",
		"\t\tvar inlineAddressClient = new RSAPIClient(new Uri(instanceUrl), new UsernamePasswordCredentials(userName, password));",
		"\t\tvar qualifiedClient = new Sdk.Rsapi.RSAPIClient(rsapiUri, new Sdk.Security.UsernamePasswordCredentials(userName, password));",
		"\t\tRSAPIClient targetClient = new(rsapiUri, new UsernamePasswordCredentials(userName, password));",
		"\t\tcache.Add(new(instanceUrl), \"Basic\", new System.Net.NetworkCredential(userName, password));",
		"\t\tHttpBasicAuthenticator targetAuthenticator = new(userName, password);",
		"\t\tvar qualifiedHeader = new System.Net.Http.Headers.AuthenticationHeaderValue(\"Basic\", encodedCredentials);",
		"\t\tAuthenticationHeaderValue targetHeader = new(\"Basic\", encodedCredentials);",
		"\t\tvar parenthesizedHandler = new HttpClientHandler() { Credentials = new NetworkCredential(userName, password) };",
		"\t\tHttpClientHandler targetInitializedHandler = new() { Credentials = new NetworkCredential(userName, password) };",
		TARGET_TYPED_HANDLER_WINDOW_OPENER + TARGET_TYPED_HANDLER_WINDOW_CLOSER,
		"\t\tNetworkCredential targetCredential = new(userName, password);",
		"\t\tvar qualifiedCredential = new System.Net.NetworkCredential(userName, password);",
		ROOTED_PAIR_WINDOW_OPENER + ROOTED_PAIR_WINDOW_CLOSER,
		"\t\tstring inlineRooted = Convert.ToBase64String(Encoding.ASCII.GetBytes(userBox.Text + \":\" + Secrets.Read(index)[0]));",
		"\t\trequest.Headers.Add(HttpRequestHeader.Authorization, AuthConstants.SCHEME + \" \" + encoded);",
		"\t\trequest.Headers.Set(HttpRequestHeader.Authorization, $\"{SCHEME} {encoded}\");",
		"\t\trequest.Headers[System.Net.HttpRequestHeader.Authorization] = \"Basic \" + encoded;",
		"\t\tbinding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Basic;",
		"\t\tImportAPI targetImport = new(userName, password, webServiceUrl);",
		"\t\tvar qualifiedImport = new Vendor.ImportAPI(userName, password, webServiceUrl);",
		"\t\tNetworkCredential parenthesizedCredential = new NetworkCredential() { UserName = userName };\n\t\tparenthesizedCredential.Password = password;",
		"\t\tvar mailInitializer = new SmtpClient(host, port) { Credentials = new NetworkCredential(userName, password) };",
		"\t\tSmtpClient targetMailInitializer = new(host, port) { Credentials = new NetworkCredential(userName, password) };",
		TARGET_TYPED_MAIL_WINDOW_OPENER + TARGET_TYPED_MAIL_WINDOW_CLOSER,
		TARGET_TYPED_SINK_FIRST_WINDOW_OPENER + TARGET_TYPED_SINK_FIRST_WINDOW_CLOSER,
		TARGET_TYPED_CREDENTIAL_FIRST_WINDOW_OPENER + TARGET_TYPED_CREDENTIAL_FIRST_WINDOW_CLOSER
	];

	/// <summary>
	/// A run of ordinary characters the encoder window of the inline pair reads in full and never closes: no
	/// colon-joined pair and no statement end within the window. Declared before the open windows that read it:
	/// a static field read before its initializer has run is null, which would leave the run empty.
	/// </summary>
	private static readonly string _ENCODER_FILLER_WITHOUT_A_PAIR = new string('x', 190);

	/// <summary>A run the indexer suffix reads up to its length bound and never closes with a bracket. Declared before its readers, for the same reason.</summary>
	private static readonly string _INDEXER_FILLER_WITHOUT_A_CLOSING_BRACKET = new string('y', 130);

	private static readonly ImmutableArray<string> _OPEN_PROXIMITY_WINDOWS =
	[
		"\t\tstring pair = identifier + \":\" + secret;",
		"\t\tbinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;",
		"\t\tNetworkCredential assembled = new NetworkCredential { UserName = userName };",
		DIRECT_HANDLER_ASSIGNMENT_WINDOW_OPENER,
		FILE_TRANSFER_REQUEST_WINDOW_OPENER,
		DIRECTORY_CREDENTIAL_WINDOW_OPENER,
		"\t\tstring headerValue = string.Format(\"Basic {0}\", encoded);",
		MAIL_CREDENTIAL_WINDOW_OPENER + "\n\t\tmailCredential.Password = password;",
		MAIL_SINK_WINDOW_OPENER,
		"\t\tstring openPair = Convert.ToBase64String(Encoding.ASCII.GetBytes(" + _ENCODER_FILLER_WITHOUT_A_PAIR,
		"\t\tcache.Add(host, port, \"Basic\", keys[" + _INDEXER_FILLER_WITHOUT_A_CLOSING_BRACKET,
		TARGET_TYPED_HANDLER_WINDOW_OPENER,
		TARGET_TYPED_MAIL_WINDOW_OPENER,
		TARGET_TYPED_SINK_FIRST_WINDOW_OPENER,
		TARGET_TYPED_CREDENTIAL_FIRST_WINDOW_OPENER + "\n\t\ttargetFirst.Password = password;",
		ROOTED_PAIR_WINDOW_OPENER,
		"\t\tstring openRooted = Convert.ToBase64String(Encoding.ASCII.GetBytes(ReadSecret(" + _ENCODER_FILLER_WITHOUT_A_PAIR,
		"\t\tvar unclosedHandler = new HttpClientHandler(" + _INDEXER_FILLER_WITHOUT_A_CLOSING_BRACKET
	];

	/// <summary>
	/// Near-miss lines for the indexer suffix and the inline pair: indexers inside indexers, a comma inside a
	/// bracket, an indexer past its length bound, and an encoder whose window holds no pair.
	/// </summary>
	private static readonly ImmutableArray<string> _INDEXER_AND_INLINE_PAIR_NEAR_MISSES =
	[
		"\t\tvar nested = new NetworkCredential(values[keys[0]], parts[1, 2]);",
		"\t\tvar overlong = new UsernamePasswordCredentials(values[" + new string('z', 125) + "], password);",
		"\t\tvar collection = new NetworkCredential([userName], [password]);",
		"\t\tstring single = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName));",
		"\t\tstring outside = Convert.ToBase64String(bytes); string joined = userName + \":\" + password;",
		"\t\tcache.Add(host[0], port[index][0], \"Basic\", factory.Create(userName, password));"
	];

	/// <summary>
	/// Near-miss lines for the creation forms, the named-constant scheme and the pair halves: types that only
	/// resemble a created type, long qualifier chains, a handler declaration that is not on the allow-list,
	/// default credentials, a constant concatenated directly, another enumeration member, a pair with a
	/// literal half or an arithmetic half, and an arity that does not match.
	/// </summary>
	private static readonly ImmutableArray<string> _CREATION_FORM_NEAR_MISSES =
	[
		"\t\tvar wrappedMailer = new Vendor.Net.Mail.SmtpClientWrapper(host, port) { Credentials = new Vendor.Net.NetworkCredentialFactory(userName, password) };",
		"\t\tvar longQualifier = new A.B.C.D.E.F.G.H.I.J.K.L.M.N.O.P.NetworkCredentialHelper(userName, password);",
		"\t\tHttpClientFactory notAHandler = new() { Credentials = CredentialCache.DefaultCredentials };",
		"\t\tSmtpClient defaultMailer = new(host, port) { UseDefaultCredentials = true };",
		"\t\tbinding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Windows;",
		"\t\trequest.Headers.Add(HttpRequestHeader.Authorization, SCHEME_WITH_SPACE + encoded);",
		"\t\trequest.Headers.Add(HttpRequestHeader.ContentType, AuthConstants.SCHEME + \" \" + encoded);",
		"\t\tstring literalPair = string.Format(\"{0}:{1}\", \"operator\", secretBox.Entry); string literalEncoded = Convert.ToBase64String(literalBytes);",
		"\t\tstring arithmeticPair = $\"{first + second}:{secretBox.Entry}\";\n\t\tstring arithmeticEncoded = Convert.ToBase64String(arithmeticBytes);",
		"\t\tstring deepPair = $\"{Get(Wrap(Inner(1)))}:{secretBox.Entry}\";",
		"\t\tImportAPI wrongArity = new(userName, webServiceUrl);",
		"\t\tHttpBasicAuthenticator lonelyAuthenticator = new(userName);",
		"\t\tvar unfinished = new HttpClientHandler(Build(Wrap(1)) { Credentials = new NetworkCredential(userName, password) };"
	];

	private static readonly ImmutableArray<string> _LONG_LINE_TOKENS =
	[
		"userBox.Text + \":\" + ",
		"Console.ReadLine() + \":\" + ",
		"credential.UserName + \":\" + ",
		"Authorization + ",
		"Convert.ToBase64String(bytes) + ",
		"new NetworkCredential(a, b).Password + ",
		"\"Basic \" + "
	];

	private static readonly ImmutableArray<string> _LITERAL_AND_COMMENT_LINES =
	[
		"\t\t// Credential, Authorization and UserName appear here only in prose, never in code.",
		"\t\tstring path = @\"C:\\data\\credentials\\archive\\Authorization\\UserName\";",
		"\t\tstring message = $\"Credential {name} refreshed for {userName} at {DateTime.UtcNow:O}\";",
		"\t\t/* new NetworkCredential(userName, password) was removed in the migration */",
		"\t\tstring note = \"Basic authentication is disabled; use a bearer token instead.\";"
	];

	/// <summary>
	/// Every flow-signal shape's name starts with this, so the shapes that carry a High construct
	/// can be told apart from the six that do not.
	/// </summary>
	internal const string FLOW_SHAPE_NAME_PREFIX = "flow-";

	/// <summary>
	/// The one High finding an invented flow-signal shape opens with: a basic-authentication
	/// extension call over invented operands. It makes the engine evaluate the seventeen flow signals
	/// over the whole file, inside the same match-bound accumulator as the detection rules.
	/// </summary>
	internal const string FLOW_SHAPE_HIGH_CONSTRUCT_LINE = "var result = client.WithBasicAuth(operatorName, accessKey);\n";

	private const int _FLOW_LONG_LITERAL_LENGTH = 200;
	private const int _FLOW_OPERAND_ARGUMENT_LENGTH = 118;
	private const int _FLOW_FILLER_STATEMENTS_PER_WINDOW = 3;
	private const string _FLOW_FILLER_STATEMENT = "\tvar total = first + second;\n";

	/// <summary>
	/// Text built to make each flow signal do its most work: each shape repeats, up to the size
	/// ceiling, the construct that opens one signal's window or alternation without ever completing
	/// it. Every statement is invented, ordinary-looking C#.
	/// </summary>
	private static readonly ImmutableArray<(string Name, string Statements)> _FLOW_SHAPE_STATEMENTS =
	[
		("credential-words-dense", "\tvar passwordValue = secretText + pinCode + apiKeyValue; // password secret token pin\n"),
		("read-key-with-no-credential-word", "\tvar pressed = Console.ReadKey(true);\n"),
		("read-key-beside-enter-comparisons", "\tif (pressed.Key == ConsoleKey.Enter) { }\n\tvar pressed = Console.ReadKey(true);\n"),
		("console-writes-with-long-literals", $"\tConsole.Write(\"{new string('a', _FLOW_LONG_LITERAL_LENGTH)}\");\n"),
		("secret-client-mentions-with-no-fetch", "\tSecretClient holder;\n"),
		("posted-attributes-with-no-login-method", "\t[HttpPost]\n\tpublic IActionResult SaveRecord(RecordInput input)\n"),
		("indexers-with-plain-keys", "\tvar value = table[\"Name\"] + table[\"Db:Passage\"];\n"),
		("form-and-collection-names", "\tvar entry = form  [  \"aaa\" + platformForm[ \"Bypass\" ];\n"),
		("instance-setting-style-calls", "\tvar value = settings.GetString(\"Section\", \"PageSize\");\n"),
		("html-and-type-assignments", "\tvar markup = \"<input type=\\\"text\\\" />\"; var type = \"aaa\";\n"),
		("member-names-of-the-signals-without-arguments", "\tvar names = nameof(AddUserSecrets) + nameof(Unprotect) + nameof(PasswordBox) + nameof(GetValue);\n"),
		("open-windows-with-filler", OpenFlowWindowsWithFiller()),
		("credential-keyed-indexers-on-configuration-receivers", "\tvar value = configuration[\"Db:Password\"] + config?[\"Api:Token\"] + authSection[\"Secret\"] + configuration.GetSection(\"Auth\")[\"Password\"];\n"),
		("credential-keyed-indexers-on-other-receivers", "\tvar value = Request.Headers[\"token\"] + Session[\"password\"] + values[\"ApiKey\"] + payload.GetValue(\"secret\") + options.GetValue<string>(\"Db:Pwd\");\n"),
		("password-box-casts-with-no-read-in-the-window", "\tvar box = (PasswordBox)sender;\n\tvar total = first + second;\n\tvar holder = other as PasswordBox;\n\t((PasswordBox)sender).Focus();\n"),
		("password-box-casts-read-on-other-variables", "\tvar box = (PasswordBox)sender; var entered = other.Password + outerbox.Password + holder.box.Password;\n"),
		("password-box-casts-read-directly-and-stored", "\tvar box = (PasswordBox)sender; var entered = box.Password + ((PasswordBox)sender).SecurePassword + (sender as PasswordBox).Password;\n"),
		("password-box-as-calls-with-no-read-in-the-window", $"\tvar box = FindName(\"PasswordInput\") as PasswordBox;\n\tvar holder = this.Root.Find(\"{new string('a', _FLOW_OPERAND_ARGUMENT_LENGTH)}\") as PasswordBox;\n\tvar other = controls[\"entry\"][index] as PasswordBox;\n\t((PasswordBox)FindName(\"PasswordInput\")).Focus();\n\tvar open = Locate(\"{new string('b', _FLOW_OPERAND_ARGUMENT_LENGTH)} as PasswordBox;\n"),
		("receiver-names-with-no-key-after-them", "\tvar value = configuration + config?[ index ] + Section [ \"Plain\" ] + GetSection(\"a\" + \"b\")(\"c\") + GetSection(((((\n")
	];

	/// <summary>
	/// The six benign shapes followed by the flow-signal shapes, in a fixed order. Each name is what
	/// the timing table reports.
	/// </summary>
	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		return
		[
			Shape("ordinary-code-at-size-ceiling", FillWithMembers(_ORDINARY_STATEMENTS)),
			Shape("anchor-dense-near-misses", FillWithMembers(_ANCHOR_DENSE_NEAR_MISSES)),
			Shape("every-class-at-match-density", FillWithMembers(_EVERY_CLASS_CONSTRUCTS)),
			Shape("open-proximity-windows", FillWithMembers(InterleaveWithFiller(_OPEN_PROXIMITY_WINDOWS))),
			Shape("long-lines-at-line-ceiling", FillWithLongLines()),
			Shape("literal-and-comment-heavy", FillWithMembers(_LITERAL_AND_COMMENT_LINES)),
			Shape("indexer-and-inline-pair-near-misses", FillWithMembers(_INDEXER_AND_INLINE_PAIR_NEAR_MISSES)),
			Shape("creation-form-near-misses", FillWithMembers(_CREATION_FORM_NEAR_MISSES)),
			Shape(MULTI_LINE_CALL_ARGUMENT_CREDENTIAL_SHAPE_NAME, FillWithMembers([MultiLineCallArgumentCredentialWithFurtherLines()])),
			Shape(NEUTRAL_COLON_PAIR_SHAPE_NAME, FillWithMembers(_NEUTRAL_COLON_PAIRS)),
			Shape(PLUS_SIGNS_WITHOUT_A_COLON_LITERAL_SHAPE_NAME, FillWithMembers(_MANY_PLUS_SIGNS_WITHOUT_A_COLON_LITERAL)),
			.. CSharpDetectionCorrectionShapes(),
			.. IdentifierSuffixShapes(),
			.. FlowShapes()
		];
	}

	/// <summary>
	/// The shapes of three detection windows, each the worst benign text for one window: a member store alternating with a sink,
	/// the same on lines of four hundred characters, stores alone, a brace-free run of the user-name half of the <c>Rsapi</c> pair, and a block comment in the argument gap of
	/// the proxy factory call that no text closes. Every shape is invented, ordinary-looking C# with generic names.
	/// </summary>
	private static ImmutableArray<MatchBoundStressShape> CSharpDetectionCorrectionShapes()
	{
		return
		[
			Shape(ALTERNATING_MEMBER_STORE_AND_SINK_SHAPE_NAME, FillWithMembers([MEMBER_STORE_STATEMENT, MEMBER_SINK_DECLARATION_STATEMENT])),
			Shape(
				ALTERNATING_MEMBER_STORE_AND_SINK_ON_LONG_LINES_SHAPE_NAME,
				FillWithMembers([MEMBER_STORE_STATEMENT.PadRight(_MEMBER_WINDOW_LINE_LENGTH_IN_CHARACTERS), MEMBER_SINK_DECLARATION_STATEMENT.PadRight(_MEMBER_WINDOW_LINE_LENGTH_IN_CHARACTERS)])),
			Shape(MEMBER_STORES_ONLY_SHAPE_NAME, FillWithMembers([MemberStoreWithItsWholeWindowOfOrdinaryLines()])),
			Shape(BRACE_FREE_REPEATED_RSAPI_USER_NAME_SHAPE_NAME, FillWithBraceFreeRsapiUserNameEntries()),
			Shape(UNTERMINATED_PROXY_FACTORY_COMMENT_SHAPE_NAME, FillWithMembers([UNTERMINATED_PROXY_FACTORY_COMMENT_CALL]))
		];
	}

	/// <summary>
	/// The shapes of the identifier-suffix walk of <c>CS-009</c>, each a colon-joined pair whose halves hold hundreds of credential-word occurrences and the encoder on the
	/// next line, so the engine reads every occurrence of both halves through the timed door and the suffix segment scanner. Camel-case runs and underscore runs in which every
	/// occurrence is suffixed (the walk visits all of them and the pair stays neutral); the same runs with only the last word unsuffixed (the walk's worst case before it stops); acronym runs and a long
	/// segment no boundary cuts; and both halves filling a line at the line ceiling. Every shape is invented, ordinary-looking C# with generic names.
	/// </summary>
	private static ImmutableArray<MatchBoundStressShape> IdentifierSuffixShapes()
	{
		string camelLeft = WordRun(_SUFFIXED_CAMEL_USER_WORDS, string.Empty, "UserId", _SUFFIX_HALF_LENGTH_IN_CHARACTERS);
		string camelRight = WordRun(_SUFFIXED_CAMEL_SECRET_WORDS, string.Empty, "PasswordId", _SUFFIX_HALF_LENGTH_IN_CHARACTERS);
		string underscoreLeft = WordRun(_SUFFIXED_UNDERSCORE_USER_WORDS, _UNDERSCORE, "login_code", _SUFFIX_HALF_LENGTH_IN_CHARACTERS);
		string underscoreRight = WordRun(_SUFFIXED_UNDERSCORE_SECRET_WORDS, _UNDERSCORE, "token_code", _SUFFIX_HALF_LENGTH_IN_CHARACTERS);
		string lineHalf = WordRun(_SUFFIXED_CAMEL_USER_WORDS, string.Empty, "UserId", LineCeilingHalfLength());
		string lineOtherHalf = WordRun(_SUFFIXED_CAMEL_SECRET_WORDS, string.Empty, "PasswordId", LineCeilingHalfLength());

		return
		[
			Shape(IDENTIFIER_SUFFIX_CAMEL_RUNS_SHAPE_NAME, FillWithMembers([SuffixedPairStatement("camelPair", camelLeft, camelRight)])),
			Shape(IDENTIFIER_SUFFIX_UNDERSCORE_RUNS_SHAPE_NAME, FillWithMembers([SuffixedPairStatement("underscorePair", underscoreLeft, underscoreRight)])),
			Shape(
				IDENTIFIER_SUFFIX_LAST_WORD_UNSUFFIXED_SHAPE_NAME,
				FillWithMembers(
				[
					SuffixedPairStatement(
						"camelLastPair",
						WordRun(_SUFFIXED_CAMEL_USER_WORDS, string.Empty, "Account", _SUFFIX_HALF_LENGTH_IN_CHARACTERS),
						WordRun(_SUFFIXED_CAMEL_SECRET_WORDS, string.Empty, "Secret", _SUFFIX_HALF_LENGTH_IN_CHARACTERS)),
					SuffixedPairStatement(
						"underscoreLastPair",
						WordRun(_SUFFIXED_UNDERSCORE_USER_WORDS, _UNDERSCORE, "account", _SUFFIX_HALF_LENGTH_IN_CHARACTERS),
						WordRun(_SUFFIXED_UNDERSCORE_SECRET_WORDS, _UNDERSCORE, "secret", _SUFFIX_HALF_LENGTH_IN_CHARACTERS))
				])),
			Shape(
				IDENTIFIER_SUFFIX_ACRONYM_RUNS_SHAPE_NAME,
				FillWithMembers(
				[
					SuffixedPairStatement(
						"acronymPair",
						WordRun(_SUFFIXED_ACRONYM_USER_WORDS, string.Empty, "USERId", _SUFFIX_HALF_LENGTH_IN_CHARACTERS),
						WordRun(_SUFFIXED_ACRONYM_SECRET_WORDS, string.Empty, "PASSWORDId", _SUFFIX_HALF_LENGTH_IN_CHARACTERS)),
					SuffixedPairStatement(
						"acronymSegmentPair",
						$"user{RepeatToLength(_ACRONYM_USER_RUN, _SUFFIX_HALF_LENGTH_IN_CHARACTERS)}",
						$"password{RepeatToLength(_ACRONYM_SECRET_RUN, _SUFFIX_HALF_LENGTH_IN_CHARACTERS)}")
				])),
			Shape(IDENTIFIER_SUFFIX_LINE_CEILING_SHAPE_NAME, FillWithMembers([SuffixedPairStatement("ceilingPair", lineHalf, lineOtherHalf)]))
		];
	}

	/// <summary>The length of each half of a pair that, with the statement around it, fills a line to <see cref="LONG_LINE_LENGTH_IN_CHARACTERS"/>.</summary>
	private static int LineCeilingHalfLength()
	{
		string framing = SuffixedPairStatement("ceilingPair", string.Empty, string.Empty);
		int firstLineFraming = framing[..framing.IndexOf(_LINE_BREAK, StringComparison.Ordinal)].Length;

		return (LONG_LINE_LENGTH_IN_CHARACTERS - firstLineFraming) / 2;
	}

	/// <summary>A colon-joined pair of the two halves as a statement of its own, and the encoder call on the next line.</summary>
	private static string SuffixedPairStatement(string variableName, string leftHalf, string rightHalf)
	{
		return string.Format(CultureInfo.InvariantCulture, _SUFFIXED_PAIR_STATEMENT_FORMAT, variableName, leftHalf, rightHalf);
	}

	/// <summary>The words cycled and joined with the separator while there is room for them and for the closing word, then the closing word: a run no longer than the length.</summary>
	private static string WordRun(ImmutableArray<string> words, string separator, string closingWord, int lengthInCharacters)
	{
		StringBuilder run = new();
		int index = 0;
		int reserved = closingWord.Length + separator.Length;

		while (run.Length + words[index % words.Length].Length + separator.Length + reserved <= lengthInCharacters)
		{
			run.Append(words[index % words.Length]).Append(separator);
			index++;
		}

		run.Append(closingWord);

		return run.ToString();
	}

	private static string RepeatToLength(string unit, int lengthInCharacters)
	{
		return string.Concat(Enumerable.Repeat(unit, (lengthInCharacters / unit.Length) + 1))[..lengthInCharacters];
	}

	/// <summary>A member store followed by the thirty lines of its window, none of which holds the credential type, so the window is read in full and never closed.</summary>
	private static string MemberStoreWithItsWholeWindowOfOrdinaryLines()
	{
		string window = string.Join(_LINE_BREAK, Enumerable.Repeat(_FILLER_STATEMENT.TrimEnd(_LINE_BREAK_CHARACTER), _MEMBER_WINDOW_LINES));

		return $"{MEMBER_STORE_STATEMENT}{_LINE_BREAK}{window}";
	}

	/// <summary>
	/// One initializer opened once and closed only at the end of the file, holding the user-name half of the <c>Rsapi</c> pair over and over and no password half: the
	/// brace-free run in which the bounded search for the password half starts at every entry.
	/// </summary>
	private static string FillWithBraceFreeRsapiUserNameEntries()
	{
		StringBuilder builder = new(_RSAPI_INITIALIZER_OPENING);
		string line = $"{_RSAPI_ENTRY_LINE_INDENT}{string.Concat(Enumerable.Repeat(RSAPI_USER_NAME_ENTRY, _RSAPI_ENTRIES_PER_LINE)).TrimEnd()}";

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(line);
			builder.Append(_LINE_BREAK);
		}

		builder.Append(_RSAPI_INITIALIZER_CLOSING);

		return builder.ToString();
	}

	/// <summary>
	/// The flow-signal shapes. Each opens with <see cref="FLOW_SHAPE_HIGH_CONSTRUCT_LINE"/> so the
	/// engine evaluates the flow signals over it, and is then filled with the statements that make
	/// one signal do its most work.
	/// </summary>
	private static ImmutableArray<MatchBoundStressShape> FlowShapes()
	{
		return
		[
			.. _FLOW_SHAPE_STATEMENTS.Select(flowShape => Shape(
				FLOW_SHAPE_NAME_PREFIX + flowShape.Name,
				FLOW_SHAPE_HIGH_CONSTRUCT_LINE + FillWithMembers([flowShape.Statements.TrimEnd('\n')])))
		];
	}

	/// <summary>
	/// The multi-line credential window followed by enough ordinary lines, none naming a directory entry, for the
	/// bounded window that looks for one to run out of lines instead of ending at the end of the file.
	/// </summary>
	private static string MultiLineCallArgumentCredentialWithFurtherLines()
	{
		string furtherLines = string.Concat(Enumerable.Repeat(_FILLER_STATEMENT, _FURTHER_LINES_AFTER_THE_WINDOW)).TrimEnd(_LINE_BREAK_CHARACTER);

		return $"{MULTI_LINE_CALL_ARGUMENT_CREDENTIAL_WINDOW}{_LINE_BREAK}{furtherLines}";
	}

	private static string OpenFlowWindowsWithFiller()
	{
		string filler = string.Concat(Enumerable.Repeat(_FLOW_FILLER_STATEMENT, _FLOW_FILLER_STATEMENTS_PER_WINDOW));

		return "\tvar seed = password + Console.ReadKey(true);\n" + filler
			+ "\tSecretClient holder;\n" + filler
			+ "\t[HttpPost]\n" + filler;
	}

	private static MatchBoundStressShape Shape(string name, string text)
	{
		return new MatchBoundStressShape { Name = name, Text = text };
	}

	/// <summary>
	/// Wraps the statements in members of a class and repeats them until the text reaches the
	/// target size. A member holds one pass over the statements, so every statement recurs at the
	/// same density from the first line of the file to the last.
	/// </summary>
	private static string FillWithMembers(ImmutableArray<string> statements)
	{
		StringBuilder builder = new();
		string body = string.Concat(statements.Select(statement => statement + _LINE_BREAK));
		int memberIndex = 0;

		builder.Append("namespace Synthetic.Stress\n{\ninternal static class StressSubject\n{\n");

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.AppendFormat(CultureInfo.InvariantCulture, _MEMBER_FORMAT, memberIndex, body);
			memberIndex++;
		}

		builder.Append("}\n}\n");

		return builder.ToString();
	}

	/// <summary>
	/// Follows each window-opening statement with four ordinary statements, so the five-line
	/// window each opens is searched in full and never closed.
	/// </summary>
	private static ImmutableArray<string> InterleaveWithFiller(ImmutableArray<string> openingStatements)
	{
		const int FILLER_STATEMENTS_PER_WINDOW = 4;
		string filler = string.Concat(Enumerable.Repeat(_FILLER_STATEMENT, FILLER_STATEMENTS_PER_WINDOW)).TrimEnd('\n');

		return [.. openingStatements.Select(opening => opening + _LINE_BREAK + filler)];
	}

	/// <summary>
	/// Lines at the line-length ceiling, each one long concatenation of anchor-bearing tokens -
	/// the shape a generated file produces, and the one where an expression's per-line work is
	/// largest.
	/// </summary>
	private static string FillWithLongLines()
	{
		StringBuilder builder = new();
		string longLine = BuildOneLongLine();

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(longLine);
			builder.Append(_LINE_BREAK);
		}

		return builder.ToString();
	}

	private static string BuildOneLongLine()
	{
		const string LINE_OPENING = "\t\tstring assembled = ";
		const string LINE_CLOSING = "string.Empty;";
		StringBuilder line = new(LINE_OPENING);
		int tokenIndex = 0;
		int bodyLimit = LONG_LINE_LENGTH_IN_CHARACTERS - LINE_CLOSING.Length;

		while (line.Length + _LONG_LINE_TOKENS[tokenIndex % _LONG_LINE_TOKENS.Length].Length <= bodyLimit)
		{
			line.Append(_LONG_LINE_TOKENS[tokenIndex % _LONG_LINE_TOKENS.Length]);
			tokenIndex++;
		}

		line.Append(LINE_CLOSING);

		return line.ToString();
	}
}
