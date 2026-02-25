using Light.PortableResults.SharedJsonSerialization;

namespace Light.PortableResults.CloudEvents.Writing;

/// <summary>
/// Represents resolved CloudEvents write settings that are frozen for a single serialization operation.
/// </summary>
/// <param name="MetadataSerializationMode">The mode that determines how metadata is serialized.</param>
public readonly record struct ResolvedCloudEventsWriteOptions(MetadataSerializationMode MetadataSerializationMode);
