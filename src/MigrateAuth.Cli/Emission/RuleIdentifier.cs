using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Emission;

/// <summary>
/// The pattern class identifier a finding is emitted under - <c>CS-001</c>, <c>JS-004</c> - as
/// a type rather than as a string.
///
/// The shape is enforced on construction and that is the point of the type existing. The
/// catalog's identifiers are two to six upper-case ASCII letters, a hyphen and three digits,
/// and NO LINE OF SOURCE CODE CAN SATISFY THAT. A credential, a matched line, a decoded header
/// - none of them is representable here, so this parameter of the emission point cannot become
/// the route by which file text leaves the process, no matter what a later call site passes.
///
/// It is the identifier only, and never the rule's expression, its pattern or any text it
/// matched: the identifier names a class in a catalog embedded in the binary, so it carries no
/// information about the repository at all.
/// </summary>
public readonly partial record struct RuleIdentifier
{
	private RuleIdentifier(string value)
	{
		Value = value;
	}

	/// <summary>The identifier. Empty on <c>default</c>, which no emission accepts.</summary>
	public string Value { get; }

	/// <summary>Whether this instance carries an identifier at all.</summary>
	public bool IsDeclared => !string.IsNullOrEmpty(Value);

	/// <summary>
	/// Accepts a catalog pattern class identifier, or refuses. There is no constructor that
	/// takes arbitrary text, and this is the only way in.
	/// </summary>
	public static bool TryFromPatternClass(string patternClassIdentifier, out RuleIdentifier ruleIdentifier)
	{
		bool accepted =
			!string.IsNullOrEmpty(patternClassIdentifier)
			&& CatalogIdentifierShape().IsMatch(patternClassIdentifier);

		ruleIdentifier = accepted ? new RuleIdentifier(patternClassIdentifier) : default;

		return accepted;
	}

	/// <summary>
	/// Generated at build time rather than constructed at run time, as every other expression in
	/// this product is: under ahead-of-time compilation a runtime-constructed expression is
	/// interpreted.
	/// </summary>
	[GeneratedRegex(@"^[A-Z]{2,6}-[0-9]{3}$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
	private static partial Regex CatalogIdentifierShape();
}
