namespace MigrateAuth.Cli.Configuration;

/// <summary>What <see cref="OutputPathHomeExpansion.Expand"/> decided about one configured output path.</summary>
public enum OutputPathExpansionOutcome
{
	/// <summary>The text does not start with <c>~</c>, so it is returned untouched.</summary>
	NotApplicable = 0,

	/// <summary>A leading <c>~</c>, <c>~/</c> or <c>~\</c> was replaced by the user's home folder.</summary>
	Expanded = 1,

	/// <summary>The text starts with <c>~</c> and the home folder could not be resolved.</summary>
	HomeFolderUnresolved = 2,

	/// <summary>The text is a <c>~user</c> form, which is not supported.</summary>
	OtherUserFormRefused = 3
}

/// <summary>The decision and the text to canonicalize. <see cref="Text"/> is the input unchanged unless expanded.</summary>
public readonly record struct OutputPathExpansion(OutputPathExpansionOutcome Outcome, string Text);

/// <summary>
/// A leading <c>~</c> in the <c>outputPath</c> of the USER PROFILE configuration file.
///
/// The shell expands a <c>~</c> typed on the command line, so the command-line value is never passed through
/// here; a configuration file has no shell, and without this the text would become the working folder followed
/// by a folder named <c>~</c>. The home folder is supplied by the caller as a delegate so that tests never
/// change the process environment; <see cref="ReadUserProfileFolder"/> is the production source. Expansion
/// happens BEFORE canonicalization, so the write-boundary comparison sees the expanded folder.
///
/// Only the three forms <c>~</c>, <c>~/</c> and <c>~\</c> (followed by anything) are expanded. <c>~user</c> forms
/// are refused, and so is a home folder that cannot be resolved: both end the run with exit code 4. The
/// repository's own <c>outputPath</c> is never read, and <c>repositoryFolderPath</c> is not expanded.
/// </summary>
public static class OutputPathHomeExpansion
{
	private const char _HOME_MARKER = '~';
	private static readonly char[] _SEPARATORS = ['/', '\\'];

	/// <summary>The production home provider: the user's profile folder, empty where the platform has none.</summary>
	public static string ReadUserProfileFolder() =>
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

	/// <summary>Decides and, for a <c>~</c> form with a resolvable home, expands.</summary>
	public static OutputPathExpansion Expand(string configuredText, Func<string> homeFolderProvider)
	{
		ArgumentNullException.ThrowIfNull(configuredText);
		ArgumentNullException.ThrowIfNull(homeFolderProvider);

		OutputPathExpansion expansion = new(OutputPathExpansionOutcome.NotApplicable, configuredText);

		if (configuredText.Length > 0 && configuredText[0] == _HOME_MARKER)
		{
			string afterMarker = configuredText[1..];

			expansion = afterMarker.Length == 0 || _SEPARATORS.Contains(afterMarker[0])
				? ExpandOwnHomeForm(configuredText, afterMarker, homeFolderProvider())
				: new OutputPathExpansion(OutputPathExpansionOutcome.OtherUserFormRefused, configuredText);
		}

		return expansion;
	}

	private static OutputPathExpansion ExpandOwnHomeForm(string configuredText, string afterMarker, string? homeFolder)
	{
		string remainder = afterMarker.TrimStart(_SEPARATORS);
		OutputPathExpansion expansion;

		if (string.IsNullOrWhiteSpace(homeFolder))
		{
			expansion = new OutputPathExpansion(OutputPathExpansionOutcome.HomeFolderUnresolved, configuredText);
		}
		else
		{
			string expandedText = remainder.Length == 0 ? homeFolder : Path.Combine(homeFolder, remainder);

			expansion = new OutputPathExpansion(OutputPathExpansionOutcome.Expanded, expandedText);
		}

		return expansion;
	}
}
