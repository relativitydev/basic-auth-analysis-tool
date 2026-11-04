using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.Release;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>One link found in a document: where it points, exactly as written.</summary>
internal sealed record DocumentLink(string Target);

/// <summary>
/// THE LINKS OF THE PUBLIC SET, CHECKED OFFLINE.
///
/// Every internal link of the README and of the User Guide must resolve to a file inside
/// <c>docs/public/</c>, and where the link names an anchor the anchor must exist in that file. An internal
/// link is a relative one; an absolute <c>http</c> or <c>https</c> link is not followed, because the check
/// runs offline, but it is required to point at a Relativity documentation host, so that nothing else is
/// linked from the guide by accident. Nothing in the public set may link into <c>docs</c> internal folders or any file the public set leaves out: the public set is <c>docs/public/</c> only.
///
/// Anchors follow GitHub's rule for a heading: lower-case, punctuation other than hyphens and underscores
/// removed, spaces to hyphens, and a numeric suffix for a heading that repeats. An explicit
/// <c>&lt;a id="..."&gt;</c> is an anchor too.
/// </summary>
public sealed class PublicLinkTests
{
	private const string _INLINE_LINK_PATTERN = "(?<!\\!)\\[[^\\]\\n]*\\]\\((?<target>[^)\\s]+)(?:\\s+\"[^\"]*\")?\\)";
	private const string _REFERENCE_DEFINITION_PATTERN = "^\\[[^\\]\\n]+\\]:\\s*(?<target>\\S+)";
	private const string _HEADING_PATTERN = "^#{1,6}\\s+(?<text>.+?)\\s*#*\\s*$";
	private const string _EXPLICIT_ANCHOR_PATTERN = "<a\\s+id=\"(?<id>[^\"]+)\"";
	private const string _SLUG_REMOVED_CHARACTERS_PATTERN = "[^\\p{L}\\p{N}\\p{M} _-]";
	private const string _FENCE_MARKER = "```";
	private const string _EXTERNAL_PREFIX_HTTP = "http://";
	private const string _EXTERNAL_PREFIX_HTTPS = "https://";
	private const string _MAIL_PREFIX = "mailto:";
	private const char _FRAGMENT_SEPARATOR = '#';
	private const string _MARKDOWN_EXTENSION = ".md";
	private const string _HYPHEN = "-";
	private const string _SPACE = " ";
	private const string _PARENT_PREFIX = "..";
	private const char _WINDOWS_SEPARATOR = '\\';
	private const char _URL_SEPARATOR = '/';
	private const char _LINE_BREAK = '\n';
	private const string _NOT_PUBLIC_PATH_MESSAGE = "{0}: the link '{1}' leaves docs/public.";
	private const string _NO_FILE_MESSAGE = "{0}: the link '{1}' names no file.";
	private const string _NO_ANCHOR_MESSAGE = "{0}: the link '{1}' names an anchor that does not exist in {2}.";
	private const string _EXTERNAL_HOST_MESSAGE = "{0}: the external link '{1}' is not on a Relativity documentation host.";
	private const string _SELF_CHECK_SOURCE_NAME = "synthetic.md";
	private const string _SELF_CHECK_TEXT =
		"# Title\n\n## Second heading, with punctuation\n\n[ok](#second-heading-with-punctuation) [bad](#nowhere) [gone](missing.md) [out](../../v1/prd.md)\n\n```text\n[ignored](missing.md)\n```\n";

	private static readonly string[] _relativityHosts = ["help.relativity.com", "platform.relativity.com"];
	private static readonly ImmutableArray<string> _forbiddenTargetFragments = PublicSourceIdentifierTests.InternalLinkTargetFragments();

	private static readonly Regex _inlineLink = new(_INLINE_LINK_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _referenceDefinition = new(_REFERENCE_DEFINITION_PATTERN, RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.ExplicitCapture);
	private static readonly Regex _heading = new(_HEADING_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _explicitAnchor = new(_EXPLICIT_ANCHOR_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _slugRemovedCharacters = new(_SLUG_REMOVED_CHARACTERS_PATTERN, RegexOptions.CultureInvariant);

	/// <summary>Every link of the README resolves inside the public set.</summary>
	[Fact]
	public void EveryLinkOfTheReadmeResolvesInsideThePublicSet()
	{
		Assert.Empty(ProblemsOf(PublicDocuments.ReadmePath()));
		Assert.NotEmpty(LinksOf(File.ReadAllText(PublicDocuments.ReadmePath())));
	}

	/// <summary>Every link of the User Guide, the index and the eight rows, resolves inside the public set.</summary>
	[Fact]
	public void EveryLinkOfTheUserGuideResolvesInsideThePublicSet()
	{
		string[] problems = [.. PublicDocuments.UserGuidePaths().SelectMany(ProblemsOf)];

		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The guide has the index and the eight rows, one file each, and the README links to the guide and to the Migration Documentation.</summary>
	[Fact]
	public void TheGuideHasOneFilePerRowAndTheReadmeLinksToTheGuideAndTheMigrationDocumentation()
	{
		string readme = File.ReadAllText(PublicDocuments.ReadmePath());

		Assert.All(PublicDocuments.UserGuidePaths(), path => Assert.True(File.Exists(path), $"{path} is missing."));
		Assert.Equal(PublicDocuments.UserGuideRowCount() + 1, PublicDocuments.UserGuidePaths().Length);
		Assert.Contains("user-guide/index.md", readme, StringComparison.Ordinal);
		Assert.Contains("migration/part-a-csharp.md", readme, StringComparison.Ordinal);
	}

	/// <summary>Every internal link of every Markdown file of the public set, the Migration Documentation included, resolves.</summary>
	[Fact]
	public void EveryInternalLinkOfThePublicSetResolves()
	{
		string[] problems = [.. PublicDocuments.AllPublicMarkdownPaths().SelectMany(InternalProblemsOf)];

		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The link check itself fails on a missing anchor, a missing file and a link that leaves the public set, and ignores a fenced example.</summary>
	[Fact]
	public void TheLinkCheckReportsAMissingAnchorAMissingFileAndALinkOutOfThePublicSet()
	{
		string sourcePath = Path.Combine(PublicDocuments.PublicFolder(), _SELF_CHECK_SOURCE_NAME);

		string[] problems = [.. InternalProblemsIn(sourcePath, _SELF_CHECK_TEXT)];

		Assert.Equal(3, problems.Length);
		Assert.Contains(problems, problem => problem.Contains("#nowhere", StringComparison.Ordinal));
		Assert.Contains(problems, problem => problem.Contains("missing.md", StringComparison.Ordinal) && problem.Contains("names no file", StringComparison.Ordinal));
		Assert.Contains(problems, problem => problem.Contains("../../v1/prd.md", StringComparison.Ordinal));
	}

	private static string[] ProblemsOf(string path)
	{
		string text = File.ReadAllText(path);

		return [.. InternalProblemsIn(path, text), .. LinksOf(text).Select(link => ExternalProblem(path, link)).OfType<string>()];
	}

	private static string[] InternalProblemsOf(string path)
	{
		return [.. InternalProblemsIn(path, File.ReadAllText(path))];
	}

	private static ImmutableArray<string> InternalProblemsIn(string sourcePath, string text)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		string sourceName = Path.GetRelativePath(PublicDocuments.PublicFolder(), sourcePath).Replace(_WINDOWS_SEPARATOR, _URL_SEPARATOR);

		foreach (DocumentLink link in LinksOf(text).Where(candidate => !IsExternal(candidate.Target)))
		{
			string? problem = InternalProblem(sourcePath, sourceName, text, link);

			if (problem is not null)
			{
				problems.Add(problem);
			}
		}

		return problems.ToImmutable();
	}

	private static string? InternalProblem(string sourcePath, string sourceName, string sourceText, DocumentLink link)
	{
		int fragmentIndex = link.Target.IndexOf(_FRAGMENT_SEPARATOR, StringComparison.Ordinal);
		string pathPart = fragmentIndex < 0 ? link.Target : link.Target[..fragmentIndex];
		string fragment = fragmentIndex < 0 ? string.Empty : link.Target[(fragmentIndex + 1)..];
		string targetPath = pathPart.Length == 0
			? sourcePath
			: Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, Uri.UnescapeDataString(pathPart)));
		string? problem = null;

		if (!IsInsidePublicSet(targetPath) || _forbiddenTargetFragments.Any(fragmentText => link.Target.Contains(fragmentText, StringComparison.Ordinal)))
		{
			problem = string.Format(CultureInfo.InvariantCulture, _NOT_PUBLIC_PATH_MESSAGE, sourceName, link.Target);
		}
		else if (pathPart.Length > 0 && !File.Exists(targetPath))
		{
			problem = string.Format(CultureInfo.InvariantCulture, _NO_FILE_MESSAGE, sourceName, link.Target);
		}
		else if (fragment.Length > 0 && !AnchorsOf(pathPart.Length == 0 ? sourceText : File.ReadAllText(targetPath)).Contains(fragment))
		{
			problem = string.Format(CultureInfo.InvariantCulture, _NO_ANCHOR_MESSAGE, sourceName, link.Target, Path.GetFileName(targetPath));
		}

		return problem;
	}

	private static string? ExternalProblem(string sourcePath, DocumentLink link)
	{
		string? problem = null;

		if (IsExternal(link.Target) && !link.Target.StartsWith(_MAIL_PREFIX, StringComparison.Ordinal)
			&& !_relativityHosts.Contains(new Uri(link.Target).Host, StringComparer.OrdinalIgnoreCase))
		{
			problem = string.Format(
				CultureInfo.InvariantCulture,
				_EXTERNAL_HOST_MESSAGE,
				Path.GetRelativePath(PublicDocuments.PublicFolder(), sourcePath).Replace(_WINDOWS_SEPARATOR, _URL_SEPARATOR),
				link.Target);
		}

		return problem;
	}

	private static bool IsExternal(string target)
	{
		return target.StartsWith(_EXTERNAL_PREFIX_HTTP, StringComparison.OrdinalIgnoreCase)
			|| target.StartsWith(_EXTERNAL_PREFIX_HTTPS, StringComparison.OrdinalIgnoreCase)
			|| target.StartsWith(_MAIL_PREFIX, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsInsidePublicSet(string fullPath)
	{
		string relative = Path.GetRelativePath(PublicDocuments.PublicFolder(), fullPath);

		return !relative.StartsWith(_PARENT_PREFIX, StringComparison.Ordinal) && !Path.IsPathRooted(relative);
	}

	/// <summary>The links of a document outside its fenced blocks: inline links and reference definitions.</summary>
	private static ImmutableArray<DocumentLink> LinksOf(string text)
	{
		string outsideFences = OutsideFences(text);

		return
		[
			.. _inlineLink.Matches(outsideFences).Select(match => new DocumentLink(match.Groups["target"].Value)),
			.. _referenceDefinition.Matches(outsideFences).Select(match => new DocumentLink(match.Groups["target"].Value))
		];
	}

	private static string OutsideFences(string text)
	{
		List<string> kept = [];
		bool insideFence = false;

		foreach (string line in text.Split(_LINE_BREAK))
		{
			if (line.TrimStart().StartsWith(_FENCE_MARKER, StringComparison.Ordinal))
			{
				insideFence = !insideFence;
			}
			else if (!insideFence)
			{
				kept.Add(line);
			}
		}

		return string.Join(_LINE_BREAK, kept);
	}

	private static HashSet<string> AnchorsOf(string text)
	{
		HashSet<string> anchors = [];
		Dictionary<string, int> repeats = [];

		foreach (string line in OutsideFences(text).Split(_LINE_BREAK))
		{
			Match heading = _heading.Match(line);

			if (heading.Success)
			{
				anchors.Add(UniqueSlug(Slug(heading.Groups["text"].Value), repeats));
			}

			foreach (Match anchor in _explicitAnchor.Matches(line))
			{
				anchors.Add(anchor.Groups["id"].Value);
			}
		}

		return anchors;
	}

	private static string Slug(string headingText)
	{
		string withoutMarkup = _slugRemovedCharacters.Replace(headingText.ToLowerInvariant(), string.Empty);

		return withoutMarkup.Replace(_SPACE, _HYPHEN, StringComparison.Ordinal);
	}

	private static string UniqueSlug(string slug, Dictionary<string, int> repeats)
	{
		repeats.TryGetValue(slug, out int seen);
		repeats[slug] = seen + 1;

		return seen == 0 ? slug : $"{slug}{_HYPHEN}{seen}";
	}
}
