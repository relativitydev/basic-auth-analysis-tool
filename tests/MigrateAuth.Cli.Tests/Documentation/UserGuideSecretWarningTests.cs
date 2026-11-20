using System.Globalization;

using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// THE USER GUIDE DESCRIBES THE WARNING THE TOOL PRINTS FOR A CREDENTIAL WRITTEN INTO THE SOURCE CODE, AND THE DESCRIPTION IS HELD TO THE TEXT THE TOOL EMITS.
///
/// Part B is a security guide for a developer who does not run the tool, so it does not describe the scan-time warning; the User Guide does. The guide shows the
/// warning with the three placeholders a reader replaces by the finding identifier, the location and the pattern class, and these checks compare that text, the
/// suffix of a finding below the reporting threshold, the pointer past the row cap and the row cap itself with the constants the tool prints from, so a change of
/// the tool's wording fails here until the guide says the same.
/// </summary>
public sealed class UserGuideSecretWarningTests
{
	private const string _FINDING_IDENTIFIER_PLACEHOLDER = "<finding-identifier>";
	private const string _LOCATION_PLACEHOLDER = "<path>:<line>";
	private const string _PATTERN_CLASS_PLACEHOLDER = "<pattern-class>";
	private const string _OMITTED_COUNT_PLACEHOLDER = "<n>";
	private const string _OMITTED_IN_REPORT_COUNT_PLACEHOLDER = "<m>";
	private const string _MAXIMUM_WARNINGS_SENTENCE_FORMAT = "The console shows at most {0} warnings.";
	private const string _ROTATING_CLIENT_SECRETS_LINK = "(../migration/part-b-secret-management.md#rotating-client-secrets)";
	private const string _CHOOSING_THE_FLOW_HEADING = "## Choosing and confirming the flow";

	/// <summary>The guide shows the warning word for word, with its three placeholders.</summary>
	[Fact]
	public void TheUserGuideShowsTheShippedLiteralCredentialWarningWordForWord()
	{
		string warning = string.Format(
			CultureInfo.InvariantCulture,
			AnalyzeConsoleReport.LITERAL_CREDENTIAL_WARNING_MESSAGE_FORMAT,
			_FINDING_IDENTIFIER_PLACEHOLDER,
			_LOCATION_PLACEHOLDER,
			_PATTERN_CLASS_PLACEHOLDER);

		Assert.Contains(warning, UsingTheToolGuide(), StringComparison.Ordinal);
	}

	/// <summary>The guide says what the warning does for a finding below the threshold, with the suffix the tool appends.</summary>
	[Fact]
	public void TheUserGuideSaysWhatTheShippedWarningDoesForAFindingBelowTheThreshold()
	{
		string suffix = AnalyzeConsoleReport.BELOW_REPORTING_THRESHOLD_SUFFIX.TrimStart();

		Assert.Contains(suffix, UsingTheToolGuide(), StringComparison.Ordinal);
	}

	/// <summary>The guide gives the row cap of the warning list and the line that counts the warnings past it.</summary>
	[Fact]
	public void TheUserGuideGivesTheWarningRowCapAndTheLineThatCountsTheRest()
	{
		string pointer = string.Format(
			CultureInfo.InvariantCulture,
			AnalyzeConsoleReport.FURTHER_LITERAL_CREDENTIAL_WARNINGS_MESSAGE_FORMAT,
			_OMITTED_COUNT_PLACEHOLDER,
			_OMITTED_IN_REPORT_COUNT_PLACEHOLDER);
		string maximumWarnings = string.Format(
			CultureInfo.InvariantCulture,
			_MAXIMUM_WARNINGS_SENTENCE_FORMAT,
			ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS);

		Assert.Contains(pointer, UsingTheToolGuide(), StringComparison.Ordinal);
		Assert.Contains(maximumWarnings, UsingTheToolGuide(), StringComparison.Ordinal);
	}

	/// <summary>The guide sends the reader to Part B to rotate the secret, and keeps the section on how the flow is chosen under a stable heading.</summary>
	[Fact]
	public void TheUserGuideLinksPartBToRotateTheSecretAndKeepsTheFlowSectionHeading()
	{
		string guide = UsingTheToolGuide();

		Assert.Contains(_ROTATING_CLIENT_SECRETS_LINK, guide, StringComparison.Ordinal);
		Assert.Contains(_CHOOSING_THE_FLOW_HEADING, guide, StringComparison.Ordinal);
	}

	private static string UsingTheToolGuide()
	{
		return PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_USING_THE_TOOL);
	}
}
