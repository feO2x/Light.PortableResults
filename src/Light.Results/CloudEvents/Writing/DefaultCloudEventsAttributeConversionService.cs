using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Default implementation of <see cref="ICloudEventsAttributeConversionService" /> using a converter registry.
/// </summary>
public sealed class DefaultCloudEventsAttributeConversionService : ICloudEventsAttributeConversionService
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultCloudEventsAttributeConversionService" />.
    /// </summary>
    /// <param name="converters">The converters keyed by metadata key.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="converters" /> is <see langword="null" />.</exception>
    public DefaultCloudEventsAttributeConversionService(
        FrozenDictionary<string, CloudEventsAttributeConverter> converters
    )
    {
        Converters = converters ?? throw new ArgumentNullException(nameof(converters));
    }

    /// <summary>
    /// Gets the singleton default conversion service without custom converters.
    /// </summary>
    public static DefaultCloudEventsAttributeConversionService Instance { get; } = new (
        new Dictionary<string, CloudEventsAttributeConverter>(StringComparer.Ordinal).ToFrozenDictionary(
            StringComparer.Ordinal
        )
    );

    /// <summary>
    /// Gets the converters keyed by metadata key.
    /// </summary>
    public FrozenDictionary<string, CloudEventsAttributeConverter> Converters { get; }

    /// <summary>
    /// Converts a metadata value into a CloudEvents attribute.
    /// </summary>
    /// <param name="metadataKey">The metadata key to convert.</param>
    /// <param name="metadataValue">The metadata value to convert.</param>
    /// <returns>The CloudEvents attribute key and value pair.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metadataKey" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when the resulting attribute name is invalid, reserved, or the value is not a primitive type for extension attributes.</exception>
    public KeyValuePair<string, MetadataValue> PrepareCloudEventsAttribute(
        string metadataKey,
        MetadataValue metadataValue
    )
    {
        if (metadataKey is null)
        {
            throw new ArgumentNullException(nameof(metadataKey));
        }

        var converted = Converters.TryGetValue(metadataKey, out var targetConverter) ?
            targetConverter.PrepareCloudEventsAttribute(metadataKey, metadataValue) :
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
                "CloudEvents attribute names must not be null or whitespace.",
                nameof(attributeName)
            );
        }

        if (CloudEventsConstants.ForbiddenConvertedAttributeNames.Contains(attributeName))
        {
            throw new ArgumentException(
                $"The CloudEvents attribute '{attributeName}' is reserved and cannot be set via metadata conversion.",
                nameof(attributeName)
            );
        }

        if (CloudEventsConstants.StandardAttributeNames.Contains(attributeName))
        {
            return;
        }

        if (!IsValidExtensionAttributeName(attributeName))
        {
            throw new ArgumentException(
                $"The CloudEvents extension attribute '{attributeName}' is invalid. Only lowercase alphanumeric names are allowed.",
                nameof(attributeName)
            );
        }
    }

    private static bool IsValidExtensionAttributeName(string attributeName)
    {
        foreach (var character in attributeName)
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
        if (CloudEventsConstants.StandardAttributeNames.Contains(attributeName))
        {
            return;
        }

        if (!value.Kind.IsPrimitive())
        {
            throw new ArgumentException(
                $"The CloudEvents extension attribute '{attributeName}' must be a primitive JSON value.",
                nameof(value)
            );
        }
    }
}
