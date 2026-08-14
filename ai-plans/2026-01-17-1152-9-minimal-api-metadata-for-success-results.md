# Metadata Serialization for Success Results in Minimal APIs

*Please note: this plan was written before the AGENTS.md file in this folder was updated to its current form. Do not
take this plan as an example for how to write plans.*

## Goals & Constraints

1. **Opt-in feature**: Allow callers to include `MetadataObject` in success responses, changing the response schema.
2. **Per-value control**: Enable fine-grained control over where each metadata value is serialized (HTTP body, headers, or both).
3. **Performance**: Maintain the low-allocation design of Light.Results; use source-generated JSON serialization.
4. **Schema clarity**: When metadata is included, the response body uses a wrapped format: `{ "value": T, "metadata": {...} }`.
5. **Native AOT compatibility**: All new types must work with `<IsAotCompatible>true</IsAotCompatible>`.
6. **Minimal API focus**: This plan covers ASP.NET Core Minimal APIs only.

## Current State Analysis

### Success Response Handling (Current)

From `MinimalApiResultExtensions.cs`:

```csharp
if (result.IsValid)
{
    return TypedResults.Ok(result.Value);  // Metadata is discarded
}
```

- `Result<T>.Metadata` is only serialized when the result has errors (via `LightProblemDetailsResult`).
- Success responses return `T` directly, losing any attached metadata.

### Use Cases for Metadata on Success

| Use Case | Example Metadata |
|----------|------------------|
| Pagination | `totalCount`, `nextPageToken`, `hasMore` |
| Cache hints | `etag`, `lastModified` |
| Audit/tracing | `correlationId`, `processingTimeMs` |
| Warnings | Non-fatal issues that didn't prevent success |
| Feature flags | `experimentGroup`, `featureEnabled` |

## Design Decisions

### 1. Opt-In Enum for Metadata Serialization

**Decision**: Introduce `MetadataSerializationMode` enum to control when metadata is serialized.

```csharp
/// <summary>
/// Specifies when metadata should be serialized in HTTP responses.
/// </summary>
public enum MetadataSerializationMode
{
    /// <summary>
    /// Metadata is only serialized for error responses (Problem Details).
    /// Success responses return the value directly without metadata.
    /// This is the default behavior.
    /// </summary>
    ErrorsOnly,

    /// <summary>
    /// Metadata is always serialized, even for success responses.
    /// Success responses use a wrapped format: { "value": T, "metadata": {...} }.
    /// </summary>
    Always
}
```

**Rationale**:
- `ErrorsOnly` preserves backward compatibility and clean API contracts.
- `Always` enables rich metadata transport for specialized use cases.

### 2. Wrapped Response Format

When `MetadataSerializationMode.Always` is used, success responses use this structure:

```json
{
  "value": { /* T */ },
  "metadata": {
    "totalCount": 42,
    "correlationId": "abc-123"
  }
}
```

**Design notes**:
- `value` contains the serialized `T`.
- `metadata` is omitted if `MetadataObject` is `null` (to avoid `"metadata": null`), but included as `{}` if empty to maintain schema consistency.
- For `Result` (void), the response is `{ "metadata": {...} }` with HTTP 200 (not 204, since there's a body).

### 3. Custom IResult Implementations

**Decision**: Create `LightResult<T>` for `Result<T>` and `LightResult` for `Result`, implementing Minimal API's
`IResult` for wrapped success responses.

**Rationale**:

- These types replace the existing `LightProblemDetailsResult`.
- Mirrors the performance approach of `LightProblemDetailsResult`.
- Enables source-generated JSON serialization.
- Implements `IResultTypeMetadata<T>` for OpenAPI schema generation.

**Rationale for parameter order**: `metadataMode` is placed last because:
- It's the newest parameter and least commonly used.
- Default value of `ErrorsOnly` preserves existing behavior.
- Callers opting in will typically use named arguments: `metadataMode: MetadataSerializationMode.Always`.

### 4. JSON Serialization Strategy

Two converters are needed:

#### LightSuccessResultJsonConverter<T> (Generic)

For `LightResult<T>` (corresponding to `Result<T>`).

When the result is valid, then:

1. Writes `{ "value": ... }` using the serializer for `T`.
2. Writes `"metadata": ...` using the existing `MetadataObjectJsonConverter`.

When the result is invalid, then:

1. Writes Problem Details response
2. Writes `"metadata": ...` using the existing `MetadataObjectJsonConverter`.

#### LightResultJsonConverter (Non-Generic)

Basically the same as above, but when the result is valid, then just write the metadata if required.

### OpenAPI Schema Generation

#### Complexity

There are **two sources of complexity** for OpenAPI schema generation:

1. **Generic type `T`**: The `value` property's schema depends on `T`, which is only known at runtime. ASP.NET Core's `IEndpointMetadataProvider` uses static methods, making it difficult to provide type-specific schema information.

2. **Dynamic metadata structure**: `MetadataObject` is a flexible key-value store—its schema varies per endpoint. OpenAPI expects a fixed schema, but metadata keys/types are determined by the application logic, not the type system.

#### Approach: Explicit Schema Generation by the User

**Opt-in typed metadata**: Users who want typed OpenAPI schemas should use the
`ProducesLightResult<TValue, TMetadata>()` extension method to register a specific metadata type.

#### Usage

```csharp
// Typed metadata: full schema in OpenAPI
app.MapGet("/items", GetItems)
   .ProducesLightResult<List<Item>, PaginationMetadata>();
```

#### User-Defined Metadata Types

Users define their metadata shape as a regular C# type:

```csharp
// User defines their metadata shape
public record PaginationMetadata(int TotalCount, string? NextPageToken, bool HasMore);

// Endpoint with typed metadata schema
app.MapGet("/items", async () =>
{
    var result = await GetItemsAsync();
    return result
        .WithMetadata("totalCount", 42)
        .WithMetadata("nextPageToken", "abc123")
        .WithMetadata("hasMore", true)
        .ToMinimalApiResult(metadataMode: MetadataSerializationMode.Always);
})
.ProducesLightResult<List<Item>, PaginationMetadata>();
```

**Note**: The `PaginationMetadata` type is only used for OpenAPI schema generation. At runtime, `MetadataObject` is still used for flexibility. Users are responsible for ensuring the runtime metadata matches the documented schema.

### 9. Per-Value Serialization Annotations

In addition to controlling metadata serialization at the result level, we provide **per-value control** by adding annotations to `MetadataValue`. This enables fine-grained decisions about where each metadata value should be serialized (HTTP response body, HTTP headers, or both).

#### MetadataValueAnnotation Flags Enum

Add a new flags enum to the core `Light.Results` project:

```csharp
namespace Light.Results.Metadata;

/// <summary>
/// Specifies where a metadata value should be serialized in HTTP responses.
/// </summary>
[Flags]
public enum MetadataValueAnnotation
{
    /// <summary>
    /// No annotation specified. Serialization behavior is determined by the serialization mode.
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
    SerializeInBoth = SerializeInHttpResponseBody | SerializeInHttpHeader
}
```

#### MetadataValue Changes

Add an `Annotation` property to `MetadataValue`:

```csharp
public readonly struct MetadataValue : IEquatable<MetadataValue>
{
    private readonly MetadataPayload _payload;

    private MetadataValue(MetadataKind kind, MetadataPayload payload, MetadataValueAnnotation annotation)
    {
        Kind = kind;
        _payload = payload;
        Annotation = annotation;
    }

    public MetadataKind Kind { get; }
    public MetadataValueAnnotation Annotation { get; }  // NEW

    // Factory methods updated to accept optional annotation
    public static MetadataValue FromString(string? value, MetadataValueAnnotation annotation = MetadataValueAnnotation.None) =>
        value is null ? Null : new MetadataValue(MetadataKind.String, new MetadataPayload(value), annotation);

    // ... similar updates for other factory methods
}
```

#### Header Serialization Constraints

`SerializeInHttpHeader` is only valid for types that can be represented as HTTP header values:

| MetadataKind | Header-Compatible | Notes |
|--------------|-------------------|-------|
| `Null` | No | Omit the header entirely |
| `Boolean` | Yes | `"true"` or `"false"` |
| `Int64` | Yes | String representation |
| `Double` | Yes | String representation |
| `String` | Yes | Direct value |
| `Array` | Conditional | Only if all elements are primitives |
| `Object` | No | Too complex for headers |

**Validation**: Factory methods throw `ArgumentException` if `SerializeInHttpHeader` is set on an incompatible type.

#### Array Header Serialization

For arrays of primitives, use **comma-separated values** (per RFC 7230):

```
X-Tags: foo, bar, baz
```

Alternatively, multiple headers with the same name could be used, but comma-separation is more compact and widely supported.

#### Usage Example

```csharp
var result = Result<string[]>
    .Ok(items)
    .MergeMetadata(
        ("correlationId", MetadataValue.FromString(correlationId, MetadataValueAnnotation.SerializeInHttpHeader)),
        ("totalCount", MetadataValue.FromInt64(42, MetadataValueAnnotation.SerializeInHttpResponseBody)),
        ("etag", MetadataValue.FromString(etag, MetadataValueAnnotation.SerializeInBoth))
    );
```

This would produce:
- **Headers**: `X-CorrelationId: abc-123`, `ETag: "xyz"`
- **Body**: `{ "value": [...], "metadata": { "totalCount": 42, "etag": "xyz" } }`

#### Header Naming Convention

Metadata keys are converted to HTTP header names using a configurable strategy:
- Default: `X-{PascalCase(key)}` (e.g., `correlationId` → `X-CorrelationId`)
- Well-known headers: `etag` → `ETag`, `lastModified` → `Last-Modified`

#### Impact on Struct Size

Adding `MetadataValueAnnotation` (4 bytes) to `MetadataValue` is acceptable given the flexibility it provides. The struct remains small and stack-allocated.

### Annotation Behavior with MetadataSerializationMode

The `MetadataValueAnnotation` flags interact with `MetadataSerializationMode` as follows:

| MetadataSerializationMode | Annotation | Body Serialization | Header Serialization |
|---------------------------|------------|-------------------|---------------------|
| `ErrorsOnly` | `None` | No | No |
| `ErrorsOnly` | `SerializeInHttpResponseBody` | No | No |
| `ErrorsOnly` | `SerializeInHttpHeader` | No | **Yes** |
| `ErrorsOnly` | `SerializeInBoth` | No | **Yes** |
| `Always` | `None` | Yes | No |
| `Always` | `SerializeInHttpResponseBody` | Yes | No |
| `Always` | `SerializeInHttpHeader` | No | Yes |
| `Always` | `SerializeInBoth` | Yes | Yes |

**Key insight**: Header annotations are honored regardless of `MetadataSerializationMode`. This allows metadata like `correlationId` to be written to headers even when the body uses the unwrapped format (`ErrorsOnly`).
