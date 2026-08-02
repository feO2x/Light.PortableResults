# Add an `IsUuidV7` Assertion

## Rationale

Distributed systems increasingly let clients generate their own identifiers, and UUIDv7 (RFC 9562 §5.7) is the format
of choice because its leading 48-bit Unix millisecond timestamp keeps client-generated keys roughly sortable and
index-friendly. A server accepting such an identifier over the wire currently has no way to state that requirement:
`Check<Guid>` offers only `IsEmpty`/`IsNotEmpty`, so a v4 GUID from a misconfigured client is accepted and only shows
up later as index fragmentation.

`Checks.IsUuidV7` closes that gap as the first Guid-shaped format assertion, following the same structure as the
string format assertions (`IsEmail`, `ContainsOnlyDigits`): a dedicated error code, a customizable message template,
and a metadata-free OpenAPI contract. It is the first of several assertions planned for the validation library; this
plan covers only this one.

## Acceptance Criteria

- [ ] `Check<Guid>` exposes `IsUuidV7` in both built-in-message and `ErrorOverrides` overloads, each honoring
      `shortCircuitOnError` and the already-short-circuited state, matching the shape of every other built-in assertion.
- [ ] The same invariant is available standalone as public `GuidExtensions.IsUuidV7(this Guid value)`, under that one
      name, with the assertion delegating to it so the bit manipulation exists in a single place.
- [ ] The assertion passes only for GUIDs whose RFC 9562 version field is `7` **and** whose two most significant
      variant bits are `10`. `Guid.Empty`, `Guid.NewGuid()` (v4), and version-7 values carrying a non-RFC variant all fail.
- [ ] Failures carry the new `ValidationErrorCodes.UuidV7` code and a message from a new customizable
      `ValidationErrorTemplates.UuidV7` template, which survives `with`-expression copies of `ValidationErrorTemplates`.
- [ ] The rule is discoverable by OpenAPI source generation and resolves to a registered metadata-free contract, so a
      validator using it documents the `UuidV7` code without an explicit hint and without an unknown-code diagnostic.
- [ ] An exhaustive version-nibble × variant-nibble matrix checks the predicate against an oracle built from **both**
      `Guid.Version` and `Guid.Variant`, so the BCL guards the whole layout assumption rather than half of it.
- [ ] Automated tests additionally cover both overloads, short-circuit propagation, template customization, and the
      generated OpenAPI metadata. Solution test coverage stays above 95%.
- [ ] One predicate implementation serves both target frameworks, without conditional compilation, and the passing
      path allocates nothing on either.
- [ ] The README shows the assertion where client-generated identifiers are validated, and the 0.7.0
      `<PackageReleaseNotes>` of **both** affected packages record their own part of the change.

## Technical Details

### Predicate

RFC 9562 places the version in the high nibble of octet 6 and the variant in the two most significant bits of
octet 8. Both must be checked: `Guid.Version` reports the version nibble regardless of the variant bits, so a value
such as `017f22e2-79b0-7cc3-08c4-dc0c0c07398f` — an NCS-reserved variant — would pass a version-only test even though
`Guid.CreateVersion7` can never produce it. Requiring `10` keeps the assertion a statement about RFC 9562 identifiers
rather than about a nibble. The remaining two bits of the variant nibble are free, so `8`, `9`, `a`, and `b` are all
acceptable there.

Read both fields by reinterpreting the `Guid` over a struct mirroring its field layout. This is allocation-free on
both target frameworks and needs no conditional compilation:

```csharp
#pragma warning disable CS0649 // Fields are populated by reinterpreting Guid storage via Unsafe.As.
private struct GuidFields
{
    public int A;
    public short B;
    public short C; // high nibble of the high byte carries the version
    public byte D;  // top two bits carry the variant
}
#pragma warning restore CS0649

ref var fields = ref Unsafe.As<Guid, GuidFields>(ref value);
// version: (fields.C >> 12) & 0x0F        variant: (fields.D & 0xC0) == 0x80
```

The `CS0649` suppression is mandatory rather than cosmetic: nothing assigns these fields in source, so the compiler
reports them as never assigned, and `TreatWarningsAsErrors` turns that into a Release build failure. Keep it narrow,
around the type only.

The obvious implementation — `value.Version == 7 && (value.Variant & 0xC) == 0x8` — is not available. Both properties
are public on `net10.0` (`Version` since .NET 9), but `netstandard2.0` has neither, and `Polyfill` supplies
`Guid.CreateVersion7` there without them. Using them would reintroduce exactly the per-target split this section
exists to avoid. They are the right *oracle*, not the right implementation, and the test project targets `net10.0`
only, so it can use them freely.

The byte-based alternatives are worse still. `Guid.TryWriteBytes(…, bigEndian: true, …)` is .NET 8+, and
`netstandard2.0` has neither it nor the `netstandard2.1` `TryWriteBytes`. That leaves `ToByteArray()` on the legacy
target, which allocates a 16-byte array per call *and* splits the two targets apart: `Guid`'s default byte order is
mixed-endian, so the version nibble sits at index 7 there but at index 6 in a big-endian span. That off-by-one is
exactly the kind of defect this repository cannot catch, because the suite runs on `net10.0` only.

Reading the fields removes both problems and is endianness-independent, since the version comes from a `short` read
rather than a byte position.

This technique is not new to the repository, which is the main argument for it. `CanonicalTextFormatter` already
declares an identical `GuidFields` overlay — same `int, short, short, byte…` shape, same `CS0649` suppression
(`src/Light.PortableResults/Text/CanonicalTextFormatter.cs:1085`) — and reinterprets through it at line 785 to format
GUIDs canonically. The `Guid` layout assumption is therefore already load-bearing and shipping in the core library's
formatting path; this plan reuses an established assumption rather than introducing a new one. Mirror that
declaration, including the pragma and the comment explaining it. The duplication is deliberate: the existing type is
a private nested type in a different assembly, and promoting it to shared public API would widen this change across
package boundaries for no benefit.

Two supporting facts, both already true in the Validation assembly: `TrimStringNormalizer` uses `Unsafe.As` with no
TFM guard, so `System.Runtime.CompilerServices.Unsafe` demonstrably resolves there — it reaches `netstandard2.0`
consumers transitively through `Light.PortableResults` → `System.Text.Json` → `System.Memory` — and the technique
needs no `AllowUnsafeBlocks`, which this project does not set even though the core project does.

The layout assumption is sound and, more to the point, verifiable. `Guid`'s field order is fixed by its
`(int, short, short, byte…)` constructor, by `ToByteArray()`, and by its role as the interop mapping of the Win32
`GUID`; the two expressions above are what `Guid.Version` and `Guid.Variant` themselves compute from `_c` and `_d`.
The assumption spans both fields, so the oracle must too — verifying only the version would leave the `D` offset and
the variant mask unguarded. The matrix test below delegates both to the BCL.

Prototyped against .NET 10: across all 256 matrix cases the predicate agrees with the oracle and both reinterpreted
fields agree with `Guid.Version` and `Guid.Variant` individually, one million calls allocate zero bytes, and the same
source compiles clean for `netstandard2.0` under `TreatWarningsAsErrors` with `Unsafe` resolved only transitively.
The per-field agreement is prototype evidence for this plan, not a shipped test — see the test section.

### Public surface

The invariant gets one name, `IsUuidV7`, on two receivers:

```csharp
namespace Light.PortableResults.Validation;

public static class GuidExtensions
{
    public static bool IsUuidV7(this Guid value);
}

public static partial class Checks
{
    public static Check<Guid> IsUuidV7(this Check<Guid> check, bool shortCircuitOnError = false);
    public static Check<Guid> IsUuidV7(this Check<Guid> check, ErrorOverrides overrides, bool shortCircuitOnError = false);
}
```

The raw predicate is public rather than a private helper like `LooksLikeEmail`: it is properly encapsulated, useful
outside a check chain — a repository or message handler guarding the same invariant — and the root `AGENTS.md`
prefers public APIs. It lives in its own `GuidExtensions` class, not in `Checks`, so that `Checks` remains
exclusively the home of `Check<T>` extensions; `CheckExtensions` is the existing precedent for a top-level
`*Extensions` class in this namespace, and no `GuidExtensions` type exists in the solution today.

Both members share the name because they state the same thing, and nothing forces them apart: the receivers are
`Guid` and `Check<Guid>`, so overload resolution never has to choose between them even though a single
`using Light.PortableResults.Validation;` imports both. The assertion is then the usual short-circuit test plus a
call to the predicate, keeping the bit manipulation in exactly one place.

Importing the namespace does surface `IsUuidV7` on every `Guid` in scope. That is intended — it is the point of
making the predicate public — and it is why the name is specific enough to carry its own meaning at a bare `Guid`
call site.

### Registration points

Adding a built-in rule touches five places beyond the assertion itself; two are easy to miss because omitting them
compiles and fails silently:

- `ValidationErrorTemplates`' **copy constructor** must copy the new `UuidV7` property. It is what `with` expressions
  run, so a missing line silently resets a caller's customized template to the default. A test must customize the
  template through `ValidationErrorTemplates.Default with { … }` and assert the resulting message.
- `BuiltInValidationErrorContracts.Contracts` must register `[ValidationErrorCodes.UuidV7] = ErrorMetadataContract.NoMetadata`,
  and `BuiltInValidationErrorContractsTests`' expected no-metadata code list must grow with it — that test asserts the
  registry's full key set. Without the entry, a validator using the assertion cannot document the code from the
  built-in registry.

The remaining additions, each following its `Email` counterpart exactly:

- the `ValidationErrorCodes.UuidV7` constant;
- a `UuidV7ValidationErrorDefinition` class modeled on `EmailValidationErrorDefinition`, including
  `TryGetStableMessageProvider`, since the message has no per-error parameters and is cacheable;
- the shared `BuiltInValidationErrorDefinitions.UuidV7` static property returning that instance — the class alone is
  not enough, and it is what the assertion passes to `AddBuiltInError`;
- the `ValidationErrorTemplates.UuidV7` property, defaulting to `new DisplayName(" must be a version 7 UUID")`, in
  addition to the copy-constructor line above — the property and the copy are two separate edits to the same type.

Place the assertion and definition in `Checks.Guids.cs` and `BuiltInValidationErrorDefinitions.Guids.cs`, matching the
existing partial-class-per-family layout, and the predicate in `GuidExtensions.cs` beside `CheckExtensions.cs`.

Source generation needs no generator change: it discovers rules through `[ValidationRule(ValidationErrorCodes.UuidV7)]`
on the built-in-message overload, with `[ValidationRuleMessage("{displayName} must be a version 7 UUID")]` supplying
the compile-time example message. The default `ValidationRuleMetadataShape.Registered` is correct — the rule has no
metadata of its own.

### Test data and cases

A single exhaustive matrix replaces hand-picked boundary values. Start from RFC 9562's own example,
`017f22e2-79b0-7cc3-98c4-dc0c0c07398f`, and substitute the version nibble at string index 14 and the variant nibble at
string index 19 across all 16 × 16 combinations, and assert for each of the 256 GUIDs that

```csharp
guid.IsUuidV7() == (guid.Version == 7 && (guid.Variant & 0xC) == 0x8)
```

`Guid.Variant` returns the full high nibble of octet 8 rather than the two variant bits alone, which is why the oracle
masks with `0xC` instead of comparing against `0x2`.

That single boolean is enough to pin both offsets and both masks, even though it never reads the extracted fields
individually — which it could not do anyway, since only the `bool` is public and inventing accessors purely to test
internals would be the wrong trade. The matrix varies the two nibbles independently over their full ranges, so any
error in either field — a shifted offset, a widened or narrowed mask — changes which of the 256 inputs are accepted,
and the oracle disagrees. Assert the accepted count as a second, blunter statement of the same property: exactly 4 of
256 pass, being version `7` crossed with variant nibbles `8`–`b`. Any drift moves that number, which makes it a
useful mutation-testing target.

This subsumes every boundary that a hand-written list would have enumerated (versions `6` and `8`; NCS, Microsoft, and
reserved variants) while being deterministic, so it also replaces the random-sample cross-check. Keep `Guid.Empty`,
`Guid.NewGuid()`, and `Guid.CreateVersion7()` as three separate named cases: they are statements about the platform's
own values rather than about the bit layout, and the matrix cannot make them.

Assertion-level tests then stay small, since the predicate is already pinned: one accepted and one rejected value
through `Check<Guid>`, both overloads, short-circuit propagation, and template customization.

The suite runs on `net10.0` only, so `netstandard2.0` gets compile verification rather than execution. That is an
argument for the shared implementation above rather than something to test around: with no per-target branch, the
behavior these tests pin is the behavior both packages ship, and the layout it rests on is identical on .NET
Framework, where `Guid` is the same interop-mapped type.

### Release notes

Two packages ship a change, so both sets of 0.7.0 notes move:

- **Light.PortableResults.Validation** — the `IsUuidV7` assertion, the `UuidV7` error code, the customizable
  `ValidationErrorTemplates.UuidV7` template, and the new public `GuidExtensions` type.
- **Light.PortableResults.Validation.OpenApi** — the built-in metadata contract registered for the `UuidV7` code.

The OpenApi entry is easy to skip because that package's own source barely changes, but its registry's key set is
public behavior: a consumer reading the built-in contracts, or narrowing error schemas against them, sees a new entry.
Note that these notes are capability-level rather than a per-release changelog, so the entry is one short line in that
register — not a changelog section imported from the Validation package.

### Deliberately out of scope

- **Timestamp plausibility.** A v7 identifier from a client with a wrong clock is structurally valid but useless for
  ordering. Validating the leading 48-bit timestamp against a permitted skew window needs a clock abstraction, a
  configurable tolerance, and metadata carrying the bounds — a separate rule with a separate error code, not a
  parameter on this one.
- **`Check<Guid?>` overloads.** No built-in assertion offers nullable value-type overloads today; `IsNotNull` covers
  the null case generically. Adding them for one assertion would be inconsistent.
- **Other UUID versions.** A generalized `IsUuidVersion(int)` would need version metadata on the error and an OpenAPI
  contract to match. Revisit only if a second version is actually requested.
