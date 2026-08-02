using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration.Tests;

public sealed class BoundaryMetadataIncrementalTests
{
    [Fact]
    public void GeneratorMovesUnresolvedArgumentDiagnosticWhenDriverIsReused()
    {
        var firstCompilation = GeneratorTestHarness.CreateCompilation(
            ("Validator.cs", CreateSource(extraBlankLine: false))
        );
        var driver = GeneratorTestHarness.CreateDriver().RunGenerators(
            firstCompilation,
            TestContext.Current.CancellationToken
        );
        var firstDiagnostic = GetReconstructionDiagnostic(driver);

        var secondCompilation = GeneratorTestHarness.CreateCompilation(
            ("Validator.cs", CreateSource(extraBlankLine: true))
        );
        driver = driver.RunGenerators(secondCompilation, TestContext.Current.CancellationToken);
        var secondDiagnostic = GetReconstructionDiagnostic(driver);

        secondDiagnostic.Location.SourceSpan.Start.Should().BeGreaterThan(firstDiagnostic.Location.SourceSpan.Start);
        secondDiagnostic.Location.GetLineSpan().StartLinePosition.Line.Should()
           .Be(firstDiagnostic.Location.GetLineSpan().StartLinePosition.Line + 1);
        secondDiagnostic.Location.SourceTree!.GetText(TestContext.Current.CancellationToken)
           .GetSubText(secondDiagnostic.Location.SourceSpan)
           .ToString()
           .Should()
           .Be("DateTime.Now");
    }

    private static Diagnostic GetReconstructionDiagnostic(GeneratorDriver driver) =>
        driver.GetRunResult().Results
           .SelectMany(static result => result.Diagnostics)
           .Single(static diagnostic => diagnostic.Id == "LPRSG0015");

    private static string CreateSource(bool extraBlankLine)
    {
        var blankLine = extraBlankLine ? "\n" : string.Empty;
        return $$"""
                 using System;
                 using Light.PortableResults.Validation;
                 using Light.PortableResults.Validation.OpenApi;

                 [GeneratePortableValidationOpenApi]
                 public sealed partial class BoundaryValidator : Validator<DateTime>
                 {
                     public BoundaryValidator(IValidationContextFactory validationContextFactory)
                         : base(validationContextFactory) { }

                     protected override ValidatedValue<DateTime> PerformValidation(
                         ValidationContext context,
                         ValidationCheckpoint checkpoint,
                         DateTime value
                     )
                     {
                         {{blankLine}}context.Check(value).IsEqualTo(DateTime.Now);
                         return checkpoint.ToValidatedValue(value);
                     }
                 }
                 """;
    }
}
