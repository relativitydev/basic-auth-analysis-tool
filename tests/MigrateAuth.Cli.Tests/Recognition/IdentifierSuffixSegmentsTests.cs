using System.Collections.Immutable;

using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE SEGMENT READING OF A CREDENTIAL WORD'S OCCURRENCE, on its own: where an occurrence ends, and whether the
/// next identifier word segment is one of the closed suffixes, compared whole and ignoring letter case. The names are generic.
/// </summary>
public sealed class IdentifierSuffixSegmentsTests
{
	private static readonly ImmutableArray<string> _SUFFIXES = ["Id", "Ids", "Identifier", "Guid", "Uuid", "Number", "Index", "Code"];

	[Theory]
	[InlineData("userId", 4, true)]
	[InlineData("user_id", 4, true)]
	[InlineData("user.Id", 4, true)]
	[InlineData("user . Id", 4, true)]
	[InlineData("userID", 4, true)]
	[InlineData("userIDValue", 4, true)]
	[InlineData("USER_GUID", 4, true)]
	[InlineData("ACCOUNTIdValue", 7, true)]
	[InlineData("accountIds", 7, true)]
	[InlineData("accountIdentifier", 7, true)]
	[InlineData("loginUuid", 5, true)]
	[InlineData("tokenNumber", 5, true)]
	[InlineData("tokenIndex", 5, true)]
	[InlineData("passwordCode", 8, true)]
	[InlineData("apiKeyId", 6, true)]
	[InlineData("userName", 4, false)]
	[InlineData("userIdentity", 4, false)]
	[InlineData("userCodec", 4, false)]
	[InlineData("userIndexer", 4, false)]
	[InlineData("userNumbered", 4, false)]
	[InlineData("userId2", 4, false)]
	[InlineData("userid", 4, false)]
	[InlineData("USERID", 4, false)]
	[InlineData("accountsId", 7, false)]
	[InlineData("user", 4, false)]
	[InlineData("user_", 4, false)]
	[InlineData("user.", 4, false)]
	[InlineData("userValueId", 4, false)]
	[InlineData("userIdValueId", 4, true)]
	public void AnOccurrenceIsFollowedByASuffixOnlyWhenItEndsASegmentAndTheNextSegmentIsOneOfTheSuffixesWhole(string text, int wordEnd, bool expected)
	{
		Assert.Equal(expected, IdentifierSuffixSegments.IsDirectlyFollowedByASuffix(text, wordEnd, _SUFFIXES));
	}

	[Fact]
	public void NoDeclaredSuffixMeansNoOccurrenceIsEverFollowedByOne()
	{
		Assert.False(IdentifierSuffixSegments.IsDirectlyFollowedByASuffix("userId", 4, []));
	}
}
