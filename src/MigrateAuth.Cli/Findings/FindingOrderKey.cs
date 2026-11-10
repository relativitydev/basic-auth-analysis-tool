namespace MigrateAuth.Cli.Findings;

/// <summary>
/// THE TOTAL KEY: relative path normalized to forward slashes, compared ORDINALLY; then
/// line; then column; then pattern class identifier; then, as a final tie-break, the raw
/// path compared ordinally.
///
/// IT IS GLOBAL AND NOT PER FILE. Subsumption reduction already orders within one file, and that
/// is this key restricted to a constant first term - but the numbering this key governs runs over
/// every file in the repository at once, and a sequence assembled by concatenating per-file runs
/// would depend on the order the walk happened to return directories in.
///
/// ORDINAL COMPARISON IS NECESSARY AND INSUFFICIENT. Comparing ordinally makes the SEQUENCE
/// reproducible; it says nothing about the SET, which still depends on the platform in two
/// named ways: the Unicode form of the path, which <see cref="NormalizedPathKey"/> does not
/// change, and the match bound, which is counted in its own skipped category.
///
/// THE LAST TERM IS THE CLASS IDENTIFIER AND NOT THE CLASS NAME. A name is prose and prose
/// gets improved, so the
/// day someone sharpens a class name every finding of that class moves in the order and the
/// identifiers a consumer compares against move with it.
/// </summary>
public readonly record struct FindingOrderKey : IComparable<FindingOrderKey>
{
	/// <summary>Term one. The separator-normalized relative path.</summary>
	public required NormalizedPathKey Path { get; init; }

	/// <summary>Term two. The one-based line.</summary>
	public required int Line { get; init; }

	/// <summary>Term three. The one-based column, which is published rather than hidden here.</summary>
	public required int Column { get; init; }

	/// <summary>Term four. The catalog pattern class identifier.</summary>
	public required string PatternClassIdentifier { get; init; }

	/// <summary>
	/// Term five. The raw path - the path as the platform returned it - which separates two files
	/// whose keys are equal. It is consulted last, so a repository in which no two paths share a
	/// key is ordered by the first four terms alone.
	/// </summary>
	public required string RawPath { get; init; }

	/// <summary>
	/// The five terms in order, each consulted only where the ones before it tied.
	///
	/// Both string comparisons are ordinal, which keeps the order
	/// free of locale data - a culture-sensitive comparison would place the same two paths
	/// differently on two machines with different installed locales, which is the failure the
	/// whole ordering rule exists to prevent.
	/// </summary>
	public int CompareTo(FindingOrderKey other)
	{
		int comparison = StringComparer.Ordinal.Compare(Path.Value, other.Path.Value);

		if (comparison == 0)
		{
			comparison = Line.CompareTo(other.Line);
		}

		if (comparison == 0)
		{
			comparison = Column.CompareTo(other.Column);
		}

		if (comparison == 0)
		{
			comparison = StringComparer.Ordinal.Compare(PatternClassIdentifier, other.PatternClassIdentifier);
		}

		if (comparison == 0)
		{
			comparison = StringComparer.Ordinal.Compare(RawPath, other.RawPath);
		}

		return comparison;
	}
}
