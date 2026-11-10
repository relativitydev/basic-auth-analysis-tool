using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Scanning;

using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// The candidate test: the anchors, their case rules, and the reason the text is decoded
/// before any of it happens.
/// </summary>
public sealed class CandidatePreselectionTests
{
	private const string _EXTENSION_OF_THE_REFERENCE_PROFILE = ".ex1";
	private const string _CASE_SENSITIVE_ANCHOR = "NewExemplarCredentials";
	private const string _CASE_INSENSITIVE_ANCHOR = "Authorization";
	private const string _TEXT_WITH_NO_ANCHOR = "public sealed class Ordinary\n{\n}\n";

	[Fact]
	public void TextCarryingACaseSensitiveAnchorIsACandidate()
	{
		Assert.True(IsCandidate($"var credentials = {_CASE_SENSITIVE_ANCHOR}(user, secret);\n"));
	}

	/// <summary>
	/// The case rule is the ANCHOR's, not the profile's. This anchor is declared sensitive, so a
	/// differently cased occurrence is not one - and the profile it sits in declares its keywords
	/// case-insensitive, which is exactly the confusion the design avoids by keeping the two fields separate.
	/// </summary>
	[Fact]
	public void ACaseSensitiveAnchorDoesNotMatchAnotherCasing()
	{
		Assert.False(IsCandidate("var credentials = newexemplarcredentials(user, secret);\n"));
	}

	[Fact]
	public void ACaseInsensitiveAnchorMatchesEitherCasing()
	{
		Assert.True(IsCandidate($"headers['{_CASE_INSENSITIVE_ANCHOR.ToUpperInvariant()}'] = value;\n"));
		Assert.True(IsCandidate($"headers['{_CASE_INSENSITIVE_ANCHOR.ToLowerInvariant()}'] = value;\n"));
	}

	[Fact]
	public void TextWithNoAnchorIsNotACandidate()
	{
		Assert.False(IsCandidate(_TEXT_WITH_NO_ANCHOR));
	}

	[Fact]
	public void EmptyTextIsNotACandidate()
	{
		Assert.False(IsCandidate(string.Empty));
	}

	/// <summary>
	/// THE FAILURE THE DESIGN EXISTS TO PREVENT, REPRODUCED.
	///
	/// The same file is presented twice: once as the byte buffer transliterated one byte per
	/// character - which is what a preselection over raw bytes sees - and once as the decoded
	/// text. The transliteration contains no anchor at all, so the file never becomes a candidate,
	/// and NOTHING REPORTS IT: the file was read successfully, no counter fires, and its whole
	/// encoding's worth of the repository is silently never analyzed.
	///
	/// The assertion is the pair. Either half alone would pass for the wrong reason.
	/// </summary>
	[Fact]
	public void ATwoByteEncodedFileIsACandidateOnlyOnceItIsDecoded()
	{
		byte[] twoByteEncoded = new UnicodeEncoding(bigEndian: false, byteOrderMark: false)
			.GetBytes($"headers = @{{ {_CASE_INSENSITIVE_ANCHOR} = 'Basic ' }}\n");

		Assert.False(IsCandidate(Encoding.Latin1.GetString(twoByteEncoded)));
		Assert.True(IsCandidate(Encoding.Unicode.GetString(twoByteEncoded)));
	}

	private static bool IsCandidate(string decodedText)
	{
		LanguageCatalog catalog = ExemplarFixtureCatalog.Build();

		_ = catalog.TryGetAnalyzerForFileExtension(
			_EXTENSION_OF_THE_REFERENCE_PROFILE,
			out ILanguageAnalyzer? analyzer);

		return CandidatePreselection.IsCandidate(analyzer!, decodedText);
	}
}
