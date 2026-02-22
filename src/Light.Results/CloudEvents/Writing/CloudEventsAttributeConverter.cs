using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Base type for converting metadata values into CloudEvents attributes.
/// </summary>
public abstract class CloudEventsAttributeConverter
{
    /// <summary>
    /// Initializes a new instance of <see cref="CloudEventsAttributeConverter" />.
    /// </summary>
    /// <param name="supportedMetadataKeys">The metadata keys supported by this converter.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="supportedMetadataKeys" /> is default or empty.
    /// </exception>
    protected CloudEventsAttributeConverter(ImmutableArray<string> supportedMetadataKeys)
    {
        if (supportedMetadataKeys.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                $"{nameof(supportedMetadataKeys)} must not be empty",
                nameof(supportedMetadataKeys)
            );
        }

        SupportedMetadataKeys = supportedMetadataKeys;
    }

    /// <summary>
    /// Gets the metadata keys supported by this converter.
    /// </summary>
    public ImmutableArray<string> SupportedMetadataKeys { get; }

    /// <summary>
    /// Converts the specified metadata value into a CloudEvents attribute key and value pair.
    /// </summary>
    /// <param name="metadataKey">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The CloudEvents attribute key and value pair.</returns>
    public abstract KeyValuePair<string, MetadataValue> PrepareCloudEventsAttribute(
        string metadataKey,
        MetadataValue value
    );
}
