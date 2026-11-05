using System.Collections.Immutable;
using System.Globalization;

namespace MigrateAuth.Cli.Session;

/// <summary>What the command line yielded, with every rejected token kept rather than dropped.</summary>
public sealed record CommandLineParseResult
{
	/// <summary>The text supplied to the repository option, or null when it was not supplied.</summary>
	public string? RepositoryFolderPathText { get; init; }

	/// <summary>The text supplied to the destination option, or null when it was not supplied.</summary>
	public string? DocumentationDestinationFolderPathText { get; init; }

	/// <summary>
	/// Tokens the parser does not recognize, in the order they appeared. A bare positional
	/// token lands here too, which is how a transposition of the two paths stops being
	/// expressible: there is no position that means anything.
	/// </summary>
	public ImmutableArray<string> UnrecognizedTokens { get; init; } = [];

	/// <summary>Known options that were supplied with no value after them.</summary>
	public ImmutableArray<string> OptionsMissingValue { get; init; } = [];
}

/// <summary>
/// The named-option parser. The session has exactly two inputs and both arrive by name:
/// <c>--repositoryFolderPath</c> and <c>--documentationDestinationFolderPath</c>, in either
/// order, written either as two arguments or as <c>--name=value</c>.
///
/// Positional arguments are not accepted at all. Transposing two positional paths would silently aim
/// the output at the audited repository; with no positional form there is nothing to transpose, and
/// the write boundary remains a last line of defence.
/// </summary>
public static class CommandLineParser
{
	/// <summary>The product's own name, as it appears on the command line and in its own output.</summary>
	public const string PRODUCT_NAME = "migrate-auth";

	/// <summary>The named option carrying the folder to audit.</summary>
	public const string REPOSITORY_FOLDER_PATH_OPTION = "--repositoryFolderPath";

	/// <summary>The named option carrying the folder to write to.</summary>
	public const string DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION = "--documentationDestinationFolderPath";

	/// <summary>
	/// The long form of the launch option that prints usage and the command list, then exits
	/// without resolving a repository, reading configuration, running Pre-flight Validation,
	/// reading or writing session state, or opening a prompt.
	/// </summary>
	public const string HELP_OPTION = "--help";

	/// <summary>The short form of <see cref="HELP_OPTION"/>. Either answers the same request.</summary>
	public const string HELP_OPTION_SHORT_FORM = "-h";

	/// <summary>The launch option that prints the tool and catalog versions, then exits on the same terms as <see cref="HELP_OPTION"/>.</summary>
	public const string VERSION_OPTION = "--version";

	/// <summary>
	/// Reported for a known option supplied without a value. It is a different mistake from an
	/// unrecognized token - the name was right and the value was absent - so it says so rather
	/// than claiming the option does not exist.
	/// </summary>
	public const string MISSING_OPTION_VALUE_MESSAGE_FORMAT = "no value supplied for: {0}";

	private const char _OPTION_VALUE_SEPARATOR = '=';
	private const string _OPTION_NAME_PREFIX = "--";
	private const int _ARGUMENTS_CONSUMED_BY_ONE_TOKEN = 1;
	private const int _ARGUMENTS_CONSUMED_BY_A_NAME_AND_ITS_VALUE = 2;

	/// <summary>The usage line, built from the option names so that it cannot contradict them.</summary>
	public static string UsageLine =>
		$"Usage: {PRODUCT_NAME} {REPOSITORY_FOLDER_PATH_OPTION} <path> [{DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION} <path>]";

	/// <summary>Parses the process arguments. Touches no filesystem and throws for no input.</summary>
	public static CommandLineParseResult Parse(IReadOnlyList<string> arguments)
	{
		ArgumentNullException.ThrowIfNull(arguments);

		Dictionary<string, string> valuesByOptionName = new(StringComparer.OrdinalIgnoreCase);
		ImmutableArray<string>.Builder unrecognizedTokens = ImmutableArray.CreateBuilder<string>();
		ImmutableArray<string>.Builder optionsMissingValue = ImmutableArray.CreateBuilder<string>();
		int position = 0;

		while (position < arguments.Count)
		{
			string argument = arguments[position];
			OptionName optionName = ReadOptionName(argument);
			int consumedArgumentCount = _ARGUMENTS_CONSUMED_BY_ONE_TOKEN;

			if (optionName.KnownName is null)
			{
				unrecognizedTokens.Add(ConsoleTextSafety.StripUnsafeCharacters(argument));
			}
			else
			{
				string? suppliedValue = optionName.InlineValue ?? ReadFollowingValue(arguments, position);

				if (suppliedValue is null)
				{
					optionsMissingValue.Add(optionName.KnownName);
				}
				else
				{
					valuesByOptionName[optionName.KnownName] = suppliedValue;
					consumedArgumentCount = optionName.InlineValue is null
						? _ARGUMENTS_CONSUMED_BY_A_NAME_AND_ITS_VALUE
						: _ARGUMENTS_CONSUMED_BY_ONE_TOKEN;
				}
			}

			position += consumedArgumentCount;
		}

		return BuildResult(valuesByOptionName, unrecognizedTokens.ToImmutable(), optionsMissingValue.ToImmutable());
	}

	/// <summary>
	/// Reads the repository option without touching the filesystem, so that the session
	/// exception boundary can be handed a root to relativize against before anything that can
	/// fail has run.
	/// </summary>
	public static string? PeekRepositoryFolderPath(IReadOnlyList<string> arguments) =>
		Parse(arguments).RepositoryFolderPathText;

	/// <summary>
	/// Whether any launch argument is exactly <see cref="HELP_OPTION"/> or
	/// <see cref="HELP_OPTION_SHORT_FORM"/>, outside a position <see cref="Parse"/> would consume
	/// as the value of one of the two named options - see <see cref="ContainsFlagOutsideOptionValues"/>.
	/// Checked independently of <see cref="Parse"/>'s result, because a launch flag answered before
	/// a session exists is not one of the two named options and must not depend on how they are
	/// read; it is, however, position-sensitive on the same terms <see cref="Parse"/> itself reads
	/// argument positions, so a token like <c>-h</c> that landed where a value was expected is not
	/// mistaken for the flag.
	/// </summary>
	public static bool RequestsHelp(IReadOnlyList<string> arguments)
	{
		ArgumentNullException.ThrowIfNull(arguments);

		return ContainsFlagOutsideOptionValues(arguments, HELP_OPTION, HELP_OPTION_SHORT_FORM);
	}

	/// <summary>
	/// Whether any launch argument is exactly <see cref="VERSION_OPTION"/>, on the same
	/// value-position-aware terms as <see cref="RequestsHelp"/>.
	/// </summary>
	public static bool RequestsVersion(IReadOnlyList<string> arguments)
	{
		ArgumentNullException.ThrowIfNull(arguments);

		return ContainsFlagOutsideOptionValues(arguments, VERSION_OPTION);
	}

	/// <summary>Builds the message for a known option supplied with no value.</summary>
	public static string FormatMissingOptionValueMessage(string optionName) =>
		string.Format(CultureInfo.InvariantCulture, MISSING_OPTION_VALUE_MESSAGE_FORMAT, optionName);

	private static CommandLineParseResult BuildResult(
		Dictionary<string, string> valuesByOptionName,
		ImmutableArray<string> unrecognizedTokens,
		ImmutableArray<string> optionsMissingValue)
	{
		valuesByOptionName.TryGetValue(REPOSITORY_FOLDER_PATH_OPTION, out string? repositoryText);
		valuesByOptionName.TryGetValue(DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION, out string? destinationText);

		return new CommandLineParseResult
		{
			RepositoryFolderPathText = repositoryText,
			DocumentationDestinationFolderPathText = destinationText,
			UnrecognizedTokens = unrecognizedTokens,
			OptionsMissingValue = optionsMissingValue
		};
	}

	/// <summary>
	/// Walks the arguments exactly as <see cref="Parse"/> does - skipping
	/// past a known option's value the same way, one or two tokens at a time - so a bare flag name
	/// is recognized only at a position <see cref="Parse"/> itself would read as a token rather than
	/// as a supplied value. <c>--repositoryFolderPath -h</c> therefore never matches: <c>-h</c> sits
	/// where <see cref="ReadFollowingValue"/> reads the repository path from, and is skipped along
	/// with the option name that claimed it.
	/// </summary>
	private static bool ContainsFlagOutsideOptionValues(IReadOnlyList<string> arguments, params string[] flagForms)
	{
		bool found = false;
		int position = 0;

		while (position < arguments.Count && !found)
		{
			string argument = arguments[position];
			OptionName optionName = ReadOptionName(argument);
			int consumedArgumentCount = _ARGUMENTS_CONSUMED_BY_ONE_TOKEN;

			if (optionName.KnownName is null)
			{
				found = Array.IndexOf(flagForms, argument) >= 0;
			}
			else
			{
				string? suppliedValue = optionName.InlineValue ?? ReadFollowingValue(arguments, position);

				consumedArgumentCount = suppliedValue is not null && optionName.InlineValue is null
					? _ARGUMENTS_CONSUMED_BY_A_NAME_AND_ITS_VALUE
					: _ARGUMENTS_CONSUMED_BY_ONE_TOKEN;
			}

			position += consumedArgumentCount;
		}

		return found;
	}

	/// <summary>
	/// The value in the argument that follows, or null when there is none or when what follows
	/// is itself an option name. The second case is what keeps a forgotten value from silently
	/// eating the next option.
	/// </summary>
	private static string? ReadFollowingValue(IReadOnlyList<string> arguments, int position)
	{
		int followingPosition = position + 1;
		bool followingArgumentIsAValue =
			followingPosition < arguments.Count
			&& !arguments[followingPosition].StartsWith(_OPTION_NAME_PREFIX, StringComparison.Ordinal);

		return followingArgumentIsAValue ? arguments[followingPosition] : null;
	}

	private static OptionName ReadOptionName(string argument)
	{
		int separatorIndex = argument.IndexOf(_OPTION_VALUE_SEPARATOR, StringComparison.Ordinal);
		string writtenName = separatorIndex < 0 ? argument : argument[..separatorIndex];
		string? inlineValue = separatorIndex < 0 ? null : argument[(separatorIndex + 1)..];
		string? knownName = MatchKnownOptionName(writtenName);

		return new OptionName(knownName, string.IsNullOrEmpty(inlineValue) ? null : inlineValue);
	}

	private static string? MatchKnownOptionName(string writtenName)
	{
		string? knownName = null;

		if (string.Equals(writtenName, REPOSITORY_FOLDER_PATH_OPTION, StringComparison.OrdinalIgnoreCase))
		{
			knownName = REPOSITORY_FOLDER_PATH_OPTION;
		}
		else if (string.Equals(writtenName, DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION, StringComparison.OrdinalIgnoreCase))
		{
			knownName = DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION;
		}

		return knownName;
	}

	/// <summary>An argument read as a known option name and an inline value, either of which may be absent.</summary>
	private readonly record struct OptionName(string? KnownName, string? InlineValue);
}
