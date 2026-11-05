using System.Text;

namespace MigrateAuth.Cli.Lexical;

/// <summary>
/// THE ANSI-C ESCAPES OF A QUOTED WORD, decoded the way a shell with ANSI-C quoting reads them (<c>bash</c>,
/// <c>zsh</c>, <c>ksh93</c>, <c>mksh</c>): the single-character escapes, one to three octal digits, one or two
/// hexadecimal digits after <c>x</c>, up to four after <c>u</c>, up to eight after <c>U</c> and a control
/// character after <c>c</c>. The text handed over is the content between the quotes.
///
/// THREE READINGS ARE FIXED HERE. (1) A decoded NUL ends the word, as it does in a shell: <c>\0</c>,
/// <c>\x00</c>, <c>\u0000</c>, <c>\c@</c> and an octal escape that is a multiple of 256 all end it, and nothing
/// after them is part of the word. (2) A <c>\u</c> or <c>\U</c> escape that is not a Unicode scalar value
/// (above U+10FFFF, or a surrogate) is kept as written, backslash, letter and digits, so no wrong character is
/// ever produced. (3) An octal escape takes its value modulo 256, as <c>bash</c> does, and a byte value is read
/// as the code point of the same number. An unknown escape keeps its backslash.
/// </summary>
public static class AnsiCEscapes
{
	private const char _ESCAPE_INTRODUCER = '\\';
	private const char _NUL = '\0';
	private const char _CONTROL_QUESTION_MARK = '?';
	private const char _SINGLE_QUOTE = '\'';
	private const char _DOUBLE_QUOTE = '"';
	private const char _ALERT_LETTER = 'a';
	private const char _BACKSPACE_LETTER = 'b';
	private const char _LOWER_ESCAPE_LETTER = 'e';
	private const char _UPPER_ESCAPE_LETTER = 'E';
	private const char _FORM_FEED_LETTER = 'f';
	private const char _LINE_FEED_LETTER = 'n';
	private const char _CARRIAGE_RETURN_LETTER = 'r';
	private const char _TAB_LETTER = 't';
	private const char _VERTICAL_TAB_LETTER = 'v';
	private const char _HEXADECIMAL_LETTER = 'x';
	private const char _SHORT_UNICODE_LETTER = 'u';
	private const char _LONG_UNICODE_LETTER = 'U';
	private const char _CONTROL_LETTER = 'c';
	private const char _FIRST_OCTAL_DIGIT = '0';
	private const char _LAST_OCTAL_DIGIT = '7';
	private const string _ALERT_TEXT = "\a";
	private const string _BACKSPACE_TEXT = "\b";
	private const string _FORM_FEED_TEXT = "\f";
	private const string _LINE_FEED_TEXT = "\n";
	private const string _CARRIAGE_RETURN_TEXT = "\r";
	private const string _TAB_TEXT = "\t";
	private const string _VERTICAL_TAB_TEXT = "\v";
	private const string _ESCAPE_CHARACTER_TEXT = "\u001B";
	private const int _NO_DIGITS = 0;
	private const int _SINGLE_CHARACTER_ESCAPE_LENGTH = 1;
	private const int _INTRODUCER_LENGTH = 1;
	private const int _CONTROL_ESCAPE_LENGTH = 2;
	private const int _MAXIMUM_OCTAL_DIGITS = 3;
	private const int _MAXIMUM_BYTE_HEXADECIMAL_DIGITS = 2;
	private const int _MAXIMUM_SHORT_HEXADECIMAL_DIGITS = 4;
	private const int _MAXIMUM_LONG_HEXADECIMAL_DIGITS = 8;
	private const int _OCTAL_RADIX = 8;
	private const int _HEXADECIMAL_RADIX = 16;
	private const int _BYTE_MASK = 0xFF;
	private const int _CONTROL_MASK = 0x1F;
	private const int _DELETE_CHARACTER = 0x7F;
	private const int _MAXIMUM_CODE_POINT = 0x10FFFF;
	private const int _FIRST_SURROGATE = 0xD800;
	private const int _LAST_SURROGATE = 0xDFFF;

	private static readonly DigitRule _octal = new(_MAXIMUM_OCTAL_DIGITS, _OCTAL_RADIX, IsAByte: true);
	private static readonly DigitRule _byteHexadecimal = new(_MAXIMUM_BYTE_HEXADECIMAL_DIGITS, _HEXADECIMAL_RADIX, IsAByte: false);
	private static readonly DigitRule _shortHexadecimal = new(_MAXIMUM_SHORT_HEXADECIMAL_DIGITS, _HEXADECIMAL_RADIX, IsAByte: false);
	private static readonly DigitRule _longHexadecimal = new(_MAXIMUM_LONG_HEXADECIMAL_DIGITS, _HEXADECIMAL_RADIX, IsAByte: false);

	/// <summary>How many digits an escape reads and in what radix, and whether its value is reduced to a byte.</summary>
	private readonly record struct DigitRule(int MaximumDigits, int Radix, bool IsAByte);

	/// <summary>Decodes the content of an ANSI-C quoted word.</summary>
	public static string Decode(string content)
	{
		StringBuilder decoded = new(content.Length);
		int position = 0;
		bool endedByNul = false;

		while (position < content.Length && !endedByNul)
		{
			if (content[position] == _ESCAPE_INTRODUCER && position + _INTRODUCER_LENGTH < content.Length)
			{
				(string text, int length) = DecodeOneEscape(content, position + _INTRODUCER_LENGTH);

				endedByNul = text.Length > 0 && text[0] == _NUL;

				if (!endedByNul)
				{
					decoded.Append(text);
				}

				position += _INTRODUCER_LENGTH + length;
			}
			else
			{
				decoded.Append(content[position]);
				position++;
			}
		}

		return decoded.ToString();
	}

	private static (string Text, int Length) DecodeOneEscape(string content, int position)
	{
		char letter = content[position];
		(string Text, int Length) decoded = letter switch
		{
			_ALERT_LETTER => (_ALERT_TEXT, _SINGLE_CHARACTER_ESCAPE_LENGTH),
			_BACKSPACE_LETTER => (_BACKSPACE_TEXT, _SINGLE_CHARACTER_ESCAPE_LENGTH),
			_LOWER_ESCAPE_LETTER or _UPPER_ESCAPE_LETTER => (_ESCAPE_CHARACTER_TEXT, _SINGLE_CHARACTER_ESCAPE_LENGTH),
			_FORM_FEED_LETTER => (_FORM_FEED_TEXT, _SINGLE_CHARACTER_ESCAPE_LENGTH),
			_LINE_FEED_LETTER => (_LINE_FEED_TEXT, _SINGLE_CHARACTER_ESCAPE_LENGTH),
			_CARRIAGE_RETURN_LETTER => (_CARRIAGE_RETURN_TEXT, _SINGLE_CHARACTER_ESCAPE_LENGTH),
			_TAB_LETTER => (_TAB_TEXT, _SINGLE_CHARACTER_ESCAPE_LENGTH),
			_VERTICAL_TAB_LETTER => (_VERTICAL_TAB_TEXT, _SINGLE_CHARACTER_ESCAPE_LENGTH),
			_ESCAPE_INTRODUCER or _SINGLE_QUOTE or _DOUBLE_QUOTE or _CONTROL_QUESTION_MARK => (letter.ToString(), _SINGLE_CHARACTER_ESCAPE_LENGTH),
			_HEXADECIMAL_LETTER => DecodeDigits(content, position + _SINGLE_CHARACTER_ESCAPE_LENGTH, _byteHexadecimal, letter),
			_SHORT_UNICODE_LETTER => DecodeDigits(content, position + _SINGLE_CHARACTER_ESCAPE_LENGTH, _shortHexadecimal, letter),
			_LONG_UNICODE_LETTER => DecodeDigits(content, position + _SINGLE_CHARACTER_ESCAPE_LENGTH, _longHexadecimal, letter),
			_CONTROL_LETTER when position + _SINGLE_CHARACTER_ESCAPE_LENGTH < content.Length => (DecodeControl(content[position + _SINGLE_CHARACTER_ESCAPE_LENGTH]), _CONTROL_ESCAPE_LENGTH),
			>= _FIRST_OCTAL_DIGIT and <= _LAST_OCTAL_DIGIT => DecodeDigits(content, position, _octal, null),
			_ => ($"{_ESCAPE_INTRODUCER}{letter}", _SINGLE_CHARACTER_ESCAPE_LENGTH)
		};

		return decoded;
	}

	/// <summary>
	/// The digits of the rule from the position. With none, the escape letter is kept as written (a shell does the
	/// same for <c>\x</c> with no digit). A value that is not a Unicode scalar value is kept as written too, the
	/// digits included. The length counts the escape letter when there is one.
	/// </summary>
	private static (string Text, int Length) DecodeDigits(string content, int position, DigitRule rule, char? escapeLetter)
	{
		int count = 0;

		while (count < rule.MaximumDigits && position + count < content.Length && IsDigitOfRadix(content[position + count], rule.Radix))
		{
			count++;
		}

		int letterLength = escapeLetter is null ? 0 : _SINGLE_CHARACTER_ESCAPE_LENGTH;
		string digits = content.Substring(position, count);
		bool hasDigits = count > _NO_DIGITS;
		long value = hasDigits ? Convert.ToInt64(digits, rule.Radix) : 0;
		long reduced = rule.IsAByte ? value & _BYTE_MASK : value;
		(string Text, int Length) decoded = hasDigits && IsAScalarValue(reduced)
			? (char.ConvertFromUtf32((int)reduced), letterLength + count)
			: ($"{_ESCAPE_INTRODUCER}{escapeLetter}{digits}", letterLength + count);

		return decoded;
	}

	private static bool IsAScalarValue(long codePoint)
	{
		return codePoint is >= 0 and <= _MAXIMUM_CODE_POINT
			&& codePoint is not (>= _FIRST_SURROGATE and <= _LAST_SURROGATE);
	}

	private static bool IsDigitOfRadix(char character, int radix)
	{
		return radix == _OCTAL_RADIX
			? character is >= _FIRST_OCTAL_DIGIT and <= _LAST_OCTAL_DIGIT
			: char.IsAsciiHexDigit(character);
	}

	private static string DecodeControl(char character)
	{
		int code = character == _CONTROL_QUESTION_MARK ? _DELETE_CHARACTER : character & _CONTROL_MASK;

		return char.ConvertFromUtf32(code);
	}
}
