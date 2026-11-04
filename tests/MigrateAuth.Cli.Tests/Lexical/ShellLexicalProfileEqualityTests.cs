using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE SHIPPED `Shell` PROFILE IS PINNED to <see cref="ShellShapedProfile.BuildWithExecutedStrings"/>, the reference profile the real-shaped text tests use: the same rules in the same order, field by field (expressions by pattern text, options and timeout), the same arithmetic context, code
/// escape, arm blocks and executed-string declaration, the context predicates compared by what they answer, and the same masks on the same text. The reference and the shipped profile carry the same comment predecessors (an angle bracket is no comment predecessor).
/// </summary>
public sealed class ShellLexicalProfileEqualityTests
{
	private static readonly LexicalProfile _shipped = ShellRegisteredExpressions.Analyzer.LexicalProfile;

	private static readonly LexicalProfile _reference = ShellShapedProfile.BuildWithExecutedStrings();

	[Fact]
	public void TheShippedProfileDeclaresTheCaseSensitivitiesOfTheReference()
	{
		Assert.Equal(_reference.KeywordCaseSensitivity, _shipped.KeywordCaseSensitivity);
		Assert.Equal(_reference.DelimiterCaseSensitivity, _shipped.DelimiterCaseSensitivity);
		Assert.Equal(_reference.IdentifierCaseSensitivity, _shipped.IdentifierCaseSensitivity);
		Assert.Equal(CaseSensitivity.Sensitive, _shipped.KeywordCaseSensitivity);
	}

	[Fact]
	public void TheShippedProfileDeclaresTheCodeEscapeTheArithmeticContextAndTheExecutedStringsOfTheReference()
	{
		Assert.Equal('\\', _shipped.CodeEscapeCharacter);
		Assert.Equal(_reference.CodeEscapeCharacter, _shipped.CodeEscapeCharacter);
		Assert.Equal(_reference.ArithmeticContext, _shipped.ArithmeticContext);
		Assert.NotNull(_shipped.ExecutedStrings);
		AssertDeepEqual(_reference.ExecutedStrings, _shipped.ExecutedStrings, nameof(LexicalProfile.ExecutedStrings));
	}

	[Fact]
	public void TheShippedLineCommentRulesEqualTheReferenceOnesInOrder()
	{
		AssertDeepEqual(_reference.LineCommentRulesInEngineOrder, _shipped.LineCommentRulesInEngineOrder, nameof(LexicalProfile.LineCommentRulesInEngineOrder));
		Assert.Single(_shipped.LineCommentRulesInEngineOrder);
		Assert.Empty(_reference.BlockCommentRulesInEngineOrder);
		Assert.Empty(_shipped.BlockCommentRulesInEngineOrder);
	}

	[Fact]
	public void TheShippedQuoteRulesEqualTheReferenceOnesInOrderFieldByField()
	{
		AssertDeepEqual(_reference.QuoteRulesInEngineOrder, _shipped.QuoteRulesInEngineOrder, nameof(LexicalProfile.QuoteRulesInEngineOrder));
		Assert.Equal(4, _shipped.QuoteRulesInEngineOrder.Length);
	}

	[Fact]
	public void TheShippedHereDocumentRulesCloseOnAWholeLineAndOnAWholeLineAfterLeadingTabsAndBothStartTheBodyAfterTheOpeningLine()
	{
		QuoteRule plain = _shipped.QuoteRulesInEngineOrder.Single(rule => rule.Opening == "<<");
		QuoteRule tabStripping = _shipped.QuoteRulesInEngineOrder.Single(rule => rule.Opening == "<<-");

		Assert.Equal(PositionalAnchoring.WholeLine, plain.ClosingAnchoring);
		Assert.Equal(PositionalAnchoring.WholeLineAfterLeadingTabs, tabStripping.ClosingAnchoring);
		Assert.Equal(HereDocumentBodyStart.AfterTheOpeningLine, plain.CapturedTerminator!.BodyStart);
		Assert.Equal(HereDocumentBodyStart.AfterTheOpeningLine, tabStripping.CapturedTerminator!.BodyStart);
		Assert.StartsWith(@"\G", plain.CapturedTerminator.OpeningExpression.ToString(), StringComparison.Ordinal);
		Assert.StartsWith(@"\G", tabStripping.CapturedTerminator.OpeningExpression.ToString(), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("# note", 0, true)]
	[InlineData("a # note", 2, true)]
	[InlineData("a\t# note", 2, true)]
	[InlineData("a\n# note", 2, true)]
	[InlineData("a;# note", 2, true)]
	[InlineData("a&# note", 2, true)]
	[InlineData("a|# note", 2, true)]
	[InlineData("(#i)*.sh", 1, false)]
	[InlineData("( # note", 2, true)]
	[InlineData("a)# note", 2, true)]
	[InlineData("a>#w", 2, false)]
	[InlineData("a<#w", 2, false)]
	[InlineData("a >#w", 3, false)]
	[InlineData("a <#w", 3, false)]
	[InlineData("a#b", 1, false)]
	[InlineData("${#v}", 2, false)]
	[InlineData("$#", 1, false)]
	[InlineData("16#ff", 2, false)]
	[InlineData("a\\ #b", 3, false)]
	[InlineData("a\\;#b", 3, false)]
	[InlineData("a\\\n#b", 3, false)]
	[InlineData("a \\\n#b", 4, true)]
	public void TheCommentPredicateAnswersAsTheReferenceDoes(string text, int position, bool expected)
	{
		LineCommentRule shipped = _shipped.LineCommentRulesInEngineOrder.Single();
		LineCommentRule reference = _reference.LineCommentRulesInEngineOrder.Single();

		Assert.Equal('#', text[position]);
		Assert.Equal(expected, reference.OpeningContextPredicate!(text, position));
		Assert.Equal(expected, shipped.OpeningContextPredicate!(text, position));
	}

	[Theory]
	[InlineData("echo 'a'", 5, "'", true)]
	[InlineData("echo \\'a'", 6, "'", false)]
	[InlineData("echo \\\\'a'", 7, "'", true)]
	[InlineData("echo \"a\"", 5, "\"", true)]
	[InlineData("echo \\\"a\"", 6, "\"", false)]
	public void TheQuotePredicateAnswersAsTheReferenceDoes(string text, int position, string opening, bool expected)
	{
		QuoteRule shipped = _shipped.QuoteRulesInEngineOrder.Single(rule => rule.Opening == opening);
		QuoteRule reference = _reference.QuoteRulesInEngineOrder.Single(rule => rule.Opening == opening);

		Assert.Equal(opening[0], text[position]);
		Assert.Equal(expected, reference.OpeningContextPredicate!(text, position));
		Assert.Equal(expected, shipped.OpeningContextPredicate!(text, position));
	}

	[Theory]
	[InlineData("cat <<EOF", 4, true)]
	[InlineData("cat <<<EOF", 5, false)]
	[InlineData("<<EOF", 0, true)]
	[InlineData("cat <<<<EOF", 6, false)]
	[InlineData("let v=1<<3", 7, true)]
	public void TheHereDocumentPredicateAnswersAsTheReferenceDoes(string text, int position, bool expected)
	{
		foreach (string opening in new[] { "<<", "<<-" })
		{
			QuoteRule shipped = _shipped.QuoteRulesInEngineOrder.Single(rule => rule.Opening == opening);
			QuoteRule reference = _reference.QuoteRulesInEngineOrder.Single(rule => rule.Opening == opening);

			Assert.Equal(expected, reference.OpeningContextPredicate!(text, position));
			Assert.Equal(expected, shipped.OpeningContextPredicate!(text, position));
		}
	}

	[Theory]
	[InlineData("# a comment \"quoted\"\nx='single'\ny=\"double $(echo \"inner\") tail\"\nz=$'ansi\\'s'\n")]
	[InlineData("cat <<EOF\nbody 'not a quote' # not a comment\nEOF\nafter=1\n")]
	[InlineData("cat <<-'EOF'\n\tbody $(not a hole)\n\tEOF\nafter=1\n")]
	[InlineData("cat <<A <<B\nfirst\nA\nsecond\nB\nafter=1\n")]
	[InlineData("cat <<<'here string' <<EOF\nbody\nEOF\nafter=1\n")]
	[InlineData("echo $(( 1 << 3 )) <<EOF\nbody\nEOF\nafter=1\n")]
	[InlineData("(( x = 1 << 3 ))\ncat <<EOF\nbody\nEOF\nafter=1\n")]
	[InlineData("echo \"$(case $x in a) echo 1;; esac)\" # tail\nafter=1\n")]
	[InlineData("echo x >#w\necho y <#w\nafter=1\n")]
	[InlineData("echo a\\ #b\necho c # d\nafter=1\n")]
	[InlineData("bash -c 'echo \"inner\" # note'\nsudo -u root sh -c \"curl -u a:b http://h.test/\"\nafter=1\n")]
	[InlineData("let v=1<<3\nbody\n3\nafter=1\n")]
	[InlineData("echo \"unterminated\nstill inside\n")]
	public void TheShippedProfileAndTheReferenceProduceTheSameMask(string source)
	{
		ZoneMask referenceMask = ShellShapedProfile.ScanWithExecutedStrings(source);
		ZoneMask shippedMask = new LexicalScanner(_shipped).Scan(source);

		Assert.Equal(referenceMask.IsCompletelyValid, shippedMask.IsCompletelyValid);
		Assert.Equal(referenceMask.Length, shippedMask.Length);

		for (int position = 0; position < source.Length; position++)
		{
			Assert.Equal(referenceMask.ZoneAt(position), shippedMask.ZoneAt(position));
		}
	}

	[Theory]
	[InlineData("A=1", "A", "1")]
	[InlineData("readonly B=two", "B", "two")]
	[InlineData("export C='three four'", "C", "three four")]
	[InlineData("declare -r E=\"five\"", "E", "five")]
	[InlineData("typeset -x F=6", "F", "6")]
	public void TheShippedConstantDeclarationReadsTheNameAndTheValueOfTheReference(string line, string name, string value)
	{
		Match shipped = _shipped.ConstantDeclarationExpression.Match(line);
		Match reference = _reference.ConstantDeclarationExpression.Match(line);

		Assert.True(shipped.Success);
		Assert.Equal(reference.Groups["name"].Value, shipped.Groups["name"].Value);
		Assert.Equal(reference.Groups["value"].Value, shipped.Groups["value"].Value);
		Assert.Equal(name, shipped.Groups["name"].Value);
		Assert.Equal(value, shipped.Groups["value"].Value);
	}

	[Theory]
	[InlineData("H=$HOME")]
	[InlineData("I=$(cmd)")]
	[InlineData("echo J=1")]
	[InlineData("K=\"has $expansion\"")]
	public void TheShippedConstantDeclarationDeclinesWhatTheReferenceDeclines(string line)
	{
		Assert.DoesNotMatch(_reference.ConstantDeclarationExpression, line);
		Assert.DoesNotMatch(_shipped.ConstantDeclarationExpression, line);
	}

	[Fact]
	public void TheShippedProfileCarriesTheLexicalAndRecognitionTimeoutsOfThePolicy()
	{
		Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, _shipped.ConstantDeclarationExpression.MatchTimeout);

		foreach (QuoteRule rule in _shipped.QuoteRulesInEngineOrder.Where(candidate => candidate.CapturedTerminator is not null))
		{
			Assert.Equal(LexicalEnginePolicy.LEXICAL_BUDGET, rule.CapturedTerminator!.OpeningExpression.MatchTimeout);
		}
	}

	/// <summary>
	/// Compares two declarations field by field through their public properties: an expression by its pattern text, options and timeout, a sequence element by
	/// element, a nested declaration recursively. A context predicate is a delegate and is compared by what it answers, in the tests above, not here.
	/// </summary>
	private static void AssertDeepEqual(object? expected, object? actual, string path)
	{
		if (expected is Regex expectedExpression)
		{
			Regex actualExpression = Assert.IsType<Regex>(actual, exactMatch: false);

			Assert.True(expectedExpression.ToString() == actualExpression.ToString(), $"{path}: the expression text differs.");
			Assert.True(expectedExpression.Options == actualExpression.Options, $"{path}: the expression options differ.");
			Assert.True(expectedExpression.MatchTimeout == actualExpression.MatchTimeout, $"{path}: the expression timeout differs.");
		}
		else if (expected is Delegate)
		{
			Assert.True(actual is Delegate, $"{path}: a predicate is declared on one side only.");
		}
		else if (expected is IEnumerable expectedSequence && expected is not string)
		{
			List<object?> expectedItems = [.. expectedSequence.Cast<object?>()];
			List<object?> actualItems = [.. ((IEnumerable)actual!).Cast<object?>()];

			Assert.True(expectedItems.Count == actualItems.Count, $"{path}: {expectedItems.Count} elements expected, {actualItems.Count} found.");

			for (int index = 0; index < expectedItems.Count; index++)
			{
				AssertDeepEqual(expectedItems[index], actualItems[index], $"{path}[{index}]");
			}
		}
		else if (expected is null || expected.GetType().IsPrimitive || expected is string || expected.GetType().IsEnum)
		{
			Assert.True(Equals(expected, actual), $"{path}: '{expected}' expected, '{actual}' found.");
		}
		else
		{
			Assert.NotNull(actual);

			foreach (PropertyInfo property in expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(candidate => candidate.GetIndexParameters().Length == 0 && candidate.Name != "EqualityContract"))
			{
				AssertDeepEqual(property.GetValue(expected), property.GetValue(actual), $"{path}.{property.Name}");
			}
		}
	}
}
