using System.Collections.Generic;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Converts metadata values into CloudEvent attributes.
/// </summary>
public interface ICloudEventsAttributeConversionService
{
    /// <summary>
    /// Converts a metadata value into a CloudEvent attribute.
    /// </summary>
    /// <param name="metadataKey">The metadata key.</param>
    /// <param name="metadataValue">The metadata value.</param>
    /// <returns>The CloudEvent attribute key and value pair.</returns>
    KeyValuePair<string, MetadataValue> PrepareCloudEventAttribute(string metadataKey, MetadataValue metadataValue);
}
