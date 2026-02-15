using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Default implementation of <see cref="ICloudEventAttributeConversionService" /> using a converter registry.
/// </summary>
public sealed class DefaultCloudEventAttributeConversionService : ICloudEventAttributeConversionService
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultCloudEventAttributeConversionService" />.
    /// </summary>
    /// <param name="converters">The converters keyed by metadata key.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="converters" /> is <see langword="null" />.</exception>
    public DefaultCloudEventAttributeConversionService(
        FrozenDictionary<string, CloudEventAttributeConverter> converters
    )
    {
        Converters = converters ?? throw new ArgumentNullException(nameof(converters));
    }

    /// <summary>
    /// Gets the singleton default conversion service without custom converters.
    /// </summary>
    public static DefaultCloudEventAttributeConversionService Instance { get; } = new (
        new Dictionary<string, CloudEventAttributeConverter>(StringComparer.Ordinal).ToFrozenDictionary(
            StringComparer.Ordinal
        )
    );

    /// <summary>
    /// Gets the converters keyed by metadata key.
    /// </summary>
    public FrozenDictionary<string, CloudEventAttributeConverter> Converters { get; }

    /// <summary>
    /// Converts a metadata value into a CloudEvent attribute.
    /// </summary>
    public KeyValuePair<string, MetadataValue> PrepareCloudEventAttribute(
        string metadataKey,
        MetadataValue metadataValue
    )
    {
        if (metadataKey is null)
        {
            throw new ArgumentNullException(nameof(metadataKey));
        }

        var converted = Converters.TryGetValue(metadataKey, out var targetConverter) ?
            targetConverter.PrepareCloudEventAttribute(metadataKey, metadataValue) :
            new KeyValuePair<string, MetadataValue>(metadataKey, metadataValue);

        ValidateAttributeName(converted.Key);
        ValidateAttributeValue(converted.Key, converted.Value);

        return converted;
    }

    private static void ValidateAttributeName(string attributeName)
    {
        if (string.IsNullOrWhiteSpace(attributeName))
        {
            throw new ArgumentException(
                "CloudEvent attribute names must not be null or whitespace.",
                nameof(attributeName)
            );
        }

        if (CloudEventConstants.ForbiddenConvertedAttributeNames.Contains(attributeName))
        {
            throw new ArgumentException(
                $"The CloudEvent attribute '{attributeName}' is reserved and cannot be set via metadata conversion.",
                nameof(attributeName)
            );
        }

        if (CloudEventConstants.StandardAttributeNames.Contains(attributeName))
        {
            return;
        }

        if (!IsValidExtensionAttributeName(attributeName))
        {
            throw new ArgumentException(
                $"The CloudEvent extension attribute '{attributeName}' is invalid. Only lowercase alphanumeric names are allowed.",
                nameof(attributeName)
            );
        }
    }

    private static bool IsValidExtensionAttributeName(string attributeName)
    {
        foreach(var character in attributeName)
        {
            if (character is (< 'a' or > 'z') and (< '0' or > '9'))
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidateAttributeValue(string attributeName, MetadataValue value)
    {
        if (CloudEventConstants.StandardAttributeNames.Contains(attributeName))
        {
            return;
        }

        if (!value.Kind.IsPrimitive())
        {
            throw new ArgumentException(
                $"The CloudEvent extension attribute '{attributeName}' must be a primitive JSON value.",
                nameof(value)
            );
        }
    }
}
