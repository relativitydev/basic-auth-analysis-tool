using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// A CONSTRUCT THAT BUILDS A VARIABLE NOTHING EVER READS IS DROPPED, with no construct and no Low one. The rule
/// declares a <see cref="RuleVariableUse"/>; the variable must be referenced anywhere in the file outside the
/// construct's own span, at a position that is not a comment.
/// </summary>
public sealed class NeverSentConstructTests
{
	private const string _BUILD_LINE = "mkauth into TOK";
	private const string _BUILD_WITHOUT_A_VARIABLE_LINE = "mkauth";
	private const string _SELF_REFERENCING_BUILD_LINE = "mkauthself TOK \"$TOK\"";

	[Fact]
	public void AConstructBuiltIntoAVariableThatIsNeverReferencedProducesNoFinding()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(_BUILD_LINE, "echo done");

		Assert.Empty(constructs);
	}

	[Theory]
	[InlineData("echo \"$TOK\"")]
	[InlineData("echo \"${TOK}\"")]
	[InlineData("echo ${TOK:-none}")]
	public void AConstructWhoseVariableIsReferencedLaterIsStillRead(string use)
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(_BUILD_LINE, use);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(ShellProbeAnalyzer.BUILT_CREDENTIAL_CLASS, construct.PatternClassIdentifier);
	}

	[Fact]
	public void AReferenceBeforeTheConstructCountsAsAUse()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize("echo \"$TOK\"", _BUILD_LINE);

		Assert.Single(constructs);
	}

	[Fact]
	public void AReferenceInsideTheConstructsOwnSpanDoesNotCount()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(_SELF_REFERENCING_BUILD_LINE, "echo done");

		Assert.Empty(constructs);
	}

	[Fact]
	public void AReferenceInsideTheSpanDoesNotCountButOneOutsideItDoes()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(_SELF_REFERENCING_BUILD_LINE, "echo \"$TOK\"");

		Assert.Single(constructs);
	}

	[Fact]
	public void AReferenceInACommentDoesNotCount()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize($"{_BUILD_LINE} # echo \"$TOK\"", "# $TOK");

		Assert.Empty(constructs);
	}

	[Theory]
	[InlineData("echo \"$TOKEN\"")]
	[InlineData("echo \"$TOK_2\"")]
	[InlineData("echo \"${TOKS}\"")]
	public void AReferenceToAVariableWithTheSamePrefixIsNotAReference(string use)
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(_BUILD_LINE, use);

		Assert.Empty(constructs);
	}

	[Fact]
	public void AMatchWhereTheVariableGroupDidNotParticipateIsKept()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(_BUILD_WITHOUT_A_VARIABLE_LINE, "echo done");

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(ShellProbeAnalyzer.BUILT_CREDENTIAL_CLASS, construct.PatternClassIdentifier);
	}

	[Fact]
	public void ARuleThatDeclaresNoVariableUseKeepsAnUnreferencedConstructAsItAlwaysDid()
	{
		ShellProbeAnalyzer withoutTheDeclaration = ShellProbe.AnalyzerWith([], builtCredentialVariableUse: null);

		ImmutableArray<RecognizedConstruct> constructs = ShellProbe
			.Recognize(withoutTheDeclaration, ShellProbe.Source(_BUILD_LINE, "echo done")).Result.Constructs;

		Assert.Single(constructs);
	}

	[Fact]
	public void TheDroppedConstructLeavesTheOtherConstructsOfTheFileUntouched()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(ShellProbe.CLIENT_CALL_LINE, _BUILD_LINE);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(ShellProbeAnalyzer.CLIENT_CALL_CLASS, construct.PatternClassIdentifier);
	}

	[Fact]
	public void TheVariableGroupTheRuleNamesIsTheOneRead()
	{
		RuleVariableUse other = new() { VariableGroupName = "absentgroup" };
		ShellProbeAnalyzer analyzer = ShellProbe.AnalyzerWith([], builtCredentialVariableUse: other);

		ImmutableArray<RecognizedConstruct> constructs = ShellProbe
			.Recognize(analyzer, ShellProbe.Source(_BUILD_LINE, "echo done")).Result.Constructs;

		Assert.Single(constructs);
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(params string[] lines)
	{
		ShellProbeAnalyzer analyzer = ShellProbe.AnalyzerWith([], builtCredentialVariableUse: new RuleVariableUse());

		return ShellProbe.Recognize(analyzer, ShellProbe.Source(lines)).Result.Constructs;
	}
}
