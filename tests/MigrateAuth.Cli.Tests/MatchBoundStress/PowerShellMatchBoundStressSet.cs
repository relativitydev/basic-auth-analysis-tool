using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE `PowerShell` SHAPES OF THE MATCH-BOUND STRESS SET, written beside the `CSharp` set and read by the same kind of test: every shape is legal,
/// ordinary-looking script text that a client repository could contain, filled to just under the reader's size ceiling, and none is built to make an
/// expression backtrack catastrophically - that is what the bound exists to stop, not what it must let through. What the shapes do is push every honest
/// cost to the ceilings the engine already enforces: files at the size ceiling, lines at the line-length ceiling, anchors and matches at the densest a
/// generated script plausibly carries, and every bounded window of the catalog (the command window, the eight-line receiver window, the ten-line pair window,
/// the twenty-line splat window, the here-string and the block comment) opened over and over without being closed.
///
/// Every shape is deterministic and invented: placeholders and generic names, no fragment of any repository.
/// </summary>
internal static class PowerShellMatchBoundStressSet
{
	internal const int TARGET_SIZE_IN_CHARACTERS = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;

	internal const int LONG_LINE_LENGTH_IN_CHARACTERS = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _LINE_MARGIN_IN_CHARACTERS;

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;
	private const int _LINE_MARGIN_IN_CHARACTERS = 16;
	private const int _FILLER_LINES_PER_WINDOW = 4;
	private const string _LINE_BREAK = "\n";
	private const string _FUNCTION_FORMAT = "function Invoke-Step{0:D6}\n{{\n{1}}}\n";
	private const string _FILLER_STATEMENT = "\t$total = $first + $second\n";

	private static readonly ImmutableArray<string> _ORDINARY_STATEMENTS =
	[
		"\t$order = Get-Order -Identifier $orderIdentifier",
		"\tif ($null -eq $order) { return }",
		"\t$amount = ($order.Lines | Measure-Object -Property Total -Sum).Sum",
		"\tWrite-Verbose \"Order $($order.Identifier) totals $amount\"",
		"\t$items | ForEach-Object { $_.Name.ToUpper() } | Sort-Object"
	];

	private static readonly ImmutableArray<string> _ANCHOR_DENSE_NEAR_MISSES =
	[
		"\t$label = $options.CredentialName",
		"\t$has = $request.Headers.ContainsKey('Authorization')",
		"\t$mode = $settings.AuthenticationMode",
		"\t$payload = [Convert]::ToBase64String($documentBytes)",
		"\t$fallback = [System.Net.CredentialCache]::DefaultCredentials",
		"\tInvoke-WebRequest -Uri $uri -UseBasicParsing -UseDefaultCredentials",
		"\tGet-Credential | Out-Null",
		"\t$name = 'Basic'",
		"\t# Authorization and Credential in prose only, never in code"
	];

	private static readonly ImmutableArray<string> _EVERY_CLASS_CONSTRUCTS =
	[
		"\t$credentials = New-Object Vendor.Sdk.UsernamePasswordCredentials($loginName, $loginSecret)",
		"\t$proxy = New-Object Vendor.Sdk.RSAPIClient ((New-Object System.Uri -ArgumentList $address), (New-Object Vendor.Sdk.UsernamePasswordCredentials($loginName, $loginSecret)))",
		"\tInvoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic",
		"\t$headers = @{ Authorization = \"Basic $encoded\" }",
		"\t$headerTable.Add('Authorization', 'Basic ' + $encoded)",
		"\t$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, \"Basic $encoded\")",
		"\t$value = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Basic', $encoded)",
		"\t$cache.Add([Uri]$uri, 'Basic', (New-Object System.Net.NetworkCredential($loginName, $loginSecret)))",
		"\t$handler = New-Object System.Net.Http.HttpClientHandler\n\t$handler.Credentials = New-Object System.Net.NetworkCredential($loginName, $loginSecret)",
		"\t$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(\"$($loginName):$($loginSecret)\"))",
		"\t$tokenPair = \":\" + $accessToken\n\t$tokenEncoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($tokenPair))",
		"\tInvoke-RestMethod -Uri $uri -Credential $credential",
		"\tInvoke-Command -ComputerName $server -Credential $credential -ScriptBlock { hostname }",
		"\tfunction New-BasicAuthorizationHeader { param($loginName, $loginSecret) }",
		"\t$secret = Get-Credential\n\t$stored = $env:SERVICE_PASSWORD\n\t$secure = ConvertTo-SecureString $plain -AsPlainText -Force",
		"\t$params = @{ Uri = $uri; Authentication = 'Basic' }\n\tInvoke-RestMethod @params"
	];

	private static readonly ImmutableArray<string> _OPEN_WINDOWS =
	[
		"\t$wc = New-Object System.Net.WebClient",
		"\t$mailer = New-Object System.Net.Mail.SmtpClient",
		"\t$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession",
		"\t$params = @{ Uri = $uri; Credential = $credential; Authentication = 'Basic' }",
		"\t$pair = \"$($loginName):$($loginSecret)\"",
		"\t$empty = ''",
		"\t$value = \"Basic $encoded\"",
		"\t$parts = $loginName, $loginSecret",
		"\t$cache = New-Object System.Net.CredentialCache",
		"\tInvoke-RestMethod -Uri $uri -Headers @{ Accept = 'application/json' } -Method Get -ContentType 'application/json' -TimeoutSec 30"
	];

	private static readonly ImmutableArray<string> _LONG_LINE_TOKENS =
	[
		"Invoke-RestMethod -Uri $uri -Method Get ",
		"irm $uri -Cr ",
		"New-Object System.Net.NetworkCredential($a, $b) ",
		"$x.Credentials = ",
		"[Convert]::ToBase64String($bytes) + ",
		"@{ Authorization = 'Basic ' + "
	];

	/// <summary>
	/// THE EIGHT BENIGN LONG-LINE SHAPES: each file is lines of just under the line-length ceiling that repeat one
	/// ordinary statement group, after a first line that carries a preselection anchor so that every class runs over it. No group holds a construct of any
	/// class: the splat tables are never splatted, the arrays are never joined, the credentials are variables. What each exercises is a window that opens at
	/// a frequent token and is scanned forward for a rarer one, which on a line at the ceiling costs the square of the line length. Each is named
	/// <c>ps-long-line-</c> and its slug.
	/// </summary>
	private static readonly ImmutableArray<(string Slug, string Group)> _LONG_LINE_GROUPS =
	[
		("splat-authentication", "$p = @{ Authentication = $s }; "),
		("splat-credential", "$p = @{ Credential = $c }; "),
		("array-assignments", "$a = $x, $y; "),
		("web-client-credentials-assignment", "$x.Credentials = $y; New-Object System.Net.WebClient; "),
		("mail-client-credentials-assignment", "$m.Credentials = $y; New-Object System.Net.Mail.SmtpClient; "),
		("encoder-calls-followed-by-values", "[Convert]::ToBase64String($bytes) + $value, "),
		("credential-creations-and-collection-adds", "New-Object System.Net.NetworkCredential($a, $b) $list.Add($item) "),
		("web-commands-without-credentials", "Invoke-RestMethod -Uri $uri -Method Get iwr $uri -OutFile $path ")
	];

	/// <summary>The first line of every long-line shape: an encoder call in code, which is a preselection anchor, so the file is analyzed.</summary>
	private const string _LONG_LINE_SHAPE_ANCHOR_LINE = "$encoded = [Convert]::ToBase64String($bytes)";

	private const string _LONG_LINE_SHAPE_NAME_PREFIX = "ps-long-line-";

	private static readonly ImmutableArray<string> _LITERAL_AND_COMMENT_LINES =
	[
		"\t# Credential, Authorization and Authentication appear here only in prose, never in code.",
		"\t$path = 'C:\\data\\credentials\\archive\\Authorization'",
		"\t$message = \"Credential $name refreshed for $($user.Name) at $(Get-Date -Format o)\"",
		"\t<# Invoke-RestMethod -Credential $c -Authentication Basic was removed in the migration #>",
		"\t$note = 'Basic authentication is disabled; use a bearer token instead.'"
	];

	private static readonly ImmutableArray<string> _LEXICAL_HEAVY_LINES =
	[
		"\t$body = @\"  \n{ \"name\": \"item\", \"kind\": 'x' }\n\"@",
		"\t$sql = @'\t\nSELECT * FROM items WHERE kind = 'x'\n'@",
		"\t$text = \"nested $($a.Replace('x', $(1 + (2 * (3 + 4))))) done\"",
		"\t$smart = \u201Ctext with ' and #\u201D + \u2018literal\u2019",
		"\tWrite-Host `\"escaped`\" # trailing comment a#b `# not a comment"
	];

	/// <summary>The prefix of every flow-signal shape's name, so the timing procedure can tell which shapes the engine must run the flow signals over.</summary>
	internal const string FLOW_SHAPE_NAME_PREFIX = "ps-flow-";

	/// <summary>The extension of the shapes that stand as extensionless files: the binary-like ones and the ones attributed by a first line alone.</summary>
	internal const string EXTENSIONLESS = "";

	/// <summary>
	/// The first line of every flow-signal shape: one construct the catalog reads at High, so the engine runs the flow signals over the rest of the
	/// file, which is then filled with the text one signal does its most work on.
	/// </summary>
	internal const string FLOW_SHAPE_HIGH_CONSTRUCT_LINE =
		"$credentials = New-Object Vendor.Sdk.UsernamePasswordCredentials($loginName, $loginSecret)\n";

	private const string _SCRIPT_EXTENSION = ".ps1";
	private const string _STRESS_RELATIVE_PATH_FORMAT = "stress/{0}{1}";
	private const int _REMOTING_PADDING_REPEATS = 26;
	private const int _BINARY_LIKE_LINE_COUNT_PERIOD = 7;
	private const int _BINARY_LIKE_SHORT_LINE_LENGTH = 61;
	private const string _REMOTING_PADDING_UNIT = "-Name $value ";
	private const string _PADDED_SHEBANG_LINE = "#!/usr/bin/env pwsh";
	private const string _PADDING_BLANK = " ";
	private const string _PATH_SEGMENT_SHEBANG_OPENING = "#!";
	private const string _PATH_SEGMENT = "/seg";

	private static readonly string _REMOTING_PADDING = string.Concat(Enumerable.Repeat(_REMOTING_PADDING_UNIT, _REMOTING_PADDING_REPEATS));

	/// <summary>
	/// What each flow signal does its most work on, one shape each: the credential prompts themselves; a credential prompt behind a long remoting command
	/// the exclusion has to read back over; a console read with a long argument list and no secure switch; console reads whose prompt is long and holds no
	/// credential word; environment reads whose name is long and holds none, and names that hold a credential word only as part of another word; secure-string
	/// conversions of every argument shape; a password control opened and never read; the secret-store and saved-credential commands; and command names with
	/// nothing after them.
	/// </summary>
	private static readonly ImmutableArray<(string Name, ImmutableArray<string> Statements)> _FLOW_SHAPE_STATEMENTS =
	[
		(
			"credential-prompts-dense",
			[
				"\t$secret = Get-Credential",
				"\t$dialog = $Host.UI.PromptForCredential('Sign in', 'Enter your details', '', '')",
				"\t$typed = Read-Host -Prompt 'value' -AsSecureString",
				"\t$masked = Read-Host 'value' -MaskInput"
			]
		),
		(
			"get-credential-behind-long-remoting-commands",
			[
				"\tInvoke-Command -ComputerName $server " + _REMOTING_PADDING + "$account = Get-Credential",
				"\tEnter-PSSession -ComputerName $server " + _REMOTING_PADDING + "$account = Get-Credential"
			]
		),
		(
			"read-host-with-long-argument-lists-and-no-secure-switch",
			[
				"\t$answer = Read-Host -Prompt 'Name' " + _REMOTING_PADDING + "-As",
				"\t$answer = Read-Host " + _REMOTING_PADDING + "-Mask"
			]
		),
		(
			"read-host-with-long-prompts-and-no-credential-word",
			[
				"\t$site = Read-Host 'Please type the name of the site you would like to visit today, for example the main office or the warehouse, and then press enter'",
				"\t$site = Read-Host -Prompt \"Please type the name of the site you would like to visit today, for example the main office or the warehouse, and then press enter\""
			]
		),
		(
			"environment-reads-with-no-credential-word",
			[
				"\t$endpoint = $env:SERVICE_ENDPOINT_BASE_ADDRESS_FOR_THE_REGION_AND_ZONE",
				"\t$endpoint = ${env:SERVICE_ENDPOINT_BASE_ADDRESS_FOR_THE_REGION_AND_ZONE}",
				"\t$endpoint = [System.Environment]::GetEnvironmentVariable('SERVICE_ENDPOINT_BASE_ADDRESS_FOR_THE_REGION_AND_ZONE')",
				"\t$endpoint = Get-Item Env:SERVICE_ENDPOINT_BASE_ADDRESS_FOR_THE_REGION_AND_ZONE"
			]
		),
		(
			"environment-names-with-whole-token-near-misses",
			[
				"\t$compass = $env:COMPASS_HEADING_DEGREES",
				"\t$bypass = $env:BYPASS_LIST_FOR_PROXY",
				"\t$layout = $env:API_KEYBOARD_LAYOUT",
				"\t$mode = $env:PASSTHROUGH_MODE_ENABLED"
			]
		),
		(
			"secure-string-conversions",
			[
				"\t$secure = ConvertTo-SecureString $plain -AsPlainText -Force",
				"\t$secure = ConvertTo-SecureString 'a literal' -AsPlainText -Force",
				"\t$secure = ConvertTo-SecureString -String $plain -AsPlainText -Force",
				"\t$secure = ConvertTo-SecureString " + _REMOTING_PADDING + "-AsPlainText"
			]
		),
		(
			"password-control-opened-and-never-read",
			InterleaveWithFiller(
			[
				"\t$box = New-Object System.Windows.Controls.PasswordBox",
				"\t$box = [System.Windows.Controls.PasswordBox]::new()"
			])
		),
		(
			"secret-store-and-saved-credential-commands",
			[
				"\t$saved = Import-Clixml -Path $path",
				"\t$value = Get-Secret -Name $name",
				"\t$value = Get-StoredCredential -Target $name",
				"\t$value = Get-AutomationPSCredential -Name $name"
			]
		),
		(
			"command-names-with-nothing-after-them",
			[
				"\tRead-Host",
				"\tConvertTo-SecureString",
				"\tGet-Credential-Helper",
				"\t$Host.UI.PromptForCredential"
			]
		),
		(
			"open-flow-windows-with-filler",
			InterleaveWithFiller(
			[
				"\t$answer = Read-Host -Prompt 'x'",
				"\t$secure = ConvertTo-SecureString $text",
				"\t$box = New-Object Windows.Controls.PasswordBox"
			])
		)
	];

	/// <summary>What the file-scoped symbol table's constant declaration and the content markers do their most work on: quoted constants, near misses of them, and test-framework blocks.</summary>
	private static readonly ImmutableArray<string> _CONSTANT_DECLARATIONS_AND_MARKERS =
	[
		"\t$endpoint = 'https://example.invalid/api'",
		"\t${scope:name} = \"plain text\"",
		"\t$script:value = 'x' # trailing comment",
		"\t$interpolated = \"value $other\"",
		"\t$unterminated = 'no closing quote",
		"\tfunction Invoke-Check([string]$mode = 'default', [int]$count = 3) { }",
		"\tDescribe 'a block' { It 'checks a value' { $true | Should -Be $true } }",
		"\t# generated by a tool, edited by hand"
	];

	private static readonly ImmutableArray<string> _FIRST_LINE_SHAPE_BODY =
	[
		"\t$order = Get-Order -Identifier $orderIdentifier",
		"\t$amount = ($order.Lines | Measure-Object -Property Total -Sum).Sum",
		"\tWrite-Verbose \"Order $($order.Identifier) totals $amount\""
	];

	/// <summary>
	/// Every shape of the set, in a fixed order, each name being what the timing table reports: the benign script shapes, the flow-signal shapes,
	/// then the shapes that put the first line and the binary-like file under the bound.
	/// </summary>
	internal static ImmutableArray<MatchBoundStressShape> Build()
	{
		return
		[
			Shape("ps-ordinary-code-at-size-ceiling", FillWithFunctions(_ORDINARY_STATEMENTS)),
			Shape("ps-anchor-dense-near-misses", FillWithFunctions(_ANCHOR_DENSE_NEAR_MISSES)),
			Shape("ps-every-class-at-match-density", FillWithFunctions(_EVERY_CLASS_CONSTRUCTS)),
			Shape("ps-open-windows", FillWithFunctions(InterleaveWithFiller(_OPEN_WINDOWS))),
			Shape("ps-long-lines-at-line-ceiling", FillWithLongLines()),
			.. LongLineShapes(),
			Shape("ps-literal-and-comment-heavy", FillWithFunctions(_LITERAL_AND_COMMENT_LINES)),
			Shape("ps-lexical-heavy", FillWithFunctions(_LEXICAL_HEAVY_LINES)),
			Shape("ps-lone-commands-and-parameters", FillWithFunctions(_LONE_COMMANDS)),
			Shape("ps-constant-declarations-and-markers-dense", FillWithFunctions(_CONSTANT_DECLARATIONS_AND_MARKERS)),
			.. FlowShapes(),
			.. FirstLineAndBinaryLikeShapes()
		];
	}

	/// <summary>
	/// The path a shape stands at, by name and extension, so the engine sees the extension a real file of the shape would have, or none.
	/// </summary>
	internal static string RelativePathOf(MatchBoundStressShape shape)
	{
		return string.Format(CultureInfo.InvariantCulture, _STRESS_RELATIVE_PATH_FORMAT, shape.Name, shape.FileExtension);
	}

	/// <summary>
	/// Each opens with <see cref="FLOW_SHAPE_HIGH_CONSTRUCT_LINE"/>, so the engine evaluates the flow signals over it, and is then filled with the
	/// statements that make one signal do its most work.
	/// </summary>
	private static ImmutableArray<MatchBoundStressShape> FlowShapes()
	{
		return
		[
			.. _FLOW_SHAPE_STATEMENTS.Select(flowShape => Shape(
				FLOW_SHAPE_NAME_PREFIX + flowShape.Name,
				FLOW_SHAPE_HIGH_CONSTRUCT_LINE + FillWithFunctions(flowShape.Statements)))
		];
	}

	/// <summary>
	/// THE SHAPES THE LEXICAL-BUDGET SET SHARES WITH THE MATCH BOUND. An extensionless script is attributed by its first line alone, so a first line at the
	/// reader's line-length ceiling - a shebang padded to it, or an interpreter path of many segments - opens a whole file of ordinary script at the size
	/// ceiling; and a binary-like extensionless file, all noise and lines at the ceiling, is what the walk can hand the analyzer when the noise decodes.
	/// </summary>
	private static ImmutableArray<MatchBoundStressShape> FirstLineAndBinaryLikeShapes()
	{
		string body = FillWithFunctions(_FIRST_LINE_SHAPE_BODY);

		return
		[
			Shape(
				"ps-first-line-shebang-at-line-ceiling",
				PowerShellFirstLineStressSet.PadWith(_PADDED_SHEBANG_LINE, _PADDING_BLANK) + _LINE_BREAK + body,
				EXTENSIONLESS),
			Shape(
				"ps-first-line-path-segments-at-line-ceiling",
				PowerShellFirstLineStressSet.PadWith(_PATH_SEGMENT_SHEBANG_OPENING, _PATH_SEGMENT) + _LINE_BREAK + body,
				EXTENSIONLESS),
			Shape("ps-binary-like-extensionless", FillWithNoise(), EXTENSIONLESS)
		];
	}

	/// <summary>
	/// Lines of deterministic noise to the size ceiling, mostly at the line-length ceiling and every few lines a short one, the first line included.
	/// </summary>
	private static string FillWithNoise()
	{
		StringBuilder builder = new();
		int bytes = 0;
		int lineNumber = 0;

		while (true)
		{
			int length = lineNumber % _BINARY_LIKE_LINE_COUNT_PERIOD == _BINARY_LIKE_LINE_COUNT_PERIOD - 1
				? _BINARY_LIKE_SHORT_LINE_LENGTH
				: PowerShellFirstLineStressSet.LINE_LENGTH_IN_CHARACTERS;
			string line = PowerShellFirstLineStressSet.NoiseLine(string.Empty, PowerShellFirstLineStressSet.NOISE_SEED + (ulong)lineNumber, length) + _LINE_BREAK;

			int lineBytes = Encoding.UTF8.GetByteCount(line);

			if (bytes + lineBytes > TARGET_SIZE_IN_CHARACTERS)
			{
				break;
			}

			builder.Append(line);
			bytes += lineBytes;
			lineNumber++;
		}

		return builder.ToString();
	}

	private static MatchBoundStressShape Shape(string name, string text, string fileExtension = _SCRIPT_EXTENSION)
	{
		return new MatchBoundStressShape { Name = name, Text = text, FileExtension = fileExtension };
	}

	private static readonly ImmutableArray<string> _LONE_COMMANDS =
	[
		"\tInvoke-RestMethod",
		"\tirm",
		"\tNew-Object",
		"\tStart-Process -FilePath $p -Credential",
		"\tConvertTo-SecureString -AsPlainText",
		"\tRead-Host -Prompt 'name'",
		"\t$h = @{",
		"\t[Convert]::ToBase64String(",
		"\t$x.Add('Authorization',"
	];

	/// <summary>One file per group: the anchor line, then lines of one group repeated up to the line-length ceiling, to the size ceiling.</summary>
	private static ImmutableArray<MatchBoundStressShape> LongLineShapes()
	{
		return
		[
			.. _LONG_LINE_GROUPS.Select(group => Shape(
				_LONG_LINE_SHAPE_NAME_PREFIX + group.Slug,
				FillWithRepeatedGroupLines(group.Group)))
		];
	}

	private static string FillWithRepeatedGroupLines(string group)
	{
		StringBuilder line = new();

		while (line.Length + group.Length <= LONG_LINE_LENGTH_IN_CHARACTERS)
		{
			line.Append(group);
		}

		string groupLine = line.ToString().TrimEnd() + _LINE_BREAK;
		StringBuilder builder = new(_LONG_LINE_SHAPE_ANCHOR_LINE + _LINE_BREAK);

		while (builder.Length < TARGET_SIZE_IN_CHARACTERS)
		{
			builder.Append(groupLine);
		}

		return builder.ToString();
	}

	private static string FillWithFunctions(ImmutableArray<string> statements)
	{
		StringBuilder builder = new();
		string body = string.Concat(statements.Select(statement => statement + _LINE_BREAK));
		int memberIndex = 0;
		int bytes = 0;

		while (bytes < TARGET_SIZE_IN_CHARACTERS)
		{
			string member = string.Format(CultureInfo.InvariantCulture, _FUNCTION_FORMAT, memberIndex, body);

			builder.Append(member);
			bytes += Encoding.UTF8.GetByteCount(member);
			memberIndex++;
		}

		return builder.ToString();
	}

	private static ImmutableArray<string> InterleaveWithFiller(ImmutableArray<string> openingStatements)
	{
		string filler = string.Concat(Enumerable.Repeat(_FILLER_STATEMENT, _FILLER_LINES_PER_WINDOW)).TrimEnd('\n');

		return [.. openingStatements.Select(opening => opening + _LINE_BREAK + filler)];
	}

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
		const string LINE_CLOSING = "$done = 1";
		StringBuilder line = new("$assembled = ");
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
