namespace MigrateAuth.Cli.Writing;

/// <summary>
/// The one operation the AI-assist stage needs of the artifact writer after the prompt pack is written:
/// appending a section to the report this process wrote. It exists as a seam so that a test double can
/// return the refusals that cannot arise naturally (for example the boundary refusing after the pack write
/// passed it) and the stage's own line for each refusal is driven by its branch.
/// </summary>
public interface IReportAppender
{
	/// <summary>
	/// Appends one section to the report recorded in <paramref name="holder"/>, failing closed with the
	/// original untouched.
	/// </summary>
	ReportAppendResult AppendToReport(ReportWriteHolder holder, Action<TextWriter> writeSection);
}
