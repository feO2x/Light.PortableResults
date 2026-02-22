using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Default implementation of <see cref="ICloudEventsAttributeParsingService" /> using a parser registry.
/// </summary>
public sealed class DefaultCloudEventsAttributeParsingService : ICloudEventsAttributeParsingService
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultCloudEventsAttributeParsingService" />.
    /// </summary>
    /// <param name="parsers">The optional frozen dictionary of parsers keyed by CloudEvents extension attribute name.</param>
    /// <param name="conflictStrategy">How conflicts are handled when multiple attributes map to the same metadata key.</param>
    /// <param name="metadataAnnotation">The annotation applied to metadata values originating from CloudEvents extension attributes.</param>
    public DefaultCloudEventsAttributeParsingService(
        FrozenDictionary<string, CloudEventsAttributeParser>? parsers = null,
        CloudEventsAttributeConflictStrategy conflictStrategy = CloudEventsAttributeConflictStrategy.Throw,
        MetadataValueAnnotation metadataAnnotation = MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
    )
    {
        Parsers = parsers ?? EmptyParsers;
        ConflictStrategy = conflictStrategy;
        MetadataAnnotation = metadataAnnotation;
    }

    /// <summary>
    /// Gets a frozen dictionary containing no parsers.
    /// </summary>
    public static FrozenDictionary<string, CloudEventsAttributeParser> EmptyParsers { get; } =
        new Dictionary<string, CloudEventsAttributeParser>(StringComparer.Ordinal).ToFrozenDictionary(
            StringComparer.Ordinal
        );

    /// <summary>
    /// Gets the parsers keyed by CloudEvents extension attribute name.
    /// </summary>
    public FrozenDictionary<string, CloudEventsAttributeParser> Parsers { get; }

    /// <summary>
    /// Gets how conflicts are handled when multiple attributes map to the same metadata key.
    /// </summary>
    public CloudEventsAttributeConflictStrategy ConflictStrategy { get; }

    /// <summary>
    /// Gets the annotation applied to metadata values originating from CloudEvents extension attributes.
    /// </summary>
    public MetadataValueAnnotation MetadataAnnotation { get; }

    /// <summary>
    /// Parses all extension attributes into metadata.
    /// </summary>
    /// <param name="extensionAttributes">The CloudEvents extension attributes to parse.</param>
    /// <returns>A <see cref="MetadataObject" /> containing the parsed metadata, or <see langword="null" /> if no entries were produced.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="ConflictStrategy" /> is <see cref="CloudEventsAttributeConflictStrategy.Throw" /> and multiple attributes map to the same metadata key.</exception>
    public MetadataObject? ReadExtensionMetadata(MetadataObject extensionAttributes)
    {
        if (extensionAttributes.Count == 0)
        {
            return null;
        }

        using var builder = MetadataObjectBuilder.Create(extensionAttributes.Count);
        foreach (var keyValuePair in extensionAttributes)
        {
            var metadataEntry = ParseExtensionAttribute(keyValuePair.Key, keyValuePair.Value, MetadataAnnotation);

            if (builder.TryGetValue(metadataEntry.Key, out _))
            {
                if (ConflictStrategy == CloudEventsAttributeConflictStrategy.Throw)
                {
                    throw new InvalidOperationException(
                        $"CloudEvents attribute '{keyValuePair.Key}' maps to metadata key '{metadataEntry.Key}', which is already present."
                    );
                }

                builder.AddOrReplace(metadataEntry.Key, metadataEntry.Value);
                continue;
            }

            builder.Add(metadataEntry.Key, metadataEntry.Value);
        }

        return builder.Count == 0 ? null : builder.Build();
    }

    /// <summary>
    /// Parses a single CloudEvents extension attribute.
    /// </summary>
    /// <param name="attributeName">The CloudEvents extension attribute name.</param>
    /// <param name="value">The raw metadata value from the CloudEvents extension attribute.</param>
    /// <param name="annotation">The annotation to apply to the parsed metadata value.</param>
    /// <returns>The parsed metadata key and value pair.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="attributeName" /> is <see langword="null" />.</exception>
    public KeyValuePair<string, MetadataValue> ParseExtensionAttribute(
        string attributeName,
        MetadataValue value,
        MetadataValueAnnotation annotation
    )
    {
        if (attributeName is null)
        {
            throw new ArgumentNullException(nameof(attributeName));
        }

        if (Parsers.TryGetValue(attributeName, out var parser))
        {
            var parsedValue = parser.ParseAttribute(attributeName, value, annotation);
            return new KeyValuePair<string, MetadataValue>(parser.MetadataKey, parsedValue);
        }

        var defaultValue = value.Kind.IsPrimitive() ?
            MetadataValueAnnotationHelper.WithAnnotation(value, annotation) :
            MetadataValueAnnotationHelper.WithAnnotation(value, MetadataValueAnnotation.SerializeInCloudEventsData);
        return new KeyValuePair<string, MetadataValue>(attributeName, defaultValue);
    }
}
