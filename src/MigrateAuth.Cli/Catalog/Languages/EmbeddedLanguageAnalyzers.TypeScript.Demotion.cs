using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The demotion signals of the `TypeScript` analyzer, each with the kind it declares: a fired Test kind proposes Medium and any other fired kind proposes Low, the minimum taken once, and a finding
/// carries the test-code mark whenever Test fired. Every signal is declared <c>Insensitive</c>.
///
/// BY REFERENCE from <see cref="JavaScriptDemotionDeclarations"/>: the folder signals, the file-name prefix signals and the two content markers (the test-framework call and the generated-file banner), so a
/// later change to them reaches `TypeScript` by construction. REBUILT: the file-name suffix signals, which are the five stems (<c>.test .spec .mock .stub .fake</c>) crossed with the four extensions
/// <c>.ts .tsx .mts .cts</c>; the separator is part of the suffix, so <c>latest.ts</c> does not end in one. A parity test compares them with the file-name signals of the `JavaScript` declarations.
///
/// DECLARATION FILES. The three suffixes <c>.d.ts</c>, <c>.d.mts</c> and <c>.d.cts</c> are of kind Generated, so every finding in a declaration file reports at Low. There is no <c>.min.js</c> analogue
/// and no generation-marker expression, and Angular's testing modules are not signals.
/// </summary>
internal static class TypeScriptDemotionDeclarations
{
	private static readonly ImmutableArray<string> _typeScriptExtensions = [".ts", ".tsx", ".mts", ".cts"];

	private static readonly ImmutableArray<string> _testFileStems = [".test", ".spec"];

	private static readonly ImmutableArray<string> _mockFileStems = [".mock", ".stub", ".fake"];

	private static readonly ImmutableArray<string> _declarationFileSuffixes = [".d.ts", ".d.mts", ".d.cts"];

	internal static LanguageShapedDemotionSignals Build()
	{
		LanguageShapedDemotionSignals javaScriptSignals = JavaScriptDemotionDeclarations.Build();

		LanguageShapedDemotionSignals signals = new()
		{
			FileNameSignals = BuildFileNameSignals(javaScriptSignals),
			FolderNameSignals = javaScriptSignals.FolderNameSignals,
			GenerationMarkerExpressions = [],
			ContentMarkers = javaScriptSignals.ContentMarkers
		};

		return signals;
	}

	private static FrozenSet<FileNameSignal> BuildFileNameSignals(LanguageShapedDemotionSignals javaScriptSignals)
	{
		IEnumerable<FileNameSignal> prefixSignals = javaScriptSignals.FileNameSignals.Where(signal => signal.Match == FileNameMatch.Prefix);

		IEnumerable<FileNameSignal> signals = SuffixesOf(_testFileStems, DemotionKind.Test)
			.Concat(SuffixesOf(_mockFileStems, DemotionKind.Mock))
			.Concat(prefixSignals)
			.Concat(_declarationFileSuffixes.Select(suffix => Signal(suffix, DemotionKind.Generated)));

		return signals.ToFrozenSet();
	}

	/// <summary>One suffix per stem and per extension: <c>.test</c> gives <c>.test.ts</c>, <c>.test.tsx</c>, <c>.test.mts</c> and <c>.test.cts</c>.</summary>
	private static IEnumerable<FileNameSignal> SuffixesOf(ImmutableArray<string> stems, DemotionKind kind)
	{
		return stems.SelectMany(
			stem => _typeScriptExtensions,
			(stem, extension) => Signal(stem + extension, kind));
	}

	private static FileNameSignal Signal(string suffix, DemotionKind kind)
	{
		return new FileNameSignal { Match = FileNameMatch.Suffix, Text = suffix, Sensitivity = CaseSensitivity.Insensitive, Kind = kind };
	}
}
