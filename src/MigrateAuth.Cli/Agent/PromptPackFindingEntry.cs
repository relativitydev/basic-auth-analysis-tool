using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// ONE FINDING AS IT APPEARS IN THE PROMPT PACK - and the reason this type exists is its
/// CONSTRUCTOR, not its properties.
///
/// The pack's content rule: "The pack is built EXCLUSIVELY from the finding record - class, relative
/// path, line, level - plus static template text. Repository fragments are explicitly forbidden, because
/// no repository content may leave the process." The four fields below are that list, taken with the same
/// types <see cref="MigrateAuth.Cli.Findings.Finding"/> carries them in; the fifth parameter is
/// a case rather than a text; the last three are closed values of the finding record as well (the index
/// that renders the identifier, the flow, and whether the flow was defaulted). None of them can hold a
/// fragment of the analyzed file. Putting one into a pack entry would require changing this signature,
/// which is a thing a reviewer sees.
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
/// repository text, a comment in the analyzed code - or in a vendored dependency, which the confidence rules
/// demote but does not exclude from reading - would reach an agent that way, and the no-network
/// guarantee would be honoured to the letter and evaded entirely.
///
/// IT IS AN ENTRY AND NOT A PACK. Assembling entries, writing the file and launching anything happen
/// elsewhere.
/// </summary>
public sealed record PromptPackFindingEntry
{
	/// <summary>The first line a file has, below which a line number is not one.</summary>
	public const int FIRST_LINE_NUMBER = 1;

	/// <summary>
	/// Builds one entry from the finding-record fields the rule names plus one static text,
	/// selected by route and never supplied.
	///
	/// Invariants: a High or Medium finding has a flow; a Low finding has none and is never marked as
	/// defaulted. The identifier is rendered from the sequential index by <see cref="FindingIdentifiers.Of"/>
	/// so it is the one the report shows.
	/// </summary>
	public PromptPackFindingEntry(
		RuleIdentifier patternClass,
		RepositoryRelativePath path,
		int line,
		ConfidenceLevel level,
		RecommendationKind guidance,
		int sequentialIndex,
		CredentialFlow? flow,
		bool flowWasDefaulted)
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
		ArgumentOutOfRangeException.ThrowIfLessThan(sequentialIndex, FindingIdentifiers.FIRST_INDEX);
		RequireFlowConsistentWithLevel(level, flow, flowWasDefaulted);

		PatternClass = patternClass;
		Path = path;
		Line = line;
		Level = level;
		Guidance = guidance;
		InstructionText = PromptPackInstructionText.Describe(guidance);
		SequentialIndex = sequentialIndex;
		Identifier = FindingIdentifiers.Of(sequentialIndex);
		Flow = flow;
		FlowWasDefaulted = flowWasDefaulted;
	}

	/// <summary>The catalog pattern class, which is also the index into the template catalog.</summary>
	public RuleIdentifier PatternClass { get; }

	/// <summary>Where the construct sits, relative to the repository root. The only path form permitted.</summary>
	public RepositoryRelativePath Path { get; }

	/// <summary>The one-based line of the construct's anchor.</summary>
	public int Line { get; }

	/// <summary>The composed confidence level, carried unchanged and never recomposed here.</summary>
	public ConfidenceLevel Level { get; }

	/// <summary>Which of the catalog's three routes this finding took, which selects the text below.</summary>
	public RecommendationKind Guidance { get; }

	/// <summary>
	/// The static text for that route. It is resolved here rather than stored from a parameter, so
	/// that the value is a compile-time constant of this assembly in every instance that exists.
	/// </summary>
	public string InstructionText { get; }

	/// <summary>The finding's one-based position in the full ordered set, from which the identifier is rendered.</summary>
	public int SequentialIndex { get; }

	/// <summary>The identifier the report shows for this finding, rendered by <see cref="FindingIdentifiers.Of"/>.</summary>
	public string Identifier { get; }

	/// <summary>The OAuth 2.0 flow the finding carries; null exactly for a Low finding.</summary>
	public CredentialFlow? Flow { get; }

	/// <summary>Whether no signal in the code decided the flow and the default was applied. Never true for Low.</summary>
	public bool FlowWasDefaulted { get; }

	private static void RequireFlowConsistentWithLevel(ConfidenceLevel level, CredentialFlow? flow, bool flowWasDefaulted)
	{
		if (level == ConfidenceLevel.Low)
		{
			if (flow is not null || flowWasDefaulted)
			{
				throw new ArgumentException("A Low-confidence entry carries no flow and no default-flow mark.", nameof(flow));
			}
		}
		else if (flow is null)
		{
			throw new ArgumentException("A High or Medium entry carries the flow.", nameof(flow));
		}
	}
}
