using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// THE USER GUIDE IS CHECKED AGAINST THE THINGS IT DESCRIBES.
///
/// The first comparison is against three checklists read from the tool itself: the commands <c>help</c>
/// prints, the properties of <c>ConfigurationDocument</c> and the members of <see cref="ExitCode"/>. Each
/// comparison is by set equality, so a command, a key or a code that the guide lacks fails, and so does one
/// the guide documents that the tool does not have.
/// </summary>
public sealed partial class UserGuideChecklistTests
{
	private const string _READING_THE_REPORT_HEADING = "## Reading the report";
	private const string _COMMANDS_HEADING = "## The commands";
	private const string _CONFIGURATION_HEADING = "## The configuration file";
	private const string _EXIT_CODES_HEADING = "## Exit codes";
	private const string _DOES_AND_DOES_NOT_HEADING = "## What the tool does and does not do";
	private const string _CONFIGURATION_DOCUMENT_TYPE_NAME = "MigrateAuth.Cli.Configuration.ConfigurationDocument";
	private const string _HELP_COMMAND_LINE_INDENT = "  ";
	private const string _HELP_NAME_TERMINATOR = "  ";
	private const string _ALTERNATE_NAME_SEPARATOR = "/";
	private const string _TABLE_ROW_PREFIX = "|";
	private const char _TABLE_CELL_SEPARATOR = '|';
	private const char _LINE_BREAK = '\n';
	private const char _CARRIAGE_RETURN = '\r';
	private const char _CODE_SPAN_MARKER = '`';
	private const string _CODE_SPAN_PATTERN = "`([^`]+)`";
	private const string _TOKEN_SPLIT_PATTERN = "[^A-Za-z0-9]+";
	private const string _YEAR_PATTERN = "\\b(19|20)\\d{2}\\b";
	private const string _VERSION_LINE_FORMAT = "{0} {1} (catalog {2})";
	private const string _PART_A_LINK = "../migration/part-a-csharp.md";
	private const string _PART_C_LINK = "../migration/part-c-csharp.md";
	private const string _OAUTH2_CLIENTS_URL = "https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm";
	private const string _REST_AUTHENTICATION_URL = "https://platform.relativity.com/RelativityOne/Content/REST_API/REST_API_authentication.htm";
	private const string _ALPHA_WORD = "alpha";
	private const string _CSRF_RULE = "X-CSRF-Header: -";
	private const string _CSRF_CONSEQUENCE = "404";
	private const string _NOT_ANALYZED_SENTENCE = "not analyzed in this release";
	private const string _EMPTY_RESULT_SENTENCE = "does not mean that no Basic Authentication was found";
	private const string _DEFAULT_EXPECTED_SENTENCE = "`Default` is the expected decision for most real integrations";
	private const string _TWO_FILES_SENTENCE = "reported as **two files**";
	private const string _NTFS_NAME = "Windows (NTFS)";
	private const string _MACOS_NAME = "macOS";
	private const string _CAPS_SENTENCE = "a bound on pathological or generated input";
	private const string _FILE_CAP_TEXT = "2,000";
	private const string _SCAN_CAP_TEXT = "50,000";
	private const string _JAVASCRIPT_BARE_MEMBER_TEXT = "bare member name";
	private const string _JAVASCRIPT_FUNCTION_HEADER_TEXT = "built inside the project's own function";

	private static readonly string[] _statementsOfWhatTheToolDoesNot =
	[
		"analyzes only", "never edits your code", "never contacts RelativityOne or any network service", "never configures an OAuth2 Client"
	];

	private static readonly string[] _knownIssuePhrases =
	[
		"verified by NSerio against a live RelativityOne instance", "desktop applications", "binaries are unsigned",
		"continuous integration and delivery is deferred", "first launch of a freshly downloaded copy", "is not resumed",
		"decomposed Unicode form", "the finding's own file", "planned after the alpha"
	];

	/// <summary>Every command the tool's <c>help</c> lists is in the guide's command table, and the table lists no other command.</summary>
	[Fact]
	public void TheGuideDocumentsEveryCommandHelpListsAndNoOther()
	{
		string[] listedByHelp = CommandNamesPrintedByHelp();
		string[] documented = FirstColumnCodeNames(SectionOfRow(PublicDocuments.ROW_USING_THE_TOOL, _COMMANDS_HEADING));

		Assert.Equal(listedByHelp.Order(StringComparer.Ordinal), documented.Order(StringComparer.Ordinal));
		Assert.All(CommandRegistry.CommandsInHelpOrder, command => Assert.Contains(command.Name, listedByHelp));
	}

	/// <summary>Every key <c>ConfigurationDocument</c> accepts is in the guide's key table, and the table lists no other key.</summary>
	[Fact]
	public void TheGuideDocumentsEveryConfigurationKeyAndNoOther()
	{
		Type document = typeof(CommandRegistry).Assembly.GetType(_CONFIGURATION_DOCUMENT_TYPE_NAME, throwOnError: true)!;
		string[] accepted =
		[
			.. document.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name))
		];
		string[] documented = FirstColumnCodeNames(SectionOfRow(PublicDocuments.ROW_USING_THE_TOOL, _CONFIGURATION_HEADING));

		Assert.NotEmpty(accepted);
		Assert.Equal(accepted.Order(StringComparer.Ordinal), documented.Order(StringComparer.Ordinal));
	}

	/// <summary>Every member of <see cref="ExitCode"/> is in the guide's exit-code table with its number, and the table lists no other code.</summary>
	[Fact]
	public void TheGuideDocumentsEveryExitCodeWithItsNumberAndName()
	{
		string section = SectionOfRow(PublicDocuments.ROW_USING_THE_TOOL, _EXIT_CODES_HEADING);
		string[] documented = [.. TableRows(section).Select(cells => $"{cells[0]}={cells[1].Trim(_CODE_SPAN_MARKER)}")];
		string[] members = [.. Enum.GetValues<ExitCode>().Select(code => $"{(int)code}={code}")];

		Assert.Equal(members.Order(StringComparer.Ordinal), documented.Order(StringComparer.Ordinal));
	}

	/// <summary>The section on what the tool does and does not do states each of the four things.</summary>
	[Fact]
	public void TheGuideStatesWhatTheToolDoesNotDo()
	{
		string section = SectionOfRow(PublicDocuments.ROW_USING_THE_TOOL, _DOES_AND_DOES_NOT_HEADING);

		Assert.All(_statementsOfWhatTheToolDoesNot, statement => Assert.Contains(statement, section, StringComparison.Ordinal));
	}

	/// <summary>
	/// A report shows only the fields its run produced, and a list that was empty in that run
	/// shows none of its members, so this check runs over the tool's own document types: every property of
	/// the artifact, at every level, is named.
	/// </summary>
	[Fact]
	public void ReadingTheReportNamesEveryFieldOfTheArtifactSchema()
	{
		HashSet<string> named = CodeSpanTokens(SectionOfRow(PublicDocuments.ROW_USING_THE_TOOL, _READING_THE_REPORT_HEADING));

		string[] missing = [.. SchemaFieldNames(typeof(ArtifactDocument)).Distinct().Order(StringComparer.Ordinal).Where(name => !named.Contains(name))];

		Assert.True(missing.Length == 0, $"'Reading the report' does not name the schema fields: {string.Join(", ", missing)}.");
	}

	/// <summary>The guide quotes what <c>--version</c> prints, built from the constants the tool prints it from.</summary>
	[Fact]
	public void TheGuideQuotesTheVersionLineTheToolPrints()
	{
		string usingTheTool = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_USING_THE_TOOL);
		string versionLine = string.Format(
			CultureInfo.InvariantCulture,
			_VERSION_LINE_FORMAT,
			CommandLineParser.PRODUCT_NAME,
			ToolVersion.CURRENT,
			CSharpRenderedTexts.Catalog().Version.DisplayValue);

		Assert.Contains(versionLine, usingTheTool, StringComparison.Ordinal);
	}

	/// <summary>Row 1 links to Part A and Part C and keeps the SmartScreen first-run steps.</summary>
	[Fact]
	public void RowOneLinksToPartsAAndCAndKeepsTheWindowsFirstRunSteps()
	{
		string usingTheTool = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_USING_THE_TOOL);

		Assert.Contains($"({_PART_A_LINK}", usingTheTool, StringComparison.Ordinal);
		Assert.Contains($"({_PART_C_LINK}", usingTheTool, StringComparison.Ordinal);
		Assert.Contains("SmartScreen", usingTheTool, StringComparison.Ordinal);
		Assert.Contains("Run anyway", usingTheTool, StringComparison.Ordinal);
	}

	/// <summary>The release notes name the version, say alpha, list each known issue and state the not-analyzed sentence.</summary>
	[Fact]
	public void TheReleaseNotesNameTheVersionListTheKnownIssuesAndStateTheNotAnalyzedSentence()
	{
		string notes = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_RELEASE_NOTES);

		Assert.Contains(ToolVersion.CURRENT, notes, StringComparison.Ordinal);
		Assert.Contains(_ALPHA_WORD, notes, StringComparison.Ordinal);
		Assert.All(_knownIssuePhrases, phrase => Assert.Contains(phrase, notes, StringComparison.Ordinal));
		Assert.Contains(_NOT_ANALYZED_SENTENCE, notes, StringComparison.Ordinal);
		Assert.Contains(_EMPTY_RESULT_SENTENCE, notes, StringComparison.Ordinal);
	}

	/// <summary>Row 4 gives no date for the removal of Basic, states the header rule with its 404 consequence, and cites public pages.</summary>
	[Fact]
	public void RowFourGivesNoRemovalDateAndStatesTheCsrfHeaderRule()
	{
		string essentials = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_OAUTH2_ESSENTIALS);

		Assert.DoesNotMatch(_YEAR_PATTERN, essentials);
		Assert.Contains(_CSRF_RULE, essentials, StringComparison.Ordinal);
		Assert.Contains(_CSRF_CONSEQUENCE, essentials, StringComparison.Ordinal);
		Assert.Contains("There is no date for the removal of Basic Authentication", essentials, StringComparison.Ordinal);
		Assert.Contains("Implicit", essentials, StringComparison.Ordinal);
		Assert.Contains("Resource Owner", essentials, StringComparison.Ordinal);
	}

	/// <summary>Row 3 states the Default expectation, the two-files statement naming NTFS and macOS, the not-analyzed sentence and the caps.</summary>
	[Fact]
	public void TheLimitsRowStatesTheDefaultTheTwoFilesTheNotAnalyzedSentenceAndTheCaps()
	{
		string limits = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_LIMITS);

		Assert.Contains(_DEFAULT_EXPECTED_SENTENCE, limits, StringComparison.Ordinal);
		Assert.Contains(_TWO_FILES_SENTENCE, limits, StringComparison.Ordinal);
		Assert.Contains(_NTFS_NAME, limits, StringComparison.Ordinal);
		Assert.Contains(_MACOS_NAME, limits, StringComparison.Ordinal);
		Assert.Contains(_EMPTY_RESULT_SENTENCE, limits, StringComparison.Ordinal);
		Assert.Contains(_CAPS_SENTENCE, limits, StringComparison.Ordinal);
		Assert.Contains(_FILE_CAP_TEXT, limits, StringComparison.Ordinal);
		Assert.Contains(_SCAN_CAP_TEXT, limits, StringComparison.Ordinal);
	}

	/// <summary>
	/// The JavaScript bullet of row 3 names two declared entries: a bare member name in a credential object, and a header value built inside the project's own function.
	/// Each phrase the bullet quotes is the text of exactly one declared entry of the analyzer, so an entry the bullet names and the analyzer does not declare fails here.
	/// </summary>
	[Fact]
	public void TheJavaScriptBulletOfTheLimitsRowNamesDeclaredEntriesWithTheirOwnText()
	{
		string limits = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_LIMITS);
		ImmutableArray<DeclaredUnsupportedCoverage> declared = [.. JavaScriptRegisteredExpressions.Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage];

		Assert.Contains(_JAVASCRIPT_BARE_MEMBER_TEXT, limits, StringComparison.Ordinal);
		Assert.Contains(_JAVASCRIPT_FUNCTION_HEADER_TEXT, limits, StringComparison.Ordinal);
		Assert.Single(declared, entry => entry.Construct.Contains(_JAVASCRIPT_BARE_MEMBER_TEXT, StringComparison.Ordinal));
		Assert.Single(declared, entry => entry.Construct.Contains(_JAVASCRIPT_FUNCTION_HEADER_TEXT, StringComparison.Ordinal));
	}

	/// <summary>
	/// Each form the JavaScript bullet of row 3 describes is a sample that no class reads at High or Medium: a bare member name in a credential object that no rule reads in that
	/// spelling, a literal ending in a colon followed by code in an encoder, and a header value built inside the project's own function.
	/// </summary>
	[Theory]
	[InlineData("const options = { auth: { username, password } };\n")]
	[InlineData("const response = await got.get(url, { auth: { username, password } });\n")]
	[InlineData("const header = 'Basic ' + btoa('exampleUser:' + secret);\n")]
	[InlineData("const SCHEME = 'Basic';\nfunction authHeader(token) {\n\treturn `${SCHEME} ${token}`;\n}\nawait fetch(url, { headers: { Authorization: authHeader(token) } });\n")]
	public void TheFormsTheJavaScriptBulletDescribesAreNotReadAtHighOrMedium(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.Level is ConfidenceLevel.High or ConfidenceLevel.Medium);
	}

	/// <summary>Row 8 carries both Relativity links.</summary>
	[Fact]
	public void RowEightCarriesBothRelativityLinks()
	{
		string moreInformation = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_MORE_INFORMATION);

		Assert.Contains(_OAUTH2_CLIENTS_URL, moreInformation, StringComparison.Ordinal);
		Assert.Contains(_REST_AUTHENTICATION_URL, moreInformation, StringComparison.Ordinal);
	}

	/// <summary>Row 7 describes each flow's expiry handling: re-acquire with the secret for Client Credentials, refresh or sign in again for Authorization Code.</summary>
	[Fact]
	public void RowSevenDescribesTheExpiryHandlingOfEachFlow()
	{
		string expiry = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_TOKEN_EXPIRY);

		Assert.Contains("does not offer refresh tokens", expiry, StringComparison.Ordinal);
		Assert.Contains("request a new one", expiry, StringComparison.Ordinal);
		Assert.Contains("grant_type=refresh_token", expiry, StringComparison.Ordinal);
		Assert.Contains("the user signs in again", expiry, StringComparison.Ordinal);
	}

	/// <summary>The flow-notes block of row 1 equals a fresh composition from the tool's own notes; the failure names the block.</summary>
	[Fact]
	public void TheFlowNotesBlockEqualsTheToolsOwnNotes()
	{
		(string committed, string fresh) = UserGuideBlockComposition.CommittedAndFresh(PublicDocuments.UserGuideRowPath(PublicDocuments.ROW_USING_THE_TOOL));

		Assert.Empty(GeneratedBlockMarkers.BalanceProblems(committed));
		Assert.Single(GeneratedBlockMarkers.Blocks(committed), block => block.Key == UserGuideBlockComposition.FLOW_NOTES_KEY);
		Assert.True(fresh == committed, $"The flow-notes block of row 1 differs from the tool's notes. {UserGuideBlockComposition.REGENERATE_HINT}");
	}

	private static string SectionOfRow(int row, string heading)
	{
		string? section = PublicDocumentChecks.SectionOf(PublicDocuments.ReadUserGuideRow(row), heading);

		Assert.True(section is not null, $"Row {row} has no section headed '{heading}'.");

		return section!;
	}

	private static string[] CommandNamesPrintedByHelp()
	{
		StringWriter writer = new();

		HelpRenderer.Render(writer);

		string[] commandLines =
		[
			.. writer.ToString().Split(_LINE_BREAK)
				.Select(line => line.TrimEnd(_CARRIAGE_RETURN))
				.Where(line => line.StartsWith(_HELP_COMMAND_LINE_INDENT, StringComparison.Ordinal))
		];

		return
		[
			.. commandLines.SelectMany(line => line.Trim().Split(_HELP_NAME_TERMINATOR)[0].Split(_ALTERNATE_NAME_SEPARATOR))
		];
	}

	private static ImmutableArray<string[]> TableRows(string section)
	{
		return
		[
			.. section.Split(_LINE_BREAK)
				.Where(line => line.StartsWith(_TABLE_ROW_PREFIX, StringComparison.Ordinal))
				.Select(line => line.Trim().Trim(_TABLE_CELL_SEPARATOR).Split(_TABLE_CELL_SEPARATOR).Select(cell => cell.Trim()).ToArray())
				.Where(cells => cells[0].Length > 0 && (cells[0].StartsWith(_CODE_SPAN_MARKER) || char.IsDigit(cells[0][0])))
		];
	}

	private static string[] FirstColumnCodeNames(string section)
	{
		return [.. TableRows(section).SelectMany(cells => CodeSpans(cells[0]))];
	}

	private static IEnumerable<string> CodeSpans(string text)
	{
		return Regex.Matches(text, _CODE_SPAN_PATTERN).Select(match => match.Groups[1].Value);
	}

	private static HashSet<string> CodeSpanTokens(string text)
	{
		return [.. CodeSpans(text).SelectMany(span => Regex.Split(span, _TOKEN_SPLIT_PATTERN)).Where(token => token.Length > 0)];
	}

	private static IEnumerable<string> SchemaFieldNames(Type documentType)
	{
		return documentType.GetProperties(BindingFlags.Public | BindingFlags.Instance).SelectMany(property =>
		{
			Type valueType = property.PropertyType.IsArray ? property.PropertyType.GetElementType()! : property.PropertyType;
			IEnumerable<string> nested = valueType.Namespace == documentType.Namespace ? SchemaFieldNames(valueType) : [];

			return nested.Prepend(JsonNamingPolicy.CamelCase.ConvertName(property.Name));
		});
	}

}
