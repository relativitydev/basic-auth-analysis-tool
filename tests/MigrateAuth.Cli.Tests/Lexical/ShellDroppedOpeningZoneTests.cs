using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE ZONE OF A DROPPED HANDED-OVER OPENING: an opening that a hole hands to a holder whose queue is full is dropped without a
/// body and is code, as an opening refused where it stands is. It does not affect analysis (the following statement and the validity of
/// the mask are the same either way), so it is pinned by the zone itself.
/// </summary>
public sealed class ShellDroppedOpeningZoneTests
{
	private const string _FOLLOWING_STATEMENT = "after=1";
	private const string _PENDING_OPENING = "<<A ";

	// ---- The zone of a dropped handed-over opening ----

	[Fact]
	public void C4TheOpeningARegionRefusesWhereItStandsIsCode()
	{
		string openings = string.Concat(Enumerable.Repeat(_PENDING_OPENING, LexicalScanner.MAXIMUM_PENDING_BODIES + 1));
		string bodies = string.Concat(Enumerable.Repeat("A\n", LexicalScanner.MAXIMUM_PENDING_BODIES));
		string source = $"cat {openings}\n{bodies}after=1\n";
		int refused = source.IndexOf(_PENDING_OPENING, StringComparison.Ordinal) + (LexicalScanner.MAXIMUM_PENDING_BODIES * _PENDING_OPENING.Length);

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(refused));
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(refused + 1));
	}

	[Fact]
	public void C4AHandedOverOpeningDroppedForAFullQueueLeavesTheFollowingStatementCodeAndTheMaskValid()
	{
		string source = BuildAFullQueueAndAHandedOverOpening(out _);

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void C4AHandedOverOpeningDroppedForAFullQueueIsCodeLikeTheOneRefusedWhereItStands()
	{
		string source = BuildAFullQueueAndAHandedOverOpening(out int droppedOpening);

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(droppedOpening));
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(droppedOpening + 1));
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(droppedOpening + 2));
	}

	[Fact]
	public void C4NeighbourControlTheOpeningsThatFitTheQueueKeepTheirDelimiterMarking()
	{
		string source = BuildAFullQueueAndAHandedOverOpening(out int droppedOpening);

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.IndexOf(_PENDING_OPENING, StringComparison.Ordinal)));
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.LastIndexOf(_PENDING_OPENING, droppedOpening, StringComparison.Ordinal)));
	}

	private static string BuildAFullQueueAndAHandedOverOpening(out int droppedOpening)
	{
		string openings = string.Concat(Enumerable.Repeat(_PENDING_OPENING, LexicalScanner.MAXIMUM_PENDING_BODIES));
		string bodies = string.Concat(Enumerable.Repeat("A\n", LexicalScanner.MAXIMUM_PENDING_BODIES));
		string head = $"cat {openings}\"$(cat <<B)\"\n";

		droppedOpening = head.IndexOf("<<B", StringComparison.Ordinal);

		return $"{head}{bodies}after=1\n";
	}
}
