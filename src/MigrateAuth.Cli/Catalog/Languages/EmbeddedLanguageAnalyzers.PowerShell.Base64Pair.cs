using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The two Base64 classes of the `PowerShell` analyzer, PS-010 (<c>Base64UserPasswordPair</c>) and PS-011 (<c>TokenCarriedUnderBasicScheme</c>), and the
/// pair spellings they share. The shared vocabulary they read is in <c>EmbeddedLanguageAnalyzers.PowerShell.Expressions.cs</c>.
///
/// THE SHAPE OF BOTH EXPRESSIONS. Both anchor on the encoder call, so a pair that feeds one encoder
/// is described by one anchor position, and PS-011 subsumes PS-010 where both match it. A pair is read in one of three places:
/// (1) INSIDE THE ENCODER CALL, within the call's own parentheses, which the window may enter and cross balanced groups of but never leave, across line
/// breaks, and which stops at a statement end (so a pair after the call's closing parenthesis is not read as inside it);
/// (2) IN A STATEMENT BEFORE THE ENCODER, which counts only when that statement assigns its result to a variable that is read back by name before the
/// encoder, or inside the encoder call - an unrelated colon-bearing literal above an encoder is never a pair;
/// (3) AS A TWO-ELEMENT ARRAY joined later, which is read from the join backwards: the rare conjunct, the join, is verified first and the array
/// assignment is reached by a look-behind of the same ten lines.
/// PS-011 reads, besides, a variable assigned an empty half within the ten lines before the pair, through the assignment and never through the symbol table.
/// </summary>
internal static partial class PowerShellPatternExpressions
{
	// ------------------------------------------------------------------------------------------
	// Delimiters, halves and the empty half
	// ------------------------------------------------------------------------------------------

	private const string _SINGLE_OPEN = @"['\u2018]";

	private const string _SINGLE_CLOSE = @"['\u2019]";

	private const string _DOUBLE_OPEN = @"[""\u201C]";

	private const string _DOUBLE_CLOSE = @"[""\u201D]";

	/// <summary>The colon literal of a concatenation, a join or a call, in any of the six string kinds that can hold one character.</summary>
	private const string _COLON_LITERAL = @"(?:['\u2018]:['\u2019]|[""\u201C]:[""\u201D])";

	private const string _EMPTY_LITERAL = @"(?:['\u2018]['\u2019]|[""\u201C][""\u201D])";

	private const string _SINGLE_QUOTED_NON_EMPTY = @"['\u2018][^'\u2018\u2019\n]+['\u2019]";

	private const string _DOUBLE_QUOTED_NON_EMPTY = @"[""\u201C][^""\u201C\u201D\n]+[""\u201D]";

	/// <summary>A half that is certainly not empty: a sub-expression, a variable that is not <c>$null</c>, a parenthesised expression, a static member that is not <c>[string]::Empty</c>, or a non-empty literal.</summary>
	private const string _NON_EMPTY_HALF =
		@"(?:(?!\$null(?![\w])|\[(?:System\.)?string\]::Empty(?!\w))" + _CODE_ARGUMENT + @"|" + _SINGLE_QUOTED_NON_EMPTY + @"|" + _DOUBLE_QUOTED_NON_EMPTY + @")";

	/// <summary>An empty half that is a literal: an empty literal, <c>$null</c> or <c>[string]::Empty</c>.</summary>
	private const string _EMPTY_HALF_LITERAL =
		@"(?:" + _EMPTY_LITERAL + @"|\$null(?![\w])|\[(?:System\.)?string\]::Empty(?!\w))";

	/// <summary>
	/// The name of a variable that may have been assigned an empty half, captured as <c>e</c> and never <c>null</c>. Whether it was is decided once the whole pair has
	/// matched, by a look-behind at the pair's end (<see cref="_EMPTY_VARIABLE_WAS_ASSIGNED_BEFORE"/>), so the pair alternatives stay free of it.
	/// </summary>
	private const string _EMPTY_VARIABLE_NAME = @"(?:" + _SCOPE + @")?(?!null(?!\w))(?<e>" + _NAME + @")";

	private const string _EMPTY_VARIABLE = @"(?:\$\{[ \t]*" + _EMPTY_VARIABLE_NAME + @"[ \t]*\}|\$" + _EMPTY_VARIABLE_NAME + @"(?![\w:.\[(]))";

	/// <summary>An empty half: an empty literal, <c>$null</c>, <c>[string]::Empty</c>, or a variable that was assigned one in the lines before.</summary>
	private const string _EMPTY_HALF = @"(?:" + _EMPTY_HALF_LITERAL + @"|" + _EMPTY_VARIABLE + @")";

	private const string _EMPTY_NAME_OR_NULL = @"(?:null(?!\w)|" + _EMPTY_VARIABLE_NAME + @")";

	/// <summary>An empty half written as the user half of an interpolated pair: a hole, <c>${null}</c>, or <c>$null</c> before a backtick colon.</summary>
	private const string _EMPTY_USER_HOLE =
		@"(?:\$\([ \t]*" + _EMPTY_HALF + @"[ \t]*\)|\$\{[ \t]*" + _EMPTY_NAME_OR_NULL + @"[ \t]*\}|\$" + _EMPTY_NAME_OR_NULL + @"(?=`:))";

	/// <summary>An empty half written as the password half of an interpolated pair: a hole, <c>${null}</c>, or a bare <c>$null</c> at the end of the string.</summary>
	private const string _EMPTY_PASSWORD_HOLE =
		@"(?:\$\([ \t]*" + _EMPTY_HALF + @"[ \t]*\)|\$\{[ \t]*" + _EMPTY_NAME_OR_NULL + @"[ \t]*\}|\$" + _EMPTY_NAME_OR_NULL + @"(?![\w:]))";

	/// <summary>The password half of an interpolated pair that is code: a sub-expression, which is captured, or a bare variable, which sits in the string body and is not.</summary>
	private const string _INTERPOLATED_HALF = @"(?:(?<credential>" + _SUB_EXPRESSION + @")|" + _BARE_INTERPOLATION + @")";

	/// <summary>The user half of an interpolated pair that stands beside a literal half: a hole or a braced variable, or a bare variable before a backtick colon, never a bare <c>$scope:name</c>.</summary>
	private const string _INTERPOLATED_USER_HALF = @"(?:" + _SUB_EXPRESSION + @"|\$\{[^{}\n]{1,120}\}|\$" + _NAME + @"(?=`:))";

	private const string _PAIR_COLON = @"(?:`:|:)";

	/// <summary>Where a half may open in a concatenation or a join: not glued to the end of a word, a member access, a closing bracket or a quote.</summary>
	private const string _HALF_BOUNDARY = @"(?<![\w\-.$)\]'""\u2019\u201D])";

	private const string _HALF_SEPARATOR = @"[ \t]*,[ \t]*";

	/// <summary>The one-literal exclusions that apply before the colon: a single letter is a drive, and an all-digit half is a time or a port.</summary>
	private const string _NOT_A_DRIVE_OR_DIGITS_BEFORE = @"(?![A-Za-z]:)(?![0-9]+:)";

	/// <summary>The one-literal exclusions that apply after the colon: a URL scheme or a path, and an all-digit half. The closing delimiter is the one of the literal.</summary>
	private const string _SINGLE_NOT_A_URL_OR_DIGITS_AFTER = @"(?![/\\])(?![0-9]+['\u2019])";

	private const string _DOUBLE_NOT_A_URL_OR_DIGITS_AFTER = @"(?![/\\])(?![0-9]+[""\u201D])";

	private const string _SINGLE_BODY_BEFORE_COLON = @"[^'\u2018\u2019:\n{]+";

	private const string _SINGLE_BODY_AFTER_COLON = @"[^'\u2018\u2019\n{]+";

	private const string _DOUBLE_BODY_BEFORE_COLON = @"[^""\u201C\u201D:$`\n{]+";

	private const string _DOUBLE_BODY_AFTER_COLON = @"[^""\u201C\u201D$`\n{]+";

	/// <summary>The format string of a two-hole pair, in any string kind.</summary>
	private const string _FORMAT_STRING = @"['""\u2018\u2019\u201C\u201D]\{0\}:\{1\}['""\u2018\u2019\u201C\u201D]";

	/// <summary>The format operator and its argument list, bare, parenthesised or as an array expression, up to the first half.</summary>
	private const string _FORMAT_OPERATOR = _FORMAT_STRING + @"[ \t]*-f[ \t]*(?:@?\([ \t]*)?";

	private const string _FORMAT_METHOD = @"\[(?:System\.)?string\]::Format[ \t]*\([ \t]*" + _FORMAT_STRING + _HALF_SEPARATOR;

	private const string _CONCAT_METHOD = @"\[(?:System\.)?string\]::Concat[ \t]*\([ \t]*";

	private const string _JOIN_METHOD = @"\[(?:System\.)?string\]::Join[ \t]*\([ \t]*" + _COLON_LITERAL + _HALF_SEPARATOR;

	/// <summary>The array-join spelling: <c>($u, $p) -join ':'</c>, bare or parenthesised. The two halves are given by the caller.</summary>
	private const string _JOIN_OPERATOR = @"(?:[ \t]*\))?[ \t]*-join[ \t]*" + _COLON_LITERAL;

	private const string _JOIN_OPENING = @"(?<![\w\-.$])(?:@?\([ \t]*)?";

	// ------------------------------------------------------------------------------------------
	// The pair, in every spelling
	//
	// A SPELLING IS WRITTEN ONCE, AROUND ITS HALVES. The two halves are the expensive part of every expression here, and the generated code of an expression that
	// repeats them in every alternative is too large for the runtime to optimize. So the spellings that put one half on each side of a separator share one skeleton,
	// an opening, the two halves, a separator and a closing, and a group says which opening was taken so that the separator and the closing are the ones that
	// opening needs; the double-quoted spellings share one body in the same way.
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// What opens a pair of two halves: a bare concatenation (<c>n</c>), the array join, bare or parenthesised (<c>j</c>), the format operator (<c>f</c>), the format
	/// method (<c>m</c>), the concatenation method (<c>c</c>), or the join method, which carries the colon in its own arguments.
	/// </summary>
	private const string _PAIR_OPENING =
		@"(?:(?<n>" + _HALF_BOUNDARY + @")|(?<j>" + _JOIN_OPENING + @")|(?<f>" + _FORMAT_OPERATOR + @")|(?<m>" + _FORMAT_METHOD + @")|(?<c>" + _CONCAT_METHOD + @")|" + _JOIN_METHOD + @")";

	/// <summary>What stands between the two halves for the opening that was taken: the concatenated colon, or a comma, and for the concatenation method the colon argument.</summary>
	private const string _PAIR_SEPARATOR =
		@"(?(n)[ \t]*\+[ \t]*" + _COLON_LITERAL + @"[ \t]*\+[ \t]*|[ \t]*,[ \t]*(?(c)" + _COLON_LITERAL + @"[ \t]*,[ \t]*))";

	/// <summary>What closes the pair for the opening that was taken: the join operator, or the format method's parenthesis.</summary>
	private const string _PAIR_CLOSING = @"(?(j)" + _JOIN_OPERATOR + @")(?(m)[ \t]*\))";

	/// <summary>The password half of a pair that is a literal, with the exclusions of a one-literal pair: not a URL scheme or a path, and not all digits.</summary>
	private const string _DOUBLE_QUOTED_PASSWORD_LITERAL =
		_DOUBLE_NOT_A_URL_OR_DIGITS_AFTER + @"(?<credential>" + _DOUBLE_BODY_AFTER_COLON + @")";

	/// <summary>The literal user half of a double-quoted pair, with the exclusions of a one-literal pair: not a drive, and not all digits.</summary>
	private const string _DOUBLE_QUOTED_USER_LITERAL = _NOT_A_DRIVE_OR_DIGITS_BEFORE + _DOUBLE_BODY_BEFORE_COLON;

	/// <summary>
	/// The double-quoted spellings of a pair of two non-empty halves: a hole or a bare variable on each side of the colon; a literal user half and a hole password
	/// half; and a hole user half, one that cannot be a bare scope name, and a literal password half.
	/// </summary>
	private const string _DOUBLE_QUOTED_PAIR_NON_EMPTY =
		_DOUBLE_OPEN + @"(?:(?<us>" + _INTERPOLATED_USER_HALF + @")|(?:" + _SUB_EXPRESSION + @"|" + _BARE_INTERPOLATION + @")|" + _DOUBLE_QUOTED_USER_LITERAL + @")" + _PAIR_COLON
		+ @"(?:" + _INTERPOLATED_HALF + @"|(?(us)" + _DOUBLE_QUOTED_PASSWORD_LITERAL + @"|(?!)))" + _DOUBLE_CLOSE;

	/// <summary>The one literal that holds both halves, single-quoted, with the exclusions: <c>'user:password'</c>, and not a URL, a drive, a path, a time or a port.</summary>
	private const string _SINGLE_QUOTED_ONE_LITERAL_PAIR =
		_SINGLE_OPEN + _NOT_A_DRIVE_OR_DIGITS_BEFORE + _SINGLE_BODY_BEFORE_COLON + @":" + _SINGLE_NOT_A_URL_OR_DIGITS_AFTER + @"(?<credential>" + _SINGLE_BODY_AFTER_COLON + @")" + _SINGLE_CLOSE;

	/// <summary>The one literal that holds both halves, double-quoted.</summary>
	private const string _DOUBLE_QUOTED_ONE_LITERAL_PAIR =
		_DOUBLE_OPEN + _NOT_A_DRIVE_OR_DIGITS_BEFORE + _DOUBLE_BODY_BEFORE_COLON + @":" + _DOUBLE_QUOTED_PASSWORD_LITERAL + _DOUBLE_CLOSE;

	/// <summary>The pair in every spelling, both halves non-empty; the credential position is the password half.</summary>
	private const string _PAIR_NON_EMPTY_BOTH =
		@"(?:" + _DOUBLE_QUOTED_PAIR_NON_EMPTY
		+ @"|" + _PAIR_OPENING + _NON_EMPTY_HALF + _PAIR_SEPARATOR + @"(?<credential>" + _NON_EMPTY_HALF + @")" + _PAIR_CLOSING
		+ @"|" + _HALF_BOUNDARY + @"(?:['\u2018][^'\u2018\u2019\n]+:['\u2019]|[""\u201C][^""\u201C\u201D\n$`]+:[""\u201D])[ \t]*\+[ \t]*(?<credential>" + _NON_EMPTY_HALF + @")"
		+ @"|" + _SINGLE_QUOTED_ONE_LITERAL_PAIR
		+ @"|" + _DOUBLE_QUOTED_ONE_LITERAL_PAIR + @")";

	/// <summary>The double-quoted spellings of a pair with one empty half: the empty half as a hole, or as nothing, on either side of the colon; the other half is the credential.</summary>
	private const string _DOUBLE_QUOTED_PAIR_ONE_EMPTY =
		_DOUBLE_OPEN + @"(?:(?<u>" + _EMPTY_USER_HOLE + @"|)|(?:(?<credential>" + _SUB_EXPRESSION + @")|" + _BARE_INTERPOLATION + @")|(?<credential>" + _DOUBLE_QUOTED_USER_LITERAL + @"))" + _PAIR_COLON
		+ @"(?(u)(?:" + _INTERPOLATED_HALF + @"|" + _DOUBLE_QUOTED_PASSWORD_LITERAL + @")|(?:" + _EMPTY_PASSWORD_HOLE + @")?)" + _DOUBLE_CLOSE;

	/// <summary>The pair with one empty half in every spelling; the credential position is the half that is not empty.</summary>
	private const string _PAIR_ONE_EMPTY =
		@"(?:" + _DOUBLE_QUOTED_PAIR_ONE_EMPTY
		+ @"|" + _PAIR_OPENING + @"(?:(?<u>" + _EMPTY_HALF + @")|(?<credential>" + _NON_EMPTY_HALF + @"))" + _PAIR_SEPARATOR + @"(?(u)(?<credential>" + _NON_EMPTY_HALF + @")|" + _EMPTY_HALF + @")" + _PAIR_CLOSING
		+ @"|" + _HALF_BOUNDARY + @"(?<!\+[ \t]*)" + _COLON_LITERAL + @"[ \t]*\+[ \t]*(?<credential>" + _NON_EMPTY_HALF + @")"
		+ @"|" + _HALF_BOUNDARY + @"(?<credential>" + _NON_EMPTY_HALF + @")[ \t]*\+[ \t]*" + _COLON_LITERAL + @"(?![ \t]*\+)"
		+ @"|" + _SINGLE_OPEN + @":" + _SINGLE_NOT_A_URL_OR_DIGITS_AFTER + @"(?<credential>" + _SINGLE_BODY_AFTER_COLON + @")" + _SINGLE_CLOSE
		+ @"|" + _SINGLE_OPEN + _NOT_A_DRIVE_OR_DIGITS_BEFORE + @"(?<credential>" + _SINGLE_BODY_BEFORE_COLON + @"):" + _SINGLE_CLOSE + @")";

	/// <summary>
	/// WHAT EVERY SPELLING OF A PAIR HAS IN COMMON, tested before any of them is tried. Every spelling above lies on one line - none of its parts reads a line
	/// break - and holds a colon that a quote character precedes: the colon of a quoted colon, of an interpolated string, of a one-literal pair or of a format string. So
	/// a pair can start only where, on the rest of the line, a colon follows a quote character. Each part is read in one way only (the first quote, then the first colon after
	/// it), so the look-ahead never backtracks. It gives the same matches and the same captures as the expressions without it: it only refuses, cheaply, the places where
	/// the spellings would all have failed, which a window that is open across lines offers by the hundred.
	/// </summary>
	private const string _PAIR_STARTS_BEFORE_A_QUOTED_COLON_ON_THE_LINE = @"(?=[^\n'""‘’“”]*['""‘’“”][^\n:]*:)";

	// ------------------------------------------------------------------------------------------
	// The encoder, and where a pair may stand relative to it
	// ------------------------------------------------------------------------------------------

	private const string _ENCODER_TYPE_PREFIX = @"\[(?:[A-Za-z_]\w*\.){0,3}Convert\]::";

	private const string _ENCODER_METHOD_NAME = "ToBase64String";

	private const string _ENCODER = _ENCODER_TYPE_PREFIX + @"(?<encoder>" + _ENCODER_METHOD_NAME + @")[ \t]*\(";

	/// <summary>The encoder as a position that can be tested without capturing it: the same text as <see cref="_ENCODER"/>, with no group.</summary>
	private const string _ENCODER_PROBE = _ENCODER_TYPE_PREFIX + _ENCODER_METHOD_NAME + @"[ \t]*\(";

	private const string _ENCODER_AHEAD = @"(?=" + _ENCODER_PROBE + @")";

	// ------------------------------------------------------------------------------------------
	// The line windows to the encoder, unrolled
	//
	// THE WINDOW BETWEEN A PAIR AND THE ENCODER IS A BOUNDED NUMBER OF LINES, N, WRITTEN UNROLLED. A lazy bounded loop, (?:[^\n]*\n){0,N}?[^\n]*?, followed by
	// the encoder is a shape the [GeneratedRegex] source generator and RegexOptions.Compiled mishandle: when the text after the pair holds N line breaks and the
	// encoder then fails, the generated matcher does not terminate. Level k says "the encoder starts on the rest of this line, or this line ends and level k-1
	// says it of the next one". The alternatives are the loop's own, in the loop's own order (fewest line breaks first, then the shortest run on the line), so
	// the same position is found first and every later one is offered in the same order when what follows fails; no level is a loop that can be left by its
	// upper bound. The encoder is tested by a look-ahead without a group and then consumed by the caller, so the captures of the match are the loop's own.
	// ------------------------------------------------------------------------------------------

	private const string _TO_THE_ENCODER_0 = @"[^\n]*?" + _ENCODER_AHEAD;

	private const string _TO_THE_ENCODER_1 = @"(?:[^\n]*?" + _ENCODER_AHEAD + @"|[^\n]*\n" + _TO_THE_ENCODER_0 + @")";

	private const string _TO_THE_ENCODER_2 = @"(?:[^\n]*?" + _ENCODER_AHEAD + @"|[^\n]*\n" + _TO_THE_ENCODER_1 + @")";

	private const string _TO_THE_ENCODER_3 = @"(?:[^\n]*?" + _ENCODER_AHEAD + @"|[^\n]*\n" + _TO_THE_ENCODER_2 + @")";

	private const string _TO_THE_ENCODER_4 = @"(?:[^\n]*?" + _ENCODER_AHEAD + @"|[^\n]*\n" + _TO_THE_ENCODER_3 + @")";

	/// <summary>The encoder within five lines, as the array join needs it: <see cref="_LINE_WINDOW_FIVE"/> followed by the encoder, unrolled. The caller consumes the encoder.</summary>
	private const string _TO_THE_ENCODER_5 = @"(?:[^\n]*?" + _ENCODER_AHEAD + @"|[^\n]*\n" + _TO_THE_ENCODER_4 + @")";

	private const string _TO_THE_ENCODER_6 = @"(?:[^\n]*?" + _ENCODER_AHEAD + @"|[^\n]*\n" + _TO_THE_ENCODER_5 + @")";

	private const string _TO_THE_ENCODER_7 = @"(?:[^\n]*?" + _ENCODER_AHEAD + @"|[^\n]*\n" + _TO_THE_ENCODER_6 + @")";

	private const string _TO_THE_ENCODER_8 = @"(?:[^\n]*?" + _ENCODER_AHEAD + @"|[^\n]*\n" + _TO_THE_ENCODER_7 + @")";

	private const string _TO_THE_ENCODER_9 = @"(?:[^\n]*?" + _ENCODER_AHEAD + @"|[^\n]*\n" + _TO_THE_ENCODER_8 + @")";

	/// <summary>The encoder within ten lines, as the pair's tie needs it: <see cref="_LINE_WINDOW_TEN"/> followed by the encoder, unrolled. The caller consumes the encoder.</summary>
	private const string _TO_THE_ENCODER_10 = @"(?:[^\n]*?" + _ENCODER_AHEAD + @"|[^\n]*\n" + _TO_THE_ENCODER_9 + @")";

	/// <summary>
	/// THE PAIR INSIDE THE ENCODER CALL. The window may enter an opening parenthesis and cross a balanced group, may cross line breaks and
	/// backtick continuations, and stops at the call's closing parenthesis - a closing parenthesis with no opening one in the window cannot be crossed - or at a
	/// statement end. A unit is a run of characters that cannot open a pair, or one character that can, so the pair is tried at every place it could start and nowhere else.
	/// </summary>
	private const string _IN_CALL_PAIR_WINDOW =
		@"(?:(?:(?>[^;()""'$\[@\u201C\u2018]+)|[""'$\[@\u201C\u2018]|(?<call>\()|(?<-call>\)))" + _BACKTRACK_POINT + @"){0,120}?";

	/// <summary>The same window kept inside one statement, before the encoder: it does not cross a statement end and never reads an encoder as part of the statement.</summary>
	private const string _STATEMENT_PAIR_WINDOW =
		@"(?:(?:(?>[^;\n""'$(\[@\u201C\u2018`]+)|[""'$(@\u201C\u2018]|\[(?!" + _ENCODER_TYPE_PREFIX + _ENCODER_METHOD_NAME + @")|`[\s\S])" + _BACKTRACK_POINT + @"){0,80}?";

	/// <summary>
	/// A cheap required literal before the expensive alternation: every spelling of a pair holds a colon, so a call whose own window holds none is never tried
	/// against the spellings. The look-ahead walks the call exactly as the window does, entering parentheses and never leaving the call, and is atomic.
	/// </summary>
	private const string _COLON_AHEAD_IN_THE_CALL = @"(?=(?>(?:[^;():]|(?<ahead>\()|(?<-ahead>\))){0,400}):)";

	/// <summary>The same required colon for a statement before the encoder: it stands before the statement's end, across a backtick continuation.</summary>
	private const string _COLON_AHEAD_IN_THE_STATEMENT = @"(?=(?>(?:[^;\n`:]|`(?!:)[\s\S]){0,400})`?:)";

	/// <summary>
	/// The statement before the encoder that assigns a pair, or something that holds one, to a variable. <c>+=</c> counts: a pair accumulated across two statements
	/// is read as the token class.
	/// </summary>
	private const string _ASSIGNMENT_OF_THE_PAIR =
		@"(?<![\w.:])\$(?:\{[ \t]*(?:" + _SCOPE + @")?(?<v>" + _NAME + @")[ \t]*\}|(?:" + _SCOPE + @")?(?<v>" + _NAME + @"))[ \t]*\+?=(?!=)";

	/// <summary>
	/// A cheap guard in front of every alternative that opens on an assignment: a variable followed by an assignment operator. Most dollar signs of a script
	/// are not that, and the guard lets them leave after one look instead of one look per alternative.
	/// </summary>
	private const string _ASSIGNMENT_GUARD = @"(?=\$(?:\{[ \t]*)?(?:" + _SCOPE + @")?" + _NAME + @"[ \t]*\}?[ \t]*\+?=(?!=))";

	/// <summary>
	/// A required look-ahead before the pair is tried: the tie that follows needs an encoder within ten lines, so a statement with none is left at once instead of
	/// failing the tie after each pair candidate and backtracking into the window. It is the dearer of the two look-aheads that guard the statement before the encoder
	/// (it reads up to eleven lines), so it stands AFTER <see cref="_COLON_AHEAD_IN_THE_STATEMENT"/>, which ends at the statement's own end: on a long line of assignments
	/// that hold no colon, every assignment leaves at the colon and none reads the lines below it. Both are pure look-aheads and join by conjunction, so their order decides
	/// what is read, never what matches or what is captured.
	/// </summary>
	private const string _ENCODER_WITHIN_THE_WINDOW = @"(?=" + _TO_THE_ENCODER_10 + _ENCODER + @")";

	/// <summary>A read of the variable that carries the pair.</summary>
	private const string _READ_OF_THE_PAIR_VARIABLE = @"\$(?:\{[ \t]*)?(?:" + _SCOPE + @")?(?-i:\k<v>)(?![\w])";

	/// <summary>The pair's variable is read, in the up to ten lines after the pair, before any encoder: the intermediate statement that turns the pair into bytes.</summary>
	private const string _READ_BEFORE_THE_ENCODER =
		@"(?=(?:(?:(?:(?!" + _ENCODER_METHOD_NAME + @")[^\n])*\n)" + _BACKTRACK_POINT + @"){0,10}?(?:(?!" + _ENCODER_METHOD_NAME + @")[^\n])*?" + _READ_OF_THE_PAIR_VARIABLE + @")";

	/// <summary>
	/// What ties a pair in a statement before the encoder to the encoder: the encoder within ten lines of the pair, and the variable the pair was assigned to
	/// read back before it, or inside its call. A pair inside the call needs no tie.
	/// </summary>
	private const string _TIE_TO_THE_ENCODER =
		@"(?:" + _READ_BEFORE_THE_ENCODER + _TO_THE_ENCODER_10 + _ENCODER + @"|" + _TO_THE_ENCODER_10 + _ENCODER + _IN_CALL_PAIR_WINDOW + _READ_OF_THE_PAIR_VARIABLE + @")";

	// ------------------------------------------------------------------------------------------
	// The array join
	// ------------------------------------------------------------------------------------------

	private const string _JOIN_OVER_A_VARIABLE = @"\$(?:" + _SCOPE + @")?(?<a>" + _NAME + @")[ \t]*-join[ \t]*" + _COLON_LITERAL;

	private const string _ARRAY_JOIN_VARIABLE = @"\$(?:" + _SCOPE + @")?(?-i:\k<a>)";

	// The join is the position-fixed conjunct and is verified first, naming its variable; the array assignment is then read by a look-behind of the ten lines
	// before it, and the encoder by the five lines after. The two elements are given by the pair spelling.
	private const string _ARRAY_ASSIGNMENT_OPENING = _ARRAY_JOIN_VARIABLE + @"[ \t]*=[ \t]*(?:@\([ \t]*)?";

	private const string _ARRAY_ASSIGNMENT_CLOSING = @"[ \t]*\)?" + _LINE_WINDOW_TEN;

	private const string _ARRAY_ELEMENTS_NON_EMPTY = _NON_EMPTY_HALF + _HALF_SEPARATOR + @"(?<credential>" + _NON_EMPTY_HALF + @")";

	// A look-behind reads right to left, so the right element decides whether the left one is empty.
	private const string _ARRAY_ELEMENTS_ONE_EMPTY =
		@"(?(w)(?<credential>" + _NON_EMPTY_HALF + @")|" + _EMPTY_HALF_LITERAL + @")" + _HALF_SEPARATOR + @"(?:(?<w>" + _EMPTY_HALF_LITERAL + @")|(?<credential>" + _NON_EMPTY_HALF + @"))";

	private const string _ARRAY_JOIN_PAIR_NON_EMPTY =
		@"(?=" + _JOIN_OVER_A_VARIABLE + @")(?<=" + _ARRAY_ASSIGNMENT_OPENING + _ARRAY_ELEMENTS_NON_EMPTY + _ARRAY_ASSIGNMENT_CLOSING + @")"
		+ _ARRAY_JOIN_VARIABLE + @"[ \t]*-join[ \t]*" + _COLON_LITERAL + _TO_THE_ENCODER_5 + _ENCODER;

	private const string _ARRAY_JOIN_PAIR_ONE_EMPTY =
		@"(?=" + _JOIN_OVER_A_VARIABLE + @")(?<=" + _ARRAY_ASSIGNMENT_OPENING + _ARRAY_ELEMENTS_ONE_EMPTY + _ARRAY_ASSIGNMENT_CLOSING + @")"
		+ _ARRAY_JOIN_VARIABLE + @"[ \t]*-join[ \t]*" + _COLON_LITERAL + _TO_THE_ENCODER_5 + _ENCODER;

	// ------------------------------------------------------------------------------------------
	// The empty-half variable (PS-011 only)
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// An empty half as the whole value of a statement. The last look-behind is a one-character test, which the right-to-left reader that walks the window
	/// before it meets first: every spelling of an empty half ends on a closing quote (an empty literal of either kind), on the <c>l</c> of <c>$null</c> or on the
	/// <c>y</c> of <c>[string]::Empty</c>, so a position that ends on anything else is left at once and never reads the spellings.
	/// </summary>
	private const string _EMPTY_ASSIGNED_VALUE = _EMPTY_HALF_LITERAL + @"(?=[ \t]*(?:[;\n}]|#|\z))(?<=['""’”lLyY])";

	/// <summary>
	/// The variable captured as <c>e</c> in the pair was assigned an empty half as a statement of its own in the ten lines before the end of the pair, read as the
	/// file's constant declaration reads a constant: a parameter default, which a caller can override, is not read. The assignment is looked for backwards from the
	/// pair, because an empty assignment is rare and a pair that uses a variable is rarer: the look-behind runs only for a pair that has already matched.
	/// </summary>
	private const string _EMPTY_VARIABLE_WAS_ASSIGNED_BEFORE =
		@"(?<=(?<![\w$.:])(?<![,(][ \t\n]*(?:\[[^\]\n]{1,80}\][ \t]*)*)\$(?:\{[ \t]*)?(?:" + _SCOPE + @")?(?-i:\k<e>)[ \t]*\}?[ \t]*=[ \t]*" + _EMPTY_ASSIGNED_VALUE + _LINE_WINDOW_TEN + @")";

	// ------------------------------------------------------------------------------------------
	// The two classes
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// PS-010. A colon-separated pair of two non-empty halves in any spelling, reaching the standard Base64 encoder through any byte-producing call. The encoder is
	/// the anchor. The pair stands inside the encoder call, within the call's own parentheses; or in a statement before it that assigns it to a variable read back
	/// before the encoder or inside its call, within ten lines; or as a two-element array joined later. A pair with an empty half is the next class's.
	/// </summary>
	[GeneratedRegex(
		@"(?:(?:" + _ENCODER + _COLON_AHEAD_IN_THE_CALL + _IN_CALL_PAIR_WINDOW + @"|" + _ASSIGNMENT_GUARD + _ASSIGNMENT_OF_THE_PAIR + _COLON_AHEAD_IN_THE_STATEMENT + _ENCODER_WITHIN_THE_WINDOW + _STATEMENT_PAIR_WINDOW + @")" + _PAIR_STARTS_BEFORE_A_QUOTED_COLON_ON_THE_LINE + _PAIR_NON_EMPTY_BOTH + @"(?(v)" + _TIE_TO_THE_ENCODER + @")"
		+ @"|" + _ARRAY_JOIN_PAIR_NON_EMPTY + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Base64UserPasswordPairConstruction();

	/// <summary>
	/// PS-011. A token carried under the scheme: the same pair with ONE half empty (an empty literal, <c>$null</c> or <c>[string]::Empty</c>, in any spelling a half
	/// can take, inside a hole included, a variable assigned an empty half in the ten lines before the pair, or a colon with nothing on its other side), reaching the
	/// encoder in the same three places. The empty half is the whole discrimination from the class before; where an expression lets both match one encoder, this
	/// class subsumes that one. The credential position is the half that is not empty.
	/// </summary>
	[GeneratedRegex(
		@"(?:(?:" + _ENCODER + _COLON_AHEAD_IN_THE_CALL + _IN_CALL_PAIR_WINDOW
		+ @"|" + _ASSIGNMENT_GUARD + _ASSIGNMENT_OF_THE_PAIR + _COLON_AHEAD_IN_THE_STATEMENT + _ENCODER_WITHIN_THE_WINDOW + _STATEMENT_PAIR_WINDOW + @")"
		+ _PAIR_STARTS_BEFORE_A_QUOTED_COLON_ON_THE_LINE + _PAIR_ONE_EMPTY + @"(?(e)" + _EMPTY_VARIABLE_WAS_ASSIGNED_BEFORE + @")" + @"(?(v)" + _TIE_TO_THE_ENCODER + @")"
		+ @"|" + _ARRAY_JOIN_PAIR_ONE_EMPTY + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex TokenCarriedUnderBasicSchemeConstruction();
}
