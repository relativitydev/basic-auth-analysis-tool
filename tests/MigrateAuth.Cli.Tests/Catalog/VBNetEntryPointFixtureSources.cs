namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The sources, written once, that exercise the declaration reach of the SDK entry point of <c>VB-010</c>: a construction of <c>ImportAPI</c>, a type of the project with the same
/// simple name, and filler that puts one beyond the reach of the other. The correction fixtures and the claims of Part A both use them, so the two cannot drift apart.
/// </summary>
internal static class VBNetEntryPointFixtureSources
{
	/// <summary>How far the declaration look-around reaches each side of a construction, in characters.</summary>
	private const int _DECLARATION_REACH_IN_CHARACTERS = 1024;

	/// <summary>Lines added beyond the reach, so that a declaration is farther than the reach and not merely at its edge.</summary>
	private const int _MARGIN_LINES_BEYOND_THE_REACH = 20;

	/// <summary>The lower bound of the band the boundary fixtures use inside the reach: half of it.</summary>
	internal const int HALF_OF_THE_REACH_IN_CHARACTERS = _DECLARATION_REACH_IN_CHARACTERS / _PARTS_IN_A_HALF;

	/// <summary>A distance between the declaration and the use that is inside the reach and past its half.</summary>
	internal const int DISTANCE_BETWEEN_HALF_AND_THE_FULL_REACH = (HALF_OF_THE_REACH_IN_CHARACTERS + _DECLARATION_REACH_IN_CHARACTERS) / _PARTS_IN_A_HALF;

	/// <summary>The farthest distance at which the declaration is still inside the reach.</summary>
	internal const int DISTANCE_AT_THE_EDGE_OF_THE_REACH = _DECLARATION_REACH_IN_CHARACTERS;

	/// <summary>The nearest distance at which the declaration is outside the reach.</summary>
	internal const int DISTANCE_JUST_BEYOND_THE_REACH = _DECLARATION_REACH_IN_CHARACTERS + 1;

	private const int _PARTS_IN_A_HALF = 2;

	private const string _SPELLING = "ImportAPI";
	private const string _DECLARATION_KEYWORD = "Class";

	private const string _FILLER_LINE = "Dim fillerValue As Integer = 0\n";

	internal const string CONSTRUCTION = "Dim importApi As New ImportAPI(userName, password, webServiceUrl)";
	internal const string QUALIFIED_CONSTRUCTION = "Dim importApi As New Vendor.Sdk.ImportAPI(userName, password, webServiceUrl)";
	internal const string GLOBALLY_QUALIFIED_CONSTRUCTION = "Dim importApi As New Global.Vendor.Sdk.ImportAPI(userName, password, webServiceUrl)";
	internal const string OWN_CLASS = "Public Class ImportAPI\n\tPublic Sub New(a As String, b As String, c As String)\n\tEnd Sub\nEnd Class\n";
	internal const string OWN_STRUCTURE = "Public Structure ImportAPI\n\tPublic Sub New(a As String, b As String, c As String)\n\tEnd Sub\nEnd Structure\n";

	private static readonly int _fillerLinesBeyondTheReach = (_DECLARATION_REACH_IN_CHARACTERS / _FILLER_LINE.Length) + _MARGIN_LINES_BEYOND_THE_REACH;

	/// <summary>Enough filler lines to put a declaration beyond the characters that the declaration look-around reaches each side of a construction.</summary>
	internal static string FillerBeyondTheReach()
	{
		return string.Concat(Enumerable.Repeat(_FILLER_LINE, _fillerLinesBeyondTheReach));
	}

	/// <summary>
	/// The class of the project, then filler, then the construction, with exactly <paramref name="distanceInCharacters"/> characters between the end of the declared name and the
	/// start of the name at the use: the distance the look-behind measures.
	/// </summary>
	internal static string OwnClassAtADistanceBeforeTheUse(int distanceInCharacters)
	{
		int tailOfTheDeclaration = OWN_CLASS.Length - (OWN_CLASS.IndexOf(_SPELLING, StringComparison.Ordinal) + _SPELLING.Length);
		int headOfTheUse = CONSTRUCTION.IndexOf(_SPELLING, StringComparison.Ordinal);

		return $"{OWN_CLASS}{FillerOf(distanceInCharacters - tailOfTheDeclaration - headOfTheUse)}{CONSTRUCTION}";
	}

	/// <summary>
	/// The construction, then filler, then the class of the project, with exactly <paramref name="distanceInCharacters"/> characters between the start of the name at the use and the
	/// start of the declaration: the distance the look-ahead measures.
	/// </summary>
	internal static string OwnClassAtADistanceAfterTheUse(int distanceInCharacters)
	{
		int tailOfTheUse = CONSTRUCTION.Length - CONSTRUCTION.IndexOf(_SPELLING, StringComparison.Ordinal);
		int headOfTheDeclaration = OWN_CLASS.IndexOf(_DECLARATION_KEYWORD, StringComparison.Ordinal);

		return $"{CONSTRUCTION}{FillerOf(distanceInCharacters - tailOfTheUse - headOfTheDeclaration)}{OWN_CLASS}";
	}

	/// <summary>Filler of exactly the given length: whole filler lines, then spaces for the remainder.</summary>
	private static string FillerOf(int lengthInCharacters)
	{
		int wholeLines = lengthInCharacters / _FILLER_LINE.Length;
		int remainder = lengthInCharacters % _FILLER_LINE.Length;

		return string.Concat(Enumerable.Repeat(_FILLER_LINE, wholeLines)) + new string(' ', remainder);
	}

	/// <summary>The class of the project, then filler beyond the reach, then the construction.</summary>
	internal static string OwnClassFarBeforeTheUse()
	{
		return $"{OWN_CLASS}{FillerBeyondTheReach()}{CONSTRUCTION}";
	}

	/// <summary>The construction, then filler beyond the reach, then the class of the project.</summary>
	internal static string OwnClassFarAfterTheUse()
	{
		return $"{CONSTRUCTION}\n{FillerBeyondTheReach()}{OWN_CLASS}";
	}
}
