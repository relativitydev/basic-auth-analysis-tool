using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// SAME-FILE WRAPPERS. A construct recognized inside a function body that uses one of the function's
/// parameters is evidence at each call that passes a credential-bearing argument there, carrying the inner pattern
/// class and level, anchored at the call's command word. A wrapper never called with a credential yields nothing.
/// The expressions are the test-local ones of <see cref="ShellProbe"/>.
/// </summary>
public sealed class SameFileWrapperTests
{
	private const string _DEFINITION_OPENING = "fetch() {";
	private const string _DEFINITION_CLOSING = "}";
	private const string _USER_OPTION_LINE = "\tcurl -u \"$1\" \"$2\"";
	private const string _CREDENTIAL_CALL = "fetch admin:hunter2 https://example.invalid/a";
	private const string _OTHER_CREDENTIAL_CALL = "fetch svc:token9 https://example.invalid/c";
	private const string _PLAIN_CALL = "fetch plainword https://example.invalid/b";
	private const string _LITERAL_CALL_LINE = "curl -u plain:secret https://example.invalid/literal";
	private const int _ONE_MILLISECOND = 1;
	private const string _CATASTROPHIC_PATTERN = @"(?<name>(\w+\s?)*)(?<open>)$";
	private const string _LINE_THAT_CANNOT_MATCH = "the quick brown fox jumps over the lazy dog and keeps running well past the point of reason!";

	[Fact]
	public void AWrapperCalledWithACredentialIsReadAtTheInnerClassAndLevelAnchoredAtTheCall()
	{
		string source = ShellProbe.Source(_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING, _CREDENTIAL_CALL);

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(ShellProbeAnalyzer.CLIENT_CALL_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(source.IndexOf(_CREDENTIAL_CALL, StringComparison.Ordinal), construct.AnchorOffset);
		Assert.False(construct.LiteralCredentialSignal);
	}

	[Fact]
	public void EachCredentialBearingCallIsOneFindingAndACallWithANonCredentialArgumentIsNone()
	{
		string source = ShellProbe.Source(
			_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING,
			_CREDENTIAL_CALL,
			_PLAIN_CALL,
			_OTHER_CREDENTIAL_CALL);

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		Assert.Equal(
			[
				source.IndexOf(_CREDENTIAL_CALL, StringComparison.Ordinal),
				source.IndexOf(_OTHER_CREDENTIAL_CALL, StringComparison.Ordinal)
			],
			constructs.Select(construct => construct.AnchorOffset).Order());
		Assert.All(constructs, construct => Assert.Equal(ShellProbeAnalyzer.CLIENT_CALL_CLASS, construct.PatternClassIdentifier));
	}

	[Fact]
	public void TheConstructIsNotReadAtTheDefinitionSite()
	{
		string source = ShellProbe.Source(_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING, _CREDENTIAL_CALL);
		int insideTheBody = source.IndexOf("curl", StringComparison.Ordinal);

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.AnchorOffset == insideTheBody);
	}

	[Fact]
	public void AWrapperThatIsNeverCalledWithACredentialYieldsNothingAndIsNotReadAtItsDefinition()
	{
		string neverCalled = ShellProbe.Source(_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING);
		string calledWithoutACredential = ShellProbe.Source(_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING, _PLAIN_CALL);

		Assert.Empty(Recognize(neverCalled));
		Assert.Empty(Recognize(calledWithoutACredential));
	}

	[Fact]
	public void ADefinitionInAnotherFileIsNotFollowedSoACallAloneYieldsNothing()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(ShellProbe.Source(_CREDENTIAL_CALL));

		Assert.Empty(constructs);
	}

	[Fact]
	public void AConstructOutsideEveryBodyIsReadWhereItStands()
	{
		string source = ShellProbe.Source(_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING, _CREDENTIAL_CALL, _LITERAL_CALL_LINE);

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		Assert.Equal(2, constructs.Length);
		Assert.Contains(constructs, construct => construct.AnchorOffset == source.IndexOf(_LITERAL_CALL_LINE, StringComparison.Ordinal));
		Assert.Contains(constructs, construct => construct.AnchorOffset == source.IndexOf(_CREDENTIAL_CALL, StringComparison.Ordinal));
	}

	[Fact]
	public void ThePassThroughOfAllArgumentsIsFollowed()
	{
		string source = ShellProbe.Source(_DEFINITION_OPENING, "\tcurl -u \"$@\"", _DEFINITION_CLOSING, _CREDENTIAL_CALL);

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(source.IndexOf(_CREDENTIAL_CALL, StringComparison.Ordinal), construct.AnchorOffset);
	}

	[Fact]
	public void ThePassThroughOfAllArgumentsIsNotFollowedWhenNoArgumentIsACredential()
	{
		string source = ShellProbe.Source(_DEFINITION_OPENING, "\tcurl -u \"$*\"", _DEFINITION_CLOSING, _PLAIN_CALL);

		Assert.Empty(Recognize(source));
	}

	[Fact]
	public void ANamerefOfAPositionalParameterIsFollowed()
	{
		string source = ShellProbe.Source(
			_DEFINITION_OPENING,
			"\tlocal -n ref=$1",
			"\tcurl -u \"$ref\" url",
			_DEFINITION_CLOSING,
			_CREDENTIAL_CALL);

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(source.IndexOf(_CREDENTIAL_CALL, StringComparison.Ordinal), construct.AnchorOffset);
	}

	[Fact]
	public void ACopyOfAPositionalParameterIsFollowedAndOneOfAnotherParameterIsNot()
	{
		string followed = ShellProbe.Source(_DEFINITION_OPENING, "\tcred=$1", "\tcurl -u \"$cred\" url", _DEFINITION_CLOSING, _CREDENTIAL_CALL);
		string notCredential = ShellProbe.Source(_DEFINITION_OPENING, "\tcred=$2", "\tcurl -u \"$cred\" url", _DEFINITION_CLOSING, _CREDENTIAL_CALL);

		Assert.Single(Recognize(followed));
		Assert.Empty(Recognize(notCredential));
	}

	[Fact]
	public void OnlyTheArgumentAtTheParameterTheConstructUsesIsTested()
	{
		string source = ShellProbe.Source(
			_DEFINITION_OPENING,
			"\tcurl -u \"$2\" \"$1\"",
			_DEFINITION_CLOSING,
			"fetch https://example.invalid/a admin:hunter2",
			"fetch admin:hunter2 https://example.invalid/b");

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(source.IndexOf("fetch https://example.invalid/a admin:hunter2", StringComparison.Ordinal), construct.AnchorOffset);
	}

	/// <summary>
	/// An argument that is wholly an expansion and does not resolve is not credential-bearing, and its own text is never
	/// tested: the expression below would match any text holding a dollar sign.
	/// </summary>
	[Fact]
	public void AnUnresolvedWholeExpansionArgumentIsNeverTestedAsText()
	{
		WrapperTracking matchesDollarText = ShellProbe.Wrappers() with
		{
			CredentialBearingArgument = ShellProbeExpressions.AnythingWithADollarOrAUserAndSecret()
		};
		string source = ShellProbe.Source(
			_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING,
			"fetch \"$@\" url",
			"fetch $1 url",
			"fetch ${arr[0]} url",
			"fetch \"$UNKNOWN\" url",
			"fetch \"${UNKNOWN}\" url",
			_CREDENTIAL_CALL);
		ShellProbeAnalyzer analyzer = ShellProbe.AnalyzerWith([], wrappers: matchesDollarText);

		ImmutableArray<RecognizedConstruct> constructs = ShellProbe.Recognize(analyzer, source).Result.Constructs;

		Assert.Equal(source.IndexOf(_CREDENTIAL_CALL, StringComparison.Ordinal), Assert.Single(constructs).AnchorOffset);
	}

	/// <summary>
	/// A braced expansion with an operator after the name can carry a literal credential, so its text is read; the plain
	/// braced forms are not read when they do not resolve.
	/// </summary>
	[Fact]
	public void ABracedExpansionWithAnOperatorIsTestedAsTextAndAPlainOneIsNot()
	{
		WrapperTracking matchesDollarText = ShellProbe.Wrappers() with
		{
			CredentialBearingArgument = ShellProbeExpressions.AnythingWithADollarOrAUserAndSecret()
		};
		string[] withOperators =
		[
			"fetch \"${TOKEN:-admin:hunter2}\" url",
			"fetch ${TOKEN:=admin:hunter2} url",
			"fetch ${TOKEN-admin:hunter2} url",
			"fetch ${TOKEN+admin:hunter2} url",
			"fetch ${TOKEN#pat} url",
			"fetch ${TOKEN%pat} url",
			"fetch ${TOKEN/a/b} url",
			"fetch ${#X} url",
			"fetch ${!X} url"
		];
		string[] plain = ["fetch ${X} url", "fetch \"${X}\" url", "fetch ${arr[0]} url", "fetch ${1} url", "fetch $* url"];
		string source = ShellProbe.Source([_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING, .. plain, .. withOperators]);
		ShellProbeAnalyzer analyzer = ShellProbe.AnalyzerWith([], wrappers: matchesDollarText);

		ImmutableArray<RecognizedConstruct> constructs = ShellProbe.Recognize(analyzer, source).Result.Constructs;

		Assert.Equal(
			withOperators.Select(call => source.IndexOf(call, StringComparison.Ordinal)).Order(),
			constructs.Select(construct => construct.AnchorOffset).Order());
	}

	[Fact]
	public void AnArgumentThatIsAVariableIsResolvedThroughTheSymbolTableToItsUniqueValue()
	{
		string source = ShellProbe.Source(
			"CRED=\"admin:hunter2\"",
			_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING,
			"fetch \"$CRED\" https://example.invalid/a",
			"fetch ${CRED} https://example.invalid/b",
			"fetch \"$UNKNOWN\" https://example.invalid/c");

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		Assert.Equal(
			[
				source.IndexOf("fetch \"$CRED\"", StringComparison.Ordinal),
				source.IndexOf("fetch ${CRED}", StringComparison.Ordinal)
			],
			constructs.Select(construct => construct.AnchorOffset).Order());
	}

	[Fact]
	public void AVariableDeclaredTwiceDoesNotResolveAndIsNotCredentialBearing()
	{
		string source = ShellProbe.Source(
			"CRED=\"admin:hunter2\"",
			"CRED=\"other:value\"",
			_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING,
			"fetch \"$CRED\" https://example.invalid/a");

		Assert.Empty(Recognize(source));
	}

	[Fact]
	public void TheBodyEndsAtTheMatchingBraceWhateverTheExpansionsCommentsAndStringsContain()
	{
		string source = ShellProbe.Source(
			_DEFINITION_OPENING,
			"\tlocal fallback=${3:-none}",
			"\t# a stray } in a comment",
			"\techo \"}\"",
			"\techo '{'",
			_USER_OPTION_LINE,
			_DEFINITION_CLOSING,
			_LITERAL_CALL_LINE,
			_CREDENTIAL_CALL);

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		Assert.Equal(2, constructs.Length);
		Assert.Contains(constructs, construct => construct.AnchorOffset == source.IndexOf(_LITERAL_CALL_LINE, StringComparison.Ordinal));
		Assert.Contains(constructs, construct => construct.AnchorOffset == source.IndexOf(_CREDENTIAL_CALL, StringComparison.Ordinal));
	}

	[Fact]
	public void TwoConstructsOfTheSameClassInOneBodyAreOneFindingPerCall()
	{
		string source = ShellProbe.Source(_DEFINITION_OPENING, _USER_OPTION_LINE, _USER_OPTION_LINE, _DEFINITION_CLOSING, _CREDENTIAL_CALL);

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		Assert.Single(constructs);
	}

	[Fact]
	public void ABodyNotClosedWithinTheDeclaredLinesIsAbandonedAndKeepsItsDefinitionSiteReading()
	{
		string[] longBody = [_DEFINITION_OPENING, _USER_OPTION_LINE, "\techo 1", "\techo 2", "\techo 3", "\techo 4", _DEFINITION_CLOSING, _CREDENTIAL_CALL];
		string source = ShellProbe.Source(longBody);
		ShellProbeAnalyzer abandoning = ShellProbe.AnalyzerWith([], wrappers: ShellProbe.Wrappers(maxBodyLines: 3));
		ShellProbeAnalyzer following = ShellProbe.AnalyzerWith([], wrappers: ShellProbe.Wrappers());

		ImmutableArray<RecognizedConstruct> kept = ShellProbe.Recognize(abandoning, source).Result.Constructs;
		ImmutableArray<RecognizedConstruct> moved = ShellProbe.Recognize(following, source).Result.Constructs;

		Assert.Equal(source.IndexOf("curl", StringComparison.Ordinal), Assert.Single(kept).AnchorOffset);
		Assert.Equal(source.IndexOf(_CREDENTIAL_CALL, StringComparison.Ordinal), Assert.Single(moved).AnchorOffset);
	}

	[Fact]
	public void OnlyTheDeclaredNumberOfCallsIsExamined()
	{
		string source = ShellProbe.Source(
			_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING,
			_CREDENTIAL_CALL,
			_OTHER_CREDENTIAL_CALL);
		ShellProbeAnalyzer limited = ShellProbe.AnalyzerWith([], wrappers: ShellProbe.Wrappers(maxCalls: 1));

		ImmutableArray<RecognizedConstruct> constructs = ShellProbe.Recognize(limited, source).Result.Constructs;

		Assert.Equal(source.IndexOf(_CREDENTIAL_CALL, StringComparison.Ordinal), Assert.Single(constructs).AnchorOffset);
	}

	[Fact]
	public void ACallFromOneWrapperIntoAnotherIsNotFollowed()
	{
		string source = ShellProbe.Source(
			"outer() {",
			"\tfetch \"$1\" url",
			"}",
			_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING,
			"outer admin:hunter2");

		Assert.Empty(Recognize(source));
	}

	[Fact]
	public void ACallInsideAnotherBodyWithALiteralCredentialIsStillACredentialBearingCall()
	{
		string source = ShellProbe.Source(
			_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING,
			"outer() {",
			"\tfetch admin:hunter2 url",
			"}");

		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		Assert.Equal(source.IndexOf("fetch admin:hunter2", StringComparison.Ordinal), Assert.Single(constructs).AnchorOffset);
	}

	[Fact]
	public void ACommentedCallIsNotACall()
	{
		string source = ShellProbe.Source(_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING, $"# {_CREDENTIAL_CALL}");

		Assert.Empty(Recognize(source));
	}

	[Fact]
	public void ALanguageThatDeclaresNoWrapperTrackingKeepsTheDefinitionSiteReading()
	{
		string source = ShellProbe.Source(_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING, _CREDENTIAL_CALL);
		ShellProbeAnalyzer untracked = ShellProbe.AnalyzerWith([]);

		ImmutableArray<RecognizedConstruct> constructs = ShellProbe.Recognize(untracked, source).Result.Constructs;

		Assert.Equal(source.IndexOf("curl", StringComparison.Ordinal), Assert.Single(constructs).AnchorOffset);
	}

	[Fact]
	public void AWrapperExpressionThatTimesOutStopsTheFileLikeARuleThatTimesOut()
	{
		WrapperTracking timingOut = ShellProbe.Wrappers() with
		{
			FunctionDefinitionExpression = new Regex(_CATASTROPHIC_PATTERN, RegexOptions.None, TimeSpan.FromMilliseconds(_ONE_MILLISECOND))
		};
		ShellProbeAnalyzer analyzer = ShellProbe.AnalyzerWith([], wrappers: timingOut);
		string source = ShellProbe.Source(ShellProbe.CLIENT_CALL_LINE, _LINE_THAT_CANNOT_MATCH);

		(RecognitionResult result, RecognitionEngine _, RecognitionHarness harness) = ShellProbe.Recognize(analyzer, source);

		Assert.Empty(result.Constructs);
		Assert.Contains(harness.SkippedItems(), fact => fact.SkippedItem == SkippedItemCategory.MatchBoundExceeded);
	}

	[Fact]
	public void AWrapperExpressionIsChargedToTheMatchBoundLikeEveryOtherExpression()
	{
		string source = ShellProbe.Source(_DEFINITION_OPENING, _USER_OPTION_LINE, _DEFINITION_CLOSING, _CREDENTIAL_CALL);
		CountingTimeProvider tracked = new();
		CountingTimeProvider untracked = new();

		ShellProbe.Recognize(ShellProbe.AnalyzerWith([], wrappers: ShellProbe.Wrappers()), source, timeProvider: tracked);
		ShellProbe.Recognize(ShellProbe.AnalyzerWith([]), source, timeProvider: untracked);

		Assert.True(tracked.Reads > untracked.Reads);
	}

	[Fact]
	public void TheNameOfTheWrapperIsTheOneTheDefinitionDeclares()
	{
		string source = ShellProbe.Source(
			"fetch2() {",
			_USER_OPTION_LINE,
			_DEFINITION_CLOSING,
			_CREDENTIAL_CALL);

		Assert.Empty(Recognize(source));
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(string source)
	{
		ShellProbeAnalyzer analyzer = ShellProbe.AnalyzerWith([], wrappers: ShellProbe.Wrappers());

		return ShellProbe.Recognize(analyzer, source).Result.Constructs;
	}
}
