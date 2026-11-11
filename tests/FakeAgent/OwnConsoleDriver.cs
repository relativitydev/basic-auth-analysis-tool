using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace MigrateAuth.FakeAgent;

/// <summary>How the driver answers the batch-job prompt after the interrupt.</summary>
public enum OwnConsoleAnswerMode
{
	/// <summary>Writes the answer as key events into the driver's own console input buffer.</summary>
	Console,

	/// <summary>Starts the child with its standard input redirected and writes the answer to that pipe.</summary>
	Redirected,
}

/// <summary>What the driver is asked to do, written by the test as a JSON file.</summary>
public sealed record OwnConsoleRequest
{
	/// <summary>The program to start, exactly as the product would start it.</summary>
	public string FileName { get; init; } = string.Empty;

	/// <summary>The one constructed argument string of the program.</summary>
	public string Arguments { get; init; } = string.Empty;

	/// <summary>The working directory of the program.</summary>
	public string WorkingDirectory { get; init; } = string.Empty;

	/// <summary>A file whose existence says the fake behind the wrapper has started.</summary>
	public string ReadyPath { get; init; } = string.Empty;

	/// <summary>Where the driver writes its <see cref="OwnConsoleResult"/>.</summary>
	public string ResultPath { get; init; } = string.Empty;

	/// <summary>How the prompt is answered.</summary>
	public OwnConsoleAnswerMode AnswerMode { get; init; } = OwnConsoleAnswerMode.Console;

	private const int _DEFAULT_MAX_SECONDS = 60;

	/// <summary>The most seconds the driver waits for each step before it gives up and ends the tree.</summary>
	public int MaxSeconds { get; init; } = _DEFAULT_MAX_SECONDS;
}

/// <summary>What happened in the driver's own console.</summary>
public sealed record OwnConsoleResult
{
	/// <summary>Whether the child started.</summary>
	public bool Started { get; init; }

	/// <summary>Whether the ready file appeared before the interrupt was raised.</summary>
	public bool ReadySeen { get; init; }

	/// <summary>Whether the interrupt was raised on the driver's console.</summary>
	public bool InterruptRaised { get; init; }

	/// <summary>How many interrupts the driver's own handler saw (the driver survives its own interrupt).</summary>
	public int InterruptsSeenByDriver { get; init; }

	/// <summary>Whether the answer was delivered to the prompt.</summary>
	public bool AnswerDelivered { get; init; }

	/// <summary>Whether the child ended by itself, without the driver ending its tree.</summary>
	public bool EndedByItself { get; init; }

	/// <summary>The exit code the child reported.</summary>
	public int ExitCode { get; init; }

	/// <summary>A short statement of what stopped the run early, empty when nothing did.</summary>
	public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// A helper that owns its own console: the test starts it so that it is given a console of its own, which means an
/// interrupt raised there reaches only the processes that share that console and never the test host. The driver
/// starts the program it was asked to start, raises the keyboard interrupt on its console, answers the prompt the
/// command interpreter shows about ending the batch job, and writes what it saw.
/// </summary>
public static class OwnConsoleDriver
{
	public const string MODE_ARGUMENT = "--own-console-interrupt";

	private const uint _CTRL_C_EVENT = 0;
	private const int _STANDARD_INPUT_HANDLE = -10;
	private const ushort _KEY_EVENT = 1;
	private const ushort _VIRTUAL_KEY_Y = 0x59;
	private const ushort _VIRTUAL_KEY_RETURN = 0x0D;
	private const ushort _SCAN_Y = 0x15;
	private const ushort _SCAN_RETURN = 0x1C;
	private const int _SETTLE_MILLISECONDS = 3000;
	private const int _POLL_MILLISECONDS = 100;
	private const int _MILLISECONDS_PER_SECOND = 1000;
	private const int _ONE_INTERRUPT = 1;
	private const string _ANSWER_TEXT = "Y";
	private const char _ANSWER_CHARACTER = 'Y';
	private const char _RETURN_CHARACTER = '\r';
	private const int _KEY_DOWN = 1;
	private const int _KEY_UP = 0;
	private const ushort _SINGLE_REPEAT = 1;
	private const uint _CURRENT_PROCESS_GROUP = 0;
	private const int _REQUEST_ARGUMENT_INDEX = 1;
	private const int _SUCCESS_EXIT_CODE = 0;
	private const int _INPUT_RECORD_SIZE = 20;
	private const int _EVENT_TYPE_OFFSET = 0;
	private const int _KEY_DOWN_OFFSET = 4;
	private const int _REPEAT_COUNT_OFFSET = 8;
	private const int _VIRTUAL_KEY_CODE_OFFSET = 10;
	private const int _VIRTUAL_SCAN_CODE_OFFSET = 12;
	private const int _UNICODE_CHAR_OFFSET = 14;

	private static int _interruptsSeen;

	internal static int Run(string[] arguments)
	{
		OwnConsoleRequest request = JsonSerializer.Deserialize<OwnConsoleRequest>(File.ReadAllText(arguments[_REQUEST_ARGUMENT_INDEX])) ?? new OwnConsoleRequest();
		OwnConsoleResult result = Drive(request);

		File.WriteAllText(request.ResultPath, JsonSerializer.Serialize(result));

		return _SUCCESS_EXIT_CODE;
	}

	private static OwnConsoleResult Drive(OwnConsoleRequest request)
	{
		// A process that was started by one that ignores the interrupt inherits that setting, and so would the
		// child; switching the ignore setting off makes the interrupt processed normally for both.
		SetConsoleCtrlHandler(IntPtr.Zero, false);

		Console.CancelKeyPress += (_, eventArguments) =>
		{
			eventArguments.Cancel = true;
			Interlocked.Add(ref _interruptsSeen, _ONE_INTERRUPT);
		};

		ProcessStartInfo startInfo = new(request.FileName, request.Arguments)
		{
			UseShellExecute = false,
			WorkingDirectory = request.WorkingDirectory,
			RedirectStandardInput = request.AnswerMode == OwnConsoleAnswerMode.Redirected,
		};
		OwnConsoleResult result;

		try
		{
			using Process? child = Process.Start(startInfo);

			result = child is null
				? new OwnConsoleResult { Detail = "The child did not start." }
				: Interrupt(request, child);
		}
		catch (System.ComponentModel.Win32Exception exception)
		{
			result = new OwnConsoleResult { Detail = $"The child could not be started: {exception.NativeErrorCode}" };
		}

		return result;
	}

	private static OwnConsoleResult Interrupt(OwnConsoleRequest request, Process child)
	{
		int budget = request.MaxSeconds * _MILLISECONDS_PER_SECOND;
		bool ready = WaitForFile(request.ReadyPath, budget);
		bool raised = ready && GenerateConsoleCtrlEvent(_CTRL_C_EVENT, _CURRENT_PROCESS_GROUP);
		bool answered = false;

		if (raised)
		{
			Thread.Sleep(_SETTLE_MILLISECONDS);
			answered = Answer(request.AnswerMode, child);
		}

		bool ended = child.WaitForExit(budget);

		if (!ended)
		{
			child.Kill(entireProcessTree: true);
			child.WaitForExit();
		}

		return new OwnConsoleResult
		{
			Started = true,
			ReadySeen = ready,
			InterruptRaised = raised,
			InterruptsSeenByDriver = Volatile.Read(ref _interruptsSeen),
			AnswerDelivered = answered,
			EndedByItself = ended,
			ExitCode = child.ExitCode,
			Detail = ready ? string.Empty : "The fake behind the wrapper never started.",
		};
	}

	private static bool Answer(OwnConsoleAnswerMode mode, Process child)
	{
		bool delivered;

		if (mode == OwnConsoleAnswerMode.Redirected)
		{
			child.StandardInput.WriteLine(_ANSWER_TEXT);
			child.StandardInput.Flush();
			delivered = true;
		}
		else
		{
			delivered = WriteKeys();
		}

		return delivered;
	}

	private static bool WriteKeys()
	{
		InputRecord[] records =
		[
			KeyRecord(true, _VIRTUAL_KEY_Y, _SCAN_Y, _ANSWER_CHARACTER),
			KeyRecord(false, _VIRTUAL_KEY_Y, _SCAN_Y, _ANSWER_CHARACTER),
			KeyRecord(true, _VIRTUAL_KEY_RETURN, _SCAN_RETURN, _RETURN_CHARACTER),
			KeyRecord(false, _VIRTUAL_KEY_RETURN, _SCAN_RETURN, _RETURN_CHARACTER),
		];

		return WriteConsoleInputW(GetStdHandle(_STANDARD_INPUT_HANDLE), records, (uint)records.Length, out uint written)
			&& written == records.Length;
	}

	private static InputRecord KeyRecord(bool down, ushort virtualKey, ushort scanCode, char character)
	{
		return new InputRecord
		{
			EventType = _KEY_EVENT,
			KeyDown = down ? _KEY_DOWN : _KEY_UP,
			RepeatCount = _SINGLE_REPEAT,
			VirtualKeyCode = virtualKey,
			VirtualScanCode = scanCode,
			UnicodeChar = character,
		};
	}

	private static bool WaitForFile(string path, int budgetMilliseconds)
	{
		int waited = 0;

		while (!File.Exists(path) && waited < budgetMilliseconds)
		{
			Thread.Sleep(_POLL_MILLISECONDS);
			waited += _POLL_MILLISECONDS;
		}

		return File.Exists(path);
	}

	[StructLayout(LayoutKind.Explicit, Size = _INPUT_RECORD_SIZE)]
	private struct InputRecord
	{
		[FieldOffset(_EVENT_TYPE_OFFSET)]
		internal ushort EventType;

		[FieldOffset(_KEY_DOWN_OFFSET)]
		internal int KeyDown;

		[FieldOffset(_REPEAT_COUNT_OFFSET)]
		internal ushort RepeatCount;

		[FieldOffset(_VIRTUAL_KEY_CODE_OFFSET)]
		internal ushort VirtualKeyCode;

		[FieldOffset(_VIRTUAL_SCAN_CODE_OFFSET)]
		internal ushort VirtualScanCode;

		[FieldOffset(_UNICODE_CHAR_OFFSET)]
		internal char UnicodeChar;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GenerateConsoleCtrlEvent(uint controlEvent, uint processGroupId);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, [MarshalAs(UnmanagedType.Bool)] bool add);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr GetStdHandle(int standardHandle);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool WriteConsoleInputW(IntPtr consoleInput, InputRecord[] buffer, uint length, out uint written);
}
