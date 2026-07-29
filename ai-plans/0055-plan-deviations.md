# 0055 Plan Deviations

This document compares `ai-plans/0055-metadata-restructuring.md` with the implementation of the typed metadata vocabulary on branch `54-metadatakind-restructuring`. The plan text is left as it was written, so it stays the record of what was specified; the differences are recorded here.

`ai-plans/0054-1-metadata-round-trip-envelope.md` is not part of this comparison. It is a follow-up plan that has not been implemented yet.

## Summary

The plan was implemented as specified. Every kind in the vocabulary table exists, the shape and canonical-text derivations are in place, the exhaustive switches fail the Release build when a kind is added, and the public API differs between `netstandard2.0` and `net10.0` only in the `DateOnly`/`TimeOnly` members.

Four decisions in the plan were revised during the review that followed implementation, because they specified behavior that turned out to be wrong rather than merely inconvenient. Three of them (1, 3, 4) were reached through the plan's own reasoning and are corrections to it; the fourth was a pre-existing defect the plan would have frozen into a criterion.

## Deviations From The Original Plan

### 1. `DateTime` accepts `DateTimeKind.Unspecified` and stores `ToBinary()` instead of UTC ticks

**Original plan:**
The vocabulary table specified the `DateTime` payload as `inline UTC ticks` and its encoding as "RFC 3339 date-time, `Z` suffix". The Vocabulary notes stated: "`FromDateTime` converts `Local` to UTC and throws for `DateTimeKind.Unspecified`."

**Implemented:**
`FromDateTime` converts `Local` to UTC and accepts `Unspecified` as it is. The payload stores `DateTime.ToBinary()`, which packs the kind into the two spare high bits of the tick count — the same 8-byte `Int64` slot raw ticks occupied, with no loss of precision and no growth of the struct. Only `Utc` and `Unspecified` are ever stored, so decoding is deterministic; a `Local` result from `FromBinary` is treated as a corrupt payload, alongside the out-of-range case, because `FromBinary` would otherwise resolve it against the reading machine's time zone.

An `Unspecified` value renders without a designator (`2026-07-26T13:45:30`).

**Why:**
`Unspecified` is what every `new DateTime(...)` literal and every zone-less `DateTime.Parse` produces, so it is the common case for a validation boundary rather than an edge case. Combined with the criterion routing the ten BCL types to the typed factories, throwing turned the kind of an ordinary literal into an exception in `CreateMetadataValue`, `ValidationErrorMessageFormatting`, and OpenAPI example generation alike: a validation comparison against an `Unspecified` boundary threw instead of producing a validation error, and the failure surfaced as a 500 on the documentation endpoint with no compile-time signal to the author.

**Impact:**
The suffix-less rendering is valid ISO 8601 local time but **not** RFC 3339, which makes the offset mandatory, so an `Unspecified` value does not satisfy the `format: date-time` the schema mapper emits for `System.DateTime`. This is accepted deliberately: the alternative is inventing a `Z` the caller never asserted, which is the silent-wrong-data failure mode the plan exists to remove. The one encoding is used at every site — JSON bodies, headers, CloudEvents attributes, message text, OpenAPI examples — so the document never disagrees with the payload.

Two consequences worth knowing: an `Unspecified` and a `Utc` value with the same wall clock are not equal, because the kind is part of the payload; and steering callers toward `Utc` at API boundaries is left to a future analyzer diagnostic rather than a runtime throw.

### 2. CloudEvents core string attributes treat `Null` as absent

**Original plan:**
An acceptance criterion required that "a value of any primitive kind resolves as a core string attribute."

**Implemented:**
`GetStringAttribute` resolves a value of any primitive kind **except** `Null`, which resolves to no attribute at all.

**Why:**
`Null` is a primitive kind whose canonical text is `"null"`, so the letter of the criterion made an explicitly null attribute resolve to the four-character string. A `source` of `"null"` passed the required-attribute check and shipped an invalid event; a `time` of `"null"` failed RFC 3339 parsing instead of falling back to the current timestamp.

**Impact:**
This is a defect fix, not a design change. The behavior before this plan was already to treat `Null` as absent — the pre-plan implementation walked `TryGetString → TryGetBoolean → TryGetInt64 → TryGetDouble → Kind == Decimal`, all of which miss `Null` — and the criterion would have frozen the regression that the unified canonical-text path introduced. A test asserting `(FromNull, "null")` was removed with it.

Note that `DefaultHttpHeaderConversionService` still emits `"null"` for `MetadataValue.Null`. That is pre-existing behavior tracked separately in #51 and was deliberately left alone here.

### 3. `TryGetDateTimeOffset` accepts both zero-offset designators

**Original plan:**
The Accessors section specified that each `TryGet*` "converts from `MetadataKind.String` when the string holds the kind's canonical encoding and rejects any other text", where the canonical encoding for `DateTimeOffset` is the offset form its own writer produces (`+00:00` for a zero offset).

**Implemented:**
`TryGetDateTimeOffset` additionally accepts the `Z` designator for a zero offset, although the writer never produces it. Text carrying no designator at all stays rejected.

**Why:**
Strictly matching the writer defeats the purpose of the lenient string path. `Z` is what RFC 3339 emitters produce almost everywhere, including this library's own `DateTime` kind, so it is the form a wire-degraded value most often arrives in. Rejecting it meant the accessor failed on the majority of real-world timestamps.

**Impact:**
The relaxation is bounded to the zero-offset case and does not admit ambiguity: the two accepted forms denote the same instant. Text without any designator remains rejected, because it is not a point in time — resolving it against the reader's local offset would make the same text mean different instants on different hosts. The symmetric rule on the other side is that `TryGetDateTime` rejects text carrying a numeric offset, for the same reason.

### 4. `Int64` and `Decimal` are written with the JSON writer's own number formatting

**Original plan:**
The Dispatch safety section specified: "The JSON writers switch on the six shapes; the `Number` arm dispatches over `Int64`/`Double`/`Single`/`Decimal` only."

**Implemented:**
Only `Double` and `Single` take the canonical-text `WriteRawValue` path. `Int64` and `Decimal` use `Utf8JsonWriter`'s native `WriteNumberValue` overloads. The dispatch is driven by a new public `MetadataNumberEncoding` enum (`None`, `Int64`, `Double`, `Single`, `Decimal`) and a `GetNumberEncoding()` extension method sitting next to `GetJsonShape()`.

**Why:**
Two reasons, one performance and one structural.

Routing all four kinds through the canonical text cost a string allocation and a JSON validation pass per value on `Int64` and `Decimal`, where the writer's own formatting is byte-identical anyway. Only `Double` and `Single` need the raw path, because only they carry the trailing `.0` that keeps a whole-number token from reading back as an `Int64`. Measured: 48 B → 0 B per value for `Int64` and `Decimal`; byte-identity against the native overloads was verified across 19 edge cases.

Structurally, the kind list guarding that arm was the last dispatch in the chain that was not compiler-checked, which contradicts the section it lives in. Re-deriving the shape inside the arm would be circular — the check is already inside the `Number` arm and can never fail — and a `switch` *statement* over the kind gets no exhaustiveness diagnostic, so the classification has to be a value-returning switch to be checked at all. That is what earns the enum rather than a private helper: both classifications now fail the Release build together when a kind is added, and a test pins them against each other so a `Number` shape without an encoding cannot ship.

**Impact:**
`MetadataNumberEncoding` is public API that the plan did not anticipate. It is documented as the companion to `MetadataJsonShape` for serializers of protocols other than JSON, which need the same "which primitive is this written from" answer.

## Known Gaps

These were identified in the same review and deliberately not addressed on this branch:

- **`TryFormatCanonical` still allocates.** It calls `ToCanonicalString()` and copies into the destination, so the span overload the plan describes does not yet deliver the allocation-free path its shape implies. An honest fix needs per-kind span formatting, a hand-rolled ISO 8601 duration formatter (`XmlConvert.ToString(TimeSpan)` has no span overload), and a `netstandard2.0` fork (`double.TryFormat` does not exist there).
- **Temporal validation boundaries produce degraded OpenAPI examples.** A `DateTime` boundary cannot be folded by `SemanticModel.GetConstantValue`, so the generated error example carries neither a message nor a `comparativeValue`. This is pre-existing and is tracked in #57. The `DateTime`/`DateTimeOffset`/`TimeSpan`/`Guid`/`Uri` arms added to `ValidatorOpenApiEmitter.ToLiteral` are unreachable through the analyzer until that issue is fixed, and are kept deliberately as its landing site.
