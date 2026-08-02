# Add `DateTimeKind` Assertions

## Rationale

With the default `System.Text.Json` converter, a request `DateTime` retains the distinction between a trailing `Z` (`Utc`), an explicit numeric offset converted to server-local time (`Local`), and no zone (`Unspecified`). The library cannot currently require any of these kinds, leaving values vulnerable to later persistence or comparison under the wrong time-zone assumption.

Add `IsUtc`, `IsLocal`, and `IsUnspecified` as the first candidate from #74, following `IsUuidV7` from #72: dedicated error codes, customizable messages, and metadata-free OpenAPI contracts.

## Acceptance Criteria

- [ ] `Check<DateTime>` exposes `IsUtc`, `IsLocal`, and `IsUnspecified` in built-in-message and `ErrorOverrides` overloads; all honor `shortCircuitOnError` and an already-short-circuited check.
- [ ] Failures use new `ValidationErrorCodes.Utc`, `Local`, and `Unspecified` codes and customizable `ValidationErrorTemplates` properties that survive `with`-expression copies.
- [ ] All three built-in-message overloads are discoverable by OpenAPI source generation. A validator using them generates a document against the built-in registry without generator errors, `WithErrorMetadata`, or an unregistered-code failure.
- [ ] A 3×3 kind matrix proves that each assertion accepts exactly one `DateTimeKind` and that together they partition the enum.
- [ ] A DTO deserialization test covers the four ISO 8601 forms below, including `+00:00` separately from a non-zero offset, and asserts both the resulting kind and accepted assertion.
- [ ] `IsUtc` is documented as testing the normalized value's `Kind`, independent of its origin. XML remarks and the README separately explain that the default JSON converter requires trailing `Z` because `+00:00` produces `Local` and is rejected.
- [ ] Tests also cover both overload forms, short-circuit propagation, template customization, and generated OpenAPI metadata. Solution coverage remains above 95%.
- [ ] The README shows the assertions in request validation, and the 0.7.0 `<PackageReleaseNotes>` of both affected packages describe their respective changes.

## Technical Details

### Semantics and normalization

Each predicate compares `check.Value.Kind` with its corresponding `DateTimeKind`. The contract is only the kind of the normalized value: `DateTime.UtcNow`, `DateTime.SpecifyKind`, custom converters, and non-JSON transports may all produce `Utc`. Do not add a standalone `DateTimeExtensions` predicate; direct `Kind` comparison is already the clearest outside a check chain and is available on `netstandard2.0`.

Measured on .NET 10 with the default `System.Text.Json` converter. Only the kind is host-independent; the two `Local` values themselves depend on the server's zone:

| Wire value | `DateTime.Kind` | Accepted by |
| --- | --- | --- |
| `2026-08-02T10:00:00Z` | `Utc` | `IsUtc` |
| `2026-08-02T10:00:00+00:00` | `Local` | `IsLocal` |
| `2026-08-02T10:00:00+02:00` | `Local` | `IsLocal` |
| `2026-08-02T10:00:00` | `Unspecified` | `IsUnspecified` |

Although `+00:00` and `Z` denote the same instant, the default converter maps them to different kinds. Present the trailing-`Z` requirement as JSON-specific guidance in XML remarks, the README, and the OpenAPI description—not as the assertion's transport-independent contract.

`ValidationContext.Check<T>` applies its per-check normalizer or `Options.ValueNormalizer` before creating `Check<T>`. The default `TrimStringNormalizer` preserves non-strings, but a custom normalizer may rewrite a `DateTime`; the assertions then describe that normalized value. Document this and warn in the README that coercing `Unspecified` to `Utc` destroys the signal. Do not guard against that caller choice in code.

`IsLocal` completes the enum partition despite its limited value in portable APIs, where server-relative time is rarely desirable.

### Errors and registration

Use three metadata-free rules rather than `HasKind(DateTimeKind)` with `expectedKind` metadata, consistent with the existing named empty/null rules:

```csharp
public const string Utc = "Utc";
public const string Local = "Local";
public const string Unspecified = "Unspecified";
```

The terse names follow existing codes such as `Empty`, `Null`, and `Email`; the error target disambiguates `Unspecified` for consumers.

Default templates are `new DisplayName(" must be represented in UTC")`, `new DisplayName(" must be a local date and time")`, and `new DisplayName(" must not specify a time zone")`. The UTC message describes representation without claiming a JSON origin; JSON-specific `Z` guidance belongs in contextual documentation. Two rejected alternatives, recorded so they are not reintroduced: `"must be in UTC"` describes the instant and so reads as already satisfied on a `+00:00` payload, and `"must be encoded with a trailing 'Z'"` is false for a value that never arrived as JSON. `IsUnspecified` likewise avoids `"must have an unspecified kind"`, which names a CLR concept the client cannot see.

Follow the `UuidV7` registration shape:

- Add the constants; three definitions with `TryGetStableMessageProvider`, which applies here because these messages have no per-error parameters and are therefore cacheable; three shared `BuiltInValidationErrorDefinitions` properties; and three template defaults/properties.
- Copy all three properties in `ValidationErrorTemplates`' copy constructor so customizations survive subsequent `with` expressions.
- Register all three as `ErrorMetadataContract.NoMetadata` and extend `BuiltInValidationErrorContractsTests`' exhaustive no-metadata list. This requires a generated-document test because a missing registry entry fails during document construction, not through a generator diagnostic.
- Put assertions and definitions in `Checks.Temporal.cs` and `Definitions/BuiltInValidationErrorDefinitions.Temporal.cs`.
- Annotate each built-in-message overload with `[ValidationRule(...)]` and `[ValidationRuleMessage(...)]`; the default `ValidationRuleMetadataShape.Registered` is correct. No generator change is needed, but omitted method-level attributes make a rule silently undiscoverable.

### Tests and documentation

The 3×3 matrix is the central assertion test and must reject both wrong kinds for every rule, killing equality mutations. The wire-format test guards the JSON premise: assert kind and validation outcome, not the time-zone-dependent wall-clock value. It overlaps the matrix by design and must not be folded into it — it is a regression detector for third-party behavior, so if a future `System.Text.Json` release changes the mapping, the assertions keep working while their documented meaning shifts, and this test is what surfaces that.

Retain named cases for `DateTime.UtcNow`, `DateTime.Now`, and `default(DateTime)`. `UtcNow` proves that `IsUtc` tests kind rather than JSON origin; `default(DateTime)` documents the `Unspecified` default-value trap. Keep remaining assertion tests focused on both overloads, short-circuit behavior, and customization through `ValidationErrorTemplates.Default with { … }`.

The Validation package release notes cover the three assertions, codes, and templates. Validation.OpenApi covers the three new registry entries.

### Deliberately out of scope

- **`Check<DateTimeOffset>` assertions.** `Offset == TimeSpan.Zero` is a canonicalization rule, not a kind check. The same `+00:00` JSON value fails `DateTime.IsUtc` but would pass this rule, while both CLR types map to OpenAPI `string`/`date-time`; an unzoned `DateTimeOffset` also inherits the server offset and becomes host-dependent. Any future rule needs its own `ZeroOffset` code/message and design, tracked in #74.
- **`IsInThePast` / `IsInTheFuture`.** Tracked in #74; they need a clock abstraction, tolerance, and a resolved `TimeProvider` strategy for `netstandard2.0`.
- **A UTC-converting normalizer.** Coercion would hide the condition being validated and belongs in mapping.
- **`Check<DateTime?>`.** No built-in assertion has nullable value-type overloads; use `IsNotNull` first.
- **`DateOnly`, `TimeOnly`, and `TimeSpan`.** They carry neither kind nor offset.
