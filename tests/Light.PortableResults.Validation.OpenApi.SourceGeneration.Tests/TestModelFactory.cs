using System.Collections.Immutable;
using System.Linq;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration.Tests;

internal static class TestModelFactory
{
    public static RuleCallModel TypedRule(
        string code,
        string? typedValueTypeName,
        RuleMetadataShape shape = RuleMetadataShape.TypedComparison
    ) =>
        new (
            code,
            shape,
            target: null,
            message: null,
            typedValueTypeName,
            ImmutableArray<MetadataValueModel>.Empty,
            ImmutableArray<MetadataSchemaPropertyModel>.Empty
        );

    public static RuleCallModel InlineSchemaRule(
        string code,
        params (string Key, string TypeName)[] properties
    ) =>
        new (
            code,
            RuleMetadataShape.Registered,
            target: null,
            message: null,
            typedValueTypeName: null,
            ImmutableArray<MetadataValueModel>.Empty,
            [..properties.Select(static property => new MetadataSchemaPropertyModel(property.Key, property.TypeName))]
        );

    public static RuleCallModel RegisteredRule(
        string code,
        string? target = null,
        string? message = null,
        ImmutableArray<MetadataValueModel> metadataValues = default
    ) =>
        new (
            code,
            RuleMetadataShape.Registered,
            target,
            message,
            typedValueTypeName: null,
            metadataValues.IsDefault ? ImmutableArray<MetadataValueModel>.Empty : metadataValues,
            ImmutableArray<MetadataSchemaPropertyModel>.Empty
        );
}
