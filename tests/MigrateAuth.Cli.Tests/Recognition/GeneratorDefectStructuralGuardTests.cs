using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Documentation;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE STRUCTURAL GUARD OF THE SOURCE-GENERATOR DEFECT, TIED TO THE GENERATOR ACTUALLY IN USE.
///
/// The product project writes the generator's own output under its own <c>obj</c> folder
/// (<c>EmitCompilerGeneratedFiles</c>, never committed). This guard reads that file for the configuration under test and
/// looks for the defect's signature in the code the generator emitted: in the block of a lazy bounded loop that runs when the loop
/// has reached its upper bound (the comment "If the upper bound N has already been reached", with N of 2 or more), the
/// generator removes a CONSTANT number of entries from its backtracking stack (<c>stackpos--;</c> or <c>stackpos -= k;</c>), where
/// a generator without the defect removes the iterations made times the entries each one pushes
/// (<c>stackpos -= lazyloop_iteration * k;</c>). The guard requires ZERO such sites in the code generated for every <c>[GeneratedRegex]</c> of the product
/// project: detection rules, flow signals, demotion markers and the expressions of the lexical profile and of first-line detection. A site is cured
/// by appending the zero-width point of the language's expressions file inside the body of the lazy bounded loop, <c>(?:body POINT){m,n}?</c>, which
/// gives the generator a backtracking construct in the body and so the correct cleanup; the failure message names every expression that has a site and says so.
///
/// WHICH FILE IS READ. The guard reads the file the PRODUCT project emitted, <c>src/MigrateAuth.Cli/obj/{configuration}/{framework}/generated/...</c>, found from the
/// configuration and framework folders this test assembly runs from. Only the product project emits its generated source (<c>EmitCompilerGeneratedFiles</c> is set in its
/// project file and in no other), so this project's own deliberately defective fixtures, declared to keep the malformed-match guard exercised, are never read by it.
/// A missing generated file is a FAILURE, never a skip, because a guard that cannot read what it guards would pass for nothing.
/// </summary>
public sealed class GeneratorDefectStructuralGuardTests
{
	private const string _PRODUCT_PROJECT_FOLDER = "MigrateAuth.Cli";
	private const string _SOURCE_FOLDER = "src";
	private const string _INTERMEDIATE_FOLDER = "obj";
	private const string _GENERATED_FOLDER = "generated";
	private const string _GENERATOR_ASSEMBLY_NAME = "System.Text.RegularExpressions.Generator";
	private const string _GENERATOR_TYPE_NAME = "System.Text.RegularExpressions.Generator.RegexGenerator";
	private const string _EMITTED_FILE_NAME = "RegexGenerator.g.cs";
	private const string _UPPER_BOUND_COMMENT_PREFIX = "// If the upper bound ";
	private const string _UPPER_BOUND_COMMENT_SUFFIX = " has already been reached";
	private const string _BLOCK_END = "}";
	private const string _SINGLE_STACK_REMOVAL = "stackpos--;";
	private const string _CONSTANT_STACK_REMOVAL_PREFIX = "stackpos -= ";
	private const string _STATEMENT_END = ";";
	private const string _NAMESPACE_PREFIX = "namespace ";
	private const string _PARTIAL_TYPE_PREFIX = "partial ";
	private const char _TYPE_NAME_SEPARATOR = ' ';
	private const string _GENERATED_TYPE_PREFIX = "file sealed class ";
	private const string _GENERATED_TYPE_SUFFIX = " : Regex";
	private const string _PARTIAL_METHOD_MARKER = " partial global::System.Text.RegularExpressions.Regex ";
	private const string _GENERATED_NAMESPACE_MARKER = "global::System.Text.RegularExpressions.Generated.";
	private const string _INSTANCE_MEMBER_SUFFIX = ".Instance";
	private const char _PARAMETER_LIST_OPENING = '(';
	private const int _MINIMUM_UPPER_BOUND_OF_AFFECTED_LOOP = 2;
	private const int _NOT_FOUND = -1;
	private const char _BLOCK_LINE_SEPARATOR = '\n';
	private const string _BOUND_OF_ONE_CONDITION = "!= 0";
	private const int _BOUND_OF_ONE = 1;
	private const int _BOUND_ABOVE_ONE = 3;
	private const string _C_SHARP_EXPRESSIONS_CLASS = "CSharpPatternExpressions";
	private const string _JAVA_SCRIPT_EXPRESSIONS_CLASS = "JavaScriptPatternExpressions";

	private readonly ITestOutputHelper _output;

	public GeneratorDefectStructuralGuardTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoExpressionOfTheProductHasADefectiveSiteInItsGeneratedCode()
	{
		EmittedRegexSource source = EmittedRegexSource.Read(LocateTheEmittedSource());
		string[] defectiveNames =
		[
			.. source.Methods
				.Where(method => source.DefectiveSitesByGeneratedType.ContainsKey(method.GeneratedTypeName))
				.Select(method => $"{method.DisplayName} ({source.DefectiveSitesByGeneratedType[method.GeneratedTypeName]} site(s))")
				.Order(StringComparer.Ordinal)
		];

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"Generated types: {0}; partial methods: {1}; defective expressions: {2}; defective sites: {3}.",
			source.GeneratedTypeNames.Count,
			source.Methods.Count,
			source.DefectiveSitesByGeneratedType.Count,
			source.DefectiveSitesByGeneratedType.Values.Sum()));

		Assert.NotEmpty(source.GeneratedTypeNames);
		Assert.Equal(source.GeneratedTypeNames.Count, source.Methods.Count);
		Assert.True(
			defectiveNames.Length == 0,
			$"The generated code of these expressions carries the source-generator defect signature (a lazy bounded loop over a group whose backtracking cleanup removes a constant number of stack entries). Wrap the loop body with the zero-width point of the language's expressions file, (?:body POINT){{m,n}}?: {string.Join(", ", defectiveNames)}.");
	}

	[Fact]
	public void EveryGeneratedTypeOfTheEmittedSourceIsDeclaredByOnePartialMethodTheGuardCanReach()
	{
		EmittedRegexSource source = EmittedRegexSource.Read(LocateTheEmittedSource());

		foreach (EmittedPartialMethod method in source.Methods)
		{
			Assert.Contains(method.GeneratedTypeName, source.GeneratedTypeNames);
			Assert.NotNull(ResolveTheDeclaredExpression(method));
		}

		Assert.Equal(source.GeneratedTypeNames.Order(StringComparer.Ordinal), source.Methods.Select(method => method.GeneratedTypeName).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void TheChangedNeutralPairExpressionAndItsTwoWordExpressionsAreReadByTheGuardAndCarryNoDefectiveSite()
	{
		EmittedRegexSource source = EmittedRegexSource.Read(LocateTheEmittedSource());
		string[] changedExpressions =
		[
			$"{_C_SHARP_EXPRESSIONS_CLASS}.Base64UserPasswordPairConstruction",
			$"{_C_SHARP_EXPRESSIONS_CLASS}.Base64UserPasswordPairUserHalfWords",
			$"{_C_SHARP_EXPRESSIONS_CLASS}.Base64UserPasswordPairSecretHalfWords"
		];

		foreach (string displayName in changedExpressions)
		{
			EmittedPartialMethod method = Assert.Single(source.Methods, candidate => candidate.DisplayName == displayName);

			Assert.DoesNotContain(method.GeneratedTypeName, source.DefectiveSitesByGeneratedType.Keys);
			Assert.NotNull(ResolveTheDeclaredExpression(method));
		}
	}

	[Fact]
	public void TheSettingsPropertyExpressionAndTheMemberExpressionAreReadByTheGuardAndCarryNoDefectiveSite()
	{
		EmittedRegexSource source = EmittedRegexSource.Read(LocateTheEmittedSource());
		string[] addedExpressions =
		[
			$"{_JAVA_SCRIPT_EXPRESSIONS_CLASS}.AuthorizationHeaderValueFromSettingsProperty",
			$"{_JAVA_SCRIPT_EXPRESSIONS_CLASS}.MemberDeclaration"
		];

		foreach (string displayName in addedExpressions)
		{
			EmittedPartialMethod method = Assert.Single(source.Methods, candidate => candidate.DisplayName == displayName);

			Assert.DoesNotContain(method.GeneratedTypeName, source.DefectiveSitesByGeneratedType.Keys);
			Assert.NotNull(ResolveTheDeclaredExpression(method));
		}
	}

	[Theory]
	[InlineData("if (lazyloop_iteration1 >= 5)\n{\nstackpos -= 2;\ngoto AlternationBacktrack17;\n}", 1)]
	[InlineData("if (lazyloop_iteration >= 3)\n{\nstackpos--;\ngoto Label;\n}", 1)]
	[InlineData("if (lazyloop_iteration >= 3)\n{\nstackpos -= lazyloop_iteration * 2;\ngoto Label;\n}", 0)]
	[InlineData("if (lazyloop_iteration != 0)\n{\nstackpos -= 2;\ngoto Label;\n}", 0)]
	[InlineData("if (lazyloop_iteration >= 3)\n{\ngoto Label;\n}", 0)]
	public void TheDefectSignatureIsReadFromTheShapeTheGeneratorEmitsAndNotFromTheFixedForm(string emittedBlock, int expectedSites)
	{
		int upperBound = emittedBlock.Contains(_BOUND_OF_ONE_CONDITION, StringComparison.Ordinal) ? _BOUND_OF_ONE : _BOUND_ABOVE_ONE;
		string[] lines =
		[
			"file sealed class Sample_0 : Regex",
			$"{_UPPER_BOUND_COMMENT_PREFIX}{upperBound}{_UPPER_BOUND_COMMENT_SUFFIX},",
			"// don't continue lazily iterating. Instead, backtrack.",
			.. emittedBlock.Split(_BLOCK_LINE_SEPARATOR)
		];

		EmittedRegexSource source = EmittedRegexSource.ReadLines(lines);

		Assert.Equal(expectedSites, source.DefectiveSitesByGeneratedType.Values.Sum());
	}

	private static string LocateTheEmittedSource()
	{
		DirectoryInfo outputFolder = new(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		string framework = outputFolder.Name;
		string configuration = outputFolder.Parent!.Name;
		string path = Path.Combine(
			PublicDocuments.RepositoryRoot(),
			_SOURCE_FOLDER,
			_PRODUCT_PROJECT_FOLDER,
			_INTERMEDIATE_FOLDER,
			configuration,
			framework,
			_GENERATED_FOLDER,
			_GENERATOR_ASSEMBLY_NAME,
			_GENERATOR_TYPE_NAME,
			_EMITTED_FILE_NAME);

		Assert.True(
			File.Exists(path),
			$"The source the regular-expression generator emitted for the product project was not found at '{path}'. The structural guard cannot read what it guards: build the product project in the configuration under test ('{configuration}') with EmitCompilerGeneratedFiles on, and run it again.");

		return path;
	}

	private static Regex ResolveTheDeclaredExpression(EmittedPartialMethod method)
	{
		const BindingFlags MEMBERS = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
		Type? declaringType = typeof(RecognitionEngine).Assembly.GetType($"{method.Namespace}.{method.ClassName}");
		MethodInfo? declaration = declaringType?.GetMethod(method.MethodName, MEMBERS, Type.EmptyTypes);

		Assert.True(declaration is not null, $"{method.DisplayName}: the partial method could not be found in the assembly by reflection.");
		Assert.Empty(declaration!.GetParameters());

		return (Regex)declaration.Invoke(null, null)!;
	}

	internal sealed record EmittedPartialMethod(string Namespace, string ClassName, string MethodName, string GeneratedTypeName)
	{
		internal string DisplayName => $"{ClassName}.{MethodName}";
	}

	/// <summary>
	/// What the guard reads of the generator's emitted file, in one streaming pass (the file is tens of megabytes): the generated types, the partial
	/// methods that return them, and how many sites of each generated type carry the defect signature.
	/// </summary>
	internal sealed class EmittedRegexSource
	{
		private EmittedRegexSource()
		{
		}

		internal List<string> GeneratedTypeNames { get; } = [];

		internal List<EmittedPartialMethod> Methods { get; } = [];

		internal Dictionary<string, int> DefectiveSitesByGeneratedType { get; } = [];

		internal static EmittedRegexSource Read(string path)
		{
			return ReadLines(File.ReadLines(path));
		}

		internal static EmittedRegexSource ReadLines(IEnumerable<string> lines)
		{
			EmittedRegexSource source = new();
			ReadingState state = new();

			foreach (string line in lines)
			{
				string trimmed = line.Trim();

				if (state.UpperBoundOfTheBlockBeingRead >= _MINIMUM_UPPER_BOUND_OF_AFFECTED_LOOP)
				{
					ReadInsideTheUpperBoundBlock(source, state, trimmed);
				}
				else
				{
					ReadOutsideAnyBlock(source, state, trimmed);
				}
			}

			return source;
		}

		private static void ReadInsideTheUpperBoundBlock(EmittedRegexSource source, ReadingState state, string trimmed)
		{
			if (trimmed == _BLOCK_END)
			{
				if (state.BlockRemovesAConstantNumberOfEntries)
				{
					source.DefectiveSitesByGeneratedType[state.CurrentGeneratedType] = source.DefectiveSitesByGeneratedType.GetValueOrDefault(state.CurrentGeneratedType) + 1;
				}

				state.UpperBoundOfTheBlockBeingRead = 0;
				state.BlockRemovesAConstantNumberOfEntries = false;
			}
			else if (RemovesAConstantNumberOfEntries(trimmed))
			{
				state.BlockRemovesAConstantNumberOfEntries = true;
			}
		}

		private static void ReadOutsideAnyBlock(EmittedRegexSource source, ReadingState state, string trimmed)
		{
			if (trimmed.StartsWith(_NAMESPACE_PREFIX, StringComparison.Ordinal))
			{
				state.CurrentNamespace = trimmed[_NAMESPACE_PREFIX.Length..];
			}
			else if (trimmed.StartsWith(_PARTIAL_TYPE_PREFIX, StringComparison.Ordinal) && !trimmed.Contains(_PARAMETER_LIST_OPENING, StringComparison.Ordinal))
			{
				state.CurrentClass = trimmed[(trimmed.LastIndexOf(_TYPE_NAME_SEPARATOR) + 1)..];
			}
			else if (trimmed.StartsWith(_GENERATED_TYPE_PREFIX, StringComparison.Ordinal) && trimmed.EndsWith(_GENERATED_TYPE_SUFFIX, StringComparison.Ordinal))
			{
				state.CurrentGeneratedType = trimmed[_GENERATED_TYPE_PREFIX.Length..^_GENERATED_TYPE_SUFFIX.Length];
				source.GeneratedTypeNames.Add(state.CurrentGeneratedType);
			}
			else if (trimmed.Contains(_PARTIAL_METHOD_MARKER, StringComparison.Ordinal))
			{
				source.Methods.Add(ReadPartialMethod(state, trimmed));
			}
			else if (trimmed.StartsWith(_UPPER_BOUND_COMMENT_PREFIX, StringComparison.Ordinal))
			{
				state.UpperBoundOfTheBlockBeingRead = ReadTheUpperBound(trimmed);
			}
		}

		private static EmittedPartialMethod ReadPartialMethod(ReadingState state, string trimmed)
		{
			int nameStart = trimmed.IndexOf(_PARTIAL_METHOD_MARKER, StringComparison.Ordinal) + _PARTIAL_METHOD_MARKER.Length;
			int nameEnd = trimmed.IndexOf(_PARAMETER_LIST_OPENING, nameStart);
			int typeStart = trimmed.IndexOf(_GENERATED_NAMESPACE_MARKER, StringComparison.Ordinal) + _GENERATED_NAMESPACE_MARKER.Length;
			int typeEnd = trimmed.IndexOf(_INSTANCE_MEMBER_SUFFIX, typeStart, StringComparison.Ordinal);

			Assert.True(nameEnd != _NOT_FOUND && typeStart >= _GENERATED_NAMESPACE_MARKER.Length && typeEnd != _NOT_FOUND, $"The partial method declaration could not be read: {trimmed}");

			return new EmittedPartialMethod(state.CurrentNamespace, state.CurrentClass, trimmed[nameStart..nameEnd], trimmed[typeStart..typeEnd]);
		}

		private static int ReadTheUpperBound(string comment)
		{
			string rest = comment[_UPPER_BOUND_COMMENT_PREFIX.Length..];
			int end = rest.IndexOf(_UPPER_BOUND_COMMENT_SUFFIX, StringComparison.Ordinal);
			int upperBound = 0;

			if (end > 0 && int.TryParse(rest[..end], NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
			{
				upperBound = parsed;
			}

			return upperBound;
		}

		private static bool RemovesAConstantNumberOfEntries(string trimmed)
		{
			bool removes = trimmed == _SINGLE_STACK_REMOVAL;

			if (!removes && trimmed.StartsWith(_CONSTANT_STACK_REMOVAL_PREFIX, StringComparison.Ordinal) && trimmed.EndsWith(_STATEMENT_END, StringComparison.Ordinal))
			{
				string amount = trimmed[_CONSTANT_STACK_REMOVAL_PREFIX.Length..^_STATEMENT_END.Length];
				removes = amount.Length > 0 && amount.All(char.IsAsciiDigit);
			}

			return removes;
		}

		private sealed class ReadingState
		{
			internal string CurrentNamespace { get; set; } = string.Empty;

			internal string CurrentClass { get; set; } = string.Empty;

			internal string CurrentGeneratedType { get; set; } = string.Empty;

			internal int UpperBoundOfTheBlockBeingRead { get; set; }

			internal bool BlockRemovesAConstantNumberOfEntries { get; set; }
		}
	}
}
