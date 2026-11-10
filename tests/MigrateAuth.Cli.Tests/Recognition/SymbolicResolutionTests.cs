using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE FOUR-ROW TABLE, EXERCISED ROW BY ROW, AND THEN WHAT EACH VERDICT DOES TO A FINDING.
///
/// The table is arithmetic over declarations and not a scope model, so every row here is a
/// statement about COUNTING and POSITION and nothing else: how many declarations of the name the
/// file holds, and whether the one it holds comes before the use. The tests assert against
/// <see cref="FileSymbolTable"/> directly for the verdict and against the engine for what the
/// verdict costs, because the two are different claims - a resolved-and-matching verdict and an
/// indeterminate one both land at Medium under the reference analyzer's rule, so a level assertion
/// alone would pass whichever verdict the engine reached.
/// </summary>
public sealed class SymbolicResolutionTests
{
	private const string _HAND_ASSEMBLED_HEADER_PATTERN_CLASS = "EXM-002";
	private const string _SCHEME_CONSTANT_NAME = "AUTH_SCHEME";
	private const string _EXPECTED_SCHEME_TOKEN = "Basic";
	private const string _OTHER_SCHEME_TOKEN = "Digest";

	private const int _BEFORE_EVERY_DECLARATION = 0;
	private const int _AFTER_EVERY_DECLARATION = int.MaxValue;

	/// <summary>The use, with no declaration of its scheme constant anywhere in the file.</summary>
	private const string _USE_WITHOUT_DECLARATION = """
		let request = SetHeader("Authorization", BuildScheme(AUTH_SCHEME))
		""";

	private const string _ONE_MATCHING_DECLARATION_BEFORE_THE_USE = """
		const AUTH_SCHEME = "Basic"
		let request = SetHeader("Authorization", BuildScheme(AUTH_SCHEME))
		""";

	private const string _ONE_NON_MATCHING_DECLARATION_BEFORE_THE_USE = """
		const AUTH_SCHEME = "Digest"
		let request = SetHeader("Authorization", BuildScheme(AUTH_SCHEME))
		""";

	private const string _TWO_DECLARATIONS = """
		const AUTH_SCHEME = "Basic"
		const AUTH_SCHEME = "Digest"
		let request = SetHeader("Authorization", BuildScheme(AUTH_SCHEME))
		""";

	private const string _THE_ONLY_DECLARATION_FOLLOWS_THE_USE = """
		let request = SetHeader("Authorization", BuildScheme(AUTH_SCHEME))
		const AUTH_SCHEME = "Basic"
		""";

	/// <summary>
	/// A declaration written inside a comment. It is not a declaration: the table counts
	/// declarations in CODE zones, and counting this one would let a commented-out line decide a
	/// live finding's level.
	/// </summary>
	private const string _THE_ONLY_DECLARATION_IS_COMMENTED_OUT = """
		-- const AUTH_SCHEME = "Basic"
		let request = SetHeader("Authorization", BuildScheme(AUTH_SCHEME))
		""";

	[Fact]
	public void ExactlyOneDeclarationBeforeTheUseResolvesToTheValueItDeclares()
	{
		FileSymbolTable symbols = Collect(_ONE_MATCHING_DECLARATION_BEFORE_THE_USE);

		Assert.Equal(1, symbols.DeclarationCountOf(_SCHEME_CONSTANT_NAME));
		Assert.True(symbols.TryResolveUnambiguously(
			_SCHEME_CONSTANT_NAME,
			_AFTER_EVERY_DECLARATION,
			out string declaredValue));
		Assert.Equal(_EXPECTED_SCHEME_TOKEN, declaredValue);
	}

	/// <summary>
	/// The same single declaration, resolved in the other direction. The counting rule does not
	/// care what the value says - the comparison against the expected token is the engine's, and it
	/// happens after resolution rather than inside it.
	/// </summary>
	[Fact]
	public void ExactlyOneDeclarationBeforeTheUseResolvesEvenWhereItDoesNotMatch()
	{
		FileSymbolTable symbols = Collect(_ONE_NON_MATCHING_DECLARATION_BEFORE_THE_USE);

		Assert.True(symbols.TryResolveUnambiguously(
			_SCHEME_CONSTANT_NAME,
			_AFTER_EVERY_DECLARATION,
			out string declaredValue));
		Assert.Equal(_OTHER_SCHEME_TOKEN, declaredValue);
	}

	/// <summary>
	/// More than one declaration, anywhere, in any scope. The file is not consulted for which one
	/// is nearer, which is the whole of the refusal to build a scope model it cannot have.
	/// </summary>
	[Fact]
	public void MoreThanOneDeclarationOfTheNameIsIndeterminate()
	{
		FileSymbolTable symbols = Collect(_TWO_DECLARATIONS);

		Assert.Equal(2, symbols.DeclarationCountOf(_SCHEME_CONSTANT_NAME));
		Assert.False(symbols.TryResolveUnambiguously(_SCHEME_CONSTANT_NAME, _AFTER_EVERY_DECLARATION, out _));
	}

	[Fact]
	public void TheOnlyDeclarationFollowingTheUseIsIndeterminate()
	{
		FileSymbolTable symbols = Collect(_THE_ONLY_DECLARATION_FOLLOWS_THE_USE);

		Assert.Equal(1, symbols.DeclarationCountOf(_SCHEME_CONSTANT_NAME));
		Assert.False(symbols.TryResolveUnambiguously(_SCHEME_CONSTANT_NAME, _BEFORE_EVERY_DECLARATION, out _));
	}

	[Fact]
	public void NoDeclarationOfTheNameIsIndeterminate()
	{
		FileSymbolTable symbols = Collect(_USE_WITHOUT_DECLARATION);

		Assert.Equal(0, symbols.DeclarationCountOf(_SCHEME_CONSTANT_NAME));
		Assert.False(symbols.TryResolveUnambiguously(_SCHEME_CONSTANT_NAME, _AFTER_EVERY_DECLARATION, out _));
	}

	[Fact]
	public void ADeclarationInsideACommentIsNotADeclaration()
	{
		FileSymbolTable symbols = Collect(_THE_ONLY_DECLARATION_IS_COMMENTED_OUT);

		Assert.Equal(0, symbols.DeclarationCountOf(_SCHEME_CONSTANT_NAME));
	}

	/// <summary>
	/// The demonstrated negative, which is a RECOGNITION outcome and not a classification one.
	/// The reference analyzer reads a resolved-and-not-matching verdict as suppressing the finding,
	/// and what that has to produce is NOTHING - not a Low finding, which would still count as a
	/// false positive and would still be shown to a developer as something to look at.
	/// </summary>
	[Fact]
	public void ADemonstratedNegativeVerdictProducesNoFindingAtAllRatherThanALowOne()
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized =
			harness.Recognize(_ONE_NON_MATCHING_DECLARATION_BEFORE_THE_USE);

		Assert.Empty(recognized);
	}

	/// <summary>
	/// The positive verdict, which confirms the class applies and does NOT promote. Resolving a
	/// scheme argument supplies no evidence about the credential, and High requires that the
	/// credential be establishable - so this class stays at the ceiling its own self-sufficiency
	/// declaration allows.
	/// </summary>
	[Fact]
	public void APositiveVerdictConfirmsTheClassWithoutPromotingIt()
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized =
			harness.Recognize(_ONE_MATCHING_DECLARATION_BEFORE_THE_USE);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_HAND_ASSEMBLED_HEADER_PATTERN_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>
	/// Every indeterminate row of the table, through the engine: each lands at Medium.
	/// </summary>
	[Theory]
	[InlineData(_TWO_DECLARATIONS)]
	[InlineData(_THE_ONLY_DECLARATION_FOLLOWS_THE_USE)]
	[InlineData(_USE_WITHOUT_DECLARATION)]
	[InlineData(_THE_ONLY_DECLARATION_IS_COMMENTED_OUT)]
	public void AnIndeterminateVerdictLandsAtMediumAndNotBelowIt(string sourceText)
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(sourceText);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_HAND_ASSEMBLED_HEADER_PATTERN_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	private static FileSymbolTable Collect(string sourceText)
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(sourceText);

		return FileSymbolTable.Collect(context.Analyzer.LexicalProfile, context.NormalizedText, context.Mask);
	}
}
