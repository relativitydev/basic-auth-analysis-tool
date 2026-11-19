using System.Globalization;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// THE AI-ASSIST STAGE: choose an environment, write the prompt pack, add a section to the report this process
/// wrote, and, only on the user's explicit confirmation, launch the user's own agent against a copy of the pack.
///
/// THE STAGE NEVER CALLS A PROVIDER AND NEVER ASKS FOR A CREDENTIAL. It reads exactly two kinds of answer
/// from the console - the environment, and a yes or no - one whole line each, and it reads nothing at all
/// while the child runs, because the child owns the terminal. It never echoes what the user typed.
///
/// EVERY PATH ENDS WITH <see cref="StageRunOutcome.SelfReported"/>. The stage prints its own closing line on
/// every branch, so the shell neither commits nor adds a line, and the session state is read (the findings)
/// and never written: running the stage again, for another environment, is always allowed after the report.
///
/// THE ORDER IS FIXED AND EACH STEP FAILS CLOSED: a refused pack write leaves the report and the data artifact
/// as they were; a refused append keeps the pack; nothing is offered unless every check passed; the launch
/// copy is written only after the user said yes; and no absolute path of any kind is printed.
///
/// The two seams are the context's (the launcher for an environment, which also carries its own resolution, and
/// the terminal predicate), plus the two this class takes itself: the operation that appends to the report, so a test can
/// refuse it in ways the real writer cannot be made to, and the native console, used for the one thing only it
/// can do - throwing away input typed ahead, before the confirmation is read and again after the child ends.
/// </summary>
public sealed class AiAssistStageRunner : IWorkflowStageRunner
{
	private const int _MAXIMUM_SELECTION_READS = 2;

	private readonly AiAssistStageContext _context;
	private readonly IReportAppender _reportAppender;
	private readonly INativeConsole _nativeConsole;
	private readonly TextWriter _output;

	/// <summary>Creates the stage over its context and the two seams it takes directly.</summary>
	public AiAssistStageRunner(AiAssistStageContext context, IReportAppender reportAppender, INativeConsole nativeConsole)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(context.Console);
		ArgumentNullException.ThrowIfNull(context.Catalog);
		ArgumentNullException.ThrowIfNull(context.ArtifactWriter);
		ArgumentNullException.ThrowIfNull(context.ReportHolder);
		ArgumentNullException.ThrowIfNull(context.LauncherFor);
		ArgumentNullException.ThrowIfNull(context.IsInteractiveTerminal);
		ArgumentNullException.ThrowIfNull(reportAppender);
		ArgumentNullException.ThrowIfNull(nativeConsole);

		_context = context;
		_reportAppender = reportAppender;
		_nativeConsole = nativeConsole;
		_output = context.Console.Output;
	}

	/// <summary>Everything one run knows once the environment is chosen and the pack is written.</summary>
	private sealed record ReviewRun(
		AgentEnvironmentDescriptor Environment,
		PromptPackScope Scope,
		string? ReportFileName,
		PromptPackFilePath Pack);

	/// <summary>The launch copy: its path, and the name of the folder it lives in.</summary>
	private sealed record LaunchCopy(PromptPackFilePath Pack, string FolderName);

	/// <inheritdoc />
	public StageRunOutcome Run(CommandId workflowCommand, SessionState state, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(state);

		if (workflowCommand is not CommandId.AiAssist)
		{
			throw new ArgumentOutOfRangeException(nameof(workflowCommand), workflowCommand, "This runner handles ai-assist only.");
		}

		Execute(state, cancellationToken);

		return StageRunOutcome.SelfReported;
	}

	private void Execute(SessionState state, CancellationToken cancellationToken)
	{
		AgentEnvironment? environment = SelectEnvironment();

		if (environment is not null && !WasInterrupted(cancellationToken))
		{
			GenerateAndOffer(AgentEnvironmentCatalog.Describe(environment.Value), state, cancellationToken);
		}
	}

	private void GenerateAndOffer(AgentEnvironmentDescriptor environment, SessionState state, CancellationToken cancellationToken)
	{
		PromptPackScope scope = new(state.Findings, _context.MinimumConfidenceLevel, _context.Catalog);
		string? reportFileName = _context.ReportHolder.Current?.FileName;

		if (!WasInterrupted(cancellationToken))
		{
			PromptPackFilePath? pack = WritePack(environment, scope, reportFileName);

			if (pack is not null)
			{
				ReviewRun run = new(environment, scope, reportFileName, pack.Value);

				AppendSectionToReport(run);
				OfferLaunch(run, cancellationToken);
			}
		}
	}

	/// <summary>Prints the interrupted line and reports true when the stage's token was cancelled at a decision point.</summary>
	private bool WasInterrupted(CancellationToken cancellationToken)
	{
		bool interrupted = cancellationToken.IsCancellationRequested;

		if (interrupted)
		{
			_output.WriteLine(AiAssistTexts.INTERRUPTED);
		}

		return interrupted;
	}

	// ---- Selection ----

	private AgentEnvironment? SelectEnvironment()
	{
		WriteSelectionList();

		AgentEnvironment? chosen = null;
		bool ended = false;

		for (int read = 0; read < _MAXIMUM_SELECTION_READS && chosen is null && !ended; read++)
		{
			string? line = ReadAnswer();

			if (string.IsNullOrWhiteSpace(line))
			{
				ended = true;
			}
			else
			{
				chosen = ParseSelection(line);

				if (chosen is null && IsCursor(line))
				{
					_output.WriteLine(AiAssistTexts.SELECTION_CURSOR_NOT_SUPPORTED);
				}
				else if (chosen is null && read == 0)
				{
					_output.WriteLine(AiAssistTexts.SELECTION_REASK);
				}
			}
		}

		if (chosen is null)
		{
			_output.WriteLine(AiAssistTexts.NO_ENVIRONMENT_CHOSEN);
		}

		return chosen;
	}

	private void WriteSelectionList()
	{
		_output.WriteLine(AiAssistTexts.SELECTION_HEADING);

		foreach (AgentEnvironmentDescriptor descriptor in AgentEnvironmentCatalog.All)
		{
			_output.WriteLine(AiAssistTexts.SelectionOption(descriptor.Environment));
		}

		_output.Write(AiAssistTexts.SELECTION_PROMPT);
	}

	/// <summary>One whole line from the console, or null at the end of input, after which the line is ended.</summary>
	private string? ReadAnswer()
	{
		string? line = _context.Console.ReadLine();

		if (line is null)
		{
			_output.WriteLine();
		}

		return line;
	}

	private static AgentEnvironment? ParseSelection(string input)
	{
		string text = input.Trim();
		AgentEnvironment? chosen = null;

		foreach (AgentEnvironmentDescriptor descriptor in AgentEnvironmentCatalog.All)
		{
			bool matches = text == descriptor.SelectionNumber.ToString(CultureInfo.InvariantCulture)
				|| descriptor.AcceptedNames.Any(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase));

			if (matches)
			{
				chosen = descriptor.Environment;
			}
		}

		return chosen;
	}

	private static bool IsCursor(string input)
	{
		return string.Equals(input.Trim(), AiAssistTexts.CURSOR_NAME, StringComparison.OrdinalIgnoreCase);
	}

	// ---- The pack and the report section ----

	private PromptPackFilePath? WritePack(AgentEnvironmentDescriptor environment, PromptPackScope scope, string? reportFileName)
	{
		ArtifactWriteResult result = _context.ArtifactWriter.WritePromptPackStreaming(
			environment.PackFileNameStem,
			sink => PromptPackWriter.WriteBody(sink, environment.Environment, scope, reportFileName));
		PromptPackFilePath? pack = null;

		if (result.Wrote && PromptPackFilePath.TryForWrittenPack(_context.Destination, result, out PromptPackFilePath written))
		{
			_output.WriteLine(AiAssistConsoleLines.Written(written.FileName, _context.Destination.ConsoleDisplayText));
			pack = written;
		}
		else
		{
			ReportGenerationFailure(result);
		}

		return pack;
	}

	private void ReportGenerationFailure(ArtifactWriteResult result)
	{
		_output.WriteLine(AiAssistTexts.PACK_GENERATION_FAILED);

		if (!string.IsNullOrEmpty(result.RefusalMessage))
		{
			_output.WriteLine(result.RefusalMessage);
		}
	}

	private void AppendSectionToReport(ReviewRun run)
	{
		ReportWriteHolder holder = _context.ReportHolder;

		if (holder.Current is null)
		{
			_output.WriteLine(holder.RecordingWasLost ? AiAssistTexts.REPORT_COULD_NOT_BE_RECORDED : AiAssistTexts.REPORT_NOT_WRITTEN_BY_THIS_RUN);
		}
		else
		{
			string[] lines = AiAssistTexts.SectionLines(run.Environment.Environment, run.Pack.FileName, run.Scope.TotalCount);
			ReportAppendResult result = _reportAppender.AppendToReport(holder, sink => WriteSection(sink, lines));

			_output.WriteLine(result.Appended
				? AiAssistConsoleLines.SectionAppended(run.Pack.FileName)
				: AiAssistConsoleLines.AppendRefusal(result.Refusal));
		}
	}

	private static void WriteSection(TextWriter sink, string[] lines)
	{
		sink.NewLine = PromptPackWriter.LINE_ENDING;
		sink.WriteLine();

		foreach (string line in lines)
		{
			sink.WriteLine(line);
		}
	}

	// ---- Availability, the offer and the confirmation ----

	private void OfferLaunch(ReviewRun run, CancellationToken cancellationToken)
	{
		IExternalAgentLauncher launcher = _context.LauncherFor(run.Environment.Environment);

		_output.WriteLine(AiAssistConsoleLines.Searching(run.Environment.CommandName));

		switch (launcher.CheckAvailability())
		{
			case AgentAvailability.Available:
				OfferAvailableAgent(run, launcher, cancellationToken);
				break;

			case AgentAvailability.NotFoundMatchInsideRepositoryIgnored:
				_output.WriteLine(AiAssistConsoleLines.MatchInsideRepository(run.Environment.CommandName));
				break;

			default:
				_output.WriteLine(AiAssistConsoleLines.NotFound(run.Environment.CommandName));
				break;
		}
	}

	private void OfferAvailableAgent(ReviewRun run, IExternalAgentLauncher launcher, CancellationToken cancellationToken)
	{
		if (!_context.IsInteractiveTerminal())
		{
			_output.WriteLine(AiAssistTexts.NO_TERMINAL);
		}
		else
		{
			AgentLaunchEligibility eligibility = launcher.CheckLaunchable(run.Pack);

			if (eligibility is AgentLaunchEligibility.Launchable)
			{
				ShowNoticesAndConfirm(run, launcher, cancellationToken);
			}
			else
			{
				_output.WriteLine(AiAssistConsoleLines.Ineligible(eligibility, run.Environment.CommandName));
			}
		}
	}

	private void ShowNoticesAndConfirm(ReviewRun run, IExternalAgentLauncher launcher, CancellationToken cancellationToken)
	{
		WriteNotices(run, launcher.DescribeResolution());

		if (!WasInterrupted(cancellationToken))
		{
			if (ReadConfirmation(run.Environment.Environment))
			{
				LaunchFromCopy(run, launcher);
			}
			else
			{
				_output.WriteLine(AiAssistTexts.LAUNCH_DECLINED);
			}
		}
	}

	/// <summary>
	/// The block before the question: the resolution line, then each notice (cost, repository access, agent settings)
	/// as its own paragraph (its title on a line
	/// of its own, with no Markdown mark), then the entry count, each separated from the next by a blank line, and a
	/// blank line before the question.
	/// </summary>
	private void WriteNotices(ReviewRun run, AgentResolutionDescription resolution)
	{
		_output.WriteLine(AiAssistConsoleLines.Resolution(resolution));
		WriteParagraph(AiAssistTexts.CostNoticeLines(run.Environment.Environment, run.Scope.TotalCount));
		WriteParagraph(AiAssistTexts.RepositoryAccessLines(run.Environment.Environment));
		WriteParagraph(AiAssistTexts.AgentSettingsLines(run.Environment.Environment));
		WriteParagraph([AiAssistTexts.EntryCountLine(run.Scope)]);
		_output.WriteLine();
	}

	private void WriteParagraph(IEnumerable<string> lines)
	{
		_output.WriteLine();

		foreach (string line in lines)
		{
			_output.WriteLine(line);
		}
	}

	/// <summary>Flushes typed-ahead input, asks the question and reads one line: only y or yes confirms.</summary>
	private bool ReadConfirmation(AgentEnvironment environment)
	{
		_nativeConsole.TryFlushInput();
		_output.Write(AiAssistTexts.Confirmation(environment));

		string? answer = ReadAnswer()?.Trim();

		return answer is not null
			&& AiAssistTexts.ConfirmingAnswers.Any(accepted => string.Equals(accepted, answer, StringComparison.OrdinalIgnoreCase));
	}

	// ---- The launch ----

	private void LaunchFromCopy(ReviewRun run, IExternalAgentLauncher launcher)
	{
		LaunchCopy? copy = WriteLaunchCopy(run);

		if (copy is not null)
		{
			AgentTerminationStatus status = launcher.Launch(copy.Pack);

			_nativeConsole.TryFlushInput();
			_output.WriteLine(AiAssistConsoleLines.Termination(status, run.Environment));
			_output.WriteLine(AiAssistConsoleLines.LaunchFolder(_context.Destination.ConsoleDisplayText, copy.FolderName));
		}
	}

	/// <summary>Writes the same pack body again, in a new folder under the destination, and returns the copy's path.</summary>
	private LaunchCopy? WriteLaunchCopy(ReviewRun run)
	{
		LaunchCopy? copy = null;

		try
		{
			string folderPath = LaunchCopyLayout.NewFolder(_context.Destination.CanonicalPath);
			string folderName = Path.GetFileName(folderPath);
			string folderConsoleText = AiAssistTexts.LaunchFolderText(_context.Destination.ConsoleDisplayText, folderName);
			DocumentationDestinationFolderPath copyDestination = DocumentationDestinationFolderPath.FromResolvedText(folderPath, folderConsoleText);
			ArtifactWriteResult result = new AtomicArtifactWriter(_context.Repository, copyDestination).WritePromptPackStreaming(
				run.Environment.PackFileNameStem,
				sink => PromptPackWriter.WriteBody(sink, run.Environment.Environment, run.Scope, run.ReportFileName));

			if (result.Wrote && PromptPackFilePath.TryForWrittenPack(copyDestination, result, out PromptPackFilePath written))
			{
				copy = new LaunchCopy(written, folderName);
			}
			else
			{
				ReportGenerationFailure(result);
			}
		}
		catch (IOException)
		{
			ReportLaunchCopyFailure();
		}
		catch (UnauthorizedAccessException)
		{
			ReportLaunchCopyFailure();
		}
		catch (NotSupportedException)
		{
			ReportLaunchCopyFailure();
		}

		return copy;
	}

	private void ReportLaunchCopyFailure()
	{
		_output.WriteLine(AiAssistTexts.PACK_GENERATION_FAILED);
	}
}
