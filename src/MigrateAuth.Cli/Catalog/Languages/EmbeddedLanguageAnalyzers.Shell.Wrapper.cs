using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The six expressions of the same-file wrapper function, which the engine evaluates alone: a function definition, a positional reference in a body, the
/// variable that stands for a parameter, a call of a function by its name, one argument word of a call, and the test of an argument for a literal credential.
/// A function whose body builds a credential from its own parameters is evidence at each call that passes a credential-bearing argument, and not at the
/// definition. Every expression carries the policy match timeout and is source-generated.
/// </summary>
internal static partial class ShellPatternExpressions
{
	private const string _FUNCTION_NAME = @"[A-Za-z_][A-Za-z0-9_:-]*";

	private const string _POSITIONAL_INDEX = @"[1-9@*]";

	/// <summary>A positional parameter, braced or not, with the default and error operators a braced one may carry.</summary>
	private const string _POSITIONAL_PARAMETER =
		@"\$(?:\{(?<index>" + _POSITIONAL_INDEX + @")(?::?[-=?+][^}\n]{0,100})?\}|(?<index>" + _POSITIONAL_INDEX + @"))";

	private const string _WRAPPER_START_KEYWORDS = @"(?:(?:then|do|else|elif|if|while|until|time|!)[ \t]+)*";

	private const string _ARGUMENT_WORD =
		@"(?:[^\s\x22\x27\\]|\\[\s\S]|\x22(?:[^\x22\\]|\\[\s\S])*\x22|\x27[^\x27]*\x27)+";

	private const string _LITERAL_PAIR_WORD =
		@"^[\x22\x27]?[A-Za-z_][^\s:$`\\\x27\x22]*:(?!\d{1,5}[\x22\x27]?$)[^\s$`\\\x27\x22/][^\s$`\\\x27\x22]*[\x22\x27]?$";

	private const string _URL_WITH_LITERAL_USERINFO =
		@"https?://[^\s/@:\x27\x22$`\\]+:[^\s/@\x27\x22$`\\]+@";

	private const string _LITERAL_BASIC_HEADER =
		@"(?i:authorization)[ \t]*:[ \t]*(?i:basic)[ \t]+[^\s$`\\\x27\x22]{4,}";

	private const string _LITERAL_BASIC_TOKEN_WORD =
		@"^[\x22\x27]?(?i:basic)[ \t]+[A-Za-z0-9+/=_-]{4,}[\x22\x27]?$";

	/// <summary>A braced expansion with a default or assignment operator whose text is a literal pair: the text can carry a credential, so it is tested as text.</summary>
	private const string _DEFAULTED_EXPANSION_HOLDING_A_PAIR =
		@"\$\{[A-Za-z_][A-Za-z0-9_]*:?[-=][^}$`\\]*[^\s$`\\\x27\x22:}]+:[^\s$`\\\x27\x22}]+\}";

	/// <summary>
	/// A function definition, in either spelling and with the opening brace on the next line: the name, and the opening brace whose position is where the body
	/// starts. A function whose body is a subshell is not one.
	/// </summary>
	[GeneratedRegex(
		@"^[ \t]*(?:function[ \t]+(?<name>" + _FUNCTION_NAME + @")(?:[ \t]*\([ \t]*\))?|(?<name>" + _FUNCTION_NAME + @")[ \t]*\([ \t]*\))[ \t]*(?:\n[ \t]*)?(?<open>\{)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WrapperFunctionDefinition();

	/// <summary>A positional parameter of the function: <c>$1</c> to <c>$9</c>, <c>${1}</c>, <c>"$@"</c> and <c>"$*"</c>, with or without an operator.</summary>
	[GeneratedRegex(
		_POSITIONAL_PARAMETER,
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WrapperPositionalReference();

	/// <summary>
	/// A variable that stands for a parameter: a plain copy of a positional parameter, with or without a declaration keyword, and a name reference that a
	/// <c>local</c>, <c>declare</c>, <c>typeset</c> or <c>nameref</c> declaration makes of one.
	/// </summary>
	[GeneratedRegex(
		@"(?:(?:local|declare|typeset|readonly|export)[ \t]+(?:-[A-Za-z]+[ \t]+)*|nameref[ \t]+)?(?<alias>[A-Za-z_][A-Za-z0-9_]*)=[\x22\x27]?" + _POSITIONAL_PARAMETER + @"[\x22\x27]?",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WrapperParameterAlias();

	/// <summary>A simple command whose first word is a function's name, at the start of a line or after a separator, with the words that follow it as its arguments.</summary>
	[GeneratedRegex(
		@"(?:^|(?<=[;&|(]))[ \t]*" + _WRAPPER_START_KEYWORDS + @"(?<name>" + _FUNCTION_NAME + @")(?<args>(?:[ \t]+(?:[^\n;&|)\x22\x27\\]|\\[^\n]|\x22(?:[^\x22\\\n]|\\[^\n])*\x22|\x27[^\x27\n]*\x27)*)?)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WrapperCallSite();

	/// <summary>One shell word of a call's arguments: adjacent quoted and unquoted parts are one word.</summary>
	[GeneratedRegex(
		_ARGUMENT_WORD,
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WrapperArgumentWord();

	/// <summary>
	/// A word that carries a literal credential: a colon pair that is not a port and not a path, a URL with literal userinfo, a literal Basic header or token,
	/// or a braced expansion whose default text is a pair. A plain whole expansion is never one.
	/// </summary>
	[GeneratedRegex(
		_LITERAL_PAIR_WORD + @"|" + _URL_WITH_LITERAL_USERINFO + @"|" + _LITERAL_BASIC_HEADER + @"|" + _LITERAL_BASIC_TOKEN_WORD + @"|" + _DEFAULTED_EXPANSION_HOLDING_A_PAIR,
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WrapperCredentialBearingArgument();
}

/// <summary>The `Shell` declaration of the same-file wrapper: the six expressions and the engine's default bounds.</summary>
internal static class ShellWrapperDeclarations
{
	internal static WrapperTracking Build()
	{
		return new WrapperTracking
		{
			FunctionDefinitionExpression = ShellPatternExpressions.WrapperFunctionDefinition(),
			PositionalReferenceExpression = ShellPatternExpressions.WrapperPositionalReference(),
			ParameterAliasExpression = ShellPatternExpressions.WrapperParameterAlias(),
			CallSiteExpression = ShellPatternExpressions.WrapperCallSite(),
			ArgumentExpression = ShellPatternExpressions.WrapperArgumentWord(),
			CredentialBearingArgument = ShellPatternExpressions.WrapperCredentialBearingArgument()
		};
	}
}
