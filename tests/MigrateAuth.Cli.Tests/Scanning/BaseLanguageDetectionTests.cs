using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// The base's own extension and shebang table, exercised directly and without a catalog -
/// this table has no registered analyzer behind it by construction, so a test that went through
/// <see cref="LanguageAttribution"/> would only ever prove the table is reachable, not what it
/// answers.
/// </summary>
public sealed class BaseLanguageDetectionTests
{
	[Theory]
	[InlineData(".cs", AssessedLanguage.CSharp)]
	[InlineData(".vb", AssessedLanguage.VBNet)]
	[InlineData(".js", AssessedLanguage.JavaScript)]
	[InlineData(".mjs", AssessedLanguage.JavaScript)]
	[InlineData(".cjs", AssessedLanguage.JavaScript)]
	[InlineData(".jsx", AssessedLanguage.JavaScript)]
	[InlineData(".ts", AssessedLanguage.TypeScript)]
	[InlineData(".tsx", AssessedLanguage.TypeScript)]
	[InlineData(".mts", AssessedLanguage.TypeScript)]
	[InlineData(".cts", AssessedLanguage.TypeScript)]
	[InlineData(".py", AssessedLanguage.Python)]
	[InlineData(".pyw", AssessedLanguage.Python)]
	[InlineData(".go", AssessedLanguage.Go)]
	[InlineData(".ps1", AssessedLanguage.PowerShell)]
	[InlineData(".psm1", AssessedLanguage.PowerShell)]
	[InlineData(".psd1", AssessedLanguage.PowerShell)]
	[InlineData(".sh", AssessedLanguage.Shell)]
	[InlineData(".bash", AssessedLanguage.Shell)]
	[InlineData(".zsh", AssessedLanguage.Shell)]
	[InlineData(".ksh", AssessedLanguage.Shell)]
	public void EveryDeclaredExtensionResolvesToItsLanguage(string extension, AssessedLanguage expected)
	{
		bool found = BaseLanguageDetection.TryGetByExtension(extension, out AssessedLanguage language);

		Assert.True(found);
		Assert.Equal(expected, language);
	}

	/// <summary>The catalog's own extension index compares without case, and this table matches it.</summary>
	[Fact]
	public void TheExtensionTableIsMatchedWithoutCase()
	{
		bool found = BaseLanguageDetection.TryGetByExtension(".CS", out AssessedLanguage language);

		Assert.True(found);
		Assert.Equal(AssessedLanguage.CSharp, language);
	}

	[Fact]
	public void AnUndeclaredExtensionIsNotFound()
	{
		bool found = BaseLanguageDetection.TryGetByExtension(".rb", out _);

		Assert.False(found);
	}

	[Theory]
	[InlineData("#!/usr/bin/env python3", AssessedLanguage.Python)]
	[InlineData("#!/usr/bin/env python3.12", AssessedLanguage.Python)]
	[InlineData("#!/usr/bin/env python2", AssessedLanguage.Python)]
	[InlineData("#!/usr/bin/env python", AssessedLanguage.Python)]
	[InlineData("#!/usr/bin/python3", AssessedLanguage.Python)]
	[InlineData("#!/bin/bash", AssessedLanguage.Shell)]
	[InlineData("#!/bin/sh", AssessedLanguage.Shell)]
	[InlineData("#!/usr/bin/env zsh", AssessedLanguage.Shell)]
	[InlineData("#!/bin/ksh", AssessedLanguage.Shell)]
	[InlineData("#!/usr/bin/env pwsh", AssessedLanguage.PowerShell)]
	[InlineData("#!/usr/bin/powershell", AssessedLanguage.PowerShell)]
	public void EveryDeclaredShebangResolvesToItsLanguage(string firstLine, AssessedLanguage expected)
	{
		bool found = BaseLanguageDetection.TryGetByShebangFirstLine(firstLine, out AssessedLanguage language);

		Assert.True(found);
		Assert.Equal(expected, language);
	}

	/// <summary>
	/// `env` carrying a flag before the interpreter name - `-S` is the common real-world case - is
	/// skipped rather than mistaken for the interpreter itself.
	/// </summary>
	[Fact]
	public void AnEnvFlagIsSkippedToReachTheInterpreter()
	{
		bool found = BaseLanguageDetection.TryGetByShebangFirstLine(
			"#!/usr/bin/env -S python3",
			out AssessedLanguage language);

		Assert.True(found);
		Assert.Equal(AssessedLanguage.Python, language);
	}

	[Fact]
	public void AVersionedPythonThreeInterpreterIsRecognized()
	{
		bool found = BaseLanguageDetection.TryGetByShebangFirstLine(
			"#!/usr/bin/env python3.9",
			out AssessedLanguage language);

		Assert.True(found);
		Assert.Equal(AssessedLanguage.Python, language);
	}

	[Fact]
	public void AnUnknownInterpreterIsNotFound()
	{
		bool found = BaseLanguageDetection.TryGetByShebangFirstLine("#!/usr/bin/perl", out _);

		Assert.False(found);
	}

	/// <summary>`python2.<digits>` is not one of the declared forms - only `python3.<digits>` is.</summary>
	[Fact]
	public void AVersionedPythonTwoInterpreterIsNotFound()
	{
		bool found = BaseLanguageDetection.TryGetByShebangFirstLine("#!/usr/bin/env python2.7", out _);

		Assert.False(found);
	}

	[Fact]
	public void ALineWithNoShebangPrefixIsNotFound()
	{
		bool found = BaseLanguageDetection.TryGetByShebangFirstLine("python3 -m build", out _);

		Assert.False(found);
	}

	[Fact]
	public void AShebangWithNoInterpreterTokenIsNotFound()
	{
		bool found = BaseLanguageDetection.TryGetByShebangFirstLine("#!", out _);

		Assert.False(found);
	}
}
