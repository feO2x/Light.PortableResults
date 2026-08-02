using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration.Tests;

public sealed class BoundaryMetadataRejectionTests
{
    public static IEnumerable<object[]> OmittedBoundaryCases()
    {
        yield return ["DateTime", "DateTime.Now", "", ""];
        yield return ["DateTime", "DateTime.UtcNow", "", ""];
        yield return ["DateTime", "DateTime.Today", "", ""];
        yield return ["DateTimeOffset", "DateTimeOffset.Now", "", ""];
        yield return ["DateTimeOffset", "DateTimeOffset.UtcNow", "", ""];
        yield return ["Guid", "Guid.NewGuid()", "", ""];
        yield return ["DateTime", "DateTime.Parse(\"2026-01-02\")", "", ""];
        yield return
        [
            "DateTime",
            "DateTime.ParseExact(\"2026-01-02\", \"yyyy-MM-dd\", CultureInfo.InvariantCulture)",
            "",
            ""
        ];
        yield return ["DateTimeOffset", "DateTimeOffset.Parse(\"2026-01-02+02:00\")", "", ""];
        yield return
        [
            "DateTimeOffset",
            "DateTimeOffset.ParseExact(\"2026-01-02+02:00\", \"yyyy-MM-ddzzz\", CultureInfo.InvariantCulture)",
            "",
            ""
        ];
        yield return
        [
            "DateTime",
            "ParseWithTryParse()",
            "private static DateTime ParseWithTryParse() { DateTime.TryParse(\"2026-01-02\", out var value); return value; }",
            ""
        ];
        yield return
        [
            "DateTimeOffset",
            "ParseOffsetWithTryParse()",
            "private static DateTimeOffset ParseOffsetWithTryParse() { DateTimeOffset.TryParse(\"2026-01-02+02:00\", out var value); return value; }",
            ""
        ];
        yield return
        [
            "DateTimeOffset",
            "new DateTimeOffset(new DateTime(2026, 1, 2))",
            "",
            ""
        ];
        yield return
        [
            "DateTime",
            "new DateTime(2026, 1, 2, new GregorianCalendar())",
            "",
            ""
        ];
        yield return ["Guid", "new Guid(new byte[16])", "", ""];
        yield return ["DateTime", "default", "", ""];
        yield return ["DateTime", "default(DateTime)", "", ""];
        yield return ["DateTime", "value", "", ""];
        yield return
        [
            "DateTime",
            "BoundaryProperty",
            "private static DateTime BoundaryProperty => new (2026, 1, 2);",
            ""
        ];
        yield return
        [
            "DateTime",
            "_instanceBoundary",
            "private readonly DateTime _instanceBoundary = new (2026, 1, 2);",
            ""
        ];
        yield return
        [
            "DateTime",
            "MutableBoundary",
            "private static DateTime MutableBoundary = new (2026, 1, 2);",
            ""
        ];
        yield return ["DateTime", "DateTime.UnixEpoch.AddDays(1)", "", ""];
        yield return
        [
            "TimeSpan",
            "TimeSpan.FromHours(1, 2, 3, 4, 5)",
            "",
            ""
        ];
        yield return ["DateOnly", "DateOnly.FromDateTime(DateTime.UnixEpoch)", "", ""];
        yield return ["TimeOnly", "TimeOnly.FromDateTime(DateTime.UnixEpoch)", "", ""];
        yield return ["TimeOnly", "TimeOnly.FromTimeSpan(TimeSpan.Zero)", "", ""];
        yield return
        [
            "DateTime",
            "new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Local)",
            "",
            ""
        ];
    }

    [Theory]
    [MemberData(nameof(OmittedBoundaryCases))]
    public void GeneratorReportsInfoAtEveryOmittedBoundary(
        string valueType,
        string expression,
        string additionalMember,
        string additionalType
    )
    {
        AssertRejectedBoundary(valueType, expression, additionalMember, additionalType);
    }

    [Theory]
    [InlineData("DateTime", "new DateTime(2026, 13, 1)")]
    [InlineData("Guid", "Guid.Parse(\"invalid\")")]
    [InlineData("Uri", "new Uri(\"http://[\")")]
    [InlineData("Uri", "new Uri(\"items/42\")")]
    [InlineData("TimeSpan", "TimeSpan.FromDays(double.MaxValue)")]
    [InlineData("DateOnly", "DateOnly.FromDayNumber(-1)")]
    [InlineData("TimeOnly", "new TimeOnly(-1L)")]
    [InlineData("TimeSpan", "TimeSpan.FromMicroseconds(double.NaN)")]
    [InlineData("TimeSpan", "TimeSpan.FromMicroseconds(double.MaxValue)")]
    public void GeneratorDegradesWhenRecognizedEvaluationThrows(string valueType, string expression)
    {
        var result = AssertRejectedBoundary(valueType, expression);

        result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           .Should()
           .BeEmpty();
    }

    [Fact]
    public void GeneratorPropagatesCancellation()
    {
        var compilation = GeneratorTestHarness.CreateCompilation(
            (
                "Validator.cs",
                CreateValidatorSource("DateTime", "new DateTime(2026, 1, 2)")
            )
        );
        var cancellationToken = new CancellationToken(canceled: true);

        Action act = () => GeneratorTestHarness.CreateDriver().RunGeneratorsAndUpdateCompilation(
            compilation,
            out _,
            out _,
            cancellationToken
        );

        act.Should().ThrowExactly<OperationCanceledException>();
    }

    [Theory]
    [InlineData("TimeSpan.MinValue")]
    [InlineData("TimeSpan.MaxValue")]
    public void GeneratorDegradesWhenAcceptedTimeSpanCannotBeDateTimeOffset(string offsetExpression)
    {
        AssertRejectedBoundary(
            "DateTimeOffset",
            $"new DateTimeOffset(2026, 1, 2, 0, 0, 0, {offsetExpression})"
        );
    }

    [Fact]
    public void GeneratorRejectsStaticReadonlyFieldAssignedByStaticConstructor()
    {
        AssertRejectedBoundary(
            "DateTime",
            "Boundary",
            """
            private static readonly DateTime Boundary = new (2026, 1, 2);

            static BoundaryValidator()
            {
                Boundary = new DateTime(2027, 1, 2);
            }
            """
        );
    }

    [Fact]
    public void GeneratorRejectsLocalVariableBoundary()
    {
        var result = GeneratorTestHarness.Run(
            """
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
                    var boundary = new DateTime(2026, 1, 2);
                    context.Check(value).IsEqualTo(boundary);
                    return checkpoint.ToValidatedValue(value);
                }
            }
            """
        );
        var diagnostic = result.ResultDiagnostics
           .Where(static candidate => candidate.Id == "LPRSG0015")
           .Should()
           .ContainSingle()
           .Subject;

        diagnostic.Severity.Should().Be(DiagnosticSeverity.Info);
        GetLocatedText(diagnostic).Should().Be("boundary");
        result.GeneratedSources.Should().ContainSingle().Subject
           .Should()
           .Contain("builder.WithErrorExample(\"EqualTo\", null, null);");
    }

    [Fact]
    public void GeneratorRejectsStaticReadonlyFieldCycle()
    {
        AssertRejectedBoundary(
            "DateTime",
            "First",
            """
            private static readonly DateTime First = Second;
            private static readonly DateTime Second = First;
            """
        );
    }

    [Fact]
    public void GeneratorRejectsExpressionBeyondRecursionBound()
    {
        AssertRejectedBoundary(
            "DateTime",
            "First",
            """
            private static readonly DateTime First = Second;
            private static readonly DateTime Second = Third;
            private static readonly DateTime Third = Fourth;
            private static readonly DateTime Fourth = Fifth;
            private static readonly DateTime Fifth = Sixth;
            private static readonly DateTime Sixth = new (2026, 1, 2);
            """
        );
    }

    [Fact]
    public void GeneratorRejectsUserDefinedTypeWithFrameworkName()
    {
        AssertRejectedBoundary(
            "Fake.DateTime",
            "new Fake.DateTime(2026, 1, 2)",
            additionalType:
            """
            namespace Fake
            {
                public readonly struct DateTime
                {
                    public DateTime(int year, int month, int day) { }
                }
            }
            """
        );
    }

    [Fact]
    public void GeneratorRejectsUserDefinedFactoryWithFrameworkMemberName()
    {
        AssertRejectedBoundary(
            "TimeSpan",
            "FakeTimeSpan.FromHours(2)",
            additionalType:
            """
            public static class FakeTimeSpan
            {
                public static TimeSpan FromHours(int value) => TimeSpan.FromHours(value);
            }
            """
        );
    }

    [Fact]
    public void GeneratorReportsWarningForFieldInAnotherSourceFile()
    {
        var validatorSource = CreateValidatorSource("DateTime", "Boundaries.Value");
        var result = GeneratorTestHarness.Run(
            ("Validator.cs", validatorSource),
            (
                "Boundaries.cs",
                """
                using System;

                public static class Boundaries
                {
                    public static readonly DateTime Value = new (2026, 1, 2);
                }
                """
            )
        );

        AssertCrossFileWarning(result, "Boundaries.Value");
    }

    [Fact]
    public void GeneratorPropagatesCrossFileWarningFromNestedConstructorArgument()
    {
        const string expression =
            "new DateTimeOffset(2026, 1, 2, 0, 0, 0, Boundaries.Offset)";
        var validatorSource = CreateValidatorSource("DateTimeOffset", expression);
        var result = GeneratorTestHarness.Run(
            ("Validator.cs", validatorSource),
            (
                "Boundaries.cs",
                """
                using System;

                public static class Boundaries
                {
                    public static readonly TimeSpan Offset = TimeSpan.FromHours(2);
                }
                """
            )
        );

        AssertCrossFileWarning(result, expression);
    }

    [Fact]
    public void GeneratorReportsWarningWhenFieldTypeHasAnotherSourceDeclaration()
    {
        var validatorSource = CreateValidatorSource(
            "DateTime",
            "Boundary",
            "private static readonly DateTime Boundary = new (2026, 1, 2);"
        );
        var result = GeneratorTestHarness.Run(
            ("Validator.cs", validatorSource),
            ("Validator.Partial.cs", "public sealed partial class BoundaryValidator { }")
        );

        AssertCrossFileWarning(result, "Boundary");
    }

    [Fact]
    public void GeneratorReportsWarningForFieldInReferencedAssembly()
    {
        var boundaryCompilation = GeneratorTestHarness.CreateCompilation(
                (
                    "ExternalBoundaries.cs",
                    """
                    using System;

                    public static class ExternalBoundaries
                    {
                        public static readonly DateTime Value = new (2026, 1, 2);
                    }
                    """
                )
            )
           .WithAssemblyName("BoundaryLibrary");
        using var stream = new MemoryStream();
        var emitResult = boundaryCompilation.Emit(
            stream,
            cancellationToken: TestContext.Current.CancellationToken
        );
        emitResult.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           .Should()
           .BeEmpty();
        var reference = MetadataReference.CreateFromImage(stream.ToArray());
        var validatorCompilation = GeneratorTestHarness.CreateCompilation(
                ("Validator.cs", CreateValidatorSource("DateTime", "ExternalBoundaries.Value"))
            )
           .AddReferences(reference);

        var result = GeneratorTestHarness.Run(validatorCompilation);

        AssertCrossFileWarning(result, "ExternalBoundaries.Value");
    }

    [Fact]
    public void GeneratorReportsOneInfoPerDistinctUnresolvedRangeArgument()
    {
        var source = CreateRangeValidatorSource();
        var result = GeneratorTestHarness.Run(source);
        var diagnostics = result.ResultDiagnostics
           .Where(static diagnostic => diagnostic.Id == "LPRSG0015")
           .ToArray();

        diagnostics.Should().HaveCount(2);
        diagnostics.Select(GetLocatedText).Should().BeEquivalentTo("DateTime.Now", "DateTime.UtcNow");
        var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject;
        generatedSource.Should().Contain("builder.WithErrorExample(\"InRange\", null, null);");
        generatedSource.Should().NotContain("[\"lowerBoundary\"]");
        generatedSource.Should().NotContain("[\"upperBoundary\"]");
    }

    private static GeneratorTestResult AssertRejectedBoundary(
        string valueType,
        string expression,
        string additionalMember = "",
        string additionalType = ""
    )
    {
        var result = GeneratorTestHarness.Run(
            CreateValidatorSource(valueType, expression, additionalMember, additionalType)
        );
        result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           .Should()
           .BeEmpty();
        var diagnostic = result.ResultDiagnostics
           .Where(static candidate => candidate.Id == "LPRSG0015")
           .Should()
           .ContainSingle()
           .Subject;

        diagnostic.Severity.Should().Be(DiagnosticSeverity.Info);
        diagnostic.Location.SourceTree!.FilePath.Should().Be("Validator.cs");
        GetLocatedText(diagnostic).Should().Be(expression);
        diagnostic.GetMessage().Should().Contain("IsEqualTo").And.Contain("comparativeValue");
        var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject;
        generatedSource.Should().Contain("builder.WithErrorExample(\"EqualTo\", null, null);");
        generatedSource.Should().NotContain("[\"comparativeValue\"]");
        return result;
    }

    private static void AssertCrossFileWarning(GeneratorTestResult result, string expression)
    {
        result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           .Should()
           .BeEmpty();
        var diagnostic = result.ResultDiagnostics
           .Where(static candidate => candidate.Id == "LPRSG0016")
           .Should()
           .ContainSingle()
           .Subject;
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.Location.SourceTree!.FilePath.Should().Be("Validator.cs");
        GetLocatedText(diagnostic).Should().Be(expression);
        diagnostic.GetMessage().Should().Contain("multi-file resolution");
        result.ResultDiagnostics.Select(static candidate => candidate.Id).Should().NotContain("LPRSG0015");
        var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject;
        generatedSource.Should().Contain("builder.WithErrorExample(\"EqualTo\", null, null);");
        generatedSource.Should().NotContain("[\"comparativeValue\"]");
    }

    private static string GetLocatedText(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree!.GetText().GetSubText(diagnostic.Location.SourceSpan).ToString();

    private static string CreateValidatorSource(
        string valueType,
        string expression,
        string additionalMember = "",
        string additionalType = ""
    ) =>
        $$"""
          using System;
          using System.Globalization;
          using Light.PortableResults.Validation;
          using Light.PortableResults.Validation.OpenApi;

          [GeneratePortableValidationOpenApi]
          public sealed partial class BoundaryValidator : Validator<{{valueType}}>
          {
              {{additionalMember}}

              public BoundaryValidator(IValidationContextFactory validationContextFactory)
                  : base(validationContextFactory) { }

              protected override ValidatedValue<{{valueType}}> PerformValidation(
                  ValidationContext context,
                  ValidationCheckpoint checkpoint,
                  {{valueType}} value
              )
              {
                  context.Check(value).IsEqualTo({{expression}});
                  return checkpoint.ToValidatedValue(value);
              }
          }

          {{additionalType}}
          """;

    private static string CreateRangeValidatorSource() =>
        """
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
                context.Check(value).IsInRange(DateTime.Now, DateTime.UtcNow);
                return checkpoint.ToValidatedValue(value);
            }
        }
        """;
}
