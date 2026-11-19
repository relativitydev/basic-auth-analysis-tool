using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The declared unsupported coverage of the `Go` analyzer, the statement the client's report carries when the language is enabled. A separate <c>internal</c> holder because <c>GoAnalyzer</c> is
/// <c>file</c>-scoped. Written for the client reading the report: each construct names in plain words what the tool does not detect or cannot tell, and each reason says why and, where useful,
/// what to do about it. No internal design term, class identifier or section reference belongs in either field. The classes supported at Low only are a different statement (they ARE recognized)
/// and do not appear here.
///
/// The forty-six declared records: forty-two entries of the declared unsupported coverage and four known limits, in six themes and one group of limits; the order carries no meaning in the report. Only what is impossible by
/// design, open by nature, a declared cost or an internal limit is declared. Grouped in helpers, each under the method-length limit.
/// </summary>
internal static class GoDeclaredCoverage
{
	internal static FrozenSet<DeclaredUnsupportedCoverage> Build()
	{
		IEnumerable<DeclaredUnsupportedCoverage> entries = ForRequestsAndClientsTheToolDoesNotRead()
			.Concat(ForHeadersNamesAndValues())
			.Concat(ForWhatAPerFileReadingCannotJoin())
			.Concat(ForFilesAndLanguagesOtherThanGo())
			.Concat(ForFilesThatReadAtTheWrongLevel())
			.Concat(ForProxiesAndTheFlowOfAFinding())
			.Concat(ForLimitsOfTheTool());

		return entries.ToFrozenSet();
	}

	private static DeclaredUnsupportedCoverage Entry(string construct, string reason)
	{
		return new DeclaredUnsupportedCoverage { Construct = construct, Reason = reason };
	}

	/// <summary>Requests and clients the tool cannot see as a call it knows.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForRequestsAndClientsTheToolDoesNotRead()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a request written by hand to a socket or to a raw TLS connection, with a literal Authorization: Basic line in the text",
				"the set of transports is open, and the header text sits inside a text string, which is treated as weak evidence and given only Low confidence; flagging every file that mentions the header would flag nearly every network file"),
			Entry(
				"generated API clients, hooks and interceptors that are not read, and a header written by a function call, except an Authorization header written by Set, Add or an index assignment whose value is a call to a Basic helper declared once in the same file (that call matches); a helper in another file or package, a method call and a call through a value held in a variable stay unread",
				"the set is open: the requests are hidden behind generated methods, and only the HTTP clients listed are recognized; search such clients separately for Basic credentials"),
			Entry(
				"ways of sending credentials other than a standard HTTP header: the metadata of a remote procedure call, a message header of another protocol",
				"only a header set by a call, an assignment to a header entry, or a header written out as a list of values is recognized, so a Basic value written anywhere else is not detected; a connection that takes a standard HTTP header is covered like any other header"),
			Entry(
				"a custom transport written as a function type or an anonymous function value, with no method named RoundTrip at the construct",
				"a custom transport is found through the method name RoundTrip, and a function has none; a Basic construct inside the function is still detected where it is written"),
			Entry(
				"the credential options of a third-party client other than a method named SetBasicAuth: a configuration struct with user name and password fields, a builder with another method name",
				"the set of such clients is open, and only the HTTP clients listed are recognized"),
			Entry(
				"the encoding of a user name and password with a custom alphabet, or with the streaming encoder",
				"the alphabet is only known when the program runs, and the streaming form has no call to recognize; neither is the standard encoding that the Basic scheme uses"),
			Entry(
				"a header call written through its type with the header as the first argument, and a method taken as a value and never called, whether a header method or the Basic method",
				"a header call is recognized by its first argument being the header name; written through its type the header comes first, and a method taken as a value has no call at that place")
		];

		return entries;
	}

	/// <summary>Headers, names and values the text does not give at the call.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForHeadersNamesAndValues()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a Basic value held in a variable, or in an element of a composite literal such as a table of test cases, with no header call in the same statement; a name whose value the file does not settle; a header or map entry whose value is a name or a function call",
				"a text held in a variable shows nothing about how it is used, and its use is not in the same statement; the value of a name is followed only when the file declares it once, before its use, and otherwise nothing is matched, because a header set from a name is the usual shape of the migrated Bearer code"),
			Entry(
				"a header name that is not written as text at the call, such as a constant that holds Authorization, and a header map filled key by key in a loop or with a computed key",
				"the name is not visible at the call; the value of a name is followed to find the scheme word and not to find a header name; a constant holding the header name is common in Go, so search for such constants"),
			Entry(
				"an assignment to a header entry whose right-hand side is a bare name that holds a list of header values",
				"only a list written out in place or an append call is recognized at the assignment, so a bare name cannot be told from any other list"),
			Entry(
				"a credentialed HTTP or HTTPS URL held as the value of a struct field key or of a map key",
				"a credentialed URL kept in a variable or constant is recognized by a parse call or by the name it is declared under, and a field key or a map key is neither; the Medium match for such a URL is absent"),
			Entry(
				"an interpreted string literal that holds an escape sequence in the position of a literal password",
				"the construct is still matched, at its level and with its flow; only the warning about a secret written in the code is absent, because escape sequences are not decoded and a text that contains a backslash is not read as a password"),
			Entry(
				"any construction outside the forms listed: a colon pair built with a print or byte-join call, a Basic value built with a print or string-join call, a credentialed URL held in a slice element, a return value or an element of a composite value written without a key, a raw format text given directly to the encoder, an environment variable read whose name is not written as a text or that is not the whole header value, and a URL whose user name or password holds an escape",
				"each detection recognizes a fixed list of forms and does not follow values between statements; a construction outside the list has nothing to recognize, so its match is absent"),
			Entry(
				"a credentialed URL assembled from parts: a format call, a concatenation, or the string form of a URL built field by field",
				"the user information is not one text at the call, and the URL detections read a single text only; user information built by the standard function is detected where that function is called")
		];

		return entries;
	}

	/// <summary>What a per-file reading cannot join.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForWhatAPerFileReadingCannotJoin()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a credential struct or factory of the project's own type, and a header object or a request held in a struct field, a variable or a package-level value and handed to a transport or helper defined elsewhere",
				"the credential and its use are in different statements and the program is not parsed; the construct that writes the credential is detected only where that construct is itself one that is recognized"),
			Entry(
				"calls to a wrapper function, except an Authorization header written by Set, Add or an index assignment whose value is a call to a Basic helper declared once in the same file; a call to a helper in another file or package, a method call, a name declared twice, a function value held in a variable, a header literal or map value that is a call, a call to a helper that is not read as a Basic helper, and a literal given to the project's own function",
				"which argument of a call becomes which parameter of a function cannot be told; the same-file call is read only because the header write names the Authorization header and the file names the helper; the helper's definition is matched where it is written, and the Basic construct inside it is matched there too"),
			Entry(
				"a pair built in one statement and encoded in another, and a buffer or builder that assembles a user name, a colon and a password",
				"a variable is not followed from one statement to another; the only exception is a header value held in a name that the file declares once, before its use, whose scheme word is read"),
			Entry(
				"a declaration that follows a semicolon or a brace on the same line, several names declared on one line, a raw text continued over several lines, a name declared in another file, as a parameter or as a struct field, and an environment fallback written as an if statement",
				"the declaration of a name is read only when it starts a line and fits on it, which keeps the work small; reading the value of a name wrongly or not at all can miss or misplace a match but never stops the analysis of the file"),
			Entry(
				"a bare name imported without a qualifier that belongs to the project and not to the standard library",
				"import statements are not seen, so a bare name is taken to be the standard library's name, and the header name, the value or the credentialed text near it carries the evidence; such a name can be a false positive")
		];

		return entries;
	}

	/// <summary>Files that are not Go, and text that is Go but is not analyzed as code.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForFilesAndLanguagesOtherThanGo()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"shell text in an argument of a command-execution call, such as a transfer tool given a user and a password",
				"the text is inside a string, which is given only Low confidence, and reading a command inside a string needs the shell's own way of quoting text, which a Go file does not use"),
			Entry(
				"the C code of a cgo preamble, in the comment before the import of the C pseudo-package",
				"the preamble is a comment as far as can be told and its text is another language; it is skipped as a comment and not analyzed"),
			Entry(
				"assembly files ending in .s",
				"they are not Go source, so they are not attributed to Go and stay in the list of files that were not assessed"),
			Entry(
				"SQL text inside a string literal",
				"a string is not analyzed as another language, and a construct inside a string is given only Low confidence"),
			Entry(
				"a polyglot run script with no file extension whose first line is a Go comment that is also a shell command",
				"the file is not recognized as Go and is never read, because a file with no extension is recognized only by its first line and Go has no first-line signature; the files that were not assessed are named"),
			Entry(
				"files that are not Go: HTML and text templates, files read through an embed directive, module files, recorded request fixtures and data fixtures under a test-data folder",
				"the container is not a Go file, so its text is not analyzed as one; a login form in a template file is not taken as evidence of how a user signs in; search such files separately"),
			Entry(
				"credentials held in files that are not read: environment files, YAML, JSON or TOML configuration, a mounted secret, a pipeline variable",
				"the credential is in another file, not in the text analyzed, and a configuration file is where a credential usually lives; search such files separately for user names and passwords"),
			Entry(
				"struct tags and other literals read as data: a tag such as a JSON field name, a format string",
				"a struct tag is a text string and is skipped as one; its content is not analyzed")
		];

		return entries;
	}

	/// <summary>Findings that read at a level the file does not deserve, and the construct that is built and never sent.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForFilesThatReadAtTheWrongLevel()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a Basic construct that is built and never sent: a dead branch, a function that is never called, code behind a build constraint that no build selects",
				"whether a line runs depends on control flow, which cannot be known without parsing; such code may be matched although it never authenticates to RelativityOne"),
			Entry(
				"a test, mock, sample, vendored or generated file with a neutral name: a test helper in a folder with an ordinary name, a hand-written fake named like a source file, a copy of a dependency outside the vendor folder",
				"no name or folder says it is one and nothing in its text shows it, so it is given its full level and may be a false positive; the opposite case is a source file under a folder named tests, which is given Medium confidence instead of High"),
			Entry(
				"a generated file whose header is not recognized: the header inside a block comment, after the first line of code, or without the closing words",
				"a generated-file header is recognized only as a line comment before the first line of code, within the first fifty lines; a file generated another way is given its full level"),
			Entry(
				"a file that is not valid Go: a string or character literal across a line, an escaped line break, a lone quote character, an unterminated block comment",
				"Go itself rejects these texts; the text is recovered at the end of the line, and an unterminated block comment ends the reading of the file from its opening, so later constructs are treated as text inside a string and given Low confidence"),
			Entry(
				"the Basic method used to authenticate the client at an OAuth 2.0 token endpoint",
				"it is Basic on the wire and is given High confidence, and telling a token endpoint from any other request needs the destination, which cannot be seen; review such matches before acting"),
			Entry(
				"a file with any line over 10,000 characters",
				"the file is skipped whole and counted, so the loss is visible as a number; it yields no match")
		];

		return entries;
	}

	/// <summary>Proxy credentials, the evidence of how a user signs in, and the reading that reaches further than the statement of a class.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForProxiesAndTheFlowOfAFinding()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"proxy credentials: a proxy authorization header, user information given to a proxy function, the connect header of a transport, user information in a proxy URL",
				"this is Basic towards a proxy and not towards the target; it is kept out of the matches, so a URL held under a name that contains proxy is not matched even when it points at the target, and a user and password pair is not matched when the text just before or after it also names a proxy authorization header; the body of a transport or helper that writes both is given Low confidence"),
			Entry(
				"a user information call that belongs to a proxy or to a non-HTTP URL but lies beyond the stretch of text that is checked: more than 400 characters into a proxy function's argument, on a later line than a variable whose name contains proxy or than the scheme field of its literal",
				"only a limited stretch of text is checked, and the structure of a call or of a literal cannot be read; such a call is given Medium confidence and may be a false positive"),
			Entry(
				"a helper definition that is not recognized: a generic function, a function with no parameter, a parameter after the eighth, and a parameter that follows one written in a different parameter form",
				"the head of a function is read through at most eight parameters written with simple types; the Low match at the definition is absent, and the Basic construct inside the helper is still matched where it is written"),
			Entry(
				"a transport or helper whose body holds an encoded colon pair, Basic and a proxy authorization header",
				"the check that keeps proxy headers out applies to the encoder written on its own only; for the body of a transport or helper it does not, so such a definition is given Low confidence and may be a false positive"),
			Entry(
				"sign-in evidence that is not listed: an inbound request's credentials that a handler forwards, a credential taken from a command-line flag or argument, an environment read whose name is held in a constant, a custom prompt wrapper, another terminal library, a login form password field whose type is not written in lower case",
				"the set of possible prompts is open and a prompt with no stable shape cannot be listed in advance; the match is kept and the file is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in; that is an expected outcome and not a failure"),
			Entry(
				"the misreadings of the evidence of how a user signs in: a standard-input read with a credential prompt in a program run unattended, a login route that issues a token to a machine, a project's own password-reading function that wraps a different read",
				"the checks look at the shape of the code and not at intent, so a file may be taken as interactive that is not; review the flow recommended for such a file"),
			Entry(
				"a form value read as a posted password that actually arrived in the query string",
				"a form value is read from the query string as well as from the form body, and the two cannot be told apart, so a file may be taken as interactive although its password came in a URL"),
			Entry(
				"login routes registered by the routers of third-party packages, and a web front end written in Go and compiled to WebAssembly",
				"only the standard library's own route registration is taken as a login route; a router package's interface was not studied, and a compiled front end has no server at all; the match is kept and the flow is the default flow (Client Credentials unless another default is configured), because nothing shows that a person signs in"),
			Entry(
				"a credential-named assignment whose text is a raw string over several lines or is longer than 400 characters",
				"only a one-line text of at most 400 characters is read here; a miss loses a piece of sign-in evidence, so the file is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in, and never loses a match")
		];

		return entries;
	}

	/// <summary>The internal limits of the tool, each of which keeps its cost bounded.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForLimitsOfTheTool()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a file whose analysis takes too long, a call whose arguments run past about 1,500 characters, a use more than thirty line breaks after its definition, a quoted string longer than about a thousand characters, and a line longer than 10,000 characters",
				"these are limits that keep the time spent on a file under control: a file that exceeds a time limit is skipped and counted, so the loss is visible as a number, and a limit that grew without end would make the time grow without end"),
			Entry(
				"matches left out when the cap on matches is reached, listed per file or overall",
				"the cap drops matches in path order and not by confidence, and it is stated in aggregate only, so which matches were left out cannot be said; a vendored folder is also scanned and its matches count against the cap"),
			Entry(
				"confirmation that a call goes to RelativityOne and not to another server",
				"a call is seen and the destination host cannot be determined; such matches are still kept, with a note that they may not apply if the destination is not RelativityOne - review each one before acting"),
			Entry(
				"a construction whose encoder and header are written in different statements",
				"one construction is then two matches, the header and the encoder, because only a construction written in one statement is merged into one; the encoder match is expected and not an error")
		];

		return entries;
	}
}
