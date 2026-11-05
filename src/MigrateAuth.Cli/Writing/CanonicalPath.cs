namespace MigrateAuth.Cli.Writing;

/// <summary>
/// Path canonicalization, and the segment decomposition its comparison is built on.
///
/// Two things happen here and they are separate on purpose. <see cref="Resolve"/> turns
/// supplied text into an absolute path with links resolved along the whole path rather than
/// only at its last component - a destination reached through a directory link named anywhere
/// above it resolves to where it actually lands, which is the only form a containment
/// comparison can be trusted on. <see cref="SplitIntoSegments"/> then breaks a canonical path
/// into the units that comparison uses, because a string prefix test fails on case differences, on
/// sibling folders with overlapping names, on traversals, on short names, on drives mapped to shares,
/// and on directory links resolving inside the tree.
/// </summary>
public static class CanonicalPath
{
	/// <summary>
	/// How many times a link may be followed before resolution gives up and keeps what it has.
	/// A bound rather than cycle detection: a link loop is the case this exists for, and the
	/// cheapest correct answer to it is to stop rather than to diagnose it.
	/// </summary>
	private const int _MAXIMUM_LINK_HOPS = 16;

	private const string _ROOT_SEGMENT_FALLBACK = "/";

	/// <summary>
	/// A Windows junction records its target as an NT object path. The prefix is not something
	/// <see cref="Path.GetFullPath(string)"/> understands, so it is removed before the target is
	/// treated as a path - without it a junction resolves to nonsense and the comparison that
	/// follows is worthless.
	/// </summary>
	private const string _NT_OBJECT_PATH_PREFIX = @"\??\";

	private static readonly char[] _directorySeparators =
	[
		Path.DirectorySeparatorChar,
		Path.AltDirectorySeparatorChar
	];

	private static readonly char[] _wildcardCharacters = ['*', '?'];

	/// <summary>
	/// Resolves supplied text to an absolute path with every link component followed.
	///
	/// Resolution is best effort and never throws for a path that is simply not there: a
	/// destination that does not exist yet is the ordinary case at pre-flight, and it keeps its
	/// non-resolved absolute form. What the caller gets back is therefore "as canonical as the
	/// filesystem permitted", which is the correct input to a comparison that fails toward
	/// refusing.
	/// </summary>
	public static string Resolve(string suppliedText)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(suppliedText);

		string canonicalPath = Path.GetFullPath(suppliedText);

		for (int hop = 0; hop < _MAXIMUM_LINK_HOPS; hop++)
		{
			string? followed = FollowFirstLinkComponent(canonicalPath);

			if (followed is null || string.Equals(followed, canonicalPath, StringComparison.Ordinal))
			{
				break;
			}

			canonicalPath = followed;
		}

		return AdoptOnDiskNames(canonicalPath);
	}

	/// <summary>
	/// Replaces each component that exists with the name the filesystem actually holds for it.
	///
	/// This handles the short name among the things a string prefix gets wrong: the platform's own
	/// temporary folder is commonly handed out in its eight-dot-three form, so the same folder can arrive
	/// as <c>USERNA~1</c> from one source and in full from another. Two names for one directory
	/// compare as different segments and the boundary would permit a write it must refuse.
	/// Expanding them costs one directory lookup per component, once, at startup.
	///
	/// A component that does not exist, cannot be listed, or carries a wildcard keeps the text
	/// it arrived with - the last because the lookup pattern would then match a different entry
	/// on a platform where a wildcard is a legal character in a name.
	/// </summary>
	private static string AdoptOnDiskNames(string canonicalPath)
	{
		string[] segments = SplitIntoSegments(canonicalPath);
		string rebuiltPath = segments.Length == 0 ? canonicalPath : EnsureRootIsTraversable(segments[0]);

		for (int position = 1; position < segments.Length; position++)
		{
			rebuiltPath = Path.Combine(rebuiltPath, ReadOnDiskName(rebuiltPath, segments[position]));
		}

		return rebuiltPath;
	}

	private static string ReadOnDiskName(string parentPath, string declaredName)
	{
		string onDiskName = declaredName;

		if (declaredName.IndexOfAny(_wildcardCharacters) < 0)
		{
			try
			{
				string[] matches = [.. Directory.EnumerateFileSystemEntries(parentPath, declaredName)];

				if (matches.Length == 1)
				{
					onDiskName = Path.GetFileName(matches[0]);
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (ArgumentException)
			{
			}
		}

		return onDiskName;
	}

	/// <summary>
	/// Breaks a canonical path into its comparison units: the root first, where the platform
	/// has one, then each named component. Empty components are dropped, so a trailing
	/// separator and a doubled one change nothing.
	/// </summary>
	public static string[] SplitIntoSegments(string canonicalPath)
	{
		ArgumentNullException.ThrowIfNull(canonicalPath);

		string root = Path.GetPathRoot(canonicalPath) ?? string.Empty;
		string rootSegment = root.TrimEnd(_directorySeparators);

		if (rootSegment.Length == 0 && root.Length > 0)
		{
			rootSegment = _ROOT_SEGMENT_FALLBACK;
		}

		string remainder = canonicalPath[root.Length..];
		string[] namedSegments = remainder.Split(_directorySeparators, StringSplitOptions.RemoveEmptyEntries);
		string[] segments = rootSegment.Length == 0 ? namedSegments : [rootSegment, .. namedSegments];

		return segments;
	}

	/// <summary>
	/// Walks the path from its root and follows the first component that turns out to be a
	/// link, returning the rebuilt path, or null when no component is one. The caller loops,
	/// because the target may itself sit under another link.
	/// </summary>
	private static string? FollowFirstLinkComponent(string absolutePath)
	{
		string[] segments = SplitIntoSegments(absolutePath);
		string? rebuiltPath = null;
		string prefix = string.Empty;

		for (int position = 0; position < segments.Length && rebuiltPath is null; position++)
		{
			prefix = position == 0
				? EnsureRootIsTraversable(segments[0])
				: Path.Combine(prefix, segments[position]);

			if (ReadLinkTarget(prefix) is string linkTarget)
			{
				rebuiltPath = RebuildThroughLinkTarget(prefix, linkTarget, segments, position);
			}
		}

		return rebuiltPath;
	}

	/// <summary>
	/// A root segment loses its trailing separator during decomposition, and <c>C:</c> without
	/// one means "the working directory on C" rather than "the root of C". It is given back
	/// before the prefix is used as a path.
	/// </summary>
	private static string EnsureRootIsTraversable(string rootSegment)
	{
		string traversableRoot = rootSegment.EndsWith(Path.DirectorySeparatorChar)
			? rootSegment
			: $"{rootSegment}{Path.DirectorySeparatorChar}";

		return traversableRoot;
	}

	/// <summary>
	/// Reads a component's link target without following it, or null when the component is not
	/// a link, does not exist, or cannot be probed. A component that refuses to be probed is
	/// treated as not a link, which leaves the comparison working on the unresolved form - the
	/// direction that refuses a write rather than permitting one.
	/// </summary>
	private static string? ReadLinkTarget(string componentPath)
	{
		string? linkTarget;

		try
		{
			FileSystemInfo component = File.Exists(componentPath) && !Directory.Exists(componentPath)
				? new FileInfo(componentPath)
				: new DirectoryInfo(componentPath);

			linkTarget = component.LinkTarget;
		}
		catch (IOException)
		{
			linkTarget = null;
		}
		catch (UnauthorizedAccessException)
		{
			linkTarget = null;
		}
		catch (NotSupportedException)
		{
			linkTarget = null;
		}

		return linkTarget;
	}

	/// <summary>
	/// Replaces the link component with what it points at and reattaches everything below it.
	/// A relative target is resolved against the link's own parent, which is what the operating
	/// system does and what a target such as <c>../elsewhere</c> means.
	/// </summary>
	private static string RebuildThroughLinkTarget(
		string linkPath,
		string linkTarget,
		string[] segments,
		int linkPosition)
	{
		string linkParent = Path.GetDirectoryName(linkPath) ?? linkPath;
		string declaredTarget = linkTarget.StartsWith(_NT_OBJECT_PATH_PREFIX, StringComparison.Ordinal)
			? linkTarget[_NT_OBJECT_PATH_PREFIX.Length..]
			: linkTarget;
		string resolvedPrefix = Path.GetFullPath(declaredTarget, linkParent);
		string remainder = Path.Combine([.. segments[(linkPosition + 1)..]]);

		return Path.GetFullPath(Path.Combine(resolvedPrefix, remainder));
	}
}
