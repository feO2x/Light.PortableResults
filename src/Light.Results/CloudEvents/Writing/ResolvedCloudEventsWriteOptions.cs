using Light.Results.SharedJsonSerialization;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Represents resolved CloudEvents write settings that are frozen for a single serialization operation.
/// </summary>
/// <param name="MetadataSerializationMode">The mode that determines how metadata is serialized.</param>
public readonly record struct ResolvedCloudEventsWriteOptions(MetadataSerializationMode MetadataSerializationMode);
