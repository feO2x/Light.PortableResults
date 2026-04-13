# HttpResponseMessage Deserialization for Light.Results

## Goals & Constraints
- Deserialize `Result` and `Result<T>` from `HttpResponseMessage` (HttpClient scenarios).
- Preserve metadata from response body and/or headers with configurable header selection.
- Support both validation error formats (ASP.NET Core compatible and rich) and auto-detect the format.
- Implement `Read` methods for `DefaultResultJsonConverter`, `DefaultResultJsonConverter<T>`, `MetadataObjectJsonConverter`, `MetadataValueJsonConverter`.
- Keep code Native AOT compatible (avoid reflection/dynamic code where possible; rely on `JsonTypeInfo` when reading `T`).
- Maintain Light.Results performance bias; avoid unnecessary allocations.

## Current Serialization Shapes (for reference)
- **Success (Result<T>, MetadataSerializationMode.ErrorsOnly):** body is bare JSON value of `T`.
- **Success (Result<T>, MetadataSerializationMode.Always):** `{ "value": <T>, "metadata": { ... } }` (metadata omitted if no response-body-annotated values).
- **Success (Result, MetadataSerializationMode.Always):** `{ "metadata": { ... } }` (or empty body if no response-body-annotated values).
- **Error (all):** RFC 9457 problem details with an `errors` property.
  - **ASP.NET Core compatible (400/422):** `errors` is an object of `target -> string[]` plus optional `errorDetails` array.
  - **Rich format (all other cases or explicit setting):** `errors` is an array of error objects.

## Proposed API Surface (HttpClient)
- Add `HttpResponseMessage` extension methods in `Light.Results`:
  - `Task<Result> ReadResultAsync(this HttpResponseMessage response, LightResultHttpReadOptions? options = null, JsonSerializerOptions? serializerOptions = null, CancellationToken cancellationToken = default)`
  - `Task<Result<T>> ReadResultAsync<T>(this HttpResponseMessage response, LightResultHttpReadOptions? options = null, JsonSerializerOptions? serializerOptions = null, CancellationToken cancellationToken = default)`
- `LightResultHttpReadOptions` (new):
  - `HeaderSelectionMode` (None | All | AllowList | DenyList)
  - `HeaderAllowList` / `HeaderDenyList` (case-insensitive by default)
  - `HeaderConflictStrategy` (Throw | LastWriteWins) when multiple header names map to the same metadata key (default: Throw)
  - `MergeStrategy` for combining body/headers metadata (default `AddOrReplace`)
  - `PreferSuccessPayload` (Auto | BareValue | WrappedValue) for disambiguating `{ "value": ... }` vs value objects. Auto rule: treat as wrapper only when the root object contains only `value` and optional `metadata`.
  - `TreatProblemDetailsAsFailure` (default: true)
  - `HeaderMetadataAnnotation` (default `SerializeInHttpHeader`)
  - Optional `HttpHeaderParser` registry / `IHttpHeaderParsingService` override

## JSON Converter Read Design
- Implement a shared `ResultJsonReader` helper to parse:
  - **Problem details** vs **success payload** with an explicit `isFailure` hint.
  - Auto-detect validation format based on `errors` token type (array => rich, object => ASP.NET Core compatible).
- `MetadataValueJsonConverter.Read`:
  - Parse JSON token types directly into `MetadataValue` (null/bool/number/string/array/object).
  - For numbers: use `MetadataValue.FromInt64` when the value is integral and fits `long`; otherwise use `MetadataValue.FromDouble`.
  - Use `MetadataArrayBuilder` and `MetadataObjectBuilder` for efficient construction.
- `MetadataObjectJsonConverter.Read`:
  - Read object properties and delegate each value to `MetadataValueJsonConverter.Read`.
- `DefaultResultJsonConverter.Read` / `DefaultResultJsonConverter<T>.Read`:
  - Use `ResultJsonReader` (no reflection) and `JsonTypeInfo` for `T` (same pattern as `WriteGenericValue`).
  - Provide a fallback to treat the entire JSON payload as the `T` value when not a problem details document.

## HttpResponseMessage Deserialization Algorithm
- Determine failure based on:
  - Non-success status code **or** `Content-Type: application/problem+json` (configurable).
  - `Content-Type` checks compare media type only (ignore parameters like `charset`).
- If failure:
  - If the body is not valid problem-details JSON, throw.
  - Parse problem details into `Errors` + metadata.
  - Only deserialize the `metadata` property; other extensions are ignored.
  - If errors missing, create a single `Error` using `title`/`detail`/`status` as a fallback.
- If success:
  - For `Result<T>`:
    - If empty body => throw (caller must ensure the endpoint returns a value).
    - Otherwise parse as either wrapped or bare value (based on `PreferSuccessPayload` + heuristics). Auto heuristic: wrapper only when root has only `value` and optional `metadata` properties.
    - If a wrapper is detected but `value` is missing => throw.
  - For `Result`:
    - Empty body => `Result.Ok()`.
    - Object with `metadata` only => `Result.Ok(metadata)`.
    - Non-empty body that is not `{ "metadata": ... }` => throw.
- Extract headers into metadata per `HeaderSelectionMode` from both `HttpResponseMessage.Headers` and `HttpResponseMessage.Content.Headers`, then merge with body metadata.

## Header Metadata Parsing
- New `HttpHeaderParser` base type (parallel to `HttpHeaderConverter`) with `SupportedHeaderNames` and a target metadata key.
- Register parsers and build a `FrozenDictionary<string, HttpHeaderParser>` keyed by header name (case-insensitive).
  This enables multiple header names to map to the same metadata key for backwards compatibility.
- If multiple header names resolve to the same metadata key in a single response, default to throwing; allow a configurable
  `HeaderConflictStrategy.LastWriteWins` override.
- Default parsing behavior:
  - Attempt JSON parse to `MetadataValue` (valid for numbers/booleans/quoted strings/arrays).
  - Fallback: treat raw header string as `MetadataValue.FromString`.
  - Support multi-value headers by mapping to `MetadataArray` of parsed primitives.
- Apply `HeaderSelectionMode` and use `HeaderMetadataAnnotation` on values.

## Native AOT Considerations
- Avoid `Activator.CreateInstance` or `MakeGenericType` in new code paths.
- For reading `T`, use `JsonSerializerOptions.GetTypeInfo(typeof(T))` and the converter from type info.
- Keep `ResultJsonReader` purely `Utf8JsonReader`-based to avoid `JsonDocument` allocations.
- Any new factory should be source-generated friendly or sealed with explicit generic methods.

## Test Plan
- **Unit tests (Light.Results.Tests):**
  - Deserialize success with bare value, wrapped value + metadata, and metadata-only (non-generic).
  - Deserialize error responses for rich format and ASP.NET Core compatible format.
  - Header selection: all/allow/deny/none, multi-value headers, merge strategy behaviors.
  - Header alias conflicts (default throw; LastWriteWins override).
  - Wrapper missing `value` throws; non-generic unexpected body throws.
  - Numeric parsing: integral values to `int64`, non-integral to `double`.
  - Empty body handling for `Result<T>` vs `Result`.
- **Integration tests (optional):** round-trip through Minimal API (server) + HttpClient (client) with headers/metadata.
