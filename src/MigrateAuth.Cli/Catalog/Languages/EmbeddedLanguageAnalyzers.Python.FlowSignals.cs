using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The `Python` analyzer's <see cref="CandidateSelectionAndRecommendation.FlowSignals"/>: twelve signals, eight <see cref="FlowSignalKind.InteractiveUser"/> and four <see cref="FlowSignalKind.FixedCredential"/>, each
/// declared over an expression of <see cref="PythonPatternExpressions"/> that carries the policy match timeout and is evaluated by the engine alone, once per file that holds a High or Medium construct. Every signal is
/// file-scoped, and no dialect is declared: a `Python` file is read in one dialect.
///
/// THE APPLICATION TYPE IS NOT PART OF THE CONTRACT. The engine and the template provider are never told whether a file is a command-line tool or a web server. The type reaches a finding only through the two kinds, through
/// the zone each signal reads, and through the labelled variants of the recommendation text, each of which opens with its own precondition. The eight interactive signals fall in two groups: the first four read a
/// command-line or desktop tool (group <c>cli</c>) and the next four a server-side web application (group <c>web-server</c>). The groups are named here and in the tests that exercise them, and they add no member to
/// <see cref="FlowSignal"/>.
///
/// A CREDENTIAL IS READ FROM A PERSON ONLY WHEN SOMETHING IN THE CODE ASKS A PERSON FOR IT: a hidden prompt, a prompt whose wording names a credential, a browser opened for sign-in, a loopback listener that reads the
/// authorization code, a form field, a framework's own sign-in call or its per-visitor session. A credential read from the process environment, from a configuration object, from a netrc lookup or written as a literal
/// in a credential-named assignment is fixed. NO LITERAL CREDENTIAL AS SUCH IS FLOW EVIDENCE: the in-code secret warning is a security property, and only the credential-named assignment is a signal.
/// </summary>
internal static class PythonFlowSignalDeclarations
{
	private const string _GETPASS_CALL = "GetpassCall";
	private const string _INPUT_PASSWORD_PROMPT = "InputPasswordPrompt";
	private const string _WEB_BROWSER_OPEN_CALL = "WebBrowserOpenCall";
	private const string _LOOPBACK_REDIRECT_RECEIVER = "LoopbackRedirectReceiver";
	private const string _REQUEST_FORM_CREDENTIAL_READ = "RequestFormCredentialRead";
	private const string _FASTAPI_CREDENTIAL_FORM = "FastApiCredentialForm";
	private const string _DJANGO_AUTHENTICATE_OR_LOGIN = "DjangoAuthenticateOrLogin";
	private const string _FRAMEWORK_SESSION_USE = "FrameworkSessionUse";
	private const string _ENVIRONMENT_CREDENTIAL_READ = "EnvironmentCredentialRead";
	private const string _CONFIG_FILE_CREDENTIAL_READ = "ConfigFileCredentialRead";
	private const string _NETRC_CREDENTIAL_READ = "NetrcCredentialRead";
	private const string _CREDENTIAL_NAMED_LITERAL_ASSIGNMENT = "CredentialNamedLiteralAssignment";

	internal static ImmutableArray<FlowSignal> Build()
	{
		return [.. InteractiveUserSignals(), .. FixedCredentialSignals()];
	}

	/// <summary>The eight interactive signals: four for a command-line or desktop tool, then four for a server-side web application.</summary>
	private static ImmutableArray<FlowSignal> InteractiveUserSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(_GETPASS_CALL, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, PythonPatternExpressions.GetpassCall()),
			Signal(_INPUT_PASSWORD_PROMPT, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, PythonPatternExpressions.InputPasswordPrompt()),
			Signal(_WEB_BROWSER_OPEN_CALL, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, PythonPatternExpressions.WebBrowserOpenCall()),
			Signal(_LOOPBACK_REDIRECT_RECEIVER, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, PythonPatternExpressions.LoopbackRedirectReceiver()),
			Signal(_REQUEST_FORM_CREDENTIAL_READ, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, PythonPatternExpressions.RequestFormCredentialRead()),
			Signal(_FASTAPI_CREDENTIAL_FORM, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, PythonPatternExpressions.FastApiCredentialForm()),
			Signal(_DJANGO_AUTHENTICATE_OR_LOGIN, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, PythonPatternExpressions.DjangoAuthenticateOrLogin()),
			Signal(_FRAMEWORK_SESSION_USE, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, PythonPatternExpressions.FrameworkSessionUse())
		];

		return signals;
	}

	/// <summary>The four fixed-credential signals: a credential taken from the environment, a configuration object, a netrc lookup or a credential-named literal.</summary>
	private static ImmutableArray<FlowSignal> FixedCredentialSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(_ENVIRONMENT_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, PythonPatternExpressions.EnvironmentCredentialRead()),
			Signal(_CONFIG_FILE_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, PythonPatternExpressions.ConfigFileCredentialRead()),
			Signal(_NETRC_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, PythonPatternExpressions.NetrcCredentialRead()),
			Signal(_CREDENTIAL_NAMED_LITERAL_ASSIGNMENT, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, PythonPatternExpressions.CredentialNamedLiteralAssignment())
		];

		return signals;
	}

	private static FlowSignal Signal(string name, FlowSignalKind kind, FlowSignalZone zone, Regex expression)
	{
		return new FlowSignal
		{
			Name = name,
			Kind = kind,
			Zone = zone,
			Expression = expression
		};
	}
}
