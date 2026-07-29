# Typed Metadata Kinds

> **AMENDED** after implementation, during the review of branch `54-metadatakind-restructuring`. Two decisions
> below were reversed because they specified behavior that turned out to be wrong. Remaining review findings
> are not yet reflected here.
>
> **`DateTime` and `DateTimeKind.Unspecified`.** `FromDateTime` originally threw for `Unspecified`. Combined
> with the criterion routing the ten BCL types to the typed factories, that turned the kind of every
> `new DateTime(...)` literal into an exception in `CreateMetadataValue`, `ValidationErrorMessageFormatting`,
> and OpenAPI example generation - a validation comparison against an `Unspecified` boundary threw instead of
> producing a validation error. Changed: the `DateTime` row of the vocabulary table, its notes in Vocabulary,
> the `TryGetDateTime` strictness rule in Accessors and equality, and one added acceptance criterion. The
> reversal and the RFC 3339 trade-off it carries are argued in the Vocabulary notes; read those before
> re-tightening the factory.
>
> **CloudEvents core string attributes and `Null`.** The criterion below originally required a value of *any*
> primitive kind to resolve as a core string attribute. `Null` is a primitive kind whose canonical text is
> `"null"`, so the letter of that criterion made an explicitly null attribute resolve to the four-character
> string - a `source` of `"null"` passed the required-attribute check and shipped an invalid event. The
> criterion now excludes `Null`, restoring the pre-plan behavior of treating it as absent.

## Rationale

`MetadataKind` covers only the JSON-shaped types. Every other .NET value reaches metadata through the `IFormattable` fallback in `BuiltInValidationErrorDefinitions.CreateMetadataValue`, producing non-interoperable text: a `DateTime` boundary emits `07/26/2026 13:45:30` instead of RFC 3339, a `TimeSpan` emits `00:00:05` instead of an ISO 8601 duration, and a `TimeOnly` silently drops seconds. These values appear in `problem+json` bodies, so the published OpenAPI contract disagrees with the runtime payload. In-process consumers are equally underserved: a `Guid` or `DateTimeOffset` stored as text costs an allocation on write and a parse on every read, in a library whose primary claim is low allocation.

This plan extends the primitive range that `0052` reserved with kinds for the common scalar BCL types, stored natively, and restructures the dispatch sites so that adding a kind is compiler-checked instead of silently corrupting data. `0054-1` follows with an opt-in JSON envelope that makes every kind round-trippable.

## Acceptance Criteria

- [x] `MetadataKind` declares the kinds of the vocabulary table as values 6–15; `Array` and `Object` keep 200 and 201; the expected classifications in the existing enum test are updated.
- [x] `Unsafe.SizeOf<MetadataValue>()` remains 24.
- [x] Each kind has a factory method, an implicit conversion where the BCL type allows one, and a typed `TryGet*` accessor that returns the exact stored value without text parsing. `ulong.MaxValue` survives construction, serialization, and reading without precision loss.
- [x] Each `TryGet*` additionally converts from `MetadataKind.String` when the string holds the kind's canonical encoding and rejects any other text, so consumers work identically on in-process and wire-degraded values without accepting input the writers never produce.
- [x] Every kind serializes into JSON bodies using the encoding in the vocabulary table; in particular `TimeOnly` preserves seconds, `TimeSpan` emits an ISO 8601 duration, `0.1f` emits `0.1`, and a whole-number `Double` or `Single` emits a trailing `.0` (`5.0`, not `5`).
- [x] Generated OpenAPI schemas and examples match the JSON encoding of every kind, asserted against the vocabulary table: `ulong` is a string, `TimeSpan` is `format: duration` rather than `time`, and `char` and `Uri` no longer degrade to a schema without a type.
- [x] HTTP header values, CloudEvents core string attributes, and validation error message text use the same canonical encodings: header output is unquoted for every primitive kind including plain strings, and a value of any primitive kind except `Null` resolves as a core string attribute. `Null` resolves to no attribute at all, as it did before this plan.
- [x] The JSON shape of a kind (`Null`, `Boolean`, `Number`, `String`, `Array`, `Object`) is publicly derivable from `MetadataKind` alone.
- [x] Every `switch` over `MetadataKind` inside `MetadataValue` lists all members without a discard arm, so declaring a new member fails the Release build (CS8509 with `TreatWarningsAsErrors`) until every switch is updated.
- [x] `MetadataValueAnnotationHelper.WithAnnotation` preserves the value of every kind; annotation constraints for arrays and objects are still enforced.
- [x] Equality and hashing cover all kinds: values of different kinds are never equal, boxed kinds compare by value, `Uri` values compare by ordinal `OriginalString`, `Annotation` stays excluded, and a test stores every kind in a `MetadataObject` and reads it back by key.
- [x] Serialized output for all previously existing kinds is byte-identical to today, apart from the trailing `.0` for whole-number doubles.
- [x] `CreateMetadataValue` routes the ten BCL types of the vocabulary table to the typed factories; the `problem+json` OpenAPI conformance test from `0052` passes unchanged, and an equivalent test covers a `DateTime` or `TimeSpan` boundary.
- [x] A `DateTime` of any `DateTimeKind` reaches metadata without throwing, keeps its kind through storage and reading, and renders through one encoding at every site. A validation comparison against a `DateTimeKind.Unspecified` boundary produces a validation error, never an exception.
- [x] The core project builds for `netstandard2.0` and `net10.0` in Release with warnings as errors, and the only public API difference between the targets is the `DateOnly`/`TimeOnly` factories and accessors.
- [x] Test code coverage stays above 95%.

## Technical Details

### One discriminator

`MetadataKind` determines the live payload slot, the JSON shape, and the canonical encoding; nothing else discriminates. The new kinds fill the range that `0052` reserved, in declaration order from 6. Semantic tags on strings (a "uuid-formatted string") are deliberately not expressible: parse at the boundary into the typed kind instead.

Members are named after their `System.*` type, as the existing ones are (`Int64`, not C#'s `long`), hence `Single` rather than `Float32`, and `Double` is not renamed; factories and accessors follow the member (`FromSingle`, `TryGetSingle`), matching the `TypeCode` arms that `CreateMetadataValue` already dispatches on. Width-explicit naming is not available to the whole family because `0052` rejected `decimal128` as misleading for `System.Decimal`.

### Vocabulary

| Kind | .NET type | Storage | JSON encoding | OpenAPI schema |
| --- | --- | --- | --- | --- |
| `UInt64` = 6 | `ulong` | inline | decimal digits as JSON **string** | `string`, `uint64` |
| `Single` = 7 | `float` | inline, widened `double` | shortest round-trippable number | `number`, `float` |
| `Char` = 8 | `char` | inline | single-character string | `string`, `char` |
| `DateTime` = 9 | `DateTime` | inline `ToBinary()` | RFC 3339 date-time with `Z`, or ISO 8601 local time without a designator when the kind is `Unspecified` | `string`, `date-time` |
| `DateTimeOffset` = 10 | `DateTimeOffset` | boxed | RFC 3339 date-time with offset | `string`, `date-time` |
| `DateOnly` = 11 | `DateOnly` | inline day number | RFC 3339 full-date | `string`, `date` |
| `TimeOnly` = 12 | `TimeOnly` | inline ticks | RFC 3339 full-time | `string`, `time` |
| `TimeSpan` = 13 | `TimeSpan` | inline ticks | ISO 8601 duration | `string`, `duration` |
| `Guid` = 14 | `Guid` | boxed | RFC 4122 lowercase string | `string`, `uuid` |
| `Uri` = 15 | `Uri` | reference | `OriginalString` | `string`, `uri-reference` |

The schema column is normative: `PortableOpenApiSchemaTypeMapper` and the source-generation emitter transcribe this table instead of maintaining a parallel one, and a test asserts them against it. The table also corrects two existing mappings — `TimeSpan` currently shares the `time` row with `TimeOnly`, and `float` carries no format — and adds `char` and `Uri`, which fall through to a typeless schema today. Every format name used here is registered in the [OpenAPI Format Registry](https://spec.openapis.org/registry/format/). Notes:

- `ulong` is written as a JSON string because values above `long.MaxValue` are silently corrupted by common JSON consumers. The registry sanctions this: `uint64` lists `number, string` as its base types and recommends the string form above the 53-bit range. We always emit a string, so the schema does not depend on the value.
- `Uri` maps to `uri-reference` rather than `uri` because the schema is per CLR type while absoluteness is a per-value property.
- `Single` is widened with a plain `(double)` cast at construction and narrowed back with `(float)` before formatting; the float → double → float round trip is lossless, so `0.1f` still emits `0.1` at no construction cost. The consequence is that `TryGetDouble` on a `Single` yields the widened value (`0.10000000149011612` for `0.1f`), not the double nearest `0.1`.
- The existing `Double` kind gains a canonical rule shared with `Single`: when the shortest round-trippable text contains neither a fraction nor an exponent, `.0` is appended (via an integral check and `WriteRawValue`), so a bare `Double` token never reads back as `Int64`. `Decimal` is deliberately excluded from this rule: `5m` has scale 0 and must keep emitting `5`.
- `DateTime` and `DateTimeOffset` are separate kinds because the common UTC case then stores inline and avoids the boxing an offset requires. `FromDateTime` converts `Local` to UTC — a local wall clock is meaningless once it leaves the process — and accepts `DateTimeKind.Unspecified` as it is. Rejecting `Unspecified` is not an option: it is what every `new DateTime(...)` literal and every zone-less `DateTime.Parse` produces, so it is the common case for a validation boundary, and throwing turns a validation failure into an exception in `CreateMetadataValue`, `ValidationErrorMessageFormatting`, and OpenAPI example generation alike.
- Because the kind must survive, the payload stores `DateTime.ToBinary()` rather than raw ticks: it packs the kind into the two spare high bits of the tick count, in the same `Int64` slot and with no loss (for `Utc` and `Unspecified` it is a bit-exact copy of the internal state). Only `Utc` and `Unspecified` are ever stored, so decoding is deterministic; `FromBinary` resolves a `Local` payload against the reading machine's time zone, so a `Local` result is treated as a corrupt payload alongside the out-of-range case.
- An `Unspecified` value renders without a designator (`2026-07-26T13:45:30`). That is valid ISO 8601 local time but **not** RFC 3339, which makes the offset mandatory, so it does not satisfy the `format: date-time` that the schema mapper emits for `System.DateTime`. This is accepted deliberately: the alternative is inventing a `Z` the caller never asserted, which is exactly the silent-wrong-data failure mode this plan exists to remove. The one encoding is used everywhere — JSON bodies, headers, CloudEvents attributes, message text, OpenAPI examples — so the document never disagrees with the payload. Steering callers toward `Utc` at API boundaries belongs in an analyzer diagnostic, not a runtime throw.
- `DateTimeOffset` boxes like `Decimal` — see `MetadataValue.FromDecimal` for the rationale. `FromUri(null)` returns `Null`, mirroring `FromString`.

### Payload storage

`MetadataPayload` stays a bit-level union and gains no typed views for the date and time kinds: those are stored as ticks or a day number through the existing `Int64` slot, and `MetadataValue` owns the conversion. Overlaying them at offset 0 would save only a mask and a range check on paths dominated by dictionary lookups and JSON writing, while forcing `#if` into the layout-critical struct (`DateOnly` and `TimeOnly` do not exist on `netstandard2.0`), making its layout differ per target, and introducing views whose bit patterns are not all valid — where every bit pattern is a valid `long`. Storing ticks also enforces the UTC normalization structurally instead of by documentation.

One typed view is required, and for correctness rather than speed: **`ulong` binds to the `MetadataPayload(double)` constructor**, because `ulong` has an implicit conversion to `double` but none to `long`. Every `UInt64` value above 2^53 would be silently corrupted with no diagnostic. Add a `ulong` view at offset 0 with a named factory — that view is total and carries none of the objections above. `char` is unaffected (it binds to the `long` overload), and `Single` needs no view because it is widened into the `Float64` slot.

While editing the struct, replace `record struct` with a plain `readonly struct`: nothing in the solution uses the compiler-generated `Equals`, `GetHashCode`, `==`, `with`, or `ToString`, because `MetadataValue` compares payload slots itself. Dropping `record` does not change the layout, so the size pin is unaffected. Override `Equals` and `GetHashCode` to throw `NotSupportedException` — the inherited `ValueType` versions would reflect over fields rather than compare bitwise, since the struct holds an object reference, and payload equality is meaningless without a kind in any case: identical bits mean different values under `Int64`, `Double`, and `Boolean`.

### Dispatch safety

`0052` documented that adding a kind breaks nothing at compile time and corrupts silently. This plan removes that hazard structurally:

- **Derived shape:** `GetJsonShape()` is an extension method on `MetadataKind` next to `IsPrimitive`, implemented as an exhaustive switch expression. The shape is a function of the kind alone, and the callers that need it most — the schema mapper, the header conversion service — hold a kind rather than a value; `MetadataValue` exposes a forwarding property. A lookup table is deliberately not used: it would map an unlisted kind to shape `0` silently, which is the failure mode this section exists to remove, and the switch is what the JIT turns into a jump table anyway. The JSON writers switch on the six shapes; the `Number` arm dispatches over `Int64`/`Double`/`Single`/`Decimal` only.
- **One formatter:** a single canonical-text method (with a `TryFormat`-style span overload) is the only place that knows how string-shaped kinds render. JSON string values, `DefaultHttpHeaderConversionService`'s fallback (currently `ToString()`, which wrongly quotes strings), and `GetStringAttribute` all call it, replacing the latter's special-cased decimal branch. Adding a kind touches this one site instead of eight. `ToString()` becomes debug-only output.
- **Payload-preserving constructor:** `WithAnnotation`'s per-kind switch exists only because no constructor copies an existing payload. An internal `MetadataValue(Kind, payload, annotation)` path reduces the primitive case to one line; `Array`/`Object` still recurse to rewrite children and revalidate annotation constraints.
- **Exhaustive switches:** the remaining kind switches (`Equals`, `GetHashCode`, `ToString`, the formatter) become switch expressions listing every member with no discard arm.

`MetadataValueTestFactory` keeps its value — it still guards the arms that a declared kind reaches with a payload the factories never produce — but without discard arms an undeclared kind now surfaces as `SwitchExpressionException` instead of the custom `InvalidOperationException` from `ToString()`, so those expectations change.

### Accessors and equality

Exact-kind reads never parse. Lenient conversions mirror the existing `TryGetDecimal` precedent: every new `TryGet*` also accepts a `String` whose content is the canonical encoding, and `TryGetDouble` converts from `Single`. This is the pressure valve for wire degradation: code calling `TryGetGuid` works whether the value arrived typed or as a bare string.

Strictness is part of that contract, because the framework defaults are too permissive in two places:

- `Uri` accepts `UriKind.Absolute` only. `UriKind.RelativeOrAbsolute` succeeds for nearly any text, which would make `TryGetUri` return `true` for `"hello world"` and turn the accessor into a footgun for callers that probe kinds in sequence.
- Date and time kinds parse the exact canonical format with the invariant culture and `DateTimeStyles.RoundtripKind` — never `DateTime.Parse`, which accepts `07/26/2026 13:45:30` and would resurrect inside the accessors the ambiguity this plan removes from the writers.
- `TryGetDateTime` accepts both `DateTime` encodings (with and without the `Z`) and rejects text carrying a numeric offset. That text is the `DateTimeOffset` encoding, and resolving it into a `DateTime` would make the result depend on the reading machine's time zone.

Equality stays strict — kind plus payload, `Annotation` excluded. `FromGuid(g)` is not equal to `FromString(g.ToString())`; round-trip fidelity is `0054-1`'s job, not `Equals`'. Boxed kinds unbox and compare by value with the same defensive `is` pattern as the `Decimal` arm. `Uri` is a reference rather than a boxed value and must not use `Uri.Equals`, which ignores the fragment and compares hosts case-insensitively: two URIs that serialize differently would compare equal. It compares and hashes its `OriginalString` ordinally, which is exactly what is written to the wire.

### Targets

The core project multi-targets `netstandard2.0;net10.0`. All kinds, wire behavior, and equality are identical on both; only the `DateOnly`/`TimeOnly` factory and accessor signatures are `net10.0`-only, because Polyfill's types are internal and cannot appear in public APIs. The `#if` surface is confined to those signatures: storage (day number, ticks), formatting, and parsing stay target-agnostic.

The test projects stay single-targeted at `net10.0`. Exercising the `netstandard2.0` assets would require a TFM for which they are the best match (net472 or older), doubling the CI matrix in order to cover differences that are signature-only by construction and that the Release build of both targets already catches.

### Affected components

| Site | Change |
| --- | --- |
| `MetadataValue`, `MetadataPayload` | new factories, accessors, equality/hash/ToString arms, payload-preserving constructor |
| `MetadataKindExtensions` | `GetJsonShape()` alongside `IsPrimitive` |
| `SharedJsonSerialization` writers/readers | shape-driven dispatch, canonical formatter |
| `DefaultHttpHeaderConversionService`, `CloudEventsResultExtensions.GetStringAttribute`, `MetadataValueAnnotationHelper` | formatter / payload-preserving constructor |
| `BuiltInValidationErrorDefinitions.CreateMetadataValue`, `ValidationErrorMessageFormatting` | typed routing, canonical message text |
| `PortableOpenApiSchemaTypeMapper`, `PortableResultsOpenApiDocumentTransformer`, source-gen emitter | schema column of the vocabulary table, canonical example values |

### Breaking changes

Pre-1.0, silent at compile time, to be listed in release notes: values previously flattened to strings by `CreateMetadataValue` now carry typed kinds, so `TryGetString` returns `false` for them and their serialized text changes (`DateTime`, `TimeSpan`, `TimeOnly`, `float`); whole-number doubles serialize as `5.0` instead of `5`; default header serialization of strings loses the surrounding quotes; `TimeSpan` schemas change from `format: time` to `format: duration`; the core package gains a `net10.0` target. The `0052` enum numbering is unchanged.

### Sequencing

`0054-1` adds the round-trip envelope on top of this plan and depends on the vocabulary and the shape API. Issues #51 (HTTP header formatting) and #53 (CloudEvents extension attribute types) land afterwards: both become lookups from `MetadataKind` (structured-field formats, CloudEvents `Timestamp`/`URI` types) instead of independent classifications. #53 also carries the deferred `Bytes` kind, since CloudEvents `Binary` is the first concrete consumer of it.

### Out of scope

Packing `DateTimeOffset` into the struct's three spare padding bytes, a JSON-equivalence comparer for cross-serialization comparisons, additional scalar kinds (`Half`, `Int128`), gRPC mappings, and any user-configurable override of the kind-to-shape or kind-to-schema mapping.

A `Bytes` kind for `byte[]` (OpenAPI `format: byte`, base64 on the wire) is deliberately excluded. It is the only candidate that is not a fixed-size immutable value type, and it raises two questions the scalar kinds do not: whether equality is structural (O(n) comparison and hashing on values used as `MetadataObject` entries) or by reference (inconsistent with every other kind), and whether the factory copies defensively to preserve immutability. Binary data stays a base64 `String` for now, which is what callers do today. Appending the kind later is additive and not wire-visible, since the enum's numeric values never leave the process.
