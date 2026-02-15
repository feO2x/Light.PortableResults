# CloudEvents JSON Writing Optimization

## Rationale

The CloudEvents writing integration in `Light.Results.CloudEvents.Writing` is functionally correct and fulfills the requirements of the original serialization plan. However, a review identified two architectural gaps:

1. **No STJ Converter Integration**: Unlike the HTTP integration which provides `HttpWriteResultJsonConverter` and `HttpWriteResultJsonConverterFactory`, the CloudEvents integration only exposes low-level `WriteCloudEvent` extension methods. This prevents users from leveraging `JsonSerializer.Serialize()` with custom converters and limits composability with messaging libraries that use STJ.

2. **Suboptimal Allocations**: The current implementation allocates intermediate objects (metadata filtering) and uses `MemoryStream` + `ToArray()` which copies data twice.

This plan addresses both gaps to align CloudEvents writing with the HTTP integration pattern and reduce memory pressure.

## Acceptance Criteria

### STJ Converter Integration
- [ ] `CloudEventWriteResultJsonConverter` is implemented as a `JsonConverter<Result>` that serializes non-generic results as CloudEvents envelopes.
- [ ] `CloudEventWriteResultJsonConverter<T>` is implemented as a `JsonConverter<Result<T>>` that serializes generic results as CloudEvents envelopes.
- [ ] `CloudEventWriteResultJsonConverterFactory` is implemented to create `CloudEventWriteResultJsonConverter<T>` instances for any `Result<T>` type.
- [ ] CloudEvents envelope parameters (`successType`, `failureType`, `id`, `source`, `subject`, `dataschema`, `time`) are configurable via `LightResultsCloudEventWriteOptions` and can be overridden per-serialization using a context mechanism.
- [ ] `Module.AddDefaultLightResultsCloudEventWriteJsonConverters()` extension method is provided to register all CloudEvents write converters on a `JsonSerializerOptions` instance.
- [ ] The existing `ToCloudEvent` and `WriteCloudEvent` extension methods remain available for callers who prefer direct `Utf8JsonWriter` control.
- [ ] Callers can serialize a result as a CloudEvent by calling `JsonSerializer.Serialize(result, options)` when the converters are registered.

### Allocation Reduction
- [ ] `ToCloudEvent` uses `ArrayBufferWriter<byte>` instead of `MemoryStream` to avoid double-copy allocations.
- [ ] An overload `WriteCloudEvent(this Result result, IBufferWriter<byte> bufferWriter, ...)` is provided for zero-copy scenarios.
- [ ] Metadata filtering for `SerializeInCloudEventData` is performed inline during the write loop rather than building an intermediate `MetadataObject`.
- [ ] Extension attribute conversion iterates and writes directly rather than collecting into an intermediate `MetadataObject` first.
- [ ] Metadata serialization uses `SharedSerializerExtensions.WriteMetadataPropertyAndValue()` to leverage STJ converters rather than duplicated local methods.

### Code Hygiene
- [ ] Duplicated `WriteMetadataObject`, `WriteMetadataArray`, and `WriteMetadataValue` methods in `CloudEventResultExtensions` are removed in favor of shared helpers or STJ converters.
- [ ] Automated tests verify no regressions in serialization output.
- [ ] Allocation benchmarks are added to track memory usage for CloudEvents serialization.

## Technical Details

### Converter Architecture

The converters follow the same pattern as `Http/Writing/Json/`:

```
CloudEvents/
  Writing/
    Json/
      CloudEventWriteResultJsonConverter.cs
      CloudEventWriteResultJsonConverterFactory.cs
```

#### Parameter Passing Challenge

STJ converters receive `Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)` — there is no direct way to pass per-call envelope parameters like `id` or `type`. Two approaches are viable:

**Option A: Store parameters on the converter**
The converter is constructed with `LightResultsCloudEventWriteOptions` which contains `SuccessType`, `FailureType`, `Source`, and other defaults. Per-call customization is not supported; callers must create different converter instances for different envelope configurations.

**Option B: Context via options or AsyncLocal**
Use `JsonSerializerOptions` with a custom property bag or an `AsyncLocal<CloudEventWriteContext>` to pass per-call parameters. This is more flexible but adds complexity.

**Recommendation**: Start with Option A (parameters on converter/options). Most messaging scenarios use consistent envelope types per message type, so per-call customization is less common. The low-level `WriteCloudEvent` methods remain available for advanced scenarios.

#### Converter Implementation Sketch

```csharp
public sealed class CloudEventWriteResultJsonConverter : JsonConverter<Result>
{
    private readonly LightResultsCloudEventWriteOptions _options;

    public CloudEventWriteResultJsonConverter(LightResultsCloudEventWriteOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    public override Result Read(...) => throw new NotSupportedException(...);

    public override void Write(Utf8JsonWriter writer, Result result, JsonSerializerOptions options) =>
        result.WriteCloudEvent(
            writer,
            successType: _options.SuccessType,
            failureType: _options.FailureType,
            id: _options.IdResolver?.Invoke() ?? Guid.NewGuid().ToString(),
            source: _options.Source,
            subject: _options.Subject,
            dataschema: _options.DataSchema,
            time: _options.Time,
            options: _options
        );
}
```

The generic converter and factory follow the same pattern as `HttpWriteResultJsonConverter<T>` and `HttpWriteResultJsonConverterFactory`.

### Options Extensions

`LightResultsCloudEventWriteOptions` gains additional properties for converter scenarios:

```csharp
public sealed record LightResultsCloudEventWriteOptions
{
    // Existing properties...
    public string? Source { get; set; }
    public MetadataSerializationMode MetadataSerializationMode { get; set; } = MetadataSerializationMode.Always;
    public JsonSerializerOptions SerializerOptions { get; set; } = ...;
    public ICloudEventAttributeConversionService ConversionService { get; set; } = ...;

    // New properties for converter scenarios
    public string? SuccessType { get; set; }
    public string? FailureType { get; set; }
    public string? Subject { get; set; }
    public string? DataSchema { get; set; }
    public DateTimeOffset? Time { get; set; }
    public Func<string>? IdResolver { get; set; }
}
```

`IdResolver` is a factory function because `id` must be unique per event. Callers can set `IdResolver = () => Guid.NewGuid().ToString()` or integrate with their idempotency key generation.

### Allocation Optimizations

#### Replace MemoryStream with ArrayBufferWriter

```csharp
public static byte[] ToCloudEvent(this Result result, ...)
{
    var bufferWriter = new ArrayBufferWriter<byte>();
    using var writer = new Utf8JsonWriter(bufferWriter);
    result.WriteCloudEvent(writer, ...);
    writer.Flush();
    return bufferWriter.WrittenSpan.ToArray();
}
```

This eliminates the internal `MemoryStream` buffer management overhead and the copy from `MemoryStream.GetBuffer()` to `ToArray()`.

#### IBufferWriter Overload

For high-throughput scenarios (e.g., writing directly to Kestrel's pipe):

```csharp
public static void WriteCloudEvent(
    this Result result,
    IBufferWriter<byte> bufferWriter,
    string? successType = null,
    ...
)
{
    using var writer = new Utf8JsonWriter(bufferWriter);
    result.WriteCloudEvent(writer, successType, ...);
    writer.Flush();
}
```

#### Inline Metadata Filtering

Instead of:
```csharp
var metadataForData = SelectMetadataByAnnotation(result.Metadata, MetadataValueAnnotation.SerializeInCloudEventData);
// ... later ...
WriteMetadataPropertyAndValue(writer, metadataForData.Value);
```

Use inline filtering during write:
```csharp
private static bool WriteMetadataPropertyAndValueIfAnnotated(
    Utf8JsonWriter writer,
    MetadataObject? metadata,
    MetadataValueAnnotation annotation,
    JsonSerializerOptions serializerOptions
)
{
    if (metadata is null)
        return false;

    var hasAnnotatedValues = false;
    foreach (var kvp in metadata.Value)
    {
        if (kvp.Value.HasAnnotation(annotation))
        {
            hasAnnotatedValues = true;
            break;
        }
    }

    if (!hasAnnotatedValues)
        return false;

    writer.WritePropertyName("metadata");
    writer.WriteStartObject();
    foreach (var kvp in metadata.Value)
    {
        if (kvp.Value.HasAnnotation(annotation))
        {
            writer.WritePropertyName(kvp.Key);
            // Use STJ converter for MetadataValue
            ...
        }
    }
    writer.WriteEndObject();
    return true;
}
```

This approach iterates metadata once or twice (check + write) but avoids allocating a new `MetadataObject`.

#### Use SharedSerializerExtensions

Replace local `WriteMetadataObject`, `WriteMetadataArray`, and `WriteMetadataValue` methods with calls to `SharedSerializerExtensions` or the STJ `MetadataObject`/`MetadataValue` converters. This ensures consistent behavior with HTTP and respects user-provided converters.

### Module Registration

```csharp
public static class Module
{
    // Existing methods...

    public static void AddDefaultLightResultsCloudEventWriteJsonConverters(
        this JsonSerializerOptions serializerOptions,
        LightResultsCloudEventWriteOptions options
    )
    {
        if (serializerOptions is null)
            throw new ArgumentNullException(nameof(serializerOptions));

        serializerOptions.Converters.Add(new HttpWriteMetadataObjectJsonConverter());
        serializerOptions.Converters.Add(new HttpWriteMetadataValueJsonConverter());
        serializerOptions.Converters.Add(new CloudEventWriteResultJsonConverter(options));
        serializerOptions.Converters.Add(new CloudEventWriteResultJsonConverterFactory(options));
    }
}
```

Note: The metadata converters are shared with HTTP since the write logic is identical.

### Benchmarks

Add benchmarks in `Light.Results.Benchmarks` project:

- `CloudEventWriteResultBenchmark` — measures serialization time and allocations for `Result` and `Result<T>` with varying metadata sizes.
- Compare `ToCloudEvent()` (current MemoryStream) vs optimized (ArrayBufferWriter) implementations.
- Compare intermediate `MetadataObject` allocation vs inline filtering.
