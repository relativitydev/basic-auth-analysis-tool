using System.Security;

namespace MigrateAuth.Cli;

/// <summary>
/// The exception boundary that sits at the session root. It translates every
/// unforeseen failure into <see cref="ExitCode.UnexpectedInternalError"/> and emits a
/// single description line that carries the exception type, a
/// <see cref="FailureCategory"/>, and - where one applies - the path relative to the
/// scanned root.
///
/// Three things it must never do, and the reason each one matters:
/// it never emits an absolute path, because the scanned tree is the client's;
/// it never emits file content, because the content under analysis is credential text;
/// and it never emits the framework's own exception message, because framework I/O
/// messages embed the absolute path by construction, which is exactly what is forbidden.
///
/// It is also the mechanism that keeps the operating system's crash reporter out of the
/// picture: the process terminates by returning an exit code, never by an unhandled
/// exception, so no crash reporter is ever handed a dying process whose buffers hold the
/// file under analysis.
/// </summary>
public static class SessionExceptionBoundary
{
	/// <summary>Name of the field carrying the failure category.</summary>
	public const string CATEGORY_FIELD_NAME = "category";

	/// <summary>Name of the field carrying the exception type name.</summary>
	public const string EXCEPTION_TYPE_FIELD_NAME = "type";

	/// <summary>Name of the field carrying the path relative to the scanned root.</summary>
	public const string PATH_FIELD_NAME = "path";

	/// <summary>Emitted in the path field when no in-scope path could be established.</summary>
	public const string NO_PATH_AVAILABLE_MARKER = "none";

	/// <summary>Separator between the fields of a description line.</summary>
	public const string FIELD_SEPARATOR = "; ";

	/// <summary>Separator between a field name and its value.</summary>
	public const string KEY_VALUE_SEPARATOR = "=";

	/// <summary>
	/// Key under which callers may attach the path an operation was working on to
	/// <see cref="Exception.Data"/>, so that the boundary can relativize and report it.
	/// </summary>
	public const string PATH_EXCEPTION_DATA_KEY = "migrate-auth.path";

	private const string _DESCRIPTION_PREFIX = "internal error";
	private const string _PARENT_DIRECTORY_SEGMENT = "..";
	private const char _PORTABLE_DIRECTORY_SEPARATOR = '/';

	/// <summary>
	/// Runs the session under the boundary and returns the exit code the process reports.
	/// </summary>
	/// <param name="session">The session to run.</param>
	/// <param name="scannedRootPath">
	/// The root of the scanned tree, used to relativize any path a failure carries. Null
	/// while no repository has been selected; every path is then suppressed.
	/// </param>
	/// <param name="diagnosticWriter">Where the single description line is written.</param>
	public static ExitCode Execute(Func<ExitCode> session, string? scannedRootPath, TextWriter diagnosticWriter)
	{
		ArgumentNullException.ThrowIfNull(session);
		ArgumentNullException.ThrowIfNull(diagnosticWriter);

		ExitCode result;

		try
		{
			result = session();
		}
		catch (Exception failure)
		{
			ReportFailure(failure, scannedRootPath, diagnosticWriter);
			result = ExitCode.UnexpectedInternalError;
		}

		return result;
	}

	/// <summary>
	/// Builds the single line the boundary emits for a failure. Public because its shape
	/// is part of the boundary's contract and is asserted directly by its tests.
	/// </summary>
	public static string DescribeFailure(Exception failure, string? scannedRootPath)
	{
		ArgumentNullException.ThrowIfNull(failure);

		Type failureType = failure.GetType();
		string typeName = failureType.FullName ?? failureType.Name;
		string category = ClassifyFailure(failure).ToString();
		string path = DescribeContainedPath(failure, scannedRootPath);

		string description = $"{_DESCRIPTION_PREFIX}{FIELD_SEPARATOR}{EXCEPTION_TYPE_FIELD_NAME}{KEY_VALUE_SEPARATOR}{typeName}{FIELD_SEPARATOR}{CATEGORY_FIELD_NAME}{KEY_VALUE_SEPARATOR}{category}{FIELD_SEPARATOR}{PATH_FIELD_NAME}{KEY_VALUE_SEPARATOR}{path}";

		return description;
	}

	/// <summary>Maps an exception to the category reported for it.</summary>
	public static FailureCategory ClassifyFailure(Exception failure)
	{
		ArgumentNullException.ThrowIfNull(failure);

		FailureCategory category = failure switch
		{
			OperationCanceledException => FailureCategory.Cancellation,
			UnauthorizedAccessException => FailureCategory.FileAccess,
			SecurityException => FailureCategory.FileAccess,
			IOException => FailureCategory.FileAccess,
			ArgumentException => FailureCategory.InvalidInput,
			FormatException => FailureCategory.InvalidInput,
			InvalidOperationException => FailureCategory.InvalidState,
			NotSupportedException => FailureCategory.InvalidState,
			OutOfMemoryException => FailureCategory.ResourceExhaustion,
			InsufficientExecutionStackException => FailureCategory.ResourceExhaustion,
			_ => FailureCategory.Unknown
		};

		return category;
	}

	private static void ReportFailure(Exception failure, string? scannedRootPath, TextWriter diagnosticWriter)
	{
		try
		{
			diagnosticWriter.WriteLine(DescribeFailure(failure, scannedRootPath));
		}
		catch (IOException)
		{
			// The boundary is the last resort: a diagnostic channel that has itself failed
			// must not change the exit code the caller receives, and must not escape.
		}
		catch (ObjectDisposedException)
		{
		}
	}

	/// <summary>
	/// Produces the path to report, or the no-path marker. A path is reported only when it
	/// can be expressed as a relative path that stays inside the scanned root; anything
	/// rooted, or escaping upwards, is suppressed rather than trimmed, because a suppressed
	/// path costs a triage hint and a leaked one costs a client's directory layout.
	/// </summary>
	private static string DescribeContainedPath(Exception failure, string? scannedRootPath)
	{
		string description = NO_PATH_AVAILABLE_MARKER;
		string? candidatePath = ExtractCandidatePath(failure);

		if (candidatePath is not null && !string.IsNullOrEmpty(scannedRootPath))
		{
			string relativePath = Path.GetRelativePath(scannedRootPath, candidatePath);

			if (IsContainedRelativePath(relativePath))
			{
				description = relativePath.Replace(Path.DirectorySeparatorChar, _PORTABLE_DIRECTORY_SEPARATOR);
			}
		}

		return description;
	}

	private static bool IsContainedRelativePath(string relativePath)
	{
		bool isContained =
			!string.IsNullOrEmpty(relativePath)
			&& !Path.IsPathRooted(relativePath)
			&& !relativePath.StartsWith(_PARENT_DIRECTORY_SEGMENT, StringComparison.Ordinal);

		return isContained;
	}

	private static string? ExtractCandidatePath(Exception failure)
	{
		string? candidatePath = failure switch
		{
			FileNotFoundException fileNotFound => fileNotFound.FileName,
			FileLoadException fileLoad => fileLoad.FileName,
			_ => null
		};

		candidatePath ??= failure.Data[PATH_EXCEPTION_DATA_KEY] as string;

		return candidatePath;
	}
}
