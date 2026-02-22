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
    /// <param name="extensionAttributes">The CloudEvents extension attributes to parse.</param>
    /// <returns>A <see cref="MetadataObject" /> containing the parsed metadata, or <see langword="null" /> if no entries were produced.</returns>
    MetadataObject? ReadExtensionMetadata(MetadataObject extensionAttributes);

    /// <summary>
    /// Parses a single CloudEvents extension attribute into a metadata key and value pair.
    /// </summary>
    /// <param name="attributeName">The CloudEvents extension attribute name.</param>
    /// <param name="value">The raw metadata value from the CloudEvents extension attribute.</param>
    /// <param name="annotation">The annotation to apply to the parsed metadata value.</param>
    /// <returns>The parsed metadata key and value pair.</returns>
    KeyValuePair<string, MetadataValue> ParseExtensionAttribute(
        string attributeName,
        MetadataValue value,
        MetadataValueAnnotation annotation
    );
}
