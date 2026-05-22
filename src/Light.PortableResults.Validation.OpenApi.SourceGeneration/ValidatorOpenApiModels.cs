using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration;

/// <summary>
/// Represents the output of validator OpenAPI source-generation analysis.
/// </summary>
public sealed class ValidatorOpenApiAnalysis : IEquatable<ValidatorOpenApiAnalysis>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidatorOpenApiAnalysis" />.
    /// </summary>
    public ValidatorOpenApiAnalysis(
        string hintName,
        string? source,
        ImmutableArray<Diagnostic> diagnostics
    )
    {
        HintName = hintName;
        Source = source;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the generated source hint name.
    /// </summary>
    public string HintName { get; }

    /// <summary>
    /// Gets the generated source, or <see langword="null" /> when no source should be emitted.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets diagnostics produced during analysis.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <inheritdoc />
    public bool Equals(ValidatorOpenApiAnalysis? other)
    {
        if (other is null)
        {
            return false;
        }

        return HintName == other.HintName &&
               Source == other.Source &&
               DiagnosticsEqual(Diagnostics, other.Diagnostics);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ValidatorOpenApiAnalysis);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = HintName.GetHashCode();
            hashCode = hashCode * 31 + (Source?.GetHashCode() ?? 0);
            foreach (var diagnostic in Diagnostics)
            {
                hashCode = hashCode * 31 + diagnostic.Id.GetHashCode();
                hashCode = hashCode * 31 + diagnostic.Severity.GetHashCode();
                hashCode = hashCode * 31 + diagnostic.GetMessage().GetHashCode();
            }

            return hashCode;
        }
    }

    private static bool DiagnosticsEqual(
        ImmutableArray<Diagnostic> left,
        ImmutableArray<Diagnostic> right
    )
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (left[i].Id != right[i].Id ||
                left[i].Severity != right[i].Severity ||
                left[i].GetMessage() != right[i].GetMessage())
            {
                return false;
            }
        }

        return true;
    }
}

public sealed class ValidatorModel
{
    public ValidatorModel(
        string metadataName,
        string displayName,
        string? namespaceName,
        string accessibility,
        string className,
        bool allowUnknownErrorCodes,
        ImmutableArray<RuleCallModel> rules,
        ImmutableArray<ErrorHintModel> hints,
        ImmutableArray<ExampleHintModel> examples
    )
    {
        MetadataName = metadataName;
        DisplayName = displayName;
        NamespaceName = namespaceName;
        Accessibility = accessibility;
        ClassName = className;
        AllowUnknownErrorCodes = allowUnknownErrorCodes;
        Rules = rules;
        Hints = hints;
        Examples = examples;
    }

    public string MetadataName { get; }
    public string DisplayName { get; }
    public string? NamespaceName { get; }
    public string Accessibility { get; }
    public string ClassName { get; }
    public bool AllowUnknownErrorCodes { get; }
    public ImmutableArray<RuleCallModel> Rules { get; }
    public ImmutableArray<ErrorHintModel> Hints { get; }
    public ImmutableArray<ExampleHintModel> Examples { get; }
}

public sealed class RuleCallModel
{
    public RuleCallModel(
        string code,
        RuleMetadataShape shape,
        string? target,
        string? message,
        string? typedValueTypeName,
        ImmutableArray<MetadataValueModel> metadataValues,
        ImmutableArray<MetadataSchemaPropertyModel> metadataSchemaProperties
    )
    {
        Code = code;
        Shape = shape;
        Target = target;
        Message = message;
        TypedValueTypeName = typedValueTypeName;
        MetadataValues = metadataValues;
        MetadataSchemaProperties = metadataSchemaProperties;
    }

    public string Code { get; }
    public RuleMetadataShape Shape { get; }
    public string? Target { get; }
    public string? Message { get; }
    public string? TypedValueTypeName { get; }
    public ImmutableArray<MetadataValueModel> MetadataValues { get; }
    public ImmutableArray<MetadataSchemaPropertyModel> MetadataSchemaProperties { get; }
}

public sealed class MetadataValueModel
{
    public MetadataValueModel(string key, object? value, bool hasConstantValue, string typeName)
    {
        Key = key;
        Value = value;
        HasConstantValue = hasConstantValue;
        TypeName = typeName;
    }

    public string Key { get; }
    public object? Value { get; }
    public bool HasConstantValue { get; }
    public string TypeName { get; }
}

public sealed class MetadataSchemaPropertyModel
{
    public MetadataSchemaPropertyModel(string key, string typeName)
    {
        Key = key;
        TypeName = typeName;
    }

    public string Key { get; }
    public string TypeName { get; }
}

public sealed class ErrorHintModel
{
    public ErrorHintModel(
        string code,
        string? metadataTypeName,
        ImmutableArray<MetadataSchemaPropertyModel> metadataSchemaProperties
    )
    {
        Code = code;
        MetadataTypeName = metadataTypeName;
        MetadataSchemaProperties = metadataSchemaProperties;
    }

    public string Code { get; }
    public string? MetadataTypeName { get; }
    public ImmutableArray<MetadataSchemaPropertyModel> MetadataSchemaProperties { get; }
}

public sealed class ExampleHintModel
{
    public ExampleHintModel(
        string code,
        string? target,
        string? message,
        ImmutableArray<MetadataValueModel> metadataValues
    )
    {
        Code = code;
        Target = target;
        Message = message;
        MetadataValues = metadataValues;
    }

    public string Code { get; }
    public string? Target { get; }
    public string? Message { get; }
    public ImmutableArray<MetadataValueModel> MetadataValues { get; }
}

public enum RuleMetadataShape
{
    Registered = 0,
    TypedComparison = 1,
    TypedRange = 2
}

public sealed class RuleSchemaKeyComparer : IEqualityComparer<RuleCallModel>
{
    public static RuleSchemaKeyComparer Instance { get; } = new ();

    public bool Equals(RuleCallModel? x, RuleCallModel? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.Code == y.Code &&
               x.Shape == y.Shape &&
               x.TypedValueTypeName == y.TypedValueTypeName;
    }

    public int GetHashCode(RuleCallModel obj)
    {
        unchecked
        {
            var hashCode = 17;
            hashCode = hashCode * 31 + obj.Code.GetHashCode();
            hashCode = hashCode * 31 + obj.Shape.GetHashCode();
            hashCode = hashCode * 31 + (obj.TypedValueTypeName?.GetHashCode() ?? 0);
            return hashCode;
        }
    }
}

public sealed class InlineSchemaRuleComparer : IEqualityComparer<RuleCallModel>
{
    public static InlineSchemaRuleComparer Instance { get; } = new ();

    public bool Equals(RuleCallModel? x, RuleCallModel? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null ||
            x.Code != y.Code ||
            x.MetadataSchemaProperties.Length != y.MetadataSchemaProperties.Length)
        {
            return false;
        }

        for (var i = 0; i < x.MetadataSchemaProperties.Length; i++)
        {
            if (x.MetadataSchemaProperties[i].Key != y.MetadataSchemaProperties[i].Key ||
                x.MetadataSchemaProperties[i].TypeName != y.MetadataSchemaProperties[i].TypeName)
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(RuleCallModel obj)
    {
        unchecked
        {
            var hashCode = obj.Code.GetHashCode();
            foreach (var property in obj.MetadataSchemaProperties)
            {
                hashCode = hashCode * 31 + property.Key.GetHashCode();
                hashCode = hashCode * 31 + property.TypeName.GetHashCode();
            }

            return hashCode;
        }
    }
}
