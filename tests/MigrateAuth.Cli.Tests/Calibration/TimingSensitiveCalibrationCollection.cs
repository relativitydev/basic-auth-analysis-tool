using Xunit;

namespace MigrateAuth.Cli.Tests.Calibration;

/// <summary>
/// The calibration tests that compare wall-clock time with the match bound, run one at a time and
/// alone. The match bound is charged in wall-clock time by <c>MatchBoundWatch</c>, so a dense
/// stress shape that takes well under the bound on a quiet machine can be charged several times
/// that when other test collections run on the same cores at the same moment. Serializing these
/// collections keeps what they assert - termination, the bound and the agreement of the generated
/// and interpreted expressions - independent of what else the suite is doing.
/// </summary>
[CollectionDefinition(TimingSensitiveCalibrationCollection.NAME, DisableParallelization = true)]
public sealed class TimingSensitiveCalibrationCollection
{
	internal const string NAME = "TimingSensitiveCalibration";
}
