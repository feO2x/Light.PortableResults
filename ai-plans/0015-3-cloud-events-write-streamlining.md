# CloudEvents JSON Writing Streamlining

## Rationale

The current CloudEvents writing implementation bypasses the STJ pipeline by not calling `JsonSerializer.Serialize()` in `CloudEventResultExtensions`. While this approach was optimized in the previous iteration (replacing `MemoryStream` with `PooledByteBufferWriter`), it has fundamental architectural limitations:

1. **No converter customization**: Callers cannot exchange the default JSON converters for `Result`, `Result<T>`, or metadata types because STJ's `JsonSerializer.Serialize()` is never invoked.

2. **Constructor injection required**: `CloudEventWriteResultJsonConverter` requires `LightResultsCloudEventWriteOptions` to be passed at construction time, complicating DI scenarios and preventing stateless converters.

3. **Monolithic logic**: Envelope construction, attribute resolution, extension attribute writing, and data serialization are tightly coupled in a single class with many private helper methods.

4. **Limited composability**: Messaging libraries that use STJ cannot easily integrate with the current design.

This plan introduces the use of an intermediary **readonly record struct** (`CloudEventEnvelopeForWriting<T>`) that represents a fully-resolved Cloud Events envelope with frozen serialization options. A single JSON converter serializes this envelope type, handling both envelope attributes and Result data inline. This design enables full STJ pipeline integration, stateless converters, and clean separation of concerns.

We still use `Utf8JsonWriter` with `PooledByteBufferWriter`, the former is passed to `JsonSerializer.Serialize` to invoke the STJ pipeline correctly.

## Acceptance Criteria

### Writing-Specific Envelope Types
- [ ] A `ResolvedCloudEventWriteOptions` readonly record struct captures frozen serialization options (e.g., `MetadataSerializationMode`).
- [ ] A `CloudEventEnvelopeForWriting<T>` readonly record struct (and non-generic variant) carries resolved Cloud Events attributes and the frozen options.
- [ ] These types are separate from the reading envelope (`CloudEventEnvelope<T>`) to avoid conflating read and write concerns.

### Envelope JSON Converters
- [ ] `CloudEventEnvelopeForWritingJsonConverter` serializes `CloudEventEnvelopeForWriting` by writing envelope attributes and Result data inline.
- [ ] `CloudEventEnvelopeForWritingJsonConverter<T>` serializes `CloudEventEnvelopeForWriting<T>` similarly.
- [ ] `CloudEventEnvelopeForWritingJsonConverterFactory` creates generic envelope converters for any `CloudEventEnvelopeForWriting<T>`.
- [ ] Converters are **stateless** (no constructor parameters).
- [ ] Converters are **write-only** (`Read` throws `NotSupportedException`). Reading uses a separate flow with `CloudEventEnvelopePayload` for byte offset tracking.
- [ ] Converters serialize Result data (value/errors/metadata) inline with direct access to `envelope.ResolvedOptions.MetadataMode`.

### Extension Methods
- [ ] `ToCloudEventEnvelopeForWriting()` extension methods on `Result` and `Result<T>` construct the envelope struct with resolved attributes and frozen options.
- [ ] `ToCloudEvent()` extension methods remain available, internally constructing the envelope and calling `JsonSerializer.Serialize()`.
- [ ] `WriteCloudEvent(Utf8JsonWriter)` methods remain for callers who need direct writer control.

### Module Registration
- [ ] `AddDefaultLightResultsCloudEventWriteJsonConverters()` registers the envelope converters without requiring options at registration time.

### Code Hygiene
- [ ] Legacy converter classes (`CloudEventWriteResultJsonConverter`, `CloudEventWriteResultJsonConverterFactory`) are removed.
- [ ] Complex logic in `CloudEventResultExtensions` is simplified by delegating to STJ.
- [ ] Existing tests pass with no regressions.
- [ ] New tests verify envelope construction and serialization.

### Benchmarks
- [ ] Benchmarks compare the new STJ-based approach against the previous direct-write approach.
- [ ] Memory allocations are tracked to ensure no regression.

## Technical Details

### Writing-Specific Envelope Types

The existing `CloudEventEnvelope<T>` types in `src/Light.Results/CloudEvents/CloudEventEnvelope.cs` are used for **reading** and should remain focused on that concern. Writing requires additional context (serialization options) that reading doesn't need.

#### ResolvedCloudEventWriteOptions

A frozen struct capturing resolved serialization state:

```csharp
public readonly record struct ResolvedCloudEventWriteOptions(
    MetadataSerializationMode MetadataMode
    // Future resolved settings added here as needed
);
```

This struct is constructed in `ToCloudEventEnvelopeForWriting()` by merging:
- Global `LightResultsCloudEventWriteOptions` (from DI or defaults)
- Per-call parameter overrides
- Per-call options overrides

Note: `JsonSerializerOptions` is NOT included because it's already part of the STJ pipeline.

#### CloudEventEnvelopeForWriting

```csharp
public readonly record struct CloudEventEnvelopeForWriting<T>(
    string Type,
    string Source,
    string Id,
    Result<T> Data,
    ResolvedCloudEventWriteOptions ResolvedOptions,
    string? Subject = null,
    DateTimeOffset? Time = null,
    string? DataContentType = null,
    string? DataSchema = null,
    MetadataObject? ExtensionAttributes = null
);
```

A non-generic `CloudEventEnvelopeForWriting` (with `Result Data`) follows the same pattern.

The `ToCloudEventEnvelopeForWriting()` extension method handles:
- Resolving `Type` based on `IsValid` (success vs failure type)
- Generating `Id` if not provided
- Extracting extension attributes from result metadata
- Validating `Source` as URI-reference and `DataSchema` as absolute URI
- Creating `ResolvedCloudEventWriteOptions` from merged configuration sources

### Converter Architecture

```
CloudEvents/Writing/
├── ResolvedCloudEventWriteOptions.cs
├── CloudEventEnvelopeForWriting.cs
├── Json/
│   └── CloudEventEnvelopeForWritingJsonConverter.cs
│       ├── CloudEventEnvelopeForWritingJsonConverter (non-generic)
│       ├── CloudEventEnvelopeForWritingJsonConverter<T>
│       └── CloudEventEnvelopeForWritingJsonConverterFactory
```

#### Converter Responsibilities

The single converter handles both envelope attributes and Result data serialization:

**Envelope attributes:**
1. `specversion` (always "1.0")
2. `type`, `source`, `id`, `time` (required attributes)
3. `subject`, `dataschema` (if present)
4. `datacontenttype` (always "application/json")
5. Extension attributes (iterating `ExtensionAttributes` MetadataObject)

**Result data (written inline as `data` property):**

*For success:*
- Non-generic `Result`: writes `{ "metadata": { ... } }` if metadata exists with `SerializeInCloudEventData` annotation, otherwise writes `null` or empty object.
- Generic `Result<T>`: writes `{ "value": ..., "metadata": { ... } }` as a wrapped object. If no metadata exists, writes just `{ "value": ... }` (still wrapped).

*For failure:*
- Writes `{ "errors": [...], "metadata": { ... } }` using existing error serialization helpers.

The converter accesses `envelope.ResolvedOptions.MetadataMode` directly to determine metadata serialization behavior. This eliminates the need to pass options between converters through the STJ pipeline.

**Implementation details:**
- **Value serialization**: Uses `JsonSerializer.Serialize(writer, result.Value, options)` to leverage existing converters for the `T` type.
- **Metadata serialization**: Delegates to existing metadata serialization helpers to maintain consistency with HTTP serialization.
- **Error serialization**: Uses existing error serialization helpers (same as HTTP path).
- **Extension attributes**: Iterates `ExtensionAttributes` MetadataObject and writes each as a top-level property using `writer.WritePropertyName()` and appropriate value writers.
- **Timestamp formatting**: `time` property (if present) is serialized as ISO 8601 using STJ's default `DateTimeOffset` converter.

### Extension Method Flow

```csharp
public static byte[] ToCloudEvent<T>(
    this Result<T> result,
    string? successType = null,
    string? failureType = null,
    string? id = null,
    string? source = null,
    ...
    LightResultsCloudEventWriteOptions? options = null
)
{
    var resolvedOptions = options ?? LightResultsCloudEventWriteOptions.Default;
    
    // Create frozen options struct
    var frozenOptions = new ResolvedCloudEventWriteOptions(
        resolvedOptions.MetadataSerializationMode
    );
    
    // Construct envelope with frozen options
    var envelope = result.ToCloudEventEnvelopeForWriting(
        successType, failureType, id, source, ...
        frozenOptions
    );
    
    using var bufferWriter = new PooledByteBufferWriter();
    using var writer = new Utf8JsonWriter(bufferWriter);
    var envelopeTypeInfo = (JsonTypeInfo<CloudEventEnvelopeForWriting<T>>) 
        resolvedOptions.SerializerOptions.GetTypeInfo(typeof(CloudEventEnvelopeForWriting<T>));
    // TODO: ensure that envelopeTypeInfo is not null
    JsonSerializer.Serialize(writer, envelope, envelopeTypeInfo);
    writer.Flush();
    return bufferWriter.ToArray();
}
```

This approach:
1. Freezes configuration into `ResolvedCloudEventWriteOptions` (resolution happens once)
2. Constructs the envelope struct (no heap allocation for the struct itself)
3. Delegates entirely to STJ for serialization
4. Allows callers to replace any converter in the chain
5. Ensures converters receive all necessary context without ambient state lookups

### Metadata Serialization Logic

The converter accesses `envelope.ResolvedOptions.MetadataMode` to determine metadata serialization:

1. Check `MetadataMode` (`MetadataSerializationMode` enum: `Always`, `ErrorsOnly`, or `Never`)
2. Apply `MetadataValueAnnotation.SerializeInCloudEventData` annotation as a secondary filter
3. Only serialize metadata values that pass both checks

Note: For CloudEvents, metadata is always included in the `data` payload (not in envelope attributes), so the serialization mode only controls whether metadata appears in the data object.

This approach keeps all serialization logic in one place with direct access to frozen options, avoiding the complexity of passing state through the STJ pipeline.

### Removing Legacy Converters

The following files can be deleted or significantly simplified:
- `CloudEventWriteResultJsonConverter.cs` — replaced by unified envelope converter
- `CloudEventWriteResultJsonConverterFactory.cs` — replaced by unified envelope factory

The complex private methods in `CloudEventResultExtensions` (attribute resolution, validation) remain but are invoked during envelope construction rather than during serialization.

### Performance Considerations

1. **Struct envelope**: `CloudEventEnvelopeForWriting<T>` is a readonly record struct, avoiding heap allocations.
2. **Frozen options**: `ResolvedCloudEventWriteOptions` stores only a single enum value (4 bytes), minimal memory overhead.
3. **Single-pass resolution**: Configuration merging happens once in `ToCloudEventEnvelope()`, not repeatedly during serialization.
4. **STJ source generators**: Callers can use `JsonSerializerContext` for AOT-friendly serialization.
5. **Single STJ call**: One `JsonSerializer.Serialize()` call instead of manual writer manipulation.
6. **Potential overhead**: STJ's converter resolution has some overhead, but this is typically negligible and enables caching.

We already have benchmarks which should be run to compare the performance of the new approach against the current implementation.

Even if we face regression, the benefits of a unified, maintainable, and extensible approach outweigh the performance cost.
