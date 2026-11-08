using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration.Tests;

public sealed class DiagnosticDescriptorsTests
{
    private const string HelpLinkBase = "https://github.com/feO2x/Light.PortableResults/blob/main/docs/openapi.md#";

    private static readonly DiagnosticDescriptor[] AllDescriptors =
        typeof(DiagnosticDescriptors)
           .GetFields(BindingFlags.Public | BindingFlags.Static)
           .Where(static field => field.FieldType == typeof(DiagnosticDescriptor))
           .Select(static field => (DiagnosticDescriptor) field.GetValue(null)!)
           .OrderBy(static descriptor => descriptor.Id, StringComparer.Ordinal)
           .ToArray();

    [Fact]
    public void Descriptors_ShouldCoverLprsg0001ToLprsg0016()
    {
        var expectedIds = Enumerable.Range(1, 16).Select(static number => $"LPRSG{number:D4}");

        AllDescriptors.Select(static descriptor => descriptor.Id).Should().Equal(expectedIds);
    }

    [Fact]
    public void EveryDescriptor_ShouldLinkToItsAnchorInTheOpenApiDocumentation()
    {
        foreach (var descriptor in AllDescriptors)
        {
            descriptor.HelpLinkUri.Should().Be(
                HelpLinkBase + descriptor.Id.ToLowerInvariant(),
                "{0} must link to its own section in docs/openapi.md",
                descriptor.Id
            );
        }
    }

    [Fact]
    public void OpenApiDocumentation_ShouldDeclareAnExplicitAnchorForEveryDescriptor()
    {
        var documentation = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", "openapi.md"));

        foreach (var descriptor in AllDescriptors)
        {
            documentation.Should().Contain(
                $"<a id=\"{descriptor.Id.ToLowerInvariant()}\"></a>",
                "the help link of {0} points to this anchor",
                descriptor.Id
            );
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Light.PortableResults.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ??
               throw new InvalidOperationException("Could not find the repository root containing Light.PortableResults.slnx.");
    }
}
