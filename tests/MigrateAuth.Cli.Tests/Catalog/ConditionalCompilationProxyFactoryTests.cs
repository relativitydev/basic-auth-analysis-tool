using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE INACTIVE-REGION READING REACHES THE CLASS CS-020 TOO. The region map applies to every `CSharp` class with no per-class edit, and the creation-form matrix
/// (<see cref="ConditionalCompilationEveryClassTests"/>) does not hold CS-020, so it is checked here. A call of the proxy
/// factory that sits in a block the file's own symbols switch off is still recognized as CS-020, now at Low with the inactive reason; the same call in a block only a
/// build setting can switch off keeps High and gets no reason, and outside any directive it reads High.
/// </summary>
public sealed class ConditionalCompilationProxyFactoryTests
{
	private const string _NEWLINE = "\n";
	private const string _PROXY_FACTORY_CLASS = "CS-020";
	private const string _PROXY_FACTORY_SOURCE = "var proxy = helper.GetProxy<IExampleService>(userName, password);";
	private const string _INACTIVE_BLOCK_OPENING = "#if FEAT_NEVER_DEFINED_HERE";
	private const string _MAYBE_BLOCK_OPENING = "#if DEBUG";
	private const string _END_OF_GROUP = "#endif";

	[Fact]
	public void ACallInsideASwitchedOffBlockIsStillCs020AndReadsLowWithTheInactiveReason()
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrapped(_INACTIVE_BLOCK_OPENING));

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(_PROXY_FACTORY_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.True(construct.LowReasons.HasFlag(LowConfidenceReasons.InactivePreprocessorRegion));
	}

	[Fact]
	public void ACallInsideABlockOnlyABuildSettingCanSwitchOffKeepsHighAndGetsNoInactiveReason()
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(Wrapped(_MAYBE_BLOCK_OPENING));

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(_PROXY_FACTORY_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.False(construct.LowReasons.HasFlag(LowConfidenceReasons.InactivePreprocessorRegion));
	}

	[Fact]
	public void TheSameCallOutsideAnyDirectiveReadsHigh()
	{
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize(_PROXY_FACTORY_SOURCE + _NEWLINE);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(_PROXY_FACTORY_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	private static string Wrapped(string blockOpening)
	{
		return $"{string.Join(_NEWLINE, blockOpening, _PROXY_FACTORY_SOURCE, _END_OF_GROUP)}{_NEWLINE}";
	}
}
