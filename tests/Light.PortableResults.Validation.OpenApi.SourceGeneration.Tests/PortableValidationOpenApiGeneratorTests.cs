using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Light.PortableResults.AspNetCore.OpenApi;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration.Tests;

public sealed class PortableValidationOpenApiGeneratorTests
{
    [Fact]
    public void Generator_ShouldEmitContractForSupportedValidator()
    {
        var result = RunGenerator(
            """
            using System;
            using Light.PortableResults.Validation;
            using Light.PortableResults.Validation.OpenApi;

            namespace TestApp;

            public sealed class RatingDto
            {
                public Guid Id { get; init; }
                public string Comment { get; set; } = "";
                public int Rating { get; init; }
            }

            [GeneratePortableValidationOpenApi]
            public sealed partial class RatingValidator : Validator<RatingDto>
            {
                public RatingValidator(IValidationContextFactory validationContextFactory)
                    : base(validationContextFactory) { }

                protected override ValidatedValue<RatingDto> PerformValidation(
                    ValidationContext context,
                    ValidationCheckpoint checkpoint,
                    RatingDto dto
                )
                {
                    context.Check(dto.Id).IsNotEmpty();
                    dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
                    context.Check(dto.Rating).IsInRange(1, 5);
                    return checkpoint.ToValidatedValue(dto);
                }
            }
            """
        );

        result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           .Should()
           .BeEmpty();
        var source = result.GeneratedSources.Single();
        source.Should().Contain("partial class RatingValidator : IPortableValidationOpenApiContract");
        source.Should().Contain("builder.WithErrorCodes(\"LengthInRange\", \"NotEmpty\");");
        source.Should().Contain("builder.WithInRangeError<global::System.Int32>();");
        source.Should().Contain("builder.WithErrorExample(\"NotEmpty\", \"id\");");
        source.Should().Contain("[\"minLength\"] = 10");
        source.Should().Contain("[\"upperBoundary\"] = 5");
    }

    [Fact]
    public void Generator_ShouldReportDiagnosticForNonPartialValidator()
    {
        var result = RunGenerator(
            """
            using Light.PortableResults.Validation;
            using Light.PortableResults.Validation.OpenApi;

            [GeneratePortableValidationOpenApi]
            public sealed class NonPartialValidator : Validator<string>
            {
                public NonPartialValidator(IValidationContextFactory validationContextFactory)
                    : base(validationContextFactory) { }

                protected override ValidatedValue<string> PerformValidation(
                    ValidationContext context,
                    ValidationCheckpoint checkpoint,
                    string value
                ) => checkpoint.ToValidatedValue(value);
            }
            """
        );

        result.Diagnostics.Select(static diagnostic => diagnostic.Id).Should().Contain("LPRSG0001");
        result.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void Generator_ShouldWarnForNestedChecksAndOpaqueMust()
    {
        var result = RunGenerator(
            """
            using Light.PortableResults.Validation;
            using Light.PortableResults.Validation.OpenApi;

            [GeneratePortableValidationOpenApi]
            public sealed partial class OpaqueValidator : Validator<string>
            {
                public OpaqueValidator(IValidationContextFactory validationContextFactory)
                    : base(validationContextFactory) { }

                protected override ValidatedValue<string> PerformValidation(
                    ValidationContext context,
                    ValidationCheckpoint checkpoint,
                    string value
                )
                {
                    if (value.Length > 0)
                    {
                        context.Check(value).IsNotEmpty();
                    }

                    context.Check(value).Must(static x => x.Length > 0);
                    return checkpoint.ToValidatedValue(value);
                }
            }
            """
        );

        result.Diagnostics.Select(static diagnostic => diagnostic.Id)
           .Should()
           .Contain(["LPRSG0005", "LPRSG0006"]);
    }

    [Fact]
    public void Generator_ShouldEmitInlineSchemaForAnnotatedCustomRule()
    {
        var result = RunGenerator(
            """
            using Light.PortableResults.Validation;
            using Light.PortableResults.Validation.Definitions;
            using Light.PortableResults.Validation.Messaging;
            using Light.PortableResults.Validation.OpenApi;

            [GeneratePortableValidationOpenApi]
            public sealed partial class CustomRuleValidator : Validator<int>
            {
                public CustomRuleValidator(IValidationContextFactory validationContextFactory)
                    : base(validationContextFactory) { }

                protected override ValidatedValue<int> PerformValidation(
                    ValidationContext context,
                    ValidationCheckpoint checkpoint,
                    int value
                )
                {
                    context.Check(value).IsDivisibleBy(3);
                    return checkpoint.ToValidatedValue(value);
                }
            }

            public static class CustomRuleChecks
            {
                [ValidationRule("DivisibleBy", ErrorDefinitionType = typeof(DivisibleByDefinition))]
                [ValidationRuleMetadata("divisor", "divisor")]
                public static Check<int> IsDivisibleBy(this Check<int> check, int divisor) => check;
            }

            [ValidationErrorContract("DivisibleBy")]
            [ValidationErrorMetadataContract("divisor", typeof(int))]
            public sealed class DivisibleByDefinition : ValidationErrorDefinition
            {
                public override ValidationErrorMessage ProvideMessage<T>(
                    in ValidationErrorMessageContext<T> context
                ) => new("The value must be divisible by the configured divisor.");
            }
            """
        );

        result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           .Should()
           .BeEmpty();
        var source = result.GeneratedSources.Single();
        source.Should().Contain("builder.WithErrorMetadata(\"DivisibleBy\", _ => new OpenApiSchema");
        source.Should().Contain("[\"divisor\"] = PortableOpenApiSchemaTypeMapper.Map<global::System.Int32>()");
        source.Should().Contain(
            "builder.WithErrorExample(\"DivisibleBy\", null, new Dictionary<string, object?>(StringComparer.Ordinal) { [\"divisor\"] = 3 });"
        );
    }

    [Fact]
    public void Generator_ShouldApplyMethodLevelErrorHints()
    {
        var result = RunGenerator(
            """
            using Light.PortableResults.Validation;
            using Light.PortableResults.Validation.OpenApi;

            [GeneratePortableValidationOpenApi]
            public sealed partial class MethodHintValidator : Validator<string>
            {
                public MethodHintValidator(IValidationContextFactory validationContextFactory)
                    : base(validationContextFactory) { }

                [PortableValidationOpenApiErrorHint("CustomCode")]
                protected override ValidatedValue<string> PerformValidation(
                    ValidationContext context,
                    ValidationCheckpoint checkpoint,
                    string value
                ) => checkpoint.ToValidatedValue(value);
            }
            """
        );

        result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           .Should()
           .BeEmpty();
        result.GeneratedSources.Single().Should().Contain("builder.WithErrorCodes(\"CustomCode\");");
    }

    [Fact]
    public void Generator_ShouldEmitInlineSchemaAndExampleHints()
    {
        var result = RunGenerator(
            """
            using Light.PortableResults.Validation;
            using Light.PortableResults.Validation.OpenApi;

            [GeneratePortableValidationOpenApi]
            [PortableValidationOpenApiErrorHint("RatingTooLow")]
            [PortableValidationOpenApiErrorMetadataProperty("RatingTooLow", "lowerBoundary", typeof(int))]
            [PortableValidationOpenApiErrorMetadataProperty("RatingTooLow", "upperBoundary", typeof(int))]
            [PortableValidationOpenApiExampleHint("RatingTooLow", Target = "rating")]
            [PortableValidationOpenApiExampleMetadata("RatingTooLow", "lowerBoundary", 1)]
            [PortableValidationOpenApiExampleMetadata("RatingTooLow", "upperBoundary", 5)]
            public sealed partial class InlineHintValidator : Validator<int>
            {
                public InlineHintValidator(IValidationContextFactory validationContextFactory)
                    : base(validationContextFactory) { }

                protected override ValidatedValue<int> PerformValidation(
                    ValidationContext context,
                    ValidationCheckpoint checkpoint,
                    int value
                ) => checkpoint.ToValidatedValue(value);
            }
            """
        );

        result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           .Should()
           .BeEmpty();
        var source = result.GeneratedSources.Single();
        source.Should().Contain("builder.WithErrorMetadata(\"RatingTooLow\", _ => new OpenApiSchema");
        source.Should().Contain("[\"lowerBoundary\"] = PortableOpenApiSchemaTypeMapper.Map<global::System.Int32>()");
        source.Should().Contain("[\"upperBoundary\"] = PortableOpenApiSchemaTypeMapper.Map<global::System.Int32>()");
        source.Should().Contain(
            "builder.WithErrorExample(\"RatingTooLow\", \"rating\", new Dictionary<string, object?>(StringComparer.Ordinal) { [\"lowerBoundary\"] = 1, [\"upperBoundary\"] = 5 });"
        );
        source.Should().NotContain("builder.AllowUnknownErrorCodes();");
    }

    [Fact]
    public void Generator_ShouldTreatExampleOnlyHintsAsCodeOnlySchemaHints()
    {
        var result = RunGenerator(
            """
            using Light.PortableResults.Validation;
            using Light.PortableResults.Validation.OpenApi;

            [GeneratePortableValidationOpenApi]
            public sealed partial class ExampleOnlyHintValidator : Validator<string>
            {
                public ExampleOnlyHintValidator(IValidationContextFactory validationContextFactory)
                    : base(validationContextFactory) { }

                [PortableValidationOpenApiExampleHint("CustomCode", Target = "name")]
                protected override ValidatedValue<string> PerformValidation(
                    ValidationContext context,
                    ValidationCheckpoint checkpoint,
                    string value
                ) => checkpoint.ToValidatedValue(value);
            }
            """
        );

        result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           .Should()
           .BeEmpty();
        var source = result.GeneratedSources.Single();
        source.Should().Contain("builder.WithErrorCodes(\"CustomCode\");");
        source.Should().Contain("builder.WithErrorExample(\"CustomCode\", \"name\");");
    }

    [Fact]
    public void Generator_ShouldEmitMetadataTypeHintAndDeduplicateMatchingCodeHints()
    {
        var result = RunGenerator(
            """
            using Light.PortableResults.Validation;
            using Light.PortableResults.Validation.OpenApi;

            public sealed class CustomMetadata
            {
                public int Value { get; init; }
            }

            [GeneratePortableValidationOpenApi]
            [PortableValidationOpenApiErrorHint("CustomCode", typeof(CustomMetadata))]
            public sealed partial class MetadataTypeHintValidator : Validator<string>
            {
                public MetadataTypeHintValidator(IValidationContextFactory validationContextFactory)
                    : base(validationContextFactory) { }

                [PortableValidationOpenApiErrorHint("CustomCode", typeof(CustomMetadata))]
                [PortableValidationOpenApiExampleHint("CustomCode", Target = "name")]
                protected override ValidatedValue<string> PerformValidation(
                    ValidationContext context,
                    ValidationCheckpoint checkpoint,
                    string value
                ) => checkpoint.ToValidatedValue(value);
            }
            """
        );

        result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           .Should()
           .BeEmpty();
        var source = result.GeneratedSources.Single();
        source.Should().Contain("builder.WithErrorMetadata<global::CustomMetadata>(\"CustomCode\");");
        source.Should().Contain("builder.WithErrorExample(\"CustomCode\", \"name\");");
        source.Should().NotContain("builder.WithErrorCodes(\"CustomCode\");");
    }

    [Fact]
    public void Generator_ShouldReportDiagnosticsForMalformedHints()
    {
        var result = RunGenerator(
            """
            using Light.PortableResults.Validation;
            using Light.PortableResults.Validation.OpenApi;

            public sealed class MetadataOne { }
            public sealed class MetadataTwo { }

            [GeneratePortableValidationOpenApi]
            [PortableValidationOpenApiErrorHint("CustomCode", typeof(MetadataOne))]
            [PortableValidationOpenApiErrorHint("CustomCode", typeof(MetadataTwo))]
            [PortableValidationOpenApiErrorMetadataProperty("OrphanCode", "value", typeof(int))]
            [PortableValidationOpenApiExampleMetadata("OtherCode", "value", 1)]
            public sealed partial class MalformedHintValidator : Validator<string>
            {
                public MalformedHintValidator(IValidationContextFactory validationContextFactory)
                    : base(validationContextFactory) { }

                protected override ValidatedValue<string> PerformValidation(
                    ValidationContext context,
                    ValidationCheckpoint checkpoint,
                    string value
                ) => checkpoint.ToValidatedValue(value);
            }
            """
        );

        result.Diagnostics.Select(static diagnostic => diagnostic.Id)
           .Should()
           .Contain(["LPRSG0010", "LPRSG0011"]);
    }

    private static GeneratorRunResult RunGenerator(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [syntaxTree],
            CreateMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        var generator = new PortableValidationOpenApiGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
           .WithUpdatedParseOptions(parseOptions)
           .RunGeneratorsAndUpdateCompilation(
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
           .Select(static sourceText => sourceText.SourceText.ToString())
           .ToImmutableArray();
        return new GeneratorRunResult(diagnostics, generatedSources);
    }

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
        return references.Select(static path => MetadataReference.CreateFromFile(path)).ToArray();
    }

    private static void AddAssembly(ISet<string> references, Assembly assembly)
    {
        if (!string.IsNullOrWhiteSpace(assembly.Location))
        {
            references.Add(assembly.Location);
        }
    }

    private sealed class GeneratorRunResult
    {
        public GeneratorRunResult(
            ImmutableArray<Diagnostic> diagnostics,
            ImmutableArray<string> generatedSources
        )
        {
            Diagnostics = diagnostics;
            GeneratedSources = generatedSources;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<string> GeneratedSources { get; }
    }
}
