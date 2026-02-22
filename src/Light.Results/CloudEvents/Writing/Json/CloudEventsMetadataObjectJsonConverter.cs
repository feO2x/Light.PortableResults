using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization.Writing;

namespace Light.Results.CloudEvents.Writing.Json;

/// <summary>
/// JSON converter for writing <see cref="MetadataObject" /> payloads in CloudEvents.
/// </summary>
public sealed class CloudEventsMetadataObjectJsonConverter : JsonConverter<MetadataObject>
{
    /// <inheritdoc />
    public override MetadataObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException(
            $"{nameof(CloudEventsMetadataObjectJsonConverter)} supports serialization only. Use a deserialization converter for reading."
        );
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, MetadataObject value, JsonSerializerOptions options) =>
        writer.WriteMetadataObject(value, requiredAnnotation: MetadataValueAnnotation.SerializeInCloudEventsData);
}
