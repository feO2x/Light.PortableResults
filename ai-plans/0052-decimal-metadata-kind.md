# Decimal Metadata Kind

## Rationale

`MetadataValue.FromDecimal` converts its input to invariant text and stores `MetadataKind.String`. Decimals therefore serialize into JSON bodies as quoted strings, while `PortableOpenApiSchemaTypeMapper` maps `decimal` to `JsonSchemaType.Number` and `PortableResultsOpenApiDocumentTransformer` emits decimal examples as unquoted numbers. The published contract and the runtime payload disagree. The mismatch is reachable through the built-in validation error definitions, which route every `TypeCode.Decimal` parameter through `FromDecimal`, so any decimal precision-and-scale rule produces a `problem+json` body that violates the OpenAPI document generated for the same endpoint.

Storing decimals as text also loses type information (a decimal is indistinguishable from a numeric-looking string), forces `TryGetDecimal` to run `decimal.TryParse` on every call, and allocates more than necessary. This plan introduces a dedicated `MetadataKind.Decimal` backed by a boxed `decimal`.

## Acceptance Criteria

- [ ] `MetadataValue.FromDecimal` produces a value whose `Kind` is `MetadataKind.Decimal`.
- [ ] A decimal metadata value is written into JSON bodies as an unquoted JSON number that preserves all significant digits and the original scale.
- [ ] A `problem+json` body produced by a decimal precision-and-scale validation rule conforms to the OpenAPI document generated for the same endpoint, asserted by an integration test that inspects the raw response body.
- [ ] `MetadataKind.Decimal` is classified as primitive, so decimals remain valid inside arrays annotated for header serialization and valid as CloudEvents extension attributes.
- [ ] Every declared `MetadataKind` member is asserted to be classified correctly as primitive or complex by a test that enumerates the enum, so a member declared outside the reserved primitive range fails the build.
- [ ] `TryGetDecimal` returns the stored value for `MetadataKind.Decimal` without parsing text, and continues to convert from `Int64`, `Double`, and numeric strings.
- [ ] `TryGetString` returns `false` for a decimal metadata value.
- [ ] `MetadataValue.ToString()` renders decimals as unquoted invariant-culture numeric text.
- [ ] A decimal metadata value resolves correctly when used as a CloudEvents core string attribute instead of silently becoming `null`.
- [ ] HTTP header formatting emits decimals without quote characters.
- [ ] The JSON reader's treatment of numeric tokens is explicitly specified and covered by tests, including the documented cases where a decimal does not read back as `MetadataKind.Decimal`.
- [ ] `Unsafe.SizeOf<MetadataValue>()` is unchanged from before this plan, asserted by a test that pins the current value.
- [ ] Test code coverage stays above 95%.

## Technical Details

### Storage

`MetadataKind.Decimal` stores a **boxed** `decimal` in the existing `Reference` slot of `MetadataPayload` at offset 8. An inline 128-bit field would push `Reference` to offset 16 and grow every `MetadataValue` by 8 bytes, including array elements stored inline in `MetadataArrayData` — an unacceptable cost in a library whose primary claim is reduced allocation. Boxing costs 24 bytes on x64, less than the 32–56 bytes the current string representation typically occupies, and removes the parse on every read.

`double` remains the representation for general floating-point numbers. It covers the full JSON numeric range, where `decimal` is limited to ±7.9e28 and could not represent legitimate inbound values such as `1e100`.

### Enum ordering is a hard constraint

`MetadataKindExtensions.IsPrimitive` is implemented as `kind < MetadataKind.Array`, so membership of the primitive set is decided purely by ordering. Adding `Decimal` after `Object` compiles cleanly and silently classifies decimals as complex values: they would be rejected as CloudEvents extension attributes, rejected inside arrays annotated for header serialization, and would flip `HasOnlyPrimitiveChildren` to `false` on any containing array or object. No compiler diagnostic catches this.

`Decimal` is therefore appended to the primitive block as `5`, and the complex kinds move to a reserved range:

```csharp
public enum MetadataKind : byte
{
    Null = 0, Boolean = 1, Int64 = 2, Double = 3, String = 4, Decimal = 5,
    // 6-199 are reserved for future primitive kinds
    Array = 200,
    Object = 201
}
```

The gap exists so that later primitives can be added without renumbering the complex kinds a second time. This is not speculative: CloudEvents defines `Binary`, `URI`, `URI-reference`, and `Timestamp` as attribute types, all of which are currently flattened into `String`. The values are renumbered in this change because `Array` and `Object` move regardless, making the reservation free now and a separate breaking change later. It also settles the numbering before any gRPC mapping can make it wire-visible.

The reserved range must be documented on the enum itself, and `IsPrimitive` must keep the boundary comparison rather than switching to an enumerated list — the comparison is a single instruction on a path used by every array and object construction.

A gap alone does not enforce the invariant. `IsPrimitive` currently has no direct test coverage at all, so the ordering constraint is unguarded. A test must iterate `Enum.GetValues<MetadataKind>()` and assert each member against an explicit expected classification, so that a future member declared on the wrong side of the boundary fails immediately rather than degrading silently.

### Read side: an explicit non-guarantee

A JSON number carries no discriminator between a decimal and a double, so the reader must choose. Two candidate behaviours exist, and the choice must be recorded rather than implied:

- **Default:** `SharedJsonSerialization.Reading.MetadataJsonReader.ReadNumber` keeps its current `Int64`-then-`Double` behaviour and never produces `MetadataKind.Decimal`. `Http.Reading.Json.MetadataJsonReader` delegates to it, so this is a single site.
- **Opt-in:** a reader option, following the precedent of `HeaderValueParsingMode`, that prefers `Decimal` for numeric tokens outside `Int64` range that fit in `decimal`.

The default is chosen. Preferring `Decimal` unconditionally would make the resulting kind depend on the *magnitude* of the value and would break round-tripping in the opposite direction, with `FromDouble(0.1)` reading back as `Decimal`.

This plan therefore improves **outbound** fidelity. It deliberately does not claim that a decimal round-trips as a decimal; that limitation is inherent to untyped numeric wire formats and is the same constraint recorded for HTTP headers in `0051`. Acceptance criteria must not assert round-trip symmetry for decimals.

### Affected components

Only two exhaustive `MetadataKind` switches exist outside the `Metadata` folder, and both fail to compile without a new branch:

- `SharedJsonSerialization/Writing/MetadataExtensions.WriteMetadataValue` — write via `WriteNumberValue(decimal)`.
- `CloudEvents/MetadataValueAnnotationHelper`.

The dangerous case is `CloudEventsResultExtensions.GetStringAttribute`, which is an `if`-chain over `TryGetString`/`TryGetBoolean`/`TryGetInt64`/`TryGetDouble` falling through to `null`. Today a decimal is caught by the `TryGetString` branch. After this change it falls through and a decimal-valued `subject`, `type`, or `source` silently becomes `null` instead of resolving or throwing. This is a behavioural regression the compiler will not surface.

Everything else reaches decimals through `IsPrimitive` or the `TryGet*` accessors and needs no change.

### Equality and hashing

`MetadataKind.Decimal` joins the existing `String or Array or Object` group in `GetHashCode`, since a boxed `decimal` hashes correctly through `Reference.GetHashCode()`. `Equals` needs a branch that unboxes and compares as `decimal` rather than comparing references.

This changes observable behaviour: `decimal.Equals` and `decimal.GetHashCode` are scale-insensitive and mutually consistent, so `19.50m` and `19.5m` become equal metadata values. Under the current string storage they compare as `"19.50"` and `"19.5"` and are *not* equal. Scale is still preserved for serialization — only equality changes.

### Breaking changes

The library is pre-1.0 and breaking changes are permitted, but these are silent at compile time for downstream callers and must be listed in the package release notes:

- `TryGetString` no longer returns `true` for decimals.
- `Kind` for a decimal is no longer `MetadataKind.String`.
- The numeric values of `MetadataKind.Array` and `MetadataKind.Object` change to `200` and `201`. No code in the solution casts `MetadataKind` to a numeric type and the enum is not exposed by the OpenAPI, Validation, or source-generation packages, so this is invisible today — but any consumer that persisted or transmitted the numeric value is affected.
- Decimal metadata appears as a JSON number rather than a JSON string in serialized bodies.
- Decimals differing only in trailing zeros now compare equal.

### Sequencing with #51

This plan should land before `0051-http-header-value-formatting`. That plan's kind table currently folds decimals into the `String` row; landing it first would require amending both the table and its test matrix immediately afterwards. With this plan first, `0051` gains a `Decimal` row from the outset — invariant numeric text, unquoted, identical in shape to the `Double` row.

### Tests

`MetadataValueTests.FromDecimal_ShouldStoreAsString` inverts and must be renamed alongside its assertions. `MetadataObjectTests` and `MetadataValueAnnotationTests` also construct decimal values and need review.

Beyond the unit-level kind matrix, the criterion that carries the actual defect is the OpenAPI conformance test: trigger a decimal precision-and-scale validation failure through an integration test app and assert that the raw `problem+json` body contains an unquoted number for the precision and scale metadata.

### Out of scope

Making decimals round-trip as decimals, and the opt-in reader mode described above. Both are follow-up work once the outbound representation is correct.
