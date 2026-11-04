using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli;

/// <summary>
/// Process entry point. It establishes what has to hold before a prompt can exist - the two
/// named folder options, a usable repository, the effective configuration, Pre-flight
/// Validation, a writable destination and the session state - and then hands over to
/// <see cref="SessionShell"/>.
///
/// The exit code is last-condition-wins: whatever terminated the session names the
/// code, and a session that ended normally reports zero. Only four codes are reachable from
/// here and each describes a different thing: a repository that cannot be assessed, a
/// destination that cannot be written before anything was salvageable - including the
/// containment refusal at Pre-flight - a configuration that enables no language at all,
/// and the exception boundary. A gating violation and an unrecognized command return to the
/// prompt instead, so they terminate nothing and set nothing.
///
/// The process terminates by returning a code and never by an unhandled exception, and nothing
/// here calls Environment.FailFast - which is what keeps the operating system's crash reporter
/// away from a dying process whose buffers hold the file under analysis.
/// </summary>
internal static class Program
{
	private const string _IDENTIFICATION_LINE = "migrate-auth - Basic Authentication assessment CLI";
	private const string _CATALOG_VERSION_LABEL = "catalog";
	private const string _OPTION_LINE_INDENT = "  ";
	private const string _OPTION_DESCRIPTION_SEPARATOR = "  ";

	private const string _REPOSITORY_OPTION_DESCRIPTION_FORMAT =
		"{0} <path>{1}The folder to assess. Required.";

	private const string _DESTINATION_OPTION_DESCRIPTION_FORMAT =
		"{0} <path>{1}The folder the report and the data artifact are written to. Defaults to the outputPath in the user profile's configuration file, then to the working directory.";

	private const string _INTERACTIVE_SESSION_NOTICE_FORMAT =
		"The session this starts is interactive: commands are typed at the '{0}' prompt.";

	private static int Main(string[] arguments)
	{
		ExitCode exitCode = SessionExceptionBoundary.Execute(
			() => RunSessionOrAnswerInformationalLaunch(arguments),
			CommandLineParser.PeekRepositoryFolderPath(arguments),
			Console.Error);

		return (int)exitCode;
	}

	/// <summary>
	/// <c>--help</c>/<c>-h</c> and <c>--version</c> are questions about the tool, not a
	/// request to assess a repository, so each is answered here and only here - before the
	/// repository is resolved, before configuration is read, before Pre-flight runs, before any
	/// session state is read or written, and before a prompt opens. <c>--help</c> is checked
	/// first, so it wins when both are supplied on the same launch: the more complete
	/// answer subsumes the narrower one rather than either silently overriding the other.
	/// </summary>
	private static ExitCode RunSessionOrAnswerInformationalLaunch(string[] arguments)
	{
		ExitCode exitCode;

		if (CommandLineParser.RequestsHelp(arguments))
		{
			PrintHelp();
			exitCode = ExitCode.Success;
		}
		else if (CommandLineParser.RequestsVersion(arguments))
		{
			PrintVersion();
			exitCode = ExitCode.Success;
		}
		else
		{
			exitCode = RunSession(arguments);
		}

		return exitCode;
	}

	/// <summary>
	/// The <c>--help</c> answer. Reuses <see cref="HelpRenderer"/> for the command list rather than
	/// writing a second one, so a command added to the registry cannot appear here without also
	/// appearing in the session's own <c>help</c>.
	/// </summary>
	private static void PrintHelp()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		WriteIdentification(catalog);
		Console.Out.WriteLine(CommandLineParser.UsageLine);
		Console.Out.WriteLine(_OPTION_LINE_INDENT + string.Format(
			CultureInfo.InvariantCulture,
			_REPOSITORY_OPTION_DESCRIPTION_FORMAT,
			CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION,
			_OPTION_DESCRIPTION_SEPARATOR));
		Console.Out.WriteLine(_OPTION_LINE_INDENT + string.Format(
			CultureInfo.InvariantCulture,
			_DESTINATION_OPTION_DESCRIPTION_FORMAT,
			CommandLineParser.DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION,
			_OPTION_DESCRIPTION_SEPARATOR));
		Console.Out.WriteLine();
		HelpRenderer.Render(Console.Out);
		Console.Out.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			_INTERACTIVE_SESSION_NOTICE_FORMAT,
			SessionShell.PROMPT.TrimEnd()));
	}

	/// <summary>The <c>--version</c> answer: the tool's own number beside the catalog's.</summary>
	private static void PrintVersion()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		Console.Out.WriteLine(
			$"{CommandLineParser.PRODUCT_NAME} {ToolVersion.CURRENT} ({_CATALOG_VERSION_LABEL} {catalog.Version.DisplayValue})");
	}

	private static ExitCode RunSession(string[] arguments)
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		WriteIdentification(catalog);

		CommandLineParseResult parsedCommandLine = CommandLineParser.Parse(arguments);

		SessionStartup.ReportRejectedArguments(parsedCommandLine, Console.Out);

		ExitCode exitCode;

		if (!SessionStartup.TryResolveRepositoryFolder(parsedCommandLine, Console.Out, out RepositoryFolderPath repositoryFolder))
		{
			exitCode = ExitCode.UnusableRepositoryPath;
		}
		else
		{
			exitCode = RunAgainstResolvedRepository(parsedCommandLine, repositoryFolder, catalog);
		}

		return exitCode;
	}

	/// <summary>
	/// Everything between a usable repository and a prompt, in the one order that works.
	///
	/// The effective configuration is resolved FIRST, because the destination it carries may come
	/// from the user profile and because Pre-flight is evaluated against it. The destination is
	/// then canonicalized but not yet probed. Pre-flight runs next - degradation notices, the
	/// containment comparison and the enabled-language check - and only a session it permits
	/// goes on to probe writability and open a prompt.
	///
	/// PRE-FLIGHT SITS EXACTLY HERE FOR TWO REASONS. It is after both folders are resolved,
	/// because the containment comparison needs both. And it is before the session state is read
	/// and before <see cref="SessionShell"/> accepts a command, so a refusal here means the prompt
	/// never existed. Nothing re-checks either condition afterwards.
	/// </summary>
	private static ExitCode RunAgainstResolvedRepository(
		CommandLineParseResult parsedCommandLine,
		RepositoryFolderPath repositoryFolder,
		LanguageCatalog catalog)
	{
		EffectiveConfiguration configuration = ConfigurationResolver.Resolve(repositoryFolder, parsedCommandLine);
		ExitCode exitCode;

		if (!SessionStartup.TryCanonicalizeDestinationFolder(
			configuration,
			Console.Out,
			out DocumentationDestinationFolderPath destinationFolder))
		{
			exitCode = ExitCode.OutputDestinationNotWritable;
		}
		else
		{
			PreflightVerdict verdict = PreflightValidation.Run(
				configuration,
				repositoryFolder,
				destinationFolder,
				Console.Out);

			exitCode = verdict.PermitsSession
				? StartSession(repositoryFolder, destinationFolder, catalog, configuration)
				: verdict.ExitCode;
		}

		return exitCode;
	}

	private static ExitCode StartSession(
		RepositoryFolderPath repositoryFolder,
		DocumentationDestinationFolderPath destinationFolder,
		LanguageCatalog catalog,
		EffectiveConfiguration configuration)
	{
		ExitCode exitCode;

		if (!SessionStartup.TryEstablishDestinationIsWritable(destinationFolder, Console.Out))
		{
			exitCode = ExitCode.OutputDestinationNotWritable;
		}
		else
		{
			exitCode = RunInteractiveSession(repositoryFolder, destinationFolder, catalog, configuration);
		}

		return exitCode;
	}

	private static ExitCode RunInteractiveSession(
		RepositoryFolderPath repositoryFolder,
		DocumentationDestinationFolderPath destinationFolder,
		LanguageCatalog catalog,
		EffectiveConfiguration configuration)
	{
		SessionStateStore sessionStateStore = new(destinationFolder);
		SessionStateLoad sessionStateLoad = sessionStateStore.Load(repositoryFolder, catalog);

		SessionStartup.ReportSessionStart(sessionStateLoad, repositoryFolder, Console.Out);

		SessionConsole console = new(BuildSessionInputReader(), Console.Out);

		SessionShell shell = new(
			repositoryFolder,
			sessionStateLoad.State,
			sessionStateStore,
			BuildStageRunner(new SessionComposition
			{
				Repository = repositoryFolder,
				Destination = destinationFolder,
				Catalog = catalog,
				Configuration = configuration,
				Console = console
			}),
			console);

		return RunUnderCancelKeyHandler(shell);
	}

	/// <summary>
	/// The session's own input reader: <see cref="Console.In"/> unchanged for an
	/// interactive session, or a byte-level reader over the raw stream when standard input is
	/// redirected - which is what actually consumes a UTF-8 byte-order mark regardless of this
	/// machine's own console code page.
	///
	/// <see cref="Console.InputEncoding"/> IS ITSELF CODE-PAGE-DERIVED. Decoding through it first
	/// and stripping a character afterwards - <see cref="SessionConsole"/>'s own leading-mark
	/// strip, kept below as the second line of defence - only consumes the mark when that code
	/// page already happens to be UTF-8. On a redirected pipe under any other code page, such as
	/// 437, the same three bytes decode as three unrelated glyphs before
	/// <see cref="SessionConsole"/> ever sees a single character to strip.
	///
	/// <see cref="StreamReader"/>'s OWN <c>detectEncodingFromByteOrderMarks</c> SIDESTEPS THE CODE
	/// PAGE ENTIRELY: it inspects the stream's raw leading bytes for a known mark, switches to the
	/// matching encoding and CONSUMES the mark - so a UTF-8 byte-order mark never reaches text
	/// decoding as a character at all. Input carrying no mark decodes through
	/// the same <see cref="Console.InputEncoding"/> passed in as the fallback.
	///
	/// THE INTERACTIVE PATH IS LEFT ON <see cref="Console.In"/> DELIBERATELY. A real terminal does
	/// not inject a byte-order mark, and re-reading its raw stream by hand would trade a working,
	/// well-tested line-editing surface for one this session does not need to own.
	///
	/// THE READER IS NOT DISPOSED. It lives for the process, on the same terms
	/// <see cref="Console.In"/> itself is never disposed - closing it would close the process's own
	/// standard input handle out from under a shell that still owns the session loop.
	/// </summary>
	private static TextReader BuildSessionInputReader()
	{
		return Console.IsInputRedirected
			? new StreamReader(Console.OpenStandardInput(), Console.InputEncoding, detectEncodingFromByteOrderMarks: true)
			: Console.In;
	}

	/// <summary>
	/// THE STAGE RUNNER, WITH FOUR STAGES WIRED AND ONE STILL EMPTY.
	///
	/// The chain is built inside out: the empty runner at the bottom, then <c>scan</c>, then
	/// <c>analyze</c>, <c>suggest</c> and <c>report</c>. Each decorator handles its own command and
	/// delegates the rest, so <c>ai-assist</c>, <c>help</c>, <c>clear</c> and <c>exit</c>
	/// reach the classes at the bottom of the chain.
	///
	/// ONE OBJECT IS BUILT HERE BECAUSE IT BELONGS TO THE SESSION RATHER THAN TO ANY STAGE: the
	/// skipped-item ledger, shared by <c>scan</c> and <c>analyze</c> through the one emission point
	/// both route through. Each of the two clears it at the start of its own run and snapshots what
	/// it accumulated onto <see cref="Session.SessionState"/> at completion - which is also
	/// where Section 2's numbers and the truncation counter live, so <c>report</c> reads
	/// every one of the report's sections from the state <see cref="SessionShell"/> already threads
	/// through every stage, and none of it is lost when a session RESUMES IN A NEW PROCESS.
	/// </summary>
	private static IWorkflowStageRunner BuildStageRunner(SessionComposition composition)
	{
		SkippedItemLedger skippedItems = new();
		EmissionPoint emissionPoint = new([skippedItems]);
		TextWriter output = composition.Console.Output;
		IGenericRecommendationProvider genericRecommendations = new GenericRecommendationProvider();

		ScanStage scanStage = new(new ScanStageContext
		{
			Repository = composition.Repository,
			Catalog = composition.Catalog,
			Configuration = composition.Configuration,
			EmissionPoint = emissionPoint,
			SkippedItems = skippedItems,
			Output = output,
			TimeProvider = TimeProvider.System
		});

		AnalyzeStage analyzeStage = new(new AnalyzeStageContext
		{
			Repository = composition.Repository,
			Catalog = composition.Catalog,
			Configuration = composition.Configuration,
			EmissionPoint = emissionPoint,
			SkippedItems = skippedItems,
			Output = output,
			TimeProvider = TimeProvider.System
		});

		SuggestStage suggestStage = new(new SuggestStageContext
		{
			Catalog = composition.Catalog,
			GenericRecommendations = genericRecommendations,
			Configuration = composition.Configuration,
			Output = output
		});

		ReportStage reportStage = new(new ReportStageContext
		{
			Catalog = composition.Catalog,
			Configuration = composition.Configuration,
			RepositoryLabel = composition.Repository.Label,
			ArtifactWriter = new AtomicArtifactWriter(composition.Repository, composition.Destination),
			GenericRecommendations = genericRecommendations,
			Output = output,
			TimeProvider = TimeProvider.System
		});

		return new ReportWiredStageRunner(
			reportStage,
			new SuggestWiredStageRunner(
				suggestStage,
				new AnalyzeWiredStageRunner(
					analyzeStage,
					new ScanWiredStageRunner(scanStage, new AiAssistNotAvailableStageRunner(output)))));
	}

	/// <summary>
	/// What a session was started with, gathered so that the composition above takes one parameter
	/// rather than five. Everything in it was established before the prompt existed and none of it
	/// changes for the life of the session.
	/// </summary>
	private sealed record SessionComposition
	{
		internal required RepositoryFolderPath Repository { get; init; }

		internal required DocumentationDestinationFolderPath Destination { get; init; }

		internal required LanguageCatalog Catalog { get; init; }

		internal required EffectiveConfiguration Configuration { get; init; }

		internal required SessionConsole Console { get; init; }
	}

	/// <summary>
	/// Interrupting a running stage returns to the prompt with the session intact, so
	/// the cancel key is handled rather than left to end the process. The handler is removed in
	/// a finally block: the session must not leave a callback pointing into a shell it no
	/// longer owns.
	/// </summary>
	private static ExitCode RunUnderCancelKeyHandler(SessionShell shell)
	{
		ConsoleCancelEventHandler cancelKeyHandler = (sender, cancelEvent) =>
		{
			cancelEvent.Cancel = true;
			shell.RequestCancellation();
		};

		Console.CancelKeyPress += cancelKeyHandler;

		ExitCode exitCode;

		try
		{
			exitCode = shell.Run();
		}
		finally
		{
			Console.CancelKeyPress -= cancelKeyHandler;
		}

		return exitCode;
	}

	/// <summary>
	/// The catalog is numbered independently of the product and that number appears in all
	/// output, so the identification line carries it, beside the product's own number, on the
	/// same terms <c>--version</c> reports both: <see cref="ToolVersion.CURRENT"/> is
	/// the one constant either output reads, so the two can never disagree.
	/// </summary>
	private static void WriteIdentification(LanguageCatalog catalog)
	{
		Console.Out.WriteLine(
			$"{_IDENTIFICATION_LINE} {ToolVersion.CURRENT} ({_CATALOG_VERSION_LABEL} {catalog.Version.DisplayValue})");
	}
}
