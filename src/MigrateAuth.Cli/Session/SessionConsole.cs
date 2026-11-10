namespace MigrateAuth.Cli.Session;

/// <summary>
/// The console a session talks to: where it reads entered commands and where it writes.
///
/// It is one collaborator rather than two because a reader without its writer is not a thing
/// this product has — every prompt is a write followed by a read, and nothing in the session
/// ever wants one without the other. Bundling them is what keeps the shell's collaborators to
/// the three it genuinely has: the state store, the stage runner and this.
/// </summary>
public sealed class SessionConsole
{
	/// <summary>
	/// A byte-order mark a redirected stdin can carry: Windows PowerShell writes one at the
	/// start of a piped stream by default, and .NET's own text-mode decoding of that stream turns
	/// it into this literal leading character rather than consuming it - so the first line this
	/// session ever reads can arrive as this character followed by the command the user meant.
	/// <c>string.Trim()</c> does not remove it: U+FEFF is a Unicode format character, not
	/// whitespace, so a mark left unstripped resolves the command it prefixes as "not found"
	/// under its own visible name.
	/// </summary>
	private const char _BYTE_ORDER_MARK = (char)0xFEFF;

	private readonly TextReader _inputReader;

	public SessionConsole(TextReader inputReader, TextWriter outputWriter)
	{
		ArgumentNullException.ThrowIfNull(inputReader);
		ArgumentNullException.ThrowIfNull(outputWriter);

		_inputReader = inputReader;
		Output = outputWriter;
	}

	/// <summary>
	/// Where the session writes. Exposed as the writer itself because the renderers take one
	/// directly, and wrapping every write method would buy nothing but indirection.
	/// </summary>
	public TextWriter Output { get; }

	/// <summary>
	/// The next entered line, or null once the input has ended. A leading byte-order mark is
	/// stripped from whatever line carries it.
	///
	/// THE STRIP APPLIES TO ANY LINE, WITH NO STATE TRACKING WHICH ONE IS FIRST - a plain rule
	/// rather than a narrower one that would need to remember where in the stream this call is. A
	/// genuine byte-order mark can only ever occur at the very start of the whole input, so in
	/// practice this rule only ever has anything to strip on the first line this method reads; it
	/// does not need to distinguish "first line" from "any other line" to get that result.
	///
	/// THIS IS DISTINCT FROM <c>Program</c>'S OWN BYTE-LEVEL DETECTION (see
	/// <c>BuildSessionInputReader</c>), which inspects the stream's raw leading bytes once, before
	/// any text reaches this class, and only ever consumes a mark there - never mid-stream. What
	/// reaches this method as a leading U+FEFF character is whatever that detection did not already
	/// consume; this strip is the fallback for that case, kept as a per-line check because it costs
	/// nothing to apply uniformly rather than because a mark could arrive on any other line.
	/// </summary>
	public string? ReadLine()
	{
		string? enteredLine = _inputReader.ReadLine();

		return enteredLine is { Length: > 0 } && enteredLine[0] == _BYTE_ORDER_MARK
			? enteredLine[1..]
			: enteredLine;
	}
}
