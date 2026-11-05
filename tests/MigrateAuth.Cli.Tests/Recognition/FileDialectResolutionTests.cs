using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// ONE DIALECT PER FILE, FROM THE LANGUAGE'S OWN DECLARATION. The declaration is the test-local one of
/// <see cref="ShellProbe"/>; the engine treats every identifier in it as an opaque string.
/// </summary>
public sealed class FileDialectResolutionTests
{
	private const string _PATH_WITHOUT_A_MAPPED_EXTENSION = "scripts/run.sh";
	private const string _ZSH_PATH = "scripts/run.zsh";
	private const string _UPPER_CASE_ZSH_PATH = "scripts/RUN.ZSH";
	private const string _KSH_PATH = "scripts/run.ksh";
	private const string _PATH_WITHOUT_AN_EXTENSION = "scripts/run";

	[Theory]
	[InlineData("#!/bin/bash\necho x", ShellProbe.BASH)]
	[InlineData("#!/usr/bin/zsh\necho x", ShellProbe.ZSH)]
	[InlineData("#!/bin/dash\necho x", ShellProbe.DASH)]
	[InlineData("#!/bin/mksh\necho x", ShellProbe.MKSH)]
	[InlineData("#!/usr/local/bin/ksh93 -p\necho x", ShellProbe.KSH93)]
	[InlineData("#! /bin/zsh\necho x", ShellProbe.ZSH)]
	[InlineData("#!/bin/zsh", ShellProbe.ZSH)]
	[InlineData("#!/bin/zsh\r\necho x", ShellProbe.ZSH)]
	public void TheInterpreterOfTheFirstLineDecidesTheDialect(string text, string expected)
	{
		string? dialect = FileDialectResolution.Resolve(ShellProbe.Dialects, text, _PATH_WITHOUT_A_MAPPED_EXTENSION);

		Assert.Equal(expected, dialect);
	}

	[Theory]
	[InlineData("#!/usr/bin/env zsh\necho x", ShellProbe.ZSH)]
	[InlineData("#!/usr/bin/env -S zsh -f\necho x", ShellProbe.ZSH)]
	[InlineData("#!/usr/bin/env -i ksh93\necho x", ShellProbe.KSH93)]
	[InlineData("#!/usr/bin/env -- mksh\necho x", ShellProbe.MKSH)]
	[InlineData("#!/usr/bin/env LANG=C PATH=/x dash\necho x", ShellProbe.DASH)]
	[InlineData("#!/usr/bin/env -S -i LC_ALL=C zsh -f\necho x", ShellProbe.ZSH)]
	[InlineData("#!/usr/bin/env\necho x", ShellProbe.BASH)]
	public void EnvIsSkippedWithItsOptionsAndAssignmentsToTheFirstRealWord(string text, string expected)
	{
		string? dialect = FileDialectResolution.Resolve(ShellProbe.Dialects, text, _PATH_WITHOUT_A_MAPPED_EXTENSION);

		Assert.Equal(expected, dialect);
	}

	[Theory]
	[InlineData("#!C:/tools/zsh.exe\necho x", ShellProbe.ZSH)]
	[InlineData("#!C:\\tools\\zsh.EXE\necho x", ShellProbe.ZSH)]
	[InlineData("#!/usr/bin/env ksh93.exe\necho x", ShellProbe.KSH93)]
	public void AnExeSuffixIsDroppedBeforeTheLookup(string text, string expected)
	{
		string? dialect = FileDialectResolution.Resolve(ShellProbe.Dialects, text, _PATH_WITHOUT_A_MAPPED_EXTENSION);

		Assert.Equal(expected, dialect);
	}

	[Fact]
	public void TheMapDecidesWhatAnInterpreterMeansSoShAndKshAreWhatTheProfileSays()
	{
		string? sh = FileDialectResolution.Resolve(ShellProbe.Dialects, "#!/bin/sh\n", _ZSH_PATH);
		string? ksh = FileDialectResolution.Resolve(ShellProbe.Dialects, "#!/bin/ksh\n", _PATH_WITHOUT_A_MAPPED_EXTENSION);

		Assert.Equal(ShellProbe.BASH, sh);
		Assert.Equal(ShellProbe.KSH93, ksh);
	}

	[Theory]
	[InlineData("echo x\n", _ZSH_PATH, ShellProbe.ZSH)]
	[InlineData("echo x\n", _UPPER_CASE_ZSH_PATH, ShellProbe.ZSH)]
	[InlineData("echo x\n", _KSH_PATH, ShellProbe.KSH93)]
	[InlineData("", _ZSH_PATH, ShellProbe.ZSH)]
	public void WithoutAFirstLineInterpreterTheExtensionDecidesTheDialect(string text, string path, string expected)
	{
		string? dialect = FileDialectResolution.Resolve(ShellProbe.Dialects, text, path);

		Assert.Equal(expected, dialect);
	}

	[Theory]
	[InlineData("echo x\n", _PATH_WITHOUT_A_MAPPED_EXTENSION)]
	[InlineData("echo x\n", _PATH_WITHOUT_AN_EXTENSION)]
	[InlineData("\n#!/bin/zsh\n", _PATH_WITHOUT_A_MAPPED_EXTENSION)]
	[InlineData(" #!/bin/zsh\n", _PATH_WITHOUT_A_MAPPED_EXTENSION)]
	public void WithNeitherAFirstLineInterpreterNorAMappedExtensionTheDefaultDialectApplies(string text, string path)
	{
		string? dialect = FileDialectResolution.Resolve(ShellProbe.Dialects, text, path);

		Assert.Equal(ShellProbe.BASH, dialect);
	}

	[Fact]
	public void AnUnknownInterpreterFallsThroughToTheExtensionAndThenToTheDefault()
	{
		const string unknownInterpreter = "#!/usr/bin/perl\nprint 1;\n";

		string? viaExtension = FileDialectResolution.Resolve(ShellProbe.Dialects, unknownInterpreter, _ZSH_PATH);
		string? viaDefault = FileDialectResolution.Resolve(ShellProbe.Dialects, unknownInterpreter, _PATH_WITHOUT_A_MAPPED_EXTENSION);

		Assert.Equal(ShellProbe.ZSH, viaExtension);
		Assert.Equal(ShellProbe.BASH, viaDefault);
	}

	[Fact]
	public void AFirstLineInterpreterBeatsTheExtension()
	{
		string? dialect = FileDialectResolution.Resolve(ShellProbe.Dialects, "#!/bin/dash\n", _ZSH_PATH);

		Assert.Equal(ShellProbe.DASH, dialect);
	}

	[Fact]
	public void ALanguageThatDeclaresNoDialectsHasNoDialectWhateverTheFileSays()
	{
		string? dialect = FileDialectResolution.Resolve(null, "#!/bin/zsh\n", _ZSH_PATH);

		Assert.Null(dialect);
	}
}
