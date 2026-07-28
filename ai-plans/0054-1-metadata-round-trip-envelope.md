# Metadata Round-Trip Envelope

## Rationale

After `0054-0`, every metadata kind has a canonical JSON encoding, but JSON carries no discriminator for most of them: a `Guid`, a `TimeSpan`, and a `UInt64` all arrive as `MetadataKind.String`, and a `DateTimeOffset` is indistinguishable from a timestamp someone typed by hand. A producer and a consumer that both use this library therefore cannot reconstruct what was sent. The lenient `TryGet*` conversions cover the case where the consumer already knows which type it wants; they do not help generic code that compares, re-emits, or forwards metadata, where kind loss silently changes `Equals` results and re-serialized output.

This plan adds an opt-in JSON envelope that makes the kind explicit on the wire. It stays off by default because the plain encoding is what non-.NET consumers and the OpenAPI document generated in `0054-0` expect.

## Acceptance Criteria

- [ ] The envelope is opt-in through a setting on the existing write and read options for HTTP, CloudEvents, and shared serialization; the module-level default `JsonSerializerOptions` keep it disabled.
- [ ] With the setting disabled, written output and read results are byte-identical and kind-identical to `0054-0`: no envelope is emitted, no envelope is interpreted, and no bare token is sniffed.
- [ ] With the setting enabled on both sides, a write/read cycle reproduces the kind and value of every metadata value, asserted by a matrix test over all kinds; the documented exception is `MetadataKind.DateTime`, which reads back as `DateTimeOffset` with the same instant.
- [ ] A value is wrapped only when reading its bare token back would not reproduce its kind, so `Null`, `Boolean`, `Int64`, `Double`, `String`, `Array`, and `Object` are never wrapped.
- [ ] Envelopes nest: array elements and object member values wrap individually, and values inside arrays and objects round-trip at any depth.
- [ ] Deserialization handles every envelope-shaped input without corrupting data: an unknown `$format` reads as the bare `$value` token, a known `$format` whose value does not parse throws `JsonException`, and any object that is not exactly a `$format` string member plus a `$value` member reads as a plain `MetadataObject`.
- [ ] An envelope written by a producer and read by a consumer that has the setting disabled reads as a plain `MetadataObject` with the two members, without an exception.
- [ ] CloudEvents extension attributes and HTTP headers never emit an envelope, regardless of the setting.
- [ ] On `netstandard2.0`, reading a `date` or `time` envelope produces the correct kind and payload even though no typed accessor exists on that target.
- [ ] Test code coverage stays above 95%.

## Technical Details

### Shape and format names

```json
{ "$format": "uuid", "$value": "dd6a721c-7438-4755-bf60-1960fae12dcd" }
```

`$value` always holds the canonical encoding from the `0054-0` vocabulary table, so the envelope adds a discriminator without introducing a second representation of the value.

| Kind | `$format` |
| --- | --- |
| `UInt64` | `uint64` |
| `Single` | `float` |
| `Char` | `char` |
| `DateTime`, `DateTimeOffset` | `date-time` |
| `DateOnly` | `date` |
| `TimeOnly` | `time` |
| `TimeSpan` | `duration` |
| `Guid` | `uuid` |
| `Uri` | `uri` when `IsAbsoluteUri`, otherwise `uri-reference` |

All names are registered in the [OpenAPI Format Registry](https://spec.openapis.org/registry/format/), matching the schema formats emitted in `0054-0`. `date-time` always reads back as `DateTimeOffset`, which is why `MetadataKind.DateTime` round-trips with kind degradation but an exact instant: the two kinds are distinguishable in memory only by whether an offset was supplied, and the registry has no name for that distinction.

Wrapping is static per kind, decided by whether the bare token reproduces the kind — never by inspecting the value. `Double` is not wrapped because the trailing-`.0` rule from `0054-0` already makes a bare `Double` token self-describing.

### Options and threading

A setting on the existing write and read options, threaded to the readers and writers by turning the parameterless metadata converters into configured instances and adding a trailing parameter to the static `Read*`/`Write*` methods (illustrative):

```csharp
public enum MetadataEnvelopeMode : byte { Disabled = 0, Enabled = 1 }

public static MetadataValue ReadMetadataValue(
    ref Utf8JsonReader reader,
    MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody,
    MetadataEnvelopeMode envelopeMode = MetadataEnvelopeMode.Disabled
);
```

`Disabled` as the default of both the enum and the parameter keeps every existing call site behaving as it does today. The CloudEvents extension-attribute and HTTP header write paths pass `Disabled` unconditionally rather than reading the option: the CloudEvents specification requires plain JSON types for extension attributes, and a header carries text.

### Reading

An object is an envelope only when envelope reading is enabled **and** it has exactly two members, a `$format` whose token is a string and a `$value`. Anything else — a third member, a missing `$value`, a non-string `$format` — is an ordinary `MetadataObject`, and so is any envelope-shaped object seen while the setting is disabled. That last case is the interop failure mode worth designing for explicitly: a producer with envelopes enabled talking to a consumer with them disabled degrades to two-member objects silently rather than throwing, so the setting is meant to be configured symmetrically at both ends of a link.

An unknown `$format` reads as the bare `$value` token, which keeps a reader forward-compatible with kinds added later. A known `$format` whose `$value` does not parse throws `JsonException`, because that is malformed input rather than an unknown vocabulary.

Bare tokens are never sniffed. Without an envelope the reader produces exactly the kinds it produces in `0054-0`, so enabling the setting on the read side alone cannot change how existing payloads are interpreted.

### Limitations

- A genuine `MetadataObject` with exactly the members `$format` (string) and `$value` cannot round-trip while the envelope is enabled. The `$` prefix minimizes the collision; the case is documented rather than escaped.
- With the envelope enabled, metadata in a `problem+json` body no longer matches the OpenAPI schemas generated in `0054-0`, since a scalar becomes an object. The envelope is therefore a closed-world optimization for .NET-to-.NET links, and enabling it on a publicly documented endpoint is a deliberate trade-off.

### Affected components

| Site | Change |
| --- | --- |
| `SharedJsonSerialization` writers/readers | envelope write and read behind the mode parameter |
| `Http` and `CloudEvents` writing/reading options and modules | envelope setting, configured converter instances |
| `HttpWriteMetadataValueJsonConverter` and its reading counterparts | constructor taking the mode; no parameterless registration |

### Out of scope

Envelopes for CloudEvents extension attributes or HTTP headers, a schema dialect describing the envelope shape, and any per-value or per-key opt-in — the setting is per serializer configuration.
