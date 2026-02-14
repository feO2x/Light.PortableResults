using System;
using System.Collections.Immutable;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Base type for parsing CloudEvent extension attributes into metadata values.
/// </summary>
public abstract class CloudEventAttributeParser
{
    /// <summary>
    /// Initializes a new instance of <see cref="CloudEventAttributeParser" />.
    /// </summary>
    /// <param name="metadataKey">The metadata key to use for parsed values.</param>
    /// <param name="supportedAttributeNames">The CloudEvent extension attribute names supported by this parser.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="metadataKey" /> is null/whitespace or
    /// <paramref name="supportedAttributeNames" /> is default/empty.
    /// </exception>
    protected CloudEventAttributeParser(string metadataKey, ImmutableArray<string> supportedAttributeNames)
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
    /// Parses the specified CloudEvent extension attribute value into metadata.
    /// </summary>
    public abstract MetadataValue ParseAttribute(
        string attributeName,
        MetadataValue value,
        MetadataValueAnnotation annotation
    );
}
