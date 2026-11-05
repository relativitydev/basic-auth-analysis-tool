using MigrateAuth.Cli.Findings;

using Xunit;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// The evidence values are ordered so that the stronger reading compares greater, and the zero
/// value is "nothing established" - a default-initialized value must never read as a signal.
/// </summary>
public sealed class FlowEvidenceTests
{
	[Fact]
	public void TheZeroValueIsNoEvidence()
	{
		Assert.Equal(FlowEvidence.None, default(FlowEvidence));
		Assert.Equal(0, (int)FlowEvidence.None);
	}

	[Fact]
	public void AnInteractiveSignalOutranksAFixedOneWhichOutranksNone()
	{
		Assert.True(FlowEvidence.Interactive > FlowEvidence.Fixed);
		Assert.True(FlowEvidence.Fixed > FlowEvidence.None);
		Assert.Equal(FlowEvidence.Interactive, Stronger(FlowEvidence.Fixed, FlowEvidence.Interactive));
	}

	[Fact]
	public void TheEnumerationDeclaresExactlyThreeValues()
	{
		Assert.Equal(
			[FlowEvidence.None, FlowEvidence.Fixed, FlowEvidence.Interactive],
			Enum.GetValues<FlowEvidence>());
	}

	private static FlowEvidence Stronger(FlowEvidence first, FlowEvidence second)
	{
		return first > second ? first : second;
	}
}
