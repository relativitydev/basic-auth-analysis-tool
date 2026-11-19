using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// THE WINDOWS CONSOLE, through the four calls of the system library that the console seam needs. Each is bound
/// at build time by source generation, which is what keeps the published native binary free of runtime code
/// generation, and each is loaded only from the system folder, so no library placed beside the program or in
/// the working directory can answer in its place.
///
/// A handle that is not a console (a redirected stream) makes the mode calls fail, and the failure is the
/// answer: there is nothing to guard.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class WindowsNativeConsole : INativeConsole
{
	private const string _SYSTEM_LIBRARY = "kernel32.dll";
	private const int _STANDARD_INPUT_HANDLE = -10;
	private const int _STANDARD_OUTPUT_HANDLE = -11;
	private const long _INVALID_HANDLE_VALUE = -1;
	private const long _NO_HANDLE_VALUE = 0;

	/// <inheritdoc />
	public bool TryGetMode(NativeConsoleStream stream, out uint mode)
	{
		nint handle = GetStdHandle(HandleNumberOf(stream));
		bool read = false;

		mode = 0;

		if (IsUsable(handle))
		{
			read = GetConsoleMode(handle, out mode);
		}

		return read;
	}

	/// <inheritdoc />
	public bool TrySetMode(NativeConsoleStream stream, uint mode)
	{
		nint handle = GetStdHandle(HandleNumberOf(stream));

		return IsUsable(handle) && SetConsoleMode(handle, mode);
	}

	/// <inheritdoc />
	public bool TryFlushInput()
	{
		nint handle = GetStdHandle(_STANDARD_INPUT_HANDLE);

		return IsUsable(handle) && FlushConsoleInputBuffer(handle);
	}

	[LibraryImport(_SYSTEM_LIBRARY)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static partial nint GetStdHandle(int standardHandle);

	[LibraryImport(_SYSTEM_LIBRARY)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool GetConsoleMode(nint consoleHandle, out uint mode);

	[LibraryImport(_SYSTEM_LIBRARY)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool SetConsoleMode(nint consoleHandle, uint mode);

	[LibraryImport(_SYSTEM_LIBRARY)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool FlushConsoleInputBuffer(nint consoleHandle);

	private static int HandleNumberOf(NativeConsoleStream stream)
	{
		return stream == NativeConsoleStream.Input ? _STANDARD_INPUT_HANDLE : _STANDARD_OUTPUT_HANDLE;
	}

	private static bool IsUsable(nint handle)
	{
		return handle != _NO_HANDLE_VALUE && handle != _INVALID_HANDLE_VALUE;
	}
}
