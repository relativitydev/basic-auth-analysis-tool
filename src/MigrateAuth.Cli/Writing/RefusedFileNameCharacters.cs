namespace MigrateAuth.Cli.Writing;

/// <summary>
/// The one set of characters a bare file name may not contain, the same on every platform: the Windows set
/// (<c>&lt; &gt; : " / \ | ? *</c> and the control characters U+0000 to U+001F). It is fixed rather than taken from
/// the running operating system, because a name accepted on Linux must still be a name Windows can hold.
/// </summary>
internal static class RefusedFileNameCharacters
{
	private const char _LAST_CONTROL_CHARACTER = '\u001F';
	private const string _PUNCTUATION = "<>:\"/\\|?*";

	/// <summary>Every refused character.</summary>
	internal static readonly char[] ALL = [.. _PUNCTUATION, .. Enumerable.Range(0, _LAST_CONTROL_CHARACTER + 1).Select(code => (char)code)];
}
