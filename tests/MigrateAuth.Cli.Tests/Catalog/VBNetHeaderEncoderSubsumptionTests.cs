using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE `VBNet` HEADER CLASS OVER THE ENCODER INSIDE ITS VALUE (catalog amendment 0.7). A header built with `New AuthenticationHeaderValue(...)` or
/// `AuthenticationHeaderValue.Parse(...)` whose value carries the Base64 encoding of a colon pair is one finding, at VB-006, and VB-014 is subsumed at overlapping spans. A split construction, a header
/// write (VB-007), a Bearer scheme and a header with no credential pair keep their reading. Every source is invented from the catalog's stated forms.
/// </summary>
public sealed class VBNetHeaderEncoderSubsumptionTests
{
	private const string _VB_006 = "VB-006";
	private const string _VB_007 = "VB-007";
	private const string _VB_014 = "VB-014";

	private const string _ASSIGNMENT_PREFIX = "request.Headers.Authorization = ";

	private const string _ENCODER_PLACEHOLDER = "ENCODER";
	private const string _CONSTANT_SCHEME_DECLARATION = "Private Const SchemeName As String = \"Basic\"\n";
	private const string _VARIABLE_SCHEME_NAME = "scheme";
	private const string _VARIABLE_SCHEME_DECLARATION = "Dim scheme As String = \"Basic\"\n";

	private const string _AMPERSAND_ENCODER = "Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\" & password))";
	private const string _PLUS_ENCODER = "Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + \":\" + password))";
	private const string _CHARACTER_COLON_ENCODER = "Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\"c & password))";
	private const string _INTERPOLATION_ENCODER = "Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{userName}:{password}\"))";
	private const string _FORMAT_ENCODER = "Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format(\"{0}:{1}\", userName, password)))";
	private const string _CONCAT_ENCODER = "Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Concat(userName, \":\", password)))";

	private static readonly ImmutableArray<string> _parseEncoderSpellings = [_AMPERSAND_ENCODER, _PLUS_ENCODER, _INTERPOLATION_ENCODER, _FORMAT_ENCODER, _CONCAT_ENCODER];

	private static readonly ImmutableArray<string> _encoderSpellings =
	[
		_AMPERSAND_ENCODER,
		_PLUS_ENCODER,
		_CHARACTER_COLON_ENCODER,
		_INTERPOLATION_ENCODER,
		_FORMAT_ENCODER,
		_CONCAT_ENCODER,
		"Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Join(\":\", userName, password)))",
		"Convert.ToBase64String(Encoding.Default.GetBytes(userName & \":\" & password))",
		"Convert.ToBase64String(Encoding.GetEncoding(\"iso-8859-1\").GetBytes(userName & \":\" & password))",
		"System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userName & \":\" & password))"
	];

	private static readonly ImmutableArray<string> _parseValueShapes =
	[
		"\"Basic \" & ENCODER",
		"$\"Basic {ENCODER}\"",
		"String.Format(\"Basic {0}\", ENCODER)",
		"String.Concat(\"Basic \", ENCODER)",
		"scheme & \" \" & ENCODER",
		"String.Join(\" \", \"Basic\", ENCODER)",
		"String.Format(\"{0} {1}\", \"Basic\", ENCODER)"
	];

	public static TheoryData<string> ConstructorSources()
	{
		TheoryData<string> data = [];

		foreach (string encoder in _encoderSpellings)
		{
			data.Add($"{_ASSIGNMENT_PREFIX}New AuthenticationHeaderValue(\"Basic\", {encoder})");
			data.Add($"{_ASSIGNMENT_PREFIX}New System.Net.Http.Headers.AuthenticationHeaderValue(\"Basic\", {encoder})");
			data.Add($"{_ASSIGNMENT_PREFIX}New AuthenticationHeaderValue(scheme:=\"Basic\", parameter:={encoder})");
			data.Add($"{_CONSTANT_SCHEME_DECLARATION}{_ASSIGNMENT_PREFIX}New AuthenticationHeaderValue(SchemeName, {encoder})");
			data.Add($"{_VARIABLE_SCHEME_DECLARATION}{_ASSIGNMENT_PREFIX}New AuthenticationHeaderValue(scheme, {encoder})");
		}

		data.Add($"With client.DefaultRequestHeaders\n\t.Authorization = New AuthenticationHeaderValue(\"Basic\", {_AMPERSAND_ENCODER})\nEnd With");
		data.Add($"With client.DefaultRequestHeaders\n\t.Authorization = New AuthenticationHeaderValue(\"Basic\", {_INTERPOLATION_ENCODER})\nEnd With");

		return data;
	}

	public static TheoryData<string> ParseSources()
	{
		TheoryData<string> data = [];

		foreach (string shape in _parseValueShapes)
		{
			foreach (string encoder in _parseEncoderSpellings)
			{
				string value = shape.Replace(_ENCODER_PLACEHOLDER, encoder, StringComparison.Ordinal);
				string declaration = shape.StartsWith(_VARIABLE_SCHEME_NAME, StringComparison.Ordinal) ? _VARIABLE_SCHEME_DECLARATION : string.Empty;

				data.Add($"{declaration}Dim header = AuthenticationHeaderValue.Parse({value})");
			}
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(ConstructorSources))]
	[MemberData(nameof(ParseSources))]
	public void AHeaderValueBuiltFromAnInlineEncoderIsOneFindingAtTheHeaderClassAtMedium(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(_VB_006, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Theory]
	[MemberData(nameof(ConstructorSources))]
	[MemberData(nameof(ParseSources))]
	public void TheHeaderSpanCoversTheEncoderInTheGeneratedAndTheInterpretedEngine(string source)
	{
		VBNetRecognitionHarness harness = new();
		string text = harness.ContextFor(source).NormalizedText;
		Regex generatedHeader = harness.Analyzer.DetectionRulesByPatternClass[_VB_006].Expression;
		Regex generatedEncoder = harness.Analyzer.DetectionRulesByPatternClass[_VB_014].Expression;
		Regex[] headers = [generatedHeader, new Regex(generatedHeader.ToString(), generatedHeader.Options, generatedHeader.MatchTimeout)];
		Regex[] encoders = [generatedEncoder, new Regex(generatedEncoder.ToString(), generatedEncoder.Options, generatedEncoder.MatchTimeout)];

		for (int engine = 0; engine < headers.Length; engine++)
		{
			Match headerMatch = headers[engine].Match(text);
			Match encoderMatch = encoders[engine].Match(text);

			Assert.True(headerMatch.Success && encoderMatch.Success);
			Assert.True(encoderMatch.Index < headerMatch.Index + headerMatch.Length && headerMatch.Index < encoderMatch.Index + encoderMatch.Length, "the header span overlaps the encoder match");
			Assert.True(encoderMatch.Index >= headerMatch.Index, "the encoder call lies within the header value");
		}
	}

	[Fact]
	public void APairAssembledOnAnEarlierLineWithTheEncoderInlineInTheHeaderIsOneFinding()
	{
		const string source = "Dim pair = userName & \":\" & password\nrequest.Headers.Authorization = New AuthenticationHeaderValue(\"Basic\", Convert.ToBase64String(Encoding.ASCII.GetBytes(pair)))";
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(_VB_006, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void AnEncoderInItsOwnStatementBeforeTheHeaderIsTwoFindings()
	{
		const string source = "Dim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\" & password))\nrequest.Headers.Authorization = New AuthenticationHeaderValue(\"Basic\", encoded)";
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		Assert.Equal([_VB_006, _VB_014], constructs.Select(construct => construct.PatternClassIdentifier).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void AHeaderWriteWithAnInlineEncoderKeepsTwoFindings()
	{
		const string source = "client.DefaultRequestHeaders.Add(\"Authorization\", \"Basic \" & Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\" & password)))";
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		Assert.Equal([_VB_007, _VB_014], constructs.Select(construct => construct.PatternClassIdentifier).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void ABearerHeaderCannotSubsumeSoTheEncoderIsTheOnlyFinding()
	{
		const string source = "request.Headers.Authorization = New AuthenticationHeaderValue(\"Bearer\", Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\" & password)))";
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(_VB_014, construct.PatternClassIdentifier);
	}

	[Fact]
	public void ANonCredentialPairInsideABasicHeaderIsTheHeaderFindingAlone()
	{
		const string source = "request.Headers.Authorization = New AuthenticationHeaderValue(\"Basic\", Convert.ToBase64String(Encoding.ASCII.GetBytes(widgetHost & \":\" & portNumber)))";
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(_VB_006, construct.PatternClassIdentifier);
	}

	[Fact]
	public void TwoHeadersWithInlineEncodersInOneFileAreTwoHeaderFindingsAndNoEncoderFinding()
	{
		const string source = "Sub Build()\n\trequestOne.Headers.Authorization = New AuthenticationHeaderValue(\"Basic\", Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\" & password)))\n\trequestTwo.Headers.Authorization = New AuthenticationHeaderValue(\"Basic\", Convert.ToBase64String(Encoding.UTF8.GetBytes(otherUser & \":\" & otherPassword)))\nEnd Sub";
		ImmutableArray<RecognizedConstruct> constructs = new VBNetRecognitionHarness().Recognize(source);

		Assert.Equal([_VB_006, _VB_006], constructs.Select(construct => construct.PatternClassIdentifier));
	}
}
