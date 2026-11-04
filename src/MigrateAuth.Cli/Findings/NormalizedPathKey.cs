using MigrateAuth.Cli.Emission;

namespace MigrateAuth.Cli.Findings;

/// <summary>
/// THE FIRST ORDERING TERM, AND THE FIRST FINGERPRINT COMPONENT: the relative path
/// with its separators normalized to forward slashes, and nothing else.
///
/// IT IS ONE TYPE BECAUSE THE TWO USES MUST NOT DIVERGE. The ordering key and the fingerprint
/// each name "the normalized relative path", and if each computed its own the day one of them
/// gained a step would be the day a finding sorted under one path and was identified under
/// another. There is one key and both read it. The global findings cap is a third reader: it
/// admits files in this key's order, so the set that survives the cap and the order it is
/// emitted in are one key rather than two.
///
/// THE ONLY WAY IN IS A <see cref="RepositoryRelativePath"/>. That type exists because the data
/// invariant's single exemption is a hole shaped exactly like a string, and a key that accepted
/// arbitrary text would re-open the hole one level down - an ordering key is emitted material,
/// since the path and the identifiers the ordering generates are published.
///
/// SEPARATOR NORMALIZATION HAPPENS UPSTREAM, AND IS ALL THAT THIS KEY GUARANTEES. The path is
/// the one the platform returned, with its separators rewritten to the portable form by
/// <see cref="RepositoryRelativePath"/>. No Unicode normalization is applied: a
/// name the platform returns in decomposed form keys, sorts, is identified and is published in
/// decomposed form, and the same name returned composed does the same in composed form. The
/// cross-platform guarantee is therefore separator normalization plus ordinal comparison, and
/// the limit is declared rather than hidden. Under <c>InvariantGlobalization</c> the platform's
/// normalization call is a no-op, so applying it would make a test host and the shipped binary
/// key paths differently.
///
/// TWO RAW PATHS CAN STILL MAP TO ONE KEY in principle, which is why the order key and the
/// fingerprint carry a raw-ordinal tie-break and a disambiguator - the raw path being
/// <see cref="RepositoryRelativePath.Value"/>, the path as the platform returned it. Today no two
/// paths the product builds share a key, so the tie-break and the disambiguator never fire.
/// </summary>
public readonly record struct NormalizedPathKey
{
	private NormalizedPathKey(string value)
	{
		Value = value;
	}

	/// <summary>The key text. Empty on <c>default</c>.</summary>
	public string Value { get; }

	/// <summary>Whether this key names a file at all.</summary>
	public bool IsDeclared => !string.IsNullOrEmpty(Value);

	/// <summary>The key for a path the write boundary already accepted.</summary>
	public static NormalizedPathKey From(RepositoryRelativePath path)
	{
		return new NormalizedPathKey(path.Value ?? string.Empty);
	}
}
