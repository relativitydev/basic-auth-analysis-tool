using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
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
/// THE THREE CASES ARE THE THREE RECOMMENDATION ROUTES, reused rather than reinvented -
/// <see cref="RecommendationKind"/> already divides findings by exactly the question an agent needs
/// answered: has this construct been characterized, is it routed to a human, or does the catalog not
/// cover it. A parallel enumeration here would be a second copy of that division and would give the
/// two somewhere to disagree.
///
/// WHAT THE TEXTS SAY IS AN INSTRUCTION ABOUT HOW FAR TO TRUST THE ENTRY, never migration guidance.
/// The migration guidance is the catalog's and it is rendered into the report;
/// repeating it here would make the pack a second place it is written. Assembling a whole pack from
/// these - a preamble, the findings in order, the file on disk - is the AI-assisted review feature's work.
/// </summary>
public static class PromptPackInstructionText
{
	/// <summary>Said when the catalog characterizes the class and the report carries its migration.</summary>
	public const string CHARACTERIZED_CONSTRUCT_TEXT =
		"The catalog characterizes this pattern class and the report states the migration for it. Apply the migration the report gives for this class rather than inferring a different one from the surrounding code.";

	/// <summary>Said for a Low-confidence class, which is routed to a human.</summary>
	public const string MANUAL_REVIEW_TEXT =
		"This construct is reported at Low confidence and carries no migration template. Establish from the code whether it authenticates at all before changing anything.";

	/// <summary>Said when no template covers the class, which is the generic recommendation route.</summary>
	public const string UNCHARACTERIZED_CONSTRUCT_TEXT =
		"No template covers this pattern class, so the construct has not been characterized. Design the migration from the code rather than applying a transformation from this pack.";

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

/// <summary>
/// ONE FINDING AS IT APPEARS IN THE PROMPT PACK - and the reason this type exists is its
/// CONSTRUCTOR, not its properties.
///
/// The content rule: "The pack is built EXCLUSIVELY from the finding record - class, relative
/// path, line, level - plus static template text. Repository fragments are explicitly forbidden." The four fields below are that list, taken with the same
/// types <see cref="MigrateAuth.Cli.Findings.Finding"/> carries them in, and the fifth parameter is
/// a case rather than a text. There is no sixth. Putting a fragment of the analyzed file into a pack
/// entry would require changing this signature, which is a thing a reviewer sees.
///
/// THE TYPES ARE DOING THE WORK, NOT THE FIELD LIST. <see cref="RuleIdentifier"/> accepts two to six
/// upper-case letters, a hyphen and three digits, which NO LINE OF SOURCE CODE SATISFIES;
/// <see cref="RepositoryRelativePath"/> is obtainable only by handing a path and its repository to
/// the write boundary's containment primitive and having it agree; a line is an integer;
/// <see cref="ConfidenceLevel"/> and <see cref="RecommendationKind"/> are closed enumerations. Even
/// a caller determined to smuggle a matched line through has no parameter to put it in.
///
/// WHY THE PACK IS SINGLED OUT, since <see cref="MigrateAuth.Cli.Findings.Finding"/> already obeys
/// the same invariant. The pack is the only artifact intended to LEAVE THE MACHINE and the only one
/// handed to a process that holds both the repository and its own network. Were it to carry
/// repository text, a comment in the analyzed code - or in a vendored dependency, which the rubric
/// demotes but does not exclude from reading - would reach an agent that way, and the no-network
/// guarantee would be honoured to the letter and evaded entirely.
///
/// IT IS AN ENTRY AND NOT A PACK. Assembling entries, writing the file and launching anything are the AI-assisted review feature's work.
/// </summary>
public sealed record PromptPackFindingEntry
{
	/// <summary>The first line a file has, below which a line number is not one.</summary>
	public const int FIRST_LINE_NUMBER = 1;

	/// <summary>
	/// Builds one entry from the four finding-record fields the content rule names plus one static text,
	/// selected by route and never supplied.
	/// </summary>
	public PromptPackFindingEntry(
		RuleIdentifier patternClass,
		RepositoryRelativePath path,
		int line,
		ConfidenceLevel level,
		RecommendationKind guidance)
	{
		if (!patternClass.IsDeclared)
		{
			throw new ArgumentException("A pack entry needs the catalog pattern class it was recognized under.", nameof(patternClass));
		}

		if (!path.IsDeclared)
		{
			throw new ArgumentException("A pack entry needs the repository-relative path of the file it was found in.", nameof(path));
		}

		ArgumentOutOfRangeException.ThrowIfLessThan(line, FIRST_LINE_NUMBER);

		PatternClass = patternClass;
		Path = path;
		Line = line;
		Level = level;
		Guidance = guidance;
		InstructionText = PromptPackInstructionText.Describe(guidance);
	}

	/// <summary>The catalog pattern class, which is also the index into the template catalog.</summary>
	public RuleIdentifier PatternClass { get; }

	/// <summary>Where the construct sits, relative to the repository root. The only path form permitted.</summary>
	public RepositoryRelativePath Path { get; }

	/// <summary>The one-based line of the construct's anchor.</summary>
	public int Line { get; }

	/// <summary>The composed confidence level, carried unchanged and never recomposed here.</summary>
	public ConfidenceLevel Level { get; }

	/// <summary>Which of the three recommendation routes this finding took, which selects the text below.</summary>
	public RecommendationKind Guidance { get; }

	/// <summary>
	/// The static text for that route. It is resolved here rather than stored from a parameter, so
	/// that the value is a compile-time constant of this assembly in every instance that exists.
	/// </summary>
	public string InstructionText { get; }
}
