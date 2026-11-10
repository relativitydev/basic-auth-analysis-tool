using System.Collections.Immutable;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>One legend line of the Suggest console: a short key and the note text it stands for.</summary>
public readonly record struct FlowNoteLegendEntry(string Key, string Note);

/// <summary>
/// THE SHORT KEYS THE SUGGEST CONSOLE USES SO THAT EACH FLOW NOTE IS PRINTED
/// ONCE, IN A LEGEND, AND EVERY FINDING ROW CARRIES ONLY THE KEY.
///
/// Fifty thousand rows that each spelled out a note would print the same four paragraphs fifty
/// thousand times; a legend gives each note once and a key resolves a row to it. The report and the
/// JSON artifact are not affected: they carry the full note beside every finding (<see cref="FlowNotes"/>).
///
/// THERE IS ONE KEY PER NOTE TEXT, NOT PER PAIR. <see cref="FlowNotes.NoteFor"/> is total over the
/// (decision, flow) pairs but only four distinct texts exist, so the key is derived by asking that
/// function and looking the text up here: a key can never resolve to a note other than the one the
/// pair's own constant holds, and a fifth note added there without a key here fails loudly instead of
/// printing a row nobody can resolve. The keys are fixed strings in a fixed order, which is what makes
/// the console output byte-identical for the same state.
/// </summary>
public static class FlowNoteKeys
{
	/// <summary>The key of <see cref="FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE"/>.</summary>
	public const string DEFAULT_CLIENT_CREDENTIALS_KEY = "[N1]";

	/// <summary>The key of <see cref="FlowNotes.DEFAULT_AUTHORIZATION_CODE_NOTE"/>.</summary>
	public const string DEFAULT_AUTHORIZATION_CODE_KEY = "[N2]";

	/// <summary>The key of <see cref="FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE"/>.</summary>
	public const string FIXED_SIGNAL_CLIENT_CREDENTIALS_KEY = "[N3]";

	/// <summary>The key of <see cref="FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE"/>.</summary>
	public const string INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_KEY = "[N4]";

	/// <summary>All four legend lines, in key order.</summary>
	public static ImmutableArray<FlowNoteLegendEntry> AllEntries { get; } =
	[
		new FlowNoteLegendEntry(DEFAULT_CLIENT_CREDENTIALS_KEY, FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE),
		new FlowNoteLegendEntry(DEFAULT_AUTHORIZATION_CODE_KEY, FlowNotes.DEFAULT_AUTHORIZATION_CODE_NOTE),
		new FlowNoteLegendEntry(FIXED_SIGNAL_CLIENT_CREDENTIALS_KEY, FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE),
		new FlowNoteLegendEntry(INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_KEY, FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE)
	];

	/// <summary>The key of the note a finding with this decision and flow carries.</summary>
	public static string KeyFor(FlowDecision decision, CredentialFlow flow)
	{
		string note = FlowNotes.NoteFor(decision, flow);
		string? key = null;

		foreach (FlowNoteLegendEntry entry in AllEntries)
		{
			if (string.Equals(entry.Note, note, StringComparison.Ordinal))
			{
				key = entry.Key;
			}
		}

		return key ?? throw new InvalidOperationException("A flow note has no legend key.");
	}
}
