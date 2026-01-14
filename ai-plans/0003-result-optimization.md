# Plan for Result/Errors performance & metadata enhancements

## Goals

1. Tighten `Result<T>` / `Result` hot-path performance: minimize allocations, avoid unnecessary copying, and keep struct size predictable.
2. Extend `Error`/`Errors` information model with service-level metadata (`Source`, `CorrelationId`, `Category`) without regressing performance.
3. Maintain RFC 9457/gRPC-friendly metadata surfaces introduced in plan 0001.

## Current snapshot

- `Result<T>` is a readonly struct holding `T Value`, `Errors _errors`, and `MetadataObject? _metadata`. Success path stores metadata as nullable value type but still pays an extra field.
- `Errors` uses SBO: single error inline, multiple in `Error[]`.
- `Error` currently contains message, code, target, metadata only.

## Proposed optimizations

### 1. Struct layout & field packing

- Reorder `Result<T>` fields (`Errors _errors`, `MetadataObject? _metadata`, `T Value`, `bool IsSuccess`) to reduce padding. Measure `Unsafe.SizeOf<Result<T>>()` before/after.
- Consider `MetadataObject?` → `MetadataObjectReference` (internal struct containing pointer + bool) to avoid Nullable boxing overhead.
- Provide `internal Result(T value, Errors errors, MetadataObject? metadata, bool isSuccess)` factory to avoid duplicate constructors.

### 2. Avoid redundant metadata merging/copies

- In `Bind`/`Map`, skip metadata merging if references are equal (`ReferenceEquals`). Already partially done; enforce across all helper methods.
- Introduce `MetadataObject.MergeIfNeeded(existing, incoming, strategy)` returning same reference when incoming is empty; propagate to `Result<T>.MergeMetadata`.

### 3. Errors storage, enumeration & conversions

- Remove `Result<T>.ErrorList` (`ImmutableArray<Error>`) entirely; instead expose the internal `Errors` struct via `public Errors Errors => _errors;`.
- Rework `Errors` to keep `_one` for SBO plus a `ReadOnlyMemory<Error>` for multi-error cases.
- Implement `Errors : IReadOnlyList<Error>` with a custom value-type enumerator that stores `_one`/`_many` references and iterates without a compiler-generated state machine (single case uses `_one`, multi case indexes `_many.Span`). Indexer and `Count` become zero-cost operations.

### 4. Result&lt;T> TryGetValue

- Add `Result<T>.TryGetValue(out T value)` that returns `true` for success without throwing.
- When false is returned, `Errors` is not null. Use appropriate NRT attributes to help the compiler understand this.

## Error model enhancements

### New fields

- `ErrorCategory Category` enum covering Validation, NotFound, Conflict, Unauthorized, Forbidden, DependencyFailure, Transient, Unexpected. Backed by byte to keep struct tiny; default = `ErrorCategory.Unclassified` (0).

### API & perf implications

- Update `Error` record struct signature to include new fields with default values to preserve existing call sites:
  ```csharp
  public readonly record struct Error(
      string Message,
      string? Code = null,
      string? Target = null,
      MetadataObject? Metadata = null,
      ErrorCategory Category = ErrorCategory.Unclassified)
  ```
- Ensure `Errors` SBO still works: when storing single error, new fields live inline; when many, array holds enriched errors.
- Provide factory helpers: `Error.Validation(string message, string? target = null, ...)` to set categories & defaults without repeated args.
- Update serialization adapters (RFC 9457/gRPC) to map new properties:
  - `Category` → `extensions.category`

## Implementation notes

### Tracing support via metadata extensions (architectural decision)

**Original plan**: Add `Source` (string?) and `CorrelationId` (Guid?) as direct fields on the `Error` struct.

**Implemented approach**: Keep `Error` struct minimal and provide tracing support through metadata extensions via the
`IHasOptionalMetadata<T>` interface.

**Rationale for deviation**:

1. **Smaller struct size**: Keeping `Error` minimal improves cache locality and reduces memory footprint. Adding
   `Source` (8 bytes ref + 1 byte hasValue) and `CorrelationId` (16 bytes Guid + 1 byte hasValue) would add ~26 bytes
   per error.
2. **Flexibility**: Metadata-based approach allows arbitrary tracing properties without bloating the core `Error` type.
   Different applications may need different tracing identifiers (trace ID, span ID, request ID, etc.).
3. **Opt-in overhead**: Applications that don't use tracing don't pay for unused fields. Metadata allocation only occurs
   when explicitly needed.
4. **Extensibility without breaking changes**: New tracing properties can be added via extension methods without
   modifying the `Error` struct signature.

**Implementation details**:

- Introduced `IHasOptionalMetadata<T>` interface to unify metadata operations across `Result<T>`, `Result`, and
  potentially `Error` in the future
- `Tracing` extension methods in `Light.Results.MetadataExtensions` namespace provide:
    - `WithSource(string source)` - adds source identifier to metadata
    - `WithCorrelationId(string correlationId)` - adds correlation ID to metadata
    - `WithTracing(string source, string correlationId)` - adds both in one call
    - `TryGetSource(out string? source)` - retrieves source from metadata
    - `TryGetCorrelationId(out string? correlationId)` - retrieves correlation ID from metadata
- Uses string for correlation ID (not Guid) to support various formats (UUID, hex strings, custom formats)
- Metadata keys: `"source"` and `"correlationId"`

**Trade-offs accepted**:

- ❌ Requires metadata allocation for tracing (vs. inline fields)
- ❌ Lookup overhead via dictionary access (vs. direct field access)
- ✅ Significantly smaller `Error` struct
- ✅ Zero cost for non-tracing scenarios
- ✅ More flexible and extensible

**Serialization implications**:

- RFC 9457/gRPC serialization adapters should check both Error.Metadata and Result.Metadata for tracing properties
- Map `metadata["source"]` → `extensions.source` or `instance`
- Map `metadata["correlationId"]` → `traceId` or equivalent tracing header
