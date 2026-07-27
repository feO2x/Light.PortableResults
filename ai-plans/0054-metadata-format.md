# Metadata Format Discriminator

## Rationale

`MetadataKind` is a JSON-shaped discriminator with six members. Every .NET type that is not one of them is flattened into `String` by the `IFormattable` fallback in `BuiltInValidationErrorDefinitions.CreateMetadataValue`, and the resulting text is not interoperable: a `DateTime` boundary emits `07/26/2026 13:45:30` rather than RFC 3339, a `TimeSpan` emits `00:00:05` rather than an ISO 8601 duration, and a `TimeOnly` emits `13:45` — silently dropping seconds. These values appear in `problem+json` bodies produced by comparison and range rules, so the published contract disagrees with the payload in the same way `0052` documented for decimals, and a non-.NET client cannot parse them at all.

The type information is also unavailable in-process. Generic caller code — a reducer, a log enricher, a mapper onto another metadata system — can only switch on `MetadataKind` and cannot tell a UUID from an arbitrary string. This plan introduces `MetadataFormat`, an orthogonal refinement aligned with the OpenAPI Format Registry, plus an opt-in wire envelope that makes metadata round-trippable across technology stacks.

## Acceptance Criteria

- [ ] `MetadataValue` exposes a `MetadataFormat` property, and `Unsafe.SizeOf<MetadataValue>()` remains `24`.
- [ ] `MetadataKind.Decimal` is removed; `Array` and `Object` return to `5` and `6`, and every member is asserted to be classified correctly as primitive or complex by a test enumerating the enum.
- [ ] A `decimal` metadata value has `Kind == MetadataKind.Double` and `Format == MetadataFormat.Decimal`, and still serializes into JSON bodies as an unquoted number preserving all significant digits and the original scale.
- [ ] The `problem+json` conformance test introduced by `0052` for decimal comparison and range rules still passes unchanged.
- [ ] `MetadataValue` accepts `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `ulong`, `float`, `char`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, `Uri`, and `byte[]`, each carrying the format listed in the vocabulary table.
- [ ] Date, time, duration, UUID, URI, and binary values serialize using their registered canonical encodings; in particular `TimeOnly` preserves seconds and `TimeSpan` emits an ISO 8601 duration.
- [ ] A `float` metadata value serializes using its shortest round-trippable representation, so `0.1f` emits `0.1` rather than `0.10000000149011612`.
- [ ] Each `MetadataFormat` member maps to its OpenAPI Format Registry name, asserted by a test covering every declared member.
- [ ] Generated OpenAPI schemas carry the matching `format` keyword for metadata values whose format is known at documentation time.
- [ ] Illegal kind/format pairings cannot be constructed through the public API.
- [ ] Round-trip envelopes are **off by default**: with the default options the serialized bytes for every metadata value that existed before this plan are unchanged, apart from the encoding fixes above.
- [ ] With round-tripping enabled, a value is enveloped only when reading the bare JSON token back would not reproduce its `(Kind, Format)` pair; `null`, booleans, unformatted strings, arrays, and objects are never enveloped.
- [ ] With round-tripping enabled, every `(Kind, Format)` pair survives a write/read cycle intact, asserted by a matrix test over all pairs.
- [ ] The reader never infers a format from an unenveloped token, and never interprets an envelope unless round-tripping is enabled.
- [ ] Test code coverage stays above 95%.

## Technical Details

### Layout

`MetadataFormat` is a `byte` enum stored in the padding that already exists between `MetadataValue.Kind` (1 byte) and `MetadataValue.Annotation` (a 4-byte `int` enum). The struct stays at 24 bytes, so `MetadataArrayData`'s inline `MetadataValue[]` does not grow. `None = 0` keeps `default(MetadataValue)` a `(Null, None)` value.

### The kind/format contract

`MetadataKind` selects the JSON shape and therefore the writer arm. `MetadataFormat` refines the meaning of a value **within** that shape. The general rule is that a format must not change the lexical form the writer produces, which is what allows formats to be added later without touching any dispatch site.

`MetadataFormat.Decimal` is the one deliberate exception. A `decimal` is stored boxed in `MetadataPayload.Reference` under `MetadataKind.Double`, so for that kind alone the live payload slot is determined by `(Kind, Format)` rather than by `Kind`. Two consequences must be handled explicitly, because neither produces a compiler diagnostic:

- Any site that reads `_payload.Float64` for `Kind.Double` without checking `Format` reads the zeroed overlapping slot and silently yields `0.0`.
- Any writer arm that calls the `double` overload for `Kind.Double` truncates the value and loses scale, regressing `0052`.

To keep that rule in one place rather than repeated across dispatch sites, `MetadataValue` exposes the numeric shape and the writers branch on it instead of re-deriving the condition:

```csharp
public enum MetadataNumberShape : byte { Int64, Double, Decimal }

public MetadataNumberShape GetNumberShape();
```

`TryGetDouble` returns a lossy `(double)` conversion for a decimal-formatted value rather than `false`, so numeric consumers keep working; `TryGetDecimal` returns the exact value.

### Format vocabulary

Names come from the OpenAPI Format Registry (`spec.openapis.org/registry/format/`), which layers on the JSON Schema `format` keyword. Nothing is invented, and the vocabulary is language-neutral. Note that `format` is annotation-only in JSON Schema unless assertion is explicitly enabled, so adopting it commits the library to no validation semantics.

| .NET type | Kind | Format | Registry name | Encoding |
| --- | --- | --- | --- | --- |
| `sbyte` / `short` / `int` | `Int64` | `Int8` / `Int16` / `Int32` | `int8` / `int16` / `int32` | JSON number |
| `byte` / `ushort` / `uint` | `Int64` | `UInt8` / `UInt16` / `UInt32` | `uint8` / `uint16` / `uint32` | JSON number |
| `ulong` | `String` | `UInt64` | `uint64` | decimal text (registered as number-or-string; string avoids the `long` overflow that the current `FromString` fallback already works around) |
| `float` | `Double` | `Float` | `float` | shortest round-trippable text, normalised at construction |
| `decimal` | `Double` | `Decimal` | `decimal` | JSON number, scale preserved |
| `DateTimeOffset` / `DateTime` | `String` | `DateTime` | `date-time` | RFC 3339 |
| `DateOnly` | `String` | `Date` | `date` | RFC 3339 full-date |
| `TimeOnly` | `String` | `Time` | `time` | RFC 3339 full-time |
| `TimeSpan` | `String` | `Duration` | `duration` | RFC 3339 duration |
| `Guid` | `String` | `Uuid` | `uuid` | RFC 4122, lowercase canonical |
| `Uri` | `String` | `Uri` / `UriReference` | `uri` / `uri-reference` | RFC 3986 |
| `byte[]` | `String` | `Byte` | `byte` | base64, RFC 4648 |
| `char` | `String` | `Char` | `char` | single character |

`decimal128` is **not** used: it denotes IEEE 754-2008 decimal128 with 34 significant digits, whereas `System.Decimal` has a 96-bit mantissa and a 0–28 scale. Labelling it `decimal128` would mislead exactly the cross-stack consumers this feature serves.

A `DateTime` is normalised to UTC on construction. A `Local` value is meaningless once it leaves the process, and `DateTimeOffset` is the type to reach for when the offset matters.

Every string-shaped format stores its already-canonical text in `Reference`, so `Reference is string` holds for all `Kind.String` values and no existing site changes. Packing ticks inline is a later optimisation and is out of scope; the current code already allocates a string for these types.

Formats are reachable only through the typed factory methods and conversion operators, so pairs such as `(Int64, Uuid)` are unconstructible. Any general-purpose factory must validate the pairing.

### Equality

`Format` participates in `Equals` and `GetHashCode`. Excluding it would make values that serialize differently under the envelope compare equal, and the decimal arm has to branch on it regardless. This is a behaviour change: `FromInt32(5)` and `FromInt64(5)` are no longer equal, and a UUID-formatted string is no longer equal to the identical plain string.

### Round-trip envelope

Disabled by default, enabled through the existing write and read options (following the precedent of `ValidationProblemSerializationFormat` and `HeaderValueParsingMode`). When enabled, a value is wrapped only if the reader's default inference would not reproduce its `(Kind, Format)` pair:

```json
{
  "trace-id": { "format": "uuid", "value": "dd6a721c-7438-4755-bf60-1960fae12dcd" },
  "scopes": ["profile", "email"],
  "price": { "format": "decimal", "value": 19.50 }
}
```

`null`, booleans, unformatted strings, arrays, and objects are never wrapped, because their token already determines the pair. Integral numbers read back as `Int64` and fractional numbers as `Double`, so only numeric formats that deviate from those defaults are wrapped. Array elements are wrapped individually. The discriminator key is `format`, not `type`: RFC 9457 reserves `type` for the problem-type URI at the top level of the same document, and reusing it with different semantics one level down is ambiguous.

The envelope is deliberately not the default. The generated OpenAPI document already carries `format` out of band for HTTP consumers, and wrapping would replace precise per-member schemas with `{ format: string, value: any }` — degrading the artifact the OpenAPI packages exist to produce — while multiplying payload size. Self-description earns its keep where no schema travels with the message: asynchronous messaging, CloudEvents `data`, and cross-runtime boundaries.

### Read side

The reader never infers a format from a bare token. Sniffing would make the resulting format depend on the content of the value, which `0052` rejected for numeric tokens, and would misclassify strings that merely look like timestamps. The envelope is the explicit override of that rule rather than a parallel mechanism.

Envelope recognition is gated on the opt-in read mode. Without the gate, a legitimate `MetadataObject` carrying `format` and `value` members would be silently reinterpreted as an envelope.

An unrecognised format name reads as the underlying kind with `Format.None` rather than failing, so a consumer on an older version of the library stays forward-compatible.

### Affected components

Adding `MetadataFormat` requires no change at the sites that dispatch on `MetadataKind`, with the exception of the number arms described above. Removing `MetadataKind.Decimal` does change all of them, and — as `0052` recorded — none of these fail to compile:

| Site | Change |
| --- | --- |
| `SharedJsonSerialization/Writing/MetadataExtensions.WriteMetadataValue` | drop the `Decimal` arm; the `Double` arm branches on `GetNumberShape()` |
| `MetadataValue.Equals` / `GetHashCode` / `ToString` | same, plus folding `Format` into equality and hashing |
| `CloudEvents/MetadataValueAnnotationHelper.WithAnnotation` | drop the `Decimal` arm |
| `CloudEvents/Writing/CloudEventsResultExtensions.GetStringAttribute` | the explicit `Kind == Decimal` check at line 611 becomes a format check |
| `Http/Reading/Json` and `SharedJsonSerialization/Reading` readers | envelope handling behind the opt-in mode |
| `BuiltInValidationErrorDefinitions.CreateMetadataValue` | route the new type codes to the typed factories instead of the `IFormattable` fallback |
| `PortableOpenApiSchemaTypeMapper` and the schema builders | emit `format` |

### Breaking changes

Pre-1.0, but silent at compile time for downstream callers and to be listed in the release notes:

- `MetadataKind.Decimal` no longer exists; decimals report `MetadataKind.Double`.
- `MetadataKind.Array` and `MetadataKind.Object` change from `200`/`201` back to `5`/`6`, reverting the renumbering from `0052`, which has not shipped.
- Values differing only in `Format` no longer compare equal.
- Date, time, duration, and `TimeOnly` values change their serialized text; `TimeOnly` stops losing seconds.
- `float` values change their serialized text.

The reserved primitive range documented on `MetadataKind` is removed. Its stated justification — that CloudEvents `Binary`, `URI`, `URI-reference`, and `Timestamp` would become kinds — no longer holds, since all four are formats under this design.

### Sequencing

Issue #53 (CloudEvents extension attributes written with JSON types outside the CloudEvents type system) should land after this plan: the CloudEvents type system maps onto a subset of this vocabulary — `Timestamp` to `date-time`, `Binary` to `byte`, `URI` to `uri`, `URI-reference` to `uri-reference` — so the mapping becomes a lookup rather than a second classification. Issue #51 (HTTP header value formatting) likewise gains the `sf-*` structured-field formats as its natural vocabulary.

### Out of scope

Packing date and time formats inline in the `Int64` slot. The visitor API that would turn the dispatch sites above into compile errors and give generic in-process callers a push-based accessor; that is a follow-up plan.
