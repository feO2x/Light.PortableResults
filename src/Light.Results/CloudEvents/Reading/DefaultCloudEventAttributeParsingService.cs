using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Default implementation of <see cref="ICloudEventAttributeParsingService" /> using a parser registry.
/// </summary>
public sealed class DefaultCloudEventAttributeParsingService : ICloudEventAttributeParsingService
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultCloudEventAttributeParsingService" />.
    /// </summary>
    public DefaultCloudEventAttributeParsingService(
        FrozenDictionary<string, CloudEventAttributeParser>? parsers = null,
        CloudEventAttributeConflictStrategy conflictStrategy = CloudEventAttributeConflictStrategy.Throw,
        MetadataValueAnnotation metadataAnnotation = MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
    )
    {
        Parsers = parsers ?? EmptyParsers;
        ConflictStrategy = conflictStrategy;
        MetadataAnnotation = metadataAnnotation;
    }

    /// <summary>
    /// Gets a frozen dictionary containing no parsers.
    /// </summary>
    public static FrozenDictionary<string, CloudEventAttributeParser> EmptyParsers { get; } =
        new Dictionary<string, CloudEventAttributeParser>(StringComparer.Ordinal).ToFrozenDictionary(
            StringComparer.Ordinal
        );

    /// <summary>
    /// Gets the parsers keyed by CloudEvent extension attribute name.
    /// </summary>
    public FrozenDictionary<string, CloudEventAttributeParser> Parsers { get; }

    /// <summary>
    /// Gets how conflicts are handled when multiple attributes map to the same metadata key.
    /// </summary>
    public CloudEventAttributeConflictStrategy ConflictStrategy { get; }

    /// <summary>
    /// Gets the annotation applied to metadata values originating from CloudEvent extension attributes.
    /// </summary>
    public MetadataValueAnnotation MetadataAnnotation { get; }

    /// <summary>
    /// Parses all extension attributes into metadata.
    /// </summary>
    public MetadataObject? ReadExtensionMetadata(MetadataObject extensionAttributes)
    {
        using var builder = MetadataObjectBuilder.Create(extensionAttributes.Count);
        foreach (var keyValuePair in extensionAttributes)
        {
            var metadataEntry = ParseExtensionAttribute(keyValuePair.Key, keyValuePair.Value, MetadataAnnotation);

            if (builder.TryGetValue(metadataEntry.Key, out _))
            {
                if (ConflictStrategy == CloudEventAttributeConflictStrategy.Throw)
                {
                    throw new InvalidOperationException(
                        $"CloudEvent attribute '{keyValuePair.Key}' maps to metadata key '{metadataEntry.Key}', which is already present."
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
    /// Parses a single CloudEvent extension attribute.
    /// </summary>
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

        var defaultValue = IsPrimitive(value.Kind) ?
            MetadataValueAnnotationHelper.WithAnnotation(value, annotation) :
            MetadataValueAnnotationHelper.WithAnnotation(value, MetadataValueAnnotation.SerializeInCloudEventData);
        return new KeyValuePair<string, MetadataValue>(attributeName, defaultValue);
    }

    private static bool IsPrimitive(MetadataKind metadataKind) =>
        metadataKind == MetadataKind.Null ||
        metadataKind == MetadataKind.Boolean ||
        metadataKind == MetadataKind.Int64 ||
        metadataKind == MetadataKind.Double ||
        metadataKind == MetadataKind.String;
}
