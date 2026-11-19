using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The declared unsupported coverage of the `VBNet` analyzer, the statement the client's report carries when the language is enabled. A separate
/// <c>internal</c> holder because <c>VBNetAnalyzer</c> is <c>file</c>-scoped. Written for the client reading the report: each construct names in plain
/// words what the tool does not detect or cannot tell, and each reason says why and, where useful, what to do about it. No internal design term, class
/// identifier or section reference belongs in either field. The classes supported at Low only are a different statement (they ARE recognized) and
/// do not appear here.
///
/// Grouped in five helpers, each under the method-length limit.
/// </summary>
internal static class VBNetDeclaredCoverage
{
	internal static FrozenSet<DeclaredUnsupportedCoverage> Build()
	{
		IEnumerable<DeclaredUnsupportedCoverage> entries = ForFilesAndDialects()
			.Concat(ForScopeAndDirection())
			.Concat(ForTheMaskOfTheText())
			.Concat(ForCredentialForms())
			.Concat(ForPairsHeadersAndCalls());

		return entries.ToFrozenSet();
	}

	private static DeclaredUnsupportedCoverage Entry(string construct, string reason)
	{
		return new DeclaredUnsupportedCoverage { Construct = construct, Reason = reason };
	}

	/// <summary>Which files and which dialects are read at all.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForFilesAndDialects()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"Visual Basic 6, VBA, VBScript and other Basic dialects: files ending in .vbs, .bas, .cls, .frm or .vbhtml, and classic ASP",
				"these are a different language from Visual Basic .NET; such files are not attributed to it and stay in the list of files that were not assessed"),
			Entry(
				"Visual Basic .NET written inside markup: the server-side script blocks and inline code of .aspx, .ascx and .master files",
				"the markup is not scanned; the code-behind file of the same page, which ends in .aspx.vb, is"),
			Entry(
				"credentials stored in configuration files such as app.config, web.config, .settings files, appsettings.json or .resx resources",
				"these files belong to none of the languages that are read, and a configuration file is where a credential usually lives; search them separately for user names and passwords"),
			Entry(
				"a source file saved in a single-byte encoding other than Windows-1252, or one whose only line terminator is the Unicode line or paragraph separator",
				"a single-byte file is read as Windows-1252, and the files that could not be read with confidence are named; line ends other than carriage return and line feed are not recognized")
		];

		return entries;
	}

	/// <summary>What a per-file reading cannot join, which way a service faces, and what the tool limits.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForScopeAndDirection()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"credentials, a scheme or evidence of how a user signs in that are supplied from another file: a partial class or module in another file, a code-behind page whose markup holds the login control, project-level imports, a loaded configuration file",
				"no per-file analysis joins a definition in one file to its use in another; a file whose flow evidence is elsewhere is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in; that is an expected outcome and not a failure - where the code acts for a signed-in user, use the Authorization Code flow"),
			Entry(
				"a scheme or credential name set in a way that is not read as a constant: a non-literal reassignment, several names declared in one statement, a static local, a constant in another file or project, a resource or application setting, or a constant reached through an import alias",
				"the scheme cannot be resolved, so the match is kept at a lower level instead of being dropped; a scheme that is in fact not Basic and cannot be resolved is still given that level - review such matches"),
			Entry(
				"a scheme argument written as a qualified constant, such as a constant held in another module and named with its module",
				"the scheme is read only when it is a literal or a bare name declared in the same file; with a qualified name the construct is not matched as written"),
			Entry(
				"a service that checks incoming Basic Authentication from its own callers, rather than sending it out to another server",
				"which way a call faces cannot be determined from a single file; code that writes a header is read, which excludes the commonest inbound form, but a service that decodes the header itself may still be matched"),
			Entry(
				"confirmation that a matched Relativity SDK type comes from the Relativity SDK, that a call goes to RelativityOne and not to another server, or that a credential is consumed by an HTTP client",
				"the type name is matched as written and neither the assembly nor the destination address can be seen; such matches are still kept, with a note that they may not apply if the destination is not RelativityOne - review each one before acting"),
			Entry(
				"whether a credential, header or value is ever sent: one that is built and only logged, built on a branch that never runs, or sent to an address that is not an HTTP server",
				"reachability, transmission and protocol need control-flow and data-flow analysis that is not performed; such code may be matched although it never authenticates to RelativityOne"),
			Entry(
				"code inside a disabled conditional-compilation branch, such as a block under a false condition",
				"a disabled branch is read as code: a construct in it is matched as if it were live, and a stray double quote in it can end the analysis of the rest of the file")
		];

		return entries;
	}

	/// <summary>What the lexical reading of the text cannot follow.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForTheMaskOfTheText()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"XML literals written in expression position, and any code inside them",
				"handling them means tracking element context, which is parsing; an apostrophe in their prose is read as a comment and an odd double quote can make the rest of the file be read as text, so matches after it are given the lowest level"),
			Entry(
				"a string delimited by a mixed pair of typographic quotes, a typographic interpolated string, the fullwidth quotation mark, the fullwidth apostrophe, and an authorization header key written between typographic quotes",
				"the compiler accepts these but the reading of the text can invert from the opening character to the next quote of its kind, so matches after it may be given the wrong level"),
			Entry(
				"the folding of letter case for non-ASCII letters in names and in the words that are searched for",
				"the words searched for are ASCII; a name written with other letters that differs from them only in case is not matched")
		];

		return entries;
	}

	/// <summary>How a credential or a client is created in forms the tool does not read.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForCredentialForms()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a Relativity SDK credential created by an object initializer or by assigning its properties after creation",
				"whether the type can be created without arguments and its properties set was not established, so the construct is not read"),
			Entry(
				"a credential or a client created by reflection or from a computed type: Activator.CreateInstance with a type, a generic Activator.CreateInstance, or a type name read from configuration",
				"the type name does not appear as a constructor call in the text"),
			Entry(
				"a type, client, authenticator, header type or SDK entry point that the file renames with an import alias of the type, or a user type with the same simple name as an SDK type",
				"following a rename is a symbol resolution that is not done; an alias of a namespace with the type written in full is read, and a user type with the same name as an SDK type is read as the SDK type unless the same file declares it close to the use, which can be a false positive for a type declared in another file or farther away"),
			Entry(
				"a system token returned by a call whose own argument holds a further call, handed as the password of the SDK entry point",
				"a token returned by a call whose arguments hold no call of their own is recognized; with a deeper call it is matched as a user name and password, a false positive to dismiss on review"),
			Entry(
				"arguments written out of order with names, such as a password given by name before the user name, and a named argument that ends the call without a closing parenthesis",
				"named arguments are read in order only; out of order the two halves would be swapped and the credential would be misread"),
			Entry(
				"a collection initializer that writes a header or a credential-cache entry, such as a header collection or a credential cache created with a list of entries",
				"there is no call to anchor the match on"),
			Entry(
				"a credential assigned from a variable whose receiver type is not written in the lines before it, a With block on a variable whose type is declared elsewhere, and a credential held in a variable and assigned later",
				"the credential type or the receiver type is not in the text; a credential built with its arguments is read where it is built"),
			Entry(
				"a credential built across statements: an empty credential created first and its user name and password set afterwards, then bound to a client",
				"no pattern reads the pair split across statements"),
			Entry(
				"a credential built with three arguments, the third a domain",
				"a domain marks integrated Windows authentication, which Basic Authentication does not carry; a Basic exchange that also sends a domain is missed"),
			Entry(
				"file-transfer calls of My.Computer.Network with overloads that are not listed in the documentation, an argument list that spans more than the reading window, and the network class of the runtime library instantiated directly",
				"the credential arguments are read at their documented positions only"),
			Entry(
				"late-bound COM requests that remain unread: a call to Open whose method is held in a variable, and an Open with fewer than five arguments",
				"the call shape is not in the text")
		];

		return entries;
	}

	/// <summary>How a credential pair or a header is put together in forms the tool does not follow, and what the text cannot show.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForPairsHeadersAndCalls()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a user name and password joined by a colon and encoded by the project's own Base64 helper, by an encoder called without its type name, or built up across several statements",
				"the standard library's conversion is recognized by name only; widening it to any call with Base64 in its name would read every conversion as the end of a credential pair. A header value that names the scheme, written next to the conversion, is still matched"),
			Entry(
				"a half, first or second, whose last member is `Length`, `Count`, `Size` or `Port`, in the spellings of the pair where the check that declines it is not applied",
				"the pair is matched as an encoded user name and password, so a name and a size can be matched as a credential pair"),
			Entry(
				"a pair with one empty half in a spelling the empty-half class does not read, such as a variable assigned an empty literal more than ten statements above the encoder call",
				"the empty half is read only in the forms the class lists, so a token encoded as the password or the user name of a Basic value in another spelling is not matched by that class; search for the encoder call and review it by hand"),
			Entry(
				"a user name and password joined by a separator held in a named constant",
				"the colon is not in the text of the pair; a colon written as a literal is read"),
			Entry(
				"a user name and password written directly into a URL, such as an address of the form user, colon, password, at sign, host, including through a URI builder",
				"this form is not detected. This is a real exposure independent of the OAuth2 migration and should be searched for and removed whatever authentication scheme is in use"),
			Entry(
				"Basic Authentication performed by a native program the code starts, such as a command-line transfer tool given a user and password or an Authorization header argument",
				"an argument to an external command is text and not a Visual Basic construct"),
			Entry(
				"an Authorization header written with a value held in a variable or a parameter whose name carries no credential word, and a value produced by the code's own function whose name carries no scheme word",
				"neither the scheme nor the name gives evidence of what the value is, and reading a neutral name would match every write of an opaque value; a value whose name carries a credential word or a scheme word, or that is assigned a scheme-bearing value in the lines before it, is read"),
			Entry(
				"a fixed Authorization value kept in a constant, whose declaration is more than a few lines from the write, and written by name",
				"the write carries only a neutral name; a value written in full in the write itself is read"),
			Entry(
				"an enumeration member reached through an import of the enumeration type without its name, and a header-enumeration value built from a constant that carries the scheme and the space and joined directly",
				"the member name carries no type to anchor on, and reading a bare concatenation would read every two-part value written into an Authorization header"),
			Entry(
				"a user name and password joined by a colon and encoded that are not a credential, such as a user name and a token expiry",
				"such values cannot always be told apart from an encoded user name and password, so a value with this shape may occasionally be matched and should be dismissed on review"),
			Entry(
				"a user name and password joined by a colon and encoded, where the second half is held under a name that carries no credential word, such as `pin` or `passphrase`",
				"the second half of an encoded pair is read only when its name carries a credential word, so that a pair of two ordinary values is not matched; a credential kept under a neutral name is not matched as an encoded pair, and a header value that names the scheme next to it is still read"),
			Entry(
				"an Authorization header written with a value held in a variable whose name carries `pass`, `pwd` or `apikey` in lower case followed directly by a digit, such as `pass2`",
				"the name is not read as carrying a credential word, while the same word with a capital first letter, such as `Pass2`, is read"),
			Entry(
				"flow evidence that is not listed: a login control declared in markup, a password control declared in XAML, a custom credential prompt, a vault command other than the ones that are known, and an instance-setting key that reaches the call through a helper's parameter",
				"only the signals with a stable shape in the code are read; a file whose evidence is of another kind is given the default flow (Client Credentials unless another default is configured), because nothing in it shows that a person signs in")
		];

		return entries;
	}
}
