using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The declared unsupported coverage of the `Shell` analyzer, the statement the client's report carries when the language is enabled. A separate
/// <c>internal</c> holder because <c>ShellAnalyzer</c> is <c>file</c>-scoped. Each construct names in plain words what the tool does not detect or cannot
/// tell, and each reason says why and, where useful, what to do about it. The classes supported at Low only are a different statement (they ARE recognized)
/// and do not appear here.
///
/// Only what is impossible by design is declared: a fact that is not in the analyzed text, an open set, an internal limit or a fact of one dialect. The
/// entries are written in the order of the catalog's declared list, and the order carries no meaning in the report. Grouped in five helpers.
/// </summary>
internal static class ShellDeclaredCoverage
{
	internal static FrozenSet<DeclaredUnsupportedCoverage> Build()
	{
		IEnumerable<DeclaredUnsupportedCoverage> entries = ForFilesAndLanguages()
			.Concat(ForOtherFilesAndFlowEvidence())
			.Concat(ForClientsAndForms())
			.Concat(ForDialectFacts())
			.Concat(ForLimitsOfTheTool());

		return entries.ToFrozenSet();
	}

	private static DeclaredUnsupportedCoverage Entry(string construct, string reason)
	{
		return new DeclaredUnsupportedCoverage { Construct = construct, Reason = reason };
	}

	/// <summary>Which files and which languages are read at all.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForFilesAndLanguages()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"other shells and batch files: fish, csh and tcsh scripts, and Windows batch files ending in .bat or .cmd",
				"these are different languages from the POSIX family of shells this tool reads; such files are not attributed to Shell and stay in the list of files that were not assessed"),
			Entry(
				"shell commands embedded in other files: CI pipeline definitions, Dockerfiles, Makefiles, crontabs, service unit files and Markdown documents",
				"the containing file is not a shell script, so its text is not analyzed as one; many command lines that pass a user name and password to an HTTP client live in such files, so search them separately"),
			Entry(
				"shell scripts the tool does not attribute to Shell: files ending in .bats, shell start-up files such as .bashrc, .bash_profile, .profile, .zshrc, .zshenv and .envrc, and a script with no file extension whose first line does not name bash, sh, zsh or ksh, in lower case, as the interpreter, directly or after env and any options written as one word, such as dash, ash, mksh, ksh93 or busybox sh, or that puts a separate argument word or a variable assignment between env and the shell",
				"such a file is not attributed to Shell, so it stays in the list of files that were not assessed; a silent omission is the cost, and the report names the files it did not assess. The first line is read only for a file with no extension: a file ending in .sh, .bash, .zsh or .ksh is read whatever its first line names, and a file with any other extension is not attributed to Shell by its first line"),
			Entry(
				"a source file saved in a single-byte encoding other than Windows-1252, or one whose only line terminator is a carriage return or the Unicode line separator",
				"the tool reads a single-byte file as Windows-1252, and the report names the files it could not read with confidence; line ends other than carriage return and line feed are not recognized")
		];

		return entries;
	}

	/// <summary>What a per-file reading cannot join, and the flow evidence it cannot list.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForOtherFilesAndFlowEvidence()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"credentials held in a file the script only reads: the contents of a .netrc file, a .curlrc file or a configuration file passed to curl that the script does not write, a .wgetrc file, .env files and other credential files",
				"the credential is in another file, not in the text analyzed, and a configuration file is where a credential usually lives; search such files separately for user names and passwords. A configuration the script writes itself is read"),
			Entry(
				"a header, token or scheme supplied by another file through source or the dot command",
				"no per-file analysis joins a definition in one file to its use in another; an option such as a user given as a variable is read whatever the variable holds"),
			Entry(
				"evidence of how a user signs in that is supplied from another file through source or the dot command",
				"sign-in evidence is read per file; a file whose evidence is elsewhere is assigned the default flow, which is an expected outcome and not a failure - where the script acts for a signed-in user, use the Authorization Code flow"),
			Entry(
				"sign-in evidence the tool does not list: a custom prompt function, a prompt program, a credential read by command substitution from a file, a sourced environment file",
				"the set of possible prompts is open and a prompt with no stable shape cannot be listed in advance; only the signals with a stable shape in the code are read, and a file whose evidence is of another kind is assigned the default flow"),
			Entry(
				"the idiom of an environment variable first and a silent prompt otherwise",
				"the flow rule assigns the interactive flow when an interactive signal is present, so such a file is assigned Authorization Code even where the script also reads a fixed credential")
		];

		return entries;
	}

	/// <summary>Clients, headers and names the tool does not follow.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForClientsAndForms()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a header written to a file that is passed to the client as a header file",
				"the header text is in a file the script names by path and builds with format strings and variables; telling a Basic header from the migrated Bearer one needs that text, which a per-file analysis has only for the part the file writes literally"),
			Entry(
				"a name reached by indirection, and, behind a same-file alias or command variable, the forms of a credentials file option, the any-authentication option, a configuration file, an argument array and a header taken from a file",
				"a name reached by indirection is not text at the call, and the further forms behind an alias are left out to keep that reading inside its cost limits; the same forms written directly on the client's command are read"),
			Entry(
				"any other client: other command-line transfer tools, python, perl and node programs that make the call, the short aliases of the lwp-request tool, and a configuration of another client that the script writes",
				"the set of clients is open and no list can name every one; a call written by hand to a network device or to a socket is read, a program in another language is not"),
			Entry(
				"a mock, fake or stub script with a neutral name",
				"no name or folder says it is one and no text shows it, so it is reported at its full level and may be a false positive; review such findings"),
			Entry(
				"a construct that is in a dead branch or after an exit, or a function that is never called when it references no parameter",
				"whether a line runs depends on the order and the conditions of execution, which the tool cannot know without parsing the script; such code may be reported although it never authenticates to RelativityOne"),
			Entry(
				"confirmation that a call goes to RelativityOne and not to another server",
				"the tool sees a command line and cannot determine the destination host; such findings are still reported, with a note that they may not apply if the destination is not RelativityOne - review each one before acting")
		];

		return entries;
	}

	/// <summary>Facts of one dialect that the tool does not model, because it reads one family of shells.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForDialectFacts()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"ANSI-C quoting in a script run by dash, including as a here-document terminator word",
				"dash has no ANSI-C quoting and reads such a word as an expansion, a different word from the one bash, zsh, ksh93 and mksh decode, so the script means something else in dash itself; the tool reads the form as those shells do"),
			Entry(
				"a here-document terminator continued across a backslash and a line feed on a body line, in a script run by dash",
				"dash compares the word with the physical line and removes the pair only afterwards, so such a terminator never closes the body in dash; bash joins the lines and closes it, and the tool follows bash"),
			Entry(
				"a here-document terminator word quoted across a raw line feed, in a script run by dash",
				"dash closes the body on two consecutive lines that hold the two halves of the word, while bash never closes it and reads to the end of the file; the tool follows bash, so the lines dash runs after the two halves are read as body"),
			Entry(
				"a here-document opened inside a command substitution that closes on the opening line, in code or in a double-quoted string, in bash before version 5.2 and in dash",
				"those shells end the here-document at the substitution's end with an empty body and run the following lines as commands, where the tool reads them as the body, as bash 5.2 does")
		];

		return entries;
	}

	/// <summary>The internal limits of the tool, each of which keeps its cost bounded.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForLimitsOfTheTool()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a file whose analysis takes too long, a command longer than the reading window, a quoted string longer than 4,096 characters inside a window, a line longer than 10,000 characters, an array of command options whose text, from its opening parenthesis to its closing one, is longer than one line of 10,000 characters or thirty lines of 400 characters, and a use of a variable more than thirty lines after its definition",
				"these are internal limits that keep the cost bounded: a file that trips a time limit or holds a line over the ceiling is skipped and counted in the report, so the loss is visible as a number, and a window that grew without bound would make the cost grow without bound"),
			Entry(
				"which findings were left out when the findings cap was reached, per file or in all",
				"the cap is reported in aggregate only, so the report cannot say which findings were left out"),
			Entry(
				"more than 5,000 here-document openings waiting for their bodies in one region of a file",
				"an opening past that bound is read as code and its body is read as code, silently; no script holds thousands of openings before its first body, and the bound limits memory and time"),
			Entry(
				"whether a double parenthesis opens arithmetic or two nested subshells, when a here-document body inside the expression hides its closing, and any run read after the per-file read-ahead budget is spent",
				"the choice is a bounded read-ahead and not the shell's parser; after the budget is spent a run is read as not arithmetic, silently, so that the walk stays linear")
		];

		return entries;
	}
}
