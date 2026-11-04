using System.Reflection;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.Emission;

/// <summary>
/// THE HARDCODED-CREDENTIAL REQUIREMENT BY CONSTRUCTION.
///
/// The emission point STRUCTURALLY cannot carry text from the analyzed file: the record that crosses
/// it has six fields, five of them incapable of holding text at all, and the sixth reachable only
/// through the write boundary's containment primitive. Adding a seventh breaks these tests, which is
/// the point - emitting file text must require changing this signature, and this is the test
/// that notices when someone does. The skipped-item category is admitted because a closed
/// enumeration of nine values cannot express one character of an analyzed file. Raising the number
/// for anything that CAN is the change to refuse.
/// </summary>
public sealed class EmissionPointSignatureTests
{
	private const int _PERMITTED_FIELD_COUNT = 6;
	private const string _TRY_FACTORY_PREFIX = "Try";

	/// <summary>
	/// A line of the shape the analyzers will one day match, invented for this test. It is what
	/// the invariant exists to keep out of a report, and neither string-bearing field accepts it.
	/// </summary>
	private const string _SAMPLE_ANALYZED_LINE = "var credential = \"invented-for-this-test\";";

	private static readonly Type[] _permittedFieldTypes =
	[
		typeof(EmissionCategory),
		typeof(RepositoryRelativePath),
		typeof(int),
		typeof(RuleIdentifier),
		typeof(ConfidenceLevel),
		typeof(SkippedItemCategory)
	];

	[Fact]
	public void EmittedFactCarriesExactlyThePermittedFields()
	{
		PropertyInfo[] fields = typeof(EmittedFact).GetProperties(BindingFlags.Public | BindingFlags.Instance);

		Assert.Equal(_PERMITTED_FIELD_COUNT, fields.Length);
		Assert.Equal(
			["Category", "Level", "LineNumber", "Path", "Rule", "SkippedItem"],
			fields.Select(field => field.Name).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void EveryPermittedFieldIsATypeThatCannotHoldArbitraryText()
	{
		PropertyInfo[] fields = typeof(EmittedFact).GetProperties(BindingFlags.Public | BindingFlags.Instance);

		foreach (PropertyInfo field in fields)
		{
			Type fieldType = Nullable.GetUnderlyingType(field.PropertyType) ?? field.PropertyType;

			Assert.Contains(fieldType, _permittedFieldTypes);
			Assert.NotEqual(typeof(string), fieldType);
		}
	}

	[Fact]
	public void TheEmissionPointTakesTheFactAndNothingElse()
	{
		MethodInfo emit = typeof(EmissionPoint).GetMethod(nameof(EmissionPoint.Emit))!;

		Assert.Equal([typeof(EmittedFact)], emit.GetParameters().Select(parameter => parameter.ParameterType));
	}

	[Fact]
	public void NoPublicMemberOfTheEmissionPointOrItsSinkContractAcceptsText()
	{
		IEnumerable<MethodInfo> declaredMethods =
		[
			.. typeof(EmissionPoint).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
			.. typeof(IEmissionSink).GetMethods(BindingFlags.Public | BindingFlags.Instance)
		];

		IEnumerable<Type> parameterTypes = declaredMethods
			.SelectMany(method => method.GetParameters())
			.Select(parameter => parameter.ParameterType);

		Assert.DoesNotContain(typeof(string), parameterTypes);
	}

	[Fact]
	public void NeitherStringBearingFieldHasAConstructorThatTakesText()
	{
		Type[] stringBearingTypes = [typeof(RepositoryRelativePath), typeof(RuleIdentifier)];

		foreach (Type stringBearingType in stringBearingTypes)
		{
			IEnumerable<Type> constructorParameterTypes = stringBearingType
				.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
				.SelectMany(constructor => constructor.GetParameters())
				.Select(parameter => parameter.ParameterType);

			Assert.DoesNotContain(typeof(string), constructorParameterTypes);
		}
	}

	/// <summary>
	/// The only public way into either string-bearing field is a validating factory that can
	/// refuse. A factory that could not refuse would be a constructor with a longer name.
	/// </summary>
	[Fact]
	public void EveryPublicFactoryOfAStringBearingFieldCanRefuse()
	{
		Type[] stringBearingTypes = [typeof(RepositoryRelativePath), typeof(RuleIdentifier)];

		foreach (Type stringBearingType in stringBearingTypes)
		{
			MethodInfo[] factories =
			[
				.. stringBearingType
					.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
					.Where(method => method.ReturnType != typeof(bool) || method.GetParameters().Length > 0)
					.Where(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(string)))
			];

			Assert.NotEmpty(factories);
			Assert.All(factories, factory => Assert.StartsWith(_TRY_FACTORY_PREFIX, factory.Name, StringComparison.Ordinal));
			Assert.All(factories, factory => Assert.Equal(typeof(bool), factory.ReturnType));
		}
	}

	[Fact]
	public void ALineOfAnalyzedSourceIsNotARuleIdentifier()
	{
		Assert.False(RuleIdentifier.TryFromPatternClass(_SAMPLE_ANALYZED_LINE, out RuleIdentifier rejected));
		Assert.False(rejected.IsDeclared);
	}

	[Fact]
	public void ACatalogPatternClassIsARuleIdentifier()
	{
		Assert.True(RuleIdentifier.TryFromPatternClass("CS-001", out RuleIdentifier accepted));
		Assert.Equal("CS-001", accepted.Value);
	}

	[Theory]
	[InlineData("cs-001")]
	[InlineData("CS-1")]
	[InlineData("CS001")]
	[InlineData("C-001")]
	[InlineData("TOOMANYLETTERS-001")]
	[InlineData("CS-001 and then some prose")]
	[InlineData("")]
	public void AnythingThatIsNotACatalogPatternClassIsRefused(string candidate)
	{
		Assert.False(RuleIdentifier.TryFromPatternClass(candidate, out RuleIdentifier _));
	}

	[Fact]
	public void AFactMustDeclareItsCategory()
	{
		EmissionPoint emissionPoint = new([]);

		Assert.Throws<ArgumentException>(() => emissionPoint.Emit(new EmittedFact { Category = EmissionCategory.None }));
	}

	[Fact]
	public void EveryRegisteredSinkReceivesTheSameFact()
	{
		RecordingSink firstSink = new(EmissionDestination.Terminal);
		RecordingSink secondSink = new(EmissionDestination.Markdown);
		EmissionPoint emissionPoint = new([firstSink, secondSink]);
		EmittedFact fact = new() { Category = EmissionCategory.Coverage };

		emissionPoint.Emit(fact);

		Assert.Equal([fact], firstSink.Received);
		Assert.Equal([fact], secondSink.Received);
	}

	private sealed class RecordingSink : IEmissionSink
	{
		public RecordingSink(EmissionDestination destination)
		{
			Destination = destination;
		}

		public EmissionDestination Destination { get; }

		public List<EmittedFact> Received { get; } = [];

		public void Receive(EmittedFact fact)
		{
			Received.Add(fact);
		}
	}
}
