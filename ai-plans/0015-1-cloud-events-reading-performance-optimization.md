# CloudEvents Reading Performance Optimization

## Rationale

After implementing the initial plan 0015-cloud-events-serialization, the current CloudEvent JSON reading implementation incurs unnecessary allocations when deserializing the `data` property. In `CloudEventEnvelopeJsonReader.ReadEnvelope`, when the `data` property is encountered, the implementation:

1. Parses the data subtree into a `JsonDocument`
2. Serializes it back to a `byte[]` via `JsonSerializer.SerializeToUtf8Bytes`
3. Stores this copy in `CloudEventEnvelopePayload.DataBytes`
4. Later deserializes from `DataBytes` to the actual payload type

This creates three allocations (JsonDocument, internal buffers, byte[] copy) that can be eliminated. Since the caller provides a `ReadOnlyMemory<byte>` containing the original JSON, we can track byte positions and use a slice of the original buffer instead of copying.

## Acceptance Criteria

- [x] `CloudEventEnvelopePayload` stores position-based tracking (`DataStart`, `DataLength`) instead of `byte[]` for the data segment
- [x] No `JsonDocument` is allocated when parsing the CloudEvent envelope
- [x] No intermediate `byte[]` copy is created for the data payload
- [x] The original buffer slice is used directly when deserializing the data payload
- [x] All existing CloudEvent reading functionality remains intact
- [x] Automated tests are written or updated to verify the new implementation
- [x] BenchmarkDotNet benchmarks are added to `./benchmarks/Benchmarks/` to measure the allocation reduction

## Technical Details

### Current Allocation Flow

```
ReadOnlyMemory<byte> cloudEvent (original buffer)
    │
    ▼
JsonSerializer.Deserialize<CloudEventEnvelopePayload>(cloudEvent.Span, options)
    │
    ▼ (inside CloudEventEnvelopePayloadJsonConverter.Read)
    │
CloudEventEnvelopeJsonReader.ReadEnvelope(ref Utf8JsonReader reader)
    │
    └─ When hitting "data" property:
        │
        ├─ JsonDocument.ParseValue(ref reader)     ← Allocation #1
        │
        └─ JsonSerializer.SerializeToUtf8Bytes()   ← Allocation #2
            │
            ▼
        byte[] dataBytes stored in payload
            │
            ▼
Later: JsonSerializer.Deserialize<T>(dataBytes)    ← Re-parsing same content
```

### Zero-Copy Architecture (Position-Based)

The key insight is that `Utf8JsonReader.BytesConsumed` tracks the current byte position within the input buffer. By recording positions before and after skipping the `data` value, we can compute a slice of the original buffer **after** deserialization completes.

This approach maintains full STJ converter extensibility - users can still provide custom `JsonConverter` implementations.

#### 1. Modify `CloudEventEnvelopePayload`

Change the data storage from `byte[]?` to position tracking:

```csharp
public readonly struct CloudEventEnvelopePayload
{
    // Remove: public byte[]? DataBytes { get; }
    // Add:
    public int DataStart { get; }   // Byte offset where data value begins
    public int DataLength { get; }  // Length of data value in bytes

    // HasData and IsDataNull remain for semantic clarity
}
```

#### 2. Modify `CloudEventEnvelopeJsonReader.ReadEnvelope`

Update the existing method to track byte positions using `BytesConsumed` instead of copying bytes:

```csharp
public static CloudEventEnvelopePayload ReadEnvelope(ref Utf8JsonReader reader)
{
    // ... existing envelope attribute parsing ...

    int dataStart = 0;
    int dataLength = 0;
    var hasData = false;
    var isDataNull = false;

    // When hitting "data" property:
    else if (reader.ValueTextEquals("data"))
    {
        // Record position BEFORE reading the data value token
        // BytesConsumed is the position after the property name
        int positionBeforeDataValue = (int)reader.BytesConsumed;

        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading data.");
        }

        hasData = true;
        if (reader.TokenType == JsonTokenType.Null)
        {
            isDataNull = true;
            // dataStart and dataLength remain 0
        }
        else
        {
            // Skip the entire data subtree without parsing into JsonDocument
            reader.Skip();

            int positionAfterDataValue = (int)reader.BytesConsumed;

            dataStart = positionBeforeDataValue;
            dataLength = positionAfterDataValue - positionBeforeDataValue;
        }
    }

    // ... rest of parsing ...

    return new CloudEventEnvelopePayload(
        type!,
        source!,
        id!,
        subject,
        time,
        dataContentType,
        dataSchema,
        extensionAttributes,
        hasData,
        isDataNull,
        dataStart,
        dataLength
    );
}
```

**Important:** `Utf8JsonReader.BytesConsumed` returns the number of bytes consumed *up to and including* the current token. We capture the position *after* reading the property name (before the value), then again after `Skip()` to get the complete range.

#### 3. Update Extension Methods

Modify `ReadOnlyMemoryCloudEventExtensions` to slice the original buffer after deserialization:

```csharp
public static CloudEventEnvelope ReadResultWithCloudEventEnvelope(
    this ReadOnlyMemory<byte> cloudEvent,
    LightResultsCloudEventReadOptions? options = null)
{
    var readOptions = options ?? LightResultsCloudEventReadOptions.Default;

    // Deserialize through STJ (maintains converter extensibility)
    var parsedEnvelope = JsonSerializer.Deserialize<CloudEventEnvelopePayload>(
        cloudEvent.Span,
        readOptions.SerializerOptions
    );

    var isFailure = DetermineIsFailure(parsedEnvelope, readOptions);

    // Slice the original buffer using tracked positions - ZERO COPY
    var dataSegment = parsedEnvelope.HasData && !parsedEnvelope.IsDataNull
        ? cloudEvent.Slice(parsedEnvelope.DataStart, parsedEnvelope.DataLength)
        : ReadOnlyMemory<byte>.Empty;

    var result = ParseResultPayload(dataSegment, isFailure, readOptions);

    // ... rest unchanged ...
}
```

#### 4. Update Payload Parsing Methods

Change signature to accept `ReadOnlyMemory<byte>` instead of extracting from payload:

```csharp
private static Result ParseResultPayload(
    ReadOnlyMemory<byte> dataSegment,
    bool isFailure,
    LightResultsCloudEventReadOptions options)
{
    if (dataSegment.IsEmpty)
    {
        if (isFailure)
        {
            throw new JsonException(
                "CloudEvent failure payloads for non-generic Result must contain non-null data."
            );
        }
        return Result.Ok();
    }

    if (isFailure)
    {
        var failurePayload = JsonSerializer.Deserialize<CloudEventFailurePayload>(
            dataSegment.Span,
            options.SerializerOptions
        );
        return Result.Fail(failurePayload.Errors, failurePayload.Metadata);
    }

    // ... etc ...
}
```

#### 5. Converter Extensibility Preserved

The `CloudEventEnvelopePayloadJsonConverter` continues to work through STJ's normal deserialization pipeline. Users can:
- Register custom converters for envelope parsing
- Override behavior via `JsonSerializerOptions`
- Extend without modifying core library code

The optimization is transparent to the converter - it simply stores positions instead of copying bytes.

### Benchmark Design

Create `CloudEventReadingBenchmarks.cs` with:

1. **Baseline:** Current implementation (JsonDocument + SerializeToUtf8Bytes copy)
2. **Optimized:** Position-based approach (BytesConsumed + Skip + slice)
3. **Test cases:**
   - Small data payload (~100 bytes)
   - Medium data payload (~1KB)
   - Large data payload (~10KB)
   - Success vs failure payloads

Measure both execution time and allocations using `[MemoryDiagnoser]`.

### Edge Cases

- **Empty data segment:** `DataStart = 0, DataLength = 0` when `IsDataNull` is true or `HasData` is false
- **Nested complex data:** `reader.Skip()` correctly handles any valid JSON subtree
- **Unicode and escapes:** The slice captures raw UTF-8 bytes which `JsonSerializer` handles correctly
- **Whitespace:** `BytesConsumed` includes any whitespace between tokens; this is fine since the slice is re-parsed by STJ

### Breaking Changes

Changing `byte[]? DataBytes` to `int DataStart` and `int DataLength` is a breaking change, but the library is not published yet, so there is no issue here.
