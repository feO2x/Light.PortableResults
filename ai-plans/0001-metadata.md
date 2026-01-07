# Plan for metadata in Light.Results

## Goals & Constraints

1. Metadata must be trivially serializable/deserializable across:
    - HTTP payloads that conform to RFC 9457 (problem details).
    - gRPC error trailers/payloads.
    - Asynchronous messaging envelopes.
2. The shape of metadata must be statically enforceable so that only JSON-compatible values are representable.
3. Keep allocations to a minimum. Ideally, the metadata types are stack-only structs backed by small arrays/spans with
   pooling.
4. Integration points:
    - Replace the current `IReadOnlyDictionary<string, object?>? Meta` on `Error` with the new types without breaking
      ergonomics for callers that only need primitive metadata @src/Light.Results/Error.cs#5-10.
    - Introduce metadata support on `Result<T>` (and non-generic `Result`) for correlation/context payloads that apply
      regardless of success/failure, while preserving zero-cost success cases @src/Light.Results/Result.cs#11-146.
    - Ensure `Result<T>` stays allocation-free when success paths avoid metadata.

## Value Domain (JSON-Compatible)

- Primitive scalars: string, long (int64), double (IEEE 754), bool, null.
- Developer ergonomics:
    - `float` inputs are accepted via implicit conversion and stored as double (no precision loss beyond IEEE 754
      rules).
    - `decimal` inputs are accepted and canonically stored as strings using
      `decimal.ToString(CultureInfo.InvariantCulture)`.
      Retrieval helpers (see API) can materialize decimals back by parsing; errors bubble up if the payload cannot be
      represented exactly.
- NaN and ±Infinity are rejected at creation/serialization time to remain RFC 9457 and protobuf friendly.
- Structured values:
    - Arrays: ordered sequences of other metadata values.
    - Objects: string-keyed property bags whose values are other metadata values.
- No delegates, type handles, or arbitrary CLR graphs; everything must collapse to the above recursively.

### Decimal Storage Rationale

- Even though we accept `decimal` inputs, they are stored canonically as invariant-culture strings. Reasons:
    1. **Transport compatibility** – JSON and protobuf `Struct` lack first-class decimal representations; they require
       textual encoding anyway, so keeping the canonical string internally avoids double conversions at serialization.
    2. **Struct size budget** – embedding `decimal` (16 bytes) into `MetadataValue` alongside existing payload fields
       would inflate the struct and increase copy cost.
    3. **Cross-language predictability** – downstream services in other runtimes expect string-encoded decimals; storing
       them as strings keeps round-trips deterministic.
- Developer ergonomics are preserved via:
    - Implicit decimal overloads that perform the canonical conversion automatically.
    - `MetadataValue.TryGetDecimal` / `MetadataObject.TryGetDecimal` which parse once and can cache the materialized
      decimal within the payload for repeated reads.

## Core Types

1. `MetadataValue`
    - `readonly struct` discriminated union with two fields: `MetadataKind Kind` + `MetadataPayload Payload`.
    - `MetadataPayload` is a small struct containing `long I64`, `double F64`, `object? Ref`. Booleans piggyback on
      `I64` (0/1). Strings/arrays/objects live in `Ref`.
    - Factory members: `MetadataValue.FromString(...)`, implicit conversions from primitive CLR types (`int`, `long`,
      `float`, `double`, `decimal`), plus `MetadataValue.FromJsonNumber(ReadOnlySpan<char>)` to centralize numeric
      policy
      (prefers Int64 when exact, otherwise double; decimal literals can be routed through string representation).
    - Validation enforces numeric policy (reject NaN/Infinity) and maximum depth/size (see guardrails).

2. `MetadataArray`
    - `readonly struct` view over a sealed backing (`MetadataArrayData`) that owns a `MetadataValue[]` plus length.
    - Provides allocation-free enumeration; exposes `Span<MetadataValue>` internally for builders.
    - Builders rent buffers from `ArrayPool<MetadataValue>`, fill them, then call `MetadataArrayData.FromOwnedBuffer`
      which trims/copies once and returns a reusable backing instance.

3. `MetadataObject`
    - `readonly struct` view over `MetadataObjectData` backing which stores parallel arrays of keys/values and optional
      lazy dictionary for faster lookup when property count exceeds a threshold (e.g., >8).
    - Deterministic ordering: properties are stored sorted by `StringComparer.Ordinal` during build, ensuring stable
      serialization/caching.
    - Builders maintain uniqueness (error on duplicate keys) and only materialize the optional dictionary when lookups
      demand it.

### Immutability & Ownership

- Public view types never expose mutable buffers; all mutation happens inside builders before the backing objects are
  sealed.
- Builders (`MetadataObjectBuilder`, `MetadataArrayBuilder`) are `ref struct` types implementing a scope pattern:
    - `MetadataObjectBuilder.Create()` rents buffers, tracks an `_active` flag, and optionally implements `Dispose`
      (returning buffers) so `using var builder = ...;` is the happy path.
    - After `Build()` the builder becomes invalid (debug assert if reused), forcing callers to respect the pooling
      contract.
- Backing instances ensure no references to pooled arrays remain once sealed; large metadata graphs can therefore be
  shared safely across threads.

### Backing Storage Model

- `MetadataArrayData` / `MetadataObjectData` are sealed reference types that own arrays, cached hash codes, and optional
  lookup dictionaries. They are internal and never exposed.
- `MetadataArray` / `MetadataObject` store just two fields: a reference to the backing + length (or property count).
- This pattern keeps public structs tiny (two references + count) and allows lazy caching (e.g., computed hash) without
  changing the public API.
- Builders produce these backings once, avoiding double allocations versus copying data out of pools.

## Result-Level Metadata

- Motivation:
    - Capture execution-wide metadata (correlation IDs, timing data, server node) even when no errors exist.
    - Enable transport layers to propagate metadata without materializing an `Error`.
- Design:
    - Add optional `MetadataObject? Metadata` property on `Result<T>` and the non-generic `Result`.
    - Store metadata in a dedicated field rather than piggybacking on `_errors` to avoid conflating concerns.
    - Provide fluent helpers (`WithMetadata`, `MergeMetadata`) that return new `Result<T>` without copying when the
      metadata reference matches.
    - When upgrading `Result<T>` with new metadata on success, delay allocation by using `MetadataObjectBuilder` that
      writes into a pooled buffer only if metadata is actually added.
    - On failure, propagate both `Result<T>.Metadata` (context) and `Error.Metadata` (per-error) when serializing.
    - Serialization: envelope-level metadata maps to RFC 9457’s top-level extension members, while per-error metadata
      maps to each `error` entry’s extensions.

## Memory & Performance Considerations

- Use `readonly struct` + `readonly ref struct` builders to keep metadata on stack where possible.
- When builders must allocate:
    - Rent buffers from `ArrayPool<T>` and seal them into `ImmutableArray<T>` (copy) or keep pooled arrays with
      copy-on-write semantics to avoid double allocations.
- Strings remain reference types; expose public builder APIs that accept `ReadOnlySpan<char>` /
  `ReadOnlySpan<MetadataValue>`
  so callers can feed preallocated data without extra copies.
- Investigate source generators for compile-time creation of metadata (e.g., `Metadata.Object(("key", 42))`) to avoid
  runtime array allocations.

## Serialization Strategy

- JSON/RFC 9457:
    - Provide `MetadataJsonConverter` for System.Text.Json built on `Utf8JsonWriter/Utf8JsonReader` (no JsonDocument,
      no boxing).
    - Enforce deterministic order (keys sorted ordinal during build) and guardrails (max depth 64, max properties per
      object, max array length) while reading.
    - Reject NaN/Infinity values; parse numbers as Int64 when integral and in-range, otherwise double.
- gRPC:
    - Map `MetadataObject` to protobuf `Struct`/`Value`, or define our own proto schema mirroring the value union.
    - Provide extension methods to emit/read `MetadataValue` to/from `Google.Protobuf.WellKnownTypes.Value`.
  - Keep code structured so we can later swap in a dedicated `MetadataValueProto` without breaking callers.
- Async messaging:
    - Ensure metadata can be flattened into a UTF-8 JSON payload; expose `IBufferWriter<byte>` writer to stream without
      intermediate strings.

| Metadata construct    | RFC 9457 mapping                                    | gRPC mapping                                    | Async messaging (JSON envelope)   |
|-----------------------|-----------------------------------------------------|-------------------------------------------------|-----------------------------------|
| Result-level metadata | Top-level extension members on the Problem Details. | `google.protobuf.Struct` attached to status.    | Envelope-level `metadata` object. |
| Per-error metadata    | `errors[n].extensions` object.                      | `google.rpc.Status.details[n]` per error entry. | `errors[n].metadata` object.      |
| Primitive values      | Native JSON fields.                                 | `google.protobuf.Value` scalar kinds.           | JSON primitives.                  |
| Arrays/objects        | JSON arrays/objects; maintain deterministic order.  | `google.protobuf.ListValue` / `Struct`.         | JSON arrays/objects.              |

## API Surface Sketch

- `readonly record struct Error(...)` becomes:
  ```csharp
  public readonly record struct Error(
      string Message,
      string? Code = null,
      string? Target = null,
      MetadataObject? Metadata = null);
  ```
- Helper entry points:
    - `Error.WithMetadata(params (string Key, MetadataValue Value)[] properties)`
  - `MetadataValue.From<T>(T value)` constrained to supported primitives. Decimal overload serializes to canonical
    string automatically.
  - `MetadataObject.With(string key, MetadataValue value, ...)` for concise construction without a separate map type.
- Typed getters:
    - `bool MetadataObject.TryGetInt64(string key, out long value)`
    - `bool MetadataObject.TryGetString(string key, out string? value)`
    - `bool MetadataObject.TryGetDecimal(string key, out decimal value)` (parses decimal-encoded strings and exact
      doubles where possible).
    - `bool MetadataObject.TryGetObject(string key, out MetadataObject value)` etc.
    - `bool MetadataValue.TryGetInt64(out long value)` instance members, so consumers avoid `switch`.
    - `bool MetadataValue.TryGetDecimal(out decimal value)` that succeeds when the payload is either a canonical decimal
      string or a double that fits into decimal without precision loss.
- Builders for advanced scenarios:
    - `ref struct MetadataObjectBuilder`
    - `ref struct MetadataArrayBuilder`
  - Both builder types expose public `Add(ReadOnlySpan<char> key, MetadataValue value)` /
    `AddRange(ReadOnlySpan<MetadataValue>)`
    overloads so high-performance callers can pass spans without intermediate allocations.
- Result helpers:
    - `Result<T> WithMetadata(MetadataObject metadata)` / `Result WithMetadata(...)`.
    - `Result<T> MergeMetadata(MetadataObject other, MetadataMergeStrategy strategy)`.
    - `MetadataObject? Result<T>.Metadata` exposed publicly with defensive copy only when builders were used.

### Metadata Merge Semantics

- Default strategy: **AddOrReplace**, where keys from the incoming metadata overwrite existing keys (recursively for
  objects, entire-array replacement for arrays to avoid surprising element-wise behavior).
- Alternative strategies:
    1. **PreserveExisting** – keep original values, ignore duplicates.
    2. **FailOnConflict** – throw if the same key is present in both sources (useful for safety-critical metadata).
- Merges preserve deterministic key order (e.g., insertion order) so serialization remains stable regardless of
  strategy.
- Provide a small helper (`MetadataObject.Merge`) that takes the strategy enum, ensuring consistent behavior between
  `Error` and `Result` helpers.

#### Sample Merge Algorithm (AddOrReplace)

```csharp
public static MetadataObject Merge(
    MetadataObject original,
    MetadataObject incoming,
    MetadataMergeStrategy strategy = MetadataMergeStrategy.AddOrReplace)
{
    var builder = MetadataObjectBuilder.From(original);

    foreach (var (key, value) in incoming)
    {
        if (!builder.TryGetValue(key, out var existing))
        {
            builder.Add(key, value);
            continue;
        }

        switch (strategy)
        {
            case MetadataMergeStrategy.AddOrReplace:
                builder.Replace(key, MergeValues(existing, value, strategy));
                break;
            case MetadataMergeStrategy.PreserveExisting:
                break;
            case MetadataMergeStrategy.FailOnConflict:
                throw new InvalidOperationException($"Duplicate metadata key '{key}'.");
        }
    }

    return builder.Build();
}

private static MetadataValue MergeValues(
    MetadataValue left,
    MetadataValue right,
    MetadataMergeStrategy strategy)
{
    if (left.Kind == MetadataKind.Object && right.Kind == MetadataKind.Object)
    {
        return Merge(left.AsObject(), right.AsObject(), strategy);
    }

    return right; // Scalars and arrays are replaced wholesale.
}
```

Key points:

- Builders guarantee determinism by keeping insertion order stable.
- Recursive merge only applies to object/object collisions; scalars and arrays follow strategy rules directly.
- Helper `MetadataObjectBuilder.From` copies the existing object lazily (e.g., rent buffer, copy once) to avoid repeated
  allocations.

## Validation & Testing

- Property-based tests to ensure round-tripping between metadata and JSON / protobuf retains structure.
- Benchmarks comparing:
    - Old `Dictionary<string, object?>` approach vs new structs for creation, serialization, and enumeration.
- Tests for boundary cases (large arrays, deeply nested objects, invalid value attempts).
- Use the dedicated benchmark project at `./benchmarks/Benchmarks/` to capture before/after numbers for each struct
  layout/merge optimization.
- Add targeted benchmarks for:
    - MetadataObject lookup (small vs large with lazy dictionary).
    - Merge strategies (replacement vs preserve) on varying sizes.
    - Builder scope usage to ensure pooling doesn’t regress.

## Struct Layout Considerations

- Default layout (auto) may introduce padding between discriminator and payload fields; measure the actual size via
  `Unsafe.SizeOf<MetadataValue>()`.
- Target size for `MetadataValue` is ≤24 bytes; exceeding this threshold should trigger layout experiments.
- With the `Kind + Payload` model, we expect the struct to be two fields (kind enum, payload struct). Verify copy cost
  in
  benchmarks.
- `StructLayout(LayoutKind.Explicit)` can shrink the struct by overlaying fields, but:
    - Requires careful alignment to avoid unaligned access penalties on some architectures.
    - Can complicate `readonly` semantics; prefer to keep fields `readonly` but remember explicit layout disallows
      auto-init.
- Recommended approach:
    1. Start with `LayoutKind.Sequential` and order fields from largest to smallest (e.g., union storage first, then
       `MetadataKind`/flags).
    2. If size exceeds desired threshold (e.g., >24 bytes), experiment with `LayoutKind.Explicit`. Provide benchmarks
       comparing copy-by-value throughput before adopting.
    3. Consider splitting payload storage into `MetadataPrimitive` (fits in 16 bytes) and reference payload (
       arrays/objects) to avoid always carrying reference fields.
- Any layout decision should remain CLS-safe and avoid `unsafe` code in public API, keeping the copy-by-value story
  friendly for callers.

## Protobuf Schema Strategy

- Current plan emits metadata via `google.protobuf.Struct` / `Value` for simplicity, but this structure blurs int64 vs
  double semantics and lacks a native decimal representation.
- Shipping a custom schema (e.g., `message MetadataValue { oneof kind { int64 i64 = 1; double f64 = 2; string str = 3;
  MetadataObject obj = 4; MetadataArray arr = 5; bool boolean = 6; google.protobuf.NullValue null = 7; string decimal =
  8; } }`) would:
    - Preserve numeric intent (distinguish int64 from double, provide dedicated decimal slot).
    - Enable deterministic binary formats without lossy conversions.
    - Offer forward-compatibility for additional kinds (e.g., bytes) without overloading Struct.
- Cons to weigh:
    - Requires consumers to depend on Light.Results protobuf contracts (less plug-and-play with tooling that expects
      Struct/Value).
    - Slightly more work to generate and version the proto artefacts.
    - Increases maintenance burden if transports need both Struct compatibility and custom schema support.
- Recommendation: keep the Struct/Value adapter for quick interoperability, but design the code so a custom schema can
  be introduced later without breaking callers. Evaluate demand/interop requirements before investing.
