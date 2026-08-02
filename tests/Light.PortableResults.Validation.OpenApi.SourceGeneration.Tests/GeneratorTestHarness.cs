using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Light.PortableResults.AspNetCore.OpenApi;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.OpenApi;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration.Tests;

internal static class GeneratorTestHarness
{
    public static readonly CSharpParseOptions ParseOptions =
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    public static GeneratorTestResult Run(string source, string path = "Validator.cs") =>
        Run((path, source));

    public static GeneratorTestResult Run(params (string Path, string Source)[] sources)
    {
        return Run(CreateCompilation(sources));
    }

    public static GeneratorTestResult Run(CSharpCompilation compilation)
    {
        var driver = CreateDriver().RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics
        );
        var runResult = driver.GetRunResult();
        var diagnostics = outputCompilation.GetDiagnostics()
           .Concat(generatorDiagnostics)
           .Concat(runResult.Diagnostics)
           .ToImmutableArray();
        var generatedSources = runResult.Results
           .SelectMany(static result => result.GeneratedSources)
           .Select(static generatedSource => generatedSource.SourceText.ToString())
           .ToImmutableArray();
        var resultDiagnostics = runResult.Results
           .SelectMany(static result => result.Diagnostics)
           .ToImmutableArray();
        return new GeneratorTestResult(diagnostics, resultDiagnostics, generatedSources);
    }

    public static CSharpCompilation CreateCompilation(params (string Path, string Source)[] sources)
    {
        var syntaxTrees = sources.Select(
            static source => CSharpSyntaxTree.ParseText(source.Source, ParseOptions, source.Path)
        );
        return CSharpCompilation.Create(
            "GeneratorTests",
            syntaxTrees,
            CreateMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    public static GeneratorDriver CreateDriver() =>
        CSharpGeneratorDriver.Create(new PortableValidationOpenApiGenerator())
           .WithUpdatedParseOptions(ParseOptions);

    private static MetadataReference[] CreateMetadataReferences()
    {
        var trustedPlatformAssemblies = ((string?) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
           .Split(Path.PathSeparator);
        var references = new HashSet<string>(trustedPlatformAssemblies, StringComparer.OrdinalIgnoreCase);
        AddAssembly(references, typeof(ValidationContext).Assembly);
        AddAssembly(references, typeof(GeneratePortableValidationOpenApiAttribute).Assembly);
        AddAssembly(references, typeof(PortableOpenApiSchemaTypeMapper).Assembly);
        AddAssembly(references, typeof(OpenApiSchema).Assembly);
        AddAssembly(references, typeof(PortableValidationOpenApiGenerator).Assembly);
        return references.Select(static path => MetadataReference.CreateFromFile(path)).ToArray<MetadataReference>();
    }

    private static void AddAssembly(ISet<string> references, Assembly assembly)
    {
        if (!string.IsNullOrWhiteSpace(assembly.Location))
        {
            references.Add(assembly.Location);
        }
    }
}

internal sealed class GeneratorTestResult
{
    public GeneratorTestResult(
        ImmutableArray<Diagnostic> diagnostics,
        ImmutableArray<Diagnostic> resultDiagnostics,
        ImmutableArray<string> generatedSources
    )
    {
        Diagnostics = diagnostics;
        ResultDiagnostics = resultDiagnostics;
        GeneratedSources = generatedSources;
    }

    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public ImmutableArray<Diagnostic> ResultDiagnostics { get; }
    public ImmutableArray<string> GeneratedSources { get; }
}
