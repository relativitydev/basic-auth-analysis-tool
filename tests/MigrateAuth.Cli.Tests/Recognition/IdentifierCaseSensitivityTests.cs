using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// <see cref="LexicalProfile.IdentifierCaseSensitivity"/> selects the comparer of
/// <see cref="FileSymbolTable"/> and nothing else. Scope-prefix and brace normalisation are not the engine's.
/// </summary>
public sealed class IdentifierCaseSensitivityTests
{
	private const string _PROFILE_NAME = "IdentifierCaseTestLocal";
	private const string _DECLARATION_PATTERN = @"^\s*const\s+(?<name>\$?[A-Za-z_][A-Za-z0-9_]*)\s*=\s*""(?<value>[^""\r\n]*)""";
	private const string _UPPER_FIRST_NAME = "$Token";
	private const string _LOWER_FIRST_NAME = "$token";
	private const string _DECLARED_VALUE = "Basic";
	private const string _SECOND_DECLARED_VALUE = "Digest";
	private const int _AFTER_EVERY_DECLARATION = int.MaxValue;

	private const string _ONE_DECLARATION = """
		const $Token = "Basic"
		""";

	private const string _TWO_DECLARATIONS_DIFFERING_ONLY_IN_CASE = """
		const $Token = "Basic"
		const $token = "Digest"
		""";

	private const string _CSHARP_TWO_NAMES_DIFFERING_ONLY_IN_CASE = """
		class A
		{
			const string Token = "Basic";
			const string token = "Digest";
		}
		""";

	[Fact]
	public void TheDefaultIsSensitive()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = BuildDeclarationExpression(),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = []
		};

		Assert.Equal(IdentifierCaseSensitivityMode.Sensitive, profile.IdentifierCaseSensitivity);
	}

	[Fact]
	public void InsensitiveResolvesTheDifferentlyCasedSpellingToTheOneDeclaration()
	{
		FileSymbolTable symbols = Collect(BuildProfile(IdentifierCaseSensitivityMode.Insensitive), _ONE_DECLARATION);

		Assert.Equal(1, symbols.DeclarationCountOf(_UPPER_FIRST_NAME));
		Assert.Equal(1, symbols.DeclarationCountOf(_LOWER_FIRST_NAME));
		Assert.True(symbols.TryResolveUnambiguously(_LOWER_FIRST_NAME, _AFTER_EVERY_DECLARATION, out string declaredValue));
		Assert.Equal(_DECLARED_VALUE, declaredValue);
	}

	[Fact]
	public void InsensitiveCountsTwoSpellingsOfOneNameAsTwoDeclarationsAndIsIndeterminate()
	{
		FileSymbolTable symbols = Collect(
			BuildProfile(IdentifierCaseSensitivityMode.Insensitive),
			_TWO_DECLARATIONS_DIFFERING_ONLY_IN_CASE);

		Assert.Equal(2, symbols.DeclarationCountOf(_UPPER_FIRST_NAME));
		Assert.False(symbols.TryResolveUnambiguously(_LOWER_FIRST_NAME, _AFTER_EVERY_DECLARATION, out _));
	}

	[Fact]
	public void SensitiveKeepsTheTwoSpellingsApart()
	{
		FileSymbolTable symbols = Collect(
			BuildProfile(IdentifierCaseSensitivityMode.Sensitive),
			_TWO_DECLARATIONS_DIFFERING_ONLY_IN_CASE);

		Assert.Equal(1, symbols.DeclarationCountOf(_UPPER_FIRST_NAME));
		Assert.Equal(1, symbols.DeclarationCountOf(_LOWER_FIRST_NAME));
		Assert.True(symbols.TryResolveUnambiguously(_UPPER_FIRST_NAME, _AFTER_EVERY_DECLARATION, out string upperValue));
		Assert.True(symbols.TryResolveUnambiguously(_LOWER_FIRST_NAME, _AFTER_EVERY_DECLARATION, out string lowerValue));
		Assert.Equal(_DECLARED_VALUE, upperValue);
		Assert.Equal(_SECOND_DECLARED_VALUE, lowerValue);
	}

	[Fact]
	public void CSharpDeclaresSensitiveAndItsCaseDistinctIdentifiersStayDistinct()
	{
		CSharpRecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(_CSHARP_TWO_NAMES_DIFFERING_ONLY_IN_CASE);
		FileSymbolTable symbols = FileSymbolTable.Collect(context.Analyzer.LexicalProfile, context.NormalizedText, context.Mask);

		Assert.Equal(IdentifierCaseSensitivityMode.Sensitive, context.Analyzer.LexicalProfile.IdentifierCaseSensitivity);
		Assert.Equal(1, symbols.DeclarationCountOf("Token"));
		Assert.Equal(1, symbols.DeclarationCountOf("token"));
	}

	private static Regex BuildDeclarationExpression()
	{
		return new Regex(
			_DECLARATION_PATTERN,
			RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
			RecognitionEnginePolicy.MATCH_BOUND);
	}

	private static LexicalProfile BuildProfile(IdentifierCaseSensitivityMode identifierCaseSensitivity)
	{
		return new LexicalProfile
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = BuildDeclarationExpression(),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = [],
			IdentifierCaseSensitivity = identifierCaseSensitivity
		};
	}

	private static FileSymbolTable Collect(LexicalProfile profile, string sourceText)
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(sourceText);

		return FileSymbolTable.Collect(profile, context.NormalizedText, context.Mask);
	}
}
