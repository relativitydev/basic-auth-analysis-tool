using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// The closed set of texts an entry may carry, and the whole of what "static template text" means
/// in the pack's content rule.
///
/// IT IS A LOOKUP OVER AN ENUMERATION RATHER THAN A STRING PARAMETER, and that is the mechanism
/// rather than a style choice. "Plus static template text" written as a <c>string</c> argument is
/// an opening the size of the file: a caller could pass a matched line through it and every
/// signature would still read as correct. Passing a case instead makes the text unreachable except
/// as one of the three constants compiled into the binary.
///
/// THE THREE CASES ARE THE RECOMMENDATION CATALOG'S THREE ROUTES, reused rather than reinvented.
///
/// EACH TEXT IS SELF-SUFFICIENT. The agent is not given the report, so no text sends it to "the report"
/// for anything; each says what to establish from the code itself. The migration guidance is the
/// catalog's and is not repeated here.
/// </summary>
public static class PromptPackInstructionText
{
	/// <summary>Said when the catalog characterizes the class and the report already states a migration for it.</summary>
	public const string CHARACTERIZED_CONSTRUCT_TEXT =
		"The tool recognized this pattern class and already stated a migration for it in the report. Confirm from the code that the construct is real and that the flow shown is right; do not propose a different migration unless the code clearly requires it.";

	/// <summary>Said for a Low-confidence class, which is routed to a human.</summary>
	public const string MANUAL_REVIEW_TEXT =
		"This construct is reported at Low confidence and carries no migration. Establish from the code whether it authenticates at all before anything is changed.";

	/// <summary>Said when no template covers the class, which is the catalog's generic route.</summary>
	public const string UNCHARACTERIZED_CONSTRUCT_TEXT =
		"No template covers this pattern class, so it has not been characterized. Describe from the code what it does and propose a migration design.";

	/// <summary>Resolves the one text a route carries.</summary>
	public static string Describe(RecommendationKind guidance)
	{
		string instructionText = guidance switch
		{
			RecommendationKind.FromTemplate => CHARACTERIZED_CONSTRUCT_TEXT,
			RecommendationKind.ManualReview => MANUAL_REVIEW_TEXT,
			RecommendationKind.Generic => UNCHARACTERIZED_CONSTRUCT_TEXT,
			_ => throw new ArgumentOutOfRangeException(nameof(guidance))
		};

		return instructionText;
	}
}
