using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The `VBNet` detection fixtures, through the real embedded analyzer and the real recognition engine. One named fixture per form read, one per declared gap
/// and one near-miss beside each, every source INVENTED from the catalog's own prose with placeholder names. The names are stable: the differential
/// test lists them by <see cref="CorrectionFixture.Name"/>.
///
/// A fixture that carries a level asserts that its class reports at that level; a fixture without one asserts that its class does not report at all (a declined form, a near-miss or a
/// declared unsupported form); <see cref="CorrectionFixture.AlsoAbsent"/> names the classes that must not report beside it. A declared COST fixture is pinned as the analyzer reads it,
/// the wrong finding included, so that a later change to a declared form is seen.
/// </summary>
public sealed class VBNetCatalogFiveCorrectionTests
{
	private const string _LANGUAGE_NAME = "VBNet";

	private const string _VB_006 = "VB-006";
	private const string _VB_007 = "VB-007";
	private const string _VB_010 = "VB-010";
	private const string _VB_014 = "VB-014";
	private const string _VB_015 = "VB-015";

	private const string _FIXED = "fixed";
	private const string _NEAR_MISS_OF_A_FIX = "near-miss of a fix";
	private const string _DECLARED_UNSUPPORTED = "declared unsupported";
	private const string _DECLARED_COST = "declared cost";
	private const string _PINNED_AS_SHIPPED = "pinned as shipped";

	private const string _ENCODE_THE_PAIR = "Dim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))";
	private const string _ENCODE_CALL_OPENING = "Dim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(";
	private const string _ENCODE_CALL_CLOSING = "))";
	private const string _AUTHORIZATION_WRITE_FORMAT = "widgetClient.Headers.Add(\"Authorization\", {0})";

	/// <summary>The item a fixture answers and how the catalog disposes of it.</summary>
	private sealed record Reason(string Item, string Disposition);

	/// <summary>A fixture: the item it answers, how the catalog disposes of it, the class it is about, the level expected (none: the class must not report) and the source.</summary>
	public sealed record CorrectionFixture(string Name, string Item, string Disposition, string PatternClass, ConfidenceLevel? Level, string Source, ImmutableArray<string> AlsoAbsent);

	private static readonly ImmutableArray<CorrectionFixture> _fixtures =
	[
		// (a): the character-literal colon that continues the pair is not the colon-last pair of VB-015
		Medium("CharColonAmpersandBeforeTheEncoder", Fixed("RB-VB-B5-175"), _VB_014, Assembled("userName & \":\"c & password"), _VB_015),
		Medium("CharColonPlusBeforeTheEncoder", Fixed("RB-VB-B5-175"), _VB_014, Assembled("userName + \":\"c + password"), _VB_015),
		Medium("CharColonLastBeforeTheEncoder", NearMissOfAFix("RB-VB-B5-175"), _VB_015, Assembled("sessionBlob & \":\"c")),

		// The colon-last pair does not read an accumulation of the pair into itself, with the string colon or the character-literal colon
		None("AccumulatingTheColonIntoThePairIsNotAColonLastPair", NearMissOfAFix("RB-VB-B5-175"), _VB_015, $"Dim pair As String = \"\"\npair = pair & \":\"\n{_ENCODE_THE_PAIR}"),
		None("AccumulatingTheCharColonIntoThePairIsNotAColonLastPair", NearMissOfAFix("RB-VB-B5-175"), _VB_015, $"Dim pair As String = \"\"\npair = pair & \":\"c\n{_ENCODE_THE_PAIR}"),

		// The character-literal colon inside the encoder call
		Medium("CharColonInsideTheCallAmpersand", Fixed("character-literal colon inside the encoder call"), _VB_014, Inline("userName & \":\"c & password"), _VB_015),
		Medium("CharColonInsideTheCallPlus", Fixed("character-literal colon inside the encoder call"), _VB_014, Inline("userName + \":\"c + password"), _VB_015),
		Medium("CharColonEmptyFirstInsideTheCallAmpersand", Fixed("character-literal colon inside the encoder call"), _VB_015, Inline("\"\" & \":\"c & sessionBlob"), _VB_014),
		Medium("CharColonEmptyFirstInsideTheCallPlus", Fixed("character-literal colon inside the encoder call"), _VB_015, Inline("String.Empty + \":\"c + sessionBlob"), _VB_014),
		Medium("CharColonFirstInsideTheCall", Fixed("character-literal colon inside the encoder call"), _VB_015, Inline("\":\"c & sessionBlob")),
		Medium("CharColonLastInsideTheCall", Fixed("character-literal colon inside the encoder call"), _VB_015, Inline("sessionBlob & \":\"c")),
		Medium("CharColonEmptyLastInsideTheCall", Fixed("character-literal colon inside the encoder call"), _VB_015, Inline("sessionBlob & \":\"c & \"\"")),

		// (c): interpolation with the colon first or last and the credential as its only hole
		Medium("InterpolationColonFirstInline", Fixed("RB-VB-B6-007"), _VB_015, Inline("$\":{archivePassphrase}\""), _VB_014),
		Medium("InterpolationColonLastInline", Fixed("RB-VB-B6-007"), _VB_015, Inline("$\"{archivePassphrase}:\""), _VB_014),
		Medium("InterpolationColonFirstAssigned", Fixed("RB-VB-B6-007"), _VB_015, Assembled("$\":{archivePassphrase}\""), _VB_014),
		Medium("InterpolationColonLastAssigned", Fixed("RB-VB-B6-007"), _VB_015, Assembled("$\"{archivePassphrase}:\""), _VB_014),
		Medium("InterpolationTwoHolesIsThePairClass", NearMissOfAFix("RB-VB-B6-007"), _VB_014, Inline("$\"{widgetHost}:{accessPassword}\""), _VB_015),
		None("InterpolationColonLastFeedingNoEncoder", NearMissOfAFix("RB-VB-B6-007"), _VB_015, "Dim label = $\"{archivePassphrase}:\"\nDim shown = label.Trim()"),
		None("InterpolationColonWithOtherText", NearMissOfAFix("RB-VB-B6-007"), _VB_015, Inline("$\"{archivePassphrase}:x\"")),

		// (d): String.Concat and String.Join with an explicit line continuation
		Medium("ConcatContinuedAfterTheLastComma", Fixed("RB-VB-B6-008"), _VB_015, Assembled("String.Concat(\"\", \":\", _\n\t\tarchivePassphrase)")),
		Medium("ConcatContinuedAfterTheFirstComma", Fixed("RB-VB-B6-008"), _VB_015, Assembled("String.Concat(\"\", _\n\t\t\":\", archivePassphrase)")),
		Medium("ConcatContinuedBeforeTheClosingParenthesis", Fixed("RB-VB-B6-008"), _VB_015, Assembled("String.Concat(\"\", \":\", archivePassphrase _\n\t)")),
		Medium("JoinContinued", Fixed("RB-VB-B6-008"), _VB_015, Assembled("String.Join(\":\", \"\", _\n\t\tarchivePassphrase)")),
		Medium("ConcatEmptyVariableContinued", Fixed("RB-VB-B6-008"), _VB_015, $"Dim seed As String = \"\"\nDim pair = String.Concat(seed, \":\", _\n\tarchivePassphrase)\n{_ENCODE_THE_PAIR}"),
		Medium("ConcatOneLineUnchanged", NearMissOfAFix("RB-VB-B6-008"), _VB_015, Assembled("String.Concat(\"\", \":\", archivePassphrase)")),

		// The empty first operand inside the encoder call, with an operand after the credential, is read, and a colon first with the same trailing operand stays declined
		Medium("EmptyFirstOperandWithATrailingOperandInsideTheCallIsRead", PinnedAsShipped("empty first operand with a trailing operand"), _VB_015, Inline("\"\" & \":\" & sessionBlob & suffix")),
		None("ColonFirstWithATrailingOperandInsideTheCallIsDeclined", PinnedAsShipped("empty first operand with a trailing operand"), _VB_015, Inline("\":\" & sessionBlob & suffix")),

		// vbNullString as the first operand inside the encoder call
		Medium("VbNullStringFirstInsideTheCallAmpersand", Fixed("vbNullString first operand inside the encoder call"), _VB_015, Inline("vbNullString & \":\" & sessionBlob"), _VB_014),
		Medium("VbNullStringFirstInsideTheCallPlus", Fixed("vbNullString first operand inside the encoder call"), _VB_015, Inline("vbNullString + \":\" + sessionBlob"), _VB_014),
		Medium("VbNullStringQualifiedFirstInsideTheCall", Fixed("vbNullString first operand inside the encoder call"), _VB_015, Inline("Constants.vbNullString & \":\" & sessionBlob"), _VB_014),
		Medium("VbNullStringAsPartOfAName", NearMissOfAFix("vbNullString first operand inside the encoder call"), _VB_014, Inline("vbNullStringValue & \":\" & token"), _VB_015),

		// The size and port guard, declared as a cost (the spellings of the catalog's table, rows 2 and 5)
		Medium("SizeHalfSecondInsideTheCallIsStillRead", DeclaredCost("O5-30"), _VB_014, Inline("userName & \":\" & passwordFile.Length")),
		Medium("SizeHalfFirstBeforeTheEncoderIsStillRead", DeclaredCost("O5-30"), _VB_014, Assembled("itemList.Count & \":\" & password")),
		None("SizeHalfInsideTheFormatIsDeclined", NearMissOfAFix("O5-30"), _VB_014, Inline("String.Format(\"{0}:{1}\", userName, passwordFile.Length)")),
		Medium("PortMemberSecondHalfInsideTheCallIsStillRead", DeclaredCost("O5-30"), _VB_014, Inline("userName & \":\" & password.Port")),
		None("PortMemberSecondHalfBeforeTheEncoderIsDeclined", NearMissOfAFix("O5-30"), _VB_014, Assembled("userName & \":\" & password.Port")),

		// Row 6: VB-014 reads a pair only where its second half carries a credential word
		None("NeutralPairBeforeTheEncoderAmpersand", Fixed("RB-VB-B6-004"), _VB_014, Assembled("itemLabel & \":\" & siteName")),
		None("NeutralPairBeforeTheEncoderPlus", Fixed("RB-VB-B6-004"), _VB_014, Assembled("itemLabel + \":\" + siteName")),
		None("NeutralPairInsideTheCallAmpersand", Fixed("RB-VB-B6-004"), _VB_014, Inline("itemLabel & \":\" & siteName")),
		None("NeutralPairInsideTheFormat", Fixed("RB-VB-B6-004"), _VB_014, Inline("String.Format(\"{0}:{1}\", itemLabel, siteName)")),
		None("NeutralPairAssembledWithTheFormat", Fixed("RB-VB-B6-004"), _VB_014, Assembled("String.Format(\"{0}:{1}\", itemLabel, siteName)")),
		None("NeutralPairInsideTheInterpolation", Fixed("RB-VB-B6-004"), _VB_014, Inline("$\"{itemLabel}:{siteName}\"")),
		None("NeutralPairInsideTheConcat", Fixed("RB-VB-B6-004"), _VB_014, Inline("String.Concat(itemLabel, \":\", siteName)")),
		None("HostAndNonPortIdentifierEncoded", Fixed("RB-VB-B6-004"), _VB_014, Assembled("widgetHost & \":\" & slotNumber")),
		Medium("CredentialSecondHalfStaysBeforeTheEncoder", NearMissOfAFix("RB-VB-B6-004"), _VB_014, Assembled("userName & \":\" & clientSecret")),
		Medium("CredentialSecondHalfStaysInsideTheFormat", NearMissOfAFix("RB-VB-B6-004"), _VB_014, Inline("String.Format(\"{0}:{1}\", userName, apiKey)")),
		Medium("CredentialWordInADottedSegmentIsRead", NearMissOfAFix("RB-VB-B6-004"), _VB_014, Assembled("userName & \":\" & txtPassword.Text")),
		Medium("CredentialWordInTheReceiverOfACallIsRead", NearMissOfAFix("RB-VB-B6-004"), _VB_014, Inline("userName & \":\" & password.ToString")),
		None("SecondHalfWithNoCredentialWordBeforeTheEncoder", DeclaredUnsupported("O5-41"), _VB_014, Assembled("loginName & \":\" & pin")),
		None("SecondHalfWithNoCredentialWordInsideTheCall", DeclaredUnsupported("O5-41"), _VB_014, Inline("loginName & \":\" & passphrase")),
		Medium("SecondHalfCarriesAWordButIsNotACredential", DeclaredCost("K-7"), _VB_014, Assembled("userName & \":\" & tokenExpiry")),

		// (a): a token returned by a call is not the password of the entry point
		None("TokenCallWithNoArgumentsAsThePassword", Fixed("RB-VB-B6-096"), _VB_010, "Dim api As New ImportAPI(sentinelLogin, FetchPlatformToken(), webServiceUrl)"),
		None("TokenCallWithAnArgumentAsThePassword", Fixed("RB-VB-B6-096"), _VB_010, "Dim api As New ImportAPI(sentinelLogin, FetchPlatformToken(scope), webServiceUrl)"),
		None("TokenCallDottedAsThePassword", Fixed("RB-VB-B6-096"), _VB_010, "Dim api As New ImportAPI(sentinelLogin, Me.Tokens.FetchPlatformToken(), webServiceUrl)"),
		None("TokenCallNamedArgumentAsThePassword", Fixed("RB-VB-B6-096"), _VB_010, "Dim api As New ImportAPI(sentinelLogin, password:=FetchPlatformToken(), webServiceUrl)"),
		High("PasswordWordedCallStaysRead", NearMissOfAFix("RB-VB-B6-096"), _VB_010, "Dim api As New ImportAPI(sentinelLogin, GetPasswordToken(), webServiceUrl)"),
		High("TokenCallWithANestedGroupIsStillRead", DeclaredCost("O5-40"), _VB_010, "Dim api As New ImportAPI(sentinelLogin, FetchPlatformToken(Resolve(scope)), webServiceUrl)"),

		// (b): the project's own type named like the SDK entry point
		None("OwnStructureConstructedUnqualified", Fixed("RB-VB-B6-113"), _VB_010, $"{VBNetEntryPointFixtureSources.OWN_STRUCTURE}{VBNetEntryPointFixtureSources.CONSTRUCTION}"),
		None("OwnClassConstructedUnqualified", Fixed("RB-VB-B6-113"), _VB_010, $"{VBNetEntryPointFixtureSources.OWN_CLASS}{VBNetEntryPointFixtureSources.CONSTRUCTION}"),
		None("OwnClassDeclaredAfterTheUse", Fixed("RB-VB-B6-113"), _VB_010, $"{VBNetEntryPointFixtureSources.CONSTRUCTION}\n{VBNetEntryPointFixtureSources.OWN_CLASS}"),
		High("SdkTypeQualifiedBesideAHomonym", NearMissOfAFix("RB-VB-B6-113"), _VB_010, $"{VBNetEntryPointFixtureSources.OWN_STRUCTURE}{VBNetEntryPointFixtureSources.QUALIFIED_CONSTRUCTION}"),
		High("SdkTypeGloballyQualifiedBesideAHomonym", NearMissOfAFix("RB-VB-B6-113"), _VB_010, $"{VBNetEntryPointFixtureSources.OWN_CLASS}{VBNetEntryPointFixtureSources.GLOBALLY_QUALIFIED_CONSTRUCTION}"),
		High("PrefixedSubtypeDeclaredAndConstructed", NearMissOfAFix("RB-VB-B6-113"), _VB_010, "Public Class ExtendedImportAPI\n\tPublic Sub New(a As String, b As String, c As String)\n\tEnd Sub\nEnd Class\nDim api As New ExtendedImportAPI(userName, password, webServiceUrl)"),
		High("HomonymDeclaredBeyondTheReachBefore", DeclaredCost("O5-39"), _VB_010, VBNetEntryPointFixtureSources.OwnClassFarBeforeTheUse()),
		High("HomonymDeclaredBeyondTheReachAfter", DeclaredCost("O5-39"), _VB_010, VBNetEntryPointFixtureSources.OwnClassFarAfterTheUse()),
		None("HomonymDeclaredWithinTheReachBetween512And1024Before", Fixed("RB-VB-B6-113"), _VB_010, VBNetEntryPointFixtureSources.OwnClassAtADistanceBeforeTheUse(VBNetEntryPointFixtureSources.DISTANCE_BETWEEN_HALF_AND_THE_FULL_REACH)),
		None("HomonymDeclaredWithinTheReachBetween512And1024After", Fixed("RB-VB-B6-113"), _VB_010, VBNetEntryPointFixtureSources.OwnClassAtADistanceAfterTheUse(VBNetEntryPointFixtureSources.DISTANCE_BETWEEN_HALF_AND_THE_FULL_REACH)),
		None("HomonymDeclaredAtTheEdgeOf1024Before", Fixed("RB-VB-B6-113"), _VB_010, VBNetEntryPointFixtureSources.OwnClassAtADistanceBeforeTheUse(VBNetEntryPointFixtureSources.DISTANCE_AT_THE_EDGE_OF_THE_REACH)),
		None("HomonymDeclaredAtTheEdgeOf1024After", Fixed("RB-VB-B6-113"), _VB_010, VBNetEntryPointFixtureSources.OwnClassAtADistanceAfterTheUse(VBNetEntryPointFixtureSources.DISTANCE_AT_THE_EDGE_OF_THE_REACH)),
		High("HomonymDeclaredJustBeyond1024Before", DeclaredCost("O5-39"), _VB_010, VBNetEntryPointFixtureSources.OwnClassAtADistanceBeforeTheUse(VBNetEntryPointFixtureSources.DISTANCE_JUST_BEYOND_THE_REACH)),
		High("HomonymDeclaredJustBeyond1024After", DeclaredCost("O5-39"), _VB_010, VBNetEntryPointFixtureSources.OwnClassAtADistanceAfterTheUse(VBNetEntryPointFixtureSources.DISTANCE_JUST_BEYOND_THE_REACH)),

		// (a): the whole-token words end at the boundary of their spelling
		Medium("ForwardPassHeaderIsRead", Fixed("RB-VB-B6-029"), _VB_007, Write("forwardPassHeader")),
		Medium("RelayPassHeaderIsRead", Fixed("RB-VB-B6-029"), _VB_007, Write("relayPassHeader")),
		Medium("LowerCasePassHeaderIsRead", Fixed("RB-VB-B6-029"), _VB_007, Write("passHeader")),
		Medium("LowerCasePwdHeaderIsRead", Fixed("RB-VB-B6-029"), _VB_007, Write("pwdHeader")),
		Medium("PascalPassHeaderIsRead", Fixed("RB-VB-B6-029"), _VB_007, Write("PassHeader")),
		Medium("SnakePassHeaderIsRead", Fixed("RB-VB-B6-029"), _VB_007, Write("forward_pass_header")),
		Medium("AllCapitalPrefixBeforePassIsRead", PinnedAsShipped("all-capital prefix before Pass"), _VB_007, Write("BYPass")),
		None("BypassIsNotAToken", NearMissOfAFix("RB-VB-B6-029"), _VB_007, Write("Bypass")),
		None("CompassIsNotAToken", NearMissOfAFix("RB-VB-B6-029"), _VB_007, Write("Compass")),
		None("LowerCasePassportIsNotAToken", NearMissOfAFix("RB-VB-B6-029"), _VB_007, Write("passport")),
		None("PascalPassportIsNotAToken", NearMissOfAFix("RB-VB-B6-029"), _VB_007, Write("Passport")),
		Medium("PascalPassWithADigitIsRead", NearMissOfAFix("O5-42"), _VB_007, Write("Pass2")),
		None("LowerCasePassWithADigitIsNotRead", DeclaredUnsupported("O5-42"), _VB_007, Write("pass2")),
		None("LowerCasePwdWithADigitIsNotRead", DeclaredUnsupported("O5-42"), _VB_007, Write("pwd1")),

		// (b): the VB-006 anchor, a file whose only construct is the header value
		Medium("HeaderValueFunctionWithALiteralScheme", Fixed("VB-006 anchor"), _VB_006, "Private Function BuildHeader(encoded As String) As AuthenticationHeaderValue\n\tReturn New AuthenticationHeaderValue(\"Basic\", encoded)\nEnd Function"),
		Medium("HeaderValueFunctionWithASchemeConstant", Fixed("VB-006 anchor"), _VB_006, "Private Const BASIC_SCHEME As String = \"Basic\"\nPrivate Function BuildHeader(encoded As String) As AuthenticationHeaderValue\n\tReturn New AuthenticationHeaderValue(BASIC_SCHEME, encoded)\nEnd Function"),
		None("HeaderValueFunctionWithABearerScheme", NearMissOfAFix("VB-006 anchor"), _VB_006, "Private Function BuildHeader(accessValue As String) As AuthenticationHeaderValue\n\tReturn New AuthenticationHeaderValue(\"Bearer\", accessValue)\nEnd Function")
	];

	private static readonly FrozenDictionary<string, CorrectionFixture> _fixturesByName = _fixtures.ToFrozenDictionary(fixture => fixture.Name, StringComparer.Ordinal);

	/// <summary>Every fixture, in a fixed order; the differential test lists them by name.</summary>
	public static ImmutableArray<CorrectionFixture> Fixtures()
	{
		return _fixtures;
	}

	public static TheoryData<string> FixtureNames()
	{
		TheoryData<string> data = [];

		foreach (CorrectionFixture fixture in _fixtures)
		{
			data.Add(fixture.Name);
		}

		return data;
	}

	[Fact]
	public void EveryFixtureNameIsUniqueAndEveryFixedDefectAndEveryDeclaredGapHasAFixture()
	{
		Assert.Equal(_fixtures.Length, _fixturesByName.Count);

		string[] fixedItems =
		[
			"RB-VB-B5-175", "RB-VB-B6-007", "RB-VB-B6-008", "character-literal colon inside the encoder call", "vbNullString first operand inside the encoder call", "RB-VB-B6-004", "RB-VB-B6-096", "RB-VB-B6-113", "RB-VB-B6-029", "VB-006 anchor"
		];
		string[] declaredItems = ["O5-30", "O5-39", "O5-40", "O5-41", "O5-42", "K-7"];

		Assert.All(fixedItems, item => Assert.Contains(_fixtures, fixture => fixture.Item == item && fixture.Disposition == _FIXED));
		Assert.All(declaredItems, item => Assert.Contains(_fixtures, fixture => fixture.Item == item && fixture.Disposition is _DECLARED_COST or _DECLARED_UNSUPPORTED));
	}

	[Theory]
	[MemberData(nameof(FixtureNames))]
	public void ACorrectionFixtureIsReadAsTheCatalogDecides(string name)
	{
		CorrectionFixture fixture = _fixturesByName[name];
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(fixture.Source);
		string found = string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}@{construct.Level}"));

		if (fixture.Level is { } level)
		{
			Assert.True(
				constructs.Any(construct => construct.PatternClassIdentifier == fixture.PatternClass && construct.Level == level),
				$"{name}: {fixture.PatternClass} did not report at {level}; got [{found}]");
		}
		else
		{
			Assert.False(
				constructs.Any(construct => construct.PatternClassIdentifier == fixture.PatternClass),
				$"{name}: {fixture.PatternClass} reported; got [{found}]");
		}

		Assert.All(
			fixture.AlsoAbsent,
			absent => Assert.False(constructs.Any(construct => construct.PatternClassIdentifier == absent), $"{name}: {absent} reported beside it; got [{found}]"));
	}

	[Fact]
	public void TheAuthenticationHeaderValueAnchorAdmitsAFileWhoseOnlyConstructIsTheHeaderValue()
	{
		ILanguageAnalyzer analyzer = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_LANGUAGE_NAME];
		string literalScheme = _fixturesByName["HeaderValueFunctionWithALiteralScheme"].Source;
		string schemeConstant = _fixturesByName["HeaderValueFunctionWithASchemeConstant"].Source;

		Assert.True(CandidatePreselection.IsCandidate(analyzer, literalScheme));
		Assert.True(CandidatePreselection.IsCandidate(analyzer, schemeConstant));
		Assert.DoesNotContain("Credential", literalScheme, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("Authorization", literalScheme, StringComparison.OrdinalIgnoreCase);
		Assert.False(CandidatePreselection.IsCandidate(analyzer, literalScheme.Replace("AuthenticationHeaderValue", "WidgetHeaderValue", StringComparison.Ordinal)));
	}

	private static CorrectionFixture Medium(string name, Reason reason, string patternClass, string source, params string[] alsoAbsent)
	{
		return new CorrectionFixture(name, reason.Item, reason.Disposition, patternClass, ConfidenceLevel.Medium, source, [.. alsoAbsent]);
	}

	private static CorrectionFixture High(string name, Reason reason, string patternClass, string source, params string[] alsoAbsent)
	{
		return new CorrectionFixture(name, reason.Item, reason.Disposition, patternClass, ConfidenceLevel.High, source, [.. alsoAbsent]);
	}

	private static CorrectionFixture None(string name, Reason reason, string patternClass, string source, params string[] alsoAbsent)
	{
		return new CorrectionFixture(name, reason.Item, reason.Disposition, patternClass, null, source, [.. alsoAbsent]);
	}

	private static Reason Fixed(string item)
	{
		return new Reason(item, _FIXED);
	}

	private static Reason NearMissOfAFix(string item)
	{
		return new Reason(item, _NEAR_MISS_OF_A_FIX);
	}

	private static Reason DeclaredUnsupported(string item)
	{
		return new Reason(item, _DECLARED_UNSUPPORTED);
	}

	private static Reason PinnedAsShipped(string item)
	{
		return new Reason(item, _PINNED_AS_SHIPPED);
	}

	private static Reason DeclaredCost(string item)
	{
		return new Reason(item, _DECLARED_COST);
	}

	/// <summary>The pair expression written inside the encoder call.</summary>
	private static string Inline(string pairExpression)
	{
		return $"{_ENCODE_CALL_OPENING}{pairExpression}{_ENCODE_CALL_CLOSING}";
	}

	/// <summary>The pair expression assigned to a variable that the encoder then reads.</summary>
	private static string Assembled(string pairExpression)
	{
		return $"Dim pair = {pairExpression}\n{_ENCODE_THE_PAIR}";
	}

	private static string Write(string valueName)
	{
		return string.Format(System.Globalization.CultureInfo.InvariantCulture, _AUTHORIZATION_WRITE_FORMAT, valueName);
	}
}
