using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// OPTIONAL, on <see cref="CandidateSelectionAndRecommendation"/>. How the engine follows a
/// same-file wrapper: a function whose body builds a credential from its own parameters. A construct that
/// the rules recognized INSIDE such a body and that uses one of the function's parameters is not evidence
/// at the definition; it is evidence at each call of the function that passes a credential-bearing argument
/// at a parameter the construct uses. The engine alone evaluates every expression here, inside the file's
/// match bound.
///
/// LIMITS, stated here and not hidden: a wrapper defined in another file is not followed; a call from one
/// wrapper's body into another wrapper is not followed (the argument is a parameter there, which is never
/// credential-bearing); a body that is not closed within <see cref="MaxBodyLines"/> lines is abandoned and its
/// constructs keep the definition-site reading; at most <see cref="MaxCalls"/> calls per file are examined.
///
/// The integers are bounds, never ordinals or positions.
/// </summary>
public sealed record WrapperTracking
{
	/// <summary>The capture group that holds a function's name, in <see cref="FunctionDefinitionExpression"/> and <see cref="CallSiteExpression"/>.</summary>
	public const string NAME_GROUP_NAME = "name";

	/// <summary>The capture group, in <see cref="FunctionDefinitionExpression"/>, whose start is the offset of the body's opening brace.</summary>
	public const string OPEN_GROUP_NAME = "open";

	/// <summary>The capture group that holds a parameter's index: a digit, or <c>@</c> or <c>*</c>.</summary>
	public const string INDEX_GROUP_NAME = "index";

	/// <summary>The capture group, in <see cref="ParameterAliasExpression"/>, that holds the alias variable's name.</summary>
	public const string ALIAS_GROUP_NAME = "alias";

	/// <summary>The capture group, in <see cref="CallSiteExpression"/>, that holds the call's argument text.</summary>
	public const string ARGUMENTS_GROUP_NAME = "args";

	/// <summary>The default for <see cref="MaxBodyLines"/>.</summary>
	public const int DEFAULT_MAX_BODY_LINES = 200;

	/// <summary>The default for <see cref="MaxCalls"/>.</summary>
	public const int DEFAULT_MAX_CALLS = 200;

	/// <summary>The engine's ceiling for <see cref="MaxBodyLines"/>.</summary>
	public const int MAXIMUM_BODY_LINES = 2000;

	/// <summary>The engine's ceiling for <see cref="MaxCalls"/>.</summary>
	public const int MAXIMUM_CALLS = 2000;

	/// <summary>A function definition. Groups <see cref="NAME_GROUP_NAME"/> and <see cref="OPEN_GROUP_NAME"/>.</summary>
	public required Regex FunctionDefinitionExpression { get; init; }

	/// <summary>A positional parameter reference inside a body. Group <see cref="INDEX_GROUP_NAME"/>.</summary>
	public required Regex PositionalReferenceExpression { get; init; }

	/// <summary>
	/// A variable that stands for a positional parameter inside a body (a nameref or a plain copy). Groups
	/// <see cref="ALIAS_GROUP_NAME"/> and <see cref="INDEX_GROUP_NAME"/>.
	/// </summary>
	public required Regex ParameterAliasExpression { get; init; }

	/// <summary>A simple command whose first word is a function's name. Groups <see cref="NAME_GROUP_NAME"/> and <see cref="ARGUMENTS_GROUP_NAME"/>.</summary>
	public required Regex CallSiteExpression { get; init; }

	/// <summary>One match per shell-word argument of the call's argument text.</summary>
	public required Regex ArgumentExpression { get; init; }

	/// <summary>
	/// Tested on one argument word's text. A word that is a pure variable reference is first resolved through
	/// the file's symbol table, one hop, and the VALUE is tested; an unresolved reference is not credential-bearing.
	/// </summary>
	public required Regex CredentialBearingArgument { get; init; }

	/// <summary>How many line feeds a body may hold before it is abandoned: a closing brace after exactly this many is accepted, one that needs more is not.</summary>
	public int MaxBodyLines { get; init; } = DEFAULT_MAX_BODY_LINES;

	/// <summary>How many calls of wrappers are examined per file.</summary>
	public int MaxCalls { get; init; } = DEFAULT_MAX_CALLS;
}
