# Add an `IsUuidV7` Assertion

## Rationale

UUIDv7 (RFC 9562 §5.7) leads with a 48-bit Unix millisecond timestamp, which keeps client-generated keys roughly sortable and index-friendly — the reason distributed systems let clients mint their own identifiers. A server accepting one cannot currently state that requirement: `Check<Guid>` offers only `IsEmpty`/`IsNotEmpty`, so a v4 GUID from a misconfigured client is accepted and surfaces later as index fragmentation.

`Checks.IsUuidV7` closes the gap as the first Guid-shaped format assertion, structured like the string format assertions (`IsEmail`, `ContainsOnlyDigits`): a dedicated error code, a customizable message template, and a metadata-free OpenAPI contract. More assertions are planned for the library; this plan covers only this one.

## Acceptance Criteria

- [x] `Check<Guid>` exposes `IsUuidV7` in both built-in-message and `ErrorOverrides` overloads, each honoring `shortCircuitOnError` and the already-short-circuited state, matching every other built-in assertion.
- [x] The same invariant is available standalone as public `GuidExtensions.IsUuidV7(this Guid value)`, under that one name, with the assertion delegating to it so the bit manipulation exists in a single place.
- [x] The assertion passes only for GUIDs whose RFC 9562 version field is `7` **and** whose two most significant variant bits are `10`. `Guid.Empty`, `Guid.NewGuid()` (v4), and version-7 values with a non-RFC variant all fail.
- [x] Failures carry the new `ValidationErrorCodes.UuidV7` code and a message from a new customizable `ValidationErrorTemplates.UuidV7` template, which survives `with`-expression copies of `ValidationErrorTemplates`.
- [x] The rule is discoverable by OpenAPI source generation and resolves to a registered metadata-free contract, so a validator using it documents the `UuidV7` code without an explicit hint and without an unknown-code diagnostic.
- [x] An exhaustive version-nibble × variant-nibble matrix checks the predicate against an oracle built from **both** `Guid.Version` and `Guid.Variant`, so the BCL guards the whole layout assumption rather than half of it.
- [x] Automated tests additionally cover both overloads, short-circuit propagation, template customization, and the generated OpenAPI metadata. Solution test coverage stays above 95%.
- [x] One predicate implementation serves both target frameworks, without conditional compilation, and the passing path allocates nothing on either.
- [x] The README shows the assertion where client-generated identifiers are validated, and the 0.7.0 `<PackageReleaseNotes>` of **both** affected packages record their own part of the change.

## Technical Details

### Predicate

RFC 9562 puts the version in the high nibble of octet 6 and the variant in the two most significant bits of octet 8, and both must be checked. `Guid.Version` reports the version regardless of the variant, so `017f22e2-79b0-7cc3-08c4-dc0c0c07398f` — an NCS-reserved variant that `Guid.CreateVersion7` cannot produce — would pass a version-only test. The variant's remaining two bits are free, so nibbles `8`–`b` are all acceptable.

Read both fields by reinterpreting the `Guid` over a struct mirroring its layout, which is allocation-free on both targets and needs no conditional compilation:

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

`CS0649` fires because nothing assigns these fields in source, and `TreatWarningsAsErrors` turns it into a Release build failure, so the suppression is mandatory rather than cosmetic. Keep it narrow, around the type only.

Neither obvious alternative works. `value.Version == 7 && (value.Variant & 0xC) == 0x8` needs two properties that `netstandard2.0` lacks (`Version` arrived in .NET 9; `Polyfill` supplies `CreateVersion7` there without either), so it would reintroduce the per-target split this section exists to avoid. They are the right *oracle*, not the right implementation, and the `net10.0`-only test project may use them freely. The byte-based route is worse: `TryWriteBytes(…, bigEndian: true, …)` is .NET 8+ and `netstandard2.0` has neither it nor the `netstandard2.1` overload, leaving `ToByteArray()`, which allocates 16 bytes per call *and* splits the targets apart — `Guid`'s default order is mixed-endian, putting the version nibble at index 7 there against index 6 in a big-endian span. A `net10.0`-only suite cannot catch that off-by-one. A `short` read carries no such dependency.

The technique is already established here, which is the main argument for it. `CanonicalTextFormatter` declares an identical overlay — same shape, same suppression (`src/Light.PortableResults/Text/CanonicalTextFormatter.cs:1085`) — and reinterprets through it at line 785 to format GUIDs canonically, so the layout assumption is load-bearing in shipping core code rather than new to this change. Mirror that declaration, pragma and comment included. The duplication is deliberate: the existing type is private to another assembly, and promoting it would widen this change across package boundaries for no benefit. Two supporting facts already hold inside the Validation assembly: `TrimStringNormalizer` uses `Unsafe.As` with no TFM guard, so `System.Runtime.CompilerServices.Unsafe` resolves there — reaching `netstandard2.0` consumers through `Light.PortableResults` → `System.Text.Json` → `System.Memory` — and the technique needs no `AllowUnsafeBlocks`, which this project does not set even though the core project does.

The assumption itself is fixed by `Guid`'s `(int, short, short, byte…)` constructor, by `ToByteArray()`, and by its role as the Win32 `GUID` interop mapping; the two expressions above are what `Guid.Version` and `Guid.Variant` compute from `_c` and `_d`. It spans both fields, so the oracle must too — checking only the version would leave the `D` offset and the variant mask unguarded.

Prototyped against .NET 10: across all 256 matrix cases the predicate agrees with the oracle and each field agrees with its property, one million calls allocate zero bytes, and the same source compiles clean for `netstandard2.0` under `TreatWarningsAsErrors` with `Unsafe` resolved only transitively. The per-field agreement is evidence for this plan, not a shipped test.

### Public surface

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

The predicate is public rather than a private helper like `LooksLikeEmail`: it is properly encapsulated, useful outside a check chain — a repository or message handler guarding the same invariant — and the root `AGENTS.md` prefers public APIs. It gets its own class so that `Checks` remains exclusively the home of `Check<T>` extensions; `CheckExtensions` is the precedent for a top-level `*Extensions` class in this namespace, and no `GuidExtensions` exists in the solution today. Both members share the one name because they state the same thing and their receivers differ, so overload resolution never has to choose even though a single `using` imports both. The assertion is then the usual short-circuit test plus a call to the predicate. Importing the namespace does surface `IsUuidV7` on every `Guid` in scope: intended, and why the name is specific enough to carry its meaning at a bare `Guid` call site.

### Registration points

Six places change beyond the assertion. Two fail silently when missed:

- `ValidationErrorTemplates`' **copy constructor** must copy the new property. It is what `with` expressions run, so omitting it silently resets a caller's customized template to the default. A test must customize through `ValidationErrorTemplates.Default with { … }` and assert the resulting message.
- `BuiltInValidationErrorContracts.Contracts` must register `[ValidationErrorCodes.UuidV7] = ErrorMetadataContract.NoMetadata`, and `BuiltInValidationErrorContractsTests`' expected no-metadata list must grow with it, since that test asserts the registry's full key set. Without the entry the code cannot be documented from the built-in registry.

The other four follow their `Email` counterparts: the `ValidationErrorCodes.UuidV7` constant; a `UuidV7ValidationErrorDefinition` modeled on `EmailValidationErrorDefinition`, including `TryGetStableMessageProvider` because the message has no per-error parameters and is cacheable; the shared `BuiltInValidationErrorDefinitions.UuidV7` static property returning that instance, which is what the assertion passes to `AddBuiltInError`; and the `ValidationErrorTemplates.UuidV7` property defaulting to `new DisplayName(" must be a version 7 UUID")` — a separate edit from the copy-constructor line above.

Place the assertion and definition in `Checks.Guids.cs` and `BuiltInValidationErrorDefinitions.Guids.cs`, matching the partial-class-per-family layout, and the predicate in `GuidExtensions.cs` beside `CheckExtensions.cs`. Source generation needs no generator change: it discovers the rule through `[ValidationRule(ValidationErrorCodes.UuidV7)]` on the built-in-message overload, with `[ValidationRuleMessage("{displayName} must be a version 7 UUID")]` as the compile-time example message. The default `ValidationRuleMetadataShape.Registered` is correct — the rule carries no metadata of its own.

### Test data and cases

One exhaustive matrix replaces hand-picked boundaries. From RFC 9562's own example `017f22e2-79b0-7cc3-98c4-dc0c0c07398f`, substitute the version nibble at string index 14 and the variant nibble at index 19 across all 16 × 16 combinations, asserting for each of the 256 GUIDs that

```csharp
guid.IsUuidV7() == (guid.Version == 7 && (guid.Variant & 0xC) == 0x8)
```

`Guid.Variant` returns octet 8's full high nibble rather than the two variant bits alone, hence masking with `0xC` rather than comparing against `0x2`.

That single boolean pins both offsets and both masks without reading the fields individually — which it could not do anyway, since only the `bool` is public, and inventing accessors purely to observe internals would be the wrong trade. Because the matrix varies both nibbles independently over their full ranges, any shifted offset or altered mask changes which of the 256 inputs are accepted, and the oracle disagrees. Assert the accepted count as a blunter statement of the same property: exactly 4 of 256 pass, version `7` crossed with variant nibbles `8`–`b`. Any drift moves that number, which makes it a useful mutation-testing target.

This subsumes every boundary a hand-written list would enumerate (versions `6` and `8`; NCS, Microsoft, and reserved variants) and, being deterministic, also replaces a random sample. Keep `Guid.Empty`, `Guid.NewGuid()`, and `Guid.CreateVersion7()` as named cases: they state things about the platform's own values that the matrix cannot. Assertion-level tests then stay small — one accepted and one rejected value through `Check<Guid>`, both overloads, short-circuit propagation, and template customization.

The suite runs on `net10.0` only, so `netstandard2.0` is compile-verified rather than executed. That argues for the shared implementation rather than for testing around it: with no per-target branch, what these tests pin is what both target assets ship, and the layout holds identically on .NET Framework, where `Guid` is the same interop-mapped type.

### Release notes

Both packages' 0.7.0 notes move. **Validation** gains the assertion, the `UuidV7` code, the customizable template, and the new public `GuidExtensions` type; **Validation.OpenApi** gains the built-in metadata contract registered for that code. The second is easy to skip because that package's own source barely changes, but its registry's key set is public behavior — a consumer reading the built-in contracts, or narrowing error schemas against them, sees a new entry. Those notes are capability-level rather than a per-release changelog, so it is one short line there.

### Deliberately out of scope

- **Timestamp plausibility.** A v7 identifier from a client with a wrong clock is structurally valid but useless for ordering. Checking the leading timestamp against a permitted skew window needs a clock abstraction, a configurable tolerance, and metadata carrying the bounds: a separate rule with its own error code, not a parameter on this one.
- **`Check<Guid?>` overloads.** No built-in assertion offers nullable value-type overloads today; `IsNotNull` covers the null case generically. Adding them for one assertion would be inconsistent.
- **Other UUID versions.** A generalized `IsUuidVersion(int)` would need version metadata on the error and a matching OpenAPI contract. Revisit only if a second version is actually requested.
