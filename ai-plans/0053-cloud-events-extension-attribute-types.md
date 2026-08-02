# CloudEvents Extension Attributes Follow the CloudEvents Type System

## Rationale

`JsonCloudEventsExtensions.WriteExtensionAttributes` currently uses `SharedJsonSerialization.Writing.MetadataExtensions.WriteMetadataValue`, which emits each `MetadataKind` in its natural JSON form. That is correct for event `data` and `problem+json`, but not for context attributes: CloudEvents core §2.4 limits them to `Boolean`, `Integer`, `String`, `Binary`, `URI`, `URI-reference`, and `Timestamp`; §2.3 applies the same types to extensions; and JSON Event Format §2.2 permits only boolean, integer-number, and string representations, with `null` meaning unset. `Double`, `Single`, `Decimal`, and out-of-range `Int64` therefore violate the format today.

Extension attributes will instead use the CloudEvents mapping, falling back to `String` when a metadata value has no valid native attribute type and enforcing `String`'s character contract. Conforming non-null values retain their canonical text; null is deliberately lossy because the format requires readers to treat it as absent.

## Acceptance Criteria

- [ ] The public encoding API and README document that extension attributes use the JSON Event Format mapping rather than each metadata kind's natural JSON shape.
- [ ] `Boolean` writes a JSON boolean; an `Int64` in the inclusive int32 range writes a JSON number; every other conforming non-null primitive writes a JSON string containing its canonical invariant text. A test pinning the quoted, canonical decimal text replaces `ToCloudEvent_ShouldWriteDecimalExtensionAttribute_AsUnquotedNumber`, and a public-surface matrix covers every non-null primitive plus all four `Int64` boundaries.
- [ ] A `Null` value omits the property and produces bytes identical to an envelope without that entry. An inbound null attribute is absent from extension metadata and cannot replace payload metadata with the same key.
- [ ] One public, value-level API classifies the JSON encoding and one public writer owns the complete name/value operation. Its exhaustive `MetadataKind` switch makes a later named kind fail the Release build; complex values throw with the kind named.
- [ ] The public writer rejects null, empty, whitespace, non-lowercase-alphanumeric, reserved, and standard attribute names before writing anything. Direct guard tests cover every category, including an in-range `Int64` named `type`.
- [ ] CloudEvents-disallowed `String` text is rejected without normalization before a property name is written, with the attribute and offending code point identified. This holds for custom conversion services and directly constructed envelopes. The public character rule is tested with C0, C1, lone high and low surrogates, a noncharacter, an invalid `Char`, and an accepted surrogate pair plus non-ASCII text.
- [ ] The default path scans only caller-controlled text — `String`, `Char`, and `Uri` — and performs at most one CloudEvents character-validation scan per conforming attribute. A custom service may deliberately preflight through the public rule, paying for a second scan to fail before serialization.
- [ ] `MetadataValue.TryGetInt64` additionally accepts a `MetadataKind.String` holding a `long`'s canonical text, and only the canonical form: the parsed value must reproduce the source text, rejecting `"+5"`, `"-0"`, `"01234"`, `" 5"`, and out-of-range text. An out-of-int32 extension value reads back through this accessor unchanged.
- [ ] The public API and README document the value-dependent `Int64` encoding, its stable-type deviation, and the `CloudEventsAttributeConverter` remedy that converts a key to `MetadataKind.String`. Tests pin both the shape change for one attribute name across two events and the stable converter.
- [ ] Read-back tests cover every JSON encoding: string-mapped values return as `MetadataKind.String`, null is absent, and a registered `CloudEventsAttributeParser` restores an original kind. The encoding API documents this asymmetry like `MetadataJsonReader` documents numeric tokens.
- [ ] Standard attributes resolved from metadata (`type`, `source`, `subject`, `dataschema`, `time`, `id`) retain their current string rendering.
- [ ] Writing `Double` and `Single` extension attributes allocates nothing after warm-up. Other string-mapped kinds allocate no more than the one canonical string that `MetadataValue.TryFormatCanonical` materializes today.
- [ ] The 0.7.0 `<PackageReleaseNotes>` records every behavior change with the correct scope and corrects the existing claim that decimals always serialize as JSON numbers.
- [ ] Test code coverage remains above 95%.

## Technical Details

### Encoding policy

| `MetadataKind` | Abstract CloudEvents type | JSON encoding | Change |
| --- | --- | --- | --- |
| `Null` | — (unset) | omitted | **new** |
| `Boolean` | `Boolean` | boolean | — |
| `Int64`, `-2147483648..2147483647` | `Integer` | number | — |
| `Int64`, outside that range | `String` | string | **new** |
| `Double`, `Single`, `Decimal` | `String` | string | **new** |
| `UInt64`, `String`, `Char`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, `Uri` | `String` | string | — |
| `Array`, `Object` | — | throws | **new** |

String encodings use `MetadataValue.ToCanonicalString()`, matching HTTP headers and `MetadataValue.ToString()`. Only the JSON encoding is modeled: `Binary`, `URI`, `URI-reference`, and `Timestamp` share JSON's string representation, while the metadata value alone cannot always justify the narrower abstract type (`DateTimeKind.Unspecified` is not RFC 3339 and `Uri` may be relative). A future binary-mode writer would need the seven abstract types plus information unavailable here; it would not reuse this JSON enum.

Mapping wins over documenting the deviation: portability across CloudEvents implementations is why the feature exists, and a receiver validating attribute types against the spec is entitled to reject a fractional number. Mapping is also preferred to an opt-out on `PortableResultsCloudEventsWriteOptions`: that would double the write matrix and preserve an invalid mode, while pre-1.0 callers needing a specific valid shape already have `CloudEventsAttributeConverter`. Out-of-range `Int64` values are string-mapped rather than rejected because common values such as snowflake IDs and nanosecond timestamps remain representable without loss.

### Value-dependent `Int64`

One key may appear as `2147483647` and later as `"2147483648"`, contrary to core §2.3's stable-type expectation. A stable `Integer` is impossible across the `long` domain. Always using `String` would be stable and, after adding canonical parsing to `TryGetInt64`, would preserve typed accessor use; however, every integer would become quoted, `MetadataKind.Int64` would never round-trip, and no kind would produce CloudEvents `Integer` until a narrower integer kind existed. Its strongest argument is the failure mode: always-`String` takes one uniform break, pre-1.0 and stated in release notes, in exchange for removing a latent, data-dependent one — a consumer that types the attribute as `Integer` works until a value crosses 2^31 and then fails in production. The chosen value-dependent rule preserves natural JSON and `MetadataKind.Int64` wherever CloudEvents permits it. A straddling key still changes raw JSON and `Kind`, and a foreign consumer declaring it as `Integer` can fail at the boundary; a consistently small or large key remains stable in practice. The library can guarantee valid emitted attributes, not compatibility with an extension type declaration it cannot see.

`TryGetInt64` (`MetadataValue.cs:385`) therefore gains the same canonical-string path as `TryGetUInt64` (`MetadataValue.cs:475`), `TryGetSingle`, `TryGetChar`, and `TryGetDecimal`: parse with `CultureInfo.InvariantCulture` and `NumberStyles.AllowLeadingSign`, then require an exact formatting round trip. This makes the accessor work on both sides of the int32 threshold without becoming lenient. It changes every `MetadataValue` of kind `String`, not only CloudEvents values, and is release-noted accordingly. Existing production writing remains unaffected: outside `MetadataObject.TryGetInt64`, only `MetadataExtensions.WriteNumberValue` calls it, after `GetNumberEncoding() == Int64`; and `DefaultHttpHeaderParsingService` already sniffs integral header text into `MetadataValue.FromInt64`, so the header reader never depended on the string arm.

`UInt64` stays uniformly `String`: its accessor already parses canonical text, and a JSON number would not restore `MetadataKind.UInt64`, so a magnitude-dependent form would buy no fidelity. Publishers needing a stable `String` for `Int64` can use `CloudEventsAttributeConverter` to convert the value before writing.

### Deferred `Bytes`

This supersedes `0055-0`'s sequencing note assigning `Bytes` to #53; completed plans remain unedited. A `Bytes` kind would still select JSON `String` and emit the same base64 text callers use today, so it changes neither this wire contract nor its four encodings. Its unresolved questions—structural versus reference equality for `byte[]`, O(n) hashing, and defensive copying—belong to the metadata model. Adding it also touches every exhaustive kind dispatch (`GetJsonShape`, `GetNumberEncoding`, canonical/debug formatting, equality/hash, HTTP, OpenAPI, validation messages, and this mapping), so it moves to its own issue. Deferral is additive and not wire-visible because enum values do not leave the process and base64 is allowable ASCII text.

### Public API and write boundary

```csharp
namespace Light.PortableResults.CloudEvents;

public enum CloudEventsAttributeJsonEncoding { Null, Boolean, Integer, String }

public static class CloudEventsAttributeJsonEncodingExtensions
{
    public static CloudEventsAttributeJsonEncoding GetCloudEventsAttributeJsonEncoding(this MetadataValue value);
}

public static class CloudEventsAttributeText
{
    public static int IndexOfDisallowedCharacter(ReadOnlySpan<char> text);
}
```

```csharp
namespace Light.PortableResults.CloudEvents.Writing.Json;

public static class JsonCloudEventsExtensions
{
    public static void WriteCloudEventsExtensionAttribute(
        this Utf8JsonWriter writer,
        string attributeName,
        MetadataValue value
    );
}
```

The enum names the four JSON encodings, not the seven abstract CloudEvents types; `Null` represents the format's unset encoding rather than an abstract type. It lives in the root CloudEvents namespace because the same classification can support read-side conformance. Classification is value-level because out-of-range `Int64` differs from in-range values. Its switch lists every `MetadataKind` without a default, following `MetadataKindExtensions`; suppressing only `CS8524` preserves `CS8509`, which `TreatWarningsAsErrors` turns into a Release error for a newly declared kind. `Array` and `Object` explicitly throw `InvalidOperationException`.

The writer takes the name and value together so it can validate before `WritePropertyName`, omit null atomically, identify failures, and prevent partial properties. It rejects null/blank names, names outside lowercase ASCII letters and digits, `ForbiddenConvertedAttributeNames`, and every `CloudEventsConstants.StandardAttributeNames` member. The grammar rule exists today only as the private `DefaultCloudEventsAttributeConversionService.IsValidExtensionAttributeName` (`DefaultCloudEventsAttributeConversionService.cs:99`), so it moves to a shared location both the service and the writer call rather than being reimplemented. Standard names cannot use generic extension encoding—for example, an `Int64` `type` would become a number even though `type` is defined as `String`. `WriteExtensionAttributes` skips standard names already emitted through `ResolveAttributes`, then delegates every remaining pair to this method. Custom writers get the identical contract, including paths that bypass `DefaultCloudEventsAttributeConversionService` through a custom service or direct `CloudEventsEnvelopeForWriting` construction. Direct public guard tests are appropriate because these paths are otherwise unreachable through the default sociable surface.

Complex values fail here even if conversion validation was bypassed, matching the existing `MetadataNumberEncoding.None` failure in `MetadataExtensions.WriteNumberValue`. The old `MetadataValueAnnotation` argument is unnecessary: it filtered only complex children, while top-level entries were already selected in `ConvertMetadataToCloudEventsAttributes`.

### CloudEvents `String` validation

`String` excludes C0/C1 controls (U+0000–U+001F, U+007F–U+009F), noncharacters (U+FDD0–U+FDEF and U+FFFE/U+FFFF in every plane), and unpaired surrogates. JSON escaping does not make these values valid. Only caller-controlled `String`, `Char`, and `Uri` text is scanned; numeric, boolean, date/time, `Guid`, and `TimeSpan` text is safe by construction. Values are rejected, never normalized.

The public `CloudEventsAttributeText.IndexOfDisallowedCharacter` returns the UTF-16 index of the first invalid code point, or `-1` when the text conforms; it treats a valid surrogate pair as one scalar and still rejects a pair that encodes a noncharacter. The writer throws `InvalidOperationException` naming the attribute and code point. A conversion service that preflights may instead throw its normal `ArgumentException`; this deliberately adds a second scan to gain failure before serialization. The default service does not preflight, so the mandatory writer check scans once. The fast path is one ASCII range check, with surrogate/noncharacter work only above U+D7FF.

Do not test `Utf8JsonWriter`'s replacement/exception behavior for malformed UTF-16: it is not this library's contract, and a `System.Text.Json` update could break such a test without changing anything here. The public serialization test must prove invalid text never reaches it. Two deliberate gaps remain: standard attributes are rendered elsewhere and retain their current JSON string shape without this character scan, which belongs in a follow-up review of that path; inbound invalid strings remain accepted to avoid breaking consumers because of a producer defect.

### Null, reading, and standard attributes

The writer omits null before committing a property name. The reader skips a null token before adding it to `CloudEventsEnvelopeJsonReader`'s builder, so it is indistinguishable from omission, cannot participate in `CloudEventsAttributeConflictStrategy`, and cannot replace payload metadata under `MergeStrategy`. A null extension therefore cannot round-trip, as required by the format. Tests cover omission, a hand-written inbound null, and collision with payload metadata. Standard attributes already treat null as absent through `GetStringAttribute` and `ReadOptionalStringValue`.

Apart from null, reading is unchanged. String-mapped `Double`, `Single`, `Decimal`, and out-of-range `Int64` return as `MetadataKind.String`, the same kind of asymmetry already documented because `MetadataJsonReader` never produces `Decimal`. The public encoding API and README document this, and a registered `CloudEventsAttributeParser` is the supported way to restore a specific original kind. Standard attributes never enter this writer: `ResolveAttributes` continues writing their canonical text as strings. Their JSON shape is unchanged, although their separate path does not yet enforce the CloudEvents `String` character restrictions.

### Allocation, release notes, and verification

The string arm first calls `MetadataValue.TryFormatCanonical` into a 32-character stack buffer and passes the resulting span to `Utf8JsonWriter.WriteStringValue`, falling back to `ToCanonicalString()` when necessary. The widest numeric text is the 31-character scale-28 value `-0.0000000000000000000000000001`; `decimal.MinValue` is 30. Only `Double` and `Single` span-format without allocation today; `Int64`, `UInt64`, and `Decimal` still materialize one string (`MetadataValue.cs:848`). Removing that requires netstandard2.0 `TryFormat` shims following the `BitOperationsCompat` pattern, or a UTF-8 `Utf8Formatter` path with differential tests, and remains follow-up work from `0058`. Thus floating-point attributes improve, decimal and out-of-range `Int64` regress by one required string, and existing string-shaped kinds are unchanged; the intermediate copy keeps the call site ready for the deferred formatter. The allocation assertion follows `CanonicalFloatingPointFormatterTests`; the existing CloudEvents benchmark already exercises the path, so no new benchmark is required.

Release notes go into unreleased 0.7.0. Four breaking changes are CloudEvents-extension-only: string encoding for `Double`, `Single`, `Decimal`, and out-of-range `Int64`; null omission on write; null-as-unset on read; and rejection of invalid `String` text. Their entries explicitly exclude data and `problem+json`. The fifth change—canonical-string support in `TryGetInt64`—applies globally to strings from headers, JSON, or caller code and is documented separately, including its rejection of noncanonical forms. The existing decimal note is narrowed to payload serialization.

Verification uses the public `ToCloudEvent` matrix for every non-null primitive, asserting `JsonValueKind` and text. The four `Int64` cases are `int.MinValue`, `int.MaxValue`, `(long) int.MinValue - 1`, and `(long) int.MaxValue + 1`; `Null` is covered separately. Read-back covers every encoding and one parser restoration. Release builds, allocation tests, and solution coverage complete the checks.
