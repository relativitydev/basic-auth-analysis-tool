using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// A FLOW SIGNAL IS EVALUATED ONLY IN THE DIALECTS IT IS DECLARED FOR. The dialect is the file's, resolved once;
/// the signals are the test-local ones of <see cref="ShellProbe"/> and nothing in the engine knows what a dialect is.
/// Every file carries one client call, because the flow phase is evaluated for a file holding a High or Medium construct.
/// </summary>
public sealed class DialectGatedFlowSignalTests
{
	private const string _SILENT_READ_LINE = "read -s PW";
	private const string _PROMPT_READ_LINE = "read -p \"Password: \" PW";
	private const string _SELECT_PROMPT_LINE = "select_prompt";
	private const string _SERVICE_TOKEN_LINE = "USE_SERVICE_TOKEN";
	private const string _ZSH_PATH = "scripts/run.zsh";
	private const string _KSH_PATH = "scripts/run.ksh";

	[Theory]
	[InlineData("#!/bin/bash", FlowEvidence.Interactive)]
	[InlineData("#!/bin/sh", FlowEvidence.Interactive)]
	[InlineData("#!/usr/bin/env bash", FlowEvidence.Interactive)]
	[InlineData("#!/bin/ksh93", FlowEvidence.None)]
	[InlineData("#!/bin/mksh", FlowEvidence.None)]
	[InlineData("#!/usr/bin/env -S ksh93 -p", FlowEvidence.None)]
	[InlineData("#!/bin/zsh", FlowEvidence.None)]
	public void AReadWithTheSilentOptionIsInteractiveEvidenceInBashAndNotInTheOtherShells(string firstLine, FlowEvidence expected)
	{
		FlowEvidence evidence = EvidenceOf(
			[ShellProbe.SilentReadInBash],
			ShellProbe.Source(firstLine, ShellProbe.CLIENT_CALL_LINE, _SILENT_READ_LINE));

		Assert.Equal(expected, evidence);
	}

	[Theory]
	[InlineData("#!/bin/bash", FlowEvidence.Interactive)]
	[InlineData("#!/bin/dash", FlowEvidence.Interactive)]
	[InlineData("#!/bin/sh", FlowEvidence.Interactive)]
	[InlineData("#!/bin/zsh", FlowEvidence.None)]
	[InlineData("#!/bin/ksh93", FlowEvidence.None)]
	[InlineData("#!/bin/ksh", FlowEvidence.None)]
	[InlineData("#!/bin/mksh", FlowEvidence.None)]
	public void AReadWithAPromptIsInteractiveEvidenceInBashAndDashAndNotInZshOrTheKornShells(string firstLine, FlowEvidence expected)
	{
		FlowEvidence evidence = EvidenceOf(
			[ShellProbe.PromptReadInBashAndDash],
			ShellProbe.Source(firstLine, ShellProbe.CLIENT_CALL_LINE, _PROMPT_READ_LINE));

		Assert.Equal(expected, evidence);
	}

	[Fact]
	public void AFileWithNoFirstLineKeepsTheDefaultDialectAndTheSignalsOfThatDialect()
	{
		string source = ShellProbe.Source(ShellProbe.CLIENT_CALL_LINE, _SILENT_READ_LINE, _PROMPT_READ_LINE);

		FlowEvidence viaTheUnmappedExtension = EvidenceOf(
			[ShellProbe.SilentReadInBash, ShellProbe.PromptReadInBashAndDash],
			source,
			ShellProbe.PathWithUnmappedExtension);

		Assert.Equal(FlowEvidence.Interactive, viaTheUnmappedExtension);
	}

	[Theory]
	[InlineData(_ZSH_PATH)]
	[InlineData(_KSH_PATH)]
	public void AFileWithNoFirstLineTakesTheDialectOfItsExtension(string path)
	{
		string source = ShellProbe.Source(ShellProbe.CLIENT_CALL_LINE, _SILENT_READ_LINE, _PROMPT_READ_LINE);

		FlowEvidence evidence = EvidenceOf([ShellProbe.SilentReadInBash, ShellProbe.PromptReadInBashAndDash], source, path);

		Assert.Equal(FlowEvidence.None, evidence);
	}

	[Fact]
	public void AFirstLineInterpreterBeatsTheExtensionAndAnUnknownOneFallsBackToIt()
	{
		string bashOverZsh = ShellProbe.Source("#!/bin/bash", ShellProbe.CLIENT_CALL_LINE, _SILENT_READ_LINE);
		string unknownOnZsh = ShellProbe.Source("#!/usr/bin/perl", ShellProbe.CLIENT_CALL_LINE, _SILENT_READ_LINE);

		FlowEvidence beaten = EvidenceOf([ShellProbe.SilentReadInBash], bashOverZsh, _ZSH_PATH);
		FlowEvidence fellBack = EvidenceOf([ShellProbe.SilentReadInBash], unknownOnZsh, _ZSH_PATH);

		Assert.Equal(FlowEvidence.Interactive, beaten);
		Assert.Equal(FlowEvidence.None, fellBack);
	}

	[Theory]
	[InlineData("#!/bin/bash", FlowEvidence.Interactive)]
	[InlineData("#!/bin/ksh93", FlowEvidence.Interactive)]
	[InlineData("#!/bin/zsh", FlowEvidence.None)]
	public void ASignalThatExcludesADialectIsEvaluatedInEveryOtherOne(string firstLine, FlowEvidence expected)
	{
		FlowEvidence evidence = EvidenceOf(
			[ShellProbe.SelectPromptExceptInZsh],
			ShellProbe.Source(firstLine, ShellProbe.CLIENT_CALL_LINE, _SELECT_PROMPT_LINE));

		Assert.Equal(expected, evidence);
	}

	[Fact]
	public void ASignalWithNoDialectDeclarationIsEvaluatedInEveryDialect()
	{
		string source = ShellProbe.Source("#!/bin/zsh", ShellProbe.CLIENT_CALL_LINE, _SERVICE_TOKEN_LINE);

		FlowEvidence evidence = EvidenceOf([ShellProbe.ServiceToken], source);

		Assert.Equal(FlowEvidence.Fixed, evidence);
	}

	[Fact]
	public void WhereTheLanguageDeclaresNoDialectsASignalThatNamesOneNeverFiresAndOtherSignalsStillDo()
	{
		string source = ShellProbe.Source("#!/bin/bash", ShellProbe.CLIENT_CALL_LINE, _SILENT_READ_LINE, _SERVICE_TOKEN_LINE);
		ShellProbeAnalyzer withoutDialects = ShellProbe.AnalyzerWith([ShellProbe.SilentReadInBash]);
		ShellProbeAnalyzer withoutDialectsButAPlainSignal = ShellProbe.AnalyzerWith([ShellProbe.SilentReadInBash, ShellProbe.ServiceToken]);

		RecognitionResult named = ShellProbe.Recognize(withoutDialects, source).Result;
		RecognitionResult plain = ShellProbe.Recognize(withoutDialectsButAPlainSignal, source).Result;

		Assert.Equal(FlowEvidence.None, named.FlowEvidence);
		Assert.Equal(FlowEvidence.Fixed, plain.FlowEvidence);
	}

	[Fact]
	public void TheSameSignalsInTheSameDialectGiveTheSameEvidenceOnTwoRuns()
	{
		string source = ShellProbe.Source("#!/bin/dash", ShellProbe.CLIENT_CALL_LINE, _PROMPT_READ_LINE);
		ImmutableArray<FlowSignal> signals = [ShellProbe.PromptReadInBashAndDash];

		FlowEvidence first = EvidenceOf(signals, source);
		FlowEvidence second = EvidenceOf(signals, source);

		Assert.Equal(FlowEvidence.Interactive, first);
		Assert.Equal(first, second);
	}

	private static FlowEvidence EvidenceOf(
		ImmutableArray<FlowSignal> signals,
		string source,
		string path = ShellProbe.DEFAULT_PATH)
	{
		return ShellProbe.Recognize(ShellProbe.AnalyzerWith(signals, ShellProbe.Dialects), source, path).Result.FlowEvidence;
	}
}
