using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// Well-formed JSON for the three sections a completed <c>scan</c> or
/// <c>analyze</c> must carry, shared by every fixture that hand-writes a schema-3 state document
/// naming those stages complete - <c>SessionStateStore</c>'s read path rejects an absent section
/// beside a completed producing stage.
/// </summary>
internal static class SessionStateDocumentSections
{
	/// <summary>A scan that enumerated nothing - as valid a scan summary as any other.</summary>
	internal const string EMPTY_SCAN_SUMMARY_JSON = """{"fileCount":0,"lineCount":0,"candidateFileCount":0,"rows":[]}""";

	/// <summary>All eleven declared categories, each recording nothing.</summary>
	internal static string EmptySkippedItemTalliesJson()
	{
		IEnumerable<string> entries = SkippedItemCategories.ALL.Select(category =>
			$$"""{"category":"{{SkippedItemCategoryNames.NameOf(category)}}","count":0,"enumeratedPaths":[]}""");

		return $"[{string.Join(",", entries)}]";
	}

	/// <summary>No languages enabled - schema-valid on the read path regardless of whether a real session could produce it.</summary>
	internal const string EMPTY_SCAN_ENABLED_LANGUAGES_JSON = "[]";

	/// <summary>Schema 5. The built-in default flow, as a JSON string literal.</summary>
	internal const string DEFAULT_FLOW_JSON = "\"ClientCredentials\"";

	/// <summary>Schema 5. The built-in origin, as a JSON string literal.</summary>
	internal const string DEFAULT_FLOW_ORIGIN_JSON = "\"BuiltInDefault\"";

	/// <summary>Schema 5. No file evaluated and no trip - as valid as any other pair of counters.</summary>
	internal const string EMPTY_FLOW_EVALUATION_DIAGNOSTICS_JSON = """{"filesEvaluated":0,"signalTripCount":0}""";

	/// <summary>An untripped cap outcome whose retained count agrees with the persisted finding set's own length.</summary>
	internal static string UntrippedFindingsCapOutcomeJson(int findingsCount)
	{
		return $$"""
			{
				"totalRecognizedCount": {{findingsCount}},
				"retainedCount": {{findingsCount}},
				"perFileCapTrippedFileCount": 0,
				"perFileOmittedCount": 0,
				"globalCapTripped": false,
				"globalOmittedCount": 0
			}
			""";
	}
}
