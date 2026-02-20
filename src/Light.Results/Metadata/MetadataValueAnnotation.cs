using System;

namespace Light.Results.Metadata;

/// <summary>
/// Specifies where a metadata value should be serialized.
/// </summary>
[Flags]
public enum MetadataValueAnnotation
{
    /// <summary>
    /// No annotation specified. Metadata will not be serialized.
    /// </summary>
    None = 0,

    /// <summary>
    /// Serialize this value in the HTTP response body.
    /// </summary>
    SerializeInHttpResponseBody = 1,

    /// <summary>
    /// Serialize this value as an HTTP response header.
    /// Only valid for primitive types and arrays of primitives.
    /// </summary>
    SerializeInHttpHeader = 2,

    /// <summary>
    /// Serialize this value in both the HTTP response body and as a header.
    /// </summary>
    SerializeInHttpHeaderAndBody = SerializeInHttpResponseBody | SerializeInHttpHeader,

    /// <summary>
    /// Serialize this value inside the CloudEvent <c>data</c> payload.
    /// </summary>
    SerializeInCloudEventData = 4,

    /// <summary>
    /// Serialize this value as a CloudEvent extension attribute.
    /// Only valid for primitive types and arrays of primitives.
    /// </summary>
    SerializeAsCloudEventExtensionAttribute = 8,

    /// <summary>
    /// Serialize this value in both CloudEvent extension attributes and in the CloudEvent <c>data</c> payload.
    /// </summary>
    SerializeInCloudEventExtensionAttributeAndData =
        SerializeInCloudEventData | SerializeAsCloudEventExtensionAttribute,

    /// <summary>
    /// Serialize this value in both HTTP response bodies and CloudEvent <c>data</c> payloads.
    /// </summary>
    SerializeInBodies = SerializeInHttpResponseBody | SerializeInCloudEventData
}
