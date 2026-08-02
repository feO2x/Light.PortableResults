# CloudEvents Extension Attributes Follow the CloudEvents Type System

## Rationale

`JsonCloudEventsExtensions.WriteExtensionAttributes` writes every converted attribute through `SharedJsonSerialization.Writing.MetadataExtensions.WriteMetadataValue`, which emits each `MetadataKind` in its natural JSON form. That is correct for the event `data` payload and for `problem+json` bodies, but context attributes are not free-form JSON: CloudEvents core §2.4 closes the attribute type system to `Boolean`, `Integer`, `String`, `Binary`, `URI`, `URI-reference`, and `Timestamp`, §2.3 binds extension attributes to that same set, and the JSON Event Format §2.2 maps it onto exactly three JSON forms — boolean, number (integer digits only), and string — with `null` reserved as the encoding of an attribute that is not set. A fractional JSON number is therefore not the valid serialization of any CloudEvents attribute type, and a JSON number outside the int32 range is outside the only type it could belong to. Today `Double`, `Single`, `Decimal`, and out-of-range `Int64` all violate this.

This plan resolves the open decision in favor of mapping rather than deviating: extension attributes are written through the CloudEvents type system, with `String` used as the spec's escape hatch for values the type system cannot express — subject to the character rules that `String` itself imposes, which the library does not enforce today either. For every non-null value the framing on the wire changes but nothing is lost. Null attributes are the one exception, and in the other direction: the format defines a null attribute as unset, so the reader must stop materializing one as `MetadataKind.Null` — a `Null` entry annotated for extension attributes therefore no longer survives a round trip, by design.

## Acceptance Criteria

- [ ] The decision is recorded in the codebase: extension attributes are written through the JSON Event Format's type-system mapping instead of the metadata kind's natural JSON form, documented on the public encoding API and in the README's CloudEvents section.
- [ ] In extension attributes, `Boolean` writes a JSON boolean, `Int64` inside the inclusive int32 range writes a JSON number, and every other primitive kind — including `Double`, `Single`, `Decimal`, and `Int64` outside the int32 range — writes a JSON string carrying the value's canonical invariant text.
- [ ] A `Null` metadata value produces no extension attribute at all: the property name is not written, and the envelope is byte-identical to one whose metadata never contained the entry.
- [ ] An inbound extension attribute whose JSON value is `null` is treated as unset: it does not appear in the metadata produced by reading the envelope, and it does not replace a value of the same key coming from the payload.
- [ ] The kind-to-JSON-encoding decision is reachable through one public API, is named for the JSON encoding it selects rather than for the abstract CloudEvents type system, and is applied at the single extension-attribute write site. A `MetadataKind` declared later fails the Release build instead of silently picking a JSON form.
- [ ] A complex metadata value that reaches the extension-attribute writer is rejected with an exception naming the kind, rather than emitting a nested JSON array or object.
- [ ] Extension attribute text that core §2.4 excludes from `String` — C0 and C1 control characters, Unicode noncharacters, and surrogate code points outside a valid pair — is rejected with an exception identifying the attribute and the offending code point. Values are never normalized, and kinds whose canonical text is machine-generated are not scanned.
- [ ] The character rule is public API, so a custom `ICloudEventsAttributeConversionService` can apply the same check. Tests cover a C0 control character, a C1 control character, a lone high surrogate, a lone low surrogate, a noncharacter, a `Char` value holding one of these, and a valid surrogate pair plus non-ASCII text that must be accepted unchanged.
- [ ] `ToCloudEvent_ShouldWriteDecimalExtensionAttribute_AsUnquotedNumber` is replaced by a test pinning the quoted, canonical decimal text.
- [ ] A test matrix covers every primitive `MetadataKind` in an extension attribute, including all four `Int64` range boundaries (`int.MinValue`, `int.MaxValue`, and the first value outside the range on each side).
- [ ] The `Int64` encoding is documented as value-dependent on the public encoding API and in the README: one attribute name can appear as a JSON number in one event and a JSON string in the next, which deviates from the stable-type expectation in core §2.3. The documentation names the supported way to obtain a stable `String` attribute — a `CloudEventsAttributeConverter` that converts the value before it reaches the writer — and a test pins both the instability across two events and the converter that removes it.
- [ ] Reading a written envelope back is covered for each JSON encoding: string-mapped values return as `MetadataKind.String` with the canonical text, a null attribute is absent from the metadata rather than returned as `MetadataKind.Null`, the kind change is documented on the encoding API the way numeric-token behavior is documented on `MetadataJsonReader`, and a registered `CloudEventsAttributeParser` restores the original kind.
- [ ] Standard attributes resolved from metadata (`type`, `source`, `subject`, `dataschema`, `time`, `id`) keep their current string rendering and are unaffected by the mapping.
- [ ] Writing a `Double` or `Single` extension attribute allocates nothing after warm-up, asserted the same way as in `CanonicalFloatingPointFormatterTests`. The remaining string-mapped kinds allocate at most the one canonical string `MetadataValue.TryFormatCanonical` materializes for them today; removing that is separate work.
- [ ] `<PackageReleaseNotes>` records the new encoding and every behavior change it causes, and the existing decimal entry is corrected so that it no longer claims decimals serialize as JSON numbers everywhere.
- [ ] Test code coverage stays above 95%.

## Technical Details

### Decision and rejected alternatives

Mapping wins over documenting a deviation: portability across CloudEvents implementations is the reason the feature exists, and a receiver validating attribute types against the spec is entitled to reject a fractional number. Two alternatives were considered and rejected:

- **An opt-out on `PortableResultsCloudEventsWriteOptions`.** It would double the write matrix and enshrine a mode whose only purpose is producing invalid events. Pre-1.0 the break is cheap; a publisher who needs a specific wire shape already has `CloudEventsAttributeConverter`.
- **Throwing for an out-of-range `Int64`.** Large integers — snowflake IDs, Unix timestamps in nanoseconds — are exactly what people put in attributes, and they are perfectly expressible as `String`. Turning a working publish into a runtime exception is a worse outcome than reframing the value.
- **Mapping every `Int64` to `String`.** This buys a kind-stable encoding and would be the right answer if the cost were only cosmetic, but `MetadataValue.TryGetInt64` matches on `Kind` alone and does not parse text (`MetadataValue.cs:385`). Every integer extension attribute would then read back as `MetadataKind.String` and `TryGetInt64` would return `false`, so every consumer of every integer attribute — the most common case by far — would need a registered `CloudEventsAttributeParser` or a manual parse to recover what it has today. The next section explains why the resulting shape instability is the better trade.

### The mapping

| `MetadataKind` | Abstract CloudEvents type (core §2.4) | JSON encoding (JSON format §2.2) | Change |
| --- | --- | --- | --- |
| `Null` | — (unset) | attribute omitted | **new** |
| `Boolean` | `Boolean` | boolean | — |
| `Int64`, `-2147483648..2147483647` | `Integer` | number | — |
| `Int64`, outside that range | `String` | string | **new** |
| `Double`, `Single`, `Decimal` | `String` | string | **new** |
| `UInt64`, `String`, `Char`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, `Uri` | `String` | string | — |
| `Array`, `Object` | — | throws | **new** |

The string text is `MetadataValue.ToCanonicalString()`, unchanged from what HTTP headers and `MetadataValue.ToString()` already produce, so the same metadata yields the same text on every transport.

### The `Int64` arm is value-dependent, and that is part of the contract

`Integer` is the only encoding chosen from the value rather than the kind, so one extension attribute name can appear as a JSON number in one event and a JSON string in the next — `2147483647` versus `"2147483648"`. Core §2.3 expects an extension definition to fix one type, so this is a real deviation and it is documented rather than hidden.

It is accepted for three reasons:

- **Exactly one stable mapping exists, and its cost is the one rejected above.** Stability requires the encoding to be a function of the kind alone, which for `Int64` means `String` for every value — the option that costs every consumer its `TryGetInt64`. Keeping `Integer` for in-range values necessarily makes the boundary observable. What is *not* available at any price is a stable `Integer`: for a value beyond int32 that would mean emitting an out-of-range number, the bug this plan fixes.
- **A stable type is guidance to whoever defines the extension**, and this library cannot see that definition. It can guarantee that every event it emits is valid; it cannot guarantee that a caller's values fit a type the caller never declared.
- **Only a straddling key varies.** A key that consistently holds small values, or consistently holds large ones, has a stable encoding in practice — and a key that does straddle could never have carried a valid `Integer` definition to begin with.

The governing rule is that a value-dependent encoding is accepted **only where it buys a kind-preserving round trip**. `Int64` qualifies: `MetadataJsonReader` turns a JSON number back into `MetadataKind.Int64`, so the common case survives the round trip intact. `UInt64` does not qualify — nothing reads back as `MetadataKind.UInt64` — so promoting small `UInt64` values to numbers would add a second unstable shape and buy nothing. That is the asymmetry between the two rows, and it is the principle rather than an exception.

A publisher who needs one fixed type for a key has a supported way to get it: a `CloudEventsAttributeConverter` that converts the value to `MetadataKind.String` before it reaches the writer produces a stable `String` attribute for every value, matching a `String`-typed extension definition. Document that alongside the rule.

The two right-hand columns are distinct things, and only the last one is modeled in code. The abstract type column records why each row lands where it does; the JSON column is the decision the writer makes. `Binary`, `URI`, `URI-reference`, and `Timestamp` are deliberately not modeled, because the library never has enough information to promise the stricter type: a `DateTime` with `DateTimeKind.Unspecified` has no offset and is therefore not a valid RFC 3339 `Timestamp`, and a `Uri` metadata value may be relative. Claiming the narrower type would be a claim the canonical text cannot back — so the code names the JSON encoding it actually decides and stays silent about the abstract type.

### `String` is a character contract, not just a JSON string

Choosing the `String` type is necessary but not sufficient. Core §2.4 defines `String` as a sequence of *allowable* Unicode characters and excludes three groups: the C0 and C1 control characters (U+0000–U+001F and U+007F–U+009F), the Unicode noncharacters (U+FDD0–U+FDEF and U+FFFE/U+FFFF in each of the 17 planes), and surrogate code points outside a valid pair. Escaping does not help — a `\u0001` escape in JSON still decodes to a control character in the attribute value, so the event is non-conformant however it is written.

Most rows of the table are safe by construction: the canonical text of every numeric, boolean, date, time, `Guid`, and `TimeSpan` kind is ASCII digits, signs, and separators. Only `String`, `Char`, and `Uri` carry text the caller controls, and those three admit all of the disallowed groups today.

These values are rejected, not normalized. Silently stripping or replacing a character changes data the caller deliberately put in an attribute and is discovered, if ever, on the consumer side. The check belongs in `DefaultCloudEventsAttributeConversionService.ValidateAttributeValue`, which is already the seam that rejects invalid attribute names and complex values, and it throws the same `ArgumentException` shape naming the attribute and the offending code point. The rule itself is public so a custom `ICloudEventsAttributeConversionService` can apply it; supplying such a service already opts out of the name and primitive checks, and it opts out of this one on the same terms.

Scanning is limited to the three text-bearing kinds, and the per-character test is a single range comparison for the ASCII-printable majority, with the noncharacter and surrogate work reached only above U+D7FF. A conformant attribute therefore pays one linear pass over text the writer is about to walk again anyway. Attribute *names* need no new check — `IsValidExtensionAttributeName` already restricts them to lowercase alphanumerics.

`Utf8JsonWriter`'s own behavior for ill-formed UTF-16 (replacement versus exception) is not this library's contract to define and must not be relied on: validation happens before the writer is reached, and a test should pin what the writer does with a lone surrogate so the interaction is known rather than assumed.

Two gaps remain and are deliberate, not oversights:

- **Standard attributes** resolved from metadata (`type`, `subject`, `id`, and the `source`/`dataschema` URI-references) are rendered on a different path in `ResolveAttributes` and are not scanned. They are usually developer-supplied constants rather than runtime data, and adding a second validation site belongs with a review of that path. Worth a follow-up issue.
- **Inbound events** are not validated. A producer that ships a control character in an attribute produces an event this library will still read; rejecting it would break consumers over a defect in someone else's encoder, which is the wrong trade for a consumer library.

### Null attributes are unset, in both directions

The JSON Event Format permits `null` for an attribute and requires a decoder to treat it as if the attribute were not present. That rule is normative and it settles both ends:

- **Reading.** `CloudEventsEnvelopeJsonReader` currently adds every extension attribute to the builder, so `"ext": null` becomes a `MetadataKind.Null` entry. That contradicts the rule: the decoded event must look as though `ext` was never there. The null token is skipped before the builder sees it, which also means a null attribute can no longer replace a payload-metadata value of the same key under `CloudEventsAttributeConflictStrategy`/`MergeStrategy`. Inbound envelopes from other producers get the same treatment; this is not limited to what this library writes.
- **Writing.** Because a conformant decoder must ignore it, emitting `null` conveys nothing, so the attribute is omitted entirely rather than written as `null`. The two are semantically identical under the format and omission is the cheaper of the two. This changes the bytes on the wire for a `Null` metadata value annotated for extension attributes.

Omission has to be decided before the property name is written, so `WriteExtensionAttributes` consults the encoding and skips the pair; `WriteCloudEventsExtensionAttributeValue` still writes JSON `null` when called directly, because by then the caller has already committed to a property name. Standard attributes already behave this way: `GetStringAttribute` excludes `MetadataKind.Null` explicitly, and `ReadOptionalStringValue` maps an inbound `null` to "not present".

The consequence for the metadata model is that a `Null` extension attribute does not round-trip. That is the correct reading of the format rather than a defect: CloudEvents has no way to say "this attribute is present and empty".

### Public API

```csharp
namespace Light.PortableResults.CloudEvents;

public enum CloudEventsAttributeJsonEncoding { Null, Boolean, Integer, String }

public static class CloudEventsAttributeJsonEncodingExtensions
{
    public static CloudEventsAttributeJsonEncoding GetCloudEventsAttributeJsonEncoding(this MetadataValue value);
}

public static class CloudEventsAttributeText
{
    // Returns the index of the first character core §2.4 excludes from a String, or -1 when the text conforms.
    // A high surrogate followed by a low surrogate is one valid character; either one alone is not.
    public static int IndexOfDisallowedCharacter(ReadOnlySpan<char> text);
}
```

```csharp
namespace Light.PortableResults.CloudEvents.Writing.Json;

public static class JsonCloudEventsExtensions
{
    public static void WriteCloudEventsExtensionAttributeValue(this Utf8JsonWriter writer, MetadataValue value);
}
```

The enum is named for the JSON Event Format encoding it selects, not for the abstract type system. Calling it `CloudEventsAttributeType` while omitting `Binary`, `URI`, `URI-reference`, and `Timestamp` — and adding `Null`, which core §2.4 does not define as a type at all — would misstate a normative distinction of the v1.0.2 type system. `Null`, `Boolean`, `Integer`, and `String` are exactly the four JSON encodings the format admits, and the enum is complete for that job.

The enum lives in `Light.PortableResults.CloudEvents` rather than under `Writing.Json` because it describes the format in both directions; a reader-side conformance check would classify against the same four values.

Only the value-level classification is public. A kind-level overload would answer differently from the writer for an out-of-range `Int64`, which is a footgun in a rule whose whole point is having one answer. `GetCloudEventsAttributeJsonEncoding` is implemented over an exhaustive `switch` expression on `MetadataKind` with no default arm, following `MetadataKindExtensions`: `CS8524` stays suppressed for unnamed values, while a newly declared named kind produces `CS8509`, which is an error under `TreatWarningsAsErrors` in Release. `Array` and `Object` throw `InvalidOperationException` from that switch — a branch a caller can reach directly, so it is testable without a contrived writer setup.

`WriteCloudEventsExtensionAttributeValue` dispatches on the classification and is the only place `WriteExtensionAttributes` writes a value. It is public because a custom envelope writer needs the same rule. The `MetadataValueAnnotation` argument that `WriteMetadataValue` takes disappears: it only ever filtered complex children, which can no longer occur, and the top level was never filtered by it — attributes are already selected by annotation in `CloudEventsResultExtensions.ConvertMetadataToCloudEventsAttributes`.

The throw for complex kinds is a behavior change only for a custom `ICloudEventsAttributeConversionService` that bypasses `DefaultCloudEventsAttributeConversionService.ValidateAttributeValue`; such a service currently produces a nested JSON value that no CloudEvents receiver can type. Failing fast is the better contract, and it matches the existing `MetadataNumberEncoding.None` arm in `MetadataExtensions.WriteNumberValue`.

Standard attributes never reach this path. `WriteExtensionAttributes` skips `CloudEventsConstants.StandardAttributeNames`, and `ResolveAttributes` renders them from `ToCanonicalString()` into `WriteString` calls, which is already spec-conformant.

### Release notes

Everything here ships in the unreleased 0.7.0, so the entries go into that block of `<PackageReleaseNotes>` rather than a new one. Four behavior changes are visible to a consumer and belong under **Breaking changes**: the string encoding of `Double`, `Single`, `Decimal`, and out-of-range `Int64` in extension attributes; the omission of null extension attributes on write; their treatment as unset on read; and the rejection of text that CloudEvents excludes from a `String`. Each needs the scope stated — extension attributes only, with `data` payloads and `problem+json` bodies untouched — because the natural reading of "decimals are now strings" is that it applies everywhere.

The existing decimal entry has to be corrected rather than merely supplemented. It currently states that decimal metadata values "are serialized as JSON numbers instead of quoted strings", which after this plan is true of `data` and `problem+json` but false of extension attributes — the same conflation that #52 introduced and that this plan resolves. Scope that sentence to the payload, and let the new entry carry the attribute rule.

### Allocation

The string arm formats through `MetadataValue.TryFormatCanonical` into a 32-character stack buffer and calls `Utf8JsonWriter.WriteStringValue(ReadOnlySpan<char>)`, falling back to `WriteStringValue(value.ToCanonicalString())` when the destination is too small. Thirty-two characters cover every numeric kind: the widest decimal text is 31 characters — a negative value at scale 28, `-0.0000000000000000000000000001`, not `decimal.MinValue` at 30.

That path is only genuinely allocation-free for `Double` and `Single` today. `TryFormatCanonical` span-formats those two kinds and materializes `ToCanonicalString()` for everything else (`MetadataValue.cs:848`), so `Int64`, `UInt64`, and `Decimal` still allocate a string that is then copied into the buffer. Plan `0058` deferred direct span formatting for the remaining kinds deliberately, and it stays deferred here: doing it properly needs `TryFormat` shims for `long`, `ulong`, and `decimal` on `netstandard2.0`, where those overloads do not exist — the `BitOperationsCompat` pattern or a UTF-8 `Utf8Formatter` path, either of which is its own change with its own differential tests. It is tracked as follow-up work on a separate branch.

The net effect of this plan on allocations is therefore: `Double` and `Single` improve (today they allocate through `WriteRawValue(ToCanonicalString())`), `Decimal` and out-of-range `Int64` regress by one string each as the price of conformance, and every kind that already wrote as a string is unchanged. Writing through the buffer rather than passing the string straight to the writer costs a copy but keeps the call site correct for free once the deferred work lands. No new benchmark is required: the change is one branch plus a span format on a path `CloudEventsWritingBenchmarks` already exercises.

### Reading

Apart from the null rule above, the read path is unchanged. A value written as a string returns as `MetadataKind.String`, so `Double`, `Single`, `Decimal`, and out-of-range `Int64` change kind on the way in — the same class of asymmetry already documented on `MetadataJsonReader`, which never produces `Decimal` at all. State it on `CloudEventsAttributeJsonEncodingExtensions` and in the README, and point at the supported remedy: register a `CloudEventsAttributeParser` for the attribute name to parse the canonical text back into the intended kind. A test should demonstrate that remedy for one kind so the documented escape hatch is known to work.

A binary content-mode writer would not reuse this enum: attributes become header text there, so the JSON encoding has nothing to say about them. What carries over is the canonical text and the decision recorded here that `String` is the fallback for anything the type system cannot express. If binary mode ever needs to distinguish `Binary` from `Timestamp` from `URI` in a header, that is the point at which modeling the seven abstract types earns its keep — and it will need input the writer does not have today, so it is a separate decision rather than a rename of this one.

### Testing notes

The matrix belongs on the public `ToCloudEvent` surface rather than on the writer helper, so it pins the shipped behavior: for each primitive kind, assert both `JsonValueKind` and the value's text. The four `Int64` boundaries are the only non-obvious cases — `int.MinValue` and `int.MaxValue` must be numbers, `(long) int.MinValue - 1` and `(long) int.MaxValue + 1` must be the corresponding quoted digits.

The null rule needs three tests, because writing and reading are now separate statements: a `Null` metadata value yields an envelope with no such property; a hand-written envelope carrying `"ext": null` — the form other producers may send — reads back without an `ext` entry; and a null extension attribute does not displace a payload-metadata entry of the same key.
