# Deviations from Plan 0015-cloud-events-serialization

This document outlines the architectural and implementation deviations from the original `0015-cloud-events-serialization.md` plan. These deviations were introduced through subsequent optimization and streamlining plans to improve performance, reduce allocations, and better integrate with the `System.Text.Json` (STJ) pipeline.

## 1. Reading Performance Optimization (Zero-Copy Architecture)
*Reference: `0015-cloud-events-reading-performance-optimization.md`*

**Original Plan:**
The original plan implied parsing the `data` property of the CloudEvent envelope into a `JsonDocument` and serializing it back to a `byte[]` for later deserialization into the actual payload type.

**Deviation:**
To eliminate unnecessary allocations (`JsonDocument`, internal buffers, `byte[]` copy), a zero-copy architecture was implemented.
- `CloudEventEnvelopePayload` was changed to store position-based tracking (`DataStart`, `DataLength`) instead of a `byte[]` for the data segment.
- `CloudEventEnvelopeJsonReader.ReadEnvelope` tracks byte positions using `Utf8JsonReader.BytesConsumed` and skips the data subtree without parsing it into a `JsonDocument`.
- `ReadOnlyMemoryCloudEventExtensions` slices the original `ReadOnlyMemory<byte>` buffer using the tracked positions after deserialization completes, and passes this slice directly to the payload parser.

## 2. Writing Optimization and STJ Integration
*Reference: `0015-cloud-events-write-optimization.md`*

**Original Plan:**
The original plan specified `ToCloudEvent` and `WriteCloudEvent` extension methods that directly wrote to `Utf8JsonWriter`, bypassing the STJ pipeline for the envelope itself. It also used `MemoryStream` for byte array generation.

**Deviation:**
To align with the HTTP integration pattern and reduce memory pressure:
- `ToCloudEvent` was updated to use `RentedArrayBufferWriter` (an `IBufferWriter<byte>`) instead of `MemoryStream` to avoid double-copy allocations.
- The write path now finalizes pooled buffers through `FinishWriting()` and returns an `IRentedArray` handle for zero-copy handoff and deterministic pool return on disposal.
- Metadata filtering and extension attribute conversion were optimized to iterate and write directly inline, avoiding the allocation of intermediate `MetadataObject` instances where possible.

## 3. Writing Streamlining (Unified Envelope Converter)
*Reference: `0015-cloud-events-write-streamlining.md`*

**Original Plan / Intermediate State:**
The intermediate optimization plan introduced `CloudEventWriteResultJsonConverter` which required constructor injection of `LightResultsCloudEventWriteOptions`. This complicated DI scenarios and prevented stateless converters. The logic was also monolithic and tightly coupled in extension methods.

**Deviation:**
To enable full STJ pipeline integration, stateless converters, and clean separation of concerns:
- Introduced `ResolvedCloudEventWriteOptions` readonly record struct to capture frozen serialization options (e.g., `MetadataSerializationMode`).
- Introduced `CloudEventEnvelopeForWriting<T>` (and non-generic) readonly record struct to carry resolved Cloud Events attributes and the frozen options.
- Created a single, stateless `CloudEventEnvelopeForWritingJsonConverter` that serializes this envelope struct, handling both envelope attributes and Result data inline.
- The `ToCloudEvent` extension methods now construct this envelope struct and call `JsonSerializer.Serialize()`, delegating the actual writing entirely to the STJ pipeline.
- Legacy converters (`CloudEventWriteResultJsonConverter`, `CloudEventWriteResultJsonConverterFactory`) were removed.

## 4. Shared JSON Serialization Location
*Reference: `0015-cloud-events-serialization.md`*

**Original Plan:**
The plan stated that transport-agnostic JSON serialization helpers would be extracted from `Http/` into a `SharedJsonSerialization/` folder at the same level.

**Deviation:**
While the `SharedJsonSerialization` folder was created, the helpers were placed in `SharedJsonSerialization/Reading/` and `SharedJsonSerialization/Writing/` subfolders to better organize the code by concern, rather than keeping them all at the root of `SharedJsonSerialization/`.

## 5. Success Payload Wrapping
*Reference: `0015-cloud-events-write-streamlining.md`*

**Original Plan:**
For successful `Result<T>`, the `data` payload was supposed to contain the serialized value of `T` directly when success metadata is not included. It would only wrap the value in `{ "value": <T>, "metadata": { ... } }` if metadata was included.

**Deviation:**
As documented in the streamlining plan, the implementation now *always* wraps the success payload in an object. If no metadata exists, it writes `{ "value": <T> }`. This simplifies the JSON schema for consumers, as the payload is always an object with a `value` property, rather than sometimes being a primitive and sometimes an object.

## 6. CloudEvent ID Generation
*Reference: `0015-cloud-events-serialization.md`*

**Original Plan:**
The original plan implied using `Guid.NewGuid()` (UUIDv4) to generate unique identifiers for CloudEvents when an ID is not explicitly provided.

**Deviation:**
To improve sortability and database insertion performance for consumers of these events, the implementation was updated to use the `Ulid` package. It now generates a UUIDv7 via `Ulid.NewUlid().ToGuid().ToString()` instead of a standard UUIDv4.

## 7. MetadataValueAnnotation Flag Naming

**Original Plan:**
The plan specified `SerializeAsCloudEventExtensionAttribute = 8` for the flag indicating serialization into CloudEvents extension attributes, and `SerializeInCloudEventExtensionAttributeAndData` for the combination flag.

**Deviation:**
The implementation renamed these flags to better align with the existing `SerializeIn` prefix convention (used by `SerializeInHttpResponseBody`, `SerializeInHttpHeader`, etc.) and to use plural forms consistent with the CloudEvents specification name:
- `SerializeAsCloudEventExtensionAttribute` → `SerializeInCloudEventsExtensionAttributes` (changed prefix from `SerializeAs` to `SerializeIn`, singular to plural `CloudEvents`, singular to plural `Attributes`)
- `SerializeInCloudEventExtensionAttributeAndData` → `SerializeInCloudEventsExtensionAttributesAndData` (same plural adjustments)

## Summary
The core requirements of the original plan (CloudEvents v1.0 compliance, attribute resolution, data payload formatting) remain fulfilled. The deviations are purely architectural improvements focused on:
1. **Performance**: Zero-copy reading and reduced allocations during writing.
2. **Extensibility**: Full integration with the `System.Text.Json` pipeline, allowing callers to customize serialization via standard STJ mechanisms.
3. **Maintainability**: Clean separation of concerns using dedicated envelope structs and stateless converters.
