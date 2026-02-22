using System.Collections.Generic;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Maps CloudEvents extension attributes back to Light.Results metadata values.
/// </summary>
public interface ICloudEventsAttributeParsingService
{
    /// <summary>
    /// Parses all extension attributes into metadata values.
    /// Returns <see langword="null" /> when no metadata entries are produced.
    /// </summary>
    MetadataObject? ReadExtensionMetadata(MetadataObject extensionAttributes);

    /// <summary>
    /// Parses a single CloudEvents extension attribute into a metadata key and value pair.
    /// </summary>
    KeyValuePair<string, MetadataValue> ParseExtensionAttribute(
        string attributeName,
        MetadataValue value,
        MetadataValueAnnotation annotation
    );
}
