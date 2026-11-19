using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.Documentation;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// SOURCE RULES FOR STARTING A PROCESS, read from the product's own files, because a process started
/// from anywhere else is a way for text the product did not decide to become a program that runs.
///
/// The product starts exactly one kind of process, in exactly one file: the agent, by a path the resolver
/// settled from the injected search path, or the Windows command interpreter by its absolute path from the
/// system folder. Direct launches never go through a shell and never redirect; the one constructed command
/// line is the interpreter's, built by the allow-list class; and the environment is read in one place, for
/// the search path, with the literal name.
/// </summary>
public sealed class AgentProcessStartSourceRulesTests
{
	private const string _BUILD_OUTPUT_OBJECT_FOLDER = "obj";
	private const string _BUILD_OUTPUT_BINARY_FOLDER = "bin";
	private const string _PRODUCT_FOLDER = "MigrateAuth.Cli";
	private const string _AGENT_FOLDER = "Agent";
	private const string _CHILD_PROCESS_FILE = "AgentChildProcess.cs";
	private const string _PROVIDER_FILE = "PathEnvironmentSearchPathProvider.cs";
	private const string _PROGRAM_FILE = "Program.cs";
	private const string _CATALOG_FOLDER = "Catalog";
	private const string _CONSOLE_TEXT_FILE = "AiAssistTexts.cs";
	private const string _INTERPRETER_NAME = "cmd.exe";
	private const string _INTERPRETER_SENTENCE = "through the Windows command interpreter (cmd.exe)";
	private const string _PROCESS_START_CALL = "Process.Start(";
	private const string _START_INFO_TYPE = "ProcessStartInfo";
	private const string _SEARCH_PATH_NAME_CONSTANT = "PATH_VARIABLE_NAME = \"PATH\"";

	[Fact]
	public void TheProductStartsAProcessInOneFileOnly()
	{
		string[] callers = [.. ProductFiles().Where(file => ReadSource(file).Contains(_PROCESS_START_CALL, StringComparison.Ordinal))];

		Assert.Equal([_CHILD_PROCESS_FILE], callers.Select(file => Path.GetFileName(file)));
		Assert.Equal(1, Regex.Count(ReadSource(callers[0]), Regex.Escape(_PROCESS_START_CALL)));
	}

	[Fact]
	public void TheStartInformationIsBuiltInTheAgentFolderOnly()
	{
		string[] users = [.. ProductFiles().Where(file => ReadSource(file).Contains(_START_INFO_TYPE, StringComparison.Ordinal))];

		Assert.NotEmpty(users);
		Assert.All(users, file => Assert.Equal(_AGENT_FOLDER, Path.GetFileName(Path.GetDirectoryName(file))));
	}

	[Fact]
	public void TheFileNameIsAResolvedPathOrTheInterpreterFromTheSystemFolderAndNothingElse()
	{
		string[] assignments = FileNameAssignments();

		Assert.Equal(
			[
				"FileName = Path.Combine(Environment.SystemDirectory, _COMMAND_INTERPRETER_FILE_NAME);",
				"FileName = resolution.ExecutablePath;"
			],
			assignments);
	}

	[Fact]
	public void TheInterpreterLineIsOnlyEverTheOneTheAllowListClassBuilds()
	{
		string source = ChildProcessSource();
		string[] assignments = [.. Regex.Matches(source, @"startInfo\.Arguments\s*=[^;]*;").Select(match => match.Value)];

		Assert.Equal(
			["startInfo.Arguments = AgentCommandLineSafety.BuildWrapperCommandLine(resolution.ExecutablePath, arguments);"],
			assignments.Select(assignment => Regex.Replace(assignment, @"\s+", " ")));
		Assert.Contains("startInfo.ArgumentList.Add(argument)", source, StringComparison.Ordinal);
	}

	[Fact]
	public void NoShellIsUsedAndNothingIsRedirected()
	{
		string source = ChildProcessSource();

		Assert.Contains("UseShellExecute = false", source, StringComparison.Ordinal);
		Assert.DoesNotContain("UseShellExecute = true", source, StringComparison.Ordinal);
		Assert.DoesNotContain("Redirect", source, StringComparison.Ordinal);
		Assert.DoesNotContain("Verb", source, StringComparison.Ordinal);
		Assert.DoesNotContain("CreateNoWindow", source, StringComparison.Ordinal);
	}

	[Fact]
	public void TheChildProcessStartsTheInterpreterFromTheSystemFolderAndOnlyTheConsoleTextsAlsoNameIt()
	{
		string source = ChildProcessSource();

		Assert.Contains("Environment.SystemDirectory", source, StringComparison.Ordinal);
		Assert.Equal(1, Regex.Count(source, @"""cmd\.exe"""));

		string[] namingTheInterpreter =
		[
			.. ProductFiles()
				.Where(file => Path.GetFileName(file) != _CHILD_PROCESS_FILE && !IsInsideCatalog(file))
				.Where(file => ReadSourceWithoutComments(file).Contains(_INTERPRETER_NAME, StringComparison.OrdinalIgnoreCase))
				.Select(file => Path.GetFileName(file))
		];

		Assert.Equal([_CONSOLE_TEXT_FILE], namingTheInterpreter);
	}

	/// <summary>The one exemption is the console text file, and there the interpreter is named once, inside the sentence the user reads.</summary>
	[Fact]
	public void TheConsoleTextFileNamesTheInterpreterOnlyInItsOneSentence()
	{
		string source = ReadSourceWithoutComments(ProductFiles().Single(file => Path.GetFileName(file) == _CONSOLE_TEXT_FILE));

		Assert.Equal(1, Regex.Count(source, Regex.Escape(_INTERPRETER_NAME)));
		Assert.Contains(_INTERPRETER_SENTENCE, source, StringComparison.Ordinal);
		Assert.DoesNotContain(_PROCESS_START_CALL, source, StringComparison.Ordinal);
		Assert.DoesNotContain(_START_INFO_TYPE, source, StringComparison.Ordinal);
	}

	[Fact]
	public void TheEnvironmentIsReadInTheProviderFileOnlyAndOnlyForTheSearchPath()
	{
		string[] readers = [.. AgentFiles().Where(file => ReadSource(file).Contains("GetEnvironmentVariable", StringComparison.Ordinal))];

		Assert.Equal([_PROVIDER_FILE], readers.Select(file => Path.GetFileName(file)));
		Assert.Contains(_SEARCH_PATH_NAME_CONSTANT, ReadSource(readers[0]), StringComparison.Ordinal);
		Assert.DoesNotContain(AgentFiles(), file => ReadSource(file).Contains("GetCommandLineArgs", StringComparison.Ordinal));
	}

	[Fact]
	public void TheShippedProviderIsConstructedOnlyWhereTheProductIsComposed()
	{
		string[] constructors =
		[
			.. ProductFiles().Where(file => ReadSource(file).Contains("new PathEnvironmentSearchPathProvider(", StringComparison.Ordinal))
		];

		Assert.All(constructors, file => Assert.Equal(_PROGRAM_FILE, Path.GetFileName(file)));
	}

	private static string[] FileNameAssignments()
	{
		return
		[
			.. ProductFiles()
				.Where(file => ReadSource(file).Contains(_START_INFO_TYPE, StringComparison.Ordinal))
				.SelectMany(file => Regex.Matches(ReadSource(file), @"\bFileName\s*=[^;]*;").Select(match => Regex.Replace(match.Value, @"\s+", " ")))
		];
	}

	private static string ChildProcessSource()
	{
		return ReadSource(AgentFiles().Single(file => Path.GetFileName(file) == _CHILD_PROCESS_FILE));
	}

	private static string[] AgentFiles()
	{
		return [.. Directory.EnumerateFiles(AgentFolderPath(), "*.cs", SearchOption.TopDirectoryOnly)];
	}

	private static string AgentFolderPath()
	{
		return Path.Combine(PublicDocuments.RepositoryRoot(), "src", _PRODUCT_FOLDER, _AGENT_FOLDER);
	}

	/// <summary>The product's source files, leaving out the catalog's embedded pattern text, which names programs as data.</summary>
	private static string[] ProductFiles()
	{
		string productRoot = Path.Combine(PublicDocuments.RepositoryRoot(), "src", _PRODUCT_FOLDER);

		return
		[
			.. Directory.EnumerateFiles(productRoot, "*.cs", SearchOption.AllDirectories)
				.Where(file => !IsInsideCatalog(file) && !IsBuildOutput(file, productRoot))
		];
	}

	private static bool IsInsideCatalog(string file)
	{
		return file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains(_CATALOG_FOLDER);
	}

	private static bool IsBuildOutput(string file, string productRoot)
	{
		string firstSegment = Path.GetRelativePath(productRoot, file).Split(Path.DirectorySeparatorChar)[0];

		return firstSegment is _BUILD_OUTPUT_OBJECT_FOLDER or _BUILD_OUTPUT_BINARY_FOLDER;
	}

	private static string ReadSource(string file)
	{
		return File.ReadAllText(file);
	}

	private static string ReadSourceWithoutComments(string file)
	{
		return Regex.Replace(ReadSource(file), @"//.*$", string.Empty, RegexOptions.Multiline);
	}
}
