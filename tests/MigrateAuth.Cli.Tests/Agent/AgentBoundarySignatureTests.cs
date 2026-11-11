using System.Reflection;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE AGENT BOUNDARY'S TWO PROHIBITIONS, ASSERTED OVER THE SIGNATURES RATHER THAN BY READING THEM.
///
/// The contract states them separately because they close different doors. The LAUNCH boundary must
/// not let a repository-derived type reach the external tool at all; the PACK's content must be the
/// finding record plus static template text and nothing else. Both are properties of a signature, so
/// both are checkable here without running a launcher.
///
/// EACH TEST BELOW ENUMERATES WHAT IS PERMITTED AND FAILS ON ANYTHING ELSE, in the manner
/// <see cref="MigrateAuth.Cli.Tests.Emission.EmissionPointSignatureTests"/> already uses for the
/// emission point. A permitted list is the assertion a reviewer is meant to be asked about: widening
/// it is a visible line in a diff, whereas a test that only banned <c>string</c> would pass the day
/// someone wrapped a matched line in a type.
/// </summary>
public sealed class AgentBoundarySignatureTests
{
	private const int _CONTRACT_OPERATION_COUNT = 4;
	private const string _TRY_FACTORY_PREFIX = "Try";

	/// <summary>
	/// A line of the shape an analyzer matches, invented for this test. It is what the
	/// content rule exists to keep out of a pack, and no parameter on either side accepts it.
	/// </summary>
	private const string _SAMPLE_ANALYZED_LINE = "var credential = \"invented-for-this-test\";";

	/// <summary>
	/// Everything the three operations are allowed to name. <see cref="PromptPackFilePath"/> is the
	/// only path that crosses, and it is obtainable only from the documentation destination plus a
	/// completed write - never from the repository under assessment.
	/// </summary>
	private static readonly Type[] _permittedContractTypes =
	[
		typeof(PromptPackFilePath),
		typeof(AgentAvailability),
		typeof(AgentLaunchEligibility),
		typeof(AgentTerminationStatus),
		typeof(AgentResolutionDescription)
	];

	/// <summary>
	/// Types that can carry text from the analyzed repository, named rather than inferred. None may
	/// appear anywhere in the contract, and the second and third are the near misses - a repository
	/// folder or the relative path of an analyzed file would each look reasonable at a call site.
	/// </summary>
	private static readonly Type[] _repositoryDerivedTypes =
	[
		typeof(string),
		typeof(RepositoryFolderPath),
		typeof(RepositoryRelativePath),
		typeof(Finding),
		typeof(RecognizedFile),
		typeof(Stream),
		typeof(TextReader),
		typeof(TextWriter),
		typeof(byte[]),
		typeof(char[]),
		typeof(ReadOnlyMemory<char>)
	];

	/// <summary>
	/// The four finding-record fields, in the types <see cref="Finding"/> carries them in, plus
	/// the case that selects the static text, the sequential index, the flow and the default-flow mark.
	/// A ninth entry here is the change to refuse.
	/// </summary>
	private static readonly Type[] _permittedPackEntryParameterTypes =
	[
		typeof(RuleIdentifier),
		typeof(RepositoryRelativePath),
		typeof(int),
		typeof(ConfidenceLevel),
		typeof(RecommendationKind),
		typeof(int),
		typeof(CredentialFlow?),
		typeof(bool)
	];

	private static MethodInfo[] ContractOperations =>
		typeof(IExternalAgentLauncher).GetMethods(BindingFlags.Public | BindingFlags.Instance);

	private static Type[] TypesNamedByTheContract =>
	[
		.. ContractOperations
			.SelectMany(operation => operation.GetParameters().Select(parameter => parameter.ParameterType))
			.Concat(ContractOperations.Select(operation => operation.ReturnType))
	];

	[Fact]
	public void TheContractCarriesExactlyTheOperationsTheAgentBoundaryNames()
	{
		Assert.Equal(_CONTRACT_OPERATION_COUNT, ContractOperations.Length);
		Assert.Equal(
			[
				nameof(IExternalAgentLauncher.CheckAvailability),
				nameof(IExternalAgentLauncher.CheckLaunchable),
				nameof(IExternalAgentLauncher.DescribeResolution),
				nameof(IExternalAgentLauncher.Launch)
			],
			ContractOperations.Select(operation => operation.Name).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void EveryTypeNamedByTheContractIsOnThePermittedList()
	{
		Type[] namedTypes = TypesNamedByTheContract;

		Assert.NotEmpty(namedTypes);
		Assert.All(namedTypes, namedType => Assert.Contains(namedType, _permittedContractTypes));
	}

	[Fact]
	public void NoRepositoryDerivedTypeReachesTheContract()
	{
		Type[] namedTypes = TypesNamedByTheContract;

		Assert.All(
			_repositoryDerivedTypes,
			repositoryDerivedType => Assert.DoesNotContain(repositoryDerivedType, namedTypes));
	}

	/// <summary>
	/// The launch takes the pack path and nothing else. The third operation is this one's answer
	/// rather than a separate member, because the child inherits the terminal and the call blocks
	/// until it is gone.
	/// </summary>
	[Fact]
	public void TheLaunchTakesThePackPathAndReturnsTheTerminationStatus()
	{
		MethodInfo launch = typeof(IExternalAgentLauncher).GetMethod(nameof(IExternalAgentLauncher.Launch))!;

		Assert.Equal([typeof(PromptPackFilePath)], launch.GetParameters().Select(parameter => parameter.ParameterType));
		Assert.Equal(typeof(AgentTerminationStatus), launch.ReturnType);
	}

	[Fact]
	public void AvailabilityIsAskedWithoutBeingToldAnything()
	{
		MethodInfo checkAvailability =
			typeof(IExternalAgentLauncher).GetMethod(nameof(IExternalAgentLauncher.CheckAvailability))!;

		Assert.Empty(checkAvailability.GetParameters());
		Assert.Equal(typeof(AgentAvailability), checkAvailability.ReturnType);
	}

	/// <summary>
	/// The offer gate takes the pack that was written and nothing else, and answers with a closed
	/// enumeration: no text and no repository-derived type enters or leaves it.
	/// </summary>
	[Fact]
	public void TheLaunchEligibilityCheckTakesTheWrittenPackAndReturnsAClosedEnumeration()
	{
		MethodInfo checkLaunchable =
			typeof(IExternalAgentLauncher).GetMethod(nameof(IExternalAgentLauncher.CheckLaunchable))!;

		Assert.Equal([typeof(PromptPackFilePath)], checkLaunchable.GetParameters().Select(parameter => parameter.ParameterType));
		Assert.Equal(typeof(AgentLaunchEligibility), checkLaunchable.ReturnType);
		Assert.True(typeof(AgentLaunchEligibility).IsEnum);
	}

	/// <summary>The description of the resolution carries a file name, a start kind and two positions: no absolute path.</summary>
	[Fact]
	public void TheResolutionDescriptionTakesNothingAndCarriesNoPath()
	{
		MethodInfo describe = typeof(IExternalAgentLauncher).GetMethod(nameof(IExternalAgentLauncher.DescribeResolution))!;

		Assert.Empty(describe.GetParameters());
		Assert.Equal(typeof(AgentResolutionDescription), describe.ReturnType);
		Assert.Equal(
			["FileName", "StartKind", "EntryPosition", "EntryCount"],
			typeof(AgentResolutionDescription).GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(property => property.Name != "EqualityContract")
				.Select(property => property.Name));
		Assert.Equal(["Direct", "ThroughCommandInterpreter"], Enum.GetNames<AgentStartKind>());
	}

	[Fact]
	public void TheAvailabilityAndEligibilityEnumerationsHaveExactlyTheirDeclaredValues()
	{
		Assert.Equal(
			["NotFound", "Available", "NotFoundMatchInsideRepositoryIgnored"],
			Enum.GetNames<AgentAvailability>());
		Assert.Equal(
			["Launchable", "UnsafeCommandLine", "PathContainsControlCharacter", "WrapperNeedsLocalRepository"],
			Enum.GetNames<AgentLaunchEligibility>());
	}

	/// <summary>
	/// The one path that crosses cannot be built out of text, for the reason
	/// <see cref="RepositoryRelativePath"/> cannot either: a string parameter named after a path
	/// accepts a matched line just as readily.
	/// </summary>
	[Fact]
	public void ThePackPathHasNoPublicConstructorAndNoFactoryThatTakesText()
	{
		MethodInfo[] declaredFactories =
		[
			.. typeof(PromptPackFilePath)
				.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
				.Where(method => !method.IsSpecialName)
		];

		Assert.Empty(typeof(PromptPackFilePath).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
		Assert.NotEmpty(declaredFactories);
		Assert.All(declaredFactories, factory => Assert.StartsWith(_TRY_FACTORY_PREFIX, factory.Name, StringComparison.Ordinal));
		Assert.All(declaredFactories, factory => Assert.Equal(typeof(bool), factory.ReturnType));
		Assert.All(
			declaredFactories.SelectMany(factory => factory.GetParameters()),
			parameter => Assert.NotEqual(typeof(string), parameter.ParameterType));
	}

	/// <summary>
	/// What the one factory accepts, named. Both halves are types that cannot carry repository
	/// content: the destination folder is never the audited repository, and a write result reports a
	/// file that already passed the write boundary.
	/// </summary>
	[Fact]
	public void ThePackPathIsBuiltFromTheDestinationAndACompletedWrite()
	{
		MethodInfo factory = typeof(PromptPackFilePath).GetMethod(nameof(PromptPackFilePath.TryForWrittenPack))!;

		Assert.Equal(
			[
				typeof(DocumentationDestinationFolderPath),
				typeof(ArtifactWriteResult),
				typeof(PromptPackFilePath).MakeByRefType()
			],
			factory.GetParameters().Select(parameter => parameter.ParameterType));
	}

	[Fact]
	public void ThePackEntryHasOneConstructorAndItTakesTheFindingRecordFieldsOnly()
	{
		ConstructorInfo[] constructors =
			typeof(PromptPackFindingEntry).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

		ConstructorInfo onlyConstructor = Assert.Single(constructors);

		Assert.Equal(
			_permittedPackEntryParameterTypes,
			onlyConstructor.GetParameters().Select(parameter => parameter.ParameterType));
	}

	[Fact]
	public void ThePackEntryExposesOnlyClosedValuesAndTheTextTheRouteSelects()
	{
		string[] publicMembers =
		[
			.. typeof(PromptPackFindingEntry)
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(property => property.Name != "EqualityContract")
				.Select(property => $"{property.Name}:{property.PropertyType.Name}")
				.Order(StringComparer.Ordinal)
		];

		Assert.Equal(
			[
				"Flow:Nullable`1",
				"FlowWasDefaulted:Boolean",
				"Guidance:RecommendationKind",
				"Identifier:String",
				"InstructionText:String",
				"Level:ConfidenceLevel",
				"Line:Int32",
				"Path:RepositoryRelativePath",
				"PatternClass:RuleIdentifier",
				"SequentialIndex:Int32"
			],
			publicMembers);
	}

	/// <summary>
	/// The acceptance condition in its own words: including a fragment of the analyzed file would
	/// require changing this signature. No parameter can hold one, and the static text arrives as a
	/// CASE rather than as a value, so there is nowhere to put one.
	/// </summary>
	[Fact]
	public void NoPackEntryParameterCanHoldAFragmentOfTheAnalyzedFile()
	{
		IEnumerable<Type> parameterTypes = typeof(PromptPackFindingEntry)
			.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
			.SelectMany(constructor => constructor.GetParameters())
			.Select(parameter => parameter.ParameterType);

		Assert.All(
			_repositoryDerivedTypes.Where(candidate => candidate != typeof(RepositoryRelativePath)),
			repositoryDerivedType => Assert.DoesNotContain(repositoryDerivedType, parameterTypes));
	}

	/// <summary>
	/// The single exemption is the relative path, and it is the same exemption the write boundary already grants
	/// the emission point - obtainable only through the write boundary's containment primitive, and
	/// never from arbitrary text.
	/// </summary>
	[Fact]
	public void ThePackEntrysOnlyTextBearingFieldIsAPathThatCannotBeBuiltFromText()
	{
		Assert.Contains(typeof(RepositoryRelativePath), _permittedPackEntryParameterTypes);
		Assert.Empty(typeof(RepositoryRelativePath).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
		Assert.False(RuleIdentifier.TryFromPatternClass(_SAMPLE_ANALYZED_LINE, out RuleIdentifier rejected));
		Assert.False(rejected.IsDeclared);
	}

	/// <summary>
	/// The static template text is a compile-time constant of this assembly in every instance that
	/// can exist, and the enumeration is what selects it. A <c>string</c> parameter in its place is
	/// the opening the content rule exists to close.
	/// </summary>
	[Theory]
	[InlineData(RecommendationKind.FromTemplate)]
	[InlineData(RecommendationKind.ManualReview)]
	[InlineData(RecommendationKind.Generic)]
	public void EveryReachableInstructionTextIsOneOfTheDeclaredConstants(RecommendationKind guidance)
	{
		string[] declaredTexts =
		[
			PromptPackInstructionText.CHARACTERIZED_CONSTRUCT_TEXT,
			PromptPackInstructionText.MANUAL_REVIEW_TEXT,
			PromptPackInstructionText.UNCHARACTERIZED_CONSTRUCT_TEXT
		];

		Assert.Contains(PromptPackInstructionText.Describe(guidance), declaredTexts);
	}
}
