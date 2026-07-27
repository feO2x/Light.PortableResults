# Typed Metadata Kinds

## Rationale

`MetadataKind` covers only the JSON-shaped types. Every other .NET value reaches metadata through the `IFormattable` fallback in `BuiltInValidationErrorDefinitions.CreateMetadataValue`, producing non-interoperable text: a `DateTime` boundary emits `07/26/2026 13:45:30` instead of RFC 3339, a `TimeSpan` emits `00:00:05` instead of an ISO 8601 duration, and a `TimeOnly` silently drops seconds. These values appear in `problem+json` bodies, so the published OpenAPI contract disagrees with the runtime payload. In-process consumers are equally underserved: a `Guid` or `DateTimeOffset` stored as text costs an allocation on write and a parse on every read, in a library whose primary claim is low allocation.

This plan extends the primitive range that `0052` reserved with kinds for the common scalar BCL types, stored natively. It restructures the dispatch sites so that adding a kind is compiler-checked instead of silently corrupting data, and adds an opt-in JSON envelope that makes every kind round-trippable. It supersedes the withdrawn `MetadataFormat` design: one discriminator, no second axis.

## Acceptance Criteria

- [ ] `MetadataKind` declares `UInt64`, `Float32`, `Char`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, `Uri`, and `Bytes` as values 6–16; `Array` and `Object` keep 200 and 201; the existing test enumerating the enum asserts every member's primitive/complex classification.
- [ ] `Unsafe.SizeOf<MetadataValue>()` remains 24.
- [ ] Each new kind has a factory method, an implicit conversion where the BCL type allows it, and a typed `TryGet*` accessor that returns the exact stored value without text parsing when the kind matches.
- [ ] Each `TryGet*` accessor additionally converts from `MetadataKind.String` when the string holds the kind's canonical encoding, so consumers work identically on in-process and wire-degraded values.
- [ ] Every kind serializes into JSON bodies using the canonical encoding in the vocabulary table; in particular `TimeOnly` preserves seconds, `TimeSpan` emits an ISO 8601 duration, and `0.1f` emits `0.1`.
- [ ] A whole-number `Double` or `Float32` value emits a trailing `.0` (`5.0`, not `5`), so a bare `Double` JSON token always reads back as `MetadataKind.Double` without an envelope.
- [ ] `MetadataValue` exposes its JSON shape (`Null`, `Boolean`, `Number`, `String`, `Array`, `Object`) through a public API, and one shared canonical-text formatter backs JSON string values, HTTP header values, and CloudEvents attribute strings.
- [ ] The default HTTP header conversion emits unquoted canonical text for all primitive kinds, including plain strings.
- [ ] A metadata value of any primitive kind resolves correctly as a CloudEvents core string attribute; the special-cased decimal branch in `GetStringAttribute` is replaced by the shared formatter.
- [ ] `MetadataValueAnnotationHelper.WithAnnotation` rewrites primitive values without per-kind dispatch via a payload-preserving constructor; annotation constraints for arrays and objects are still enforced.
- [ ] Every `switch` over `MetadataKind` inside `MetadataValue` lists all members without a discard arm, so declaring a new member fails the Release build (CS8509 with `TreatWarningsAsErrors`) until every switch is updated.
- [ ] Equality and hashing cover all kinds: values of different kinds are never equal, boxed kinds compare by value, `Annotation` stays excluded, and a test stores every kind in a `MetadataObject` and reads it back by key.
- [ ] With default options, serialized output for all previously existing kinds is byte-identical to today, apart from the trailing-`.0` change for whole-number doubles; the envelope is opt-in on both the write and the read side, and a reader never interprets an envelope unless enabled.
- [ ] With the envelope enabled, a write/read cycle reproduces the kind and value of every metadata value, asserted by a matrix test over all kinds; the documented exception is `MetadataKind.DateTime`, which reads back as `DateTimeOffset` with the same instant.
- [ ] `CreateMetadataValue` routes `ulong`, `float`, `char`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, `Uri`, and `byte[]` to the typed factories; the `problem+json` OpenAPI conformance test from `0052` passes unchanged, and an equivalent test covers a `DateTime` or `TimeSpan` boundary.
- [ ] Generated OpenAPI schemas and examples match the runtime encodings: `TimeSpan` maps to `format: duration`, and example values for the new kinds render as their canonical text.
- [ ] Validation error message text formats these types with the same canonical encodings as the metadata.
- [ ] Test code coverage stays above 95%.

## Technical Details

### One discriminator

`MetadataKind` remains the single discriminator: it determines the live payload slot, the JSON shape, and the canonical encoding. The `0052` layout is kept as merged — `Decimal = 5`, reserved range 6–199, `Array = 200`, `Object = 201`, `IsPrimitive` as a single comparison. The new kinds fill the reserved range in declaration order starting at 6. Semantic tags on strings (a "uuid-formatted string") are deliberately not expressible: parse at the boundary into the typed kind instead.

### Vocabulary

| Kind | .NET type | Storage | JSON encoding | Envelope name |
| --- | --- | --- | --- | --- |
| `UInt64` = 6 | `ulong` | inline | decimal digits as JSON **string** | `uint64` |
| `Float32` = 7 | `float` | inline, normalized `double` | shortest round-trippable number | `float` |
| `Char` = 8 | `char` | inline | single-character string | `char` |
| `DateTime` = 9 | `DateTime` | inline UTC ticks | RFC 3339 date-time, `Z` suffix | `date-time` |
| `DateTimeOffset` = 10 | `DateTimeOffset` | boxed | RFC 3339 date-time with offset | `date-time` |
| `DateOnly` = 11 | `DateOnly` | inline day number | RFC 3339 full-date | `date` |
| `TimeOnly` = 12 | `TimeOnly` | inline ticks | RFC 3339 full-time | `time` |
| `TimeSpan` = 13 | `TimeSpan` | inline ticks | ISO 8601 duration | `duration` |
| `Guid` = 14 | `Guid` | boxed | RFC 4122 lowercase string | `uuid` |
| `Uri` = 15 | `Uri` | reference | `OriginalString` | `uri` / `uri-reference` by `IsAbsoluteUri` |
| `Bytes` = 16 | `byte[]` | reference, no defensive copy | base64 string (RFC 4648) | `byte` |

Envelope names come from the OpenAPI Format Registry. Notes:

- `ulong` is written as a JSON string because values above `long.MaxValue` are silently corrupted by common JSON consumers.
- `Float32` stores `double.Parse(value.ToString("R", InvariantCulture))` at construction, so the shortest representation of the stored double equals the float's; a plain `(double)` cast would emit `0.10000000149011612`.
- `FromDateTime` converts `Local` to UTC and throws for `DateTimeKind.Unspecified`.
- `Bytes` documents that the array is held by reference; mutation after construction is the caller's responsibility.
- `DateTimeOffset` boxes like `Decimal` (see `MetadataValue.FromDecimal` for the rationale); packing its offset into the struct's 3 spare padding bytes is a possible later optimization, out of scope here.
- The existing `Double` kind gains a canonical rule shared with `Float32`: when the shortest round-trippable text contains neither a fraction nor an exponent, `.0` is appended (via an integral check and `WriteRawValue`). A bare `Double` token consequently never reads back as `Int64`, which removes `Double` from the envelope entirely. `Decimal` is deliberately excluded from this rule: `5m` has scale 0 and must keep emitting `5`.

The core project multi-targets `netstandard2.0;net10.0`. All kinds, wire behavior, and equality are identical on both targets; only the `DateOnly`/`TimeOnly` factory and accessor signatures are `net10.0`-only, because Polyfill's types are internal and cannot appear in public APIs. On `netstandard2.0`, envelope reads of `date`/`time` still produce the correct kinds from the inline payload.

### Dispatch safety

`0052` documented that adding a kind breaks nothing at compile time and corrupts silently. This plan removes that hazard structurally:

- **Derived shape:** `GetJsonShape()` is backed by a 256-entry `ReadOnlySpan<byte>` lookup (a byte index is bounds-check-free). The JSON writers switch on the six shapes; the `Number` arm dispatches over `Int64`/`Double`/`Float32`/`Decimal` only.
- **One formatter:** a single canonical-text method (with a `TryFormat`-style span overload) is the only place that knows how string-shaped kinds render. JSON string values, `DefaultHttpHeaderConversionService`'s fallback (currently `ToString()`, which wrongly quotes strings), and `GetStringAttribute` all call it. Adding a kind touches this one site instead of eight. `ToString()` becomes debug-only output.
- **Payload-preserving constructor:** `WithAnnotation`'s per-kind switch exists only because no constructor copies an existing payload. An internal `MetadataValue(Kind, payload, annotation)` path reduces the primitive case to one line; `Array`/`Object` still recurse to rewrite children and revalidate annotation constraints.
- **Exhaustive switches:** the remaining kind switches (`Equals`, `GetHashCode`, `ToString`, the formatter) become switch expressions listing every member with no discard arm.

### Accessors and equality

Exact-kind reads never parse. Lenient conversions mirror the existing `TryGetDecimal` precedent: every new `TryGet*` also accepts a `String` whose content is the canonical encoding, and `TryGetDouble` converts from `Float32`. This is the pressure valve for wire degradation: code calling `TryGetGuid` works whether the value arrived typed or as a bare string.

Equality stays strict — kind plus payload, `Annotation` excluded. `FromGuid(g)` is not equal to `FromString(g.ToString())`; round-trip fidelity is the envelope's job, not `Equals`'. Boxed kinds unbox and compare by value with the same defensive `is` pattern as the `Decimal` arm.

### Round-trip envelope

Off by default; enabled through a setting on the existing write and read options (HTTP, CloudEvents, shared serialization), threaded to the readers/writers by turning the parameterless metadata converters into configured instances and adding an options parameter to the static `Read*`/`Write*` methods. The module-level default `JsonSerializerOptions` keep the envelope disabled; enabling requires building serializer options through the module with the setting.

```json
{ "$format": "uuid", "$value": "dd6a721c-7438-4755-bf60-1960fae12dcd" }
```

- A value is wrapped only when reading its bare token back would not reproduce its kind. This is static per kind: `Null`, `Boolean`, `Int64`, `Double`, `String`, `Array`, and `Object` are never wrapped (the trailing-`.0` rule guarantees this for `Double`); every kind in the vocabulary table always is.
- Array elements wrap individually. The `$` prefix minimizes collisions with user data; an object is an envelope only when reading is enabled and it has exactly a `$format` string member and a `$value` member. A genuine two-member `MetadataObject` with these keys cannot round-trip while the envelope is enabled — a documented limitation.
- An unknown `$format` name reads as the bare `$value` token (forward compatibility); a known name whose value does not parse throws `JsonException`.
- `date-time` always reads as `DateTimeOffset`, which is why `MetadataKind.DateTime` round-trips with kind degradation but exact instant.
- The envelope never applies to CloudEvents extension attributes (the spec requires plain JSON types) or HTTP headers; those write paths always pass the disabled mode regardless of options.

Bare tokens are never sniffed: without an envelope, the reader keeps producing exactly the base kinds it produces today.

### Affected components

| Site | Change |
| --- | --- |
| `MetadataValue`, `MetadataPayload` | new factories, accessors, equality/hash/ToString arms, shape API, payload-preserving constructor |
| `SharedJsonSerialization` writers/readers | shape-driven dispatch, formatter, envelope behind options |
| `Http` and `CloudEvents` converters and options | configured converter instances, envelope setting, header fallback via formatter |
| `CloudEventsResultExtensions.GetStringAttribute`, `MetadataValueAnnotationHelper` | formatter / payload-preserving constructor |
| `BuiltInValidationErrorDefinitions.CreateMetadataValue`, `ValidationErrorMessageFormatting` | typed routing, canonical message text |
| `PortableOpenApiSchemaTypeMapper`, `PortableResultsOpenApiDocumentTransformer`, source-gen emitter | `TimeSpan` → `duration`, canonical example values |

### Breaking changes

Pre-1.0, silent at compile time, to be listed in release notes: values previously flattened to strings by `CreateMetadataValue` now carry typed kinds, so `TryGetString` returns `false` for them and their serialized text changes (`DateTime`, `TimeSpan`, `TimeOnly`, `float`); whole-number doubles serialize as `5.0` instead of `5`; default header serialization of strings loses the surrounding quotes; the core package gains a `net10.0` target. The `0052` enum numbering is unchanged.

### Sequencing

Issues #51 (HTTP header formatting) and #53 (CloudEvents extension attribute types) land after this plan: both become lookups from `MetadataKind` (structured-field formats, CloudEvents `Timestamp`/`Binary`/`URI` types) instead of independent classifications.

### Out of scope

Packing `DateTimeOffset` into the padding bytes, a JSON-equivalence comparer for cross-serialization comparisons, additional scalar kinds (`Half`, `Int128`), and gRPC mappings.
