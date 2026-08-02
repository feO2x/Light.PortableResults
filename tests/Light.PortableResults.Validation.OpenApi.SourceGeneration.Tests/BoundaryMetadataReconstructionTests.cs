using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration.Tests;

public sealed class BoundaryMetadataReconstructionTests
{
    private const string GuidText = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

    [Fact]
    public void GeneratorReconstructsEveryAcceptedDateTimeConstructorAndStatic()
    {
        var cases = new[]
        {
            DateTimeCase("new (2026, 1, 2)", new DateTime(2026, 1, 2)),
            DateTimeCase("new DateTime(2026, 1, 2, 3, 4, 5)", new DateTime(2026, 1, 2, 3, 4, 5)),
            DateTimeCase("new DateTime(2026, 1, 2, 3, 4, 5, 6)", new DateTime(2026, 1, 2, 3, 4, 5, 6)),
            DateTimeCase(
                "new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)",
                new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
            ),
            DateTimeCase(
                "new DateTime(2026, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc)",
                new DateTime(2026, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc)
            ),
            DateTimeCase("new DateTime(638713838450000000L)", new DateTime(638713838450000000L)),
            DateTimeCase(
                "new DateTime(638713838450000000L, DateTimeKind.Utc)",
                new DateTime(638713838450000000L, DateTimeKind.Utc)
            ),
            DateTimeCase("DateTime.MinValue", DateTime.MinValue),
            DateTimeCase("DateTime.MaxValue", DateTime.MaxValue),
            DateTimeCase("DateTime.UnixEpoch", DateTime.UnixEpoch)
        };

        AssertAcceptedCases("DateTime", cases);
    }

    [Fact]
    public void GeneratorReconstructsEveryAcceptedDateTimeOffsetConstructorFactoryAndStatic()
    {
        var offset = TimeSpan.FromHours(2);
        var dateTime = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        var cases = new[]
        {
            DateTimeOffsetCase(
                "new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2))",
                new DateTimeOffset(2026, 1, 2, 3, 4, 5, offset)
            ),
            DateTimeOffsetCase(
                "new DateTimeOffset(2026, 1, 2, 3, 4, 5, 6, TimeSpan.FromHours(2))",
                new DateTimeOffset(2026, 1, 2, 3, 4, 5, 6, offset)
            ),
            DateTimeOffsetCase(
                "new DateTimeOffset(638713838450000000L, TimeSpan.FromHours(2))",
                new DateTimeOffset(638713838450000000L, offset)
            ),
            DateTimeOffsetCase(
                "new DateTimeOffset(new DateTime(2026, 1, 2, 3, 4, 5), TimeSpan.FromHours(2))",
                new DateTimeOffset(dateTime, offset)
            ),
            DateTimeOffsetCase(
                "DateTimeOffset.FromUnixTimeSeconds(1L)",
                DateTimeOffset.FromUnixTimeSeconds(1L)
            ),
            DateTimeOffsetCase(
                "DateTimeOffset.FromUnixTimeMilliseconds(1L)",
                DateTimeOffset.FromUnixTimeMilliseconds(1L)
            ),
            DateTimeOffsetCase("DateTimeOffset.MinValue", DateTimeOffset.MinValue),
            DateTimeOffsetCase("DateTimeOffset.MaxValue", DateTimeOffset.MaxValue),
            DateTimeOffsetCase("DateTimeOffset.UnixEpoch", DateTimeOffset.UnixEpoch)
        };

        AssertAcceptedCases("DateTimeOffset", cases);
    }

    [Fact]
    public void GeneratorReconstructsEveryAcceptedTimeSpanConstructorFactoryAndStatic()
    {
        var cases = new[]
        {
            TimeSpanCase("new TimeSpan(2, 3, 4)", new TimeSpan(2, 3, 4)),
            TimeSpanCase("new TimeSpan(1, 2, 3, 4)", new TimeSpan(1, 2, 3, 4)),
            TimeSpanCase("new TimeSpan(1, 2, 3, 4, 5)", new TimeSpan(1, 2, 3, 4, 5)),
            TimeSpanCase("new TimeSpan(123456789L)", new TimeSpan(123456789L)),
            TimeSpanCase("TimeSpan.FromTicks(123456789L)", TimeSpan.FromTicks(123456789L)),
            TimeSpanCase("TimeSpan.FromDays(0.25D)", TimeSpan.FromDays(0.25D)),
            TimeSpanCase("TimeSpan.FromDays(1)", TimeSpan.FromDays(1)),
            TimeSpanCase("TimeSpan.FromHours(2D)", TimeSpan.FromHours(2D)),
            TimeSpanCase("TimeSpan.FromHours(2)", TimeSpan.FromHours(2)),
            TimeSpanCase("TimeSpan.FromMinutes(3D)", TimeSpan.FromMinutes(3D)),
            TimeSpanCase("TimeSpan.FromMinutes(3L)", TimeSpan.FromMinutes(3L)),
            TimeSpanCase("TimeSpan.FromSeconds(4D)", TimeSpan.FromSeconds(4D)),
            TimeSpanCase("TimeSpan.FromSeconds(4L)", TimeSpan.FromSeconds(4L)),
            TimeSpanCase("TimeSpan.FromMilliseconds(5D)", TimeSpan.FromMilliseconds(5D)),
            TimeSpanCase("TimeSpan.FromMilliseconds(5L)", TimeSpan.FromMilliseconds(5L)),
            TimeSpanCase("TimeSpan.FromMicroseconds(6D)", TimeSpan.FromMicroseconds(6D)),
            TimeSpanCase("TimeSpan.FromMicroseconds(6L)", TimeSpan.FromMicroseconds(6L)),
            TimeSpanCase("TimeSpan.Zero", TimeSpan.Zero),
            TimeSpanCase("TimeSpan.MinValue", TimeSpan.MinValue),
            TimeSpanCase("TimeSpan.MaxValue", TimeSpan.MaxValue)
        };

        AssertAcceptedCases("TimeSpan", cases);
    }

    [Fact]
    public void GeneratorReconstructsEveryAcceptedDateOnlyConstructorFactoryAndStatic()
    {
        var cases = new[]
        {
            DateOnlyCase("new DateOnly(2026, 1, 2)", new DateOnly(2026, 1, 2)),
            DateOnlyCase("DateOnly.FromDayNumber(1234)", DateOnly.FromDayNumber(1234)),
            DateOnlyCase("DateOnly.MinValue", DateOnly.MinValue),
            DateOnlyCase("DateOnly.MaxValue", DateOnly.MaxValue)
        };

        AssertAcceptedCases("DateOnly", cases);
    }

    [Fact]
    public void GeneratorReconstructsEveryAcceptedTimeOnlyConstructorAndStatic()
    {
        var cases = new[]
        {
            TimeOnlyCase("new TimeOnly(3, 4)", new TimeOnly(3, 4)),
            TimeOnlyCase("new TimeOnly(3, 4, 5)", new TimeOnly(3, 4, 5)),
            TimeOnlyCase("new TimeOnly(3, 4, 5, 6)", new TimeOnly(3, 4, 5, 6)),
            TimeOnlyCase("new TimeOnly(123456789L)", new TimeOnly(123456789L)),
            TimeOnlyCase("TimeOnly.MinValue", TimeOnly.MinValue),
            TimeOnlyCase("TimeOnly.MaxValue", TimeOnly.MaxValue)
        };

        AssertAcceptedCases("TimeOnly", cases);
    }

    [Fact]
    public void GeneratorReconstructsEveryAcceptedGuidConstructorFactoryAndStatic()
    {
        var guid = new Guid(GuidText);
        var cases = new[]
        {
            GuidCase($"new Guid(\"{GuidText}\")", guid),
            GuidCase($"Guid.Parse(\"{GuidText}\")", guid),
            GuidCase("Guid.ParseExact(\"a1b2c3d4e5f67890abcdef1234567890\", \"N\")", guid),
            GuidCase("Guid.Empty", Guid.Empty)
        };

        AssertAcceptedCases("Guid", cases);
    }

    [Fact]
    public void GeneratorReconstructsEveryAcceptedUriConstructor()
    {
        var absoluteText = "https://example.com/items/42?view=full#summary";
        var relativeText = "items/42#summary";
        var cases = new[]
        {
            UriCase($"new Uri(\"{absoluteText}\")", new Uri(absoluteText)),
            UriCase(
                $"new Uri(\"{relativeText}\", UriKind.Relative)",
                new Uri(relativeText, UriKind.Relative)
            )
        };

        AssertAcceptedCases("Uri", cases);
    }

    [Fact]
    public void GeneratorReconstructsDateTimeOffsetWithEveryAcceptedTimeSpanShape()
    {
        var offset = TimeSpan.FromHours(2);
        var expressions = new[]
        {
            "new TimeSpan(2, 0, 0)",
            "new TimeSpan(0, 2, 0, 0)",
            "new TimeSpan(0, 2, 0, 0, 0)",
            "new TimeSpan(72000000000L)",
            "TimeSpan.FromTicks(72000000000L)",
            "TimeSpan.FromDays(0.08333333333333333D)",
            "TimeSpan.FromDays(0)",
            "TimeSpan.FromHours(2D)",
            "TimeSpan.FromHours(2)",
            "TimeSpan.FromMinutes(120D)",
            "TimeSpan.FromMinutes(120L)",
            "TimeSpan.FromSeconds(7200D)",
            "TimeSpan.FromSeconds(7200L)",
            "TimeSpan.FromMilliseconds(7200000D)",
            "TimeSpan.FromMilliseconds(7200000L)",
            "TimeSpan.FromMicroseconds(7200000000D)",
            "TimeSpan.FromMicroseconds(7200000000L)",
            "TimeSpan.Zero",
            "Offset"
        };
        var cases = expressions.Select(
                (expression, index) => DateTimeOffsetCase(
                    $"new DateTimeOffset(2026, 1, {index + 1}, 0, 0, 0, {expression})",
                    new DateTimeOffset(
                        2026,
                        1,
                        index + 1,
                        0,
                        0,
                        0,
                        expression is "TimeSpan.Zero" or "TimeSpan.FromDays(0)" ? TimeSpan.Zero : offset
                    )
                )
            )
           .ToArray();

        AssertAcceptedCases(
            "DateTimeOffset",
            cases,
            "private static readonly TimeSpan Offset = TimeSpan.FromHours(2);"
        );
    }

    [Fact]
    public void GeneratorReconstructsStaticReadonlyFieldDeclaredInValidatorFile()
    {
        var value = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var cases = new[] { DateTimeCase("Boundary", value) };

        AssertAcceptedCases(
            "DateTime",
            cases,
            "private static readonly DateTime Boundary = new (2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);"
        );
    }

    private static void AssertAcceptedCases(
        string valueType,
        IReadOnlyList<BoundaryCase> cases,
        string additionalMember = ""
    )
    {
        var source = CreateValidatorSource(valueType, cases, additionalMember);
        var result = GeneratorTestHarness.Run(source);

        result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           .Should()
           .BeEmpty();
        result.ResultDiagnostics.Select(static diagnostic => diagnostic.Id)
           .Should()
           .NotContain(["LPRSG0015", "LPRSG0016"]);
        var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject;
        for (var i = 0; i < cases.Count; i++)
        {
            generatedSource.Should().Contain($"case{i} must be equal to {cases[i].CanonicalText}");
            generatedSource.Should().Contain($"[\"comparativeValue\"] = {cases[i].CSharpLiteral}");
        }
    }

    private static string CreateValidatorSource(
        string valueType,
        IReadOnlyList<BoundaryCase> cases,
        string additionalMember
    )
    {
        var calls = string.Join(
            Environment.NewLine,
            cases.Select(
                static (item, index) =>
                    $"context.Check(value, displayName: \"case{index}\").IsEqualTo({item.Expression});"
            )
        );
        return $$"""
                 using System;
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
                         {{calls}}
                         return checkpoint.ToValidatedValue(value);
                     }
                 }
                 """;
    }

    private static BoundaryCase DateTimeCase(string expression, DateTime value) =>
        new (
            expression,
            MetadataValue.FromDateTime(value).ToCanonicalString(),
            "new global::System.DateTime(" +
            value.Ticks.ToString(CultureInfo.InvariantCulture) +
            "L, global::System.DateTimeKind." +
            value.Kind +
            ")"
        );

    private static BoundaryCase DateTimeOffsetCase(string expression, DateTimeOffset value) =>
        new (
            expression,
            MetadataValue.FromDateTimeOffset(value).ToCanonicalString(),
            "new global::System.DateTimeOffset(" +
            value.Ticks.ToString(CultureInfo.InvariantCulture) +
            "L, global::System.TimeSpan.FromTicks(" +
            value.Offset.Ticks.ToString(CultureInfo.InvariantCulture) +
            "L))"
        );

    private static BoundaryCase TimeSpanCase(string expression, TimeSpan value) =>
        new (
            expression,
            MetadataValue.FromTimeSpan(value).ToCanonicalString(),
            "global::System.TimeSpan.FromTicks(" +
            value.Ticks.ToString(CultureInfo.InvariantCulture) +
            "L)"
        );

    private static BoundaryCase DateOnlyCase(string expression, DateOnly value) =>
        new (
            expression,
            MetadataValue.FromDateOnly(value).ToCanonicalString(),
            "global::System.DateOnly.FromDayNumber(" +
            value.DayNumber.ToString(CultureInfo.InvariantCulture) +
            ")"
        );

    private static BoundaryCase TimeOnlyCase(string expression, TimeOnly value) =>
        new (
            expression,
            MetadataValue.FromTimeOnly(value).ToCanonicalString(),
            "new global::System.TimeOnly(" +
            value.Ticks.ToString(CultureInfo.InvariantCulture) +
            "L)"
        );

    private static BoundaryCase GuidCase(string expression, Guid value) =>
        new (
            expression,
            MetadataValue.FromGuid(value).ToCanonicalString(),
            "global::System.Guid.ParseExact(\"" + value.ToString("D") + "\", \"D\")"
        );

    private static BoundaryCase UriCase(string expression, Uri value) =>
        new (
            expression,
            MetadataValue.FromUri(value).ToCanonicalString(),
            "new global::System.Uri(\"" +
            value.OriginalString +
            "\", global::System.UriKind.RelativeOrAbsolute)"
        );

    private sealed class BoundaryCase
    {
        public BoundaryCase(string expression, string canonicalText, string cSharpLiteral)
        {
            Expression = expression;
            CanonicalText = canonicalText;
            CSharpLiteral = cSharpLiteral;
        }

        public string Expression { get; }
        public string CanonicalText { get; }
        public string CSharpLiteral { get; }
    }
}
