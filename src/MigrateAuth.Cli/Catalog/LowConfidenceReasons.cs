namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// WHICH OF THE CLASSIFICATION SIGNALS PROPOSED LOW FOR A FINDING, KEPT SO THAT A READER CAN BE TOLD WHY.
///
/// The level of a finding is the minimum over every proposal, and the minimum forgets its
/// inputs: two findings that both read Low can be Low for unrelated reasons, and only one of them
/// is the kind a person can dismiss quickly. This type records the reasons at the one place that
/// still knows them, beside the level and never as an input to it - nothing reads it to compute a
/// level, so it cannot become a second composition.
///
/// It is a closed vocabulary of this product's own, with no text from the analyzed file in it. It is
/// <see cref="None"/> for every finding whose level is not Low, because a signal that proposes
/// anything above Low is not a reason for one.
/// </summary>
[Flags]
public enum LowConfidenceReasons
{
	/// <summary>No signal proposed Low: the finding is not Low, or its reason was not recorded.</summary>
	None = 0,

	/// <summary>The pattern class's own base level is Low, so every finding of the class is.</summary>
	PatternClass = 1,

	/// <summary>The construct's anchor sits in a comment or a string literal rather than in code.</summary>
	CommentOrStringLiteral = 2,

	/// <summary>A demotion signal of the file - its name, its folder or its content - proposed Low.</summary>
	DemotedFile = 4,

	/// <summary>A demotion signal that names a vendored or third-party folder proposed Low.</summary>
	DemotedVendoredFile = 8
}
