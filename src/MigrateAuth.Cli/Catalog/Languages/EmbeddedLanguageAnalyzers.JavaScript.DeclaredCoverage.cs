using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The declared unsupported coverage of the `JavaScript` analyzer, the statement the client's report carries when the language is enabled. A separate
/// <c>internal</c> holder because <c>JavaScriptAnalyzer</c> is <c>file</c>-scoped. Written for the client reading the report: each construct names in plain words
/// what the tool does not detect or cannot tell, and each reason says why and, where useful, what to do about it. No internal design term, class identifier
/// or section reference belongs in either field. The classes supported at Low only are a different statement (they ARE recognized) and do not appear here.
///
/// The thirty-seven entries of the catalog's declared unsupported coverage and its four known limits, in the catalog's order, which carries no meaning in the report. Only what is
/// impossible by design, open by nature, a declared cost or an internal limit is declared. Grouped in helpers, each under the method-length limit.
/// </summary>
internal static class JavaScriptDeclaredCoverage
{
	internal static FrozenSet<DeclaredUnsupportedCoverage> Build()
	{
		IEnumerable<DeclaredUnsupportedCoverage> entries = ForRequestsWrittenByHand()
			.Concat(ForClientsAndWrappers())
			.Concat(ForHeadersAndNames())
			.Concat(ForCostsOfTheReading())
			.Concat(ForFilesAndFolders())
			.Concat(ForTheFlowOfAFinding())
			.Concat(ForUrlsAndTheSymbolTable())
			.Concat(ForLimitsOfTheTool());

		return entries.ToFrozenSet();
	}

	private static DeclaredUnsupportedCoverage Entry(string construct, string reason)
	{
		return new DeclaredUnsupportedCoverage { Construct = construct, Reason = reason };
	}

	/// <summary>Requests the tool cannot see as a call it knows.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForRequestsWrittenByHand()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a request written by hand to a socket (net, tls or raw HTTP/2) with a literal Authorization: Basic line in the text",
				"the transports are an open set, and anchoring on the header text alone would make every file that mentions it a candidate; search such files separately for the literal header"),
			Entry(
				"an options object, authentication object or header object held in a variable, a module or a default export and handed to a client elsewhere, and an alias of a client or of the http module",
				"the call and the object are in different statements and the tool reads text, not the program; the object is read only where a header carrier or an authentication object with a user and a password sits in the same text"),
			Entry(
				"a 'Basic ...' literal held in a variable with no header write in the same statement, and a header value held in an identifier that cannot be resolved",
				"a literal in a variable has no call to anchor on and its use is not in the statement; a name the file does not declare exactly once before its use is not resolved, and a value that resolves to another scheme is the migrated form"),
			Entry(
				"header-setting methods and hooks of third-party clients whose documentation was not read for them, and a header built by a function call",
				"an option or method name is read only after its documentation and version range were checked, so an unchecked name is left out rather than guessed"),
			Entry(
				"a credentialed URL given directly to a jQuery or other XMLHttpRequest wrapper",
				"such a request sends the credential only after the server challenges for it, which is not how the tool's target behaves; a URL held in a variable is read as a held URL"),
			Entry(
				"calls to a wrapper function are not followed; the construct inside the wrapper is reported where the wrapper is defined, and a literal given to an aliased callee is not read",
				"following a call needs the mapping of a named parameter to its argument, which an expression cannot make; the construct is reported once, at the definition, and the call sites are not")
		];

		return entries;
	}

	/// <summary>Clients, wrappers and runtimes the tool does not follow.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForClientsAndWrappers()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"computed property names, such as headers[name] or an object key written as [key], where the name is not a literal",
				"the name is not text at the call, so neither the header nor the option can be read"),
			Entry(
				"generated API clients",
				"the call surface is hidden behind generated methods and no list can name every one"),
			Entry(
				"variable use across statements: a credential assigned in one statement and sent in another is not joined",
				"a variable has no marker in this language and the tool does not track it; the only cross-statement reading is a header value held in a name declared once before its use in the same file"),
			Entry(
				"shell text inside an executed string, such as a curl command given to child_process",
				"the command is text inside a string and the tool does not read a string as a script; search such calls separately"),
			Entry(
				"a project's own wrapper beyond what one statement shows",
				"a wrapper's behaviour is not in the call to it and wrapper calls are not followed"),
			Entry(
				"credentials assembled across modules",
				"no analysis joins a definition in one file to its use in another"),
			Entry(
				"runtimes other than Node.js, such as Deno and Bun",
				"the runtimes are an open set; their environment reads, built-in clients and shell helpers are not listed, although code written on the standard fetch and XMLHttpRequest is read as any other")
		];

		return entries;
	}

	/// <summary>Headers and encoders the tool reads only inside a bound.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForHeadersAndNames()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"proxy credentials: the Proxy-Authorization header, the credential member of a proxy option and user information in a proxy URL",
				"this is Basic towards a proxy and not towards the target, which is not the migration this tool serves; such text is kept out of the findings"),
			Entry(
				"a quoted carrier key, such as 'headers' or \"headers\" written in quotes",
				"the key is read as text inside a string and a construct anchored there is routed to manual review at Low; the header name and scheme inside it are still read"),
			Entry(
				"an encoder whose Basic or Authorization word is more than 300 characters from it",
				"the use requirement that keeps cache keys and host and port pairs out of the findings reads 300 characters on each side; the encoder is read at the header if at all"),
			Entry(
				"headers given to undici as a flat array of alternating names and values",
				"the array has no key per header, so a name and its value are not read as a pair"),
			Entry(
				"a header-writing construct built and never sent, such as a dead branch or a function that is never called",
				"whether a line runs depends on control flow, which the tool does not read; such code may be reported although it never authenticates to RelativityOne"),
			Entry(
				"a test, mock, sample or vendored file with a neutral name, and the .min.mjs, .min.cjs, .bundle.mjs and .chunk.mjs forms of a generated name",
				"no name or folder says the file is one and no text shows it, so it is reported at its full level and may be a false positive; review such findings")
		];

		return entries;
	}

	/// <summary>The reading costs of the lexical profile.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForCostsOfTheReading()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"HTML-like comments (the opener <!-- and the closer -->) in a non-module script",
				"these are legal code in other positions (a comparison followed by a decrement, a countdown loop), so the tool does not read them as comments; code on such a line is read as code"),
			Entry(
				"the Unicode line separator and paragraph separator, which JavaScript treats as line ends",
				"the tool's line ends at the line feed; the reading is damaged around such a character, and no construct fails to be read"),
			Entry(
				"a regular expression written after a trailing line comment or after a block comment",
				"the tool decides whether a slash opens a regular expression from the text before it, and not from where comments are; the slash is read as a division and the expression as code"),
			Entry(
				"JSX beyond the two rules for closing tags: a backtick or a block-comment opener in element text abandons the rest of the file, and a quote in element text damages only its own line",
				"element text is not modelled, because modelling it means parsing; the file is reported as not fully read and a construct after the damage is not found"),
			Entry(
				"a regular expression written after a closing parenthesis (if (c) /re/.test(s)) or as the first token after a closing brace",
				"a slash after those characters is read as a division, which keeps a self-closing tag after an expression hole out of the regular-expression reading; that rare regular expression is read as code"),
			Entry(
				"a template literal nested more than eight deep, and any unterminated literal or block comment",
				"the depth is bounded and an unterminated literal cannot be read past; the file is reported as not fully read and a construct after the damage is not found")
		];

		return entries;
	}

	/// <summary>Which files are read at all.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForFilesAndFolders()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"the upper-case folders Dist and BUILD",
				"the walk skips only the lower-case dist and build, so these are read and no name marks their content as generated; a generated tree under them is reported at its full level"),
			Entry(
				"a .js, .mjs, .cjs or .jsx file with any line over 10,000 characters, which includes a minified single-line bundle",
				"this is an internal limit that keeps the cost bounded: such a file is skipped whole and counted in the report, so the loss is visible as a number; a shorter minified file is read"),
			Entry(
				"TypeScript files: .ts, .tsx, .mts and .cts",
				"they are counted and not assessed in this release, because TypeScript is read by a later analyzer"),
			Entry(
				"JavaScript embedded in other files: HTML script elements, single-file components of frameworks and templating files, and extensions outside the table",
				"the containing file is not a JavaScript file, so its text is not read as one; search such files separately"),
			Entry(
				"a file under a folder the repository walk never enters: bin, obj, .git, .vs, node_modules, dist, build and packages, including a node script under bin",
				"these folders are excluded before any file is read, so no rule can reach the file; this is a limit of the walk and not of a form of the construct"),
			Entry(
				"where a credential lives: .env files, JSON or YAML configuration and .npmrc, which the tool does not read",
				"the credential is in another file, not in the text analyzed, and a configuration file is where a credential usually lives; search such files separately for user names and passwords")
		];

		return entries;
	}

	/// <summary>The flow evidence a per-file reading cannot join, or does not list.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForTheFlowOfAFinding()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a password typed in another module, so that the file that sends it holds no sign-in evidence",
				"sign-in evidence is read per file; a file whose evidence is elsewhere is assigned the default flow, which is an expected outcome and not a failure - where the code acts for a signed-in user, use the Authorization Code flow"),
			Entry(
				"sign-in evidence the tool does not list: a custom prompt wrapper, a credential read through a helper, and markers of a server alone",
				"the set of possible prompts is open and a prompt with no stable shape cannot be listed in advance; only the signals with a stable shape in the code are read, and a file whose evidence is of another kind is assigned the default flow"),
			Entry(
				"a flow misread: server markup in a plain .js or .mjs file, a node script whose only evidence is prompt wording, and a locally defined prompt function beyond the reading window",
				"the reading separates a file that names itself a node script from a browser file, and a window is bounded; such a file may be assigned the interactive flow although the code acts as itself")
		];

		return entries;
	}

	/// <summary>URLs assembled from parts, and the symbol table's residuals.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForUrlsAndTheSymbolTable()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a credentialed URL assembled from parts: a concatenation, a URL parsed from a string or a template with holes",
				"the user information is not one literal at the call, and only a literal is read"),
			Entry(
				"a constant declaration that follows a brace or a semicolon on the same line, a literal continued over lines by a backslash, and a template with a hole",
				"the declaration is read one line at a time from the start of the line, which keeps the cost linear; a name with one counted and one uncounted declaration can be resolved to the wrong value, which is a missed or misplaced finding"),
			Entry(
				"a URL object whose scheme is not visible where its user name and password are assigned, such as one built from a parameter",
				"the setters are read on a name bound to a URL built from a literal; when the scheme is in a parameter a non-HTTP address cannot be told, so such an assignment is reported at Medium")
		];

		return entries;
	}

	/// <summary>The internal limits of the tool, each of which keeps its cost bounded.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForLimitsOfTheTool()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a file whose analysis takes too long, a call longer than the reading window of 1,500 units, a use of a name more than thirty lines after its definition, and a quoted string longer than the window allows",
				"these are internal limits that keep the cost bounded: a file that trips a time limit is skipped and counted in the report, so the loss is visible as a number, and a window that grew without bound would make the cost grow without bound"),
			Entry(
				"which findings were left out when the findings cap was reached, per file or in all",
				"the cap is reported in aggregate only, so the report cannot say which findings were left out"),
			Entry(
				"confirmation that a call goes to RelativityOne and not to another server",
				"the tool sees a call and cannot determine the destination host; such findings are still reported, with a note that they may not apply if the destination is not RelativityOne - review each one before acting"),
			Entry(
				"one construction split across statements: an encoder in one statement and the header that uses it in another, and the two assignments of a URL object's user name and password",
				"the tool merges only a construct nested in another, so a split construction is reported as two findings; the encoder finding is the expected one of the pair")
		];

		return entries;
	}
}
