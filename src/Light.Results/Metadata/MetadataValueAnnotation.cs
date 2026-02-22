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
    /// Serialize this value inside the CloudEvents <c>data</c> payload.
    /// </summary>
    SerializeInCloudEventsData = 4,

    /// <summary>
    /// Serialize this value as a CloudEvents extension attribute.
    /// Only valid for primitive types.
    /// </summary>
    SerializeInCloudEventsExtensionAttributes = 8,

    /// <summary>
    /// Serialize this value in both CloudEvents extension attributes and in the CloudEvents <c>data</c> payload.
    /// </summary>
    SerializeInCloudEventsExtensionAttributesAndData =
        SerializeInCloudEventsData | SerializeInCloudEventsExtensionAttributes,

    /// <summary>
    /// Serialize this value in both HTTP response bodies and CloudEvents <c>data</c> payloads.
    /// </summary>
    SerializeInBodies = SerializeInHttpResponseBody | SerializeInCloudEventsData
}
