using System;
using System.Collections.Immutable;
using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration.Tests;

public sealed class ValidatorOpenApiEmitterTests
{
    [Fact]
    public void Emit_ShouldRenderNamespaceAccessibilityAndAllowUnknownErrorCodes()
    {
        var model = new ValidatorModel(
            "TestApp.SampleValidator",
            "SampleValidator",
            "TestApp",
            "public",
            "SampleValidator",
            allowUnknownErrorCodes: true,
            rules: [TestModelFactory.RegisteredRule("NotEmpty", "name", "name must not be empty")],
            hints: ImmutableArray<ErrorHintModel>.Empty,
            examples: ImmutableArray<ExampleHintModel>.Empty
        );

        var source = ValidatorOpenApiEmitter.Emit(model);

        source.Should().Contain("namespace TestApp");
        source.Should().Contain("public partial class SampleValidator : IPortableValidationOpenApiContract");
        source.Should().Contain("builder.WithErrorCodes(\"NotEmpty\");");
        source.Should().Contain("builder.WithErrorExample(\"NotEmpty\", \"name\", \"name must not be empty\");");
        source.Should().Contain("builder.AllowUnknownErrorCodes();");
    }

    [Fact]
    public void Emit_ShouldOmitNamespaceAndAllowUnknownErrorCodesWhenNotRequested()
    {
        var model = new ValidatorModel(
            "SampleValidator",
            "SampleValidator",
            namespaceName: null,
            "internal",
            "SampleValidator",
            allowUnknownErrorCodes: false,
            rules: [TestModelFactory.RegisteredRule("NotEmpty")],
            hints: ImmutableArray<ErrorHintModel>.Empty,
            examples: ImmutableArray<ExampleHintModel>.Empty
        );

        var source = ValidatorOpenApiEmitter.Emit(model);

        source.Should().NotContain("namespace ");
        source.Should().Contain("internal partial class SampleValidator");
        source.Should().NotContain("builder.AllowUnknownErrorCodes();");
    }

    [Fact]
    public void Emit_ShouldRenderTypedHelpersAndSkipUnknownTypedCodes()
    {
        var model = ModelWithRules(
            TestModelFactory.TypedRule("InRange", "global::System.Int32", RuleMetadataShape.TypedRange),
            TestModelFactory.TypedRule("EqualTo", "global::System.Int32"),
            TestModelFactory.TypedRule("NotEqualTo", "global::System.Int32"),
            TestModelFactory.TypedRule("GreaterThan", "global::System.Int32"),
            TestModelFactory.TypedRule("GreaterThanOrEqualTo", "global::System.Int32"),
            TestModelFactory.TypedRule("LessThan", "global::System.Int32"),
            TestModelFactory.TypedRule("LessThanOrEqualTo", "global::System.Int32"),
            TestModelFactory.TypedRule("NotInRange", "global::System.Int32"),
            TestModelFactory.TypedRule("ExclusiveRange", "global::System.Int32"),
            // Unknown typed code -> GetTypedHelperName returns null -> emitter skips it.
            TestModelFactory.TypedRule("UnknownTyped", "global::System.Int32"),
            // Typed shape without a value type name -> also skipped.
            TestModelFactory.TypedRule("InRange", typedValueTypeName: null)
        );

        var source = ValidatorOpenApiEmitter.Emit(model);

        source.Should().Contain("builder.WithInRangeError<global::System.Int32>();");
        source.Should().Contain("builder.WithEqualToError<global::System.Int32>();");
        source.Should().Contain("builder.WithNotEqualToError<global::System.Int32>();");
        source.Should().Contain("builder.WithGreaterThanError<global::System.Int32>();");
        source.Should().Contain("builder.WithGreaterThanOrEqualToError<global::System.Int32>();");
        source.Should().Contain("builder.WithLessThanError<global::System.Int32>();");
        source.Should().Contain("builder.WithLessThanOrEqualToError<global::System.Int32>();");
        source.Should().Contain("builder.WithNotInRangeError<global::System.Int32>();");
        source.Should().Contain("builder.WithExclusiveRangeError<global::System.Int32>();");
        // The unknown typed code and the typed rule without a value type produce no typed-helper call,
        // so only the nine known typed helpers are emitted.
        source.Split(["Error<global::System.Int32>();"], StringSplitOptions.None).Should().HaveCount(10);
        source.Should().NotContain("builder.WithUnknownTyped");
    }

    [Fact]
    public void Emit_ShouldDeduplicateTypedRulesSharingTheSameSchemaKey()
    {
        var model = ModelWithRules(
            TestModelFactory.TypedRule("InRange", "global::System.Int32", RuleMetadataShape.TypedRange),
            TestModelFactory.TypedRule("InRange", "global::System.Int32", RuleMetadataShape.TypedRange)
        );

        var source = ValidatorOpenApiEmitter.Emit(model);

        source.Should().Contain("builder.WithInRangeError<global::System.Int32>();");
        source.Split(["builder.WithInRangeError"], StringSplitOptions.None).Should().HaveCount(2);
    }

    [Fact]
    public void Emit_ShouldRenderInlineSchemaForRegisteredRulesAndDeduplicate()
    {
        var model = ModelWithRules(
            TestModelFactory.InlineSchemaRule("DivisibleBy", ("divisor", "global::System.Int32")),
            TestModelFactory.InlineSchemaRule("DivisibleBy", ("divisor", "global::System.Int32"))
        );

        var source = ValidatorOpenApiEmitter.Emit(model);

        source.Should().Contain("builder.WithErrorMetadata(\"DivisibleBy\", _ => new OpenApiSchema");
        source.Should().Contain("[\"divisor\"] = PortableOpenApiSchemaTypeMapper.Map<global::System.Int32>()");
        source.Should().Contain("Required = new HashSet<string>(StringComparer.Ordinal) { \"divisor\" }");
        source.Split(["WithErrorMetadata(\"DivisibleBy\""], StringSplitOptions.None).Should().HaveCount(2);
    }

    [Fact]
    public void Emit_ShouldRenderMetadataTypeHintsExampleOnlyCodesAndInlineSchemaHints()
    {
        var model = new ValidatorModel(
            "TestApp.HintValidator",
            "HintValidator",
            "TestApp",
            "public",
            "HintValidator",
            allowUnknownErrorCodes: false,
            rules: ImmutableArray<RuleCallModel>.Empty,
            hints:
            [
                new ErrorHintModel(
                    "CustomCode",
                    "global::TestApp.CustomMetadata",
                    ImmutableArray<MetadataSchemaPropertyModel>.Empty
                ),
                // Duplicate metadata-type hint -> deduplicated to a single WithErrorMetadata<T>.
                new ErrorHintModel(
                    "CustomCode",
                    "global::TestApp.CustomMetadata",
                    ImmutableArray<MetadataSchemaPropertyModel>.Empty
                ),
                new ErrorHintModel(
                    "InlineCode",
                    metadataTypeName: null,
                    [new MetadataSchemaPropertyModel("limit", "global::System.Int32")]
                ),
                // Code-only hint -> contributes a registered error code.
                new ErrorHintModel(
                    "PlainCode",
                    metadataTypeName: null,
                    ImmutableArray<MetadataSchemaPropertyModel>.Empty
                )
            ],
            examples: [new ExampleHintModel("ExampleOnlyCode", "field", null, ImmutableArray<MetadataValueModel>.Empty)]
        );

        var source = ValidatorOpenApiEmitter.Emit(model);

        source.Should().Contain("builder.WithErrorMetadata<global::TestApp.CustomMetadata>(\"CustomCode\");");
        source.Split(["WithErrorMetadata<global::TestApp.CustomMetadata>"], StringSplitOptions.None)
           .Should()
           .HaveCount(2);
        source.Should().Contain("builder.WithErrorMetadata(\"InlineCode\", _ => new OpenApiSchema");
        source.Should().Contain("builder.WithErrorCodes(\"ExampleOnlyCode\", \"PlainCode\");");
        source.Should().Contain("builder.WithErrorExample(\"ExampleOnlyCode\", \"field\", null);");
    }

    [Fact]
    public void Emit_ShouldRenderAllConstantMetadataLiteralTypes()
    {
        var metadataValues = ImmutableArray.Create(
            MetadataValue("text", "hello"),
            MetadataValue("ch", 'q'),
            MetadataValue("flagTrue", true),
            MetadataValue("flagFalse", false),
            MetadataValue("b", (byte) 1),
            MetadataValue("sb", (sbyte) -2),
            MetadataValue("sh", (short) -3),
            MetadataValue("ush", (ushort) 4),
            MetadataValue("i", 5),
            MetadataValue("ui", 6U),
            MetadataValue("l", 7L),
            MetadataValue("ul", 8UL),
            MetadataValue("f", 1.5F),
            MetadataValue("d", 2.5D),
            MetadataValue("m", 3.5M),
            MetadataValue("nothing", null),
            MetadataValue("escapes", "a\\b\"c\nd\te\rfg\0h\ai\bj\fk\vl'm")
        );
        var model = ModelWithRules(
            TestModelFactory.RegisteredRule("Types", "field", "message", metadataValues)
        );

        var source = ValidatorOpenApiEmitter.Emit(model);

        source.Should().Contain("[\"text\"] = \"hello\"");
        source.Should().Contain("[\"ch\"] = 'q'");
        source.Should().Contain("[\"flagTrue\"] = true");
        source.Should().Contain("[\"flagFalse\"] = false");
        source.Should().Contain("[\"b\"] = 1");
        source.Should().Contain("[\"sb\"] = -2");
        source.Should().Contain("[\"sh\"] = -3");
        source.Should().Contain("[\"ush\"] = 4");
        source.Should().Contain("[\"i\"] = 5");
        source.Should().Contain("[\"ui\"] = 6U");
        source.Should().Contain("[\"l\"] = 7L");
        source.Should().Contain("[\"ul\"] = 8UL");
        source.Should().Contain("[\"f\"] = 1.5F");
        source.Should().Contain("[\"d\"] = 2.5D");
        source.Should().Contain("[\"m\"] = 3.5M");
        source.Should().Contain("[\"nothing\"] = null");
        source.Should()
           .Contain("[\"escapes\"] = \"a\\\\b\\\"c\\nd\\te\\rf\\u0001g\\0h\\ai\\bj\\fk\\vl\\'m\"");
    }

    [Fact]
    public void Emit_ShouldOmitMetadataDictionaryWhenAValueIsNotConstant()
    {
        var metadataValues = ImmutableArray.Create(
            new MetadataValueModel("min", value: null, hasConstantValue: false, "global::System.Int32")
        );
        var model = ModelWithRules(
            TestModelFactory.RegisteredRule("MinLength", "name", "name is too short", metadataValues)
        );

        var source = ValidatorOpenApiEmitter.Emit(model);

        source.Should().Contain("builder.WithErrorExample(\"MinLength\", \"name\", \"name is too short\");");
        source.Should().NotContain("new Dictionary<string, object?>");
    }

    [Fact]
    public void Emit_ShouldDeduplicateExampleHintsAndRenderMetadata()
    {
        var metadataValues =
            ImmutableArray.Create(MetadataValue("lowerBoundary", 1), MetadataValue("upperBoundary", 5));
        var model = new ValidatorModel(
            "TestApp.ExampleValidator",
            "ExampleValidator",
            "TestApp",
            "public",
            "ExampleValidator",
            allowUnknownErrorCodes: false,
            rules: ImmutableArray<RuleCallModel>.Empty,
            hints: ImmutableArray<ErrorHintModel>.Empty,
            examples:
            [
                new ExampleHintModel("RatingTooLow", "rating", null, metadataValues),
                new ExampleHintModel("RatingTooLow", "rating", null, metadataValues)
            ]
        );

        var source = ValidatorOpenApiEmitter.Emit(model);

        source.Should().Contain(
            "builder.WithErrorExample(\"RatingTooLow\", \"rating\", null, new Dictionary<string, object?>(StringComparer.Ordinal) { [\"lowerBoundary\"] = 1, [\"upperBoundary\"] = 5 });"
        );
        source.Split(["WithErrorExample(\"RatingTooLow\""], StringSplitOptions.None).Should().HaveCount(2);
    }

    private static MetadataValueModel MetadataValue(string key, object? value) =>
        new (key, value, hasConstantValue: true, "global::System.Object");

    private static ValidatorModel ModelWithRules(params RuleCallModel[] rules) =>
        new (
            "TestApp.SampleValidator",
            "SampleValidator",
            "TestApp",
            "public",
            "SampleValidator",
            allowUnknownErrorCodes: false,
            [.. rules],
            ImmutableArray<ErrorHintModel>.Empty,
            ImmutableArray<ExampleHintModel>.Empty
        );
}
