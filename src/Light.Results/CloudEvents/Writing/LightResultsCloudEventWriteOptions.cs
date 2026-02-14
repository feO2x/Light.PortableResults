using System.Text.Json;
using Light.Results.SharedJsonSerialization;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Configures how Light.Results values are serialized to CloudEvents JSON envelopes.
/// </summary>
public sealed record LightResultsCloudEventWriteOptions
{
    /// <summary>
    /// Gets the default options instance for CloudEvent serialization.
    /// </summary>
    public static LightResultsCloudEventWriteOptions Default { get; } = new ();

    /// <summary>
    /// Gets or sets the default source URI-reference used when no source is provided per call.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets when result metadata should be serialized in CloudEvent data payloads.
    /// </summary>
    public MetadataSerializationMode MetadataSerializationMode { get; set; } = MetadataSerializationMode.Always;

    /// <summary>
    /// Gets or sets serializer options used for result value serialization.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; } = Http.Reading.Module.CreateDefaultSerializerOptions();

    /// <summary>
    /// Gets or sets the conversion service used to map metadata entries to CloudEvent attributes.
    /// </summary>
    public ICloudEventAttributeConversionService ConversionService { get; set; } =
        DefaultCloudEventAttributeConversionService.Instance;
}
