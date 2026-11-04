using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// SAME-FILE WRAPPERS, FOLLOWED BY THE ENGINE FROM THE DATA A LANGUAGE DECLARED
/// (<see cref="WrapperTracking"/>). It runs once per file, after every rule produced its classified matches
/// and BEFORE subsumption, and only where the language declares wrapper tracking.
///
/// A construct whose match lies inside a function's body and contains a positional reference (or the
/// alias of one) is PARAMETERIZED: its credential is whatever the caller passes. It is removed from the
/// definition and replaced by one construct per call of that function, anywhere in the file, that passes a
/// credential-bearing argument at a parameter the construct uses (<c>@</c> and <c>*</c> stand for any argument).
/// The replacement carries the inner construct's pattern class and level and low-confidence reasons, is anchored
/// at the call's command word, spans the call, and is never a literal-credential signal (the call is not wholly
/// literal evidence). Identical (class, call) pairs are one construct. A wrapper that is never called with a
/// credential-bearing argument yields nothing, and is not read at its definition either.
///
/// BODIES. The body runs from the declared opening brace to the matching closing one, counting braces ONLY at
/// trustworthy code positions, so an expansion's braces balance on their own and a brace in a comment or a string
/// is never counted. A body not closed within <see cref="WrapperTracking.MaxBodyLines"/> lines is ABANDONED:
/// it is no wrapper, and its constructs keep the reading they had at the definition. When the same function name is
/// defined more than once every definition is read, and the (class, call) dedup makes the union one set.
///
/// LIMITS, stated in <see cref="WrapperTracking"/>: a nested call from one wrapper into another
/// is not followed (the argument is a parameter there, and a parameter is never credential-bearing); a wrapper
/// defined in another file is not followed; at most <see cref="WrapperTracking.MaxCalls"/> calls are examined.
///
/// EVERY EXPRESSION GOES THROUGH <see cref="TimedRegexEvaluation"/> and the bound is polled between matches; a
/// trip is reported as not completed and the caller stops the file, exactly as a rule's own trip does.
/// </summary>
internal static class WrapperResolution
{
	internal static bool TryApply(
		FileRecognitionContext context,
		FileSymbolTable symbols,
		VariableReferenceIndex references,
		MatchBoundWatch matchBoundWatch,
		List<ClassifiedMatch> classifiedMatches)
	{
		WrapperTracking? tracking = context.Analyzer.CandidateSelectionAndRecommendation.WrapperTracking;
		bool completed = true;

		if (tracking is not null && classifiedMatches.Count > 0)
		{
			completed = new WrapperScan(tracking, context, symbols, references, matchBoundWatch)
				.TryReplaceParameterizedConstructs(classifiedMatches);
		}

		return completed;
	}

	/// <summary>One function whose body was closed: its name, its definition's match span and its body's extent.</summary>
	private sealed class FunctionBody
	{
		internal required string Name { get; init; }

		internal required int DefinitionStart { get; init; }

		internal required int DefinitionEnd { get; init; }

		/// <summary>The offset after the opening brace.</summary>
		internal required int BodyStart { get; init; }

		/// <summary>The offset of the closing brace.</summary>
		internal required int BodyEnd { get; init; }

		/// <summary>The aliases of a positional parameter this body declares; read on first need.</summary>
		internal List<ParameterAlias>? Aliases { get; set; }
	}

	private readonly record struct ParameterAlias(string Name, string Index);

	/// <summary>What the replacement of one file's parameterized constructs reads and fills, carried together.</summary>
	private sealed record ReplacementState(
		List<ClassifiedMatch> ClassifiedMatches,
		List<ParameterizedConstruct> Parameterized,
		HashSet<(string PatternClass, int Anchor)> Seen,
		List<ClassifiedMatch> Replaced);

	/// <summary>A construct that uses a parameter of the function it sits in, and which parameters.</summary>
	private readonly record struct ParameterizedConstruct(int MatchPosition, FunctionBody Body, HashSet<string> ParameterIndexes);

	/// <summary>One call of a wrapper: where it is, where its command word is, and its arguments' words.</summary>
	private sealed class CallSite
	{
		internal required string Name { get; init; }

		internal required int MatchIndex { get; init; }

		internal required int MatchLength { get; init; }

		internal required int NameIndex { get; init; }

		internal required string Arguments { get; init; }
	}

	private sealed class WrapperScan
	{
		private const char _OPENING_BRACE = '{';
		private const char _CLOSING_BRACE = '}';
		private const char _LINE_FEED = '\n';
		private const char _DOLLAR = '$';
		private const char _DOUBLE_QUOTE = '"';
		private const string _ANY_ARGUMENT_FIRST = "@";
		private const string _ANY_ARGUMENT_SECOND = "*";
		private const char _FIRST_POSITIONAL_DIGIT = '1';
		private const char _LAST_POSITIONAL_DIGIT = '9';
		private const char _OPENING_BRACKET = '[';
		private const char _CLOSING_BRACKET = ']';
		private const string _SPECIAL_PARAMETERS = "@*#?!$-";

		private readonly WrapperTracking _tracking;
		private readonly FileRecognitionContext _context;
		private readonly FileSymbolTable _symbols;
		private readonly VariableReferenceIndex _references;
		private readonly MatchBoundWatch _watch;
		private readonly string _text;

		internal WrapperScan(
			WrapperTracking tracking,
			FileRecognitionContext context,
			FileSymbolTable symbols,
			VariableReferenceIndex references,
			MatchBoundWatch watch)
		{
			_tracking = tracking;
			_context = context;
			_symbols = symbols;
			_references = references;
			_watch = watch;
			_text = context.NormalizedText;
		}

		internal bool TryReplaceParameterizedConstructs(List<ClassifiedMatch> classifiedMatches)
		{
			bool completed = TryReadBodies(out List<FunctionBody> bodies);
			List<ParameterizedConstruct> parameterized = [];

			if (completed && bodies.Count > 0)
			{
				completed = TryFindParameterizedConstructs(classifiedMatches, bodies, parameterized);
			}

			if (completed && parameterized.Count > 0)
			{
				completed = TryFindCalls(parameterized, bodies, out List<CallSite> calls)
					&& TryReplace(classifiedMatches, parameterized, calls);
			}

			return completed && !_watch.HasBeenExceeded();
		}

		// ---- function bodies ----

		private bool TryReadBodies(out List<FunctionBody> bodies)
		{
			bodies = [];

			bool completed = !_watch.HasBeenExceeded();
			Match match = completed ? TimedRegexEvaluation.Match(_watch, _tracking.FunctionDefinitionExpression, _text) : Match.Empty;

			while (completed && match.Success)
			{
				AddBodyIfClosed(match, bodies);
				completed = !_watch.HasBeenExceeded();
				match = completed ? TimedRegexEvaluation.NextMatch(_watch, match) : Match.Empty;
			}

			return completed;
		}

		private void AddBodyIfClosed(Match definition, List<FunctionBody> bodies)
		{
			Group name = definition.Groups[WrapperTracking.NAME_GROUP_NAME];
			Group open = definition.Groups[WrapperTracking.OPEN_GROUP_NAME];
			bool opensABody = name.Success
				&& open.Success
				&& open.Index < _text.Length
				&& _text[open.Index] == _OPENING_BRACE
				&& FileDemotionSignals.IsTrustworthyCodeAt(_context.Mask, open.Index);
			int close = opensABody ? FindClosingBrace(open.Index) : -1;

			if (close >= 0)
			{
				bodies.Add(new FunctionBody
				{
					Name = name.Value,
					DefinitionStart = definition.Index,
					DefinitionEnd = definition.Index + definition.Length,
					BodyStart = open.Index + 1,
					BodyEnd = close
				});
			}
		}

		/// <summary>
		/// The offset of the brace that closes the one at <paramref name="openIndex"/>, or -1 where it is abandoned. A closing
		/// brace found after exactly <see cref="WrapperTracking.MaxBodyLines"/> line feeds is accepted; one that needs more is not,
		/// so a body may span MaxBodyLines + 1 physical lines.
		/// </summary>
		private int FindClosingBrace(int openIndex)
		{
			int depth = 0;
			int lines = 0;
			int close = -1;
			int position = openIndex;

			while (close < 0 && position < _text.Length && lines <= _tracking.MaxBodyLines)
			{
				char character = _text[position];

				if (character == _LINE_FEED)
				{
					lines++;
				}
				else if (character is _OPENING_BRACE or _CLOSING_BRACE
					&& FileDemotionSignals.IsTrustworthyCodeAt(_context.Mask, position))
				{
					depth += character == _OPENING_BRACE ? 1 : -1;
					close = depth == 0 ? position : -1;
				}

				position++;
			}

			return close;
		}

		// ---- parameterized constructs ----

		private bool TryFindParameterizedConstructs(
			List<ClassifiedMatch> classifiedMatches,
			List<FunctionBody> bodies,
			List<ParameterizedConstruct> parameterized)
		{
			bool completed = true;

			for (int position = 0; completed && position < classifiedMatches.Count; position++)
			{
				ClassifiedMatch construct = classifiedMatches[position];
				FunctionBody? body = InnermostBodyContaining(bodies, construct.MatchIndex, construct.MatchIndex + construct.MatchLength);

				if (body is not null)
				{
					completed = TryReadParameterIndexes(construct, body, out HashSet<string> indexes);

					if (completed && indexes.Count > 0)
					{
						parameterized.Add(new ParameterizedConstruct(position, body, indexes));
					}
				}
			}

			return completed;
		}

		private static FunctionBody? InnermostBodyContaining(List<FunctionBody> bodies, int start, int end)
		{
			FunctionBody? innermost = null;

			foreach (FunctionBody body in bodies)
			{
				bool contains = start >= body.BodyStart && end <= body.BodyEnd;

				if (contains && (innermost is null || body.BodyStart > innermost.BodyStart))
				{
					innermost = body;
				}
			}

			return innermost;
		}

		private bool TryReadParameterIndexes(ClassifiedMatch construct, FunctionBody body, out HashSet<string> indexes)
		{
			indexes = new HashSet<string>(StringComparer.Ordinal);

			int spanEnd = construct.MatchIndex + construct.MatchLength;
			bool completed = TryReadPositionalReferences(construct, indexes) && TryReadAliases(body);

			if (completed)
			{
				foreach (ParameterAlias alias in body.Aliases!)
				{
					if (_references.LiveReferencesIn(alias.Name, construct.MatchIndex, spanEnd).Count > 0)
					{
						indexes.Add(alias.Index);
					}
				}
			}

			return completed;
		}

		private bool TryReadPositionalReferences(ClassifiedMatch construct, HashSet<string> indexes)
		{
			string span = _text.Substring(construct.MatchIndex, construct.MatchLength);
			bool completed = !_watch.HasBeenExceeded();
			Match match = completed ? TimedRegexEvaluation.Match(_watch, _tracking.PositionalReferenceExpression, span) : Match.Empty;

			while (completed && match.Success)
			{
				Group index = match.Groups[WrapperTracking.INDEX_GROUP_NAME];

				if (index.Success
					&& FileDemotionSignals.IsTrustworthyOutsideACommentAt(_context.Mask, construct.MatchIndex + match.Index))
				{
					indexes.Add(index.Value);
				}

				completed = !_watch.HasBeenExceeded();
				match = completed ? TimedRegexEvaluation.NextMatch(_watch, match) : Match.Empty;
			}

			return completed;
		}

		private bool TryReadAliases(FunctionBody body)
		{
			bool completed = true;

			if (body.Aliases is null)
			{
				List<ParameterAlias> aliases = [];
				string bodyText = _text[body.BodyStart..body.BodyEnd];

				completed = !_watch.HasBeenExceeded();
				Match match = completed ? TimedRegexEvaluation.Match(_watch, _tracking.ParameterAliasExpression, bodyText) : Match.Empty;

				while (completed && match.Success)
				{
					AddAlias(match, body.BodyStart, aliases);
					completed = !_watch.HasBeenExceeded();
					match = completed ? TimedRegexEvaluation.NextMatch(_watch, match) : Match.Empty;
				}

				body.Aliases = completed ? aliases : null;
			}

			return completed;
		}

		private void AddAlias(Match match, int bodyStart, List<ParameterAlias> aliases)
		{
			Group alias = match.Groups[WrapperTracking.ALIAS_GROUP_NAME];
			Group index = match.Groups[WrapperTracking.INDEX_GROUP_NAME];

			if (alias.Success
				&& alias.Length > 0
				&& index.Success
				&& FileDemotionSignals.IsTrustworthyCodeAt(_context.Mask, bodyStart + match.Index))
			{
				aliases.Add(new ParameterAlias(alias.Value, index.Value));
			}
		}

		// ---- calls ----

		private bool TryFindCalls(List<ParameterizedConstruct> parameterized, List<FunctionBody> bodies, out List<CallSite> calls)
		{
			HashSet<string> wrapperNames = [.. parameterized.Select(construct => construct.Body.Name)];
			List<CallSite> found = [];
			bool completed = !_watch.HasBeenExceeded();
			Match match = completed ? TimedRegexEvaluation.Match(_watch, _tracking.CallSiteExpression, _text) : Match.Empty;

			while (completed && match.Success && found.Count < _tracking.MaxCalls)
			{
				AddCallIfToAWrapper(match, wrapperNames, bodies, found);
				completed = !_watch.HasBeenExceeded();
				match = completed ? TimedRegexEvaluation.NextMatch(_watch, match) : Match.Empty;
			}

			calls = found;

			return completed;
		}

		private void AddCallIfToAWrapper(Match match, HashSet<string> wrapperNames, List<FunctionBody> bodies, List<CallSite> calls)
		{
			Group name = match.Groups[WrapperTracking.NAME_GROUP_NAME];
			Group arguments = match.Groups[WrapperTracking.ARGUMENTS_GROUP_NAME];

			if (name.Success
				&& wrapperNames.Contains(name.Value)
				&& FileDemotionSignals.IsTrustworthyCodeAt(_context.Mask, name.Index)
				&& !bodies.Any(body => name.Index >= body.DefinitionStart && name.Index < body.DefinitionEnd))
			{
				calls.Add(new CallSite
				{
					Name = name.Value,
					MatchIndex = match.Index,
					MatchLength = match.Length,
					NameIndex = name.Index,
					Arguments = arguments.Success ? arguments.Value : string.Empty
				});
			}
		}

		// ---- replacement ----

		private bool TryReplace(
			List<ClassifiedMatch> classifiedMatches,
			List<ParameterizedConstruct> parameterized,
			List<CallSite> calls)
		{
			HashSet<int> removedPositions = [.. parameterized.Select(construct => construct.MatchPosition)];
			List<ClassifiedMatch> replaced = [];
			HashSet<(string PatternClass, int Anchor)> seen = [];
			bool completed = true;

			foreach (CallSite call in calls)
			{
				List<bool> bearing = [];

				if (completed)
				{
					completed = TryReadBearingArguments(call, out bearing);
				}

				if (completed)
				{
					AddReplacements(call, bearing, new ReplacementState(classifiedMatches, parameterized, seen, replaced));
				}
			}

			if (completed)
			{
				List<ClassifiedMatch> kept = [.. classifiedMatches.Where((_, position) => !removedPositions.Contains(position))];

				classifiedMatches.Clear();
				classifiedMatches.AddRange(kept);
				classifiedMatches.AddRange(replaced);
			}

			return completed;
		}

		private static void AddReplacements(CallSite call, List<bool> bearing, ReplacementState state)
		{
			foreach (ParameterizedConstruct construct in state.Parameterized)
			{
				ClassifiedMatch inner = state.ClassifiedMatches[construct.MatchPosition];

				if (string.Equals(construct.Body.Name, call.Name, StringComparison.Ordinal)
					&& UsesABearingArgument(construct.ParameterIndexes, bearing)
					&& state.Seen.Add((inner.PatternClassIdentifier, call.NameIndex)))
				{
					state.Replaced.Add(inner with
					{
						MatchIndex = call.MatchIndex,
						MatchLength = call.MatchLength,
						AnchorOffset = call.NameIndex,
						LiteralCredentialSignal = false
					});
				}
			}
		}

		private static bool UsesABearingArgument(HashSet<string> parameterIndexes, List<bool> bearing)
		{
			bool uses = false;

			foreach (string index in parameterIndexes)
			{
				bool anyArgument = index is _ANY_ARGUMENT_FIRST or _ANY_ARGUMENT_SECOND;
				bool positional = index.Length == 1 && index[0] is >= _FIRST_POSITIONAL_DIGIT and <= _LAST_POSITIONAL_DIGIT;
				int argumentPosition = positional ? index[0] - _FIRST_POSITIONAL_DIGIT : -1;

				uses = uses
					|| (anyArgument && bearing.Contains(true))
					|| (positional && argumentPosition < bearing.Count && bearing[argumentPosition]);
			}

			return uses;
		}

		/// <summary>One flag per argument word of the call: whether that word carries a credential.</summary>
		private bool TryReadBearingArguments(CallSite call, out List<bool> bearing)
		{
			bearing = [];

			bool completed = !_watch.HasBeenExceeded();
			Match match = completed ? TimedRegexEvaluation.Match(_watch, _tracking.ArgumentExpression, call.Arguments) : Match.Empty;

			while (completed && match.Success)
			{
				completed = TryTestArgument(match.Value, call, out bool carriesACredential);
				bearing.Add(carriesACredential);
				completed = completed && !_watch.HasBeenExceeded();
				match = completed ? TimedRegexEvaluation.NextMatch(_watch, match) : Match.Empty;
			}

			return completed;
		}

		/// <summary>
		/// An argument that is wholly ONE PLAIN expansion (<c>$X</c>, <c>"$X"</c>, <c>${X}</c>, <c>${N}</c>,
		/// <c>${X[idx]}</c>, <c>$1</c>, <c>"$@"</c>, <c>$*</c>, a special parameter) is replaced by the unique declared
		/// value of the variable it names, one hop, where it names a plain variable the symbol table resolves. One that does
		/// not resolve - an unknown variable, a positional or special parameter, an element expansion - is NOT
		/// credential-bearing, and its own text is never tested. Every other word is tested as written, and that includes a
		/// braced expansion with an operator after the name (<c>${X:-lit}</c>, <c>${X:=lit}</c>, <c>${X#pat}</c>,
		/// <c>${X/a/b}</c>, <c>${#X}</c>, ...): it can carry a literal credential, so its text is read.
		/// </summary>
		private bool TryTestArgument(string word, CallSite call, out bool carriesACredential)
		{
			bool isExpansion = WholeExpansionOf(word, out string? variable);
			string? tested = isExpansion ? null : word;
			carriesACredential = false;

			if (variable is not null && _symbols.TryResolveUnambiguously(variable, call.NameIndex, out string declaredValue))
			{
				tested = declaredValue;
			}

			bool completed = !_watch.HasBeenExceeded();

			if (completed && tested is not null)
			{
				carriesACredential = TimedRegexEvaluation.IsMatch(_watch, _tracking.CredentialBearingArgument, tested);
				completed = !_watch.HasBeenExceeded();
			}

			return completed;
		}

		/// <summary>
		/// Whether a word is, optionally inside one pair of double quotes, exactly one PLAIN expansion;
		/// <paramref name="variable"/> is the plain variable name where it names one, else null. A braced body is plain when
		/// it is a name, digits, one special character, or a name followed by a bracketed subscript to the closing brace;
		/// anything else after the name is an operator and not plain.
		/// </summary>
		private static bool WholeExpansionOf(string word, out string? variable)
		{
			string unquoted = word.Length >= 2 && word[0] == _DOUBLE_QUOTE && word[^1] == _DOUBLE_QUOTE
				? word[1..^1]
				: word;
			bool braced = unquoted.Length >= 4
				&& unquoted[0] == _DOLLAR
				&& unquoted[1] == _OPENING_BRACE
				&& unquoted.IndexOf(_CLOSING_BRACE) == unquoted.Length - 1;
			string body = braced ? unquoted[2..^1] : (unquoted.Length >= 2 && unquoted[0] == _DOLLAR ? unquoted[1..] : string.Empty);
			int nameLength = body.TakeWhile(VariableReferenceIndex.IsIdentifierCharacter).Count();
			bool startsWithADigit = body.Length > 0 && char.IsAsciiDigit(body[0]);
			bool isName = body.Length > 0 && !startsWithADigit && nameLength == body.Length;
			bool isSpecial = body.Length > 0 && (body.All(char.IsAsciiDigit) || (body.Length == 1 && _SPECIAL_PARAMETERS.Contains(body[0])));
			bool isSubscripted = braced && !startsWithADigit && nameLength > 0 && nameLength < body.Length && body[nameLength] == _OPENING_BRACKET && body[^1] == _CLOSING_BRACKET;

			variable = isName ? body : null;

			return isName || isSpecial || isSubscripted;
		}
	}
}
