using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The console mode guard, around a launch, with a fake console that behaves like a console whose modes a child
/// can change: the mode after the launch equals the mode before it, also when the launch throws; a stream
/// without a console is skipped; input typed ahead is discarded after the child and on demand before a read.
/// </summary>
public sealed class ConsoleModeGuardTests
{
	private const uint _ORIGINAL_INPUT_MODE = 0x01F7;
	private const uint _ORIGINAL_OUTPUT_MODE = 0x0007;
	private const uint _CHANGED_INPUT_MODE = 0x0000;
	private const uint _CHANGED_OUTPUT_MODE = 0x0001;
	private const int _LAUNCH_RESULT = 42;

	[Fact]
	public void TheModesAfterALaunchThatChangedThemEqualTheModesBeforeIt()
	{
		FakeNativeConsole console = FakeNativeConsole.WithModes(_ORIGINAL_INPUT_MODE, _ORIGINAL_OUTPUT_MODE);
		ConsoleModeGuard guard = new(console);

		int result = guard.Run(() => ChangeModesLikeAChild(console));

		Assert.Equal(_LAUNCH_RESULT, result);
		Assert.Equal(_ORIGINAL_INPUT_MODE, console.ModeOf(NativeConsoleStream.Input));
		Assert.Equal(_ORIGINAL_OUTPUT_MODE, console.ModeOf(NativeConsoleStream.Output));
	}

	[Fact]
	public void TheModesAreSavedBeforeTheLaunchStartsAndNotAfterwards()
	{
		FakeNativeConsole console = FakeNativeConsole.WithModes(_ORIGINAL_INPUT_MODE, _ORIGINAL_OUTPUT_MODE);
		ConsoleModeGuard guard = new(console);
		int readsBeforeLaunch = 0;

		guard.Run(() =>
		{
			readsBeforeLaunch = console.ModeReads;

			return ChangeModesLikeAChild(console);
		});

		Assert.Equal(2, readsBeforeLaunch);
		Assert.Equal(2, console.ModeReads);
	}

	[Fact]
	public void TheModesAreRestoredWhenTheLaunchThrows()
	{
		FakeNativeConsole console = FakeNativeConsole.WithModes(_ORIGINAL_INPUT_MODE, _ORIGINAL_OUTPUT_MODE);
		ConsoleModeGuard guard = new(console);

		Assert.Throws<InvalidOperationException>(() => guard.Run<int>(() =>
		{
			ChangeModesLikeAChild(console);

			throw new InvalidOperationException("The launch failed.");
		}));

		Assert.Equal(_ORIGINAL_INPUT_MODE, console.ModeOf(NativeConsoleStream.Input));
		Assert.Equal(_ORIGINAL_OUTPUT_MODE, console.ModeOf(NativeConsoleStream.Output));
		Assert.Equal(1, console.FlushCount);
	}

	[Fact]
	public void WithoutAConsoleNothingIsSavedOrRestoredAndTheLaunchStillRuns()
	{
		FakeNativeConsole console = FakeNativeConsole.WithoutConsole();
		ConsoleModeGuard guard = new(console);

		int result = guard.Run(() => _LAUNCH_RESULT);

		Assert.Equal(_LAUNCH_RESULT, result);
		Assert.Equal(0, console.ModeWrites);
	}

	[Fact]
	public void OnlyTheStreamThatHasAConsoleIsRestored()
	{
		FakeNativeConsole console = FakeNativeConsole.WithModes(_ORIGINAL_INPUT_MODE, _ORIGINAL_OUTPUT_MODE);

		console.HideStream(NativeConsoleStream.Output);

		ConsoleModeGuard guard = new(console);

		guard.Run(() => ChangeModesLikeAChild(console));

		Assert.Equal(_ORIGINAL_INPUT_MODE, console.ModeOf(NativeConsoleStream.Input));
		Assert.Equal(2, console.ModeWrites);
	}

	[Fact]
	public void InputTypedAheadIsFlushedAfterTheChildReturnsAndOnDemandBeforeARead()
	{
		FakeNativeConsole console = FakeNativeConsole.WithModes(_ORIGINAL_INPUT_MODE, _ORIGINAL_OUTPUT_MODE);
		ConsoleModeGuard guard = new(console);

		guard.FlushPendingInput();

		Assert.Equal(1, console.FlushCount);

		guard.Run(() => _LAUNCH_RESULT);

		Assert.Equal(2, console.FlushCount);
	}

	[Fact]
	public void TheFlushHappensAfterTheChildHasEndedAndNotWhileItRuns()
	{
		FakeNativeConsole console = FakeNativeConsole.WithModes(_ORIGINAL_INPUT_MODE, _ORIGINAL_OUTPUT_MODE);
		ConsoleModeGuard guard = new(console);
		int flushesWhileTheChildRan = -1;

		guard.Run(() =>
		{
			flushesWhileTheChildRan = console.FlushCount;

			return _LAUNCH_RESULT;
		});

		Assert.Equal(0, flushesWhileTheChildRan);
		Assert.Equal(1, console.FlushCount);
	}

	[Fact]
	public void TheGuardForAPlatformWithoutAConsoleDoesNothingAndRunsTheLaunch()
	{
		ConsoleModeGuard guard = new(new NoNativeConsole());

		Assert.Equal(_LAUNCH_RESULT, guard.Run(() => _LAUNCH_RESULT));
		Assert.False(new NoNativeConsole().TryGetMode(NativeConsoleStream.Input, out _));
		Assert.False(new NoNativeConsole().TrySetMode(NativeConsoleStream.Input, 0));
		Assert.False(new NoNativeConsole().TryFlushInput());
	}

	[Fact]
	public void TheGuardOfThisPlatformRunsALaunchAndReturnsItsResult()
	{
		Assert.Equal(_LAUNCH_RESULT, ConsoleModeGuard.ForThisPlatform().Run(() => _LAUNCH_RESULT));
	}

	private static int ChangeModesLikeAChild(FakeNativeConsole console)
	{
		console.TrySetMode(NativeConsoleStream.Input, _CHANGED_INPUT_MODE);
		console.TrySetMode(NativeConsoleStream.Output, _CHANGED_OUTPUT_MODE);

		return _LAUNCH_RESULT;
	}

	private sealed class FakeNativeConsole : INativeConsole
	{
		private readonly Dictionary<NativeConsoleStream, uint> _modes = [];
		private readonly HashSet<NativeConsoleStream> _hidden = [];

		internal int ModeReads { get; private set; }

		internal int ModeWrites { get; private set; }

		internal int FlushCount { get; private set; }

		internal static FakeNativeConsole WithModes(uint inputMode, uint outputMode)
		{
			FakeNativeConsole console = new();

			console._modes[NativeConsoleStream.Input] = inputMode;
			console._modes[NativeConsoleStream.Output] = outputMode;

			return console;
		}

		internal static FakeNativeConsole WithoutConsole()
		{
			return new FakeNativeConsole();
		}

		internal uint ModeOf(NativeConsoleStream stream)
		{
			return _modes[stream];
		}

		internal void HideStream(NativeConsoleStream stream)
		{
			_hidden.Add(stream);
		}

		public bool TryGetMode(NativeConsoleStream stream, out uint mode)
		{
			bool found = _modes.TryGetValue(stream, out mode) && !_hidden.Contains(stream);

			ModeReads += found ? 1 : 0;

			return found;
		}

		public bool TrySetMode(NativeConsoleStream stream, uint mode)
		{
			bool set = _modes.ContainsKey(stream) && !_hidden.Contains(stream);

			if (set)
			{
				_modes[stream] = mode;
				ModeWrites++;
			}

			return set;
		}

		public bool TryFlushInput()
		{
			FlushCount++;

			return _modes.Count > 0;
		}
	}
}
