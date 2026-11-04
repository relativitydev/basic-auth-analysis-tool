using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE FIVE AGENT TERMINATION CASES, EACH REPRESENTABLE AND EACH DISTINGUISHABLE FROM THE OTHER FOUR.
///
/// What is exercised is the vocabulary the launch contract answers in. A type that cannot tell an
/// agent that hung (timeout) from an agent that ran and lost cannot report either one correctly.
///
/// The exit-code case is the one worth stating twice. Zero is a real exit code meaning success, so a
/// status that published a plain integer would read as success for the four cases that never
/// produced one. Each test below therefore asserts the absence as well as the presence.
/// </summary>
public sealed class AgentTerminationStatusTests
{
	private const int _DECLARED_OUTCOME_COUNT = 5;
	private const int _DECLARED_AVAILABILITY_COUNT = 2;
	private const int _SUCCESSFUL_EXIT_CODE = 0;
	private const int _FAILING_EXIT_CODE = 3;
	private const int _INTERRUPT_SIGNAL_NUMBER = 2;

	[Fact]
	public void NotFoundCarriesNeitherAnExitCodeNorASignal()
	{
		AgentTerminationStatus status = AgentTerminationStatus.NotFound();

		Assert.Equal(AgentTerminationOutcome.NotFound, status.Outcome);
		Assert.Null(status.ExitCode);
		Assert.Null(status.SignalNumber);
	}

	[Fact]
	public void ExitWithCodeCarriesTheCodeAndNoSignal()
	{
		AgentTerminationStatus status = AgentTerminationStatus.ExitedWithCode(_FAILING_EXIT_CODE);

		Assert.Equal(AgentTerminationOutcome.ExitedWithCode, status.Outcome);
		Assert.Equal(_FAILING_EXIT_CODE, status.ExitCode);
		Assert.Null(status.SignalNumber);
	}

	/// <summary>
	/// The reading that makes the nullable worth its ceremony: an agent that exited successfully and
	/// an agent that was never found both hold zero in a plain integer, and only one of them ran.
	/// </summary>
	[Fact]
	public void AnAgentThatSucceededIsNotAnAgentThatWasNeverFound()
	{
		AgentTerminationStatus succeeded = AgentTerminationStatus.ExitedWithCode(_SUCCESSFUL_EXIT_CODE);
		AgentTerminationStatus notFound = AgentTerminationStatus.NotFound();

		Assert.Equal(_SUCCESSFUL_EXIT_CODE, succeeded.ExitCode);
		Assert.Null(notFound.ExitCode);
		Assert.NotEqual(succeeded, notFound);
	}

	[Fact]
	public void TerminationBySignalCarriesTheSignalAndNoExitCode()
	{
		AgentTerminationStatus status = AgentTerminationStatus.TerminatedBySignal(_INTERRUPT_SIGNAL_NUMBER);

		Assert.Equal(AgentTerminationOutcome.TerminatedBySignal, status.Outcome);
		Assert.Equal(_INTERRUPT_SIGNAL_NUMBER, status.SignalNumber);
		Assert.Null(status.ExitCode);
	}

	[Fact]
	public void UserCancellationCarriesNeitherAnExitCodeNorASignal()
	{
		AgentTerminationStatus status = AgentTerminationStatus.CancelledByUser();

		Assert.Equal(AgentTerminationOutcome.CancelledByUser, status.Outcome);
		Assert.Null(status.ExitCode);
		Assert.Null(status.SignalNumber);
	}

	[Fact]
	public void TimeoutCarriesNeitherAnExitCodeNorASignal()
	{
		AgentTerminationStatus status = AgentTerminationStatus.TimedOut();

		Assert.Equal(AgentTerminationOutcome.TimedOut, status.Outcome);
		Assert.Null(status.ExitCode);
		Assert.Null(status.SignalNumber);
	}

	[Fact]
	public void TheFiveCasesAreDistinguishableFromEachOther()
	{
		AgentTerminationStatus[] cases =
		[
			AgentTerminationStatus.NotFound(),
			AgentTerminationStatus.ExitedWithCode(_SUCCESSFUL_EXIT_CODE),
			AgentTerminationStatus.TerminatedBySignal(_INTERRUPT_SIGNAL_NUMBER),
			AgentTerminationStatus.CancelledByUser(),
			AgentTerminationStatus.TimedOut()
		];

		Assert.Equal(_DECLARED_OUTCOME_COUNT, cases.Select(status => status.Outcome).Distinct().Count());
		Assert.Equal(_DECLARED_OUTCOME_COUNT, cases.Distinct().Count());
	}

	/// <summary>
	/// The enumeration is closed at five. A sixth case should read as a deliberate change in a diff
	/// rather than arriving with an implementation.
	/// </summary>
	[Fact]
	public void TheOutcomeEnumerationDeclaresFiveCasesAndNoMore()
	{
		Assert.Equal(_DECLARED_OUTCOME_COUNT, Enum.GetValues<AgentTerminationOutcome>().Length);
	}

	[Fact]
	public void TwoExitCodesAreNotTheSameStatus()
	{
		Assert.NotEqual(
			AgentTerminationStatus.ExitedWithCode(_SUCCESSFUL_EXIT_CODE),
			AgentTerminationStatus.ExitedWithCode(_FAILING_EXIT_CODE));
	}

	[Fact]
	public void TheSameCaseBuiltTwiceIsTheSameStatus()
	{
		Assert.Equal(AgentTerminationStatus.TimedOut(), AgentTerminationStatus.TimedOut());
		Assert.Equal(
			AgentTerminationStatus.ExitedWithCode(_FAILING_EXIT_CODE),
			AgentTerminationStatus.ExitedWithCode(_FAILING_EXIT_CODE));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void ANumberThatIsNotASignalIsRefused(int signalNumber)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => AgentTerminationStatus.TerminatedBySignal(signalNumber));
	}

	[Fact]
	public void AvailabilityIsTwoValuedAndNotFoundIsItsDefault()
	{
		Assert.Equal(_DECLARED_AVAILABILITY_COUNT, Enum.GetValues<AgentAvailability>().Length);
		Assert.Equal(AgentAvailability.NotFound, default(AgentAvailability));
	}
}
