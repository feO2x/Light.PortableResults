using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Defines the metadata-related members that a CloudEvents envelope must expose to participate in metadata-aware
/// helper logic.
/// </summary>
/// <typeparam name="TResult">The type of the result associated with the CloudEvents payload.</typeparam>
public interface ICloudEventsEnvelopeForWritingWithMetadata<out TResult>
    where TResult : IHasOptionalMetadata
{
    /// <summary>
    /// Gets the resolved CloudEvents write options that govern whether metadata should be serialized.
    /// </summary>
    ResolvedCloudEventsWriteOptions ResolvedOptions { get; }

    /// <summary>
    /// Gets the result associated with the CloudEvents payload, if any.
    /// </summary>
    TResult Data { get; }
}
