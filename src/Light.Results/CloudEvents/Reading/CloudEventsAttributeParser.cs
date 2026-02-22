using System;
using System.Collections.Immutable;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Base type for parsing CloudEvents extension attributes into metadata values.
/// </summary>
public abstract class CloudEventsAttributeParser
{
    /// <summary>
    /// Initializes a new instance of <see cref="CloudEventsAttributeParser" />.
    /// </summary>
    /// <param name="metadataKey">The metadata key to use for parsed values.</param>
    /// <param name="supportedAttributeNames">The CloudEvents extension attribute names supported by this parser.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="metadataKey" /> is null/whitespace or
    /// <paramref name="supportedAttributeNames" /> is default/empty.
    /// </exception>
    protected CloudEventsAttributeParser(string metadataKey, ImmutableArray<string> supportedAttributeNames)
    {
        if (string.IsNullOrWhiteSpace(metadataKey))
        {
            throw new ArgumentException("Metadata key must not be null or whitespace.", nameof(metadataKey));
        }

        if (supportedAttributeNames.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                $"{nameof(supportedAttributeNames)} must not be empty",
                nameof(supportedAttributeNames)
            );
        }

        MetadataKey = metadataKey;
        SupportedAttributeNames = supportedAttributeNames;
    }

    /// <summary>
    /// Gets the metadata key assigned to parsed values.
    /// </summary>
    public string MetadataKey { get; }

    /// <summary>
    /// Gets the attribute names supported by this parser.
    /// </summary>
    public ImmutableArray<string> SupportedAttributeNames { get; }

    /// <summary>
    /// Parses the specified CloudEvents extension attribute value into metadata.
    /// </summary>
    /// <param name="attributeName">The CloudEvents extension attribute name.</param>
    /// <param name="value">The raw metadata value from the CloudEvents extension attribute.</param>
    /// <param name="annotation">The annotation to apply to the parsed metadata value.</param>
    /// <returns>The parsed metadata value.</returns>
    public abstract MetadataValue ParseAttribute(
        string attributeName,
        MetadataValue value,
        MetadataValueAnnotation annotation
    );
}
