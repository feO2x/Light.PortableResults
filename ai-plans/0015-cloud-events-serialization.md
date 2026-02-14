# CloudEvents JSON Serialization for Light.Results

## Rationale

Light.Results provides serialization of `Result` and `Result<T>` for HTTP (writing to responses, reading from `HttpResponseMessage`). To support asynchronous messaging scenarios, we add a CloudEvents JSON serialization layer in the core `Light.Results` project. This enables callers to serialize results as CloudEvents-compliant JSON envelopes and deserialize them back, independent of any specific messaging library (MassTransit, Rebus, raw RabbitMQ, etc.). The implementation lives in `Light.Results` under the `Light.Results.CloudEvents` namespace, uses `System.Text.Json` directly (no dependency on `CloudNative.CloudEvents`), and follows the same patterns as the existing `Http/` namespace. Framework-specific integration packages (e.g. for MassTransit or Rebus) are out of scope for this issue and can be added later.

The official CloudEvents specification can be found at https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/formats/json-format.md. Please read it before implementing this plan.

## Acceptance Criteria

### Writing
- [ ] `Result` and `Result<T>` can be serialized to a CloudEvents v1.0 JSON envelope (`application/cloudevents+json`) via `ToCloudEvent` (returns UTF-8 JSON byte array) and `WriteCloudEvent` (writes to `Utf8JsonWriter`) extension methods.
- [ ] CloudEvents envelope attributes (both required and extension) are sourced from two places: (1) result metadata annotated with `SerializeAsCloudEventExtensionAttribute`, and (2) explicit parameters passed to the write methods. Explicit parameters override metadata values.
- [ ] A conversion service (analogous to `DefaultHttpHeaderConversionService`) maps metadata keys/values to CloudEvents attribute keys/values, decoupling business logic from the CloudEvents envelope format.
- [ ] The caller provides `successType` and `failureType` explicitly — Light.Results uses the appropriate one based on `result.IsValid`.
- [ ] The caller always provides `id` (it may be an idempotency key). `source` can come from options (configured once) or be passed per call.
- [ ] The optional `subject` attribute is supported.
- [ ] `time` defaults to `DateTimeOffset.UtcNow` but can be overridden.
- [ ] For successful `Result<T>`, the `data` payload contains the serialized value of `T`. For successful `Result` (non-generic), `data` is omitted unless metadata annotated with `SerializeInCloudEventData` is present, in which case `data` contains `{ "metadata": { ... } }`.
- [ ] When `data` is omitted, `datacontenttype` is also omitted. When `data` is present, `datacontenttype` is `"application/json"`.
- [ ] For failed results, the `data` payload uses a Light.Results-native error format: an `errors` array (each entry with `message`, `code`, `target`, `category`, `metadata`) and optional top-level `metadata`. No top-level category, no RFC 9457 Problem Details, no HTTP `status` integer.
- [ ] Metadata values annotated with `SerializeInCloudEventData` are serialized inside the `data` payload (analogous to `SerializeInHttpResponseBody`).
- [ ] An `InvalidOperationException` is thrown when a required CloudEvents attribute (`type`, `source`, `id`) cannot be resolved from either metadata or explicit parameters.

### Reading
- [ ] `ReadResultAsync` and `ReadResultAsync<T>` methods deserialize the `data` payload into `Result`/`Result<T>` without converting envelope attributes to metadata by default. Users can opt in to envelope-to-metadata conversion via options.
- [ ] `ReadResultWithCloudEventEnvelopeAsync` and `ReadResultWithCloudEventEnvelopeAsync<T>` methods return `CloudEventEnvelope`/`CloudEventEnvelope<T>`, giving the caller access to both the result and the parsed envelope attributes.
- [ ] A parsing service (analogous to `DefaultHttpHeaderParsingService`) maps CloudEvents extension attribute keys/values back to metadata keys/values when the user opts in.
- [ ] On read, a `JsonException` is thrown when required CloudEvents attributes (`specversion`, `type`, `source`, `id`) are missing, or when `specversion` is not `"1.0"`, or when `datacontenttype` indicates a non-JSON format, or when `data_base64` is present instead of `data`.

### Shared
- [ ] The `MetadataValueAnnotation` flags enum is extended with new CloudEvents flags. The XML doc on the enum is updated to no longer be HTTP-specific.
- [ ] Transport-agnostic JSON serialization helpers are extracted from `Http/` into `SharedJsonSerialization/`, with existing tests verifying no regressions.
- [ ] All new code is Native AOT compatible (`Utf8JsonReader`/`Utf8JsonWriter`-based). `MakeGenericType` is allowed when the generic type definition is statically referenced via `typeof`, ensuring the trimmer preserves it. Avoid unbound reflection where the target type could be trimmed.
- [ ] All new code lives in the `Light.Results` project (netstandard2.0) under the `Light.Results.CloudEvents` namespace.
- [ ] Automated tests are written for writing and reading CloudEvents envelopes.

## Technical Details

### Namespace and Folder Structure

```
Light.Results/
  CloudEvents/
    Writing/
      Json/
        CloudEventWriteResultJsonConverter.cs
        CloudEventWriteResultJsonConverter{T}.cs
        CloudEventSerializerExtensions.cs
        LightResultsCloudEventWriteOptions.cs
        Module.cs
    Reading/
      Json/
        CloudEventReadResultJsonConverter.cs
        LightResultsCloudEventReadOptions.cs
        Module.cs
```

Mirrors the `Http/` structure with `Json` subfolders to leave room for future Protobuf or other serialization formats (e.g. for gRPC integration). This is a representative outline — the implementer should add additional files as needed (e.g. a `JsonConverterFactory` for open generic `Result<T>`, payload DTOs for reading, a reader helper analogous to `ResultJsonReader`, etc.).

### Writing

Two extension methods are provided on `Result` and `Result<T>`:
- **`ToCloudEvent`** — returns a `byte[]` containing the UTF-8 JSON CloudEvents envelope.
- **`WriteCloudEvent`** — writes the envelope directly to a `Utf8JsonWriter`.

Both methods accept optional explicit parameters for CloudEvents attributes (`successType`, `failureType`, `id`, `source`, `subject`, `time`). The resulting envelope looks like:

```json
{
    "specversion": "1.0",
    "type": "<successType or failureType>",
    "source": "<source>",
    "subject": "<subject>",
    "id": "<id>",
    "time": "<RFC 3339 timestamp>",
    "datacontenttype": "application/json",
    "<extensionKey>": "<extensionValue>",
    "data": { ... }
}
```

#### Attribute Resolution

Envelope attributes are resolved from two sources (in priority order):
1. **Explicit parameters** passed to `ToCloudEvent`/`WriteCloudEvent`
2. **Result metadata** annotated with `SerializeAsCloudEventExtensionAttribute`

Explicit parameters override metadata values. A **conversion service** (analogous to `DefaultHttpHeaderConversionService`) maps metadata keys/values to CloudEvents attribute keys/values. This decouples business logic from CloudEvents — domain code sets metadata, and the conversion service translates it to the correct envelope attributes.

For required attributes (`type`, `source`, `id`): if the attribute cannot be resolved from either source, throw `InvalidOperationException`. `type` is resolved by selecting `successType` or `failureType` based on `result.IsValid`. `time` defaults to `DateTimeOffset.UtcNow`. `subject` is optional.

#### Data Payload Format

**Success `Result<T>`:**
The `data` value is the serialized `T` directly (same as the bare value in HTTP `MetadataSerializationMode.ErrorsOnly`). If metadata with `SerializeInCloudEventData` annotation exists, wrap in `{ "value": <T>, "metadata": { ... } }`.

**Success `Result` (non-generic):**
If metadata with `SerializeInCloudEventData` exists, `data` is `{ "metadata": { ... } }`. Otherwise, `data` is omitted and `datacontenttype` is also omitted.

**Failed results:**
```json
{
    "errors": [
        {
            "message": "Order already exists",
            "code": "ORDER_DUPLICATE",
            "target": "orderId",
            "category": "Conflict",
            "metadata": { ... }
        }
    ],
    "metadata": { ... }
}
```

No top-level category — each error carries its own `category`, and the CloudEvents `type` attribute already distinguishes success from failure at the envelope level. The `errors` array reuses the same rich format as `SerializerExtensions.WriteRichErrors`, but without the `isValidationResponse` / HTTP status code logic. The `metadata` at the top level is the result's metadata (if present and annotated for CloudEvent data).

#### Extension Attributes

Metadata values annotated with `SerializeAsCloudEventExtensionAttribute` are written as top-level JSON properties in the envelope. CloudEvents extension attribute names must be lowercase alphanumeric — the metadata key is used directly (callers are responsible for using valid keys), but the conversion service can remap keys. Only primitive types and arrays of primitives are valid for extension attributes (same constraint as HTTP headers). Objects are not allowed — throw `ArgumentException` on annotation, same pattern as `SerializeInHttpHeader` for objects.

### MetadataValueAnnotation Changes

Extend the flags enum:

```csharp
SerializeInCloudEventData = 4,
SerializeAsCloudEventExtensionAttribute = 8,
SerializeInCloudEventExtensionAttributeAndData = SerializeInCloudEventData | SerializeAsCloudEventExtensionAttribute
```

Update the enum's XML doc summary to be transport-agnostic (e.g. "Specifies where a metadata value should be serialized").

### Reading (Deserialization)

Two tiers of reading methods are provided:

#### Tier 1: `ReadResultAsync` / `ReadResultAsync<T>`

Returns `Result` or `Result<T>` directly. By default, envelope attributes are **not** converted to metadata — only the `data` payload is deserialized. Users can opt in to envelope-to-metadata conversion via `LightResultsCloudEventReadOptions`, which uses a **parsing service** (analogous to `DefaultHttpHeaderParsingService`) to map extension attribute keys/values back to metadata keys/values.

This tier is for consumers that handle CloudEvents-specific data at the process boundary and pass plain results to core logic.

#### Tier 2: `ReadResultWithCloudEventEnvelopeAsync` / `ReadResultWithCloudEventEnvelopeAsync<T>`

Returns `CloudEventEnvelope` or `CloudEventEnvelope<T>`, giving the caller access to both the deserialized result and all parsed envelope attributes as strongly-typed properties. Extension attributes are available as a `MetadataObject`.

This tier is for consumers that need access to the full CloudEvents context (e.g. for idempotency checks on `Id`, routing on `Type`, filtering on `Subject`).

#### Parsing Algorithm

Both tiers share the same parsing logic using `Utf8JsonReader`:

1. Read top-level properties. Collect `specversion`, `type`, `source`, `id`, `time`, `subject`, `datacontenttype`, and any unknown keys as extension attributes.
2. Validate that all required attributes (`specversion`, `type`, `source`, `id`) are present and that `specversion` is `"1.0"` — throw `JsonException` otherwise. If `datacontenttype` is present and does not indicate JSON content (i.e. media subtype is not `json` and does not end with `+json`), throw `JsonException`. If `data_base64` is encountered instead of `data`, throw `JsonException` since only JSON data is supported.
3. Determine success/failure: use a caller-provided `IsFailureType` callback, or default to auto-detecting from the `data` shape (presence of `errors` array).
4. Parse `data` into `Result`/`Result<T>` using the same patterns as `ResultJsonReader`.
5. For Tier 1 with opt-in: map extension attributes back into metadata via the parsing service and merge with data payload metadata.
6. For Tier 2: populate the `CloudEventEnvelope` struct with the parsed attributes and result.

#### `CloudEventEnvelope` Types

```csharp
public readonly record struct CloudEventEnvelope<T>
{
    public const string SpecVersion = "1.0";

    public required string Type { get; init; }
    public required string Source { get; init; }
    public string? Subject { get; init; }
    public required string Id { get; init; }
    public required DateTimeOffset Time { get; init; }
    public required string DataContentType { get; init; }
    public required Result<T> Data { get; init; }
    public MetadataObject? ExtensionAttributes { get; init; }
}

public readonly record struct CloudEventEnvelope
{
    public const string SpecVersion = "1.0";

    public required string Type { get; init; }
    public required string Source { get; init; }
    public string? Subject { get; init; }
    public required string Id { get; init; }
    public required DateTimeOffset Time { get; init; }
    public string? DataContentType { get; init; }
    public required Result Data { get; init; }
    public MetadataObject? ExtensionAttributes { get; init; }
}
```

Note: `DataContentType` is `required string` on `CloudEventEnvelope<T>` (data is always present) and `string?` on `CloudEventEnvelope` (data may be absent for a successful non-generic `Result` without metadata).

### Native AOT Compatibility

Reflection is not inherently incompatible with Native AOT — only unbound reflection where the target type has been removed by the trimmer causes issues. `MakeGenericType` is safe when the generic type definition is statically referenced with the `typeof` keyword (e.g. `typeof(CloudEventWriteResultJsonConverter<>).MakeGenericType(typeArg)`), because the trimmer sees the static reference and preserves the type. This is the same pattern used in the existing `HttpWriteResultJsonConverterFactory` and `HttpReadSuccessResultPayloadJsonConverterFactory`. Avoid patterns where the target type is only known at runtime via strings or untyped `Type` variables with no static reference.

### Shared JSON Serialization

Transport-agnostic serialization helpers that both `Http/` and `CloudEvents/` use should be extracted into a `SharedJsonSerialization/` folder at the same level:

```
Light.Results/
  SharedJsonSerialization/
    SharedSerializerExtensions.cs
    SharedJsonReader.cs
    ...
  Http/
    ...
  CloudEvents/
    ...
```

Candidates for extraction:
- `WriteGenericValue<T>` — writing a generic value to `Utf8JsonWriter`.
- `WriteMetadataPropertyAndValue` — writing metadata inside a JSON object.
- The rich error writing logic (message, code, target, category, metadata per error) — currently in `SerializerExtensions.WriteRichErrors`. Extract the transport-agnostic parts so that both HTTP and CloudEvents call the same helper, keeping the error array format consistent.
- `MetadataJsonReader` — reading metadata from JSON.
- Parts of `ResultJsonReader` that parse the error array format.

The existing `Http/` code should be refactored to call into these shared helpers. This is a refactoring step that should be done first, with existing tests verifying no regressions.

### Options and Conversion Services

**`LightResultsCloudEventWriteOptions`:**
- `Source` (string?) — default source URI, used when not provided per call.
- `MetadataSerializationMode` — same enum as HTTP, but defaults to `Always` (unlike HTTP which defaults to `ErrorsOnly`). In messaging, metadata is expected to travel with messages, and silent data loss on success results would be surprising.
- `ConversionService` (ICloudEventAttributeConversionService?) — optional service that maps metadata keys/values to CloudEvents attribute keys/values on write. Analogous to `IHttpHeaderConversionService`. When null, metadata keys are used as attribute names directly and values are converted via `ToString()`.

**`LightResultsCloudEventReadOptions`:**
- `SerializerOptions` (JsonSerializerOptions?) — for deserializing `data`.
- `IsFailureType` (Func<string, bool>?) — optional callback to determine failure from the `type` attribute. If null, auto-detect from `data` shape (presence of `errors` array).
- `ParsingService` (ICloudEventAttributeParsingService?) — optional service that maps CloudEvents extension attribute keys/values back to metadata keys/values on read. Analogous to `IHttpHeaderParsingService`. Only used when the caller opts in to envelope-to-metadata conversion. When null, extension attributes are not converted to metadata (Tier 1 default behavior).
- `MergeStrategy` — for combining extension attribute metadata with data payload metadata.

**`ICloudEventAttributeConversionService`:**
Maps metadata entries to CloudEvents attribute key/value pairs. Follows the same pattern as `IHttpHeaderConversionService` / `DefaultHttpHeaderConversionService` — a converter registry keyed by metadata key, with a default implementation that uses a `FrozenDictionary<string, CloudEventAttributeConverter>`.

**`ICloudEventAttributeParsingService`:**
Maps CloudEvents extension attribute entries back to metadata key/value pairs. Follows the same pattern as `IHttpHeaderParsingService` / `DefaultHttpHeaderParsingService` — a parser registry keyed by attribute name, with a default implementation that uses a `FrozenDictionary<string, CloudEventAttributeParser>`.
