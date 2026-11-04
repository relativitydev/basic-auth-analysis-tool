using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE INTERNAL HOOKS THE `TypeScript` ANALYZER PUBLISHES FOR ITS OWN TESTS, read from the shipped assembly by name. The assembly grants its tests no access to its internals, so each hook is
/// reached by reflection and the lookup fails loudly when a member is renamed or removed: a test never silently passes because a hook went missing.
/// </summary>
internal static class TypeScriptInternalHooks
{
	private const string _LANGUAGES_NAMESPACE = "MigrateAuth.Cli.Catalog.Languages";

	/// <summary>The identifiers of the inherited `JavaScript` classes the `TypeScript` analyzer takes, the explicit allow-list.</summary>
	internal static FrozenSet<string> InheritedClassAllowList => (FrozenSet<string>)Read("TypeScriptRuleDeclarations", "InheritedClassAllowList");

	/// <summary>The start of the construct text of each `JavaScript` declared entry that does not apply to `TypeScript` text.</summary>
	internal static ImmutableArray<string> JavaScriptOnlyConstructStarts => (ImmutableArray<string>)Read("TypeScriptDeclaredCoverage", "JavaScriptOnlyConstructStarts");

	/// <summary>The `JavaScript` entries whose wording changes for `TypeScript`, by the start of their construct text, with the entry `TypeScript` carries in their place.</summary>
	internal static ImmutableArray<(string JavaScriptConstructStart, DeclaredUnsupportedCoverage Replacement)> AdaptedEntries =>
		(ImmutableArray<(string JavaScriptConstructStart, DeclaredUnsupportedCoverage Replacement)>)Read("TypeScriptDeclaredCoverage", "AdaptedEntries");

	/// <summary>The shared `JavaScript` constants the `TypeScript` expressions are built from, as the compile-time list names them.</summary>
	internal static ImmutableArray<string> ReliedOnSharedConstants => (ImmutableArray<string>)Read("JavaScriptPatternExpressions", "ReliedOnSharedConstants");

	private static object Read(string typeName, string propertyName)
	{
		Type type = typeof(EmbeddedLanguageAnalyzers).Assembly.GetType($"{_LANGUAGES_NAMESPACE}.{typeName}", throwOnError: true)!;
		PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
			?? throw new MissingMemberException(type.FullName, propertyName);

		return property.GetValue(null) ?? throw new InvalidOperationException($"{typeName}.{propertyName} returned nothing.");
	}
}
