using System;
using System.Buffers;
using System.Text.Json;
using Light.PortableResults.Buffers;
using Light.PortableResults.SharedJsonSerialization;

namespace Light.PortableResults.CloudEvents.Writing;

/// <summary>
/// Configures how Light.PortableResults values are serialized to CloudEvents JSON envelopes.
/// </summary>
public sealed record PortableResultsCloudEventsWriteOptions
{
    /// <summary>
    /// Gets the default options instance for CloudEvents serialization.
    /// </summary>
    public static PortableResultsCloudEventsWriteOptions Default { get; } = new ();

    /// <summary>
    /// Gets or sets the default source URI-reference used when no source is provided per call.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets when result metadata should be serialized in CloudEvents data payloads.
    /// </summary>
    public MetadataSerializationMode MetadataSerializationMode { get; set; } = MetadataSerializationMode.Always;

    /// <summary>
    /// Gets or sets serializer options used for result value serialization.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; } = Module.DefaultSerializerOptions;

    /// <summary>
    /// Gets or sets the conversion service used to map metadata entries to CloudEvents attributes.
    /// </summary>
    public ICloudEventsAttributeConversionService ConversionService { get; set; } =
        DefaultCloudEventsAttributeConversionService.Instance;

    /// <summary>
    /// Gets or sets the CloudEvents type for successful results. Used by STJ converters.
    /// </summary>
    public string? SuccessType { get; set; }

    /// <summary>
    /// Gets or sets the CloudEvents type for failed results. Used by STJ converters.
    /// </summary>
    public string? FailureType { get; set; }

    /// <summary>
    /// Gets or sets the CloudEvents subject. Used by STJ converters.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the CloudEvents data schema URI. Used by STJ converters.
    /// </summary>
    public string? DataSchema { get; set; }

    /// <summary>
    /// Gets or sets the CloudEvents time. When null, UTC now is used. Used by STJ converters.
    /// </summary>
    public DateTimeOffset? Time { get; set; }

    /// <summary>
    /// Gets or sets a factory function to generate unique CloudEvents IDs.
    /// Defaults to generating a new UUIDv7 string.
    /// </summary>
    public Func<string>? IdResolver { get; set; }

    /// <summary>
    /// Gets or sets the array pool used for writing CloudEvents.
    /// </summary>
    public ArrayPool<byte> ArrayPool { get; set; } = ArrayPool<byte>.Shared;

    /// <summary>
    /// Gets or sets the initial array capacity used for writing CloudEvents. The default is 2048 bytes,
    /// see <see cref="RentedArrayBufferWriter.DefaultInitialCapacity" />.
    /// </summary>
    public int PooledArrayInitialCapacity { get; set; } = RentedArrayBufferWriter.DefaultInitialCapacity;
}
