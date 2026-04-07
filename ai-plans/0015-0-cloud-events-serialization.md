# CloudEvents JSON Serialization for Light.Results

## Rationale

Light.Results provides serialization of `Result` and `Result<T>` for HTTP (writing to responses, reading from `HttpResponseMessage`). To support asynchronous messaging scenarios, we add a CloudEvents JSON serialization layer in the core `Light.Results` project. This enables callers to serialize results as CloudEvents-compliant JSON envelopes and deserialize them back, independent of any specific messaging library (MassTransit, Rebus, raw RabbitMQ, etc.). The implementation lives in `Light.Results` under the `Light.Results.CloudEvents` namespace, uses `System.Text.Json` directly (no dependency on `CloudNative.CloudEvents`), and follows the same patterns as the existing `Http/` namespace. Framework-specific integration packages (e.g. for MassTransit or Rebus) are out of scope for this issue and can be added later.

The official CloudEvents JSON format specification can be found at https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/formats/json-format.md and the core specification at https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/spec.md. Please read both before implementing this plan.

## Acceptance Criteria

### Writing
- [ ] `Result` and `Result<T>` can be serialized to a CloudEvents v1.0 JSON envelope (`application/cloudevents+json`) via `ToCloudEvent` (returns UTF-8 JSON byte array) and `WriteCloudEvent` (writes to `Utf8JsonWriter`) extension methods.
- [ ] CloudEvents envelope attributes (both required and extension) are sourced from two places: (1) result metadata annotated with `SerializeAsCloudEventExtensionAttribute`, and (2) explicit parameters passed to the write methods. Explicit parameters override metadata values. The only forbidden targets are `data`, `data_base64`, and `lroutcome`.
- [ ] The serializer exclusively writes the `data` property and the `lroutcome` extension attribute. Attempting to produce `data`, `data_base64`, or `lroutcome` from metadata must throw an exception.
- [ ] A conversion service (analogous to `DefaultHttpHeaderConversionService`) maps metadata keys/values to CloudEvents attribute keys/values, decoupling business logic from the CloudEvents envelope format.
- [ ] The caller provides `successType` and `failureType` explicitly — Light.Results uses the appropriate one based on `result.IsValid`.
- [ ] The caller always provides `id` (it may be an idempotency key). `source` can come from options (configured once) or be passed per call.
- [ ] The optional `subject` and `dataschema` attributes are supported.
- [ ] `source` is validated as a URI-reference (may be relative, per RFC 3986). `dataschema` is validated as an absolute URI. Invalid caller-supplied values throw `ArgumentException`.
- [ ] `specversion` is always written as `"1.0"`.
- [ ] `time` is optional but defaults to `DateTimeOffset.UtcNow` on write. If `time` is not provided explicitly, the default value is written.
- [ ] `lroutcome` is always written as a CloudEvents extension attribute with value `"success"` for valid results and `"failure"` for invalid results.
- [ ] For successful `Result<T>`, the `data` payload contains the serialized value of `T` when success metadata is not included. If success metadata is included (`MetadataSerializationMode.Always`, the default), `data` must be `{ "value": <T>, "metadata": { ... } }`.
- [ ] For successful `Result` (non-generic), `data` is omitted unless metadata annotated with `SerializeInCloudEventData` is present, in which case `data` contains `{ "metadata": { ... } }`.
- [ ] When `data` is omitted, `datacontenttype` is also omitted. When `data` is present, `datacontenttype` is `"application/json"`. When `datacontenttype` is omitted, the implicit default is still `"application/json"`.
- [ ] For failed results, the `data` payload uses a Light.Results-native error format: an `errors` array (each entry with `message`, `code`, `target`, `category`, `metadata`) and optional top-level `metadata`. No top-level category, no RFC 9457 Problem Details, no HTTP `status` integer.
- [ ] Metadata values annotated with `SerializeInCloudEventData` are serialized inside the `data` payload (analogous to `SerializeInHttpResponseBody`). `MetadataSerializationMode.ErrorsOnly` suppresses metadata on success results even when annotated.
- [ ] An `InvalidOperationException` is thrown when a required CloudEvents attribute (`type`, `source`, `id`) cannot be resolved from either metadata or explicit parameters.
- [ ] The attribute conversion service enforces CloudEvents extension attribute name rules (lowercase alphanumeric, no reserved names including `data`, `data_base64`, and `lroutcome`) and allows only primitive JSON values for extension attributes (no arrays or objects).

### Reading
- [ ] `ReadResult` and `ReadResult<T>` extension methods on `ReadOnlyMemory<byte>` deserialize the `data` payload into `Result`/`Result<T>` without converting envelope attributes to metadata by default. Users can opt in to envelope-to-metadata conversion via options.
- [ ] `ReadResultWithCloudEventEnvelope` and `ReadResultWithCloudEventEnvelope<T>` extension methods on `ReadOnlyMemory<byte>` return `CloudEventEnvelope`/`CloudEventEnvelope<T>`, giving the caller access to both the result and the parsed envelope attributes.
- [ ] A parsing service (analogous to `DefaultHttpHeaderParsingService`) maps CloudEvents extension attribute keys/values back to metadata keys/values when the user opts in.
- [ ] On read, a `JsonException` is thrown when required CloudEvents attributes (`specversion`, `type`, `source`, `id`) are missing, or when `specversion` is not `"1.0"`, or when `datacontenttype` is present and is neither `"application/json"` nor a media type with the `+json` suffix (e.g. `"application/vnd.myapp+json"`), or when `data_base64` is present.
- [ ] Success/failure is determined by convention: `lroutcome = "success" | "failure"`. If `lroutcome` is present with any other value, throw `JsonException`.
- [ ] If `lroutcome` is absent, `IsFailureType` (optional fallback callback) may classify based on `type`. If neither `lroutcome` nor `IsFailureType` can classify the event, throw `InvalidOperationException`.
- [ ] On read, a `JsonException` is thrown when `data` is missing or `null` for `Result<T>` (successful or failed).
- [ ] For non-generic `Result`, if `data` is missing or `null` and the event is classified as success, return `Result.Ok()` (assuming no metadata in `data`). If the event is classified as failure, throw `JsonException`.
- [ ] On read, `source` is validated as a URI-reference and `dataschema` as an absolute URI. Invalid values cause a `JsonException`.

### Shared
- [ ] The `MetadataValueAnnotation` flags enum is extended with new CloudEvents flags. The XML doc on the enum is updated to no longer be HTTP-specific.
- [ ] Transport-agnostic JSON serialization helpers are extracted from `Http/` into `SharedJsonSerialization/`, with existing tests verifying no regressions.
- [ ] All new code is Native AOT compatible (`Utf8JsonReader`/`Utf8JsonWriter`-based). `MakeGenericType` is allowed when the generic type definition is statically referenced via `typeof`, ensuring the trimmer preserves it. Avoid unbound reflection where the target type could be trimmed.
- [ ] All new code lives in the `Light.Results` project (netstandard2.0) under the `Light.Results.CloudEvents` namespace.
- [ ] Composition Root `Module` classes with `IServiceCollection` extension methods are provided for DI registration of CloudEvents writing and reading options and services, following the pattern of `Http/Writing/Module.cs` and `Http/Reading/Module.cs`.
- [ ] Automated tests are written for writing and reading CloudEvents envelopes.

## Technical Details

### Namespace and Folder Structure

```
Light.Results/
  CloudEvents/
    Writing/
      Json/
    Reading/
      Json/
```

Mirrors the `Http/` structure with `Json` subfolders to leave room for future Protobuf or other serialization formats (e.g. for gRPC integration). The exact file split is intentionally left open so implementers can structure converters, helpers, and options types in a way that best fits the existing code base.

### Writing

Two extension methods are provided on `Result` and `Result<T>`:
- **`ToCloudEvent`** — returns a `byte[]` containing the UTF-8 JSON CloudEvents envelope.
- **`WriteCloudEvent`** — writes the envelope directly to a `Utf8JsonWriter`.

Both methods accept optional explicit parameters for CloudEvents attributes (`successType`, `failureType`, `id`, `source`, `subject`, `dataschema`, `time`). The resulting envelope looks like:

```json
{
    "specversion": "1.0",
    "type": "<successType or failureType>",
    "source": "<source>",
    "subject": "<subject>",
    "dataschema": "<schemaUri>",
    "id": "<id>",
    "time": "<RFC 3339 timestamp>",
    "lroutcome": "<success|failure>",
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
Metadata entries annotated with `SerializeAsCloudEventExtensionAttribute` can supply any envelope attribute (including standard ones such as `type`, `source`, or `id`) except `data`, `data_base64`, and `lroutcome`; explicit parameters still override.

For required attributes (`type`, `source`, `id`): if the attribute cannot be resolved from either source, throw `InvalidOperationException`. `type` is resolved by selecting `successType` or `failureType` based on `result.IsValid`. `specversion` is always written as `"1.0"`. `time` is optional but defaults to `DateTimeOffset.UtcNow` when not provided explicitly. `subject` and `dataschema` are optional. `source`, when present, must be a valid URI-reference (RFC 3986). `dataschema`, when present, must be a valid absolute URI. `lroutcome` is reserved by Light.Results and is always written by the serializer as `"success"` or `"failure"`.

#### Data Payload Format

**Success `Result<T>`:**
The `data` value is the serialized `T` directly (same as the bare value in HTTP `MetadataSerializationMode.ErrorsOnly`). If metadata with `SerializeInCloudEventData` annotation exists and `MetadataSerializationMode` allows metadata on success results, wrap in `{ "value": <T>, "metadata": { ... } }`.
The behavior should match the HTTP serialization behavior for success results. The main difference is that `MetadataSerializationMode.Always` is the default for CloudEvents.

**Success `Result` (non-generic):**
If metadata with `SerializeInCloudEventData` exists and `MetadataSerializationMode` allows metadata on success results, `data` is `{ "metadata": { ... } }`. Otherwise, `data` is omitted and `datacontenttype` is also omitted.

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

Metadata values annotated with `SerializeAsCloudEventExtensionAttribute` are written as top-level JSON properties in the envelope. For extension attributes, names must be lowercase alphanumeric — the conversion service can remap keys, validates names, and rejects reserved names (including `data`, `data_base64`, and `lroutcome`). Only primitive JSON values are valid for extension attributes (no arrays or objects); throw `ArgumentException` on annotation, same pattern as `SerializeInHttpHeader` for objects.

`data` and `lroutcome` are always produced by the serializer logic and never through metadata conversion. `data_base64` is not supported for this integration.

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

#### Tier 1: `ReadResult` / `ReadResult<T>`

Extension methods on `ReadOnlyMemory<byte>`. Returns `Result` or `Result<T>` directly. By default, envelope attributes are **not** converted to metadata — only the `data` payload is deserialized. Users can opt in to envelope-to-metadata conversion via `LightResultsCloudEventReadOptions`, which uses a **parsing service** (analogous to `DefaultHttpHeaderParsingService`) to map extension attribute keys/values back to metadata keys/values.

This tier is for consumers that handle CloudEvents-specific data at the process boundary and pass plain results to core logic.

#### Tier 2: `ReadResultWithCloudEventEnvelope` / `ReadResultWithCloudEventEnvelope<T>`

Extension methods on `ReadOnlyMemory<byte>`. Returns `CloudEventEnvelope` or `CloudEventEnvelope<T>`, giving the caller access to both the deserialized result and all parsed envelope attributes as strongly-typed properties. Extension attributes are available as a `MetadataObject`.

This tier is for consumers that need access to the full CloudEvents context (e.g. for idempotency checks on `Id`, routing on `Type`, filtering on `Subject`).

#### Parsing Algorithm

Both tiers share the same synchronous parsing logic using `Utf8JsonReader` over the `ReadOnlyMemory<byte>` input. Messaging libraries typically provide message bodies as byte arrays or memory, so there is no need for async stream-based reading.

The parser must:
- Read top-level envelope attributes, collecting unknown keys as extension attributes.
- Validate required CloudEvents attributes and `specversion == "1.0"`.
- Reject unsupported combinations and formats (`data_base64` present, `datacontenttype` that is neither `"application/json"` nor a media type with the `+json` suffix, `source` that is not a valid URI-reference, `dataschema` that is not a valid absolute URI).
- Decide success/failure semantics using the following precedence: `lroutcome` extension attribute (`"success"` / `"failure"`), then optional `IsFailureType(type)` fallback callback. If neither can classify the event, throw `InvalidOperationException`.
- Parse `data` into `Result`/`Result<T>` using existing JSON reader patterns, with these special rules:
  - `Result<T>` requires non-null `data`.
  - non-generic `Result` with missing/null `data` maps to `Result.Ok()` when the event is classified as success; otherwise it is invalid and throws `JsonException`.
- For Tier 1 (opt-in), map extension attributes back into metadata via the parsing service and merge with payload metadata.
- For Tier 2, return envelope attributes plus the parsed result.

#### `CloudEventEnvelope` Types

Provide `CloudEventEnvelope` and `CloudEventEnvelope<T>` as readonly record structs with strongly typed CloudEvents properties (`Type`, `Source`, `Subject`, `Id`, `Time`, `DataContentType`, `DataSchema`), a `Data` property of `Result`/`Result<T>`, and `ExtensionAttributes` as `MetadataObject?`. Both types expose `SpecVersion = "1.0"`.

Note: `DataContentType` can be omitted even when `data` is present (implicit `application/json`), so it is nullable in both envelopes. `Time` is optional in the CloudEvents spec and is therefore nullable. `DataSchema` is optional and intended for schema URIs.

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
- `MetadataSerializationMode` — currently in `Light.Results.Http.Writing`, this enum is used by both HTTP and CloudEvents options and should be moved to the shared location.

The existing `Http/` code should be refactored to call into these shared helpers. This is a refactoring step that should be done first, with existing tests verifying no regressions.

### Options and Conversion Services

**`LightResultsCloudEventWriteOptions`:**
- `Source` (string?) — default source URI, used when not provided per call.
- `MetadataSerializationMode` — same enum as HTTP (relocated to `SharedJsonSerialization/`), but defaults to `Always` (unlike HTTP which defaults to `ErrorsOnly`). In messaging, metadata is expected to travel with messages, and silent data loss on success results would be surprising.
- `SerializerOptions` (JsonSerializerOptions) — used to serialize `T` values and metadata objects (e.g. via `WriteGenericValue<T>`). Unlike ASP.NET Core integrations where `JsonOptions` are available from DI, CloudEvents serialization is framework-agnostic and requires the caller to provide serializer options explicitly.
- `ConversionService` (ICloudEventAttributeConversionService) — maps metadata keys/values to CloudEvents attribute keys/values on write. Analogous to `IHttpHeaderConversionService`. A default implementation (`DefaultCloudEventAttributeConversionService`) validates attribute names, rejects reserved names, and emits primitive JSON values. The property is pre-initialized with a singleton instance of the default implementation, so callers only need to replace it when custom conversion logic is required.

**`LightResultsCloudEventReadOptions`:**
- `SerializerOptions` (JsonSerializerOptions?) — for deserializing `data`.
- `PreferSuccessPayload` (`PreferSuccessPayload`) — controls how successful `Result<T>` payloads are interpreted, using the same enum and auto-detection algorithm as `LightResultsHttpReadOptions.PreferSuccessPayload`. The default is `Auto`.
- `IsFailureType` (Func<string, bool>) — optional fallback callback to determine failure from the `type` attribute when the `lroutcome` extension attribute is absent.
- `ParsingService` (ICloudEventAttributeParsingService?) — optional service that maps CloudEvents extension attribute keys/values back to metadata keys/values on read. Analogous to `IHttpHeaderParsingService`. Only used when the caller opts in to envelope-to-metadata conversion. When null, extension attributes are not converted to metadata (Tier 1 default behavior).
- `MergeStrategy` (`MetadataMergeStrategy`) — controls how extension attribute metadata (from the envelope) is combined with metadata from the `data` payload when both are present. Reuses the existing `MetadataMergeStrategy` enum from `Light.Results.Metadata`. The default is `AddOrReplace`, matching the behavior of `LightResultsHttpReadOptions.MergeStrategy`: envelope extension attributes are processed first, then data payload metadata is merged second, so payload values overwrite envelope values on key conflicts.

**`ICloudEventAttributeConversionService`:**
Maps metadata entries to CloudEvents attribute key/value pairs. Follows the same pattern as `IHttpHeaderConversionService` / `DefaultHttpHeaderConversionService` — a converter registry keyed by metadata key, with a default implementation that uses a `FrozenDictionary<string, CloudEventAttributeConverter>`. The default implementation must reject attempts to map metadata to `data`, `data_base64`, or `lroutcome`.

**`ICloudEventAttributeParsingService`:**
Maps CloudEvents extension attribute entries back to metadata key/value pairs. Follows the same pattern as `IHttpHeaderParsingService` / `DefaultHttpHeaderParsingService` — a parser registry keyed by attribute name, with a default implementation that uses a `FrozenDictionary<string, CloudEventAttributeParser>`.

### Composition Root Integration

Following the pattern of `Http/Writing/Module.cs` and `Http/Reading/Module.cs`, provide `Module` classes in the CloudEvents writing and reading namespaces with `IServiceCollection` extension methods for DI registration:

- **Writing Module** — registers `LightResultsCloudEventWriteOptions` via the options pattern and wires up the `ICloudEventAttributeConversionService` with its `FrozenDictionary<string, CloudEventAttributeConverter>` registry (analogous to `AddLightResultsHttpHeaderConversionService`).
- **Reading Module** — registers `LightResultsCloudEventReadOptions` via the options pattern and wires up the `ICloudEventAttributeParsingService` with its `FrozenDictionary<string, CloudEventAttributeParser>` registry (analogous to `AddLightResultsHttpHeaderParsingService`).
