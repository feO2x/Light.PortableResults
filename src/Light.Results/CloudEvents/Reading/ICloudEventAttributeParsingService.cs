using System.Collections.Generic;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Maps CloudEvent extension attributes back to Light.Results metadata values.
/// </summary>
public interface ICloudEventAttributeParsingService
{
    /// <summary>
    /// Parses all extension attributes into metadata values.
    /// Returns <see langword="null" /> when no metadata entries are produced.
    /// </summary>
    MetadataObject? ReadExtensionMetadata(MetadataObject extensionAttributes);

    /// <summary>
    /// Parses a single CloudEvent extension attribute into a metadata key and value pair.
    /// </summary>
    KeyValuePair<string, MetadataValue> ParseExtensionAttribute(
        string attributeName,
        MetadataValue value,
        MetadataValueAnnotation annotation
    );
}
