using System.Collections.Immutable;
using System.Globalization;

namespace MigrateAuth.Cli.Session;

/// <summary>Whether the prompt comes back after a command.</summary>
internal enum SessionContinuation
{
	/// <summary>Return to the prompt. Every command but one does this, including every failure.</summary>
	Continue = 0,

	/// <summary>End the session normally.</summary>
	End = 1
}

/// <summary>
/// The interactive session: the prompt, the dispatcher, the gating chain and the cascade.
///
/// Exit codes are last-condition-wins and this loop is where most of that rule lives:
/// a gating violation and an unrecognized command both report and come back to the prompt, so
/// neither ends the session and neither sets an exit code. There is consequently no code path
/// here that returns <see cref="ExitCode.SequentialGatingViolation"/> or
/// <see cref="ExitCode.UnrecognizedCommand"/>, and that absence is deliberate: the enumeration
/// keeps both values because it is a published contract, but a branch trying to return one
/// would be unreachable.
///
/// The session ends by returning a code and never by an unhandled exception.
/// </summary>
public sealed class SessionShell
{
	/// <summary>The prompt. Written without a trailing newline.</summary>
	public const string PROMPT = "migrate-auth> ";

	/// <summary>The one invalidated stage is named to the user.</summary>
	public const string INVALIDATED_STAGE_MESSAGE_FORMAT =
		"Invalidated: {0}. Re-run it before the report reflects the new results.";

	/// <summary>Several invalidated stages are named to the user.</summary>
	public const string INVALIDATED_STAGES_MESSAGE_FORMAT =
		"Invalidated: {0}. Re-run them before the report reflects the new results.";

	/// <summary>Said when a stage commits.</summary>
	public const string STAGE_COMPLETED_MESSAGE_FORMAT = "{0}: completed.";

	/// <summary>Said when a stage is interrupted. Not an error and not an exit code.</summary>
	public const string STAGE_CANCELLED_MESSAGE_FORMAT =
		"{0}: interrupted. The session is intact and the stage is not marked completed.";

	/// <summary>Said when a stage could not finish its work.</summary>
	public const string STAGE_FAILED_MESSAGE_FORMAT =
		"{0}: did not finish. The session is intact and the stage is not marked completed.";

	/// <summary>
	/// A destination that cannot be written, mid-session: it reports and returns to the prompt rather
	/// than terminating with exit code 4, because the completed stages are still valid and discarding
	/// them over a permission the user can restore in seconds costs more than it saves.
	/// </summary>
	public const string STATE_NOT_PERSISTED_MESSAGE =
		"The session state could not be written to the documentation destination folder. The completed stages hold for this session but will not survive it.";

	private const string _STAGE_NAME_SEPARATOR = ", ";

	private readonly RepositoryFolderPath _repositoryFolder;
	private readonly SessionState _sessionState;
	private readonly SessionStateStore _sessionStateStore;
	private readonly IWorkflowStageRunner _stageRunner;
	private readonly SessionConsole _console;

	private CancellationTokenSource _currentStageCancellation = new();

	public SessionShell(
		RepositoryFolderPath repositoryFolder,
		SessionState sessionState,
		SessionStateStore sessionStateStore,
		IWorkflowStageRunner stageRunner,
		SessionConsole console)
	{
		ArgumentNullException.ThrowIfNull(sessionState);
		ArgumentNullException.ThrowIfNull(sessionStateStore);
		ArgumentNullException.ThrowIfNull(stageRunner);
		ArgumentNullException.ThrowIfNull(console);

		_repositoryFolder = repositoryFolder;
		_sessionState = sessionState;
		_sessionStateStore = sessionStateStore;
		_stageRunner = stageRunner;
		_console = console;
	}

	/// <summary>
	/// Runs the prompt until the user ends the session or the input ends. Returns
	/// <see cref="ExitCode.Success"/> in both cases: the session ended normally, which is what
	/// zero means.
	/// </summary>
	public ExitCode Run()
	{
		SessionContinuation continuation = SessionContinuation.Continue;

		while (continuation is SessionContinuation.Continue)
		{
			_console.Output.Write(PROMPT);

			string? enteredLine = _console.ReadLine();

			if (enteredLine is null)
			{
				_console.Output.WriteLine();
				continuation = SessionContinuation.End;
			}
			else
			{
				continuation = HandleEnteredLine(enteredLine);
			}
		}

		return ExitCode.Success;
	}

	/// <summary>
	/// Signals the running stage to stop. Called from the console's cancel-key handler, which
	/// is the one place in this build that runs on another thread; the cancellation source is
	/// the whole of the shared state, and it is replaced before each stage rather than reused.
	/// </summary>
	public void RequestCancellation()
	{
		_currentStageCancellation.Cancel();
	}

	private SessionContinuation HandleEnteredLine(string enteredLine)
	{
		string enteredText = enteredLine.Trim();
		SessionContinuation continuation;

		if (enteredText.Length == 0)
		{
			continuation = SessionContinuation.Continue;
		}
		else if (CommandRegistry.TryResolve(enteredText, out CommandDescriptor? command))
		{
			continuation = Dispatch(command!);
		}
		else
		{
			ReportNotRecognized(enteredText);
			continuation = SessionContinuation.Continue;
		}

		return continuation;
	}

	/// <summary>
	/// The dispatcher. The switch is exhaustive over <see cref="CommandId"/> with no discard
	/// arm, so a command identity added without a dispatch arm does not compile - CS8509 is a
	/// warning and this repository builds with warnings as errors. That is the compile-time
	/// half of the guarantee that the dispatcher and <c>help</c> cannot disagree; the
	/// runtime half is the registry's refusal to construct with an identity missing.
	///
	/// Only CS8524 is suppressed, and it is a different check: it fires for values the
	/// enumeration does not name, which can only arrive by casting an arbitrary integer. The
	/// one route from input to a command identity is
	/// <see cref="CommandRegistry.TryResolve"/>, which returns registry entries and nothing
	/// else, so no such value can reach here. Suppressing it narrowly is what keeps CS8509 -
	/// the check that matters - armed.
	/// </summary>
	private SessionContinuation Dispatch(CommandDescriptor command)
	{
#pragma warning disable CS8524
		SessionContinuation continuation = command.Id switch
		{
			CommandId.Scan => RunWorkflowStage(command),
			CommandId.Analyze => RunWorkflowStage(command),
			CommandId.Suggest => RunWorkflowStage(command),
			CommandId.Report => RunWorkflowStage(command),
			CommandId.AiAssist => RunWorkflowStage(command),
			CommandId.Help => ShowHelp(),
			CommandId.Clear => ClearConsole(),
			CommandId.Exit => SessionContinuation.End
		};
#pragma warning restore CS8524

		return continuation;
	}

	private SessionContinuation RunWorkflowStage(CommandDescriptor command)
	{
		if (_sessionState.TryFindUnmetPredecessor(command.Id, out CommandDescriptor? unmetPredecessor))
		{
			_console.Output.WriteLine(CommandRegistry.FormatGatingMessage(unmetPredecessor!.Id));
		}
		else
		{
			ExecuteStage(command);
		}

		return SessionContinuation.Continue;
	}

	private void ExecuteStage(CommandDescriptor command)
	{
		_currentStageCancellation.Dispose();
		_currentStageCancellation = new CancellationTokenSource();

		StageRunOutcome outcome = _stageRunner.Run(command.Id, _sessionState, _currentStageCancellation.Token);

		switch (outcome)
		{
			case StageRunOutcome.Completed:
				CommitStage(command);
				break;

			case StageRunOutcome.Cancelled:
				_sessionState.MarkNotCompleted(command.Id);
				WriteFormatted(STAGE_CANCELLED_MESSAGE_FORMAT, command.Name);
				break;

			case StageRunOutcome.NotAvailable:
				// Completion state is left exactly as it was and nothing further is
				// written - the stage runner has already said everything there is to say, in
				// its own client-facing words.
				break;

			default:
				_sessionState.MarkNotCompleted(command.Id);
				WriteFormatted(STAGE_FAILED_MESSAGE_FORMAT, command.Name);
				break;
		}
	}

	/// <summary>
	/// A stage commits on completion and only then: the cascade is applied, the
	/// invalidated stages are named, and the state reaches disk in one atomic write.
	/// </summary>
	private void CommitStage(CommandDescriptor command)
	{
		ImmutableArray<CommandDescriptor> invalidatedStages = _sessionState.MarkCompleted(command.Id);

		WriteFormatted(STAGE_COMPLETED_MESSAGE_FORMAT, command.Name);

		if (invalidatedStages.Length > 0)
		{
			string invalidatedNames = string.Join(_STAGE_NAME_SEPARATOR, invalidatedStages.Select(stage => stage.Name));

			WriteFormatted(
				invalidatedStages.Length == 1 ? INVALIDATED_STAGE_MESSAGE_FORMAT : INVALIDATED_STAGES_MESSAGE_FORMAT,
				invalidatedNames);
		}

		PersistState();
	}

	private void PersistState()
	{
		try
		{
			_sessionStateStore.Save(_repositoryFolder, _sessionState);
		}
		catch (SessionStateTooLargeException tooLarge)
		{
			// The message names the ceiling and says the previous saved state is unchanged.
			_console.Output.WriteLine(tooLarge.Message);
		}
		catch (IOException)
		{
			_console.Output.WriteLine(STATE_NOT_PERSISTED_MESSAGE);
		}
		catch (UnauthorizedAccessException)
		{
			_console.Output.WriteLine(STATE_NOT_PERSISTED_MESSAGE);
		}
	}

	private SessionContinuation ShowHelp()
	{
		HelpRenderer.Render(_console.Output);

		return SessionContinuation.Continue;
	}

	private SessionContinuation ClearConsole()
	{
		try
		{
			Console.Clear();
		}
		catch (IOException)
		{
			// A console whose output is redirected has nothing to clear. The command is a
			// convenience and must never end a session over the shape of its output stream.
		}

		return SessionContinuation.Continue;
	}

	private void ReportNotRecognized(string enteredText)
	{
		string safeText = ConsoleTextSafety.StripUnsafeCharacters(enteredText);

		_console.Output.WriteLine(CommandRegistry.FormatNotRecognizedMessage(safeText));
		_console.Output.WriteLine(CommandRegistry.RUN_HELP_HINT);
	}

	private void WriteFormatted(string messageFormat, string argument)
	{
		_console.Output.WriteLine(string.Format(CultureInfo.InvariantCulture, messageFormat, argument));
	}
}
