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
- [ ] Extension attribute text that core §2.4 excludes from `String` — C0 and C1 control characters, Unicode noncharacters, and surrogate code points outside a valid pair — is rejected at the write boundary with an exception identifying the attribute and the offending code point. Values are never normalized, and kinds whose canonical text is machine-generated are not scanned.
- [ ] The rejection holds on every write path, including a custom `ICloudEventsAttributeConversionService` and a directly constructed `CloudEventsEnvelopeForWriting`, and the envelope carries no partially written property when it fails — the public writer takes the attribute name and value together, so it owns validation, null omission, and the property name as one unit. On the built-in write path no conformant attribute is scanned more than once per event; a caller that opts into preflight validation in a custom conversion service is choosing a second scan in exchange for failing before serialization starts.
- [ ] The character rule is public API, so a conversion service can apply the same check at its own seam and fail before serialization starts. Tests cover a C0 control character, a C1 control character, a lone high surrogate, a lone low surrogate, a noncharacter, a `Char` value holding one of these, and a valid surrogate pair plus non-ASCII text that must be accepted unchanged.
- [ ] `ToCloudEvent_ShouldWriteDecimalExtensionAttribute_AsUnquotedNumber` is replaced by a test pinning the quoted, canonical decimal text.
- [ ] A test matrix covers every non-null primitive `MetadataKind` in an extension attribute, including all four `Int64` range boundaries (`int.MinValue`, `int.MaxValue`, and the first value outside the range on each side). `Null` is covered by the omission and unset criteria above, having no JSON value to assert on.
- [ ] `MetadataValue.TryGetInt64` returns the value for a `MetadataKind.String` holding its canonical text, guarded by a round trip so that `"+5"`, `"-0"`, `"01234"`, `" 5"`, and out-of-range text are rejected. `Int64` therefore matches `UInt64`, `Single`, `Char`, and `Decimal`, which already parse their canonical encodings.
- [ ] An extension attribute holding an `Int64` outside int32 range reads back through `TryGetInt64` with its original value, so the value-dependent encoding is not observable through the typed accessor on either side of the boundary.
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
- **Mapping every `Int64` to `String`, with `TryGetInt64` gaining canonical-string parsing.** This is the serious alternative and it is treated in its own section below. On its own, always-`String` would leave every integer attribute unreadable through `TryGetInt64`, which matches on `Kind` alone (`MetadataValue.cs:385`) — but that is a gap rather than a constraint: `TryGetUInt64`, `TryGetSingle`, `TryGetChar`, and `TryGetDecimal` all parse their canonical text behind a round-trip check (`MetadataValue.cs:475`), and `Int64` is the only numeric kind that does not. Closing it is a few lines against an established pattern, and it makes a stable `String` encoding cheap for consumers.

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

One thing is unavailable at any price: a stable `Integer`. For a value beyond int32 that would mean emitting an out-of-range number, the bug this plan fixes. The genuine choice is between the value-dependent encoding above and mapping every `Int64` to `String`, and the two trade against each other as follows.

**Always `String`** makes the encoding a pure function of the kind, which is the rule the rest of this plan follows and the only way to give an attribute name one stable type. Paired with canonical-string parsing in `TryGetInt64` — the gap noted above, where `Int64` is the only numeric kind that cannot read its own canonical text — consumer access survives: `TryGetInt64` keeps working, only `Kind` changes. It also removes the one exception to the kind-function rule, and it converts a latent, data-dependent interop failure into a single documented break: a consumer that types an attribute as `Integer` works until a value crosses 2^31 and then fails in production, which is a worse failure mode than a uniform change taken once, pre-1.0, in release notes. Its costs are that every integer attribute on the wire becomes a quoted number, `MetadataKind.Int64` no longer survives the round trip, and the `Integer` encoding has no producer until a kind exists whose domain fits int32.

**Value-dependent** keeps the common case natural — small integers stay JSON numbers and read back as `MetadataKind.Int64` — and confines the instability to keys whose values straddle the boundary. Such a key could never have carried a valid `Integer` definition anyway, and a key that consistently holds small or consistently holds large values is stable in practice. Its cost is that one attribute name can change JSON type between events, and that the "encoding is a function of the kind" rule acquires an exception.

A stable type is guidance to whoever *defines* an extension, and this library cannot see that definition: it can guarantee that every event it emits is valid, not that a caller's values fit a type the caller never declared. That argument supports validity in both designs and does not by itself decide between them.

**The decision is value-dependent encoding together with canonical-string parsing in `TryGetInt64`.** The parsing arm is what makes the threshold defensible: without it, an out-of-range value writes as a string and then reads back as something `TryGetInt64` refuses, so the threshold would quietly break exactly the consumers it was meant to keep working. With it, the accessor is total across both encodings — whichever side of the boundary a value falls on, a consumer calling `TryGetInt64` gets its `long` back. What remains observable is `Kind` and the raw JSON, not the ability to read the value.

That also settles the asymmetry with `UInt64` properly. `UInt64` was already safe as a string precisely because `TryGetUInt64` parses its canonical text; `Int64` was the outlier that could not. Once both parse, the rule reads cleanly in two layers: **every numeric kind can read its own canonical text, and on top of that the writer preserves `MetadataKind` wherever the CloudEvents type system permits.** `Int64` in int32 range is the only place it permits it, because a JSON number is what `MetadataJsonReader` turns back into `MetadataKind.Int64`. Promoting `UInt64` would add an unstable shape while preserving no kind, so it stays `String`.

Two costs survive this and are not fixed by the parsing arm: `Kind` still differs between events for a straddling key, and a foreign consumer whose extension definition types the attribute as `Integer` still breaks when a value crosses the boundary. Only always-`String` would remove the second, and nothing removes it for a key that straddles.

A publisher who needs one fixed type for a key has a supported way to get it: a `CloudEventsAttributeConverter` that converts the value to `MetadataKind.String` before it reaches the writer produces a stable `String` attribute for every value, matching a `String`-typed extension definition. Document that alongside the rule.

### `TryGetInt64` reads canonical text

`MetadataValue.TryGetInt64` gains a string arm modeled on `TryGetUInt64` (`MetadataValue.cs:475`): parse with `CultureInfo.InvariantCulture`, then accept only when re-formatting the parsed value reproduces the input exactly. The round-trip guard is what keeps this from turning the accessor into a lenient parser — `"+5"`, `"-0"`, `"01234"`, `" 5"`, and text outside `long` range are all rejected, because none of them is the canonical encoding of the value they parse to. `NumberStyles.AllowLeadingSign` replaces `TryGetUInt64`'s `NumberStyles.None`, since `Int64` is signed.

The change is confined to what a consumer can read. The only production caller outside `MetadataObject.TryGetInt64` is `MetadataExtensions.WriteNumberValue`, which reaches it after dispatching on `GetNumberEncoding() == Int64`, so `Kind` is already `Int64` there and the string arm is unreachable. The HTTP header reader does not need it either: `DefaultHttpHeaderParsingService` already sniffs integral header text into `MetadataValue.FromInt64` itself.

It is still a visible behavior change for existing callers — a `MetadataKind.String` value holding `"5"` now returns `true` where it returned `false` — so it belongs in the release notes rather than passing as an internal fix.

The two right-hand columns are distinct things, and only the last one is modeled in code. The abstract type column records why each row lands where it does; the JSON column is the decision the writer makes. `Binary`, `URI`, `URI-reference`, and `Timestamp` are deliberately not modeled, because the library never has enough information to promise the stricter type: a `DateTime` with `DateTimeKind.Unspecified` has no offset and is therefore not a valid RFC 3339 `Timestamp`, and a `Uri` metadata value may be relative. Claiming the narrower type would be a claim the canonical text cannot back — so the code names the JSON encoding it actually decides and stays silent about the abstract type.

### The deferred `Bytes` kind stays deferred

`0055-0` assigned the deferred `Bytes` kind to this issue, "since CloudEvents `Binary` is the first concrete consumer of it", as part of a sequencing note that expected #53 to become "a lookup from `MetadataKind`" to CloudEvents *types*. This plan models the JSON *encoding* instead, for the reasons above. That change removes the premise: `Binary` is not a JSON encoding. The JSON Event Format renders it as a base64 string, so a `Bytes` kind would select the same `String` encoding that base64 text in a `MetadataKind.String` already selects today. It would change no byte on the wire, no acceptance criterion here, and nothing about the four encodings the writer chooses between.

The blockers `0055-0` recorded are also untouched by anything in this plan, and they are metadata-system questions rather than CloudEvents ones: whether equality over a `byte[]` payload is structural (O(n) comparison and hashing for values used as `MetadataObject` entries) or by reference (inconsistent with every other kind), and whether the factory copies defensively to preserve immutability. Settling those inside a conformance fix would be the wrong venue, and adding the kind would touch every exhaustive `MetadataKind` switch in the library — `GetJsonShape`, `GetNumberEncoding`, `ToCanonicalString`, `Equals`, `GetHashCode`, `ToString`, `HttpHeaderValueFormatter`, the OpenAPI mapper, validation message formatting, and the new encoding switch — turning a scoped bug fix into a cross-cutting change.

`Bytes` therefore remains deferred and moves to its own issue; the sequencing note in `0055-0` is superseded on this point, and that plan is not edited, per the convention that completed plans are left as written. Deferring stays cheap for the same reason `0055-0` gave: enum values never leave the process, and base64 inside a `String` is exactly what the kind would produce, so introducing it later is additive and not wire-visible. A caller holding `byte[]` today writes base64 into a `String` attribute, which this plan encodes correctly — and the base64 alphabet is ASCII-printable, so it never trips the character rule below.

### `String` is a character contract, not just a JSON string

Choosing the `String` type is necessary but not sufficient. Core §2.4 defines `String` as a sequence of *allowable* Unicode characters and excludes three groups: the C0 and C1 control characters (U+0000–U+001F and U+007F–U+009F), the Unicode noncharacters (U+FDD0–U+FDEF and U+FFFE/U+FFFF in each of the 17 planes), and surrogate code points outside a valid pair. Escaping does not help — a `\u0001` escape in JSON still decodes to a control character in the attribute value, so the event is non-conformant however it is written.

Most rows of the table are safe by construction: the canonical text of every numeric, boolean, date, time, `Guid`, and `TimeSpan` kind is ASCII digits, signs, and separators. Only `String`, `Char`, and `Uri` carry text the caller controls, and those three admit all of the disallowed groups today.

These values are rejected, not normalized. Silently stripping or replacing a character changes data the caller deliberately put in an attribute and is discovered, if ever, on the consumer side.

Enforcement belongs at the write boundary, not at the conversion seam. `DefaultCloudEventsAttributeConversionService.ValidateAttributeValue` is bypassed by any custom `ICloudEventsAttributeConversionService` and by a directly constructed `CloudEventsEnvelopeForWriting`, whose constructor takes the attribute `MetadataObject` and is public. Validating only there would make the character rule advisory while the complex-kind rule is absolute — an inconsistency with no justification, since both protect the same invariant. `WriteExtensionAttributes` therefore performs the check itself, before `WritePropertyName`, so no path can emit prohibited text and no half-written property is left behind when it fails. That site also holds the attribute name the exception message needs; the value writer alone does not.

There is exactly one place that writes an attribute, so on the built-in path the text is scanned once and every caller is covered by construction. `DefaultCloudEventsAttributeConversionService` deliberately does not repeat the scan: writer validation is mandatory, so a second check there would make every conformant attribute pay twice on every event to buy nothing the writer does not already guarantee.

`CloudEventsAttributeText` is public so that a custom service can nonetheless run the check at its own seam. That is a real trade and not a free option — the writer still validates afterwards, so preflighting costs a second pass over conforming text. It buys failing at `ToCloudEvent` time, with an `ArgumentException` naming the offending attribute before a single byte is serialized, which is worth the pass to a caller assembling attributes from untrusted input. The single-scan guarantee is therefore a property of the default path, not of every configuration.

The exception is an `InvalidOperationException` naming the attribute and the offending code point, matching the complex-kind throw at the same site. A conversion service applying the rule earlier throws `ArgumentException` from its own seam, as `ValidateAttributeValue` already does for names and complex values.

Scanning is limited to the three text-bearing kinds, and the per-character test is a single range comparison for the ASCII-printable majority, with the noncharacter and surrogate work reached only above U+D7FF. A conformant attribute therefore pays one linear pass over text the writer is about to walk again anyway. Attribute *names* need no new check — `IsValidExtensionAttributeName` already restricts them to lowercase alphanumerics.

`Utf8JsonWriter`'s own behavior for ill-formed UTF-16 — replacement versus exception — is not this library's contract to define, and no test should pin it: it is incidental to the promise being made, and a test asserting it would break on a `System.Text.Json` update that changes nothing about this library. What matters is that the writer never sees such text. The lone-surrogate test therefore asserts the rejection through the public serialization API, which is the behavior the library actually owns.

Two gaps remain and are deliberate, not oversights:

- **Standard attributes** resolved from metadata (`type`, `subject`, `id`, and the `source`/`dataschema` URI-references) are rendered on a different path in `ResolveAttributes` and are not scanned. They are usually developer-supplied constants rather than runtime data, and adding a second validation site belongs with a review of that path. Worth a follow-up issue.
- **Inbound events** are not validated. A producer that ships a control character in an attribute produces an event this library will still read; rejecting it would break consumers over a defect in someone else's encoder, which is the wrong trade for a consumer library.

### Null attributes are unset, in both directions

The JSON Event Format permits `null` for an attribute and requires a decoder to treat it as if the attribute were not present. That rule is normative and it settles both ends:

- **Reading.** `CloudEventsEnvelopeJsonReader` currently adds every extension attribute to the builder, so `"ext": null` becomes a `MetadataKind.Null` entry. That contradicts the rule: the decoded event must look as though `ext` was never there. The null token is skipped before the builder sees it, which also means a null attribute can no longer replace a payload-metadata value of the same key under `CloudEventsAttributeConflictStrategy`/`MergeStrategy`. Inbound envelopes from other producers get the same treatment; this is not limited to what this library writes.
- **Writing.** Because a conformant decoder must ignore it, emitting `null` conveys nothing, so the attribute is omitted entirely rather than written as `null`. The two are semantically identical under the format and omission is the cheaper of the two. This changes the bytes on the wire for a `Null` metadata value annotated for extension attributes.

Omission has to be decided before the property name is written, which is why the public writer takes the name and the value together: it consults the encoding first and returns without writing anything for a null. No entry point exists that can be reached after a property name is already committed, so there is no path on which a null attribute still has to be written as `null`. Standard attributes already behave this way: `GetStringAttribute` excludes `MetadataKind.Null` explicitly, and `ReadOptionalStringValue` maps an inbound `null` to "not present".

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
    // Writes one complete extension attribute: omits it entirely for a null value, otherwise validates
    // the text, writes the property name, and emits the value in its CloudEvents JSON encoding.
    public static void WriteCloudEventsExtensionAttribute(
        this Utf8JsonWriter writer,
        string attributeName,
        MetadataValue value
    );
}
```

The enum is named for the JSON Event Format encoding it selects, not for the abstract type system. Calling it `CloudEventsAttributeType` while omitting `Binary`, `URI`, `URI-reference`, and `Timestamp` — and adding `Null`, which core §2.4 does not define as a type at all — would misstate a normative distinction of the v1.0.2 type system. `Null`, `Boolean`, `Integer`, and `String` are exactly the four JSON encodings the format admits, and the enum is complete for that job.

The enum lives in `Light.PortableResults.CloudEvents` rather than under `Writing.Json` because it describes the format in both directions; a reader-side conformance check would classify against the same four values.

Only the value-level classification is public. A kind-level overload would answer differently from the writer for an out-of-range `Int64`, which is a footgun in a rule whose whole point is having one answer. `GetCloudEventsAttributeJsonEncoding` is implemented over an exhaustive `switch` expression on `MetadataKind` with no default arm, following `MetadataKindExtensions`: `CS8524` stays suppressed for unnamed values, while a newly declared named kind produces `CS8509`, which is an error under `TreatWarningsAsErrors` in Release. `Array` and `Object` throw `InvalidOperationException` from that switch — a branch a caller can reach directly, so it is testable without a contrived writer setup.

The public method takes the name and the value, because the attribute — not the value — is the unit the rule applies to. Every part of the contract needs the pair: omission is a decision about whether the property exists at all, the exception message names the attribute, and "no partially written property" is only enforceable by whoever calls `WritePropertyName`. A value-only method would push all three onto the caller and could satisfy none of them. `WriteExtensionAttributes` therefore reduces to a filter plus a loop over this one method, and a custom writer that calls it gets the identical rule rather than a reimplementation of it — which matters because `CloudEventsEnvelopeForWriting` is a record struct whose primary constructor accepts `ExtensionAttributes` outright, so the writer is reachable without passing through any conversion service.

The `MetadataValueAnnotation` argument that `WriteMetadataValue` takes disappears: it only ever filtered complex children, which can no longer occur, and the top level was never filtered by it — attributes are already selected by annotation in `CloudEventsResultExtensions.ConvertMetadataToCloudEventsAttributes`.

Name policy splits along whether a name is illegitimate or merely written elsewhere. `data`, `data_base64`, and `lproutcome` are never valid as extension attributes, so the method rejects them exactly as `ValidateAttributeName` does at the conversion seam. The standard names are legitimate attributes that this integration renders from `ResolveAttributes`, and only the envelope writer knows they have already been emitted, so skipping them stays in `WriteExtensionAttributes` where that knowledge lives. A custom writer doing its own envelope assembly owns that decision, and the documentation says so.

The throw for complex kinds is a behavior change only on the paths that bypass `DefaultCloudEventsAttributeConversionService.ValidateAttributeValue` — a custom conversion service, or an envelope constructed directly — which today produce a nested JSON value that no CloudEvents receiver can type. Failing fast is the better contract, and it matches the existing `MetadataNumberEncoding.None` arm in `MetadataExtensions.WriteNumberValue`.

Standard attributes never reach this path. `WriteExtensionAttributes` skips `CloudEventsConstants.StandardAttributeNames`, and `ResolveAttributes` renders them from `ToCanonicalString()` into `WriteString` calls, so their JSON shape already matches what the type-system mapping would choose and this plan leaves it untouched. That is a statement about their rendering, not a claim that they are fully conformant: their text is not scanned for the characters `String` excludes, which is the gap recorded above.

### Release notes

Everything here ships in the unreleased 0.7.0, so the entries go into that block of `<PackageReleaseNotes>` rather than a new one. Five behavior changes are visible to a consumer and belong under **Breaking changes**. Four are confined to CloudEvents extension attributes: the string encoding of `Double`, `Single`, `Decimal`, and out-of-range `Int64`; the omission of null attributes on write; their treatment as unset on read; and the rejection of text that CloudEvents excludes from a `String`. Each of those four needs its scope stated — extension attributes only, with `data` payloads and `problem+json` bodies untouched — because the natural reading of "decimals are now strings" is that it applies everywhere.

The fifth is not a CloudEvents change and must not be filed under that scope: `TryGetInt64` returning `true` for a string holding canonical integer text applies to every `MetadataValue` of kind `String`, whatever produced it — an HTTP header, a JSON body, or a literal in caller code. Its entry belongs with the metadata accessors and states the round-trip guard, so a reader can tell that `"01234"` and `"+5"` are still rejected.

The existing decimal entry has to be corrected rather than merely supplemented. It currently states that decimal metadata values "are serialized as JSON numbers instead of quoted strings", which after this plan is true of `data` and `problem+json` but false of extension attributes — the same conflation that #52 introduced and that this plan resolves. Scope that sentence to the payload, and let the new entry carry the attribute rule.

### Allocation

The string arm formats through `MetadataValue.TryFormatCanonical` into a 32-character stack buffer and calls `Utf8JsonWriter.WriteStringValue(ReadOnlySpan<char>)`, falling back to `WriteStringValue(value.ToCanonicalString())` when the destination is too small. Thirty-two characters cover every numeric kind: the widest decimal text is 31 characters — a negative value at scale 28, `-0.0000000000000000000000000001`, not `decimal.MinValue` at 30.

That path is only genuinely allocation-free for `Double` and `Single` today. `TryFormatCanonical` span-formats those two kinds and materializes `ToCanonicalString()` for everything else (`MetadataValue.cs:848`), so `Int64`, `UInt64`, and `Decimal` still allocate a string that is then copied into the buffer. Plan `0058` deferred direct span formatting for the remaining kinds deliberately, and it stays deferred here: doing it properly needs `TryFormat` shims for `long`, `ulong`, and `decimal` on `netstandard2.0`, where those overloads do not exist — the `BitOperationsCompat` pattern or a UTF-8 `Utf8Formatter` path, either of which is its own change with its own differential tests. It is tracked as follow-up work on a separate branch.

The net effect of this plan on allocations is therefore: `Double` and `Single` improve (today they allocate through `WriteRawValue(ToCanonicalString())`), `Decimal` and out-of-range `Int64` regress by one string each as the price of conformance, and every kind that already wrote as a string is unchanged. Writing through the buffer rather than passing the string straight to the writer costs a copy but keeps the call site correct for free once the deferred work lands. No new benchmark is required: the change is one branch plus a span format on a path `CloudEventsWritingBenchmarks` already exercises.

### Reading

Apart from the null rule above, the read path is unchanged. A value written as a string returns as `MetadataKind.String`, so `Double`, `Single`, `Decimal`, and out-of-range `Int64` change kind on the way in — the same class of asymmetry already documented on `MetadataJsonReader`, which never produces `Decimal` at all. State it on `CloudEventsAttributeJsonEncodingExtensions` and in the README, and point at the supported remedy: register a `CloudEventsAttributeParser` for the attribute name to parse the canonical text back into the intended kind. A test should demonstrate that remedy for one kind so the documented escape hatch is known to work.

A binary content-mode writer would not reuse this enum: attributes become header text there, so the JSON encoding has nothing to say about them. What carries over is the canonical text and the decision recorded here that `String` is the fallback for anything the type system cannot express. If binary mode ever needs to distinguish `Binary` from `Timestamp` from `URI` in a header, that is the point at which modeling the seven abstract types earns its keep — and it will need input the writer does not have today, so it is a separate decision rather than a rename of this one.

### Testing notes

The matrix belongs on the public `ToCloudEvent` surface rather than on the writer helper, so it pins the shipped behavior: for every non-null primitive kind, assert both `JsonValueKind` and the value's text. `Null` is not in the matrix — an omitted property has neither — and is covered by the three null tests below instead. The four `Int64` boundaries are the only non-obvious cases — `int.MinValue` and `int.MaxValue` must be numbers, `(long) int.MinValue - 1` and `(long) int.MaxValue + 1` must be the corresponding quoted digits.

The null rule needs three tests, because writing and reading are now separate statements: a `Null` metadata value yields an envelope with no such property; a hand-written envelope carrying `"ext": null` — the form other producers may send — reads back without an `ext` entry; and a null extension attribute does not displace a payload-metadata entry of the same key.
