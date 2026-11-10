using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Catalog;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// WHAT THE GENERATED BLOCKS OF THE USER GUIDE SAY, COMPOSED BY THE SAME
/// <see cref="RecommendationComposer"/> AND THE SAME <see cref="FlowNoteKeys"/> THE TOOL REPORTS FROM.
///
/// Two kinds of block sit in the guide, delimited by the same markers Part C uses
/// (<see cref="GeneratedBlockMarkers"/>). The <c>oauth2-client {flow slug}</c> blocks of row 5 hold the
/// OAuth2 Client field list of a flow, taken from the composed recommendation of a class the analyzer
/// declares no template for, so the block is the engine-owned one and no language's text is mixed in. The
/// <c>flow-notes</c> block of row 1 holds the four flow notes with the keys the suggest console prints.
/// Everything outside a marker pair is hand-written and is never touched.
///
/// THE DEFAULT RUN NEVER WRITES. Setting the variable named by <see cref="REGENERATE_ENVIRONMENT_VARIABLE"/>
/// to <c>1</c> makes a guide test write the regenerated file first (UTF-8 without a byte-order mark, LF
/// line endings) and then compare.
/// </summary>
internal static class UserGuideBlockComposition
{
	public const string FLOW_NOTES_KEY = "flow-notes";
	public const string OAUTH2_CLIENT_KEY_PREFIX = "oauth2-client";
	public const string REGENERATE_ENVIRONMENT_VARIABLE = "MIGRATE_AUTH_REGENERATE_USER_GUIDE";
	public const string REGENERATE_HINT = "Regenerate the blocks by running the test with the environment variable MIGRATE_AUTH_REGENERATE_USER_GUIDE=1.";

	private const string _REGENERATE_ENABLED_VALUE = "1";
	private const char _KEY_SEPARATOR = ' ';
	private const string _LINE_BREAK = "\n";
	private const string _NOTES_TABLE_HEADER = "| Key | When the tool shows it | Note |";
	private const string _NOTES_TABLE_RULE = "| --- | --- | --- |";
	private const string _NOTES_ROW_FORMAT = "| {0} | {1} | {2} |";
	private const string _DEFAULT_CLIENT_CREDENTIALS_CONDITION = "No signal in the file decides, and the default flow is Client Credentials (`flowDecision` is `Default`, `flow` is `ClientCredentials`).";
	private const string _DEFAULT_AUTHORIZATION_CODE_CONDITION = "No signal in the file decides, and your user profile file makes Authorization Code the default (`flowDecision` is `Default`, `flow` is `AuthorizationCode`).";
	private const string _FIXED_SIGNAL_CONDITION = "A fixed-credential signal in the file (`flowDecision` is `FixedSignal`, `flow` is `ClientCredentials`).";
	private const string _INTERACTIVE_SIGNAL_CONDITION = "An interactive-user signal in the file (`flowDecision` is `InteractiveSignal`, `flow` is `AuthorizationCode`).";

	/// <summary>The key of the block that holds the OAuth2 Client field list of a flow.</summary>
	public static string OAuth2ClientKey(CredentialFlow flow)
	{
		return string.Concat(OAUTH2_CLIENT_KEY_PREFIX, _KEY_SEPARATOR, PartCBlockComposition.SlugOf(flow));
	}

	/// <summary>The OAuth2 Client field list of a flow as the composer produces it: the piece every composed recommendation of that flow carries.</summary>
	public static string OAuth2ClientBlockOf(CredentialFlow flow)
	{
		return RecommendationComposer.Compose(
			CSharpRenderedTexts.Catalog(),
			AssessedLanguage.CSharp,
			CSharpRenderedTexts.UNDECLARED_PATTERN_CLASS,
			flow).OAuth2ClientBlock;
	}

	/// <summary>The whole content of the block with this key.</summary>
	public static string ContentFor(string key)
	{
		string content = key == FLOW_NOTES_KEY
			? FlowNotesTable()
			: OAuth2ClientBlockOf(FlowOfKey(key));

		return content;
	}

	/// <summary>
	/// The file with every block rewritten from the tool's own text. When the regenerate variable is set
	/// and the file differs, the fresh text is written first, so the run that regenerates passes and the
	/// next run proves the file on disk is what the tool says.
	/// </summary>
	public static (string Committed, string Fresh) CommittedAndFresh(string path)
	{
		string committed = File.ReadAllText(path);
		string fresh = GeneratedBlockMarkers.Rewrite(committed, ContentFor);

		if (Environment.GetEnvironmentVariable(REGENERATE_ENVIRONMENT_VARIABLE) == _REGENERATE_ENABLED_VALUE && fresh != committed)
		{
			File.WriteAllText(path, fresh, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			committed = fresh;
		}

		return (committed, fresh);
	}

	private static CredentialFlow FlowOfKey(string key)
	{
		string slug = key.Split(_KEY_SEPARATOR)[1];

		return slug == PartCBlockComposition.CLIENT_CREDENTIALS_SLUG ? CredentialFlow.ClientCredentials : CredentialFlow.AuthorizationCode;
	}

	private static string FlowNotesTable()
	{
		string[] rows =
		[
			NotesRow(FlowDecision.Default, CredentialFlow.ClientCredentials, _DEFAULT_CLIENT_CREDENTIALS_CONDITION),
			NotesRow(FlowDecision.Default, CredentialFlow.AuthorizationCode, _DEFAULT_AUTHORIZATION_CODE_CONDITION),
			NotesRow(FlowDecision.FixedSignal, CredentialFlow.ClientCredentials, _FIXED_SIGNAL_CONDITION),
			NotesRow(FlowDecision.InteractiveSignal, CredentialFlow.AuthorizationCode, _INTERACTIVE_SIGNAL_CONDITION)
		];

		return string.Join(_LINE_BREAK, [_NOTES_TABLE_HEADER, _NOTES_TABLE_RULE, .. rows]);
	}

	private static string NotesRow(FlowDecision decision, CredentialFlow flow, string condition)
	{
		return string.Format(
			CultureInfo.InvariantCulture,
			_NOTES_ROW_FORMAT,
			FlowNoteKeys.KeyFor(decision, flow),
			condition,
			FlowNotes.NoteFor(decision, flow));
	}
}
