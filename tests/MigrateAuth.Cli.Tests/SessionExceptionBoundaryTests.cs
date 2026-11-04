using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests;

/// <summary>
/// Proves the two obligations on the session boundary: every unforeseen failure
/// becomes exit code 5, and the line it emits leaks neither an absolute path nor the
/// framework's own message.
/// </summary>
public sealed class SessionExceptionBoundaryTests
{
	private const string _SCANNED_ROOT_DIRECTORY_NAME = "scanned-root";
	private const string _IN_SCOPE_RELATIVE_PATH = "src/Client/Connector.cs";
	private const string _OUT_OF_SCOPE_FILE_NAME = "elsewhere.cs";
	private const string _FRAMEWORK_MESSAGE_SENTINEL = "FRAMEWORK-MESSAGE-SENTINEL";
	private const string _WINDOWS_DRIVE_ROOTED_PROBE = ":\\";
	private const ExitCode _ARBITRARY_SUCCESSFUL_SESSION_RESULT = ExitCode.Success;

	private static string ScannedRootPath => Path.Combine(Path.GetTempPath(), _SCANNED_ROOT_DIRECTORY_NAME);

	[Fact]
	public void ExecuteReturnsTheSessionResultWhenNothingFails()
	{
		ExitCode result = SessionExceptionBoundary.Execute(
			() => _ARBITRARY_SUCCESSFUL_SESSION_RESULT,
			ScannedRootPath,
			TextWriter.Null);

		Assert.Equal(_ARBITRARY_SUCCESSFUL_SESSION_RESULT, result);
	}

	[Fact]
	public void ExecuteTranslatesAnyUnforeseenFailureIntoExitCodeFive()
	{
		ExitCode result = SessionExceptionBoundary.Execute(
			() => throw new InvalidProgramException(_FRAMEWORK_MESSAGE_SENTINEL),
			ScannedRootPath,
			TextWriter.Null);

		Assert.Equal(ExitCode.UnexpectedInternalError, result);
		Assert.Equal((int)ExitCode.UnexpectedInternalError, (int)result);
	}

	[Fact]
	public void ExecuteWritesExactlyOneDiagnosticLine()
	{
		Mock<TextWriter> diagnosticWriter = new() { CallBase = false };

		SessionExceptionBoundary.Execute(
			() => throw new InvalidOperationException(_FRAMEWORK_MESSAGE_SENTINEL),
			ScannedRootPath,
			diagnosticWriter.Object);

		diagnosticWriter.Verify(writer => writer.WriteLine(It.IsAny<string>()), Times.Once);
	}

	[Fact]
	public void ExecuteWritesNothingWhenTheSessionSucceeds()
	{
		Mock<TextWriter> diagnosticWriter = new() { CallBase = false };

		SessionExceptionBoundary.Execute(
			() => _ARBITRARY_SUCCESSFUL_SESSION_RESULT,
			ScannedRootPath,
			diagnosticWriter.Object);

		diagnosticWriter.Verify(writer => writer.WriteLine(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public void DescriptionCarriesTheExceptionTypeAndACategory()
	{
		UnauthorizedAccessException failure = new(_FRAMEWORK_MESSAGE_SENTINEL);

		string description = SessionExceptionBoundary.DescribeFailure(failure, ScannedRootPath);

		Assert.Contains(
			$"{SessionExceptionBoundary.EXCEPTION_TYPE_FIELD_NAME}{SessionExceptionBoundary.KEY_VALUE_SEPARATOR}{typeof(UnauthorizedAccessException).FullName}",
			description,
			StringComparison.Ordinal);
		Assert.Contains(
			$"{SessionExceptionBoundary.CATEGORY_FIELD_NAME}{SessionExceptionBoundary.KEY_VALUE_SEPARATOR}{FailureCategory.FileAccess}",
			description,
			StringComparison.Ordinal);
	}

	[Fact]
	public void DescriptionNeverRepeatsTheFrameworkMessage()
	{
		IOException failure = new(_FRAMEWORK_MESSAGE_SENTINEL);

		string description = SessionExceptionBoundary.DescribeFailure(failure, ScannedRootPath);

		Assert.DoesNotContain(_FRAMEWORK_MESSAGE_SENTINEL, description, StringComparison.Ordinal);
	}

	[Fact]
	public void DescriptionReportsAnInScopePathRelativeToTheScannedRoot()
	{
		string absolutePath = Path.Combine(ScannedRootPath, _IN_SCOPE_RELATIVE_PATH);
		IOException failure = new(_FRAMEWORK_MESSAGE_SENTINEL);
		failure.Data[SessionExceptionBoundary.PATH_EXCEPTION_DATA_KEY] = absolutePath;

		string description = SessionExceptionBoundary.DescribeFailure(failure, ScannedRootPath);

		Assert.Equal(_IN_SCOPE_RELATIVE_PATH, ReadPathField(description));
		AssertNoAbsolutePathLeaked(description, absolutePath);
	}

	[Fact]
	public void DescriptionSuppressesAPathThatEscapesTheScannedRoot()
	{
		string absolutePath = Path.Combine(Path.GetTempPath(), _OUT_OF_SCOPE_FILE_NAME);
		FileNotFoundException failure = new(_FRAMEWORK_MESSAGE_SENTINEL, absolutePath);

		string description = SessionExceptionBoundary.DescribeFailure(failure, ScannedRootPath);

		Assert.Equal(SessionExceptionBoundary.NO_PATH_AVAILABLE_MARKER, ReadPathField(description));
		AssertNoAbsolutePathLeaked(description, absolutePath);
	}

	[Fact]
	public void DescriptionSuppressesEveryPathWhileNoRepositoryHasBeenSelected()
	{
		string absolutePath = Path.Combine(ScannedRootPath, _IN_SCOPE_RELATIVE_PATH);
		FileNotFoundException failure = new(_FRAMEWORK_MESSAGE_SENTINEL, absolutePath);

		string description = SessionExceptionBoundary.DescribeFailure(failure, scannedRootPath: null);

		Assert.Equal(SessionExceptionBoundary.NO_PATH_AVAILABLE_MARKER, ReadPathField(description));
		AssertNoAbsolutePathLeaked(description, absolutePath);
	}

	[Theory]
	[InlineData(typeof(UnauthorizedAccessException), FailureCategory.FileAccess)]
	[InlineData(typeof(IOException), FailureCategory.FileAccess)]
	[InlineData(typeof(FileNotFoundException), FailureCategory.FileAccess)]
	[InlineData(typeof(OperationCanceledException), FailureCategory.Cancellation)]
	[InlineData(typeof(ArgumentNullException), FailureCategory.InvalidInput)]
	[InlineData(typeof(FormatException), FailureCategory.InvalidInput)]
	[InlineData(typeof(InvalidOperationException), FailureCategory.InvalidState)]
	[InlineData(typeof(NotSupportedException), FailureCategory.InvalidState)]
	[InlineData(typeof(OutOfMemoryException), FailureCategory.ResourceExhaustion)]
	[InlineData(typeof(InvalidProgramException), FailureCategory.Unknown)]
	public void FailureIsClassifiedByItsType(Type failureType, FailureCategory expectedCategory)
	{
		Exception failure = (Exception)Activator.CreateInstance(failureType)!;

		FailureCategory actualCategory = SessionExceptionBoundary.ClassifyFailure(failure);

		Assert.Equal(expectedCategory, actualCategory);
	}

	private static string ReadPathField(string description)
	{
		string fieldMarker = $"{SessionExceptionBoundary.FIELD_SEPARATOR}{SessionExceptionBoundary.PATH_FIELD_NAME}{SessionExceptionBoundary.KEY_VALUE_SEPARATOR}";
		int valueStart = description.IndexOf(fieldMarker, StringComparison.Ordinal) + fieldMarker.Length;
		string pathField = description[valueStart..];

		return pathField;
	}

	private static void AssertNoAbsolutePathLeaked(string description, string absolutePath)
	{
		Assert.DoesNotContain(absolutePath, description, StringComparison.Ordinal);
		Assert.DoesNotContain(_WINDOWS_DRIVE_ROOTED_PROBE, description, StringComparison.Ordinal);
		Assert.False(Path.IsPathRooted(ReadPathField(description)));
	}
}
