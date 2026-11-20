using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The declared unsupported coverage of the `Python` analyzer, the statement the client's report carries when the language is enabled. A separate <c>internal</c> holder because <c>PythonAnalyzer</c> is <c>file</c>-scoped.
/// Written for the client reading the report: each construct names in plain words what the tool does not detect or cannot tell, and each reason says why and, where useful, what to do about it. No internal design term,
/// class identifier or section reference belongs in either field.
///
/// Only what is impossible by design, or bounded on purpose, is declared: a fact that is not in the analyzed text, an open set, an internal limit or a form of the language the reading does not model. The entries follow the
/// order of the statement of what is not covered (files and first lines, lexical forms, class and form entries, flow evidence) and the order carries no meaning in the report. A form that the analyzer reads at Low only is a
/// different statement (it IS recognized) and does not appear here. Grouped in helpers, each under the method-length limit.
/// </summary>
internal static class PythonDeclaredCoverage
{
	internal static FrozenSet<DeclaredUnsupportedCoverage> Build()
	{
		IEnumerable<DeclaredUnsupportedCoverage> entries = ForFilesAndFirstLines()
			.Concat(ForLexicalForms())
			.Concat(ForHeldAndHandedOverValues())
			.Concat(ForClientsAndForms())
			.Concat(ForReportedWithoutBeingApplicable())
			.Concat(ForNamesAndFolders())
			.Concat(ForSignInEvidence())
			.Concat(ForLimitsOfTheTool());

		return entries.ToFrozenSet();
	}

	private static DeclaredUnsupportedCoverage Entry(string construct, string reason)
	{
		return new DeclaredUnsupportedCoverage { Construct = construct, Reason = reason };
	}

	/// <summary>Which files are read as Python at all.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForFilesAndFirstLines()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"type stub files ending in .pyi, notebooks ending in .ipynb and Cython sources ending in .pyx",
				"these are not read as Python: a stub holds signatures, a notebook holds its code inside JSON text and a Cython source is a different language; such files are not attributed to Python and stay in the list of files that were not assessed"),
			Entry(
				"Python code held in a file of another type: a fenced block in a Markdown document, a template, a configuration file or a JSON string",
				"the containing file is not a Python script, so its text is not analyzed as one; search such files separately for credentials passed to an HTTP client"),
			Entry(
				"a Python script with no file extension whose first line does not name python, python2, python3 or python3 followed by a dot and digits, in lower case, as the interpreter, directly or after env and options written as one word: python2 with a minor version such as python2.7, pypy, an interpreter that follows a variable assignment after env, an interpreter followed by the -S option, and a file with no first line at all",
				"such a file is not attributed to Python, so it stays in the list of files that were not assessed; a silent omission is the cost, and the files that were not assessed are named. The first line is read only for a file with no extension: a file ending in .py or .pyw is read whatever its first line names"),
			Entry(
				"syntax that exists only in Python 2: the print statement, backtick quoting, raw_input and the combined ur prefix on a string",
				"Python 2 code is read as Python 3 and is not parsed as Python 2; a construct that has no Python 3 form is not recognized, while the library names that survive, such as the Basic authentication handler of the standard library, are")
		];

		return entries;
	}

	/// <summary>Forms of the language that the reading of strings and comments does not model, and what follows from one.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForLexicalForms()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a format specification in an f-string or t-string field that holds a hash after a space sign or after the z option, a quote character used as the fill character, or a backslash before a brace",
				"the text after the colon of a field is literal text in Python, but the field is read as code and only the common forms of a hash are repaired; with these forms the reading of the file can stop at that point"),
			Entry(
				"what follows in a file whose reading stopped at such a form: every construct after that point",
				"the rest of the file is read as string text, so a construct there is given Low confidence and not its own level, a literal credential there carries no in-code secret warning, no sign-in evidence there is seen, so a file whose only evidence lies there is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in, and a marker of a test or a generated file that lies there does not apply"),
			Entry(
				"a comment written with no space directly after a colon, an operator character or a dictionary colon, such as a comment after a block opening colon, and one that holds an apostrophe or a triple quote",
				"the hash after such a character is taken for part of a format specification and not for a comment, so the words of the comment can be read as code, and an apostrophe or a triple quote in it can open a string that nothing closes"),
			Entry(
				"another language written inside a string: SQL, markup, a template or a regular expression, and a doctest line in a docstring",
				"the text of a string is not read as another language or as code; a construct written in it is given Low confidence for manual review, never at its own level")
		];

		return entries;
	}

	/// <summary>A value, tuple, header or URL that is not written at the call.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForHeldAndHandedOverValues()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a tuple, header mapping or URL held in a variable, module or argument and handed over elsewhere (for example an auth value that is a name, headers given as a name, *args and **kwargs); a URL assembled from parts; a list, a one-element or a three-element tuple in place of a pair; arguments nested deeper than four levels or longer than the reading window of one call; a URL with user information inside a bracketed group of a call such as a proxies mapping; and a token request whose grant type is built in another statement",
				"one call is read at a time, within a bounded window, and a definition is not joined to its use; a pair held in a name that is itself called auth is read, and a helper object held in a name is read where it is created"),
			Entry(
				"a header written by merging, copying or a hook: the | and ** operators, setdefault, copy, a custom adapter",
				"the header carriers that are read are a fixed list of the documented forms, and a header assembled by another mechanism has no carrier to anchor on"),
			Entry(
				"a Basic header built by a project function and returned, or held with no header write; an encoder and its header in different statements farther apart than the reading window; a call to a wrapper function; a scheme held in a parameter default, in another module or in a multi-line or triple-quoted literal",
				"calls to a wrapper are not followed and the scheme of a held value is resolved only from one single-line declaration before its use in the same file; the construct inside the wrapper is read where it is written, and a literal credential handed to the wrapper carries no in-code secret warning at the call"),
			Entry(
				"a Basic header assembled through a t-string template and rendered by project code, a header joined with join, and an assembly whose first operand is not a literal or whose scheme word is held in a constant or split across literals",
				"a template is not a string and no HTTP client documents rendering one, and only a leading literal word is read as the scheme")
		];

		return entries;
	}

	/// <summary>Clients, protocols and mechanisms the tool does not follow.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForClientsAndForms()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"the implicit use of a .netrc file: a client that reads credentials from the home directory or from the netrc environment variable with no construct in the code, and a program that reads the file itself with the standard library",
				"the code holds no Basic construct and the credential is in a file outside the analyzed text; search such files separately for credentials"),
			Entry(
				"the credential options of HTTP clients that are not documented, such as another library's own basic authentication, HTTP authentication or login option, and undocumented or private helpers and a hand-written authentication class",
				"the set of clients is open and no list can name every one; the header and the two-element auth forms are read whatever the client, and a Basic header that a hand-written class writes is read where it is written"),
			Entry(
				"a request written by hand to a socket, a TLS stream or an asynchronous stream, including a literal Authorization: Basic line",
				"the set of places a request text can be written to is open"),
			Entry(
				"code reached through reflection or text: exec, eval, getattr with a built name and an import with a built name",
				"executed strings are not parsed, and a name that is built at run time is not text at the call"),
			Entry(
				"credentials of the non-HTTP protocols of the standard library (mail, file transfer and similar) and the Digest, NTLM, Kerberos and OAuth 1 helpers",
				"these are other mechanisms, not Basic authentication, and are deliberately not matched")
		];

		return entries;
	}

	/// <summary>Constructs that are reported although they may not apply: the tool cannot see the destination or the reachability.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForReportedWithoutBeingApplicable()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"whether an auth pair, a proxy header built with the Basic encoder of an HTTP client, a password manager given to a Digest or proxy handler, or a Basic construct that is built and never sent (unreachable code, a branch that is never taken) is Basic authentication to RelativityOne",
				"the receiver of a call, its destination and whether a line runs are not examined, so these constructs are given their level and may be false positives; review each one before acting"),
			Entry(
				"confirmation that a call goes to RelativityOne and not to another server",
				"a call is seen and the destination host cannot be determined; such matches are still kept, with a note that they may not apply if the destination is not RelativityOne - review each one before acting")
		];

		return entries;
	}

	/// <summary>The demotion of a file by its name.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForNamesAndFolders()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a test, mock, sample or generated file with a neutral name and no marker in its text",
				"no name or marker says it is one and no text shows it, so it is given its full level and may be a false positive; review such matches"),
			Entry(
				"a product file named like a test, a source file under a folder named for tests, a source folder named like a virtual environment (venv, .venv, dist-packages, __pypackages__, .tox), and the folders env, virtualenv, .nox, .eggs and a versioned venv name",
				"the demotion of a file by its name rests on conventions: a product file named like a test or a source folder with such a name is given a lower level than it deserves, and a virtual-environment folder with another name is walked and given its full level")
		];

		return entries;
	}

	/// <summary>How the tool decides between the two OAuth 2.0 flows, and what it does not see.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForSignInEvidence()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"sign-in evidence that is not listed: a prompt library outside the standard library, a password given as a command-line argument, a web server whose sign-in is handled outside the code, a program that is already an OAuth callback, the raw_input prompt of Python 2, and a configuration read through a receiver that is not in the list",
				"the set of possible prompts is open and only the signals with a stable shape in the code are read; a file whose evidence is of another kind is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in; that is an expected outcome and not a failure - where the program acts for a signed-in user, use the Authorization Code flow"),
			Entry(
				"sign-in evidence written in a form that is not read: a request object reached through another object, a framework import list written over several lines, a qualified form class name, a form field with no annotation, a name imported from os or from the browser module, and a module renamed on import",
				"the signal expressions require the plain form, so such a file is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in; the evidence is lost and never a match"),
			Entry(
				"a program that opens a browser to a help page and also reads its credential from the environment, a command-line tool that listens on all interfaces to receive a redirect, a program run with no terminal that prompts for a password, and a prompt function defined more than thirty lines before its call",
				"the flow rule assigns the interactive flow when an interactive signal is present, so such a file can be assigned Authorization Code where it is run for a service, and the other forms are read as the default flow (Client Credentials unless another default is configured), the flow used when nothing shows that a person signs in; review the flow of such a file")
		];

		return entries;
	}

	/// <summary>The internal limits of the tool, each of which keeps its cost bounded.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForLimitsOfTheTool()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a file whose analysis takes too long, a file larger than 2 MiB, a line longer than 10,000 characters, a string literal read as part of a call that is longer than 300 characters, and a credential longer than 200 characters",
				"these are internal limits that keep the cost bounded: a file that trips a time limit or holds a line over the ceiling is skipped and counted, so the loss is visible as a number, and a window that grew without bound would make the cost grow without bound"),
			Entry(
				"which matches were left out when the cap on matches was reached, per file or in all, including a real match pushed out by the many matches of a walked vendored folder",
				"the cap is stated in aggregate only, so which matches were left out cannot be said"),
			Entry(
				"a literal credential written as a triple-quoted string, as adjacent literals, with a backslash escape or as the value of a login keyword, and a literal assigned to an attribute such as self.password or self.url",
				"the construct is still matched where its class reads it, but the in-code secret warning is not raised for these forms, and an attribute target that holds a literal credential or a user-information URL is not read at all")
		];

		return entries;
	}
}
