using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration.Tests;

public sealed class ValidatorOpenApiAnalysisTests
{
    // Reuse a production descriptor so the test does not declare a new analyzer rule (which would trip RS2008).
    private static readonly DiagnosticDescriptor Descriptor = DiagnosticDescriptors.NoDocumentedRules;

    [Fact]
    public void Equals_ShouldReturnTrueForIdenticalAnalyses()
    {
        var first = new ValidatorOpenApiAnalysis("Hint.g.cs", "source", CreateDiagnostics("a"));
        var second = new ValidatorOpenApiAnalysis("Hint.g.cs", "source", CreateDiagnostics("a"));

        first.Equals(second).Should().BeTrue();
        first.Equals((object) second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldReturnFalseForNull()
    {
        var analysis = new ValidatorOpenApiAnalysis("Hint.g.cs", "source", ImmutableArray<Diagnostic>.Empty);

        analysis.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseForDifferentType()
    {
        var analysis = new ValidatorOpenApiAnalysis("Hint.g.cs", "source", ImmutableArray<Diagnostic>.Empty);

        // ReSharper disable once SuspiciousTypeConversion.Global -- required for test scenario
        // ReSharper disable once RedundantCast
        analysis.Equals((object) "not an analysis").Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenHintNameDiffers()
    {
        var first = new ValidatorOpenApiAnalysis("First.g.cs", "source", ImmutableArray<Diagnostic>.Empty);
        var second = new ValidatorOpenApiAnalysis("Second.g.cs", "source", ImmutableArray<Diagnostic>.Empty);

        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenSourceDiffers()
    {
        var first = new ValidatorOpenApiAnalysis("Hint.g.cs", "source", ImmutableArray<Diagnostic>.Empty);
        var second = new ValidatorOpenApiAnalysis("Hint.g.cs", null, ImmutableArray<Diagnostic>.Empty);

        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenDiagnosticCountDiffers()
    {
        var first = new ValidatorOpenApiAnalysis("Hint.g.cs", "source", CreateDiagnostics("a"));
        var second = new ValidatorOpenApiAnalysis("Hint.g.cs", "source", ImmutableArray<Diagnostic>.Empty);

        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenDiagnosticContentDiffers()
    {
        var first = new ValidatorOpenApiAnalysis("Hint.g.cs", "source", CreateDiagnostics("a"));
        var second = new ValidatorOpenApiAnalysis("Hint.g.cs", "source", CreateDiagnostics("b"));

        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenDiagnosticSourcePathDiffers()
    {
        var first = new ValidatorOpenApiAnalysis(
            "Hint.g.cs",
            "source",
            CreateDiagnostics("a", "First.cs", 1)
        );
        var second = new ValidatorOpenApiAnalysis(
            "Hint.g.cs",
            "source",
            CreateDiagnostics("a", "Second.cs", 1)
        );

        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenDiagnosticSourceSpanDiffers()
    {
        var first = new ValidatorOpenApiAnalysis(
            "Hint.g.cs",
            "source",
            CreateDiagnostics("a", "Validator.cs", 1)
        );
        var second = new ValidatorOpenApiAnalysis(
            "Hint.g.cs",
            "source",
            CreateDiagnostics("a", "Validator.cs", 2)
        );

        first.Equals(second).Should().BeFalse();
    }

    private static ImmutableArray<Diagnostic> CreateDiagnostics(string argument) =>
        [Diagnostic.Create(Descriptor, Location.None, argument)];

    private static ImmutableArray<Diagnostic> CreateDiagnostics(
        string argument,
        string path,
        int spanStart
    )
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("abcd", path: path);
        var location = Location.Create(syntaxTree, new TextSpan(spanStart, 1));
        return [Diagnostic.Create(Descriptor, location, argument)];
    }
}

public sealed class RuleSchemaKeyComparerTests
{
    [Fact]
    public void Equals_ShouldReturnTrueForSameReference()
    {
        var rule = TestModelFactory.TypedRule("InRange", "global::System.Int32");

        RuleSchemaKeyComparer.Instance.Equals(rule, rule).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenOneOperandIsNull()
    {
        var rule = TestModelFactory.TypedRule("InRange", "global::System.Int32");

        RuleSchemaKeyComparer.Instance.Equals(rule, null).Should().BeFalse();
        RuleSchemaKeyComparer.Instance.Equals(null, rule).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnTrueForMatchingKeyParts()
    {
        var first = TestModelFactory.TypedRule("InRange", "global::System.Int32");
        var second = TestModelFactory.TypedRule("InRange", "global::System.Int32");

        RuleSchemaKeyComparer.Instance.Equals(first, second).Should().BeTrue();
        RuleSchemaKeyComparer.Instance.GetHashCode(first)
           .Should()
           .Be(RuleSchemaKeyComparer.Instance.GetHashCode(second));
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenCodeDiffers()
    {
        var first = TestModelFactory.TypedRule("InRange", "global::System.Int32");
        var second = TestModelFactory.TypedRule("NotInRange", "global::System.Int32");

        RuleSchemaKeyComparer.Instance.Equals(first, second).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenShapeDiffers()
    {
        var first = TestModelFactory.TypedRule("InRange", "global::System.Int32");
        var second = TestModelFactory.TypedRule("InRange", "global::System.Int32", RuleMetadataShape.TypedRange);

        RuleSchemaKeyComparer.Instance.Equals(first, second).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenTypedValueTypeNameDiffers()
    {
        var first = TestModelFactory.TypedRule("InRange", "global::System.Int32");
        var second = TestModelFactory.TypedRule("InRange", typedValueTypeName: null);

        RuleSchemaKeyComparer.Instance.Equals(first, second).Should().BeFalse();
        RuleSchemaKeyComparer.Instance.GetHashCode(second).Should().NotBe(0);
    }
}

public sealed class InlineSchemaRuleComparerTests
{
    [Fact]
    public void Equals_ShouldReturnTrueForSameReference()
    {
        var rule = TestModelFactory.InlineSchemaRule("DivisibleBy", ("divisor", "global::System.Int32"));

        InlineSchemaRuleComparer.Instance.Equals(rule, rule).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenOneOperandIsNull()
    {
        var rule = TestModelFactory.InlineSchemaRule("DivisibleBy", ("divisor", "global::System.Int32"));

        InlineSchemaRuleComparer.Instance.Equals(rule, null).Should().BeFalse();
        InlineSchemaRuleComparer.Instance.Equals(null, rule).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenCodeDiffers()
    {
        var first = TestModelFactory.InlineSchemaRule("DivisibleBy", ("divisor", "global::System.Int32"));
        var second = TestModelFactory.InlineSchemaRule("MultipleOf", ("divisor", "global::System.Int32"));

        InlineSchemaRuleComparer.Instance.Equals(first, second).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenPropertyCountDiffers()
    {
        var first = TestModelFactory.InlineSchemaRule("DivisibleBy", ("divisor", "global::System.Int32"));
        var second = TestModelFactory.InlineSchemaRule(
            "DivisibleBy",
            ("divisor", "global::System.Int32"),
            ("scale", "global::System.Int32")
        );

        InlineSchemaRuleComparer.Instance.Equals(first, second).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenPropertyKeyDiffers()
    {
        var first = TestModelFactory.InlineSchemaRule("DivisibleBy", ("divisor", "global::System.Int32"));
        var second = TestModelFactory.InlineSchemaRule("DivisibleBy", ("factor", "global::System.Int32"));

        InlineSchemaRuleComparer.Instance.Equals(first, second).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalseWhenPropertyTypeNameDiffers()
    {
        var first = TestModelFactory.InlineSchemaRule("DivisibleBy", ("divisor", "global::System.Int32"));
        var second = TestModelFactory.InlineSchemaRule("DivisibleBy", ("divisor", "global::System.Int64"));

        InlineSchemaRuleComparer.Instance.Equals(first, second).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnTrueForMatchingProperties()
    {
        var first = TestModelFactory.InlineSchemaRule(
            "DivisibleBy",
            ("divisor", "global::System.Int32"),
            ("scale", "global::System.Int32")
        );
        var second = TestModelFactory.InlineSchemaRule(
            "DivisibleBy",
            ("divisor", "global::System.Int32"),
            ("scale", "global::System.Int32")
        );

        InlineSchemaRuleComparer.Instance.Equals(first, second).Should().BeTrue();
        InlineSchemaRuleComparer.Instance.GetHashCode(first)
           .Should()
           .Be(InlineSchemaRuleComparer.Instance.GetHashCode(second));
    }
}
