using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// The anchor position: the offset within the match whose
/// zone decides classification. The engine tests that position, not the start of the match:
/// a rule recognizing a header added as a string starts on the opening quote, and with the
/// whole literal marked non-code that rule could otherwise never emit a finding.
///
/// The position is expressed as a named capture group rather than as an integer, which is a
/// deliberate part of keeping numbering out of the analyzer's reach: an analyzer names a
/// place in its own expression, and only the engine turns a name into a number.
/// </summary>
public readonly record struct MatchAnchor
{
	private const int _UNRESOLVED_OFFSET = -1;

	private MatchAnchor(string? captureGroupName)
	{
		CaptureGroupName = captureGroupName;
	}

	/// <summary>The group naming the anchor, or null when the anchor is the match start.</summary>
	public string? CaptureGroupName { get; }

	public bool IsMatchStart => CaptureGroupName is null;

	/// <summary>The anchor is the start of the match. Also the value of <c>default</c>.</summary>
	public static MatchAnchor AtMatchStart()
	{
		MatchAnchor anchor = new(captureGroupName: null);

		return anchor;
	}

	public static MatchAnchor AtCaptureGroup(string captureGroupName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(captureGroupName);

		MatchAnchor anchor = new(captureGroupName);

		return anchor;
	}

	/// <summary>
	/// Turns the declared anchor into an absolute offset in the analyzed text. This is engine
	/// work: the analyzer declares a name and never computes a position.
	/// </summary>
	public bool TryResolveOffset(Match match, out int offset)
	{
		ArgumentNullException.ThrowIfNull(match);

		bool resolved = false;
		offset = _UNRESOLVED_OFFSET;

		if (match.Success && CaptureGroupName is string anchorGroupName)
		{
			Group anchorGroup = match.Groups[anchorGroupName];

			if (anchorGroup.Success)
			{
				offset = anchorGroup.Index;
				resolved = true;
			}
		}
		else if (match.Success)
		{
			offset = match.Index;
			resolved = true;
		}

		return resolved;
	}
}

/// <summary>
/// The credential position, optional and defaulting to none, which
/// is the honest answer for a class where no argument is the credential.
///
/// It is not the anchor offset, and the two are separate fields because a construct's anchor
/// and its credential sit at different places: a credential type is anchored at its type name
/// and carries its credential at argument two. The literal-credential signal reads this
/// position's zone, never the text at it.
/// </summary>
public readonly record struct CredentialPosition
{
	private const int _UNRESOLVED_OFFSET = -1;

	private CredentialPosition(string? captureGroupName)
	{
		CaptureGroupName = captureGroupName;
	}

	/// <summary>The group naming the credential argument, or null when none is declared.</summary>
	public string? CaptureGroupName { get; }

	public bool IsDeclared => CaptureGroupName is not null;

	/// <summary>No argument of this class is the credential. Also the value of <c>default</c>.</summary>
	public static CredentialPosition None()
	{
		CredentialPosition position = new(captureGroupName: null);

		return position;
	}

	public static CredentialPosition AtCaptureGroup(string captureGroupName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(captureGroupName);

		CredentialPosition position = new(captureGroupName);

		return position;
	}

	/// <summary>Turns the declared credential position into an absolute offset. Engine work.</summary>
	public bool TryResolveOffset(Match match, out int offset)
	{
		ArgumentNullException.ThrowIfNull(match);

		bool resolved = false;
		offset = _UNRESOLVED_OFFSET;

		if (match.Success && CaptureGroupName is string credentialGroupName)
		{
			Group credentialGroup = match.Groups[credentialGroupName];

			if (credentialGroup.Success)
			{
				offset = credentialGroup.Index;
				resolved = true;
			}
		}

		return resolved;
	}
}
