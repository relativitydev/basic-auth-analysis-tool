using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Recognition;

namespace MigrateAuth.Cli.Findings;

/// <summary>
/// STEP 6 OF THE ANALYSIS PIPELINE: recognized constructs become findings, ordered by a total key and
/// then numbered.
///
/// THE THREE STEPS RUN IN THIS ORDER AND THE ORDER IS THE POINT. Place, then order, then
/// number, because an identifier assigned before the sort is an identifier that depends on
/// the order the walk returned directories in.
///
/// THE THRESHOLD DOES NOT FILTER HERE, AND THE FINDINGS CAP DOES - the two are different
/// rules and this component draws the line between them. The threshold filter runs AFTER
/// numbering, over the numbered set, removing rows from the output; the visible cost is that a
/// filtered artifact is not gapless, which is the lesser cost. The alternative makes a
/// finding's identifier a function of a configuration file, which breaks the stable identity
/// findings need - so this component does not
/// read the configuration at all, which is stronger than reading it and choosing not to act. The
/// findings cap is not configuration: it is a resource bound decided once, here, at analysis time, over what
/// is persisted - so <see cref="ComposeWithCapOutcome"/> admits constructs per file BEFORE
/// numbering, and survivors are numbered gaplessly rather than carrying the gaps a later, reversible
/// filter would leave.
///
/// THE ORDINAL IS COUNTED IN THE SAME PASS THAT PLACES, and that is not an optimization. The
/// ordinal is "of that class's occurrence WITHIN THE FILE", so it is countable only while the file
/// is the unit being walked - once the findings are in the global order, the occurrences of one
/// class in one file are no longer adjacent and no longer in their original sequence.
///
/// THE SORT IS STABLE, WHICH SETTLES WHAT THE TOTAL KEY LEAVES OPEN. The total key is total in practice:
/// two constructs of one class cannot share an anchor, because one rule's matches are disjoint.
/// Should that ever cease to hold, a stable sort resolves the tie by the input order - which is
/// the order the files were offered in and the key's own order within one - rather than by whatever an
/// unstable sort's partitioning happened to do, which is the only outcome that would make two runs
/// of one input differ.
/// </summary>
public static class FindingComposition
{
	/// <summary>
	/// The ordered, numbered finding set for a whole repository, reduced by the findings cap.
	///
	/// EVERY RECOGNIZED CONSTRUCT BECOMES A FINDING UNLESS THE CAP REMOVED IT. Nothing is
	/// deduplicated or thresholded here - that is what the later, reversible threshold filter describes.
	/// A caller that needs to know whether the cap removed anything calls
	/// <see cref="ComposeWithCapOutcome"/> instead.
	/// </summary>
	public static ImmutableArray<Finding> Compose(IEnumerable<RecognizedFile> recognizedFiles)
	{
		return ComposeWithCapOutcome(recognizedFiles).Findings;
	}

	/// <summary>
	/// <see cref="Compose"/>, with the truncation counter alongside the set it describes.
	///
	/// THE CAP IS APPLIED WHILE OFFERING, NOT AFTER. The alternative - compose the full, uncapped set
	/// and remove the excess afterward - is exactly the unbounded accumulation the findings cap exists to
	/// forbid: the one term that grows without bound would still be allocated in full before anything
	/// bounded it. Instead each file offers <see cref="RecognizedFile.Constructs"/>, already in
	/// the key's local order, to <see cref="FindingsCapAdmission.Offer"/>, which keeps at most the
	/// global cap's worth of constructs and lets go of the rest, so a construct the caps reject is
	/// never given a position or a fingerprint at all and its file's text is not held.
	///
	/// ADMISSION FOLLOWS THE TOTAL ORDERING KEY AND NOT THE ARRIVAL ORDER. The constructs that
	/// survive the global cap are the leading prefix of the files ordered by <see cref="NormalizedPathKey"/>
	/// and then by raw path compared ordinally, whatever order the files were offered in, so the set
	/// that is admitted and the order it is emitted in are decided by one key. File I/O is not
	/// touched: a file is read by its raw path before it reaches this method. Within one file,
	/// <see cref="RecognizedFile.Constructs"/> is already ordered by anchor offset and then pattern
	/// class identifier, so the per-file cap's prefix is that file's own leading prefix under the key's
	/// local order. Neither cap's survivor set depends on recognition's internal rule order or on an
	/// arrival accident.
	/// </summary>
	public static ComposedFindingSet ComposeWithCapOutcome(IEnumerable<RecognizedFile> recognizedFiles)
	{
		ArgumentNullException.ThrowIfNull(recognizedFiles);

		FindingsCapAdmission admission = new();

		foreach (RecognizedFile recognizedFile in recognizedFiles)
		{
			admission.Offer(recognizedFile);
		}

		List<PlacedConstruct> placed = [];

		foreach (FindingsCapAdmission.AdmittedFile admittedFile in admission.AdmittedFilesInKeyOrder())
		{
			PlaceEveryConstructOfOneFile(admittedFile, placed);
		}

		return new ComposedFindingSet
		{
			Findings = PathDisambiguation.Apply(NumberInTotalOrder(placed)),
			CapOutcome = admission.ToOutcome()
		};
	}

	/// <summary>
	/// One file's constructs, given a line, a column and a fingerprint - the whole of what a
	/// finding needs that the global pass cannot supply - up to as many as
	/// <see cref="FindingsCapAdmission"/> admitted from this file.
	///
	/// The per-class counter is local to this file by construction rather than by discipline: it
	/// is created here and goes out of scope with the file, so there is no counter a later edit
	/// could forget to reset. IT IS COUNTED ONLY OVER ADMITTED CONSTRUCTS, because a construct the
	/// cap rejects never becomes a finding and so never needs an occurrence ordinal at all.
	/// </summary>
	private static void PlaceEveryConstructOfOneFile(
		FindingsCapAdmission.AdmittedFile admittedFile,
		List<PlacedConstruct> placed)
	{
		RecognizedFile recognizedFile = admittedFile.File;
		NormalizedPathKey pathKey = NormalizedPathKey.From(recognizedFile.Path);
		Dictionary<string, int> occurrencesByPatternClass = new(StringComparer.Ordinal);

		for (int index = 0; index < admittedFile.AdmittedCount; index++)
		{
			placed.Add(Place(recognizedFile, recognizedFile.Constructs[index], pathKey, occurrencesByPatternClass));
		}
	}

	private static PlacedConstruct Place(
		RecognizedFile recognizedFile,
		RecognizedConstruct construct,
		NormalizedPathKey pathKey,
		Dictionary<string, int> occurrencesByPatternClass)
	{
		RuleIdentifier patternClass = PatternClassOf(construct.PatternClassIdentifier);
		TextPosition position = TextPositions.LineAndColumnOf(recognizedFile.NormalizedText, construct.AnchorOffset);
		int occurrenceOrdinal = NextOrdinalFor(construct.PatternClassIdentifier, occurrencesByPatternClass);

		PlacedConstruct placedConstruct = new()
		{
			File = recognizedFile,
			Construct = construct,
			PatternClass = patternClass,
			Position = position,
			Fingerprint = new FindingFingerprint
			{
				Path = pathKey,
				PatternClass = patternClass,
				OccurrenceOrdinal = occurrenceOrdinal
			}
		};

		return placedConstruct;
	}

	/// <summary>
	/// The ordinal: which occurrence of THIS class within THIS file this construct is, counted
	/// in the order recognition delivered them, which is the total key restricted to one file.
	/// </summary>
	private static int NextOrdinalFor(string patternClassIdentifier, Dictionary<string, int> occurrencesByPatternClass)
	{
		int ordinal = occurrencesByPatternClass.TryGetValue(patternClassIdentifier, out int previousOrdinal)
			? previousOrdinal + 1
			: FindingFingerprint.FIRST_OCCURRENCE_ORDINAL;

		occurrencesByPatternClass[patternClassIdentifier] = ordinal;

		return ordinal;
	}

	/// <summary>
	/// The sort and the numbering, in that order and in one place so that they cannot be separated.
	/// </summary>
	private static ImmutableArray<Finding> NumberInTotalOrder(List<PlacedConstruct> placed)
	{
		ImmutableArray<Finding>.Builder findings = ImmutableArray.CreateBuilder<Finding>(placed.Count);
		int sequentialIndex = FindingIdentifiers.FIRST_INDEX;

		foreach (PlacedConstruct placedConstruct in placed.OrderBy(OrderKeyOf))
		{
			findings.Add(FindingFrom(placedConstruct, sequentialIndex));
			sequentialIndex++;
		}

		return findings.MoveToImmutable();
	}

	private static FindingOrderKey OrderKeyOf(PlacedConstruct placedConstruct)
	{
		FindingOrderKey orderKey = new()
		{
			Path = placedConstruct.Fingerprint.Path,
			Line = placedConstruct.Position.Line,
			Column = placedConstruct.Position.Column,
			PatternClassIdentifier = placedConstruct.PatternClass.Value,
			RawPath = placedConstruct.File.Path.Value
		};

		return orderKey;
	}

	private static Finding FindingFrom(PlacedConstruct placedConstruct, int sequentialIndex)
	{
		Finding finding = new()
		{
			Identifier = FindingIdentifiers.Of(sequentialIndex),
			SequentialIndex = sequentialIndex,
			Path = placedConstruct.File.Path,
			Line = placedConstruct.Position.Line,
			Column = placedConstruct.Position.Column,
			Language = placedConstruct.File.Language,
			PatternClass = placedConstruct.PatternClass,
			Level = placedConstruct.Construct.Level,
			DestinationCertainty = DestinationCertaintyOf(placedConstruct),
			LiteralCredentialSignal = placedConstruct.Construct.LiteralCredentialSignal,
			Fingerprint = placedConstruct.Fingerprint,
			LowReasons = placedConstruct.Construct.LowReasons,
			FlowEvidence = FlowEvidenceOf(placedConstruct)
		};

		return finding;
	}

	/// <summary>
	/// The file's one evidence value, copied onto a High or Medium finding; a Low finding
	/// carries none. Nothing is read from configuration or from the catalog here: the value was
	/// decided by recognition and is only carried.
	/// </summary>
	private static FlowEvidence? FlowEvidenceOf(PlacedConstruct placedConstruct)
	{
		bool carriesEvidence = placedConstruct.Construct.Level is ConfidenceLevel.High or ConfidenceLevel.Medium;

		return carriesEvidence ? placedConstruct.File.FlowEvidence : null;
	}

	/// <summary>
	/// READ FROM THE RULE THE CATALOG DECLARED IT ON, NEVER DERIVED. Each catalog declares it per
	/// class, and the value is a claim about where a request goes - something this component is in
	/// no position to establish.
	/// </summary>
	private static DestinationCertainty DestinationCertaintyOf(PlacedConstruct placedConstruct)
	{
		if (!placedConstruct.File.Analyzer.DetectionRulesByPatternClass.TryGetValue(
			placedConstruct.Construct.PatternClassIdentifier,
			out DetectionRule? rule))
		{
			throw new InvalidOperationException(
				$"Pattern class '{placedConstruct.Construct.PatternClassIdentifier}' is not declared by the analyzer that recognized it.");
		}

		return rule.DestinationCertainty;
	}

	/// <summary>
	/// A CATALOG IDENTIFIER THAT DOES NOT SATISFY THE SHAPE IS A BUILD-TIME DEFECT AND IS TREATED
	/// AS ONE. The catalogs are embedded in the binary, so this is never a property of the
	/// repository under assessment and can never be triggered by a client's code; it is a catalog
	/// nobody could have shipped a correct report from. Dropping the construct instead would lose a
	/// finding silently.
	///
	/// The message carries the identifier and nothing else, which the data invariant permits: an
	/// identifier names a class in a catalog embedded in this binary and says nothing about the
	/// repository. The session's exception boundary turns this into an exit code rather than a
	/// crash.
	/// </summary>
	private static RuleIdentifier PatternClassOf(string patternClassIdentifier)
	{
		if (!RuleIdentifier.TryFromPatternClass(patternClassIdentifier, out RuleIdentifier patternClass))
		{
			throw new InvalidOperationException(
				$"Pattern class '{patternClassIdentifier}' does not carry the shape a catalog identifier must have.");
		}

		return patternClass;
	}

	/// <summary>
	/// A construct that has acquired everything its own file could give it, and nothing the global
	/// pass owes it. It is a private nested type because it exists only between this component's
	/// first step and its third, and the shape of the boundary is the point: it carries no
	/// identifier and no index, so the sort CANNOT be given a numbered set by mistake.
	/// </summary>
	private readonly record struct PlacedConstruct
	{
		internal required RecognizedFile File { get; init; }

		internal required RecognizedConstruct Construct { get; init; }

		internal required RuleIdentifier PatternClass { get; init; }

		internal required TextPosition Position { get; init; }

		internal required FindingFingerprint Fingerprint { get; init; }
	}

	/// <summary>
	/// THE TWO FINDINGS CAPS, ENFORCED WHILE OFFERING, WITH THE COUNTERS THEIR TRUNCATION DECLARATION
	/// NEEDS, AND KEY-ORDERED ADMISSION. It is a private nested type for the same reason
	/// <see cref="PlacedConstruct"/> is: it exists only for the lifetime of one
	/// <see cref="ComposeWithCapOutcome"/> call and carries state nothing outside that call has any
	/// business holding.
	///
	/// THE GLOBAL CAP KEEPS THE SMALLEST CONSTRUCTS UNDER THE KEY, NOT THE FIRST TO ARRIVE. Each
	/// offered file is held with the count of its constructs the per-file cap lets through; whenever
	/// the held total passes the global cap, the LARGEST held file under (key, raw path) gives up its
	/// trailing constructs, or is dropped whole when none of its constructs fit. The held set after
	/// any prefix of arrivals is therefore the leading prefix of what has arrived so far, so after all
	/// of them it is the leading prefix of the whole - which does not depend on the order they came
	/// in. Files arriving already in key order, which is how the persisted candidates arrive, never
	/// displace an earlier file: once the cap is reached each later file is the largest and is
	/// trimmed at once. Memory is bounded by the global cap, since a file with no admitted construct
	/// is not held.
	///
	/// EVERY COUNTER IS A SUM OVER FILES OF A QUANTITY THAT IS ITSELF ORDER-FREE. What a file offers
	/// and what the per-file cap removes depend on that file alone; the global omission is what the
	/// per-file cap let through minus what was retained. The two omission counters therefore stay
	/// mutually exclusive: a finding removed because its own file exceeded the per-file cap is never
	/// also counted as a global omission.
	/// </summary>
	private sealed class FindingsCapAdmission
	{
		private readonly SortedSet<AdmittedFile> _admittedFiles = new(new KeyOrder());
		private int _sequence;
		private int _heldCount;
		private int _totalRecognizedCount;
		private int _perFileAdmissibleCount;
		private int _perFileOmittedCount;
		private int _perFileCapTrippedFileCount;

		/// <summary>One file's constructs, as many of them as are currently admitted.</summary>
		internal sealed class AdmittedFile
		{
			internal AdmittedFile(RecognizedFile file, int sequence, int admittedCount)
			{
				File = file;
				Sequence = sequence;
				AdmittedCount = admittedCount;
			}

			internal RecognizedFile File { get; }

			internal int Sequence { get; }

			internal int AdmittedCount { get; set; }
		}

		/// <summary>
		/// Takes one file's constructs, in the key's local order, and decides how many of them survive.
		/// The per-file cap is applied first and depends on this file alone; the global cap is applied
		/// by keeping the smallest constructs under the key, as the class comment describes.
		/// </summary>
		internal void Offer(RecognizedFile recognizedFile)
		{
			ArgumentNullException.ThrowIfNull(recognizedFile);

			int offeredCount = recognizedFile.Constructs.Length;
			int perFileAdmissible = Math.Min(offeredCount, FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP);
			int perFileExcess = offeredCount - perFileAdmissible;

			_totalRecognizedCount += offeredCount;
			_perFileAdmissibleCount += perFileAdmissible;
			_perFileOmittedCount += perFileExcess;

			if (perFileExcess > 0)
			{
				_perFileCapTrippedFileCount++;
			}

			if (perFileAdmissible > 0)
			{
				_admittedFiles.Add(new AdmittedFile(recognizedFile, _sequence++, perFileAdmissible));
				_heldCount += perFileAdmissible;
				TrimToTheGlobalCap();
			}
		}

		/// <summary>The files that hold at least one admitted construct, ascending under the key.</summary>
		internal IEnumerable<AdmittedFile> AdmittedFilesInKeyOrder()
		{
			return _admittedFiles;
		}

		/// <summary>The counters as the truncation declaration reads them.</summary>
		internal FindingsCapOutcome ToOutcome()
		{
			int globalOmittedCount = _perFileAdmissibleCount - _heldCount;

			return new FindingsCapOutcome
			{
				TotalRecognizedCount = _totalRecognizedCount,
				RetainedCount = _heldCount,
				PerFileCapTrippedFileCount = _perFileCapTrippedFileCount,
				PerFileOmittedCount = _perFileOmittedCount,
				GlobalCapTripped = globalOmittedCount > 0,
				GlobalOmittedCount = globalOmittedCount
			};
		}

		private void TrimToTheGlobalCap()
		{
			while (_heldCount > FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP)
			{
				AdmittedFile largest = _admittedFiles.Max!;
				int excess = _heldCount - FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP;

				if (largest.AdmittedCount <= excess)
				{
					_admittedFiles.Remove(largest);
					_heldCount -= largest.AdmittedCount;
				}
				else
				{
					largest.AdmittedCount -= excess;
					_heldCount -= excess;
				}
			}
		}

		/// <summary>
		/// The ordering key of a file - the path key compared ordinally - then the raw path
		/// compared ordinally, then the order the file was offered in, which only ever separates a
		/// file offered twice.
		/// </summary>
		private sealed class KeyOrder : IComparer<AdmittedFile>
		{
			public int Compare(AdmittedFile? x, AdmittedFile? y)
			{
				ArgumentNullException.ThrowIfNull(x);
				ArgumentNullException.ThrowIfNull(y);

				int comparison = StringComparer.Ordinal.Compare(
					NormalizedPathKey.From(x.File.Path).Value,
					NormalizedPathKey.From(y.File.Path).Value);

				if (comparison == 0)
				{
					comparison = StringComparer.Ordinal.Compare(x.File.Path.Value, y.File.Path.Value);
				}

				if (comparison == 0)
				{
					comparison = x.Sequence.CompareTo(y.Sequence);
				}

				return comparison;
			}
		}
	}
}
