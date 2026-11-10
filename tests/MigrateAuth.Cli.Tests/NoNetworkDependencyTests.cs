using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Xunit;

namespace MigrateAuth.Cli.Tests;

/// <summary>
/// The binary makes no outbound network call and emits no telemetry. Discipline is
/// not a mechanism, so this reads the compiled CLI assembly's metadata directly and fails
/// the build the moment a networking assembly or type reference appears in it. It catches
/// the reference at the point it is introduced rather than at the point it is noticed.
/// </summary>
public sealed class NoNetworkDependencyTests
{
	private const string _CLI_ASSEMBLY_FILE_NAME = "migrate-auth.dll";
	private const string _NETWORKING_NAMESPACE_PREFIX = "System.Net";
	private const string _NETWORKING_ASSEMBLY_PREFIX = "System.Net";
	private const string _NAMESPACE_QUALIFIER_SEPARATOR = ".";

	private static string CliAssemblyPath => Path.Combine(AppContext.BaseDirectory, _CLI_ASSEMBLY_FILE_NAME);

	[Fact]
	public void CompiledCliAssemblyReferencesNoNetworkingAssembly()
	{
		IReadOnlyList<string> networkingAssemblies = ReadReferencedNames(
			ReadNetworkingAssemblyReferences);

		Assert.Empty(networkingAssemblies);
	}

	[Fact]
	public void CompiledCliAssemblyReferencesNoNetworkingType()
	{
		IReadOnlyList<string> networkingTypes = ReadReferencedNames(
			ReadNetworkingTypeReferences);

		Assert.Empty(networkingTypes);
	}

	private static IReadOnlyList<string> ReadReferencedNames(
		Func<MetadataReader, IReadOnlyList<string>> read)
	{
		using FileStream assemblyStream = File.OpenRead(CliAssemblyPath);
		using PEReader portableExecutableReader = new(assemblyStream);
		MetadataReader metadataReader = portableExecutableReader.GetMetadataReader();
		IReadOnlyList<string> referencedNames = read(metadataReader);

		return referencedNames;
	}

	private static IReadOnlyList<string> ReadNetworkingAssemblyReferences(MetadataReader metadataReader)
	{
		List<string> networkingAssemblies = [];

		foreach (AssemblyReferenceHandle referenceHandle in metadataReader.AssemblyReferences)
		{
			AssemblyReference reference = metadataReader.GetAssemblyReference(referenceHandle);
			string referencedAssemblyName = metadataReader.GetString(reference.Name);

			if (IsNetworkingName(referencedAssemblyName, _NETWORKING_ASSEMBLY_PREFIX))
			{
				networkingAssemblies.Add(referencedAssemblyName);
			}
		}

		return networkingAssemblies;
	}

	private static IReadOnlyList<string> ReadNetworkingTypeReferences(MetadataReader metadataReader)
	{
		List<string> networkingTypes = [];
		ImmutableArray<TypeReferenceHandle> typeReferenceHandles =
			[.. metadataReader.TypeReferences];

		foreach (TypeReferenceHandle referenceHandle in typeReferenceHandles)
		{
			TypeReference reference = metadataReader.GetTypeReference(referenceHandle);
			string referencedNamespace = reference.Namespace.IsNil
				? string.Empty
				: metadataReader.GetString(reference.Namespace);

			if (IsNetworkingName(referencedNamespace, _NETWORKING_NAMESPACE_PREFIX))
			{
				networkingTypes.Add($"{referencedNamespace}{_NAMESPACE_QUALIFIER_SEPARATOR}{metadataReader.GetString(reference.Name)}");
			}
		}

		return networkingTypes;
	}

	private static bool IsNetworkingName(string candidate, string prefix)
	{
		bool isNetworking =
			string.Equals(candidate, prefix, StringComparison.Ordinal)
			|| candidate.StartsWith($"{prefix}{_NAMESPACE_QUALIFIER_SEPARATOR}", StringComparison.Ordinal);

		return isNetworking;
	}
}
